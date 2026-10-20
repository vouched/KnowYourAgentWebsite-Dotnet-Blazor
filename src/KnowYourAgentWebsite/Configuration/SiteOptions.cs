namespace KnowYourAgentWebsite.Configuration;

/// <summary>Branding and links that a fork is likely to change.</summary>
public sealed class SiteOptions
{
	public const string SectionName = "Site";

	public string Name { get; set; } = "Know Your Agent";

	public Uri RepositoryUri { get; set; } = new("https://github.com/TheCodeTraveler/KnowYourAgentWebsite-Dotnet-Blazor");
}
