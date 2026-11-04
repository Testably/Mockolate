using System.ComponentModel;
using Mockolate.Internals;
using Mockolate.Setup;

namespace Mockolate.Interactions;

/// <summary>
///     An access of an indexer.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public abstract class IndexerAccess : IInteraction
{
	/// <summary>
	///     The number of indexer parameters.
	/// </summary>
	public abstract int ParameterCount { get; }

	/// <summary>
	///     Returns the indexer parameter value at the given <paramref name="index" />.
	/// </summary>
	public abstract object? GetParameterValueAt(int index);

	internal IndexerValueStorage? Storage { get; set; }

	internal void AttachStorage(IndexerValueStorage storage)
		=> Storage = storage;

	/// <summary>
	///     Walks the given <paramref name="storage" /> along the typed parameter path for this access,
	///     returning the leaf node or <see langword="null" /> if no path exists (and
	///     <paramref name="createMissing" /> is <see langword="false" />).
	/// </summary>
	/// <remarks>
	///     Each concrete <see cref="IndexerAccess" /> subclass knows its parameter types and traverses
	///     without boxing. Not intended for external implementation — the only supported subclasses are
	///     <see cref="IndexerGetterAccess{T1}" />, <see cref="IndexerSetterAccess{T1, TValue}" />, their
	///     multi-parameter siblings, and the source-generated 5+ parameter variants.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected abstract IndexerValueStorage? TraverseStorage(IndexerValueStorage? storage, bool createMissing);

	/// <summary>
	///     Attempts to find a previously stored value for this access.
	/// </summary>
	public bool TryFindStoredValue<T>(out T value)
	{
		if (TraverseStorage(Storage, false) is IndexerValueStorage<T> typedLeaf && typedLeaf.HasValue)
		{
			value = typedLeaf.Value;
			return true;
		}

		value = default!;
		return false;
	}

	/// <summary>
	///     Stores the given <paramref name="value" /> for this access.
	/// </summary>
	public void StoreValue<T>(T value)
	{
		if (TraverseStorage(Storage, true) is IndexerValueStorage<T> typedLeaf)
		{
			typedLeaf.Value = value;
			typedLeaf.HasValue = true;
		}
	}
}
