using System.Collections.Concurrent;
using Checkpoint.Core.Api;
using Checkpoint.Core.Policy;

namespace KnowYourAgentWebsite.Tests.Infrastructure;

/// <summary>Stands in for the Vouched Checkpoint API and records every detection the middleware reports to the dashboard.</summary>
public sealed class RecordingCheckpointApiClient : ICheckpointApiClient, IDetectionBatchReporter
{
	readonly ConcurrentQueue<DetectionEvent> _reportedDetections = new();
	TaskCompletionSource _deliveryGate = CreateOpenGate();

	public IReadOnlyCollection<DetectionEvent> ReportedDetections => [.. _reportedDetections];

	/// <summary>The policy your Checkpoint dashboard would return; <see langword="null"/> means none is configured.</summary>
	public PolicyConfig? Policy { get; set; }

	/// <summary>Rejects every detection, like Vouched does for a wrong Project ID or API key.</summary>
	public bool RejectDeliveries { get; set; }

	/// <summary>Asks Checkpoint to retry every detection later, like Vouched does when it answers HTTP 429 or 503.</summary>
	public bool DeferDeliveries { get; set; }

	/// <summary>Holds every delivery until <see cref="ReleaseDeliveries"/> is called, like a slow network.</summary>
	public void HoldDeliveries() => _deliveryGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

	public void ReleaseDeliveries() => _deliveryGate.TrySetResult();

	/// <summary>Detections are reported in the background, so wait until one matching <paramref name="predicate"/> arrives.</summary>
	public async Task<DetectionEvent> WaitForDetectionAsync(Func<DetectionEvent, bool> predicate, TimeSpan timeout)
	{
		using var cancellation = new CancellationTokenSource(timeout);

		while (true)
		{
			if (_reportedDetections.FirstOrDefault(predicate) is { } detection)
				return detection;

			try
			{
				await Task.Delay(TimeSpan.FromMilliseconds(50), cancellation.Token);
			}
			catch (OperationCanceledException exception)
			{
				throw new TimeoutException($"No matching detection was reported within {timeout}. Reported: {_reportedDetections.Count}", exception);
			}
		}
	}

	public Task<PolicyConfig?> GetPolicyConfigAsync(string projectId, CancellationToken cancellationToken) =>
		Task.FromResult(Policy);

	public Task<ComposedPolicyFlags?> GetComposedPolicyFlagsAsync(string projectId, CancellationToken cancellationToken) =>
		Task.FromResult<ComposedPolicyFlags?>(null);

	public async Task<bool> ReportDetectionAsync(DetectionEvent detectionEvent, CancellationToken cancellationToken)
	{
		var delivered = await ReportDetectionsAsync([detectionEvent], cancellationToken);
		return delivered.Delivered is 1;
	}

	public async Task<DetectionBatchResult> ReportDetectionsAsync(IReadOnlyList<DetectionEvent> detectionEvents, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(detectionEvents);

		await _deliveryGate.Task.WaitAsync(cancellationToken);

		if (RejectDeliveries)
			return DetectionBatchResult.Of(0);

		if (DeferDeliveries)
			return new DetectionBatchResult(0, [.. detectionEvents], DateTimeOffset.UtcNow.AddMinutes(1));

		foreach (var detectionEvent in detectionEvents)
			_reportedDetections.Enqueue(detectionEvent);

		return DetectionBatchResult.Of(detectionEvents.Count);
	}

	static TaskCompletionSource CreateOpenGate()
	{
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		gate.SetResult();
		return gate;
	}
}
