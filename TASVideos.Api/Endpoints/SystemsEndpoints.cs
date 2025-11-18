namespace TASVideos.Api;

/// <summary>
/// Defines API endpoints for gaming systems.
/// </summary>
/// <remarks>
/// Systems represent gaming platforms (e.g., NES, SNES, Genesis) with their
/// technical specifications including supported framerates.
/// </remarks>
internal static class SystemsEndpoints
{
	/// <summary>
	/// Maps system-related endpoints to the application.
	/// </summary>
	/// <param name="app">The web application to map endpoints to.</param>
	/// <returns>The web application with system endpoints mapped.</returns>
	public static WebApplication MapSystems(this WebApplication app)
	{
		var group = app.MapApiGroup("Systems");

		group
			.MapGet("{id:int}", async (int id, IGameSystemService systemService) => ApiResults.OkOr404((await systemService.GetAll()).SingleOrDefault(p => p.Id == id)))
			.WithName("GetSystemById")
			.WithSummary("Get a gaming system by ID")
			.WithDescription("Retrieves information about a specific gaming system including its supported framerates.")
			.WithTags("Systems")
			.ProducesFromId<SystemsResponse>("system");

		group
			.MapGet("", async (IGameSystemService systemService) => Results.Ok(await systemService.GetAll()))
			.WithName("GetSystems")
			.WithSummary("Get all gaming systems")
			.WithDescription("Retrieves a list of all available gaming systems with their technical specifications and supported framerates.")
			.WithTags("Systems")
			.ProducesList<SystemsResponse>("a list of available game systems, including supported framerates");

		return app;
	}
}
