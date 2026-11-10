namespace Build.Tests;

public sealed class BenchmarkReportHelpersTests
{
	[Test]
	public async Task ApplyHeaderAndPrefixStripping_ShouldRemoveCommonPrefixFromDataRow()
	{
		TableRow row = new(BenchmarkTableParser.DataRowStartIndex, ["PropRead_Mockolate", "1",]);

		string[] result = BenchmarkReport.ApplyHeaderAndPrefixStripping(row, "PropRead_");

		await That(result).IsEqualTo(["Mockolate", "1",]);
	}

	[Test]
	public async Task ApplyHeaderAndPrefixStripping_ShouldStripTrailingUnderscoreFromHeader()
	{
		TableRow header = new(0, ["Method", "Mean",]);

		string[] result = BenchmarkReport.ApplyHeaderAndPrefixStripping(header, "PropRead_");

		await That(result).IsEqualTo(["PropRead", "Mean",]);
	}

	[Test]
	public async Task ApplyHeaderAndPrefixStripping_WhenCommonPrefixIsNull_ShouldReturnTokensUnchanged()
	{
		TableRow row = new(BenchmarkTableParser.DataRowStartIndex, ["Foo", "1",]);

		string[] result = BenchmarkReport.ApplyHeaderAndPrefixStripping(row, null);

		await That(result).IsEqualTo(["Foo", "1",]);
	}

	[Test]
	public async Task DetermineDroppedColumnIndices_ShouldMatchCaseInsensitivelyAndReturnIndicesInHeaderOrder()
	{
		int[] indices = BenchmarkReport.DetermineDroppedColumnIndices(
			"| Method | Mean | Gen0 | gen1 | Allocated |",
			["GEN0", "Gen1",]);

		await That(indices).IsEqualTo([2, 3,]);
	}

	[Test]
	public async Task DetermineDroppedColumnIndices_WhenNoColumnsMatch_ShouldReturnEmpty()
	{
		int[] indices = BenchmarkReport.DetermineDroppedColumnIndices(
			"| Method | Mean | Allocated |",
			["Gen0",]);

		await That(indices).IsEqualTo(Array.Empty<int>());
	}

	[Test]
	public async Task FindCommonRowPrefix_ShouldIgnoreHeaderAndSeparatorRows()
	{
		List<TableRow> rows =
		[
			new(0, ["Method", "Mean",]),
			new(1, ["---", "---",]),
		];

		string? prefix = BenchmarkReport.FindCommonRowPrefix(rows);

		await That(prefix).IsNull();
	}

	[Test]
	public async Task FindCommonRowPrefix_ShouldSkipEmptyDataRowAndUseRemainingRows()
	{
		List<TableRow> rows =
		[
			new(BenchmarkTableParser.DataRowStartIndex, ["Indexer_Mockolate", "1",]),
			new(3, ["Indexer_Moq", "1",]),
			new(4, []),
			new(5, ["Indexer_Mockolate", "10",]),
			new(6, ["Indexer_Moq", "10",]),
		];

		string? prefix = BenchmarkReport.FindCommonRowPrefix(rows);

		await That(prefix).IsEqualTo("Indexer_");
	}

	[Test]
	public async Task FindCommonRowPrefix_WhenADataRowHasNoUnderscore_ShouldReturnNull()
	{
		List<TableRow> rows =
		[
			new(BenchmarkTableParser.DataRowStartIndex, ["NoUnderscore", "1",]),
		];

		string? prefix = BenchmarkReport.FindCommonRowPrefix(rows);

		await That(prefix).IsNull();
	}

	[Test]
	public async Task FindCommonRowPrefix_WhenAllDataRowsShareAPrefix_ShouldReturnPrefixWithTrailingUnderscore()
	{
		List<TableRow> rows =
		[
			new(0, ["Method", "Mean",]),
			new(1, ["---", "---",]),
			new(BenchmarkTableParser.DataRowStartIndex, ["PropRead_Mockolate", "1",]),
			new(3, ["PropRead_Imposter", "2",]),
		];

		string? prefix = BenchmarkReport.FindCommonRowPrefix(rows);

		await That(prefix).IsEqualTo("PropRead_");
	}

	[Test]
	public async Task FindCommonRowPrefix_WhenDataRowsDisagree_ShouldReturnNull()
	{
		List<TableRow> rows =
		[
			new(BenchmarkTableParser.DataRowStartIndex, ["A_Mockolate", "1",]),
			new(3, ["B_Imposter", "2",]),
		];

		string? prefix = BenchmarkReport.FindCommonRowPrefix(rows);

		await That(prefix).IsNull();
	}

	[Test]
	[Arguments(new[] { 3.0, 1.0, 2.0, }, 2.0)]
	[Arguments(new[] { 4.0, 1.0, 3.0, 2.0, }, 2.5)]
	public async Task Median_ShouldReturnTheMiddleValueOrTheMeanOfBothMiddleValues(double[] values, double expected)
	{
		double median = BenchmarkReport.Median(values);

		await That(median).IsEqualTo(expected);
	}

