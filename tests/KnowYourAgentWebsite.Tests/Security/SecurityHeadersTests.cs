using KnowYourAgentWebsite.Configuration;
using KnowYourAgentWebsite.Security;
using KnowYourAgentWebsite.Tests.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace KnowYourAgentWebsite.Tests.Security;

[TestFixture]
public sealed class SecurityHeadersTests
{
	[Test]
	public async Task Every_response_has_defense_in_depth_headers()
	{
		using var factory = new WebsiteFactory();
		using var client = factory.CreateClient();

		using var response = await client.GetAsync(new Uri("/", UriKind.Relative));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(response.Headers.GetValues("X-Content-Type-Options"), Is.EqualTo(new[] { "nosniff" }));
			Assert.That(response.Headers.GetValues("X-Frame-Options"), Is.EqualTo(new[] { "DENY" }));
			Assert.That(response.Headers.GetValues("Referrer-Policy"), Is.EqualTo(new[] { "strict-origin-when-cross-origin" }));
			Assert.That(response.Headers.Contains("Permissions-Policy"), Is.True);
			Assert.That(response.Headers.GetValues("Content-Security-Policy").Single(), Does.Contain("frame-ancestors 'none'"));
		}
	}

	[Test]
	public async Task Content_security_policy_is_relaxed_in_Development_for_hot_reload()
	{
		using var factory = new WebsiteFactory(Environments.Development);
		using var client = factory.CreateClient();

		using var response = await client.GetAsync(new Uri("/", UriKind.Relative));

		Assert.That(response.Headers.Contains("Content-Security-Policy"), Is.False);
	}

	[Test]
	public void Content_security_policy_allows_the_Checkpoint_pixel_only_when_it_is_enabled()
	{
		var enabled = SecurityHeadersMiddleware.BuildContentSecurityPolicy(new CheckpointPixelOptions { Enabled = true });
		var disabled = SecurityHeadersMiddleware.BuildContentSecurityPolicy(new CheckpointPixelOptions { Enabled = false });

		using (Assert.EnterMultipleScope())
		{
			Assert.That(enabled, Does.Contain("script-src 'self' https://kya.vouched.id;"));
			Assert.That(enabled, Does.Contain("connect-src 'self' https://kya.vouched.id;"));
			Assert.That(disabled, Does.Not.Contain("kya.vouched.id"));
		}
	}

	[Test]
	public async Task Checkpoint_pixel_reports_to_your_project()
	{
		using var factory = new WebsiteFactory();
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");
		var pixel = document.QuerySelector("script[data-project-id]");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(pixel?.GetAttribute("src"), Is.EqualTo("https://kya.vouched.id/pixel.js"));
			Assert.That(pixel?.GetAttribute("data-project-id"), Is.EqualTo(WebsiteFactory.TestProjectId));
			Assert.That(pixel?.HasAttribute("data-debug"), Is.False);
		}
	}

	[Test]
	public async Task Checkpoint_pixel_logs_its_reports_to_the_browser_console_when_debugging()
	{
		using var factory = new WebsiteFactory(settings: new Dictionary<string, string?> { ["CheckpointPixel:Debug"] = "true" });
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");

		Assert.That(document.QuerySelector("script[data-project-id]")?.GetAttribute("data-debug"), Is.EqualTo("true"));
	}

	[Test]
	public async Task Checkpoint_pixel_is_omitted_until_you_add_a_project_id()
	{
		using var factory = new WebsiteFactory(Environments.Development, isConfigured: false);
		using var client = factory.CreateClient();

		var document = await client.GetDocumentAsync("/");

		Assert.That(document.QuerySelector("script[data-project-id]"), Is.Null);
	}
}
