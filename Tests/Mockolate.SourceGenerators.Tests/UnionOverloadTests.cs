using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

// The generated code is compiled against the pinned minimum Roslyn, which predates unions; these tests therefore
// check the emitted overload shapes, while the call-site behaviour lives in Mockolate.Tests on net11.0.
public sealed class UnionOverloadTests
{
	private const string MockFile = "Mock.IMyService.g.cs";

	private static readonly Dictionary<string, string> UnionsEnabled = new()
	{
		["build_property.MockolateUnionParameters"] = "true",
	};

	private const string Source = """
	                              #nullable enable
	                              using System;
	                              using Mockolate;

	                              namespace MyCode
	                              {
	                                  public class Program
	                                  {
	                                      public static void Main(string[] args)
	                                      {
	                                          _ = IMyService.CreateMock();
	                                      }
	                                  }

	                                  public interface IMyService
	                                  {
	                                      bool Plain(int value, string text);
	                                      void Register(Func<int, bool> callback);
	                                      int Five(int a, int b, int c, int d, int e);
	                                      void WithParams(int first, params int[] rest);
	                                      T Generic<T>(T value);
	                                      void WithRef(ref int value, string text);
	                                      void WithDefaults(int i = 5, string? s = null);
	                                      bool TakeObject(object? obj);
	                                      int Overloaded(int value);
	                                      int Overloaded(string? value);
	                                      int Mixed(int value);
	                                      int Mixed<T>(T value);
	                                      string this[int key, string name] { get; set; }
	                                      string this[Func<int, bool> selector] { get; }
	                                  }
	                              }
	                              """;

	private static GeneratorResult RunInUnionMode()
		=> Generator.Run([Source,], ["NET11_0_OR_GREATER",], LanguageVersion.Preview, UnionsEnabled);

	private static string GenerateMockInUnionMode()
		=> RunInUnionMode().Sources[MockFile].Replace("\r\n", "\n");

	[Fact]
	public async Task GeneratedCode_ShouldCompile()
	{
		GeneratorResult result = RunInUnionMode();

		await That(result.Diagnostics).IsEmpty();
	}

