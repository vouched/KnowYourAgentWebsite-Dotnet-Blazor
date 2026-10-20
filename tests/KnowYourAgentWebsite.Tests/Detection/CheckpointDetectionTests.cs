using System.Net;
using KnowYourAgentWebsite.Tests.Infrastructure;

namespace KnowYourAgentWebsite.Tests.Detection;

/// <summary>
/// End-to-end examples of how Vouched Checkpoint classifies real traffic. Each request runs through the real middleware and
/// detection engine in an in-memory copy of this site, and the verdict is read from the <c>KYA-*</c> headers Checkpoint adds
/// to every response. Add your own cases to see how Checkpoint treats the clients you care about.
/// </summary>
/// <remarks>
/// These tests pin the behavior of the KyaOs.Checkpoint package, so when Dependabot proposes an update that changes how
/// traffic is classified, CI tells you before your users do.
/// </remarks>
[TestFixture]
public sealed class CheckpointDetectionTests
{
	WebsiteFactory _factory = default!;
	HttpClient _client = default!;

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		_factory = new WebsiteFactory();
		_client = _factory.CreateClient();
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		_client.Dispose();
		_factory.Dispose();
	}

	[TestCase(UserAgents.ChatGptUser, "ChatGPT", TestName = "ChatGPT browsing for a user is an AI agent")]
	[TestCase(UserAgents.ClaudeUser, "Claude", TestName = "Claude browsing for a user is an AI agent")]
	public async Task AI_assistants_acting_for_a_person_are_classified_as_AI_agents(string userAgent, string expectedAgentName)
	{
		using var response = await _client.GetAsAsync(userAgent);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.GetKyaHeader("KYA-Class"), Is.EqualTo("AiAgent"));
			Assert.That(response.GetKyaHeader("KYA-Agent"), Is.EqualTo(expectedAgentName));
			Assert.That(response.GetKyaHeader("KYA-Detected"), Is.EqualTo("true"));
		}
	}

	[TestCase(UserAgents.GptBot, "GPTBot", TestName = "OpenAI's GPTBot crawler is a bot")]
	[TestCase(UserAgents.ClaudeBot, "ClaudeBot", TestName = "Anthropic's ClaudeBot crawler is a bot")]
	[TestCase(UserAgents.Googlebot, "Googlebot", TestName = "Googlebot crawler is a bot")]
	public async Task Crawlers_are_classified_as_bots(string userAgent, string expectedAgentName)
	{
		using var response = await _client.GetAsAsync(userAgent);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.GetKyaHeader("KYA-Class"), Is.EqualTo("Bot"));
			Assert.That(response.GetKyaHeader("KYA-Agent"), Is.EqualTo(expectedAgentName));
			Assert.That(response.GetKyaHeader("KYA-Confidence"), Is.EqualTo("100"));
			Assert.That(response.GetKyaHeader("KYA-Verification"), Is.EqualTo("Pattern"));
		}
	}

	[TestCase(UserAgents.Curl, TestName = "curl is a bot")]
	[TestCase(UserAgents.PythonRequests, TestName = "A Python requests script is a bot")]
	public async Task Scripts_and_command_line_tools_are_classified_as_bots(string userAgent)
	{
		using var response = await _client.GetAsAsync(userAgent);

		Assert.That(response.GetKyaHeader("KYA-Class"), Is.EqualTo("Bot"));
	}

	[Test]
	public async Task A_person_browsing_in_Chrome_is_never_flagged()
	{
		using var response = await _client.GetAsAsync(UserAgents.Chrome, headers: UserAgents.ChromeNavigationHeaders);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(response.GetKyaHeader("KYA-Detected"), Is.EqualTo("false"));
			Assert.That(response.GetKyaHeader("KYA-Agent"), Is.Null);
		}
	}

	[Test]
	public async Task The_default_OnAgentDetected_action_logs_agents_without_blocking_them()
	{
		using var response = await _client.GetAsAsync(UserAgents.GptBot);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
		}
	}

	[Test]
	public async Task Detections_are_reported_to_your_Vouched_dashboard()
	{
		using var response = await _client.GetAsAsync(UserAgents.PerplexityUser);

		var reported = await _factory.CheckpointApi.WaitForDetectionAsync(
			static detection => detection.UserAgent == UserAgents.PerplexityUser,
			TimeSpan.FromSeconds(30));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(reported.ProjectId, Is.EqualTo(WebsiteFactory.TestProjectId));
			Assert.That(reported.AgentName, Is.EqualTo("Perplexity"));
			Assert.That(reported.Path, Is.EqualTo("/"));
		}
	}

	[Test]
	public async Task Skipped_paths_are_never_analyzed()
	{
		using var factory = new WebsiteFactory(settings: new Dictionary<string, string?> { ["Checkpoint:SkipPaths:0"] = "/" });
		using var client = factory.CreateClient();

		using var response = await client.GetAsAsync(UserAgents.GptBot);

		Assert.That(response.GetKyaHeader("KYA-Class"), Is.Null);
	}
}
