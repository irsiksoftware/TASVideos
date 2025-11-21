using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace TASVideos.Benchmarks;

/// <summary>
/// Custom BenchmarkDotNet configuration for TASVideos performance tracking.
/// Configures memory diagnostics, export formats, and job settings.
/// </summary>
public class TASVideosBenchmarkConfig : ManualConfig
{
	public TASVideosBenchmarkConfig()
	{
		// Add default loggers
		AddLogger(ConsoleLogger.Default);

		// Add memory diagnoser to track allocations
		AddDiagnoser(MemoryDiagnoser.Default);

		// Export results in multiple formats for tracking
		AddExporter(MarkdownExporter.GitHub);
		AddExporter(JsonExporter.Full);
		AddExporter(HtmlExporter.Default);

		// Configure job for consistent results
		AddJob(Job.Default
			.WithId("TASVideos.Performance")
			.WithWarmupCount(3)      // 3 warmup iterations
			.WithIterationCount(5)   // 5 measured iterations
			.WithInvocationCount(1)  // Auto-detect invocations per iteration
			.WithUnrollFactor(1));   // No loop unrolling

		// Disable optimizations validator for benchmarks that use reflection
		WithOptions(ConfigOptions.DisableOptimizationsValidator);
	}
}
