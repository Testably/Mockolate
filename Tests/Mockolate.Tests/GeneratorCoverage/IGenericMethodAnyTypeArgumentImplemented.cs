namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     A generic method that <see cref="GenericMethodAnyTypeArgumentImplementor" /> implements with another name for the
///     type parameter.
/// </summary>
public interface IGenericMethodAnyTypeArgumentImplemented
{
	void Generic<T>(T value);
}
