namespace TASVideos.Api;

/// <summary>
/// Defines API endpoints for TAS submissions.
/// </summary>
/// <remarks>
/// Submissions are TAS movies that have been submitted to TASVideos for review
/// but have not yet been published. These endpoints provide access to submission
/// metadata, status, and related information.
/// </remarks>
internal static class SubmissionsEndpoints
{
	/// <summary>
	/// Maps submission-related endpoints to the application.
	/// </summary>
	/// <param name="app">The web application to map endpoints to.</param>
	/// <returns>The web application with submission endpoints mapped.</returns>
	public static WebApplication MapSubmissions(this WebApplication app)
	{
		var group = app.MapApiGroup("Submissions");

		group
			.MapGet("{id:int}", async (int id, ApplicationDbContext db) => ApiResults.OkOr404(
				await db.Submissions
					.ToSubmissionsResponse()
					.SingleOrDefaultAsync(p => p.Id == id)))
			.WithName("GetSubmissionById")
			.WithSummary("Get a submission by ID")
			.WithDescription("Retrieves detailed information about a specific TAS submission by its unique identifier.")
			.WithTags("Submissions")
			.ProducesFromId<SubmissionsResponse>("submission");

		group.MapGet("", async ([AsParameters] SubmissionsRequest request, HttpContext context, ApplicationDbContext db) =>
		{
			var validationError = ApiResults.Validate(request, context);
			if (validationError is not null)
			{
				return validationError;
			}

			var subs = (await db.Submissions
				.FilterBy(request)
				.ToSubmissionsResponse()
				.SortAndPaginate(request)
				.ToListAsync())
				.FieldSelect(request);

			return Results.Ok(subs);
		})
		.WithName("GetSubmissions")
		.WithSummary("Get submissions with filtering")
		.WithDescription(@"Retrieves a list of TAS submissions with optional filtering, sorting, and pagination.

Supports filtering by:
- System code
- Game ID
- Status (e.g., 'new', 'accepted', 'rejected')
- Year range

Results can be sorted by any sortable field and paginated using pageSize and currentPage parameters.

**Note:** When using field selection, the actual returned count may be less than pageSize due to deduplication of distinct values.")
		.WithTags("Submissions")
		.Receives<SubmissionsRequest>()
		.ProducesList<SubmissionsResponse>("a list of submissions, searchable by the given criteria.");

		return app;
	}
}
