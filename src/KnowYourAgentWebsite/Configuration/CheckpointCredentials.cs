namespace KnowYourAgentWebsite.Configuration;

/// <summary>
/// The values that connect this site to your Vouched Checkpoint project. Both come from Installations in the Checkpoint
/// dashboard (<see cref="VouchedLinks.Dashboard"/>) and are read from configuration, never from source code.
/// </summary>
/// <remarks>
/// <para>
/// The Project ID is all the Checkpoint pixel needs to report browser visits to your dashboard. The API key is optional: add it
/// so the server-side middleware can report its detections (like this page's simulated traffic) and load your dashboard policies.
/// </para>
/// <para>
/// Local development: paste them into the home page, or use <c>dotnet user-secrets</c> (see README.md).
/// Deployed environments: set the <c>Checkpoint__ProjectId</c> and <c>Checkpoint__ApiKey</c> environment variables.
/// </para>
/// </remarks>
public sealed class CheckpointCredentials
{
	public const string SectionName = "Checkpoint";
	public const string ProjectIdKey = $"{SectionName}:{nameof(ProjectId)}";
	public const string ApiKeyKey = $"{SectionName}:{nameof(ApiKey)}";
	public const string ProjectIdEnvironmentVariable = $"{SectionName}__{nameof(ProjectId)}";
	public const string ApiKeyEnvironmentVariable = $"{SectionName}__{nameof(ApiKey)}";

	public string? ProjectId { get; set; }

	public string? ApiKey { get; set; }

	public bool HasProjectId => !string.IsNullOrWhiteSpace(ProjectId);

	public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

	/// <summary>The server-side middleware needs both values to report detections to Vouched; the pixel only needs the Project ID.</summary>
	public bool CanReportServerDetections => HasProjectId && HasApiKey;

	/// <summary>Masks the API key so the home page can confirm which key is in use without ever displaying it.</summary>
	public string? ApiKeyHint => ApiKey?.Trim() switch
	{
		null or "" => null,
		{ Length: > 12 } apiKey => $"••••{apiKey[^4..]}",
		_ => "••••",
	};
}
