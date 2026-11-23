namespace Mockolate.TUnit.Tests.Domain;

public class OptionalArgumentsService(
	IOrderRepository repository,
	string? name,
	int retries = 3,
	decimal factor = 1.5m,
	params string[] tags)
{
	public IOrderRepository Repository { get; } = repository;
	public string? Name { get; } = name;
	public int Retries { get; } = retries;
	public decimal Factor { get; } = factor;
	public string[] Tags { get; } = tags;
}
