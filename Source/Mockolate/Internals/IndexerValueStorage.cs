#pragma warning disable CS8714 // Child lookups and additions use TKey after an explicit null check; callers forward via key!.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Mockolate.Internals;

/// <summary>
///     Marker type for the typed child indexer of an <see cref="IndexerValueStorage" /> node.
///     Concrete implementations inherit from <see cref="Dictionary{TKey,TValue}" /> for zero dispatch overhead.
/// </summary>
internal interface IIndexerChildIndex;

/// <summary>
///     A typed child-index dictionary for a specific key type <typeparamref name="TKey" /> and value type
///     <typeparamref name="TValue" />. Inherits from <see cref="Dictionary{TKey, TValue}" /> so lookups go
///     straight through without boxing.
/// </summary>
[DebuggerNonUserCode]
internal sealed class IndexerChildIndex<TKey, TValue>
	: Dictionary<TKey, IndexerValueStorage<TValue>>, IIndexerChildIndex
	where TKey : notnull
{
}

/// <summary>
///     Non-generic base for an indexer value-storage tree node. Held by
///     <see cref="Mockolate.Interactions.IndexerAccess.Storage" /> — each concrete node is a typed
///     <see cref="IndexerValueStorage{TValue}" />. The generic parameter types (T1, T2, ...) of an
///     <see cref="Mockolate.Interactions.IndexerAccess" /> are only known on the subclass, so tree traversal
///     goes through these non-generic dispatch forwarders.
/// </summary>
/// <remarks>
///     This type is public only because source-generated <c>IndexerGetterAccess</c> / <c>IndexerSetterAccess</c>
///     subclasses (5+ parameters) live in the user's assembly and must override
///     <c>IndexerAccess.TraverseStorage</c>. It is not intended to be constructed or subclassed directly.
/// </remarks>
[DebuggerNonUserCode]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class IndexerValueStorage
{
	/// <summary>
	///     Non-generic dispatch to <see cref="IndexerValueStorage{TValue}.GetChild{TKey}(TKey)" /> on the
	///     concrete typed node. Used by generated access subclasses that only know the parameter types.
	/// </summary>
	public abstract IndexerValueStorage? GetChildDispatch<TKey>(TKey key);

	/// <summary>
	///     Non-generic dispatch to <see cref="IndexerValueStorage{TValue}.GetOrAddChild{TKey}(TKey)" /> on the
	///     concrete typed node. Used by generated access subclasses that only know the parameter types.
	/// </summary>
	public abstract IndexerValueStorage GetOrAddChildDispatch<TKey>(TKey key);
}

/// <summary>
///     Tree node that stores indexer values by their typed parameter path. The leaf node's <see cref="Value" />
///     holds the stored indexer result; internal nodes carry an <see cref="IIndexerChildIndex" /> that maps
///     the next parameter to further child nodes.
/// </summary>
[DebuggerNonUserCode]
internal sealed class IndexerValueStorage<TValue> : IndexerValueStorage
{
	private IIndexerChildIndex? _childIndex;
	private IndexerValueStorage<TValue>? _nullChild;

	/// <summary>
	///     Whether <see cref="Value" /> has been assigned. Distinguishes "never stored" from "stored default".
	/// </summary>
	internal bool HasValue;

	/// <summary>
	///     The value stored at this tree node. Typed to avoid boxing for value-type indexer return types.
	///     Only meaningful when <see cref="HasValue" /> is <see langword="true" />.
	/// </summary>
	internal TValue Value = default!;

	/// <inheritdoc cref="IndexerValueStorage.GetChildDispatch{TKey}(TKey)" />
	public override IndexerValueStorage? GetChildDispatch<TKey>(TKey key) => GetChild(key);

	/// <inheritdoc cref="IndexerValueStorage.GetOrAddChildDispatch{TKey}(TKey)" />
	public override IndexerValueStorage GetOrAddChildDispatch<TKey>(TKey key) => GetOrAddChild(key);

	/// <summary>
	///     Returns the child node for the given <paramref name="key" />, or <see langword="null" /> if none exists.
	/// </summary>
	public IndexerValueStorage<TValue>? GetChild<TKey>(TKey key)
	{
		if (key is null)
		{
			return _nullChild;
		}

		// key was null-checked above, so TKey is effectively notnull here.
		return GetChildNonNull(key!);
	}

	/// <summary>
	///     Returns the child node for the given <paramref name="key" />, creating one if none exists.
	/// </summary>
	public IndexerValueStorage<TValue> GetOrAddChild<TKey>(TKey key)
	{
		if (key is null)
		{
			if (_nullChild is null)
			{
				Interlocked.CompareExchange(ref _nullChild, new IndexerValueStorage<TValue>(), null);
			}

			return _nullChild!;
		}

		return GetOrAddChildNonNull(key!);
	}

#pragma warning disable CS8714 // TKey on call sites above has been null-checked; the callers pass key! to satisfy notnull.
	private IndexerValueStorage<TValue>? GetChildNonNull<TKey>(TKey key) where TKey : notnull
	{
		if (_childIndex is not IndexerChildIndex<TKey, TValue> typedIndex)
		{
			return null;
		}

		lock (typedIndex)
		{
			return typedIndex.TryGetValue(key, out IndexerValueStorage<TValue>? child) ? child : null;
		}
	}

	private IndexerValueStorage<TValue> GetOrAddChildNonNull<TKey>(TKey key) where TKey : notnull
	{
		if (_childIndex is not IndexerChildIndex<TKey, TValue> typedIndex)
		{
			Interlocked.CompareExchange(ref _childIndex, new IndexerChildIndex<TKey, TValue>(), null);
			typedIndex = (IndexerChildIndex<TKey, TValue>)_childIndex!;
		}

		lock (typedIndex)
		{
			if (!typedIndex.TryGetValue(key, out IndexerValueStorage<TValue>? child))
			{
				child = new IndexerValueStorage<TValue>();
				typedIndex[key] = child;
			}

			return child;
		}
	}
#pragma warning restore CS8714
}
