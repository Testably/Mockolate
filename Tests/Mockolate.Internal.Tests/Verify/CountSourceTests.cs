using System.Reflection;
using Mockolate.Interactions;
using Mockolate.Parameters;
using Mockolate.Verify;

namespace Mockolate.Internal.Tests.Verify;

public class CountSourceTests
{
	[Test]
	public async Task EventCountSource_Subscribe_Count_IsExercised()
	{
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		MethodInfo m = typeof(CountSourceTests).GetMethod(
			nameof(EventCountSource_Subscribe_Count_IsExercised))!;
		buffer.AppendSubscription("OnFoo", null, m);
		buffer.AppendSubscription("OnFoo", null, m);

		registry.SubscribedToTyped(new object(), 0, "OnFoo").Twice();

		await That(true).IsTrue();
	}

	[Test]
	public async Task EventCountSource_Unsubscribe_Count_IsExercised()
	{
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		MethodInfo m = typeof(CountSourceTests).GetMethod(
			nameof(EventCountSource_Unsubscribe_Count_IsExercised))!;
		buffer.AppendUnsubscription("OnFoo", null, m);
		buffer.AppendUnsubscription("OnFoo", null, m);
		buffer.AppendUnsubscription("OnFoo", null, m);

		registry.UnsubscribedFromTyped(new object(), 0, "OnFoo").Exactly(3);

		await That(true).IsTrue();
	}

	[Test]
	public async Task IndexerGetter1_FastPath_Count_IsExercised()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append(5);
		buffer.Append(6);
		buffer.Append(5);

