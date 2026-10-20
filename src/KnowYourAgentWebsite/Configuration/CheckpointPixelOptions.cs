namespace KnowYourAgentWebsite.Configuration;

/// <summary>
/// Settings for the Checkpoint Detect browser pixel, which adds client-side signals to the server-side verdict.
/// </summary>
public sealed class CheckpointPixelOptions
{
	public const string SectionName = "CheckpointPixel";

	public bool Enabled { get; set; } = true;

	public Uri ScriptUri { get; set; } = new("https://kya.vouched.id/pixel.js");

	/// <summary>Logs every report the pixel sends to the browser console, to troubleshoot visits missing from the dashboard.</summary>
	public bool Debug { get; set; }

	/// <summary>The origin the pixel loads from and reports to; used to build the Content-Security-Policy.</summary>
	public string Origin => ScriptUri.GetLeftPart(UriPartial.Authority);
}
