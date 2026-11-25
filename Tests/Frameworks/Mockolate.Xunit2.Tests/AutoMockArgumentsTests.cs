using System.Linq;
using System.Reflection;
using Mockolate.Integrations.Xunit;
using Mockolate.Frameworks.TestHelpers.Domain;

namespace Mockolate.Xunit2.Tests;

public sealed class AutoMockArgumentsTests
{
	[Test]
	public async Task GetData_ShouldPassTheParameterMocksToTheClassUnderTest()
	{
		object[] row = GetSingleRow(nameof(SharedMocks));
		OrderService sut = (OrderService)row[0];
		IOrderRepository repository = (IOrderRepository)row[1];
		repository.Mock.Setup.Find(42).Returns(new Order(42, 10m));

		sut.Ship(42);

		await That(repository.Mock.Verify.Find(42)).Once();
	}

	[Test]
	public async Task GetData_WithArgument_ShouldPassItToTheFirstParameter()
	{
		object[] row = GetSingleRow(nameof(WithArgument));

		await That(row[0]).IsEqualTo(7);
		await That(row[1]).Is<IOrderRepository>();
	}

	[Test]
	public async Task GetData_WithTransientParameter_ShouldNotShareIt()
	{
		object[] row = GetSingleRow(nameof(Transient));
		OrderService sut = (OrderService)row[0];
		IOrderRepository repository = (IOrderRepository)row[1];
		IShippingClient shippingClient = (IShippingClient)row[2];
		Order order = new(42, 10m);
		repository.Mock.Setup.Find(42).Returns(order);

		sut.Ship(42);

		await That(shippingClient.Mock.Verify.Ship(order)).Never();
	}

	private static object[] GetSingleRow(string methodName)
	{
		MethodInfo method = typeof(AutoMockArgumentsTests).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!;
		return method.GetCustomAttribute<AutoMockArgumentsAttribute>()!.GetData(method).Single();
	}

	[AutoMockArguments]
	private static void SharedMocks(OrderService sut, IOrderRepository repository, IShippingClient shippingClient)
	{
	}

	[AutoMockArguments(7)]
	private static void WithArgument(int id, IOrderRepository repository)
	{
	}

	[AutoMockArguments]
	private static void Transient(OrderService sut, IOrderRepository repository,
		[Transient] IShippingClient shippingClient)
	{
	}
}
