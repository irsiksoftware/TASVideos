namespace TASVideos.Api;

/// <summary>
/// Defines API endpoints for TASVideos events.
/// </summary>
/// <remarks>
/// Events represent special marathon or showcase runs (e.g., GDQ, TASBot)
/// where TAS movies are featured or demonstrated. Events include metadata
/// about the event, authors, tags, and related URLs.
/// </remarks>
internal static class EventsEndpoints
{
	/// <summary>
	/// Maps event-related endpoints to the application.
	/// </summary>
	/// <param name="app">The web application to map endpoints to.</param>
	/// <returns>The web application with event endpoints mapped.</returns>
	public static WebApplication MapEvents(this WebApplication app)
	{
		var group = app.MapApiGroup("Events");

		group
			.MapGet("{id:int}", async (int id, ApplicationDbContext db) => ApiResults.OkOr404(
				await db.Events
					.ToEventsResponse()
					.SingleOrDefaultAsync(e => e.Id == id)))
			.WithName("GetEventById")
			.WithSummary("Get an event by ID")
			.WithDescription("Retrieves detailed information about a specific TASVideos event (e.g., marathon or showcase run) by its unique identifier.")
			.WithTags("Events")
			.ProducesFromId<EventsResponse>("event");

		group.MapGet("", async ([AsParameters] EventsRequest request, HttpContext context, ApplicationDbContext db) =>
		{
			var validationError = ApiResults.Validate(request, context);
			if (validationError is not null)
			{
				return validationError;
			}

			var events = (await db.Events
				.FilterByTokens(request)
				.SortAndPaginate(request)
				.ToListAsync())
			.FieldSelect(request);

			return Results.Ok(events);
		})
		.WithName("GetEvents")
		.WithSummary("Get events with filtering")
		.WithDescription(@"Retrieves a list of TASVideos events with optional filtering and sorting.

Events are special marathon or showcase runs where TAS movies are featured.

Supports filtering by:
- Years
- Tags
- Authors
- Event names

Results can be sorted by date or title.

**Note:** When using field selection, the actual returned count may be less than the requested limit due to deduplication of distinct values.")
		.WithTags("Events")
		.Receives<EventsRequest>()
		.ProducesList<EventsResponse>("a list of event entries, searchable by the given criteria");

		return app;
	}

	private static IQueryable<EventsResponse> ToEventsResponse(this IQueryable<Event> events)
	{
		return events
			.Include(e => e.Authors)
			.ThenInclude(a => a.Author)
			.Include(e => e.EventTags)
			.ThenInclude(t => t.Tag)
			.Include(e => e.EventUrls)
			.Select(e => new EventsResponse
			{
				Id = e.Id,
				Title = e.Title,
				EventName = e.EventName,
				EventDate = e.EventDate,
				SubmissionId = e.SubmissionId,
				CreateTimestamp = e.CreateTimestamp,
				Authors = e.Authors
					.OrderBy(a => a.Ordinal)
					.Select(a => a.Author!.UserName)
					.ToList(),
				AdditionalAuthors = e.AdditionalAuthors,
				Tags = e.EventTags.Select(t => t.Tag!.Code).ToList(),
				Urls = e.EventUrls.Select(u => new EventUrlResponse
				{
					Url = u.Url,
					Type = u.Type.ToString(),
					DisplayName = u.DisplayName
				}).ToList()
			});
	}

	private static IQueryable<EventsResponse> FilterByTokens(this IQueryable<Event> events, EventsRequest request)
	{
		var query = events
			.Include(e => e.Authors)
			.ThenInclude(a => a.Author)
			.Include(e => e.EventTags)
			.ThenInclude(t => t.Tag)
			.Include(e => e.EventUrls)
			.AsQueryable();

		if (request.Years.Any())
		{
			query = query.Where(e => request.Years.Contains(e.CreateTimestamp.Year));
		}

		if (request.Tags.Any())
		{
			query = query.Where(e => e.EventTags.Any(t => request.Tags.Contains(t.Tag!.Code)));
		}

		if (request.Authors.Any())
		{
			query = query.Where(e => e.Authors.Select(a => a.UserId).Any(a => request.Authors.Contains(a)));
		}

		if (request.EventNames.Any())
		{
			query = query.Where(e => request.EventNames.Contains(e.EventName));
		}

		return query.Select(e => new EventsResponse
		{
			Id = e.Id,
			Title = e.Title,
			EventName = e.EventName,
			EventDate = e.EventDate,
			SubmissionId = e.SubmissionId,
			CreateTimestamp = e.CreateTimestamp,
			Authors = e.Authors
				.OrderBy(a => a.Ordinal)
				.Select(a => a.Author!.UserName)
				.ToList(),
			AdditionalAuthors = e.AdditionalAuthors,
			Tags = e.EventTags.Select(t => t.Tag!.Code).ToList(),
			Urls = e.EventUrls.Select(u => new EventUrlResponse
			{
				Url = u.Url,
				Type = u.Type.ToString(),
				DisplayName = u.DisplayName
			}).ToList()
		});
	}

	private static IQueryable<EventsResponse> SortAndPaginate(this IQueryable<EventsResponse> query, EventsRequest request)
	{
		var sorted = request.SortBy switch
		{
			"date" => query.OrderBy(e => e.CreateTimestamp),
			"date-desc" => query.OrderByDescending(e => e.CreateTimestamp),
			"title" => query.OrderBy(e => e.Title),
			_ => query.OrderByDescending(e => e.CreateTimestamp)
		};

		if (request.Limit.HasValue)
		{
			sorted = (IOrderedQueryable<EventsResponse>)sorted.Take(request.Limit.Value);
		}

		return sorted;
	}
}
