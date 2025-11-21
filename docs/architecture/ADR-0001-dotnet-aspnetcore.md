# ADR-0001: Use .NET 8 and ASP.NET Core with Razor Pages

## Status

Accepted

## Date

2024-11-18 (Documented retrospectively)

## Decision Makers

* TASVideos Development Team

## Context

TASVideos needed a modern, performant web framework to support:
- Server-side rendered pages for SEO and accessibility
- RESTful API for mobile apps and third-party integrations
- Long-term support and active development from the platform vendor
- Strong typing and compile-time safety
- Cross-platform deployment capabilities
- Integration with existing .NET ecosystem libraries

The application has complex domain logic including:
- Forum and wiki engines
- Movie file parsing from 23+ different formats
- Publication workflow with multiple approval stages
- User authentication and fine-grained permissions
- Full-text search capabilities

## Decision

Use **.NET 8.0** with **ASP.NET Core** and **Razor Pages** architecture.

### Key Technology Choices

1. **.NET 8.0** (LTS release)
   - Target framework: `net8.0`
   - C# 12.0 language features
   - Long-term support until November 2026

2. **ASP.NET Core Razor Pages** for web UI
   - Page-focused development model
   - Better suited for content-heavy pages than MVC
   - Built-in anti-forgery protection

3. **Minimal APIs** for RESTful endpoints
   - Lightweight alternative to controllers
   - Better performance for simple API routes
   - Cleaner route definition with route groups

4. **Modular architecture** with extension methods
   - Each subsystem (Data, Core, API, Parsers) is a separate library
   - Self-contained dependency injection via `ServiceCollectionExtensions.cs`
   - Clean separation of concerns

### Implementation Details

**Project Structure:**
- `TASVideos.Data` - Entity Framework Core data access
- `TASVideos.Core` - Business logic and services
- `TASVideos.Api` - REST API endpoints
- `TASVideos.Parsers` - Movie file parsers
- `TASVideos.ForumEngine` - Forum markup processing
- `TASVideos.WikiEngine` - Wiki markup processing
- `tasvideos` - Main web application

**Configuration:** Directory.Build.props
```xml
<TargetFramework>net8.0</TargetFramework>
<LangVersion>12.0</LangVersion>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
```

**Centralized Package Management:** Directory.Packages.props
- All package versions defined in single file
- Prevents version conflicts across projects
- Easier to maintain and update dependencies

## Alternatives Considered

### Node.js with Express/Next.js
**Pros:**
- Large ecosystem
- JavaScript/TypeScript familiarity
- Good for real-time features

**Cons:**
- Weaker typing (even with TypeScript)
- Less suitable for CPU-intensive parsing tasks
- Higher memory usage for long-running processes

**Why not chosen:** Movie file parsing and complex domain logic benefit from .NET's performance and strong typing.

### Python with Django/Flask
**Pros:**
- Rapid development
- Strong data science libraries
- Good for prototyping

**Cons:**
- Performance limitations for high-traffic scenarios
- GIL (Global Interpreter Lock) issues
- Deployment complexity

**Why not chosen:** Performance requirements and need for compile-time safety.

### Java with Spring Boot
**Pros:**
- Mature ecosystem
- Enterprise-grade features
- Strong typing

**Cons:**
- More verbose than C#
- Slower startup times
- Heavier resource usage

**Why not chosen:** .NET offers similar benefits with better developer experience and performance.

## Consequences

### Positive

* **Performance:** Excellent runtime performance and startup times
* **Long-term support:** .NET 8 LTS supported until November 2026
* **Type safety:** Nullable reference types catch potential bugs at compile-time
* **Developer productivity:** Modern C# features reduce boilerplate code
* **Tooling:** Excellent IDE support (Visual Studio, Rider, VS Code)
* **Cross-platform:** Runs on Linux, Windows, macOS
* **Modular architecture:** Easy to test and maintain individual components
* **Central package management:** Simplifies dependency updates

### Negative

* **Learning curve:** Developers need .NET/C# knowledge
* **Windows legacy:** Some tools and documentation assume Windows development
* **Breaking changes:** Future .NET versions may require migration effort
* **Package ecosystem:** Smaller than JavaScript ecosystem for some niches

### Neutral

* **.NET release cadence:** New major version every November
* **Microsoft dependency:** Framework direction controlled by Microsoft
* **Memory footprint:** Higher than interpreted languages, lower than JVM

## Links

* Code: [Directory.Build.props](../../Directory.Build.props)
* Code: [Directory.Packages.props](../../Directory.Packages.props)
* Code: [Program.cs](../../tasvideos/Program.cs)
* Code: [ServiceCollectionExtensions.cs](../../TASVideos.Data/ServiceCollectionExtensions.cs)
* Related ADRs: [ADR-0005](./ADR-0005-movie-parser-plugins.md) - Movie Parser Plugin System
* Documentation: [.NET 8 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
