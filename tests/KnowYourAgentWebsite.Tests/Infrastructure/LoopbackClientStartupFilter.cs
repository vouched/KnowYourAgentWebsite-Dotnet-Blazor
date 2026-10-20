using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace KnowYourAgentWebsite.Tests.Infrastructure;

/// <summary>The in-memory TestServer has no socket, so it reports no client IP. This makes requests look like they come from 127.0.0.1.</summary>
sealed class LoopbackClientStartupFilter : IStartupFilter
{
	public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
	{
		app.Use((context, nextMiddleware) =>
		{
			context.Connection.RemoteIpAddress ??= IPAddress.Loopback;
			return nextMiddleware(context);
		});

		next(app);
	};
}
