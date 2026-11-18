using System;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Optional parameters of a method type parameter type whose declared default is <see langword="null" />. The
///     generated code must repeat it as <see langword="default" /> unless the type parameter is known to be a reference
///     type.
/// </summary>
public interface IGenericParameterDefaultValues
{
	void Unconstrained<T>(T value = default!);
	void Annotated<T>(T? value = default);
	void Struct<T>(T value = default) where T : struct;
	void NullableStruct<T>(T? value = null) where T : struct;
	void Class<T>(T value = null!) where T : class;
	void NotNull<T>(T value = default!) where T : notnull;
	void Interface<T>(T value = default!) where T : IComparable;
	int Trailing<T>(int a, T value = default!);
}

public abstract class GenericParameterDefaultValuesBase
{
	public abstract void Unconstrained<T>(T value = default!);
	public virtual int Virtual<T>(T value = default!) => 0;
}
