using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Mockolate.Setup;

internal partial class MockSetups
{
	[DebuggerDisplay("{ToString()}")]
#if !DEBUG
	[DebuggerNonUserCode]
#endif
	internal sealed class EventSetups
	{
		private int _count;
		private List<EventSetup>? _storage;

		// ReSharper disable once InconsistentlySynchronizedField
		public int Count => Volatile.Read(ref _count);

		private List<EventSetup> GetOrCreateStorage()
		{
			if (_storage is null)
			{
				Interlocked.CompareExchange(ref _storage, [], comparand: null);
			}

			return _storage!;
		}

		public void Add(EventSetup setup)
		{
			List<EventSetup> storage = GetOrCreateStorage();
			lock (storage)
			{
				storage.Add(setup);
				Volatile.Write(ref _count, _count + 1);
			}
		}

		public List<EventSetup> GetByName(string name)
		{
			List<EventSetup> result = [];
			if (_storage is null)
			{
				return result;
			}

			List<EventSetup> storage = _storage;
			lock (storage)
			{
				foreach (EventSetup setup in storage)
				{
					if (string.Equals(setup.Name, name, StringComparison.Ordinal))
					{
						result.Add(setup);
					}
				}
			}

			return result;
		}
	}
}
