using Mockolate.Interactions;

namespace Mockolate.Internal.Tests.Interactions;

public class FastEventBufferTests
{
	[Test]
	public async Task EventSubscribe_RoutesThroughTypedBuffer()
	{
		IFastEventService sut = IFastEventService.CreateMock();

		sut.SomeEvent += static (_, _) => { };
		sut.SomeEvent += static (_, _) => { };

		FastMockInteractions fast = (FastMockInteractions)((IMock)sut).MockRegistry.Interactions;
		int subscribeId = Mock.FastEventBufferTests_IFastEventService.MemberId_SomeEvent_Subscribe;
		FastEventBuffer buffer = fast.GetOrCreateBuffer<FastEventBuffer>(subscribeId, static f => new FastEventBuffer(f, FastEventBufferKind.Subscribe));
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
		int unsubscribeId = Mock.FastEventBufferTests_IFastEventService.MemberId_SomeEvent_Unsubscribe;
		FastEventBuffer buffer = fast.GetOrCreateBuffer<FastEventBuffer>(unsubscribeId, static f => new FastEventBuffer(f, FastEventBufferKind.Unsubscribe));
		await That(buffer.Count).IsEqualTo(1);
	}

	public interface IFastEventService
	{
		event EventHandler SomeEvent;
	}
}
