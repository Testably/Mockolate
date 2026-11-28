using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mockolate.SourceGenerators.Sources;

namespace Mockolate.SourceGenerators.Tests.Modes;

public sealed class UnionParameterArgTests
{
	private const string DisableUnionParametersProperty = "build_property.MockolateDisableUnionParameters";
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
	public async Task WithDisablePropertyFalse_WhenBelowCSharp15_ShouldNotEmitParameterArg()
	{
		GeneratorResult result = Generator.Run(Source, LanguageVersion.CSharp14,
			new Dictionary<string, string> { [DisableUnionParametersProperty] = "false", });

		await That(result.Sources.Keys).DoesNotContain(ParameterArgFile);
	}

	[Test]
	[Arguments(LanguageVersion.CSharp15)]
	[Arguments(LanguageVersion.Preview)]
	public async Task WithCSharp15OrLater_ShouldEmitParameterArg(LanguageVersion languageVersion)
	{
		GeneratorResult result = Generator.Run(Source, languageVersion, null);

		await That(result.Sources.Keys).Contains(ParameterArgFile);
		await That(result.Sources[ParameterArgFile])
			.Contains("[global::System.Runtime.CompilerServices.Union]").And
			.Contains("internal readonly struct ParameterArg<T>");
		await That(result.Diagnostics).IsEmpty();
	}

	[Test]
	[Arguments("true")]
	[Arguments("TRUE")]
	[Arguments(" true ")]
	public async Task WithDisablePropertyTrue_ShouldNotEmitParameterArg(string value)
	{
		GeneratorResult result = Generator.Run(Source, LanguageVersion.CSharp15,
			new Dictionary<string, string> { [DisableUnionParametersProperty] = value, });

		await That(result.Sources.Keys).DoesNotContain(ParameterArgFile);
	}

	[Test]
	[Arguments("false")]
	[Arguments("")]
	[Arguments("no")]
	public async Task WithDisablePropertyNotTrue_ShouldEmitParameterArg(string value)
	{
		GeneratorResult result = Generator.Run(Source, LanguageVersion.CSharp15,
			new Dictionary<string, string> { [DisableUnionParametersProperty] = value, });

		await That(result.Sources.Keys).Contains(ParameterArgFile);
	}

	[Test]
	public async Task WhenTheFrameworkDeclaresUnionAttribute_ShouldNotEmitThePolyfill()
	{
		GeneratorResult result = Generator.Run(Source, LanguageVersion.CSharp15, null);

		await That(result.Sources[ParameterArgFile]).DoesNotContain(PolyfillDeclaration);
		await That(result.Diagnostics).IsEmpty();
	}

