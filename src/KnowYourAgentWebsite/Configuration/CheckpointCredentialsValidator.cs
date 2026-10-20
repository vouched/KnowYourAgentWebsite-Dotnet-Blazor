using Microsoft.Extensions.Options;

namespace KnowYourAgentWebsite.Configuration;

/// <summary>
/// Fails fast at startup when a deployed (non-Development) instance has no Vouched Project ID.
/// </summary>
/// <remarks>
/// The API key is optional everywhere: without it, the Checkpoint pixel still reports browser visits and the middleware runs
/// in local detection-only mode. In Development the site starts without either value so the home page can walk you through
/// adding them.
/// </remarks>
sealed class CheckpointCredentialsValidator(IHostEnvironment environment) : IValidateOptions<CheckpointCredentials>
{
	public ValidateOptionsResult Validate(string? name, CheckpointCredentials options)
	{
		ArgumentNullException.ThrowIfNull(options);

		if (options.HasProjectId || environment.IsDevelopment())
			return ValidateOptionsResult.Success;

		return ValidateOptionsResult.Fail(CreateMissingProjectIdMessage(environment.EnvironmentName));
	}

	internal static string CreateMissingProjectIdMessage(string environmentName) =>
		string.Join(
			Environment.NewLine,
			$"Missing your Vouched Checkpoint Project ID ({CheckpointCredentials.ProjectIdKey}).",
			string.Empty,
			$"The '{environmentName}' environment requires it. Set it as an environment variable (or in your host's secret store):",
			$"    {CheckpointCredentials.ProjectIdEnvironmentVariable}=<your Vouched project id>",
			$"    {CheckpointCredentials.ApiKeyEnvironmentVariable}=<your Vouched API key>    (optional: also reports server-side detections)",
			string.Empty,
			$"Copy both from Installations in your Checkpoint dashboard: {VouchedLinks.Dashboard}",
			"Running locally? Start the app with ASPNETCORE_ENVIRONMENT=Development and paste them into the home page, or follow README.md.");
}
