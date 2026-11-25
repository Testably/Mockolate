namespace Mockolate.Frameworks.TestHelpers.Domain;

public class Order(int id, decimal amount)
{
	public int Id { get; } = id;
	public decimal Amount { get; } = amount;
}
