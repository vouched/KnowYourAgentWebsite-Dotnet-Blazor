using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using KnowYourAgentWebsite.Components.Pages;
using KnowYourAgentWebsite.Setup;
using KnowYourAgentWebsite.Tests.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace KnowYourAgentWebsite.Tests.Setup;

/// <summary>Step 1 on the home page: saving your Vouched keys to user secrets and restarting the app to apply them.</summary>
[TestFixture]
public sealed class ConnectProjectTests
{
	[Test]
	public async Task Shows_that_your_keys_are_missing()
	{
		using var factory = new WebsiteFactory(Environments.Development, isConfigured: false);
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("[data-testid=connection-status]")?.TextContent, Is.EqualTo("Not configured"));
			Assert.That(document.QuerySelector("#project-id"), Is.Not.Null);
			Assert.That(document.QuerySelector("#api-key")?.GetAttribute("type"), Is.EqualTo("password"));
		}
	}

	[Test]
	public async Task Shows_your_project_but_never_your_api_key()
	{
		using var factory = new WebsiteFactory(Environments.Development);
		using var client = factory.CreateClient();

		var html = await client.GetStringAsync(new Uri("/", UriKind.Relative));
		var document = await client.GetDocumentAsync("/");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(html, Does.Not.Contain(WebsiteFactory.TestApiKey));
			Assert.That(document.QuerySelector("[data-testid=connection-status]")?.TextContent, Is.EqualTo("Configured"));
			Assert.That(document.QuerySelector("[data-testid=project-id]")?.TextContent, Is.EqualTo(WebsiteFactory.TestProjectId));
			Assert.That(document.QuerySelector("[data-testid=api-key-hint]")?.TextContent, Is.EqualTo($"••••{WebsiteFactory.TestApiKey[^4..]}"));
		}
	}

	[Test]
	public async Task Saving_writes_your_keys_to_user_secrets_and_restarts_the_app()
	{
		using var factory = new WebsiteFactory(Environments.Development, isConfigured: false);
		using var client = factory.CreateClient();

		using var response = await client.SubmitFormAsync(Home.ConnectFormName, new Dictionary<string, string>
		{
			["project-id"] = "  my-project  ",
			["api-key"] = "sk_my_secret_api_key",
		});
		var document = await response.ReadDocumentAsync();
		await factory.Restarter.WaitForRestartAsync(TimeSpan.FromSeconds(10));

		var secrets = JsonNode.Parse(await File.ReadAllTextAsync(factory.UserSecretsFilePath))!.AsObject();
		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(document.QuerySelector("[data-testid=restart-panel]"), Is.Not.Null);
			Assert.That(secrets["Checkpoint:ProjectId"]?.GetValue<string>(), Is.EqualTo("my-project"));
			Assert.That(secrets["Checkpoint:ApiKey"]?.GetValue<string>(), Is.EqualTo("sk_my_secret_api_key"));
			Assert.That(factory.Restarter.RequestedDelays, Has.Count.EqualTo(1));
		}
	}

	[Test]
	public async Task A_project_id_is_all_you_need_to_save()
	{
		using var factory = new WebsiteFactory(Environments.Development, isConfigured: false);
		using var client = factory.CreateClient();

		using var response = await client.SubmitFormAsync(Home.ConnectFormName, new Dictionary<string, string> { ["project-id"] = "my-project" });
		await factory.Restarter.WaitForRestartAsync(TimeSpan.FromSeconds(10));

		var secrets = JsonNode.Parse(await File.ReadAllTextAsync(factory.UserSecretsFilePath))!.AsObject();
		using (Assert.EnterMultipleScope())
		{
			Assert.That(secrets["Checkpoint:ProjectId"]?.GetValue<string>(), Is.EqualTo("my-project"));
			Assert.That(secrets.ContainsKey("Checkpoint:ApiKey"), Is.False);
		}
	}

	[Test]
	public async Task Saving_without_an_api_key_removes_the_one_saved_for_a_previous_project()
	{
		using var factory = new WebsiteFactory(Environments.Development);
		using var client = factory.CreateClient();
		Directory.CreateDirectory(Path.GetDirectoryName(factory.UserSecretsFilePath)!);
		await File.WriteAllTextAsync(factory.UserSecretsFilePath, """{ "Checkpoint:ProjectId": "old-project", "Checkpoint:ApiKey": "sk_old_project_key" }""");

		using var response = await client.SubmitFormAsync(Home.ConnectFormName, new Dictionary<string, string> { ["project-id"] = "new-project", ["api-key"] = string.Empty });
		await factory.Restarter.WaitForRestartAsync(TimeSpan.FromSeconds(10));

		var secrets = JsonNode.Parse(await File.ReadAllTextAsync(factory.UserSecretsFilePath))!.AsObject();
		using (Assert.EnterMultipleScope())
		{
			Assert.That(secrets["Checkpoint:ProjectId"]?.GetValue<string>(), Is.EqualTo("new-project"));
			Assert.That(secrets.ContainsKey("Checkpoint:ApiKey"), Is.False);
		}
	}

	[Test]
	public async Task A_project_id_alone_runs_in_pixel_only_mode()
	{
		using var factory = new WebsiteFactory(Environments.Development, settings: new Dictionary<string, string?> { ["Checkpoint:ApiKey"] = string.Empty });
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("[data-testid=connection-status]")?.TextContent, Is.EqualTo("Pixel only"));
			Assert.That(document.QuerySelector("[data-testid=api-key-hint]")?.TextContent, Is.EqualTo("Not added"));
			Assert.That(document.QuerySelector("[data-testid=pixel-only-note]"), Is.Not.Null);
			Assert.That(document.QuerySelector("details.disclosure > summary")?.TextContent, Is.EqualTo("Add your API key"));
			Assert.That(document.QuerySelector("script[data-project-id]")?.GetAttribute("data-project-id"), Is.EqualTo(WebsiteFactory.TestProjectId));
		}
	}

	[Test]
	public async Task Invalid_keys_are_rejected_without_saving_anything()
	{
		using var factory = new WebsiteFactory(Environments.Development, isConfigured: false);
		using var client = factory.CreateClient();

		using var response = await client.SubmitFormAsync(Home.ConnectFormName, new Dictionary<string, string>
		{
			["project-id"] = "not a project id!",
			["api-key"] = "sk_has spaces",
		});
		var document = await response.ReadDocumentAsync();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelectorAll(".validation-message").Select(static element => element.TextContent), Is.EquivalentTo(new[]
			{
				"A Project ID only contains letters, numbers, dashes and underscores.",
				"An API key cannot contain spaces.",
			}));
			Assert.That(File.Exists(factory.UserSecretsFilePath), Is.False);
			Assert.That(factory.Restarter.RequestedDelays, Is.Empty);
		}
	}

	[Test]
	public async Task Changing_projects_keeps_the_form_open_to_show_errors()
	{
		using var factory = new WebsiteFactory(Environments.Development);
		using var client = factory.CreateClient();

		using var response = await client.SubmitFormAsync(Home.ConnectFormName, new Dictionary<string, string> { ["project-id"] = "not a project id!" });
		var document = await response.ReadDocumentAsync();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("details.disclosure")?.HasAttribute("open"), Is.True);
			Assert.That(factory.Restarter.RequestedDelays, Is.Empty);
		}
	}

	[Test]
	public async Task Status_reports_the_running_instance_so_the_browser_can_detect_the_restart()
	{
		using var factory = new WebsiteFactory(Environments.Development, isConfigured: false);
		using var client = factory.CreateClient();

		var status = await client.GetFromJsonAsync<SetupStatus>(new Uri(SetupEndpoints.StatusPath, UriKind.Relative));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(status!.InstanceId, Has.Length.EqualTo(32));
			Assert.That(status.HasProjectId, Is.False);
		}
	}

	[Test]
	public async Task Key_form_is_hidden_outside_of_Development()
	{
		using var factory = new WebsiteFactory(WebsiteFactory.TestingEnvironment);
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");
		using var status = await client.GetAsync(new Uri(SetupEndpoints.StatusPath, UriKind.Relative));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("#project-id"), Is.Null);
			Assert.That(status.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		}
	}

	[Test]
	public async Task Key_form_is_hidden_from_other_machines()
	{
		using var factory = new WebsiteFactory(Environments.Development, isConfigured: false, isLocalClient: false);
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");

		Assert.That(document.QuerySelector("#project-id"), Is.Null);
	}

	[Test]
	public async Task Key_form_is_hidden_from_DNS_rebinding_attacks()
	{
		using var factory = new WebsiteFactory(Environments.Development, isConfigured: false);
		using var client = factory.CreateClient();
		client.DefaultRequestHeaders.Host = "attacker.example";

		var document = await client.GetDocumentAsync("/");
		using var status = await client.GetAsync(new Uri(SetupEndpoints.StatusPath, UriKind.Relative));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("#project-id"), Is.Null);
			Assert.That(status.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		}
	}

	[TestCase("X-Forwarded-Host", "abc123.euw.devtunnels.ms")]
	[TestCase("X-Forwarded-For", "203.0.113.7")]
	[TestCase("Forwarded", "for=203.0.113.7;host=example.ngrok.app")]
	[TestCase("Via", "1.1 proxy.example")]
	[TestCase("X-Real-IP", "203.0.113.7")]
	[TestCase("CF-Connecting-IP", "203.0.113.7")]
	public async Task Key_form_is_hidden_from_dev_tunnels_and_reverse_proxies(string header, string value)
	{
		using var factory = new WebsiteFactory(Environments.Development, isConfigured: false);
		using var client = factory.CreateClient();
		client.DefaultRequestHeaders.TryAddWithoutValidation(header, value);

		var document = await client.GetDocumentAsync("/");
		using var status = await client.GetAsync(new Uri(SetupEndpoints.StatusPath, UriKind.Relative));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(document.QuerySelector("#project-id"), Is.Null);
			Assert.That(status.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		}
	}
}
