using TASVideos.Data.Entity.Game;
using TASVideos.Data.Services;

namespace TASVideos.Api;

internal static class GamesEndpoints
{
	public static WebApplication MapGames(this WebApplication app)
	{
		var group = app.MapApiGroup("Games");

		group
			.MapGet("{id:int}", async (int id, IGamesConfigService gamesConfig) =>
			{
				var game = await gamesConfig.GetGameByIdAsync(id);
				return game != null ? Results.Ok(game) : Results.NotFound();
			})
			.ProducesFromId<GameDto>("game");

		group.MapGet("", async ([AsParameters] GamesRequest request, HttpContext context, IGamesConfigService gamesConfig) =>
		{
			var validationError = ApiResults.Validate(request, context);
			if (validationError is not null)
			{
				return validationError;
			}

			var games = await gamesConfig.GetAllGamesAsync();

			return Results.Ok(games);
		})
		.Receives<GamesRequest>()
		.ProducesList<GameDto>("a list of games");

		return app;
	}
}
