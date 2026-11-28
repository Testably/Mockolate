namespace Mockolate.Tests.GeneratorCoverage;

public abstract class MethodConstrainedToClassTypeArgumentBase<T>
{
	public abstract void Accept<U>(U? value) where U : class, T;
}
