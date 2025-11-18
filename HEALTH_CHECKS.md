# TASVideos Health Checks Documentation

## Overview

This document describes the comprehensive health check system implemented for the TASVideos application. Health checks provide visibility into the application's operational status and are essential for monitoring, alerting, and orchestration (Kubernetes, Docker Swarm, etc.).

## Health Check Endpoints

### 1. `/health` - Overall Health (Public)

**Purpose**: Provides a quick overview of the overall application health status.

**Access**: Public (no authentication required)

**Response Format**:
```json
{
  "status": "Healthy",
  "timestamp": "2025-11-18T10:30:00.000Z",
  "totalDuration": 150.5
}
```

**Status Codes**:
- `200 OK` - Application is healthy
- `503 Service Unavailable` - Application is unhealthy

**Use Cases**:
- Load balancer health checks
- Uptime monitoring services
- Quick status verification

---

### 2. `/health/ready` - Readiness Check (Kubernetes)

**Purpose**: Indicates whether the application is ready to accept traffic. Checks critical dependencies like the database.

**Access**: Public (no authentication required)

**What it Checks**:
- PostgreSQL database connectivity (critical)
- Any service tagged as "critical" or "db"

**Response Format**:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": 45.2
    }
  ],
  "timestamp": "2025-11-18T10:30:00.000Z"
}
```

**Use Cases**:
- Kubernetes readiness probes
- Container orchestration readiness checks
- Determining if the pod should receive traffic

**Kubernetes Configuration Example**:
```yaml
readinessProbe:
  httpGet:
    path: /health/ready
    port: 80
  initialDelaySeconds: 10
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3
```

---

### 3. `/health/live` - Liveness Check (Kubernetes)

**Purpose**: Indicates whether the application process is alive and responsive. Does NOT check external dependencies.

**Access**: Public (no authentication required)

**What it Checks**:
- Memory availability
- File system accessibility
- Services tagged as "memory" or "filesystem"

**Response Format**:
```json
{
  "status": "Healthy",
  "timestamp": "2025-11-18T10:30:00.000Z"
}
```

**Use Cases**:
- Kubernetes liveness probes
- Detecting application deadlocks or hangs
- Automatic pod restarts when unhealthy

**Kubernetes Configuration Example**:
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 30
  timeoutSeconds: 5
  failureThreshold: 3
```

---

### 4. `/health/detailed` - Detailed Status (Admin Only)

**Purpose**: Provides comprehensive health information for all registered health checks with detailed diagnostics.

**Access**: **Requires authentication** - User must have `SeeDiagnostics` permission (PermissionTo.SeeDiagnostics)

**What it Checks**: All health checks with full details

**Response Format**:
```json
{
  "status": "Healthy",
  "totalDuration": 250.8,
  "timestamp": "2025-11-18T10:30:00.000Z",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Indicates that the application is healthy.",
      "duration": 45.2,
      "tags": ["db", "sql", "postgresql", "critical"],
      "exception": null,
      "data": {}
    },
    {
      "name": "cache_redis",
      "status": "Healthy",
      "description": null,
      "duration": 15.3,
      "tags": ["cache", "redis"],
      "exception": null,
      "data": {}
    },
    {
      "name": "memory",
      "status": "Healthy",
      "description": "Memory usage is healthy. Available: 2048 MB, Used: 512 MB",
      "duration": 2.1,
      "tags": ["system", "memory"],
      "exception": null,
      "data": {
        "totalMemoryMB": 4096,
        "usedMemoryMB": 512,
        "availableMemoryMB": 2048,
        "workingSetMB": 450,
        "gen0Collections": 10,
        "gen1Collections": 5,
        "gen2Collections": 2
      }
    }
  ]
}
```

**Use Cases**:
- Debugging operational issues
- Monitoring dashboards
- Alerting system integration
- Performance analysis

---

## Health Checks Implemented

### 1. Database (PostgreSQL)

**Name**: `database`

**Tags**: `db`, `sql`, `postgresql`, `critical`

**Failure Status**: `Unhealthy`

**What it Checks**:
- PostgreSQL database connectivity
- Ability to execute queries

**Configuration**: Uses connection string from `AppSettings.ConnectionStrings.PostgresConnection`

**Why Critical**: Without database access, the application cannot function

---

### 2. Cache (Redis or Memory)

#### Redis Cache

**Name**: `cache_redis`

**Tags**: `cache`, `redis`

**Failure Status**: `Degraded`

**What it Checks**:
- Redis server connectivity
- Ability to execute Redis commands

**Configuration**: Only registered when `AppSettings.CacheSettings.CacheType = "Redis"`

**Why Degraded**: Application can function without cache, but performance will be affected

#### Memory Cache

**Name**: `cache_memory`

**Tags**: `cache`, `memory`

**Failure Status**: `Healthy`

**What it Checks**:
- Memory cache service availability
- Read/write operations to cache

**Configuration**: Only registered when `AppSettings.CacheSettings.CacheType = "Memory"`

---

### 3. External Services

#### Discord

