using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Internal.Tests.TestHelpers;
using Mockolate.Parameters;
using Mockolate.Setup;
using Mockolate.Verify;

namespace Mockolate.Internal.Tests.Registry;

public sealed class MockRegistryTests
{
	private static MethodInfo GetMethodInfo()
		=> typeof(MockRegistryTests).GetMethod(nameof(GetMethodInfo),
			BindingFlags.Static | BindingFlags.NonPublic)!;

	public sealed class ConstructorSkipFlagAgreementTests
	{
		[Test]
		public async Task BehaviorAndInteractions_WhenSkipFlagsDisagree_Throws()
		{
			MockBehavior recordingBehavior = MockBehavior.Default;
			FastMockInteractions skippingInteractions = new(0, true);

			void Act()
			{
				_ = new MockRegistry(recordingBehavior, skippingInteractions);
			}

			await That(Act).Throws<ArgumentException>()
				.WithMessage("*SkipInteractionRecording*").AsWildcard();
		}

		[Test]
		public async Task BehaviorAndInteractions_WhenSkippingBehaviorMeetsRecordingInteractions_Throws()
		{
			MockBehavior skippingBehavior = MockBehavior.Default.SkippingInteractionRecording();
			FastMockInteractions recordingInteractions = new(0);

			void Act()
			{
				_ = new MockRegistry(skippingBehavior, recordingInteractions);
			}

			await That(Act).Throws<ArgumentException>()
				.WithMessage("*SkipInteractionRecording*").AsWildcard();
		}

		[Test]
		public async Task RegistryAndInteractions_WhenSkipFlagsDisagree_Throws()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FastMockInteractions skippingInteractions = new(0, true);

			void Act()
			{
				_ = new MockRegistry(registry, skippingInteractions);
			}

			await That(Act).Throws<ArgumentException>()
				.WithMessage("*SkipInteractionRecording*").AsWildcard();
		}

