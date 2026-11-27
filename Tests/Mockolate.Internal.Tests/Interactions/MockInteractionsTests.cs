using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Interactions;

public class MockInteractionsTests
{
	[Test]
	public async Task Append_ShouldRaiseInteractionAdded()
	{
		MockInteractions sut = new(1);
		MethodRecords buffer = InstallMethod(sut, 0);

		int invocations = 0;
		sut.InteractionAdded += Handler;

		buffer.Append("foo");
		buffer.Append("bar");

		sut.InteractionAdded -= Handler;

		await That(invocations).IsEqualTo(2);

		void Handler(object? sender, EventArgs e)
		{
			invocations++;
		}
	}

	[Test]
	public async Task Clear_FallbackPath_ShouldNotResurfaceOldRecordsAfterRefill()
	{
		IMockInteractions sut = new MockInteractions(0);
		MethodInvocation first = new("first");
		MethodInvocation second = new("second");

		sut.RegisterInteraction(first);
		sut.RegisterInteraction(second);

		sut.Clear();

		MethodInvocation third = new("third");
		MethodInvocation fourth = new("fourth");
		sut.RegisterInteraction(third);
		sut.RegisterInteraction(fourth);

		List<IInteraction> ordered = [..sut,];

		await That(sut.Count).IsEqualTo(2);
		await That(ordered).HasCount(2);
		await That(ordered.Contains(first)).IsFalse();
		await That(ordered.Contains(second)).IsFalse();
		await That(ordered[0]).IsSameAs(third);
		await That(ordered[1]).IsSameAs(fourth);
	}

	[Test]
	public async Task Clear_ShouldFireOnClearing()
	{
		MockInteractions sut = new(1);
		InstallMethod(sut, 0);
		int invocations = 0;
		sut.OnClearing += Handler;

		sut.Clear();

		sut.OnClearing -= Handler;

		await That(invocations).IsEqualTo(1);

		void Handler(object? sender, EventArgs e)
		{
			invocations++;
		}
	}

	[Test]
	public async Task Clear_ShouldResetAllBuffersAndCount()
	{
		MockInteractions sut = new(2);
		MethodRecords methodBuffer = InstallMethod(sut, 0);
		PropertyGetterRecords getterBuffer = InstallPropertyGetter(sut, 1);

		methodBuffer.Append("a");
		getterBuffer.Append();

		sut.Clear();

		await That(sut.Count).IsEqualTo(0);
		await That(methodBuffer.Count).IsEqualTo(0);
		await That(getterBuffer.Count).IsEqualTo(0);
		await That(sut.ToList()).IsEmpty();
	}

	[Test]
	public async Task Clear_ShouldResetPerBufferVerifiedTracking()
	{
		MockInteractions sut = new(2);
		MethodRecords<int> methodBuffer = InstallMethod<int>(sut, 0);
		PropertyGetterRecords getterBuffer = InstallPropertyGetter(sut, 1);

		methodBuffer.Append("M", 1);
		getterBuffer.Append();
		_ = methodBuffer.ConsumeMatching((IParameterMatch<int>)It.Is(1));
		_ = getterBuffer.ConsumeAll();

		sut.Clear();

		methodBuffer.Append("M", 2);
		getterBuffer.Append();

		await That(sut.GetUnverifiedInteractions()).HasCount(2);
	}

	[Test]
	public async Task Clear_ShouldResetVerifiedBookkeeping()
	{
		MockInteractions sut = new(1);
		MethodRecords methodBuffer = InstallMethod(sut, 0);
		methodBuffer.Append("first");
		List<IInteraction> all = [..sut,];
		((IMockInteractions)sut).Verified(all);

		sut.Clear();
		methodBuffer.Append("second");

		IReadOnlyCollection<IInteraction> unverified = sut.GetUnverifiedInteractions();

		await That(unverified).HasCount(1);
	}

