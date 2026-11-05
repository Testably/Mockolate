using System.Linq;
using System.Text.Json;

namespace Build.Tests;

internal static class BenchmarkReportTestData
{
	public static string Measurements(params (string Method, string Parameters, double Mean, double Allocated)[] benchmarks)
		=> JsonSerializer.Serialize(new
		{
			Benchmarks = benchmarks.Select(benchmark => new
			{
				benchmark.Method,
				benchmark.Parameters,
				Statistics = new { benchmark.Mean, },
				Metrics = new[]
				{
					new { Value = 1.0, Descriptor = new { Id = "Gen0Collects", }, },
					new { Value = benchmark.Allocated, Descriptor = new { Id = "Allocated Memory", }, },
				},
			}),
		});

	public static string BaselineData(
		params (string Name, string[] Commits, (string Label, double[] Data)[] Datasets)[] benchmarks)
		=> "window.BENCHMARK_DATA = " + JsonSerializer.Serialize(benchmarks.ToDictionary(
			benchmark => benchmark.Name,
			benchmark => new
			{
				commits = benchmark.Commits.Select(sha => new { sha, }),
				datasets = benchmark.Datasets.Select(dataset => new { label = dataset.Label, data = dataset.Data, }),
			}));
}
