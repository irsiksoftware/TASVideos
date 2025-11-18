# ADR-0006: Minimal API with Field Selection and URL Versioning

## Status

Accepted

## Date

2024-11-18 (Documented retrospectively)

## Decision Makers

* TASVideos Development Team

## Context

TASVideos needed a RESTful API to:
- Support mobile apps and third-party integrations
- Provide programmatic access to publications, submissions, games, users
- Authenticate with JWT Bearer tokens (separate from cookie auth)
- Support pagination and filtering
- Allow clients to request only needed fields (reduce payload size)
- Version the API for future breaking changes
- Generate OpenAPI/Swagger documentation

API consumers include:
- Mobile apps with limited bandwidth
- Discord bots announcing new publications
- Third-party analytics tools
- Community-built tools and scripts

Requirements:
- **Performance:** Fast response times, minimal overhead
- **Flexibility:** Clients choose which fields to receive
- **Discoverability:** Self-documenting with Swagger UI
- **Versioning:** Support multiple API versions simultaneously
- **Validation:** Request validation with clear error messages
- **Security:** JWT authentication, prevent over-fetching

Traditional controller-based APIs add overhead and ceremony. With .NET 8, Minimal APIs provide a lightweight alternative.

## Decision

Use **ASP.NET Core Minimal APIs** with **route groups**, **URL-based versioning**, and **field selection**.

### Architecture

**Endpoint Registration:** TASVideos.Api/WebApplicationExtensions.cs

```csharp
public static IApplicationBuilder UseTasvideosApiEndpoints(
    this WebApplication app,
    AppSettings settings)
{
    return app.MapPublications()
        .MapSubmissions()
        .MapEvents()
        .MapGames()
        .MapSystems()
        .MapUsers()
        .MapTags()
        .MapClasses()
        .MapMigrations();
}
```

**Route Group Helper:** TASVideos.Api/Helpers/RouteGroupExtensions.cs

```csharp
public static RouteGroupBuilder MapApiGroup(
    this WebApplication app,
    string groupName)
{
    return app.MapGroup($"/api/v1/{groupName.ToLower()}")
        .WithTags(groupName)
        .WithOpenApi();
}
```

### Example Endpoint

**TASVideos.Api/Endpoints/PublicationsEndpoints.cs:**

```csharp
internal static class PublicationsEndpoints
{
    public static WebApplication MapPublications(this WebApplication app)
    {
        var group = app.MapApiGroup("Publications");

        // GET /api/v1/publications/{id}
        group.MapGet("{id:int}", async (
            int id,
            ApplicationDbContext db) =>
        {
            var publication = await db.Publications
                .ToPublicationsResponse()
                .SingleOrDefaultAsync(p => p.Id == id);

            return ApiResults.OkOr404(publication);
        })
        .ProducesFromId<PublicationsResponse>("publication")
        .WithName("GetPublication");

        // GET /api/v1/publications?search=mario&limit=10
        group.MapGet("", async (
            [AsParameters] PublicationsRequest request,
            ApplicationDbContext db) =>
        {
            var query = db.Publications.AsQueryable();

            // Apply filters from request
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(p =>
                    EF.Functions.ILike(p.Title, $"%{request.Search}%"));
            }

            // Pagination
            var total = await query.CountAsync();
            var results = await query
                .OrderByDescending(p => p.CreateTimestamp)
                .Skip((request.Page - 1) * request.Limit)
                .Take(request.Limit)
                .ToPublicationsResponse()
                .ToListAsync();

            return new PagedResponse<PublicationsResponse>
            {
                Items = results,
                TotalCount = total,
                Page = request.Page,
                Limit = request.Limit
            };
        })
        .ProducesFromRequest<PublicationsRequest, PublicationsResponse>("publications");

        return app;
    }
}
```

### Field Selection

**Feature:** Clients specify which fields to return in response.

**Query Parameter:** `?fields=id,title,authors`

**Implementation:** TASVideos.Api/Helpers/FieldSelection.cs

```csharp
public static IQueryable<TResponse> SelectFields<TResponse>(
    this IQueryable<TResponse> query,
    string? fields)
{
    if (string.IsNullOrWhiteSpace(fields))
    {
        return query; // Return all fields
    }

    // Parse requested fields
    var fieldList = fields.Split(',')
        .Select(f => f.Trim())
        .ToList();

    // Use Expression Trees to build dynamic projection
    var parameter = Expression.Parameter(typeof(TResponse), "x");
    var bindings = fieldList.Select(field =>
    {
        var property = typeof(TResponse).GetProperty(
            field,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (property == null)
        {
            throw new ApiException($"Unknown field: {field}");
        }

        return Expression.Bind(
            property,
            Expression.Property(parameter, property));
    });

    var memberInit = Expression.MemberInit(
        Expression.New(typeof(TResponse)),
        bindings);

    var lambda = Expression.Lambda<Func<TResponse, TResponse>>(
        memberInit,
        parameter);

    return query.Select(lambda);
}
```

**Example Request:**
```
GET /api/v1/publications/1000?fields=id,title,authors
```

**Response:**
```json
{
  "id": 1000,
  "title": "Super Mario Bros.",
  "authors": ["Author1", "Author2"]
}
```

