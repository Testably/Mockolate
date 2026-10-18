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

		subscribe.Append("E", this, SampleMethod);
		unsubscribe.Append("E", this, SampleMethod);

		await That(subscribeStore.Single()).IsExactly<EventSubscription>();
		await That(unsubscribeStore.Single()).IsExactly<EventUnsubscription>();
	}

	[Test]
	public async Task FastEventBuffer_AppendBoxedUnverified_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = InstallEventSubscribe(store, 0);
		buffer.Append("E", this, SampleMethod);
		buffer.Append("E", this, SampleMethod);
		buffer.Append("E", this, SampleMethod);

		_ = buffer.ConsumeMatching();

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).IsEmpty();

		buffer.Append("E", this, SampleMethod);
		dest.Clear();
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(1);
		await That(dest[0].Interaction).IsExactly<EventSubscription>();
	}

	[Test]
	public async Task FastEventBuffer_Subscribe_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		// Pins the `r.Boxed ??= new EventSubscription(...)` cache in AppendBoxed. With the
		// `??=` mutated to `=`, every AppendBoxed call would allocate a fresh record.
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = InstallEventSubscribe(store, 0);
		buffer.Append("E", this, SampleMethod);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastEventBuffer_Subscribe_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		// Pins the `r.Boxed ??= new EventSubscription(...)` cache in AppendBoxedUnverified.
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = InstallEventSubscribe(store, 0);
		buffer.Append("E", this, SampleMethod);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastEventBuffer_SubscribeAppend_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastEventBuffer buffer = InstallEventSubscribe(store, 0);
			return () => buffer.Append("E", this, SampleMethod);
		});

	[Test]
	public async Task FastEventBuffer_Unsubscribe_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = InstallEventUnsubscribe(store, 0);
		buffer.Append("E", this, SampleMethod);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastEventBuffer_Unsubscribe_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = InstallEventUnsubscribe(store, 0);
		buffer.Append("E", this, SampleMethod);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastEventBuffer_Unsubscribe_AppendBoxedUnverified_ShouldBoxAsEventUnsubscription()
	{
		FastMockInteractions store = new(1);
		FastEventBuffer buffer = InstallEventUnsubscribe(store, 0);
		buffer.Append("E", this, SampleMethod);

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(1);
		await That(dest[0].Interaction).IsExactly<EventUnsubscription>()
			.Because(
				"the `_kind == FastEventBufferKind.Subscribe` ternary in AppendBoxedUnverified must pick the record type from the buffer kind, so an unsubscribe buffer never surfaces EventSubscription");
	}

	[Test]
	public async Task FastEventBuffer_UnsubscribeAppend_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastEventBuffer buffer = InstallEventUnsubscribe(store, 0);
			return () => buffer.Append("E", this, SampleMethod);
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

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastIndexerGetterBuffer1_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int> buffer = InstallIndexerGetter<int>(store, 0);

		buffer.Append(7);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
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

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastIndexerGetterBuffer2_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string> buffer = InstallIndexerGetter<int, string>(store, 0);

		buffer.Append(7, "k");

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
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

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastIndexerGetterBuffer3_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string, bool> buffer =
			InstallIndexerGetter<int, string, bool>(store, 0);

		buffer.Append(7, "k", true);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
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

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastIndexerGetterBuffer4_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string, bool, double> buffer =
			InstallIndexerGetter<int, string, bool, double>(store, 0);

		buffer.Append(7, "k", true, 3.14);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
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

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastIndexerSetterBuffer1_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string> buffer = InstallIndexerSetter<int, string>(store, 0);

		buffer.Append(7, "k");

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
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

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastIndexerSetterBuffer2_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool> buffer =
			InstallIndexerSetter<int, string, bool>(store, 0);

		buffer.Append(7, "k", true);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
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

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastIndexerSetterBuffer3_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool, double> buffer =
			InstallIndexerSetter<int, string, bool, double>(store, 0);

		buffer.Append(7, "k", true, 3.14);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
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

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastIndexerSetterBuffer4_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string, bool, double, char> buffer =
			InstallIndexerSetter<int, string, bool, double, char>(store, 0);

		buffer.Append(7, "k", true, 3.14, 'z');

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
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
		FastMethod0Buffer buffer = store.GetOrCreateBuffer<FastMethod0Buffer>(0,
			static f => new FastMethod0Buffer(f));

		buffer.Append("M");

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastMethod1Buffer_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastMethod1Buffer<int> buffer = store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
				static f => new FastMethod1Buffer<int>(f));
			return () => buffer.Append("M", 1);
		});

	[Test]
	public async Task FastMethod1Buffer_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
			static f => new FastMethod1Buffer<int>(f));

		buffer.Append("M", 1);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastMethod2Buffer_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			FastMethod2Buffer<int, string> buffer = store.GetOrCreateBuffer<FastMethod2Buffer<int, string>>(0,
				static f => new FastMethod2Buffer<int, string>(f));
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

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastMethod3Buffer_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		// Pins the `r.Boxed ??= new MethodInvocation<T1,T2,T3>(...)` cache in Method3 AppendBoxedUnverified.
		FastMockInteractions store = new(1);
		FastMethod3Buffer<int, string, bool> buffer = InstallMethod<int, string, bool>(store, 0);

		buffer.Append("M", 1, "a", true);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastMethod3Buffer_AppendBoxedUnverified_ShouldSkipMatchedSlots()
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

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(2);
		await That(((MethodInvocation<int, string, bool>)dest[0].Interaction).Parameter1).IsEqualTo(0);
		await That(((MethodInvocation<int, string, bool>)dest[1].Interaction).Parameter1).IsEqualTo(2);
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

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastMethod4Buffer_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		// Pins the `r.Boxed ??= new MethodInvocation<T1,T2,T3,T4>(...)` cache in Method4 AppendBoxedUnverified.
		FastMockInteractions store = new(1);
		FastMethod4Buffer<int, string, bool, double> buffer =
			InstallMethod<int, string, bool, double>(store, 0);

		buffer.Append("M", 1, "a", true, 1.0);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastMethod4Buffer_AppendBoxedUnverified_ShouldSkipMatchedSlots()
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

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(2);
		await That(((MethodInvocation<int, string, bool, double>)dest[0].Interaction).Parameter1).IsEqualTo(0);
		await That(((MethodInvocation<int, string, bool, double>)dest[1].Interaction).Parameter1).IsEqualTo(2);
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

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastPropertyGetterBuffer_AppendBoxed_SharesSingletonAcrossRecords()
	{
		// All recorded getter accesses for the same property surface as one PropertyGetterAccess
		// reference. This is intentional — getters carry no parameters, so every record is
		// semantically identical, and the two reference-keyed verification paths
		// (FastMockInteractions._verified and VerificationResultExtensions.Then) tolerate
		// shared identity (the Then walker is positional, the verified filter is
		// all-or-nothing per matched property).
		FastMockInteractions store = new(1);
		FastPropertyGetterBuffer buffer = InstallPropertyGetter(store, 0);

		buffer.Append();
		buffer.Append();

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxed(dest);

		await That(dest).HasCount(2);
		await That(dest[0].Interaction).IsSameAs(dest[1].Interaction);
	}

	[Test]
	public async Task FastPropertyGetterBuffer_AppendString_LazyInitsAccessOnce()
	{
		FastMockInteractions store = new(1);
		FastPropertyGetterBuffer buffer = InstallPropertyGetter(store, 0);

		buffer.Append();
		List<(long Seq, IInteraction Interaction)> firstSnapshot = [];
		((IFastMemberBuffer)buffer).AppendBoxed(firstSnapshot);

		buffer.Append();
		List<(long Seq, IInteraction Interaction)> secondSnapshot = [];
		((IFastMemberBuffer)buffer).AppendBoxed(secondSnapshot);

		await That(firstSnapshot).HasCount(1);
		await That(secondSnapshot).HasCount(2);
		await That(secondSnapshot[0].Interaction).IsSameAs(firstSnapshot[0].Interaction);
		await That(secondSnapshot[1].Interaction).IsSameAs(firstSnapshot[0].Interaction);
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

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxed(first);
		((IFastMemberBuffer)buffer).AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastPropertySetterBuffer_AppendBoxedUnverified_CachesAndReusesAlreadyBoxedRecord()
	{
		// Mirrors the AppendBoxed caching test for the Unverified path so the `r.Boxed ??= new
		// PropertySetterAccess<T>(...)` mutation is killed there as well.
		FastMockInteractions store = new(1);
		FastPropertySetterBuffer<int> buffer = InstallPropertySetter<int>(store, 0);

		buffer.Append("P", 5);

		List<(long Seq, IInteraction Interaction)> first = [];
		List<(long Seq, IInteraction Interaction)> second = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(first);
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0].Interaction).IsSameAs(first[0].Interaction);
	}

	[Test]
	public async Task FastPropertySetterBuffer_AppendBoxedUnverified_ShouldSkipMatchedSlots()
	{
		// Covers the AppendBoxedUnverified branch on the setter buffer (NoCoverage cluster).
		FastMockInteractions store = new(1);
		FastPropertySetterBuffer<int> buffer = InstallPropertySetter<int>(store, 0);
		buffer.Append("P", 1);
		buffer.Append("P", 2);
		buffer.Append("P", 3);

		_ = buffer.ConsumeMatching((IParameterMatch<int>)It.Is(2));

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(2);
		await That(((PropertySetterAccess<int>)dest[0].Interaction).Value).IsEqualTo(1);
		await That(((PropertySetterAccess<int>)dest[1].Interaction).Value).IsEqualTo(3);
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

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).IsEmpty()
			.Because(
				"ConsumeMatching must write `_storage.VerifiedUnderLock(slot) = true`, otherwise the matched slots stay unverified and a second call would re-count the same records");
	}

	[Test]
	public async Task IndexerGetterBuffer1_AppendBoxedUnverified_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int> buffer = InstallIndexerGetter<int>(store, 0);
		buffer.Append(0);
		buffer.Append(1);
		buffer.Append(2);

		_ = buffer.ConsumeMatching((IParameterMatch<int>)It.Is(1));

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(2);
		await That(((IndexerGetterAccess<int>)dest[0].Interaction).Parameter1).IsEqualTo(0);
		await That(((IndexerGetterAccess<int>)dest[1].Interaction).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerGetterBuffer2_AppendBoxedUnverified_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int, string> buffer = InstallIndexerGetter<int, string>(store, 0);
		buffer.Append(0, "a");
		buffer.Append(1, "b");
		buffer.Append(2, "c");

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("b"));

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(2);
		await That(((IndexerGetterAccess<int, string>)dest[0].Interaction).Parameter1).IsEqualTo(0);
		await That(((IndexerGetterAccess<int, string>)dest[1].Interaction).Parameter1).IsEqualTo(2);
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
	public async Task IndexerGetterBuffer3_AppendBoxedUnverified_ShouldSkipMatchedSlots()
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

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(2);
		await That(((IndexerGetterAccess<int, string, bool>)dest[0].Interaction).Parameter1).IsEqualTo(0);
		await That(((IndexerGetterAccess<int, string, bool>)dest[1].Interaction).Parameter1).IsEqualTo(2);
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
	public async Task IndexerGetterBuffer4_AppendBoxedUnverified_ShouldSkipMatchedSlots()
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

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(2);
		await That(((IndexerGetterAccess<int, string, bool, double>)dest[0].Interaction).Parameter1).IsEqualTo(0);
		await That(((IndexerGetterAccess<int, string, bool, double>)dest[1].Interaction).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerSetterBuffer1_AppendBoxedUnverified_ShouldSkipMatchedSlots()
	{
		FastMockInteractions store = new(1);
		FastIndexerSetterBuffer<int, string> buffer = InstallIndexerSetter<int, string>(store, 0);
		buffer.Append(0, "x");
		buffer.Append(1, "y");
		buffer.Append(2, "z");

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("y"));

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(2);
		await That(((IndexerSetterAccess<int, string>)dest[0].Interaction).Parameter1).IsEqualTo(0);
		await That(((IndexerSetterAccess<int, string>)dest[1].Interaction).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerSetterBuffer2_AppendBoxedUnverified_ShouldSkipMatchedSlots()
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

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(2);
		await That(((IndexerSetterAccess<int, string, bool>)dest[0].Interaction).Parameter1).IsEqualTo(0);
		await That(((IndexerSetterAccess<int, string, bool>)dest[1].Interaction).Parameter1).IsEqualTo(2);
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
	public async Task IndexerSetterBuffer3_AppendBoxedUnverified_ShouldSkipMatchedSlots()
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

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(2);
		await That(((IndexerSetterAccess<int, string, bool, double>)dest[0].Interaction).Parameter1).IsEqualTo(0);
		await That(((IndexerSetterAccess<int, string, bool, double>)dest[1].Interaction).Parameter1).IsEqualTo(2);
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
	public async Task IndexerSetterBuffer4_AppendBoxedUnverified_ShouldSkipMatchedSlots()
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

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		await That(dest).HasCount(2);
		await That(((IndexerSetterAccess<int, string, bool, double, char>)dest[0].Interaction).Parameter1).IsEqualTo(0);
		await That(((IndexerSetterAccess<int, string, bool, double, char>)dest[1].Interaction).Parameter1).IsEqualTo(2);
	}

	private static FastMethod3Buffer<T1, T2, T3> InstallMethod<T1, T2, T3>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastMethod3Buffer<T1, T2, T3>>(memberId,
			static f => new FastMethod3Buffer<T1, T2, T3>(f));

	private static FastMethod4Buffer<T1, T2, T3, T4> InstallMethod<T1, T2, T3, T4>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastMethod4Buffer<T1, T2, T3, T4>>(memberId,
			static f => new FastMethod4Buffer<T1, T2, T3, T4>(f));

	private static FastIndexerGetterBuffer<T1> InstallIndexerGetter<T1>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastIndexerGetterBuffer<T1>>(memberId,
			static f => new FastIndexerGetterBuffer<T1>(f));

	private static FastIndexerGetterBuffer<T1, T2> InstallIndexerGetter<T1, T2>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastIndexerGetterBuffer<T1, T2>>(memberId,
			static f => new FastIndexerGetterBuffer<T1, T2>(f));

	private static FastIndexerGetterBuffer<T1, T2, T3> InstallIndexerGetter<T1, T2, T3>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastIndexerGetterBuffer<T1, T2, T3>>(memberId,
			static f => new FastIndexerGetterBuffer<T1, T2, T3>(f));

	private static FastIndexerGetterBuffer<T1, T2, T3, T4> InstallIndexerGetter<T1, T2, T3, T4>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastIndexerGetterBuffer<T1, T2, T3, T4>>(memberId,
			static f => new FastIndexerGetterBuffer<T1, T2, T3, T4>(f));

	private static FastIndexerSetterBuffer<T1, TValue> InstallIndexerSetter<T1, TValue>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastIndexerSetterBuffer<T1, TValue>>(memberId,
			static f => new FastIndexerSetterBuffer<T1, TValue>(f));

	private static FastIndexerSetterBuffer<T1, T2, TValue> InstallIndexerSetter<T1, T2, TValue>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastIndexerSetterBuffer<T1, T2, TValue>>(memberId,
			static f => new FastIndexerSetterBuffer<T1, T2, TValue>(f));

	private static FastIndexerSetterBuffer<T1, T2, T3, TValue> InstallIndexerSetter<T1, T2, T3, TValue>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastIndexerSetterBuffer<T1, T2, T3, TValue>>(memberId,
			static f => new FastIndexerSetterBuffer<T1, T2, T3, TValue>(f));

	private static FastIndexerSetterBuffer<T1, T2, T3, T4, TValue> InstallIndexerSetter<T1, T2, T3, T4, TValue>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastIndexerSetterBuffer<T1, T2, T3, T4, TValue>>(memberId,
			static f => new FastIndexerSetterBuffer<T1, T2, T3, T4, TValue>(f));

	private static FastEventBuffer InstallEventSubscribe(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastEventBuffer>(memberId,
			static f => new FastEventBuffer(f, FastEventBufferKind.Subscribe));

	private static FastEventBuffer InstallEventUnsubscribe(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastEventBuffer>(memberId,
			static f => new FastEventBuffer(f, FastEventBufferKind.Unsubscribe));

	private static FastPropertyGetterBuffer InstallPropertyGetter(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastPropertyGetterBuffer>(memberId,
			static f => new FastPropertyGetterBuffer(f, new PropertyGetterAccess(string.Empty)));

	private static FastPropertySetterBuffer<T> InstallPropertySetter<T>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastPropertySetterBuffer<T>>(memberId,
			static f => new FastPropertySetterBuffer<T>(f));

	private static async Task VerifyAppendWithAccessPublishesAndRaises(
		Func<FastMockInteractions, (Action Append, Func<int> CountReader)> setupFactory)
	{
		// The singleton-aware Append(access) overloads each end with three statements that the
		// existing tests never exercise: `_storage.Publish();`, `if (_owner.HasInteractionAddedSubscribers)`,
		// and `_owner.RaiseAdded();`. Asserting both buffer.Count and the InteractionAdded handler
		// runs pins the publish, the guard, and the raise call together — covers the L61/L63/L65
		// mutant cluster across all 8 indexer arity overloads.
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
