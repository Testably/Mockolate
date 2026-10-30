using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Mockolate.Interactions;

namespace Mockolate.Setup;

internal partial class MockSetups
{
	[DebuggerDisplay("{ToString()}")]
#if !DEBUG
	[DebuggerNonUserCode]
#endif
	internal sealed class IndexerSetups
	{
		private IndexerSetup[]? _storage;
		private readonly MockolateLock _storageLock = new();
		private IndexerValueStorage?[]? _valueStorages;
		private readonly object _valueStoragesLock = new();

		public int Count => Volatile.Read(ref _storage)?.Length ?? 0;

		/// <summary>
		///     Pre-sizes the per-signature value-storage array. Optional — <see cref="GetOrCreateStorage{TValue}(int)" />
		///     grows lazily if unset. When the total signature count is known at mock-construction time, calling this
		///     once avoids any reallocations.
		/// </summary>
		internal void InitializeStorageCount(int count)
		{
			if (_valueStorages is null)
			{
				Interlocked.CompareExchange(ref _valueStorages, new IndexerValueStorage?[count], null);
			}
		}

		/// <summary>
		///     Returns the root value storage for the indexer signature at the given <paramref name="signatureIndex" />,
		///     creating one if none exists. Grows the backing array on demand when not pre-initialised.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="signatureIndex" /> is negative.</exception>
		internal IndexerValueStorage<TValue> GetOrCreateStorage<TValue>(int signatureIndex)
		{
			if (signatureIndex < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(signatureIndex), signatureIndex,
					"Signature index must be non-negative.");
			}

			IndexerValueStorage?[]? storages = _valueStorages;
			if (storages is null || storages.Length <= signatureIndex)
			{
				storages = EnsureCapacity(signatureIndex);
			}

			IndexerValueStorage? slot = storages[signatureIndex];
			if (slot is null)
			{
				IndexerValueStorage<TValue> created = new();
				slot = Interlocked.CompareExchange(ref storages[signatureIndex], created, null) ?? created;
			}

			if (slot is not IndexerValueStorage<TValue> typed)
			{
				throw new InvalidOperationException(
					$"Indexer storage at signature index {signatureIndex} was created as '{slot.GetType()}' but is being accessed as 'IndexerValueStorage<{typeof(TValue)}>'. This indicates a signature-index collision between distinct indexer signatures - please report a bug against the source generator.");
			}

			return typed;
		}

		private IndexerValueStorage?[] EnsureCapacity(int signatureIndex)
		{
			lock (_valueStoragesLock)
			{
				IndexerValueStorage?[]? storages = _valueStorages;
				if (storages is null)
				{
					storages = new IndexerValueStorage?[signatureIndex + 1];
					_valueStorages = storages;
					return storages;
				}

				if (storages.Length <= signatureIndex)
				{
					IndexerValueStorage?[] grown = new IndexerValueStorage?[signatureIndex + 1];
					Array.Copy(storages, grown, storages.Length);
					_valueStorages = grown;
					return grown;
				}

				return storages;
			}
		}

		public T? GetMatching<T>(Func<T, bool> predicate) where T : IndexerSetup
		{
			IndexerSetup[]? storage = Volatile.Read(ref _storage);
			if (storage is null)
			{
				return null;
			}

			for (int i = storage.Length - 1; i >= 0; i--)
			{
				if (storage[i] is T typedSetup && predicate(typedSetup))
				{
					return typedSetup;
				}
			}

			return null;
		}

		public T? GetMatching<T>(IndexerAccess access) where T : IndexerSetup
		{
			IndexerSetup[]? storage = Volatile.Read(ref _storage);
			if (storage is null)
			{
				return null;
			}

			for (int i = storage.Length - 1; i >= 0; i--)
			{
				if (storage[i] is T typedSetup &&
				    ((IInteractiveIndexerSetup)typedSetup).Matches(access))
				{
					return typedSetup;
				}
			}

			return null;
		}

		/// <inheritdoc cref="object.ToString()" />
		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			IndexerSetup[]? storage = Volatile.Read(ref _storage);
			if (storage is null || storage.Length == 0)
			{
				return "0 indexers";
			}

			int count = storage.Length;
			StringBuilder sb = new();
			sb.Append(count).Append(count == 1 ? " indexer:" : " indexers:").AppendLine();
			foreach (IndexerSetup indexerSetup in storage)
			{
				sb.Append(indexerSetup).AppendLine();
			}

			sb.Length -= Environment.NewLine.Length;
			return sb.ToString();
		}

		/// <summary>
		///     Copies the setups on write, so that matching reads them without a lock while it runs user code.
		/// </summary>
		internal void Add(IndexerSetup setup)
		{
			lock (_storageLock)
			{
				IndexerSetup[]? existing = _storage;
				IndexerSetup[] storage;
				if (existing is null)
				{
					storage = [setup,];
				}
				else
				{
					storage = new IndexerSetup[existing.Length + 1];
					Array.Copy(existing, storage, existing.Length);
					storage[existing.Length] = setup;
				}

				Volatile.Write(ref _storage, storage);
			}
		}

		internal IEnumerable<IndexerSetup> EnumerateUnusedSetupsBy(IMockInteractions interactions)
		{
			IndexerSetup[]? storage = Volatile.Read(ref _storage);
			if (storage is null)
			{
				return [];
			}

			return storage.Where(indexerSetup => interactions.OfType<IndexerAccess>()
					.All(indexerAccess => !((IInteractiveIndexerSetup)indexerSetup).Matches(indexerAccess)))
				.ToList();
		}
	}
}
