using System;
using System.Diagnostics;
using System.Threading;
using Mockolate.Interactions;
using Mockolate.Setup;

namespace Mockolate.Internals;

public partial class MockRegistry
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private object SetupsByMemberIdLock
	{
		get
		{
			if (field is { } existing)
			{
				return existing;
			}

			Interlocked.CompareExchange(ref field, new object(), null);
			return field!;
		}
	}

	/// <summary>
	///     Returns the generator-known member count hint when <see cref="Interactions" /> is a
	///     <see cref="FastMockInteractions" />, so dispatch tables can be sized once on first allocation
	///     instead of growing one slot at a time as setups for higher-numbered members come in.
	/// </summary>
	private int GetMemberCountHint()
		=> _interactions is FastMockInteractions fast ? fast.Buffers.Length : _interactionMemberCount;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private EventSetup[]?[]? _eventSetupsByMemberId;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private IndexerSetup[]?[]? _indexerSetupsByMemberId;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private PropertySetup?[]? _propertySetupsByMemberId;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private MethodSetup[]?[]? _setupsByMemberId;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private MockSetups? _setups;

	/// <summary>
	///     The registered setups for the mock, including methods, properties, indexers and events.
	/// </summary>
	internal MockSetups Setup => _setups ?? EnsureSetups();

	private MockSetups EnsureSetups()
	{
		Interlocked.CompareExchange(ref _setups, new MockSetups(), null);
		return _setups!;
	}

	/// <summary>
	///     Registers <paramref name="indexerSetup" /> for the default scenario.
	/// </summary>
	/// <param name="indexerSetup">The indexer setup produced by the fluent <c>Setup[...]</c> API.</param>
	internal void SetupIndexer(IndexerSetup indexerSetup)
		=> SetupIndexer("", indexerSetup);

	/// <summary>
	///     Registers <paramref name="indexerSetup" /> for the given <paramref name="scenario" />.
	/// </summary>
	/// <param name="scenario">The scenario name the setup applies to. The setup only applies while <see cref="Scenario" /> equals <paramref name="scenario" /> and does not leak into the default bucket.</param>
	/// <param name="indexerSetup">The indexer setup produced by the fluent <c>Setup[...]</c> API.</param>
	private void SetupIndexer(string scenario, IndexerSetup indexerSetup)
		=> Setup.GetOrCreateScenario(scenario).Indexers.Add(indexerSetup);

	/// <summary>
	///     Registers <paramref name="indexerSetup" /> for the default scenario by indexing it under the
	///     generator-emitted <paramref name="memberId" /> for fast dispatch from the proxy indexer body.
	/// </summary>
	/// <remarks>
	///     The default-scope string-keyed list is intentionally bypassed — the snapshot is authoritative
	///     for default-scope dispatch from generator-emitted code, and <see cref="GetIndexerSetup{T}(Mockolate.Interactions.IndexerAccess)" />
	///     consults the snapshot table directly for scenario-active fallback callers.
	/// </remarks>
	/// <param name="memberId">The generator-emitted member id for the setup's target indexer accessor.</param>
	/// <param name="indexerSetup">The indexer setup produced by the fluent <c>Setup[...]</c> API.</param>
	public void SetupIndexer(int memberId, IndexerSetup indexerSetup)
		=> AppendToIndexerMemberIdBucket(memberId, indexerSetup);

	/// <summary>
	///     Registers <paramref name="indexerSetup" /> for the given <paramref name="scenario" />. When
	///     <paramref name="scenario" /> is the default scope, the setup is indexed by <paramref name="memberId" />
	///     for fast dispatch instead of the string-keyed list; scenario-scoped setups continue to land in the
	///     scenario bucket only.
	/// </summary>
	/// <param name="memberId">The generator-emitted member id for the setup's target indexer accessor.</param>
	/// <param name="scenario">The scenario name the setup applies to.</param>
	/// <param name="indexerSetup">The indexer setup produced by the fluent <c>Setup[...]</c> API.</param>
	public void SetupIndexer(int memberId, string scenario, IndexerSetup indexerSetup)
	{
		if (string.IsNullOrEmpty(scenario))
		{
			AppendToIndexerMemberIdBucket(memberId, indexerSetup);
		}
		else
		{
			SetupIndexer(scenario, indexerSetup);
		}
	}

	private void AppendToIndexerMemberIdBucket(int memberId, IndexerSetup indexerSetup)
	{
		lock (SetupsByMemberIdLock)
		{
			IndexerSetup[]?[]? oldTable = _indexerSetupsByMemberId;
			int requiredLen = memberId + 1;
			// Stryker disable once Conditional : when oldTable is not null and requiredLen <= oldTable.Length, the resize-condition below is false either way and the else-branch reuses oldTable; newLen is unread, so collapsing the ternary to requiredLen is observationally identical.
			int newLen = oldTable is null ? Math.Max(requiredLen, GetMemberCountHint()) : Math.Max(oldTable.Length, requiredLen);
			IndexerSetup[]?[] newTable;
			// Stryker disable once Equality : flipping > to >= only fires the realloc-and-copy path when newLen == oldTable.Length, producing a fresh table with identical contents — externally indistinguishable from reusing oldTable.
			if (oldTable is null || newLen > oldTable.Length)
			{
				newTable = new IndexerSetup[newLen][];
				if (oldTable is not null)
				{
					Array.Copy(oldTable, newTable, oldTable.Length);
				}
			}
			else
			{
				newTable = oldTable;
			}

			IndexerSetup[]? existing = newTable[memberId];
			IndexerSetup[] bucket;
			if (existing is null || existing.Length == 0)
			{
				bucket = new[]
				{
					indexerSetup,
				};
			}
			else
			{
				bucket = new IndexerSetup[existing.Length + 1];
				Array.Copy(existing, bucket, existing.Length);
				bucket[existing.Length] = indexerSetup;
			}

			newTable[memberId] = bucket;
			Volatile.Write(ref _indexerSetupsByMemberId, newTable);
		}
	}

	/// <summary>
	///     Registers <paramref name="methodSetup" /> for the default scenario.
	/// </summary>
	/// <param name="methodSetup">The method setup produced by the fluent <c>Setup.MethodName(...)</c> API.</param>
	internal void SetupMethod(MethodSetup methodSetup)
		=> SetupMethod("", methodSetup);

	/// <summary>
	///     Registers <paramref name="methodSetup" /> for the given <paramref name="scenario" />.
	/// </summary>
	/// <param name="scenario">The scenario name the setup applies to. The setup only applies while <see cref="Scenario" /> equals <paramref name="scenario" /> and does not leak into the default bucket.</param>
	/// <param name="methodSetup">The method setup produced by the fluent <c>Setup.MethodName(...)</c> API.</param>
	private void SetupMethod(string scenario, MethodSetup methodSetup)
		=> Setup.GetOrCreateScenario(scenario).Methods.Add(methodSetup);

	/// <summary>
	///     Registers <paramref name="methodSetup" /> for the default scenario by indexing it under the
	///     generator-emitted <paramref name="memberId" /> for fast dispatch from the proxy method body.
	/// </summary>
	/// <remarks>
	///     The <paramref name="memberId" /> is a compile-time constant emitted by the source generator, one per
	///     mocked member. Reads via <see cref="GetMethodSetupSnapshot(int)" /> are lock-free; writes take an
	///     internal lock and publish a new snapshot via <see cref="Volatile.Write{T}(ref T, T)" />.
	///     The default-scope string-keyed list (<see cref="GetMethodSetups{T}(string)" />) is intentionally
	///     bypassed — the snapshot is authoritative for default-scope dispatch from generator-emitted code.
	/// </remarks>
	/// <param name="memberId">The generator-emitted member id for the setup's target member.</param>
	/// <param name="methodSetup">The method setup produced by the fluent <c>Setup.MethodName(...)</c> API.</param>
	public void SetupMethod(int memberId, MethodSetup methodSetup)
		=> AppendToMemberIdBucket(memberId, methodSetup);

	/// <summary>
	///     Registers <paramref name="methodSetup" /> for the given <paramref name="scenario" />. When
	///     <paramref name="scenario" /> is the default scope, the setup is indexed by <paramref name="memberId" />
	///     for fast dispatch instead of the string-keyed list; scenario-scoped setups continue to land in the
	///     scenario bucket only.
	/// </summary>
	/// <param name="memberId">The generator-emitted member id for the setup's target member.</param>
	/// <param name="scenario">The scenario name the setup applies to.</param>
	/// <param name="methodSetup">The method setup produced by the fluent <c>Setup.MethodName(...)</c> API.</param>
	public void SetupMethod(int memberId, string scenario, MethodSetup methodSetup)
	{
		if (string.IsNullOrEmpty(scenario))
		{
			AppendToMemberIdBucket(memberId, methodSetup);
		}
		else
		{
			SetupMethod(scenario, methodSetup);
		}
	}

	private void AppendToMemberIdBucket(int memberId, MethodSetup methodSetup)
	{
		lock (SetupsByMemberIdLock)
		{
			MethodSetup[]?[]? oldTable = _setupsByMemberId;
			int requiredLen = memberId + 1;
			// Stryker disable once Conditional : when oldTable is not null and requiredLen <= oldTable.Length, the resize-condition below is false either way and the else-branch reuses oldTable; newLen is unread, so collapsing the ternary to requiredLen is observationally identical.
			int newLen = oldTable is null ? Math.Max(requiredLen, GetMemberCountHint()) : Math.Max(oldTable.Length, requiredLen);
			MethodSetup[]?[] newTable;
			// Stryker disable once Equality : flipping > to >= only fires the realloc-and-copy path when newLen == oldTable.Length, producing a fresh table with identical contents — externally indistinguishable from reusing oldTable.
			if (oldTable is null || newLen > oldTable.Length)
			{
				newTable = new MethodSetup[newLen][];
				if (oldTable is not null)
				{
					Array.Copy(oldTable, newTable, oldTable.Length);
				}
			}
			else
			{
				newTable = oldTable;
			}

			MethodSetup[]? existing = newTable[memberId];
			MethodSetup[] bucket;
			if (existing is null || existing.Length == 0)
			{
				bucket = new[]
				{
					methodSetup,
				};
			}
			else
			{
				bucket = new MethodSetup[existing.Length + 1];
				Array.Copy(existing, bucket, existing.Length);
				bucket[existing.Length] = methodSetup;
			}

			newTable[memberId] = bucket;
			Volatile.Write(ref _setupsByMemberId, newTable);
		}
	}

	/// <summary>
	///     Registers <paramref name="propertySetup" /> for the default scenario.
	/// </summary>
	/// <param name="propertySetup">The property setup produced by the fluent <c>Setup.PropertyName</c> API.</param>
	internal void SetupProperty(PropertySetup propertySetup)
		=> SetupProperty("", propertySetup);

	/// <summary>
	///     Registers <paramref name="propertySetup" /> for the given <paramref name="scenario" />.
	/// </summary>
	/// <param name="scenario">The scenario name the setup applies to. The setup only applies while <see cref="Scenario" /> equals <paramref name="scenario" /> and does not leak into the default bucket.</param>
	/// <param name="propertySetup">The property setup produced by the fluent <c>Setup.PropertyName</c> API.</param>
	private void SetupProperty(string scenario, PropertySetup propertySetup)
		=> Setup.GetOrCreateScenario(scenario).Properties.Add(propertySetup);

	/// <summary>
	///     Registers <paramref name="propertySetup" /> for the default scenario and additionally indexes it by the
	///     generator-emitted <paramref name="memberId" /> for fast dispatch from the proxy property body.
	/// </summary>
	/// <remarks>
	///     The <paramref name="memberId" /> is a compile-time constant emitted by the source generator, one per
	///     mocked property accessor. Reads of the lock-free snapshot table are paired with writes that take an
	///     internal lock and publish via <see cref="Volatile.Write{T}(ref T, T)" />. A non-default setup never
	///     overrides itself with a <see cref="PropertySetup.Default" /> placeholder — mirroring the rules in
	///     <see cref="MockSetups.PropertySetups.Add" />.
	/// </remarks>
	/// <param name="memberId">The generator-emitted member id for the setup's target property.</param>
	/// <param name="propertySetup">The property setup produced by the fluent <c>Setup.PropertyName</c> API.</param>
	public void SetupProperty(int memberId, PropertySetup propertySetup)
	{
		SetupProperty(propertySetup);
		PublishPropertyToMemberIdBucket(memberId, propertySetup);
	}

	/// <summary>
	///     Registers <paramref name="propertySetup" /> for the given <paramref name="scenario" />. When
	///     <paramref name="scenario" /> is the default scope, the setup is additionally indexed by
	///     <paramref name="memberId" /> for fast dispatch.
	/// </summary>
	/// <param name="memberId">The generator-emitted member id for the setup's target property.</param>
	/// <param name="scenario">The scenario name the setup applies to.</param>
	/// <param name="propertySetup">The property setup produced by the fluent <c>Setup.PropertyName</c> API.</param>
	public void SetupProperty(int memberId, string scenario, PropertySetup propertySetup)
	{
		SetupProperty(scenario, propertySetup);
		if (string.IsNullOrEmpty(scenario))
		{
			PublishPropertyToMemberIdBucket(memberId, propertySetup);
		}
	}

	private void PublishPropertyToMemberIdBucket(int memberId, PropertySetup propertySetup)
	{
		lock (SetupsByMemberIdLock)
		{
			PropertySetup?[]? oldTable = _propertySetupsByMemberId;
			int requiredLen = memberId + 1;
			// Stryker disable once Conditional : when oldTable is not null and requiredLen <= oldTable.Length, the resize-condition below is false either way and the else-branch reuses oldTable; newLen is unread, so collapsing the ternary to requiredLen is observationally identical.
			int newLen = oldTable is null ? Math.Max(requiredLen, GetMemberCountHint()) : Math.Max(oldTable.Length, requiredLen);
			PropertySetup?[] newTable;
			// Stryker disable once Equality : flipping > to >= only fires the realloc-and-copy path when newLen == oldTable.Length, producing a fresh table with identical contents — externally indistinguishable from reusing oldTable.
			if (oldTable is null || newLen > oldTable.Length)
			{
				newTable = new PropertySetup?[newLen];
				if (oldTable is not null)
				{
					Array.Copy(oldTable, newTable, oldTable.Length);
				}
			}
			else
			{
				newTable = oldTable;
			}

			PropertySetup? existing = newTable[memberId];
			// Mirror MockSetups.PropertySetups.Add: never overwrite a user-configured setup with a default placeholder.
			if (existing is not null && existing is not PropertySetup.Default && propertySetup is PropertySetup.Default)
			{
				return;
			}

			newTable[memberId] = propertySetup;
			Volatile.Write(ref _propertySetupsByMemberId, newTable);
		}
	}

	/// <summary>
	///     Registers <paramref name="eventSetup" /> for the default scenario.
	/// </summary>
	/// <param name="eventSetup">The event setup produced by the fluent <c>Setup.EventName</c> API.</param>
	internal void SetupEvent(EventSetup eventSetup)
		=> SetupEvent("", eventSetup);

	/// <summary>
	///     Registers <paramref name="eventSetup" /> for the given <paramref name="scenario" />.
	/// </summary>
	/// <param name="scenario">The scenario name the setup applies to. The setup only applies while <see cref="Scenario" /> equals <paramref name="scenario" /> and does not leak into the default bucket.</param>
	/// <param name="eventSetup">The event setup produced by the fluent <c>Setup.EventName</c> API.</param>
	private void SetupEvent(string scenario, EventSetup eventSetup)
		=> Setup.GetOrCreateScenario(scenario).Events.Add(eventSetup);

	/// <summary>
	///     Registers <paramref name="eventSetup" /> for the default scenario by indexing it under the
	///     generator-emitted subscribe-side <paramref name="memberId" /> for fast dispatch from the proxy event
	///     subscribe/unsubscribe body.
	/// </summary>
	/// <remarks>
	///     The <paramref name="memberId" /> is the subscribe-side member id emitted by the source generator. A
	///     single <see cref="EventSetup" /> typically wires both subscribe and unsubscribe behavior, so the bucket
	///     is keyed off the subscribe id. Reads via <see cref="GetEventSetupSnapshot(int)" /> are lock-free; writes
	///     take an internal lock and publish a new snapshot via <see cref="Volatile.Write{T}(ref T, T)" />.
	///     The default-scope string-keyed list is intentionally bypassed — the snapshot is authoritative for
	///     default-scope subscribe dispatch. The unsubscribe path falls through to a name-filtered scan of the
	///     snapshot table inside <c>GetEventSetupsByName</c> (see <see cref="RemoveEvent(int, string, object?, System.Reflection.MethodInfo?)" />).
	/// </remarks>
	/// <param name="memberId">The generator-emitted subscribe-side member id for the setup's target event.</param>
	/// <param name="eventSetup">The event setup produced by the fluent <c>Setup.EventName</c> API.</param>
	public void SetupEvent(int memberId, EventSetup eventSetup)
		=> AppendToEventMemberIdBucket(memberId, eventSetup);

	/// <summary>
	///     Registers <paramref name="eventSetup" /> for the given <paramref name="scenario" />. When
	///     <paramref name="scenario" /> is the default scope, the setup is indexed by <paramref name="memberId" />
	///     for fast dispatch instead of the string-keyed list; scenario-scoped setups continue to land in the
	///     scenario bucket only.
	/// </summary>
	/// <param name="memberId">The generator-emitted subscribe-side member id for the setup's target event.</param>
	/// <param name="scenario">The scenario name the setup applies to.</param>
	/// <param name="eventSetup">The event setup produced by the fluent <c>Setup.EventName</c> API.</param>
	public void SetupEvent(int memberId, string scenario, EventSetup eventSetup)
	{
		if (string.IsNullOrEmpty(scenario))
		{
			AppendToEventMemberIdBucket(memberId, eventSetup);
		}
		else
		{
			SetupEvent(scenario, eventSetup);
		}
	}

	private void AppendToEventMemberIdBucket(int memberId, EventSetup eventSetup)
	{
		lock (SetupsByMemberIdLock)
		{
			EventSetup[]?[]? oldTable = _eventSetupsByMemberId;
			int requiredLen = memberId + 1;
			// Stryker disable once Conditional : when oldTable is not null and requiredLen <= oldTable.Length, the resize-condition below is false either way and the else-branch reuses oldTable; newLen is unread, so collapsing the ternary to requiredLen is observationally identical.
			int newLen = oldTable is null ? Math.Max(requiredLen, GetMemberCountHint()) : Math.Max(oldTable.Length, requiredLen);
			EventSetup[]?[] newTable;
			// Stryker disable once Equality : flipping > to >= only fires the realloc-and-copy path when newLen == oldTable.Length, producing a fresh table with identical contents — externally indistinguishable from reusing oldTable.
			if (oldTable is null || newLen > oldTable.Length)
			{
				newTable = new EventSetup[newLen][];
				if (oldTable is not null)
				{
					Array.Copy(oldTable, newTable, oldTable.Length);
				}
			}
			else
			{
				newTable = oldTable;
			}

			EventSetup[]? existing = newTable[memberId];
			EventSetup[] bucket;
			if (existing is null || existing.Length == 0)
			{
				bucket = new[]
				{
					eventSetup,
				};
			}
			else
			{
				bucket = new EventSetup[existing.Length + 1];
				Array.Copy(existing, bucket, existing.Length);
				bucket[existing.Length] = eventSetup;
			}

			newTable[memberId] = bucket;
			Volatile.Write(ref _eventSetupsByMemberId, newTable);
		}
	}
}
