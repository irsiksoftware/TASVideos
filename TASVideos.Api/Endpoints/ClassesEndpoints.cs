namespace TASVideos.Api;

/// <summary>
/// Defines API endpoints for publication classes.
/// </summary>
/// <remarks>
/// Classes categorize publications by tier and quality (e.g., Standard, Moons, Stars).
/// Each class has specific criteria and represents different levels of entertainment value.
/// </remarks>
internal static class ClassesEndpoints
{
	/// <summary>
	/// Maps publication class-related endpoints to the application.
	/// </summary>
	/// <param name="app">The web application to map endpoints to.</param>
	/// <returns>The web application with class endpoints mapped.</returns>
	public static WebApplication MapClasses(this WebApplication app)
	{
		var group = app.MapApiGroup("Classes");

		group
			.MapGet("{id:int}", async (int id, IClassService classService) => ApiResults.OkOr404(await classService.GetById(id)))
			.WithName("GetClassById")
			.WithSummary("Get a publication class by ID")
			.WithDescription("Retrieves information about a specific publication class including its criteria and icon.")
			.WithTags("Classes")
			.ProducesFromId<PublicationClass>("publication class");

		group
			.MapGet("", async (IClassService classService) => Results.Ok(await classService.GetAll()))
			.WithName("GetClasses")
			.WithSummary("Get all publication classes")
			.WithDescription("Retrieves a list of all available publication classes (e.g., Standard, Moons, Stars) with their criteria and metadata.")
			.WithTags("Classes")
			.ProducesList<PublicationClass>("a list of available publication classes");

		return app;
	}
}
