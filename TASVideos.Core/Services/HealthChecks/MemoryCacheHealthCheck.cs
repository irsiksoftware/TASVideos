using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TASVideos.Core.Services.HealthChecks;

/// <summary>
/// Health check for Memory Cache availability and functionality.
/// </summary>
public class MemoryCacheHealthCheck : IHealthCheck
{
	private readonly IMemoryCache? _memoryCache;

	public MemoryCacheHealthCheck(IServiceProvider serviceProvider)
	{
		// Try to resolve IMemoryCache if available
		_memoryCache = serviceProvider.GetService(typeof(IMemoryCache)) as IMemoryCache;
	}

	public Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		try
		{
			if (_memoryCache == null)
			{
				return Task.FromResult(
					HealthCheckResult.Degraded(
						"Memory cache service is not registered"));
			}

			// Test cache write/read operations
			var testKey = $"health_check_{Guid.NewGuid()}";
			var testValue = DateTime.UtcNow;

			_memoryCache.Set(testKey, testValue, TimeSpan.FromSeconds(1));
			var retrieved = _memoryCache.Get<DateTime>(testKey);

			if (retrieved == testValue)
			{
				_memoryCache.Remove(testKey);
				return Task.FromResult(
					HealthCheckResult.Healthy("Memory cache is functioning correctly"));
			}

			return Task.FromResult(
				HealthCheckResult.Degraded(
					"Memory cache read/write test failed"));
		}
		catch (Exception ex)
		{
			return Task.FromResult(
				HealthCheckResult.Degraded(
					$"Memory cache check failed: {ex.Message}",
					exception: ex));
		}
	}
}
