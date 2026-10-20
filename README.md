# Know Your Agent: a Checkpoint by Vouched sample for ASP.NET Core

[![Build and Test](https://github.com/TheCodeTraveler/KnowYourAgentWebsite-Dotnet-Blazor/actions/workflows/build.yml/badge.svg)](https://github.com/TheCodeTraveler/KnowYourAgentWebsite-Dotnet-Blazor/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-453FC1.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![KyaOs.Checkpoint on NuGet](https://img.shields.io/nuget/v/KyaOs.Checkpoint.svg?label=KyaOs.Checkpoint)](https://www.nuget.org/packages/KyaOs.Checkpoint)

A small ASP.NET Core app that shows how to add [Checkpoint by Vouched](https://kya.vouched.id/docs/integrations/dotnet) to a .NET website. Run it, connect your Vouched project, send it simulated AI agent and bot traffic, and watch every visit arrive in your Checkpoint dashboard.

![ChatGPT, Claude, Perplexity, GPTBot, ClaudeBot, Googlebot, Headless Chrome, curl and a Python script, each classified by Checkpoint and delivered to the dashboard](docs/images/simulated-traffic.png)

## Get your Project ID and API key

You need a free Vouched account and a Checkpoint project. Detect is free, so there's no credit card to enter.

### 1. Create a project

1. Sign up at [kya.vouched.id/signup](https://kya.vouched.id/signup), or [sign in](https://kya.vouched.id/signin) if you already have an account. The first time you sign in, you're asked to create an organization.
2. Click **New** in the top navigation, then **New project**.
3. Enter a **Project Name**, like `Know Your Agent sample`, and the **Site URL** of the website you'll protect, like `https://example.com`. This sample still runs on `localhost`.
4. Click **Add Domain**. The project is created right away, and the dialog shows its installation steps.

### 2. Copy your Project ID

Open **Installations** in the project's left rail and start the Detect setup with **Configure & install**. Every install snippet includes your Project ID: in the ASP.NET Core snippet it's the value of `options.ProjectId`, and in the pixel snippet it's `data-project-id`. The ID is all you need from the snippet, because this sample already contains the rest.

### 3. Create an API key (optional)

[Install the `kya-os` CLI](https://kya.vouched.id/docs/installation). Use the following commands to create your API key and save it, along with your Project ID, to this app's user secrets. Skip the `Program.cs` lines the CLI prints; [`WebsiteApplication.cs`](src/KnowYourAgentWebsite/WebsiteApplication.cs) already has them.

```bash
cd src/KnowYourAgentWebsite
kya-os login
kya-os detect install --surface sdk --project <your project id>
```

Once these commands are run, the API Key will be automatically added to the Blazor app. You can then retrieve the Project ID and API Key by running the following command:

```bash
dotnet user-secrets list
```

The API key is optional:

| You add | What shows up in your dashboard |
| --- | --- |
| Project ID | Your visits to this sample, reported by the Checkpoint pixel in your browser |
| Project ID and API key | Your visits, plus everything the Checkpoint middleware detects, including the simulated traffic |

## Run it

New to .NET? No problem. These steps start from a computer with nothing installed. If you already have the .NET 10 SDK, skip to [step 2](#2-download-the-code).

### 1. Install the prerequisites

You need two free tools, and a terminal to type commands into: **Terminal** on macOS, **Windows Terminal** or **PowerShell** on Windows, or the terminal built into [Visual Studio Code](https://code.visualstudio.com).

**The .NET 10 SDK** builds and runs .NET apps, and gives you the `dotnet` command used below. This sample needs version 10 specifically (see [`global.json`](global.json)):

| Your computer | How to install the .NET 10 SDK |
| --- | --- |
| Windows | Download and run the **SDK** installer (x64, or Arm64 for Arm devices) from [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0), or run `winget install Microsoft.DotNet.SDK.10` |
| macOS | Download and run the **SDK** installer from [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0): Arm64 for Apple silicon, x64 for Intel Macs |
| Linux | Follow [Install .NET on Linux](https://learn.microsoft.com/dotnet/core/install/linux), which covers the package managers of each distribution |

**Git** downloads the code. Many computers already have it:

| Your computer | How to install Git |
| --- | --- |
| Windows | Download and run the installer from [git-scm.com/downloads](https://git-scm.com/downloads), or run `winget install --id Git.Git -e` |
| macOS | Run `xcode-select --install`, which installs Apple's developer tools, Git included |
| Linux | Use your package manager, for example `sudo apt install git` |

When the installers finish, **close your terminal and open a new one**, so it picks up the new commands. Then check that everything is ready:

```bash
dotnet --version
git --version
```

You should see a version that starts with `10.0` for .NET, and any version number for Git.

Finally, trust the HTTPS development certificate that the .NET SDK installed, so your browser accepts `https://localhost`. You only need to do this once per computer:

```bash
dotnet dev-certs https --trust
```

Windows and macOS ask you to confirm; macOS asks for your password. On Linux, `--trust` doesn't work on every distribution, so see [Trust the HTTPS certificate on Linux](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl#trust-https-certificate-on-linux), or use the HTTP-only option in step 3.

> [!TIP]
> Rather not install Git? On this repository's GitHub page, click **Code**, then **Download ZIP**, unzip it and open a terminal in the unzipped folder. Then skip the `git clone` and `cd` commands below.

### 2. Download the code

```bash
git clone https://github.com/TheCodeTraveler/KnowYourAgentWebsite-Dotnet-Blazor.git
cd KnowYourAgentWebsite-Dotnet-Blazor
```

### 3. Start the app

```bash
dotnet run --project src/KnowYourAgentWebsite
```

The first run downloads the app's NuGet packages and builds it, which takes a minute. When you see `Application started`, open **https://localhost:7273** in your browser:

```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7273
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5272
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

To stop the app, click into the terminal and press **Ctrl+C**. Prefer an IDE? Open `KnowYourAgentWebsite.slnx` in Visual Studio, Visual Studio Code with the C# Dev Kit, or Rider, and press **F5**.

Can't trust the development certificate? Run `dotnet run --project src/KnowYourAgentWebsite --launch-profile http` instead, and open **http://localhost:5272**.

### 4. Follow the three steps on the page

1. **Connect your Vouched project.** Paste your Project ID, and your API key if you have it. The app saves them to your [.NET user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) and restarts itself to apply them.
2. **Send simulated traffic.** This step requires an API key. One button sends a request from ChatGPT, Claude, Perplexity, GPTBot, ClaudeBot, Googlebot, Headless Chrome, curl and a Python script through the Checkpoint middleware, shows how Checkpoint classified each one, and reports every visit to your dashboard.
3. **Watch it arrive in your dashboard.** Open [your dashboard](https://kya.vouched.id/dashboard), switch to the project you connected, and go to **Detect → Activity**. With an API key, each simulated visit appears with its class, confidence and agent name, usually within a minute. With only a Project ID, look for your own visit, reported by the pixel.

![Step 1: paste your Project ID and, optionally, your API key](docs/images/connect-your-project.png)

![Step 3: the simulated visits in your dashboard's Activity feed, with the Python script's visit open to show how Checkpoint identified it](docs/images/dashboard-activity.png)

> [!NOTE]
> Saving keys and sending traffic only work when the app runs in the `Development` environment and you browse it from the same machine. A deployed copy never exposes them, and they refuse requests from dev tunnels and reverse proxies that add forwarding headers. Like any app running in `Development`, don't expose it to the internet.

Prefer the command line? These write to the same user-secrets file. The second one is optional:

```bash
dotnet user-secrets set "Checkpoint:ProjectId" "<your project id>" --project src/KnowYourAgentWebsite
dotnet user-secrets set "Checkpoint:ApiKey" "<your api key>" --project src/KnowYourAgentWebsite
```

### If something goes wrong

- **`dotnet` or `git` is "not recognized" or "command not found".** Close your terminal (or your IDE) and open it again, so it picks up the newly installed tools. If that doesn't help, restart your computer.
- **"A compatible .NET SDK was not found".** This sample needs the .NET 10 SDK, even if you have a newer or older one. Install it as described above; .NET SDKs install side by side.
- **Your browser warns that the connection isn't private.** Run `dotnet dev-certs https --trust`, then restart your browser.
- **"Address already in use".** Another app, maybe another copy of this one, is using port 7273 or 5272. Stop it, or change the ports in [`launchSettings.json`](src/KnowYourAgentWebsite/Properties/launchSettings.json).
- **Nothing shows up in your dashboard.** See [Not seeing traffic in your dashboard?](#not-seeing-traffic-in-your-dashboard)

### Not seeing traffic in your dashboard?

- **Check the project.** The dashboard shows one project at a time, and the app sends everything to the Project ID in step 1. You'll find each project's ID under **Installations**.
- **Check Activity's filters.** Widen the time range and clear any filters. Your own visit is classified as human.
- **Add an API key for the simulated traffic.** With only a Project ID, the dashboard shows the visits the pixel reports from your browser, not the simulated traffic.
- **Check the pixel.** Browser extensions that block trackers can block `kya.vouched.id/pixel.js`. Set `CheckpointPixel:Debug` to `true` in `appsettings.json` and the pixel logs every report it sends to your browser's console.

## How it works

Checkpoint is ASP.NET Core middleware from the [`KyaOs.Checkpoint`](https://www.nuget.org/packages/KyaOs.Checkpoint) NuGet package. It classifies every request as a human, an AI agent or a bot, adds `KYA-*` headers describing the verdict, and reports agents and bots to your dashboard.

| File | What to look at |
| --- | --- |
| [`WebsiteApplication.cs`](src/KnowYourAgentWebsite/WebsiteApplication.cs) | `AddCheckpoint()` binds the `Checkpoint` configuration section, and `UseCheckpoint()` runs before routing so every request is classified. Without an API key, the middleware stays in local detection-only mode instead of sending reports Vouched would reject |
| [`CheckpointPixel.razor`](src/KnowYourAgentWebsite/Components/Elements/CheckpointPixel.razor) | Adds the Checkpoint pixel, which only needs your Project ID |
| [`appsettings.json`](src/KnowYourAgentWebsite/appsettings.json) | Checkpoint's settings, starting in observe mode (`OnAgentDetected: Log`) |
| [`TrafficSimulator.cs`](src/KnowYourAgentWebsite/Simulation/TrafficSimulator.cs) | Runs each simulated visit through a private `UseCheckpoint()` pipeline, reads the verdict with `HttpContext.GetCheckpointResult()` and waits for Vouched to receive the detections |
| [`SimulatedVisitor.cs`](src/KnowYourAgentWebsite/Simulation/SimulatedVisitor.cs) | The real user agents of the simulated visitors. Add your own! |
| [`CheckpointCredentialsValidator.cs`](src/KnowYourAgentWebsite/Configuration/CheckpointCredentialsValidator.cs) | Outside Development, the app refuses to start without a Project ID and says exactly how to add one |

Checkpoint recognizes the simulated visitors by the user agent each one declares. That tells Checkpoint the agent's name, not who really sent the request. Agents that sign their requests can be [verified cryptographically](https://kya.vouched.id/docs/detect/verification).

## Keeping your keys out of git

- Keys are only read from configuration: user secrets locally, environment variables (`Checkpoint__ProjectId`, `Checkpoint__ApiKey`) or your host's secret store when deployed.
- User secrets live in your user profile, outside the repository. The app writes them with owner-only file permissions.
- The API key is never displayed; the page only shows its last four characters.
- `.gitignore` excludes `secrets.json`, `.env` files, `appsettings.*.local.json` and the `.kya-os/` folder created by the KYA-OS CLI.
- The Checkpoint browser pixel only loads once *your* Project ID is configured, so a fork never reports to someone else's project.

## Configuration

Everything under `Checkpoint` in [`appsettings.json`](src/KnowYourAgentWebsite/appsettings.json) is bound onto the SDK's `CheckpointOptions`, so any option the SDK supports can be set without code changes. See the [.NET integration guide](https://kya.vouched.id/docs/integrations/dotnet) for the full list.

| Setting | Default | What it does |
| --- | --- | --- |
| `Checkpoint:ProjectId` | *(none)* | Your Checkpoint project. The pixel only needs this. |
| `Checkpoint:ApiKey` | *(none)* | Optional. Lets the middleware report its detections and load your dashboard policies. **Never commit it.** |
| `Checkpoint:OnAgentDetected` | `Log` | What to do with a detected agent: `Log`, `Allow`, `Block`, `Redirect`, `Instruct` or `Challenge` |
| `Checkpoint:AgentResponseFormat` | `PlainText` | `PlainText` answers a blocked agent with a notice it can relay to its user (HTTP 200); `Json` returns `403 Forbidden` |
| `Checkpoint:ConfidenceThreshold` | `70` | Minimum confidence before `OnAgentDetected` applies |
| `Checkpoint:SkipPaths` | static assets, `/healthz` | Paths Checkpoint never analyzes |
| `CheckpointPixel:Enabled` | `true` | Adds the Checkpoint pixel for browser-side signals |
| `CheckpointPixel:Debug` | `false` | Logs every report the pixel sends to your browser's console |
| `Site:Name`, `Site:RepositoryUri` | | Name and source links for your fork |

> [!TIP]
> Change `Checkpoint:OnAgentDetected` to `Block`, restart the app and send the simulated traffic again. Checkpoint blocks the AI agents, AI crawlers and Headless Chrome, but lets Googlebot, curl and the Python script through.

## Run the tests

```bash
dotnet test --solution KnowYourAgentWebsite.slnx
```

No Vouched keys or network access are needed. [`WebsiteFactory`](tests/KnowYourAgentWebsite.Tests/Infrastructure/WebsiteFactory.cs) hosts the real app in memory and swaps the Vouched API for a recording fake, so every request still runs through the real Checkpoint middleware and detection engine.

| Test fixture | What it shows |
| --- | --- |
| [`CheckpointDetectionTests`](tests/KnowYourAgentWebsite.Tests/Detection/CheckpointDetectionTests.cs) | How AI agents, crawlers, scripts and a real Chrome browser are classified, read from the `KYA-*` response headers |
| [`CheckpointEnforcementTests`](tests/KnowYourAgentWebsite.Tests/Detection/CheckpointEnforcementTests.cs) | What `Log` and `Block` do, and that blocking never affects people |
| [`TrafficSimulatorTests`](tests/KnowYourAgentWebsite.Tests/Simulation/TrafficSimulatorTests.cs) | That every simulated visit is classified and delivered to your dashboard |
| [`ConnectProjectTests`](tests/KnowYourAgentWebsite.Tests/Setup/ConnectProjectTests.cs) | That saving keys writes user secrets, restarts the app and stays invisible to other machines |

These tests pin the behavior of the `KyaOs.Checkpoint` package. When Dependabot proposes an update that changes how traffic is classified, CI tells you before your users notice.

## Deploying

Publish with `dotnet publish src/KnowYourAgentWebsite -c Release`, set `Checkpoint__ProjectId` (and optionally `Checkpoint__ApiKey`) in your host's configuration, and point its health probe at `/healthz`. Behind a reverse proxy or CDN, review Checkpoint's `ClientIpPolicy` option and [ASP.NET Core forwarded headers](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer) so Checkpoint sees the real client IP.

## Contributing

Issues and pull requests are welcome. Before you open a PR, run:

```bash
dotnet format KnowYourAgentWebsite.slnx --verify-no-changes
dotnet build KnowYourAgentWebsite.slnx -c Release
dotnet test --solution KnowYourAgentWebsite.slnx -c Release
```

The build treats warnings as errors and enforces the style in [`.editorconfig`](.editorconfig). AI coding agents should read [`AGENTS.md`](AGENTS.md). Please report security issues privately as described in [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE)
