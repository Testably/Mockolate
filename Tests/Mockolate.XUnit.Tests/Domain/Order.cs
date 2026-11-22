namespace Mockolate.XUnit.Tests.Domain;

public class Order(int id, decimal amount)
{
	public int Id { get; } = id;
	public decimal Amount { get; } = amount;
}
