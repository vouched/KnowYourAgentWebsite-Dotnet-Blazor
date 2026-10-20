namespace KnowYourAgentWebsite.Configuration;

/// <summary>Links to the Vouched Checkpoint dashboard and docs.</summary>
public static class VouchedLinks
{
	public static Uri Dashboard { get; } = new("https://kya.vouched.id/dashboard");

	public static Uri SignUp { get; } = new("https://kya.vouched.id/signup");

	public static Uri DotNetDocs { get; } = new("https://kya.vouched.id/docs/integrations/dotnet");

	public static Uri CredentialsDocs { get; } = new("https://kya.vouched.id/docs/credentials");
}
