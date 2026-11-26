namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     A virtual generic method with one parameter, whose base implementation <c>SetupGeneric</c> calls unless skipped.
/// </summary>
public class GenericMethodAnyTypeArgumentBase
{
	public virtual int Virtual<T>(T value) => 42;
}
