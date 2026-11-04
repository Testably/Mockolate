using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Setup;

namespace Mockolate.Internals;

public partial class MockRegistry
{
	private IMockInteractions? _interactions;
	private readonly int _interactionMemberCount;

	/// <summary>
	///     Gets the collection of interactions recorded by the mock object.
	/// </summary>
	public IMockInteractions Interactions => _interactions ?? EnsureInteractions();

	private IMockInteractions EnsureInteractions()
	{
		Interlocked.CompareExchange(ref _interactions,
			new FastMockInteractions(_interactionMemberCount, Behavior.SkipInteractionRecording), null);
		return _interactions!;
	}

	/// <summary>
	///     Clears all interactions recorded by the mock object.
	/// </summary>
	public void ClearAllInteractions()
		=> Interactions.Clear();

	/// <summary>
	///     Returns the current snapshot of default-scope method setups registered under the generator-emitted
	///     <paramref name="memberId" />, or <see langword="null" /> when no setup has been registered.
	/// </summary>
	/// <remarks>
	///     The returned array is immutable — callers may iterate it without a lock. Setups are appended in
	///     registration order, so callers interested in latest-registered-first should walk the array in reverse.
	///     This accessor only sees setups registered via the <c>SetupMethod(int, ...)</c> overloads; scenario-scoped
	///     and legacy string-keyed registrations are retrieved via <see cref="GetMethodSetups{T}(string)" />.
	/// </remarks>
	/// <param name="memberId">The generator-emitted member id.</param>
	/// <returns>The setups array for the member, or <see langword="null" /> when none are registered.</returns>
	public MethodSetup[]? GetMethodSetupSnapshot(int memberId)
	{
		MethodSetup[]?[]? table = Volatile.Read(ref _setupsByMemberId);
		if (table is null || (uint)memberId >= (uint)table.Length)
		{
			return null;
		}

		return table[memberId];
	}

	/// <summary>
	///     Returns the current snapshot of default-scope indexer setups registered under the generator-emitted
	///     <paramref name="memberId" />, or <see langword="null" /> when no setup has been registered.
	/// </summary>
	/// <remarks>
	///     The returned array is immutable — callers may iterate it without a lock. Setups are appended in
	///     registration order, so callers walking for latest-registered-first should iterate in reverse.
	/// </remarks>
	/// <param name="memberId">The generator-emitted member id.</param>
	/// <returns>The setups array for the member, or <see langword="null" /> when none are registered.</returns>
	public IndexerSetup[]? GetIndexerSetupSnapshot(int memberId)
	{
		IndexerSetup[]?[]? table = Volatile.Read(ref _indexerSetupsByMemberId);
		if (table is null || (uint)memberId >= (uint)table.Length)
		{
			return null;
		}

		return table[memberId];
	}

	/// <summary>
	///     Returns the current snapshot of default-scope event setups registered under the generator-emitted
	///     subscribe-side <paramref name="memberId" />, or <see langword="null" /> when no setup has been registered.
	/// </summary>
	/// <remarks>
	///     The returned array is immutable — callers may iterate it without a lock. Setups are appended in
	///     registration order. Both subscribe and unsubscribe dispatch consult this snapshot under the
	///     subscribe-side member id; the bucket is shared because a single <see cref="EventSetup" /> wires both
	///     directions.
	/// </remarks>
	/// <param name="memberId">The generator-emitted subscribe-side member id.</param>
	/// <returns>The setups array for the event, or <see langword="null" /> when none are registered.</returns>
	internal EventSetup[]? GetEventSetupSnapshot(int memberId)
	{
		EventSetup[]?[]? table = Volatile.Read(ref _eventSetupsByMemberId);
		if (table is null || (uint)memberId >= (uint)table.Length)
		{
			return null;
		}

		return table[memberId];
	}

	/// <summary>
	///     Returns the current default-scope property setup registered under the generator-emitted
	///     <paramref name="memberId" />, or <see langword="null" /> when no setup has been registered.
	/// </summary>
	/// <remarks>
	///     Property dispatch reads the snapshot via <see cref="GetPropertyFast{TResult}(int, PropertyGetterAccess, System.Func{Mockolate.MockBehavior,TResult}, Func{TResult}?)" />
	///     and falls back to the cold path when the snapshot is empty, so this accessor is intended for
	///     diagnostics and tests that need to verify the fast-path table directly.
	/// </remarks>
	/// <param name="memberId">The generator-emitted member id for the property accessor.</param>
	/// <returns>The setup for the property, or <see langword="null" /> when none is registered.</returns>
	internal PropertySetup? GetPropertySetupSnapshot(int memberId)
	{
		PropertySetup?[]? table = Volatile.Read(ref _propertySetupsByMemberId);
		if (table is null || (uint)memberId >= (uint)table.Length)
		{
			return null;
		}

		return table[memberId];
	}

