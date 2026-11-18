using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Interactions;

public class FastBufferBoxingAndUnverifiedTests
{
	[Test]
	public async Task FastEventBuffer_AppendBoxed_KindMatters()
	{
		FastMockInteractions subscribeStore = new(1);
		FastMockInteractions unsubscribeStore = new(1);
		FastEventBuffer subscribe = InstallEventSubscribe(subscribeStore, 0);
		FastEventBuffer unsubscribe = InstallEventUnsubscribe(unsubscribeStore, 0);

		subscribe.AppendSubscription("E", this, SampleMethod);
		unsubscribe.AppendUnsubscription("E", this, SampleMethod);

		await That(subscribeStore.Single()).IsExactly<EventSubscription>();
		await That(unsubscribeStore.Single()).IsExactly<EventUnsubscription>();
	}

	[Test]
	public async Task FastEventBuffer_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = InstallEventSubscribe(store, 0);
		buffer.AppendSubscription("E", this, SampleMethod);
		buffer.AppendSubscription("E", this, SampleMethod);
		buffer.AppendSubscription("E", this, SampleMethod);

		_ = buffer.ConsumeMatching();

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).IsEmpty();

		buffer.AppendSubscription("E", this, SampleMethod);
		dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(1);
		await That(dest[0]).IsExactly<EventSubscription>();
	}

	[Test]
	public async Task FastEventBuffer_Subscribe_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		// Pins the `r.Boxed ??= new EventSubscription(...)` cache in AppendBoxed. With the
		// `??=` mutated to `=`, every AppendBoxed call would allocate a fresh record.
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = InstallEventSubscribe(store, 0);
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
	public async Task FastEventBuffer_SubscribeAppend_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastEventBuffer buffer = InstallEventSubscribe(store, 0);
			return () => buffer.AppendSubscription("E", this, SampleMethod);
		});

	[Test]
	public async Task FastEventBuffer_Unsubscribe_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = InstallEventUnsubscribe(store, 0);
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
	public async Task FastEventBuffer_Unsubscribe_GetUnverifiedInteractions_ShouldBoxAsEventUnsubscription()
	{
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = InstallEventUnsubscribe(store, 0);
		buffer.AppendUnsubscription("E", this, SampleMethod);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(1);
		await That(dest[0]).IsExactly<EventUnsubscription>()
			.Because(
				"the record must pick the interaction type from its kind, so an unsubscription never surfaces as EventSubscription");
	}

	[Test]
	public async Task FastEventBuffer_UnsubscribeAppend_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastEventBuffer buffer = InstallEventUnsubscribe(store, 0);
			return () => buffer.AppendUnsubscription("E", this, SampleMethod);
		});

	[Test]
	public async Task FastIndexerGetterBuffer1_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastIndexerGetterBuffer<int> buffer = InstallIndexerGetter<int>(store, 0);
			return () => buffer.Append(1);
		});

	[Test]
	public async Task FastIndexerGetterBuffer1_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int> buffer = InstallIndexerGetter<int>(store, 0);

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
	public async Task FastIndexerGetterBuffer1_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			FastIndexerGetterBuffer<int> buffer = InstallIndexerGetter<int>(store, 0);
			IndexerGetterAccess<int> access = new(7);
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task FastIndexerGetterBuffer2_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastIndexerGetterBuffer<int, string> buffer = InstallIndexerGetter<int, string>(store, 0);
			return () => buffer.Append(1, "a");
		});

	[Test]
	public async Task FastIndexerGetterBuffer2_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string> buffer = InstallIndexerGetter<int, string>(store, 0);

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
	public async Task FastIndexerGetterBuffer2_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			FastIndexerGetterBuffer<int, string> buffer = InstallIndexerGetter<int, string>(store, 0);
			IndexerGetterAccess<int, string> access = new(7, "k");
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task FastIndexerGetterBuffer3_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastIndexerGetterBuffer<int, string, bool> buffer =
				InstallIndexerGetter<int, string, bool>(store, 0);
			return () => buffer.Append(1, "a", true);
		});

	[Test]
	public async Task FastIndexerGetterBuffer3_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string, bool> buffer =
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
	public async Task FastIndexerGetterBuffer3_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			FastIndexerGetterBuffer<int, string, bool> buffer =
				InstallIndexerGetter<int, string, bool>(store, 0);
			IndexerGetterAccess<int, string, bool> access = new(7, "k", true);
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task FastIndexerGetterBuffer4_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastIndexerGetterBuffer<int, string, bool, double> buffer =
				InstallIndexerGetter<int, string, bool, double>(store, 0);
			return () => buffer.Append(1, "a", true, 1.0);
		});

	[Test]
	public async Task FastIndexerGetterBuffer4_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string, bool, double> buffer =
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
	public async Task FastIndexerGetterBuffer4_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			FastIndexerGetterBuffer<int, string, bool, double> buffer =
				InstallIndexerGetter<int, string, bool, double>(store, 0);
			IndexerGetterAccess<int, string, bool, double> access = new(7, "k", true, 3.14);
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task FastIndexerSetterBuffer1_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastIndexerSetterBuffer<int, string> buffer = InstallIndexerSetter<int, string>(store, 0);
			return () => buffer.Append(1, "a");
		});

	[Test]
	public async Task FastIndexerSetterBuffer1_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string> buffer = InstallIndexerSetter<int, string>(store, 0);

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
	public async Task FastIndexerSetterBuffer1_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			FastIndexerSetterBuffer<int, string> buffer = InstallIndexerSetter<int, string>(store, 0);
			IndexerSetterAccess<int, string> access = new(7, "k");
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task FastIndexerSetterBuffer2_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastIndexerSetterBuffer<int, string, bool> buffer =
				InstallIndexerSetter<int, string, bool>(store, 0);
			return () => buffer.Append(1, "a", true);
		});

	[Test]
	public async Task FastIndexerSetterBuffer2_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool> buffer =
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
	public async Task FastIndexerSetterBuffer2_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			FastIndexerSetterBuffer<int, string, bool> buffer =
				InstallIndexerSetter<int, string, bool>(store, 0);
			IndexerSetterAccess<int, string, bool> access = new(7, "k", true);
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task FastIndexerSetterBuffer3_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastIndexerSetterBuffer<int, string, bool, double> buffer =
				InstallIndexerSetter<int, string, bool, double>(store, 0);
			return () => buffer.Append(1, "a", true, 1.0);
		});

	[Test]
	public async Task FastIndexerSetterBuffer3_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool, double> buffer =
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
	public async Task FastIndexerSetterBuffer3_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			FastIndexerSetterBuffer<int, string, bool, double> buffer =
				InstallIndexerSetter<int, string, bool, double>(store, 0);
			IndexerSetterAccess<int, string, bool, double> access = new(7, "k", true, 3.14);
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task FastIndexerSetterBuffer4_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastIndexerSetterBuffer<int, string, bool, double, char> buffer =
				InstallIndexerSetter<int, string, bool, double, char>(store, 0);
			return () => buffer.Append(1, "a", true, 1.0, 'x');
		});

	[Test]
	public async Task FastIndexerSetterBuffer4_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool, double, char> buffer =
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
	public async Task FastIndexerSetterBuffer4_AppendWithAccess_ShouldPublishAndRaiseInteractionAdded()
		=> await VerifyAppendWithAccessPublishesAndRaises(store =>
		{
			FastIndexerSetterBuffer<int, string, bool, double, char> buffer =
				InstallIndexerSetter<int, string, bool, double, char>(store, 0);
			IndexerSetterAccess<int, string, bool, double, char> access = new(7, "k", true, 3.14, 'z');
			return (() => buffer.Append(access), () => buffer.Count);
		});

	[Test]
	public async Task FastMethod0Buffer_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		// Pins the `r.Boxed ??= new MethodInvocation(r.Name)` cache in Method0 AppendBoxed.
		FastMockInteractions store = new(1);
		FastMethod0Buffer buffer = new(store, 0);

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
	public async Task FastMethod1Buffer_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastMethod1Buffer<int> buffer = new(store, 0);
			return () => buffer.Append("M", 1);
		});

	[Test]
	public async Task FastMethod1Buffer_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = new(store, 0);

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
	public async Task FastMethod2Buffer_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastMethod2Buffer<int, string> buffer = new(store, 0);
			return () => buffer.Append("M", 1, "a");
		});

	[Test]
	public async Task FastMethod3Buffer_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastMethod3Buffer<int, string, bool> buffer = InstallMethod<int, string, bool>(store, 0);
			return () => buffer.Append("M", 1, "a", true);
		});

	[Test]
	public async Task FastMethod3Buffer_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		// Pins the `r.Boxed ??= new MethodInvocation<T1,T2,T3>(...)` cache in Method3 AppendBoxed.
		FastMockInteractions store = new(1);
		FastMethod3Buffer<int, string, bool> buffer = InstallMethod<int, string, bool>(store, 0);

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
	public async Task FastMethod3Buffer_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastMethod3Buffer<int, string, bool> buffer = InstallMethod<int, string, bool>(store, 0);
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
	public async Task FastMethod4Buffer_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastMethod4Buffer<int, string, bool, double> buffer =
				InstallMethod<int, string, bool, double>(store, 0);
			return () => buffer.Append("M", 1, "a", true, 1.0);
		});

	[Test]
	public async Task FastMethod4Buffer_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		// Pins the `r.Boxed ??= new MethodInvocation<T1,T2,T3,T4>(...)` cache in Method4 AppendBoxed.
		FastMockInteractions store = new(1);
		FastMethod4Buffer<int, string, bool, double> buffer =
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
	public async Task FastMethod4Buffer_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastMethod4Buffer<int, string, bool, double> buffer =
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
	public async Task FastPropertyGetterBuffer_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastPropertyGetterBuffer buffer = InstallPropertyGetter(store, 0);
			return () => buffer.Append();
		});

	[Test]
	public async Task FastPropertyGetterBuffer_AppendBoxed_RepeatedCallsReturnSameSingleton()
	{
		FastMockInteractions store = new(1);
		FastPropertyGetterBuffer buffer = InstallPropertyGetter(store, 0);

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
	public async Task FastPropertyGetterBuffer_AppendBoxed_ShouldReturnTheRecordedAccessForEachRecord()
	{
		FastMockInteractions store = new(1);
		FastPropertyGetterBuffer buffer = InstallPropertyGetter(store, 0);
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
	public async Task FastPropertyGetterBuffer_AppendBoxed_ShouldKeepTheRecordedAccessAcrossSnapshots()
	{
		FastMockInteractions store = new(1);
		FastPropertyGetterBuffer buffer = InstallPropertyGetter(store, 0);
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
	public async Task FastPropertySetterBuffer_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastPropertySetterBuffer<int> buffer = InstallPropertySetter<int>(store, 0);
			return () => buffer.Append("P", 1);
		});

	[Test]
	public async Task FastPropertySetterBuffer_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastPropertySetterBuffer<int> buffer = InstallPropertySetter<int>(store, 0);

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
	public async Task FastPropertySetterBuffer_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastPropertySetterBuffer<int> buffer = InstallPropertySetter<int>(store, 0);
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
	public async Task FastPropertySetterBuffer_ConsumeMatching_ShouldMarkSlotsVerified()
	{
		FastMockInteractions store = new(1);
		FastPropertySetterBuffer<int> buffer = InstallPropertySetter<int>(store, 0);
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
	public async Task IndexerGetterBuffer1_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int> buffer = InstallIndexerGetter<int>(store, 0);
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
	public async Task IndexerGetterBuffer2_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string> buffer = InstallIndexerGetter<int, string>(store, 0);
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
	public async Task IndexerGetterBuffer3_AppendBoxed_BoxesAsIndexerGetterAccess()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string, bool> buffer =
			InstallIndexerGetter<int, string, bool>(store, 0);

		buffer.Append(7, "k", true);

		IndexerGetterAccess<int, string, bool> boxed =
			(IndexerGetterAccess<int, string, bool>)store.Single();
		await That(boxed.Parameter1).IsEqualTo(7);
		await That(boxed.Parameter2).IsEqualTo("k");
		await That(boxed.Parameter3).IsTrue();
	}

	[Test]
	public async Task IndexerGetterBuffer3_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string, bool> buffer =
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
	public async Task IndexerGetterBuffer4_AppendBoxed_BoxesAsIndexerGetterAccess()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string, bool, double> buffer =
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
	public async Task IndexerGetterBuffer4_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string, bool, double> buffer =
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
	public async Task IndexerSetterBuffer1_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string> buffer = InstallIndexerSetter<int, string>(store, 0);
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
	public async Task IndexerSetterBuffer2_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool> buffer =
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
	public async Task IndexerSetterBuffer3_AppendBoxed_BoxesAsIndexerSetterAccess()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool, double> buffer =
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
	public async Task IndexerSetterBuffer3_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool, double> buffer =
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
	public async Task IndexerSetterBuffer4_AppendBoxed_BoxesAsIndexerSetterAccess()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool, double, char> buffer =
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
	public async Task IndexerSetterBuffer4_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool, double, char> buffer =
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

	private static FastMethod3Buffer<T1, T2, T3> InstallMethod<T1, T2, T3>(FastMockInteractions store, int memberId)
		=> new FastMethod3Buffer<T1, T2, T3>(store, memberId);

	private static FastMethod4Buffer<T1, T2, T3, T4> InstallMethod<T1, T2, T3, T4>(FastMockInteractions store, int memberId)
		=> new FastMethod4Buffer<T1, T2, T3, T4>(store, memberId);

	private static FastIndexerGetterBuffer<T1> InstallIndexerGetter<T1>(FastMockInteractions store, int memberId)
		=> new FastIndexerGetterBuffer<T1>(store, memberId);

	private static FastIndexerGetterBuffer<T1, T2> InstallIndexerGetter<T1, T2>(FastMockInteractions store, int memberId)
		=> new FastIndexerGetterBuffer<T1, T2>(store, memberId);

	private static FastIndexerGetterBuffer<T1, T2, T3> InstallIndexerGetter<T1, T2, T3>(FastMockInteractions store, int memberId)
		=> new FastIndexerGetterBuffer<T1, T2, T3>(store, memberId);

	private static FastIndexerGetterBuffer<T1, T2, T3, T4> InstallIndexerGetter<T1, T2, T3, T4>(FastMockInteractions store, int memberId)
		=> new FastIndexerGetterBuffer<T1, T2, T3, T4>(store, memberId);

	private static FastIndexerSetterBuffer<T1, TValue> InstallIndexerSetter<T1, TValue>(FastMockInteractions store, int memberId)
		=> new FastIndexerSetterBuffer<T1, TValue>(store, memberId);

	private static FastIndexerSetterBuffer<T1, T2, TValue> InstallIndexerSetter<T1, T2, TValue>(FastMockInteractions store, int memberId)
		=> new FastIndexerSetterBuffer<T1, T2, TValue>(store, memberId);

	private static FastIndexerSetterBuffer<T1, T2, T3, TValue> InstallIndexerSetter<T1, T2, T3, TValue>(FastMockInteractions store, int memberId)
		=> new FastIndexerSetterBuffer<T1, T2, T3, TValue>(store, memberId);

	private static FastIndexerSetterBuffer<T1, T2, T3, T4, TValue> InstallIndexerSetter<T1, T2, T3, T4, TValue>(FastMockInteractions store, int memberId)
		=> new FastIndexerSetterBuffer<T1, T2, T3, T4, TValue>(store, memberId);

	private static FastEventBuffer InstallEventSubscribe(FastMockInteractions store, int memberId)
		=> new FastEventBuffer(store, memberId);

	private static FastEventBuffer InstallEventUnsubscribe(FastMockInteractions store, int memberId)
		=> new FastEventBuffer(store, memberId);

	private static FastPropertyGetterBuffer InstallPropertyGetter(FastMockInteractions store, int memberId)
		=> new FastPropertyGetterBuffer(store, memberId);

	private static FastPropertySetterBuffer<T> InstallPropertySetter<T>(FastMockInteractions store, int memberId)
		=> new FastPropertySetterBuffer<T>(store, memberId);

	private static async Task VerifyAppendWithAccessPublishesAndRaises(
		Func<FastMockInteractions, (Action Append, Func<int> CountReader)> setupFactory)
	{
		FastMockInteractions store = new(1);
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
		typeof(FastBufferBoxingAndUnverifiedTests).GetMethod(
			nameof(SampleMethodImpl),
			BindingFlags.Static | BindingFlags.NonPublic)!;

	private static void SampleMethodImpl()
	{
	}

	private static async Task VerifyRaisesInteractionAdded(Func<FastMockInteractions, Action> appendFactory)
	{
		FastMockInteractions store = new(1);
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
