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
	public async Task BuildBody_WithoutMain_ShouldWarnAndRenderTheRawResultsCollapsedUnderTheClassName()
	{
		string body = BenchmarkReport.BuildBody([CreateFile(_reportLines),], null, _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		await That(lines[0]).IsEqualTo("## :rocket: Benchmark Results");
		await That(lines[1]).IsEqualTo(
			"⚠️ There are no results of `main` for `FooBenchmarks`, so they are not compared.");
		await That(body).DoesNotContain("✅");
		int summary = Array.IndexOf(lines, "<summary>FooBenchmarks</summary>");
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
	public async Task BuildBody_WhenMainIsMissingForSomeClasses_ShouldWarnOnceAndCompareTheOthers()
	{
		BenchmarkReportFile compared = CreateFile("FooBenchmarks", _reportLines,
			[("Foo_Mockolate", "", 100, 64),], ("Foo_Mockolate", "", 100, 64));
		BenchmarkReportFile bar = CreateFile("BarBenchmarks", _reportLines, null, ("Bar_Mockolate", "", 100, 96));
		BenchmarkReportFile baz = CreateFile("BazBenchmarks", _reportLines, null, ("Baz_Mockolate", "", 100, 96));

		string body = BenchmarkReport.BuildBody([compared, bar, baz,], null, _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		await That(lines[1]).StartsWith("Compared to `main`, which ran on the same runner.");
		await That(lines[2]).IsEqualTo(
			"⚠️ There are no results of `main` for `BarBenchmarks`, `BazBenchmarks`, so they are not compared.");
		await That(lines.Count(line => line.Contains("⚠️"))).IsEqualTo(1);
		await That(lines).Contains("✅ No changes in memory or time (1 benchmarks)");
		await That(body).DoesNotContain("New, without results");
	}

	[Test]
	public async Task BuildBody_ShouldOrderTheClassesLikeTheBenchmarksPage()
	{
		string[] names =
		[
			"CallbackBenchmarks", "UnknownBenchmarks", "CompleteMethodBenchmarks", "MockCreationBenchmarks",
			"CompleteWorkflowBenchmarks",
		];

		string body = BenchmarkReport.BuildBody(
			names.Select(name => CreateFile(name, _reportLines, null)).ToArray(), null, _columnsToRemove);

		string[] summaries = body.Split(Environment.NewLine).Where(line => line.StartsWith("<summary>")).ToArray();
		await That(summaries).IsEqualTo([
			"<summary>CompleteWorkflowBenchmarks</summary>",
			"<summary>MockCreationBenchmarks</summary>",
			"<summary>CompleteMethodBenchmarks</summary>",
			"<summary>CallbackBenchmarks</summary>",
			"<summary>UnknownBenchmarks</summary>",
		]);
	}

	[Test]
	public async Task BuildBody_WhenNothingChanged_ShouldConfirmItAndLinkTheCommitOfMain()
	{
		BenchmarkReportFile file = CreateFile("FooBenchmarks", _reportLines,
			[("Foo_Mockolate", "", 100, 64),],
			("Foo_Mockolate", "", 109, 64),
			("Foo_Imposter", "", 1000, 512));

		string body = BenchmarkReport.BuildBody([file,], "https://example.com/commit/abc", _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		await That(lines[1]).IsEqualTo(
			"Compared to [`main`](https://example.com/commit/abc), which ran on the same runner. Times that change by more than 10% are marked.");
		await That(lines).Contains("✅ No changes in memory or time (1 benchmarks)");
		await That(body).DoesNotContain("⚠️");
		await That(body).DoesNotContain("**Memory**");
		await That(body).DoesNotContain("**Time**");
	}

	[Test]
	public async Task BuildBody_WhenMemoryChanged_ShouldListItInTheMemoryTable()
	{
		BenchmarkReportFile file = CreateFile("FooBenchmarks", _reportLines,
			[("Foo_Mockolate", "N=1", 100, 64),], ("Foo_Mockolate", "N=1", 100, 96));

		string body = BenchmarkReport.BuildBody([file,], null, _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		await That(lines).Contains("**Memory**");
		await That(lines).Contains("| `Foo (N=1)` | 64 B | 96 B | 🔴 +32 B (+50%) |");
		await That(body).DoesNotContain("✅");
	}

	[Test]
	[Arguments(111.0, "| `Foo` | 100.0 ns | 111.0 ns | 🔴 +11% |")]
	[Arguments(89.0, "| `Foo` | 100.0 ns | 89.0 ns | 🟢 -11% |")]
	public async Task BuildBody_WhenTimeChangedByMoreThanTheThreshold_ShouldListItInTheTimeTable(double mean,
		string expectedRow)
	{
		BenchmarkReportFile file = CreateFile("FooBenchmarks", _reportLines,
			[("Foo_Mockolate", "", 100, 64),], ("Foo_Mockolate", "", mean, 64));

		string body = BenchmarkReport.BuildBody([file,], null, _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		await That(lines).Contains("**Time**");
		await That(lines).Contains(expectedRow);
		await That(body).DoesNotContain("✅");
	}

	[Test]
	public async Task BuildBody_ShouldCombineTheChangesOfAllClassesBeforeTheRawResults()
	{
		BenchmarkReportFile foo = CreateFile("FooBenchmarks", _reportLines,
			[("Foo_Mockolate", "", 100, 64),], ("Foo_Mockolate", "", 200, 64));
		BenchmarkReportFile bar = CreateFile("BarBenchmarks", _reportLines,
			[("Bar_Mockolate", "", 100, 64),], ("Bar_Mockolate", "", 50, 64));

		string body = BenchmarkReport.BuildBody([foo, bar,], null, _columnsToRemove);

		string[] lines = body.Split(Environment.NewLine);
		await That(lines.Count(line => line == "**Time**")).IsEqualTo(1);
		int fooRow = Array.IndexOf(lines, "| `Foo` | 100.0 ns | 200.0 ns | 🔴 +100% |");
		int barRow = Array.IndexOf(lines, "| `Bar` | 100.0 ns | 50.0 ns | 🟢 -50% |");
		await That(fooRow).IsGreaterThan(-1);
		await That(barRow).IsGreaterThan(-1);
		await That(Array.IndexOf(lines, "<summary>BarBenchmarks</summary>")).IsGreaterThan(Math.Max(fooRow, barRow));
	}

	[Test]
	public async Task BuildBody_ShouldListNewAndRemovedBenchmarks()
	{
		BenchmarkReportFile file = CreateFile("FooBenchmarks", _reportLines,
			[("Foo_Mockolate", "", 100, 64), ("Baz_Mockolate", "", 100, 64),],
			("Foo_Mockolate", "", 100, 64),
			("Bar_Mockolate", "", 100, 64));

		string body = BenchmarkReport.BuildBody([file,], null, _columnsToRemove);

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
			"_The raw results of `FooBenchmarks` do not fit into this comment, see the artifact `Benchmarks-FooBenchmarks` of the workflow run._");
		await That(body).DoesNotContain("<summary>FooBenchmarks</summary>").Because("not even the first raw results fit");
	}

	private static BenchmarkReportFile CreateFile(string[] reportLines)
		=> CreateFile("FooBenchmarks", reportLines, null);

	private static BenchmarkReportFile CreateFile(string name, string[] reportLines,
		(string Method, string Parameters, double Mean, double Allocated)[]? main,
		params (string Method, string Parameters, double Mean, double Allocated)[] measurements)
		=> new(name, reportLines, BenchmarkReportTestData.Measurements(measurements),
			main == null ? null : BenchmarkReportTestData.Measurements(main));

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