		registry.IndexerGotTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(5), () => "(5)").Twice();

		await That(true).IsTrue();
	}

	[Test]
	public async Task IndexerGetter2_FastPath_Count_IsExercised()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append(5, "a");
		buffer.Append(6, "b");
		buffer.Append(5, "a");

		registry.IndexerGotTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(5),
			(IParameterMatch<string>)It.Is<string>("a"),
			() => "(5, a)").Twice();

		await That(true).IsTrue();
	}

	[Test]
	public async Task IndexerGetter3_FastPath_Count_IsExercised()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string, bool> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append(5, "a", true);
		buffer.Append(6, "b", false);
		buffer.Append(5, "a", true);

		registry.IndexerGotTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(5),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			() => "(5, a, true)").Twice();

		await That(true).IsTrue();
	}

	[Test]
	public async Task IndexerGetter4_FastPath_Count_IsExercised()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string, bool, double> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append(5, "a", true, 1.5);
		buffer.Append(6, "b", false, 2.5);
		buffer.Append(5, "a", true, 1.5);

		registry.IndexerGotTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(5),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(1.5),
			() => "(5, a, true, 1.5)").Twice();

		await That(true).IsTrue();
	}

	[Test]
	public async Task IndexerSetter1_FastPath_Count_IsExercised()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append(5, "v");
		buffer.Append(6, "w");
		buffer.Append(5, "v");

		registry.IndexerSetTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(5),
			(IParameterMatch<string>)It.Is<string>("v"),
			() => "(5)").Twice();

		await That(true).IsTrue();
	}

	[Test]
	public async Task IndexerSetter2_FastPath_Count_IsExercised()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, double> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append(5, "a", 1.5);
		buffer.Append(6, "b", 2.5);
		buffer.Append(5, "a", 1.5);

		registry.IndexerSetTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(5),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<double>)It.Is(1.5),
			() => "(5, a)").Twice();

		await That(true).IsTrue();
	}

	[Test]
	public async Task IndexerSetter3_FastPath_Count_IsExercised()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool, double> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append(5, "a", true, 1.5);
		buffer.Append(6, "b", false, 2.5);
		buffer.Append(5, "a", true, 1.5);

		registry.IndexerSetTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(5),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(1.5),
			() => "(5, a, true)").Twice();

		await That(true).IsTrue();
	}

	[Test]
	public async Task IndexerSetter4_FastPath_Count_IsExercised()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool, double, long> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append(5, "a", true, 1.5, 100L);
		buffer.Append(6, "b", false, 2.5, 200L);
		buffer.Append(5, "a", true, 1.5, 100L);

		registry.IndexerSetTyped(new object(), 0,
			(IParameterMatch<int>)It.Is(5),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(1.5),
			(IParameterMatch<long>)It.Is(100L),
			() => "(5, a, true, 1.5)").Twice();

		await That(true).IsTrue();
	}

	[Test]
	public async Task Method0_FastPath_Count_AndCountAll_AreExercised()
	{
		FastMockInteractions store = new(1);
		FastMethod0Buffer buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append("Foo");
		buffer.Append("Foo");
		buffer.Append("Foo");

		registry.VerifyMethod(new object(), 0, "Foo", () => "Foo()").Exactly(3);
		registry.VerifyMethod(new object(), 0, "Foo", () => "Foo()").AnyParameters().Exactly(3);

		await That(true).IsTrue();
	}

	[Test]
	public async Task Method1_FastPath_Count_AndCountAll_AreExercised()
	{
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append("Foo", 1);
		buffer.Append("Foo", 2);

		registry.VerifyMethod(new object(), 0, "Foo",
			(IParameterMatch<int>)It.Is(1), () => "Foo(1)").Once();
		registry.VerifyMethod(new object(), 0, "Foo",
			(IParameterMatch<int>)It.Is(99), () => "Foo(99)").AnyParameters().Exactly(2);

		await That(true).IsTrue();
	}

	[Test]
	public async Task Method2_FastPath_Count_AndCountAll_AreExercised()
	{
		FastMockInteractions store = new(1);
		FastMethod2Buffer<int, string> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append("Foo", 1, "a");
		buffer.Append("Foo", 2, "b");

		registry.VerifyMethod(new object(), 0, "Foo",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"), () => "Foo(1, a)").Once();
		registry.VerifyMethod(new object(), 0, "Foo",
			(IParameterMatch<int>)It.Is(99),
			(IParameterMatch<string>)It.Is<string>("z"), () => "Foo(99, z)").AnyParameters().Exactly(2);

		await That(true).IsTrue();
	}

	[Test]
	public async Task Method3_FastPath_Count_AndCountAll_AreExercised()
	{
		FastMockInteractions store = new(1);
		FastMethod3Buffer<int, string, bool> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append("Foo", 1, "a", true);
		buffer.Append("Foo", 2, "b", false);
		buffer.Append("Foo", 1, "a", true);

		registry.VerifyMethod(new object(), 0, "Foo",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			() => "Foo(1, a, true)").Twice();
		registry.VerifyMethod(new object(), 0, "Foo",
			(IParameterMatch<int>)It.Is(99),
			(IParameterMatch<string>)It.Is<string>("z"),
			(IParameterMatch<bool>)It.Is(false),
			() => "Foo(99, z, false)").AnyParameters().Exactly(3);

		await That(true).IsTrue();
	}

	[Test]
	public async Task Method4_FastPath_Count_AndCountAll_AreExercised()
	{
		FastMockInteractions store = new(1);
		FastMethod4Buffer<int, string, bool, double> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append("Foo", 1, "a", true, 1.5);
		buffer.Append("Foo", 2, "b", false, 2.5);

		registry.VerifyMethod(new object(), 0, "Foo",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(1.5),
			() => "Foo(1, a, true, 1.5)").Once();
		registry.VerifyMethod(new object(), 0, "Foo",
			(IParameterMatch<int>)It.Is(99),
			(IParameterMatch<string>)It.Is<string>("z"),
			(IParameterMatch<bool>)It.Is(false),
			(IParameterMatch<double>)It.Is(0.0),
			() => "Foo(99, z, false, 0.0)").AnyParameters().Exactly(2);

		await That(true).IsTrue();
	}

	[Test]
	public async Task PropertyGetter_FastPath_Count_IsExercised()
	{
		FastMockInteractions store = new(1);
		FastPropertyGetterBuffer buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append();
		buffer.Append();

		registry.VerifyPropertyTyped(new object(), 0, "P").Twice();

		await That(true).IsTrue();
	}

	[Test]
	public async Task PropertySetter_FastPath_Count_IsExercised()
	{
		FastMockInteractions store = new(1);
		FastPropertySetterBuffer<int> buffer = new(store, 0);
		MockRegistry registry = new(MockBehavior.Default, store);

		buffer.Append("P", 1);
		buffer.Append("P", 2);
		buffer.Append("P", 1);

		registry.VerifyPropertyTyped(new object(), 0, "P", (IParameterMatch<int>)It.Is(1)).Twice();

		await That(true).IsTrue();
	}
}
