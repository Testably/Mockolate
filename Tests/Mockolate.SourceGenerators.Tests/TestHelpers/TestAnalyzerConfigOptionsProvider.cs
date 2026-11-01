using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mockolate.SourceGenerators.Tests.TestHelpers;

internal sealed class TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> globalOptions)
	: AnalyzerConfigOptionsProvider
{
	public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(globalOptions);

	public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Options.Empty;

	public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Options.Empty;

	private sealed class Options(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
	{
		public static readonly Options Empty = new(new Dictionary<string, string>());

		public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
			=> values.TryGetValue(key, out value);
	}
}
