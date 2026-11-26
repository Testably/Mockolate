using System.Collections.Generic;

namespace Mockolate.Frameworks.TestHelpers.Domain;

public sealed class InMemoryOrderStore : IOrderRepository, IShippingClient
{
	public List<Order> Orders { get; } = [];
	public List<Order> Shipped { get; } = [];

	public Order? Find(int id)
		=> Orders.Find(order => order.Id == id);

	public void Ship(Order order)
		=> Shipped.Add(order);
}
