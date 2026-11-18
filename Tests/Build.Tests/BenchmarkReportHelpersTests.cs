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
