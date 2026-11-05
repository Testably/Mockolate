using System.Linq;

namespace Build.Tests;

public sealed class BuildBodyTests
{
	[Test]
	public async Task BuildBody_ShouldDropConfiguredColumnsFromHeaderAndDataRows()
	{
		string[] withGen2 =
		[
			"| Method | Mean | Gen2 | Allocated |",
			"|--------|-----:|-----:|----------:|",
			"| Foo_Mockolate | 100 ns | 0.1 | 1 KB |",
			"| Foo_Imposter  | 200 ns | 0.2 | 2 KB |",
		];

		string body = BenchmarkReport.BuildBody([CreateFile(withGen2),], null, _columnsToRemove);

		await That(body).DoesNotContain("Gen2");
		await That(body).DoesNotContain("0.1");
		await That(body).DoesNotContain("0.2");
	}

	[Test]
	public async Task BuildBody_WhenBdnPreBoldsTheBaselineRow_ShouldNotEmitDoubleBoldOrBreakPrefix()
	{
		string[] report =
		[
			"| Method | N | Mean | Allocated |",
			"|--------|---|-----:|----------:|",
			"| **Indexer_Mockolate** | **1** | **923 ns** | **3.81 KB** |",
			"| Indexer_Moq | 1 | 212 ns | 20.37 KB |",
			"| Indexer_Imposter | 1 | 947 ns | 5.16 KB |",
		];

		string body = BenchmarkReport.BuildBody([CreateFile(report),], null, _columnsToRemove);

		await That(body).DoesNotContain("****");
		string[] lines = body.Split(Environment.NewLine);
		await That(lines).Contains("| Indexer | N | Mean | Allocated |");
		await That(lines).Contains("| **Mockolate** | **1** | **923 ns** | **3.81 KB** |");
		await That(lines).Contains("| Moq | 1 | 212 ns | 20.37 KB |");
	}

