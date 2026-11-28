namespace Mockolate.Tests.MockEvents;

public sealed partial class EventInteractionsTests
{
	public sealed class WhenBaseThrowsTests
	{
		[Test]
		public async Task WhenBaseClassRaisesTheEventAfterUnsubscribe_ShouldNotForwardToSubscribers()
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
		public async Task WhenBaseClassRaisesTheEvent_ShouldForwardToSubscribers()
		{
			BroadcastingService sut = BroadcastingService.CreateMock();
			int received = -1;
			sut.Ping += value => received = value;

			sut.TriggerPing(42);

			await That(received).IsEqualTo(42);
		}

		[Test]
		public async Task WithSkippingBaseClass_WhenBaseClassRaisesTheEvent_ShouldNotForwardToSubscribers()
		{
			BroadcastingService sut = BroadcastingService.CreateMock(MockBehavior.Default.SkippingBaseClass());
			int received = -1;
			sut.Ping += value => received = value;

			sut.TriggerPing(42);

			await That(received).IsEqualTo(-1);
		}

		[Test]
		public async Task Subscribe_WithSkippingBaseClass_ShouldRecordTheSubscriptionWithoutThrowing()
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
		public async Task Subscribe_ShouldStillRecordTheSubscription()
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
		public async Task Unsubscribe_WithSkippingBaseClass_ShouldRecordTheUnsubscriptionWithoutThrowing()
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
		public async Task Unsubscribe_ShouldStillRecordTheUnsubscription()
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
