using Checkpoint.Core.Models;
using KnowYourAgentWebsite.Simulation;
using KnowYourAgentWebsite.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KnowYourAgentWebsite.Tests.Simulation;

[TestFixture]
public sealed class TrafficSimulatorTests
{
	static readonly Uri _siteUri = new("https://localhost:7273/");
	static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

	[Test]
	public async Task Every_visitor_is_classified_like_the_real_thing()
	{
		using var factory = new WebsiteFactory();

		var report = await SendAsync(factory);

		Assert.That(report.Visits.Select(static visit => (visit.Visitor.Id, visit.Class, visit.DetectedAs)), Is.EqualTo(new (string, DetectionClassType?, string?)[]
		{
			("chatgpt", DetectionClassType.AiAgent, "ChatGPT"),
			("claude", DetectionClassType.AiAgent, "Claude"),
			("perplexity", DetectionClassType.AiAgent, "Perplexity"),
			("gptbot", DetectionClassType.Bot, "GPTBot"),
			("claudebot", DetectionClassType.Bot, "ClaudeBot"),
			("googlebot", DetectionClassType.Bot, "Googlebot"),
			("headless-chrome", DetectionClassType.Bot, "Automation Tools"),
			("curl", DetectionClassType.Bot, "Dev Tools"),
			("python-requests", DetectionClassType.Bot, "HTTP Client"),
		}));
	}

	// The page groups visitors by Kind, so the groups must match how Checkpoint actually classifies them
	[Test]
	public async Task Every_visitor_is_listed_under_the_kind_Checkpoint_classifies_it_as()
	{
		using var factory = new WebsiteFactory();

		var report = await SendAsync(factory);

		Assert.That(report.Visits.Select(static visit => visit.Class), Is.EqualTo(report.Visits.Select(static visit => visit.Visitor.Kind switch
		{
			VisitorKind.AiAgent => DetectionClassType.AiAgent,
			_ => DetectionClassType.Bot,
		})));
	}

	[Test]
	public void Every_visitor_shows_a_token_from_its_real_user_agent()
	{
		foreach (var visitor in SimulatedVisitor.All)
			Assert.That(visitor.UserAgent, Does.Contain(visitor.Token), $"{visitor.Name}'s display token must come from its user agent");

		Assert.That(SimulatedVisitor.All.Select(static visitor => visitor.Id), Is.Unique);
	}