	/// <summary>
	///     Enumerates method setups of type <typeparamref name="T" /> matching <paramref name="methodName" />
	///     in latest-registered-first order, scenario-scoped setups before default-scope setups.
	/// </summary>
	/// <remarks>
	///     The caller iterates and evaluates each setup's matcher on the stack (passing ref-struct
	///     values where applicable), so no predicate closure is captured.
	///     Order: scenario-scoped dict (newest first), then default-scope memberId-keyed snapshot
	///     entries (newest first within each bucket, with buckets visited in memberId/index order),
	///     then default-scope hand-written dict entries. The caller is expected to stop on the first
	///     match so scenarios override the default scope.
	///     When the call stays entirely on the default scope and neither the snapshot table nor the
	///     dict has any entries, the empty-storage fast path returns <see cref="Array.Empty{T}" />
	///     directly so the dispatch hot path skips an iterator state-machine allocation per call.
	/// </remarks>
	/// <typeparam name="T">The concrete <see cref="MethodSetup" /> subtype to return.</typeparam>
	/// <param name="methodName">The simple method name.</param>
	/// <returns>A lazy stream of matching setups, scenario-scoped first.</returns>
	public IEnumerable<T> GetMethodSetups<T>(string methodName) where T : MethodSetup
	{
		MockSetups? setups = _setups;
		if (!string.IsNullOrEmpty(Scenario) && setups is not null &&
		    setups.TryGetScenario(Scenario, out MockScenarioSetup? scopedBucket))
		{
			return EnumerateScopedAndGlobalMethodSetups<T>(methodName, scopedBucket);
		}

		MethodSetup[]?[]? snapshot = Volatile.Read(ref _setupsByMemberId);
		if (snapshot is null)
		{
			return setups is null ? Array.Empty<T>() : setups.Methods.EnumerateByName<T>(methodName);
		}

		return EnumerateGlobalMethodSetups<T>(methodName, snapshot);
	}

	private IEnumerable<T> EnumerateScopedAndGlobalMethodSetups<T>(string methodName,
		MockScenarioSetup scopedBucket) where T : MethodSetup
	{
		foreach (T setup in scopedBucket.Methods.EnumerateByName<T>(methodName))
		{
			yield return setup;
		}

		MethodSetup[]?[]? snapshot = Volatile.Read(ref _setupsByMemberId);
		if (snapshot is not null)
		{
			foreach (T setup in EnumerateSnapshotByName<T>(methodName, snapshot))
			{
				yield return setup;
			}
		}

		foreach (T setup in Setup.Methods.EnumerateByName<T>(methodName))
		{
			yield return setup;
		}
	}

	private IEnumerable<T> EnumerateGlobalMethodSetups<T>(string methodName,
		MethodSetup[]?[] snapshot) where T : MethodSetup
	{
		foreach (T setup in EnumerateSnapshotByName<T>(methodName, snapshot))
		{
			yield return setup;
		}

		// Hand-written SetupMethod(MethodSetup) entries (e.g. the HttpClientExtensions pipeline) live
		// only in the root dict; when no MockSetups has been allocated there can be none, so the loop
		// is skipped entirely and never forces the lazy allocation.
		if (_setups is { } setups)
		{
			foreach (T setup in setups.Methods.EnumerateByName<T>(methodName))
			{
				yield return setup;
			}
		}
	}

	private static IEnumerable<T> EnumerateSnapshotByName<T>(string methodName,
		MethodSetup[]?[] snapshot) where T : MethodSetup
	{
		// Walk every memberId bucket: the snapshot is keyed by member id, but a name-based caller
		// (scenario-active dispatch, ref-struct dispatch) doesn't know the id, so we filter by Name
		// after the type-test. The outer scan is proportional to snapshot.Length (the memberId table
		// size / mocked member count), not interaction count; the table may be sparse for a method.
		for (int b = 0; b < snapshot.Length; b++)
		{
			MethodSetup[]? bucket = snapshot[b];
			if (bucket is null)
			{
				continue;
			}

			for (int i = bucket.Length - 1; i >= 0; i--)
			{
				if (bucket[i] is T typed && typed.Name.Equals(methodName))
				{
					yield return typed;
				}
			}
		}
	}

	/// <summary>
	///     Returns the latest registered indexer setup of type <typeparamref name="T" /> that satisfies
	///     <paramref name="predicate" />, or <see langword="null" /> when no setup matches. Scenario-scoped setups
	///     take precedence over default-scope setups.
	/// </summary>
	/// <typeparam name="T">The concrete <see cref="IndexerSetup" /> subtype to return.</typeparam>
	/// <param name="predicate">Argument matcher applied to each candidate setup.</param>
	/// <returns>The matching setup, or <see langword="null" /> when none was found.</returns>
	internal T? GetIndexerSetup<T>(Func<T, bool> predicate) where T : IndexerSetup
	{
		// Stryker disable once String : TryGetScenario returns the root bucket when given a null or empty name, so replacing the IsNullOrEmpty guard with a null/empty comparison just re-searches the same global Indexers and produces the same result.
		if (!string.IsNullOrEmpty(Scenario) &&
		    Setup.TryGetScenario(Scenario, out MockScenarioSetup? scopedBucket))
		{
			T? scoped = scopedBucket.Indexers.GetMatching(predicate);
			if (scoped is not null)
			{
				return scoped;
			}
		}

		T? snapshot = GetMatchingIndexerSetupFromSnapshot(predicate);
		if (snapshot is not null)
		{
			return snapshot;
		}

		return Setup.Indexers.GetMatching(predicate);
	}

	/// <summary>
	///     Returns the latest registered indexer setup of type <typeparamref name="T" /> that matches the
	///     <paramref name="access" />, or <see langword="null" /> when no setup matches. Scenario-scoped setups take
	///     precedence over default-scope setups.
	/// </summary>
	/// <typeparam name="T">The concrete <see cref="IndexerSetup" /> subtype to return.</typeparam>
	/// <param name="access">The indexer access whose argument values must be matched.</param>
	/// <returns>The matching setup, or <see langword="null" /> when none was found.</returns>
	public T? GetIndexerSetup<T>(IndexerAccess access) where T : IndexerSetup
	{
		if (!string.IsNullOrEmpty(Scenario) &&
		    Setup.TryGetScenario(Scenario, out MockScenarioSetup? scopedBucket))
		{
			T? scoped = scopedBucket.Indexers.GetMatching<T>(access);
			if (scoped is not null)
			{
				return scoped;
			}
		}

		T? snapshot = GetMatchingIndexerSetupFromSnapshot<T>(access);
		if (snapshot is not null)
		{
			return snapshot;
		}

		return Setup.Indexers.GetMatching<T>(access);
	}

