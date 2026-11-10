using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
// ReSharper disable once RedundantUsingDirective
using System;
// ReSharper disable once RedundantUsingDirective
using System.Collections.Generic;

namespace Build;

/// <summary>
///     Pure logic for transforming BenchmarkDotNet reports into the body posted as a PR comment, compared to the
///     results of <c>main</c> on the benchmark page.
/// </summary>
static class BenchmarkReport
{
    public const string Heading = "## :rocket: Benchmark Results";

    /// <summary>
    ///     The number of runs on <c>main</c> whose median is the baseline of the time, as a single run on a shared
    ///     runner is too noisy.
    /// </summary>
    public const int BaselineRuns = 10;

    public const double RelativeChangeThreshold = 0.15;

    /// <summary>
    ///     Leaves room for the summaries within the limit of a GitHub comment.
    /// </summary>
    public const int MaxRawResultsLength = 45000;

    const string BaselineLibrary = "Mockolate";
    const string StrongWorse = "🔴";
    const string StrongBetter = "🟢";
    const string WeakWorse = "🔸";
    const string WeakBetter = "🔹";

    // ReSharper disable once UnusedMember.Global
    public static readonly string[] DefaultColumnsToRemove = ["RatioSD", "Gen0", "Gen1", "Gen2",];

    /// <summary>
    ///     The source-generated libraries, whose ratio to Mockolate is stable between runs on <c>main</c>, unlike the one to
    ///     the reflection-based libraries.
    /// </summary>
    static readonly string[] ComparedLibraries = ["Imposter", "TUnitMocks",];

