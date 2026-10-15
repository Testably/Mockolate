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
	public async Task FindColumnIndex_ShouldMatchCaseInsensitively()
	{
		int index = BenchmarkReport.FindColumnIndex(["Method", "Mean", "Alloc Ratio",], "alloc ratio");

		await That(index).IsEqualTo(2);
	}

	[Test]
	public async Task FindColumnIndex_WhenColumnNotFound_ShouldReturnMinusOne()
	{
		int index = BenchmarkReport.FindColumnIndex(["Method", "Mean",], "Ratio");

		await That(index).IsEqualTo(-1);
	}

	[Test]
	public async Task FindCommonRowPrefix_ShouldIgnoreHeaderAndSeparatorRows()
	{
		List<TableRow> rows =
		[
			new(0, ["Method", "Mean",]),
			new(1, ["---", "---",]),
		];

		string prefix = BenchmarkReport.FindCommonRowPrefix(rows);

		await That(prefix).IsNull();
	}

	[Test]
	public async Task FindCommonRowPrefix_ShouldSkipBaselineMarkerRow()
	{
		List<TableRow> rows =
		[
			new(BenchmarkTableParser.DataRowStartIndex, ["baseline*", "1",]),
			new(3, ["Indexer_Mockolate", "1",]),
			new(4, ["Indexer_Moq", "1",]),
		];

		string prefix = BenchmarkReport.FindCommonRowPrefix(rows);

		await That(prefix).IsEqualTo("Indexer_");
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

		string prefix = BenchmarkReport.FindCommonRowPrefix(rows);

		await That(prefix).IsEqualTo("Indexer_");
	}

	[Test]
	public async Task FindCommonRowPrefix_WhenADataRowHasNoUnderscore_ShouldReturnNull()
	{
		List<TableRow> rows =
		[
			new(BenchmarkTableParser.DataRowStartIndex, ["NoUnderscore", "1",]),
		];

		string prefix = BenchmarkReport.FindCommonRowPrefix(rows);

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

		string prefix = BenchmarkReport.FindCommonRowPrefix(rows);

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

		string prefix = BenchmarkReport.FindCommonRowPrefix(rows);

		await That(prefix).IsNull();
	}

	[Test]
	public async Task RecomputeBaselineRatio_ShouldOverwriteRatioWithBaselineDividedByMockolate()
	{
		string[] baseline = ["Foo_Mockolate", "206.44 ns", "1.00", "1048 B", "1.00",];
		string[] mockolate = ["Foo_Mockolate", "216.85 ns", "1.00", "1048 B", "1.00",];

		BenchmarkReport.RecomputeBaselineRatio(baseline, mockolate, 1, 2);

		await That(baseline[2]).IsEqualTo("0.95");
	}

	[Test]
	public async Task RecomputeBaselineRatio_WhenRatioIndexIsMissing_ShouldNotMutate()
	{
		string[] baseline = ["Foo_Mockolate", "206.44 ns",];
		string[] mockolate = ["Foo_Mockolate", "216.85 ns",];

		BenchmarkReport.RecomputeBaselineRatio(baseline, mockolate, 1, -1);

		await That(baseline).IsEqualTo(["Foo_Mockolate", "206.44 ns",]);
	}

	[Test]
	public async Task RecomputeBaselineRatio_WhenValueCannotBeParsed_ShouldNotMutate()
	{
		string[] baseline = ["Foo_Mockolate", "junk", "1.00",];
		string[] mockolate = ["Foo_Mockolate", "216.85 ns", "1.00",];

		BenchmarkReport.RecomputeBaselineRatio(baseline, mockolate, 1, 2);

		await That(baseline[2]).IsEqualTo("1.00");
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

	[Test]
	[Arguments("1048 B", 1048.0)]
	[Arguments("1 KB", 1024.0)]
	[Arguments("2 MB", 2.0 * 1024 * 1024)]
	[Arguments("3 GB", 3.0 * 1024 * 1024 * 1024)]
	public async Task TryParseQuantity_ShouldNormalizeMemoryUnitsToBytes(string value, double expected)
	{
		bool ok = BenchmarkReport.TryParseQuantity(value, out double actual);

		await That(ok).IsTrue();
		await That(actual).IsEqualTo(expected).Within(0.0001);
	}

	[Test]
	[Arguments("206.44 ns", 206.44)]
	[Arguments("1,377.70 ns", 1377.70)]
	[Arguments("1.00 μs", 1000.0)]
	[Arguments("1.00 us", 1000.0)]
	[Arguments("2 ms", 2_000_000.0)]
	[Arguments("3 s", 3_000_000_000.0)]
	[Arguments("500 ps", 0.5)]
	public async Task TryParseQuantity_ShouldNormalizeTimeUnitsToNanoseconds(string value, double expected)
	{
		bool ok = BenchmarkReport.TryParseQuantity(value, out double actual);

		await That(ok).IsTrue();
		await That(actual).IsEqualTo(expected).Within(0.0001);
	}

	[Test]
	[Arguments("")]
	[Arguments("not a number")]
	[Arguments("100 weird")]
	public async Task TryParseQuantity_WhenInputIsInvalid_ShouldReturnFalse(string value)
	{
		bool ok = BenchmarkReport.TryParseQuantity(value, out _);

		await That(ok).IsFalse();
	}
}
