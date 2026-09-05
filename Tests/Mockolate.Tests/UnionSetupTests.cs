#if NET11_0_OR_GREATER
using Mockolate.Exceptions;
using Mockolate.Verify;

namespace Mockolate.Tests;

// On this target the generator emits the union-typed setup and verify overloads (C# preview with
// MockolateUnionParameters enabled): one ParameterArg<T>? or Func<T, bool> slot per parameter.
public sealed class UnionSetupTests
{
	[Fact]
	public async Task Setup_WithPredicate_ShouldMatchOnlyWhenThePredicateHolds()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Mock.Setup.Compute(x => x > 0, "a").Returns(1);

		int positive = sut.Compute(5, "a");
		int negative = sut.Compute(-1, "a");

		await That(positive).IsEqualTo(1);
		await That(negative).IsEqualTo(0);
	}

	[Fact]
	public async Task Setup_WithLiteralAndMatcher_ShouldBothBind()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Mock.Setup.Compute(5, It.IsAny<string>()).Returns(2);

		int matching = sut.Compute(5, "z");
		int other = sut.Compute(6, "z");

		await That(matching).IsEqualTo(2);
		await That(other).IsEqualTo(0);
	}

	[Fact]
	public async Task Setup_WithNull_ShouldMatchTheNullLiteral()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Mock.Setup.Describe(null).Returns("none");

		string forNull = sut.Describe(null);
		string forValue = sut.Describe("x");

		await That(forNull).IsEqualTo("none");
		await That(forValue).IsNotEqualTo("none");
	}

	[Fact]
	public async Task Setup_WithDefault_ShouldMatchTheDefaultValue()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Mock.Setup.Compute(default, "a").Returns(3);

		int forZero = sut.Compute(0, "a");
		int forOne = sut.Compute(1, "a");

		await That(forZero).IsEqualTo(3);
		await That(forOne).IsEqualTo(0);
	}

	[Fact]
	public async Task Setup_WithDelegateTypedParameter_ShouldTreatLambdasAsValues()
	{
		IUnionService sut = IUnionService.CreateMock();
		Func<int, bool> callback = x => x > 0;
		sut.Mock.Setup.Register(callback);

		sut.Register(callback);

		await That(sut.Mock.Verify.Register(callback)).Once();
		await That(sut.Mock.Verify.Register(x => x > 0)).Never();
		await That(sut.Mock.Verify.Register(It.IsAny<Func<int, bool>>())).Once();
	}

	[Fact]
	public async Task Setup_WithFourMixedArguments_ShouldMatchAll()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Mock.Setup.Sum(1, It.IsAny<int>(), x => x > 2, 4).Returns(10);

		int matching = sut.Sum(1, 2, 3, 4);
		int failingPredicate = sut.Sum(1, 2, 2, 4);
		int failingLiteral = sut.Sum(1, 2, 3, 5);

		await That(matching).IsEqualTo(10);
		await That(failingPredicate).IsEqualTo(0);
		await That(failingLiteral).IsEqualTo(0);
	}

	[Fact]
	public async Task Setup_AnyParameters_ShouldBeAvailableOnMatcherSetups()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Mock.Setup.Compute(It.Is(1), "a").AnyParameters().Returns(7);

		int result = sut.Compute(9, "zz");

		await That(result).IsEqualTo(7);
	}

	[Fact]
	public async Task Setup_OmittedOptionalParameter_ShouldUseTheDeclaredDefault()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Mock.Setup.WithDefault().Returns(3);

		int forDefault = sut.WithDefault();
		int forOther = sut.WithDefault(6);

		await That(forDefault).IsEqualTo(3);
		await That(forOther).IsEqualTo(0);
	}

	[Fact]
	public async Task Setup_ObjectParameter_ShouldTreatMatchersAsMatchers()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Mock.Setup.Take(It.IsAny<string>()).Returns(true);

		bool forString = sut.Take("x");
		bool forInt = sut.Take(1);

		await That(forString).IsTrue();
		await That(forInt).IsFalse();
	}

	[Fact]
	public async Task Verify_WithLiteralMatcherAndPredicate_ShouldCount()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Compute(5, "a");
		sut.Compute(6, "bb");

		await That(sut.Mock.Verify.Compute(5, "a")).Once();
		await That(sut.Mock.Verify.Compute(It.IsAny<int>(), s => s.Length == 2)).Once();
		await That(sut.Mock.Verify.Compute(x => x > 4, It.IsAny<string>())).Twice();
		await That(sut.Mock.Verify.Compute(x => x > 10, It.IsAny<string>())).Never();
	}

	[Fact]
	public async Task Verify_WithPredicate_FailureMessage_ShouldContainThePredicateText()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Compute(5, "a");

		void Act()
			=> sut.Mock.Verify.Compute(x => x > 10, "a").Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("*Compute(x => x > 10, a)*").AsWildcard();
	}

	[Fact]
	public async Task Setup_ObjectParameter_ShouldTreatADelegateVariableAsValueAndALambdaAsPredicate()
	{
		IUnionService sut = IUnionService.CreateMock();
		Func<object?, bool> predicate = o => o is int;
		sut.Mock.Setup.Take(predicate).Returns(true);

		bool forInt = sut.Take(1);
		bool forTheDelegateItself = sut.Take(predicate);

		await That(forInt).IsFalse();
		await That(forTheDelegateItself).IsTrue();

		IUnionService other = IUnionService.CreateMock();
		other.Mock.Setup.Take(o => o is int).Returns(true);

		await That(other.Take(1)).IsTrue();
		await That(other.Take("x")).IsFalse();
	}

	[Fact]
	public async Task Setup_WithOutParameter_ShouldCombineAMatcherSlotWithAPredicate()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Mock.Setup.TryParse(s => s.Length > 0, It.IsOut(() => 42)).Returns(true);

		bool nonEmpty = sut.TryParse("a", out int parsed);
		bool empty = sut.TryParse("", out int _);

		await That(nonEmpty).IsTrue();
		await That(parsed).IsEqualTo(42);
		await That(empty).IsFalse();
		await That(sut.Mock.Verify.TryParse("a", It.IsOut<int>())).Once();
		await That(sut.Mock.Verify.TryParse(s => s.Length == 0, It.IsOut<int>())).Once();
	}

	[Fact]
	public async Task Verify_AnyParameters_ShouldIgnoreTheUnionArguments()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Compute(1, "a");
		sut.Compute(2, "b");

		await That(sut.Mock.Verify.Compute(x => x > 100, "zzz").AnyParameters()).Exactly(2);
	}

	[Fact]
	public async Task Setup_InScenario_ShouldOnlyApplyInThatScenario()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Mock.InScenario("a").Setup.Compute(x => x > 0, "a").Returns(9);

		int beforeTransition = sut.Compute(5, "a");
		sut.Mock.TransitionTo("a");
		int inScenario = sut.Compute(5, "a");

		await That(beforeTransition).IsEqualTo(0);
		await That(inScenario).IsEqualTo(9);
	}

	[Fact]
	public async Task OverloadedMethods_ShouldKeepTheClassicBindings()
	{
		IOverloadedService sut = IOverloadedService.CreateMock();
		sut.Mock.Setup.M(null).Returns(1);
		sut.Mock.Setup.N(5).Returns(2);
		sut.Mock.Setup.Foo(5).Returns(3);

		await That(sut.M((string?)null)).IsEqualTo(1);
		await That(sut.N(5)).IsEqualTo(2);
		await That(sut.N(5L)).IsEqualTo(0);
		await That(sut.Foo(5)).IsEqualTo(3);
		await That(sut.Foo<int>(5)).IsEqualTo(0);
	}

	[Fact]
	public async Task DelegateMock_ShouldOfferPredicates()
	{
		UnionDelegate sut = UnionDelegate.CreateMock();
		sut.Mock.Setup(x => x > 0, "a").Returns(1);

		int positive = sut(5, "a");
		int negative = sut(-1, "a");

		await That(positive).IsEqualTo(1);
		await That(negative).IsEqualTo(0);
		await That(sut.Mock.Verify(x => x != 0, It.IsAny<string>())).Twice();
		await That(sut.Mock.Verify(5, "a")).Once();
	}

	[Fact]
	public async Task Indexer_Setup_WithPredicateLiteralAndMatcher_ShouldMatch()
	{
		IUnionService sut = IUnionService.CreateMock();
		sut.Mock.Setup[x => x > 0, "a"].Returns("positive");
		sut.Mock.Setup[0, It.IsAny<string>()].Returns("zero");

		await That(sut[5, "a"]).IsEqualTo("positive");
		await That(sut[-1, "a"]).IsNotEqualTo("positive");
		await That(sut[0, "whatever"]).IsEqualTo("zero");
	}

	[Fact]
	public async Task Indexer_Verify_WithPredicateLiteralAndMatcher_ShouldCount()
	{
		IUnionService sut = IUnionService.CreateMock();
		_ = sut[5, "a"];
		_ = sut[6, "bb"];
		sut[7, "c"] = "v";

		await That(sut.Mock.Verify[x => x > 0, It.IsAny<string>()].Got()).Twice();
		await That(sut.Mock.Verify[5, "a"].Got()).Once();
		await That(sut.Mock.Verify[It.IsAny<int>(), s => s.Length == 2].Got()).Once();
		await That(sut.Mock.Verify[x => x > 10, "a"].Got()).Never();
		await That(sut.Mock.Verify[7, s => s == "c"].Set(It.IsAny<string>())).Once();
	}

	[Fact]
	public async Task Indexer_Verify_FailureMessage_ShouldContainThePredicateText()
	{
		IUnionService sut = IUnionService.CreateMock();
		_ = sut[5, "a"];

		void Act()
			=> sut.Mock.Verify[x => x > 10, "a"].Got().Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("*[x => x > 10, a]*").AsWildcard();
	}

	[Fact]
	public async Task Setup_WithMoreThanFourParametersAndADelegate_ShouldKeepTheRawDelegateOverload()
	{
		IUnionService sut = IUnionService.CreateMock();
		Func<int, bool> callback = x => x > 0;
		sut.Mock.Setup.SumAll(1, 2, 3, 4, x => x < 0).Returns(1);
		sut.Mock.Setup.SumAll(1, 2, 3, 4, callback).Returns(9);

		int matching = sut.SumAll(1, 2, 3, 4, callback);
		int differentLambda = sut.SumAll(1, 2, 3, 4, x => x > 100);

		await That(matching).IsEqualTo(9);
		await That(differentLambda).IsEqualTo(0);
		await That(sut.Mock.Verify.SumAll(It.IsAny<int>(), 2, 3, 4, callback)).Once();
	}

	[Fact]
	public async Task Setup_WithSystemDelegateParameter_ShouldTreatLambdasAsValues()
	{
		IUnionService sut = IUnionService.CreateMock();
		Action handler = () => { };
		sut.Mock.Setup.Attach(() => { });
		sut.Mock.Setup.Attach(handler);

		sut.Attach(handler);

		await That(sut.Mock.Verify.Attach(handler)).Once();
		await That(sut.Mock.Verify.Attach(It.IsAny<Delegate>())).Once();
	}

	[Fact]
	public async Task Indexer_Setup_WithNullKey_ShouldUseTheDeclaredDefault()
	{
		IDefaultKeyIndexer sut = IDefaultKeyIndexer.CreateMock();
		sut.Mock.Setup[1, null].Returns("default");

		string omitted = sut[1];
		string explicitDefault = sut[1, 7];
		string other = sut[1, 0];

		await That(omitted).IsEqualTo("default");
		await That(explicitDefault).IsEqualTo("default");
		await That(other).IsNotEqualTo("default");
		await That(sut.Mock.Verify[1, null].Got()).Twice();
	}

	[Fact]
	public async Task SameMethodNameInAnotherScope_ShouldStillOfferUnionOverloads()
	{
		ScopedUnionService sut = ScopedUnionService.CreateMock();
		sut.Mock.Setup.Go(x => x > 0).Returns(1);

		int positive = sut.Go(5);
		int negative = sut.Go(-1);

		await That(positive).IsEqualTo(1);
		await That(negative).IsEqualTo(0);
	}

	[Fact]
	public async Task DelegateMock_WithObjectParameter_MatchAnyParameters_ShouldBindTheIParametersOverload()
	{
		ObjectCallback sut = ObjectCallback.CreateMock();
		sut.Mock.Setup(Match.AnyParameters());

		sut("state");

		await That(sut.Mock.Verify(Match.AnyParameters())).Once();
	}

	[Fact]
	public async Task OverloadedIndexers_ShouldKeepTheClassicBindings()
	{
		IOverloadedIndexerService sut = IOverloadedIndexerService.CreateMock();
		sut.Mock.Setup[5].Returns("int");
		sut.Mock.Setup[5, "a"].Returns("two");

		await That(sut[5]).IsEqualTo("int");
		await That(sut[5L]).IsNotEqualTo("int");
		await That(sut[5, "a"]).IsEqualTo("two");
		await That(sut.Mock.Verify[5].Got()).Once();
	}

	public delegate int UnionDelegate(int x, string y);

	public delegate void ObjectCallback(object? state);

	public interface IOverloadedIndexerService
	{
		string this[int i] { get; }
		string this[long l] { get; }
		string this[int a, string b] { get; }
	}

	public interface IDefaultKeyIndexer
	{
		string this[int key, int offset = 7] { get; }
	}

	public abstract class ScopedUnionService
	{
		public abstract int Go(int v);
		protected abstract int Go(string v);
	}

	public interface IUnionService
	{
		int Compute(int value, string text);
		string Describe(string? s);
		void Register(Func<int, bool> callback);
		int Sum(int a, int b, int c, int d);
		int SumAll(int a, int b, int c, int d, Func<int, bool> callback);
		void Attach(Delegate handler);
		int WithDefault(int i = 5);
		bool Take(object? o);
		bool TryParse(string s, out int result);
		string this[int key, string name] { get; set; }
	}

	public interface IOverloadedService
	{
		int M(int v);
		int M(string? v);
		int N(int v);
		int N(long v);
		int Foo(int v);
		int Foo<T>(T v);
	}
}
#endif
