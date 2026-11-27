using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Interactions;

public class RecordsBoxingAndUnverifiedTests
{
	[Test]
	public async Task EventRecords_AppendBoxed_KindMatters()
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
	public async Task EventRecords_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
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
	public async Task EventRecords_Subscribe_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
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
	public async Task EventRecords_SubscribeAppend_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			EventRecords buffer = InstallEventSubscribe(store, 0);
			return () => buffer.AppendSubscription("E", this, SampleMethod);
		});

	[Test]
	public async Task EventRecords_Unsubscribe_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
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
	public async Task EventRecords_Unsubscribe_GetUnverifiedInteractions_ShouldBoxAsEventUnsubscription()
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
	public async Task EventRecords_UnsubscribeAppend_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			EventRecords buffer = InstallEventUnsubscribe(store, 0);
			return () => buffer.AppendUnsubscription("E", this, SampleMethod);
		});

	[Test]
	public async Task IndexerGetterRecords1_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			IndexerGetterRecords<int> buffer = InstallIndexerGetter<int>(store, 0);
			return () => buffer.Append(1);
		});

	[Test]
	public async Task IndexerGetterRecords1_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		MockInteractions store = new(1);
		IndexerGetterRecords<int> buffer = InstallIndexerGetter<int>(store, 0);

		buffer.Append(7);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task IndexerGetterRecords1_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			IndexerGetterRecords<int> buffer = InstallIndexerGetter<int>(store, 0);
			IndexerGetterAccess<int> access = new(7);
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task IndexerGetterRecords2_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			IndexerGetterRecords<int, string> buffer = InstallIndexerGetter<int, string>(store, 0);
			return () => buffer.Append(1, "a");
		});

	[Test]
	public async Task IndexerGetterRecords2_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		MockInteractions store = new(1);
		IndexerGetterRecords<int, string> buffer = InstallIndexerGetter<int, string>(store, 0);

		buffer.Append(7, "k");

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task IndexerGetterRecords2_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			IndexerGetterRecords<int, string> buffer = InstallIndexerGetter<int, string>(store, 0);
			IndexerGetterAccess<int, string> access = new(7, "k");
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task IndexerGetterRecords3_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			IndexerGetterRecords<int, string, bool> buffer =
				InstallIndexerGetter<int, string, bool>(store, 0);
			return () => buffer.Append(1, "a", true);
		});

	[Test]
	public async Task IndexerGetterRecords3_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		MockInteractions store = new(1);
		IndexerGetterRecords<int, string, bool> buffer =
			InstallIndexerGetter<int, string, bool>(store, 0);

		buffer.Append(7, "k", true);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task IndexerGetterRecords3_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			IndexerGetterRecords<int, string, bool> buffer =
				InstallIndexerGetter<int, string, bool>(store, 0);
			IndexerGetterAccess<int, string, bool> access = new(7, "k", true);
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task IndexerGetterRecords4_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			IndexerGetterRecords<int, string, bool, double> buffer =
				InstallIndexerGetter<int, string, bool, double>(store, 0);
			return () => buffer.Append(1, "a", true, 1.0);
		});

	[Test]
	public async Task IndexerGetterRecords4_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		MockInteractions store = new(1);
		IndexerGetterRecords<int, string, bool, double> buffer =
			InstallIndexerGetter<int, string, bool, double>(store, 0);

		buffer.Append(7, "k", true, 3.14);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task IndexerGetterRecords4_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			IndexerGetterRecords<int, string, bool, double> buffer =
				InstallIndexerGetter<int, string, bool, double>(store, 0);
			IndexerGetterAccess<int, string, bool, double> access = new(7, "k", true, 3.14);
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task IndexerSetterRecords1_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			IndexerSetterRecords<int, string> buffer = InstallIndexerSetter<int, string>(store, 0);
			return () => buffer.Append(1, "a");
		});

	[Test]
	public async Task IndexerSetterRecords1_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		MockInteractions store = new(1);
		IndexerSetterRecords<int, string> buffer = InstallIndexerSetter<int, string>(store, 0);

		buffer.Append(7, "k");

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task IndexerSetterRecords1_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			IndexerSetterRecords<int, string> buffer = InstallIndexerSetter<int, string>(store, 0);
			IndexerSetterAccess<int, string> access = new(7, "k");
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task IndexerSetterRecords2_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			IndexerSetterRecords<int, string, bool> buffer =
				InstallIndexerSetter<int, string, bool>(store, 0);
			return () => buffer.Append(1, "a", true);
		});

	[Test]
	public async Task IndexerSetterRecords2_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		MockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool> buffer =
			InstallIndexerSetter<int, string, bool>(store, 0);

		buffer.Append(7, "k", true);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task IndexerSetterRecords2_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			IndexerSetterRecords<int, string, bool> buffer =
				InstallIndexerSetter<int, string, bool>(store, 0);
			IndexerSetterAccess<int, string, bool> access = new(7, "k", true);
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task IndexerSetterRecords3_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			IndexerSetterRecords<int, string, bool, double> buffer =
				InstallIndexerSetter<int, string, bool, double>(store, 0);
			return () => buffer.Append(1, "a", true, 1.0);
		});

	[Test]
	public async Task IndexerSetterRecords3_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		MockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool, double> buffer =
			InstallIndexerSetter<int, string, bool, double>(store, 0);

		buffer.Append(7, "k", true, 3.14);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task IndexerSetterRecords3_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			IndexerSetterRecords<int, string, bool, double> buffer =
				InstallIndexerSetter<int, string, bool, double>(store, 0);
			IndexerSetterAccess<int, string, bool, double> access = new(7, "k", true, 3.14);
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task IndexerSetterRecords4_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			IndexerSetterRecords<int, string, bool, double, char> buffer =
				InstallIndexerSetter<int, string, bool, double, char>(store, 0);
			return () => buffer.Append(1, "a", true, 1.0, 'x');
		});

	[Test]
	public async Task IndexerSetterRecords4_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		MockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool, double, char> buffer =
			InstallIndexerSetter<int, string, bool, double, char>(store, 0);

		buffer.Append(7, "k", true, 3.14, 'z');

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task IndexerSetterRecords4_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			IndexerSetterRecords<int, string, bool, double, char> buffer =
				InstallIndexerSetter<int, string, bool, double, char>(store, 0);
			IndexerSetterAccess<int, string, bool, double, char> access = new(7, "k", true, 3.14, 'z');
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task MethodRecords0_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		// Pins the `r.Boxed ??= new MethodInvocation(r.Name)` cache in Method0 AppendBoxed.
		MockInteractions store = new(1);
		MethodRecords buffer = new(store, 0);

		buffer.Append("M");

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task MethodRecords1_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			MethodRecords<int> buffer = new(store, 0);
			return () => buffer.Append("M", 1);
		});

	[Test]
	public async Task MethodRecords1_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		MockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		buffer.Append("M", 1);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task MethodRecords2_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			MethodRecords<int, string> buffer = new(store, 0);
			return () => buffer.Append("M", 1, "a");
		});

	[Test]
	public async Task MethodRecords3_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			MethodRecords<int, string, bool> buffer = InstallMethod<int, string, bool>(store, 0);
			return () => buffer.Append("M", 1, "a", true);
		});

	[Test]
	public async Task MethodRecords3_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		// Pins the `r.Boxed ??= new MethodInvocation<T1,T2,T3>(...)` cache in Method3 AppendBoxed.
		MockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = InstallMethod<int, string, bool>(store, 0);

		buffer.Append("M", 1, "a", true);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task MethodRecords3_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = InstallMethod<int, string, bool>(store, 0);
		buffer.Append("M", 0, "a", true);
		buffer.Append("M", 1, "b", false);
		buffer.Append("M", 2, "c", true);

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("b"),
			(IParameterMatch<bool>)It.Is(false));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((MethodInvocation<int, string, bool>)dest[0]).Parameter1).IsEqualTo(0);
		await That(((MethodInvocation<int, string, bool>)dest[1]).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task MethodRecords4_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			MethodRecords<int, string, bool, double> buffer =
				InstallMethod<int, string, bool, double>(store, 0);
			return () => buffer.Append("M", 1, "a", true, 1.0);
		});

	[Test]
	public async Task MethodRecords4_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		// Pins the `r.Boxed ??= new MethodInvocation<T1,T2,T3,T4>(...)` cache in Method4 AppendBoxed.
		MockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer =
			InstallMethod<int, string, bool, double>(store, 0);

		buffer.Append("M", 1, "a", true, 1.0);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task MethodRecords4_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer =
			InstallMethod<int, string, bool, double>(store, 0);
		buffer.Append("M", 0, "a", true, 0.5);
		buffer.Append("M", 1, "b", false, 1.5);
		buffer.Append("M", 2, "c", true, 2.5);

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("b"),
			(IParameterMatch<bool>)It.Is(false),
			(IParameterMatch<double>)It.Is(1.5));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((MethodInvocation<int, string, bool, double>)dest[0]).Parameter1).IsEqualTo(0);
		await That(((MethodInvocation<int, string, bool, double>)dest[1]).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task PropertyGetterRecords_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			PropertyGetterRecords buffer = InstallPropertyGetter(store, 0);
			return () => buffer.Append();
		});

	[Test]
	public async Task PropertyGetterRecords_AppendBoxed_RepeatedCallsReturnSameSingleton()
	{
		MockInteractions store = new(1);
		PropertyGetterRecords buffer = InstallPropertyGetter(store, 0);

		buffer.Append();

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task PropertyGetterRecords_AppendBoxed_ShouldReturnTheRecordedAccessForEachRecord()
	{
		MockInteractions store = new(1);
		PropertyGetterRecords buffer = InstallPropertyGetter(store, 0);
		PropertyGetterAccess access = new("P");

		buffer.Append(access);
		buffer.Append(access);

		List<IInteraction> dest = [];
		buffer.AppendBoxed(dest);
		dest.Reverse();

		await That(dest).HasCount(2);
		await That(dest[0]).IsSameAs(dest[1]);
	}

	[Test]
	public async Task PropertyGetterRecords_AppendBoxed_ShouldKeepTheRecordedAccessAcrossSnapshots()
	{
		MockInteractions store = new(1);
		PropertyGetterRecords buffer = InstallPropertyGetter(store, 0);
		PropertyGetterAccess access = new("P");

		buffer.Append(access);
		List<IInteraction> firstSnapshot = [];
		buffer.AppendBoxed(firstSnapshot);

		buffer.Append(access);
		List<IInteraction> secondSnapshot = [];
		buffer.AppendBoxed(secondSnapshot);

		await That(firstSnapshot).HasCount(1);
		await That(secondSnapshot).HasCount(2);
		await That(secondSnapshot[0]).IsSameAs(firstSnapshot[0]);
		await That(secondSnapshot[1]).IsSameAs(firstSnapshot[0]);
	}

	[Test]
	public async Task PropertySetterRecords_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			PropertySetterRecords<int> buffer = InstallPropertySetter<int>(store, 0);
			return () => buffer.Append("P", 1);
		});

	[Test]
	public async Task PropertySetterRecords_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		MockInteractions store = new(1);
		PropertySetterRecords<int> buffer = InstallPropertySetter<int>(store, 0);

		buffer.Append("P", 5);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task PropertySetterRecords_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		PropertySetterRecords<int> buffer = InstallPropertySetter<int>(store, 0);
		buffer.Append("P", 1);
		buffer.Append("P", 2);
		buffer.Append("P", 3);

		_ = buffer.ConsumeMatching((IParameterMatch<int>)It.Is(2));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((PropertySetterAccess<int>)dest[0]).Value).IsEqualTo(1);
		await That(((PropertySetterAccess<int>)dest[1]).Value).IsEqualTo(3);
	}

	[Test]
	public async Task PropertySetterRecords_ConsumeMatching_ShouldMarkSlotsVerified()
	{
		MockInteractions store = new(1);
		PropertySetterRecords<int> buffer = InstallPropertySetter<int>(store, 0);
		buffer.Append("P", 1);
		buffer.Append("P", 1);

		int firstPass = buffer.ConsumeMatching((IParameterMatch<int>)It.Is(1));
		await That(firstPass).IsEqualTo(2);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).IsEmpty()
			.Because(
				"ConsumeMatching must write `_storage.VerifiedUnderLock(slot) = true`, otherwise the matched slots stay unverified and a second call would re-count the same records");
	}

	[Test]
	public async Task IndexerGetterRecords1_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		IndexerGetterRecords<int> buffer = InstallIndexerGetter<int>(store, 0);
		buffer.Append(0);
		buffer.Append(1);
		buffer.Append(2);

		_ = buffer.ConsumeMatching((IParameterMatch<int>)It.Is(1));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((IndexerGetterAccess<int>)dest[0]).Parameter1).IsEqualTo(0);
		await That(((IndexerGetterAccess<int>)dest[1]).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerGetterRecords2_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		IndexerGetterRecords<int, string> buffer = InstallIndexerGetter<int, string>(store, 0);
		buffer.Append(0, "a");
		buffer.Append(1, "b");
		buffer.Append(2, "c");

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("b"));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((IndexerGetterAccess<int, string>)dest[0]).Parameter1).IsEqualTo(0);
		await That(((IndexerGetterAccess<int, string>)dest[1]).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerGetterRecords3_AppendBoxed_BoxesAsIndexerGetterAccess()
	{
		MockInteractions store = new(1);
		IndexerGetterRecords<int, string, bool> buffer =
			InstallIndexerGetter<int, string, bool>(store, 0);

		buffer.Append(7, "k", true);

		IndexerGetterAccess<int, string, bool> boxed =
			(IndexerGetterAccess<int, string, bool>)store.Single();
		await That(boxed.Parameter1).IsEqualTo(7);
		await That(boxed.Parameter2).IsEqualTo("k");
		await That(boxed.Parameter3).IsTrue();
	}

	[Test]
	public async Task IndexerGetterRecords3_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		IndexerGetterRecords<int, string, bool> buffer =
			InstallIndexerGetter<int, string, bool>(store, 0);
		buffer.Append(0, "a", true);
		buffer.Append(1, "b", false);
		buffer.Append(2, "c", true);

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("b"),
			(IParameterMatch<bool>)It.Is(false));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((IndexerGetterAccess<int, string, bool>)dest[0]).Parameter1).IsEqualTo(0);
		await That(((IndexerGetterAccess<int, string, bool>)dest[1]).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerGetterRecords4_AppendBoxed_BoxesAsIndexerGetterAccess()
	{
		MockInteractions store = new(1);
		IndexerGetterRecords<int, string, bool, double> buffer =
			InstallIndexerGetter<int, string, bool, double>(store, 0);

		buffer.Append(7, "k", true, 3.14);

		IndexerGetterAccess<int, string, bool, double> boxed =
			(IndexerGetterAccess<int, string, bool, double>)store.Single();
		await That(boxed.Parameter1).IsEqualTo(7);
		await That(boxed.Parameter2).IsEqualTo("k");
		await That(boxed.Parameter3).IsTrue();
		await That(boxed.Parameter4).IsEqualTo(3.14);
	}

	[Test]
	public async Task IndexerGetterRecords4_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		IndexerGetterRecords<int, string, bool, double> buffer =
			InstallIndexerGetter<int, string, bool, double>(store, 0);
		buffer.Append(0, "a", true, 0.5);
		buffer.Append(1, "b", false, 1.5);
		buffer.Append(2, "c", true, 2.5);

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("b"),
			(IParameterMatch<bool>)It.Is(false),
			(IParameterMatch<double>)It.Is(1.5));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((IndexerGetterAccess<int, string, bool, double>)dest[0]).Parameter1).IsEqualTo(0);
		await That(((IndexerGetterAccess<int, string, bool, double>)dest[1]).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerSetterRecords1_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		IndexerSetterRecords<int, string> buffer = InstallIndexerSetter<int, string>(store, 0);
		buffer.Append(0, "x");
		buffer.Append(1, "y");
		buffer.Append(2, "z");

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("y"));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((IndexerSetterAccess<int, string>)dest[0]).Parameter1).IsEqualTo(0);
		await That(((IndexerSetterAccess<int, string>)dest[1]).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerSetterRecords2_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool> buffer =
			InstallIndexerSetter<int, string, bool>(store, 0);
		buffer.Append(0, "x", true);
		buffer.Append(1, "y", false);
		buffer.Append(2, "z", true);

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("y"),
			(IParameterMatch<bool>)It.Is(false));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((IndexerSetterAccess<int, string, bool>)dest[0]).Parameter1).IsEqualTo(0);
		await That(((IndexerSetterAccess<int, string, bool>)dest[1]).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerSetterRecords3_AppendBoxed_BoxesAsIndexerSetterAccess()
	{
		MockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool, double> buffer =
			InstallIndexerSetter<int, string, bool, double>(store, 0);

		buffer.Append(7, "k", true, 3.14);

		IndexerSetterAccess<int, string, bool, double> boxed =
			(IndexerSetterAccess<int, string, bool, double>)store.Single();
		await That(boxed.Parameter1).IsEqualTo(7);
		await That(boxed.Parameter2).IsEqualTo("k");
		await That(boxed.Parameter3).IsTrue();
		await That(boxed.TypedValue).IsEqualTo(3.14);
	}

	[Test]
	public async Task IndexerSetterRecords3_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool, double> buffer =
			InstallIndexerSetter<int, string, bool, double>(store, 0);
		buffer.Append(0, "x", true, 0.5);
		buffer.Append(1, "y", false, 1.5);
		buffer.Append(2, "z", true, 2.5);

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("y"),
			(IParameterMatch<bool>)It.Is(false),
			(IParameterMatch<double>)It.Is(1.5));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((IndexerSetterAccess<int, string, bool, double>)dest[0]).Parameter1).IsEqualTo(0);
		await That(((IndexerSetterAccess<int, string, bool, double>)dest[1]).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerSetterRecords4_AppendBoxed_BoxesAsIndexerSetterAccess()
	{
		MockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool, double, char> buffer =
			InstallIndexerSetter<int, string, bool, double, char>(store, 0);

		buffer.Append(7, "k", true, 3.14, 'z');

		IndexerSetterAccess<int, string, bool, double, char> boxed =
			(IndexerSetterAccess<int, string, bool, double, char>)store.Single();
		await That(boxed.Parameter1).IsEqualTo(7);
		await That(boxed.Parameter2).IsEqualTo("k");
		await That(boxed.Parameter3).IsTrue();
		await That(boxed.Parameter4).IsEqualTo(3.14);
		await That(boxed.TypedValue).IsEqualTo('z');
	}

	[Test]
	public async Task IndexerSetterRecords4_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool, double, char> buffer =
			InstallIndexerSetter<int, string, bool, double, char>(store, 0);
		buffer.Append(0, "x", true, 0.5, 'a');
		buffer.Append(1, "y", false, 1.5, 'b');
		buffer.Append(2, "z", true, 2.5, 'c');

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("y"),
			(IParameterMatch<bool>)It.Is(false),
			(IParameterMatch<double>)It.Is(1.5),
			(IParameterMatch<char>)It.Is('b'));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((IndexerSetterAccess<int, string, bool, double, char>)dest[0]).Parameter1).IsEqualTo(0);
		await That(((IndexerSetterAccess<int, string, bool, double, char>)dest[1]).Parameter1).IsEqualTo(2);
	}

	private static MethodRecords<T1, T2, T3> InstallMethod<T1, T2, T3>(MockInteractions store, int memberId)
		=> new MethodRecords<T1, T2, T3>(store, memberId);

	private static MethodRecords<T1, T2, T3, T4> InstallMethod<T1, T2, T3, T4>(MockInteractions store, int memberId)
		=> new MethodRecords<T1, T2, T3, T4>(store, memberId);

	private static IndexerGetterRecords<T1> InstallIndexerGetter<T1>(MockInteractions store, int memberId)
		=> new IndexerGetterRecords<T1>(store, memberId);

	private static IndexerGetterRecords<T1, T2> InstallIndexerGetter<T1, T2>(MockInteractions store, int memberId)
		=> new IndexerGetterRecords<T1, T2>(store, memberId);

	private static IndexerGetterRecords<T1, T2, T3> InstallIndexerGetter<T1, T2, T3>(MockInteractions store, int memberId)
		=> new IndexerGetterRecords<T1, T2, T3>(store, memberId);

	private static IndexerGetterRecords<T1, T2, T3, T4> InstallIndexerGetter<T1, T2, T3, T4>(MockInteractions store, int memberId)
		=> new IndexerGetterRecords<T1, T2, T3, T4>(store, memberId);

	private static IndexerSetterRecords<T1, TValue> InstallIndexerSetter<T1, TValue>(MockInteractions store, int memberId)
		=> new IndexerSetterRecords<T1, TValue>(store, memberId);

	private static IndexerSetterRecords<T1, T2, TValue> InstallIndexerSetter<T1, T2, TValue>(MockInteractions store, int memberId)
		=> new IndexerSetterRecords<T1, T2, TValue>(store, memberId);

	private static IndexerSetterRecords<T1, T2, T3, TValue> InstallIndexerSetter<T1, T2, T3, TValue>(MockInteractions store, int memberId)
		=> new IndexerSetterRecords<T1, T2, T3, TValue>(store, memberId);

	private static IndexerSetterRecords<T1, T2, T3, T4, TValue> InstallIndexerSetter<T1, T2, T3, T4, TValue>(MockInteractions store, int memberId)
		=> new IndexerSetterRecords<T1, T2, T3, T4, TValue>(store, memberId);

	private static EventRecords InstallEventSubscribe(MockInteractions store, int memberId)
		=> new EventRecords(store, memberId);

	private static EventRecords InstallEventUnsubscribe(MockInteractions store, int memberId)
		=> new EventRecords(store, memberId);

	private static PropertyGetterRecords InstallPropertyGetter(MockInteractions store, int memberId)
		=> new PropertyGetterRecords(store, memberId);

	private static PropertySetterRecords<T> InstallPropertySetter<T>(MockInteractions store, int memberId)
		=> new PropertySetterRecords<T>(store, memberId);

	private static async Task VerifyAppendWithAccessPublishesAndRaises(
		Func<MockInteractions, (Action Append, Func<int> CountReader)> setupFactory)
	{
		MockInteractions store = new(1);
		(Action append, Func<int> countReader) = setupFactory(store);
		int invocations = 0;
		EventHandler handler = (_, _) => invocations++;
		store.InteractionAdded += handler;

		append();
		append();

		store.InteractionAdded -= handler;

		await That(countReader()).IsEqualTo(2);
		await That(invocations).IsEqualTo(2);
	}

	private static readonly MethodInfo SampleMethod =
		typeof(RecordsBoxingAndUnverifiedTests).GetMethod(
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
}