### Versioning Strategy

**Current:** URL-based versioning (`/api/v1/...`)

**Future Strategy:**
1. New version: Create `/api/v2/...` endpoints
2. Keep v1 running for backward compatibility
3. Deprecation notice in v1 responses (custom header)
4. Eventually sunset v1 after migration period

**Benefits:**
- Simple and explicit
- Easy to route in reverse proxies
- Clear in URLs and logs
- No header parsing required

**OpenAPI:** Separate Swagger docs per version

```csharp
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TASVideos API",
        Version = "v1",
        Description = "RESTful API for TASVideos"
    });
});
```

### Authentication

**JWT Bearer Tokens:** TASVideos.Api/ServiceCollectionExtensions.cs

```csharp
services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = true;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});
```

**Token Generation:** TASVideos.Core/Services/JwtAuthenticator.cs

Users obtain tokens via `/api/v1/auth/token` endpoint with username/password.

### Request Validation

**FluentValidation:** TASVideos.Api/Validators/

```csharp
public class PublicationsRequestValidator : AbstractValidator<PublicationsRequest>
{
    public PublicationsRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100)
            .WithMessage("Limit must be between 1 and 100");
    }
}
```

Validation errors return `400 Bad Request` with detailed error messages.

### Swagger/OpenAPI Documentation

**Endpoint:** `/api` - Interactive Swagger UI

**Features:**
- Try endpoints directly from browser
- See request/response schemas
- Authentication via JWT token
- Example requests

## Alternatives Considered

### Controller-Based API
**Example:**
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class PublicationsController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<PublicationResponse>> Get(int id)
    {
        // ...
    }
}
```

**Pros:**
- Traditional and familiar
- More structure and conventions
- Better for large teams

**Cons:**
- More boilerplate code
- Slower performance (routing overhead)
- Unnecessary ceremony for simple endpoints

**Why not chosen:** Minimal APIs are faster and simpler for TASVideos' straightforward API needs.

### GraphQL
**Pros:**
- Flexible field selection built-in
- Single endpoint
- Strongly typed schema

**Cons:**
- Steeper learning curve
- More complex setup
- Harder to cache
- Overkill for TASVideos API

**Why not chosen:** REST with field selection provides sufficient flexibility without GraphQL complexity.

### OData
**Pros:**
- Standardized query language
- Rich filtering and sorting
- Field selection built-in

**Cons:**
- Complex specification
- Large payloads
- Performance issues with complex queries
- Over-engineered for TASVideos

**Why not chosen:** Custom field selection provides what's needed without OData overhead.

### Header-Based Versioning
**Example:** `Accept: application/vnd.tasvideos.v1+json`

**Pros:**
- RESTful purist approach
- Cleaner URLs

**Cons:**
- Harder to test (need to set headers)
- Not visible in URL
- Harder to route in proxies
- More complex implementation

**Why not chosen:** URL-based versioning is simpler and more explicit.

### Query Parameter Versioning
**Example:** `/api/publications?version=1`

**Pros:**
- Easy to implement
- Visible in URL

**Cons:**
- Pollutes query string
- Easy to omit
- Less clear than path segment

**Why not chosen:** URL path segment is clearer and more conventional.

## Consequences

### Positive

* **Performance:** Minimal APIs are faster than controllers
* **Simplicity:** Less boilerplate, easier to read
* **Field selection:** Reduces payload size for mobile apps
* **Versioning:** Clear and explicit in URLs
* **Documentation:** Swagger UI is self-documenting
* **JWT authentication:** Stateless and scalable
* **Validation:** FluentValidation provides clear error messages
* **Route groups:** Clean organization of related endpoints

### Negative

* **Less structure:** No controller conventions to follow
* **Discovery:** Endpoints spread across multiple files
* **Middleware:** Some controller features unavailable
* **Reflection overhead:** Field selection uses reflection (mitigated by caching)

### Neutral

* **Learning curve:** Developers must learn Minimal API patterns
* **Field selection adoption:** Clients must opt-in to field selection
* **Versioning commitment:** Must maintain old versions for compatibility

## Future Considerations

1. **Rate limiting:** Add per-user/IP rate limits to prevent abuse
2. **Caching:** Add response caching for frequently accessed resources
3. **API v2:** When breaking changes needed, introduce `/api/v2/...`
4. **WebSockets:** Consider real-time API for live submission updates
5. **Batch operations:** Support batch requests (e.g., get multiple publications)

## Links

* Code: [WebApplicationExtensions.cs](../../TASVideos.Api/WebApplicationExtensions.cs)
* Code: [PublicationsEndpoints.cs](../../TASVideos.Api/Endpoints/PublicationsEndpoints.cs)
* Code: [ServiceCollectionExtensions.cs](../../TASVideos.Api/ServiceCollectionExtensions.cs)
* Code: [JwtAuthenticator.cs](../../TASVideos.Core/Services/JwtAuthenticator.cs)
* Related ADRs: [ADR-0001](./ADR-0001-dotnet-aspnetcore.md) - .NET and ASP.NET Core
* Related ADRs: [ADR-0007](./ADR-0007-permission-based-authorization.md) - Authentication/Authorization
* Documentation: [Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
