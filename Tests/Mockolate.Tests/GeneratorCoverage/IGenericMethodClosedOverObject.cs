namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Generic methods whose type parameter a call can close over <see langword="object" />, to which every matcher
///     converts, so the overload that takes values must recognize a matcher among them.
/// </summary>
public interface IGenericMethodClosedOverObject
{
	void Plain<T>(T value);
	int Two<T>(int level, T value);
	void Dependent<T, TBase>(T? value, TBase? other) where T : TBase where TBase : class;
	int Five<T>(int a, int b, int c, int d, T value);
	void Struct<T>(T value) where T : struct;
}
