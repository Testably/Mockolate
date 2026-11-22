namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     An overridable generic method with a <c>params</c> array of its type parameter, which takes matchers on a class
///     as on an interface.
/// </summary>
public abstract class GenericParamsMethodBase
{
	public abstract int Only<T>(params T[] values);
}
