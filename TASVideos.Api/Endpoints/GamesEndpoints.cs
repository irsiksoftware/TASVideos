using TASVideos.Data.Entity.Game;

namespace TASVideos.Api;

/// <summary>
/// Defines API endpoints for video games in the TASVideos database.
/// </summary>
/// <remarks>
/// Games represent the video game titles available for TAS creation.
/// Each game includes metadata such as system, genre, publisher, and related publications.
/// </remarks>
internal static class GamesEndpoints
{
	/// <summary>
	/// Maps game-related endpoints to the application.
	/// </summary>
	/// <param name="app">The web application to map endpoints to.</param>
	/// <returns>The web application with game endpoints mapped.</returns>
	public static WebApplication MapGames(this WebApplication app)
	{
		var group = app.MapApiGroup("Games");

		group
			.MapGet("{id:int}", async (int id, ApplicationDbContext db) => ApiResults.OkOr404(
				await db.Games
					.ToGamesResponse()
					.SingleOrDefaultAsync(g => g.Id == id)))
			.WithName("GetGameById")
			.WithSummary("Get a game by ID")
			.WithDescription("Retrieves detailed information about a specific video game by its unique identifier.")
			.WithTags("Games")
			.ProducesFromId<GamesResponse>("game");

		group.MapGet("", async ([AsParameters] GamesRequest request, HttpContext context, ApplicationDbContext db) =>
		{
			var validationError = ApiResults.Validate(request, context);
			if (validationError is not null)
			{
				return validationError;
			}

			var games = (await db.Games.ForSystemCodes([.. request.SystemCodes])
					.ToGamesResponse()
					.SortAndPaginate(request)
					.ToListAsync())
				.FieldSelect(request);

			return Results.Ok(games);
		})
		.WithName("GetGames")
		.WithSummary("Get games with filtering")
		.WithDescription(@"Retrieves a list of games with optional filtering by system codes.

You can filter games by one or more system codes (e.g., 'NES', 'SNES', 'Genesis').
Results support sorting and pagination.

**Note:** When using field selection, the actual returned count may be less than pageSize due to deduplication of distinct values.")
		.WithTags("Games")
		.Receives<GamesRequest>()
		.ProducesList<GamesResponse>("a list of games");

		return app;
	}
}
