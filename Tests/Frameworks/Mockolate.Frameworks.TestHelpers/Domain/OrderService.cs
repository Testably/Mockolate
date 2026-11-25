namespace Mockolate.Frameworks.TestHelpers.Domain;

public class OrderService(IOrderRepository repository, IShippingClient shippingClient)
{
	public bool Ship(int orderId)
	{
		if (repository.Find(orderId) is not { } order)
		{
			return false;
		}

		shippingClient.Ship(order);
		return true;
	}
}