	[Test]
	public async Task Every_visit_is_reported_to_your_dashboard()
	{
		using var factory = new WebsiteFactory();

		var report = await SendAsync(factory);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(report.ReportedCount, Is.EqualTo(SimulatedVisitor.All.Count));
			Assert.That(report.Delivery, Is.EqualTo(DeliveryStatus.Delivered));
			Assert.That(factory.CheckpointApi.ReportedDetections.Select(static detection => detection.UserAgent), Is.SupersetOf(SimulatedVisitor.All.Select(static visitor => visitor.UserAgent)));
		}
	}

	[Test]
	public async Task Visits_look_like_requests_to_your_site_from_this_machine()
	{
		using var factory = new WebsiteFactory();

		await SendAsync(factory);
		var reported = await factory.CheckpointApi.WaitForDetectionAsync(static detection => detection.AgentName == "ChatGPT", _timeout);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(reported.ProjectId, Is.EqualTo(WebsiteFactory.TestProjectId));
			Assert.That(reported.Method, Is.EqualTo("GET"));
			Assert.That(reported.Path, Is.EqualTo("/"));
			Assert.That(reported.AbsoluteUrl, Is.EqualTo("https://localhost:7273/"));
			Assert.That(reported.IpAddress, Is.EqualTo("127.0.0.1"));
		}
	}

	[Test]
	public async Task Detections_Vouched_rejects_are_counted_so_the_page_can_warn_you()
	{
		using var factory = new WebsiteFactory();
		factory.CheckpointApi.RejectDeliveries = true;

		var report = await SendAsync(factory);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(report.Delivery, Is.EqualTo(DeliveryStatus.Rejected));
			Assert.That(report.DroppedCount, Is.EqualTo(SimulatedVisitor.All.Count));
		}
	}

	// Checkpoint holds detections Vouched asks it to retry until it sends the next one, so they must not count as delivered
	[Test]
	public async Task Detections_Vouched_asks_to_retry_later_are_not_shown_as_delivered()
	{
		using var factory = new WebsiteFactory();
		factory.CheckpointApi.DeferDeliveries = true;

		var report = await SendAsync(factory);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(report.Delivery, Is.EqualTo(DeliveryStatus.RetryLater));
			Assert.That(report.HeldCount, Is.EqualTo(SimulatedVisitor.All.Count));
			Assert.That(report.DroppedCount, Is.Zero);
		}
	}

	[Test]
	public async Task Nothing_is_reported_until_you_add_a_project_id()
	{
		using var factory = new WebsiteFactory(Environments.Development, isConfigured: false);

		var report = await SendAsync(factory);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(report.Visits.Select(static visit => visit.Class), Has.None.Null, "Checkpoint still classifies every visit locally");
			Assert.That(report.Delivery, Is.EqualTo(DeliveryStatus.LocalOnly));
			Assert.That(factory.CheckpointApi.ReportedDetections, Is.Empty);
		}
	}

	// Vouched rejects every report that has no API key (HTTP 401), so the app keeps the middleware from trying
	[Test]
	public async Task Without_an_api_key_visits_are_classified_but_never_reported()
	{
		using var factory = new WebsiteFactory(settings: new Dictionary<string, string?> { ["Checkpoint:ApiKey"] = string.Empty });

		var report = await SendAsync(factory);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(report.Visits.Select(static visit => visit.Class), Has.None.Null);
			Assert.That(report.Delivery, Is.EqualTo(DeliveryStatus.LocalOnly));
			Assert.That(factory.CheckpointApi.ReportedDetections, Is.Empty);
		}
	}

	// Checkpoint doesn't enforce on search engines or generic HTTP clients, so they're logged but never blocked
	[Test]
	public async Task Blocking_stops_AI_agents_and_AI_crawlers_but_not_search_engines_or_HTTP_clients()
	{
		using var factory = new WebsiteFactory(settings: new Dictionary<string, string?> { ["Checkpoint:OnAgentDetected"] = "Block" });

		var report = await SendAsync(factory);
		var blockedUserAgents = report.Visits.Where(static visit => !visit.WasAllowed).Select(static visit => visit.Visitor.UserAgent).ToArray();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(report.Visits.Where(static visit => !visit.WasAllowed).Select(static visit => visit.Visitor.Id), Is.EqualTo(new[]
			{
				"chatgpt", "claude", "perplexity", "gptbot", "claudebot", "headless-chrome",
			}));
			Assert.That(
				factory.CheckpointApi.ReportedDetections.Where(static detection => detection.EnforcementAction == "block").Select(static detection => detection.UserAgent),
				Is.EquivalentTo(blockedUserAgents),
				"Blocked visits are still reported to your dashboard, as blocks");
		}
	}

	[Test]
	public async Task Only_one_simulation_runs_at_a_time()
	{
		using var factory = new WebsiteFactory();
		var simulator = factory.Services.GetRequiredService<TrafficSimulator>();
		factory.CheckpointApi.HoldDeliveries();

		var first = simulator.SendAsync(_siteUri, CancellationToken.None);
		var second = await simulator.SendAsync(_siteUri, CancellationToken.None);
		factory.CheckpointApi.ReleaseDeliveries();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(second, Is.Null);
			Assert.That(await first.WaitAsync(_timeout), Is.Not.Null);
		}
	}

	static async Task<SimulationReport> SendAsync(WebsiteFactory factory)
	{
		var simulator = factory.Services.GetRequiredService<TrafficSimulator>();
		return await simulator.SendAsync(_siteUri, CancellationToken.None).WaitAsync(_timeout) ?? throw new AssertionException("A simulation was already running");
	}
}
