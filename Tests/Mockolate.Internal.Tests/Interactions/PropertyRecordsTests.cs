using System.Linq;
using Mockolate.Interactions;

namespace Mockolate.Internal.Tests.Interactions;

public class PropertyRecordsTests
{
	[Test]
	public async Task PropertyGet_BufferRecordsArePreservedOnEnumeration()
	{
		IPropertyService sut = IPropertyService.CreateMock();
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
		IPropertyService sut = IPropertyService.CreateMock();
		sut.Mock.Setup.Counter.InitializeWith(42);

		_ = sut.Counter;
		_ = sut.Counter;

		MockInteractions mockInteractions = (MockInteractions)((IMock)sut).MockRegistry.Interactions;
		int getterId = Mock.PropertyRecordsTests_IPropertyService.MemberId_Counter_Get;
		PropertyGetterRecords buffer = new(mockInteractions, getterId);
		await That(buffer.Count).IsEqualTo(2);
		await That(mockInteractions.Count).IsEqualTo(2);
	}

	[Test]
	public async Task PropertySet_RoutesThroughTypedBuffer()
	{
		IPropertyService sut = IPropertyService.CreateMock();

		sut.Counter = 7;
		sut.Counter = 9;
		sut.Counter = 11;

		MockInteractions mockInteractions = (MockInteractions)((IMock)sut).MockRegistry.Interactions;
		int setterId = Mock.PropertyRecordsTests_IPropertyService.MemberId_Counter_Set;
		PropertySetterRecords<int> buffer = new(mockInteractions, setterId);
		await That(buffer.Count).IsEqualTo(3);
	}

	[Test]
	public async Task SkipInteractionRecording_DoesNotAppendToBuffer()
	{
		MockBehavior behavior = MockBehavior.Default with
		{
			SkipInteractionRecording = true,
		};
		IPropertyService sut = IPropertyService.CreateMock(behavior);
		sut.Mock.Setup.Counter.InitializeWith(42);

		_ = sut.Counter;
		_ = sut.Counter;

		MockInteractions mockInteractions = (MockInteractions)((IMock)sut).MockRegistry.Interactions;
		await That(mockInteractions.Count).IsEqualTo(0);
	}

	public interface IPropertyService
	{
		int Counter { get; set; }
	}
}
