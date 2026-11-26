using System.Threading.Tasks;

namespace Mockolate.Frameworks.TestHelpers.Domain;

public interface IWarehouse
{
	IOrderRepository Repository { get; }
	IOrderRepository this[string name] { get; set; }
	IOrderRepository GetRepository();
	Task<IOrderRepository> GetRepositoryAsync();
	(int Stock, IOrderRepository Repository) GetStock();
	bool TryGetRepository(out IOrderRepository repository);
}
