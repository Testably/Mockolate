using Mockolate.Exceptions;

namespace Mockolate.Tests.MockEvents;

public sealed partial class RaiseTests
{
	[Test]
	public async Task AddEvent_WithoutMethod_ShouldThrowMockException()
	{
		IRaiseEvent sut = IRaiseEvent.CreateMock();
		MockRegistry registry = ((IMock)sut).MockRegistry;

		void Act()
		{
			registry.AddEvent(nameof(IRaiseEvent.SomeEvent), this, null);
		}

		await That(Act).Throws<MockException>()
			.WithMessage("The method of an event subscription may not be null.");
	}

	[Test]
	public async Task RemoveEvent_WithoutMethod_ShouldThrowMockException()
	{
		IRaiseEvent sut = IRaiseEvent.CreateMock();
		MockRegistry registry = ((IMock)sut).MockRegistry;

		void Act()
		{
			registry.RemoveEvent(nameof(IRaiseEvent.SomeEvent), this, null);
		}

		await That(Act).Throws<MockException>()
			.WithMessage("The method of an event unsubscription may not be null.");
	}

	[Test]
	public async Task Subscription_ShouldBeRegistered()
	{
		IRaiseEvent sut = IRaiseEvent.CreateMock();
		EventHandler handler = (_, _) => { };

		sut.SomeEvent += handler;
		sut.SomeEvent += handler;

		await That(sut.Mock.Verify.SomeEvent.Subscribed()).Exactly(2);
		await That(sut.Mock.Verify.SomeEvent.Unsubscribed()).Never();
	}

	[Test]
	public async Task Unsubscription_ShouldBeRegistered()
	{
		IRaiseEvent sut = IRaiseEvent.CreateMock();
		EventHandler handler = (_, _) => { };

		sut.SomeEvent -= handler;

		await That(sut.Mock.Verify.SomeEvent.Subscribed()).Never();
		await That(sut.Mock.Verify.SomeEvent.Unsubscribed()).Once();
	}

	[Test]
	public async Task WhenMockInheritsEventMultipleTimes()
	{
		IMyEventService sut = IMyEventService.CreateMock().Implementing<IMyEventServiceBase1>();
		int callCount = 0;

		sut.SomeEvent += Subject_SomeEvent;
		sut.Mock.Raise.SomeEvent(this, "event data");
		sut.SomeEvent -= Subject_SomeEvent;

		await That(sut.Mock.Verify.SomeEvent.Subscribed()).Once();
		await That(sut.Mock.Verify.SomeEvent.Unsubscribed()).Once();
		await That(callCount).IsEqualTo(1);

		void Subject_SomeEvent(object? sender, string e)
		{
			callCount++;
		}
	}

	[Test]
	public async Task WhenSubscribedToOtherEvent_ShouldNotTrigger()
	{
		int callCount = 0;
		IRaiseEvent sut = IRaiseEvent.CreateMock();
		EventHandler handler = (_, _) => { callCount++; };

		sut.SomeOtherEvent += handler;
		sut.Mock.Raise.SomeEvent(Match.WithDefaultParameters());
		sut.Mock.Raise.SomeEvent(Match.WithDefaultParameters());
		sut.SomeOtherEvent -= handler;
		sut.Mock.Raise.SomeEvent(Match.WithDefaultParameters());
		sut.Mock.Raise.SomeEvent(Match.WithDefaultParameters());

		await That(callCount).IsEqualTo(0);
	}

	[Test]
	public async Task WhenSubscriptionThrows_ShouldNotWrapException()
	{
		IMyEventService sut = IMyEventService.CreateMock().Implementing<IMyEventServiceBase1>();

		sut.SomeEvent += SubscriptionThrowingException;

		void Act()
		{
			sut.Mock.Raise.SomeEvent(this, "event data");
		}

		await That(Act).Throws<MockException>().WithMessage("Subscription exception");

		void SubscriptionThrowingException(object? sender, string e)
		{
			throw new MockException("Subscription exception");
		}
	}

	[Test]
	public async Task WhenUnsubscribedFromOtherEvent_ShouldNotAffectOtherSubscriptions()
	{
		int callCount = 0;
		int otherCallCount = 0;
		IRaiseEvent sut = IRaiseEvent.CreateMock();
		EventHandler handler = (_, _) => { callCount++; };
		EventHandler otherHandler = (_, _) => { otherCallCount++; };

		sut.SomeEvent += handler;
		sut.SomeOtherEvent += otherHandler;
		sut.Mock.Raise.SomeEvent(Match.WithDefaultParameters());
		sut.Mock.Raise.SomeOtherEvent(Match.WithDefaultParameters());
		sut.SomeOtherEvent -= otherHandler;
		sut.Mock.Raise.SomeEvent(Match.WithDefaultParameters());
		sut.Mock.Raise.SomeOtherEvent(Match.WithDefaultParameters());
		sut.SomeEvent -= handler;
		sut.Mock.Raise.SomeEvent(Match.WithDefaultParameters());
		sut.Mock.Raise.SomeOtherEvent(Match.WithDefaultParameters());

		await That(callCount).IsEqualTo(2);
		await That(otherCallCount).IsEqualTo(1);
	}

	[Test]
	public async Task WhenUsingRaise_AnyParameters_ShouldInvokeEvent()
	{
		int callCount = 0;
		IRaiseEvent sut = IRaiseEvent.CreateMock();
		EventHandler handler = (_, _) => { callCount++; };

		sut.SomeEvent += handler;
		sut.Mock.Raise.SomeEvent(Match.WithDefaultParameters());
		sut.Mock.Raise.SomeEvent(Match.WithDefaultParameters());
		sut.SomeEvent -= handler;
		sut.Mock.Raise.SomeEvent(Match.WithDefaultParameters());
		sut.Mock.Raise.SomeEvent(Match.WithDefaultParameters());

		await That(callCount).IsEqualTo(2);
	}

	[Test]
	public async Task WhenUsingRaise_ShouldInvokeEvent()
	{
		int callCount = 0;
		IRaiseEvent sut = IRaiseEvent.CreateMock();
		EventHandler handler = (_, _) => { callCount++; };

		sut.SomeEvent += handler;
		sut.Mock.Raise.SomeEvent(this, EventArgs.Empty);
		sut.Mock.Raise.SomeEvent(this, EventArgs.Empty);
		sut.SomeEvent -= handler;
		sut.Mock.Raise.SomeEvent(this, EventArgs.Empty);
		sut.Mock.Raise.SomeEvent(this, EventArgs.Empty);

		await That(callCount).IsEqualTo(2);
	}

	[Test]
	public async Task WhenUsingRaise_WithoutRegistration_ShouldNotThrow()
	{
		IRaiseEvent sut = IRaiseEvent.CreateMock();

		void Act()
		{
			sut.Mock.Raise.SomeEvent(this, EventArgs.Empty);
		}

		await That(Act).DoesNotThrow();
	}

	public interface IMyEventService : IMyEventServiceBase1
	{
		new event EventHandler<string> SomeEvent;
	}

	public interface IMyEventServiceBase1
	{
		event EventHandler<long> SomeEvent;
	}

	public interface IRaiseEvent
	{
		event EventHandler? SomeEvent;
		event EventHandler? SomeOtherEvent;
	}
}
