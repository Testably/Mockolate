using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Mockolate.SourceGenerators.Tests.TestHelpers;

/// <summary>
///     Stores the expected generator output per scenario under <c>Snapshot/Expected/&lt;scenario&gt;/</c>.
///     Files whose content is identical across scenarios (e.g. <c>Mock.g.cs</c>) are stored once under
///     <c>Snapshot/Expected/_Shared/</c> and referenced from a <c>_shared.txt</c> manifest per scenario.
/// </summary>
public static partial class SnapshotStorage
{
	private const string SharedFolderName = "_Shared";
	private const string ManifestFileName = "_shared.txt";

	[GeneratedRegex(@"[ \t]*\[global::System\.Diagnostics\.DebuggerNonUserCode\]\r?\n?")]
	private static partial Regex DebuggerNonUserCodeRegex { get; }

	public static string ReadCoverageFile(string coverageFileName)
	{
		string path = CombinedPaths("Tests", "Mockolate.Tests", "GeneratorCoverage",
			coverageFileName);
		return File.ReadAllText(path);
	}

	public static IReadOnlyDictionary<string, string> GetExpected(string scenario)
	{
		string folder = ExpectedFolder(scenario);
		Dictionary<string, string> result = new();
		if (!Directory.Exists(folder)) return result;

		foreach (string file in Directory.GetFiles(folder).OrderBy(f => f, StringComparer.Ordinal))
		{
			if (Path.GetFileName(file) == ManifestFileName) continue;

			result[Path.GetFileName(file)] = ReadNormalized(file);
		}

		string manifest = Path.Combine(folder, ManifestFileName);
		if (File.Exists(manifest))
		{
			foreach (string line in File.ReadAllLines(manifest))
			{
				int separator = line.IndexOf('|');
				if (separator <= 0) continue;

				string fileName = line.Substring(0, separator);
				string sharedName = line.Substring(separator + 1);
				result[fileName] = ReadNormalized(Path.Combine(ExpectedFolder(SharedFolderName), sharedName));
			}
		}

		return result;
	}

	public static void SetExpected(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> scenarios)
	{
		Dictionary<(string FileName, string Content), int> useCounts = new();
		Dictionary<string, Dictionary<string, string>> normalized = new();
		foreach (var scenario in scenarios)
		{
			Dictionary<string, string> files = new();
			foreach (var source in scenario.Value)
			{
				string content = StripConfigSpecificLines(source.Value.Replace("\r\n", "\n"));
				files[source.Key] = content;
				useCounts.TryGetValue((source.Key, content), out int count);
				useCounts[(source.Key, content)] = count + 1;
			}

			normalized[scenario.Key] = files;
		}

		// A (name, content) pair used by more than one scenario is stored once in _Shared; when several
		// distinct contents of the same name are shared, a short content hash keeps them apart.
		Dictionary<(string FileName, string Content), string> sharedNames = new();
		foreach (var group in useCounts.Where(x => x.Value > 1).GroupBy(x => x.Key.FileName))
		{
			var variants = group.Select(x => x.Key).OrderBy(x => x.Content, StringComparer.Ordinal).ToList();
			foreach (var variant in variants)
				sharedNames[variant] = variants.Count == 1
					? variant.FileName
					: InsertHash(variant.FileName, variant.Content);
		}

		string expectedRoot = Path.GetDirectoryName(ExpectedFolder(SharedFolderName))!;
		if (Directory.Exists(expectedRoot)) Directory.Delete(expectedRoot, true);

		foreach (var shared in sharedNames)
			WriteNormalized(Path.Combine(ExpectedFolder(SharedFolderName), shared.Value), shared.Key.Content);

		foreach (var scenario in normalized)
		{
			string folder = ExpectedFolder(scenario.Key);
			List<string> manifest = new();
			foreach (var file in scenario.Value)
			{
				if (sharedNames.TryGetValue((file.Key, file.Value), out string? sharedName))
					manifest.Add($"{file.Key}|{sharedName}");
				else
					WriteNormalized(Path.Combine(folder, file.Key), file.Value);
			}

			if (manifest.Count > 0)
			{
				Directory.CreateDirectory(folder);
				manifest.Sort(StringComparer.Ordinal);
				File.WriteAllLines(Path.Combine(folder, ManifestFileName), manifest);
			}
		}
	}

	/// <summary>
	///     The source generator gates `[DebuggerNonUserCode]` behind `#if !DEBUG`, so its output
	///     depends on whether the generator dll was built in Debug or Release. Strip those tokens
	///     on both sides so the snapshot test passes regardless of the build configuration used to
	///     produce the generator. The attribute appears either on its own indented line or inline
	///     directly after an opening brace, so the pattern allows optional leading tabs/spaces.
	/// </summary>
	internal static string StripConfigSpecificLines(string content)
		=> DebuggerNonUserCodeRegex.Replace(content, string.Empty);

	private static string ReadNormalized(string file) =>
		File.ReadAllText(file).Replace("\r\n", "\n");

	private static void WriteNormalized(string path, string content)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content.Replace("\n", Environment.NewLine));
	}

	private static string InsertHash(string fileName, string content)
	{
		string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)))
			.Substring(0, 8).ToLowerInvariant();
		int dot = fileName.IndexOf('.');
		return dot < 0 ? $"{fileName}.{hash}" : $"{fileName.Substring(0, dot)}.{hash}{fileName.Substring(dot)}";
	}

	private static string ExpectedFolder(string scenario) =>
		CombinedPaths("Tests", "Mockolate.SourceGenerators.Tests", "Snapshot", "Expected", scenario);

	private static string CombinedPaths(params string[] paths) =>
		Path.GetFullPath(Path.Combine(paths.Prepend(GetSolutionDirectory()).ToArray()));

	private static string GetSolutionDirectory([CallerFilePath] string path = "") =>
		Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..");
}
