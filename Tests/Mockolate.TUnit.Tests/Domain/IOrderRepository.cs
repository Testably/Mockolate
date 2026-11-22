namespace Mockolate.TUnit.Tests.Domain;

public interface IOrderRepository
{
	Order? Find(int id);
}
