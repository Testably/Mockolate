using Mockolate.Exceptions;
using Mockolate.Parameters;
using Mockolate.Verify;

namespace Mockolate.Tests.Verify;

public class MockRegistryVerifyTests
{
	public sealed class FailureMessageTests
	{
		[Test]
		public async Task IndexerGotTyped_FourKeys_FailureMessageIncludesGotIndexerPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastIndexerGetterBuffer<int, string, bool, double>>(0,
				static f => new FastIndexerGetterBuffer<int, string, bool, double>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.IndexerGotTyped(new object(), 0,
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("a"),
					(IParameterMatch<bool>)It.Is(true),
					(IParameterMatch<double>)It.Is(1.0),
					() => "[1, \"a\", true, 1.0]").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*got indexer [1, \"a\", true, 1.0]*").AsWildcard();
		}

		[Test]
		public async Task IndexerGotTyped_OneKey_FailureMessageIncludesGotIndexerPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastIndexerGetterBuffer<int>>(0,
				static f => new FastIndexerGetterBuffer<int>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.IndexerGotTyped(new object(), 0,
					(IParameterMatch<int>)It.Is(1),
					() => "[1]").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*got indexer [1]*").AsWildcard();
		}

		[Test]
		public async Task IndexerGotTyped_ThreeKeys_FailureMessageIncludesGotIndexerPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastIndexerGetterBuffer<int, string, bool>>(0,
				static f => new FastIndexerGetterBuffer<int, string, bool>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.IndexerGotTyped(new object(), 0,
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("a"),
					(IParameterMatch<bool>)It.Is(true),
					() => "[1, \"a\", true]").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*got indexer [1, \"a\", true]*").AsWildcard();
		}

		[Test]
		public async Task IndexerGotTyped_TwoKeys_FailureMessageIncludesGotIndexerPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastIndexerGetterBuffer<int, string>>(0,
				static f => new FastIndexerGetterBuffer<int, string>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.IndexerGotTyped(new object(), 0,
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("a"),
					() => "[1, \"a\"]").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*got indexer [1, \"a\"]*").AsWildcard();
		}

		[Test]
		public async Task IndexerSetTyped_FourKeys_FailureMessageIncludesSetIndexerPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastIndexerSetterBuffer<int, string, bool, double, char>>(0,
				static f => new FastIndexerSetterBuffer<int, string, bool, double, char>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.IndexerSetTyped(new object(), 0,
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("a"),
					(IParameterMatch<bool>)It.Is(true),
					(IParameterMatch<double>)It.Is(1.0),
					(IParameterMatch<char>)It.Is('z'),
					() => "[1, \"a\", true, 1.0]").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*set indexer [1, \"a\", true, 1.0] to *z*").AsWildcard();
		}

		[Test]
		public async Task IndexerSetTyped_OneKey_FailureMessageIncludesSetIndexerPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastIndexerSetterBuffer<int, string>>(0,
				static f => new FastIndexerSetterBuffer<int, string>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.IndexerSetTyped(new object(), 0,
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("a"),
					() => "[1]").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*set indexer [1] to *a*").AsWildcard();
		}

		[Test]
		public async Task IndexerSetTyped_ThreeKeys_FailureMessageIncludesSetIndexerPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastIndexerSetterBuffer<int, string, bool, double>>(0,
				static f => new FastIndexerSetterBuffer<int, string, bool, double>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.IndexerSetTyped(new object(), 0,
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("a"),
					(IParameterMatch<bool>)It.Is(true),
					(IParameterMatch<double>)It.Is(1.0),
					() => "[1, \"a\", true]").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*set indexer [1, \"a\", true] to *1*").AsWildcard();
		}

		[Test]
		public async Task IndexerSetTyped_TwoKeys_FailureMessageIncludesSetIndexerPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastIndexerSetterBuffer<int, string, bool>>(0,
				static f => new FastIndexerSetterBuffer<int, string, bool>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.IndexerSetTyped(new object(), 0,
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("a"),
					(IParameterMatch<bool>)It.Is(true),
					() => "[1, \"a\"]").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*set indexer [1, \"a\"] to *true*").AsWildcard();
		}

