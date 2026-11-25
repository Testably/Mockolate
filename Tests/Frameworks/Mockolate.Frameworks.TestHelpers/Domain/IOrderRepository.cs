namespace Mockolate.Frameworks.TestHelpers.Domain;

public interface IOrderRepository
{
	Order? Find(int id);
}
