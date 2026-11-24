using Mockolate.Integrations.Xunit;
using Mockolate.TUnit.Tests.Domain;
using Mockolate.Verify;
using Xunit;

namespace Mockolate.Xunit2.Tests;

/// <summary>
///     Theories that <see cref="XunitRunnerTests" /> runs with the xUnit v2 runner. The project references no assertion
///     library of xUnit, so a failing check throws.
/// </summary>
public sealed class XunitTheories
{
	[Theory]
	[AutoMockArguments]
	public void SharedMocks(OrderService sut, IOrderRepository repository)
	{
		repository.Mock.Setup.Find(42).Returns(new Order(42, 10m));

		Check(sut.Ship(42), "The class under test did not use the mock.");
	}

	[Theory]
	[AutoMockArguments(1)]
	[AutoMockArguments(2)]
	public void SeveralAttributes(int id, OrderService sut, IOrderRepository repository)
	{
		sut.Ship(id);

		repository.Mock.Verify.Find(id).Once();
	}

	[Theory]
	[AutoMockArguments(null)]
	public void SingleNullArgument(string? name, OptionalArgumentsService sut)
		=> Check(name is null && sut.Name is null, "The single null argument was not passed.");

	[Theory]
	[AutoMockArguments]
	public void TransientParameter(OrderService sut, IOrderRepository repository,
		[Transient] IShippingClient shippingClient)
	{
		Order order = new(42, 10m);
		repository.Mock.Setup.Find(42).Returns(order);

		sut.Ship(42);

		shippingClient.Mock.Verify.Ship(order).Never();
	}

	[Theory]
	[AutoMockArguments]
	public void DisplayName_ShouldNotReadTheProperties(IChocolateStock stock)
		=> Check(stock.Mock.VerifyThatAllInteractionsAreVerified(), "Formatting the display name used the mock.");

	private static void Check(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
