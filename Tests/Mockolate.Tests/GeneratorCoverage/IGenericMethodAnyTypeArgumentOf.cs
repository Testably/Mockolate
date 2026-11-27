namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     A generic method on a generic interface, whose closed name contains the type arguments of the interface before
///     those of the method, next to a non-generic method with one parameter.
/// </summary>
public interface IGenericMethodAnyTypeArgumentOf<TItem>
{
	void Plain<T>(T value);
	void Other(TItem item);
}
