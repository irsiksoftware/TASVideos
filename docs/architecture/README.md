# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records (ADRs) documenting key architectural decisions made in the TASVideos project.

## What are ADRs?

Architecture Decision Records capture important architectural decisions along with their context and consequences. They help:
- **New developers** understand why decisions were made
- **Current team** remember the reasoning behind choices
- **Future maintainers** evaluate whether decisions still make sense

## ADR Format

Each ADR follows a consistent structure:
- **Status:** Proposed, Accepted, Deprecated, or Superseded
- **Context:** The issue motivating the decision
- **Decision:** The chosen solution
- **Alternatives Considered:** Other options evaluated
- **Consequences:** Positive, negative, and neutral outcomes

## Creating New ADRs

Use [template.md](./template.md) when creating new ADRs.

## Current ADRs

### Core Technology Stack

- [ADR-0001: Use .NET 8 and ASP.NET Core with Razor Pages](./ADR-0001-dotnet-aspnetcore.md)
  - **Decision:** Use .NET 8.0 LTS with ASP.NET Core Razor Pages for web UI and Minimal APIs for REST endpoints
  - **Context:** Modern, performant framework with long-term support and strong typing
  - **Status:** Accepted

- [ADR-0002: PostgreSQL as Primary Database](./ADR-0002-postgresql-database.md)
  - **Decision:** Use PostgreSQL with Entity Framework Core, leveraging citext and full-text search
  - **Context:** Need for robust relational database with advanced text search and case-insensitive comparisons
  - **Status:** Accepted

### Custom Engines

- [ADR-0003: Custom BBCode Forum Engine](./ADR-0003-custom-forum-bbcode-engine.md)
  - **Decision:** Build custom BBCode parser with hierarchical tag system and configurable nesting rules
  - **Context:** Secure forum markup supporting TASVideos-specific features and legacy content
  - **Status:** Accepted

- [ADR-0004: Custom Wiki Engine with Revision History](./ADR-0004-custom-wiki-engine.md)
  - **Decision:** Custom wiki markup parser with AST, revision-based storage, and dynamic module system
  - **Context:** Wiki supporting dynamic content embedding and complete revision history
  - **Status:** Accepted

### Plugin & API Architecture

- [ADR-0005: Attribute-Based Movie Parser Plugin System](./ADR-0005-movie-parser-plugins.md)
  - **Decision:** Reflection-based plugin discovery using C# attributes for 23+ movie file formats
  - **Context:** Extensible system for parsing various TAS movie file formats
  - **Status:** Accepted

- [ADR-0006: Minimal API with Field Selection and URL Versioning](./ADR-0006-minimal-api-with-field-selection.md)
  - **Decision:** Use ASP.NET Core Minimal APIs with route groups, URL versioning (`/api/v1/`), and optional field selection
  - **Context:** Lightweight REST API for mobile apps and third-party integrations
  - **Status:** Accepted

### Security & Authorization

- [ADR-0007: Permission-Based Authorization with ASP.NET Core Identity](./ADR-0007-permission-based-authorization.md)
  - **Decision:** Fine-grained permission system with enum-based permissions, composable roles, and dual authentication (cookies + JWT)
  - **Context:** Complex authorization requirements with granular permission control
  - **Status:** Accepted

## ADR Index by Topic

### Technology Choices
- ADR-0001 (Platform), ADR-0002 (Database)

### Content Management
- ADR-0003 (Forum), ADR-0004 (Wiki)

### Extensibility
- ADR-0005 (Parsers)

### API Design
- ADR-0006 (REST API)

### Security
- ADR-0007 (Auth/Authz)

## Related Documentation

- [DESIGN-SPEC.md](../DESIGN-SPEC.md) - Frontend design specification
- [README.md](../../README.md) - Project overview and setup guide

## Questions?

For questions about architectural decisions or to propose changes, please:
1. Review the existing ADRs
2. Check the related code references
3. Open an issue or discussion on GitHub
