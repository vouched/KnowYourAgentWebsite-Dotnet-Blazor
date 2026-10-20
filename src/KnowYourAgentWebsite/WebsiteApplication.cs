using Checkpoint.AspNetCore.Extensions;
using Checkpoint.Core.Configuration;
using KnowYourAgentWebsite.Components;
using KnowYourAgentWebsite.Configuration;
using KnowYourAgentWebsite.Hosting;
using KnowYourAgentWebsite.Security;
using KnowYourAgentWebsite.Setup;
using KnowYourAgentWebsite.Simulation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace KnowYourAgentWebsite;

/// <summary>Builds the web host: service registrations and the HTTP request pipeline.</summary>
public static partial class WebsiteApplication
{
	public const string HealthCheckPath = "/healthz";

	public static WebApplication Build(string[] args, ApplicationRestartCoordinator restartCoordinator)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddWebsiteServices(restartCoordinator);

		var app = builder.Build();

		app.UseWebsitePipeline();

		return app;
	}

	static IServiceCollection AddWebsiteServices(this IServiceCollection services, ApplicationRestartCoordinator restartCoordinator)
	{
		services.TryAddSingleton(TimeProvider.System);

		// In-process restarts, used to apply newly saved keys
		services.AddSingleton(restartCoordinator);
		services.AddSingleton<HostInstance>();
		services.AddSingleton<IApplicationRestarter, ApplicationRestarter>();

		// Vouched Checkpoint: every option under the "Checkpoint" configuration section is bound onto the SDK's CheckpointOptions
		services.AddCheckpoint();
		services.AddOptions<CheckpointOptions>().BindConfiguration(CheckpointCredentials.SectionName);

		// The pixel only needs a Project ID, but the middleware needs the API key too: without it, Vouched rejects every
		// detection the middleware reports (HTTP 401). So keep the middleware in local detection-only mode until both are set.
		services.PostConfigure<CheckpointOptions>(static options =>
		{
			if (string.IsNullOrWhiteSpace(options.ApiKey))
				options.ProjectId = string.Empty;
		});

		services.AddOptions<CheckpointCredentials>().BindConfiguration(CheckpointCredentials.SectionName).ValidateOnStart();
		services.AddSingleton<IValidateOptions<CheckpointCredentials>, CheckpointCredentialsValidator>();
		services.AddOptions<CheckpointPixelOptions>().BindConfiguration(CheckpointPixelOptions.SectionName);
		services.AddOptions<SiteOptions>().BindConfiguration(SiteOptions.SectionName);

		// Local developer tools: saving your keys and sending simulated traffic
		services.AddSingleton<LocalDeveloperAccess>();
		services.AddSingleton(static _ => new UserSecretsStore(UserSecretsStore.GetDefaultFilePath(typeof(WebsiteApplication).Assembly)));
		services.AddSingleton<TrafficSimulator>();

		services.AddRazorComponents();
		services.AddHealthChecks();

		return services;
	}

	static void UseWebsitePipeline(this WebApplication app)
	{
		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/error", createScopeForErrors: true);
			app.UseHsts();
		}

		app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
		app.UseHttpsRedirection();
		app.UseMiddleware<SecurityHeadersMiddleware>();

		// Checkpoint runs before routing so it can classify (and, if configured, block) every request
		app.UseCheckpoint();

		app.UseRouting();
		app.UseAntiforgery();

		app.MapStaticAssets();
		app.MapHealthChecks(HealthCheckPath);
		app.MapSetupEndpoints();
		app.MapRazorComponents<App>();

		if (app.Environment.IsDevelopment())
			app.Lifetime.ApplicationStarted.Register(static state => LogDevelopmentGuidance((WebApplication)state!), app);
	}

	static void LogDevelopmentGuidance(WebApplication app)
	{
		var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(WebsiteApplication).FullName!);
		var credentials = app.Services.GetRequiredService<IOptions<CheckpointCredentials>>().Value;

		var siteUrl = app.Urls.OrderByDescending(static url => url.StartsWith("https", StringComparison.OrdinalIgnoreCase)).FirstOrDefault() ?? string.Empty;

		if (credentials.CanReportServerDetections)
			LogConnected(logger, credentials.ProjectId!);
		else if (credentials.HasProjectId)
			LogPixelOnly(logger, credentials.ProjectId!, siteUrl);
		else
			LogNotConnected(logger, siteUrl);
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Connected to Vouched Checkpoint project {ProjectId}")]
	static partial void LogConnected(ILogger logger, string projectId);

	[LoggerMessage(Level = LogLevel.Information, Message = "The Checkpoint pixel is reporting browser visits to project {ProjectId}. The middleware runs in local detection-only mode until you add your API key at {SiteUrl}")]
	static partial void LogPixelOnly(ILogger logger, string projectId, string siteUrl);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Vouched Checkpoint is running in local detection-only mode. Open {SiteUrl} to connect your Vouched project; your keys are saved to your .NET user secrets, outside of this repository")]
	static partial void LogNotConnected(ILogger logger, string siteUrl);
}
