#if NET9_0_OR_GREATER
using System.ComponentModel;
using Mockolate.Parameters;

namespace Mockolate.Internals;

/// <summary>
///     Shared helpers for activating projection-based storage on multi-parameter
///     ref-struct-keyed indexer setups. Centralises the per-slot rules so the arity 2-4
///     hand-written setups and the arity 5+ generator-emitted setups stay consistent.
/// </summary>
/// <remarks>
///     Public only because generator-emitted arity 5+ ref-struct indexer setups (in the user's
///     assembly) need to call these helpers; not intended for direct use from user code.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RefStructStorageHelper
{
	/// <summary>
	///     Returns <see langword="true" /> when a slot can contribute to projection-based storage:
	///     either the slot carries a projection matcher, or the slot's type is not a ref struct
	///     (so the caller can supply a boxed raw value as the dispatch key).
	/// </summary>
	public static bool IsSlotStorageReady<T>(IRefStructProjectionMatch<T>? projection)
		where T : allows ref struct
		=> projection is not null || !typeof(T).IsByRefLike;

	/// <summary>
	///     Resolves the dispatch key for a slot: the projection output if a projection matcher is
	///     present, otherwise the caller-supplied boxed raw value. Returns <see langword="false" />
	///     when neither is available — storage lookup must then bail out.
	/// </summary>
	public static bool TryResolveKey<T>(IRefStructProjectionMatch<T>? projection, T value, object? rawKey, out object key)
		where T : allows ref struct
	{
		if (projection is not null)
		{
			key = projection.Project(value);
			return true;
		}

		if (rawKey is not null)
		{
			key = rawKey;
			return true;
		}

		key = null!;
		return false;
	}

	/// <summary>
	///     Allocates a typed <see cref="IndexerValueStorage" /> root for the generator-emitted
	///     arity 5+ setups (which cannot reference the internal <c>IndexerValueStorage&lt;TValue&gt;</c>
	///     directly).
	/// </summary>
	public static IndexerValueStorage CreateStorage<TValue>()
		=> new IndexerValueStorage<TValue>();

	/// <summary>
	///     Reads the value stored at <paramref name="leaf" />, typed to <typeparamref name="TValue" />.
	/// </summary>
	public static bool TryGetLeafValue<TValue>(IndexerValueStorage? leaf, out TValue value)
	{
		if (leaf is IndexerValueStorage<TValue> typed && typed.HasValue)
		{
			value = typed.Value;
			return true;
		}

		value = default!;
		return false;
	}

	/// <summary>
	///     Writes <paramref name="value" /> to <paramref name="leaf" />.
	/// </summary>
	public static void SetLeafValue<TValue>(IndexerValueStorage leaf, TValue value)
	{
		if (leaf is IndexerValueStorage<TValue> typed)
		{
			typed.Value = value;
			typed.HasValue = true;
		}
	}
}
#endif
