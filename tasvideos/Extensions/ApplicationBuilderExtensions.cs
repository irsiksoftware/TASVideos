using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using TASVideos.Core.Settings;
using TASVideos.Middleware;

namespace TASVideos.Extensions;

public static class ApplicationBuilderExtensions
{
	/// <summary>
	/// Determines the appropriate Cross-Origin-Resource-Policy based on request context.
	/// - Authenticated users get 'same-origin' for maximum protection
	/// - Static files (images, js, css, fonts) get 'cross-origin' for public sharing
	/// - Public dynamic content gets 'same-site' for moderate protection
	/// </summary>
	private static string GetCrossOriginResourcePolicy(HttpContext context)
	{
		// For authenticated users, always use same-origin to prevent cross-origin access
		if (context.User.IsLoggedIn())
		{
			return "same-origin";
		}

		var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

		// Static files (images, scripts, styles, fonts, etc.) can be cross-origin
		// These are public assets that may legitimately be embedded elsewhere
		if (IsStaticAsset(path))
		{
			return "cross-origin";
		}

		// Public dynamic content gets same-site protection
		// This allows embedding from same-site contexts but blocks cross-origin
		return "same-site";
	}

	/// <summary>
	/// Checks if the request path is for a static asset.
	/// </summary>
	private static bool IsStaticAsset(string path)
	{
		// Common static file extensions
		string[] staticExtensions =
		[
			".js", ".css", ".map",           // Scripts and styles
			".jpg", ".jpeg", ".png", ".gif", ".svg", ".ico", ".webp", ".bmp", // Images
			".woff", ".woff2", ".ttf", ".eot", ".otf",                        // Fonts
			".json", ".xml", ".txt",                                          // Data files
			".mp4", ".webm", ".ogg", ".mp3", ".wav",                         // Media
			".pdf", ".zip", ".tar", ".gz"                                     // Documents/Archives
		];

		return staticExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
	}

	public static IApplicationBuilder UseRobots(this IApplicationBuilder app)
	{
		return app.UseWhen(
			context => context.Request.IsRobotsTxt(),
			appBuilder =>
			{
				appBuilder.UseMiddleware<RobotHandlingMiddleware>();
			});
	}

	public static WebApplication UseExceptionHandlers(this WebApplication app, IHostEnvironment env)
	{
		app.UseExceptionHandler("/Error");
		app.UseStatusCodePagesWithReExecute("/Error");
		return app;
	}

	public static IApplicationBuilder UseGzipCompression(this IApplicationBuilder app, AppSettings settings)
	{
		if (settings.EnableGzipCompression)
		{
			app.UseResponseCompression();
		}

		return app;
	}

	public static IApplicationBuilder UseStaticFilesWithExtensionMapping(this IApplicationBuilder app, IWebHostEnvironment env)
	{
		var contentTypeProvider = new FileExtensionContentTypeProvider();
		var staticFileOptions = new StaticFileOptions
		{
			ContentTypeProvider = contentTypeProvider,
			ServeUnknownFileTypes = true,
			DefaultContentType = "text/plain"
		};

		if (env.IsDevelopment())
		{
			staticFileOptions.FileProvider = new DevFallbackFileProvider(env.WebRootFileProvider);
		}

		return app.UseStaticFiles(staticFileOptions);
	}

