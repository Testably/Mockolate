using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using aweXpect.Chronology;
using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Parameters;
using Mockolate.Verify;

namespace Mockolate.Internal.Tests.Verify;

public class VerificationResultTests
{
	private static MethodRecords InstallMethod(MockInteractions store, int memberId)
		=> new MethodRecords(store, memberId);

	private static MethodRecords<T> InstallMethod<T>(MockInteractions store, int memberId)
		=> new MethodRecords<T>(store, memberId);

	[Test]
	public async Task VerifyCount_WithoutCountSource_WhenSuccessful_ShouldMarkMatchingInteractionsAsVerified()
	{
		// Pins the `_interactions.Verified(matchingInteractions);` call inside the non-Awaitable
		// ICountVerifiable.VerifyCount second branch (when `_recordCountSource is null`).
		MockInteractions store = new(1);
		IndexerGetterRecords<int>.Append(store, 0, 1);
		MockRegistry registry = new(MockBehavior.Default, store);

		registry.IndexerGot(new object(), 0,
			interaction => interaction is IndexerGetterAccess<int> g && g.Parameter1 == 1,
			() => "[1]").Once();

		await That(((IMockInteractions)store).GetUnverifiedInteractions()).IsEmpty();
	}

	public sealed class ConstructorTests
	{
		[Test]
		public async Task WithLazyExpectation_ShouldCreateExpectationFromStateAndArgument()
		{
			IMockInteractions interactions = new MockInteractions(0);
			VerificationResult<int> sut = new(1, interactions, _ => false,
				new VerificationExpectation("foo", "bar", static (state, argument) => $"{state}-{argument}"));

			string result = ((IVerificationResult)sut).Expectation;

			await That(result).IsEqualTo("foo-bar");
		}

		[Test]
		public async Task ShouldExposeInteractions()
		{
			IMockInteractions interactions = new MockInteractions(0);
			VerificationResult<int> sut = new(1, interactions, _ => false,
				new VerificationExpectation("foo", null, static (state, _) => (string)state!));

			IMockInteractions exposed = ((IVerificationResult)sut).Interactions;

			await That(exposed).IsSameAs(interactions);
		}
	}

	public sealed class AwaitableTests
	{
		[Test]
		public async Task Verify_WhenPredicateNeverSatisfies_ShouldTimeOut()
		{
			MockInteractions store = new(0);
			VerificationResult<object> result = new(
				new object(),
				store,
				_ => true,
				new VerificationExpectation("expected", null, static (state, _) => (string)state!));

			VerificationResult<object> awaitable = result.Within(50.Milliseconds());

			void Act()
			{
				((IVerificationResult)awaitable).Verify(_ => false);
			}

			await That(Act).Throws<MockVerificationTimeoutException>();
		}

		[Test]
		public async Task Verify_WithAnyParameters_ShouldCollectAllInteractions()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 1);
			buffer.Append("Foo", 2);
			buffer.Append("Foo", 3);

			VerificationResult<object> awaitable = registry.VerifyMethod(
					new object(), 0, "Foo", (IParameterMatch<int>)It.Is(1), () => "Foo(1)")
				.AnyParameters()
				.Within(1.Seconds());

			int observedLength = -1;
			bool verified = ((IVerificationResult)awaitable).Verify(arr =>
			{
				observedLength = arr.Length;
				return true;
			});