	[Test]
	public async Task Clear_WithSharedSingletonInVerifiedSet_ShouldResurfaceItAsUnverifiedAfterReAppend()
	{
		// Pins the `_verified = null;` reset inside Clear. PropertyGetterRecords reuses a
		// single PropertyGetterAccess singleton across records (it is cached in the buffer's
		// `_access` field, which Clear does NOT reset). With the `_verified = null` write
		// removed, the verified set keeps the singleton across Clear, and a fresh post-Clear
		// Append would surface as already-verified — masking the new interaction.
		MockInteractions sut = new(1);
		PropertyGetterRecords buffer = InstallPropertyGetter(sut, 0);
		buffer.Append();

		List<IInteraction> all = [..sut,];
		((IMockInteractions)sut).Verified(all);

		sut.Clear();
		buffer.Append();

		IReadOnlyCollection<IInteraction> unverified = sut.GetUnverifiedInteractions();
		await That(unverified).HasCount(1);
	}

	[Test]
	public async Task Count_ShouldReflectAppendsAcrossBuffers()
	{
		MockInteractions sut = new(2);
		MethodRecords methodBuffer = InstallMethod(sut, 0);
		PropertyGetterRecords getterBuffer = InstallPropertyGetter(sut, 1);

		methodBuffer.Append("a");
		getterBuffer.Append();
		methodBuffer.Append("c");

		await That(sut.Count).IsEqualTo(3);
		await That(methodBuffer.Count).IsEqualTo(2);
		await That(getterBuffer.Count).IsEqualTo(1);
	}

	[Test]
	public async Task GetEnumerator_ShouldReturnInteractionsInRegistrationOrder()
	{
		MockInteractions sut = new(2);
		MethodRecords<int> methodBuffer = InstallMethod<int>(sut, 0);
		PropertySetterRecords<string> setterBuffer = new(sut, 1);

		methodBuffer.Append("Method", 1);
		setterBuffer.Append("Property", "x");
		methodBuffer.Append("Method", 2);
		setterBuffer.Append("Property", "y");

		List<IInteraction> ordered = [..sut,];

		await That(ordered).HasCount(4);
		await That(ordered[0] is MethodInvocation<int>).IsTrue();
		await That(ordered[1] is PropertySetterAccess<string>).IsTrue();
		await That(ordered[2] is MethodInvocation<int>).IsTrue();
		await That(ordered[3] is PropertySetterAccess<string>).IsTrue();
		await That(((MethodInvocation<int>)ordered[0]).Parameter1).IsEqualTo(1);
		await That(((MethodInvocation<int>)ordered[2]).Parameter1).IsEqualTo(2);
		await That(((PropertySetterAccess<string>)ordered[1]).Value).IsEqualTo("x");
		await That(((PropertySetterAccess<string>)ordered[3]).Value).IsEqualTo("y");
	}

	[Test]
	public async Task GetOrCreateFallbackBuffer_ConcurrentRace_RecordsAllInteractions()
	{
		const int threads = 8;
		const int callsPerThread = 25;
		IMockInteractions sut = new MockInteractions(0);

		using ManualResetEventSlim start = new(false);
		Task[] tasks = new Task[threads];
		for (int t = 0; t < threads; t++)
		{
			int threadId = t;
#pragma warning disable xUnit1051 // intentionally not using a cancellation token here; the threads run a short, deterministic loop
			tasks[t] = Task.Run(() =>
#pragma warning restore xUnit1051
			{
				start.Wait();
				for (int i = 0; i < callsPerThread; i++)
				{
					sut.RegisterInteraction(new MethodInvocation($"t{threadId}-{i}"));
				}
			});
		}

		start.Set();
		await Task.WhenAll(tasks);

		await That(sut.Count).IsEqualTo(threads * callsPerThread);
		await That(sut.ToList()).HasCount(threads * callsPerThread);
	}

