namespace Mockolate.Tests.MockEvents;

public sealed partial class EventInteractionsTests
{
	public sealed class WhenCallbackThrowsTests
	{
		[Test]
		public async Task Subscribe_ShouldStillRecordTheSubscription()
		{
			ThrowingCallbackEventService sut = ThrowingCallbackEventService.CreateMock();
			sut.Mock.Setup.SomeEvent.OnSubscribed
				.Do(() => throw new InvalidOperationException("callback throws"));

			void Act()
			{
				sut.SomeEvent += Handler;
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.SomeEvent.Subscribed()).Once();

			static void Handler(int value)
			{
			}
		}

		[Test]
		public async Task Unsubscribe_ShouldStillRecordTheUnsubscription()
		{
			ThrowingCallbackEventService sut = ThrowingCallbackEventService.CreateMock();
			sut.Mock.Setup.SomeEvent.OnUnsubscribed
				.Do(() => throw new InvalidOperationException("callback throws"));

			void Act()
			{
				sut.SomeEvent -= Handler;
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.SomeEvent.Unsubscribed()).Once();

			static void Handler(int value)
			{
			}
		}

		public delegate void ThrowingCallbackEventHandler(int value);

		public class ThrowingCallbackEventService
		{
#pragma warning disable CA1070
			public virtual event ThrowingCallbackEventHandler? SomeEvent
			{
				add => throw new InvalidOperationException("base add throws");
				remove => throw new InvalidOperationException("base remove throws");
			}
#pragma warning restore CA1070
		}
	}
}
