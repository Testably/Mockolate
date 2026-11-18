using System.Reflection;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Interactions;

public class FastBufferConsumeMatchingTests
{
	[Test]
	public async Task FastEventBuffer_ConsumeMatching_ShouldReturnCount()
	{
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = new(store, 0);

		await That(buffer.ConsumeAll()).IsEqualTo(0);

		MethodInfo method = typeof(FastBufferConsumeMatchingTests).GetMethod(
			nameof(FastEventBuffer_ConsumeMatching_ShouldReturnCount))!;
		buffer.AppendSubscription("OnFoo", null, method);
		buffer.AppendSubscription("OnFoo", null, method);

		await That(buffer.ConsumeAll()).IsEqualTo(2);
	}

	[Test]
	public async Task FastIndexerGetterBuffer1_ConsumeMatching_ShouldHonorMatcher()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int> buffer = new(store, 0);

		buffer.Append(1);
		buffer.Append(2);
		buffer.Append(1);

		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.IsAny<int>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(1))).IsEqualTo(2);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(99))).IsEqualTo(0);
	}

	[Test]
	public async Task FastIndexerGetterBuffer2_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string> buffer = new(store, 0);

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
	public async Task FastIndexerGetterBuffer3_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string, bool> buffer = new(store, 0);

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
	public async Task FastIndexerGetterBuffer4_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string, bool, double> buffer = new(store, 0);

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
	public async Task FastIndexerSetterBuffer1_ConsumeMatching_ShouldHonorMatchers()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string> buffer = new(store, 0);

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
	public async Task FastIndexerSetterBuffer2_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool> buffer = new(store, 0);

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
	public async Task FastIndexerSetterBuffer3_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool, double> buffer = new(store, 0);

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
	public async Task FastIndexerSetterBuffer4_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool, double, char> buffer = new(store, 0);

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
	public async Task FastMethod0Buffer_ConsumeMatching_ShouldReturnCount()
	{
		FastMockInteractions store = new(1);
		FastMethod0Buffer buffer = new(store, 0);

		await That(buffer.ConsumeAll()).IsEqualTo(0);

		buffer.Append("Foo");
		buffer.Append("Foo");
		buffer.Append("Foo");

		await That(buffer.ConsumeAll()).IsEqualTo(3);
	}

	[Test]
	public async Task FastMethod1Buffer_ConsumeAll_OnEmptyBuffer_ShouldReturnZeroWithoutThrowing()
	{
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = new(store, 0);

		int consumed = buffer.ConsumeAll();

		await That(consumed).IsEqualTo(0)
			.Because(
				"the `slot < n` loop bound in Method1 ConsumeAll must keep an empty buffer out of the loop; entering it at slot 0 would index past the verified flags, which are sized to the published slots");
	}

	[Test]
	public async Task FastMethod1Buffer_ConsumeMatching_ShouldHonorMatcher()
	{
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = new(store, 0);

		buffer.Append("Foo", 1);
		buffer.Append("Foo", 2);
		buffer.Append("Foo", 3);

		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.IsAny<int>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(2))).IsEqualTo(1);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(99))).IsEqualTo(0);
	}

	[Test]
	public async Task FastMethod2Buffer_ConsumeAll_OnEmptyBuffer_ShouldReturnZeroWithoutThrowing()
	{
		FastMockInteractions store = new(1);
		FastMethod2Buffer<int, string> buffer = new(store, 0);

		int consumed = buffer.ConsumeAll();

		await That(consumed).IsEqualTo(0);
	}

	[Test]
	public async Task FastMethod2Buffer_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		FastMethod2Buffer<int, string> buffer = new(store, 0);

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
	public async Task FastMethod3Buffer_ConsumeAll_OnEmptyBuffer_ShouldReturnZeroWithoutThrowing()
	{
		FastMockInteractions store = new(1);
		FastMethod3Buffer<int, string, bool> buffer = new(store, 0);

		int consumed = buffer.ConsumeAll();

		await That(consumed).IsEqualTo(0);
	}

	[Test]
	public async Task FastMethod3Buffer_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		FastMethod3Buffer<int, string, bool> buffer = new(store, 0);

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
	public async Task FastMethod4Buffer_ConsumeAll_OnEmptyBuffer_ShouldReturnZeroWithoutThrowing()
	{
		FastMockInteractions store = new(1);
		FastMethod4Buffer<int, string, bool, double> buffer = new(store, 0);

		int consumed = buffer.ConsumeAll();

		await That(consumed).IsEqualTo(0);
	}

	[Test]
	public async Task FastMethod4Buffer_ConsumeMatching_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		FastMethod4Buffer<int, string, bool, double> buffer = new(store, 0);

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
	public async Task FastPropertyGetterBuffer_ConsumeMatching_ShouldReturnCount()
	{
		FastMockInteractions store = new(1);
		FastPropertyGetterBuffer buffer = new(store, 0);

		await That(buffer.ConsumeAll()).IsEqualTo(0);

		buffer.Append();
		buffer.Append();

		await That(buffer.ConsumeAll()).IsEqualTo(2);
	}

	[Test]
	public async Task FastPropertySetterBuffer_ConsumeMatching_ShouldHonorMatcher()
	{
		FastMockInteractions store = new(1);
		FastPropertySetterBuffer<int> buffer = new(store, 0);

		buffer.Append("Bar", 1);
		buffer.Append("Bar", 2);
		buffer.Append("Bar", 1);

		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.IsAny<int>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(1))).IsEqualTo(2);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(99))).IsEqualTo(0);
	}
}