	[Test]
	public async Task GetUnverifiedInteractions_AcrossBuffersWithInterleavedAppends_ReturnsInSequenceOrder()
	{
		// Pins the `unverified.Count > 1` boundary that gates the defensive Sort by Seq inside
		// GetUnverifiedInteractions. With multiple buffers contributing in non-Seq order (each
		// buffer is iterated in install order, but appends are interleaved across the global
		// sequence), the Sort step is what restores chronological order. Mutations that skip the
		// Sort (`< 1`, `<= 1`, `!(>1)`) leave the snapshot grouped by buffer, not by Seq.
		MockInteractions sut = new(2);
		MethodRecords<int> bufA = InstallMethod<int>(sut, 0);
		MethodRecords<int> bufB = InstallMethod<int>(sut, 1);

		bufA.Append("A", 0);
		bufB.Append("B", 1);
		bufA.Append("A", 2);
		bufB.Append("B", 3);

		IInteraction[] unverified = sut.GetUnverifiedInteractions().ToArray();

		await That(unverified).HasCount(4);
		await That(((MethodInvocation<int>)unverified[0]).Parameter1).IsEqualTo(0);
		await That(((MethodInvocation<int>)unverified[1]).Parameter1).IsEqualTo(1);
		await That(((MethodInvocation<int>)unverified[2]).Parameter1).IsEqualTo(2);
		await That(((MethodInvocation<int>)unverified[3]).Parameter1).IsEqualTo(3);
	}

	[Test]
	public async Task GetUnverifiedInteractions_AcrossMultipleBuffers_ShouldUnionFastAndSlowVerifications()
	{
		MockInteractions sut = new(2);
		MethodRecords<int> methodBuffer = InstallMethod<int>(sut, 0);
		PropertyGetterRecords getterBuffer = InstallPropertyGetter(sut, 1);

		methodBuffer.Append("M", 7);
		getterBuffer.Append();

		_ = methodBuffer.ConsumeMatching((IParameterMatch<int>)It.Is(7));
		_ = getterBuffer.ConsumeAll();

		await That(sut.GetUnverifiedInteractions()).IsEmpty();
	}

	[Test]
	public async Task GetUnverifiedInteractions_AfterMatcherLessConsumeMatching_ShouldDropMarkedSlots()
	{
		MockInteractions sut = new(1);
		MethodRecords methodBuffer = InstallMethod(sut, 0);
		methodBuffer.Append("first");
		methodBuffer.Append("second");

		_ = methodBuffer.ConsumeAll();

		await That(sut.GetUnverifiedInteractions()).IsEmpty();

		methodBuffer.Append("third");

		IReadOnlyCollection<IInteraction> unverified = sut.GetUnverifiedInteractions();
		await That(unverified).HasCount(1);
		await That(((MethodInvocation)unverified.Single()).Name).IsEqualTo("third");
	}

	[Test]
	public async Task GetUnverifiedInteractions_AfterTypedConsumeMatching_ShouldDropOnlyMatchedSlots()
	{
		MockInteractions sut = new(1);
		MethodRecords<int> methodBuffer = InstallMethod<int>(sut, 0);
		methodBuffer.Append("Dispense", 1);
		methodBuffer.Append("Dispense", 2);

		_ = methodBuffer.ConsumeMatching((IParameterMatch<int>)It.Is(1));

		IReadOnlyCollection<IInteraction> unverified = sut.GetUnverifiedInteractions();
		await That(unverified).HasCount(1);
		await That(((MethodInvocation<int>)unverified.Single()).Parameter1).IsEqualTo(2);

		_ = methodBuffer.ConsumeMatching((IParameterMatch<int>)It.Is(2));

		await That(sut.GetUnverifiedInteractions()).IsEmpty();
	}

	[Test]
	public async Task GetUnverifiedInteractions_FastPathPlusSlowPath_ShouldFilterByBoth()
	{
		MockInteractions sut = new(1);
		MethodRecords<int> methodBuffer = InstallMethod<int>(sut, 0);
		methodBuffer.Append("M", 1);
		methodBuffer.Append("M", 2);
		methodBuffer.Append("M", 3);

		_ = methodBuffer.ConsumeMatching((IParameterMatch<int>)It.Is(1));

		List<IInteraction> all = [..sut,];
		((IMockInteractions)sut).Verified([all[1],]);

		IReadOnlyCollection<IInteraction> unverified = sut.GetUnverifiedInteractions();
		await That(unverified).HasCount(1);
		await That(((MethodInvocation<int>)unverified.Single()).Parameter1).IsEqualTo(3);
	}

