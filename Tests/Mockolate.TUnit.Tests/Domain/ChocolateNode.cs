namespace Mockolate.TUnit.Tests.Domain;

public class ChocolateNode(ChocolateNode? parent = null)
{
	public ChocolateNode? Parent { get; } = parent;
}
