using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Registry;

public sealed class MockRegistryIndexerTests
{
	[Test]
	public async Task FindIndexerGetterSetup_WhenSetupIsRegisteredInActiveScenario_ShouldReturnSetup()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		IndexerSetup<string, int> setup = new(registry, CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()));
		registry.SetupIndexer(0, "a", setup);
		registry.TransitionTo("a");

		IndexerSetup<string, int>? result = registry.FindIndexerGetterSetup<string, int>(0, 1);

		await That(result).IsSameAs(setup);
	}

	[Test]
	public async Task FindIndexerGetterSetup_WhenSetupIsRegisteredUnderAnotherMemberId_ShouldReturnSetup()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		IndexerSetup<string, int> setup = new(registry, CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()));
		registry.SetupIndexer(1, setup);

		IndexerSetup<string, int>? result = registry.FindIndexerGetterSetup<string, int>(0, 1);

		await That(result).IsSameAs(setup);
	}

	[Test]
	public async Task FindIndexerGetterSetup_WhenSetupIsRegisteredWithoutMemberId_ShouldReturnSetup()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		IndexerSetup<string, int> setup = new(registry, CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()));
		registry.SetupIndexer(setup);

		IndexerSetup<string, int>? result = registry.FindIndexerGetterSetup<string, int>(0, 1);

		await That(result).IsSameAs(setup);
	}

	[Test]
	public async Task FindIndexerGetterSetup_WhenSetupsAreOnlyRegisteredUnderTheMemberId_ShouldReturnNull()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		registry.SetupIndexer(0,
			new IndexerSetup<string, int>(registry, CovariantParameterAdapter<int>.Wrap(It.IsAny<int>())));

		IndexerSetup<string, int>? result = registry.FindIndexerGetterSetup<string, int>(0, 1);

		await That(result).IsNull()
			.Because("the generated code has already checked the setups of the member id");
	}

	[Test]
	public async Task FindIndexerSetterSetup_WhenSetupIsRegisteredUnderAnotherMemberId_ShouldReturnSetup()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		IndexerSetup<string, int> setup = new(registry, CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()));
		registry.SetupIndexer(1, setup);

		IndexerSetup<string, int>? result = registry.FindIndexerSetterSetup<string, int>(0, 1, "foo");

		await That(result).IsSameAs(setup);
	}

	[Test]
	public async Task FindIndexerSetterSetup_WithoutAnySetup_ShouldReturnNull()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

		IndexerSetup<string, int>? result = registry.FindIndexerSetterSetup<string, int>(0, 1, "foo");

		await That(result).IsNull();
	}

	[Test]
	public async Task GetIndexer_WithBaseValue_WithoutSetup_ShouldStoreBaseValue()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

		string first = registry.GetIndexer<string, int>(null, 1, "foo", 0);
		string second = registry.GetIndexer<string, int>(null, 1, "bar", 0);

		await That(first).IsEqualTo("foo");
		await That(second).IsEqualTo("foo").Because("the first base value was stored");
	}

	[Test]
	public async Task GetIndexer_WithoutSetup_WhenThrowingWhenNotSetup_ShouldThrow()
	{
		MockBehavior behavior = MockBehavior.Default.ThrowingWhenNotSetup();
		MockRegistry registry = new(behavior, new FastMockInteractions(0, behavior.SkipInteractionRecording));

		void Act()
		{
			registry.GetIndexer<string, int>(null, 1, static _ => "", 0);
		}

		await That(Act).Throws<MockNotSetupException>()
			.WithMessage("get indexer [1] was accessed without prior setup.");
	}

	[Test]
	public async Task SetIndexer_WithoutSetup_ShouldStoreValueForGetter()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

		registry.SetIndexer<string, int>(null, 1, "foo", 0);
		string result = registry.GetIndexer<string, int>(null, 1, static _ => "", 0);

		await That(result).IsEqualTo("foo");
	}
	[Test]
	public async Task GetIndexer_WithDerivedSetupWith1Parameters_ShouldUseOverriddenGetResult()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		DerivedSetup1 setup = new(registry);

		string result = registry.GetIndexer<string, int>(setup, 1, static _ => "", 0);

		await That(result).IsEqualTo("derived");
	}

	[Test]
	public async Task GetIndexer_WithBaseValue_WithDerivedSetupWith1Parameters_ShouldUseOverriddenGetResult()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		DerivedSetup1 setup = new(registry);

		string result = registry.GetIndexer<string, int>(setup, 1, "base", 0);

		await That(result).IsEqualTo("derived base");
	}

	[Test]
	public async Task SetIndexer_WithDerivedSetupWith1Parameters_ShouldUseOverriddenSetResult()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		DerivedSetup1 setup = new(registry);

		registry.SetIndexer<string, int>(setup, 1, "foo", 0);

		await That(setup.LastValue).IsEqualTo("foo");
	}

	[Test]
	public async Task GetIndexer_WithDerivedSetupWith2Parameters_ShouldUseOverriddenGetResult()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		DerivedSetup2 setup = new(registry);

		string result = registry.GetIndexer<string, int, int>(setup, 1, 2, static _ => "", 0);

		await That(result).IsEqualTo("derived");
	}

	[Test]
	public async Task GetIndexer_WithBaseValue_WithDerivedSetupWith2Parameters_ShouldUseOverriddenGetResult()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		DerivedSetup2 setup = new(registry);

		string result = registry.GetIndexer<string, int, int>(setup, 1, 2, "base", 0);

		await That(result).IsEqualTo("derived base");
	}

	[Test]
	public async Task SetIndexer_WithDerivedSetupWith2Parameters_ShouldUseOverriddenSetResult()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		DerivedSetup2 setup = new(registry);

		registry.SetIndexer<string, int, int>(setup, 1, 2, "foo", 0);

		await That(setup.LastValue).IsEqualTo("foo");
	}

	[Test]
	public async Task GetIndexer_WithDerivedSetupWith3Parameters_ShouldUseOverriddenGetResult()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		DerivedSetup3 setup = new(registry);

		string result = registry.GetIndexer<string, int, int, int>(setup, 1, 2, 3, static _ => "", 0);

		await That(result).IsEqualTo("derived");
	}

	[Test]
	public async Task GetIndexer_WithBaseValue_WithDerivedSetupWith3Parameters_ShouldUseOverriddenGetResult()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		DerivedSetup3 setup = new(registry);

		string result = registry.GetIndexer<string, int, int, int>(setup, 1, 2, 3, "base", 0);

		await That(result).IsEqualTo("derived base");
	}

	[Test]
	public async Task SetIndexer_WithDerivedSetupWith3Parameters_ShouldUseOverriddenSetResult()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		DerivedSetup3 setup = new(registry);

		registry.SetIndexer<string, int, int, int>(setup, 1, 2, 3, "foo", 0);

		await That(setup.LastValue).IsEqualTo("foo");
	}

	[Test]
	public async Task GetIndexer_WithDerivedSetupWith4Parameters_ShouldUseOverriddenGetResult()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		DerivedSetup4 setup = new(registry);

		string result = registry.GetIndexer<string, int, int, int, int>(setup, 1, 2, 3, 4, static _ => "", 0);

		await That(result).IsEqualTo("derived");
	}

	[Test]
	public async Task GetIndexer_WithBaseValue_WithDerivedSetupWith4Parameters_ShouldUseOverriddenGetResult()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		DerivedSetup4 setup = new(registry);

		string result = registry.GetIndexer<string, int, int, int, int>(setup, 1, 2, 3, 4, "base", 0);

		await That(result).IsEqualTo("derived base");
	}

	[Test]
	public async Task SetIndexer_WithDerivedSetupWith4Parameters_ShouldUseOverriddenSetResult()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		DerivedSetup4 setup = new(registry);

		registry.SetIndexer<string, int, int, int, int>(setup, 1, 2, 3, 4, "foo", 0);

		await That(setup.LastValue).IsEqualTo("foo");
	}

	private sealed class DerivedSetup1(MockRegistry registry)
		: IndexerSetup<string, int>(registry,
			CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()))
	{
		public object? LastValue { get; private set; }

		public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
			Func<MockBehavior, TResult> defaultValueGenerator)
			=> (TResult)(object)"derived";

		public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior, TResult baseValue)
			=> (TResult)(object)("derived " + baseValue);

		public override void SetResult<TResult>(IndexerAccess access, MockBehavior behavior, TResult value)
			=> LastValue = value;
	}

	private sealed class DerivedSetup2(MockRegistry registry)
		: IndexerSetup<string, int, int>(registry,
			CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()),
			CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()))
	{
		public object? LastValue { get; private set; }

		public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
			Func<MockBehavior, TResult> defaultValueGenerator)
			=> (TResult)(object)"derived";

		public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior, TResult baseValue)
			=> (TResult)(object)("derived " + baseValue);

		public override void SetResult<TResult>(IndexerAccess access, MockBehavior behavior, TResult value)
			=> LastValue = value;
	}

	private sealed class DerivedSetup3(MockRegistry registry)
		: IndexerSetup<string, int, int, int>(registry,
			CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()),
			CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()),
			CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()))
	{
		public object? LastValue { get; private set; }

		public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
			Func<MockBehavior, TResult> defaultValueGenerator)
			=> (TResult)(object)"derived";

		public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior, TResult baseValue)
			=> (TResult)(object)("derived " + baseValue);

		public override void SetResult<TResult>(IndexerAccess access, MockBehavior behavior, TResult value)
			=> LastValue = value;
	}

	private sealed class DerivedSetup4(MockRegistry registry)
		: IndexerSetup<string, int, int, int, int>(registry,
			CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()),
			CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()),
			CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()),
			CovariantParameterAdapter<int>.Wrap(It.IsAny<int>()))
	{
		public object? LastValue { get; private set; }

		public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
			Func<MockBehavior, TResult> defaultValueGenerator)
			=> (TResult)(object)"derived";

		public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior, TResult baseValue)
			=> (TResult)(object)("derived " + baseValue);

		public override void SetResult<TResult>(IndexerAccess access, MockBehavior behavior, TResult value)
			=> LastValue = value;
	}
}
