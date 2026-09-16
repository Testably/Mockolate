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
		"CS8321" /* The local function is declared but never used */,
		"CS1061" /* 'type' does not contain a definition for 'name' and no accessible extension method 'name' accepting a first argument of type 'type' could be found (are you missing a using directive or an assembly reference?). */,
		// TODO: Remove the following errors when tests work with extension syntax
		"CS0106" /* The modifier 'public' is not valid for this item */,
		"CS0116" /* A namespace cannot directly contain members such as fields or methods */,
		"CS1520" /* Method must have a return type */,
		"CS0710" /* Static classes cannot have instance constructors */,
		"CS1503" /* cannot convert from ... */,
		"CS1513" /* } expected */,
		"CS1001" /* Identifier expected */,
		"CS1002" /* ; expected */,
		"CS1026" /* ) expected */,
		"CS1519" /* Invalid token '(' in class, record, � */,
		"CS0501" /*  */,
		"CS8124" /* Tuple must contain at least two elem� */,
		"CS1003" /* Syntax error, ',' expected */,
		"CS0119" /* 'identifier' is a type, which is not valid in the given context */,
		"CS0128" /* A local variable named 'variable' is already defined in this scope */,
		"CS0246" /* A namespace cannot directly contain members such as fields or methods */,
		"CS0305" /* Using the generic type 'type' requires 1 type arguments */,
		"CS8803" /* Top-level statements must precede namespace and type declarations */,
		"CS1022" /* Type or namespace definition, or end-of-file expected */,
		"CS0708" /* 'member': cannot declare instance members in a static class */,
		"CS0103" /* The name 'name' does not exist in the current context */,
		"CS7022" /* The entry point of the program is global code */,
		"CS8802" /* Only one compilation unit can have top-level statements */,
		"CS0102" /* The type '<invalid-global-code>' already con*/,
		"CS8801" /* Cannot use local variable or local function*/,
		"CS0111" /* Type '<invalid-global-code>' already defines a */,
		"CS0721" /* 'Mock': static types cannot be us */,
		"CS0759" /* No defining declaration found for implementing declaration of partial method. */,
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
		LanguageVersion languageVersion = LanguageVersion.Latest,
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
