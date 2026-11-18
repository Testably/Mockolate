using Mockolate.Interactions;

namespace Mockolate.Internal.Tests.Interactions;

public class EventRecordsTests
{
	[Test]
	public async Task EventSubscribe_RoutesThroughTypedBuffer()
	{
		IFastEventService sut = IFastEventService.CreateMock();

		sut.SomeEvent += static (_, _) => { };
		sut.SomeEvent += static (_, _) => { };

		FastMockInteractions fast = (FastMockInteractions)((IMock)sut).MockRegistry.Interactions;
		int subscribeId = Mock.EventRecordsTests_IFastEventService.MemberId_SomeEvent_Subscribe;
		EventRecords buffer = new(fast, subscribeId);
		await That(buffer.Count).IsEqualTo(2);
	}

	[Test]
	public async Task EventUnsubscribe_RoutesThroughTypedBuffer()
	{
		IFastEventService sut = IFastEventService.CreateMock();

		EventHandler handler1 = (_, _) => { };
		EventHandler handler2 = (_, _) => { };
		sut.SomeEvent += handler1;
		sut.SomeEvent += handler2;
		sut.SomeEvent -= handler1;

		FastMockInteractions fast = (FastMockInteractions)((IMock)sut).MockRegistry.Interactions;
		int unsubscribeId = Mock.EventRecordsTests_IFastEventService.MemberId_SomeEvent_Unsubscribe;
		EventRecords buffer = new(fast, unsubscribeId);
		await That(buffer.Count).IsEqualTo(1);
	}

	public interface IFastEventService
	{
		event EventHandler SomeEvent;
	}
}
