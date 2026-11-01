using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

// These tests compile against the pinned minimum Roslyn (MinimumRoslynVersion), which predates unions, so they
// can only check the emission and the plain C# validity of the generated struct. The union conversions themselves
// are covered by Mockolate.Tests on net11.0 with the SDK compiler.
public sealed class UnionParameterArgTests
{
	private const string UnionParametersProperty = "build_property.MockolateUnionParameters";
	private const string ParameterArgFile = "ParameterArg.g.cs";
	private const string PolyfillDeclaration = "internal sealed class UnionAttribute : global::System.Attribute";
	private const string AttributeName = "Ext.MyAttribute";

	private const string Source = """
	                              using System;
	                              using Mockolate;

	                              namespace MyCode
	                              {
	                                  public class Program
	                                  {
	                                      public static void Main(string[] args)
	                                      {
	                                          _ = IMyInterface.CreateMock();
	                                      }
	                                  }

	                                  public interface IMyInterface
	                                  {
	                                      bool MyFunc(int value);
	                                  }
	                              }
	                              """;

	[Test]
	public async Task WithoutUnionSupport_ShouldNotEmitParameterArg()
	{
		GeneratorResult result = Generator.Run(Source, LanguageVersion.CSharp14, null);

		await That(result.Sources.Keys).DoesNotContain(ParameterArgFile);
		await That(result.Sources.Values).None().Satisfy(x => x!.Contains("ParameterArg"));
	}

	[Test]
	public async Task WithPreviewLanguageVersion_WithoutProperty_ShouldFollowCompilerCapability()
	{
		bool compilerShipsCSharp15 = Enum.TryParse("CSharp15", out LanguageVersion _);

		GeneratorResult result = Generator.Run(Source, LanguageVersion.Preview, null);

		await That(result.Sources.ContainsKey(ParameterArgFile)).IsEqualTo(compilerShipsCSharp15);
	}

	[Test]
	[Arguments("false")]
	[Arguments("False")]
	[Arguments("no")]
	public async Task WithPropertyNotTrue_ShouldNotEmitParameterArg(string value)
	{
		GeneratorResult result = Generator.Run(Source, LanguageVersion.Preview,
			new Dictionary<string, string> { [UnionParametersProperty] = value, });

		await That(result.Sources.Keys).DoesNotContain(ParameterArgFile);
	}

	[Test]
	[Arguments("true")]
	[Arguments("TRUE")]
	[Arguments(" true ")]
	public async Task WithPropertyTrue_ShouldEmitParameterArg(string value)
	{
		GeneratorResult result = Generator.Run(Source, LanguageVersion.Preview,
			new Dictionary<string, string> { [UnionParametersProperty] = value, });

		await That(result.Sources.Keys).Contains(ParameterArgFile);
		await That(result.Sources[ParameterArgFile])
			.Contains("[global::System.Runtime.CompilerServices.Union]").And
			.Contains("internal readonly struct ParameterArg<T>");
		await That(result.Diagnostics).IsEmpty();
	}

	[Test]
	public async Task WithPropertyTrue_BelowCSharp15_ShouldNotEmitParameterArg()
	{
		GeneratorResult result = Generator.Run(Source, LanguageVersion.CSharp14,
			new Dictionary<string, string> { [UnionParametersProperty] = "true", });

		await That(result.Sources.Keys).DoesNotContain(ParameterArgFile)
			.Because("union conversions fail with CS8652 below C# 15, so the union overloads would not be callable");
	}

	[Test]
	public async Task WhenTheFrameworkDeclaresUnionAttribute_ShouldNotEmitThePolyfill()
	{
		// The test compilation references the current runtime, which ships the attribute.
		GeneratorResult result = Generator.Run(Source, LanguageVersion.Preview,
			new Dictionary<string, string> { [UnionParametersProperty] = "true", });

		await That(result.Sources[ParameterArgFile]).DoesNotContain(PolyfillDeclaration);
		await That(result.Diagnostics).IsEmpty();
	}

	[Test]
	[Arguments("public", false, true)]
	[Arguments("internal", true, true)]
	[Arguments("internal", false, false)]
	public async Task HasAttribute_DeclaredInAReference_ShouldDependOnAccessibility(string accessibility,
		bool grantsInternalsVisibleTo, bool expected)
	{
		MetadataReference reference = CompileAttributeAssembly("First", accessibility, grantsInternalsVisibleTo);
		Compilation compilation = CreateCompilation("", reference);

		bool result = MockGenerator.HasAttribute(compilation, AttributeName);

		await That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task HasAttribute_PublicInTwoReferences_ShouldBeMissing()
	{
		Compilation compilation = CreateCompilation("",
			CompileAttributeAssembly("First", "public", false),
			CompileAttributeAssembly("Second", "public", false));

		bool result = MockGenerator.HasAttribute(compilation, AttributeName);

		await That(result).IsFalse()
			.Because("referencing an attribute declared in two references is ambiguous (CS0433)");
	}

	[Test]
	public async Task HasAttribute_DeclaredInSourceAndInAReference_ShouldBeAvailable()
	{
		Compilation compilation = CreateCompilation(AttributeSource("internal"),
			CompileAttributeAssembly("First", "public", false));

		bool result = MockGenerator.HasAttribute(compilation, AttributeName);

		await That(result).IsTrue().Because("the declaration in source wins over imported ones");
	}

	[Test]
	public async Task HasAttribute_NotDeclared_ShouldBeMissing()
	{
		Compilation compilation = CreateCompilation("");

		bool result = MockGenerator.HasAttribute(compilation, AttributeName);

		await That(result).IsFalse();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task ParameterArgSource_ShouldContainThePolyfillOnlyWhenRequested(bool emitPolyfill)
	{
		string source = Sources.Sources.ParameterArg(emitPolyfill, false, false);

		await That(source.Contains(PolyfillDeclaration)).IsEqualTo(emitPolyfill);
		await That(source).Contains("internal readonly struct ParameterArg<T>");
	}

	private static string AttributeSource(string accessibility)
		=> $$"""
		     namespace Ext
		     {
		         {{accessibility}} sealed class MyAttribute : System.Attribute
		         {
		         }
		     }
		     """;

	private static MetadataReference CompileAttributeAssembly(string assemblyName, string accessibility,
		bool grantsInternalsVisibleTo)
		=> ExternalAssembly.Compile(
			(grantsInternalsVisibleTo
				? """[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestAssembly")]"""
				: "") + Environment.NewLine + AttributeSource(accessibility),
			assemblyName);

	private static Compilation CreateCompilation(string source, params MetadataReference[] references)
		=> CSharpCompilation.Create(
			"TestAssembly",
			[CSharpSyntaxTree.ParseText(source),],
			[..Generator.GetReferences([]), ..references,],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
