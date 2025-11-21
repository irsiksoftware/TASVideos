using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TASVideos.Core.Services.HealthChecks;

/// <summary>
/// Health check for system memory availability.
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
	private const long WarningThresholdMB = 512; // Warn if less than 512 MB available
	private const long CriticalThresholdMB = 256; // Critical if less than 256 MB available

	public Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var gcMemoryInfo = GC.GetGCMemoryInfo();
			var totalMemoryMB = gcMemoryInfo.TotalAvailableMemoryBytes / 1024 / 1024;
			var usedMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
			var availableMemoryMB = totalMemoryMB - usedMemoryMB;

			// Get process working set
			var process = System.Diagnostics.Process.GetCurrentProcess();
			var workingSetMB = process.WorkingSet64 / 1024 / 1024;

			var data = new Dictionary<string, object>
			{
				["totalMemoryMB"] = totalMemoryMB,
				["usedMemoryMB"] = usedMemoryMB,
				["availableMemoryMB"] = availableMemoryMB,
				["workingSetMB"] = workingSetMB,
				["gen0Collections"] = GC.CollectionCount(0),
				["gen1Collections"] = GC.CollectionCount(1),
				["gen2Collections"] = GC.CollectionCount(2)
			};

			if (availableMemoryMB < CriticalThresholdMB)
			{
				return Task.FromResult(
					HealthCheckResult.Unhealthy(
						$"Critical: Only {availableMemoryMB} MB of memory available (threshold: {CriticalThresholdMB} MB)",
						data: data));
			}

			if (availableMemoryMB < WarningThresholdMB)
			{
				return Task.FromResult(
					HealthCheckResult.Degraded(
						$"Warning: Only {availableMemoryMB} MB of memory available (threshold: {WarningThresholdMB} MB)",
						data: data));
			}

			return Task.FromResult(
				HealthCheckResult.Healthy(
					$"Memory usage is healthy. Available: {availableMemoryMB} MB, Used: {usedMemoryMB} MB",
					data: data));
		}
		catch (Exception ex)
		{
			return Task.FromResult(
				HealthCheckResult.Degraded(
					$"Memory check failed: {ex.Message}",
					exception: ex));
		}
	}
}
