using System.Collections.Generic;
using System.Reflection;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests.MockEvents;

public sealed partial class SetupEventTests
{
	[Test]
	public async Task Forever_Extension_RepeatsIndefinitely()
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
	public async Task MultipleSetups_AreAllInvoked()
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
	public async Task OnlyOnce_Extension_StopsAfterSingleInvocation()
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
	public async Task OnSubscribed_Do_Action_IsInvokedOnSubscribe()
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
	public async Task OnSubscribed_Do_TargetMethod_ReceivesCorrectValues()
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
	public async Task OnSubscribed_DoesNotFireOnUnsubscribe()
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
	public async Task OnSubscribed_For_RepeatsCallbackNTimes()
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
	public async Task OnSubscribed_For2_HoldsFirstCallbackForTwoSubscriptionsBeforeAdvancing()
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
	public async Task OnSubscribed_InParallel_RunsAlongsideNextCallback()
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
	public async Task OnSubscribed_Only_StopsAfterNInvocations()
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
	public async Task OnSubscribed_When_OnlyFires_WhenPredicateMatches()
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
	public async Task OnUnsubscribed_Do_Action_IsInvokedOnUnsubscribe()
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
	public async Task OnUnsubscribed_Do_TargetMethod_ReceivesCorrectValues()
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
	public async Task OnUnsubscribed_DoesNotFireOnSubscribe()
	{
		int callCount = 0;
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnUnsubscribed.Do(() => { callCount++; });

		void Handler(string type, int amount) { }
		sut.ChocolateDispensed += Handler;

		await That(callCount).IsEqualTo(0);
	}

	[Test]
	public async Task OnUnsubscribed_For_RepeatsCallbackNTimes()
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
	public async Task OnUnsubscribed_For2_HoldsFirstCallbackForTwoUnsubscriptionsBeforeAdvancing()
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
	public async Task OnUnsubscribed_Forever_Extension_RepeatsIndefinitely()
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
	public async Task OnUnsubscribed_InParallel_RunsAlongsideNextCallback()
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
	public async Task OnUnsubscribed_Only_StopsAfterNInvocations()
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
	public async Task OnUnsubscribed_OnlyOnce_Extension_StopsAfterSingleInvocation()
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
	public async Task OnUnsubscribed_When_OnlyFires_WhenPredicateMatches()
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
	public async Task SetupDoesNotInterfereWithVerification()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Mock.Setup.ChocolateDispensed.OnSubscribed.Do(() => { });

		void Handler(string type, int amount) { }
		sut.ChocolateDispensed += Handler;

		await That(sut.Mock.Verify.ChocolateDispensed.Subscribed()).Once();
	}
}
