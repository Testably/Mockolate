namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Declares <see cref="object.Equals(object)" /> without <see cref="object.GetHashCode()" />: the mock overrides
///     only <c>Equals</c>, for which the compiler reports CS0659.
/// </summary>
public interface IEqualsOnly
{
	bool Equals(object? obj);
}

public interface IEqualsOnlyDerived : IEqualsOnly
{
}

public interface IEqualsNotNull
{
	bool Equals(object obj);
}

public interface IEqualsAndGetHashCode
{
	bool Equals(object? obj);
	int GetHashCode();
}

/// <summary>
///     Leaves only <c>Equals</c> to the mock, because the sealed <c>GetHashCode</c> cannot be overridden.
/// </summary>
public abstract class EqualsOnlyBase
{
	public abstract override bool Equals(object? obj);
	public sealed override int GetHashCode() => 42;
}

public abstract class EqualsAndGetHashCodeBase
{
	public abstract override bool Equals(object? obj);
	public override int GetHashCode() => 42;
}

/// <summary>
///     The mock overrides the hiding <c>GetHashCode</c>, not <see cref="object.GetHashCode()" />, so the compiler
///     still reports CS0659 for its <c>Equals</c> override.
/// </summary>
public abstract class HiddenGetHashCodeBase : EqualsAndGetHashCodeBase
{
	public new virtual int GetHashCode() => 43;
}
