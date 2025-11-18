namespace TASVideos.Api;

/// <summary>
/// Defines API endpoints for TASVideos publications.
/// </summary>
/// <remarks>
/// Publications are completed TAS (Tool-Assisted Speedrun) movies that have been
/// accepted and published on TASVideos.org. These endpoints provide access to
/// publication metadata, authors, tags, and related information.
/// </remarks>
internal static class PublicationsEndpoints
{
	/// <summary>
	/// Maps publication-related endpoints to the application.
	/// </summary>
	/// <param name="app">The web application to map endpoints to.</param>
	/// <returns>The web application with publication endpoints mapped.</returns>
	public static WebApplication MapPublications(this WebApplication app)
	{
		var group = app.MapApiGroup("Publications");

		group
			.MapGet("{id:int}", async (int id, ApplicationDbContext db) => ApiResults.OkOr404(
				await db.Publications
					.ToPublicationsResponse()
					.SingleOrDefaultAsync(p => p.Id == id)))
			.WithName("GetPublicationById")
			.WithSummary("Get a publication by ID")
			.WithDescription("Retrieves detailed information about a specific TASVideos publication by its unique identifier.")
			.WithTags("Publications")
			.ProducesFromId<PublicationsResponse>("publication");

		group.MapGet("", async ([AsParameters] PublicationsRequest request, HttpContext context, ApplicationDbContext db) =>
		{
			var validationError = ApiResults.Validate(request, context);
			if (validationError is not null)
			{
				return validationError;
			}

			var pubs = (await db.Publications
				.FilterByTokens(request)
				.ToPublicationsResponse()
				.SortAndPaginate(request)
				.ToListAsync())
			.FieldSelect(request);

			return Results.Ok(pubs);
		})
		.WithName("GetPublications")
		.WithSummary("Get publications with filtering")
		.WithDescription(@"Retrieves a list of publications with optional filtering, sorting, and pagination.

Supports filtering by:
- System code
- Game ID
- Class (e.g., 'standard', 'moons', 'stars')
- Tags
- Year range
- Obsolescence status

Results can be sorted by any sortable field and paginated using pageSize and currentPage parameters.

**Note:** When using field selection, the actual returned count may be less than pageSize due to deduplication of distinct values.")
		.WithTags("Publications")
		.Receives<PublicationsRequest>()
		.ProducesList<PublicationsResponse>("a list of publications, searchable by the given criteria");

		return app;
	}
}
