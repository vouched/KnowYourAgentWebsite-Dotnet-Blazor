# Guidelines for AI agents working in this repository

This is an open-source ASP.NET Core (.NET 10) Blazor sample for **Checkpoint by Vouched** (`KyaOs.Checkpoint` NuGet package): middleware that classifies every HTTP request as a human, bot or AI agent and reports agents to the Vouched dashboard.

It's a one-page app for .NET developers who run it locally. Step 1 saves their Vouched keys, step 2 sends simulated agent and bot traffic through Checkpoint, and step 3 links to their dashboard. Keep it that focused: it teaches, it doesn't sell, so don't add marketing sections.

## Commands

Run everything from the repository root.

| Task | Command |
| --- | --- |
| Run the site (https://localhost:7273) | `dotnet run --project src/KnowYourAgentWebsite` |
| Build (warnings are errors) | `dotnet build KnowYourAgentWebsite.slnx -c Release` |
| Test (Microsoft.Testing.Platform + NUnit) | `dotnet test --solution KnowYourAgentWebsite.slnx` |
| Run one fixture | `dotnet test --solution KnowYourAgentWebsite.slnx --filter "FullyQualifiedName~CheckpointDetectionTests"` |
| Verify formatting (CI enforces it) | `dotnet format KnowYourAgentWebsite.slnx --verify-no-changes` |

Always build, test and verify formatting before you finish a change. CI runs all three on Linux, Windows and macOS.

## Secrets: never commit them

- Vouched credentials are `Checkpoint:ProjectId` and the optional `Checkpoint:ApiKey`. They come **only** from configuration: .NET user secrets in Development, or environment variables (`Checkpoint__ProjectId`, `Checkpoint__ApiKey`) and secret stores elsewhere.
- Never add real or placeholder credentials to `appsettings*.json`, source files, tests, docs or workflow files. Tests use the fake values in `WebsiteFactory`.
- Never log, render or return the API key. `CheckpointCredentials.ApiKeyHint` is the only allowed representation.
- Never commit the `.kya-os/` folder that the KYA-OS CLI creates; it is gitignored.
- If a user asks you to "add their API key", tell them to paste it into step 1 on the home page or use `dotnet user-secrets set` (see README.md). Do not write it into the repository.

## Architecture

- `Program.cs` builds and runs the host in a loop so the home page can restart the app in-process after saving keys (`Hosting/ApplicationRestartCoordinator`). The Checkpoint SDK reads its options once at startup, so a host restart is the only way to apply new keys.
- `WebsiteApplication.cs` is the composition root: service registrations and the middleware pipeline. `UseCheckpoint()` must stay **before** `UseRouting()`, `UseAntiforgery()` and the endpoint mappings, as Vouched's .NET guide requires.
- The whole `Checkpoint` configuration section is bound onto the SDK's `CheckpointOptions`. Prefer adding settings to `appsettings.json` over code.
- A Project ID alone is a supported, happy setup: the Checkpoint pixel reports browser visits with just the Project ID. A `PostConfigure<CheckpointOptions>` in `WebsiteApplication.cs` keeps the middleware in local detection-only mode until there's also an API key, because Vouched rejects every server-side report without one (HTTP 401). Keep the API key optional. Sending simulated traffic is the one feature that needs it: without a key, the page says so and never checks off step 2.
- `Configuration/CheckpointCredentialsValidator` fails startup outside Development when the Project ID is missing. Keep its error message actionable.
- Pages use **static server-side rendering** (no interactive render modes); forms use Blazor's enhanced form handling. Read a request's verdict with `HttpContext.GetCheckpointResult()`, checking `WasCheckpointSkipped()` first.
- The developer tools (saving keys and sending simulated traffic) and `/setup/status` are guarded by `LocalDeveloperAccess`: Development environment **and** a loopback client IP **and** a loopback `Host` header **and** none of the headers proxies add (dev tunnels, reverse proxies, CDNs). Don't weaken any of the four.
- `Simulation/TrafficSimulator` runs each simulated visit through a private `UseCheckpoint()` pipeline that shares the app's DI singletons, so detections reach the dashboard exactly like real traffic. It then waits on `DetectionTelemetryQueue.FlushAsync()` and uses `DroppedCount` to tell whether Vouched accepted them.
- `Simulation/SimulatedVisitor.All` lists the visitors. Use each one's real, published user agent, and update `TrafficSimulatorTests.Every_visitor_is_classified_like_the_real_thing` when you change the list.

## Front end

- The stylesheet is hand-written (`wwwroot/assets/css/site.css`) with design tokens in `:root`; there is no CSS framework. Reuse the existing tokens and components.
- Outside Development a strict Content-Security-Policy is sent (`Security/SecurityHeadersMiddleware`). **No inline `<script>`, inline event handlers or `style="..."` attributes.** Put behavior in `wwwroot/assets/js/site.js` and styles in `site.css`.
- Every page must work without JavaScript; scripts are progressive enhancements.
- Icons live in `Components/Elements/Icon.razor`. Add new SVG paths there; never pass user input through `MarkupString`.

## C# conventions

Enforced by `.editorconfig` and the build (`TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild`):

- Tabs for indentation, file-scoped namespaces, `var` where the type is apparent.
- Omit default access modifiers (`private`, `internal` on top-level types).
- Private fields are `_camelCase` (including `static readonly`); constants are `PascalCase`.
- Use `[LoggerMessage]` source-generated logging. It doesn't work inside `.razor` files, so use a code-behind (`*.razor.cs`) for components that log.
- Package versions live only in `Directory.Packages.props` (central package management).

## Tests

- Integration tests use `Infrastructure/WebsiteFactory`, which hosts the real site in memory, replaces the Vouched API with `RecordingCheckpointApiClient` and never touches the network or the developer's real user secrets.
- `RecordingCheckpointApiClient.RejectDeliveries` and `HoldDeliveries()` simulate a wrong API key and a slow network.
- Name tests as behavior sentences (`Crawlers_are_classified_as_bots`) and use `Assert.That` with `Assert.EnterMultipleScope()` for multiple assertions.
- Tests in `Detection/` and `Simulation/` intentionally pin `KyaOs.Checkpoint` behavior. If an SDK update changes a verdict, update the expectation **and** call it out in the PR description. Don't loosen the assertion.
