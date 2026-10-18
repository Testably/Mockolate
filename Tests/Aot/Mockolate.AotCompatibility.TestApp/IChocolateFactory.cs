namespace Mockolate.AotCompatibility.TestApp;

public interface IChocolateFactory
{
	static abstract int ProduceBatch(string type, int amount);
	static abstract event Action<int> BatchCompleted;
}
