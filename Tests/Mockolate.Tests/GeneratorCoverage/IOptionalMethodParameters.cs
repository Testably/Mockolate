namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Methods with trailing optional parameters, which setups and verifications can leave out, while an explicit
///     <see langword="null" /> stands for <c>default(T)</c>: a value type and a nullable reference type with non-default
///     defaults, a method whose parameters are all optional, a void method, a generic method, which keeps the classic
///     overloads in union mode, a method with six parameters and one whose kept <see langword="object" /> value must not
///     capture matchers.
/// </summary>
public interface IOptionalMethodParameters
{
	int Method(int key, int count = 5, string? name = "x");
	int AllOptional(int count = 5);
	int WithObject(object? value, int count = 5);
	void VoidMethod(int key, int count = 5);
	int Generic<T>(T value, int count = 5);
	int Six(int a, int b, int c, int d, int e = 5, string? f = "f");
}

/// <summary>
///     A method with an optional parameter next to a same-named one with fewer parameters: setups and verifications
///     cannot leave out its parameters, so an explicit <see langword="null" /> still stands for the declared default.
/// </summary>
public interface IOptionalMethodParametersWithSibling
{
	int Method(int key);
	int Method(int key, int count = 5);
}

/// <summary>
///     A delegate with a trailing optional parameter.
/// </summary>
public delegate int OptionalParametersDelegate(int key, string? name = "x");
