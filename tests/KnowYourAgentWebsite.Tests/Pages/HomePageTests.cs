using System.Net;
using AngleSharp.Dom;
using KnowYourAgentWebsite.Components.Pages;
using KnowYourAgentWebsite.Simulation;
using KnowYourAgentWebsite.Tests.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace KnowYourAgentWebsite.Tests.Pages;

[TestFixture]
public sealed class HomePageTests
{
	[Test]
	public async Task Walks_you_through_three_steps()
	{
		using var factory = new WebsiteFactory();
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("h1")?.TextContent, Is.EqualTo("See AI agents arrive in your Vouched dashboard"));
			Assert.That(document.QuerySelectorAll(".step__title").Select(static title => title.TextContent), Is.EqualTo(new[]
			{
				"Connect your Vouched project",
				"Send simulated traffic",
				"Watch it arrive in your dashboard",
			}));
			Assert.That(document.QuerySelector("[data-testid=dashboard-link]")?.GetAttribute("href"), Is.EqualTo("https://kya.vouched.id/dashboard"));
		}
	}

	[Test]
	public async Task Names_the_project_to_open_in_your_dashboard()
	{
		using var factory = new WebsiteFactory();
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("[data-testid=dashboard-instructions] code")?.TextContent, Is.EqualTo(WebsiteFactory.TestProjectId));
			Assert.That(document.QuerySelector("[data-testid=dashboard-instructions]").NormalizedText(), Does.Contain("Every simulated visit shows up there"));
			Assert.That(document.QuerySelector("[data-testid=dashboard-troubleshooting]").NormalizedText(), Does.StartWith("Not there? Make sure your dashboard is showing the project with this ID"));
		}
	}

	[Test]
	public async Task Lists_every_simulated_visitor_before_you_send_anything()
	{
		using var factory = new WebsiteFactory(Environments.Development);
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelectorAll(".visitor").Select(static visitor => visitor.GetAttribute("data-testid")), Is.EqualTo(SimulatedVisitor.All.Select(static visitor => $"visitor-{visitor.Id}")));
			Assert.That(document.QuerySelectorAll("[data-testid=verdict]"), Is.Empty);
			Assert.That(document.QuerySelector("[data-testid=send-traffic]").NormalizedText(), Is.EqualTo("Send simulated traffic"));
			Assert.That(document.QuerySelector("[data-testid=simulate-instructions]").NormalizedText(), Does.EndWith("Checkpoint classifies it like any live request and reports it to your dashboard."));
		}
	}

	[Test]
	public async Task Sending_simulated_traffic_shows_how_Checkpoint_classified_each_visitor()
	{
		using var factory = new WebsiteFactory(Environments.Development);
		using var client = factory.CreateClient();

		using var response = await client.SubmitFormAsync(Home.SimulateFormName);
		var document = await response.ReadDocumentAsync();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(document.QuerySelector("[data-testid=visitor-chatgpt] [data-testid=verdict]")?.TextContent, Is.EqualTo("AI agent"));
			Assert.That(document.QuerySelector("[data-testid=visitor-chatgpt] [data-testid=detail]")?.TextContent, Is.EqualTo("ChatGPT · 100%"));
			Assert.That(document.QuerySelector("[data-testid=visitor-gptbot] [data-testid=verdict]")?.TextContent, Is.EqualTo("Bot"));
			Assert.That(document.QuerySelectorAll("[data-testid=verdict]"), Has.Length.EqualTo(SimulatedVisitor.All.Count));
			Assert.That(document.QuerySelector("[data-testid=delivery-status]").NormalizedText(), Is.EqualTo($"Delivered {SimulatedVisitor.All.Count} detections to project {WebsiteFactory.TestProjectId}."));
			Assert.That(document.QuerySelector("[data-testid=send-traffic]").NormalizedText(), Is.EqualTo("Send again"));
			Assert.That(StepState(document, "simulate"), Is.EqualTo("step--complete"));
			Assert.That(document.QuerySelector("#simulate")!.Closest(".step")!.QuerySelectorAll(".icon--check"), Is.Not.Empty);
			Assert.That(StepState(document, "dashboard"), Is.EqualTo("step--current"));
		}
	}

	[Test]
	public async Task The_send_button_comes_before_the_visitors_so_you_see_it_first()
	{
		using var factory = new WebsiteFactory(Environments.Development);
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");
		var elements = document.QuerySelector("#simulate")!.QuerySelectorAll("[data-testid=send-traffic], .visitors__heading");

		Assert.That(elements[0].GetAttribute("data-testid"), Is.EqualTo("send-traffic"));
	}

	[Test]
	public async Task With_only_a_project_id_you_know_upfront_that_simulated_traffic_needs_an_API_key()
	{
		using var factory = new WebsiteFactory(Environments.Development, settings: new Dictionary<string, string?> { ["Checkpoint:ApiKey"] = string.Empty });
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("[data-testid=simulate-instructions]").NormalizedText(), Does.EndWith(
				"This step requires an API key, so add yours in step 1 first."));
			Assert.That(document.QuerySelector("[data-testid=dashboard-instructions] code")?.TextContent, Is.EqualTo(WebsiteFactory.TestProjectId));
		}
	}

	[Test]
	public async Task With_only_a_project_id_sending_simulated_traffic_asks_for_an_API_key_instead_of_checking_off_the_step()
	{
		using var factory = new WebsiteFactory(Environments.Development, settings: new Dictionary<string, string?> { ["Checkpoint:ApiKey"] = string.Empty });
		using var client = factory.CreateClient();

		using var response = await client.SubmitFormAsync(Home.SimulateFormName);
		var document = await response.ReadDocumentAsync();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("[data-testid=delivery-status]").NormalizedText(), Is.EqualTo(
				"Sending simulated traffic to your dashboard requires an API key. Add yours in step 1, then send again."));
			Assert.That(document.QuerySelector("#simulate")!.Closest(".step")!.QuerySelectorAll(".icon--check"), Is.Empty);
			Assert.That(StepState(document, "simulate"), Is.EqualTo("step--current"));
			Assert.That(StepState(document, "dashboard"), Is.EqualTo("step--upcoming"));
			Assert.That(document.QuerySelectorAll("[data-testid=verdict]"), Has.Length.EqualTo(SimulatedVisitor.All.Count));
			Assert.That(document.QuerySelector("[data-testid=dashboard-instructions]").NormalizedText(), Does.Contain("reported by the Checkpoint pixel"));
			Assert.That(factory.CheckpointApi.ReportedDetections, Is.Empty);
		}
	}

	[Test]
	public async Task Warns_you_when_Vouched_rejects_the_detections()
	{
		using var factory = new WebsiteFactory(Environments.Development);
		using var client = factory.CreateClient();
		factory.CheckpointApi.RejectDeliveries = true;

		using var response = await client.SubmitFormAsync(Home.SimulateFormName);
		var document = await response.ReadDocumentAsync();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("[data-testid=delivery-status]").NormalizedText(), Is.EqualTo(
				$"Vouched didn't accept {SimulatedVisitor.All.Count} of {SimulatedVisitor.All.Count} detections. Double-check your Project ID and API key in step 1, then send again."));
			Assert.That(StepState(document, "simulate"), Is.EqualTo("step--current"));
			Assert.That(StepState(document, "dashboard"), Is.EqualTo("step--upcoming"));
		}
	}

	[Test]
	public async Task Tells_you_when_Vouched_asks_to_retry_later()
	{
		using var factory = new WebsiteFactory(Environments.Development);
		using var client = factory.CreateClient();
		factory.CheckpointApi.DeferDeliveries = true;

		using var response = await client.SubmitFormAsync(Home.SimulateFormName);
		var document = await response.ReadDocumentAsync();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("[data-testid=delivery-status]").NormalizedText(), Is.EqualTo(
				$"Vouched asked Checkpoint to retry {SimulatedVisitor.All.Count} detections later. Send again in a moment to deliver them."));
			Assert.That(StepState(document, "simulate"), Is.EqualTo("step--current"));
		}
	}

	[Test]
	public async Task Simulated_traffic_waits_until_you_connect_a_project()
	{
		using var factory = new WebsiteFactory(Environments.Development, isConfigured: false);
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("[data-testid=send-traffic]"), Is.Null);
			Assert.That(document.QuerySelector("#simulate .hint").NormalizedText(), Is.EqualTo("Connect your project in step 1 first, so the traffic has somewhere to go."));
		}
	}

	[Test]
	public async Task Simulated_traffic_is_only_available_on_your_own_machine()
	{
		using var factory = new WebsiteFactory(Environments.Development, isLocalClient: false);
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");

		Assert.That(document.QuerySelector("[data-testid=send-traffic]"), Is.Null);
	}

	[Test]
	public async Task Unknown_pages_return_a_friendly_404()
	{
		using var factory = new WebsiteFactory();
		using var client = factory.CreateClient();

		using var response = await client.GetAsync(new Uri("/this-page-does-not-exist", UriKind.Relative));
		var document = await response.ReadDocumentAsync();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
			Assert.That(document.QuerySelector("h1")?.TextContent, Is.EqualTo("This page wandered off."));
		}
	}

	[Test]
	public async Task Health_check_reports_healthy()
	{
		using var factory = new WebsiteFactory();
		using var client = factory.CreateClient();

		using var response = await client.GetAsync(new Uri(WebsiteApplication.HealthCheckPath, UriKind.Relative));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("Healthy"));
		}
	}

	// Each step is marked step--complete, step--current or step--upcoming
	static string? StepState(IDocument document, string panelId) =>
		document.QuerySelector($"#{panelId}")?.Closest(".step")?.ClassList.FirstOrDefault(static name => name.StartsWith("step--", StringComparison.Ordinal));
}
