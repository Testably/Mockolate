namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Implements <see cref="IGenericMethodAnyTypeArgumentImplemented" /> with another name for the type parameter, so a
///     combination mock serves the interface method with the method of the class.
/// </summary>
public class GenericMethodAnyTypeArgumentImplementor : IGenericMethodAnyTypeArgumentImplemented
{
	public virtual void Generic<TOther>(TOther value)
	{
	}
}