			await That(verified).IsTrue();
			await That(observedLength).IsEqualTo(3);
		}

		[Test]
		public async Task Verify_WithLiteralValue_ShouldCollectOnlyMatchingInteractions()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 10);
			buffer.Append("Foo", 20);
			buffer.Append("Foo", 10);

			VerificationResult<object> awaitable = registry.VerifyMethod(
					new object(), 0, "Foo", 10, static v => $"Foo({v})")
				.Within(1.Seconds());

			int observedLength = -1;
			bool verified = ((IVerificationResult)awaitable).Verify(arr =>
			{
				observedLength = arr.Length;
				return true;
			});

			await That(verified).IsTrue();
			await That(observedLength).IsEqualTo(2);
		}

		[Test]
		public async Task Verify_WithLiteralValue_WhenNeverSatisfied_ShouldTimeOut()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 10);
			buffer.Append("Foo", 20);

			VerificationResult<object> awaitable = registry.VerifyMethod(
					new object(), 0, "Foo", 10, static v => $"Foo({v})")
				.Within(50.Milliseconds());

			void Act()
			{
				((IVerificationResult)awaitable).Verify(arr => arr.Length == 2);
			}

			await That(Act).Throws<MockVerificationTimeoutException>();
		}

		[Test]
		public async Task VerifyAsync_WithLiteralValue_ShouldCollectOnlyMatchingInteractions()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 10);
			buffer.Append("Foo", 20);
			buffer.Append("Foo", 10);

			VerificationResult<object> awaitable = registry.VerifyMethod(
					new object(), 0, "Foo", 10, static v => $"Foo({v})")
				.WithCancellation(CancellationToken.None);

			int observedLength = -1;
			bool verified = await ((IAsyncVerificationResult)awaitable).VerifyAsync(arr =>
			{
				observedLength = arr.Length;
				return true;
			});

			await That(verified).IsTrue();
			await That(observedLength).IsEqualTo(2);
		}

		[Test]
		public async Task VerifyCount_WhenPredicateNeverSatisfies_ShouldTimeOut()
		{
			MockInteractions store = new(1);
			InstallMethod(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo", () => "Foo()")
					.Within(50.Milliseconds()).AtLeast(2);
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*timed out*").AsWildcard();
		}

		[Test]
		public async Task VerifyCount_WithAnyParameters_ShouldCountAllInteractions()
		{
			// Pins the `_useCountAll ? CountAll() : Count()` conditional in Awaitable.VerifyCount.
			// Buffer holds 3 records, the matcher accepts only 1 of them, AnyParameters() flips
			// _useCountAll = true. With the original code, CountAll()=3 satisfies Exactly(3) — but
			// here we assert against Exactly(1), which is the *filtered* count: only the false-branch
			// (Count()) of the mutated conditional satisfies it synchronously, then short-circuits
			// before the async loop can re-evaluate via the unmutated CountAll().
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 1);
			buffer.Append("Foo", 2);
			buffer.Append("Foo", 3);

			VerificationResult<object>.IgnoreParameters typed = registry.VerifyMethod(
				new object(), 0, "Foo",
				(IParameterMatch<int>)It.Is(1),
				() => "Foo(1)");

			VerificationResult<object> widened = typed.AnyParameters();

			void Act()
			{
				widened.Within(50.Milliseconds()).Exactly(1);
			}

			await That(Act).Throws<MockVerificationException>();
		}

		[Test]
		public async Task WithCancellation_WhenCalledAfterAnyParameters_ShouldCountAllInteractions()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 1);
			buffer.Append("Foo", 2);
			buffer.Append("Foo", 3);

			VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod(
				new object(), 0, "Foo",
				(IParameterMatch<int>)It.Is(1),
				() => "Foo(1)");

			VerificationResult<object> widened = result.AnyParameters();

			void Act()
			{
				widened.WithCancellation(CancellationToken.None).Exactly(3);
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task Within_WhenCalledAfterAnyParameters_ShouldCountAllInteractions()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 1);
			buffer.Append("Foo", 2);
			buffer.Append("Foo", 3);

			VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod(
				new object(), 0, "Foo",
				(IParameterMatch<int>)It.Is(1),
				() => "Foo(1)");

			VerificationResult<object> widened = result.AnyParameters();

			void Act()
			{
				widened.Within(50.Milliseconds()).Exactly(3);
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task VerifyCount_WithoutCountSource_WhenSuccessful_ShouldMarkMatchingInteractionsAsVerified()
		{
			// Pins the `_interactions.Verified(matchingInteractions);` call inside Awaitable's
			// ICountVerifiable.VerifyCount second branch (when `_recordCountSource is null`).
			// IndexerGot(memberId, ...) returns a VerificationResult with a buffer but no fast-count
			// source, so wrapping it with .Within(...) walks exactly that branch.
			MockInteractions store = new(1);
			IndexerGetterRecords<int>.Append(store, 0, 1);
			MockRegistry registry = new(MockBehavior.Default, store);

			registry.IndexerGot(new object(), 0,
					interaction => interaction is IndexerGetterAccess<int> g && g.Parameter1 == 1,
					() => "[1]")
				.Within(50.Milliseconds()).Once();

			await That(((IMockInteractions)store).GetUnverifiedInteractions()).IsEmpty();
		}
	}

	public sealed class MapTests
	{
		[Test]
		public async Task WithoutRecordsOrCountSource_ShouldKeepBothNull()
		{
			MockInteractions store = new(0);
			VerificationResult<object> source = new(
				new object(), store, _ => true,
				new VerificationExpectation("expected", null, static (state, _) => (string)state!));

			VerificationResult<int> mapped = source.Map(42);

			FieldInfo bufferField = typeof(VerificationResult<int>).GetField(
				"_records", BindingFlags.Instance | BindingFlags.NonPublic)!;
			FieldInfo fastSourceField = typeof(VerificationResult<int>).GetField(
				"_recordCountSource", BindingFlags.Instance | BindingFlags.NonPublic)!;

			await That(bufferField.GetValue(mapped)).IsNull();
			await That(fastSourceField.GetValue(mapped)).IsNull();
		}

		[Test]
		public async Task WithRecordsWithoutCountSource_ShouldKeepTheRecords()
		{
			// The buffer-only IgnoreParameters constructor is wired with no fast source. Map must
			// take the buffer-preserving branch — if it slips into the simple constructor, the next
			// CollectMatching falls back to a global Where over _interactions, which would pick up
			// records from OTHER buffers that satisfy the (un-name-filtered) predicate.
			MockInteractions store = new(2);
			MethodRecords<int> bufA = InstallMethod<int>(store, 0);
			MethodRecords<int> bufB = InstallMethod<int>(store, 1);
			MockRegistry registry = new(MockBehavior.Default, store);

			bufA.Append("Foo", 1);
			bufA.Append("Foo", 2);
			bufB.Append("Bar", 99);

			VerificationResult<object>.IgnoreParameters result =
				registry.VerifyMethod<object, MethodInvocation<int>>(
					new object(), 0, "Foo", _ => true, () => "Foo()");

			VerificationResult<string> mapped = result.Map("newMock");

			int observedLength = -1;
			bool verified = ((IVerificationResult)mapped).Verify(arr =>
			{
				observedLength = arr.Length;
				return true;
			});

			await That(verified).IsTrue();
			await That(observedLength).IsEqualTo(2);
		}

		[Test]
		public async Task WithRecords_ShouldReplaceTheSubjectAndKeepCounting()
		{
			MockInteractions store = new(1);
			MethodRecords buffer = InstallMethod(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo");
			buffer.Append("Foo");

			object original = new();
			VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod(
				original, 0, "Foo", () => "Foo()");

			string newSubject = "newMock";
			VerificationResult<string> mapped = result.Map(newSubject);

			await That(((IVerificationResult<string>)mapped).Object).IsEqualTo(newSubject);

			void Act()
			{
				mapped.Within(50.Milliseconds()).Exactly(2);
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task WhenCalledAfterAnyParameters_ShouldCountAllInteractions()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 1);
			buffer.Append("Foo", 2);
			buffer.Append("Foo", 3);

			VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod(
				new object(), 0, "Foo", (IParameterMatch<int>)It.Is(1), () => "Foo(1)");
			VerificationResult<object> widened = result.AnyParameters();

			VerificationResult<string> mapped = widened.Map("newMock");

			void Act()
			{
				mapped.Exactly(3);
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task WhenCalledBetweenAnyParametersAndWithin_ShouldCountAllInteractions()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 1);
			buffer.Append("Foo", 2);
			buffer.Append("Foo", 3);

			VerificationResult<object> widened = registry.VerifyMethod(
					new object(), 0, "Foo", (IParameterMatch<int>)It.Is(1), () => "Foo(1)")
				.AnyParameters();

			void Act()
			{
				widened.Map("newMock").Within(50.Milliseconds()).Exactly(3);
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task WhenCalledAfterWithin_ShouldWaitForLateInteraction()
		{
			MockInteractions store = new(1);
			MethodRecords buffer = InstallMethod(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo");

			VerificationResult<string> mapped = registry.VerifyMethod(new object(), 0, "Foo", () => "Foo()")
				.Within(5.Seconds())
				.Map("newMock");

			Task lateCall = Task.Run(async () =>
			{
				await Task.Delay(100, CancellationToken.None);
				buffer.Append("Foo");
			}, CancellationToken.None);

			void Act()
			{
				mapped.Exactly(2);
			}

			await That(Act).DoesNotThrow();
			await lateCall;
		}

		[Test]
		public async Task WhenCalledAfterWithCancellation_ShouldWaitForLateInteraction()
		{
			MockInteractions store = new(1);
			MethodRecords buffer = InstallMethod(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);
			using CancellationTokenSource cts = new(5.Seconds());

			buffer.Append("Foo");

			VerificationResult<string> mapped = registry.VerifyMethod(new object(), 0, "Foo", () => "Foo()")
				.WithCancellation(cts.Token)
				.Map("newMock");

			Task lateCall = Task.Run(async () =>
			{
				await Task.Delay(100, CancellationToken.None);
				buffer.Append("Foo");
			}, CancellationToken.None);

			void Act()
			{
				mapped.Exactly(2);
			}

			await That(Act).DoesNotThrow();
			await lateCall;
		}

		[Test]
		public async Task WithMatcher_ShouldCollectOnlyMatchingInteractions()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 1);
			buffer.Append("Foo", 2);
			buffer.Append("Foo", 1);

			VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod(
				new object(), 0, "Foo", (IParameterMatch<int>)It.Is(1), () => "Foo(1)");

			VerificationResult<string> mapped = result.Map("newMock");

			int observedLength = -1;
			bool verified = ((IVerificationResult)mapped).Verify(arr =>
			{
				observedLength = arr.Length;
				return true;
			});

			await That(verified).IsTrue();
			await That(observedLength).IsEqualTo(2);
		}

		[Test]
		public async Task WithoutRecords_ShouldReplaceTheSubjectAndKeepTheExpectation()
		{
			MockInteractions store = new(0);
			VerificationResult<object> source = new(
				new object(),
				store,
				_ => true,
				new VerificationExpectation("expected", null, static (state, _) => (string)state!));

			VerificationResult<int> mapped = source.Map(42);

			await That(((IVerificationResult<int>)mapped).Object).IsEqualTo(42);
			await That(((IVerificationResult)mapped).Expectation).IsEqualTo("expected");
		}
	}

	public sealed class CollectMatchingTests
	{
		[Test]
		public async Task WithBufferAndExactlyTwoRecords_ShouldPreserveTheCallOrder()
		{
			// Crosses the `records.Count > 1` boundary in CollectMatching at exactly N=2. Combined
			// with the existing N=1 / N=3 / N=0 cases, this pins the boundary so flipping `> 1` to
			// `>= 1` is no longer a silent rewrite of the sort path.
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 10);
			buffer.Append("Foo", 20);

			VerificationResult<object>.IgnoreParameters result =
				registry.VerifyMethod<object, MethodInvocation<int>>(
					new object(), 0, "Foo", _ => true, () => "Foo()");

			List<int> values = new();
			bool verified = ((IVerificationResult)result).Verify(arr =>
			{
				foreach (IInteraction interaction in arr)
				{
					values.Add(((MethodInvocation<int>)interaction).Parameter1);
				}

				return arr.Length == 2;
			});

			await That(verified).IsTrue();
			await That(values).IsEqualTo([10, 20,]);
		}

		[Test]
		public async Task WithBufferAndMultipleRecords_ShouldPreserveTheCallOrder()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 10);
			buffer.Append("Foo", 20);
			buffer.Append("Foo", 30);

			VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, MethodInvocation<int>>(
				new object(), 0, "Foo", _ => true, () => "Foo()");

			List<int> values = new();
			bool verified = ((IVerificationResult)result).Verify(arr =>
			{
				foreach (IInteraction interaction in arr)
				{
					values.Add(((MethodInvocation<int>)interaction).Parameter1);
				}

				return arr.Length == 3;
			});

			await That(verified).IsTrue();
			await That(values).IsEqualTo([10, 20, 30,]);
		}

		[Test]
		public async Task WithBufferAndNoMatchingRecord_ShouldReturnEmpty()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 1);

			VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, MethodInvocation<int>>(
				new object(), 0, "Foo", m => m.Parameter1 == 99, () => "Foo(99)");

			int observed = -1;
			bool verified = ((IVerificationResult)result).Verify(arr =>
			{
				observed = arr.Length;
				return arr.Length == 0;
			});

			await That(verified).IsTrue();
			await That(observed).IsEqualTo(0);
		}

		[Test]
		public async Task WithBufferAndSingleRecord_ShouldReturnTheRecord()
		{
			MockInteractions store = new(1);
			MethodRecords buffer = InstallMethod(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo");

			VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, IMethodInteraction>(
				new object(), 0, "Foo", _ => true, () => "Foo()");

			int observed = -1;
			bool verified = ((IVerificationResult)result).Verify(arr =>
			{
				observed = arr.Length;
				return arr.Length == 1;
			});

			await That(verified).IsTrue();
			await That(observed).IsEqualTo(1);
		}

		[Test]
		public async Task WithEmptyBuffer_ShouldReturnEmpty()
		{
			MockInteractions store = new(1);
			InstallMethod(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, IMethodInteraction>(
				new object(), 0, "Foo", _ => true, () => "Foo()");

			int observed = -1;
			bool verified = ((IVerificationResult)result).Verify(arr =>
			{
				observed = arr.Length;
				return arr.Length == 0;
			});

			await That(verified).IsTrue();
			await That(observed).IsEqualTo(0);
		}

		[Test]
		public async Task WithLiteralValue_ShouldCollectOnlyMatchingInteractions()
		{
			MockInteractions store = new(1);
			MethodRecords<int> buffer = InstallMethod<int>(store, 0);
			MockRegistry registry = new(MockBehavior.Default, store);

			buffer.Append("Foo", 10);
			buffer.Append("Foo", 20);
			buffer.Append("Foo", 10);

			VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod(
				new object(), 0, "Foo", 10, static v => $"Foo({v})");

			List<int> values = new();
			bool verified = ((IVerificationResult)result).Verify(arr =>
			{
				foreach (IInteraction interaction in arr)
				{
					values.Add(((MethodInvocation<int>)interaction).Parameter1);
				}

				return true;
			});

			await That(verified).IsTrue();
			await That(values).IsEqualTo([10, 10,]);
		}
	}

	public sealed class SkipInteractionRecordingTests
	{
		[Test]
		public async Task Verify_WithWithin_ShouldThrowMockException()
		{
			// Same guard, Awaitable Verify (records-array) path. Constructed with a predicate-based
			// VR (not a typed CountSource) so we exercise the slow CollectMatching branch.
			MockInteractions store = new(0, true);
			VerificationResult<object> result = new(
				new object(), store, _ => true,
				new VerificationExpectation("expected", null, static (state, _) => (string)state!));

			VerificationResult<object> awaitable = result.Within(50.Milliseconds());

			void Act()
			{
				((IVerificationResult)awaitable).Verify(_ => true);
			}

			await That(Act).Throws<MockException>()
				.WithMessage("*recording is disabled*").AsWildcard();
		}

		[Test]
		public async Task VerifyCount_WithWithin_ShouldThrowMockException()
		{
			// Same guard, Awaitable path (via Within). Distinct mutation site.
			MockInteractions store = new(1, true);
			InstallMethod(store, 0);
			MockRegistry registry = new(MockBehavior.Default with
			{
				SkipInteractionRecording = true,
			}, store);

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo", () => "Foo()")
					.Within(50.Milliseconds()).Once();
			}

			await That(Act).Throws<MockException>()
				.WithMessage("*recording is disabled*").AsWildcard();
		}

		[Test]
		public async Task VerifyCount_ShouldThrowMockException()
		{
			// Kills the ThrowIfRecordingDisabled statement-removal mutation on the non-Awaitable
			// ICountVerifiable.VerifyCount path. Without the guard, Once() would silently
			// report a (probably false) count and never throw.
			MockInteractions store = new(1, true);
			InstallMethod(store, 0);
			MockRegistry registry = new(MockBehavior.Default with
			{
				SkipInteractionRecording = true,
			}, store);

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo", () => "Foo()").Once();
			}

			await That(Act).Throws<MockException>()
				.WithMessage("*recording is disabled*").AsWildcard();
		}
	}

	public sealed class IgnoreParametersAnyParametersTests
	{
		[Test]
		public async Task WhenExpectationHasNoArgumentList_ShouldAppendAnyParameters()
		{
			IMockInteractions store = new MockInteractions(0);
			MockRegistry registry = new(MockBehavior.Default, store);

			VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, MethodInvocation<int>>(
				new object(), "Foo", _ => false, () => "Foo");

			void Act()
			{
				result.AnyParameters().Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("Expected that mock invoked method Foo(Match.AnyParameters()) exactly once, but it never did.");
		}

		[Test]
		public async Task WithoutBufferOrOverloadFilter_ShouldMatchAllInvocationsOfTheMethod()
		{
			IMockInteractions store = new MockInteractions(0);

			store.RegisterInteraction(new MethodInvocation<int>("Foo", 1));
			store.RegisterInteraction(new MethodInvocation<string>("Foo", "x"));
			store.RegisterInteraction(new MethodInvocation<int>("Bar", 1));

			VerificationResult<object>.IgnoreParameters result = new(
				new object(),
				store,
				"Foo",
				_ => false,
				null,
				new VerificationExpectation("Foo", null, static (state, _) => (string)state!));

			void Act()
			{
				result.AnyParameters().Exactly(2);
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task WithoutBuffer_ShouldKeepTheOverloadFilter()
		{
			IMockInteractions store = new MockInteractions(0);
			MockRegistry registry = new(MockBehavior.Default, store);

			store.RegisterInteraction(new MethodInvocation<int>("Foo", 1));
			store.RegisterInteraction(new MethodInvocation<string>("Foo", "x"));
			store.RegisterInteraction(new MethodInvocation<int>("Bar", 1));

			VerificationResult<object>.IgnoreParameters result = registry.VerifyMethod<object, MethodInvocation<int>>(
				new object(), "Foo", _ => false, () => "Foo");

			void Act()
			{
				result.AnyParameters().Once();
			}

			await That(Act).DoesNotThrow();
		}
	}
}
