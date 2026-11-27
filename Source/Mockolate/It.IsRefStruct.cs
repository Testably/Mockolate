#if NET9_0_OR_GREATER
using System;
using Mockolate.Parameters;

namespace Mockolate;

public partial class It
{
	/// <summary>
	///     Matches any value of the ref struct type <typeparamref name="T" />.
	/// </summary>
	/// <remarks>
	///     Use instead of <see cref="IsAny{T}" /> for parameter types that require the
	///     <c>allows ref struct</c> anti-constraint. The returned matcher implements
	///     <see cref="IParameter{T}" /> and <see cref="IParameterMatch{T}" /> only — it does not
	///     carry an <c>Action&lt;T&gt;</c> callback slot, which would be illegal for a ref struct
	///     <typeparamref name="T" />.
	/// </remarks>
	public static IParameter<T> IsAnyRefStruct<T>()
		where T : allows ref struct
		=> new AnyRefStructMatch<T>();

	/// <summary>
	///     Matches a ref struct value of type <typeparamref name="T" /> against the given
	///     <paramref name="predicate" />.
	/// </summary>
	public static IParameter<T> IsRefStruct<T>(RefStructPredicate<T> predicate)
		where T : allows ref struct
		=> new RefStructPredicateMatch<T>(predicate);

	/// <summary>
	///     Matches any ref struct value of type <typeparamref name="T" /> and carries a
	///     user-supplied <paramref name="projection" /> that turns the ref struct into a
	///     dictionary-friendly scalar key of type <typeparamref name="TProjected" />.
	/// </summary>
	/// <remarks>
	///     <para>
	///         When supplied to a combined ref-struct-keyed indexer setup, enables write-then-read
	///         correlation: values written via the setter are stored keyed by the projection, and a
	///         subsequent getter call with a key that projects to the same scalar returns the
	///         stored value. Works at any arity — for multi-parameter indexers, storage activates
	///         once every ref-struct slot carries a projection; non-ref-struct slots contribute
	///         their raw value as part of the composite dispatch key.
	///     </para>
	///     <para>
	///         If any ref-struct slot is matched without a projection (e.g. via
	///         <see cref="IsAnyRefStruct{T}" /> or <see cref="IsRefStruct{T}(RefStructPredicate{T})" />),
	///         the indexer setup stays storage-less — the getter returns its configured
	///         <c>Returns(...)</c> value or the framework default, regardless of what was written
	///         via the setter.
	///     </para>
	/// </remarks>
	public static IParameter<T> IsRefStructBy<T, TProjected>(
		RefStructProjection<T, TProjected> projection)
		where T : allows ref struct
		where TProjected : notnull
		=> new RefStructProjectionMatch<T, TProjected>(projection, projectedPredicate: null);

	/// <summary>
	///     Matches a ref struct value of type <typeparamref name="T" /> whose
	///     <paramref name="projection" /> satisfies <paramref name="projectedPredicate" />, and
	///     carries the projection for write-then-read storage correlation.
	/// </summary>
	/// <remarks>
	///     See <see cref="IsRefStructBy{T, TProjected}(RefStructProjection{T, TProjected})" />
	///     for the storage semantics.
	/// </remarks>
	public static IParameter<T> IsRefStructBy<T, TProjected>(
		RefStructProjection<T, TProjected> projection,
		Func<TProjected, bool> projectedPredicate)
		where T : allows ref struct
		where TProjected : notnull
		=> new RefStructProjectionMatch<T, TProjected>(projection, projectedPredicate);

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class AnyRefStructMatch<T> : IParameter<T>, IParameterMatch<T>
		where T : allows ref struct
	{
		public bool Matches(object? value) => false;
		public bool Matches(T value) => true;
		public void InvokeCallbacks(object? value) { }
		public void InvokeCallbacks(T value) { }

		public override string ToString()
			=> $"It.IsAnyRefStruct<{typeof(T).Name}>()";
	}

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class RefStructPredicateMatch<T>(RefStructPredicate<T> predicate)
		: IParameter<T>, IParameterMatch<T>
		where T : allows ref struct
	{
		public bool Matches(object? value) => false;
		public bool Matches(T value) => predicate(value);
		public void InvokeCallbacks(object? value) { }
		public void InvokeCallbacks(T value) { }

		public override string ToString()
			=> $"It.IsRefStruct<{typeof(T).Name}>(<predicate>)";
	}

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class RefStructProjectionMatch<T, TProjected>(
		RefStructProjection<T, TProjected> projection,
		Func<TProjected, bool>? projectedPredicate)
		: IParameter<T>, IParameterMatch<T>,
			IRefStructProjectionMatch<T>, IRefStructProjectionMatch<T, TProjected>
		where T : allows ref struct
		where TProjected : notnull
	{
		public bool Matches(object? value) => false;

		public bool Matches(T value)
			=> projectedPredicate is null || projectedPredicate(projection(value));

		public void InvokeCallbacks(object? value) { }
		public void InvokeCallbacks(T value) { }

		public TProjected Project(T value) => projection(value);

		// TProjected : notnull → boxing is safe.
		object IRefStructProjectionMatch<T>.Project(T value) => projection(value);

		public override string ToString()
			=> projectedPredicate is null
				? $"It.IsRefStructBy<{typeof(T).Name}, {typeof(TProjected).Name}>(<projection>)"
				: $"It.IsRefStructBy<{typeof(T).Name}, {typeof(TProjected).Name}>(<projection>, <predicate>)";
	}
}
#endif
