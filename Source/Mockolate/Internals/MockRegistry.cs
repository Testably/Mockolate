using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Mockolate.Interactions;
using Mockolate.Setup;

namespace Mockolate.Internals;

/// <summary>
///     Registry for mock behavior, setups and interactions.
/// </summary>
/// <remarks>
///     It also gives access to constructor parameters and the wrapped instance.
/// </remarks>
[DebuggerDisplay("{Interactions} | {Setup}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class MockRegistry
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private ScenarioState? _scenarioState;

	/// <summary>
	///     Creates a new <see cref="MockRegistry" /> with the given <paramref name="behavior" />, a caller-provided
	///     <paramref name="interactions" /> store, and optional <paramref name="constructorParameters" />.
	/// </summary>
	/// <remarks>
	///     The generator-emitted <c>CreateMock</c> paths use this overload to install a
	///     <see cref="FastMockInteractions" /> tailored to the mocked type.
	/// </remarks>
	/// <param name="behavior">The <see cref="MockBehavior" /> that governs how the mock responds without a matching setup.</param>
	/// <param name="interactions">The interaction collection that new invocations should be appended to.</param>
	/// <param name="constructorParameters">
	///     Values forwarded to the base-class constructor, or <see langword="null" /> if no
	///     base constructor call is needed.
	/// </param>
	public MockRegistry(MockBehavior behavior, IMockInteractions interactions,
		object?[]? constructorParameters = null)
	{
		if (behavior.SkipInteractionRecording != interactions.SkipInteractionRecording)
		{
			throw new ArgumentException(
				$"""{nameof(behavior)}.{nameof(MockBehavior.SkipInteractionRecording)} ({behavior.SkipInteractionRecording}) and {nameof(interactions)}.{nameof(IMockInteractions.SkipInteractionRecording)} ({interactions.SkipInteractionRecording}) must agree; recording paths gate on the behavior flag while verification gates on the interactions flag, so a mismatch leaves the registry in an inconsistent state.""",
				nameof(interactions));
		}

		Behavior = behavior;
		ConstructorParameters = constructorParameters;
		_interactions = interactions;
		Wraps = null;
	}

	/// <summary>
	///     Creates a new <see cref="MockRegistry" /> with the given <paramref name="behavior" /> whose interaction
	///     store is a <see cref="FastMockInteractions" /> sized to <paramref name="memberCount" />, allocated lazily
	///     on first access.
	/// </summary>
	/// <remarks>
	///     The generator-emitted <c>CreateMock</c> paths use this overload so that a mock which is only created — and
	///     never has a member invoked or verified — never allocates an interaction store at all.
	/// </remarks>
	/// <param name="behavior">The <see cref="MockBehavior" /> that governs how the mock responds without a matching setup.</param>
	/// <param name="memberCount">The number of distinct mockable members the lazily-created interaction store should hold.</param>
	/// <param name="constructorParameters">
	///     Values forwarded to the base-class constructor, or <see langword="null" /> if no base constructor call is needed.
	/// </param>
	public MockRegistry(MockBehavior behavior, int memberCount, object?[]? constructorParameters = null)
	{
		Behavior = behavior;
		ConstructorParameters = constructorParameters;
		_interactionMemberCount = memberCount;
		Wraps = null;
	}

	/// <summary>
	///     Creates a <see cref="MockRegistry" /> that shares setup and scenario state with <paramref name="registry" />
	///     but records interactions on a fresh bucket and forwards calls to <paramref name="wraps" />.
	/// </summary>
	/// <param name="registry">The source registry whose <see cref="Behavior" />, setups, and scenario state are reused.</param>
	/// <param name="wraps">The real instance that the mock should delegate calls to. See <see cref="Wraps" />.</param>
	public MockRegistry(MockRegistry registry, object wraps)
	{
		Behavior = registry.Behavior;
		ConstructorParameters = registry.ConstructorParameters;
		_interactions = new FastMockInteractions(0, registry.Behavior.SkipInteractionRecording);
		_setups = registry.Setup;
		_scenarioState = registry.GetOrCreateScenarioState();
		Wraps = wraps;
	}

	/// <summary>
	///     Creates a <see cref="MockRegistry" /> that shares all state with <paramref name="registry" /> but exposes
	///     a different set of <paramref name="constructorParameters" /> to the base-class constructor.
	/// </summary>
	/// <param name="registry">The source registry whose setups, interactions, scenario state, and wrapped instance are reused.</param>
	/// <param name="constructorParameters">Values forwarded to the base-class constructor of the new mock instance.</param>
	public MockRegistry(MockRegistry registry, object?[] constructorParameters)
	{
		Behavior = registry.Behavior;
		ConstructorParameters = constructorParameters;
		_interactions = registry.Interactions;
		_setups = registry.Setup;
		_scenarioState = registry.GetOrCreateScenarioState();
		Wraps = registry.Wraps;
	}

	/// <summary>
	///     Creates a <see cref="MockRegistry" /> that shares all state with <paramref name="registry" /> but records
	///     into the supplied <paramref name="interactions" /> collection. Used for scoped monitoring.
	/// </summary>
	/// <param name="registry">
	///     The source registry whose behavior, setups, constructor parameters, scenario state, and wrapped
	///     instance are reused.
	/// </param>
	/// <param name="interactions">The interaction collection that new invocations should be appended to.</param>
	public MockRegistry(MockRegistry registry, IMockInteractions interactions)
	{
		if (registry.Behavior.SkipInteractionRecording != interactions.SkipInteractionRecording)
		{
			throw new ArgumentException(
				$"""{nameof(registry)}.{nameof(Behavior)}.{nameof(MockBehavior.SkipInteractionRecording)} ({registry.Behavior.SkipInteractionRecording}) and {nameof(interactions)}.{nameof(IMockInteractions.SkipInteractionRecording)} ({interactions.SkipInteractionRecording}) must agree; recording paths gate on the behavior flag while verification gates on the interactions flag, so a mismatch leaves the registry in an inconsistent state.""",
				nameof(interactions));
		}

		Behavior = registry.Behavior;
		ConstructorParameters = registry.ConstructorParameters;
		_interactions = interactions;
		_setups = registry.Setup;
		_scenarioState = registry.GetOrCreateScenarioState();
		Wraps = registry.Wraps;
	}

	/// <summary>
	///     The name of the currently active scenario. Defaults to <see cref="string.Empty" />. Use
	///     <see cref="TransitionTo(string)" /> or the generator-emitted <c>TransitionTo</c> chained onto a setup to
	///     change it.
	/// </summary>
	/// <remarks>
	///     When a member is invoked, Mockolate resolves a matching setup in this order:
	///     <list type="number">
	///         <item>
	///             <description>the active scenario's bucket, when <see cref="Scenario" /> is not empty;</description>
	///         </item>
	///         <item>
	///             <description>the default bucket (setups registered via <c>sut.Mock.Setup.*</c>);</description>
	///         </item>
	///         <item>
	///             <description>the default response determined by <see cref="Behavior" />.</description>
	///         </item>
	///     </list>
	///     Scenario setups add to, rather than replace, the default bucket - register catch-alls at the default scope
	///     and override specific members per scenario.
	/// </remarks>
	public string Scenario => _scenarioState?.Current ?? "";

	/// <summary>
	///     Gets the behavior settings used by this mock instance.
	/// </summary>
	public MockBehavior Behavior { get; }

	/// <summary>
	///     The constructor parameters used to create the base class.
	/// </summary>
	public object?[]? ConstructorParameters { get; }

	/// <summary>
	///     The real instance that public, non-protected calls are forwarded to, or <see langword="null" /> when the
	///     mock is a plain (non-wrapping) mock.
	/// </summary>
	/// <remarks>
	///     Populated by the generator-emitted <c>Wrapping(instance)</c> extension. Public members delegate to
	///     <see cref="Wraps" /> unless a matching setup overrides them; protected members still go through the base
	///     class implementation. All forwarded calls are recorded on <see cref="Interactions" /> and can be verified
	///     like any other interaction.
	/// </remarks>
	public object? Wraps { get; }

	/// <summary>
	///     Transitions the mock to the given <paramref name="scenario" />, so that subsequent member invocations
	///     look up setups in that scenario's bucket first, and fall back to the default bucket if nothing matches.
	/// </summary>
	/// <param name="scenario">The name of the scenario to activate. Use <see cref="string.Empty" /> for the default scope.</param>
	/// <remarks>
	///     Transitioning to a scenario name for which no setups were registered via <c>InScenario(name)</c> is
	///     legal - resolution will simply fall straight through to the default bucket. See <see cref="Scenario" />
	///     for the full resolution order.
	/// </remarks>
	public void TransitionTo(string scenario)
		=> GetOrCreateScenarioState().Current = scenario;

	private ScenarioState GetOrCreateScenarioState()
	{
		if (_scenarioState is { } existing)
		{
			return existing;
		}

		Interlocked.CompareExchange(ref _scenarioState, new ScenarioState(), null);
		return _scenarioState!;
	}

	private sealed class ScenarioState
	{
		public string Current { get; set; } = "";
	}
}
