namespace Mockolate.XUnit.Tests.Domain;

public class PricingService(ITaxCalculator taxCalculator, string currency)
{
	public string GetPrice(Order order)
		=> $"{order.Amount + taxCalculator.GetTax(order.Amount)} {currency}";
}
