namespace Mockolate.AotCompatibility.TestApp;

public interface IChocolateDispenser
{
	int Stock { get; set; }
	int this[string type] { get; set; }
	bool Dispense(string type, int amount);
	bool Temper(double temperature);
	Task<int> CountAsync(string type);
	ValueTask<bool> IsAvailableAsync(string type);
	void Refill(ref int amount, out bool overflow);
	event EventHandler<int> Dispensed;
}
