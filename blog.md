# Is That Request From ChatGPT? Detecting AI Agents in ASP.NET Core

```text
KYA-Detected: true
KYA-Class: AiAgent
KYA-Agent: ChatGPT
```

Those headers came back from a plain ASP.NET Core app when ChatGPT asked it for a page, a few minutes after we added [Vouched KYA](https://kya.vouched.id) to it. Without them, a request from an AI agent browsing for one of its users looks like any other `GET`.

In this post, we'll add Detect, Vouched KYA's free tier, to an ASP.NET Core app and catch a few AI agents in the act.

## How Detect works

Detect ships as ASP.NET Core middleware in the [`KyaOs.Checkpoint`](https://www.nuget.org/packages/KyaOs.Checkpoint) NuGet package, and it runs Vouched's detection engine right inside our app. For every request, it:

- Classifies the visitor as a human, an AI agent or a bot, and names the agent when it recognizes one
- Adds that verdict to the response as `KYA-*` headers
- Reports the AI agents and bots it finds to our Vouched KYA dashboard

By default, it only observes: it never blocks a request. And yes, we'll see the name Checkpoint in the code below. That's what the package and its APIs are called.

## What we'll need

- An ASP.NET Core app on .NET 8 or higher
- A free [Vouched KYA account](https://kya.vouched.id/signup) with a project, plus that project's **Project ID** and an **API key** (it starts with `sk_`). The [Credentials docs](https://kya.vouched.id/docs/credentials) show where to find both.

## Add Detect in three steps

### 1. Install the package

From the folder that holds our app's `.csproj`:

```bash
dotnet add package KyaOs.Checkpoint
```

It's a metapackage, so NuGet picks the right adapter for our target framework: `Checkpoint.AspNetCore` for ASP.NET Core.

### 2. Put our keys in user secrets

Our API key is a secret, and [.NET user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) keep it in our user profile instead of our repository:

```bash
dotnet user-secrets init
dotnet user-secrets set "Checkpoint:ProjectId" "<your project id>"
dotnet user-secrets set "Checkpoint:ApiKey" "<your api key>"
```

In production, we set the same two values as the environment variables `Checkpoint__ProjectId` and `Checkpoint__ApiKey`, or in our host's secret store, like Azure Key Vault. Either way, our code doesn't change.

Let's also add a `Checkpoint` section to `appsettings.json`:

```json
"Checkpoint": {
  "OnAgentDetected": "Log"
}
```

`Log` is already the default, but spelling it out tells the next developer we're only observing on purpose.

### 3. Register the middleware

Here's the `Program.cs` that `dotnet new web` generates, plus two `using` directives and three lines of code:

```csharp
using Checkpoint.AspNetCore.Extensions;
using Checkpoint.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Register Vouched KYA, and read every setting under "Checkpoint" from configuration
builder.Services.AddCheckpoint();
builder.Services.AddOptions<CheckpointOptions>().BindConfiguration("Checkpoint");

var app = builder.Build();

// Classify each request before the rest of the pipeline runs
app.UseCheckpoint();

app.MapGet("/", () => "Hello World!");

app.Run();
```

In an existing app, `UseCheckpoint()` goes before `UseRouting()`, `UseAuthentication()` and `UseAuthorization()`. And because `BindConfiguration()` reads the whole `Checkpoint` section, we can change any option the SDK supports in configuration, without recompiling.

## Catch an AI agent in the act

Let's start our app on a predictable URL:

```bash
dotnet run --urls http://localhost:5080
```

Then, from a second terminal, let's pretend to be ChatGPT (in Windows PowerShell, type `curl.exe` instead of `curl`):

```bash
curl -i -A "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko); compatible; ChatGPT-User/1.0; +https://openai.com/bot" http://localhost:5080/
```

Among the response headers are the ones from the top of this post: `KYA-Class: AiAgent` and `KYA-Agent: ChatGPT`. Gotcha! 🎉

Now let's be GPTBot, OpenAI's crawler that collects training data instead of browsing for a person:

```bash
curl -i -A "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko); compatible; GPTBot/1.4; +https://openai.com/gptbot" http://localhost:5080/
```

This time, it's `KYA-Class: Bot` and `KYA-Agent: GPTBot`. Even a plain `curl http://localhost:5080/` gets caught, as a bot named `Dev Tools`.

Within a minute, all three requests show up in our [Vouched KYA dashboard](https://kya.vouched.id/dashboard) under **Detect → Activity**.

![The Detect → Activity page in the Vouched KYA dashboard, listing AI agents and bots](docs/images/dashboard-activity.png)

## Conclusion

Our ASP.NET Core app now knows whether each request came from a person, a crawler or an AI agent browsing for someone. All it took was one NuGet package and three lines of code, and the complete code is in the [open-source sample](https://github.com/TheCodeTraveler/KnowYourAgentWebsite-Dotnet-Blazor).

Now it's your turn! [Create your free Vouched KYA account](https://kya.vouched.id/signup), add Detect to your ASP.NET Core app, and see which AI agents are already visiting it. 👀
