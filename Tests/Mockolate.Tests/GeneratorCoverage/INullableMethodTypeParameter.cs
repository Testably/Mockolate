using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Generic methods that use the nullable annotation <c>T?</c> of a constrained method type parameter. The explicit
///     implementations in the generated mock inherit the constraint, so they must state whether <c>T?</c> is a nullable
///     annotation (<c>class</c> or <c>default</c>) and not <see cref="Nullable{T}" />.
/// </summary>
public interface INullableMethodTypeParameter
{
	void AcceptClass<T>(T? value) where T : class;
	void AcceptNullableClass<T>(T? value) where T : class?;
	void AcceptNotNull<T>(T? value) where T : notnull;
	void AcceptInterface<T>(T? value) where T : IComparable;
	void AcceptBaseClass<T>(T? value) where T : Exception;
	void AcceptConstructor<T>(T? value) where T : new();
	void AcceptStruct<T>(T? value) where T : struct;
	void AcceptDependent<T, TBase>(T? value, TBase? other) where T : TBase where TBase : class;
	void AcceptMany<T>(T? a, T? b, T? c, T? d, T? e) where T : notnull;
	bool TryGet<T>(T? key, out T? value, ref T? other, params T?[] rest) where T : class;
	T? GetClass<T>(T? fallback) where T : class;
	T? GetNotNull<T>(T? fallback) where T : notnull;
	Task<T?> GetAsync<T>(List<T?> values) where T : class;
}
