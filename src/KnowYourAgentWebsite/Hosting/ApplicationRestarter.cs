namespace KnowYourAgentWebsite.Hosting;

public interface IApplicationRestarter
{
	/// <summary>Gracefully stops the current host after <paramref name="delay"/> and asks <c>Program.cs</c> to start a fresh one.</summary>
	void ScheduleRestart(TimeSpan delay);
}

sealed partial class ApplicationRestarter(
	ApplicationRestartCoordinator coordinator,
	IHostApplicationLifetime lifetime,
	TimeProvider timeProvider,
	ILogger<ApplicationRestarter> logger) : IApplicationRestarter
{
	public void ScheduleRestart(TimeSpan delay)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);

		LogRestartScheduled(logger, delay);
		_ = RestartAfterDelayAsync(delay);
	}

	async Task RestartAfterDelayAsync(TimeSpan delay)
	{
		try
		{
			// The delay gives the browser time to finish loading the "Restarting…" page before the server goes away
			await Task.Delay(delay, timeProvider, lifetime.ApplicationStopping);
		}
		catch (OperationCanceledException)
		{
			// The host is already shutting down (e.g. Ctrl+C); honor that instead of restarting
			return;
		}

		coordinator.RequestRestart();
		lifetime.StopApplication();
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Restarting the web host in {Delay} to apply new configuration")]
	static partial void LogRestartScheduled(ILogger logger, TimeSpan delay);
}
