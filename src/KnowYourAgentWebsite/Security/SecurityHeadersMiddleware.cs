using KnowYourAgentWebsite.Configuration;
using Microsoft.Extensions.Options;

namespace KnowYourAgentWebsite.Security;

/// <summary>Adds defense-in-depth response headers to every response.</summary>
sealed class SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment, IOptions<CheckpointPixelOptions> pixelOptions)
{
	// Development is excluded because `dotnet watch` injects a hot-reload script that connects over a websocket on a random port
	readonly string? _contentSecurityPolicy = environment.IsDevelopment() ? null : BuildContentSecurityPolicy(pixelOptions.Value);

	public Task InvokeAsync(HttpContext context)
	{
		var headers = context.Response.Headers;
		headers.XContentTypeOptions = "nosniff";
		headers.XFrameOptions = "DENY";
		headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
		headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
		headers["Cross-Origin-Opener-Policy"] = "same-origin";

		if (_contentSecurityPolicy is not null)
			headers.ContentSecurityPolicy = _contentSecurityPolicy;

		return next(context);
	}

	internal static string BuildContentSecurityPolicy(CheckpointPixelOptions pixelOptions)
	{
		var pixelOrigin = pixelOptions.Enabled ? $" {pixelOptions.Origin}" : string.Empty;

		return string.Join("; ",
			"default-src 'self'",
			$"script-src 'self'{pixelOrigin}",
			$"connect-src 'self'{pixelOrigin}",
			"img-src 'self' data:",
			"style-src 'self'",
			"font-src 'self'",
			"object-src 'none'",
			"base-uri 'self'",
			"form-action 'self'",
			"frame-ancestors 'none'");
	}
}
