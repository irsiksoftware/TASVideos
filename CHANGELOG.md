# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Auto-generate movie filename based on publication ID ([#21](https://github.com/TASVideos/tasvideos/pull/21))
- Event entries system for marathon/showcase runs ([#25](https://github.com/TASVideos/tasvideos/pull/25))
- UI for adding/removing Forum Categories ([#24](https://github.com/TASVideos/tasvideos/pull/24), fixes [#208](https://github.com/TASVideos/tasvideos/issues/208))
- PMToGroup property to enable private messaging to role groups ([#23](https://github.com/TASVideos/tasvideos/pull/23))
- Publication history section to movie pages ([#22](https://github.com/TASVideos/tasvideos/pull/22))
- Implementation for issue #374 ([#20](https://github.com/TASVideos/tasvideos/pull/20))
- Orchestrator cache validation service ([#19](https://github.com/TASVideos/tasvideos/pull/19))
- Discord webhook notifications in GitHub Actions workflow ([#4](https://github.com/TASVideos/tasvideos/pull/4))
- TASVideos project structure and initial implementation

### Changed
- Refactored movie file binary data into centralized Files table ([#12](https://github.com/TASVideos/tasvideos/pull/12))

### Deprecated

### Removed

### Fixed
- Discord notifications by disabling Initialize containers build step ([#7](https://github.com/TASVideos/tasvideos/pull/7))

### Security

## [0.1.0] - 2025-10-07

### Added
- Initial project setup and repository structure
- Basic TASVideos website foundation

---

## How to Update This Changelog

When contributing to this project, please update the `[Unreleased]` section with your changes:

1. **Added** - for new features
2. **Changed** - for changes in existing functionality
3. **Deprecated** - for soon-to-be removed features
4. **Removed** - for now removed features
5. **Fixed** - for any bug fixes
6. **Security** - in case of vulnerabilities

Reference the pull request or issue number when applicable (e.g., `[#123]`).

When a new version is released, move items from `[Unreleased]` to a new version section with the release date.

[Unreleased]: https://github.com/TASVideos/tasvideos/compare/main...HEAD
[0.1.0]: https://github.com/TASVideos/tasvideos/releases/tag/v0.1.0