	[Test]
	public async Task GetUnverifiedInteractions_ShouldRespectVerifiedSet()
	{
		MockInteractions sut = new(1);
		MethodRecords methodBuffer = InstallMethod(sut, 0);
		methodBuffer.Append("first");
		methodBuffer.Append("second");

		List<IInteraction> all = [..sut,];
		((IMockInteractions)sut).Verified([all[0],]);

		IReadOnlyCollection<IInteraction> unverified = sut.GetUnverifiedInteractions();

		await That(unverified).HasCount(1);
		await That(unverified.Contains(all[0])).IsFalse();
		await That(unverified.Contains(all[1])).IsTrue();
	}

	[Test]
	public async Task GetUnverifiedInteractions_WhenNothingRecorded_ShouldReturnArrayEmptySingleton()
	{
		MockInteractions sut = new(1);
		InstallMethod(sut, 0);

		IReadOnlyCollection<IInteraction> result = sut.GetUnverifiedInteractions();

		await That(result).IsSameAs(Array.Empty<IInteraction>())
			.Because(
				"the empty path must take the `return Array.Empty<IInteraction>();` early-return rather than fall through and allocate a fresh zero-length array");
	}

	[Test]
	public async Task GetUnverifiedInteractions_WhenNothingVerified_ShouldReturnEverything()
	{
		MockInteractions sut = new(1);
		MethodRecords methodBuffer = InstallMethod(sut, 0);
		methodBuffer.Append("first");

		IReadOnlyCollection<IInteraction> unverified = sut.GetUnverifiedInteractions();

		await That(unverified).HasCount(1);
	}

	[Test]
	public async Task Push_ShouldAssignZeroBasedMonotonicPositions()
	{
		MockInteractions sut = new(0);
		InteractionRecord first = new(new MethodInvocation("Foo"));
		InteractionRecord second = new(new MethodInvocation("Foo"));
		InteractionRecord third = new(new MethodInvocation("Foo"));

		sut.Push(first);
		sut.Push(second);
		sut.Push(third);

		await That(first.Seq).IsEqualTo(0);
		await That(second.Seq).IsEqualTo(1);
		await That(third.Seq).IsEqualTo(2);
	}

	[Test]
	public async Task RegisterInteraction_FallbackPath_GetUnverifiedInteractions_ShouldRespectVerifiedSet()
	{
		IMockInteractions sut = new MockInteractions(0);
		MethodInvocation first = new("first");
		MethodInvocation second = new("second");
		sut.RegisterInteraction(first);
		sut.RegisterInteraction(second);

		sut.Verified([first,]);

		IReadOnlyCollection<IInteraction> unverified = sut.GetUnverifiedInteractions();

		await That(unverified).HasCount(1);
		await That(unverified.Contains(first)).IsFalse();
		await That(unverified.Contains(second)).IsTrue();
	}

	[Test]
	public async Task RegisterInteraction_FallbackPath_OnClearing_ShouldFireWhenClearIsInvoked()
	{
		int invocations = 0;
		IMockInteractions sut = new MockInteractions(0);
		sut.RegisterInteraction(new MethodInvocation("foo"));
		sut.OnClearing += Handler;

		sut.Clear();

		sut.OnClearing -= Handler;

		await That(invocations).IsEqualTo(1);
		await That(sut.Count).IsEqualTo(0);

		void Handler(object? sender, EventArgs e)
		{
			invocations++;
		}
	}