	private T? GetMatchingIndexerSetupFromSnapshot<T>(Func<T, bool> predicate) where T : IndexerSetup
	{
		IndexerSetup[]?[]? table = Volatile.Read(ref _indexerSetupsByMemberId);
		if (table is null)
		{
			return null;
		}

		// Walk every memberId bucket: the snapshot is keyed by member id, but predicate-based callers
		// don't know which id to consult, so we filter by type then by predicate. Buckets are walked
		// by ascending memberId/index order; within each bucket, reverse iteration means the latest
		// registration wins.
		for (int b = 0; b < table.Length; b++)
		{
			IndexerSetup[]? bucket = table[b];
			if (bucket is null)
			{
				continue;
			}

			for (int i = bucket.Length - 1; i >= 0; i--)
			{
				if (bucket[i] is T typed && predicate(typed))
				{
					return typed;
				}
			}
		}

		return null;
	}

	private T? GetMatchingIndexerSetupFromSnapshot<T>(IndexerAccess access) where T : IndexerSetup
	{
		IndexerSetup[]?[]? table = Volatile.Read(ref _indexerSetupsByMemberId);
		if (table is null)
		{
			return null;
		}

		for (int b = 0; b < table.Length; b++)
		{
			IndexerSetup[]? bucket = table[b];
			if (bucket is null)
			{
				continue;
			}

			for (int i = bucket.Length - 1; i >= 0; i--)
			{
				if (bucket[i] is T typed &&
				    ((IInteractiveIndexerSetup)typed).Matches(access))
				{
					return typed;
				}
			}
		}

		return null;
	}

	/// <summary>
	///     Handles the no-matching-setup fallback for an indexer getter: returns any previously stored value, throws
	///     when <see cref="MockBehavior.ThrowWhenNotSetup" /> is <see langword="true" />, or otherwise stores and
	///     returns the <see cref="MockBehavior.DefaultValue" />.
	/// </summary>
	/// <typeparam name="TResult">The indexer's value type.</typeparam>
	/// <param name="access">The indexer access whose arguments identify the slot.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns>The stored value, or the freshly generated default.</returns>
	/// <exception cref="MockNotSetupException"><see cref="MockBehavior.ThrowWhenNotSetup" /> is <see langword="true" /> and no value was previously stored for this access.</exception>
	public TResult GetIndexerFallback<TResult>(IndexerAccess access, int signatureIndex)
	{
		access.AttachStorage(Setup.Indexers.GetOrCreateStorage<TResult>(signatureIndex));
		if (access.TryFindStoredValue(out TResult stored))
		{
			return stored;
		}

		if (Behavior.ThrowWhenNotSetup)
		{
			throw new MockNotSetupException($"{access} was accessed without prior setup.");
		}

		TResult value = Behavior.DefaultValue.GenerateTyped<TResult>();
		access.StoreValue(value);
		return value;
	}

	/// <summary>
	///     Invokes the getter flow of the given <paramref name="setup" /> for the given <paramref name="access" />,
	///     ensuring the indexer value storage is wired up before dispatching.
	/// </summary>
	/// <typeparam name="TResult">The indexer's value type.</typeparam>
	/// <param name="access">The indexer access whose arguments identify the slot.</param>
	/// <param name="setup">The previously matched indexer setup.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns>The value produced by the setup.</returns>
	public TResult ApplyIndexerSetup<TResult>(IndexerAccess access, IndexerSetup setup, int signatureIndex)
	{
		access.AttachStorage(Setup.Indexers.GetOrCreateStorage<TResult>(signatureIndex));
		return setup.GetResult<TResult>(access, Behavior);
	}

	/// <summary>
	///     Applies an indexer getter using a pre-computed <paramref name="baseValue" /> and an optional matching
	///     <paramref name="setup" />. Returns the final getter result.
	/// </summary>
	/// <remarks>
	///     When <paramref name="setup" /> is <see langword="null" />, returns any previously stored value or the
	///     <paramref name="baseValue" /> (which is then stored).
	/// </remarks>
	/// <typeparam name="TResult">The indexer's value type.</typeparam>
	/// <param name="access">The indexer access whose arguments identify the slot.</param>
	/// <param name="setup">The matching indexer setup, or <see langword="null" /> to fall through to stored/base value.</param>
	/// <param name="baseValue">Value returned by the base-class indexer (or a caller-supplied default).</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns>The final getter result.</returns>
	public TResult ApplyIndexerGetter<TResult>(IndexerAccess access, IndexerSetup? setup, TResult baseValue,
		int signatureIndex)
	{
		access.AttachStorage(Setup.Indexers.GetOrCreateStorage<TResult>(signatureIndex));
		if (setup is null)
		{
			if (access.TryFindStoredValue(out TResult stored))
			{
				return stored;
			}

			access.StoreValue(baseValue);
			return baseValue;
		}

		return setup.GetResult(access, Behavior, baseValue);
	}

