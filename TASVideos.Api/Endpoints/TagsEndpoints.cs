namespace TASVideos.Api;

/// <summary>
/// Defines API endpoints for publication tags.
/// </summary>
/// <remarks>
/// Tags are labels used to categorize and describe TAS publications.
/// Examples include gameplay styles, techniques used, or special characteristics.
/// This endpoint includes both read and write operations (write operations require authentication).
/// </remarks>
internal static class TagsEndpoints
{
	/// <summary>
	/// Maps tag-related endpoints to the application.
	/// </summary>
	/// <param name="app">The web application to map endpoints to.</param>
	/// <returns>The web application with tag endpoints mapped.</returns>
	public static WebApplication MapTags(this WebApplication app)
	{
		var group = app.MapApiGroup("Tags");

		group
			.MapGet("{id:int}", async (int id, ITagService tagService) => ApiResults.OkOr404(
				await tagService.GetById(id)))
			.WithName("GetByTagId")
			.WithSummary("Get a tag by ID")
			.WithDescription("Retrieves information about a specific publication tag by its unique identifier.")
			.WithTags("Tags")
			.ProducesFromId<TagsResponse>("tag");

		group.MapGet("", async ([AsParameters] ApiRequest request, HttpContext context, ITagService tagService) =>
		{
			var validationError = ApiResults.Validate(request, context);
			if (validationError is not null)
			{
				return validationError;
			}

			var tags = (await tagService.GetAll())
				.AsQueryable()
				.SortAndPaginate(request)
				.AsEnumerable()
				.FieldSelect(request);

			return Results.Ok(tags);
		})
		.WithName("GetTags")
		.WithSummary("Get all tags")
		.WithDescription("Retrieves a list of all publication tags with optional sorting and pagination.")
		.WithTags("Tags")
		.Receives<ApiRequest>()
		.ProducesList<TagsResponse>("a list of publication tags");

		group.MapPost("", async (TagAddEditRequest request, ITagService tagService, HttpContext context) =>
		{
			var authError = ApiResults.Authorize(PermissionTo.TagMaintenance, context);
			if (authError is not null)
			{
				return authError;
			}

			var validationError = ApiResults.Validate(request, context);
			if (validationError is not null)
			{
				return validationError;
			}

			var (id, result) = await tagService.Add(request.Code, request.DisplayName);

			return result switch
			{
				TagEditResult.DuplicateCode => ApiResults.Conflict($"{request.Code} already exists"),
				TagEditResult.Success => Results.CreatedAtRoute(routeName: "GetByTagId", routeValues: new { id }),
				_ => ApiResults.BadRequest()
			};
		})
		.WithName("CreateTag")
		.WithSummary("Create a new tag")
		.WithDescription("Creates a new publication tag. Requires TagMaintenance permission. Returns 201 Created with the new tag's URL on success.")
		.WithTags("Tags")
		.RequireAuthorization()
		.WithOpenApi(g =>
		{
			g.Responses.Add(201, "The Tag was created successfully.");
			g.Responses.Add(401, "Authentication required.");
			g.Responses.Add(403, "Insufficient permissions.");
			g.Responses.Add(409, "A Tag with the given code already exists.");
			g.Responses.AddGeneric400();
			g.Responses.AddGeneric500();
			return g;
		});

		group.MapPut("{id:int}", async (int id, TagAddEditRequest request, ITagService tagService, HttpContext context) =>
		{
			var authError = ApiResults.Authorize(PermissionTo.TagMaintenance, context);
			if (authError is not null)
			{
				return authError;
			}

			var validationError = ApiResults.Validate(request, context);
			if (validationError is not null)
			{
				return validationError;
			}

			var result = await tagService.Edit(id, request.Code, request.DisplayName);
			return result switch
			{
				TagEditResult.NotFound => ApiResults.NotFound(),
				TagEditResult.DuplicateCode => ApiResults.Conflict($"{request.Code} already exists"),
				TagEditResult.Success => Results.Ok(),
				_ => ApiResults.BadRequest()
			};
		})
		.WithName("UpdateTag")
		.WithSummary("Update an existing tag")
		.WithDescription("Updates an existing publication tag's code or display name. Requires TagMaintenance permission.")
		.WithTags("Tags")
		.RequireAuthorization()
		.WithOpenApi(g =>
		{
			g.Responses.AddGeneric400();
			g.Responses.Add401();
			g.Responses.Add403();
			g.Responses.Add404ById("tag");
			g.Responses.Add(409, "A Tag with the given code already exists.");
			g.Responses.AddGeneric500();
			return g;
		});

		group.MapDelete("{id:int}", async (int id, ITagService tagService, HttpContext context) =>
		{
			var authError = ApiResults.Authorize(PermissionTo.TagMaintenance, context);
			if (authError is not null)
			{
				return authError;
			}

			var result = await tagService.Delete(id);
			return result switch
			{
				TagDeleteResult.NotFound => ApiResults.NotFound(),
				TagDeleteResult.InUse => ApiResults.Conflict("The tag is in use and cannot be deleted."),
				TagDeleteResult.Success => Results.Ok(),
				_ => ApiResults.BadRequest()
			};
		})
		.WithName("DeleteTag")
		.WithSummary("Delete an existing tag")
		.WithDescription("Deletes a publication tag if it is not in use. Requires TagMaintenance permission.")
		.WithTags("Tags")
		.RequireAuthorization()
		.WithOpenApi(g =>
		{
			g.Responses.AddGeneric400();
			g.Responses.Add401();
			g.Responses.Add403();
			g.Responses.Add404ById("tag");
			g.Responses.Add(409, "The Tag is in use and cannot be deleted.");
			g.Responses.AddGeneric500();
			return g;
		});

		return app;
	}
}
