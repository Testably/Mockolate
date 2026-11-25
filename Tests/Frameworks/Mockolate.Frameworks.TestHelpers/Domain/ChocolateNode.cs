namespace Mockolate.Frameworks.TestHelpers.Domain;

public class ChocolateNode(ChocolateNode? parent = null)
{
	public ChocolateNode? Parent { get; } = parent;
}