**Name**: `external_discord`

**Tags**: `external`, `discord`

**Failure Status**: `Degraded`

**What it Checks**:
- Discord API base URL accessibility
- HTTP connectivity to `https://discord.com/api/v10/`

**Configuration**: Only registered when `AppSettings.Discord.IsEnabled() = true`

**Timeout**: 5 seconds

#### YouTube

**Name**: `external_youtube`

**Tags**: `external`, `youtube`

**Failure Status**: `Degraded`

**What it Checks**:
- YouTube API base URL accessibility
- HTTP connectivity to `https://www.googleapis.com/youtube/v3/`

**Configuration**: Only registered when `AppSettings.YouTube.IsEnabled() = true`

**Timeout**: 5 seconds

#### Bluesky

**Name**: `external_bluesky`

**Tags**: `external`, `bluesky`

**Failure Status**: `Degraded`

**What it Checks**:
- Bluesky API base URL accessibility
- HTTP connectivity to `https://bsky.social/xrpc/`

**Configuration**: Only registered when `AppSettings.Bluesky.IsEnabled() = true`

**Timeout**: 5 seconds

---

### 4. File System

**Name**: `filesystem`

**Tags**: `system`, `filesystem`

**Failure Status**: `Degraded`

**What it Checks**:
- Write access to critical directories:
  - `wwwroot`
  - `wwwroot/media`
  - `wwwroot/media/uploads`
- Creates missing directories if possible
- Tests write operations by creating temporary test files

**Response Data**:
```json
{
  "checks": [
    "✓ wwwroot: Writable",
    "✓ wwwroot/media: Writable",
    "✓ wwwroot/media/uploads: Writable"
  ],
  "currentDirectory": "/app"
}
```

**Location**: `TASVideos.Core/Services/HealthChecks/FileSystemHealthCheck.cs`

---

### 5. Memory

**Name**: `memory`

**Tags**: `system`, `memory`

**Failure Status**: `Degraded` or `Unhealthy` based on thresholds

**What it Checks**:
- Available system memory
- Process working set
- Garbage collection statistics

**Thresholds**:
- **Unhealthy**: < 256 MB available
- **Degraded**: < 512 MB available
- **Healthy**: ≥ 512 MB available

**Response Data**:
```json
{
  "totalMemoryMB": 4096,
  "usedMemoryMB": 512,
  "availableMemoryMB": 2048,
  "workingSetMB": 450,
  "gen0Collections": 10,
  "gen1Collections": 5,
  "gen2Collections": 2
}
```

**Location**: `TASVideos.Core/Services/HealthChecks/MemoryHealthCheck.cs`

---

## Health Status Values

Health checks can return one of three statuses:

1. **Healthy** - Service is functioning normally
2. **Degraded** - Service is functional but experiencing issues or operating below optimal performance
3. **Unhealthy** - Service is not functioning and requires immediate attention

### Overall Status Determination

The overall application health status is determined by the worst status among all checks:

- If any check is `Unhealthy`, the overall status is `Unhealthy` (HTTP 503)
- If any check is `Degraded` (and none are Unhealthy), the overall status is `Degraded` (HTTP 503)
- If all checks are `Healthy`, the overall status is `Healthy` (HTTP 200)

---

## Integration with Monitoring

### Prometheus/OpenTelemetry

The application already has Prometheus metrics enabled via OpenTelemetry. Health check metrics can be correlated with:

- Request metrics
- Database connection pool metrics
- Cache hit/miss rates

**Metrics Endpoint**: `/Metrics` (requires `SeeDiagnostics` permission)

### Alerting

Recommended alerts based on health checks:

1. **Critical Alert**: `/health/ready` returns non-200 for > 2 minutes
   - Indicates database or critical service failure
   - Action: Page on-call engineer

2. **Warning Alert**: `/health/detailed` shows degraded external services
   - Indicates external API issues
   - Action: Create ticket for investigation

3. **Warning Alert**: Memory health check shows degraded status
   - Indicates memory pressure
   - Action: Monitor for memory leaks, consider scaling

### Status Page Integration

The `/health` endpoint can be used with status page services like:

- StatusCake
- Pingdom
- UptimeRobot
- Custom status pages

Example StatusCake configuration:
```
URL: https://tasvideos.org/health
Check Rate: 1 minute
Expected Status: 200
Expected Content: "Healthy"
```

---

## Docker/Container Integration

### Docker Compose

```yaml
version: '3.8'
services:
  tasvideos-web:
    image: tasvideos:latest
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

### Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tasvideos-web
spec:
  replicas: 3
  template:
    spec:
      containers:
      - name: web
        image: tasvideos:latest
        ports:
        - containerPort: 80
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 30
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
```

---

## CI/CD Integration

Health checks can be integrated into the CI/CD pipeline:

### Post-Deployment Verification