	[Test]
	public async Task BuildBody_WhenTableHasEmptyRowSeparatingParameterGroups_ShouldEmitEmptyRowMatchingHeaderColumnCount()
	{
		string body = BenchmarkReport.BuildBody([CreateFile(_groupedReportLines),], null, _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		int firstGroupLast = Array.IndexOf(lines, "| Moq | 1 | 200 ns | 2 KB |");
		int secondGroupFirst = Array.IndexOf(lines, "| **Mockolate** | **10** | **1000 ns** | **10 KB** |");
		await That(firstGroupLast).IsGreaterThan(-1);
		await That(secondGroupFirst).IsGreaterThan(firstGroupLast);
		await That(lines[firstGroupLast + 1]).IsEqualTo("|  |  |  |  |");
	}

	[Test]
	public async Task BuildBody_WhenTableHasEmptyRowSeparatingParameterGroups_ShouldNotEmitBareSeparator()
	{
		string body = BenchmarkReport.BuildBody([CreateFile(_groupedReportLines),], null, _columnsToRemove);

		await That(body).DoesNotContain($"{Environment.NewLine}|{Environment.NewLine}");
	}

	[Test]
	public async Task BuildBody_WhenTableHasEmptyRowSeparatingParameterGroups_ShouldStillStripCommonPrefix()
	{
		string body = BenchmarkReport.BuildBody([CreateFile(_groupedReportLines),], null, _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		await That(lines).Contains("| Indexer | N | Mean | Allocated |");
		await That(lines).Contains("| **Mockolate** | **1** | **100 ns** | **1 KB** |");
		await That(lines).Contains("| **Mockolate** | **10** | **1000 ns** | **10 KB** |");
		await That(lines).Contains("| Moq | 1 | 200 ns | 2 KB |");
		await That(lines).Contains("| Moq | 10 | 2000 ns | 20 KB |");
	}

	[Test]
	public async Task BuildBody_WithoutBaseline_ShouldRenderTheRawResultsCollapsedBelowTheSectionHeading()
	{
		string body = BenchmarkReport.BuildBody([CreateFile(_reportLines),], null, _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		await That(lines[0]).IsEqualTo("## :rocket: Benchmark Results");
		await That(lines[1]).IsEqualTo("_There are no results of `main` to compare to._");
		await That(lines).Contains("### FooBenchmarks");
		int summary = Array.IndexOf(lines, "<summary>Raw results</summary>");
		int table = Array.IndexOf(lines, "| Foo | Mean | Allocated |");
		int end = Array.IndexOf(lines, "</details>");
		await That(summary).IsGreaterThan(-1);
		await That(table).IsGreaterThan(Array.IndexOf(lines, "</pre>"));
		await That(end).IsGreaterThan(table).Because("the raw table belongs inside the collapsed details");
		await That(lines).Contains("<pre>");
		await That(lines).Contains("preamble line");
		await That(lines).Contains("| **Mockolate** | **100 ns** | **1 KB** |");
		await That(lines).Contains("| Imposter | 200 ns | 2 KB |");
	}

	[Test]
	public async Task BuildBody_WhenNothingChanged_ShouldConfirmItAndListAllBenchmarksCollapsed()
	{
		BenchmarkReportFile file = CreateFile(_reportLines,
			("Foo_Mockolate", "", 100, 64),
			("Foo_Imposter", "", 1000, 512),
			("Foo_Moq", "", 2000, 1024));
		BenchmarkBaseline baseline = CreateBaseline(("Foo", 100, 64, 0.1));

		string body = BenchmarkReport.BuildBody([file,], baseline, _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		await That(lines[1]).StartsWith("Compared to `main` @ `abcdef12`:");
		await That(lines).Contains("✅ No changes in memory or relative performance (1 benchmarks)");
		await That(lines).Contains("<summary>All benchmarks compared to main</summary>");
		await That(lines).Contains("| Benchmark | Time | Memory | vs Imposter |")
			.Because("only the measured source-generated libraries are compared");
		await That(lines).Contains("| `Foo` | 100.0 ns → 100.0 ns (±0%) | 64 B → 64 B | 10.0× → 10.0× |");
		await That(body).DoesNotContain("**Memory**");
		await That(body).DoesNotContain("**Relative performance**");
	}

	[Test]
	public async Task BuildBody_WhenMemoryChanged_ShouldListItInTheMemoryTable()
	{
		BenchmarkReportFile file = CreateFile(_reportLines, ("Foo_Mockolate", "N=1", 100, 96));
		BenchmarkBaseline baseline = CreateBaseline(("Foo (N=1)", 100, 64, null));

		string body = BenchmarkReport.BuildBody([file,], baseline, _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		await That(lines).Contains("**Memory**");
		await That(lines).Contains("| `Foo (N=1)` | 64 B | 96 B | 🔴 +32 B (+50%) |");
		await That(body).DoesNotContain("✅");
	}

	[Test]
	public async Task BuildBody_WhenRelativePerformanceChanged_ShouldListItInTheRelativePerformanceTable()
	{
		BenchmarkReportFile file = CreateFile(_reportLines,
			("Foo_Mockolate", "", 200, 64),
			("Foo_Imposter", "", 1000, 512),
			("Foo_TUnitMocks", "", 1000, 512));
		BenchmarkBaseline baseline = CreateBaseline(("Foo", 100, 64, 0.1));

		string body = BenchmarkReport.BuildBody([file,], baseline, _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		await That(lines).Contains(
			"**Relative performance** (how many times as long the other library takes as Mockolate, main → PR)");
		await That(lines).Contains("| Benchmark | vs Imposter | vs TUnitMocks |");
		await That(lines).Contains("| `Foo` | 10.0× → 5.00× 🔴 +100% | 10.0× → 5.00× 🔴 +100% |");
	}

	[Test]
	public async Task BuildBody_ShouldListNewAndRemovedBenchmarks()
	{
		BenchmarkReportFile file = CreateFile(_reportLines,
			("Foo_Mockolate", "", 100, 64),
			("Bar_Mockolate", "", 100, 64));
		BenchmarkBaseline baseline = CreateBaseline(("Foo", 100, 64, null), ("Baz", 100, 64, null));

		string body = BenchmarkReport.BuildBody([file,], baseline, _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		await That(lines).Contains("New, without results on `main`: `Bar`");
		await That(lines).Contains("Removed: `Baz`");
	}

	[Test]
	public async Task BuildBody_WhenTheRawResultsAreTooLong_ShouldReferToTheArtifact()
	{
		string[] longReport = Enumerable.Range(0, 1000)
			.Select(i => $"| Foo_Moq | {i} | {new string('x', 50)} |")
			.Prepend("|---|---|---|")
			.Prepend("| Method | N | Mean |")
			.ToArray();

		string body = BenchmarkReport.BuildBody([CreateFile(longReport), CreateFile(_reportLines),], null,
			_columnsToRemove);

		await That(body).Contains(
			"_The raw results do not fit into this comment, see the artifact `Benchmarks-FooBenchmarks` of the workflow run._");
		await That(body).DoesNotContain("<summary>Raw results</summary>").Because("not even the first raw results fit");
	}

	private static BenchmarkReportFile CreateFile(string[] reportLines,
		params (string Method, string Parameters, double Mean, double Allocated)[] measurements)
		=> new("FooBenchmarks", reportLines, BenchmarkReportTestData.Measurements(measurements));

	private static BenchmarkBaseline CreateBaseline(
		params (string Name, double Mean, double Allocated, double? Ratio)[] benchmarks)
		=> new("abcdef1234567890", benchmarks.ToDictionary(
			benchmark => benchmark.Name,
			benchmark => new BaselineBenchmark(benchmark.Mean, benchmark.Allocated,
				benchmark.Ratio == null
					? new Dictionary<string, double>()
					: new Dictionary<string, double>
					{
						["Imposter"] = benchmark.Ratio.Value,
						["TUnitMocks"] = benchmark.Ratio.Value,
					})));

	private static readonly string[] _columnsToRemove = ["RatioSD", "Gen0", "Gen1", "Gen2",];

	private static readonly string[] _reportLines =
	[
		"```",
		"preamble line",
		"```",
		"",
		"| Method | Mean | Allocated |",
		"|--------|-----:|----------:|",
		"| Foo_Mockolate | 100 ns | 1 KB |",
		"| Foo_Imposter  | 200 ns | 2 KB |",
	];

	private static readonly string[] _groupedReportLines =
	[
		"| Method | N | Mean | Allocated |",
		"|--------|---|-----:|----------:|",
		"| Indexer_Mockolate | 1 | 100 ns | 1 KB |",
		"| Indexer_Moq | 1 | 200 ns | 2 KB |",
		"|         |   |      |          |",
		"| Indexer_Mockolate | 10 | 1000 ns | 10 KB |",
		"| Indexer_Moq | 10 | 2000 ns | 20 KB |",
	];
}