	public static IApplicationBuilder UseMvcWithOptions(this IApplicationBuilder app, IHostEnvironment env, AppSettings settings)
	{
		string[] trustedJsHosts = [
			"https://cdn.jsdelivr.net",
			"https://cdnjs.cloudflare.com",
			"https://code.jquery.com",
			"https://embed.nicovideo.jp/watch/",
			"https://www.google.com/recaptcha/",
			"https://www.gstatic.com/recaptcha/",
			"https://www.youtube.com",
		];
		string[] cspDirectives = [
			"base-uri 'none'", // neutralises the `<base/>` footgun
			"default-src 'self'", // fallback for other `*-src` directives
			"font-src 'self' https://cdnjs.cloudflare.com/ajax/libs/font-awesome/ https://cdn.jsdelivr.net/", // CSS `font: url();` and `@font-face { src: url(); }` will be blocked unless they're from one of these domains (this also blocks nonstandard fonts installed on the system maybe)
			"form-action 'self'", // domains allowed for `<form action/>` (POST target page)
			"frame-src data: 'self' https://embed.nicovideo.jp/watch/ https://www.google.com/recaptcha/ https://www.youtube.com/embed/ https://archive.org/embed/", // allow these domains in <iframe/>
			"img-src * data:", // allow hotlinking images from any domain in UGC (not great)
			$"script-src 'self' {string.Join(' ', trustedJsHosts)}", // `<script/>`s will be blocked unless they're from one of these domains
			"style-src 'unsafe-inline' 'self' https://cdnjs.cloudflare.com/ajax/libs/font-awesome/", // allow `<style/>`, and `<link rel="stylesheet"/>` if it's from our domain or trusted CDN
			"upgrade-insecure-requests", // browser should automagically replace links to any `http://tasvideos.org/...` URL (in UGC, for example) with HTTPS
		];
		var contentSecurityPolicyValue = string.Join("; ", cspDirectives);
		var permissionsPolicyValue = string.Join(", ", [
			"camera=()", // defaults to `self`
			"display-capture=()", // defaults to `self`
			"fullscreen=()", // defaults to `self`
			"geolocation=()", // defaults to `self`
			"microphone=()", // defaults to `self`
			"publickey-credentials-get=()", // defaults to `self`
			"screen-wake-lock=()", // defaults to `self`
			"web-share=()", // defaults to `self`

			// ...and that's all the non-experimental options listed on MDN as of 2024-04
		]);
		app.Use(async (context, next) =>
		{
			context.Response.Headers["Cross-Origin-Embedder-Policy"] = "unsafe-none"; // this is as unsecure as before, but can't use `credentialless`, due to breaking YouTube Embeds, see https://github.com/TASVideos/tasvideos/issues/1852
			context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
			context.Response.Headers["Permissions-Policy"] = permissionsPolicyValue;
			context.Response.Headers.XXSSProtection = "1; mode=block";
			context.Response.Headers.XFrameOptions = "DENY";
			context.Response.Headers.XContentTypeOptions = "nosniff";
			context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
			context.Response.Headers.XPoweredBy = "";
			context.Response.Headers.ContentSecurityPolicy = contentSecurityPolicyValue;
			await next();

			// Set Cross-Origin-Resource-Policy based on authentication and content type
			// This prevents cross-origin access to authenticated content while allowing
			// public static resources to be embedded where needed
			context.Response.Headers["Cross-Origin-Resource-Policy"] = GetCrossOriginResourcePolicy(context);
		});

		app.UseCookiePolicy(new CookiePolicyOptions
		{
			Secure = CookieSecurePolicy.Always
		});

		app
			.UseRouting()
			.UseAuthorization();

		if (!env.IsProduction() && !env.IsStaging())
		{
			app.UseHsts();
		}

		return app.UseEndpoints(endpoints =>
		{
			endpoints.MapRazorPages();

			if (settings.EnableMetrics)
			{
				endpoints.MapPrometheusScrapingEndpoint().RequireAuthorization(builder =>
				{
					builder.RequireClaim(CustomClaimTypes.Permission, ((int)PermissionTo.SeeDiagnostics).ToString());
				});
			}

			// Health check endpoints
			endpoints.MapHealthChecks();
		});
	}

	private static void MapHealthChecks(this IEndpointRouteBuilder endpoints)
	{
		// Overall health - public endpoint
		endpoints.MapHealthChecks("/health", new HealthCheckOptions
		{
			Predicate = _ => true,
			ResponseWriter = async (context, report) =>
			{
				context.Response.ContentType = "application/json";
				var result = JsonSerializer.Serialize(new
				{
					status = report.Status.ToString(),
					timestamp = DateTime.UtcNow,
					totalDuration = report.TotalDuration.TotalMilliseconds
				}, new JsonSerializerOptions { WriteIndented = true });
				await context.Response.WriteAsync(result);
			}
		});

		// Readiness check - for Kubernetes/orchestrators
		// Checks critical services (database)
		endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
		{
			Predicate = check => check.Tags.Contains("critical") || check.Tags.Contains("db"),
			ResponseWriter = async (context, report) =>
			{
				context.Response.ContentType = "application/json";
				var result = JsonSerializer.Serialize(new
				{
					status = report.Status.ToString(),
					checks = report.Entries.Select(e => new
					{
						name = e.Key,
						status = e.Value.Status.ToString(),
						duration = e.Value.Duration.TotalMilliseconds
					}),
					timestamp = DateTime.UtcNow
				}, new JsonSerializerOptions { WriteIndented = true });
				await context.Response.WriteAsync(result);
			}
		});

		// Liveness check - for Kubernetes/orchestrators
		// Basic application liveness (no external dependencies)
		endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
		{
			Predicate = check => check.Tags.Contains("memory") || check.Tags.Contains("filesystem"),
			ResponseWriter = async (context, report) =>
			{
				context.Response.ContentType = "application/json";
				var result = JsonSerializer.Serialize(new
				{
					status = report.Status.ToString(),
					timestamp = DateTime.UtcNow
				}, new JsonSerializerOptions { WriteIndented = true });
				await context.Response.WriteAsync(result);
			}
		});

		// Detailed health check - admin only
		endpoints.MapHealthChecks("/health/detailed", new HealthCheckOptions
		{
			Predicate = _ => true,
			ResponseWriter = async (context, report) =>
			{
				context.Response.ContentType = "application/json";
				var result = JsonSerializer.Serialize(new
				{
					status = report.Status.ToString(),
					totalDuration = report.TotalDuration.TotalMilliseconds,
					timestamp = DateTime.UtcNow,
					checks = report.Entries.Select(e => new
					{
						name = e.Key,
						status = e.Value.Status.ToString(),
						description = e.Value.Description,
						duration = e.Value.Duration.TotalMilliseconds,
						tags = e.Value.Tags,
						exception = e.Value.Exception?.Message,
						data = e.Value.Data
					})
				}, new JsonSerializerOptions { WriteIndented = true });
				await context.Response.WriteAsync(result);
			}
		}).RequireAuthorization(builder =>
		{
			builder.RequireClaim(CustomClaimTypes.Permission, ((int)PermissionTo.SeeDiagnostics).ToString());
		});
	}
}
