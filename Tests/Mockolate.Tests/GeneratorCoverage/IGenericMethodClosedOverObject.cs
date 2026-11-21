namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Generic methods whose type parameter a call can close over <see langword="object" />, to which every matcher
///     converts, so the overload that takes values must recognize a matcher among them. A matcher binds the matcher
///     overload with the type argument it infers: also next to a value for another type parameter above four
///     parameters, next to a matcher for a trailing <c>params object[]</c> and in the overloads that leave out an
///     optional parameter, where a lone <see langword="null" /> forwards with the declared default value. A value next
///     to a non-generic sibling binds the same method as the call on the mock.
/// </summary>
public interface IGenericMethodClosedOverObject
{
	void Plain<T>(T value);
	int Get<T>(in T value);
	void Log<T>(T value, params int[] rest);
	int Count<T>(T value, params int[] rest);
	int Two<T>(int level, T value);
	void Dependent<T, TBase>(T? value, TBase? other) where T : TBase where TBase : class;
	int Five<T>(int a, int b, int c, int d, T value);
	void Struct<T>(T value) where T : struct;
	int Trailing<T>(T value, T other = default!);
	int KeptAboveFour<T>(T value, int a, int b, int c, int d = 4);
	int LeftOutAboveFour<T>(int a, int b, int c, int d, T value = default!);
	int TwoAboveFour<T1, T2>(T1 first, T2 second, int a, int b, T2 other = default!);
	int FiveAboveFour<T>(T a, T b, T c, T d, T e = default!);
	int WithRest<T>(T value, params object[] rest);
	int Sibling<T>(T value);
	int Sibling(System.IComparable value);
}
