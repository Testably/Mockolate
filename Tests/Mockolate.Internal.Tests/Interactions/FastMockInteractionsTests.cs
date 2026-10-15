using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Interactions;

public class FastMockInteractionsTests
{
	[Test]
	public async Task Append_ShouldRaiseInteractionAdded()
	{
		FastMockInteractions sut = new(1);
		FastMethod0Buffer buffer = InstallMethod(sut, 0);

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
	public async Task Buffers_ShouldExposeInstalledBuffersByMemberId()
	{
		FastMockInteractions sut = new(3);
		FastMethod0Buffer method = InstallMethod(sut, 0);
		FastPropertyGetterBuffer getter = InstallPropertyGetter(sut, 1);
		FastEventBuffer subscribe = sut.GetOrCreateBuffer<FastEventBuffer>(2,
			static f => new FastEventBuffer(f, FastEventBufferKind.Subscribe));

		await That(sut.Buffers).HasCount(3);
		await That(sut.Buffers[0]).IsSameAs(method);
		await That(sut.Buffers[1]).IsSameAs(getter);
		await That(sut.Buffers[2]).IsSameAs(subscribe);
	}

	[Test]
	public async Task Clear_FallbackPath_ShouldClearStrongReferencesInRecordsArray()
	{
		// Pins the `Array.Clear(_records, 0, _count);` call inside FallbackBuffer.Clear. With
		// the statement removed, _count is still reset to 0 but the underlying _records array
		// keeps strong references to the cleared interactions. The behavioral surface (Count,
		// enumeration) is unchanged — but the array contents leak. Inspect via reflection.
		IMockInteractions sut = new FastMockInteractions(0);
		MethodInvocation first = new("first");
		MethodInvocation second = new("second");
		sut.RegisterInteraction(first);
		sut.RegisterInteraction(second);

		sut.Clear();

		FieldInfo fallbackField = typeof(FastMockInteractions).GetField(
			"_fallback", BindingFlags.NonPublic | BindingFlags.Instance)!;
		object fallback = fallbackField.GetValue(sut)!;

		FieldInfo recordsField = fallback.GetType().GetField(
			"_records", BindingFlags.NonPublic | BindingFlags.Instance)!;
		Array records = (Array)recordsField.GetValue(fallback)!;

		Type slotType = records.GetType().GetElementType()!;
		FieldInfo interactionField = slotType.GetField("Item2")!;
		for (int i = 0; i < records.Length; i++)
		{
			object? slot = records.GetValue(i);
			object? interaction = interactionField.GetValue(slot);
			await That(interaction).IsNull();
		}
	}

	[Test]
	public async Task Clear_FallbackPath_ShouldNotResurfaceOldRecordsAfterRefill()
	{
		IMockInteractions sut = new FastMockInteractions(0);
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
		FastMockInteractions sut = new(1);
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
		FastMockInteractions sut = new(2);
		FastMethod0Buffer methodBuffer = InstallMethod(sut, 0);
		FastPropertyGetterBuffer getterBuffer = InstallPropertyGetter(sut, 1);

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
		FastMockInteractions sut = new(2);
		FastMethod1Buffer<int> methodBuffer = InstallMethod<int>(sut, 0);
		FastPropertyGetterBuffer getterBuffer = InstallPropertyGetter(sut, 1);

		methodBuffer.Append("M", 1);
		getterBuffer.Append();
		_ = methodBuffer.ConsumeMatching((IParameterMatch<int>)It.Is(1));
		_ = getterBuffer.ConsumeMatching();

		sut.Clear();

		methodBuffer.Append("M", 2);
		getterBuffer.Append();

		await That(sut.GetUnverifiedInteractions()).HasCount(2);
	}

	[Test]
	public async Task Clear_ShouldResetVerifiedBookkeeping()
	{
		FastMockInteractions sut = new(1);
		FastMethod0Buffer methodBuffer = InstallMethod(sut, 0);
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
		// Pins the `_verified = null;` reset inside Clear. FastPropertyGetterBuffer reuses a
		// single PropertyGetterAccess singleton across records (it is cached in the buffer's
		// `_access` field, which Clear does NOT reset). With the `_verified = null` write
		// removed, the verified set keeps the singleton across Clear, and a fresh post-Clear
		// Append would surface as already-verified — masking the new interaction.
		FastMockInteractions sut = new(1);
		FastPropertyGetterBuffer buffer = InstallPropertyGetter(sut, 0);
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
		FastMockInteractions sut = new(2);
		FastMethod0Buffer methodBuffer = InstallMethod(sut, 0);
		FastPropertyGetterBuffer getterBuffer = InstallPropertyGetter(sut, 1);

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
		FastMockInteractions sut = new(2);
		FastMethod1Buffer<int> methodBuffer = InstallMethod<int>(sut, 0);
		FastPropertySetterBuffer<string> setterBuffer = sut.GetOrCreateBuffer<FastPropertySetterBuffer<string>>(1,
			static f => new FastPropertySetterBuffer<string>(f));

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
	public async Task GetOrCreateFallbackBuffer_BarrierSynchronizedRace_AllCallersResolveToTheSameInstalledBuffer(CancellationToken cancellationToken)
	{
		// Pins the `existing ?? created` null-coalesce in GetOrCreateFallbackBuffer. With the
		// left side removed (`existing ?? created` → `created`), CompareExchange losers return
		// their freshly-allocated buffer instead of the winner's installed one — even though
		// only the winner's buffer ends up referenced by `_fallback`. Calls private method
		// directly via reflection from many threads released in lockstep by a Barrier; resets
		// `_fallback` to null between rounds so every round races from a clean slate. After
		// each round, every returned buffer must be identical to the one referenced by
		// `_fallback` — otherwise a loser was handed an orphan instance.
		const int threads = 32;
		const int rounds = 5;

		FastMockInteractions sut = new(0);
		FieldInfo fallbackField = typeof(FastMockInteractions).GetField(
			"_fallback", BindingFlags.NonPublic | BindingFlags.Instance)!;
		MethodInfo getOrCreate = typeof(FastMockInteractions).GetMethod(
			"GetOrCreateFallbackBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!;

		object?[] returnedBuffers = new object?[threads];

		using Barrier barrier = new(threads + 1);

		Task[] tasks = new Task[threads];
		for (int t = 0; t < threads; t++)
		{
			int threadId = t;
#pragma warning disable xUnit1051
			tasks[t] = Task.Factory.StartNew(() =>
#pragma warning restore xUnit1051
			{
				for (int round = 0; round < rounds; round++)
				{
					barrier.SignalAndWait();
					returnedBuffers[threadId] = getOrCreate.Invoke(sut, null);
					barrier.SignalAndWait();
				}
			}, TaskCreationOptions.LongRunning);
		}

		for (int round = 0; round < rounds; round++)
		{
			fallbackField.SetValue(sut, null);
			barrier.SignalAndWait(cancellationToken);
			barrier.SignalAndWait(cancellationToken);

			object? installed = fallbackField.GetValue(sut);
			await That(installed).IsNotNull();
			for (int t = 0; t < threads; t++)
			{
				await That(returnedBuffers[t]).IsSameAs(installed);
			}
		}

		await Task.WhenAll(tasks);
	}

	[Test]
	public async Task GetOrCreateFallbackBuffer_ConcurrentRace_RecordsAllInteractions()
	{
		const int threads = 8;
		const int callsPerThread = 25;
		IMockInteractions sut = new FastMockInteractions(0);

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
		FastMockInteractions sut = new(2);
		FastMethod1Buffer<int> bufA = InstallMethod<int>(sut, 0);
		FastMethod1Buffer<int> bufB = InstallMethod<int>(sut, 1);

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
		FastMockInteractions sut = new(2);
		FastMethod1Buffer<int> methodBuffer = InstallMethod<int>(sut, 0);
		FastPropertyGetterBuffer getterBuffer = InstallPropertyGetter(sut, 1);

		methodBuffer.Append("M", 7);
		getterBuffer.Append();

		_ = methodBuffer.ConsumeMatching((IParameterMatch<int>)It.Is(7));
		_ = getterBuffer.ConsumeMatching();

		await That(sut.GetUnverifiedInteractions()).IsEmpty();
	}

	[Test]
	public async Task GetUnverifiedInteractions_AfterMatcherLessConsumeMatching_ShouldDropMarkedSlots()
	{
		FastMockInteractions sut = new(1);
		FastMethod0Buffer methodBuffer = InstallMethod(sut, 0);
		methodBuffer.Append("first");
		methodBuffer.Append("second");

		_ = methodBuffer.ConsumeMatching();

		await That(sut.GetUnverifiedInteractions()).IsEmpty();

		methodBuffer.Append("third");

		IReadOnlyCollection<IInteraction> unverified = sut.GetUnverifiedInteractions();
		await That(unverified).HasCount(1);
		await That(((MethodInvocation)unverified.Single()).Name).IsEqualTo("third");
	}

	[Test]
	public async Task GetUnverifiedInteractions_AfterTypedConsumeMatching_ShouldDropOnlyMatchedSlots()
	{
		FastMockInteractions sut = new(1);
		FastMethod1Buffer<int> methodBuffer = InstallMethod<int>(sut, 0);
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
		FastMockInteractions sut = new(1);
		FastMethod1Buffer<int> methodBuffer = InstallMethod<int>(sut, 0);
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
		FastMockInteractions sut = new(1);
		FastMethod0Buffer methodBuffer = InstallMethod(sut, 0);
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
		FastMockInteractions sut = new(1);
		InstallMethod(sut, 0);

		IReadOnlyCollection<IInteraction> result = sut.GetUnverifiedInteractions();

		await That(result).IsSameAs(Array.Empty<IInteraction>())
			.Because(
				"the empty path must take the `return Array.Empty<IInteraction>();` early-return rather than fall through and allocate a fresh zero-length array");
	}

	[Test]
	public async Task GetUnverifiedInteractions_WhenNothingVerified_ShouldReturnEverything()
	{
		FastMockInteractions sut = new(1);
		FastMethod0Buffer methodBuffer = InstallMethod(sut, 0);
		methodBuffer.Append("first");

		IReadOnlyCollection<IInteraction> unverified = sut.GetUnverifiedInteractions();

		await That(unverified).HasCount(1);
	}

	[Test]
	public async Task NextSequence_ShouldReturnZeroBasedMonotonicValues()
	{
		FastMockInteractions sut = new(0);

		long first = sut.NextSequence();
		long second = sut.NextSequence();
		long third = sut.NextSequence();

		await That(first).IsEqualTo(0L);
		await That(second).IsEqualTo(1L);
		await That(third).IsEqualTo(2L);
	}

	[Test]
	public async Task RegisterInteraction_FallbackPath_GetUnverifiedInteractions_ShouldRespectVerifiedSet()
	{
		IMockInteractions sut = new FastMockInteractions(0);
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
		IMockInteractions sut = new FastMockInteractions(0);
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
		IMockInteractions sut = new FastMockInteractions(0);
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
		IMockInteractions sut = new FastMockInteractions(0);
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
		IMockInteractions sut = new FastMockInteractions(0);
		MethodInvocation interaction = new("foo");

		MethodInvocation registered = sut.RegisterInteraction(interaction);

		await That(registered).IsSameAs(interaction);
		await That(sut.Count).IsEqualTo(1);
		await That(sut.ToList()[0]).IsSameAs(interaction);
	}

	[Test]
	public async Task RegisterInteraction_WhenSkipInteractionRecording_ShouldNotRecord()
	{
		IMockInteractions sut = new FastMockInteractions(0, true);

		sut.RegisterInteraction(new MethodInvocation("foo"));

		await That(sut.Count).IsEqualTo(0);
	}

	[Test]
	public async Task SkipInteractionRecording_ShouldReflectConstructionValue()
	{
		FastMockInteractions skipping = new(0, true);
		FastMockInteractions recording = new(0);

		await That(skipping.SkipInteractionRecording).IsTrue();
		await That(recording.SkipInteractionRecording).IsFalse();
	}

	[Test]
	public async Task Verified_BeforeAnyAppend_OnSecondCallAddsToExistingSet()
	{
		FastMockInteractions sut = new(1);
		FastMethod0Buffer buffer = InstallMethod(sut, 0);

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
		FastMockInteractions sut = new(1);
		FastMethod0Buffer buffer = InstallMethod(sut, 0);
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
		FastMockInteractions sut = new(1);
		FastMethod0Buffer buffer = InstallMethod(sut, 0);
		buffer.Append("first");
		buffer.Append("second");

		List<IInteraction> all = [..sut,];
		((IMockInteractions)sut).Verified(all);

		await That(sut.GetUnverifiedInteractions()).IsEmpty();
	}

	private static FastMethod0Buffer InstallMethod(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastMethod0Buffer>(memberId, static f => new FastMethod0Buffer(f));

	private static FastMethod1Buffer<T> InstallMethod<T>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastMethod1Buffer<T>>(memberId, static f => new FastMethod1Buffer<T>(f));

	private static FastPropertyGetterBuffer InstallPropertyGetter(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastPropertyGetterBuffer>(memberId,
			static f => new FastPropertyGetterBuffer(f, new PropertyGetterAccess(string.Empty)));

	public sealed class MethodBufferTests
	{
		[Test]
		public async Task Method0_BoxesAsMethodInvocation()
		{
			FastMockInteractions store = new(1);
			FastMethod0Buffer buffer = InstallMethod(store, 0);

			buffer.Append("Foo");

			MethodInvocation boxed = (MethodInvocation)store.Single();
			await That(boxed.Name).IsEqualTo("Foo");
		}

		[Test]
		public async Task Method1_BoxesAsMethodInvocationT1()
		{
			FastMockInteractions store = new(1);
			FastMethod1Buffer<int> buffer = InstallMethod<int>(store, 0);

			buffer.Append("Foo", 42);

			MethodInvocation<int> boxed = (MethodInvocation<int>)store.Single();
			await That(boxed.Name).IsEqualTo("Foo");
			await That(boxed.Parameter1).IsEqualTo(42);
		}

		[Test]
		public async Task Method2_BoxesAsMethodInvocationT1T2()
		{
			FastMockInteractions store = new(1);
			FastMethod2Buffer<int, string> buffer = store.GetOrCreateBuffer<FastMethod2Buffer<int, string>>(0,
				static f => new FastMethod2Buffer<int, string>(f));

			buffer.Append("Foo", 42, "bar");

			MethodInvocation<int, string> boxed = (MethodInvocation<int, string>)store.Single();
			await That(boxed.Parameter1).IsEqualTo(42);
			await That(boxed.Parameter2).IsEqualTo("bar");
		}

		[Test]
		public async Task Method3_BoxesAsMethodInvocationT1T2T3()
		{
			FastMockInteractions store = new(1);
			FastMethod3Buffer<int, string, bool> buffer = store.GetOrCreateBuffer<FastMethod3Buffer<int, string, bool>>(0,
				static f => new FastMethod3Buffer<int, string, bool>(f));

			buffer.Append("Foo", 1, "two", true);

			MethodInvocation<int, string, bool> boxed = (MethodInvocation<int, string, bool>)store.Single();
			await That(boxed.Parameter1).IsEqualTo(1);
			await That(boxed.Parameter2).IsEqualTo("two");
			await That(boxed.Parameter3).IsTrue();
		}

		[Test]
		public async Task Method4_BoxesAsMethodInvocationT1T2T3T4()
		{
			FastMockInteractions store = new(1);
			FastMethod4Buffer<int, string, bool, double> buffer =
				store.GetOrCreateBuffer<FastMethod4Buffer<int, string, bool, double>>(0,
					static f => new FastMethod4Buffer<int, string, bool, double>(f));

			buffer.Append("Foo", 1, "two", true, 3.14);

			MethodInvocation<int, string, bool, double> boxed =
				(MethodInvocation<int, string, bool, double>)store.Single();
			await That(boxed.Parameter4).IsEqualTo(3.14);
		}

		[Test]
		public async Task ResizesAndPreservesOrder()
		{
			FastMockInteractions store = new(1);
			FastMethod1Buffer<int> buffer = InstallMethod<int>(store, 0);

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
			FastMockInteractions store = new(1);
			FastPropertyGetterBuffer buffer = store.GetOrCreateBuffer<FastPropertyGetterBuffer, PropertyGetterAccess>(
				0, static (f, a) => new FastPropertyGetterBuffer(f, a), new PropertyGetterAccess("Foo"));

			buffer.Append();

			PropertyGetterAccess boxed = (PropertyGetterAccess)store.Single();
			await That(boxed.Name).IsEqualTo("Foo");
		}

		[Test]
		public async Task Setter_BoxesAsPropertySetterAccessT()
		{
			FastMockInteractions store = new(1);
			FastPropertySetterBuffer<string> buffer = store.GetOrCreateBuffer<FastPropertySetterBuffer<string>>(0,
				static f => new FastPropertySetterBuffer<string>(f));

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
			FastMockInteractions store = new(1);
			FastIndexerGetterBuffer<string> buffer = store.GetOrCreateBuffer<FastIndexerGetterBuffer<string>>(0,
				static f => new FastIndexerGetterBuffer<string>(f));

			buffer.Append("k");

			IndexerGetterAccess<string> boxed = (IndexerGetterAccess<string>)store.Single();
			await That(boxed.Parameter1).IsEqualTo("k");
		}

		[Test]
		public async Task Getter2_BoxesAsIndexerGetterAccess()
		{
			FastMockInteractions store = new(1);
			FastIndexerGetterBuffer<string, int> buffer = store.GetOrCreateBuffer<FastIndexerGetterBuffer<string, int>>(0,
				static f => new FastIndexerGetterBuffer<string, int>(f));

			buffer.Append("k", 1);

			IndexerGetterAccess<string, int> boxed = (IndexerGetterAccess<string, int>)store.Single();
			await That(boxed.Parameter1).IsEqualTo("k");
			await That(boxed.Parameter2).IsEqualTo(1);
		}

		[Test]
		public async Task Setter1_BoxesAsIndexerSetterAccess()
		{
			FastMockInteractions store = new(1);
			FastIndexerSetterBuffer<string, bool> buffer = store.GetOrCreateBuffer<FastIndexerSetterBuffer<string, bool>>(0,
				static f => new FastIndexerSetterBuffer<string, bool>(f));

			buffer.Append("k", true);

			IndexerSetterAccess<string, bool> boxed = (IndexerSetterAccess<string, bool>)store.Single();
			await That(boxed.Parameter1).IsEqualTo("k");
			await That(boxed.TypedValue).IsTrue();
		}

		[Test]
		public async Task Setter2_BoxesAsIndexerSetterAccess()
		{
			FastMockInteractions store = new(1);
			FastIndexerSetterBuffer<string, int, bool> buffer =
				store.GetOrCreateBuffer<FastIndexerSetterBuffer<string, int, bool>>(0,
					static f => new FastIndexerSetterBuffer<string, int, bool>(f));

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
			FastMockInteractions store = new(1);
			FastEventBuffer buffer = store.GetOrCreateBuffer<FastEventBuffer>(0,
				static f => new FastEventBuffer(f, FastEventBufferKind.Subscribe));
			MethodInfo method = typeof(EventBufferTests).GetMethod(nameof(Subscribe_BoxesAsEventSubscription))!;

			buffer.Append("E", this, method);

			EventSubscription boxed = (EventSubscription)store.Single();
			await That(boxed.Name).IsEqualTo("E");
			await That(boxed.Target).IsSameAs(this);
			await That(boxed.Method).IsSameAs(method);
		}

		[Test]
		public async Task Unsubscribe_BoxesAsEventUnsubscription()
		{
			FastMockInteractions store = new(1);
			FastEventBuffer buffer = store.GetOrCreateBuffer<FastEventBuffer>(0,
				static f => new FastEventBuffer(f, FastEventBufferKind.Unsubscribe));
			MethodInfo method = typeof(EventBufferTests).GetMethod(nameof(Unsubscribe_BoxesAsEventUnsubscription))!;

			buffer.Append("E", null, method);

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
			FastMockInteractions store = new(threads);
			FastMethod1Buffer<int>[] buffers = new FastMethod1Buffer<int>[threads];
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
			foreach (FastMethod1Buffer<int> buffer in buffers)
			{
				sum += buffer.Count;
			}

			await That(sum).IsEqualTo(threads * callsPerThread);
		}
	}
}
