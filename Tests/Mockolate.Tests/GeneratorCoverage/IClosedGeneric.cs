using System;
using System.Collections.Generic;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Generic types that are mocked closed over type arguments (<c>IClosedGeneric&lt;int&gt;</c>). A <c>cref</c> accepts
///     no type arguments and no tuple syntax, so the generated documentation must reference the open generic definition
///     and spell tuples as <see cref="ValueTuple{T1,T2}" />.
/// </summary>
public interface IClosedGeneric<T>
{
	T Value { get; set; }
	T this[T key] { get; }
	event EventHandler<T>? Changed;
	T Get(T value, List<T> values);
	TOther Convert<TOther>(T value);
	(T Item, int Count) Pair((T, string) value);
}

public abstract class ClosedGenericBase<T>
{
	public abstract T Get(T value);
	public virtual (T, int) Pair((T, int) value) => value;
}

public delegate TResult ClosedGenericDelegate<T, TResult>(T value, (T, int) pair);
