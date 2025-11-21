# ADR-0002: PostgreSQL as Primary Database

## Status

Accepted

## Date

2024-11-18 (Documented retrospectively)

## Decision Makers

* TASVideos Development Team

## Context

TASVideos requires a robust relational database to manage:
- **Complex relationships:** Users, roles, permissions, forums, topics, posts, wiki pages, games, publications, submissions
- **Full-text search:** Search across wiki pages, forum posts, and publication metadata
- **Case-insensitive text:** Usernames, page names, and forum topics need case-insensitive comparisons
- **Data integrity:** Referential integrity, transactions, and ACID compliance
- **Audit history:** Track all changes to entities with user attribution
- **Scalability:** Support thousands of users and millions of records
- **Developer experience:** Good ORM support and tooling

Key requirements:
- Advanced text search capabilities beyond simple LIKE queries
- Efficient handling of case-insensitive string comparisons
- Support for complex queries with multiple joins
- Native support for JSON data types (for flexible metadata storage)
- Open-source licensing
- Active community and long-term viability

## Decision

Use **PostgreSQL** as the primary database with **Entity Framework Core** as the ORM.

### Key Features Utilized

1. **citext Extension** for case-insensitive text
   - All string columns automatically use citext
   - No need for LOWER() comparisons in queries
   - Better index utilization

2. **Full-Text Search (tsvector)**
   - Native PostgreSQL full-text search
   - Search across wiki pages and forum posts
   - Better than LIKE queries for large text corpuses

3. **Snake_case Naming Convention**
   - Uses `EFCore.NamingConventions` package
   - Aligns with PostgreSQL best practices
   - Table: `wiki_pages`, Column: `created_on`

4. **Auto-History Tracking**
   - Every entity change logged automatically
   - Tracks: entity type, operation, user, timestamp, old/new values
   - Implemented via `SaveChanges` override

5. **Npgsql Provider**
   - High-performance .NET data provider
   - Version 8.0.4 with EF Core 8.0.6

### Configuration

**Connection Setup:** TASVideos.Data/ServiceCollectionExtensions.cs
```csharp
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, b =>
        b.MigrationsAssembly("TASVideos.Data")
         .UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery))
        .UseSnakeCaseNamingConvention();

    if (isDevelopment)
    {
        options.EnableSensitiveDataLogging();
    }
});
```

**Database Initialization Strategies:** TASVideos.Core/Data/DbInitializer.cs
1. **Minimal** - `EnsureCreated()` for quick testing
2. **Migrate** - `Database.MigrateAsync()` for production
3. **Sample** - Downloads sample data for development

## Alternatives Considered

### SQL Server
**Pros:**
- Native Windows integration
- Excellent tooling (SSMS, Azure Data Studio)
- Strong .NET integration
- Mature EF Core support

**Cons:**
- Limited Linux support (SQL Server on Linux is newer)
- Licensing costs for production
- Case-insensitive by default (harder to change)
- Full-text search setup more complex

**Why not chosen:** PostgreSQL offers better cross-platform support and open-source licensing without compromising features.

### MySQL/MariaDB
**Pros:**
- Wide hosting support
- Mature and stable
- Good performance

**Cons:**
- Weaker full-text search (before 5.7)
- Less advanced SQL features
- Case sensitivity depends on collation and OS
- JSON support less mature than PostgreSQL

**Why not chosen:** PostgreSQL's advanced features (citext, tsvector, better JSON support) provide better developer experience.

### MongoDB
**Pros:**
- Schema flexibility
- Good for rapid prototyping
- Horizontal scaling

**Cons:**
- Lack of ACID transactions (in earlier versions)
- No foreign key constraints
- Complex relational queries difficult
- ORM support less mature

**Why not chosen:** TASVideos has complex relational data that benefits from SQL and referential integrity.

### SQLite
**Pros:**
- Zero configuration
- Perfect for development
- Serverless

**Cons:**
- Limited concurrency (write locks)
- No citext equivalent
- Weak full-text search
- Not suitable for production web apps with multiple users

**Why not chosen:** Insufficient for production workload with concurrent writes.

## Consequences

### Positive

* **Full-text search:** Native tsvector support provides fast, relevant search results
* **Case-insensitive text:** citext extension eliminates case-handling bugs and improves query performance
* **Developer experience:** Snake_case naming feels natural in PostgreSQL
* **Performance:** Excellent query optimizer and index support
* **Open source:** No licensing costs, active community
* **Cross-platform:** Runs on Linux, Windows, macOS
* **JSON support:** Store flexible metadata without schema changes
* **Auto-history:** Complete audit trail of all data changes
* **Advanced features:** CTEs, window functions, array types, and more

### Negative

* **Learning curve:** Developers need PostgreSQL-specific knowledge
* **Hosting:** Requires PostgreSQL hosting (can't use shared MySQL hosting)
* **Backup complexity:** Need proper backup strategy (pg_dump, WAL archiving)
* **Case sensitivity:** Default case-sensitive behavior can surprise developers (mitigated by citext)
* **Migration from other DBs:** Would require effort to switch database systems

### Neutral

* **Version upgrades:** Major PostgreSQL versions may require testing and migration
* **Extensions:** Reliance on extensions (citext) requires ensuring they're installed
* **EF Core limitations:** Some PostgreSQL features not exposed through EF Core

## Links

* Code: [ServiceCollectionExtensions.cs](../../TASVideos.Data/ServiceCollectionExtensions.cs)
* Code: [ApplicationDbContext.cs](../../TASVideos.Data/ApplicationDbContext.cs)
* Code: [DbInitializer.cs](../../TASVideos.Core/Data/DbInitializer.cs)
* Related ADRs: [ADR-0001](./ADR-0001-dotnet-aspnetcore.md) - .NET and ASP.NET Core
* Package: [Npgsql.EntityFrameworkCore.PostgreSQL](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL)
* Package: [EFCore.NamingConventions](https://www.nuget.org/packages/EFCore.NamingConventions)
* Documentation: [PostgreSQL Documentation](https://www.postgresql.org/docs/)
