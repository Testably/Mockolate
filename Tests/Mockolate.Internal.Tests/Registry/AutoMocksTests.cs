// These tests hand the factory to the behavior themselves, so they need no MockolateAutoMocking build property.
#pragma warning disable Mockolate0005

namespace Mockolate.Internal.Tests.Registry;

public sealed class AutoMocksTests
{
	[Test]
	public async Task AutoMockOr_WhenTheFactoryHasNoMock_ShouldReturnNullAndAskOnlyOnce()
	{
		int calls = 0;
		MockBehavior behavior = AutoMocks.UseFactory(MockBehavior.Default.AutoMocking(), (_, _) =>
		{
			calls++;
			return null;
		});
		MockRegistry registry = new(behavior, new FastMockInteractions(0));

		IServiceWithoutMock? first = registry.AutoMockOr<IServiceWithoutMock?>(0, null);
		IServiceWithoutMock? second = registry.AutoMockOr<IServiceWithoutMock?>(0, null);

		await That(first).IsNull();
		await That(second).IsNull();
		await That(calls).IsEqualTo(1).Because("a missing auto-mock is remembered per member");
	}

	[Test]
	public async Task AutoMockOr_WithoutFactory_ShouldReturnNull()
	{
		MockRegistry registry = new(MockBehavior.Default.AutoMocking(), new FastMockInteractions(0));

		IServiceWithoutMock? result = registry.AutoMockOr<IServiceWithoutMock?>(0, null);

		await That(result).IsNull()
			.Because("without the MockolateAutoMocking build property no CreateMock sets a factory");
	}

	[Test]
	public async Task Or_WhenTheFactoryReturnsAnotherType_ShouldReturnTheValue()
	{
		MockBehavior behavior = AutoMocks.UseFactory(MockBehavior.Default.AutoMocking(), (_, _) => new object());

		IServiceWithoutMock? result = AutoMocks.Or<IServiceWithoutMock?>(behavior, null);

		await That(result).IsNull();
	}

	[Test]
	public async Task UseFactory_WhenAutoMockingIsDisabled_ShouldReturnTheSameBehavior()
	{
		MockBehavior behavior = MockBehavior.Default;

		MockBehavior result = AutoMocks.UseFactory(behavior, (_, _) => null);

		await That(result).IsSameAs(behavior);
	}

	[Test]
	public async Task UseFactory_WithTheSameFactory_ShouldReturnTheSameBehavior()
	{
		Func<Type, MockBehavior, object?> factory = (_, _) => null;
		MockBehavior behavior = AutoMocks.UseFactory(MockBehavior.Default.AutoMocking(), factory);

		MockBehavior result = AutoMocks.UseFactory(behavior, factory);

		await That(result).IsSameAs(behavior).Because("creating a mock must not allocate a new behavior every time");
	}

	public interface IServiceWithoutMock;
}
