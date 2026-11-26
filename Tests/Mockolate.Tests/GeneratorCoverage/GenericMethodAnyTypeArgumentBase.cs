namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     A virtual generic method with one parameter, whose base implementation <c>SetupGeneric</c> calls unless skipped,
///     and a protected one for <c>SetupProtectedGeneric</c>.
/// </summary>
public class GenericMethodAnyTypeArgumentBase
{
	public virtual int Virtual<T>(T value) => 42;

	public int CallProtected<T>(T value) => Protected(value);

	protected virtual int Protected<T>(T value) => 42;
}
