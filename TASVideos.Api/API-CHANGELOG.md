# TASVideos API Changelog

All notable changes to the TASVideos API will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this API adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive OpenAPI/Swagger documentation with detailed descriptions
- XML documentation for all API endpoints
- Standardized error response models (`ErrorResponse`, `ValidationErrorResponse`)
- Detailed field selection behavior documentation
- Multiple server environment configurations (Production, Local Development)
- Enhanced authentication documentation with JWT Bearer token examples
- Postman collection export (`TASVideos-API.postman_collection.json`)
- Code examples for C#, JavaScript, Python, and cURL
- Comprehensive API documentation (`API-DOCUMENTATION.md`)
- API changelog tracking (`API-CHANGELOG.md`)

### Changed
- **DOCUMENTED BEHAVIOR:** Field selection now explicitly documented as applying deduplication
  - When using the `fields` parameter, returned count may be less than `pageSize`
  - This is intentional to avoid returning duplicate data
  - See API documentation for details and examples
- Enhanced Swagger UI description with usage guidelines
- Improved error response schemas with proper HTTP status codes
- Updated all endpoint documentation with detailed summaries and descriptions
- Added proper response type declarations for all endpoints

### Fixed
- Resolved TODO in `ApiRequest.cs` regarding field selection behavior documentation
- Added missing 401 and 403 error response documentation
- Added missing 500 error response documentation for all endpoints

## [1.0.0] - Initial Release

### Available Endpoints

#### Publications
- `GET /api/v1/publications` - List all publications with filtering
- `GET /api/v1/publications/{id}` - Get publication by ID

#### Submissions
- `GET /api/v1/submissions` - List all submissions with filtering
- `GET /api/v1/submissions/{id}` - Get submission by ID

#### Games
- `GET /api/v1/games` - List all games with filtering by system codes
- `GET /api/v1/games/{id}` - Get game by ID

#### Tags
- `GET /api/v1/tags` - List all publication tags
- `GET /api/v1/tags/{id}` - Get tag by ID
- `POST /api/v1/tags` - Create a new tag (requires authentication)
- `PUT /api/v1/tags/{id}` - Update a tag (requires authentication)
- `DELETE /api/v1/tags/{id}` - Delete a tag (requires authentication)

#### Systems
- `GET /api/v1/systems` - List all gaming systems
- `GET /api/v1/systems/{id}` - Get system by ID

#### Events
- `GET /api/v1/events` - List all events
- `GET /api/v1/events/{id}` - Get event by ID

#### Classes
- `GET /api/v1/classes` - List all publication classes

#### Users
- `POST /api/v1/users/authenticate` - Authenticate and receive JWT token

### Features
- JWT Bearer token authentication
- Pagination support with `pageSize` and `currentPage`
- Sorting support with `sort` parameter (ascending/descending)
- Field selection with `fields` parameter
- FluentValidation for request validation
- Comprehensive error handling with ProblemDetails

---

## Breaking Changes Policy

We strive to maintain backward compatibility, but breaking changes may occur between major versions.

### What We Consider Breaking Changes
- Removing an endpoint
- Removing a response field
- Changing response field types
- Changing authentication requirements
- Changing default behavior in a non-backward-compatible way

### What We Don't Consider Breaking Changes
- Adding new endpoints
- Adding new optional request parameters
- Adding new response fields
- Adding new error codes
- Improving error messages
- Performance improvements
- Documentation improvements

## Deprecation Policy

When we need to deprecate an endpoint or feature:
1. We'll announce it in this changelog
2. We'll mark it as deprecated in the API documentation
3. We'll provide a migration path
4. We'll maintain the deprecated feature for at least 6 months
5. We'll provide advance notice before removal

## Support

For questions about API changes or to report issues:
- GitHub Issues: https://github.com/TASVideos/tasvideos/issues
- Contact: https://tasvideos.org/HomePages/Contact
