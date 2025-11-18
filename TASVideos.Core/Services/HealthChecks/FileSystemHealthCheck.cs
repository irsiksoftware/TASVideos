using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TASVideos.Core.Services.HealthChecks;

/// <summary>
/// Health check for file system write access to critical directories.
/// </summary>
public class FileSystemHealthCheck : IHealthCheck
{
	private readonly string[] _criticalPaths =
	[
		"wwwroot",
		"wwwroot/media",
		"wwwroot/media/uploads"
	];

	public Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		var results = new List<string>();
		var hasErrors = false;
		var hasWarnings = false;

		foreach (var relativePath in _criticalPaths)
		{
			try
			{
				var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

				// Check if directory exists
				if (!Directory.Exists(fullPath))
				{
					// Try to create it if it doesn't exist
					try
					{
						Directory.CreateDirectory(fullPath);
						results.Add($"Created missing directory: {relativePath}");
						hasWarnings = true;
					}
					catch (Exception ex)
					{
						results.Add($"Failed to create directory {relativePath}: {ex.Message}");
						hasErrors = true;
						continue;
					}
				}

				// Test write access
				var testFile = Path.Combine(fullPath, $".health_check_{Guid.NewGuid()}.tmp");
				try
				{
					File.WriteAllText(testFile, "health check test");
					File.Delete(testFile);
					results.Add($"✓ {relativePath}: Writable");
				}
				catch (UnauthorizedAccessException)
				{
					results.Add($"✗ {relativePath}: No write access");
					hasErrors = true;
				}
				catch (Exception ex)
				{
					results.Add($"✗ {relativePath}: {ex.Message}");
					hasErrors = true;
				}
			}
			catch (Exception ex)
			{
				results.Add($"✗ {relativePath}: Unexpected error - {ex.Message}");
				hasErrors = true;
			}
		}

		var data = new Dictionary<string, object>
		{
			["checks"] = results,
			["currentDirectory"] = Directory.GetCurrentDirectory()
		};

		if (hasErrors)
		{
			return Task.FromResult(
				HealthCheckResult.Degraded(
					"File system has write access issues",
					data: data));
		}

		if (hasWarnings)
		{
			return Task.FromResult(
				HealthCheckResult.Healthy(
					"File system is accessible (created missing directories)",
					data: data));
		}

		return Task.FromResult(
			HealthCheckResult.Healthy(
				"File system is fully accessible",
				data: data));
	}
}
