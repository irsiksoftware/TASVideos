using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Http;

namespace TASVideos.Api.RateLimiting;

/// <summary>
/// Middleware to add rate limit headers to API responses
/// </summary>
public class RateLimitHeadersMiddleware
{
	private readonly RequestDelegate _next;
	private readonly IRateLimitConfiguration _rateLimitConfig;

	public RateLimitHeadersMiddleware(
		RequestDelegate next,
		IRateLimitConfiguration rateLimitConfig)
	{
		_next = next;
		_rateLimitConfig = rateLimitConfig;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		// Only add headers to API endpoints
		if (context.Request.Path.StartsWithSegments("/api"))
		{
			// Store original body stream
			var originalBodyStream = context.Response.Body;

			// Call the next middleware
			await _next(context);

			// Add rate limit headers if this was a rate limited response
			if (context.Response.StatusCode == 429)
			{
				// Add Retry-After header (in seconds)
				if (!context.Response.Headers.ContainsKey("Retry-After"))
				{
					context.Response.Headers["Retry-After"] = "3600"; // 1 hour
				}
			}
		}
		else
		{
			await _next(context);
		}
	}
}
