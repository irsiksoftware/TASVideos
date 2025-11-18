# TASVideos API Documentation

## Overview

The TASVideos API provides programmatic access to TASVideos.org content including publications, submissions, games, tags, and more.

**Base URL (Production):** `https://tasvideos.org/api/v1`
**Base URL (Local Development):** `http://localhost:5000/api/v1`

**Current Version:** v1
**API Specification:** OpenAPI 3.0
**Swagger UI:** Available at `/api` (e.g., `https://tasvideos.org/api`)

## Quick Start

### 1. Browse the API

Visit the Swagger UI at https://tasvideos.org/api to explore all available endpoints interactively.

### 2. Make Your First Request

```bash
# Get the first 10 publications
curl "https://tasvideos.org/api/v1/publications?pageSize=10&currentPage=1"
```

### 3. Authenticate (for write operations)

```bash
# Authenticate and get a token
curl -X POST "https://tasvideos.org/api/v1/users/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"your-username","password":"your-password"}'

# Use the token in subsequent requests
curl "https://tasvideos.org/api/v1/tags" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Authentication

### Public Endpoints

Most GET endpoints are publicly accessible and do not require authentication:
- GET `/publications`
- GET `/submissions`
- GET `/games`
- GET `/tags`
- And more...

### Protected Endpoints

Write operations (POST, PUT, DELETE) require JWT Bearer authentication:
- POST `/tags` - Requires TagMaintenance permission
- PUT `/tags/{id}` - Requires TagMaintenance permission
- DELETE `/tags/{id}` - Requires TagMaintenance permission

### How to Authenticate

1. **Get a Token**
   ```bash
   POST /api/v1/users/authenticate
   Content-Type: application/json

   {
     "username": "your-username",
     "password": "your-password"
   }
   ```

2. **Use the Token**
   Include the token in the `Authorization` header:
   ```
   Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```

3. **Token Expiration**
   Tokens expire after a configured period. Re-authenticate to get a new token.

## Core Concepts

### Pagination

All list endpoints support pagination using these parameters:

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `pageSize` | int | 100 | 100 | Number of records per page |
| `currentPage` | int | 1 | - | Page number (1-based) |

**Example:**
```bash
GET /api/v1/publications?pageSize=25&currentPage=2
```

### Sorting

Use the `sort` parameter to order results:

- **Ascending:** Prefix with `+` or omit prefix
- **Descending:** Prefix with `-`
- **Multiple fields:** Comma-separated

**Examples:**
```bash
# Sort by createTimestamp descending
GET /api/v1/publications?sort=-createTimestamp

# Sort by class ascending, then title descending
GET /api/v1/publications?sort=+class,-title
```

### Field Selection

**⚠️ Important: Field Selection Behavior**

Use the `fields` parameter to return only specific fields:

```bash
GET /api/v1/publications?fields=id,title,class
```

**Critical Consideration:**

When using field selection, **the API applies deduplication** to the results. This means:

- The actual returned count may be **less than** `pageSize`
- Duplicate records (after field selection) are removed
- This is intentional to avoid returning redundant data

**Example:**
```bash
# Request 100 publications, but only return the 'class' field
GET /api/v1/publications?pageSize=100&fields=class

# If only 5 unique classes exist, you'll receive 5 records, not 100
```

**Recommendation:**
- If you need a guaranteed number of records, don't use field selection
- Or request a larger `pageSize` to account for deduplication

## Available Endpoints

### Publications

Publications are completed TAS movies that have been accepted and published.

#### Get All Publications
```http
GET /api/v1/publications
```

**Query Parameters:**
- `pageSize` (int): Number of records to return
- `currentPage` (int): Page number
- `sort` (string): Sort order
- `fields` (string): Fields to return

**Response:** Array of publication objects

#### Get Publication by ID
```http
GET /api/v1/publications/{id}
```

**Response:** Single publication object

---

### Submissions

Submissions are TAS movies submitted for review but not yet published.

#### Get All Submissions
```http
GET /api/v1/submissions
```

#### Get Submission by ID
```http
GET /api/v1/submissions/{id}
```

---

### Games

Games represent video game titles available for TAS creation.

#### Get All Games
```http
GET /api/v1/games
```

**Additional Query Parameters:**
- `systemCodes` (string): Filter by system codes (comma-separated, e.g., "NES,SNES")

#### Get Game by ID
```http
GET /api/v1/games/{id}
```

---

### Tags

Tags are labels used to categorize publications.

#### Get All Tags
```http
GET /api/v1/tags
```

#### Get Tag by ID
```http
GET /api/v1/tags/{id}
```

#### Create Tag (Authenticated)
```http
POST /api/v1/tags
Authorization: Bearer {token}
Content-Type: application/json

