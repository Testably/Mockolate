using Mockolate.Interactions;

namespace Mockolate.Internal.Tests.Interactions;

public class EventRecordsTests
{
	[Test]
	public async Task EventSubscribe_RoutesThroughTypedBuffer()
	{
		IEventService sut = IEventService.CreateMock();

		sut.SomeEvent += static (_, _) => { };
		sut.SomeEvent += static (_, _) => { };

		MockInteractions mockInteractions = (MockInteractions)((IMock)sut).MockRegistry.Interactions;
		int subscribeId = Mock.EventRecordsTests_IEventService.MemberId_SomeEvent_Subscribe;
		EventRecords buffer = new(mockInteractions, subscribeId);
		await That(buffer.Count).IsEqualTo(2);
	}

	[Test]
	public async Task EventUnsubscribe_RoutesThroughTypedBuffer()
	{
		IEventService sut = IEventService.CreateMock();

		EventHandler handler1 = (_, _) => { };
		EventHandler handler2 = (_, _) => { };
		sut.SomeEvent += handler1;
		sut.SomeEvent += handler2;
		sut.SomeEvent -= handler1;

		MockInteractions mockInteractions = (MockInteractions)((IMock)sut).MockRegistry.Interactions;
		int unsubscribeId = Mock.EventRecordsTests_IEventService.MemberId_SomeEvent_Unsubscribe;
		EventRecords buffer = new(mockInteractions, unsubscribeId);
		await That(buffer.Count).IsEqualTo(1);
	}

	public interface IEventService
	{
		event EventHandler SomeEvent;
	}
}