	/// <summary>
	///     Applies an indexer getter using a lazy <paramref name="defaultValueGenerator" /> and an optional matching
	///     <paramref name="setup" />. Returns the final getter result.
	/// </summary>
	/// <remarks>
	///     When <paramref name="setup" /> is <see langword="null" />, returns any previously stored value, or (if
	///     <see cref="MockBehavior.ThrowWhenNotSetup" /> is <see langword="true" />) throws a
	///     <see cref="MockNotSetupException" />, otherwise stores and returns the <paramref name="defaultValueGenerator" />'s
	///     value.
	/// </remarks>
	/// <typeparam name="TResult">The indexer's value type.</typeparam>
	/// <param name="access">The indexer access whose arguments identify the slot.</param>
	/// <param name="setup">The matching indexer setup, or <see langword="null" /> to fall through to stored/default value.</param>
	/// <param name="defaultValueGenerator">Lazy producer of the default value &#8212; only invoked when a default is actually needed.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns>The final getter result.</returns>
	/// <exception cref="MockNotSetupException"><paramref name="setup" /> is <see langword="null" />, <see cref="MockBehavior.ThrowWhenNotSetup" /> is <see langword="true" />, and no value was previously stored.</exception>
	internal TResult ApplyIndexerGetter<TResult>(IndexerAccess access, IndexerSetup? setup,
		Func<TResult> defaultValueGenerator, int signatureIndex)
	{
		access.AttachStorage(Setup.Indexers.GetOrCreateStorage<TResult>(signatureIndex));
		if (setup is null)
		{
			if (access.TryFindStoredValue(out TResult stored))
			{
				return stored;
			}

			if (Behavior.ThrowWhenNotSetup)
			{
				throw new MockNotSetupException($"{access} was accessed without prior setup.");
			}

			TResult value = defaultValueGenerator();
			access.StoreValue(value);
			return value;
		}

		return setup.GetResult(access, Behavior, defaultValueGenerator);
	}

	/// <summary>
	///     Applies an indexer setter for the given <paramref name="access" /> with the given <paramref name="value" />
	///     and optional matching <paramref name="setup" />.
	/// </summary>
	/// <typeparam name="TResult">The indexer's value type.</typeparam>
	/// <param name="access">The indexer access whose arguments identify the slot.</param>
	/// <param name="setup">The matching indexer setup, or <see langword="null" /> to simply store the value.</param>
	/// <param name="value">The value being assigned.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns><see langword="true" /> when the base-class setter should be skipped.</returns>
	public bool ApplyIndexerSetter<TResult>(IndexerAccess access, IndexerSetup? setup, TResult value,
		int signatureIndex)
	{
		access.AttachStorage(Setup.Indexers.GetOrCreateStorage<TResult>(signatureIndex));
		if (setup is null)
		{
			access.StoreValue(value);
			return Behavior.SkipBaseClass;
		}

		setup.SetResult(access, Behavior, value);
		return setup.SkipBaseClass() ?? Behavior.SkipBaseClass;
	}

	/// <summary>
	///     Appends <paramref name="interaction" /> to the mock's recorded interactions.
	/// </summary>
	/// <remarks>
	///     Has no effect when <see cref="MockBehavior.SkipInteractionRecording" /> is <see langword="true" />.
	/// </remarks>
	/// <param name="interaction">The recorded interaction to append.</param>
	public void RegisterInteraction(IInteraction interaction)
	{
		if (!Behavior.SkipInteractionRecording)
		{
			Interactions.RegisterInteraction(interaction);
		}
	}

	private IEnumerable<EventSetup> GetEventSetupsByName(string name)
	{
		// Scenario-scoped setups override default scope when at least one matches the name. The
		// underlying GetByName already returns a materialized List, so a Count check is cheaper than
		// the lazy "did we yield anything?" flag pattern this used to track.
		if (!string.IsNullOrEmpty(Scenario) &&
		    Setup.TryGetScenario(Scenario, out MockScenarioSetup? scopedBucket))
		{
			List<EventSetup> scoped = scopedBucket.Events.GetByName(name);
			if (scoped.Count > 0)
			{
				return scoped;
			}
		}

		return EnumerateDefaultScopeEventSetupsByName(name);
	}

	/// <summary>
	///     Walks the memberId-keyed snapshot table for generator-emitted setups (the
	///     <c>SetupEvent(int, ...)</c> overloads bypass the dict), then the root dict for legacy
	///     <c>SetupEvent(EventSetup)</c> entries. The unsubscribe-side dispatch always lands here
	///     because the snapshot is keyed by subscribe id only — the bucket walk reunites it with its
	///     setup.
	/// </summary>
	private IEnumerable<EventSetup> EnumerateDefaultScopeEventSetupsByName(string name)
	{
		foreach (EventSetup setup in EnumerateEventSnapshotByName(name))
		{
			yield return setup;
		}

		foreach (EventSetup setup in Setup.Events.GetByName(name))
		{
			yield return setup;
		}
	}

	private IEnumerable<EventSetup> EnumerateEventSnapshotByName(string name)
	{
		EventSetup[]?[]? table = Volatile.Read(ref _eventSetupsByMemberId);
		if (table is null)
		{
			yield break;
		}

		foreach (EventSetup[]? bucket in table)
		{
			if (bucket is null)
			{
				continue;
			}

			foreach (EventSetup setup in bucket)
			{
				if (setup.Name.Equals(name))
				{
					yield return setup;
				}
			}
		}
	}

