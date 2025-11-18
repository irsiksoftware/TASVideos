# ADR-0004: Custom Wiki Engine with Revision History

## Status

Accepted

## Date

2024-11-18 (Documented retrospectively)

## Decision Makers

* TASVideos Development Team

## Context

TASVideos needed a wiki system that:
- Documents game strategies, techniques, and platform information
- Supports structured content with headings, lists, tables, links
- Embeds dynamic content (recent submissions, publication lists, etc.)
- Tracks complete revision history for accountability and rollback
- Links pages together with automatic broken link detection
- Controls editing permissions (system pages vs. user pages)
- Performs full-text search across all content
- Handles page moves and renames with reference updates

Requirements:
- **Revision tracking:** Every edit must be preserved with author and timestamp
- **Dynamic modules:** Embed live data (e.g., "10 most recent submissions")
- **Link management:** Track page references and detect broken links
- **Hierarchical pages:** Support subpages (e.g., "GameResources/NES/SMB")
- **Soft deletes:** Allow page deletion and restoration
- **Permission-based editing:** Different pages have different edit requirements
- **Table of contents:** Auto-generate TOC from headings

Traditional wiki engines (MediaWiki, DokuWiki) don't integrate well with .NET applications and lack the dynamic module system needed for TASVideos.

## Decision

Build a **custom wiki engine** (`TASVideos.WikiEngine`) with:
- Custom markup language optimized for TASVideos content
- AST (Abstract Syntax Tree) based parser
- Revision-based storage (every edit creates new revision)
- Module system for embedding dynamic content
- Automatic link tracking and broken link detection

### Architecture

**Parser:** TASVideos.WikiEngine/NewParser.cs
- Lexical analysis and recursive descent parsing
- Generates AST (Abstract Syntax Tree)
- Node types in `NodeImplementations/`:
  - Text, Headings, Lists, Tables
  - Links (internal, external, interwiki)
  - Modules (dynamic content)
  - Formatting (bold, italic, etc.)

**Example Markup:**
```wiki
!!! Game Resources / NES / Super Mario Bros.

%%MODULE SubpageNavigation%%

[h2]Overview[/h2]

This page documents techniques for Super Mario Bros. speedruns.

[ul]
[li][link=/path|Link text][/li]
[li]__Bold text__[/li]
[/ul]

%%MODULE LatestPublications|limit=5%%
```

### Service Layer

**TASVideos.Core/Services/Wiki/WikiPages.cs** provides:
```csharp
public interface IWikiPages
{
    // Page operations
    Task<bool> Exists(string? pageName, bool includeDeleted = false);
    ValueTask<IWikiPage?> Page(string? pageName, int? revisionId = null);
    Task<IWikiPage?> Add(WikiCreateRequest addRequest);
    Task<IWikiPage?> Edit(WikiEditRequest editRequest);

    // Page management
    Task<bool> Move(string originalName, string destinationName, int authorId);
    Task<int> Delete(string pageName);
    Task<bool> Undelete(string pageName);

    // Link tracking
    Task<IReadOnlyCollection<WikiOrphan>> Orphans();
    Task<IReadOnlyCollection<WikiPageReferral>> BrokenLinks();
    Task<IReadOnlyCollection<WikiPageReferral>> Referrals(string pageName);

    // Search
    Task<ICollection<IWikiPage>> AllSubpagesOf(string path);
    Task<ICollection<WikiSearchResult>> Search(WikiSearchModel search);
}
```

### Database Schema

**Revision-Based Storage:**
```
wiki_pages (composite primary key: page_name, revision)
├─ page_name (text, citext)
├─ revision (int)
├─ markup (text)
├─ author_id (int)
├─ create_timestamp (datetime)
├─ is_deleted (bool)
├─ page_data (jsonb) - Structured metadata
└─ ...

wiki_page_referral
├─ referrer (text) - Page that contains the link
├─ referral (text) - Page being linked to
└─ index on (referral) for finding broken links
```

**Key Design Decisions:**
1. **Composite key (page_name, revision):** Every edit is a new row
2. **Soft deletes:** `is_deleted` flag instead of DELETE
3. **citext columns:** Case-insensitive page names
4. **Referral tracking:** Separate table for link graph

### Module System

Modules are dynamic components embedded in wiki pages:

**Built-in Modules:**
- `SubpageNavigation` - Breadcrumb trail
- `LatestPublications` - Recent TAS publications
- `LatestSubmissions` - Submissions in queue
- `GameList` - List of games for a platform
- `PlayerPointsTable` - Player ranking
- `ListRoles` - User roles and permissions

