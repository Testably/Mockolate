namespace Mockolate.Internal.Tests.Registry;

public sealed class AutoMocksTests
{
	[Test]
	public async Task AutoMockOr_WhenTheFactoryHasNoMock_ShouldReturnNullAndAskOnlyOnce()
	{
		int calls = 0;
		MockBehavior behavior = AutoMocks.UseFactory(MockBehavior.Default, (_, _) =>
		{
			calls++;
			return null;
		});
		MockRegistry registry = new(behavior, new MockInteractions(0));

		IServiceWithoutMock? first = registry.AutoMockOr<IServiceWithoutMock?>(0, null);
		IServiceWithoutMock? second = registry.AutoMockOr<IServiceWithoutMock?>(0, null);

		await That(first).IsNull();
		await That(second).IsNull();
		await That(calls).IsEqualTo(1).Because("a missing auto-mock is remembered per member");
	}

	[Test]
	public async Task AutoMockOr_WithTheSameMemberIdForAnotherType_ShouldReturnAMockOfThatType()
	{
		MockBehavior behavior = AutoMocks.UseFactory(MockBehavior.Default, (type, _) =>
			type == typeof(IServiceWithoutMock) ? new ServiceWithoutMock() : new OtherService());
		MockRegistry registry = new(behavior, new MockInteractions(0));

		IServiceWithoutMock? first = registry.AutoMockOr<IServiceWithoutMock?>(0, null);
		IOtherService? second = registry.AutoMockOr<IOtherService?>(0, null);

		await That(first).IsExactly<ServiceWithoutMock>();
		await That(second).IsExactly<OtherService>()
			.Because("mocks that share their setups number their members independently");
	}

	[Test]
	public async Task AutoMockOr_WithoutFactory_ShouldReturnNull()
	{
		MockRegistry registry = new(MockBehavior.Default, new MockInteractions(0));

		IServiceWithoutMock? result = registry.AutoMockOr<IServiceWithoutMock?>(0, null);

		await That(result).IsNull()
			.Because("with the MockolateSkipAutoMocking build property no CreateMock sets a factory");
	}

	[Test]
	public async Task Or_WhenTheFactoryReturnsAnotherType_ShouldReturnTheValue()
	{
		MockBehavior behavior = AutoMocks.UseFactory(MockBehavior.Default, (_, _) => new object());

		IServiceWithoutMock? result = AutoMocks.Or<IServiceWithoutMock?>(behavior, null);

		await That(result).IsNull();
	}

	[Test]
	public async Task UseFactory_WhenSkippingAutoMocking_ShouldReturnTheSameBehavior()
	{
		MockBehavior behavior = MockBehavior.Default.SkippingAutoMocking();

		MockBehavior result = AutoMocks.UseFactory(behavior, (_, _) => null);

		await That(result).IsSameAs(behavior);
	}

	[Test]
	public async Task UseFactory_WithoutFactory_ShouldRemoveTheFactoryOfAnotherAssembly()
	{
		MockBehavior behavior = AutoMocks.UseFactory(MockBehavior.Default, (_, _) => new ServiceWithoutMock());

		MockBehavior result = AutoMocks.UseFactory(behavior, null);

		await That(AutoMocks.Or<IServiceWithoutMock?>(result, null)).IsNull()
			.Because("a project with the MockolateSkipAutoMocking build property creates no auto-mocks");
	}

	[Test]
	public async Task UseFactory_WithoutFactory_WhenTheBehaviorHasNone_ShouldReturnTheSameBehavior()
	{
		MockBehavior behavior = MockBehavior.Default;

		MockBehavior result = AutoMocks.UseFactory(behavior, null);

		await That(result).IsSameAs(behavior);
	}

	[Test]
	public async Task UseFactory_WithTheSameFactory_ShouldReturnTheSameBehavior()
	{
		Func<Type, MockBehavior, object?> factory = (_, _) => null;
		MockBehavior behavior = AutoMocks.UseFactory(MockBehavior.Default, factory);

		MockBehavior result = AutoMocks.UseFactory(behavior, factory);

		await That(result).IsSameAs(behavior).Because("creating a mock must not allocate a new behavior every time");
	}

	public interface IServiceWithoutMock;

	public interface IOtherService;

	private sealed class ServiceWithoutMock : IServiceWithoutMock;

	private sealed class OtherService : IOtherService;
}
