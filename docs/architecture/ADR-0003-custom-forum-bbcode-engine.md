# ADR-0003: Custom BBCode Forum Engine

## Status

Accepted

## Date

2024-11-18 (Documented retrospectively)

## Decision Makers

* TASVideos Development Team

## Context

TASVideos needed a forum system that:
- Allows users to format posts with rich text (bold, italic, links, images, quotes, code)
- Prevents XSS attacks from malicious HTML injection
- Maintains backward compatibility with legacy forum content
- Supports TASVideos-specific features (video embedding, user mentions, publication links)
- Provides control over allowed markup and nesting rules
- Renders quickly on the server side
- Works without JavaScript for accessibility

Traditional options:
1. **Allow raw HTML** - Major security risk
2. **Markdown** - Limited formatting options, no block quotes with attribution
3. **Third-party forum software** - Difficult to integrate with existing application
4. **Existing BBCode libraries** - Limited customization, often abandoned

The forum has 15+ years of legacy content that must continue to render correctly.

## Decision

Build a **custom BBCode parser** (`TASVideos.ForumEngine`) with:
- Tag-based markup similar to HTML but restricted to safe operations
- Hierarchical parsing with configurable nesting rules
- Block vs. inline element awareness for proper whitespace handling
- Extensible tag system for TASVideos-specific features
- Limited HTML fallback for legacy content

### Tag System Architecture

**Core Parser:** TASVideos.ForumEngine/BbParser.cs

```csharp
private static readonly Dictionary<string, TagInfo> KnownTags = new()
{
    { "b", new() },  // Inline formatting
    { "i", new() },
    { "u", new() },
    { "s", new() },
    { "quote", new() { IsBlock = true } },  // Block elements
    { "code", new() { Children = TagInfo.ChildrenAllowed.No, IsBlock = true } },
    { "url", new() { Children = TagInfo.ChildrenAllowed.IfParam } },
    { "img", new() { Children = TagInfo.ChildrenAllowed.No } },
    { "list", new() { Children = TagInfo.ChildrenAllowed.Restricted, IsBlock = true } },
    { "user", new() },  // TASVideos-specific
    { "module", new() { IsBlock = true } },  // Dynamic content
    // ... ~30 tags total
};
```

**Tag Configuration:**
- `IsBlock` - Treats as block element (adds newlines)
- `Children.No` - No child tags allowed (e.g., code, img)
- `Children.IfParam` - Children allowed only if parameter given (e.g., [url=...])
- `Children.Restricted` - Only specific child tags allowed (e.g., list -> *)
- `AllowSelfNesting` - Controls recursive nesting (default: not allowed)

### Features

1. **Standard BBCode Tags**
   - Formatting: [b], [i], [u], [s], [sup], [sub]
   - Structure: [quote], [code], [list], [*], [table], [tr], [td]
   - Media: [img], [url], [youtube], [archive]
   - Alignment: [center], [right]

2. **TASVideos-Specific Tags**
   - [user=UserName] - Link to user profile
   - [module=ModuleName] - Embed dynamic content
   - [publication=123M] - Link to publication

3. **Security Features**
   - URL validation (http/https only)
   - Image size restrictions
   - No JavaScript in URLs
   - HTML entity escaping

4. **Syntax Highlighting**
   - [code=language] with Prism.js integration
   - Supports: csharp, javascript, python, sql, etc.

5. **Legacy HTML Support**
   - Limited safe HTML tags: <b>, <i>, <u>, <s>, <br>, <hr>
   - Attributes stripped except href on <a>
   - All other HTML escaped

### Service Layer

**TASVideos.Core/Services/ForumService.cs** provides:
- Forum/topic/post CRUD operations
- Position calculation for pagination
- Topic watching and notifications
- Poll creation and voting
- Search integration with PostgreSQL full-text search

**Database Schema:**
```
ForumCategory (id, title, ordinal)
  └─ Forum (id, category_id, name, description)
      └─ ForumTopic (id, forum_id, title, user_id, poll_id)
          └─ ForumPost (id, topic_id, user_id, text, post_datetime)
```

## Alternatives Considered

### Markdown
**Pros:**
- Widely known syntax
- Simple to learn
- Many parsers available

**Cons:**
- No quote attribution ([quote=username])
- Limited formatting options
- Harder to prevent self-nesting issues
- No concept of block vs. inline elements

**Why not chosen:** BBCode provides better control over nesting and supports features like attributed quotes that are standard in forums.

### WYSIWYG Editor (TinyMCE, CKEditor)
**Pros:**
- Rich editing experience
- Familiar to users
- Visual feedback

**Cons:**
- Generates HTML (security concerns)
- Requires JavaScript
- Accessibility issues
- Complex sanitization needed
- Large client-side footprint

**Why not chosen:** Server-side BBCode parsing is more secure and accessible.

### Third-Party Forum Software (phpBB, vBulletin)
**Pros:**
- Full-featured
- Battle-tested
- Large communities

**Cons:**
- Separate application (data integration difficult)
- Different user authentication
- Limited customization
- Most are PHP-based (different stack)

**Why not chosen:** Need tight integration with TASVideos application and user system.

### Existing BBCode Libraries
**Pros:**
- Ready to use
- Community-tested

**Cons:**
- Limited customization options
- Often abandoned
- Don't support TASVideos-specific features
- May not handle legacy content correctly

**Why not chosen:** Custom parser provides full control over features and rendering.

### Allow Raw HTML with Sanitization
**Pros:**
- Maximum flexibility
- Familiar to developers

**Cons:**
- Security risk (sanitization libraries have CVEs)
- Hard to get right
- Attack surface is large
- Requires constant updates

**Why not chosen:** BBCode provides sufficient features with much better security.

## Consequences

### Positive

* **Security:** No XSS vulnerabilities from HTML injection
* **Control:** Full control over allowed tags and nesting
* **Performance:** Fast server-side rendering
* **Accessibility:** Works without JavaScript
* **Customization:** Easy to add TASVideos-specific features
* **Legacy support:** Backward compatible with old content
* **Maintainability:** Single codebase for all forum markup
* **Syntax highlighting:** Built-in code highlighting for technical discussions

### Negative

* **Custom maintenance:** Must maintain custom parser code
* **Learning curve:** Users need to learn BBCode syntax
* **Limited WYSIWYG:** No visual editor (typing raw BBCode)
* **Tag discovery:** Users may not know all available tags
* **Testing burden:** Must test all tag combinations and nesting scenarios

### Neutral

* **BBCode familiarity:** Some users know BBCode from other forums, others don't
* **Preview needed:** Users should preview before posting to see final rendering
* **Documentation:** Need to document all available tags and usage

## Links

* Code: [BbParser.cs](../../TASVideos.ForumEngine/BbParser.cs)
* Code: [ForumService.cs](../../TASVideos.Core/Services/ForumService.cs)
* Code: [Forum entities](../../TASVideos.Data/Entity/Forum/)
* Related ADRs: [ADR-0004](./ADR-0004-custom-wiki-engine.md) - Wiki Engine Architecture
* Related ADRs: [ADR-0007](./ADR-0007-permission-based-authorization.md) - Authentication/Authorization