		[Test]
		public async Task SubscribedToTyped_FailureMessageIncludesSubscribedToEventPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastEventBuffer>(0,
				static f => new FastEventBuffer(f, FastEventBufferKind.Subscribe));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.SubscribedToTyped(new object(), 0, "Tick").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*subscribed to event Tick*").AsWildcard();
		}

		[Test]
		public async Task UnsubscribedFromTyped_FailureMessageIncludesUnsubscribedFromEventPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastEventBuffer>(0,
				static f => new FastEventBuffer(f, FastEventBufferKind.Unsubscribe));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.UnsubscribedFromTyped(new object(), 0, "Tick").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*unsubscribed from event Tick*").AsWildcard();
		}

		[Test]
		public async Task VerifyMethodTyped_FourParameters_FailureMessageIncludesInvokedMethodPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastMethod4Buffer<int, string, bool, double>>(0,
				static f => new FastMethod4Buffer<int, string, bool, double>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo",
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("a"),
					(IParameterMatch<bool>)It.Is(true),
					(IParameterMatch<double>)It.Is(1.0),
					() => "Foo(1, \"a\", true, 1.0)").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*invoked method Foo(1, \"a\", true, 1.0)*").AsWildcard();
		}

		[Test]
		public async Task VerifyMethodTyped_OneParameter_FailureMessageIncludesInvokedMethodPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
				static f => new FastMethod1Buffer<int>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo",
					(IParameterMatch<int>)It.Is(1),
					() => "Foo(1)").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*invoked method Foo(1)*").AsWildcard();
		}

		[Test]
		public async Task VerifyMethodTyped_Parameterless_FailureMessageIncludesInvokedMethodPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastMethod0Buffer>(0,
				static f => new FastMethod0Buffer(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo", () => "Foo()").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*invoked method Foo()*").AsWildcard();
		}

		[Test]
		public async Task VerifyMethodTyped_ThreeParameters_FailureMessageIncludesInvokedMethodPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastMethod3Buffer<int, string, bool>>(0,
				static f => new FastMethod3Buffer<int, string, bool>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo",
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("a"),
					(IParameterMatch<bool>)It.Is(true),
					() => "Foo(1, \"a\", true)").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*invoked method Foo(1, \"a\", true)*").AsWildcard();
		}

		[Test]
		public async Task VerifyMethodTyped_TwoParameters_FailureMessageIncludesInvokedMethodPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastMethod2Buffer<int, string>>(0,
				static f => new FastMethod2Buffer<int, string>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo",
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("a"),
					() => "Foo(1, \"a\")").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*invoked method Foo(1, \"a\")*").AsWildcard();
		}

		[Test]
		public async Task VerifyPropertyTyped_Getter_FailureMessageIncludesGotPropertyPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastPropertyGetterBuffer>(0,
				static f => new FastPropertyGetterBuffer(f, new PropertyGetterAccess(string.Empty)));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyPropertyTyped(new object(), 0, "X").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*got property X*").AsWildcard();
		}

		[Test]
		public async Task VerifyPropertyTyped_Setter_FailureMessageIncludesSetPropertyPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastPropertySetterBuffer<int>>(0,
				static f => new FastPropertySetterBuffer<int>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyPropertyTyped(new object(), 0, "X",
					(IParameterMatch<int>)It.Is(7)).Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*set property X to *7*").AsWildcard();
		}
	}

	public sealed class TryGetBufferBoundaryTests
	{
		[Test]
		public async Task VerifyMethodTyped_WithMemberIdEqualToBufferLength_ShouldFallToSlowPath()
		{
			// Pins the `(uint)memberId < (uint)buffers.Length` boundary in TryGetBuffer.
			// Mutation `<=` would let memberId == buffers.Length pass the bounds check and
			// IndexOutOfRangeException-crash on `buffers[memberId]`. Asserting the verify
			// resolves cleanly via the slow-path fallback documents the boundary.
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastMethod0Buffer>(0,
				static f => new FastMethod0Buffer(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyMethod(new object(), 1, "Foo", () => "Foo()").Never();
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task VerifyMethodTyped_WithMemberIdFarOutOfRange_ShouldFallToSlowPath()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastMethod0Buffer>(0,
				static f => new FastMethod0Buffer(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyMethod(new object(), 99, "Foo", () => "Foo()").Never();
			}

			await That(Act).DoesNotThrow();
		}
	}

	public sealed class SlowPathPredicateTests
	{
		[Test]
		public async Task VerifyMethodTyped_FourParam_SlowPath_AllMatchersTrue_Counts()
		{
			// Positive case for the same slow-path lambda — the matched record must be counted.
			FastMockInteractions store = new(0);
			MockRegistry registry = new(MockBehavior.Default, store);

			IMockInteractions interactions = store;
			interactions.RegisterInteraction(
				new MethodInvocation<int, string, bool, double>("Foo", 1, "ok", true, 1.0));

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo",
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("ok"),
					(IParameterMatch<bool>)It.Is(true),
					(IParameterMatch<double>)It.Is(1.0),
					() => "Foo(1, \"ok\", true, 1.0)").Once();
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task VerifyMethodTyped_FourParam_SlowPath_RejectsWhenLastMatcherFails()
		{
			FastMockInteractions store = new(0);
			MockRegistry registry = new(MockBehavior.Default, store);

			IMockInteractions interactions = store;
			interactions.RegisterInteraction(
				new MethodInvocation<int, string, bool, double>("Foo", 1, "ok", true, 9.9));

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo",
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("ok"),
					(IParameterMatch<bool>)It.Is(true),
					(IParameterMatch<double>)It.Is(1.0),
					() => "Foo(1, \"ok\", true, 1.0)").Once();
			}

			await That(Act).Throws<MockVerificationException>();
		}

		[Test]
		public async Task VerifyMethodTyped_ThreeParam_SlowPath_RejectsWhenLastMatcherFails()
		{
			FastMockInteractions store = new(0);
			MockRegistry registry = new(MockBehavior.Default, store);

			IMockInteractions interactions = store;
			interactions.RegisterInteraction(new MethodInvocation<int, string, bool>("Foo", 1, "ok", false));

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo",
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("ok"),
					(IParameterMatch<bool>)It.Is(true),
					() => "Foo(1, \"ok\", true)").Once();
			}

			await That(Act).Throws<MockVerificationException>();
		}

		[Test]
		public async Task VerifyMethodTyped_TwoParam_SlowPath_RejectsWhenFirstMatcherFails()
		{
			// The "logical mutation" survivors live in the slow-path lambda used when TryGetBuffer
			// returns null. With an out-of-range memberId we force that branch; then we record an
			// interaction that satisfies match2 but NOT match1, and verify it is correctly excluded.
			// This kills the mutation that drops the `&& match1` term from the AND-chain.
			FastMockInteractions store = new(0);
			MockRegistry registry = new(MockBehavior.Default, store);

			IMockInteractions interactions = store;
			interactions.RegisterInteraction(new MethodInvocation<int, string>("Foo", 99, "ok"));

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo",
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("ok"),
					() => "Foo(1, \"ok\")").Once();
			}

			await That(Act).Throws<MockVerificationException>();
		}

		[Test]
		public async Task VerifyMethodTyped_TwoParam_SlowPath_RejectsWhenSecondMatcherFails()
		{
			FastMockInteractions store = new(0);
			MockRegistry registry = new(MockBehavior.Default, store);

			IMockInteractions interactions = store;
			interactions.RegisterInteraction(new MethodInvocation<int, string>("Foo", 1, "no"));

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo",
					(IParameterMatch<int>)It.Is(1),
					(IParameterMatch<string>)It.Is<string>("ok"),
					() => "Foo(1, \"ok\")").Once();
			}

			await That(Act).Throws<MockVerificationException>();
		}
	}

	public sealed class VerifyPropertyFastPathTests
	{
		[Test]
		public async Task Getter_FailureMessageIncludesGotPropertyPrefix()
		{
			// Pins the expectation factory `() => $"got property {propertyName...}"` against the
			// string-removal mutation that would replace it with an empty interpolation.
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastPropertyGetterBuffer>(0,
				static f => new FastPropertyGetterBuffer(f, new PropertyGetterAccess(string.Empty)));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyProperty(new object(), 0, "X").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*got property X*").AsWildcard();
		}

		[Test]
		public async Task Getter_WithBufferAndRecord_PredicateMatchesEveryRecord()
		{
			// Pins `static _ => true` predicate in the public memberId-keyed VerifyProperty getter.
			// Mutating it to `static _ => false` would skip every record and turn this Once() into a
			// failed verification.
			FastMockInteractions store = new(1);
			FastPropertyGetterBuffer buffer = store.GetOrCreateBuffer<FastPropertyGetterBuffer>(0,
				static f => new FastPropertyGetterBuffer(f, new PropertyGetterAccess("X")));
			buffer.Append();
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyProperty(new object(), 0, "X").Once();
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task Setter_FailureMessageIncludesSetPropertyPrefix()
		{
			// Pins the expectation factory `() => $"set property {propertyName...} to {value}"`
			// against the string-removal mutation.
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastPropertySetterBuffer<int>>(0,
				static f => new FastPropertySetterBuffer<int>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyProperty(new object(), 0, "X",
					(IParameterMatch<int>)It.Is(7)).Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*set property X to *7*").AsWildcard();
		}

		[Test]
		public async Task Setter_WithBufferAndMatchingRecord_PredicateMatchesByValue()
		{
			// Pins the local Predicate's body in the memberId-keyed property setter overload.
			// Block-removal would turn the predicate into `return default;` (i.e. always false) and
			// drop every recorded setter, failing this Once().
			FastMockInteractions store = new(1);
			FastPropertySetterBuffer<int> buffer = store.GetOrCreateBuffer<FastPropertySetterBuffer<int>>(0,
				static f => new FastPropertySetterBuffer<int>(f));
			buffer.Append("X", 7);
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyProperty(new object(), 0, "X",
					(IParameterMatch<int>)It.Is(7)).Once();
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task Setter_WithNonMatchingValue_PredicateRejects()
		{
			// Same Predicate body — but exercises the negative branch so the `value.Matches(...)`
			// term cannot be silently dropped.
			FastMockInteractions store = new(1);
			FastPropertySetterBuffer<int> buffer = store.GetOrCreateBuffer<FastPropertySetterBuffer<int>>(0,
				static f => new FastPropertySetterBuffer<int>(f));
			buffer.Append("X", 99);
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyProperty(new object(), 0, "X",
					(IParameterMatch<int>)It.Is(7)).Once();
			}

			await That(Act).Throws<MockVerificationException>();
		}
	}

	public sealed class IndexerFastPathTests
	{
		[Test]
		public async Task Got_FailureMessageIncludesGotIndexerPrefix()
		{
			// Pins `() => $"got indexer {parametersDescription()}"` in the public memberId-keyed
			// IndexerGot expression body against the string-removal mutation.
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastIndexerGetterBuffer<int>>(0,
				static f => new FastIndexerGetterBuffer<int>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.IndexerGot(new object(), 0,
					_ => true,
					() => "[1]").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*got indexer [1]*").AsWildcard();
		}

		[Test]
		public async Task Set_FailureMessageIncludesSetIndexerPrefix()
		{
			// Pins `() => $"set indexer {parametersDescription()} to {value}"` against the
			// string-removal mutation.
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastIndexerSetterBuffer<int, string>>(0,
				static f => new FastIndexerSetterBuffer<int, string>(f));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.IndexerSet(new object(), 0,
					(_, _) => true,
					(IParameterMatch<string>)It.Is<string>("x"),
					() => "[1]").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*set indexer [1] to *x*").AsWildcard();
		}

		[Test]
		public async Task Set_WithBufferAndMatchingRecord_PredicateMatchesByValue()
		{
			// Pins the local Predicate body in the memberId-keyed IndexerSet overload — the
			// block-removal mutant would turn it into `return default;` and skip every record.
			FastMockInteractions store = new(1);
			FastIndexerSetterBuffer<int, string> buffer = store.GetOrCreateBuffer<FastIndexerSetterBuffer<int, string>>(0,
				static f => new FastIndexerSetterBuffer<int, string>(f));
			buffer.Append(1, "x");
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.IndexerSet(new object(), 0,
					(interaction, v) => interaction is IndexerSetterAccess<int, string> s &&
					                    s.Parameter1 == 1 && v.Matches(s.TypedValue),
					(IParameterMatch<string>)It.Is<string>("x"),
					() => "[1]").Once();
			}

			await That(Act).DoesNotThrow();
		}
	}

	public sealed class SubscribedToFastPathTests
	{
		[Test]
		public async Task FailureMessageIncludesSubscribedToEventPrefix()
		{
			// Pins the expectation factory `() => $"subscribed to event {eventName...}"`.
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastEventBuffer>(0,
				static f => new FastEventBuffer(f, FastEventBufferKind.Subscribe));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.SubscribedTo(new object(), 0, "Tick").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*subscribed to event Tick*").AsWildcard();
		}

		[Test]
		public async Task WithBufferAndRecord_PredicateMatchesEveryRecord()
		{
			// Pins `static _ => true` in the memberId-keyed SubscribedTo. Mutating to `false`
			// would drop every record and fail Once().
			FastMockInteractions store = new(1);
			FastEventBuffer buffer = store.GetOrCreateBuffer<FastEventBuffer>(0,
				static f => new FastEventBuffer(f, FastEventBufferKind.Subscribe));
			buffer.Append("Tick", null, typeof(object).GetMethod(nameof(ToString))!);
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.SubscribedTo(new object(), 0, "Tick").Once();
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task WithoutBuffer_FallsBackToLegacyAndFiltersByEventName()
		{
			// Kills two coupled mutants on `if (buffer is null)`:
			//   - equality flip `is not null` would fall through to the fast-path constructor with
			//     a null buffer and `static _ => true`, which CollectMatching's null-buffer branch
			//     turns into "match every recorded interaction";
			//   - block removal `{}` does the same thing (drops the early `return SubscribedTo(...)`).
			// Either way, the unrelated PropertyGetterAccess below would be counted as a Tick
			// subscription and `.Never()` would throw.
			FastMockInteractions store = new(0);
			MockRegistry registry = new(MockBehavior.Default, store);

			IMockInteractions interactions = store;
			interactions.RegisterInteraction(new PropertyGetterAccess("Other"));

			void Act()
			{
				registry.SubscribedTo(new object(), 0, "Tick").Never();
			}

			await That(Act).DoesNotThrow();
		}
	}

	public sealed class UnsubscribedFromFastPathTests
	{
		[Test]
		public async Task FailureMessageIncludesUnsubscribedFromEventPrefix()
		{
			FastMockInteractions store = new(1);
			store.GetOrCreateBuffer<FastEventBuffer>(0,
				static f => new FastEventBuffer(f, FastEventBufferKind.Unsubscribe));
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.UnsubscribedFrom(new object(), 0, "Tick").Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*unsubscribed from event Tick*").AsWildcard();
		}

		[Test]
		public async Task WithBufferAndRecord_PredicateMatchesEveryRecord()
		{
			FastMockInteractions store = new(1);
			FastEventBuffer buffer = store.GetOrCreateBuffer<FastEventBuffer>(0,
				static f => new FastEventBuffer(f, FastEventBufferKind.Unsubscribe));
			buffer.Append("Tick", null, typeof(object).GetMethod(nameof(ToString))!);
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.UnsubscribedFrom(new object(), 0, "Tick").Once();
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task WithoutBuffer_FallsBackToLegacyAndFiltersByEventName()
		{
			FastMockInteractions store = new(0);
			MockRegistry registry = new(MockBehavior.Default, store);

			IMockInteractions interactions = store;
			interactions.RegisterInteraction(new PropertyGetterAccess("Other"));

			void Act()
			{
				registry.UnsubscribedFrom(new object(), 0, "Tick").Never();
			}

			await That(Act).DoesNotThrow();
		}
	}

	public sealed class VerifyMethodFallbackTests
	{
		[Test]
		public async Task Parameterless_WithoutFastMethod0Buffer_PredicateMatchesAllMethodInvocations()
		{
			// Kills the `_ => true` → `_ => false` mutation in the slow-path fallback at the bottom
			// of `VerifyMethod<T>(subject, memberId, methodName, expectation)`. We enter the
			// fallback because no FastMethod0Buffer is installed at memberId 0; the registered
			// MethodInvocation must still be counted.
			FastMockInteractions store = new(0);
			MockRegistry registry = new(MockBehavior.Default, store);

			IMockInteractions interactions = store;
			interactions.RegisterInteraction(new MethodInvocation("Foo"));

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo", () => "Foo()").Once();
			}

			await That(Act).DoesNotThrow();
		}
	}
}
