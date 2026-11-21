using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.MatcherAsValueAnalyzer>;

namespace Mockolate.Analyzers.Tests;

// The pinned test compiler predates C# unions, so the stand-in ParameterArg<T> converts values with an implicit operator.
public class MatcherAsValueAnalyzerTests
{
	private const string ValueHint =
		"use a matcher for 'object', or It.Is<object>(…) if the matcher itself is the expected value";

	private const string OtherArgumentsHint =
		"pass matchers of the parameter types for the other arguments as well, for example It.Is(…)";

	private const string ParamsArrayAndOtherArgumentsHint =
		"pass the elements as separate arguments instead of an array, and matchers of the parameter types for the other arguments as well, for example It.Is(…)";

	private const string ParametersHint =
		"use a matcher for 'object', such as It.IsAny<object>(); a parameters match only applies as the only argument of a method";

	private const string NoMatcherOverloadHint = "no overload of this member takes it as a matcher";

	private const string Declarations = """
	                                    #nullable enable
	                                    using System;
	                                    using Mockolate;
	                                    using Mockolate.Parameters;

	                                    namespace Mockolate
	                                    {
	                                        public readonly struct ParameterArg<T>
	                                        {
	                                            public ParameterArg(T value) { }
	                                            public static implicit operator ParameterArg<T>(T value) => new(value);
	                                        }

	                                        internal static partial class Mock
	                                        {
	                                            public interface IMockVerifyForFoo
	                                            {
	                                                int Take(object value);
	                                                int Take(IParameter<object> value);
	                                                int Take(IParameters parameters);
	                                                int Pair(object value, int count);
	                                                int Pair(IParameters parameters);
	                                                int Dyn(dynamic value);
	                                                int Dyn(IParameter<object> value);
	                                                int Typed(IParameter<int> value);
	                                                int Five(int a, int b, int c, int d, object e);
	                                                int Five(IParameter<int> a, IParameter<int> b, IParameter<int> c, IParameter<int> d, IParameter<object> e);
	                                                int Log(int level, params object[] args);
	                                                int Log(IParameter<int> level, params IParameter<object>[] args);
	                                                int Gen<T>(int level, params T[] args);
	                                                int Gen<T>(IParameter<int> level, params IParameter<T>[] args);
	                                                int Plain<T>(T value);
	                                                [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
	                                                int Plain<T>(IParameter<T>? value);
	                                                int Plain<T>(IParameters parameters);
	                                                int GenOnly<T>(params T[] args);
	                                                int Only(params object[] args);
	                                                int Only(params IParameter<object>?[] args);
	                                                int Optional(IComparable value, int count = 3);
	                                                int Comparables(params IComparable[] values);
	                                                int Mixed(object value, IParameter<int>? count);
	                                                int Constrained<T>(T value) where T : IComparable;
	                                                int ValueType<T>(T value) where T : struct;
	                                                int Dependent<T, TBase>(T value) where T : TBase;
	                                                int Union(ParameterArg<Flavor>? value);
	                                                int this[object key] { get; }
	                                                int this[object a, int b] { get; }
	                                                int this[IParameter<object> key] { get; }
	                                            }

	                                            public interface IMockSetupForFoo
	                                            {
	                                                int Take(object value);
	                                                int Take(IParameter<object> value);
	                                            }

	                                            public interface IMockRaiseOnFoo
	                                            {
	                                                void Changed(object value);
	                                            }

	                                            public interface IMockRaiseOnIVerifyForm
	                                            {
	                                                void Changed(object value);
	                                            }

	                                            public interface IMockStaticVerifyForFoo
	                                            {
	                                                int Take(object value);
	                                                int Take(IParameter<object> value);
	                                            }
	                                        }
	                                    }

	                                    namespace System.Runtime.CompilerServices
	                                    {
	                                        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property)]
	                                        public sealed class OverloadResolutionPriorityAttribute(int priority) : Attribute
	                                        {
	                                            public int Priority => priority;
	                                        }
	                                    }

	                                    namespace Mockolate.Verify
	                                    {
	                                        public class PropertyResult<T>
	                                        {
	                                            public int Set(IParameter<T> value) => 0;
	                                            [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
	                                            public int Set(T value,
	                                                [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))]
	                                                string doNotPopulateThisValue = "") => 0;
	                                        }
	                                    }

	                                    public class Other
	                                    {
	                                        public static void Take(object value) { }
	                                    }

	                                    public class Flavor { }

	                                    public class IntMatcher : Flavor, IParameter<int>
	                                    {
	                                        public bool Matches(object? value) => true;
	                                        public void InvokeCallbacks(object? value) { }
	                                    }

	                                    public class ComparableMatcher : IComparable, IParameter<IComparable>
	                                    {
	                                        public int CompareTo(object? obj) => 0;
	                                        public bool Matches(object? value) => true;
	                                        public void InvokeCallbacks(object? value) { }
	                                    }

	                                    public class FlavorMatcher : Flavor, IParameter<Flavor>
	                                    {
	                                        public bool Matches(object? value) => true;
	                                        public void InvokeCallbacks(object? value) { }
	                                    }

	                                    public struct StructMatcher : IParameter<StructMatcher>
	                                    {
	                                        public bool Matches(object? value) => true;
	                                        public void InvokeCallbacks(object? value) { }
	                                    }

	                                    """;