	[Test]
	[Arguments(0.5, "2.00×")]
	[Arguments(0.05, "20.0×")]
	public async Task FormatRatio_ShouldShowHowManyTimesAsLongTheOtherLibraryTakes(double ratio, string expected)
	{
		string result = BenchmarkReport.FormatRatio(ratio);

		await That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(0.25, "+25%")]
	[Arguments(-0.1, "-10%")]
	[Arguments(0.0, "±0%")]
	public async Task FormatPercent_ShouldIncludeTheSign(double relativeChange, string expected)
	{
		string result = BenchmarkReport.FormatPercent(relativeChange);

		await That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(123.4, "123.4 ns")]
	[Arguments(12_340.0, "12.34 μs")]
	[Arguments(12_340_000.0, "12.34 ms")]
	public async Task FormatTime_ShouldChooseTheUnitByMagnitude(double nanoseconds, string expected)
	{
		string result = BenchmarkReport.FormatTime(nanoseconds);

		await That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1000.0, 1200.0, "🔴 +200 B (+20%)")]
	[Arguments(1000.0, 800.0, "🟢 -200 B (-20%)")]
	[Arguments(0.0, 64.0, "🔴 +64 B")]
	public async Task FormatMemoryChange_ShouldMarkTheDirection(double main, double current, string expected)
	{
		string result = BenchmarkReport.FormatMemoryChange(main, current);

		await That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task ReadMeasurements_ShouldKeyByScenarioAndParametersPerLibrary()
	{
		string json = BenchmarkReportTestData.Measurements(
			("Method_Mockolate", "N=1", 100, 64),
			("Method_Moq", "N=1", 1000, 512),
			("CreateMock_Mockolate", "", 50, 32));

		Dictionary<string, Dictionary<string, BenchmarkMeasurement>> result = BenchmarkReport.ReadMeasurements(json);

		await That(result.Keys).IsEqualTo(["Method (N=1)", "CreateMock",]).InAnyOrder();
		await That(result["Method (N=1)"]["Mockolate"]).IsEqualTo(new BenchmarkMeasurement(100, 64));
		await That(result["Method (N=1)"]["Moq"]).IsEqualTo(new BenchmarkMeasurement(1000, 512));
		await That(result["CreateMock"]["Mockolate"]).IsEqualTo(new BenchmarkMeasurement(50, 32));
	}

	[Test]
	public async Task ReadBaseline_ShouldUseTheMedianTimeAndRatioOfTheLastRunsAndTheLatestMemory()
	{
		string content = BenchmarkReportTestData.BaselineData(("Method (N=1)", ["a", "b", "c",],
		[
			("Mockolate time", [100, 200, 300,]),
			("Mockolate memory", [64, 64, 96,]),
			("Imposter time", [1000, 1000, 1000,]),
			("Moq time", [1000, 1000, 1000,]),
		]));

		BenchmarkBaseline? baseline = BenchmarkReport.ReadBaseline(content);

		await That(baseline?.Sha).IsEqualTo("c");
		BaselineBenchmark benchmark = baseline!.Benchmarks["Method (N=1)"];
		await That(benchmark.Mean).IsEqualTo(200.0);
		await That(benchmark.Allocated).IsEqualTo(96.0);
		await That(benchmark.Ratios["Imposter"]).IsEqualTo(0.2).Within(0.0001);
		await That(benchmark.Ratios.Keys).IsEqualTo(["Imposter",]).InAnyOrder()
			.Because("the reflection-based libraries are too noisy to compare with");
	}

	[Test]
	public async Task ReadBaseline_ShouldIgnoreBenchmarksWhoseLatestCommitIsOutdated()
	{
		string content = BenchmarkReportTestData.BaselineData(
			("Kept", ["a", "b",], [("Mockolate time", [1, 2,]), ("Mockolate memory", [1, 1,]),]),
			("AlsoKept", ["a", "b",], [("Mockolate time", [1, 2,]), ("Mockolate memory", [1, 1,]),]),
			("Removed", ["a",], [("Mockolate time", [1,]), ("Mockolate memory", [1,]),]));

		BenchmarkBaseline? baseline = BenchmarkReport.ReadBaseline(content);

		await That(baseline?.Benchmarks.Keys).IsEqualTo(["Kept", "AlsoKept",]).InAnyOrder();
	}

	[Test]
	[Arguments(null)]
	[Arguments("not the benchmark data")]
	[Arguments("window.BENCHMARK_DATA = {}")]
	public async Task ReadBaseline_WhenThereAreNoResults_ShouldReturnNull(string? content)
	{
		BenchmarkBaseline? baseline = BenchmarkReport.ReadBaseline(content);

		await That(baseline).IsNull();
	}

	[Test]
	public async Task CompareRelativePerformance_WhenAllLibrariesAgree_ShouldUseStrongMarkers()
	{
		Dictionary<string, BenchmarkMeasurement> measurements = new()
		{
			["Mockolate"] = new BenchmarkMeasurement(150, 0),
			["Imposter"] = new BenchmarkMeasurement(1000, 0),
			["TUnitMocks"] = new BenchmarkMeasurement(1000, 0),
		};
		BaselineBenchmark main = new(100, 0, new Dictionary<string, double>
		{
			["Imposter"] = 0.1,
			["TUnitMocks"] = 0.1,
		});

		string[] cells = BenchmarkReport.CompareRelativePerformance(measurements, main,
			["Imposter", "TUnitMocks",], out bool changed);

		await That(changed).IsTrue();
		await That(cells).IsEqualTo(["10.0× → 6.67× 🔴 +50%", "10.0× → 6.67× 🔴 +50%",]);
	}

	[Test]
	public async Task CompareRelativePerformance_WhenOnlyOneLibraryChanges_ShouldUseWeakMarkers()
	{
		Dictionary<string, BenchmarkMeasurement> measurements = new()
		{
			["Mockolate"] = new BenchmarkMeasurement(100, 0),
			["Imposter"] = new BenchmarkMeasurement(500, 0),
			["TUnitMocks"] = new BenchmarkMeasurement(1000, 0),
		};
		BaselineBenchmark main = new(100, 0, new Dictionary<string, double>
		{
			["Imposter"] = 0.1,
			["TUnitMocks"] = 0.1,
		});

		string[] cells = BenchmarkReport.CompareRelativePerformance(measurements, main,
			["Imposter", "TUnitMocks",], out bool changed);

		await That(changed).IsTrue();
		await That(cells).IsEqualTo(["10.0× → 5.00× 🔸 +100%", "10.0× → 10.0×",]);
	}

	[Test]
	public async Task CompareRelativePerformance_WhenMockolateGetsFaster_ShouldUseBetterMarkers()
	{
		Dictionary<string, BenchmarkMeasurement> measurements = new()
		{
			["Mockolate"] = new BenchmarkMeasurement(50, 0),
			["Imposter"] = new BenchmarkMeasurement(1000, 0),
			["TUnitMocks"] = new BenchmarkMeasurement(1000, 0),
		};
		BaselineBenchmark main = new(100, 0, new Dictionary<string, double>
		{
			["Imposter"] = 0.1,
			["TUnitMocks"] = 0.1,
		});

		string[] cells = BenchmarkReport.CompareRelativePerformance(measurements, main,
			["Imposter", "TUnitMocks",], out bool changed);

		await That(changed).IsTrue();
		await That(cells).IsEqualTo(["10.0× → 20.0× 🟢 -50%", "10.0× → 20.0× 🟢 -50%",]);
	}

	[Test]
	public async Task CompareRelativePerformance_WhenOnlyOneLibraryIsMeasured_ShouldUseWeakMarkers()
	{
		Dictionary<string, BenchmarkMeasurement> measurements = new()
		{
			["Mockolate"] = new BenchmarkMeasurement(50, 0),
			["Imposter"] = new BenchmarkMeasurement(1000, 0),
		};
		BaselineBenchmark main = new(100, 0, new Dictionary<string, double> { ["Imposter"] = 0.1, });

		string[] cells = BenchmarkReport.CompareRelativePerformance(measurements, main,
			["Imposter", "TUnitMocks",], out bool changed);

		await That(changed).IsTrue();
		await That(cells).IsEqualTo(["10.0× → 20.0× 🔹 -50%", "",])
			.Because("a single library cannot confirm that the change is caused by Mockolate");
	}

	[Test]
	public async Task CompareRelativePerformance_WhenTheLibraryIsNewOnMain_ShouldShowOnlyTheCurrentRatio()
	{
		Dictionary<string, BenchmarkMeasurement> measurements = new()
		{
			["Mockolate"] = new BenchmarkMeasurement(100, 0),
			["TUnitMocks"] = new BenchmarkMeasurement(1000, 0),
		};
		BaselineBenchmark main = new(100, 0, new Dictionary<string, double>());

		string[] cells =
			BenchmarkReport.CompareRelativePerformance(measurements, main, ["TUnitMocks",], out bool changed);

		await That(changed).IsFalse();
		await That(cells).IsEqualTo(["→ 10.0×",]);
	}

	[Test]
	public async Task RemoveColumns_ShouldDropTheGivenIndicesAndPreserveTheRest()
	{
		string result = BenchmarkReport.RemoveColumns(
			"| Method | Mean | Gen0 | Gen1 | Allocated |",
			[2, 3,]);

		await That(result).IsEqualTo("| Method | Mean | Allocated |");
	}

	[Test]
	public async Task RemoveColumns_WhenIndicesAreEmpty_ShouldReturnLineUnchanged()
	{
		string original = "| Method | Mean |";

		string result = BenchmarkReport.RemoveColumns(original, []);

		await That(result).IsEqualTo(original);
	}

	[Test]
	public async Task SplitTableRow_ShouldReturnTrimmedInteriorCells()
	{
		string[] tokens = BenchmarkReport.SplitTableRow("|  Foo |  bar baz  | 42 |");

		await That(tokens).IsEqualTo(["Foo", "bar baz", "42",]);
	}

	[Test]
	public async Task SplitTableRow_WhenLineHasNoPipe_ShouldReturnEmptyArray()
	{
		string[] tokens = BenchmarkReport.SplitTableRow("hello world");

		await That(tokens).IsEqualTo(Array.Empty<string>());
	}

}
