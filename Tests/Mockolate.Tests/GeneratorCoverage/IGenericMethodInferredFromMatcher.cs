namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Generic methods with an optional parameter whose type argument a call can infer from a matcher, so that the
///     overloads that leave out the optional parameter take the matcher with the type argument it infers, also next to a
///     value for another type parameter above four parameters, and a lone
///     <see langword="null" /> forwards to the full overload with the declared default value.
/// </summary>
public interface IGenericMethodInferredFromMatcher
{
	int Trailing<T>(T value, T other = default!);
	int KeptAboveFour<T>(T value, int a, int b, int c, int d = 4);
	int LeftOutAboveFour<T>(int a, int b, int c, int d, T value = default!);
	int TwoAboveFour<T1, T2>(T1 first, T2 second, int a, int b, T2 other = default!);
}