	[Fact]
	public async Task TwoParameters_ShouldEmitOneOverloadPerUnionOrPredicateAssignment()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains(
				"[global::System.Runtime.CompilerServices.OverloadResolutionPriority(int.MaxValue)]\n\t\tglobal::Mockolate.Setup.IReturnMethodSetupParameterIgnorer<bool, int, string> Plain(global::Mockolate.ParameterArg<int>? value, global::Mockolate.ParameterArg<string>? text);")
			.And
			.Contains(
				"[global::System.Runtime.CompilerServices.OverloadResolutionPriority(1)]\n\t\tglobal::Mockolate.Setup.IReturnMethodSetupParameterIgnorer<bool, int, string> Plain(global::System.Func<int, bool> value, global::Mockolate.ParameterArg<string>? text, [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"value\")] string valueExpression = \"\");")
			.And
			.Contains(
				"[global::System.Runtime.CompilerServices.OverloadResolutionPriority(1)]\n\t\tglobal::Mockolate.Setup.IReturnMethodSetupParameterIgnorer<bool, int, string> Plain(global::Mockolate.ParameterArg<int>? value, global::System.Func<string, bool> text, [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"text\")] string textExpression = \"\");")
			.And
			.Contains(
				"[global::System.Runtime.CompilerServices.OverloadResolutionPriority(0)]\n\t\tglobal::Mockolate.Setup.IReturnMethodSetupParameterIgnorer<bool, int, string> Plain(global::System.Func<int, bool> value, global::System.Func<string, bool> text, [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"value\")] string valueExpression = \"\", [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"text\")] string textExpression = \"\");")
			.And
			.Contains("Plain(global::Mockolate.Parameters.IParameters parameters);").And
			.DoesNotContain("Plain(global::Mockolate.Parameters.IParameter<int>? value").And
			.DoesNotContain("IReturnMethodSetupParameterIgnorer<bool, int, string> Plain(int value, string text)");
	}

	[Fact]
	public async Task SetupImplementation_ShouldUseLiteralFastPathWhenAllArgumentsAreLiterals()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("global::Mockolate.ParameterArg<int> valueArg = value ?? default;").And
			.Contains("if (valueArg.IsLiteral && textArg.IsLiteral)").And
			.Contains(".WithLiteralValues(MockRegistry, \"global::MyCode.IMyService.Plain\", valueArg.Literal!, textArg.Literal!);").And
			.Contains(".WithParameterCollection(MockRegistry, \"global::MyCode.IMyService.Plain\", valueArg.ToParameterMatch(), textArg.ToParameterMatch());").And
			.Contains("(global::Mockolate.Parameters.IParameterMatch<int>)global::Mockolate.It.Satisfies<int>(value, valueExpression)");
	}

	[Fact]
	public async Task Verify_ShouldMirrorTheSetupOverloads()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("global::Mockolate.Verify.VerificationResult<IMockVerifyForIMyService>.IgnoreParameters Plain(global::Mockolate.ParameterArg<int>? value, global::Mockolate.ParameterArg<string>? text);").And
			.Contains("global::Mockolate.Verify.VerificationResult<IMockVerifyForIMyService>.IgnoreParameters Plain(global::System.Func<int, bool> value, global::Mockolate.ParameterArg<string>? text, [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"value\")] string valueExpression = \"\");").And
			.Contains("() => $\"Plain({valueExpression}, {textArg})\"");
	}

	[Fact]
	public async Task DelegateTypedParameter_ShouldOfferTheRawDelegateInsteadOfAPredicate()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("Register(global::Mockolate.ParameterArg<global::System.Func<int, bool>>? callback);").And
			.Contains("Register(global::System.Func<int, bool> callback);").And
			.DoesNotContain("Register(global::System.Func<global::System.Func<int, bool>, bool>");
	}

	[Fact]
	public async Task AboveFourParameters_ShouldEmitOnlyTheAllUnionOverload()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("Five(global::Mockolate.ParameterArg<int>? a, global::Mockolate.ParameterArg<int>? b, global::Mockolate.ParameterArg<int>? c, global::Mockolate.ParameterArg<int>? d, global::Mockolate.ParameterArg<int>? e);").And
			.DoesNotContain("Five(global::System.Func<int, bool>").And
			.DoesNotContain("Five(global::Mockolate.Parameters.IParameter<int>? a");
	}

	[Fact]
	public async Task ParamsGenericAndOverloadedMethods_ShouldKeepTheClassicOverloads()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("WithParams(global::Mockolate.Parameters.IParameter<int>? first, global::Mockolate.Parameters.IParameter<int[]>? rest);").And
			.Contains("WithParams(int first, params int[] rest);").And
			.DoesNotContain("WithParams(global::Mockolate.ParameterArg<int>?").And
			.Contains("Generic<T>(global::Mockolate.Parameters.IParameter<T>? value);").And
			.DoesNotContain("Generic<T>(global::Mockolate.ParameterArg<T>?").And
			.Contains("Overloaded(global::Mockolate.Parameters.IParameter<int>? value);").And
			.Contains("Overloaded(int value);").And
			.Contains("Overloaded(global::Mockolate.Parameters.IParameter<string?>? value);").And
			.Contains("Overloaded(string? value);").And
			.DoesNotContain("Overloaded(global::Mockolate.ParameterArg<").And
			.Contains("Mixed(global::Mockolate.Parameters.IParameter<int>? value);").And
			.Contains("Mixed(int value);").And
			.DoesNotContain("Mixed(global::Mockolate.ParameterArg<");
	}

	[Fact]
	public async Task Indexer_ShouldEmitOneOverloadPerUnionOrPredicateAssignment()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains(
				"[global::System.Runtime.CompilerServices.OverloadResolutionPriority(int.MaxValue)]\n\t\tglobal::Mockolate.Setup.IndexerSetup<string, int, string> this[global::Mockolate.ParameterArg<int>? parameter1, global::Mockolate.ParameterArg<string>? parameter2] { get; }")
			.And
			.Contains(
				"[global::System.Runtime.CompilerServices.OverloadResolutionPriority(1)]\n\t\tglobal::Mockolate.Setup.IndexerSetup<string, int, string> this[global::System.Func<int, bool> parameter1, global::Mockolate.ParameterArg<string>? parameter2, [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"parameter1\")] string parameter1Expression = \"\"] { get; }")
			.And
			.Contains("(parameter1 ?? default).ToParameterMatch(), (parameter2 ?? default).ToParameterMatch());").And
			.Contains("(global::Mockolate.Parameters.IParameterMatch<int>)global::Mockolate.It.Satisfies<int>(parameter1, parameter1Expression)").And
			.Contains("global::Mockolate.Verify.VerificationIndexerResult<IMockVerifyForIMyService, string> this[global::Mockolate.ParameterArg<int>? key, global::Mockolate.ParameterArg<string>? name] { get; }").And
			.Contains("global::Mockolate.Verify.VerificationIndexerResult<IMockVerifyForIMyService, string> this[global::Mockolate.ParameterArg<int>? key, global::System.Func<string, bool> name, [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"name\")] string nameExpression = \"\"] { get; }").And
			.Contains("(object?)(key ?? default), (object?)nameExpression));").And
			.DoesNotContain("this[global::Mockolate.Parameters.IParameter<int>? parameter1").And
			.DoesNotContain("this[int parameter1, string parameter2]");
	}

	[Fact]
	public async Task Indexer_WithDelegateTypedKey_ShouldOfferTheRawDelegateAsLiteral()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("this[global::Mockolate.ParameterArg<global::System.Func<int, bool>>? parameter1] { get; }").And
			.Contains("this[global::System.Func<int, bool> parameter1] { get; }").And
			.Contains("(global::Mockolate.Parameters.IParameterMatch<global::System.Func<int, bool>>)global::Mockolate.It.IsValue<global::System.Func<int, bool>>(parameter1)").And
			.DoesNotContain("this[global::System.Func<global::System.Func<int, bool>, bool>");
	}

	[Fact]
	public async Task Indexers_WithTheSameKeyCount_ShouldKeepTheClassicOverloads()
	{
		const string source = """
		                      #nullable enable
		                      using Mockolate;

		                      namespace MyCode
		                      {
		                          public class Program
		                          {
		                              public static void Main(string[] args)
		                              {
		                                  _ = IOverloadedIndexers.CreateMock();
		                              }
		                          }

		                          public interface IOverloadedIndexers
		                          {
		                              string this[int i] { get; }
		                              string this[long l] { get; }
		                              string this[int a, int b] { get; }
		                          }
		                      }
		                      """;

		GeneratorResult result = Generator.Run([source,], ["NET11_0_OR_GREATER",], LanguageVersion.Preview,
			UnionsEnabled);
		string mock = result.Sources["Mock.IOverloadedIndexers.g.cs"];

		await That(result.Diagnostics).IsEmpty();
		await That(mock)
			.Contains("this[global::Mockolate.Parameters.IParameter<int>? parameter1] { get; }").And
			.Contains("this[int parameter1] { get; }").And
			.Contains("this[global::Mockolate.Parameters.IParameter<long>? parameter1] { get; }").And
			.DoesNotContain("this[global::Mockolate.ParameterArg<int>? parameter1]").And
			.DoesNotContain("this[global::Mockolate.ParameterArg<long>? parameter1]").And
			.Contains("this[global::Mockolate.ParameterArg<int>? parameter1, global::Mockolate.ParameterArg<int>? parameter2] { get; }");
	}

	[Fact]
	public async Task IParametersOverload_ShouldKeepItsPriorityAboveTheUnionOverloads()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains(
				"[global::System.Runtime.CompilerServices.OverloadResolutionPriority(int.MaxValue - 1)]\n\t\tglobal::Mockolate.Setup.IReturnMethodSetupWithCallback<bool, object?> TakeObject(global::Mockolate.Parameters.IParameters parameters);");
	}

	[Fact]
	public async Task RefParameter_ShouldStayAMatcherSlot()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("WithRef(global::Mockolate.Parameters.IRefParameter<int> value, global::Mockolate.ParameterArg<string>? text);").And
			.Contains("WithRef(global::Mockolate.Parameters.IRefParameter<int> value, global::System.Func<string, bool> text, [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"text\")] string textExpression = \"\");");
	}

	[Fact]
	public async Task OptionalParameters_ShouldFallBackToTheDeclaredDefault()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("WithDefaults(global::Mockolate.ParameterArg<int>? i = null, global::Mockolate.ParameterArg<string?>? s = null);").And
			.Contains("global::Mockolate.ParameterArg<int> iArg = i ?? new global::Mockolate.ParameterArg<int>((int)(5));").And
			.Contains("global::Mockolate.ParameterArg<string?> sArg = s ?? new global::Mockolate.ParameterArg<string?>((string?)(null));").And
			.Contains("global::Mockolate.ParameterArg<int> valueArg = value ?? default;");
	}

	[Fact]
	public async Task ObjectParameter_ShouldNotOutrankTheIParametersOverload()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains(
				"[global::System.Runtime.CompilerServices.OverloadResolutionPriority(1)]\n\t\tglobal::Mockolate.Setup.IReturnMethodSetupParameterIgnorer<bool, object?> TakeObject(global::Mockolate.ParameterArg<object?>? obj);")
			;
	}

	[Fact]
	public async Task WithoutUnionSupport_ShouldEmitTheClassicOverloads()
	{
		GeneratorResult result = Generator.Run([Source,], [], LanguageVersion.CSharp14, null);
		string mock = result.Sources[MockFile];

		await That(mock)
			.Contains("Plain(global::Mockolate.Parameters.IParameter<int>? value, global::Mockolate.Parameters.IParameter<string>? text);").And
			.Contains("Plain(int value, string text);").And
			.DoesNotContain("ParameterArg");
		await That(result.Sources.Keys).DoesNotContain("ParameterArg.g.cs");
	}
}
