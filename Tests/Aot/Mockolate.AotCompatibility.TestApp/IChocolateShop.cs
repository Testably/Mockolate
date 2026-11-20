namespace Mockolate.AotCompatibility.TestApp;

public interface IChocolateShop
{
	IChocolateDispenser this[string name] { get; }
	IChocolateDispenser GetDispenser();
}
