using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.MatcherAsValueAnalyzer>;

namespace Mockolate.Analyzers.Tests;

// The pinned test compiler predates C# unions, so the stand-in ParameterArg<T> below takes values through a plain
// implicit operator, which cannot convert from object or from an interface. A matcher that is also a value of the
// value type picks the value case there; with the real union that call is ambiguous (CS0457) when it is a matcher for
// the value type, and binds the value case otherwise.
public class MatcherAsValueAnalyzerTests
{
	private const string ValueHint =
		"use a matcher for 'object', or It.Is<object>(…) if the matcher itself is the expected value";

	private const string OtherArgumentsHint =
		"pass matchers of the parameter types for the other arguments as well, for example It.Is(…)";

	private const string InferredHint =
		"state the type arguments explicitly, so that the matcher binds to the parameter type";

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
	                                                int Dyn(dynamic value);
	                                                int Dyn(IParameter<object> value);
	                                                int Typed(IParameter<int> value);
	                                                int Five(int a, int b, int c, int d, object e);
	                                                int Five(IParameter<int> a, IParameter<int> b, IParameter<int> c, IParameter<int> d, IParameter<object> e);
	                                                int Log(int level, params object[] args);
	                                                int Log(IParameter<int> level, params IParameter<object>[] args);
	                                                int Gen<T>(int level, params T[] args);
	                                                int Plain<T>(T value);
	                                                int GenOnly<T>(params T[] args);
	                                                int Only(params object[] args);
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
	                                        }
	                                    }

	                                    namespace Mockolate.Verify
	                                    {
	                                        public class PropertyResult<T>
	                                        {
	                                            public int Set(T value) => 0;
	                                            public int Set(IParameter<T> value) => 0;
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
	[Arguments("_ = verify[{|#0:It.IsAny<int>()|}];", "It.IsAny<int>()")]
	[Arguments("verify.Plain<object>({|#0:It.IsAny<int>()|});", "It.IsAny<int>()")]
	[Arguments("verify?.Plain<object>({|#0:It.IsAny<int>()|});", "It.IsAny<int>()")]
	[Arguments("property.Set({|#0:It.IsAny<int>()|});", "It.IsAny<int>()")]
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
	[Arguments("verify.Plain({|#0:It.IsAny<int>()|});")]
	[Arguments("verify?.Plain({|#0:It.IsAny<int>()|});")]
	public async Task WhenTypeArgumentIsInferredFromTheMatcher_ShouldSuggestExplicitTypeArguments(string call)
		=> await VerifyAsync(call,
			Verifier.Diagnostic(MatcherAsValueAnalyzer.MatcherAsValueRule)
				.WithLocation(0)
				.WithArguments("It.IsAny<int>()", "IParameterWithCallback<int>", InferredHint));

	[Test]
	[Arguments("verify.Take(It.IsAny<object>());")]
	[Arguments("verify.Take(It.IsAny<string>());")]
	[Arguments("verify.Take(It.Is<object>(It.IsAny<int>()));")]
	[Arguments("verify.Take((object)It.IsAny<int>());")]
	[Arguments("verify.Take(5);")]
	[Arguments("verify.Take(It.IsAny<Undefined>());")]
	[Arguments("setup.Take(It.IsAny<object>());")]
	[Arguments("verify.Typed(It.IsAny<int>());")]
	[Arguments("verify.Log(It.IsAny<int>(), It.IsAny<object>(), It.Is<object>(\"a\"));")]
	[Arguments("verify.Log(1, \"a\", 2);")]
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
			          Mockolate.Verify.PropertyResult<object> property)
			      {
			          {{call}}
			      }
			  }
			  """,
			expected);
}