	[Test]
	[Arguments("verify.Take({|#0:It.IsAny<int>()|});", "It.IsAny<int>()")]
	[Arguments("verify.Take(value: {|#0:It.IsAny<int>()|});", "It.IsAny<int>()")]
	[Arguments("setup.Take({|#0:It.IsAny<int>()|});", "It.IsAny<int>()")]
	[Arguments("staticVerify.Take({|#0:It.IsAny<int>()|});", "It.IsAny<int>()")]
	[Arguments("_ = verify[{|#0:It.IsAny<int>()|}];", "It.IsAny<int>()")]
	[Arguments("verify.Plain<object>({|#0:It.IsAny<int>()|});", "It.IsAny<int>()")]
	[Arguments("verify?.Plain<object>({|#0:It.IsAny<int>()|});", "It.IsAny<int>()")]
	[Arguments("property.Set({|#0:It.IsAny<int>()|});", "It.IsAny<int>()")]
	[Arguments("verify.Only(new object[] { {|#0:It.IsAny<int>()|} });", "It.IsAny<int>()")]
	[Arguments("verify.Log(1, new[] { {|#0:It.IsAny<int>()|} });", "It.IsAny<int>()")]
	[Arguments("verify.Log(1, [{|#0:It.IsAny<int>()|}]);", "It.IsAny<int>()")]
	public async Task WhenMatcherIsNoMatcherForObject_ShouldBeFlagged(string call, string matcher)
		=> await VerifyAsync(call,
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments(matcher, "object", ValueHint));

	[Test]
	public async Task WhenMatcherBindsToADynamicValue_ShouldBeFlagged() => await VerifyAsync(
		"verify.Dyn({|#0:It.IsAny<int>()|});",
		Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
			.WithLocation(0)
			.WithArguments("It.IsAny<int>()", "dynamic",
				"use a matcher for 'dynamic', or It.Is<dynamic>(…) if the matcher itself is the expected value"));

	[Test]
	[Arguments("verify.Five(1, 2, 3, 4, {|#0:It.IsAny<object>()|});", "It.IsAny<object>()")]
	[Arguments("verify.Log(1, \"a\", {|#0:It.IsAny<object>()|});", "It.IsAny<object>()")]
	[Arguments("verify.Log(1, {|#0:It.IsAny<object[]>()|});", "It.IsAny<object[]>()")]
	[Arguments("verify.Gen<object>(1, {|#0:It.IsAny<object>()|});", "It.IsAny<object>()")]
	public async Task WhenMatcherForObjectIsMixedWithValues_ShouldSuggestMatchersForTheOtherArguments(string call,
		string matcher)
		=> await VerifyAsync(call,
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments(matcher, "object", OtherArgumentsHint));

	[Test]
	[Arguments("verify.Log(1, new object[] { {|#0:It.IsAny<object>()|} });")]
	[Arguments("verify.Only(new object[] { \"a\", {|#0:It.IsAny<object>()|} });")]
	[Arguments("verify.Only([\"a\", {|#0:It.IsAny<object>()|}]);")]
	public async Task WhenMatcherInAnExplicitParamsArrayIsMixedWithValues_ShouldSuggestBothFixes(string call)
		=> await VerifyAsync(call,
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments("It.IsAny<object>()", "object", ParamsArrayAndOtherArgumentsHint));

	[Test]
	[Arguments("verify.Gen(1, {|#0:It.IsAny<object>()|});")]
	[Arguments("verify?.Gen(1, {|#0:It.IsAny<object>()|});")]
	public async Task WhenTheTypeArgumentIsInferredFromAMatcherNextToValues_ShouldSuggestMatchersForTheOtherArguments(
		string call)
		=> await VerifyAsync(call,
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments("It.IsAny<object>()", "IParameterWithCallback<object>", OtherArgumentsHint));

	[Test]
	public async Task WhenTheTypeArgumentIsInferredFromAMatcherWithoutValues_ShouldSayThatNoOverloadTakesIt()
		=> await VerifyAsync("verify.GenOnly({|#0:It.IsAny<object>()|});",
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments("It.IsAny<object>()", "IParameterWithCallback<object>", NoMatcherOverloadHint));

