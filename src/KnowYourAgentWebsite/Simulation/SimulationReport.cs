using Checkpoint.Core.Models;

namespace KnowYourAgentWebsite.Simulation;

/// <summary>How Checkpoint handled one simulated visit.</summary>
/// <remarks><c>Class</c> is <see langword="null"/> when Checkpoint skipped the request, for example because of <c>Checkpoint:IncludePaths</c>.</remarks>
public sealed record SimulatedVisit(SimulatedVisitor Visitor, DetectionClassType? Class, bool IsAgent, string? DetectedAs, double Confidence, bool WasAllowed)
{
	/// <summary>Checkpoint reports every AI agent and bot to your dashboard; people are only sampled.</summary>
	public bool IsReportable => IsAgent || Class is DetectionClassType.AiAgent or DetectionClassType.Bot;
}

/// <summary>Whether Vouched received the detections a simulation reported.</summary>
public enum DeliveryStatus
{
	/// <summary>The middleware runs in local detection-only mode (no API key), so it classified the visits without reporting them.</summary>
	LocalOnly,

	/// <summary>Checkpoint reported none of the visits, for example because <c>Checkpoint:SkipPaths</c> covers them.</summary>
	NotReported,

	/// <summary>Vouched accepted every detection.</summary>
	Delivered,

	/// <summary>Vouched rejected detections, usually because of a wrong Project ID or API key.</summary>
	Rejected,

	/// <summary>Vouched asked Checkpoint to retry later (HTTP 429 or 5xx), so Checkpoint holds them until it sends its next detection.</summary>
	RetryLater,

	/// <summary>The detections were still on their way to Vouched when the page stopped waiting.</summary>
	InProgress,
}

/// <summary>The outcome of one <see cref="TrafficSimulator.SendAsync"/> call.</summary>
/// <remarks><c>DroppedCount</c> is the number of detections Vouched rejected; <c>HeldCount</c> is the number it asked Checkpoint to retry.</remarks>
public sealed record SimulationReport(IReadOnlyList<SimulatedVisit> Visits, int ReportedCount, DeliveryStatus Delivery, int DroppedCount, int HeldCount)
{
	public SimulatedVisit? For(SimulatedVisitor visitor) => Visits.FirstOrDefault(visit => visit.Visitor == visitor);
}
