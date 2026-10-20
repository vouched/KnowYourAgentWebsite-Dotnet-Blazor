using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace KnowYourAgentWebsite.Setup;

/// <summary>
/// Writes values to the .NET user-secrets store: the same <c>secrets.json</c> file that <c>dotnet user-secrets set</c> edits.
/// </summary>
/// <remarks>
/// The file lives in your user profile, outside of this repository, so saved keys can never be committed by accident.
/// ASP.NET Core loads it automatically when <c>ASPNETCORE_ENVIRONMENT=Development</c>.
/// See https://learn.microsoft.com/aspnet/core/security/app-secrets for where it is stored on each OS.
/// </remarks>
public sealed class UserSecretsStore(string? filePath) : IDisposable
{
	static readonly JsonNodeOptions _jsonNodeOptions = new() { PropertyNameCaseInsensitive = true };
	static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

	readonly SemaphoreSlim _writeLock = new(1, 1);

	/// <summary>The absolute path of <c>secrets.json</c>, or <see langword="null"/> when the project has no <c>UserSecretsId</c>.</summary>
	public string? FilePath { get; } = filePath;

	public bool IsAvailable => FilePath is not null;

	/// <summary><see cref="FilePath"/> with the user profile shortened to <c>~</c>, for display.</summary>
	public string? DisplayPath
	{
		get
		{
			var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			return FilePath is not null && userProfile.Length > 0 && FilePath.StartsWith(userProfile, StringComparison.Ordinal)
				? $"~{FilePath[userProfile.Length..]}"
				: FilePath;
		}
	}

	/// <summary>Resolves the <c>secrets.json</c> path from the <see cref="UserSecretsIdAttribute"/> the SDK generates from the <c>UserSecretsId</c> in the .csproj.</summary>
	public static string? GetDefaultFilePath(Assembly assembly)
	{
		ArgumentNullException.ThrowIfNull(assembly);

		return assembly.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId is { Length: > 0 } userSecretsId
			? PathHelper.GetSecretsPathFromSecretsId(userSecretsId)
			: null;
	}

	/// <summary>Adds or replaces <paramref name="values"/> (or removes the ones that are <see langword="null"/>), preserving every other secret already in the file.</summary>
	public async Task SaveAsync(IReadOnlyDictionary<string, string?> values, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(values);

		var secretsFilePath = FilePath ?? throw new InvalidOperationException(
			"User secrets are unavailable because KnowYourAgentWebsite.csproj does not define a <UserSecretsId>. Add one with `dotnet user-secrets init --project src/KnowYourAgentWebsite`.");

		await _writeLock.WaitAsync(cancellationToken);
		try
		{
			var secrets = await ReadAsync(secretsFilePath, cancellationToken);

			foreach (var (key, value) in values)
				SetValue(secrets, key, value);

			Directory.CreateDirectory(Path.GetDirectoryName(secretsFilePath)!);

			// Write to a temporary file first so a crash can never leave a half-written secrets.json behind
			var temporaryFilePath = $"{secretsFilePath}.{Guid.NewGuid():N}.tmp";
			try
			{
				await using (var stream = CreateOwnerOnlyFile(temporaryFilePath))
				{
					await stream.WriteAsync(Encoding.UTF8.GetBytes(secrets.ToJsonString(_writeOptions)), cancellationToken);
				}

				File.Move(temporaryFilePath, secretsFilePath, overwrite: true);
			}
			finally
			{
				File.Delete(temporaryFilePath);
			}
		}
		finally
		{
			_writeLock.Release();
		}
	}

	public void Dispose() => _writeLock.Dispose();

	static async Task<JsonObject> ReadAsync(string secretsFilePath, CancellationToken cancellationToken)
	{
		if (!File.Exists(secretsFilePath))
			return new JsonObject(_jsonNodeOptions);

		var json = await File.ReadAllTextAsync(secretsFilePath, cancellationToken);
		if (string.IsNullOrWhiteSpace(json))
			return new JsonObject(_jsonNodeOptions);

		try
		{
			return JsonNode.Parse(json, _jsonNodeOptions, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject
				?? throw new InvalidOperationException($"'{secretsFilePath}' must contain a JSON object.");
		}
		catch (JsonException exception)
		{
			throw new InvalidOperationException($"'{secretsFilePath}' is not valid JSON. Fix or delete the file, then try again.", exception);
		}
	}

	// `dotnet user-secrets set` writes flat keys ("Checkpoint:ApiKey"), but the file may also have been hand-edited into nested sections.
	// Writing into an existing section avoids creating a duplicate key, which would stop the app from loading the file.
	static void SetValue(JsonObject node, string key, string? value)
	{
		var separatorIndex = key.IndexOf(':', StringComparison.Ordinal);
		var section = separatorIndex > 0 ? node[key[..separatorIndex]] as JsonObject : null;

		if (value is null)
		{
			// Remove every spelling of the key so configuration stops seeing it
			node.Remove(key);

			if (section is not null)
				SetValue(section, key[(separatorIndex + 1)..], null);

			return;
		}

		if (section is not null && !node.ContainsKey(key))
		{
			SetValue(section, key[(separatorIndex + 1)..], value);
			return;
		}

		node[key] = value;
	}

	static FileStream CreateOwnerOnlyFile(string path)
	{
		var options = new FileStreamOptions
		{
			Mode = FileMode.CreateNew,
			Access = FileAccess.Write,
			Share = FileShare.None,
			Options = FileOptions.Asynchronous,
		};

		if (!OperatingSystem.IsWindows())
			options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

		return new FileStream(path, options);
	}
}
