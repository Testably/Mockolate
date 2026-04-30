namespace Build.Tests;

public sealed class BenchmarkTableParserTests
{
	[Test]
	public async Task Reset_ShouldClearMeanIndexAndAllowFreshTable()
	{
		BenchmarkTableParser parser = new([]);
		parser.TryConsume("| Method | Mean |", out _, out _, out _);
		parser.TryConsume("|--------|-----:|", out _, out _, out _);

		parser.Reset();
		parser.TryConsume("| Other | Allocated |", out int header, out _, out _);

		await That(header).IsEqualTo(0);
		await That(parser.MeanIndex).IsEqualTo(-1);
	}

	[Test]
	public async Task TryConsume_ShouldAssignSequentialRowIndicesStartingAtZero()
	{
		BenchmarkTableParser parser = new([]);

		parser.TryConsume("| Method | Mean |", out int header, out _, out _);
		parser.TryConsume("|--------|-----:|", out int separator, out _, out _);
		parser.TryConsume("| Foo    | 1 ns |", out int firstData, out _, out _);
		parser.TryConsume("| Bar    | 2 ns |", out int secondData, out _, out _);

		await That(header).IsEqualTo(0);
		await That(separator).IsEqualTo(1);
		await That(firstData).IsEqualTo(BenchmarkTableParser.DataRowStartIndex);
		await That(secondData).IsEqualTo(3);
	}

	[Test]
	public async Task TryConsume_ShouldComputeDroppedIndicesOnceAndReuseThemForLaterRows()
	{
		BenchmarkTableParser parser = new(["Gen0",]);
		parser.TryConsume("| Method | Mean | Gen0 | Allocated |", out _, out _, out _);

		parser.TryConsume("| Foo | 1 ns | _ignored_ | 100 B |", out _, out string? filtered, out _);

		await That(filtered).IsEqualTo("| Foo | 1 ns | 100 B |");
	}

	[Test]
	public async Task TryConsume_ShouldDetectMeanColumnFromHeader()
	{
		BenchmarkTableParser parser = new([]);

		parser.TryConsume("| Method | Error | Mean | Allocated |", out _, out _, out _);

		await That(parser.MeanIndex).IsEqualTo(2);
	}

	[Test]
	public async Task TryConsume_ShouldDropConfiguredColumnsCaseInsensitively()
	{
		BenchmarkTableParser parser = new(["Gen0", "GEN1",]);

		parser.TryConsume("| Method | Mean | Gen0 | gen1 | Allocated |", out _, out string? filteredHeader,
			out string[]? headerTokens);
		parser.TryConsume("| Foo    | 1 ns | 0.1  | 0.0  | 100 B     |", out _, out _,
			out string[]? dataTokens);

		await That(filteredHeader).IsEqualTo("| Method | Mean | Allocated |");
		await That(headerTokens).IsEqualTo(["Method", "Mean", "Allocated",]);
		await That(dataTokens).IsEqualTo(["Foo", "1 ns", "100 B",]);
	}

	[Test]
	public async Task TryConsume_ShouldStripBdnBoldMarkdownFromTokens()
	{
		BenchmarkTableParser parser = new([]);
		parser.TryConsume("| Method | N | Mean |", out _, out _, out _);
		parser.TryConsume("|--------|---|-----:|", out _, out _, out _);

		parser.TryConsume("| **Indexer_Mockolate** | **1** | **923 ns** |", out _, out _, out string[]? tokens);

		await That(tokens).IsEqualTo(["Indexer_Mockolate", "1", "923 ns",]);
	}

	[Test]
	public async Task TryConsume_WhenHeaderHasNoMeanColumn_ShouldReportNegativeMeanIndex()
	{
		BenchmarkTableParser parser = new([]);

		parser.TryConsume("| Method | Allocated |", out _, out _, out _);

		await That(parser.MeanIndex).IsEqualTo(-1);
	}

	[Test]
	public async Task TryConsume_WhenLineIsNotTableShaped_ShouldReturnFalseAndKeepInitialState()
	{
		BenchmarkTableParser parser = new([]);

		bool consumed = parser.TryConsume("not a table line", out int rowIndex, out string? filteredLine,
			out string[]? tokens);

		await That(consumed).IsFalse();
		await That(rowIndex).IsEqualTo(-1);
		await That(filteredLine).IsNull();
		await That(tokens).IsNull();
	}

	[Test]
	public async Task TryConsume_WhenNonTableLineFollowsTable_ShouldAutoResetSoNextTableStartsAtZero()
	{
		BenchmarkTableParser parser = new([]);
		parser.TryConsume("| Method | Mean |", out _, out _, out _);
		parser.TryConsume("|--------|-----:|", out _, out _, out _);
		parser.TryConsume("| Foo    | 1 ns |", out _, out _, out _);

		parser.TryConsume("blank line", out _, out _, out _);

		parser.TryConsume("| Other | Allocated |", out int restartHeader, out _, out _);
		await That(restartHeader).IsEqualTo(0);
		await That(parser.MeanIndex).IsEqualTo(-1);
	}
}