	[Test]
	public async Task WhenTheTypeArgumentIsAnExplicitMatcherType_ShouldSuggestAMatcherForIt()
		=> await VerifyAsync("verify.Plain<IParameter<int>>({|#0:It.IsAny<int>()|});",
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments("It.IsAny<int>()", "IParameter<int>",
					"use a matcher for 'IParameter<int>', or It.Is<IParameter<int>>(…) if the matcher itself is the expected value"));

	[Test]
	[Arguments("verify.Pair({|#0:Match.AnyParameters()|}, 1);")]
	[Arguments("verify.Log(1, {|#0:Match.AnyParameters()|});")]
	[Arguments("_ = verify[{|#0:Match.AnyParameters()|}];")]
	[Arguments("property.Set({|#0:Match.AnyParameters()|});")]
	public async Task WhenAParametersMatchIsComparedAsAValue_ShouldBeFlagged(string call)
		=> await VerifyAsync(call,
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments("Match.AnyParameters()", "object", ParametersHint));

	[Test]
	public async Task WhenAParametersMatchWithAPredicateIsComparedAsAValue_ShouldBeFlagged() => await VerifyAsync(
		"verify.Pair({|#0:Match.Parameters(p => true)|}, 1);",
		Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
			.WithLocation(0)
			.WithArguments("Match.Parameters(p => true)", "object", ParametersHint));

	[Test]
	public async Task WhenMatcherForObjectIsInAnExplicitParamsArray_ShouldSuggestSeparateArguments()
		=> await VerifyAsync("verify.Only(new object[] { {|#0:It.IsAny<object>()|} });",
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments("It.IsAny<object>()", "object",
					"pass the elements as separate arguments instead of an array"));

	[Test]
	public async Task WhenSeveralElementMatchersAreMixedWithValues_ShouldFlagEachMatcher() => await VerifyAsync(
		"verify.Log(1, {|#0:It.IsAny<object>()|}, {|#1:It.Is<object>(\"a\")|});",
		Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
			.WithLocation(0)
			.WithArguments("It.IsAny<object>()", "object", OtherArgumentsHint),
		Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
			.WithLocation(1)
			.WithArguments("It.Is<object>(\"a\")", "object", OtherArgumentsHint));

	[Test]
	public async Task WhenMatcherBindsToTheValueCaseOfAUnion_ShouldBeFlagged() => await VerifyAsync(
		"verify.Union({|#0:new IntMatcher()|});",
		Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
			.WithLocation(0)
			.WithArguments("new IntMatcher()", "Flavor",
				"use a matcher for 'Flavor', or It.Is<Flavor>(…) if the matcher itself is the expected value"));

	[Test]
	public async Task WhenMatcherForAnotherTypeIsMixedWithValues_ShouldSuggestAMatcherForTheValueTypeFirst()
		=> await VerifyAsync(
			"verify.Log(1, {|#0:It.IsAny<int>()|});",
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments("It.IsAny<int>()", "object", ValueHint));

	[Test]
	public async Task WhenAMatcherForAnotherTypeBlocksTheOtherMatchers_ShouldSuggestMatchersForTheOtherArguments()
		=> await VerifyAsync(
			"verify.Only({|#0:It.IsAny<object>()|}, {|#1:It.IsAny<int>()|});",
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments("It.IsAny<object>()", "object", OtherArgumentsHint),
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(1)
				.WithArguments("It.IsAny<int>()", "object", ValueHint));

	[Test]
	public async Task WhenAnIndexerKeyIsMixedWithValues_ShouldSuggestMatchersForTheOtherArguments() => await VerifyAsync(
		"_ = verify[{|#0:It.IsAny<object>()|}, 2];",
		Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
			.WithLocation(0)
			.WithArguments("It.IsAny<object>()", "object", OtherArgumentsHint));

	[Test]
	public async Task WhenOnlyADefaultValueIsNextToTheMatcher_ShouldNotSuggestMatchersForTheOtherArguments()
		=> await VerifyAsync(
			"verify.Optional({|#0:new ComparableMatcher()|});",
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments("new ComparableMatcher()", "IComparable",
					"cast it to IParameter<IComparable> to pass it as a matcher"));

	[Test]
	[Arguments("verify.Constrained<IComparable>({|#0:new ComparableMatcher()|});", "new ComparableMatcher()",
		"IComparable")]
	[Arguments("verify.ValueType<StructMatcher>({|#0:new StructMatcher()|});", "new StructMatcher()",
		"StructMatcher")]
	[Arguments("verify.Comparables({|#0:new ComparableMatcher()|});", "new ComparableMatcher()", "IComparable")]
	[Arguments("structProperty.Set({|#0:new StructMatcher()|});", "new StructMatcher()", "StructMatcher")]
	public async Task WhenTheMatcherIsAlsoAValue_ShouldSuggestACast(string call, string matcher, string type)
		=> await VerifyAsync(call,
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments(matcher, type, $"cast it to IParameter<{type}> to pass it as a matcher"));

