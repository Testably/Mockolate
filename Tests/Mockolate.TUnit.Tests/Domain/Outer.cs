namespace Mockolate.TUnit.Tests.Domain;

public static class Outer
{
	public class NestedService(IOrderRepository repository)
	{
		public IOrderRepository Repository { get; } = repository;
	}
}
