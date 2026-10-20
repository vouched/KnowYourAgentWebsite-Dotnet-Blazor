using System.Net;
using Checkpoint.AspNetCore;
using Checkpoint.AspNetCore.Extensions;
using Checkpoint.Core.Api;
using Checkpoint.Core.Configuration;
using Microsoft.Extensions.Options;

namespace KnowYourAgentWebsite.Simulation;

/// <summary>
/// Sends simulated AI agent and bot visits through the Checkpoint middleware, so they're classified and reported to your
/// Vouched dashboard exactly like real traffic.
/// </summary>
/// <remarks>
/// Each visit is an in-memory request that runs through a private <c>UseCheckpoint()</c> pipeline. That pipeline shares the
/// app's detection engine, options and telemetry queue, so it reports the same detection events a real request would,
/// without calling the app over the network (and dealing with HTTPS certificates, ports and proxies along the way).
/// </remarks>
public sealed partial class TrafficSimulator : IDisposable
{
	const string AllowedItemKey = "KnowYourAgentWebsite.Simulation.Allowed";

	// Long enough for a slow network round trip to Vouched; after that the page reports that delivery is still in progress
	static readonly TimeSpan _deliveryTimeout = TimeSpan.FromSeconds(12);

	readonly IServiceScopeFactory _scopeFactory;
	readonly DetectionTelemetryQueue _telemetry;
	readonly IOptions<CheckpointOptions> _checkpointOptions;
	readonly TimeProvider _timeProvider;
	readonly ILogger<TrafficSimulator> _logger;
	readonly Lazy<RequestDelegate> _checkpointPipeline;
	readonly SemaphoreSlim _oneAtATime = new(1, 1);

	public TrafficSimulator(
		IServiceProvider services,
		IServiceScopeFactory scopeFactory,
		DetectionTelemetryQueue telemetry,
		IOptions<CheckpointOptions> checkpointOptions,
		TimeProvider timeProvider,
		ILogger<TrafficSimulator> logger)
	{
		_scopeFactory = scopeFactory;
		_telemetry = telemetry;
		_checkpointOptions = checkpointOptions;
		_timeProvider = timeProvider;
		_logger = logger;
		_checkpointPipeline = new(() => BuildCheckpointPipeline(services));
	}

	/// <summary>Sends one visit from every <see cref="SimulatedVisitor"/>, then waits for Vouched to receive the detections.</summary>
	/// <param name="siteUri">The site the visits appear to arrive at, as shown in your dashboard.</param>
	/// <param name="cancellationToken">Stops waiting for delivery, for example when the browser disconnects.</param>
	/// <returns>The results, or <see langword="null"/> when another simulation is already running.</returns>
	public async Task<SimulationReport?> SendAsync(Uri siteUri, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(siteUri);

		if (!await _oneAtATime.WaitAsync(TimeSpan.Zero, cancellationToken))
			return null;

		try
		{
			var droppedBefore = _telemetry.DroppedCount;

			List<SimulatedVisit> visits = [];
			foreach (var visitor in SimulatedVisitor.All)
				visits.Add(await VisitAsync(visitor, siteUri, cancellationToken));

			// Matches the middleware: without a ProjectId (which this app clears when there's no API key), it reports nothing
			if (string.IsNullOrEmpty(_checkpointOptions.Value.ProjectId))
				return Report(visits, DeliveryStatus.LocalOnly);

			var reportedCount = visits.Count(static visit => visit.IsReportable);
			if (reportedCount is 0)
				return Report(visits, DeliveryStatus.NotReported);

			var isFlushed = await WaitForDeliveryAsync(cancellationToken);

			// Rejected detections are dropped. Ones Vouched asked to retry later (HTTP 429 or 5xx) stay held in the queue
			// until Checkpoint sends its next detection, so a completed flush alone doesn't mean they were delivered.
			var droppedCount = (int)Math.Clamp(_telemetry.DroppedCount - droppedBefore, 0, reportedCount);
			var heldCount = isFlushed ? Math.Min(_telemetry.HeldEventCount, reportedCount) : 0;

			var delivery = (isFlushed, droppedCount, heldCount) switch
			{
				(false, _, _) => DeliveryStatus.InProgress,
				(_, > 0, _) => DeliveryStatus.Rejected,
				(_, _, > 0) => DeliveryStatus.RetryLater,
				_ => DeliveryStatus.Delivered,
			};

			return Report(visits, delivery, reportedCount, droppedCount, heldCount);
		}
		finally
		{
			_oneAtATime.Release();
		}
	}

	public void Dispose() => _oneAtATime.Dispose();

	SimulationReport Report(List<SimulatedVisit> visits, DeliveryStatus delivery, int reportedCount = 0, int droppedCount = 0, int heldCount = 0)
	{
		LogSent(_logger, visits.Count, reportedCount, delivery);
		return new SimulationReport(visits, reportedCount, delivery, droppedCount, heldCount);
	}

	static RequestDelegate BuildCheckpointPipeline(IServiceProvider services)
	{
		var pipeline = new ApplicationBuilder(services);
		pipeline.UseCheckpoint();

		// Only reached when Checkpoint lets the visit through: a block, challenge or redirect short-circuits before it
		pipeline.Run(static httpContext =>
		{
			httpContext.Items[AllowedItemKey] = true;
			return Task.CompletedTask;
		});

		return pipeline.Build();
	}

	async Task<SimulatedVisit> VisitAsync(SimulatedVisitor visitor, Uri siteUri, CancellationToken cancellationToken)
	{
		await using var scope = _scopeFactory.CreateAsyncScope();

		var httpContext = new DefaultHttpContext
		{
			RequestServices = scope.ServiceProvider,
			RequestAborted = cancellationToken,
		};

		var request = httpContext.Request;
		request.Method = HttpMethods.Get;
		request.Scheme = siteUri.Scheme;
		request.Host = HostString.FromUriComponent(siteUri);
		request.Path = "/";
		request.Headers.UserAgent = visitor.UserAgent;
		request.Headers.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

		// The visits really do originate on this machine
		httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

		await _checkpointPipeline.Value(httpContext);

		var result = httpContext.WasCheckpointSkipped() ? null : httpContext.GetCheckpointResult();

		return new SimulatedVisit(
			visitor,
			result?.DetectionClass?.Type,
			result?.IsAgent ?? false,
			result?.DetectedAgent?.Name,
			result?.Confidence ?? 0,
			httpContext.Items.ContainsKey(AllowedItemKey));
	}

	async Task<bool> WaitForDeliveryAsync(CancellationToken cancellationToken)
	{
		try
		{
			// Completes once nothing is being sent; see SendAsync for detections Vouched asked to retry later
			await _telemetry.FlushAsync().WaitAsync(_deliveryTimeout, _timeProvider, cancellationToken);
			return true;
		}
		catch (TimeoutException)
		{
			return false;
		}
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Sent {VisitCount} simulated visits through Checkpoint and reported {ReportedCount} to Vouched: {Delivery}")]
	static partial void LogSent(ILogger logger, int visitCount, int reportedCount, DeliveryStatus delivery);
}
