namespace Mockolate.AotCompatibility.TestApp;

public abstract class ChocolateMachine(string brand)
{
	public virtual string Describe() => $"A machine by {brand}";
	public int Dispense(string type) => DispenseInternal(type);
	protected abstract int DispenseInternal(string type);
}
