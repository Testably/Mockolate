using System.Collections.Generic;
using Mockolate.Interactions;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Interactions;

public sealed class IndexerAccessTests
{
	[Test]
	public async Task StoreValue_OnIndexerGetterAccess1_UsesGetOrAddChildDispatch()
	{
		RecordingStorage storage = new();
		IndexerGetterAccess<int> access = new(42)
		{
			Storage = storage,
		};

		access.StoreValue("v");

		await That(storage.Calls).IsEqualTo(["GetOrAdd(42)",]);
	}

	[Test]
	public async Task StoreValue_OnIndexerGetterAccess2_UsesGetOrAddChildDispatchForEachParameter()
	{
		RecordingStorage storage = new();
		IndexerGetterAccess<int, int> access = new(1, 2)
		{
			Storage = storage,
		};

		access.StoreValue("v");

		await That(storage.Calls).IsEqualTo(["GetOrAdd(1)", "GetOrAdd(2)",]);
	}

	[Test]
	public async Task StoreValue_OnIndexerGetterAccess3_UsesGetOrAddChildDispatchForEachParameter()
	{
		RecordingStorage storage = new();
		IndexerGetterAccess<int, int, int> access = new(1, 2, 3)
		{
			Storage = storage,
		};

		access.StoreValue("v");

		await That(storage.Calls).IsEqualTo(["GetOrAdd(1)", "GetOrAdd(2)", "GetOrAdd(3)",]);
	}

	[Test]
	public async Task StoreValue_OnIndexerGetterAccess4_UsesGetOrAddChildDispatchForEachParameter()
	{
		RecordingStorage storage = new();
		IndexerGetterAccess<int, int, int, int> access =
			new(1, 2, 3, 4)
			{
				Storage = storage,
			};

		access.StoreValue("v");

		await That(storage.Calls).IsEqualTo(["GetOrAdd(1)", "GetOrAdd(2)", "GetOrAdd(3)", "GetOrAdd(4)",]);
	}

	[Test]
	public async Task StoreValue_OnIndexerSetterAccess1_UsesGetOrAddChildDispatch()
	{
		RecordingStorage storage = new();
		IndexerSetterAccess<int, string> access = new(42, "v")
		{
			Storage = storage,
		};

		access.StoreValue("v");

		await That(storage.Calls).IsEqualTo(["GetOrAdd(42)",]);
	}

	[Test]
	public async Task StoreValue_OnIndexerSetterAccess2_UsesGetOrAddChildDispatchForEachParameter()
	{
		RecordingStorage storage = new();
		IndexerSetterAccess<int, int, string> access =
			new(1, 2, "v")
			{
				Storage = storage,
			};

		access.StoreValue("v");

		await That(storage.Calls).IsEqualTo(["GetOrAdd(1)", "GetOrAdd(2)",]);
	}

	[Test]
	public async Task StoreValue_OnIndexerSetterAccess3_UsesGetOrAddChildDispatchForEachParameter()
	{
		RecordingStorage storage = new();
		IndexerSetterAccess<int, int, int, string> access =
			new(1, 2, 3, "v")
			{
				Storage = storage,
			};

		access.StoreValue("v");

		await That(storage.Calls).IsEqualTo(["GetOrAdd(1)", "GetOrAdd(2)", "GetOrAdd(3)",]);
	}

	[Test]
	public async Task StoreValue_OnIndexerSetterAccess4_UsesGetOrAddChildDispatchForEachParameter()
	{
		RecordingStorage storage = new();
		IndexerSetterAccess<int, int, int, int, string> access =
			new(1, 2, 3, 4, "v")
			{
				Storage = storage,
			};

		access.StoreValue("v");

		await That(storage.Calls).IsEqualTo(["GetOrAdd(1)", "GetOrAdd(2)", "GetOrAdd(3)", "GetOrAdd(4)",]);
	}

	[Test]
	public async Task ToString_OnIndexerGetterAccess3_FormatsEachNullParameterAsNullLiteral()
	{
		IndexerGetterAccess<string?, string?, string?> access = new(null, null, null);

		await That(access.ToString()).IsEqualTo("get indexer [null, null, null]")
			.Because(
				"each `?? \"null\"` literal in IndexerGetterAccess<T1, T2, T3>.ToString must render a null parameter as `null` rather than as an empty slot like \"[, ...]\"");
	}