{
  "code": "example-tag",
  "displayName": "Example Tag"
}
```

**Requires:** TagMaintenance permission

#### Update Tag (Authenticated)
```http
PUT /api/v1/tags/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "code": "updated-tag",
  "displayName": "Updated Tag Name"
}
```

**Requires:** TagMaintenance permission

#### Delete Tag (Authenticated)
```http
DELETE /api/v1/tags/{id}
Authorization: Bearer {token}
```

**Requires:** TagMaintenance permission

---

## Error Responses

The API uses standard HTTP status codes and returns error details in JSON format.

### Error Response Schema

```json
{
  "title": "Error title",
  "status": 400,
  "message": "Additional error details (optional)"
}
```

### Common Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Request succeeded |
| 201 | Created | Resource created successfully |
| 400 | Bad Request | Invalid request parameters |
| 401 | Unauthorized | Authentication required |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource not found |
| 409 | Conflict | Resource conflict (e.g., duplicate) |
| 500 | Internal Server Error | Server error |

### Validation Errors (400)

Validation errors include detailed field-level information:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "PageSize": ["PageSize must be between 1 and 100"],
    "Sort": ["Invalid sort parameter"]
  }
}
```

## Rate Limiting

API requests may be rate-limited. Please implement appropriate backoff strategies in your applications.

When rate-limited, you'll receive a `429 Too Many Requests` response.

## Code Examples

### C# (.NET)

```csharp
using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("https://tasvideos.org/api/v1/") };

// Get publications
var publications = await client.GetFromJsonAsync<List<Publication>>("publications?pageSize=10");

// Authenticate
var authRequest = new { username = "user", password = "pass" };
var response = await client.PostAsJsonAsync("users/authenticate", authRequest);
var token = await response.Content.ReadAsStringAsync();

// Use authenticated endpoint
client.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
var tags = await client.GetFromJsonAsync<List<Tag>>("tags");
```

### JavaScript (Fetch API)

```javascript
// Get publications
const response = await fetch('https://tasvideos.org/api/v1/publications?pageSize=10');
const publications = await response.json();

// Authenticate
const authResponse = await fetch('https://tasvideos.org/api/v1/users/authenticate', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ username: 'user', password: 'pass' })
});
const token = await authResponse.text();

// Use authenticated endpoint
const tagsResponse = await fetch('https://tasvideos.org/api/v1/tags', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const tags = await tagsResponse.json();
```

### Python (requests)

```python
import requests

BASE_URL = "https://tasvideos.org/api/v1"

# Get publications
response = requests.get(f"{BASE_URL}/publications", params={"pageSize": 10})
publications = response.json()

# Authenticate
auth_response = requests.post(
    f"{BASE_URL}/users/authenticate",
    json={"username": "user", "password": "pass"}
)
token = auth_response.text

# Use authenticated endpoint
headers = {"Authorization": f"Bearer {token}"}
tags_response = requests.get(f"{BASE_URL}/tags", headers=headers)
tags = tags_response.json()
```

### cURL

```bash
# Get publications
curl "https://tasvideos.org/api/v1/publications?pageSize=10"

# Authenticate
TOKEN=$(curl -X POST "https://tasvideos.org/api/v1/users/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"user","password":"pass"}')

# Use authenticated endpoint
curl "https://tasvideos.org/api/v1/tags" \
  -H "Authorization: Bearer $TOKEN"
```

## Postman Collection

Import the Postman collection for easy API exploration:

1. Download `TASVideos-API.postman_collection.json`
2. Import into Postman
3. Set the `baseUrl` variable to your target environment
4. Use the "Authenticate User" request to get a token (automatically saved)

## Best Practices

1. **Cache Responses:** Cache GET responses when appropriate to reduce API calls
2. **Use Field Selection Wisely:** Be aware of deduplication when using `fields` parameter
3. **Handle Errors Gracefully:** Check status codes and parse error messages
4. **Respect Rate Limits:** Implement exponential backoff for retries
5. **Keep Tokens Secure:** Never expose tokens in client-side code or version control
6. **Use HTTPS:** Always use HTTPS in production

## Support & Feedback

- **Issues & Bugs:** Report at https://github.com/TASVideos/tasvideos/issues
- **API Questions:** Visit the TASVideos forums
- **Contact:** https://tasvideos.org/HomePages/Contact

## License

API content is licensed under Creative Commons Attribution 2.0.

See: https://creativecommons.org/licenses/by/2.0/

## Changelog

See [API-CHANGELOG.md](./API-CHANGELOG.md) for version history and breaking changes.
