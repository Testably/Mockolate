namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Generic methods whose type parameter is constrained to a type parameter of the mocked type. Closed over a sealed
///     class, a struct or an array (<c>IMethodConstrainedToTypeArgument&lt;int&gt;</c>), the constraint becomes one that
///     C# accepts only when it is inherited, so the generated mock implements these methods explicitly and does not
///     restate the constraint on its setup and verify surface.
/// </summary>
public interface IMethodConstrainedToTypeArgument<T>
{
	void Accept<U>(U? value) where U : T;
	U Get<U>(int key = 1) where U : T;
	U? GetNotNull<U>(U? fallback) where U : notnull, T;
	void AcceptDependent<U, V>(U? value, V? other) where U : V where V : T;
}

public abstract class MethodConstrainedToTypeArgumentBase<T>
{
	public abstract void Accept<U>(U? value) where U : T;
	public virtual U? Get<U>() where U : T => default;
}