	[Test]
	[Arguments("verify.GenOnly<object>({|#0:It.IsAny<object>()|});")]
	[Arguments("verify.Mixed({|#0:It.IsAny<object>()|}, It.IsAny<int>());")]
	[Arguments("verify.Mixed({|#0:It.IsAny<object>()|}, null);")]
	public async Task WhenNoSpellingBindsTheMatcherAsAMatcher_ShouldSaySo(string call) => await VerifyAsync(call,
		Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
			.WithLocation(0)
			.WithArguments("It.IsAny<object>()", "object", "no overload of this member takes it as a matcher"));

	[Test]
	[Arguments("verify.Take(It.IsAny<object>());")]
	[Arguments("verify.Take(It.IsAny<string>());")]
	[Arguments("verify.Take(It.Is<object>(It.IsAny<int>()));")]
	[Arguments("verify.Take((object)It.IsAny<int>());")]
	[Arguments("verify.Take(5);")]
	[Arguments("verify.Take(It.IsAny<Undefined>());")]
	[Arguments("verify.Take(It.IsAny<Undefined[]>());")]
	[Arguments("verify.Take(It.IsAny<System.Collections.Generic.List<Undefined>>());")]
	[Arguments("verify.Take(Match.AnyParameters());")]
	[Arguments("verify.Pair(Match.AnyParameters());")]
	[Arguments("verify.Plain<object>(Match.AnyParameters());")]
	[Arguments("verify.Gen(It.IsAny<int>(), It.IsAny<object>());")]
	[Arguments("property.Set(It.IsAny<string>());")]
	[Arguments("structProperty.Set((IParameter<StructMatcher>)new StructMatcher());")]
	[Arguments("setup.Take(It.IsAny<object>());")]
	[Arguments("verify.Typed(It.IsAny<int>());")]
	[Arguments("verify.Log(It.IsAny<int>(), It.IsAny<object>(), It.Is<object>(\"a\"));")]
	[Arguments("verify.Log(1, \"a\", 2);")]
	[Arguments("verify.Log(1, new object[] { \"a\", 2 });")]
	[Arguments("verify.Log(1, [\"a\", 2]);")]
	[Arguments("verify.Log(1, new[] { \"a\" });")]
	[Arguments("verify.Only(It.IsAny<object>());")]
	[Arguments("verify.Only(new[] { It.IsAny<object>() });")]
	[Arguments("verify.Only([It.IsAny<object>()]);")]
	[Arguments("verify.Plain(It.IsAny<int>());")]
	[Arguments("verify?.Plain(It.IsAny<int>());")]
	[Arguments("verify.Plain<StructMatcher>(new StructMatcher());")]
	[Arguments("verify.Plain<object>(It.IsAny<string>());")]
	[Arguments("verify.Plain<object>(It.IsAny<object>());")]
	[Arguments("verify.Plain<int>(5);")]
	[Arguments("verify.Dependent<object, object>(It.IsAny<object>());")]
	[Arguments("verify.Union(new Flavor());")]
	[Arguments("verify.Union(new FlavorMatcher());")]
	[Arguments("_ = verify[It.IsAny<object>()];")]
	[Arguments("property.Set(It.IsAny<object>());")]
	[Arguments("property.Set(5);")]
	[Arguments("raise.Changed(It.IsAny<int>());")]
	[Arguments("raiseOnVerifyForm.Changed(It.IsAny<int>());")]
	[Arguments("Other.Take(It.IsAny<int>());")]
	public async Task WhenNoMatcherIsComparedAsAValue_ShouldNotBeFlagged(string call)
		=> await VerifyAsync(call);

	private static Task VerifyAsync(string call, params DiagnosticResult[] expected)
		=> Verifier.VerifyAnalyzerAsync(
			$$"""
			  {{Declarations}}
			  public class MyClass
			  {
			      public void MyTest(Mock.IMockVerifyForFoo verify, Mock.IMockSetupForFoo setup, Mock.IMockRaiseOnFoo raise,
			          Mock.IMockRaiseOnIVerifyForm raiseOnVerifyForm, Mock.IMockStaticVerifyForFoo staticVerify,
			          Mockolate.Verify.PropertyResult<object> property,
			          Mockolate.Verify.PropertyResult<StructMatcher> structProperty)
			      {
			          {{call}}
			      }
			  }
			  """,
			expected);
}
