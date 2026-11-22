namespace Mockolate.XUnit.Tests.Domain;

public interface IOrderRepository
{
	Order? Find(int id);
}
