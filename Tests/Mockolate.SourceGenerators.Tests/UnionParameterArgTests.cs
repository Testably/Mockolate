using System.Collections.Generic;
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

	[Fact]
	public async Task WithoutUnionSupport_ShouldNotEmitParameterArg()
	{
		GeneratorResult result = Generator.Run(Source, LanguageVersion.CSharp14, null);

		await That(result.Sources.Keys).DoesNotContain(ParameterArgFile);
		await That(result.Sources.Values).None().Satisfy(x => x!.Contains("ParameterArg"));
	}

	[Fact]
	public async Task WithPreviewLanguageVersion_WithoutProperty_ShouldFollowCompilerCapability()
	{
		bool compilerShipsCSharp15 = Enum.TryParse("CSharp15", out LanguageVersion _);

		GeneratorResult result = Generator.Run(Source, LanguageVersion.Preview, null);

		await That(result.Sources.ContainsKey(ParameterArgFile)).IsEqualTo(compilerShipsCSharp15);
	}

	[Theory]
	[InlineData("false")]
	[InlineData("False")]
	[InlineData("no")]
	public async Task WithPropertyNotTrue_ShouldNotEmitParameterArg(string value)
	{
		GeneratorResult result = Generator.Run(Source, LanguageVersion.Preview,
			new Dictionary<string, string> { [UnionParametersProperty] = value, });

		await That(result.Sources.Keys).DoesNotContain(ParameterArgFile);
	}

	[Theory]
	[InlineData("true")]
	[InlineData("TRUE")]
	[InlineData(" true ")]
	public async Task WithPropertyTrue_ShouldEmitParameterArg_EvenBelowCSharp15(string value)
	{
		GeneratorResult result = Generator.Run(Source, LanguageVersion.CSharp14,
			new Dictionary<string, string> { [UnionParametersProperty] = value, });

		await That(result.Sources.Keys).Contains(ParameterArgFile);
		await That(result.Sources[ParameterArgFile])
			.Contains("[global::System.Runtime.CompilerServices.Union]").And
			.Contains("internal readonly struct ParameterArg<T>");
		await That(result.Diagnostics).IsEmpty();
	}

	[Fact]
	public async Task WhenTheFrameworkDeclaresUnionAttribute_ShouldNotEmitThePolyfill()
	{
		// The test compilation references the current runtime, which ships the attribute.
		GeneratorResult result = Generator.Run(Source, LanguageVersion.Preview,
			new Dictionary<string, string> { [UnionParametersProperty] = "true", });

		await That(result.Sources[ParameterArgFile]).DoesNotContain(PolyfillDeclaration);
		await That(result.Diagnostics).IsEmpty();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ParameterArgSource_ShouldContainThePolyfillOnlyWhenRequested(bool emitPolyfill)
	{
		string source = Sources.Sources.ParameterArg(emitPolyfill, false, false);

		await That(source.Contains(PolyfillDeclaration)).IsEqualTo(emitPolyfill);
		await That(source).Contains("internal readonly struct ParameterArg<T>");
	}
}
