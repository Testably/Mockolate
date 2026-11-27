using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using Mockolate.Internals;

namespace Mockolate.Setup;

[DebuggerDisplay("{ToString()}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
internal partial class MockSetups : MockScenarioSetup
{
	private static readonly object NoAutoMock = new();
	private ConcurrentDictionary<(int MemberId, int Element, Type Type), object>? _autoMocks;

	internal Dictionary<string, MockScenarioSetup>? Scenarios { get; private set; }

	/// <summary>
	///     Returns the auto-mock of the <paramref name="type" /> cached for the <paramref name="element" /> of
	///     <paramref name="memberId" />, or creates and caches one, so that the member keeps returning the same
	///     configurable mock.
	/// </summary>
	/// <remarks>
	///     The mock is created outside of any lock, because creating it runs user callbacks (e.g. of
	///     <c>Initialize&lt;T&gt;</c>). When two threads race, both create a mock, but both return the first one stored.
	///     A type without a generated mock is remembered as well, so that the factory is not asked again. The type is
	///     part of the key, because mocks that share their setups (e.g. an <c>HttpClient</c> and its handler) number
	///     their members independently.
	/// </remarks>
	public object? GetOrAddAutoMock(int memberId, int element, Type type, MockBehavior behavior)
	{
		ConcurrentDictionary<(int MemberId, int Element, Type Type), object>? autoMocks = Volatile.Read(ref _autoMocks);
		if (autoMocks is null)
		{
			ConcurrentDictionary<(int MemberId, int Element, Type Type), object> created = new();
			autoMocks = Interlocked.CompareExchange(ref _autoMocks, created, comparand: null) ?? created;
		}

		if (!autoMocks.TryGetValue((memberId, element, type), out object? stored))
		{
			stored = autoMocks.GetOrAdd((memberId, element, type), AutoMocks.Create(type, behavior) ?? NoAutoMock);
		}

		return ReferenceEquals(stored, NoAutoMock) ? null : stored;
	}

	public MockScenarioSetup GetOrCreateScenario(string setupScenario)
	{
		if (string.IsNullOrEmpty(setupScenario))
		{
			return this;
		}

		Scenarios ??= new Dictionary<string, MockScenarioSetup>();
		if (Scenarios.TryGetValue(setupScenario, out MockScenarioSetup? scenario))
		{
			return scenario;
		}

		scenario = new MockScenarioSetup();
		Scenarios.Add(setupScenario, scenario);
		return scenario;
	}

	public bool TryGetScenario(string setupScenario, [NotNullWhen(true)] out MockScenarioSetup? scenario)
	{
		if (string.IsNullOrEmpty(setupScenario))
		{
			scenario = this;
			return true;
		}

		if (Scenarios is not null && Scenarios.TryGetValue(setupScenario, out scenario))
		{
			return true;
		}

		scenario = null;
		return false;
	}
}

internal class MockScenarioSetup
{
	private MockSetups.EventSetups? _events;
	private MockSetups.IndexerSetups? _indexers;
	private MockSetups.MethodSetups? _methods;
	private MockSetups.PropertySetups? _properties;

	internal MockSetups.EventSetups Events
	{
		get
		{
			if (_events is null)
			{
				Interlocked.CompareExchange(ref _events, new MockSetups.EventSetups(), comparand: null);
			}

			return _events!;
		}
	}

	internal MockSetups.IndexerSetups Indexers
	{
		get
		{
			if (_indexers is null)
			{
				Interlocked.CompareExchange(ref _indexers, new MockSetups.IndexerSetups(), comparand: null);
			}

			return _indexers!;
		}
	}

	internal MockSetups.MethodSetups Methods
	{
		get
		{
			if (_methods is null)
			{
				Interlocked.CompareExchange(ref _methods, new MockSetups.MethodSetups(), comparand: null);
			}

			return _methods!;
		}
	}

	internal MockSetups.PropertySetups Properties
	{
		get
		{
			if (_properties is null)
			{
				Interlocked.CompareExchange(ref _properties, new MockSetups.PropertySetups(), comparand: null);
			}

			return _properties!;
		}
	}

	/// <inheritdoc cref="object.ToString()" />
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override string ToString()
	{
		StringBuilder sb = new();
		int methodCount = _methods?.Count ?? 0;
		if (methodCount > 0)
		{
			sb.Append(methodCount).Append(methodCount == 1 ? " method, " : " methods, ");
		}

		int propertyCount = _properties?.Count ?? 0;
		if (propertyCount > 0)
		{
			sb.Append(propertyCount).Append(propertyCount == 1 ? " property, " : " properties, ");
		}

		int indexerCount = _indexers?.Count ?? 0;
		if (indexerCount > 0)
		{
			sb.Append(indexerCount).Append(indexerCount == 1 ? " indexer, " : " indexers, ");
		}

		int eventCount = _events?.Count ?? 0;
		if (eventCount > 0)
		{
			sb.Append(eventCount).Append(eventCount == 1 ? " event, " : " events, ");
		}

		if (sb.Length == 0)
		{
			return "no setups";
		}

		sb.Length -= 2;
		return sb.ToString();
	}
}
