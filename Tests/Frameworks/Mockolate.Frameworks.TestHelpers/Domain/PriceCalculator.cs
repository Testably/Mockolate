namespace Mockolate.Frameworks.TestHelpers.Domain;

public class PriceCalculator(DiscountRule rule, ITaxCalculator taxCalculator)
{
	public DiscountRule Rule { get; } = rule;

	public decimal GetPrice(decimal amount)
	{
		decimal discounted = amount * (100m - Rule.Percentage) / 100m;
		return discounted + taxCalculator.GetTax(discounted);
	}
}
