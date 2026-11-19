namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     A <c>params object[]</c> takes any argument as an element of its expanded form, also a matcher, so the
///     overloads with a matcher for the whole array must outrank the overload that takes values.
/// </summary>
public interface IParamsObjectArray
{
	int Log(int level, params object[] args);
	int Only(params object[] args);
	void Write(object value);
	void Write(params object[] args);
}

public delegate int ParamsObjectArrayDelegate(int level, params object[] args);

public delegate int ParamsObjectArrayOnlyDelegate(params object[] args);

public delegate void ParamsArrayDelegate(params string[] values);
