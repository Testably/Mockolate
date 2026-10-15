namespace Mockolate.Tests.MockEvents;

public sealed partial class InteractionsTests
{
	public sealed class ThrowingBaseTests
	{
		[Test]
		public async Task BaseClass_InvokesEvent_AfterUnsubscribe_ShouldNotForwardToSubscribers()
		{
			BroadcastingService sut = BroadcastingService.CreateMock();
			int received = -1;

			void Handler(int value)
			{
				received = value;
			}

			sut.Ping += Handler;
			sut.Ping -= Handler;
			sut.TriggerPing(42);

			await That(received).IsEqualTo(-1);
		}

		[Test]
		public async Task BaseClass_InvokesEvent_ShouldForwardToSubscribers()
		{
			BroadcastingService sut = BroadcastingService.CreateMock();
			int received = -1;
			sut.Ping += value => received = value;

			sut.TriggerPing(42);

			await That(received).IsEqualTo(42);
		}

		[Test]
		public async Task BaseClass_InvokesEvent_WhenSkipBaseClass_ShouldNotForwardToSubscribers()
		{
			BroadcastingService sut = BroadcastingService.CreateMock(MockBehavior.Default.SkippingBaseClass());
			int received = -1;
			sut.Ping += value => received = value;

			sut.TriggerPing(42);

			await That(received).IsEqualTo(-1);
		}

		[Test]
		public async Task
			EventSubscribe_WhenBaseClassThrows_AndSkipBaseClass_ShouldNotThrow_AndShouldRecordSubscription()
		{
			ThrowingBaseEventService
				sut = ThrowingBaseEventService.CreateMock(MockBehavior.Default.SkippingBaseClass());

			void Act()
			{
				sut.SomeEvent += Handler;
			}

			await That(Act).DoesNotThrow();
			await That(sut.Mock.Verify.SomeEvent.Subscribed()).Once();

			static void Handler(int value)
			{
			}
		}

		[Test]
		public async Task EventSubscribe_WhenBaseClassThrows_ShouldStillRecordSubscription()
		{
			ThrowingBaseEventService sut = ThrowingBaseEventService.CreateMock();

			void Act()
			{
				sut.SomeEvent += Handler;
			}

			await That(Act).Throws<InvalidOperationException>().WithMessage("base add throws");
			await That(sut.Mock.Verify.SomeEvent.Subscribed()).Once();

			static void Handler(int value)
			{
			}
		}

		[Test]
		public async Task
			EventUnsubscribe_WhenBaseClassThrows_AndSkipBaseClass_ShouldNotThrow_AndShouldRecordUnsubscription()
		{
			ThrowingBaseEventService
				sut = ThrowingBaseEventService.CreateMock(MockBehavior.Default.SkippingBaseClass());

			void Act()
			{
				sut.SomeEvent -= Handler;
			}

			await That(Act).DoesNotThrow();
			await That(sut.Mock.Verify.SomeEvent.Unsubscribed()).Once();

			static void Handler(int value)
			{
			}
		}

		[Test]
		public async Task EventUnsubscribe_WhenBaseClassThrows_ShouldStillRecordUnsubscription()
		{
			ThrowingBaseEventService sut = ThrowingBaseEventService.CreateMock();

			void Act()
			{
				sut.SomeEvent -= Handler;
			}

			await That(Act).Throws<InvalidOperationException>().WithMessage("base remove throws");
			await That(sut.Mock.Verify.SomeEvent.Unsubscribed()).Once();

			static void Handler(int value)
			{
			}
		}

		public delegate void ThrowingBaseEventHandler(int value);

		public class ThrowingBaseEventService
		{
#pragma warning disable CA1070
			public virtual event ThrowingBaseEventHandler? SomeEvent
			{
				add => throw new InvalidOperationException("base add throws");
				remove => throw new InvalidOperationException("base remove throws");
			}
#pragma warning restore CA1070
		}

		public delegate void PingDelegate(int value);

		public class BroadcastingService
		{
#pragma warning disable CA1070
			public virtual event PingDelegate? Ping;
#pragma warning restore CA1070

			public void TriggerPing(int value) => Ping?.Invoke(value);
		}
	}
}
