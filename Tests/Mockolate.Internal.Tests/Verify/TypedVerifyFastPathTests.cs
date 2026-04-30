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
		FastIndexerGetterBuffer<int> buffer = store.GetOrCreateBuffer<FastIndexerGetterBuffer<int>>(0,
			static f => new FastIndexerGetterBuffer<int>(f));
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
		InstallPropertyGetter(store, 0);
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
	public async Task IndexerGotTyped_WithBuffer_ProducesParametersDescriptionInExpectation()
	{
		FastMockInteractions store = new(1);
		store.GetOrCreateBuffer<FastIndexerGetterBuffer<int>>(0,
			static f => new FastIndexerGetterBuffer<int>(f));
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
		FastIndexerSetterBuffer<int, string> buffer = store.GetOrCreateBuffer<FastIndexerSetterBuffer<int, string>>(0,
			static f => new FastIndexerSetterBuffer<int, string>(f));
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
		InstallPropertyGetter(store, 0);
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
	public async Task IndexerSetTyped_WithBuffer_ProducesParametersDescriptionInExpectation()
	{
		FastMockInteractions store = new(1);
		store.GetOrCreateBuffer<FastIndexerSetterBuffer<int, string>>(0,
			static f => new FastIndexerSetterBuffer<int, string>(f));
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
		FastEventBuffer buffer = store.GetOrCreateBuffer<FastEventBuffer>(0,
			static f => new FastEventBuffer(f, FastEventBufferKind.Subscribe));
		MockRegistry registry = new(MockBehavior.Default, store);
		MethodInfo method = typeof(TypedVerifyFastPathTests).GetMethod(
			nameof(SubscribedTo_WithMemberIdAndBuffer_CountsBufferEntriesAndProducesExpectation))!;
		buffer.Append("E", null, method);
		buffer.Append("E", null, method);

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
		store.GetOrCreateBuffer<FastEventBuffer>(0,
			static f => new FastEventBuffer(f, FastEventBufferKind.Subscribe));
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
	public async Task TryGetBuffer_WhenBufferReturned_TypedFastPathIgnoresOtherInteractions()
	{
		FastMockInteractions store = new(1);
		FastMethod0Buffer buffer = store.GetOrCreateBuffer<FastMethod0Buffer>(0,
			static f => new FastMethod0Buffer(f));
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
		FastEventBuffer buffer = store.GetOrCreateBuffer<FastEventBuffer>(0,
			static f => new FastEventBuffer(f, FastEventBufferKind.Unsubscribe));
		MockRegistry registry = new(MockBehavior.Default, store);
		MethodInfo method = typeof(TypedVerifyFastPathTests).GetMethod(
			nameof(UnsubscribedFrom_WithMemberIdAndBuffer_CountsBufferEntriesAndProducesExpectation))!;
		buffer.Append("E", null, method);
		buffer.Append("E", null, method);

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
		store.GetOrCreateBuffer<FastEventBuffer>(0,
			static f => new FastEventBuffer(f, FastEventBufferKind.Unsubscribe));
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
		store.GetOrCreateBuffer<FastMethod0Buffer>(0, static f => new FastMethod0Buffer(f));
		MockRegistry registry = new(MockBehavior.Default, store);

		VerificationResult<object> result = registry.UnsubscribedFromTyped(new object(), 0, "OnFoo");

		await That(((IVerificationResult)result).Expectation).IsEqualTo("unsubscribed from event OnFoo");
	}


	[Test]
	public async Task VerifyMethod0_TypedFastPath_ShouldCount()
	{
		FastMockInteractions store = new(1);
		FastMethod0Buffer buffer = store.GetOrCreateBuffer<FastMethod0Buffer>(0,
			static f => new FastMethod0Buffer(f));

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
		FastMethod1Buffer<int> buffer = store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
			static f => new FastMethod1Buffer<int>(f));

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
		FastMethod1Buffer<int> buffer = store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
			static f => new FastMethod1Buffer<int>(f));

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
		FastMethod2Buffer<int, string> buffer = store.GetOrCreateBuffer<FastMethod2Buffer<int, string>>(0,
			static f => new FastMethod2Buffer<int, string>(f));

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
	public async Task VerifyProperty_GetterWithMemberIdAndBuffer_CountsBufferEntriesAndProducesExpectation()
	{
		FastMockInteractions store = new(1);
		FastPropertyGetterBuffer buffer = InstallPropertyGetter(store, 0);
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
		FastPropertySetterBuffer<int> buffer = store.GetOrCreateBuffer<FastPropertySetterBuffer<int>>(0,
			static f => new FastPropertySetterBuffer<int>(f));
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
		store.GetOrCreateBuffer<FastPropertySetterBuffer<int>>(0,
			static f => new FastPropertySetterBuffer<int>(f));
		MockRegistry registry = new(MockBehavior.Default, store);

		IParameterMatch<int> value = (IParameterMatch<int>)It.Is(42);
		VerificationResult<object> result = registry.VerifyPropertyTyped(new object(), 0, "P", value);

		await That(((IVerificationResult)result).Expectation).Contains("set property P");
		await That(((IVerificationResult)result).Expectation).Contains(value.ToString()!);
	}

	private static FastPropertyGetterBuffer InstallPropertyGetter(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastPropertyGetterBuffer>(memberId,
			static f => new FastPropertyGetterBuffer(f, new PropertyGetterAccess(string.Empty)));
}
