# API Rate Limiting

## Overview

The TASVideos API implements rate limiting to protect against abuse, DoS attacks, resource exhaustion, credential stuffing, and excessive data scraping. Rate limiting is applied based on user authentication tiers and endpoint types.

## Rate Limit Tiers

The API enforces different rate limits based on user authentication status:

### Anonymous Users
- **General Limit**: 100 requests per hour
- **Search Endpoints**: 50 requests per hour (games, publications, submissions)
- **Write Operations**: Not permitted

Anonymous users are identified by their IP address and do not require authentication.

### Authenticated Users
- **General Limit**: 1,000 requests per hour
- **Search Endpoints**: Inherits general limit (1,000 requests/hour)
- **Write Operations**: 200 requests per hour (POST, PUT, DELETE)

Authenticated users must provide a valid JWT token in the `Authorization` header.

### Admin Users
- **All Endpoints**: Unlimited (effectively 999,999 requests/hour)

Admin users are identified by having the `SeeDiagnostics` permission.

## Endpoint-Specific Limits

### Search Endpoints (Anonymous Users)
The following endpoints have more restrictive limits for anonymous users:
- `GET /api/games` - 50 requests/hour
- `GET /api/publications` - 50 requests/hour
- `GET /api/submissions` - 50 requests/hour

### Write Endpoints (Authenticated Users Only)
Write operations are limited to authenticated users:
- `POST /api/*` - 200 requests/hour
- `PUT /api/*` - 200 requests/hour
- `DELETE /api/*` - 200 requests/hour

## Response Headers

When a rate limit is enforced, the following headers are included in the response:

- `X-Rate-Limit-Limit`: The maximum number of requests allowed in the current period
- `X-Rate-Limit-Remaining`: The number of requests remaining in the current period
- `X-Rate-Limit-Reset`: The timestamp when the rate limit will reset

## Rate Limit Exceeded

When a rate limit is exceeded, the API returns:

**HTTP Status Code**: `429 Too Many Requests`

**Response Headers**:
- `Retry-After`: The number of seconds to wait before making another request (default: 3600 seconds / 1 hour)

**Response Body**:
```json
{
  "Title": "Too Many Requests",
  "Status": 429,
  "Message": "API calls quota exceeded! Maximum allowed: <limit> per 1h."
}
```

## Configuration

Rate limiting can be configured in `appsettings.json`:

```json
{
  "ApiRateLimit": {
    "EnableRateLimiting": true,
    "AnonymousRequestsPerHour": 100,
    "AuthenticatedRequestsPerHour": 1000,
    "SearchRequestsPerHour": 50,
    "WriteRequestsPerHour": 200
  }
}
```

### Configuration Options

- `EnableRateLimiting` (bool): Enable or disable rate limiting globally
- `AnonymousRequestsPerHour` (int): General rate limit for anonymous users
- `AuthenticatedRequestsPerHour` (int): General rate limit for authenticated users
- `SearchRequestsPerHour` (int): Rate limit for search endpoints (anonymous users)
- `WriteRequestsPerHour` (int): Rate limit for write operations (authenticated users)

## Best Practices

### For API Consumers

1. **Implement Retry Logic**: When receiving a 429 response, respect the `Retry-After` header
2. **Cache Responses**: Cache API responses to reduce the number of requests
3. **Use Authenticated Requests**: Authenticate to get higher rate limits
4. **Monitor Headers**: Check rate limit headers to track your usage
5. **Implement Backoff**: Use exponential backoff when retrying failed requests

### Example: Handling Rate Limits

```csharp
public async Task<HttpResponseMessage> MakeApiRequestWithRetry(string url)
{
    var response = await _httpClient.GetAsync(url);

    if (response.StatusCode == (HttpStatusCode)429)
    {
        // Check Retry-After header
        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var retryAfter = int.Parse(values.First());
            await Task.Delay(retryAfter * 1000);

            // Retry the request
            response = await _httpClient.GetAsync(url);
        }
    }

    return response;
}
```

## Disabling Rate Limiting

Rate limiting can be disabled in development or testing environments by setting `EnableRateLimiting` to `false` in `appsettings.json`:

```json
{
  "ApiRateLimit": {
    "EnableRateLimiting": false
  }
}
```

## Implementation Details

The rate limiting implementation uses:
- **Library**: AspNetCoreRateLimit (v5.0.0)
- **Storage**: In-memory cache (MemoryCache)
- **Strategy**: AsyncKeyLockProcessingStrategy for thread-safe counter updates
- **Client Identification**: IP address + authentication tier (anonymous/authenticated/admin)

### Custom Components

1. **UserTierRateLimitConfiguration**: Custom rate limit configuration that determines user tier based on authentication and permissions
2. **Client ID Format**: `{tier}:{ip_address}` where tier is `anonymous`, `authenticated`, or `admin`

## Security Considerations

1. **IP Spoofing Protection**: The middleware uses the `X-Real-IP` header when available (configured on reverse proxy)
2. **Distributed Environments**: For multi-server deployments, consider using Redis as the rate limit store instead of in-memory cache
3. **Admin Detection**: Admin users are identified by the `SeeDiagnostics` permission to prevent privilege escalation

## Monitoring

Rate limiting metrics can be monitored through:
- Application logs (rate limit violations are logged)
- OpenTelemetry metrics (if enabled)
- HTTP response headers on each request

## Future Enhancements

Potential improvements to consider:
1. **Redis Storage**: For distributed rate limiting across multiple servers
2. **Custom Rate Limits**: Per-client custom rate limits for approved partners
3. **Dynamic Limits**: Adjust limits based on server load
4. **Rate Limit Tiers**: Additional tiers for power users
5. **Endpoint-Specific Quotas**: More granular control per endpoint
