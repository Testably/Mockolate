using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mockolate.Interactions;

namespace Mockolate.Internal.Tests.Interactions;

public class EventRecordsTests
{
	[Test]
	public async Task Subscribe_ShouldRecordInTheTypedBuffer()
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
	public async Task Unsubscribe_ShouldRecordInTheTypedBuffer()
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

	[Test]
	public async Task AppendBoxed_ShouldBoxAccordingToTheKind()
	{
		MockInteractions subscribeStore = new(1);
		MockInteractions unsubscribeStore = new(1);
		EventRecords subscribe = InstallEventSubscribe(subscribeStore, 0);
		EventRecords unsubscribe = InstallEventUnsubscribe(unsubscribeStore, 0);

		subscribe.AppendSubscription("E", this, SampleMethod);
		unsubscribe.AppendUnsubscription("E", this, SampleMethod);

		await That(subscribeStore.Single()).IsExactly<EventSubscription>();
		await That(unsubscribeStore.Single()).IsExactly<EventUnsubscription>();
	}

	[Test]
	public async Task GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		EventRecords buffer = InstallEventSubscribe(store, 0);
		buffer.AppendSubscription("E", this, SampleMethod);
		buffer.AppendSubscription("E", this, SampleMethod);
		buffer.AppendSubscription("E", this, SampleMethod);

		_ = buffer.ConsumeAll();

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).IsEmpty();

		buffer.AppendSubscription("E", this, SampleMethod);
		dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(1);
		await That(dest[0]).IsExactly<EventSubscription>();
	}

	[Test]
	public async Task AppendBoxed_WithSubscription_ShouldReuseTheBoxedRecord()
	{
		// Pins the `r.Boxed ??= new EventSubscription(...)` cache in AppendBoxed. With the
		// `??=` mutated to `=`, every AppendBoxed call would allocate a fresh record.
		MockInteractions store = new(1);
		EventRecords buffer = InstallEventSubscribe(store, 0);
		buffer.AppendSubscription("E", this, SampleMethod);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task AppendSubscription_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			EventRecords buffer = InstallEventSubscribe(store, 0);
			return () => buffer.AppendSubscription("E", this, SampleMethod);
		});

	[Test]
	public async Task AppendBoxed_WithUnsubscription_ShouldReuseTheBoxedRecord()
	{
		MockInteractions store = new(1);
		EventRecords buffer = InstallEventUnsubscribe(store, 0);
		buffer.AppendUnsubscription("E", this, SampleMethod);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task GetUnverifiedInteractions_WithUnsubscription_ShouldReturnAnEventUnsubscription()
	{
		MockInteractions store = new(1);
		EventRecords buffer = InstallEventUnsubscribe(store, 0);
		buffer.AppendUnsubscription("E", this, SampleMethod);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(1);
		await That(dest[0]).IsExactly<EventUnsubscription>()
			.Because(
				"the record must pick the interaction type from its kind, so an unsubscription never surfaces as EventSubscription");
	}

	[Test]
	public async Task AppendUnsubscription_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			EventRecords buffer = InstallEventUnsubscribe(store, 0);
			return () => buffer.AppendUnsubscription("E", this, SampleMethod);
		});

	[Test]
	public async Task ConsumeAll_ShouldReturnTheNumberOfRecords()
	{
		MockInteractions store = new(1);
		EventRecords buffer = new(store, 0);

		await That(buffer.ConsumeAll()).IsEqualTo(0);

		MethodInfo method = typeof(EventRecordsTests).GetMethod(
			nameof(ConsumeAll_ShouldReturnTheNumberOfRecords))!;
		buffer.AppendSubscription("OnFoo", null, method);
		buffer.AppendSubscription("OnFoo", null, method);

		await That(buffer.ConsumeAll()).IsEqualTo(2);
	}

	private static EventRecords InstallEventSubscribe(MockInteractions store, int memberId)
		=> new EventRecords(store, memberId);

	private static EventRecords InstallEventUnsubscribe(MockInteractions store, int memberId)
		=> new EventRecords(store, memberId);

	private static readonly MethodInfo SampleMethod =
		typeof(EventRecordsTests).GetMethod(
			nameof(SampleMethodImpl),
			BindingFlags.Static | BindingFlags.NonPublic)!;

	private static void SampleMethodImpl()
	{
	}

	private static async Task VerifyRaisesInteractionAdded(Func<MockInteractions, Action> appendFactory)
	{
		MockInteractions store = new(1);
		Action append = appendFactory(store);
		int invocations = 0;
		EventHandler handler = (_, _) => invocations++;
		store.InteractionAdded += handler;

		append();
		append();

		store.InteractionAdded -= handler;

		await That(invocations).IsEqualTo(2);
	}

	public interface IEventService
	{
		event EventHandler SomeEvent;
	}
}