		[Test]
		public async Task RegistryAndInteractions_WhenSkippingRegistryMeetsRecordingInteractions_Throws()
		{
			MockBehavior skippingBehavior = MockBehavior.Default.SkippingInteractionRecording();
			MockRegistry registry = new(skippingBehavior, new FastMockInteractions(0, true));
			FastMockInteractions recordingInteractions = new(0);

			void Act()
			{
				_ = new MockRegistry(registry, recordingInteractions);
			}

			await That(Act).Throws<ArgumentException>()
				.WithMessage("*SkipInteractionRecording*").AsWildcard();
		}
	}

	public sealed class GetIndexerSetupScenarioScopingTests
	{
		[Test]
		public async Task WithActiveScenario_ShouldReturnScopedSetupOverGlobalSetup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup globalSetup = new(true);
			FakeIndexerSetup scopedSetup = new(true);
			registry.Setup.Indexers.Add(globalSetup);
			registry.Setup.GetOrCreateScenario("a").Indexers.Add(scopedSetup);

			registry.TransitionTo("a");
			IndexerSetup? result = registry.GetIndexerSetup<IndexerSetup>(_ => true);

			await That(result).IsSameAs(scopedSetup);
		}

		[Test]
		public async Task WithoutActiveScenario_ShouldFallBackToGlobalSetup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup globalSetup = new(true);
			FakeIndexerSetup scopedSetup = new(true);
			registry.Setup.Indexers.Add(globalSetup);
			registry.Setup.GetOrCreateScenario("a").Indexers.Add(scopedSetup);

			IndexerSetup? result = registry.GetIndexerSetup<IndexerSetup>(_ => true);

			await That(result).IsSameAs(globalSetup);
		}
	}

	public sealed class IndexerFallbackStoresValueTests
	{
		[Test]
		public async Task ApplyIndexerGetter_WithNullSetup_ShouldStoreBaseValueForLaterLookup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			IndexerGetterAccess<int> access1 = new(1);
			IndexerGetterAccess<int> access2 = new(1);

			int first = registry.ApplyIndexerGetter(access1, null, 42, 0);
			int second = registry.ApplyIndexerGetter(access2, null, 99, 0);

			await That(first).IsEqualTo(42);
			await That(second).IsEqualTo(42);
		}

		[Test]
		public async Task GetIndexerFallback_ShouldStoreDefaultForLaterLookup()
		{
			int counter = 0;
			MockBehavior behavior = MockBehavior.Default.WithDefaultValueFor(() => ++counter);
			MockRegistry registry = new(behavior, new FastMockInteractions(0, behavior.SkipInteractionRecording));
			IndexerGetterAccess<int> access1 = new(1);
			IndexerGetterAccess<int> access2 = new(1);

			int first = registry.GetIndexerFallback<int>(access1, 0);
			int second = registry.GetIndexerFallback<int>(access2, 0);

			await That(first).IsEqualTo(1);
			await That(second).IsEqualTo(1);
		}
	}

	public sealed class ApplyIndexerGetterLazyGeneratorTests
	{
		[Test]
		public async Task WithNullSetup_LooseMode_NoStoredValue_ShouldInvokeGeneratorAndStore()
		{
			int callCount = 0;
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			IndexerGetterAccess<int> first = new(1);
			IndexerGetterAccess<int> second = new(1);

			int firstResult = registry.ApplyIndexerGetter(first, null, () => ++callCount, 0);
			int secondResult = registry.ApplyIndexerGetter(second, null, () => ++callCount, 0);

			await That(firstResult).IsEqualTo(1);
			await That(secondResult).IsEqualTo(1);
			await That(callCount).IsEqualTo(1);
		}

		[Test]
		public async Task WithNullSetup_StrictMode_NoStoredValue_ShouldThrow()
		{
			MockBehavior behavior = MockBehavior.Default.ThrowingWhenNotSetup();
			MockRegistry registry = new(behavior, new FastMockInteractions(0, behavior.SkipInteractionRecording));
			IndexerGetterAccess<int> access = new(1);

			void Act()
			{
				registry.ApplyIndexerGetter(access, null, () => 99, 0);
			}

			await That(Act).Throws<MockNotSetupException>()
				.WithMessage("*was accessed without prior setup*").AsWildcard();
		}

		[Test]
		public async Task WithNullSetup_StrictMode_WhenValueWasPreviouslyStored_ShouldReturnStored()
		{
			MockBehavior behavior = MockBehavior.Default.ThrowingWhenNotSetup();
			MockRegistry registry = new(behavior, new FastMockInteractions(0, behavior.SkipInteractionRecording));
			IndexerSetterAccess<int, int> setterAccess = new(1, 7);
			IndexerGetterAccess<int> getterAccess = new(1);

			registry.ApplyIndexerSetter(setterAccess, null, 7, 0);
			int result = registry.ApplyIndexerGetter(getterAccess, null, () => 99, 0);

			await That(result).IsEqualTo(7);
		}
	}

	public sealed class GetIndexerFallbackTests
	{
		[Test]
		public async Task StrictMode_NoStoredValue_ShouldThrow()
		{
			MockBehavior behavior = MockBehavior.Default.ThrowingWhenNotSetup();
			MockRegistry registry = new(behavior, new FastMockInteractions(0, behavior.SkipInteractionRecording));
			IndexerGetterAccess<int> access = new(1);

			void Act()
			{
				registry.GetIndexerFallback<int>(access, 0);
			}

			await That(Act).Throws<MockNotSetupException>()
				.WithMessage("*was accessed without prior setup*").AsWildcard();
		}

		[Test]
		public async Task WhenValueWasPreviouslyStored_ShouldReturnStoredWithoutGeneratingDefault()
		{
			int counter = 0;
			MockBehavior behavior = MockBehavior.Default.WithDefaultValueFor(() => ++counter);
			MockRegistry registry = new(behavior, new FastMockInteractions(0, behavior.SkipInteractionRecording));
			IndexerSetterAccess<int, int> setterAccess = new(1, 42);
			IndexerGetterAccess<int> getterAccess = new(1);

			registry.ApplyIndexerSetter(setterAccess, null, 42, 0);
			int result = registry.GetIndexerFallback<int>(getterAccess, 0);

			await That(result).IsEqualTo(42);
			await That(counter).IsEqualTo(0);
		}
	}

	public sealed class WrapConstructorTests
	{
		[Test]
		public async Task WhenBehaviorSkipsInteractionRecording_WrappedRegistryAlsoSkipsRecording()
		{
			MockBehavior behavior = MockBehavior.Default.SkippingInteractionRecording();
			MockRegistry original = new(behavior, new FastMockInteractions(0, behavior.SkipInteractionRecording));
			object wrapped = new();

			MockRegistry wrappingRegistry = new(original, wrapped);

			MethodInvocation interaction = new("test");
			wrappingRegistry.Interactions.RegisterInteraction(interaction);

			await That(wrappingRegistry.Interactions.Count).IsEqualTo(0);
		}
	}

	public sealed class VerifyPropertyStringKeyedTests
	{
		[Test]
		public async Task MockGot_WhenNameDoesNotMatch_ShouldReturnNever()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.GetProperty("foo.bar", () => 0, null);

			VerificationResult<object> result = registry.VerifyProperty<object>(this, "baz.bar");

			await That(result).Never();
		}

		[Test]
		public async Task MockGot_WhenNameMatches_ShouldReturnOnce()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.GetProperty("foo.bar", () => 0, null);

			VerificationResult<object> result = registry.VerifyProperty<object>(this, "foo.bar");

			await That(result).Once();
		}

		[Test]
		public async Task MockGot_WithoutInteractions_ShouldReturnNeverResultWithExpectation()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			VerificationResult<object> result = registry.VerifyProperty<object>(this, "foo.bar");

			await That(result).Never();
			await That(((IVerificationResult)result).Expectation).IsEqualTo("got property bar");
		}

		[Test]
		public async Task MockSet_WhenNameAndValueMatches_ShouldReturnOnce()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.SetProperty("foo.bar", 4);

			VerificationResult<object> result =
				registry.VerifyProperty<object, int>(this, "foo.bar", (IParameterMatch<int>)It.IsAny<int>());

			await That(result).Once();
		}

		[Test]
		public async Task MockSet_WhenOnlyNameMatches_ShouldReturnNever()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.SetProperty("foo.bar", 4);

			VerificationResult<object> result =
				registry.VerifyProperty<object, string>(this, "foo.bar", (IParameterMatch<string>)It.IsAny<string>());

			await That(result).Never();
		}

		[Test]
		public async Task MockSet_WhenOnlyValueMatches_ShouldReturnNever()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.SetProperty("foo.bar", 4);

			VerificationResult<object> result =
				registry.VerifyProperty<object, int>(this, "baz.bar", (IParameterMatch<int>)It.IsAny<int>());

			await That(result).Never();
		}

		[Test]
		public async Task MockSet_WithoutInteractions_ShouldReturnNeverResultWithExpectation()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			VerificationResult<object> result =
				registry.VerifyProperty<object, int>(this, "foo.bar", (IParameterMatch<int>)It.IsAny<int>());

			await That(result).Never();
			await That(((IVerificationResult)result).Expectation).IsEqualTo("set property bar to It.IsAny<int>()");
		}

		[Test]
		public async Task VerifyProperty_WhenNameContainsNoDot_ShouldIncludeFullNameInExpectation()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			VerificationResult<int> result = registry.VerifyProperty(0, "SomeNameWithoutADot");

			result.Never();
			await That(((IVerificationResult)result).Expectation).IsEqualTo("got property SomeNameWithoutADot");
		}

		[Test]
		public async Task VerifyProperty_WhenNameStartsWithDot_ShouldOmitDotInExpectation()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			VerificationResult<int> result = registry.VerifyProperty(0, ".bar");

			result.Never();
			await That(((IVerificationResult)result).Expectation).IsEqualTo("got property bar");
		}
	}

	public sealed class SubscribedToStringKeyedTests
	{
		[Test]
		public async Task Subscribed_WhenNameDoesNotMatch_ShouldReturnNever()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.AddEvent("foo.bar", this, GetMethodInfo());

			VerificationResult<object> result = registry.SubscribedTo<object>(this, "baz.bar");

			await That(result).Never();
		}

		[Test]
		public async Task Subscribed_WhenNameMatches_ShouldReturnOnce()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.AddEvent("foo.bar", this, GetMethodInfo());

			VerificationResult<object> result = registry.SubscribedTo<object>(this, "foo.bar");

			await That(result).Once();
		}

		[Test]
		public async Task Subscribed_WithoutInteractions_ShouldReturnNeverResultWithExpectation()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			VerificationResult<object> result = registry.SubscribedTo<object>(this, "baz.bar");

			await That(result).Never();
			await That(((IVerificationResult)result).Expectation).IsEqualTo("subscribed to event bar");
		}
	}

	public sealed class UnsubscribedFromStringKeyedTests
	{
		[Test]
		public async Task Unsubscribed_WhenNameDoesNotMatch_ShouldReturnNever()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.RemoveEvent("foo.bar", this, GetMethodInfo());

			VerificationResult<object> result = registry.UnsubscribedFrom<object>(this, "baz.bar");

			await That(result).Never();
		}

		[Test]
		public async Task Unsubscribed_WhenNameMatches_ShouldReturnOnce()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.RemoveEvent("foo.bar", this, GetMethodInfo());

			VerificationResult<object> result = registry.UnsubscribedFrom<object>(this, "foo.bar");

			await That(result).Once();
		}

		[Test]
		public async Task Unsubscribed_WithoutInteractions_ShouldReturnNeverResultWithExpectation()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			VerificationResult<object> result = registry.UnsubscribedFrom<object>(this, "baz.bar");

			await That(result).Never();
			await That(((IVerificationResult)result).Expectation).IsEqualTo("unsubscribed from event bar");
		}
	}

	public sealed class GetMethodSetupSnapshotTests
	{
		[Test]
		public async Task WithEmptyTable_ShouldReturnNull()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			MethodSetup[]? result = registry.GetMethodSetupSnapshot(0);

			await That(result).IsNull();
		}

		[Test]
		public async Task WithMemberIdAtBoundary_ShouldReturnTheBucket()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			ReturnMethodSetup<int>.WithParameterCollection setup =
				new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Method");
			registry.SetupMethod(5, setup);

			MethodSetup[]? result = registry.GetMethodSetupSnapshot(5);

			await That(result).IsNotNull();
			await That(result!).Contains(setup);
		}

		[Test]
		public async Task WithMemberIdBeyondTable_ShouldReturnNull()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			ReturnMethodSetup<int>.WithParameterCollection setup =
				new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Method");
			registry.SetupMethod(0, setup);

			MethodSetup[]? result = registry.GetMethodSetupSnapshot(5);

			await That(result).IsNull();
		}
	}

	public sealed class GetIndexerSetupSnapshotTests
	{
		[Test]
		public async Task WithEmptyTable_ShouldReturnNull()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			IndexerSetup[]? result = registry.GetIndexerSetupSnapshot(0);

			await That(result).IsNull();
		}

		[Test]
		public async Task WithMemberIdAtBoundary_ShouldReturnTheBucket()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			IndexerSetup<string, int> setup = new(registry, (IParameterMatch<int>)It.IsAny<int>());
			registry.SetupIndexer(5, setup);

			IndexerSetup[]? result = registry.GetIndexerSetupSnapshot(5);

			await That(result).IsNotNull();
			await That(result!).Contains(setup);
		}

		[Test]
		public async Task WithMemberIdBeyondTable_ShouldReturnNull()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			IndexerSetup<string, int> setup = new(registry, (IParameterMatch<int>)It.IsAny<int>());
			registry.SetupIndexer(0, setup);

			IndexerSetup[]? result = registry.GetIndexerSetupSnapshot(5);

			await That(result).IsNull();
		}
	}

	public sealed class GetEventSetupSnapshotTests
	{
		[Test]
		public async Task WithEmptyTable_ShouldReturnNull()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			EventSetup[]? result = registry.GetEventSetupSnapshot(0);

			await That(result).IsNull();
		}

		[Test]
		public async Task WithMemberIdAtBoundary_ShouldReturnTheBucket()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			EventSetup setup = new(registry, "OnEvent");
			registry.SetupEvent(5, setup);

			EventSetup[]? result = registry.GetEventSetupSnapshot(5);

			await That(result).IsNotNull();
			await That(result!).Contains(setup);
		}

		[Test]
		public async Task WithMemberIdBeyondTable_ShouldReturnNull()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			EventSetup setup = new(registry, "OnEvent");
			registry.SetupEvent(0, setup);

			EventSetup[]? result = registry.GetEventSetupSnapshot(5);

			await That(result).IsNull();
		}
	}

	public sealed class GetPropertyFastTests
	{
		[Test]
		public async Task WithActiveScenario_ShouldAlwaysTakeColdPath()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> defaultSetup = new(registry, "P");
			defaultSetup.InitializeWith(10);
			registry.SetupProperty(2, defaultSetup);

			PropertySetup<int> scenarioSetup = new(registry, "P");
			scenarioSetup.InitializeWith(99);
			registry.SetupProperty(2, "myScenario", scenarioSetup);

			registry.TransitionTo("myScenario");
			int result = registry.GetPropertyFast(2, new PropertyGetterAccess("P"), _ => 0);

			await That(result).IsEqualTo(99);
		}

		[Test]
		public async Task WithBaseValueAccessor_ShouldAlwaysTakeColdPath()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> snapshot = new(registry, "P");
			registry.SetupProperty(2, snapshot);

			int baseInvocations = 0;

			int Base()
			{
				baseInvocations++;
				return 99;
			}

			int result = registry.GetPropertyFast(2, new PropertyGetterAccess("P"), _ => 7, Base);

			await That(baseInvocations).IsEqualTo(1);
			await That(result).IsEqualTo(99);
		}

		[Test]
		public async Task WithNullSnapshotAtMemberIdSlot_ShouldFallBackToColdPathWithoutThrowing()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> unrelatedSetup = new(registry, "Q");
			unrelatedSetup.InitializeWith(99);
			registry.SetupProperty(5, unrelatedSetup);

			int result = registry.GetPropertyFast(0, new PropertyGetterAccess("P"), _ => 7);

			await That(result).IsEqualTo(7)
				.Because(
					"registering at index 5 sizes the member-id table to 6 and leaves indices 0..4 null, and reading such a slot must fall through to the cold path rather than dereference the null PropertySetup");
		}

		[Test]
		public async Task WithoutSnapshotSetup_ShouldFallBackToColdPath()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			int result = registry.GetPropertyFast(0, new PropertyGetterAccess("P"), _ => 7);

			await That(result).IsEqualTo(7);
		}

		[Test]
		public async Task WithSnapshotSetup_ShouldBypassSlowResolverAndReturnSetupValue()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> snapshot = new(registry, "P");
			snapshot.InitializeWith(42);
			registry.SetupProperty(2, snapshot);

			int generatorInvocations = 0;

			int Generator(MockBehavior _)
			{
				generatorInvocations++;
				return -1;
			}

			int first = registry.GetPropertyFast(2, new PropertyGetterAccess("P"), Generator);
			int second = registry.GetPropertyFast(2, new PropertyGetterAccess("P"), Generator);

			await That(first).IsEqualTo(42);
			await That(second).IsEqualTo(42);
			await That(generatorInvocations).IsEqualTo(0);
		}
	}

	public sealed class SetPropertyFastTests
	{
		[Test]
		public async Task WhenMemberIdSetupDiffersFromDictSetup_ShouldUseMemberIdSetupInDefaultScope()
		{
			// Pins the member-id-table lookup block inside SetPropertyFast. The first registration goes
			// into both the member-id table[2] and the Properties dictionary; the second (without a
			// memberId) replaces only the dictionary entry. With both entries in place but distinct
			// SkippingBaseClass overrides, the hot path must surface the snapshot setup, not the dict.
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			PropertySetup<int> snapshotSetup = new(registry, "P");
			snapshotSetup.InitializeWith(0);
			((IPropertySetup<int>)snapshotSetup).SkippingBaseClass();
			registry.SetupProperty(2, snapshotSetup);

			PropertySetup<int> dictSetup = new(registry, "P");
			dictSetup.InitializeWith(0);
			((IPropertySetup<int>)dictSetup).SkippingBaseClass(false);
			registry.SetupProperty(dictSetup);

			bool skipBase = registry.SetPropertyFast(2, 3, "P", 42);

			await That(skipBase).IsTrue();
		}

		[Test]
		public async Task WithActiveScenario_ShouldRouteToScenarioSetupOverMemberIdTableSetup()
		{
			// Pins the IsNullOrEmpty(Scenario) guard at the top of SetPropertyFast: when a scenario is
			// active, the member-id table (which only ever holds default-scope setups) must NOT be
			// consulted. The scenario-scoped setup overrides via SkippingBaseClass(true) so we can tell
			// which setup was invoked.
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			PropertySetup<int> defaultSetup = new(registry, "P");
			defaultSetup.InitializeWith(0);
			((IPropertySetup<int>)defaultSetup).SkippingBaseClass(false);
			registry.SetupProperty(2, defaultSetup);

			PropertySetup<int> scenarioSetup = new(registry, "P");
			scenarioSetup.InitializeWith(0);
			((IPropertySetup<int>)scenarioSetup).SkippingBaseClass();
			registry.SetupProperty(2, "myScenario", scenarioSetup);

			registry.TransitionTo("myScenario");
			bool skipBase = registry.SetPropertyFast(2, 3, "P", 42);

			await That(skipBase).IsTrue();
		}

		[Test]
		public async Task WithoutSnapshotSetup_ShouldFallBackToResolveSetup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup = new(registry, "P");
			setup.InitializeWith(10);
			registry.SetupProperty(setup);

			bool skipBase = registry.SetPropertyFast(2, 3, "P", 42);

			await That(skipBase).IsFalse();
			int after = registry.GetProperty("P", () => -1, null);
			await That(after).IsEqualTo(42);
		}

		[Test]
		public async Task WithSnapshotSetup_ShouldInvokeSetterAndStoreValue()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> snapshot = new(registry, "P");
			snapshot.InitializeWith(0);
			registry.SetupProperty(2, snapshot);

			bool skipBase = registry.SetPropertyFast(2, 3, "P", 42);

			await That(skipBase).IsFalse();
			int after = registry.GetPropertyFast(2, new PropertyGetterAccess("P"), _ => -1);
			await That(after).IsEqualTo(42);
		}
	}

	public sealed class AddEventMemberIdTests
	{
		[Test]
		public async Task AddEvent_WithMemberId_ShouldRecordAsEventSubscription()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			registry.AddEvent(0, "OnFoo", this, GetMethodInfo());

			IInteraction recorded = registry.Interactions.Single();
			await That(recorded).IsExactly<EventSubscription>();
		}

		[Test]
		public async Task AddEvent_WithMemberIdAndMatchingSnapshot_ShouldInvokeSubscribedCallback()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			int subscribedCount = 0;
			EventSetup setup = new(registry, "OnFoo");
			setup.OnSubscribed.Do(() => subscribedCount++);
			registry.SetupEvent(7, setup);

			registry.AddEvent(7, "OnFoo", this, GetMethodInfo());

			await That(subscribedCount).IsEqualTo(1);
		}

		[Test]
		public async Task AddEvent_WithNullMethod_ShouldThrowMockException()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			void Act()
			{
				registry.AddEvent(0, "OnFoo", this, null);
			}

			await That(Act).Throws<MockException>();
		}
	}

	public sealed class RemoveEventMemberIdTests
	{
		[Test]
		public async Task RemoveEvent_WithMemberId_ShouldRecordAsEventUnsubscription()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			registry.RemoveEvent(0, "OnFoo", this, GetMethodInfo());

			IInteraction recorded = registry.Interactions.Single();
			await That(recorded).IsExactly<EventUnsubscription>();
		}

		[Test]
		public async Task RemoveEvent_WithMemberIdAndMatchingSnapshot_ShouldInvokeUnsubscribedCallback()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			int unsubscribedCount = 0;
			EventSetup setup = new(registry, "OnFoo");
			setup.OnUnsubscribed.Do(() => unsubscribedCount++);
			registry.SetupEvent(7, setup);

			registry.RemoveEvent(7, "OnFoo", this, GetMethodInfo());

			await That(unsubscribedCount).IsEqualTo(1);
		}

		[Test]
		public async Task RemoveEvent_WithNullMethod_ShouldThrowMockException()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			void Act()
			{
				registry.RemoveEvent(0, "OnFoo", this, null);
			}

			await That(Act).Throws<MockException>();
		}
	}

	public sealed class SetPropertyFallbackBehaviorTests
	{
		[Test]
		public async Task SetProperty_WithoutSnapshot_ShouldReturnBehaviorSkipBaseClassWhenSetupAllowsBase()
		{
			MockBehavior behavior = MockBehavior.Default.SkippingBaseClass();
			MockRegistry registry = new(behavior, new FastMockInteractions(0, behavior.SkipInteractionRecording));

			bool result = registry.SetProperty("P", 42);

			await That(result).IsTrue();
		}

		[Test]
		public async Task SetProperty_WithoutSnapshot_ShouldReturnFalseWhenBehaviorDoesNotSkip()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			bool result = registry.SetProperty("P", 42);

			await That(result).IsFalse();
		}
	}

	public sealed class TryGetBufferTests
	{
		[Test]
		public async Task VerifyMethodTyped_WithNonFastInteractions_ShouldFallToSlowPath()
		{
			// Pins the `Interactions is FastMockInteractions` type-pattern guard at the top of
			// TryGetBuffer. With a non-fast IMockInteractions implementation, the fast-path branch
			// must short-circuit and the verification must succeed via the slow path. (Lives in the
			// internal test project because the custom IMockInteractions implementation needs to
			// satisfy the internal `Verified` contract.)
			NonFastInteractions store = new();
			MockRegistry registry = new(MockBehavior.Default, store);

			void Act()
			{
				registry.VerifyMethod(new object(), 0, "Foo", () => "Foo()").Never();
			}

			await That(Act).DoesNotThrow();
		}

		private sealed class NonFastInteractions : IMockInteractions
		{
			private readonly List<IInteraction> _items = new();

			public bool SkipInteractionRecording => false;

			public int Count => _items.Count;

			public event EventHandler? InteractionAdded
			{
				add { }
				remove { }
			}

			public event EventHandler? OnClearing
			{
				add { }
				remove { }
			}

			public TInteraction RegisterInteraction<TInteraction>(TInteraction interaction)
				where TInteraction : IInteraction
			{
				_items.Add(interaction);
				return interaction;
			}

			public IReadOnlyCollection<IInteraction> GetUnverifiedInteractions()
				=> _items;

			void IMockInteractions.Verified(IEnumerable<IInteraction> interactions) { }

			public void Clear() => _items.Clear();

			public IEnumerator<IInteraction> GetEnumerator() => _items.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
		}
	}

	public sealed class GetIndexerSetupSnapshotBoundaryTests
	{
		[Test]
		public async Task WithMemberIdEqualToTableLength_ShouldReturnNullWithoutThrowing()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			IndexerSetup<string, int> setup = new(registry, (IParameterMatch<int>)It.IsAny<int>());
			registry.SetupIndexer(0, setup);

			IndexerSetup[]? result = registry.GetIndexerSetupSnapshot(1);

			await That(result).IsNull();
		}
	}

	public sealed class GetPropertyFastSnapshotNullSlotTests
	{
		[Test]
		public async Task GetPropertyFast_WhenSnapshotTableHasNullEntryAtMemberId_ShouldFallToColdPath()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> snapshotAtFive = new(registry, "P5");
			snapshotAtFive.InitializeWith(50);
			registry.SetupProperty(5, snapshotAtFive);

			int result = registry.GetPropertyFast(2, new PropertyGetterAccess("P"), _ => 7);

			await That(result).IsEqualTo(7)
				.Because(
					"the snapshot table is long enough for the queried memberId but holds null there, so the `if (snapshot is not null)` guard inside ResolvePropertyFast must skip the body instead of dereferencing it");
		}
	}

	public sealed class SetPropertyFastScenarioRoutingTests
	{
		[Test]
		public async Task SetPropertyFast_WithActiveScenario_ShouldUseColdPathAndWriteToScenarioSetup()
		{
			// Pins the `string.IsNullOrEmpty(Scenario)` scenario guard around the int-keyed
			// snapshot lookup in SetPropertyFast. Negation, `Scenario != null`, and
			// `Scenario != ""` mutations all flip whether the hot path runs when a scenario
			// is active — and would write to the wrong (default-scope) PropertySetup. Verified
			// by reading the value back through GetPropertyFast (which routes to the scenario
			// setup via its cold path) and asserting the new value made it.
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> defaultSetup = new(registry, "P");
			defaultSetup.InitializeWith(100);
			registry.SetupProperty(2, defaultSetup);

			PropertySetup<int> scenarioSetup = new(registry, "P");
			scenarioSetup.InitializeWith(999);
			registry.SetupProperty(2, "myScenario", scenarioSetup);

			registry.TransitionTo("myScenario");
			registry.SetPropertyFast(2, 3, "P", 42);

			int result = registry.GetPropertyFast(2, new PropertyGetterAccess("P"), _ => -1);

			await That(result).IsEqualTo(42);
		}
	}

	public sealed class SetPropertyFastSnapshotInTableOnlyTests
	{
		[Test]
		public async Task SetPropertyFast_WithSnapshotOnlyInIntKeyedTable_ShouldHonorHotPath()
		{
			// Pins the `matchingSetup = table[memberId];` assignment inside SetPropertyFast's
			// hot-path block. With the block emptied, matchingSetup stays null and the cold
			// path runs ResolvePropertySetup, which only consults Setup.Properties. Force the
			// snapshot into the int-keyed table without registering it in Setup.Properties via
			// reflection, then prove the hot path was used: ThrowWhenNotSetup behavior makes
			// the cold path throw, so the test passing means we stayed on the hot path.
			MockBehavior behavior = MockBehavior.Default.ThrowingWhenNotSetup();
			MockRegistry registry = new(behavior, new FastMockInteractions(0, behavior.SkipInteractionRecording));

			PropertySetup<int> snapshot = new(registry, "P");
			snapshot.InitializeWith(0);

			PropertySetup?[] table = new PropertySetup?[6];
			table[5] = snapshot;
			typeof(MockRegistry).GetField(
					"_propertySetupsByMemberId", BindingFlags.NonPublic | BindingFlags.Instance)!
				.SetValue(registry, table);

			void Act()
			{
				registry.SetPropertyFast(5, 6, "P", 42);
			}

			await That(Act).DoesNotThrow();
		}
	}

	public sealed class GetPropertyFastInteractionRecordingTests
	{
		[Test]
		public async Task WithSharedAccessSingleton_AndMatchingFastBuffer_ShouldAppendToBuffer()
		{
			FastMockInteractions store = new(1);
			PropertyGetterAccess access = new("P");
			FastPropertyGetterBuffer buffer = store.GetOrCreateBuffer<FastPropertyGetterBuffer, PropertyGetterAccess>(
				0, static (f, a) => new FastPropertyGetterBuffer(f, a), access);
			MockRegistry registry = new(MockBehavior.Default, store);

			int result = registry.GetPropertyFast(0, access, _ => 7);

			await That(buffer.Count).IsEqualTo(1);
			await That(result).IsEqualTo(7);
		}

		[Test]
		public async Task WithSharedAccessSingleton_WithoutMatchingBuffer_ShouldRegisterTheSingleton()
		{
			FastMockInteractions store = new(0);
			MockRegistry registry = new(MockBehavior.Default, store);
			PropertyGetterAccess access = new("P");

			registry.GetPropertyFast(0, access, _ => 7);

			IInteraction recorded = registry.Interactions.Single();
			await That(recorded).IsSameAs(access);
		}

		[Test]
		public async Task WithStringName_AndMatchingFastBuffer_ShouldAppendToBuffer()
		{
			FastMockInteractions store = new(1);
			PropertyGetterAccess access = new("P");
			FastPropertyGetterBuffer buffer = store.GetOrCreateBuffer<FastPropertyGetterBuffer, PropertyGetterAccess>(
				0, static (f, a) => new FastPropertyGetterBuffer(f, a), access);
			MockRegistry registry = new(MockBehavior.Default, store);

			int result = registry.GetPropertyFast(0, access, _ => 7);

			await That(buffer.Count).IsEqualTo(1);
			await That(result).IsEqualTo(7);
		}

		[Test]
		public async Task WithStringName_AtBufferLengthBoundary_ShouldFallBackWithoutThrowing()
		{
			FastMockInteractions store = new(1);
			MockRegistry registry = new(MockBehavior.Default, store);

			int result = registry.GetPropertyFast(1, new PropertyGetterAccess("P"), _ => 7);

			await That(result).IsEqualTo(7);
			await That(registry.Interactions.Count).IsEqualTo(1);
		}

		[Test]
		public async Task WithStringName_AtTableLengthBoundary_ShouldFallToColdPathWithoutThrowing()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> snapshot = new(registry, "P");
			snapshot.InitializeWith(42);
			registry.SetupProperty(0, snapshot);

			int result = registry.GetPropertyFast(1, new PropertyGetterAccess("Q"), _ => 7);

			await That(result).IsEqualTo(7);
		}

		[Test]
		public async Task WithStringName_WhenBehaviorSkipsInteractionRecording_ShouldNotRecord()
		{
			MockBehavior behavior = MockBehavior.Default.SkippingInteractionRecording();
			FastMockInteractions store = new(1, behavior.SkipInteractionRecording);
			PropertyGetterAccess access = new("P");
			FastPropertyGetterBuffer buffer = store.GetOrCreateBuffer<FastPropertyGetterBuffer, PropertyGetterAccess>(
				0, static (f, a) => new FastPropertyGetterBuffer(f, a), access);
			MockRegistry registry = new(behavior, store);

			registry.GetPropertyFast(0, access, _ => 7);

			await That(buffer.Count).IsEqualTo(0);
			await That(registry.Interactions.Count).IsEqualTo(0);
		}

		[Test]
		public async Task WithStringName_WithoutMatchingBuffer_ShouldFallBackToRegisterPropertyGetterAccess()
		{
			FastMockInteractions store = new(0);
			MockRegistry registry = new(MockBehavior.Default, store);

			registry.GetPropertyFast(0, new PropertyGetterAccess("P"), _ => 7);

			IInteraction recorded = registry.Interactions.Single();
			await That(recorded).IsExactly<PropertyGetterAccess>()
				.Whose(x => x.Name, x => x.IsEqualTo("P"));
		}
	}

	public sealed class SetPropertyMemberIdTests
	{
		[Test]
		public async Task SetProperty_WithMemberId_AndSetupOverridesSkipBaseClass_ShouldReturnSetupOverride()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup = new(registry, "P");
			setup.InitializeWith(0);
			((IPropertySetup<int>)setup).SkippingBaseClass();
			registry.SetupProperty(setup);

			bool skipBase = registry.SetProperty(7, "P", 42);

			await That(skipBase).IsTrue();
		}

		[Test]
		public async Task SetProperty_WithMemberId_AtBufferLengthBoundary_ShouldFallBackWithoutThrowing()
		{
			FastMockInteractions store = new(1);
			MockRegistry registry = new(MockBehavior.Default, store);
			PropertySetup<int> setup = new(registry, "P");
			setup.InitializeWith(0);
			registry.SetupProperty(setup);

			registry.SetProperty(1, "P", 42);

			IInteraction recorded = registry.Interactions.Single();
			await That(recorded).IsExactly<PropertySetterAccess<int>>();
		}

		[Test]
		public async Task SetProperty_WithMemberId_ShouldInvokeSetterAndStoreValue()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup = new(registry, "P");
			setup.InitializeWith(0);
			registry.SetupProperty(setup);

			registry.SetProperty(7, "P", 42);

			int after = registry.GetProperty("P", () => -1, null);
			await That(after).IsEqualTo(42);
		}

		[Test]
		public async Task SetProperty_WithoutMemberId_AndSetupOverridesSkipBaseClass_ShouldReturnSetupOverride()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup = new(registry, "P");
			setup.InitializeWith(0);
			((IPropertySetup<int>)setup).SkippingBaseClass();
			registry.SetupProperty(setup);

			bool skipBase = registry.SetProperty("P", 42);

			await That(skipBase).IsTrue();
		}
	}

	public sealed class SetPropertyFastTableBoundaryTests
	{
		[Test]
		public async Task SetPropertyFast_AtTableLengthBoundary_ShouldFallToColdPathWithoutThrowing()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> snapshot = new(registry, "P");
			snapshot.InitializeWith(0);
			registry.SetupProperty(0, snapshot);

			bool skipBase = registry.SetPropertyFast(1, 2, "P", 42);

			await That(skipBase).IsFalse();
		}
	}

	public sealed class AddRemoveEventErrorMessageTests
	{
		[Test]
		public async Task AddEvent_WithMemberId_AndNullMethod_ShouldThrowSubscriptionMessage()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			void Act()
			{
				registry.AddEvent(0, "OnFoo", this, null);
			}

			await That(Act).Throws<MockException>()
				.WithMessage("The method of an event subscription may not be null.");
		}

		[Test]
		public async Task AddEvent_WithStringName_AndNullMethod_ShouldThrowSubscriptionMessage()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			void Act()
			{
				registry.AddEvent("OnFoo", this, null);
			}

			await That(Act).Throws<MockException>()
				.WithMessage("The method of an event subscription may not be null.");
		}

		[Test]
		public async Task RemoveEvent_WithMemberId_AndNullMethod_ShouldThrowUnsubscriptionMessage()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			void Act()
			{
				registry.RemoveEvent(0, "OnFoo", this, null);
			}

			await That(Act).Throws<MockException>()
				.WithMessage("The method of an event unsubscription may not be null.");
		}

		[Test]
		public async Task RemoveEvent_WithStringName_AndNullMethod_ShouldThrowUnsubscriptionMessage()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			void Act()
			{
				registry.RemoveEvent("OnFoo", this, null);
			}

			await That(Act).Throws<MockException>()
				.WithMessage("The method of an event unsubscription may not be null.");
		}
	}

	public sealed class RemoveEventScenarioRoutingTests
	{
		[Test]
		public async Task AddEvent_WithMemberId_AndActiveScenario_ShouldRouteToScenarioSetup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			EventSetup defaultSetup = new(registry, "OnFoo");
			registry.SetupEvent(7, defaultSetup);

			int scenarioCallbackCount = 0;
			EventSetup scenarioSetup = new(registry, "OnFoo");
			scenarioSetup.OnSubscribed.Do(() => scenarioCallbackCount++);
			registry.SetupEvent(7, "myScenario", scenarioSetup);

			registry.TransitionTo("myScenario");
			registry.AddEvent(7, "OnFoo", this, GetMethodInfo());

			await That(scenarioCallbackCount).IsEqualTo(1);
		}

		[Test]
		public async Task RemoveEvent_WithMemberId_AndActiveScenario_ShouldRouteToScenarioSetup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			EventSetup defaultSetup = new(registry, "OnFoo");
			registry.SetupEvent(7, defaultSetup);

			int scenarioCallbackCount = 0;
			EventSetup scenarioSetup = new(registry, "OnFoo");
			scenarioSetup.OnUnsubscribed.Do(() => scenarioCallbackCount++);
			registry.SetupEvent(7, "myScenario", scenarioSetup);

			registry.TransitionTo("myScenario");
			registry.RemoveEvent(7, "OnFoo", this, GetMethodInfo());

			await That(scenarioCallbackCount).IsEqualTo(1);
		}
	}
}
