using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace TASVideos.Benchmarks;

internal class Program
{
	private static void Main(string[] args)
	{
		var config = DefaultConfig.Instance
			.WithOptions(ConfigOptions.DisableOptimizationsValidator);

		var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
			.Run(args, config);
	}
}
