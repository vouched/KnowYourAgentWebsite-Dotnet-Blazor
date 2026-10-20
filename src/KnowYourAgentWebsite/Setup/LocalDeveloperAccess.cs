using System.Net;

namespace KnowYourAgentWebsite.Setup;

/// <summary>
/// Decides whether this page's developer tools (saving your Vouched keys and sending simulated traffic) may be used.
/// </summary>
/// <remarks>
/// They write to disk and to your Vouched dashboard, so they're only available in the Development environment and only to
/// requests made on the machine running the app. Requiring a loopback <c>Host</c> header as well stops a DNS-rebinding page
/// in your browser from reaching them through a hostname that resolves to 127.0.0.1, and rejecting the headers proxies add keeps
/// them out of reach of dev tunnels and reverse proxies, which also connect from loopback. A raw TCP tunnel adds no headers,
/// so like any app running in Development, never expose this one to the internet.
/// </remarks>
public sealed class LocalDeveloperAccess(IHostEnvironment environment)
{
	// Dev Tunnels, ngrok, Cloudflare, nginx, YARP and friends add at least one of these when they forward a request
	static readonly string[] _forwardingHeaders =
	[
		"Forwarded",
		"Via",
		"X-Forwarded-For",
		"X-Forwarded-Host",
		"X-Forwarded-Port",
		"X-Forwarded-Proto",
		"X-Forwarded-Server",
		"X-Original-Host",
		"X-Real-IP",
		"CF-Connecting-IP",
		"True-Client-IP",
	];

	public bool IsAllowed(HttpContext? httpContext) =>
		environment.IsDevelopment() && httpContext is not null && IsLocalRequest(httpContext);

	static bool IsLocalRequest(HttpContext httpContext) =>
		IsLoopback(httpContext.Connection.RemoteIpAddress)
		&& IsLoopbackHost(httpContext.Request.Host.Host)
		&& !_forwardingHeaders.Any(httpContext.Request.Headers.ContainsKey);

	static bool IsLoopback(IPAddress? ipAddress) =>
		ipAddress is not null && IPAddress.IsLoopback(ipAddress.IsIPv4MappedToIPv6 ? ipAddress.MapToIPv4() : ipAddress);

	static bool IsLoopbackHost(string host) =>
		string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
		|| (IPAddress.TryParse(host.Trim('[', ']'), out var hostAddress) && IsLoopback(hostAddress));
}
