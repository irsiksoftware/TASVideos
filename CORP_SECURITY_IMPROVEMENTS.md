# Cross-Origin-Resource-Policy Security Improvements

## Overview
This document describes the security improvements made to the Cross-Origin-Resource-Policy (CORP) implementation in TASVideos.

## Problem Statement
Previously, the application set `Cross-Origin-Resource-Policy: cross-origin` for all responses, which posed a security risk:
- Authenticated content could be accessed cross-origin
- Sensitive user data could potentially be leaked through cross-origin requests
- No differentiation between public and private resources

## Solution
Implemented conditional CORP headers based on authentication status and content type:

### Policy Matrix

| Condition | CORP Value | Rationale |
|-----------|-----------|-----------|
| **Authenticated Users** | `same-origin` | Maximum protection for any content viewed by logged-in users. Prevents cross-origin access to authenticated sessions. |
| **Static Assets** (unauthenticated) | `cross-origin` | Public assets (images, JS, CSS, fonts) can be legitimately embedded elsewhere. These are intentionally public resources. |
| **Dynamic Content** (unauthenticated) | `same-site` | Public pages get moderate protection. Allows same-site embedding but blocks cross-origin access. |

### Static Asset Detection
The following file types are classified as static assets:
- **Scripts & Styles**: .js, .css, .map
- **Images**: .jpg, .jpeg, .png, .gif, .svg, .ico, .webp, .bmp
- **Fonts**: .woff, .woff2, .ttf, .eot, .otf
- **Data Files**: .json, .xml, .txt
- **Media**: .mp4, .webm, .ogg, .mp3, .wav
- **Documents**: .pdf, .zip, .tar, .gz

## Security Benefits

1. **Protection of Authenticated Content**: Any resource accessed by an authenticated user is protected with `same-origin`, preventing cross-origin attacks.

2. **Granular Control**: Different resource types get appropriate protection levels based on their sensitivity.

3. **Backward Compatibility**: Public static assets remain accessible cross-origin, maintaining compatibility with existing integrations.

4. **Defense in Depth**: Works alongside other security headers (CSP, COOP, etc.) to provide layered security.

## Implementation Details

### Location
`tasvideos/Extensions/ApplicationBuilderExtensions.cs`

### Key Methods
- `GetCrossOriginResourcePolicy(HttpContext)`: Determines appropriate CORP value based on context
- `IsStaticAsset(string)`: Identifies static assets by file extension

### Test Coverage
Comprehensive test suite in `tests/TASVideos.RazorPages.Tests/Extensions/ApplicationBuilderExtensionsTests.cs`:
- Authenticated user scenarios
- Static asset detection (various file types)
- Dynamic content handling
- Edge cases (empty/null paths, case sensitivity)

## Migration Notes

### Breaking Changes
None expected. The change is backwards compatible for public resources.

### Behavioral Changes
- Authenticated users: Resources now protected with `same-origin` (more restrictive)
- Static assets: Remain `cross-origin` (no change in practice)
- Public pages: Now `same-site` instead of `cross-origin` (slight restriction)

## Future Considerations

1. **Content-Type Based Detection**: Could enhance static asset detection by checking response Content-Type header in addition to file extension.

2. **Path-Based Rules**: Could add specific path patterns for special cases (e.g., API endpoints, webhooks).

3. **Configuration**: Could make static asset extensions configurable if needed.

## References
- [MDN: Cross-Origin-Resource-Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Cross-Origin-Resource-Policy)
- [OWASP Secure Headers Project](https://owasp.org/www-project-secure-headers/)