```bash
#!/bin/bash
# Wait for application to be ready
MAX_ATTEMPTS=30
ATTEMPT=0

while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://tasvideos.org/health/ready)

  if [ $STATUS -eq 200 ]; then
    echo "Application is ready!"
    exit 0
  fi

  echo "Waiting for application to be ready... (Attempt $((ATTEMPT+1))/$MAX_ATTEMPTS)"
  sleep 10
  ATTEMPT=$((ATTEMPT+1))
done

echo "Application failed to become ready"
exit 1
```

### GitHub Actions

Add to `.github/workflows/dotnet-core.yml`:

```yaml
- name: Health Check
  run: |
    # Wait for application to start
    sleep 30

    # Check health endpoint
    response=$(curl -s http://localhost:5000/health)
    status=$(echo $response | jq -r '.status')

    if [ "$status" != "Healthy" ]; then
      echo "Health check failed: $response"
      exit 1
    fi

    echo "Health check passed"
```

---

## Custom Health Checks

To add a new health check:

### 1. Create Health Check Class

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TASVideos.Core.Services.HealthChecks;

public class MyCustomHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform your health check logic here
            bool isHealthy = PerformCheck();

            if (isHealthy)
            {
                return Task.FromResult(
                    HealthCheckResult.Healthy("Check passed"));
            }

            return Task.FromResult(
                HealthCheckResult.Degraded("Check failed"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "Check threw exception",
                    exception: ex));
        }
    }

    private bool PerformCheck()
    {
        // Your check logic
        return true;
    }
}
```

### 2. Register in ServiceCollectionExtensions.cs

```csharp
healthChecksBuilder.AddCheck<MyCustomHealthCheck>(
    "my_custom_check",
    failureStatus: HealthStatus.Degraded,
    tags: ["custom", "my-service"]);
```

---

## Troubleshooting

### Health Check Always Returns Unhealthy

1. Check application logs for exceptions
2. Use `/health/detailed` endpoint to see which specific check is failing
3. Verify configuration settings (connection strings, API keys, etc.)
4. Check network connectivity to external services

### Health Check Timeouts

1. Increase timeout in health check configuration
2. Optimize slow health checks
3. Consider making expensive checks run less frequently

### Authorization Issues on /health/detailed

1. Verify user has `SeeDiagnostics` permission
2. Check authentication token is valid
3. Ensure user is logged in with appropriate role

---

## File Locations

| Component | File Path |
|-----------|-----------|
| Health Check Registration | `TASVideos.Core/ServiceCollectionExtensions.cs` |
| Health Check Endpoints | `tasvideos/Extensions/ApplicationBuilderExtensions.cs` |
| Memory Cache Health Check | `TASVideos.Core/Services/HealthChecks/MemoryCacheHealthCheck.cs` |
| File System Health Check | `TASVideos.Core/Services/HealthChecks/FileSystemHealthCheck.cs` |
| Memory Health Check | `TASVideos.Core/Services/HealthChecks/MemoryHealthCheck.cs` |
| Package References | `TASVideos.Core/TASVideos.Core.csproj` |
| Package Versions | `Directory.Packages.props` |

---

## Dependencies Added

- `AspNetCore.HealthChecks.NpgSql` (8.0.1) - PostgreSQL health checks
- `AspNetCore.HealthChecks.Redis` (8.0.1) - Redis health checks
- `AspNetCore.HealthChecks.System` (8.0.1) - System health checks (memory, disk)
- `AspNetCore.HealthChecks.Uris` (8.0.1) - HTTP/HTTPS endpoint health checks
- `Microsoft.Extensions.Diagnostics.HealthChecks` (8.0.0) - Core health check framework

---

## Security Considerations

1. **Public Endpoints**: `/health`, `/health/ready`, and `/health/live` are intentionally public for monitoring and orchestration
2. **Sensitive Information**: The `/health/detailed` endpoint requires authentication to prevent information disclosure
3. **Rate Limiting**: Consider adding rate limiting to health check endpoints to prevent abuse
4. **Network Security**: Use HTTPS in production to encrypt health check responses

---

## Performance Considerations

1. Health checks run synchronously when the endpoint is called
2. External service checks have a 5-second timeout to prevent hanging
3. Database health check performs a simple connectivity test
4. Memory and file system checks are lightweight
5. Consider the frequency of health check calls when setting up monitoring

---

## Future Enhancements

Potential improvements to consider:

1. **Caching**: Cache health check results for a short period (5-10 seconds) to reduce load
2. **Startup Checks**: Add dedicated startup health checks for one-time validations
3. **Custom Metrics**: Integrate health check results with Prometheus metrics
4. **Historical Data**: Track health check trends over time
5. **Notification Integration**: Direct integration with PagerDuty, Slack, etc.
6. **Dependency Graph**: Visualize health check dependencies
7. **Performance Counters**: Add more detailed performance metrics to health checks

---

## Support

For issues or questions regarding health checks:

1. Check application logs in `/var/log/tasvideos/` or configured log location
2. Review this documentation
3. Check GitHub issues: https://github.com/TASVideos/tasvideos/issues
4. Contact the development team

---

**Last Updated**: 2025-11-18
**Version**: 1.0
**Author**: TASVideos Development Team
