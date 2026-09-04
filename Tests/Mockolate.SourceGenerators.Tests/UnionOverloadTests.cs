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
	                                  }
	                              }
	                              """;

	private static GeneratorResult RunInUnionMode()
		=> Generator.Run([Source,], ["NET11_0_OR_GREATER",], LanguageVersion.Preview, UnionsEnabled);

	private static string GenerateMockInUnionMode()
		=> RunInUnionMode().Sources[MockFile].Replace("\r\n", "\n");

	[Test]
	public async Task GeneratedCode_ShouldCompile()
	{
		GeneratorResult result = RunInUnionMode();

		await That(result.Diagnostics).IsEmpty();
	}

	[Test]
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

	[Test]
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

	[Test]
	public async Task Verify_ShouldMirrorTheSetupOverloads()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("global::Mockolate.Verify.VerificationResult<IMockVerifyForIMyService>.IgnoreParameters Plain(global::Mockolate.ParameterArg<int>? value, global::Mockolate.ParameterArg<string>? text);").And
			.Contains("global::Mockolate.Verify.VerificationResult<IMockVerifyForIMyService>.IgnoreParameters Plain(global::System.Func<int, bool> value, global::Mockolate.ParameterArg<string>? text, [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"value\")] string valueExpression = \"\");").And
			.Contains("() => $\"Plain({valueExpression}, {textArg})\"");
	}

	[Test]
	public async Task DelegateTypedParameter_ShouldOfferTheRawDelegateInsteadOfAPredicate()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("Register(global::Mockolate.ParameterArg<global::System.Func<int, bool>>? callback);").And
			.Contains("Register(global::System.Func<int, bool> callback);").And
			.DoesNotContain("Register(global::System.Func<global::System.Func<int, bool>, bool>");
	}

	[Test]
	public async Task AboveFourParameters_ShouldEmitOnlyTheAllUnionOverload()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("Five(global::Mockolate.ParameterArg<int>? a, global::Mockolate.ParameterArg<int>? b, global::Mockolate.ParameterArg<int>? c, global::Mockolate.ParameterArg<int>? d, global::Mockolate.ParameterArg<int>? e);").And
			.DoesNotContain("Five(global::System.Func<int, bool>").And
			.DoesNotContain("Five(global::Mockolate.Parameters.IParameter<int>? a");
	}

	[Test]
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

	[Test]
	public async Task IParametersOverload_ShouldKeepItsPriorityAboveTheUnionOverloads()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains(
				"[global::System.Runtime.CompilerServices.OverloadResolutionPriority(int.MaxValue - 1)]\n\t\tglobal::Mockolate.Setup.IReturnMethodSetupWithCallback<bool, object?> TakeObject(global::Mockolate.Parameters.IParameters parameters);");
	}

	[Test]
	public async Task RefParameter_ShouldStayAMatcherSlot()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("WithRef(global::Mockolate.Parameters.IRefParameter<int> value, global::Mockolate.ParameterArg<string>? text);").And
			.Contains("WithRef(global::Mockolate.Parameters.IRefParameter<int> value, global::System.Func<string, bool> text, [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"text\")] string textExpression = \"\");");
	}

	[Test]
	public async Task OptionalParameters_ShouldFallBackToTheDeclaredDefault()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains("WithDefaults(global::Mockolate.ParameterArg<int>? i = null, global::Mockolate.ParameterArg<string?>? s = null);").And
			.Contains("global::Mockolate.ParameterArg<int> iArg = i ?? new global::Mockolate.ParameterArg<int>((int)(5));").And
			.Contains("global::Mockolate.ParameterArg<string?> sArg = s ?? new global::Mockolate.ParameterArg<string?>((string?)(null));").And
			.Contains("global::Mockolate.ParameterArg<int> valueArg = value ?? default;");
	}

	[Test]
	public async Task ObjectParameter_ShouldNotOutrankTheIParametersOverload()
	{
		string mock = GenerateMockInUnionMode();

		await That(mock)
			.Contains(
				"[global::System.Runtime.CompilerServices.OverloadResolutionPriority(1)]\n\t\tglobal::Mockolate.Setup.IReturnMethodSetupParameterIgnorer<bool, object?> TakeObject(global::Mockolate.ParameterArg<object?>? obj);")
			;
	}

	[Test]
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
