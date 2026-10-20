using System.Collections.Concurrent;
using KnowYourAgentWebsite.Hosting;

namespace KnowYourAgentWebsite.Tests.Infrastructure;

/// <summary>Records restart requests instead of stopping the in-memory test host.</summary>
public sealed class RecordingApplicationRestarter : IApplicationRestarter
{
	readonly ConcurrentQueue<TimeSpan> _requestedDelays = new();
	readonly TaskCompletionSource _restartScheduled = new(TaskCreationOptions.RunContinuationsAsynchronously);

	public IReadOnlyCollection<TimeSpan> RequestedDelays => [.. _requestedDelays];

	public void ScheduleRestart(TimeSpan delay)
	{
		_requestedDelays.Enqueue(delay);
		_restartScheduled.TrySetResult();
	}

	/// <summary>The restart is scheduled after the response completes, so it can land slightly after the client receives it.</summary>
	public Task WaitForRestartAsync(TimeSpan timeout) => _restartScheduled.Task.WaitAsync(timeout);
}