	/// <summary>
	///     Executes the getter flow for the property named <paramref name="propertyName" />, honoring configured
	///     setups and the active <see cref="MockBehavior" />.
	/// </summary>
	/// <typeparam name="TResult">The property's value type.</typeparam>
	/// <param name="propertyName">The simple property name.</param>
	/// <param name="defaultValueGenerator">Producer of the default value when no setup supplies one.</param>
	/// <param name="baseValueAccessor">Optional accessor for the base-class getter; when <see langword="null" /> only the default/initial value is considered.</param>
	/// <returns>The resolved getter value.</returns>
	/// <exception cref="MockNotSetupException">No setup exists for the property and <see cref="MockBehavior.ThrowWhenNotSetup" /> is <see langword="true" />.</exception>
	internal TResult GetProperty<TResult>(string propertyName, Func<TResult> defaultValueGenerator,
		Func<TResult>? baseValueAccessor)
	{
		IInteraction? interaction = null;
		if (!Behavior.SkipInteractionRecording)
		{
			interaction = Interactions.RegisterInteraction(
				new PropertyGetterAccess(propertyName));
		}

		return ResolveGetterInternal(propertyName, defaultValueGenerator, baseValueAccessor, interaction);
	}

	/// <summary>
	///     Singleton-aware overload of <see cref="GetProperty{TResult}(string, Func{TResult}, Func{TResult}?)" />.
	///     Records the shared <paramref name="access" /> singleton emitted by the source generator
	///     (one static instance per non-indexer property), avoiding the per-call
	///     <see cref="PropertyGetterAccess" /> allocation. Sharing one reference across recorded
	///     accesses is safe because the only reference-keyed bookkeeping in the codebase tolerates
	///     it — the <c>_verified</c> filter is all-or-nothing per matched property, and
	///     <c>Then</c> walks the snapshot positionally rather than mapping interactions to a
	///     position via a dictionary.
	/// </summary>
	/// <typeparam name="TResult">The property's value type.</typeparam>
	/// <param name="access">The shared <see cref="PropertyGetterAccess" /> singleton for the property.</param>
	/// <param name="defaultValueGenerator">Producer of the default value when no setup supplies one.</param>
	/// <param name="baseValueAccessor">Optional accessor for the base-class getter; when <see langword="null" /> only the default/initial value is considered.</param>
	/// <returns>The resolved getter value.</returns>
	/// <exception cref="MockNotSetupException">No setup exists for the property and <see cref="MockBehavior.ThrowWhenNotSetup" /> is <see langword="true" />.</exception>
	public TResult GetProperty<TResult>(PropertyGetterAccess access, Func<TResult> defaultValueGenerator,
		Func<TResult>? baseValueAccessor)
	{
		IInteraction? interaction = null;
		if (!Behavior.SkipInteractionRecording)
		{
			interaction = Interactions.RegisterInteraction(access);
		}

		return ResolveGetterInternal(access.Name, defaultValueGenerator, baseValueAccessor, interaction);
	}

	/// <summary>
	///     Allocation-free fast-path overload of <see cref="GetProperty{TResult}(string, Func{TResult}, Func{TResult}?)" />
	///     that records the shared <paramref name="access" /> singleton emitted by the source generator,
	///     avoiding the per-call <see cref="PropertyGetterAccess" /> allocation. The
	///     <see cref="FastPropertyGetterBuffer" /> stores only a sequence number per call and emits the
	///     same singleton for every recorded record on verification; the cold-path fall-through likewise
	///     registers the singleton.
	/// </summary>
	/// <remarks>
	///     Avoids the per-call closure allocation by accepting a static <see cref="Func{T, TResult}" /> that takes
	///     the active <see cref="MockBehavior" /> as its argument — the source generator emits a <c>static</c>
	///     lambda so the C# compiler caches the delegate in a static field.
	/// </remarks>
	/// <typeparam name="TResult">The property's value type.</typeparam>
	/// <param name="memberId">The generator-emitted member id for the property getter.</param>
	/// <param name="access">The shared <see cref="PropertyGetterAccess" /> singleton for the property.</param>
	/// <param name="defaultValueGenerator">Cached factory invoked with the active <see cref="MockBehavior" /> when a default value is needed.</param>
	/// <param name="baseValueAccessor">Optional accessor for the base-class getter; when <see langword="null" /> only the default/initial value is considered. Pass <see langword="null" /> for the no-wrapping fast path.</param>
	/// <returns>The resolved getter value.</returns>
	/// <exception cref="MockNotSetupException">No setup exists for the property and <see cref="MockBehavior.ThrowWhenNotSetup" /> is <see langword="true" />.</exception>
	public TResult GetPropertyFast<TResult>(int memberId, PropertyGetterAccess access,
		Func<MockBehavior, TResult> defaultValueGenerator, Func<TResult>? baseValueAccessor = null)
	{
		if (!Behavior.SkipInteractionRecording)
		{
			RecordPropertyGetter(memberId, access);
		}

		return ResolvePropertyFast(memberId, access.Name, defaultValueGenerator, baseValueAccessor);
	}