	[Test]
	public async Task RegisterInteraction_FallbackPath_ShouldEnumerateMultipleInRegistrationOrder()
	{
		IMockInteractions sut = new MockInteractions(0);
		MethodInvocation first = new("first");
		MethodInvocation second = new("second");

		sut.RegisterInteraction(first);
		sut.RegisterInteraction(second);

		List<IInteraction> enumerated = [.. sut,];

		await That(sut.Count).IsEqualTo(2);
		await That(enumerated).HasCount(2);
		await That(enumerated[0]).IsSameAs(first);
		await That(enumerated[1]).IsSameAs(second);
	}

	[Test]
	public async Task RegisterInteraction_FallbackPath_ShouldFireInteractionAddedPerCall()
	{
		int invocations = 0;
		IMockInteractions sut = new MockInteractions(0);
		sut.InteractionAdded += Handler;

		sut.RegisterInteraction(new MethodInvocation("foo"));
		sut.RegisterInteraction(new MethodInvocation("bar"));

		sut.InteractionAdded -= Handler;

		await That(invocations).IsEqualTo(2);

		void Handler(object? sender, EventArgs e)
		{
			invocations++;
		}
	}

	[Test]
	public async Task RegisterInteraction_FallbackPath_ShouldRecordAndEnumerate()
	{
		IMockInteractions sut = new MockInteractions(0);
		MethodInvocation interaction = new("foo");

		MethodInvocation registered = sut.RegisterInteraction(interaction);

		await That(registered).IsSameAs(interaction);
		await That(sut.Count).IsEqualTo(1);
		await That(sut.ToList()[0]).IsSameAs(interaction);
	}

	[Test]
	public async Task RegisterInteraction_WhenSkipInteractionRecording_ShouldNotRecord()
	{
		IMockInteractions sut = new MockInteractions(0, true);

		sut.RegisterInteraction(new MethodInvocation("foo"));

		await That(sut.Count).IsEqualTo(0);
	}

	[Test]
	public async Task SkipInteractionRecording_ShouldReflectConstructionValue()
	{
		MockInteractions skipping = new(0, true);
		MockInteractions recording = new(0);

		await That(skipping.SkipInteractionRecording).IsTrue();
		await That(recording.SkipInteractionRecording).IsFalse();
	}

	[Test]
	public async Task Verified_BeforeAnyAppend_OnSecondCallAddsToExistingSet()
	{
		MockInteractions sut = new(1);
		MethodRecords buffer = InstallMethod(sut, 0);

		((IMockInteractions)sut).Verified([]);

		buffer.Append("Foo");
		MethodInvocation appended = (MethodInvocation)sut.Single();
		((IMockInteractions)sut).Verified([appended,]);

		await That(sut.GetUnverifiedInteractions()).IsEmpty();
	}

	[Test]
	public async Task Verified_OnSecondCallWithDifferentItems_PreservesPreviouslyVerifiedItems()
	{
		// Pins the `_verified ??= []` lazy-init in Verified(). With the assignment turned into a
		// plain `_verified = []`, the second call would reset the verified set, so the first
		// call's entry would re-surface as "unverified".
		MockInteractions sut = new(1);
		MethodRecords buffer = InstallMethod(sut, 0);
		buffer.Append("first");
		buffer.Append("second");
		List<IInteraction> all = [..sut,];

		((IMockInteractions)sut).Verified([all[0],]);
		((IMockInteractions)sut).Verified([all[1],]);

		await That(sut.GetUnverifiedInteractions()).IsEmpty();
	}

	[Test]
	public async Task Verified_WithMultipleInteractions_AddsAllToInternalSet()
	{
		MockInteractions sut = new(1);
		MethodRecords buffer = InstallMethod(sut, 0);
		buffer.Append("first");
		buffer.Append("second");

		List<IInteraction> all = [..sut,];
		((IMockInteractions)sut).Verified(all);

		await That(sut.GetUnverifiedInteractions()).IsEmpty();
	}

	private static MethodRecords InstallMethod(MockInteractions store, int memberId)
		=> new MethodRecords(store, memberId);

	private static MethodRecords<T> InstallMethod<T>(MockInteractions store, int memberId)
		=> new MethodRecords<T>(store, memberId);

