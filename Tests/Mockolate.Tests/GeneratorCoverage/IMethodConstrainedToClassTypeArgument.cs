namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Generic methods whose type parameter is constrained to a type parameter of the mocked type and to further
///     constraints. Closed over a class or an interface (<c>IMethodConstrainedToClassTypeArgument&lt;Exception&gt;</c>),
///     the class type comes first in the restated constraint. Where it replaces the <see langword="class" />
///     constraint, the generated mock implements the method explicitly.
/// </summary>
public interface IMethodConstrainedToClassTypeArgument<T>
{
	void Accept<U>(U? value) where U : class, T;
	U Get<U>(int key = 1) where U : T;
	U? Create<U>() where U : class, System.IComparable, T, new();
}