	private TResult ResolvePropertyFast<TResult>(int memberId, string propertyName,
		Func<MockBehavior, TResult> defaultValueGenerator, Func<TResult>? baseValueAccessor)
	{
		// Hot path: setup registered via SetupProperty(int, ...), no scenario active, no base accessor.
		if (baseValueAccessor is null && string.IsNullOrEmpty(Scenario))
		{
			PropertySetup?[]? table = Volatile.Read(ref _propertySetupsByMemberId);
			if (table is not null && (uint)memberId < (uint)table.Length)
			{
				PropertySetup? snapshot = table[memberId];
				if (snapshot is not null)
				{
					if (!snapshot.IsValueInitialized)
					{
						// First read of a not-yet-initialized setup: seed it with the default value.
						// Mirrors ResolvePropertySetup's contract — without this the next read would
						// auto-init via the cold path and overwrite a value set by a prior setter.
						((IInteractivePropertySetup)snapshot).InitializeWith(defaultValueGenerator(Behavior));
					}

					return snapshot.InvokeGetterFast(Behavior, defaultValueGenerator);
				}
			}
		}

		// Cold path: scenario, uninitialized snapshot, base-class wrapping, or legacy SetupProperty overload.
		// Bridge to the existing closure-based resolver via a per-call adapter — this allocation is
		// acceptable because we only land here outside the hot path.
		MockBehavior behavior = Behavior;
		Func<TResult> bridge = () => defaultValueGenerator(behavior);
		return ResolveGetterInternal(propertyName, bridge, baseValueAccessor, null);
	}

	private void RecordPropertyGetter(int memberId, PropertyGetterAccess access)
	{
		if (Interactions is FastMockInteractions fast && (uint)memberId < (uint)fast.Buffers.Length)
		{
			FastPropertyGetterBuffer buffer = fast.GetOrCreateBuffer(
				memberId, static (f, a) => new FastPropertyGetterBuffer(f, a), access);
			buffer.Append();
			return;
		}

		Interactions.RegisterInteraction(access);
	}

	private TResult ResolveGetterInternal<TResult>(string propertyName, Func<TResult> defaultValueGenerator,
		Func<TResult>? baseValueAccessor, IInteraction? interaction)
	{
		PropertySetup matchingSetup;
		if (baseValueAccessor is null)
		{
			// Stryker disable once Boolean : forceReinit only matters when a base class accessor exists; on this branch there is none, so flipping it to true triggers an extra InitializeWith call that is gated by _isUserInitialized || _isInitialized and becomes a no-op.
			matchingSetup = ResolvePropertySetup(propertyName, defaultValueGenerator, null, false);

			return ((IInteractivePropertySetup)matchingSetup).InvokeGetter(interaction, Behavior,
				defaultValueGenerator);
		}

		ExceptionDispatchInfo? capturedBaseException = null;
		Func<TResult> safeBaseValueAccessor = () =>
		{
			try
			{
				return baseValueAccessor();
			}
			catch (Exception ex)
			{
				capturedBaseException = ExceptionDispatchInfo.Capture(ex);
				return default!;
			}
		};

		matchingSetup = ResolvePropertySetup(
			propertyName, defaultValueGenerator, safeBaseValueAccessor, true);

		TResult result = ((IInteractivePropertySetup)matchingSetup).InvokeGetter(interaction, Behavior,
			defaultValueGenerator);

		capturedBaseException?.Throw();
		return result;
	}

	/// <summary>
	///     Executes the setter flow for the property named <paramref name="propertyName" />, honoring configured
	///     setups and the active <see cref="MockBehavior" />.
	/// </summary>
	/// <remarks>
	///     Returns a flag indicating whether the base-class setter should be skipped.
	/// </remarks>
	/// <typeparam name="T">The property's value type.</typeparam>
	/// <param name="propertyName">The simple property name.</param>
	/// <param name="value">The value being assigned.</param>
	/// <returns><see langword="true" /> when the base-class setter should be skipped.</returns>
	public bool SetProperty<T>(string propertyName, T value)
	{
		IInteraction? interaction = null;
		if (!Behavior.SkipInteractionRecording)
		{
			interaction = Interactions.RegisterInteraction(
				new PropertySetterAccess<T>(propertyName, value));
		}

		PropertySetup matchingSetup = ResolvePropertySetup<T>(propertyName, null, null, false);

		((IInteractivePropertySetup)matchingSetup).InvokeSetter(interaction, value, Behavior);
		return ((IInteractivePropertySetup)matchingSetup).SkipBaseClass() ?? Behavior.SkipBaseClass;
	}

	/// <summary>
	///     Member-id-keyed overload of <see cref="SetProperty{T}(string, T)" /> that records via the typed
	///     <see cref="FastPropertySetterBuffer{T}" /> when the mock is wired to a
	///     <see cref="FastMockInteractions" />, falling back to the legacy list otherwise.
	/// </summary>
	/// <typeparam name="T">The property's value type.</typeparam>
	/// <param name="memberId">The generator-emitted member id for the property setter.</param>
	/// <param name="propertyName">The simple property name.</param>
	/// <param name="value">The value being assigned.</param>
	/// <returns><see langword="true" /> when the base-class setter should be skipped.</returns>
	internal bool SetProperty<T>(int memberId, string propertyName, T value)
	{
		if (!Behavior.SkipInteractionRecording)
		{
			RecordPropertySetter(memberId, propertyName, value);
		}

		// Stryker disable once Boolean : forceReinitWhenFound only has an observable effect when defaultValueGenerator is non-null. Here it is null, so the only side effect of flipping to true is an extra InitializeWith(null) call that early-returns under the _isUserInitialized || _isInitialized guard inside PropertySetup<T>.InitializeValue.
		PropertySetup matchingSetup = ResolvePropertySetup<T>(propertyName, null, null, false);

		((IInteractivePropertySetup)matchingSetup).InvokeSetter(null, value, Behavior);
		return ((IInteractivePropertySetup)matchingSetup).SkipBaseClass() ?? Behavior.SkipBaseClass;
	}

