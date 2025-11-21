using System.Reflection;
using System.Text;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TASVideos.Api.RateLimiting;
using TASVideos.Api.Validators;
using TASVideos.Core.Settings;

namespace TASVideos.Api;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddTasvideosApi(this IServiceCollection services, AppSettings settings)
	{
		return services
			.AddProblemDetails()
			.AddScoped<IValidator<ApiRequest>, ApiRequestValidator>()
			.AddScoped<IValidator<AuthenticationRequest>, AuthenticationRequestValidator>()
			.AddScoped<IValidator<GamesRequest>, GamesRequestValidator>()
			.AddScoped<IValidator<PublicationsRequest>, PublicationsRequestValidator>()
			.AddScoped<IValidator<SubmissionsRequest>, SubmissionsRequestValidator>()
			.AddScoped<IValidator<TagAddEditRequest>, TagAddEditRequestValidator>()
			.AddApiRateLimiting(settings)
			.AddSwagger(settings);
	}

	private static IServiceCollection AddApiRateLimiting(this IServiceCollection services, AppSettings settings)
	{
		if (!settings.ApiRateLimit.EnableRateLimiting)
		{
			return services;
		}

		// Required for rate limiting
		services.AddMemoryCache();
		services.AddHttpContextAccessor();

		// Configure IP rate limiting
		services.Configure<ClientRateLimitOptions>(options =>
		{
			options.EnableEndpointRateLimiting = true;
			options.StackBlockedRequests = false;
			options.HttpStatusCode = 429;
			options.RealIpHeader = "X-Real-IP";
			options.ClientIdHeader = "X-ClientId";

			// General rules for different user tiers
			options.GeneralRules =
			[
				// Anonymous users: 100 requests/hour
				new RateLimitRule
				{
					Endpoint = "*",
					Period = "1h",
					Limit = settings.ApiRateLimit.AnonymousRequestsPerHour,
					ClientMatching = "anonymous:*"
				},
				// Authenticated users: 1000 requests/hour
				new RateLimitRule
				{
					Endpoint = "*",
					Period = "1h",
					Limit = settings.ApiRateLimit.AuthenticatedRequestsPerHour,
					ClientMatching = "authenticated:*"
				},
				// Admin users: Unlimited (very high limit)
				new RateLimitRule
				{
					Endpoint = "*",
					Period = "1h",
					Limit = 999999,
					ClientMatching = "admin:*"
				},
				// Search endpoints - more restrictive for anonymous users
				new RateLimitRule
				{
					Endpoint = "*/api/games",
					Period = "1h",
					Limit = settings.ApiRateLimit.SearchRequestsPerHour,
					ClientMatching = "anonymous:*"
				},
				new RateLimitRule
				{
					Endpoint = "*/api/publications",
					Period = "1h",
					Limit = settings.ApiRateLimit.SearchRequestsPerHour,
					ClientMatching = "anonymous:*"
				},
				new RateLimitRule
				{
					Endpoint = "*/api/submissions",
					Period = "1h",
					Limit = settings.ApiRateLimit.SearchRequestsPerHour,
					ClientMatching = "anonymous:*"
				},
				// Write operations (POST, PUT, DELETE) - authenticated only
				new RateLimitRule
				{
					Endpoint = "post:*/api/*",
					Period = "1h",
					Limit = settings.ApiRateLimit.WriteRequestsPerHour,
					ClientMatching = "authenticated:*"
				},
				new RateLimitRule
				{
					Endpoint = "put:*/api/*",
					Period = "1h",
					Limit = settings.ApiRateLimit.WriteRequestsPerHour,
					ClientMatching = "authenticated:*"
				},
				new RateLimitRule
				{
					Endpoint = "delete:*/api/*",
					Period = "1h",
					Limit = settings.ApiRateLimit.WriteRequestsPerHour,
					ClientMatching = "authenticated:*"
				}
			];

			options.ClientRateLimitPolicies = [];
		});

		// Register rate limiting stores and configuration
		services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
		services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
		services.AddSingleton<IRateLimitConfiguration, UserTierRateLimitConfiguration>();
		services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

		return services;
	}

	private static IServiceCollection AddSwagger(this IServiceCollection services, AppSettings settings)
	{
		services.AddAuthentication(x =>
		{
			x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
		}).AddJwtBearer(x =>
		{
			x.RequireHttpsMetadata = true;
			x.SaveToken = true;
			x.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(settings.Jwt.SecretKey)),
				ValidateIssuer = false,
				ValidateAudience = false
			};
		});

		var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version();

		services.AddEndpointsApiExplorer();
		return services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc(
				"v1",
				new OpenApiInfo
				{
					Title = "TASVideos API",
					Version = $"v{version.Major}.{version.Minor}.{version.Revision}",
					Description = @"# TASVideos API

The TASVideos API provides programmatic access to TASVideos.org content including publications, submissions, games, and more.

## Authentication

Most read endpoints are publicly accessible. Write operations require JWT Bearer authentication.

## Rate Limiting

API requests may be rate-limited. Please implement appropriate backoff strategies.

## Field Selection

**Important:** When using the `fields` parameter to select specific fields, the API returns distinct objects.
This means the actual returned count may be less than the requested `pageSize` due to deduplication.

For example, if you request 100 publications but only select the 'class' field, and only 5 unique classes exist in those 100 records, you will receive only 5 results.

## Pagination

Use `pageSize` and `currentPage` parameters to paginate through results. Maximum page size is 100.

## Sorting

Use the `sort` parameter with comma-separated field names. Prefix with `+` for ascending or `-` for descending order.
Example: `sort=-createTimestamp,+title`

## Resources

- [TASVideos.org](https://tasvideos.org)
- [Wiki Documentation](https://tasvideos.org/HomePages/Wiki)
- [API Changelog](https://tasvideos.org/HomePages/ApiChangelog)",
					Contact = new OpenApiContact
					{
						Name = "TASVideos Team",
						Url = new Uri("https://tasvideos.org/HomePages/Contact")
					},
					License = new OpenApiLicense
					{
						Name = "Creative Commons Attribution 2.0",
						Url = new Uri("https://creativecommons.org/licenses/by/2.0/")
					}
				});

			// Include XML comments
			var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
			var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
			if (File.Exists(xmlPath))
			{
				c.IncludeXmlComments(xmlPath);
			}

			// Add JWT Bearer authentication
			c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
			{
				Description = @"JWT Authorization header using the Bearer scheme.

Enter 'Bearer' [space] and then your token in the text input below.

Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
				Name = "Authorization",
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.ApiKey,
				Scheme = "Bearer"
			});

			c.AddSecurityRequirement(new OpenApiSecurityRequirement
			{
				{
					new OpenApiSecurityScheme
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.SecurityScheme,
							Id = "Bearer"
						},
						Scheme = "oauth2",
						Name = "Bearer",
						In = ParameterLocation.Header
					},
					new List<string>()
				}
			});

			// Enable annotations
			c.EnableAnnotations();

			// Add servers
			c.AddServer(new OpenApiServer
			{
				Url = "https://tasvideos.org",
				Description = "Production"
			});

			c.AddServer(new OpenApiServer
			{
				Url = "http://localhost:5000",
				Description = "Local Development"
			});
		});
	}
}
