using KnowYourAgentWebsite.Configuration;
using KnowYourAgentWebsite.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace KnowYourAgentWebsite.Setup;

/// <summary>Lets the browser detect when the in-process restart that applies new keys has finished.</summary>
public sealed record SetupStatus(string InstanceId, bool HasProjectId);

static class SetupEndpoints
{
	public const string StatusPath = "/setup/status";

	public static IEndpointRouteBuilder MapSetupEndpoints(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet(StatusPath, GetStatus)
			.WithName("GetSetupStatus")
			.ExcludeFromDescription();

		return endpoints;
	}

	internal static Results<Ok<SetupStatus>, NotFound> GetStatus(
		HttpContext httpContext,
		LocalDeveloperAccess localDeveloperAccess,
		HostInstance hostInstance,
		IOptions<CheckpointCredentials> credentials)
	{
		if (!localDeveloperAccess.IsAllowed(httpContext))
			return TypedResults.NotFound();

		httpContext.Response.Headers.CacheControl = "no-store";
		return TypedResults.Ok(new SetupStatus(hostInstance.Id, credentials.Value.HasProjectId));
	}
}
