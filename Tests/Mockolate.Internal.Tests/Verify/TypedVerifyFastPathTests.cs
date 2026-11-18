using System.Reflection;
using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Parameters;
using Mockolate.Verify;

namespace Mockolate.Internal.Tests.Verify;

public class TypedVerifyFastPathTests
{
	[Test]
	public async Task IndexerGot_WithMemberIdAndBuffer_CountsBufferEntriesAndProducesExpectation()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);
		buffer.Append(5);
		buffer.Append(7);
		buffer.Append(5);

		VerificationResult<object> result = registry.IndexerGot(
			new object(), 0,
			static i => i is IndexerGetterAccess<int> g && g.Parameter1 == 5,
			() => "[5]");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got indexer [5]");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task IndexerGot_WithMemberIdAndInstalledBuffer_OnlyWalksBuffer()
	{
		FastMockInteractions store = new(1);
		InstallPropertyGetter(store, 0).Append();
		MockRegistry registry = new(MockBehavior.Default, store);
		IMockInteractions interactions = store;

		interactions.RegisterInteraction(new IndexerGetterAccess<int>(5));
		interactions.RegisterInteraction(new IndexerGetterAccess<int>(5));

		VerificationResult<object> result = registry.IndexerGot(
			new object(), 0,
			static i => i is IndexerGetterAccess<int> g && g.Parameter1 == 5,
			() => "[5]");

		await That(((IVerificationResult)result).Verify(arr => arr.Length == 0)).IsTrue();
	}

	[Test]
	public async Task IndexerGot_WithoutBuffer_ProducesParametersDescriptionInExpectation()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);

		VerificationResult<object> result = registry.IndexerGot(
			new object(), 5,
			static _ => true,
			() => "(5)");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got indexer (5)");
	}

	[Test]
	public async Task IndexerGotTyped_FourKeys_ShouldCollectMatchesAndDescribeExpectation()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int, int, int, int> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);
		buffer.Append(1, 2, 3, 4);
		buffer.Append(1, 2, 3, 5);
		buffer.Append(1, 2, 3, 4);

		VerificationResult<object> result = registry.IndexerGotTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<int>)It.Is(2),
			(IParameterMatch<int>)It.Is(3),
			(IParameterMatch<int>)It.Is(4),
			() => "[1, 2, 3, 4]");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got indexer [1, 2, 3, 4]");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task IndexerGotTyped_ThreeKeys_ShouldCollectMatchesAndDescribeExpectation()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int, int, int> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);
		buffer.Append(1, 2, 3);
		buffer.Append(1, 2, 4);
		buffer.Append(1, 2, 3);

		VerificationResult<object> result = registry.IndexerGotTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<int>)It.Is(2),
			(IParameterMatch<int>)It.Is(3),
			() => "[1, 2, 3]");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got indexer [1, 2, 3]");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task IndexerGotTyped_TwoKeys_ShouldCollectMatchesAndDescribeExpectation()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int, int> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);
		buffer.Append(1, 2);
		buffer.Append(1, 3);
		buffer.Append(1, 2);

		VerificationResult<object> result = registry.IndexerGotTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<int>)It.Is(2),
			() => "[1, 2]");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got indexer [1, 2]");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task IndexerGotTyped_WithBuffer_ProducesParametersDescriptionInExpectation()
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);

		VerificationResult<object> result = registry.IndexerGotTyped(
			new object(), 0,
			(IParameterMatch<int>)It.Is(5),
			() => "(5)");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got indexer (5)");
	}

	[Test]
	public async Task IndexerSet_WithMemberIdAndBuffer_CountsMatchingValuesAndProducesExpectation()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);
		buffer.Append(5, "v");
		buffer.Append(7, "v");
		buffer.Append(5, "x");

		IParameterMatch<string> value = (IParameterMatch<string>)It.Is("v");
		VerificationResult<object> result = registry.IndexerSet(
			new object(), 0,
			static (i, v) => i is IndexerSetterAccess<int, string> s && s.Parameter1 == 5 && v.Matches(s.TypedValue),
			value,
			() => "[5]");

		await That(((IVerificationResult)result).Expectation).Contains("set indexer [5]");
		await That(((IVerificationResult)result).Expectation).Contains(value.ToString()!);
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	public async Task IndexerSet_WithMemberIdAndInstalledBuffer_OnlyWalksBuffer()
	{
		FastMockInteractions store = new(1);
		InstallPropertyGetter(store, 0).Append();
		MockRegistry registry = new(MockBehavior.Default, store);
		IMockInteractions interactions = store;

		interactions.RegisterInteraction(new IndexerSetterAccess<int, string>(5, "v"));
		interactions.RegisterInteraction(new IndexerSetterAccess<int, string>(5, "v"));

		IParameterMatch<string> value = (IParameterMatch<string>)It.IsAny<string>();
		VerificationResult<object> result = registry.IndexerSet(
			new object(), 0,
			static (i, v) => i is IndexerSetterAccess<int, string> s && s.Parameter1 == 5 && v.Matches(s.TypedValue),
			value,
			() => "[5]");

		await That(((IVerificationResult)result).Verify(arr => arr.Length == 0)).IsTrue();
	}

	[Test]
	public async Task IndexerSet_WithoutBuffer_ProducesParametersDescriptionInExpectation()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);

		IParameterMatch<int> value = (IParameterMatch<int>)It.Is(7);
		VerificationResult<object> result = registry.IndexerSet(
			new object(), 5,
			static (_, _) => true,
			value,
			() => "(5)");

		await That(((IVerificationResult)result).Expectation).Contains("set indexer (5)");
		await That(((IVerificationResult)result).Expectation).Contains(value.ToString()!);
	}

	[Test]
	public async Task IndexerSetTyped_FourKeys_ShouldCollectMatchesAndDescribeExpectation()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, int, int, int, string> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);
		buffer.Append(1, 2, 3, 4, "v");
		buffer.Append(1, 2, 3, 4, "w");
		buffer.Append(1, 2, 3, 5, "v");

		VerificationResult<object> result = registry.IndexerSetTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<int>)It.Is(2),
			(IParameterMatch<int>)It.Is(3),
			(IParameterMatch<int>)It.Is(4),
			(IParameterMatch<string>)It.Is("v"),
			() => "[1, 2, 3, 4]");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("set indexer [1, 2, 3, 4] to \"v\"");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	public async Task IndexerSetTyped_ThreeKeys_ShouldCollectMatchesAndDescribeExpectation()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, int, int, string> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);
		buffer.Append(1, 2, 3, "v");
		buffer.Append(1, 2, 3, "w");
		buffer.Append(1, 2, 4, "v");

		VerificationResult<object> result = registry.IndexerSetTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<int>)It.Is(2),
			(IParameterMatch<int>)It.Is(3),
			(IParameterMatch<string>)It.Is("v"),
			() => "[1, 2, 3]");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("set indexer [1, 2, 3] to \"v\"");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	public async Task IndexerSetTyped_WithBuffer_ProducesParametersDescriptionInExpectation()
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);

		IParameterMatch<string> value = (IParameterMatch<string>)It.Is("v");
		VerificationResult<object> result = registry.IndexerSetTyped(
			new object(), 0,
			(IParameterMatch<int>)It.Is(5),
			value,
			() => "(5)");

		await That(((IVerificationResult)result).Expectation).Contains("set indexer (5)");
		await That(((IVerificationResult)result).Expectation).Contains(value.ToString()!);
	}

	[Test]
	public async Task SubscribedTo_WithMemberIdAndBuffer_CountsBufferEntriesAndProducesExpectation()
	{
		FastMockInteractions store = new(1);
		EventRecords buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);
		MethodInfo method = typeof(TypedVerifyFastPathTests).GetMethod(
			nameof(SubscribedTo_WithMemberIdAndBuffer_CountsBufferEntriesAndProducesExpectation))!;
		buffer.AppendSubscription("E", null, method);
		buffer.AppendSubscription("E", null, method);

		VerificationResult<object> result = registry.SubscribedTo(new object(), 0, "E");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("subscribed to event E");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task SubscribedTo_WithMemberIdButNoBuffer_FallsBackAndFiltersByName()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);
		IMockInteractions interactions = store;
		MethodInfo method = typeof(TypedVerifyFastPathTests).GetMethod(
			nameof(SubscribedTo_WithMemberIdButNoBuffer_FallsBackAndFiltersByName))!;
		interactions.RegisterInteraction(new EventSubscription("E", null, method));
		interactions.RegisterInteraction(new EventSubscription("E", null, method));
		interactions.RegisterInteraction(new EventSubscription("F", null, method));

		VerificationResult<object> result = registry.SubscribedTo(new object(), 5, "E");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("subscribed to event E");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task SubscribedToTyped_WithBuffer_ProducesEventNameInExpectation()
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);

		VerificationResult<object> result = registry.SubscribedToTyped(new object(), 0, "OnFoo");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("subscribed to event OnFoo");
	}

	[Test]
	public async Task SubscribedToTyped_WithMemberIdButNoBuffer_FallsBackToStringKeyedPath()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);

		VerificationResult<object> result = registry.SubscribedToTyped(new object(), 5, "OnFoo");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("subscribed to event OnFoo");
	}

	[Test]
	public async Task TryGetRecords_WhenBufferReturned_TypedFastPathIgnoresOtherInteractions()
	{
		FastMockInteractions store = new(1);
		MethodRecords buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);
		IMockInteractions interactions = store;

		buffer.Append("Foo");

		interactions.RegisterInteraction(new MethodInvocation("Foo"));
		interactions.RegisterInteraction(new MethodInvocation("Foo"));

		registry.VerifyMethod(new object(), 0, "Foo", () => "Foo()").Once();

		await That(true).IsTrue();
	}

	[Test]
	public async Task UnsubscribedFrom_WithMemberIdAndBuffer_CountsBufferEntriesAndProducesExpectation()
	{
		FastMockInteractions store = new(1);
		EventRecords buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);
		MethodInfo method = typeof(TypedVerifyFastPathTests).GetMethod(
			nameof(UnsubscribedFrom_WithMemberIdAndBuffer_CountsBufferEntriesAndProducesExpectation))!;
		buffer.AppendUnsubscription("E", null, method);
		buffer.AppendUnsubscription("E", null, method);

		VerificationResult<object> result = registry.UnsubscribedFrom(new object(), 0, "E");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("unsubscribed from event E");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task UnsubscribedFrom_WithMemberIdButNoBuffer_FallsBackAndFiltersByName()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);
		IMockInteractions interactions = store;
		MethodInfo method = typeof(TypedVerifyFastPathTests).GetMethod(
			nameof(UnsubscribedFrom_WithMemberIdButNoBuffer_FallsBackAndFiltersByName))!;
		interactions.RegisterInteraction(new EventUnsubscription("E", null, method));
		interactions.RegisterInteraction(new EventUnsubscription("E", null, method));
		interactions.RegisterInteraction(new EventUnsubscription("F", null, method));

		VerificationResult<object> result = registry.UnsubscribedFrom(new object(), 5, "E");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("unsubscribed from event E");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task UnsubscribedFromTyped_WithBuffer_ProducesEventNameInExpectation()
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);

		VerificationResult<object> result = registry.UnsubscribedFromTyped(new object(), 0, "OnFoo");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("unsubscribed from event OnFoo");
	}

	[Test]
	public async Task UnsubscribedFromTyped_WithMemberIdButNoBuffer_FallsBackToStringKeyedPath()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);

		VerificationResult<object> result = registry.UnsubscribedFromTyped(new object(), 5, "OnFoo");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("unsubscribed from event OnFoo");
	}

	[Test]
	public async Task UnsubscribedFromTyped_WithMemberIdButNonMatchingBufferKind_FallsBackToStringKeyedPath()
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);

		VerificationResult<object> result = registry.UnsubscribedFromTyped(new object(), 0, "OnFoo");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("unsubscribed from event OnFoo");
	}


	[Test]
	public async Task VerifyMethod0_TypedFastPath_ShouldCount()
	{
		FastMockInteractions store = new(1);
		MethodRecords buffer = new(store, 0);

		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append("Foo");
		buffer.Append("Foo");

		registry.VerifyMethod(new object(), 0, "Foo", () => "Foo()").Twice();

		await That(true).IsTrue();
	}

	[Test]
	public async Task VerifyMethod0_WithMemberIdButNoMatchingBuffer_FallbackPredicateMatchesAllMethodInteractions()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);
		IMockInteractions interactions = store;
		interactions.RegisterInteraction(new MethodInvocation("Foo"));
		interactions.RegisterInteraction(new MethodInvocation("Foo"));
		interactions.RegisterInteraction(new MethodInvocation("Bar"));

		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod(
			new object(), 5, "Foo", () => "Foo()");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("invoked method Foo()");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task VerifyMethod1_TypedFastPath_FailsWithExpectedMessage()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append("Foo", 1);
		buffer.Append("Foo", 1);

		await That(() => registry.VerifyMethod(new object(), 0, "Foo",
				(IParameterMatch<int>)It.Is(1), () => "Foo(1)").Once())
			.Throws<MockVerificationException>();
	}

	[Test]
	public async Task VerifyMethod1_TypedFastPath_ShouldHonorMatcher()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append("Foo", 1);
		buffer.Append("Foo", 2);
		buffer.Append("Foo", 1);

		registry.VerifyMethod(new object(), 0, "Foo",
			(IParameterMatch<int>)It.Is(1), () => "Foo(1)").Twice();
		registry.VerifyMethod(new object(), 0, "Foo",
			(IParameterMatch<int>)It.IsAny<int>(), () => "Foo(*)").Exactly(3);

		await That(true).IsTrue();
	}

	[Test]
	public async Task VerifyMethod2_TypedFastPath_AnyParameters_UsesCountAll()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);

		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append("Foo", 1, "a");
		buffer.Append("Foo", 2, "b");
		buffer.Append("Foo", 3, "c");

		// Without AnyParameters: only matches values where m1==1
		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod(
			new object(), 0, "Foo",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("z"), // never matches
			() => "Foo(1, z)");

		// AnyParameters → CountAll → all 3 match
		result.AnyParameters().Exactly(3);

		await That(true).IsTrue();
	}

	[Test]
	public async Task VerifyMethod3_FallbackPath_RequiresAllParametersToMatch()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);
		IMockInteractions interactions = store;

		interactions.RegisterInteraction(new MethodInvocation<int, int, int>("M", 1, 2, 3));
		interactions.RegisterInteraction(new MethodInvocation<int, int, int>("M", 1, 2, 99));
		interactions.RegisterInteraction(new MethodInvocation<int, int, int>("M", 1, 99, 3));
		interactions.RegisterInteraction(new MethodInvocation<int, int, int>("M", 99, 2, 3));

		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod(
			new object(), 0, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<int>)It.Is(2),
			(IParameterMatch<int>)It.Is(3),
			() => "M(1, 2, 3)");

		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	public async Task VerifyMethod4_FallbackPath_RequiresAllParametersToMatch()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);
		IMockInteractions interactions = store;

		interactions.RegisterInteraction(new MethodInvocation<int, int, int, int>("M", 1, 2, 3, 4));
		interactions.RegisterInteraction(new MethodInvocation<int, int, int, int>("M", 1, 2, 3, 99));
		interactions.RegisterInteraction(new MethodInvocation<int, int, int, int>("M", 1, 2, 99, 4));
		interactions.RegisterInteraction(new MethodInvocation<int, int, int, int>("M", 1, 99, 3, 4));
		interactions.RegisterInteraction(new MethodInvocation<int, int, int, int>("M", 99, 2, 3, 4));

		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod(
			new object(), 0, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<int>)It.Is(2),
			(IParameterMatch<int>)It.Is(3),
			(IParameterMatch<int>)It.Is(4),
			() => "M(1, 2, 3, 4)");

		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task VerifyMethod1_WithLiteralFormatter_ShouldCountAndDescribeLiteralValues(bool withBuffer)
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);
		if (withBuffer)
		{
			new MethodRecords<int>(store, 0)
				.Append("M", 1);
		}
		else
		{
			((IMockInteractions)store).RegisterInteraction(new MethodInvocation<int>("M", 1));
		}

		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, int>(
			new object(), 0, "M", 1, static a => $"M({a})");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("invoked method M(1)");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task VerifyMethod1_WithNullReferenceLiteral_ShouldCountAndDescribeNullAsEmpty(bool withBuffer)
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);
		if (withBuffer)
		{
			new MethodRecords<string?>(store, 0)
				.Append("M", null);
		}
		else
		{
			((IMockInteractions)store).RegisterInteraction(new MethodInvocation<string?>("M", null));
		}

		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, string?>(
			new object(), 0, "M", null, static a => $"M({a})");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("invoked method M()");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	public async Task VerifyMethod1_WithBufferOfOtherArityAtMemberId_ShouldFallBackToInteractionList()
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);
		((IMockInteractions)store).RegisterInteraction(new MethodInvocation<int>("M", 1));

		VerificationResult<object>.IgnoreParameters literal = registry.VerifyMethod<object, int>(
			new object(), 0, "M", 1, static a => $"M({a})");
		VerificationResult<object>.IgnoreParameters matcher = registry.VerifyMethod<object, int>(
			new object(), 0, "M", (IParameterMatch<int>)It.IsAny<int>(), null, static a => $"M({a})");

		await That(((IVerificationResult)literal).Expectation).IsEqualTo("invoked method M(1)");
		await That(((IVerificationResult)literal).Verify(arr => arr.Length == 1)).IsTrue();
		await That(((IVerificationResult)matcher).Expectation).IsEqualTo("invoked method M()");
		await That(((IVerificationResult)matcher).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task VerifyMethod1_WithParameterFormatter_ShouldCountAndDescribeCallerParameters(bool withBuffer)
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);
		if (withBuffer)
		{
			new MethodRecords<int>(store, 0)
				.Append("M", 1);
		}
		else
		{
			((IMockInteractions)store).RegisterInteraction(new MethodInvocation<int>("M", 1));
		}

		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, int>(
			new object(), 0, "M", (IParameterMatch<int>)It.IsAny<int>(), null, static a => $"M({a})");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("invoked method M()")
			.Because("a null parameter is described as an empty string");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task VerifyMethod2_WithLiteralFormatter_ShouldCountAndDescribeLiteralValues(bool withBuffer)
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);
		if (withBuffer)
		{
			new MethodRecords<int, int>(store, 0)
				.Append("M", 1, 2);
		}
		else
		{
			((IMockInteractions)store).RegisterInteraction(new MethodInvocation<int, int>("M", 1, 2));
		}

		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, int, int>(
			new object(), 0, "M", 1, 2, static (a, b) => $"M({a}, {b})");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("invoked method M(1, 2)");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task VerifyMethod2_WithParameterFormatter_ShouldCountAndDescribeCallerParameters(bool withBuffer)
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);
		if (withBuffer)
		{
			new MethodRecords<int, int>(store, 0)
				.Append("M", 1, 2);
		}
		else
		{
			((IMockInteractions)store).RegisterInteraction(new MethodInvocation<int, int>("M", 1, 2));
		}

		IParameter<int> parameter1 = It.Is(1);
		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, int, int>(
			new object(), 0, "M", (IParameterMatch<int>)parameter1, (IParameterMatch<int>)It.IsAny<int>(),
			parameter1, null, static (a, b) => $"M({a}, {b})");

		await That(((IVerificationResult)result).Expectation).IsEqualTo($"invoked method M({parameter1}, )");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task VerifyMethod3_WithLiteralFormatter_ShouldCountAndDescribeLiteralValues(bool withBuffer)
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);
		if (withBuffer)
		{
			new MethodRecords<int, int, int>(store, 0)
				.Append("M", 1, 2, 3);
		}
		else
		{
			((IMockInteractions)store).RegisterInteraction(new MethodInvocation<int, int, int>("M", 1, 2, 3));
		}

		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, int, int, int>(
			new object(), 0, "M", 1, 2, 3, static (a, b, c) => $"M({a}, {b}, {c})");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("invoked method M(1, 2, 3)");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task VerifyMethod3_WithParameterFormatter_ShouldCountAndDescribeCallerParameters(bool withBuffer)
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);
		if (withBuffer)
		{
			new MethodRecords<int, int, int>(store, 0)
				.Append("M", 1, 2, 3);
		}
		else
		{
			((IMockInteractions)store).RegisterInteraction(new MethodInvocation<int, int, int>("M", 1, 2, 3));
		}

		IParameter<int> parameter1 = It.Is(1);
		IParameter<int> parameter3 = It.Is(3);
		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, int, int, int>(
			new object(), 0, "M", (IParameterMatch<int>)parameter1, (IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<int>)parameter3, parameter1, null, parameter3,
			static (a, b, c) => $"M({a}, {b}, {c})");

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo($"invoked method M({parameter1}, , {parameter3})");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task VerifyMethod4_WithLiteralFormatter_ShouldCountAndDescribeLiteralValues(bool withBuffer)
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);
		if (withBuffer)
		{
			new MethodRecords<int, int, int, int>(store, 0)
				.Append("M", 1, 2, 3, 4);
		}
		else
		{
			((IMockInteractions)store).RegisterInteraction(new MethodInvocation<int, int, int, int>("M", 1, 2, 3, 4));
		}

		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, int, int, int, int>(
			new object(), 0, "M", 1, 2, 3, 4, static (a, b, c, d) => $"M({a}, {b}, {c}, {d})");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("invoked method M(1, 2, 3, 4)");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task VerifyMethod4_WithParameterFormatter_ShouldCountAndDescribeCallerParameters(bool withBuffer)
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);
		if (withBuffer)
		{
			new MethodRecords<int, int, int, int>(store, 0)
				.Append("M", 1, 2, 3, 4);
		}
		else
		{
			((IMockInteractions)store).RegisterInteraction(new MethodInvocation<int, int, int, int>("M", 1, 2, 3, 4));
		}

		IParameter<int> parameter1 = It.Is(1);
		IParameter<int> parameter2 = It.Is(2);
		IParameter<int> parameter4 = It.Is(4);
		VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, int, int, int, int>(
			new object(), 0, "M", (IParameterMatch<int>)parameter1, (IParameterMatch<int>)parameter2,
			(IParameterMatch<int>)It.IsAny<int>(), (IParameterMatch<int>)parameter4,
			parameter1, parameter2, null, parameter4,
			static (a, b, c, d) => $"M({a}, {b}, {c}, {d})");

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo($"invoked method M({parameter1}, {parameter2}, , {parameter4})");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 1)).IsTrue();
	}

	[Test]
	public async Task VerifyProperty_GetterWithMemberIdAndBuffer_CountsBufferEntriesAndProducesExpectation()
	{
		FastMockInteractions store = new(1);
		PropertyGetterRecords buffer = InstallPropertyGetter(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);
		buffer.Append();
		buffer.Append();

		VerificationResult<object> result = registry.VerifyProperty(new object(), 0, "P");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got property P");
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task VerifyProperty_SetterWithMemberIdAndBuffer_CountsMatchingValuesAndProducesExpectation()
	{
		FastMockInteractions store = new(1);
		PropertySetterRecords<int> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);
		buffer.Append("P", 1);
		buffer.Append("P", 2);
		buffer.Append("P", 1);

		IParameterMatch<int> value = (IParameterMatch<int>)It.Is(1);
		VerificationResult<object> result = registry.VerifyProperty(new object(), 0, "P", value);

		await That(((IVerificationResult)result).Expectation).Contains("set property P");
		await That(((IVerificationResult)result).Expectation).Contains(value.ToString()!);
		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task VerifyProperty_WithMemberIdButNoBuffer_FallbackPredicateFiltersByName()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);
		IMockInteractions interactions = store;

		interactions.RegisterInteraction(new PropertyGetterAccess("P"));
		interactions.RegisterInteraction(new PropertyGetterAccess("P"));
		interactions.RegisterInteraction(new PropertyGetterAccess("Q"));

		VerificationResult<object> result = registry.VerifyProperty(new object(), 5, "P");

		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task VerifyProperty_WithMemberIdButNoBuffer_FallsBackToStringKeyedPath()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);
		IMockInteractions interactions = store;

		interactions.RegisterInteraction(new PropertyGetterAccess("P"));
		interactions.RegisterInteraction(new PropertyGetterAccess("P"));

		VerificationResult<object> result = registry.VerifyProperty(new object(), 5, "P");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got property P");
		((IVerificationResult)result).Verify(arr => arr.Length == 2);
	}

	[Test]
	public async Task VerifyPropertySetter_WithMemberIdButNoBuffer_FallbackPredicateFiltersByName()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);
		IMockInteractions interactions = store;

		interactions.RegisterInteraction(new PropertySetterAccess<int>("P", 1));
		interactions.RegisterInteraction(new PropertySetterAccess<int>("P", 2));
		interactions.RegisterInteraction(new PropertySetterAccess<int>("Q", 3));

		VerificationResult<object> result = registry.VerifyProperty(
			new object(), 5, "P", (IParameterMatch<int>)It.IsAny<int>());

		await That(((IVerificationResult)result).Verify(arr => arr.Length == 2)).IsTrue();
	}

	[Test]
	public async Task VerifyPropertySetter_WithMemberIdButNoBuffer_FallsBackToStringKeyedPath()
	{
		FastMockInteractions store = new(0);
		MockRegistry registry = new(MockBehavior.Default, store);
		IMockInteractions interactions = store;

		interactions.RegisterInteraction(new PropertySetterAccess<int>("P", 1));
		interactions.RegisterInteraction(new PropertySetterAccess<int>("P", 2));

		VerificationResult<object> result = registry.VerifyProperty(
			new object(), 5, "P", (IParameterMatch<int>)It.IsAny<int>());

		await That(((IVerificationResult)result).Expectation).Contains("set property P");
		((IVerificationResult)result).Verify(arr => arr.Length == 2);
	}

	[Test]
	public async Task VerifyPropertyTyped_Getter_WithBuffer_ProducesPropertyNameInExpectation()
	{
		FastMockInteractions store = new(1);
		InstallPropertyGetter(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		VerificationResult<object> result = registry.VerifyPropertyTyped(new object(), 0, "P");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got property P");
	}

	[Test]
	public async Task VerifyPropertyTyped_Setter_WithBuffer_ProducesPropertyNameInExpectation()
	{
		FastMockInteractions store = new(1);
		MockRegistry registry = new(MockBehavior.Default, store);

		IParameterMatch<int> value = (IParameterMatch<int>)It.Is(42);
		VerificationResult<object> result = registry.VerifyPropertyTyped(new object(), 0, "P", value);

		await That(((IVerificationResult)result).Expectation).Contains("set property P");
		await That(((IVerificationResult)result).Expectation).Contains(value.ToString()!);
	}

	private static PropertyGetterRecords InstallPropertyGetter(FastMockInteractions store, int memberId)
		=> new PropertyGetterRecords(store, memberId);
}
