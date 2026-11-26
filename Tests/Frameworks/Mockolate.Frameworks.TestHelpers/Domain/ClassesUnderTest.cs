namespace Mockolate.Frameworks.TestHelpers.Domain;

public class DiscountService(ITaxCalculator taxCalculator)
{
	public virtual decimal GetDiscount() => 10m;

	public decimal GetTotal(decimal amount)
		=> amount + taxCalculator.GetTax(amount) - GetDiscount();
}

public sealed class StockIndicator(IChocolateStock stock)
{
	public bool IsAvailable { get; } = stock.Count > 0;
	public IChocolateStock Stock { get; } = stock;
}

public sealed class SealedOrderService(IOrderRepository repository)
{
	public IOrderRepository Repository { get; } = repository;
}

public record OrderRecord
{
	public OrderRecord(IOrderRepository repository, string name)
	{
		Repository = repository;
		Name = name;
	}

	public IOrderRepository Repository { get; }
	public string Name { get; }
}
