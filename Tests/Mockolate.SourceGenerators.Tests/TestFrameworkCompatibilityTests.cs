using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

/// <summary>
///     Compiles the generated <c>AutoMockArguments</c> attributes against the oldest version of each test framework that
///     <c>Mockolate.targets</c> generates them for, which the test projects do not use.
/// </summary>
public sealed class TestFrameworkCompatibilityTests
{
	[Test]
	public async Task TUnit_WithOldestSupportedVersion_ShouldCompile()
	{
		string[] diagnostics = Compile(["MOCKOLATE_TEST_FRAMEWORK", "MOCKOLATE_TUNIT",],
			[Path.Combine("tunit.core", "1.0.0", "lib", "netstandard2.0", "TUnit.Core.dll"),],
			"""
			using Mockolate.Integrations.TUnit;

			namespace Compatibility
			{
				public class Tests
				{
					[global::TUnit.Core.Test]
					[AutoMockArguments("Dark")]
					public void Buy(string type, IChocolateDispenser dispenser, [Transient] IChocolateDispenser other) { }
				}
			}
			""");

		await That(diagnostics).IsEmpty();
	}

	[Test]
	public async Task XunitV3_WithOldestSupportedVersion_ShouldCompile()
	{
		string[] diagnostics = Compile(["MOCKOLATE_TEST_FRAMEWORK", "MOCKOLATE_XUNIT3",],
			[
				Path.Combine("xunit.v3.extensibility.core", "1.0.0", "lib", "netstandard2.0", "xunit.v3.core.dll"),
				Path.Combine("xunit.v3.common", "1.0.0", "lib", "netstandard2.0", "xunit.v3.common.dll"),
			],
			"""
			using Mockolate.Integrations.Xunit;

			namespace Compatibility
			{
				public class Tests
				{
					[global::Xunit.Theory]
					[AutoMockArguments("Dark", Skip = "Compiled only")]
					public void Buy(string type, IChocolateDispenser dispenser, [Transient] IChocolateDispenser other) { }
				}
			}
			""");

		await That(diagnostics).IsEmpty();
	}

	[Test]
	public async Task XunitV2_WithOldestSupportedVersion_ShouldCompile()
	{
		const string portable = "portable-net45+win+wpa81+wp80+monotouch+monoandroid+Xamarin.iOS";
		string[] diagnostics = Compile(["MOCKOLATE_TEST_FRAMEWORK", "MOCKOLATE_XUNIT2",],
			[
				Path.Combine("xunit.extensibility.core", "2.0.0", "lib", portable, "xunit.core.dll"),
				Path.Combine("xunit.abstractions", "2.0.0", "lib", portable, "xunit.abstractions.dll"),
			],
			"""
			using Mockolate.Integrations.Xunit;

			namespace Compatibility
			{
				public class Tests
				{
					[global::Xunit.Theory]
					[AutoMockArguments("Dark")]
					public void Buy(string type, IChocolateDispenser dispenser, [Transient] IChocolateDispenser other) { }
				}
			}
			""");

		await That(diagnostics).IsEmpty();
	}

	/// <summary>
	///     Runs the generator on the <paramref name="source" /> with the <paramref name="constants" /> that
	///     <c>Mockolate.targets</c> defines, and returns the errors and warnings of the compilation.
	/// </summary>
	private static string[] Compile(string[] constants, string[] packageFiles, string source)
	{
		string packageRoot = typeof(TestFrameworkCompatibilityTests).Assembly
			.GetCustomAttributes<AssemblyMetadataAttribute>()
			.Single(attribute => attribute.Key == "NuGetPackageRoot").Value!;
		CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.CSharp14)
			.WithPreprocessorSymbols(constants);
		CSharpCompilation compilation = CSharpCompilation.Create("Compatibility",
			[
				CSharpSyntaxTree.ParseText(source, parseOptions),
				CSharpSyntaxTree.ParseText("""
				                           namespace Compatibility
				                           {
				                           	public interface IChocolateDispenser
				                           	{
				                           		bool Dispense(string type);
				                           	}
				                           }
				                           """, parseOptions),
			],
			[
				..GetRuntimeReferences(),
				MetadataReference.CreateFromFile(typeof(MockBehavior).Assembly.Location),
				..packageFiles.Select(file => MetadataReference.CreateFromFile(Path.Combine(packageRoot, file))),
			],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
				nullableContextOptions: NullableContextOptions.Enable));
		CSharpGeneratorDriver.Create([new MockGenerator().AsSourceGenerator(),], [], parseOptions)
			.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation,
				out ImmutableArray<Diagnostic> generatorDiagnostics);
		return outputCompilation.GetDiagnostics()
			.Concat(generatorDiagnostics)
			.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
			.Select(diagnostic => diagnostic.ToString())
			.ToArray();
	}

	/// <summary>
	///     The assemblies of the runtime, without the test frameworks that this test project itself runs on, which are not
	///     in the runtime directory.
	/// </summary>
	private static IEnumerable<MetadataReference> GetRuntimeReferences()
		=> Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")
			.Where(file => Path.GetFileName(file) is { } name &&
			               (name.StartsWith("System.", StringComparison.Ordinal) ||
			                name is "netstandard.dll" or "mscorlib.dll" or "Microsoft.CSharp.dll"))
			.Where(file => !file.EndsWith(".Native.dll", StringComparison.OrdinalIgnoreCase))
			.Select(file => MetadataReference.CreateFromFile(file));
}
