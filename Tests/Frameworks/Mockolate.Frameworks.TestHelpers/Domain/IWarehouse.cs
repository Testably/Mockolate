using System.Threading.Tasks;

namespace Mockolate.Frameworks.TestHelpers.Domain;

public interface IWarehouse
{
	IOrderRepository Repository { get; }
	IOrderRepository this[string name] { get; set; }
	IOrderRepository GetRepository();
	Task<IOrderRepository> GetRepositoryAsync();
	bool TryGetRepository(out IOrderRepository repository);
}
