namespace Mockolate.Internal.Tests.Registry;

public sealed class AutoMocksTests
{
	[Test]
	public async Task AutoMockOr_WhenNoMockIsGenerated_ShouldReturnNullOnEveryCall()
	{
		MockRegistry registry = new(MockBehavior.Default.AutoMocking(), new FastMockInteractions(0));

		IServiceWithoutMock? first = registry.AutoMockOr<IServiceWithoutMock?>(0, null);
		IServiceWithoutMock? second = registry.AutoMockOr<IServiceWithoutMock?>(0, null);

		await That(first).IsNull().Because("no CreateMock call makes the generator create a mock of the interface");
		await That(second).IsNull();
	}

	[Test]
	public async Task Or_WhenTheTypeIsNoInterface_ShouldReturnTheValue()
	{
		object? result = AutoMocks.Or<object?>(MockBehavior.Default.AutoMocking(), null);

		await That(result).IsNull();
	}

	public interface IServiceWithoutMock;
}
