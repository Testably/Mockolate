namespace Mockolate.TestHelpers;

public delegate void ChocolateDispensedDelegate(string type, int amount);

public interface IChocolateDispenser
{
	int this[string type] { get; set; }
	int TotalDispensed { get; set; }
	bool Dispense(string type, int amount);
	event ChocolateDispensedDelegate ChocolateDispensed;
}
