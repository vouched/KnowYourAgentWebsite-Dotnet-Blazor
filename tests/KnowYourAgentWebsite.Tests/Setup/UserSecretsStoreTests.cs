using System.Text.Json.Nodes;
using KnowYourAgentWebsite.Setup;

namespace KnowYourAgentWebsite.Tests.Setup;

[TestFixture]
public sealed class UserSecretsStoreTests
{
	string _directory = default!;
	string _filePath = default!;
	UserSecretsStore _store = default!;

	[SetUp]
	public void SetUp()
	{
		_directory = Path.Combine(Path.GetTempPath(), "KnowYourAgentWebsite.Tests", Guid.NewGuid().ToString("N"));
		_filePath = Path.Combine(_directory, "nested", "secrets.json");
		_store = new UserSecretsStore(_filePath);
	}

	[TearDown]
	public void TearDown()
	{
		_store.Dispose();

		if (Directory.Exists(_directory))
			Directory.Delete(_directory, recursive: true);
	}

	[Test]
	public async Task Creates_the_secrets_file_with_flat_keys_like_dotnet_user_secrets()
	{
		await _store.SaveAsync(new Dictionary<string, string?> { ["Checkpoint:ApiKey"] = "key" }, CancellationToken.None);

		var json = await ReadJsonAsync();
		Assert.That(json["Checkpoint:ApiKey"]?.GetValue<string>(), Is.EqualTo("key"));
	}

	[Test]
	public async Task Preserves_every_other_secret_in_the_file()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
		await File.WriteAllTextAsync(_filePath, """{ "ConnectionStrings:Db": "keep-me", "Checkpoint:ApiKey": "old" }""");

		await _store.SaveAsync(new Dictionary<string, string?> { ["Checkpoint:ApiKey"] = "new" }, CancellationToken.None);

		var json = await ReadJsonAsync();
		using (Assert.EnterMultipleScope())
		{
			Assert.That(json["ConnectionStrings:Db"]?.GetValue<string>(), Is.EqualTo("keep-me"));
			Assert.That(json["Checkpoint:ApiKey"]?.GetValue<string>(), Is.EqualTo("new"));
		}
	}

	[Test]
	public async Task Writes_into_an_existing_nested_section_instead_of_creating_a_duplicate_key()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
		await File.WriteAllTextAsync(_filePath, """{ "checkpoint": { "ProjectId": "old" } }""");

		await _store.SaveAsync(new Dictionary<string, string?> { ["Checkpoint:ProjectId"] = "new" }, CancellationToken.None);

		var json = await ReadJsonAsync();
		using (Assert.EnterMultipleScope())
		{
			Assert.That(json["checkpoint"]?["ProjectId"]?.GetValue<string>(), Is.EqualTo("new"));
			Assert.That(json.ContainsKey("Checkpoint:ProjectId"), Is.False);
		}
	}

	[Test]
	public async Task Null_values_remove_the_key()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
		await File.WriteAllTextAsync(_filePath, """{ "Checkpoint:ProjectId": "old", "Checkpoint:ApiKey": "sk_old" }""");

		await _store.SaveAsync(new Dictionary<string, string?> { ["Checkpoint:ProjectId"] = "new", ["Checkpoint:ApiKey"] = null }, CancellationToken.None);

		var json = await ReadJsonAsync();
		using (Assert.EnterMultipleScope())
		{
			Assert.That(json["Checkpoint:ProjectId"]?.GetValue<string>(), Is.EqualTo("new"));
			Assert.That(json.ContainsKey("Checkpoint:ApiKey"), Is.False);
		}
	}

	[Test]
	public async Task Null_values_also_remove_the_key_from_a_nested_section()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
		await File.WriteAllTextAsync(_filePath, """{ "Checkpoint": { "ProjectId": "old", "ApiKey": "sk_old" } }""");

		await _store.SaveAsync(new Dictionary<string, string?> { ["Checkpoint:ApiKey"] = null }, CancellationToken.None);

		var json = await ReadJsonAsync();
		using (Assert.EnterMultipleScope())
		{
			Assert.That(json["Checkpoint"]?["ProjectId"]?.GetValue<string>(), Is.EqualTo("old"));
			Assert.That(json["Checkpoint"]!.AsObject().ContainsKey("ApiKey"), Is.False);
		}
	}

	[Test]
	public async Task Reports_a_corrupt_secrets_file_instead_of_overwriting_it()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
		await File.WriteAllTextAsync(_filePath, "{ not json");

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			_store.SaveAsync(new Dictionary<string, string?> { ["Checkpoint:ApiKey"] = "key" }, CancellationToken.None));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(exception!.Message, Does.Contain(_filePath));
			Assert.That(await File.ReadAllTextAsync(_filePath), Is.EqualTo("{ not json"));
		}
	}

	[Test]
	public async Task Leaves_no_temporary_files_behind()
	{
		await _store.SaveAsync(new Dictionary<string, string?> { ["Checkpoint:ApiKey"] = "key" }, CancellationToken.None);

		Assert.That(Directory.GetFiles(Path.GetDirectoryName(_filePath)!), Is.EqualTo(new[] { _filePath }));
	}

	[Test]
	public async Task Only_your_user_account_can_read_the_secrets_file()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Ignore("Windows protects the file with the user profile's ACLs instead of Unix permissions.");
			return;
		}

		await _store.SaveAsync(new Dictionary<string, string?> { ["Checkpoint:ApiKey"] = "key" }, CancellationToken.None);

		Assert.That(File.GetUnixFileMode(_filePath), Is.EqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite));
	}

	[Test]
	public async Task Explains_how_to_fix_a_project_without_a_UserSecretsId()
	{
		using var store = new UserSecretsStore(filePath: null);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			store.SaveAsync(new Dictionary<string, string?>(), CancellationToken.None));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(store.IsAvailable, Is.False);
			Assert.That(exception!.Message, Does.Contain("dotnet user-secrets init"));
		}
	}

	[Test]
	public void Resolves_the_path_from_the_projects_UserSecretsId()
	{
		var path = UserSecretsStore.GetDefaultFilePath(typeof(WebsiteApplication).Assembly);

		Assert.That(path, Does.EndWith(Path.Combine("eb760258-5806-4996-b846-6a747b5af07a", "secrets.json")));
	}

	[Test]
	public void Display_path_abbreviates_the_user_profile()
	{
		var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		using var store = new UserSecretsStore(Path.Combine(userProfile, ".microsoft", "usersecrets", "id", "secrets.json"));

		Assert.That(store.DisplayPath, Is.EqualTo($"~{Path.DirectorySeparatorChar}{Path.Combine(".microsoft", "usersecrets", "id", "secrets.json")}"));
	}

	[Test]
	public void Has_no_path_for_assemblies_without_a_UserSecretsId() =>
		Assert.That(UserSecretsStore.GetDefaultFilePath(typeof(UserSecretsStoreTests).Assembly), Is.Null);

	async Task<JsonObject> ReadJsonAsync() =>
		JsonNode.Parse(await File.ReadAllTextAsync(_filePath), new JsonNodeOptions { PropertyNameCaseInsensitive = true })!.AsObject();
}
