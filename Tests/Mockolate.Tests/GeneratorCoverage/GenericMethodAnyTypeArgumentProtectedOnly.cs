namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     A class whose only generic method is protected, so it has no <c>SetupGeneric</c>.
/// </summary>
public class GenericMethodAnyTypeArgumentProtectedOnly
{
	public int CallProtected(string value) => Protected(value);

	protected virtual int Protected<T>(T value) => 42;
}