    public static string BuildBody(IReadOnlyList<BenchmarkReportFile> files, BenchmarkBaseline? baseline,
        string[] columnsToRemove)
    {
        StringBuilder sb = new();
        sb.AppendLine(Heading);
        if (baseline == null)
        {
            sb.AppendLine("_There are no results of `main` to compare to._");
        }
        else
        {
            sb.AppendLine(
                $"Compared to `main` @ `{baseline.Sha.AsSpan(0, Math.Min(8, baseline.Sha.Length))}`: memory to the latest run, time to the median of the last {BaselineRuns} runs, as the runners differ in speed. " +
                $"A relative change marks when Mockolate gets more than {FormatPercent(RelativeChangeThreshold).TrimStart('+')} faster or slower relative to " +
                $"{string.Join(" and ", ComparedLibraries)}: {StrongWorse} / {StrongBetter} against both of them, " +
                $"{WeakWorse} / {WeakBetter} against only one.");
        }

        sb.AppendLine();
        HashSet<string> measuredBenchmarks = new();
        var rawResultsLength = 0;
        foreach (var file in files)
        {
            var measurements = ReadMeasurements(file.MeasurementsJson);
            measuredBenchmarks.UnionWith(measurements.Keys);
            sb.AppendLine($"### {file.Name}");
            if (baseline != null)
            {
                sb.Append(CreateSummary(measurements, baseline));
            }

            var rawResults = CreateRawResults(file.ReportLines, columnsToRemove);
            // The summaries take precedence over the raw results when the comment would get too long.
            rawResultsLength += rawResults.Length;
            sb.AppendLine(rawResultsLength <= MaxRawResultsLength
                ? rawResults
                : $"_The raw results do not fit into this comment, see the artifact `Benchmarks-{file.Name}` of the workflow run._");
            sb.AppendLine();
        }

        var removedBenchmarks = baseline?.Benchmarks.Keys.Except(measuredBenchmarks).Order(StringComparer.Ordinal)
            .ToArray() ?? [];
        if (removedBenchmarks.Length > 0)
        {
            sb.AppendLine($"Removed: {string.Join(", ", removedBenchmarks.Select(name => $"`{name}`"))}");
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Reads the results of <c>main</c> from the benchmark data that the docs site shows.
    /// </summary>
    /// <remarks>
    ///     A benchmark that was removed from <c>main</c> is ignored, as its last result belongs to an older commit than
    ///     the latest one.
    /// </remarks>
    public static BenchmarkBaseline? ReadBaseline(string? content)
    {
        const string prefix = "window.BENCHMARK_DATA = ";
        if (content?.StartsWith(prefix, StringComparison.Ordinal) != true)
        {
            return null;
        }

        using var document = JsonDocument.Parse(content.Substring(prefix.Length));
        List<(string Sha, string Name, BaselineBenchmark Benchmark)> benchmarks = new();
        foreach (var benchmark in document.RootElement.EnumerateObject())
        {
            var commits = benchmark.Value.GetProperty("commits").EnumerateArray().ToArray();
            var datasets = benchmark.Value.GetProperty("datasets").EnumerateArray().ToDictionary(
                dataset => dataset.GetProperty("label").GetString()!,
                dataset => dataset.GetProperty("data").EnumerateArray().Select(value => value.GetDouble()).ToArray());
            if (commits.Length == 0 ||
                !datasets.TryGetValue($"{BaselineLibrary} time", out var times) || times.Length == 0 ||
                !datasets.TryGetValue($"{BaselineLibrary} memory", out var allocated) || allocated.Length == 0)
            {
                continue;
            }

            Dictionary<string, double> ratios = new();
            foreach (var library in ComparedLibraries)
            {
                if (datasets.TryGetValue($"{library} time", out var otherTimes) && otherTimes.Length > 0)
                {
                    // The libraries of a run are appended together, so the datasets align at their end.
                    ratios[library] = Median(times.Reverse().Zip(otherTimes.Reverse(), (m, o) => m / o)
                        .Take(BaselineRuns));
                }
            }

            benchmarks.Add((commits[^1].GetProperty("sha").GetString()!, benchmark.Name,
                new BaselineBenchmark(Median(times.TakeLast(BaselineRuns)), allocated[^1], ratios)));
        }

        if (benchmarks.Count == 0)
        {
            return null;
        }

        var latestSha = benchmarks.GroupBy(benchmark => benchmark.Sha).MaxBy(group => group.Count())!.Key;
        return new BenchmarkBaseline(latestSha, benchmarks
            .Where(benchmark => benchmark.Sha == latestSha)
            .ToDictionary(benchmark => benchmark.Name, benchmark => benchmark.Benchmark));
    }

    /// <summary>
    ///     Reads the measurements of each library per benchmark from a full BenchmarkDotNet JSON report, keyed like the
    ///     charts of the benchmark page.
    /// </summary>
    internal static Dictionary<string, Dictionary<string, BenchmarkMeasurement>> ReadMeasurements(string json)
    {
        Dictionary<string, Dictionary<string, BenchmarkMeasurement>> result = new();
        using var document = JsonDocument.Parse(json);
        foreach (var benchmark in document.RootElement.GetProperty("Benchmarks").EnumerateArray())
        {
            var method = benchmark.GetProperty("Method").GetString()!;
            var index = method.LastIndexOf('_');
            if (index <= 0)
            {
                continue;
            }

            var parameters = benchmark.TryGetProperty("Parameters", out var p) ? p.GetString() : null;
            var scenario = method.Substring(0, index);
            var name = string.IsNullOrWhiteSpace(parameters) ? scenario : $"{scenario} ({parameters})";
            var allocated = benchmark.GetProperty("Metrics").EnumerateArray()
                .Where(metric => metric.GetProperty("Descriptor").GetProperty("Id").GetString() == "Allocated Memory")
                .Select(metric => metric.GetProperty("Value").GetDouble())
                .FirstOrDefault();
            if (!result.TryGetValue(name, out var libraries))
            {
                libraries = new Dictionary<string, BenchmarkMeasurement>();
                result[name] = libraries;
            }

            libraries[method.Substring(index + 1)] = new BenchmarkMeasurement(
                benchmark.GetProperty("Statistics").GetProperty("Mean").GetDouble(), allocated);
        }

        return result;
    }

    internal static string CreateSummary(Dictionary<string, Dictionary<string, BenchmarkMeasurement>> measurements,
        BenchmarkBaseline baseline)
    {
        var benchmarks = measurements
            .Where(pair => pair.Value.ContainsKey(BaselineLibrary))
            .Select(pair => pair.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var libraries = ComparedLibraries
            .Where(library => measurements.Values.Any(m => m.ContainsKey(library)))
            .ToArray();
        var newBenchmarks = benchmarks.Where(name => !baseline.Benchmarks.ContainsKey(name)).ToArray();
        List<string> memoryRows = new();
        List<string> relativeRows = new();
        List<string> allRows = new();
        foreach (var name in benchmarks.Except(newBenchmarks))
        {
            var current = measurements[name][BaselineLibrary];
            var main = baseline.Benchmarks[name];
            var relativeCells = CompareRelativePerformance(measurements[name], main, libraries, out var relativeChanged);
            if (current.Allocated != main.Allocated)
            {
                memoryRows.Add(
                    $"| `{name}` | {FormatBytes(main.Allocated)} | {FormatBytes(current.Allocated)} | {FormatMemoryChange(main.Allocated, current.Allocated)} |");
            }

            if (relativeChanged)
            {
                relativeRows.Add($"| `{name}` | {string.Join(" | ", relativeCells)} |");
            }

            allRows.Add(
                $"| `{name}` | {FormatTime(main.Mean)} → {FormatTime(current.Mean)} ({FormatPercent(current.Mean / main.Mean - 1)}) | " +
                $"{FormatBytes(main.Allocated)} → {FormatBytes(current.Allocated)} | {string.Join(" | ", relativeCells)} |");
        }

        StringBuilder sb = new();
        if (memoryRows.Count == 0 && relativeRows.Count == 0)
        {
            sb.AppendLine($"✅ No changes in memory or relative performance ({allRows.Count} benchmarks)");
            sb.AppendLine();
        }

        if (memoryRows.Count > 0)
        {
            sb.AppendLine("**Memory**");
            sb.AppendLine();
            sb.AppendLine("| Benchmark | main | PR | Δ |");
            sb.AppendLine("|---|--:|--:|--:|");
            memoryRows.ForEach(row => sb.AppendLine(row));
            sb.AppendLine();
        }

        if (relativeRows.Count > 0)
        {
            sb.AppendLine(
                "**Relative performance** (how many times as long the other library takes as Mockolate, main → PR)");
            sb.AppendLine();
            sb.AppendLine($"| Benchmark | {string.Join(" | ", libraries.Select(library => $"vs {library}"))} |");
            sb.AppendLine($"|---|{string.Concat(libraries.Select(_ => "--:|"))}");
            relativeRows.ForEach(row => sb.AppendLine(row));
            sb.AppendLine();
        }

        if (newBenchmarks.Length > 0)
        {
            sb.AppendLine($"New, without results on `main`: {string.Join(", ", newBenchmarks.Select(name => $"`{name}`"))}");
            sb.AppendLine();
        }

        if (allRows.Count > 0)
        {
            sb.AppendLine("<details>");
            sb.AppendLine("<summary>All benchmarks compared to main</summary>");
            sb.AppendLine();
            sb.AppendLine(
                $"| Benchmark | Time | Memory | {string.Join(" | ", libraries.Select(library => $"vs {library}"))} |");
            sb.AppendLine($"|---|--:|--:|{string.Concat(libraries.Select(_ => "--:|"))}");
            allRows.ForEach(row => sb.AppendLine(row));
            sb.AppendLine();
            sb.AppendLine("</details>");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Compares the time of Mockolate relative to each of the <paramref name="libraries" /> with <c>main</c>.
    /// </summary>
    /// <remarks>
    ///     The ratio of two benchmarks of the same run does not depend on the speed of the runner. A change against
    ///     all of the libraries in the same direction is most likely caused by Mockolate and gets a stronger marker.
    /// </remarks>
    internal static string[] CompareRelativePerformance(Dictionary<string, BenchmarkMeasurement> measurements,
        BaselineBenchmark main, string[] libraries, out bool changed)
    {
        var mockolate = measurements[BaselineLibrary];
        var ratios = libraries
            .Select(library => measurements.TryGetValue(library, out var other)
                ? mockolate.Mean / other.Mean
                : (double?)null)
            .ToArray();
        var relativeChanges = libraries
            .Select((library, index) => main.Ratios.TryGetValue(library, out var mainRatio) && ratios[index] != null
                ? ratios[index] / mainRatio - 1
                : null)
            .ToArray();
        var compared = relativeChanges.Count(relativeChange => relativeChange != null);
        var slower = relativeChanges.Count(relativeChange => relativeChange > RelativeChangeThreshold);
        var faster = relativeChanges.Count(relativeChange => relativeChange < -RelativeChangeThreshold);
        changed = slower + faster > 0;
        var strongSlower = compared > 1 && slower == compared;
        var strongFaster = compared > 1 && faster == compared;
        return libraries.Select((library, index) =>
        {
            if (ratios[index] == null)
            {
                return "";
            }

            var cell = main.Ratios.TryGetValue(library, out var mainRatio)
                ? $"{FormatRatio(mainRatio)} → {FormatRatio(ratios[index]!.Value)}"
                : $"→ {FormatRatio(ratios[index]!.Value)}";
            var relativeChange = relativeChanges[index];
            if (relativeChange > RelativeChangeThreshold)
            {
                cell += $" {(strongSlower ? StrongWorse : WeakWorse)} {FormatPercent(relativeChange.Value)}";
            }
            else if (relativeChange < -RelativeChangeThreshold)
            {
                cell += $" {(strongFaster ? StrongBetter : WeakBetter)} {FormatPercent(relativeChange.Value)}";
            }

            return cell;
        }).ToArray();
    }

    internal static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 0 ? (sorted[middle - 1] + sorted[middle]) / 2 : sorted[middle];
    }

    internal static string FormatPercent(double relativeChange)
        => (relativeChange * 100).ToString("+0;-0;±0", CultureInfo.InvariantCulture) + "%";

    /// <summary>
    ///     Formats the <paramref name="ratio" /> of Mockolate to another library as the factor of the other library, as
    ///     the docs site does, which keeps its precision when Mockolate is much faster.
    /// </summary>
    internal static string FormatRatio(double ratio)
        => (1 / ratio).ToString(ratio > 0.1 ? "0.00" : "0.0", CultureInfo.InvariantCulture) + "×";

    internal static string FormatBytes(double bytes)
        => bytes.ToString("N0", CultureInfo.InvariantCulture) + " B";

    internal static string FormatMemoryChange(double main, double current)
    {
        var difference = current - main;
        var change =
            $"{(difference > 0 ? StrongWorse : StrongBetter)} {difference.ToString("+#,0;-#,0", CultureInfo.InvariantCulture)} B";
        return main == 0 ? change : $"{change} ({FormatPercent(current / main - 1)})";
    }

    internal static string FormatTime(double nanoseconds)
        => nanoseconds switch
        {
            < 1_000 => nanoseconds.ToString("0.0", CultureInfo.InvariantCulture) + " ns",
            < 1_000_000 => (nanoseconds / 1_000).ToString("0.00", CultureInfo.InvariantCulture) + " μs",
            _ => (nanoseconds / 1_000_000).ToString("0.00", CultureInfo.InvariantCulture) + " ms",
        };

    internal static string CreateRawResults(string[] reportLines, string[] columnsToRemove)
    {
        StringBuilder sb = new();
        sb.AppendLine("<details>");
        sb.AppendLine("<summary>Raw results</summary>");
        sb.AppendLine();
        BenchmarkTableParser parser = new(columnsToRemove);
        List<TableRow> tableBuffer = new();
        var inCodeBlock = false;
        foreach (var line in reportLines)
        {
            if (line.StartsWith("```"))
            {
                FlushTableBuffer(sb, tableBuffer);
                inCodeBlock = !inCodeBlock;
                sb.AppendLine(inCodeBlock ? "<pre>" : "</pre>");
                if (!inCodeBlock) sb.AppendLine();

                parser.Reset();
                continue;
            }

            if (!parser.TryConsume(line, out var rowIndex, out _, out var filteredTokens))
            {
                FlushTableBuffer(sb, tableBuffer);
                sb.AppendLine(line);
                continue;
            }

            tableBuffer.Add(new TableRow(rowIndex, filteredTokens));
        }

        FlushTableBuffer(sb, tableBuffer);
        sb.AppendLine();
        sb.AppendLine("</details>");
        return sb.ToString();
    }

    internal static void FlushTableBuffer(StringBuilder sb, List<TableRow> tableBuffer)
    {
        if (tableBuffer.Count == 0) return;

        var commonPrefix = FindCommonRowPrefix(tableBuffer);
        var headerRow = tableBuffer[0].RowIndex == 0 ? tableBuffer[0] : null;

        foreach (var row in tableBuffer)
        {
            if (row.RowIndex >= BenchmarkTableParser.DataRowStartIndex && row.Tokens.Length == 0)
            {
                if (headerRow != null && headerRow.Tokens.Length > 0)
                {
                    var empty = new string[headerRow.Tokens.Length];
                    Array.Fill(empty, string.Empty);
                    sb.AppendLine(JoinTokens(empty));
                }

                continue;
            }

            var displayLine = JoinTokens(ApplyHeaderAndPrefixStripping(row, commonPrefix));

            var isMockolateRow = row.RowIndex >= BenchmarkTableParser.DataRowStartIndex
                                 && row.Tokens[0].Contains("_Mockolate", StringComparison.OrdinalIgnoreCase);

            if (isMockolateRow)
                MakeLineBold(sb, displayLine);
            else
                sb.AppendLine(displayLine);
        }

        tableBuffer.Clear();
    }

    internal static string[] ApplyHeaderAndPrefixStripping(TableRow row, string? commonPrefix)
    {
        if (commonPrefix == null || row.Tokens.Length == 0) return row.Tokens;

        var result = (string[])row.Tokens.Clone();
        if (row.RowIndex == 0)
            result[0] = commonPrefix.TrimEnd('_');
        else if (row.RowIndex >= BenchmarkTableParser.DataRowStartIndex
                 && result[0].StartsWith(commonPrefix, StringComparison.Ordinal))
            result[0] = result[0].Substring(commonPrefix.Length);

        return result;
    }

    internal static string? FindCommonRowPrefix(List<TableRow> rows)
    {
        string? prefix = null;
        foreach (var row in rows)
        {
            if (row.RowIndex < BenchmarkTableParser.DataRowStartIndex) continue;

            if (row.Tokens.Length == 0) continue;

            var name = row.Tokens[0];
            var underscoreIdx = name.IndexOf('_');
            if (underscoreIdx <= 0) return null;

            var thisPrefix = name.Substring(0, underscoreIdx + 1);
            if (prefix == null)
                prefix = thisPrefix;
            else if (!string.Equals(prefix, thisPrefix, StringComparison.Ordinal)) return null;
        }

        return prefix;
    }

    internal static string JoinTokens(string[] tokens)
    {
        StringBuilder sb = new();
        sb.Append('|');
        foreach (var token in tokens)
        {
            sb.Append(' ');
            sb.Append(token);
            sb.Append(" |");
        }

        return sb.ToString();
    }

    internal static int[] DetermineDroppedColumnIndices(string headerLine, string[] columnsToRemove)
    {
        var tokens = SplitTableRow(headerLine);
        List<int> indices = new();
        for (var i = 0; i < tokens.Length; i++)
            foreach (var columnName in columnsToRemove)
                if (string.Equals(tokens[i], columnName, StringComparison.OrdinalIgnoreCase))
                {
                    indices.Add(i);
                    break;
                }

        return indices.ToArray();
    }

    internal static string RemoveColumns(string line, int[] droppedColumnIndices)
    {
        if (droppedColumnIndices.Length == 0) return line;

        var tokens = SplitTableRow(line);
        StringBuilder result = new();
        result.Append('|');
        for (var i = 0; i < tokens.Length; i++)
        {
            if (Array.IndexOf(droppedColumnIndices, i) >= 0) continue;

            result.Append(' ');
            result.Append(tokens[i]);
            result.Append(" |");
        }

        return result.ToString();
    }

    internal static string StripMarkdownBold(string token)
    {
        if (token.Length >= 4 && token.StartsWith("**", StringComparison.Ordinal)
                              && token.EndsWith("**", StringComparison.Ordinal))
            return token.Substring(2, token.Length - 4);

        return token;
    }

    internal static string[] SplitTableRow(string line)
    {
        var parts = line.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return [];

        var tokens = new string[parts.Length - 2];
        Array.Copy(parts, 1, tokens, 0, tokens.Length);
        return tokens;
    }

    internal static void MakeLineBold(StringBuilder sb, string line)
    {
        var tokens = line.Split("|", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        sb.Append('|');
        foreach (var token in tokens)
        {
            sb.Append(" **");
            sb.Append(token);
            sb.Append("** |");
        }

        sb.AppendLine();
    }
}

/// <param name="Name">The benchmark class, which is also the suffix of its workflow artifact.</param>
sealed record BenchmarkReportFile(string Name, string[] ReportLines, string MeasurementsJson);

sealed record TableRow(int RowIndex, string[] Tokens);

sealed record BenchmarkMeasurement(double Mean, double Allocated);

sealed record BaselineBenchmark(double Mean, double Allocated, Dictionary<string, double> Ratios);

sealed record BenchmarkBaseline(string Sha, Dictionary<string, BaselineBenchmark> Benchmarks);

/// <summary>
///     Parses the markdown tables produced by BenchmarkDotNet's GitHub exporter.
///     Tables are framed by a header row (index 0), a separator row like <c>|---|---|</c> (index 1), and one or more
///     data rows (index >= <see cref="DataRowStartIndex" />). The parser auto-resets on the first non-table line that
///     follows a table, so a single instance can walk a document that contains multiple tables.
/// </summary>
sealed class BenchmarkTableParser
{
    public const int DataRowStartIndex = 2;

    // ReSharper disable InconsistentNaming
    readonly string[] _columnsToRemove;
    int[]? _droppedColumnIndices;
    int _nextRowIndex;
    // ReSharper restore InconsistentNaming

    public BenchmarkTableParser(string[] columnsToRemove)
    {
        _columnsToRemove = columnsToRemove;
    }

    public bool TryConsume(string line, out int rowIndex,
        [NotNullWhen(true)] out string? filteredLine,
        [NotNullWhen(true)] out string[]? tokens)
    {
        rowIndex = -1;
        if (!line.StartsWith('|') || !line.EndsWith('|'))
        {
            if (_nextRowIndex > 0) Reset();

            filteredLine = null;
            tokens = null;
            return false;
        }

        _droppedColumnIndices ??= BenchmarkReport.DetermineDroppedColumnIndices(line, _columnsToRemove);
        filteredLine = BenchmarkReport.RemoveColumns(line, _droppedColumnIndices);
        tokens = filteredLine.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < tokens.Length; i++) tokens[i] = BenchmarkReport.StripMarkdownBold(tokens[i]);

        rowIndex = _nextRowIndex;
        _nextRowIndex++;
        return true;
    }

    public void Reset()
    {
        _droppedColumnIndices = null;
        _nextRowIndex = 0;
    }
}
