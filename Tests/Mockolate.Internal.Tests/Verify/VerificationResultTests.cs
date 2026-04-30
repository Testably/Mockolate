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
	private static FastMethod0Buffer InstallMethod(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastMethod0Buffer>(memberId, static f => new FastMethod0Buffer(f));

	private static FastMethod1Buffer<T> InstallMethod<T>(FastMockInteractions store, int memberId)
		=> store.GetOrCreateBuffer<FastMethod1Buffer<T>>(memberId, static f => new FastMethod1Buffer<T>(f));

	public sealed class AwaitableTests
	{
		[Test]
		public async Task Verify_WhenPredicateNeverSatisfies_ShouldTimeOut()
		{
			FastMockInteractions store = new(0);
			VerificationResult<object> result = new(
				new object(),
				store,
				_ => true,
				"expected");

			VerificationResult<object> awaitable = result.Within(50.Milliseconds());

			void Act()
			{
				((IVerificationResult)awaitable).Verify(_ => false);
			}

			await That(Act).Throws<MockVerificationTimeoutException>();
		}

		[Test]
		public async Task VerifyCount_WhenPredicateNeverSatisfies_ShouldTimeOut()
		{
			FastMockInteractions store = new(1);
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
		public async Task VerifyCount_WithUseCountAll_ShouldPickCountAllNotFilteredCount()
		{
			// Pins the `_useCountAll ? CountAll() : Count()` conditional in Awaitable.VerifyCount.
			// Buffer holds 3 records, the matcher accepts only 1 of them, AnyParameters() flips
			// _useCountAll = true. With the original code, CountAll()=3 satisfies Exactly(3) — but
			// here we assert against Exactly(1), which is the *filtered* count: only the false-branch
			// (Count()) of the mutated conditional satisfies it synchronously, then short-circuits
			// before the async loop can re-evaluate via the unmutated CountAll().
			FastMockInteractions store = new(1);
			FastMethod1Buffer<int> buffer = InstallMethod<int>(store, 0);
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
		public async Task WithCancellation_PreservesUseCountAllFlag()
		{
			FastMockInteractions store = new(1);
			FastMethod1Buffer<int> buffer = InstallMethod<int>(store, 0);
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
		public async Task Within_PreservesUseCountAllFlag()
		{
			FastMockInteractions store = new(1);
			FastMethod1Buffer<int> buffer = InstallMethod<int>(store, 0);
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
	}

	public sealed class MapTests
	{
		[Test]
		public async Task Map_WhenBufferAndFastSourceBothNull_DropsToSimpleConstructor()
		{
			// Mirrors the buffer-only case from the other side: with neither buffer nor source, the
			// only valid Map output is the simple constructor. Asserts via reflection that _buffer
			// stays null after Map so the equality-mutation that would force the buffer-carrying
			// constructor (and crash on the null buffer) is killed.
			FastMockInteractions store = new(0);
			VerificationResult<object> source = new(
				new object(), store, _ => true, "expected");

			VerificationResult<int> mapped = source.Map(42);

			FieldInfo bufferField = typeof(VerificationResult<int>).GetField(
				"_buffer", BindingFlags.Instance | BindingFlags.NonPublic)!;
			FieldInfo fastSourceField = typeof(VerificationResult<int>).GetField(
				"_fastCountSource", BindingFlags.Instance | BindingFlags.NonPublic)!;

			await That(bufferField.GetValue(mapped)).IsNull();
			await That(fastSourceField.GetValue(mapped)).IsNull();
		}

		[Test]
		public async Task Map_WhenBufferOnlyAndFastSourceNull_PreservesBuffer()
		{
			// The buffer-only IgnoreParameters constructor is wired with no fast source. Map must
			// take the buffer-preserving branch — if it slips into the simple constructor, the next
			// CollectMatching falls back to a global Where over _interactions, which would pick up
			// records from OTHER buffers that satisfy the (un-name-filtered) predicate.
			FastMockInteractions store = new(2);
			FastMethod1Buffer<int> bufA = InstallMethod<int>(store, 0);
			FastMethod1Buffer<int> bufB = InstallMethod<int>(store, 1);
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
		public async Task Map_WithBuffer_PreservesFastPathSource()
		{
			FastMockInteractions store = new(1);
			FastMethod0Buffer buffer = InstallMethod(store, 0);
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
		public async Task Map_WithoutBuffer_StillCarriesPredicate()
		{
			FastMockInteractions store = new(0);
			VerificationResult<object> source = new(
				new object(),
				store,
				_ => true,
				"expected");

			VerificationResult<int> mapped = source.Map(42);

			await That(((IVerificationResult<int>)mapped).Object).IsEqualTo(42);
			await That(((IVerificationResult)mapped).Expectation).IsEqualTo("expected");
		}
	}

	public sealed class CollectMatchingTests
	{
		[Test]
		public async Task WithBufferAndExactlyTwoRecords_PreservesSequenceOrder()
		{
			// Crosses the `records.Count > 1` boundary in CollectMatching at exactly N=2. Combined
			// with the existing N=1 / N=3 / N=0 cases, this pins the boundary so flipping `> 1` to
			// `>= 1` is no longer a silent rewrite of the sort path.
			FastMockInteractions store = new(1);
			FastMethod1Buffer<int> buffer = InstallMethod<int>(store, 0);
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
		public async Task WithBufferAndMultipleRecords_PreservesSequenceOrder()
		{
			FastMockInteractions store = new(1);
			FastMethod1Buffer<int> buffer = InstallMethod<int>(store, 0);
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
		public async Task WithBufferAndNoMatchingRecord_ReturnsEmpty()
		{
			FastMockInteractions store = new(1);
			FastMethod1Buffer<int> buffer = InstallMethod<int>(store, 0);
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
		public async Task WithBufferAndSingleRecord_ReturnsRecord()
		{
			FastMockInteractions store = new(1);
			FastMethod0Buffer buffer = InstallMethod(store, 0);
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
		public async Task WithEmptyBuffer_ReturnsEmpty()
		{
			FastMockInteractions store = new(1);
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
	}

	public sealed class SkipInteractionRecordingTests
	{
		[Test]
		public async Task Verify_WhenRecordingDisabled_AndAwaitableViaWithin_ShouldThrowMockException()
		{
			// Same guard, Awaitable Verify (records-array) path. Constructed with a predicate-based
			// VR (not a typed CountSource) so we exercise the slow CollectMatching branch.
			FastMockInteractions store = new(0, true);
			VerificationResult<object> result = new(
				new object(), store, _ => true, "expected");

			VerificationResult<object> awaitable = result.Within(50.Milliseconds());

			void Act()
			{
				((IVerificationResult)awaitable).Verify(_ => true);
			}

			await That(Act).Throws<MockException>()
				.WithMessage("*recording is disabled*").AsWildcard();
		}

		[Test]
		public async Task VerifyCount_WhenRecordingDisabled_AndAwaitableViaWithin_ShouldThrowMockException()
		{
			// Same guard, Awaitable path (via Within). Distinct mutation site.
			FastMockInteractions store = new(1, true);
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
		public async Task VerifyCount_WhenRecordingDisabled_ShouldThrowMockException()
		{
			// Kills the ThrowIfRecordingDisabled statement-removal mutation on the non-Awaitable
			// IFastVerifyCountResult.VerifyCount path. Without the guard, Once() would silently
			// report a (probably false) count and never throw.
			FastMockInteractions store = new(1, true);
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
		public async Task AnyParameters_WithoutBuffer_AndNoOverloadFilter_MatchesAllOfMethodName()
		{
			IMockInteractions store = new FastMockInteractions(0);

			store.RegisterInteraction(new MethodInvocation<int>("Foo", 1));
			store.RegisterInteraction(new MethodInvocation<string>("Foo", "x"));
			store.RegisterInteraction(new MethodInvocation<int>("Bar", 1));

			VerificationResult<object>.IgnoreParameters result = new(
				new object(),
				store,
				"Foo",
				_ => false,
				null,
				() => "Foo");

			void Act()
			{
				result.AnyParameters().Exactly(2);
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task AnyParameters_WithoutBuffer_KeepsOverloadFilter()
		{
			IMockInteractions store = new FastMockInteractions(0);
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
