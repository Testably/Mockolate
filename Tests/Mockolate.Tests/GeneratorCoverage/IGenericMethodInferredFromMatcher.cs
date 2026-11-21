namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Generic methods with an optional parameter whose type argument a call can infer from a matcher, so that the
///     overloads that leave out the optional parameter take the matcher with the type argument it infers, also next to a
///     value for another type parameter above four parameters, and a lone
///     <see langword="null" /> forwards to the full overload with the declared default value. Also a matcher next to a
///     matcher for a trailing <c>params object[]</c>, and a value next to a non-generic sibling overload.
/// </summary>
public interface IGenericMethodInferredFromMatcher
{
	int Trailing<T>(T value, T other = default!);
	int KeptAboveFour<T>(T value, int a, int b, int c, int d = 4);
	int LeftOutAboveFour<T>(int a, int b, int c, int d, T value = default!);
	int TwoAboveFour<T1, T2>(T1 first, T2 second, int a, int b, T2 other = default!);
	int FiveAboveFour<T>(T a, T b, T c, T d, T e = default!);
	int WithRest<T>(T value, params object[] rest);
	int Sibling<T>(T value);
	int Sibling(System.IComparable value);
}