	/// <summary>
	///     Allocation-free fast-path overload of <see cref="SetProperty{T}(int, string, T)" /> that uses the
	///     member-id-keyed property setup snapshot for default-scope dispatch and bypasses the string-keyed
	///     dictionary scan when a snapshot setup is registered.
	/// </summary>
	/// <typeparam name="T">The property's value type.</typeparam>
	/// <param name="memberId">The generator-emitted member id for the property setter.</param>
	/// <param name="propertyName">The simple property name.</param>
	/// <param name="setterMemberId">The generator-emitted member id for the property setter accessor (for FastPropertySetterBuffer dispatch).</param>
	/// <param name="value">The value being assigned.</param>
	/// <returns><see langword="true" /> when the base-class setter should be skipped.</returns>
	public bool SetPropertyFast<T>(int memberId, int setterMemberId, string propertyName, T value)
	{
		if (!Behavior.SkipInteractionRecording)
		{
			RecordPropertySetter(setterMemberId, propertyName, value);
		}

		PropertySetup? matchingSetup = null;
		if (string.IsNullOrEmpty(Scenario))
		{
			PropertySetup?[]? table = Volatile.Read(ref _propertySetupsByMemberId);
			if (table is not null && (uint)memberId < (uint)table.Length)
			{
				matchingSetup = table[memberId];
			}
		}

		if (matchingSetup is not null)
		{
			if (!matchingSetup.IsValueInitialized)
			{
				// Mirror the OLD ResolvePropertySetup<T>(propertyName, null, null, false) call: mark the
				// setup as initialized without overwriting _value, so a subsequent reader's IsValueInitialized
				// check short-circuits to the value about to be written below.
				((IInteractivePropertySetup)matchingSetup).InitializeWith(null);
			}
		}
		else
		{
			// Stryker disable once Boolean : same equivalence as the SetProperty(int, string, T) path — defaultValueGenerator is null here, so flipping forceReinit only triggers an InitializeWith(null) call that early-returns under the _isUserInitialized || _isInitialized guard.
			matchingSetup = ResolvePropertySetup<T>(propertyName, null, null, false);
		}

		((IInteractivePropertySetup)matchingSetup).InvokeSetter(null, value, Behavior);
		return ((IInteractivePropertySetup)matchingSetup).SkipBaseClass() ?? Behavior.SkipBaseClass;
	}

	private void RecordPropertySetter<T>(int memberId, string propertyName, T value)
	{
		if (Interactions is FastMockInteractions fast && (uint)memberId < (uint)fast.Buffers.Length)
		{
			FastPropertySetterBuffer<T> buffer = fast.GetOrCreateBuffer(
				memberId, static f => new FastPropertySetterBuffer<T>(f));
			buffer.Append(propertyName, value);
			return;
		}

		Interactions.RegisterInteraction(new PropertySetterAccess<T>(propertyName, value));
	}

#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high
	private PropertySetup ResolvePropertySetup<TResult>(
		string propertyName,
		Func<TResult>? defaultValueGenerator,
		Func<TResult>? baseValueAccessor,
		bool forceReinitWhenFound)
	{
		PropertySetup? existingSetup = null;
		if (!string.IsNullOrEmpty(Scenario) &&
		    Setup.TryGetScenario(Scenario, out MockScenarioSetup? scopedBucket) &&
		    scopedBucket.Properties.TryGetValue(propertyName, out PropertySetup? scopedSetup))
		{
			existingSetup = scopedSetup;
		}

		if (existingSetup is null && !Setup.Properties.TryGetValue(propertyName, out existingSetup))
		{
			if (Behavior.ThrowWhenNotSetup)
			{
				throw new MockNotSetupException(
					$"The property '{propertyName}' was accessed without prior setup.");
			}

			TResult initialValue = GetInitialValue();
			PropertySetup setup = new PropertySetup.Default<TResult>(propertyName, initialValue);
			Setup.Properties.Add(setup);
			return setup;
		}

		if (forceReinitWhenFound || !existingSetup.IsValueInitialized)
		{
			object? initialValue;
			if (defaultValueGenerator is null)
			{
				initialValue = null;
			}
			else
			{
				bool skipBase = ((IInteractivePropertySetup)existingSetup).SkipBaseClass() ?? Behavior.SkipBaseClass;
				initialValue = skipBase || baseValueAccessor is null
					? defaultValueGenerator()
					: baseValueAccessor.Invoke();
			}

			((IInteractivePropertySetup)existingSetup).InitializeWith(initialValue);
		}

		return existingSetup;

		TResult GetInitialValue()
		{
			if (defaultValueGenerator is null)
			{
				return default!;
			}

			return Behavior.SkipBaseClass || baseValueAccessor is null
				? defaultValueGenerator()
				: baseValueAccessor.Invoke();
		}
	}
#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high

	/// <summary>
	///     Records a subscription to the event named <paramref name="name" /> and fires registered
	///     <c>OnSubscribed</c> callbacks.
	/// </summary>
	/// <param name="name">The simple event name.</param>
	/// <param name="target">The subscribing handler's target (<see langword="null" /> for static methods).</param>
	/// <param name="method">The subscribing handler's method.</param>
	/// <exception cref="MockException"><paramref name="method" /> is <see langword="null" />.</exception>
	public void AddEvent(string name, object? target, MethodInfo? method)
	{
		if (method is null)
		{
			throw new MockException("The method of an event subscription may not be null.");
		}

		if (!Behavior.SkipInteractionRecording)
		{
			Interactions.RegisterInteraction(new EventSubscription(name, target, method));
		}

		foreach (EventSetup setup in GetEventSetupsByName(name))
		{
			setup.InvokeSubscribed(target, method);
		}
	}

