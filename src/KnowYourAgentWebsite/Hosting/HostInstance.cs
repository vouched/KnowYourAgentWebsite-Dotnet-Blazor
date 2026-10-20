namespace KnowYourAgentWebsite.Hosting;

/// <summary>Identifies the currently running web host so a browser can detect when an in-process restart has completed.</summary>
public sealed class HostInstance
{
	public string Id { get; } = Guid.NewGuid().ToString("N");
}
