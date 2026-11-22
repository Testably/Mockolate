namespace Mockolate.TUnit.Tests.Domain;

public class ClashingNamesService(IOrderRepository mocks, IShippingClient mockBehavior, ITaxCalculator equals)
{
	public IOrderRepository Repository { get; } = mocks;
	public IShippingClient ShippingClient { get; } = mockBehavior;
	public ITaxCalculator TaxCalculator { get; } = equals;
}
