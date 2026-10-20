using Checkpoint.Core.Api;
using KnowYourAgentWebsite.Hosting;
using KnowYourAgentWebsite.Setup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace KnowYourAgentWebsite.Tests.Infrastructure;

/// <summary>
/// Hosts the real website in memory. Every request flows through the real Checkpoint middleware and its detection engine,
/// but nothing leaves your machine: the Vouched API client is replaced by <see cref="RecordingCheckpointApiClient"/>.
/// </summary>
/// <remarks>No Vouched keys are needed to run the tests, locally or in CI.</remarks>
public sealed class WebsiteFactory : WebApplicationFactory<Program>
{
	public const string TestingEnvironment = "Testing";
	public const string TestProjectId = "test-project-id";
	public const string TestApiKey = "test-api-key-0123456789";

	readonly string _environment;
	readonly bool _isLocalClient;
	readonly Dictionary<string, string?> _settings;
	readonly string _userSecretsDirectory = Path.Combine(Path.GetTempPath(), "KnowYourAgentWebsite.Tests", Guid.NewGuid().ToString("N"));

	/// <param name="environment">The ASP.NET Core environment, e.g. <see cref="Environments.Development"/> to enable the local developer tools on the home page.</param>
	/// <param name="isConfigured">Whether the site has Vouched credentials. Real user secrets are always overridden.</param>
	/// <param name="isLocalClient">Whether requests appear to come from the machine running the site (127.0.0.1).</param>
	/// <param name="settings">Extra configuration, e.g. <c>["Checkpoint:OnAgentDetected"] = "Block"</c>.</param>
	public WebsiteFactory(
		string environment = TestingEnvironment,
		bool isConfigured = true,
		bool isLocalClient = true,
		IReadOnlyDictionary<string, string?>? settings = null)
	{
		_environment = environment;
		_isLocalClient = isLocalClient;
		_settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
		{
			["Checkpoint:ProjectId"] = isConfigured ? TestProjectId : string.Empty,
			["Checkpoint:ApiKey"] = isConfigured ? TestApiKey : string.Empty,

			// Defense in depth: even if something bypassed the fake API client, it could only reach a closed local port
			["Checkpoint:BaseUrl"] = "http://127.0.0.1:9",
		};

		foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
			_settings[key] = value;
	}

	public RecordingCheckpointApiClient CheckpointApi { get; } = new();

	public RecordingApplicationRestarter Restarter { get; } = new();

	public string UserSecretsFilePath => Path.Combine(_userSecretsDirectory, "secrets.json");

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment(_environment);
		builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));
		builder.ConfigureTestServices(services =>
		{
			services.RemoveAll<ICheckpointApiClient>();
			services.RemoveAll<IDetectionBatchReporter>();
			services.AddSingleton<ICheckpointApiClient>(CheckpointApi);
			services.AddSingleton<IDetectionBatchReporter>(CheckpointApi);

			services.RemoveAll<UserSecretsStore>();
			services.AddSingleton(_ => new UserSecretsStore(UserSecretsFilePath));

			services.RemoveAll<IApplicationRestarter>();
			services.AddSingleton<IApplicationRestarter>(Restarter);

			if (_isLocalClient)
				services.AddSingleton<IStartupFilter, LoopbackClientStartupFilter>();
		});
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing && Directory.Exists(_userSecretsDirectory))
			Directory.Delete(_userSecretsDirectory, recursive: true);
	}
}
