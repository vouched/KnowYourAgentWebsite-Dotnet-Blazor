using System.Net;
using KnowYourAgentWebsite.Tests.Infrastructure;

namespace KnowYourAgentWebsite.Tests.Detection;

/// <summary>
/// Shows what happens when you change <c>Checkpoint:OnAgentDetected</c> from the default <c>Log</c> to <c>Block</c>.
/// </summary>
[TestFixture]
public sealed class CheckpointEnforcementTests
{
	[Test]
	public async Task Logging_lets_every_agent_through()
	{
		using var factory = new WebsiteFactory(settings: new Dictionary<string, string?> { ["Checkpoint:OnAgentDetected"] = "Log" });
		using var client = factory.CreateClient();

		using var response = await client.GetAsAsync(UserAgents.ChatGptUser);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
		}
	}

	// By default Checkpoint answers a blocked agent with a short plain-text notice and HTTP 200, so the AI reads it
	// and relays it to the person it's acting for instead of retrying an error
	[Test]
	public async Task Blocking_answers_AI_agents_with_a_notice_they_can_relay()
	{
		using var factory = new WebsiteFactory(settings: new Dictionary<string, string?> { ["Checkpoint:OnAgentDetected"] = "Block" });
		using var client = factory.CreateClient();

		using var response = await client.GetAsAsync(UserAgents.ChatGptUser);
		var body = await response.Content.ReadAsStringAsync();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/plain"));
			Assert.That(body, Does.Contain("does not allow direct browsing by AI agents"));
		}
	}

	[Test]
	public async Task Blocking_with_JSON_responses_returns_403_Forbidden()
	{
		using var factory = new WebsiteFactory(settings: new Dictionary<string, string?>
		{
			["Checkpoint:OnAgentDetected"] = "Block",
			["Checkpoint:AgentResponseFormat"] = "Json",
		});
		using var client = factory.CreateClient();

		using var response = await client.GetAsAsync(UserAgents.GptBot);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
			Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
		}
	}

	[Test]
	public async Task Blocking_never_affects_people()
	{
		using var factory = new WebsiteFactory(settings: new Dictionary<string, string?>
		{
			["Checkpoint:OnAgentDetected"] = "Block",
			["Checkpoint:AgentResponseFormat"] = "Json",
		});
		using var client = factory.CreateClient();

		using var response = await client.GetAsAsync(UserAgents.Chrome, headers: UserAgents.ChromeNavigationHeaders);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
		}
	}
}
