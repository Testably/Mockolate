using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Parameters;
using Mockolate.Verify;

namespace Mockolate.Internal.Tests.Registry;

public class MockRegistryVerifyTests
{
	public sealed class FailureMessageTests
	{
		[Test]
		public async Task IndexerGotTyped_FourKeys_FailureMessageIncludesGotIndexerPrefix()
		{
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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

	public sealed class TryGetRecordsBoundaryTests
	{
		[Test]
		public async Task VerifyMethodTyped_WithMemberIdEqualToBufferLength_ShouldFallToSlowPath()
		{
			// Pins the `(uint)memberId < (uint)buffers.Length` boundary in TryGetRecords.
			// Mutation `<=` would let memberId == buffers.Length pass the bounds check and
			// IndexOutOfRangeException-crash on `buffers[memberId]`. Asserting the verify
			// resolves cleanly via the slow-path fallback documents the boundary.
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(0);
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
			MockInteractions store = new(0);
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
			MockInteractions store = new(0);
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
			// The "logical mutation" survivors live in the slow-path lambda used when TryGetRecords
			// returns null. With an out-of-range memberId we force that branch; then we record an
			// interaction that satisfies match2 but NOT match1, and verify it is correctly excluded.
			// This kills the mutation that drops the `&& match1` term from the AND-chain.
			MockInteractions store = new(0);
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
			MockInteractions store = new(0);
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

	public sealed class IndexerFastPathTests
	{
		[Test]
		public async Task Got_FailureMessageIncludesGotIndexerPrefix()
		{
			// Pins `() => $"got indexer {parametersDescription()}"` in the public memberId-keyed
			// IndexerGot expression body against the string-removal mutation.
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
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
			MockInteractions store = new(1);
			IndexerSetterRecords<int, string> buffer = new(store, 0);
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

	public sealed class VerifyMethodFallbackTests
	{
		[Test]
		public async Task Parameterless_WithoutMethodRecords0_PredicateMatchesAllMethodInvocations()
		{
			// Kills the `_ => true` → `_ => false` mutation in the slow-path fallback at the bottom
			// of `VerifyMethod<T>(subject, memberId, methodName, expectation)`. We enter the
			// fallback because no MethodRecords is installed at memberId 0; the registered
			// MethodInvocation must still be counted.
			MockInteractions store = new(0);
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
