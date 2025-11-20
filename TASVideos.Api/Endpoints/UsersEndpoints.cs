using Microsoft.OpenApi.Models;

namespace TASVideos.Api;

/// <summary>
/// Defines API endpoints for user authentication and management.
/// </summary>
internal static class UsersEndpoints
{
	/// <summary>
	/// Maps user-related endpoints to the application.
	/// </summary>
	/// <param name="app">The web application to map endpoints to.</param>
	/// <returns>The web application with user endpoints mapped.</returns>
	public static WebApplication MapUsers(this WebApplication app)
	{
		app.MapPost("api/v1/users/authenticate", async (AuthenticationRequest request, HttpContext context, IJwtAuthenticator jwtAuthenticator) =>
		{
			var validationError = ApiResults.Validate(request, context);
			if (validationError is not null)
			{
				return validationError;
			}

			var token = await jwtAuthenticator.Authenticate(request.Username, request.Password);
			return string.IsNullOrWhiteSpace(token)
				? ApiResults.Unauthorized()
				: Results.Ok(token);
		})
		.WithName("Authenticate")
		.WithTags("Users")
		.WithSummary("Authenticate a user")
		.WithDescription(@"Authenticates a user with username and password, returning a JWT Bearer token on success.

The returned token should be included in subsequent requests requiring authentication using the Authorization header:
```
Authorization: Bearer <token>
```

The token expires after a configured period and must be refreshed by re-authenticating.")
		.Produces<string>(StatusCodes.Status200OK, "text/plain")
		.Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/json")
		.Produces<ErrorResponse>(StatusCodes.Status401Unauthorized, "application/json")
		.Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/json")
		.WithOpenApi(g =>
		{
			g.Responses.AddGeneric400();
			g.Responses.Add("401", new OpenApiResponse { Description = "Invalid username or password." });
			g.Responses.AddGeneric500();
			return g;
		});

		return app;
	}
}
