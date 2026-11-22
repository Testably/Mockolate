namespace Mockolate.AotCompatibility.TestApp;

public class ChocolateVendor(IChocolateDispenser dispenser, string brand = "Testably")
{
	public string Buy(string type, int amount)
		=> dispenser.Dispense(type, amount) ? $"Enjoy your {brand} {type}" : "Sold out";
}
