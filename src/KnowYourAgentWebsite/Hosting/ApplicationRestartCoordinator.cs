namespace KnowYourAgentWebsite.Hosting;

/// <summary>
/// Lets the web host restart itself in-process so new configuration (for example, freshly saved Vouched keys) is applied
/// without the developer having to stop and re-run the app.
/// </summary>
/// <remarks>
/// A single instance lives for the whole process and outlives each <see cref="WebApplication"/> that <c>Program.cs</c> builds.
/// The Checkpoint SDK reads its options once at startup, so rebuilding the host is the only reliable way to apply new keys.
/// </remarks>
public sealed class ApplicationRestartCoordinator
{
	int _isRestartRequested;

	public bool IsRestartRequested => Volatile.Read(ref _isRestartRequested) is 1;

	public void RequestRestart() => Interlocked.Exchange(ref _isRestartRequested, 1);

	/// <summary>Returns <see langword="true"/> exactly once per restart request, then resets so the next host runs normally.</summary>
	public bool TryConsumeRestartRequest() => Interlocked.Exchange(ref _isRestartRequested, 0) is 1;
}
