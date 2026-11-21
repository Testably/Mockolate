namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Methods with a single optional parameter, which setups and verifications can leave out, while an explicit
///     <see langword="null" /> stands for <c>default(T)</c>: a value type and a nullable reference type with non-default
///     defaults, a method whose only parameter is optional, one whose kept <see langword="object" /> value must not
///     capture matchers, a void method, a generic method, which keeps the classic overloads in union mode, and a method
///     with six parameters. Several optional parameters keep their <c>= null</c> defaults in classic mode, so that a call
///     can skip one by name.
/// </summary>
public interface IOptionalMethodParameters
{
	int Count(int key, int count = 5);
	int Name(int key, string? name = "x");
	int AllOptional(int count = 5);
	int WithObject(object? value, int count = 5);
	void VoidMethod(int key, int count = 5);
	int Generic<T>(T value, int count = 5);
	int Six(int a, int b, int c, int d, int e, string? f = "f");
	int SeveralOptional(int key, int count = 5, string? name = "x");
}

/// <summary>
///     A method with an optional parameter next to a same-named one with fewer parameters: setups and verifications
///     cannot leave out its parameter, so an explicit <see langword="null" /> still stands for the declared default.
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
