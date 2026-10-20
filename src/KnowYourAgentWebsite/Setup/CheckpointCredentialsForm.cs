using System.ComponentModel.DataAnnotations;

namespace KnowYourAgentWebsite.Setup;

public sealed class CheckpointCredentialsForm
{
	[Required(ErrorMessage = "Paste the Project ID from your Vouched dashboard.")]
	[StringLength(128, ErrorMessage = "A Project ID is at most 128 characters.")]
	[RegularExpression("^\\s*[A-Za-z0-9_-]+\\s*$", ErrorMessage = "A Project ID only contains letters, numbers, dashes and underscores.")]
	public string? ProjectId { get; set; }

	// Optional: the pixel works with just a Project ID; the API key also lets the middleware report its detections
	[StringLength(512, ErrorMessage = "An API key is at most 512 characters.")]
	[RegularExpression("^\\s*\\S+\\s*$", ErrorMessage = "An API key cannot contain spaces.")]
	public string? ApiKey { get; set; }
}
