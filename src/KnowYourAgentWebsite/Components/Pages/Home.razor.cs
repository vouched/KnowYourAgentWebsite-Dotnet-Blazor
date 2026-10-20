using System.Globalization;
using Checkpoint.Core.Models;
using KnowYourAgentWebsite.Configuration;
using KnowYourAgentWebsite.Hosting;
using KnowYourAgentWebsite.Setup;
using KnowYourAgentWebsite.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace KnowYourAgentWebsite.Components.Pages;

public sealed partial class Home
{
	internal const string ConnectFormName = "connect-project";
	internal const string SimulateFormName = "send-simulated-traffic";

	// Long enough for the browser to finish loading the "Restarting…" panel before the server stops
	static readonly TimeSpan _restartDelay = TimeSpan.FromSeconds(1);

	static readonly (VisitorKind Kind, string Heading)[] _visitorGroups =
	[
		(VisitorKind.AiAgent, "AI agents browsing for a person"),
		(VisitorKind.Bot, "Crawlers and scripts"),
	];

	bool _canUseDeveloperTools;
	bool _canSaveKeys;
	bool _isEditingKeys;
	bool _isRestartPending;
	bool _isSimulatorBusy;
	string? _saveError;
	SimulationReport? _report;

	[Inject]
	LocalDeveloperAccess LocalDeveloperAccess { get; set; } = default!;

	[Inject]
	UserSecretsStore UserSecretsStore { get; set; } = default!;

	[Inject]
	TrafficSimulator TrafficSimulator { get; set; } = default!;

	[Inject]
	IApplicationRestarter ApplicationRestarter { get; set; } = default!;

	[Inject]
	HostInstance HostInstance { get; set; } = default!;

	[Inject]
	IOptions<CheckpointCredentials> CredentialsOptions { get; set; } = default!;

	[Inject]
	IOptions<SiteOptions> SiteOptions { get; set; } = default!;

	[Inject]
	NavigationManager Navigation { get; set; } = default!;

	[Inject]
	ILogger<Home> Logger { get; set; } = default!;

	[CascadingParameter]
	HttpContext? HttpContext { get; set; }

	[SupplyParameterFromForm(FormName = ConnectFormName)]
	CheckpointCredentialsForm? Form { get; set; }

	CheckpointCredentials Credentials => CredentialsOptions.Value;

	bool HasProjectId => Credentials.HasProjectId;

	// Step 2 is only done once the detections are on their way to the dashboard; without an API key they never leave this app
	bool IsTrafficSent => _report is { Delivery: DeliveryStatus.Delivered or DeliveryStatus.InProgress };

	protected override void OnInitialized()
	{
		_canUseDeveloperTools = LocalDeveloperAccess.IsAllowed(HttpContext);
		_canSaveKeys = _canUseDeveloperTools && UserSecretsStore.IsAvailable;

		// The form model is only bound when it was just submitted, which keeps "Use a different project" open to show errors
		_isEditingKeys = Form is not null;
		Form ??= new CheckpointCredentialsForm { ProjectId = Credentials.ProjectId };
	}

	async Task SaveKeysAsync()
	{
		// Defense in depth: the form is never rendered for remote requests, but never trust a POST on its own
		if (!_canSaveKeys || HttpContext is null || Form is null)
			return;

		// A blank API key is fine (the pixel only needs the Project ID), and removes any key saved for a previous project
		var apiKey = Form.ApiKey?.Trim();
		var values = new Dictionary<string, string?>
		{
			[CheckpointCredentials.ProjectIdKey] = Form.ProjectId!.Trim(),
			[CheckpointCredentials.ApiKeyKey] = string.IsNullOrEmpty(apiKey) ? null : apiKey,
		};

		try
		{
			await UserSecretsStore.SaveAsync(values, HttpContext.RequestAborted);
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
		{
			LogSaveFailed(Logger, exception, UserSecretsStore.FilePath);
			_saveError = $"Your keys couldn't be saved: {exception.Message}";
			return;
		}

		LogSaved(Logger, values[CheckpointCredentials.ProjectIdKey]!, values[CheckpointCredentials.ApiKeyKey] is not null, UserSecretsStore.FilePath);

		// Checkpoint reads its options once at startup, so restart the host (after this response is sent) to apply the keys
		_isRestartPending = true;
		HttpContext.Response.OnCompleted(() =>
		{
			ApplicationRestarter.ScheduleRestart(_restartDelay);
			return Task.CompletedTask;
		});
	}

	async Task SendTrafficAsync()
	{
		// Defense in depth: the button is never rendered for remote requests, but never trust a POST on its own
		if (!_canUseDeveloperTools || !HasProjectId || HttpContext is null)
			return;

		_report = await TrafficSimulator.SendAsync(new Uri(Navigation.BaseUri), HttpContext.RequestAborted);
		_isSimulatorBusy = _report is null;
	}

	string SourceUri(string path) => $"{SiteOptions.Value.RepositoryUri.AbsoluteUri.TrimEnd('/')}/blob/main/{path}";

	string ReadmeUri(string section) => $"{SiteOptions.Value.RepositoryUri.AbsoluteUri.TrimEnd('/')}#{section}";

	static string StepClass(bool isComplete, bool isCurrent) => isComplete ? "step--complete" : isCurrent ? "step--current" : "step--upcoming";

	static string VerdictLabel(SimulatedVisit visit) => visit.Class switch
	{
		DetectionClassType.AiAgent => "AI agent",
		DetectionClassType.Bot => "Bot",
		DetectionClassType.Human => "Human",
		DetectionClassType.IncompleteData => "Inconclusive",
		DetectionClassType.Unknown => "Unknown",
		_ => "Not analyzed",
	};

	static string VerdictTone(SimulatedVisit visit) => visit.Class switch
	{
		DetectionClassType.AiAgent => "agent",
		DetectionClassType.Bot => "bot",
		DetectionClassType.Human => "human",
		_ => "muted",
	};

	static string VerdictDetail(SimulatedVisit visit)
	{
		var confidence = visit.Confidence is > 0 and < 1
			? "<1%"
			: string.Create(CultureInfo.InvariantCulture, $"{Math.Round(visit.Confidence):0}%");

		var detectedAs = visit.DetectedAs is null ? confidence : $"{visit.DetectedAs} · {confidence}";

		return visit.WasAllowed ? detectedAs : $"{detectedAs} · blocked";
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Saved Vouched Checkpoint project {ProjectId} (API key added: {HasApiKey}) to {SecretsFilePath}")]
	static partial void LogSaved(ILogger logger, string projectId, bool hasApiKey, string? secretsFilePath);

	[LoggerMessage(Level = LogLevel.Error, Message = "Couldn't save Vouched Checkpoint keys to {SecretsFilePath}")]
	static partial void LogSaveFailed(ILogger logger, Exception exception, string? secretsFilePath);
}