	/// <summary>
	///     Member-id-keyed overload of <see cref="AddEvent(string, object?, MethodInfo?)" /> that records via the
	///     typed <see cref="FastEventBuffer" /> when the mock is wired to a <see cref="FastMockInteractions" />,
	///     falling back to the legacy list otherwise.
	/// </summary>
	/// <param name="memberId">The generator-emitted member id for the event subscribe.</param>
	/// <param name="name">The simple event name.</param>
	/// <param name="target">The subscribing handler's target (<see langword="null" /> for static methods).</param>
	/// <param name="method">The subscribing handler's method.</param>
	/// <exception cref="MockException"><paramref name="method" /> is <see langword="null" />.</exception>
	public void AddEvent(int memberId, string name, object? target, MethodInfo? method)
	{
		if (method is null)
		{
			throw new MockException("The method of an event subscription may not be null.");
		}

		if (!Behavior.SkipInteractionRecording)
		{
			RecordEvent(memberId, name, target, method, true);
		}

		// Hot path: setups registered via SetupEvent(int, ...), no scenario active.
		// The snapshot is keyed off the subscribe-side member id (the value the proxy passes here),
		// so a non-null snapshot replaces the string-keyed dictionary scan entirely.
		if (string.IsNullOrEmpty(Scenario))
		{
			EventSetup[]? snapshot = GetEventSetupSnapshot(memberId);
			if (snapshot is not null)
			{
				for (int i = 0; i < snapshot.Length; i++)
				{
					snapshot[i].InvokeSubscribed(target, method);
				}

				return;
			}
		}

		foreach (EventSetup setup in GetEventSetupsByName(name))
		{
			setup.InvokeSubscribed(target, method);
		}
	}

	/// <summary>
	///     Records an unsubscription from the event named <paramref name="name" /> and fires registered
	///     <c>OnUnsubscribed</c> callbacks.
	/// </summary>
	/// <param name="name">The simple event name.</param>
	/// <param name="target">The unsubscribing handler's target (<see langword="null" /> for static methods).</param>
	/// <param name="method">The unsubscribing handler's method.</param>
	/// <exception cref="MockException"><paramref name="method" /> is <see langword="null" />.</exception>
	public void RemoveEvent(string name, object? target, MethodInfo? method)
	{
		if (method is null)
		{
			throw new MockException("The method of an event unsubscription may not be null.");
		}

		if (!Behavior.SkipInteractionRecording)
		{
			Interactions.RegisterInteraction(new EventUnsubscription(name, target, method));
		}

		foreach (EventSetup setup in GetEventSetupsByName(name))
		{
			setup.InvokeUnsubscribed(target, method);
		}
	}

	/// <summary>
	///     Member-id-keyed overload of <see cref="RemoveEvent(string, object?, MethodInfo?)" /> that records via
	///     the typed <see cref="FastEventBuffer" /> when the mock is wired to a <see cref="FastMockInteractions" />,
	///     falling back to the legacy list otherwise.
	/// </summary>
	/// <param name="memberId">The generator-emitted member id for the event unsubscribe.</param>
	/// <param name="name">The simple event name.</param>
	/// <param name="target">The unsubscribing handler's target (<see langword="null" /> for static methods).</param>
	/// <param name="method">The unsubscribing handler's method.</param>
	/// <exception cref="MockException"><paramref name="method" /> is <see langword="null" />.</exception>
	public void RemoveEvent(int memberId, string name, object? target, MethodInfo? method)
	{
		if (method is null)
		{
			throw new MockException("The method of an event unsubscription may not be null.");
		}

		if (!Behavior.SkipInteractionRecording)
		{
			RecordEvent(memberId, name, target, method, false);
		}

		// Hot path: setups registered via SetupEvent(int, ...), no scenario active. The proxy passes the
		// unsubscribe-side member id here; a snapshot is only present when the source generator emitted the
		// fast SetupEvent(int, ...) overload AND keyed it under this same id. Today only the subscribe id
		// participates, so the dictionary fall-through still serves the unsubscribe path.
		if (string.IsNullOrEmpty(Scenario))
		{
			EventSetup[]? snapshot = GetEventSetupSnapshot(memberId);
			if (snapshot is not null)
			{
				for (int i = 0; i < snapshot.Length; i++)
				{
					snapshot[i].InvokeUnsubscribed(target, method);
				}

				return;
			}
		}

		foreach (EventSetup setup in GetEventSetupsByName(name))
		{
			setup.InvokeUnsubscribed(target, method);
		}
	}

	private void RecordEvent(int memberId, string name, object? target, MethodInfo method, bool isSubscribe)
	{
		if (Interactions is FastMockInteractions fast && (uint)memberId < (uint)fast.Buffers.Length)
		{
			FastEventBuffer buffer = isSubscribe
				? fast.GetOrCreateBuffer(memberId,
					static f => new FastEventBuffer(f, FastEventBufferKind.Subscribe))
				: fast.GetOrCreateBuffer(memberId,
					static f => new FastEventBuffer(f, FastEventBufferKind.Unsubscribe));
			buffer.Append(name, target, method);
			return;
		}

		if (isSubscribe)
		{
			Interactions.RegisterInteraction(new EventSubscription(name, target, method));
		}
		else
		{
			Interactions.RegisterInteraction(new EventUnsubscription(name, target, method));
		}
	}
}
