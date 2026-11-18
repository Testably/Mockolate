using System.Reflection;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Interactions;

public class RecordsConsumeMatchingTests
{
	[Test]
	public async Task EventRecords_ConsumeMatching_ShouldReturnCount()
	{
		FastMockInteractions store = new(1);
		EventRecords buffer = new(store, 0);

		await That(buffer.ConsumeAll()).IsEqualTo(0);

		MethodInfo method = typeof(RecordsConsumeMatchingTests).GetMethod(
			nameof(EventRecords_ConsumeMatching_ShouldReturnCount))!;
		buffer.AppendSubscription("OnFoo", null, method);
		buffer.AppendSubscription("OnFoo", null, method);

		await That(buffer.ConsumeAll()).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerGetterRecords1_ConsumeMatching_ShouldHonorMatcher()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int> buffer = new(store, 0);

		buffer.Append(1);
		buffer.Append(2);
		buffer.Append(1);

		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.IsAny<int>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(1))).IsEqualTo(2);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(99))).IsEqualTo(0);
	}

	[Test]
	public async Task IndexerGetterRecords2_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int, string> buffer = new(store, 0);

		buffer.Append(1, "a");
		buffer.Append(1, "b");
		buffer.Append(2, "a");

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"))).IsEqualTo(1);
	}

	[Test]
	public async Task IndexerGetterRecords3_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int, string, bool> buffer = new(store, 0);

		buffer.Append(1, "a", true);
		buffer.Append(2, "a", false);
		buffer.Append(1, "b", true);

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.Is(true))).IsEqualTo(2);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(false))).IsEqualTo(0);
	}

	[Test]
	public async Task IndexerGetterRecords4_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int, string, bool, double> buffer = new(store, 0);

		buffer.Append(1, "a", true, 1.0);
		buffer.Append(1, "b", true, 2.0);
		buffer.Append(2, "a", false, 3.0);

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(2);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(99),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(0);
	}

	[Test]
	public async Task IndexerSetterRecords1_ConsumeMatching_ShouldHonorMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string> buffer = new(store, 0);

		buffer.Append(1, "a");
		buffer.Append(1, "b");
		buffer.Append(2, "a");

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"))).IsEqualTo(1);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(99),
			(IParameterMatch<string>)It.IsAny<string>())).IsEqualTo(0);
	}

	[Test]
	public async Task IndexerSetterRecords2_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool> buffer = new(store, 0);

		buffer.Append(1, "a", true);
		buffer.Append(1, "b", false);
		buffer.Append(2, "a", true);

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.Is(true))).IsEqualTo(1);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(99),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>())).IsEqualTo(0);
	}

	[Test]
	public async Task IndexerSetterRecords3_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool, double> buffer = new(store, 0);

		buffer.Append(1, "a", true, 1.0);
		buffer.Append(1, "b", true, 2.0);
		buffer.Append(2, "a", false, 1.0);

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(1.0))).IsEqualTo(1);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(99),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(0);
	}

	[Test]
	public async Task IndexerSetterRecords4_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool, double, char> buffer = new(store, 0);

		buffer.Append(1, "a", true, 1.0, 'x');
		buffer.Append(1, "b", true, 2.0, 'x');
		buffer.Append(2, "a", false, 3.0, 'y');

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>(),
			(IParameterMatch<char>)It.IsAny<char>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>(),
			(IParameterMatch<char>)It.Is('x'))).IsEqualTo(2);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(99),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>(),
			(IParameterMatch<char>)It.IsAny<char>())).IsEqualTo(0);
	}

	[Test]
	public async Task MethodRecords0_ConsumeMatching_ShouldReturnCount()
	{
		FastMockInteractions store = new(1);
		MethodRecords buffer = new(store, 0);

		await That(buffer.ConsumeAll()).IsEqualTo(0);

		buffer.Append("Foo");
		buffer.Append("Foo");
		buffer.Append("Foo");

		await That(buffer.ConsumeAll()).IsEqualTo(3);
	}

	[Test]
	public async Task MethodRecords1_ConsumeAll_OnEmptyBuffer_ShouldReturnZeroWithoutThrowing()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		int consumed = buffer.ConsumeAll();

		await That(consumed).IsEqualTo(0)
			.Because(
				"the `slot < n` loop bound in Method1 ConsumeAll must keep an empty buffer out of the loop; entering it at slot 0 would index past the verified flags, which are sized to the published slots");
	}

	[Test]
	public async Task MethodRecords1_ConsumeMatching_ShouldHonorMatcher()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		buffer.Append("Foo", 1);
		buffer.Append("Foo", 2);
		buffer.Append("Foo", 3);

		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.IsAny<int>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(2))).IsEqualTo(1);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(99))).IsEqualTo(0);
	}

	[Test]
	public async Task MethodRecords2_ConsumeAll_OnEmptyBuffer_ShouldReturnZeroWithoutThrowing()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);

		int consumed = buffer.ConsumeAll();

		await That(consumed).IsEqualTo(0);
	}

	[Test]
	public async Task MethodRecords2_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);

		buffer.Append("Foo", 1, "a");
		buffer.Append("Foo", 1, "b");
		buffer.Append("Foo", 2, "a");

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>())).IsEqualTo(2);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"))).IsEqualTo(1);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(99),
			(IParameterMatch<string>)It.IsAny<string>())).IsEqualTo(0);
	}

	[Test]
	public async Task MethodRecords3_ConsumeAll_OnEmptyBuffer_ShouldReturnZeroWithoutThrowing()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = new(store, 0);

		int consumed = buffer.ConsumeAll();

		await That(consumed).IsEqualTo(0);
	}

	[Test]
	public async Task MethodRecords3_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = new(store, 0);

		buffer.Append("Foo", 1, "a", true);
		buffer.Append("Foo", 2, "a", false);
		buffer.Append("Foo", 1, "b", true);

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.Is(true))).IsEqualTo(2);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.Is(false))).IsEqualTo(0);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.Is<string>("b"),
			(IParameterMatch<bool>)It.Is(false))).IsEqualTo(0);
	}

	[Test]
	public async Task MethodRecords4_ConsumeAll_OnEmptyBuffer_ShouldReturnZeroWithoutThrowing()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer = new(store, 0);

		int consumed = buffer.ConsumeAll();

		await That(consumed).IsEqualTo(0);
	}

	[Test]
	public async Task MethodRecords4_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer = new(store, 0);

		buffer.Append("Foo", 1, "a", true, 1.0);
		buffer.Append("Foo", 1, "b", true, 2.0);
		buffer.Append("Foo", 2, "a", false, 3.0);

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(2);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(99),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(0);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.Is(false),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(0);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.Is(99.0))).IsEqualTo(0);
	}

	[Test]
	public async Task PropertyGetterRecords_ConsumeMatching_ShouldReturnCount()
	{
		FastMockInteractions store = new(1);
		PropertyGetterRecords buffer = new(store, 0);

		await That(buffer.ConsumeAll()).IsEqualTo(0);

		buffer.Append();
		buffer.Append();

		await That(buffer.ConsumeAll()).IsEqualTo(2);
	}

	[Test]
	public async Task PropertySetterRecords_ConsumeMatching_ShouldHonorMatcher()
	{
		FastMockInteractions store = new(1);
		PropertySetterRecords<int> buffer = new(store, 0);

		buffer.Append("Bar", 1);
		buffer.Append("Bar", 2);
		buffer.Append("Bar", 1);

		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.IsAny<int>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(1))).IsEqualTo(2);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(99))).IsEqualTo(0);
	}
}
