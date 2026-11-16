using System;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Members typed with the nullable annotation <c>T?</c> of an unconstrained type parameter. Closed over a value type
///     (<c>INullableTypeParameter&lt;int&gt;</c>), <c>T?</c> is the plain value type and not <see cref="Nullable{T}" />.
/// </summary>
public interface INullableTypeParameter<T>
{
	T? Value { get; set; }
	T? this[T? key] { get; set; }
	T? this[T? a, int b, T? c, int d, T? e] { get; }
	event Action<T?, int>? Changed;
	T? Get();
	void Accept(T? value, int count);
	T? Combine(T? a, int b, T? c, int d, T? e);
	bool TryGet(int key, out T? value);
	void Update(ref T? value);
}
