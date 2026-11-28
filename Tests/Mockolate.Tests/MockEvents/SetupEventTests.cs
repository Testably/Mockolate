using System.Collections.Generic;
using System.Reflection;
using Mockolate.TestHelpers;

namespace Mockolate.Tests.MockEvents;

public sealed partial class SetupEventTests
{
	[Test]
	public async Task OnSubscribed_WithForever_ShouldRepeatIndefinitely()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed
			.OnSubscribed.Do(() => { callCount++; }).For(1).Forever();

		for (int i = 0; i < 10; i++)
		{
			sut.ChocolateDispensed += Handler;
		}

		await That(callCount).IsEqualTo(10);

		void Handler(string type, int amount) { }
	}

	[Test]
	public async Task OnSubscribed_WithMultipleSetups_ShouldInvokeAllOfThem()
	{
		int callCount1 = 0;
		int callCount2 = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnSubscribed.Do(() => { callCount1++; });
		sut.Mock.Setup.ChocolateDispensed.OnSubscribed.Do(() => { callCount2++; });

		sut.ChocolateDispensed += Handler;

		await That(callCount1).IsEqualTo(1);
		await That(callCount2).IsEqualTo(1);

		void Handler(string type, int amount) { }
	}

	[Test]
	public async Task OnSubscribed_WithOnlyOnce_ShouldStopAfterASingleInvocation()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnSubscribed.Do(() => { callCount++; }).OnlyOnce();

		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed += Handler;

		await That(callCount).IsEqualTo(1);

		void Handler(string type, int amount) { }
	}

	[Test]
	public async Task OnSubscribed_WithAction_ShouldInvokeItOnEverySubscription()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnSubscribed.Do(() => { callCount++; });

		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed += Handler;

		await That(callCount).IsEqualTo(2);

		void Handler(string type, int amount) { }
	}

	[Test]
	public async Task OnSubscribed_WithTargetAndMethod_ShouldPassTheSubscribedMethod()
	{
		List<MethodInfo> received = [];
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnSubscribed.Do((_, method) => { received.Add(method); });

		sut.ChocolateDispensed += Handler;

		await That(received).HasCount().EqualTo(1);
		await That(received[0].Name).Contains("Handler");

		void Handler(string type, int amount) { }
	}

	[Test]
	public async Task OnSubscribed_WhenUnsubscribing_ShouldNotExecuteTheCallback()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnSubscribed.Do(() => { callCount++; });

		void Handler(string type, int amount) { }
		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed -= Handler;

		await That(callCount).IsEqualTo(1);
	}

	[Test]
	public async Task OnSubscribed_WithFor_ShouldRepeatTheCallbackTheGivenNumberOfTimes()
	{
		int count1 = 0;
		int count2 = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed
			.OnSubscribed.Do(() => { count1++; }).For(2)
			.OnSubscribed.Do(() => { count2++; });

		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed += Handler;

		await That(count1).IsEqualTo(2);
		await That(count2).IsEqualTo(1);

		void Handler(string type, int amount) { }
	}

	[Test]
	public async Task OnSubscribed_WithFor_ShouldHoldTheFirstCallbackForTwoSubscriptionsBeforeAdvancing()
	{
		int count1 = 0;
		int count2 = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed
			.OnSubscribed.Do(() => { count1++; }).For(2)
			.OnSubscribed.Do(() => { count2++; });

		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed += Handler;

		await That(count1).IsEqualTo(3);
		await That(count2).IsEqualTo(1);

		void Handler(string type, int amount) { }
	}

	[Test]
	public async Task OnSubscribed_WithInParallel_ShouldRunAlongsideTheNextCallback()
	{
		int count1 = 0;
		int count2 = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed
			.OnSubscribed.Do(() => { count1++; }).InParallel()
			.OnSubscribed.Do(() => { count2++; });

		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed += Handler;

		await That(count1).IsEqualTo(2);
		await That(count2).IsEqualTo(2);

		void Handler(string type, int amount) { }
	}

	[Test]
	public async Task OnSubscribed_WithOnly_ShouldStopAfterTheGivenNumberOfInvocations()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnSubscribed.Do(() => { callCount++; }).Only(2);

		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed += Handler;

		await That(callCount).IsEqualTo(2);

		void Handler(string type, int amount) { }
	}

	[Test]
	public async Task OnSubscribed_WithWhen_ShouldOnlyFireWhenPredicateMatches()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnSubscribed.Do(() => { callCount++; }).When(n => n % 2 == 0);

		for (int i = 0; i < 6; i++)
		{
			sut.ChocolateDispensed += Handler;
		}

		await That(callCount).IsEqualTo(3)
			.Because("the subscriptions at index 0, 2 and 4 pass the predicate");

		void Handler(string type, int amount) { }
	}

	[Test]
	public async Task OnUnsubscribed_WithAction_ShouldInvokeItOnEveryUnsubscription()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnUnsubscribed.Do(() => { callCount++; });

		void Handler(string type, int amount) { }

		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed -= Handler;
		sut.ChocolateDispensed -= Handler;

		await That(callCount).IsEqualTo(2);
	}

	[Test]
	public async Task OnUnsubscribed_WithTargetAndMethod_ShouldPassTheUnsubscribedMethod()
	{
		List<MethodInfo> received = [];
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnUnsubscribed.Do((_, method) => { received.Add(method); });

		void Handler(string type, int amount) { }

		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed -= Handler;

		await That(received).HasCount().EqualTo(1);
		await That(received[0].Name).Contains("Handler");
	}

	[Test]
	public async Task OnUnsubscribed_WhenSubscribing_ShouldNotExecuteTheCallback()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnUnsubscribed.Do(() => { callCount++; });

		void Handler(string type, int amount) { }
		sut.ChocolateDispensed += Handler;

		await That(callCount).IsEqualTo(0);
	}

	[Test]
	public async Task OnUnsubscribed_WithFor_ShouldRepeatTheCallbackTheGivenNumberOfTimes()
	{
		int count1 = 0;
		int count2 = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed
			.OnUnsubscribed.Do(() => { count1++; }).For(2)
			.OnUnsubscribed.Do(() => { count2++; });

		void Handler(string type, int amount) { }
		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed -= Handler;
		sut.ChocolateDispensed -= Handler;
		sut.ChocolateDispensed -= Handler;

		await That(count1).IsEqualTo(2);
		await That(count2).IsEqualTo(1);
	}

	[Test]
	public async Task OnUnsubscribed_WithFor_ShouldHoldTheFirstCallbackForTwoUnsubscriptionsBeforeAdvancing()
	{
		int count1 = 0;
		int count2 = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed
			.OnUnsubscribed.Do(() => { count1++; }).For(2)
			.OnUnsubscribed.Do(() => { count2++; });

		sut.ChocolateDispensed -= Handler;
		sut.ChocolateDispensed -= Handler;
		sut.ChocolateDispensed -= Handler;
		sut.ChocolateDispensed -= Handler;

		await That(count1).IsEqualTo(3);
		await That(count2).IsEqualTo(1);

		void Handler(string type, int amount) { }
	}

	[Test]
	public async Task OnUnsubscribed_WithForever_ShouldRepeatIndefinitely()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed
			.OnUnsubscribed.Do(() => { callCount++; }).For(1).Forever();

		void Handler(string type, int amount) { }
		sut.ChocolateDispensed += Handler;
		for (int i = 0; i < 10; i++)
		{
			sut.ChocolateDispensed -= Handler;
		}

		await That(callCount).IsEqualTo(10);
	}

	[Test]
	public async Task OnUnsubscribed_WithInParallel_ShouldRunAlongsideTheNextCallback()
	{
		int count1 = 0;
		int count2 = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed
			.OnUnsubscribed.Do(() => { count1++; }).InParallel()
			.OnUnsubscribed.Do(() => { count2++; });

		void Handler(string type, int amount) { }
		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed -= Handler;
		sut.ChocolateDispensed -= Handler;

		await That(count1).IsEqualTo(2);
		await That(count2).IsEqualTo(2);
	}

	[Test]
	public async Task OnUnsubscribed_WithOnly_ShouldStopAfterTheGivenNumberOfInvocations()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnUnsubscribed.Do(() => { callCount++; }).Only(2);

		void Handler(string type, int amount) { }
		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed -= Handler;
		sut.ChocolateDispensed -= Handler;
		sut.ChocolateDispensed -= Handler;
		sut.ChocolateDispensed -= Handler;

		await That(callCount).IsEqualTo(2);
	}

	[Test]
	public async Task OnUnsubscribed_WithOnlyOnce_ShouldStopAfterASingleInvocation()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnUnsubscribed.Do(() => { callCount++; }).OnlyOnce();

		void Handler(string type, int amount) { }
		sut.ChocolateDispensed += Handler;
		sut.ChocolateDispensed -= Handler;
		sut.ChocolateDispensed -= Handler;
		sut.ChocolateDispensed -= Handler;

		await That(callCount).IsEqualTo(1);
	}

	[Test]
	public async Task OnUnsubscribed_WithWhen_ShouldOnlyFireWhenPredicateMatches()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnUnsubscribed.Do(() => { callCount++; }).When(n => n % 2 == 0);

		void Handler(string type, int amount) { }
		sut.ChocolateDispensed += Handler;
		for (int i = 0; i < 6; i++)
		{
			sut.ChocolateDispensed -= Handler;
		}

		await That(callCount).IsEqualTo(3)
			.Because("the unsubscriptions at index 0, 2 and 4 pass the predicate");
	}

	[Test]
	public async Task OnSubscribed_ShouldNotInterfereWithVerification()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnSubscribed.Do(() => { });

		void Handler(string type, int amount) { }
		sut.ChocolateDispensed += Handler;

		await That(sut.Mock.Verify.ChocolateDispensed.Subscribed()).Once();
	}
}