**Module Execution:**
- Parsed from markup: `%%MODULE ModuleName|param=value%%`
- Executed at render time (not cached in markup)
- Has access to database and services via DI
- Supports parameters for customization

### Features

1. **Full Revision History**
   - Every edit preserved with author and timestamp
   - View any historical revision
   - Compare revisions (diff view)
   - Rollback to previous revision

2. **Page Operations**
   - Create, edit, delete, undelete
   - Move (rename) with subpage handling
   - Duplicate detection

3. **Link Management**
   - Automatic referral tracking
   - Broken link detection
   - Orphan page detection
   - "What links here" functionality

4. **Search**
   - Full-text search using PostgreSQL tsvector
   - Search by page name or content
   - Filter by author or date

5. **Permissions**
   - System pages: Require `EditSystemPages` permission
   - Game resources: Require `EditGameResources` permission
   - Basic pages: Require `EditWikiPages` permission

6. **Table of Contents**
   - Auto-generated from headings
   - Hierarchical structure
   - Jump links

## Alternatives Considered

### MediaWiki
**Pros:**
- Industry standard (Wikipedia)
- Proven scalability
- Rich feature set
- Large community

**Cons:**
- PHP-based (different stack)
- Complex to integrate with .NET app
- Separate user database
- No module system for TASVideos features
- Heavyweight setup

**Why not chosen:** Integration challenges and inability to embed TASVideos-specific dynamic content.

### Markdown with Git Backend
**Pros:**
- Simple syntax
- Git provides version control
- Developer-friendly

**Cons:**
- No dynamic modules
- Git not designed for web editing
- Merge conflicts on concurrent edits
- No link tracking
- Limited formatting options

**Why not chosen:** Doesn't support dynamic content embedding and concurrent editing is problematic.

### Existing .NET Wiki Engines
**Pros:**
- Native .NET integration
- Some options available

**Cons:**
- Most are abandoned or unmaintained
- Don't support custom module system
- Limited customization
- May not handle TASVideos-specific requirements

**Why not chosen:** Need for custom modules and long-term maintainability.

### Notion/Confluence API
**Pros:**
- Modern UI
- Rich features
- Hosted solution

**Cons:**
- External dependency
- Ongoing costs
- Limited customization
- Can't embed TASVideos-specific data
- Vendor lock-in

**Why not chosen:** Need full control and integration with TASVideos data.

### DokuWiki
**Pros:**
- File-based (no database)
- Simple setup
- Good plugin system

**Cons:**
- PHP-based
- File storage problematic for multiple servers
- Separate auth system
- Plugin system not suitable for TASVideos modules

**Why not chosen:** Integration challenges and scaling issues with file-based storage.

## Consequences

### Positive

* **Full control:** Complete control over markup syntax and features
* **Revision history:** Every edit preserved with full attribution
* **Dynamic content:** Modules can embed live data from TASVideos database
* **Link tracking:** Automatic broken link detection and "what links here"
* **Integration:** Deep integration with TASVideos authentication and permissions
* **Search:** PostgreSQL full-text search across all content
* **Performance:** Optimized for TASVideos use cases
* **Hierarchical pages:** Natural support for subpages
* **Soft deletes:** Easy to recover accidentally deleted pages

### Negative

* **Custom maintenance:** Must maintain custom parser and renderer
* **Learning curve:** Users need to learn TASVideos wiki syntax
* **Limited WYSIWYG:** No visual editor (typing raw markup)
* **Markup documentation:** Need comprehensive syntax documentation
* **Migration complexity:** Hard to migrate to another wiki system
* **Testing burden:** Must test all markup combinations
* **Storage overhead:** Every revision stored separately (high storage for large pages)

### Neutral

* **Syntax differences:** Similar to but not identical to MediaWiki syntax
* **Module development:** Adding new modules requires code changes
* **Preview essential:** Users should preview before saving

## Links

* Code: [NewParser.cs](../../TASVideos.WikiEngine/NewParser.cs)
* Code: [WikiPages.cs](../../TASVideos.Core/Services/Wiki/WikiPages.cs)
* Code: [Wiki entities](../../TASVideos.Data/Entity/WikiPage.cs)
* Code: [Modules](../../TASVideos.Core/Services/Wiki/Modules/)
* Related ADRs: [ADR-0003](./ADR-0003-custom-forum-bbcode-engine.md) - Forum Engine
* Related ADRs: [ADR-0007](./ADR-0007-permission-based-authorization.md) - Authentication/Authorization
