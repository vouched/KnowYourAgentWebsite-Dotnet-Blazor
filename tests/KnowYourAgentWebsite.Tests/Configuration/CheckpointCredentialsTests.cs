using KnowYourAgentWebsite.Configuration;
using KnowYourAgentWebsite.Tests.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KnowYourAgentWebsite.Tests.Configuration;

[TestFixture]
public sealed class CheckpointCredentialsTests
{
	[TestCase("project", "key", true)]
	[TestCase("project", "", false)]
	[TestCase("", "key", false)]
	[TestCase("   ", "key", false)]
	[TestCase(null, null, false)]
	public void Server_side_reporting_needs_both_values(string? projectId, string? apiKey, bool expected)
	{
		var credentials = new CheckpointCredentials { ProjectId = projectId, ApiKey = apiKey };

		Assert.That(credentials.CanReportServerDetections, Is.EqualTo(expected));
	}

	[Test]
	public void A_project_id_is_all_the_pixel_needs()
	{
		var credentials = new CheckpointCredentials { ProjectId = "project" };

		using (Assert.EnterMultipleScope())
		{
			Assert.That(credentials.HasProjectId, Is.True);
			Assert.That(credentials.HasApiKey, Is.False);
		}
	}

	[TestCase(null, null)]
	[TestCase("", null)]
	[TestCase("short-key", "••••")]
	[TestCase("fake-api-key-0123456789abcd", "••••abcd")]
	public void Api_key_hint_never_reveals_the_key(string? apiKey, string? expectedHint)
	{
		var credentials = new CheckpointCredentials { ApiKey = apiKey };

		Assert.That(credentials.ApiKeyHint, Is.EqualTo(expectedHint));
	}

	[Test]
	public void Development_starts_without_credentials_so_the_home_page_can_guide_you()
	{
		var validator = new CheckpointCredentialsValidator(new FakeHostEnvironment(Environments.Development));

		var result = validator.Validate(Options.DefaultName, new CheckpointCredentials());

		Assert.That(result.Succeeded, Is.True);
	}

	[Test]
	public void Deployed_environments_explain_exactly_how_to_add_a_missing_project_id()
	{
		var validator = new CheckpointCredentialsValidator(new FakeHostEnvironment(Environments.Production));

		var result = validator.Validate(Options.DefaultName, new CheckpointCredentials { ApiKey = "sk_key" });

		using (Assert.EnterMultipleScope())
		{
			Assert.That(result.Failed, Is.True);
			Assert.That(result.FailureMessage, Does.Contain("Missing your Vouched Checkpoint Project ID (Checkpoint:ProjectId)."));
			Assert.That(result.FailureMessage, Does.Contain("Checkpoint__ProjectId="));
			Assert.That(result.FailureMessage, Does.Contain("Checkpoint__ApiKey=").And.Contain("optional"));
			Assert.That(result.FailureMessage, Does.Contain("https://kya.vouched.id"));
			Assert.That(result.FailureMessage, Does.Contain("README.md"));
		}
	}

	[Test]
	public void Deployed_environments_run_with_just_a_project_id()
	{
		var validator = new CheckpointCredentialsValidator(new FakeHostEnvironment(Environments.Production));

		var result = validator.Validate(Options.DefaultName, new CheckpointCredentials { ProjectId = "project" });

		Assert.That(result.Succeeded, Is.True);
	}

	[Test]
	public void Production_refuses_to_start_without_a_project_id()
	{
		using var factory = new WebsiteFactory(Environments.Production, isConfigured: false);

		var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient().Dispose());

		Assert.That(exception.Message, Does.Contain("Checkpoint__ProjectId"));
	}

	[Test]
	public void Production_starts_with_only_a_project_id()
	{
		using var factory = new WebsiteFactory(Environments.Production, settings: new Dictionary<string, string?> { ["Checkpoint:ApiKey"] = string.Empty });

		Assert.DoesNotThrow(() => factory.CreateClient().Dispose());
	}

	[Test]
	public void Production_starts_with_both_values()
	{
		using var factory = new WebsiteFactory(Environments.Production);

		Assert.DoesNotThrow(() => factory.CreateClient().Dispose());
	}

	sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = environmentName;

		public string ApplicationName { get; set; } = "KnowYourAgentWebsite";

		public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

		public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
	}
}
