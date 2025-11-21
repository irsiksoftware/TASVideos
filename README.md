![.NET Core](https://github.com/TASVideos/tasvideos/workflows/.NET%20Core/badge.svg)
[![GitHub open issues counter](https://img.shields.io/github/issues-raw/TASVideos/tasvideos.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/tasvideos/issues)
[![OpenSSF Best Practices](https://www.bestpractices.dev/projects/7161/badge)](https://www.bestpractices.dev/projects/7161)

# TASVideos

The official source code repository for [TASVideos.org](https://tasvideos.org) - the premier community and publication platform for Tool-Assisted Speedruns (TAS).

## What is TASVideos?

TASVideos is a community dedicated to Tool-Assisted Speedruns (TAS) - superhuman gameplay demonstrations created using emulators with frame-by-frame precision, save states, and other tools. This codebase powers the entire TASVideos.org platform, including movie publications, community forums, wiki documentation, and more.

This project aims to modernize the site with updated technologies and a more maintainable codebase while preserving the community's rich history and extensive content.

## Key Features

### Core Functionality
- **Publication System** - Publish, rate, and manage TAS movie submissions
- **Movie File Parsing** - Support for parsing various TAS movie file formats
- **Game Database** - Comprehensive game information with genres, platforms, and metadata
- **Awards System** - Recognize outstanding TAS contributions
- **Event Management** - Support for marathons, showcases, and special events

### Community Features
- **Forum Engine** - Custom-built forum system for community discussions
- **Wiki System** - Integrated wiki with custom markup language for documentation
- **Private Messaging** - Direct messaging with support for role groups
- **Activity Tracking** - Monitor user contributions and site activity
- **User Permissions** - Role-based access control system

### Technical Features
- **RESTful API** - JWT-authenticated API for programmatic access
- **Full-Text Search** - Advanced search capabilities across publications and content
- **Dark Mode** - Responsive design with light/dark theme support
- **Caching Layer** - Redis-based caching for optimal performance
- **Monitoring** - OpenTelemetry integration for observability

## Technology Stack

### Backend
- **.NET 8.0** - Modern cross-platform framework
- **ASP.NET Core** - Razor Pages for server-side rendering
- **Entity Framework Core** - ORM with PostgreSQL database
- **ASP.NET Identity** - Authentication and authorization

### Frontend
- **Bootstrap 5** - Responsive UI framework
- **SASS** - CSS preprocessing with custom theming
- **Razor Pages** - Server-side templating
- **JavaScript** - Client-side interactivity

### Infrastructure
- **PostgreSQL** - Primary database
- **Redis** - Distributed caching
- **MailKit** - Email notifications
- **ReCaptcha** - Bot protection
- **Serilog** - Structured logging
- **OpenTelemetry** - Metrics and monitoring

### Development & Testing
- **MSTest** - Unit testing framework
- **Playwright** - End-to-end testing
- **BenchmarkDotNet** - Performance benchmarking
- **StyleCop** - Code style enforcement
- **Custom Roslyn Analyzers** - Project-specific code analysis

## Project Structure

```
TASVideos/
├── TASVideos/              # Main web application (Razor Pages)
├── TASVideos.Api/          # RESTful API project
├── TASVideos.Core/         # Core business logic and services
├── TASVideos.Data/         # Database entities and migrations
├── TASVideos.WikiEngine/   # Custom wiki markup engine
├── TASVideos.ForumEngine/  # Forum functionality
├── TASVideos.MovieParsers/ # TAS movie file parsers
├── TASVideos.Common/       # Shared utilities and extensions
├── analyzers/              # Custom Roslyn analyzers
├── tests/                  # Test projects (unit, integration, E2E)
└── StaticErrorPages/       # Static error page templates
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- PostgreSQL 12 or later
- Redis (optional, for caching)
- Node.js (for frontend tooling)

### Local Development Setup

For detailed setup instructions, see the [Local Development Setup](https://github.com/TASVideos/tasvideos/wiki/Local-Development-Setup) wiki page.

Quick start:
```bash
# Clone the repository
git clone https://github.com/TASVideos/tasvideos.git
cd tasvideos

# Restore dependencies
dotnet restore

# Update database connection in appsettings.Development.json
# Run migrations
dotnet ef database update --project TASVideos.Data

# Run the application
dotnet run --project TASVideos
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test --collect:'Code Coverage'

# Merge coverage reports
dotnet dotnet-coverage merge 'tests/*/TestResults/**/*.coverage' \
    --output-format=cobertura \
    --output=merged.coverage.xml
```

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details on:
- Code of conduct
- Development workflow
- Code style standards (Allman style, tabs for indentation)
- Testing requirements
- Pull request process

For frontend contributions, review the [Design Specification](DESIGN-SPEC.md) for UI/UX guidelines.

Before starting work on a feature or bug fix, please check the [issues](https://github.com/TASVideos/tasvideos/issues) page or join the [site Discord](https://tasvideos.org/LiveChat) to coordinate with the team.

### Site Coding Standards

Contributors should follow the official [TASVideos Site Coding Standards](https://tasvideos.org/SiteCodingStandards).

## Documentation

- [Contributing Guidelines](CONTRIBUTING.md) - How to contribute to the project
- [Design Specification](DESIGN-SPEC.md) - Frontend design philosophy and guidelines
- [Security Policy](SECURITY.md) - Reporting security vulnerabilities
- [Code of Conduct](CODE_OF_CONDUCT.md) - Community standards

## Recent Developments

Recent improvements include:
- Auto-generated movie filenames based on publication IDs
- Event entry system for marathons and showcase runs
- Enhanced forum category management UI
- Private messaging to role groups
- Publication history tracking
- Centralized file storage system

For a complete list of changes, see the [commit history](https://github.com/TASVideos/tasvideos/commits/).

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Community

- **Website:** [tasvideos.org](https://tasvideos.org)
- **Discord:** [Join our Discord](https://tasvideos.org/LiveChat)
- **Issues:** [GitHub Issues](https://github.com/TASVideos/tasvideos/issues)

## Security

For security concerns, please review our [Security Policy](SECURITY.md) and report vulnerabilities responsibly to the project maintainers.

# Documentation

- **[Architecture Decision Records (ADRs)](./docs/architecture/README.md)** - Key architectural decisions and their rationale
- **[Frontend Design Specification](./docs/DESIGN-SPEC.md)** - Frontend design guidelines and standards

---

**Why did the TAS speedrunner break up with their partner?**

Because they were too frame-perfect and never had time for unoptimized relationships!
