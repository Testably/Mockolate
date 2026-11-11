using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests.TestHelpers;

public static class Generator
{
	private static readonly string[] NoWarn =
	[
		"CS8019" /* Unnecessary using directive. */,
	];

	public static GeneratorResult Run([StringSyntax("c#-test")] string source, params Type[] assemblyTypes)
		=> Run(source, DocumentationMode.Parse, assemblyTypes);

	public static GeneratorResult Run([StringSyntax("c#-test")] string source,
		DocumentationMode documentationMode, params Type[] assemblyTypes)
		=> Run([source,], documentationMode, [], assemblyTypes);

	public static GeneratorResult Run(string[] sources, DocumentationMode documentationMode,
		string[] preprocessorSymbols, params Type[] assemblyTypes)
		=> RunCore(sources, documentationMode, preprocessorSymbols, [], assemblyTypes);

	public static GeneratorResult RunWithReferences([StringSyntax("c#-test")] string source,
		MetadataReference[] externalReferences, params Type[] assemblyTypes)
		=> RunCore([source,], DocumentationMode.Parse, [], externalReferences, assemblyTypes);

	public static GeneratorResult Run([StringSyntax("c#-test")] string source, LanguageVersion languageVersion,
		IReadOnlyDictionary<string, string>? globalOptions)
		=> RunCore([source,], DocumentationMode.Parse, [], [], [], languageVersion, globalOptions);

	public static GeneratorResult Run(string[] sources, string[] preprocessorSymbols, LanguageVersion languageVersion,
		IReadOnlyDictionary<string, string>? globalOptions, params Type[] assemblyTypes)
		=> RunCore(sources, DocumentationMode.Parse, preprocessorSymbols, [], assemblyTypes, languageVersion,
			globalOptions);

	private static GeneratorResult RunCore(string[] sources, DocumentationMode documentationMode,
		string[] preprocessorSymbols, MetadataReference[] externalReferences, Type[] assemblyTypes,
		LanguageVersion languageVersion = LanguageVersion.CSharp14,
		IReadOnlyDictionary<string, string>? globalOptions = null)
	{
		MockGenerator generator = new();
		CSharpParseOptions parseOptions = new CSharpParseOptions(languageVersion, documentationMode)
			.WithPreprocessorSymbols(preprocessorSymbols);
		SyntaxTree[] syntaxTrees = sources
			.Select(s => CSharpSyntaxTree.ParseText(s, parseOptions))
			.ToArray();

		CSharpCompilation compilation = CSharpCompilation.Create(
			"TestAssembly",
			syntaxTrees,
			[..GetReferences(assemblyTypes), ..externalReferences,],
			new CSharpCompilationOptions(OutputKind.ConsoleApplication));

		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			[generator.AsSourceGenerator(),],
			[],
			parseOptions,
			globalOptions is null ? null : new TestAnalyzerConfigOptionsProvider(globalOptions));
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation,
			out ImmutableArray<Diagnostic> diagnostics);

		ImmutableArray<Diagnostic> compilationDiagnostics = outputCompilation.GetDiagnostics();
		GeneratorDriverRunResult runResult = driver.GetRunResult();
		Dictionary<string, string> generatedSources = runResult.Results
			.SelectMany(runResult => runResult.GeneratedSources)
			.ToDictionary(source => source.HintName, source => source.SourceText.ToString());
		string[] diagnosticMessages =
		[
			..compilationDiagnostics.Where(x => !NoWarn.Contains(x.Id)).Select(ToDiagnosticString),
			..diagnostics.Where(x => !NoWarn.Contains(x.Id)).Select(ToDiagnosticString),
		];
		return new GeneratorResult(generatedSources, diagnosticMessages);
	}

	private static string ToDiagnosticString(Diagnostic d)
	{
		string result = d.ToString();
		if (result.StartsWith("Mockolate.SourceGenerators\\Mockolate.SourceGenerators.MockGenerator\\"))
		{
			result = result["Mockolate.SourceGenerators\\Mockolate.SourceGenerators.MockGenerator\\".Length..];
		}

		return result;
	}

	internal static List<PortableExecutableReference> GetReferences(Type[] types) =>
		AppDomain.CurrentDomain.GetAssemblies()
			.Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
			.Select(x => x.Location)
			.Concat([
				typeof(MockBehavior).Assembly.Location,
				..types.Select(t => t.Assembly.Location),
			])
			.Distinct()
			.Select(loc => MetadataReference.CreateFromFile(loc))
			.ToList();
}
