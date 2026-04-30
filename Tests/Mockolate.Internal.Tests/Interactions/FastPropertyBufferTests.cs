using System.Linq;
using Mockolate.Interactions;

namespace Mockolate.Internal.Tests.Interactions;

public class FastPropertyBufferTests
{
	[Test]
	public async Task PropertyGet_BufferRecordsArePreservedOnEnumeration()
	{
		IFastPropertyService sut = IFastPropertyService.CreateMock();
		sut.Mock.Setup.Counter.InitializeWith(42);

		_ = sut.Counter;
		sut.Counter = 7;
		_ = sut.Counter;

		IInteraction[] all = ((IMock)sut).MockRegistry.Interactions.ToArray();
		await That(all).HasCount(3);
		await That(all[0]).IsExactly<PropertyGetterAccess>();
		await That(all[1]).IsExactly<PropertySetterAccess<int>>();
		await That(all[2]).IsExactly<PropertyGetterAccess>();
	}

	[Test]
	public async Task PropertyGet_RoutesThroughTypedBuffer()
	{
		IFastPropertyService sut = IFastPropertyService.CreateMock();
		sut.Mock.Setup.Counter.InitializeWith(42);

		_ = sut.Counter;
		_ = sut.Counter;

		FastMockInteractions fast = (FastMockInteractions)((IMock)sut).MockRegistry.Interactions;
		int getterId = Mock.FastPropertyBufferTests_IFastPropertyService.MemberId_Counter_Get;
		FastPropertyGetterBuffer? buffer = fast.Buffers[getterId] as FastPropertyGetterBuffer;
		await That(buffer).IsNotNull();
		await That(buffer!.Count).IsEqualTo(2);
		await That(fast.Count).IsEqualTo(2);
	}

	[Test]
	public async Task PropertySet_RoutesThroughTypedBuffer()
	{
		IFastPropertyService sut = IFastPropertyService.CreateMock();

		sut.Counter = 7;
		sut.Counter = 9;
		sut.Counter = 11;

		FastMockInteractions fast = (FastMockInteractions)((IMock)sut).MockRegistry.Interactions;
		int setterId = Mock.FastPropertyBufferTests_IFastPropertyService.MemberId_Counter_Set;
		FastPropertySetterBuffer<int>? buffer = fast.Buffers[setterId] as FastPropertySetterBuffer<int>;
		await That(buffer).IsNotNull();
		await That(buffer!.Count).IsEqualTo(3);
	}

	[Test]
	public async Task SkipInteractionRecording_DoesNotAppendToBuffer()
	{
		MockBehavior behavior = MockBehavior.Default with
		{
			SkipInteractionRecording = true,
		};
		IFastPropertyService sut = IFastPropertyService.CreateMock(behavior);
		sut.Mock.Setup.Counter.InitializeWith(42);

		_ = sut.Counter;
		_ = sut.Counter;

		FastMockInteractions fast = (FastMockInteractions)((IMock)sut).MockRegistry.Interactions;
		int getterId = Mock.FastPropertyBufferTests_IFastPropertyService.MemberId_Counter_Get;
		await That(fast.Buffers[getterId]).IsNull();
		await That(fast.Count).IsEqualTo(0);
	}

	public interface IFastPropertyService
	{
		int Counter { get; set; }
	}
}