	[Test]
	[Arguments("public", false, true)]
	[Arguments("internal", true, true)]
	[Arguments("internal", false, false)]
	public async Task HasAttribute_WhenDeclaredInAReference_ShouldDependOnAccessibility(string accessibility,
		bool grantsInternalsVisibleTo, bool expected)
	{
		MetadataReference reference = CompileAttributeAssembly("First", accessibility, grantsInternalsVisibleTo);
		Compilation compilation = CreateCompilation("", reference);

		bool result = MockGenerator.HasAttribute(compilation, AttributeName);

		await That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task HasAttribute_WhenPublicInTwoReferences_ShouldBeMissing()
	{
		Compilation compilation = CreateCompilation("",
			CompileAttributeAssembly("First", "public", false),
			CompileAttributeAssembly("Second", "public", false));

		bool result = MockGenerator.HasAttribute(compilation, AttributeName);

		await That(result).IsFalse()
			.Because("referencing an attribute declared in two references is ambiguous (CS0433)");
	}

	[Test]
	public async Task HasAttribute_WhenDeclaredInSourceAndInAReference_ShouldBeAvailable()
	{
		Compilation compilation = CreateCompilation(AttributeSource("internal"),
			CompileAttributeAssembly("First", "public", false));

		bool result = MockGenerator.HasAttribute(compilation, AttributeName);

		await That(result).IsTrue().Because("the declaration in source wins over imported ones");
	}

	[Test]
	public async Task HasAttribute_WhenEmbeddedInAReference_ShouldBeMissing()
	{
		Compilation compilation = CreateCompilation("", CompileEmbeddedAttributeAssembly("Ext", "MyAttribute"));

		bool result = MockGenerator.HasAttribute(compilation, AttributeName);

		await That(result).IsFalse().Because("the compiler ignores an embedded type of a reference");
	}

	[Test]
	public async Task HasAttribute_WhenEmbeddedInOneReferenceAndPublicInAnother_ShouldBeAvailable()
	{
		Compilation compilation = CreateCompilation("",
			CompileEmbeddedAttributeAssembly("Ext", "MyAttribute"),
			CompileAttributeAssembly("Second", "public", false));

		bool result = MockGenerator.HasAttribute(compilation, AttributeName);

		await That(result).IsTrue();
	}

	[Test]
	[Arguments(false, false, "", true)]
	[Arguments(true, false, "", false)]
	[Arguments(false, true, "", true)]
	[Arguments(true, true, "", true)]
	[Arguments(false, false, ";", true)]
	[Arguments(false, false, "System.Runtime.CompilerServices.ModuleInitializerAttribute", true)]
	[Arguments(false, false, "System.Runtime.CompilerServices.IsExternalInit", false)]
	[Arguments(false, false, " , ", true)]
	[Arguments(false, false,
		"System.Runtime.CompilerServices.IsExternalInit, System.Runtime.CompilerServices.ModuleInitializerAttribute ", true)]
	[Arguments(false, false, "system.runtime.compilerservices.moduleinitializerattribute", false)]
	[Arguments(false, false, null, false)]
	public async Task PolySharpProvidesModuleInitializer_ShouldDecideLikePolySharp(bool hasEmbeddedDeclaration,
		bool hasInaccessibleDeclaration, string? includedTypes, bool expected)
	{
		List<MetadataReference> references = [];
		if (hasEmbeddedDeclaration)
		{
			references.Add(CompileEmbeddedAttributeAssembly("System.Runtime.CompilerServices", "ModuleInitializerAttribute"));
		}

		if (hasInaccessibleDeclaration)
		{
			references.Add(ExternalAssembly.Compile(
				"""
				namespace System.Runtime.CompilerServices
				{
				    internal sealed class ModuleInitializerAttribute : System.Attribute
				    {
				    }
				}
				""", "Inaccessible"));
		}

		// Without the runtime references the compilation has no ModuleInitializerAttribute, like one for .NET Framework.
		Compilation compilation = CSharpCompilation.Create("TestAssembly", [], references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Dictionary<string, string> options = includedTypes is null
			? []
			: new Dictionary<string, string>
			{
				["build_property.PolySharpIncludeGeneratedTypes"] = includedTypes,
				["build_property.PolySharpExcludeGeneratedTypes"] = "",
			};

		bool result = MockGenerator.PolySharpProvidesModuleInitializer(compilation,
			new TestAnalyzerConfigOptionsProvider(options).GlobalOptions);

		await That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task PolySharpProvidesModuleInitializer_WhenPublicAndInaccessibleDeclarationsConflict_ShouldBeFalse()
	{
		const string attribute = """
		                         namespace System.Runtime.CompilerServices
		                         {
		                             {{accessibility}} sealed class ModuleInitializerAttribute : System.Attribute
		                             {
		                             }
		                         }
		                         """;
		Compilation compilation = CSharpCompilation.Create("TestAssembly", [],
			[
				ExternalAssembly.Compile(attribute.Replace("{{accessibility}}", "internal"), "Inaccessible"),
				ExternalAssembly.Compile(attribute.Replace("{{accessibility}}", "public"), "Public"),
			],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Dictionary<string, string> options = new()
		{
			["build_property.PolySharpIncludeGeneratedTypes"] = "",
		};

		bool result = MockGenerator.PolySharpProvidesModuleInitializer(compilation,
			new TestAnalyzerConfigOptionsProvider(options).GlobalOptions);

		await That(result).IsFalse()
			.Because("the single lookup finds no type when two references declare it, so every accessible declaration counts");
	}

	[Test]
	[Arguments(" System.Runtime.CompilerServices.ModuleInitializerAttribute ,", false)]
	[Arguments("system.runtime.compilerservices.moduleinitializerattribute", true)]
	public async Task PolySharpProvidesModuleInitializer_WithExcludedTypes_ShouldCompareOrdinally(string excludedTypes,
		bool expected)
	{
		Compilation compilation = CSharpCompilation.Create("TestAssembly", [], [],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Dictionary<string, string> options = new()
		{
			["build_property.PolySharpIncludeGeneratedTypes"] = "",
			["build_property.PolySharpExcludeGeneratedTypes"] = excludedTypes,
		};

		bool result = MockGenerator.PolySharpProvidesModuleInitializer(compilation,
			new TestAnalyzerConfigOptionsProvider(options).GlobalOptions);

		await That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task HasAttribute_WhenEmbeddedInTheCompilation_ShouldBeAvailable()
	{
		Compilation compilation = CreateCompilation(
			"""
			namespace Microsoft.CodeAnalysis
			{
			    internal sealed partial class EmbeddedAttribute : System.Attribute
			    {
			    }
			}

			namespace Ext
			{
			    [Microsoft.CodeAnalysis.Embedded]
			    internal sealed class MyAttribute : System.Attribute
			    {
			    }
			}
			""");

		bool result = MockGenerator.HasAttribute(compilation, AttributeName);

		await That(result).IsTrue().Because("only embedded types of references are ignored");
	}

	[Test]
	public async Task PolySharpProvidesModuleInitializer_WhenTheRuntimeDeclaresIt_ShouldBeFalse()
	{
		Compilation compilation = CreateCompilation("");
		Dictionary<string, string> options = new()
		{
			["build_property.PolySharpIncludeGeneratedTypes"] = "",
		};

		bool result = MockGenerator.PolySharpProvidesModuleInitializer(compilation,
			new TestAnalyzerConfigOptionsProvider(options).GlobalOptions);

		await That(result).IsFalse();
	}

	[Test]
	public async Task HasAttribute_WhenNotDeclared_ShouldBeMissing()
	{
		Compilation compilation = CreateCompilation("");

		bool result = MockGenerator.HasAttribute(compilation, AttributeName);

		await That(result).IsFalse();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task SourceTemplate_ShouldContainThePolyfillOnlyWhenRequested(bool emitPolyfill)
	{
		string source = SourceTemplates.ParameterArg(emitPolyfill, false, false);

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

	private static MetadataReference CompileEmbeddedAttributeAssembly(string @namespace, string name)
		=> ExternalAssembly.Compile(
			$$"""
			  [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestAssembly")]

			  namespace Microsoft.CodeAnalysis
			  {
			      internal sealed partial class EmbeddedAttribute : System.Attribute
			      {
			      }
			  }

			  namespace {{@namespace}}
			  {
			      [Microsoft.CodeAnalysis.Embedded]
			      internal sealed class {{name}} : System.Attribute
			      {
			      }
			  }
			  """,
			"Embedded");

	private static CSharpCompilation CreateCompilation(string source, params MetadataReference[] references)
		=> CSharpCompilation.Create(
			"TestAssembly",
			[CSharpSyntaxTree.ParseText(source),],
			[..Generator.GetReferences([]), ..references,],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
