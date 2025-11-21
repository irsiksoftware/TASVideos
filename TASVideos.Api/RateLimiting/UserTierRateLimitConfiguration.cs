using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TASVideos.Data.Entity;

namespace TASVideos.Api.RateLimiting;

/// <summary>
/// Custom rate limit configuration that applies different rate limits based on user authentication tier.
/// </summary>
public class UserTierRateLimitConfiguration : RateLimitConfiguration
{
	public UserTierRateLimitConfiguration(
		IHttpContextAccessor httpContextAccessor,
		IOptions<IpRateLimitOptions> ipOptions,
		IOptions<ClientRateLimitOptions> clientOptions)
		: base(httpContextAccessor, ipOptions, clientOptions)
	{
	}

	public override void RegisterResolvers()
	{
		base.RegisterResolvers();
	}

	protected override ClientRequestIdentity SetClientIdentity(HttpContext httpContext)
	{
		var identity = base.SetClientIdentity(httpContext);

		// Determine user tier based on authentication and permissions
		var user = httpContext.User;

		if (user.IsLoggedIn())
		{
			// Check if user has high-level admin permissions (unlimited rate limit)
			if (user.Has(PermissionTo.SeeDiagnostics))
			{
				identity.ClientId = "admin:" + identity.ClientId;
			}
			else
			{
				// Authenticated users get higher rate limits
				identity.ClientId = "authenticated:" + identity.ClientId;
			}
		}
		else
		{
			// Anonymous users get the lowest rate limit
			identity.ClientId = "anonymous:" + identity.ClientId;
		}

		return identity;
	}
}