	[Test]
	public async Task ToString_OnIndexerGetterAccess4_FormatsEachNullParameterAsNullLiteral()
	{
		IndexerGetterAccess<string?, string?, string?, string?> access =
			new(null, null, null, null);

		await That(access.ToString()).IsEqualTo("get indexer [null, null, null, null]");
	}

	[Test]
	public async Task ToString_OnIndexerSetterAccess3_FormatsEachNullParameterAsNullLiteral()
	{
		IndexerSetterAccess<string?, string?, string?, string?> access = new(null, null, null, null);

		await That(access.ToString()).IsEqualTo("set indexer [null, null, null] to null");
	}

	[Test]
	public async Task ToString_OnIndexerSetterAccess4_FormatsEachNullParameterAsNullLiteral()
	{
		IndexerSetterAccess<string?, string?, string?, string?, string?> access =
			new(null, null, null, null, null);

		await That(access.ToString()).IsEqualTo("set indexer [null, null, null, null] to null");
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerGetterAccess1_UsesGetChildDispatch()
	{
		RecordingStorage storage = new();
		IndexerGetterAccess<int> access = new(42)
		{
			Storage = storage,
		};

		access.TryFindStoredValue(out string _);

		await That(storage.Calls).IsEqualTo(["Get(42)",]);
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerGetterAccess1_WithoutStorage_ShouldReturnFalse()
	{
		IndexerGetterAccess<int> access = new(42);

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse()
			.Because(
				"the first `if (s is null) { return null; }` guard in IndexerGetterAccess<T1>.TraverseStorage must short-circuit instead of dereferencing null to call GetChildDispatch");
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerGetterAccess2_UsesGetChildDispatchForEachParameter()
	{
		RecordingStorage storage = new();
		IndexerGetterAccess<int, int> access = new(1, 2)
		{
			Storage = storage,
		};

		access.TryFindStoredValue(out string _);

		await That(storage.Calls).IsEqualTo(["Get(1)", "Get(2)",]);
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerGetterAccess2_WithoutStorage_ShouldReturnFalse()
	{
		IndexerGetterAccess<int, int> access = new(1, 2);

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerGetterAccess3_UsesGetChildDispatchForEachParameter()
	{
		RecordingStorage storage = new();
		IndexerGetterAccess<int, int, int> access = new(1, 2, 3)
		{
			Storage = storage,
		};

		access.TryFindStoredValue(out string _);

		await That(storage.Calls).IsEqualTo(["Get(1)", "Get(2)", "Get(3)",]);
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerGetterAccess3_WithoutStorage_ShouldReturnFalse()
	{
		IndexerGetterAccess<int, int, int> access = new(1, 2, 3);

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerGetterAccess4_UsesGetChildDispatchForEachParameter()
	{
		RecordingStorage storage = new();
		IndexerGetterAccess<int, int, int, int> access =
			new(1, 2, 3, 4)
			{
				Storage = storage,
			};

		access.TryFindStoredValue(out string _);

		await That(storage.Calls).IsEqualTo(["Get(1)", "Get(2)", "Get(3)", "Get(4)",]);
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerGetterAccess4_WithoutStorage_ShouldReturnFalse()
	{
		IndexerGetterAccess<int, int, int, int> access = new(1, 2, 3, 4);

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess1_UsesGetChildDispatch()
	{
		RecordingStorage storage = new();
		IndexerSetterAccess<int, string> access = new(42, "v")
		{
			Storage = storage,
		};

		access.TryFindStoredValue(out string _);

		await That(storage.Calls).IsEqualTo(["Get(42)",]);
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess1_WithoutStorage_ShouldReturnFalse()
	{
		IndexerSetterAccess<int, string> access = new(42, "v");

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess2_UsesGetChildDispatchForEachParameter()
	{
		RecordingStorage storage = new();
		IndexerSetterAccess<int, int, string> access =
			new(1, 2, "v")
			{
				Storage = storage,
			};

		access.TryFindStoredValue(out string _);

		await That(storage.Calls).IsEqualTo(["Get(1)", "Get(2)",]);
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess2_WhenFirstDispatchReturnsNull_ShouldReturnFalse()
	{
		// Pins the second `if (s is null) { return null; }` guard inside
		// IndexerSetterAccess<T1, T2, TValue>.TraverseStorage — the one after Parameter1's
		// GetChildDispatch returns null. With the body removed, the next line dereferences
		// null `s` for Parameter2's dispatch and throws NRE.
		NullAfterStorage storage = new(1);
		IndexerSetterAccess<int, int, string> access = new(1, 2, "v")
		{
			Storage = storage,
		};

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess2_WithoutStorage_ShouldReturnFalse()
	{
		IndexerSetterAccess<int, int, string> access = new(1, 2, "v");

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess3_UsesGetChildDispatchForEachParameter()
	{
		RecordingStorage storage = new();
		IndexerSetterAccess<int, int, int, string> access =
			new(1, 2, 3, "v")
			{
				Storage = storage,
			};

		access.TryFindStoredValue(out string _);

		await That(storage.Calls).IsEqualTo(["Get(1)", "Get(2)", "Get(3)",]);
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess3_WhenFirstDispatchReturnsNull_ShouldReturnFalse()
	{
		NullAfterStorage storage = new(1);
		IndexerSetterAccess<int, int, int, string> access = new(1, 2, 3, "v")
		{
			Storage = storage,
		};

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess3_WhenSecondDispatchReturnsNull_ShouldReturnFalse()
	{
		NullAfterStorage storage = new(2);
		IndexerSetterAccess<int, int, int, string> access = new(1, 2, 3, "v")
		{
			Storage = storage,
		};

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess3_WithoutStorage_ShouldReturnFalse()
	{
		IndexerSetterAccess<int, int, int, string> access = new(1, 2, 3, "v");

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess4_UsesGetChildDispatchForEachParameter()
	{
		RecordingStorage storage = new();
		IndexerSetterAccess<int, int, int, int, string> access =
			new(1, 2, 3, 4, "v")
			{
				Storage = storage,
			};

		access.TryFindStoredValue(out string _);

		await That(storage.Calls).IsEqualTo(["Get(1)", "Get(2)", "Get(3)", "Get(4)",]);
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess4_WhenFirstDispatchReturnsNull_ShouldReturnFalse()
	{
		NullAfterStorage storage = new(1);
		IndexerSetterAccess<int, int, int, int, string> access = new(1, 2, 3, 4, "v")
		{
			Storage = storage,
		};

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess4_WhenSecondDispatchReturnsNull_ShouldReturnFalse()
	{
		NullAfterStorage storage = new(2);
		IndexerSetterAccess<int, int, int, int, string> access = new(1, 2, 3, 4, "v")
		{
			Storage = storage,
		};

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess4_WhenThirdDispatchReturnsNull_ShouldReturnFalse()
	{
		NullAfterStorage storage = new(3);
		IndexerSetterAccess<int, int, int, int, string> access = new(1, 2, 3, 4, "v")
		{
			Storage = storage,
		};

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task TryFindStoredValue_OnIndexerSetterAccess4_WithoutStorage_ShouldReturnFalse()
	{
		IndexerSetterAccess<int, int, int, int, string> access = new(1, 2, 3, 4, "v");

		bool found = access.TryFindStoredValue(out string _);

		await That(found).IsFalse();
	}

	private sealed class RecordingStorage : IndexerValueStorage
	{
		public List<string> Calls { get; } = [];

		public override IndexerValueStorage? GetChildDispatch<TKey>(TKey key)
		{
			Calls.Add($"Get({key?.ToString() ?? "<null>"})");
			return this;
		}

		public override IndexerValueStorage GetOrAddChildDispatch<TKey>(TKey key)
		{
			Calls.Add($"GetOrAdd({key?.ToString() ?? "<null>"})");
			return this;
		}
	}

	// Returns null on the Nth GetChildDispatch call, otherwise returns this.
	// _nullAtCall = 1 means return null on the 1st call;
	// _nullAtCall = 2 means return null on the 2nd call (and the 1st returns this); etc.
	private sealed class NullAfterStorage : IndexerValueStorage
	{
		private readonly int _nullAtCall;
		private int _calls;

		public NullAfterStorage(int nullAtCall)
		{
			_nullAtCall = nullAtCall;
		}

		public override IndexerValueStorage? GetChildDispatch<TKey>(TKey key)
		{
			_calls++;
			return _calls == _nullAtCall ? null : this;
		}

		public override IndexerValueStorage GetOrAddChildDispatch<TKey>(TKey key) => this;
	}
}
