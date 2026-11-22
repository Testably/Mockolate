namespace Mockolate.TUnit.Tests.Domain;

public class PascalCaseService(IOrderRepository Repository)
{
	public IOrderRepository Repository { get; } = Repository;
}