	private static PropertyGetterRecords InstallPropertyGetter(MockInteractions store, int memberId)
		=> new PropertyGetterRecords(store, memberId);

	public sealed class MethodBufferTests
	{
		[Test]
		public async Task Method0_BoxesAsMethodInvocation()
		{
			MockInteractions store = new(1);
			MethodRecords buffer = InstallMethod(store, 0);

			buffer.Append("Foo");

			MethodInvocation boxed = (MethodInvocation)store.Single();
			await That(boxed.Name).IsEqualTo("Foo");
		}

		[Test]
		public async Task Method1_BoxesAsMethodInvocationT1()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);

			buffer.Append("Foo", 42);

			MethodInvocation<int> boxed = (MethodInvocation<int>)store.Single();
			await That(boxed.Name).IsEqualTo("Foo");
			await That(boxed.Parameter1).IsEqualTo(42);
		}

		[Test]
		public async Task Method2_BoxesAsMethodInvocationT1T2()
		{
			MockInteractions store = new(1);
			MethodRecords<int, string> buffer = new(store, 0);

			buffer.Append("Foo", 42, "bar");

			MethodInvocation<int, string> boxed = (MethodInvocation<int, string>)store.Single();
			await That(boxed.Parameter1).IsEqualTo(42);
			await That(boxed.Parameter2).IsEqualTo("bar");
		}

		[Test]
		public async Task Method3_BoxesAsMethodInvocationT1T2T3()
		{
			MockInteractions store = new(1);
			MethodRecords<int, string, bool> buffer = new(store, 0);

			buffer.Append("Foo", 1, "two", true);

			MethodInvocation<int, string, bool> boxed = (MethodInvocation<int, string, bool>)store.Single();
			await That(boxed.Parameter1).IsEqualTo(1);
			await That(boxed.Parameter2).IsEqualTo("two");
			await That(boxed.Parameter3).IsTrue();
		}

		[Test]
		public async Task Method4_BoxesAsMethodInvocationT1T2T3T4()
		{
			MockInteractions store = new(1);
			MethodRecords<int, string, bool, double> buffer = new(store, 0);

			buffer.Append("Foo", 1, "two", true, 3.14);

			MethodInvocation<int, string, bool, double> boxed =
				(MethodInvocation<int, string, bool, double>)store.Single();
			await That(boxed.Parameter4).IsEqualTo(3.14);
		}

		[Test]
		public async Task ResizesAndPreservesOrder()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);

			for (int i = 0; i < 100; i++)
			{
				buffer.Append("Foo", i);
			}

			await That(buffer.Count).IsEqualTo(100);
			List<IInteraction> ordered = [..store,];
			for (int i = 0; i < 100; i++)
			{
				await That(((MethodInvocation<int>)ordered[i]).Parameter1).IsEqualTo(i);
			}
		}
	}

	public sealed class PropertyBufferTests
	{
		[Test]
		public async Task Getter_BoxesAsPropertyGetterAccess()
		{
			MockInteractions store = new(1);
			PropertyGetterRecords buffer = new(store, 0);

			buffer.Append(new PropertyGetterAccess("Foo"));

			PropertyGetterAccess boxed = (PropertyGetterAccess)store.Single();
			await That(boxed.Name).IsEqualTo("Foo");
		}

		[Test]
		public async Task Setter_BoxesAsPropertySetterAccessT()
		{
			MockInteractions store = new(1);
			PropertySetterRecords<string> buffer = new(store, 0);

			buffer.Append("Foo", "value");

			PropertySetterAccess<string> boxed = (PropertySetterAccess<string>)store.Single();
			await That(boxed.Name).IsEqualTo("Foo");
			await That(boxed.Value).IsEqualTo("value");
		}
	}

	public sealed class IndexerBufferTests
	{
		[Test]
		public async Task Getter1_BoxesAsIndexerGetterAccess()
		{
			MockInteractions store = new(1);
			IndexerGetterRecords<string> buffer = new(store, 0);

			buffer.Append("k");

			IndexerGetterAccess<string> boxed = (IndexerGetterAccess<string>)store.Single();
			await That(boxed.Parameter1).IsEqualTo("k");
		}

		[Test]
		public async Task Getter2_BoxesAsIndexerGetterAccess()
		{
			MockInteractions store = new(1);
			IndexerGetterRecords<string, int> buffer = new(store, 0);

			buffer.Append("k", 1);

			IndexerGetterAccess<string, int> boxed = (IndexerGetterAccess<string, int>)store.Single();
			await That(boxed.Parameter1).IsEqualTo("k");
			await That(boxed.Parameter2).IsEqualTo(1);
		}

		[Test]
		public async Task Setter1_BoxesAsIndexerSetterAccess()
		{
			MockInteractions store = new(1);
			IndexerSetterRecords<string, bool> buffer = new(store, 0);

			buffer.Append("k", true);

			IndexerSetterAccess<string, bool> boxed = (IndexerSetterAccess<string, bool>)store.Single();
			await That(boxed.Parameter1).IsEqualTo("k");
			await That(boxed.TypedValue).IsTrue();
		}

		[Test]
		public async Task Setter2_BoxesAsIndexerSetterAccess()
		{
			MockInteractions store = new(1);
			IndexerSetterRecords<string, int, bool> buffer = new(store, 0);

			buffer.Append("k", 7, true);

			IndexerSetterAccess<string, int, bool> boxed = (IndexerSetterAccess<string, int, bool>)store.Single();
			await That(boxed.Parameter2).IsEqualTo(7);
		}
	}

	public sealed class EventBufferTests
	{
		[Test]
		public async Task Subscribe_BoxesAsEventSubscription()
		{
			MockInteractions store = new(1);
			EventRecords buffer = new(store, 0);
			MethodInfo method = typeof(EventBufferTests).GetMethod(nameof(Subscribe_BoxesAsEventSubscription))!;

			buffer.AppendSubscription("E", this, method);

			EventSubscription boxed = (EventSubscription)store.Single();
			await That(boxed.Name).IsEqualTo("E");
			await That(boxed.Target).IsSameAs(this);
			await That(boxed.Method).IsSameAs(method);
		}

		[Test]
		public async Task Unsubscribe_BoxesAsEventUnsubscription()
		{
			MockInteractions store = new(1);
			EventRecords buffer = new(store, 0);
			MethodInfo method = typeof(EventBufferTests).GetMethod(nameof(Unsubscribe_BoxesAsEventUnsubscription))!;

			buffer.AppendUnsubscription("E", null, method);

			EventUnsubscription boxed = (EventUnsubscription)store.Single();
			await That(boxed.Name).IsEqualTo("E");
		}
	}

	public sealed class ConcurrencyTests
	{
		[Test]
		public async Task ConcurrentAppendsAcrossMembers_ShouldPreserveTotalCount()
		{
			const int threads = 8;
			const int callsPerThread = 200;
			MockInteractions store = new(threads);
			MethodRecords<int>[] buffers = new MethodRecords<int>[threads];
			for (int t = 0; t < threads; t++)
			{
				buffers[t] = InstallMethod<int>(store, t);
			}

			using ManualResetEventSlim start = new(false);
			Task[] tasks = new Task[threads];
			for (int t = 0; t < threads; t++)
			{
				int memberIndex = t;
#pragma warning disable xUnit1051 // intentionally not using a cancellation token here; the threads run a short, deterministic loop
				tasks[t] = Task.Run(() =>
#pragma warning restore xUnit1051
				{
					start.Wait();
					for (int i = 0; i < callsPerThread; i++)
					{
						buffers[memberIndex].Append("M", i);
					}
				});
			}

			start.Set();
			await Task.WhenAll(tasks);

			await That(store.Count).IsEqualTo(threads * callsPerThread);
			int sum = 0;
			foreach (MethodRecords<int> buffer in buffers)
			{
				sum += buffer.Count;
			}

			await That(sum).IsEqualTo(threads * callsPerThread);
		}
	}
}
