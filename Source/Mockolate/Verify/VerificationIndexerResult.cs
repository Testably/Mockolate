using System;
using System.Runtime.CompilerServices;
using Mockolate.Interactions;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate.Verify;

/// <summary>
///     Verifications on an indexer of type <typeparamref name="TParameter" />.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationIndexerResult<TSubject, TParameter>
{
	private readonly Func<IInteraction, bool>? _gotPredicate;
	private readonly Func<IInteraction, IParameterMatch<TParameter>, bool>? _setPredicate;

	/// <summary>The mock registry holding the recorded interactions.</summary>
	private protected readonly MockRegistry MockRegistry;

	/// <summary>Factory producing the indexer-argument description used in failure messages.</summary>
	private protected readonly Func<string> ParametersDescription;

	/// <summary>The verification facade the result is bound to.</summary>
	private protected readonly TSubject Subject;

	/// <summary>Member id of the indexer getter, or <c>-1</c> when unknown.</summary>
	private protected readonly int GetMemberId;

	/// <summary>Member id of the indexer setter, or <c>-1</c> when unknown.</summary>
	private protected readonly int SetMemberId;

	/// <summary>
	///     Member-id-keyed constructor used by generated mocks to enable per-member fast Verify walks.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="mockRegistry">The mock registry holding the recorded interactions.</param>
	/// <param name="getMemberId">Member id of the indexer getter, or <c>-1</c> when unknown.</param>
	/// <param name="setMemberId">Member id of the indexer setter, or <c>-1</c> when unknown.</param>
	/// <param name="gotPredicate">Predicate evaluated against each recorded indexer-getter interaction.</param>
	/// <param name="setPredicate">Predicate evaluated against each recorded indexer-setter interaction and the value matcher.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	public VerificationIndexerResult(TSubject subject, MockRegistry mockRegistry,
		int getMemberId, int setMemberId,
		Func<IInteraction, bool> gotPredicate,
		Func<IInteraction, IParameterMatch<TParameter>, bool> setPredicate,
		Func<string> parametersDescription)
	{
		Subject = subject;
		MockRegistry = mockRegistry;
		GetMemberId = getMemberId;
		SetMemberId = setMemberId;
		_gotPredicate = gotPredicate;
		_setPredicate = setPredicate;
		ParametersDescription = parametersDescription;
	}

	/// <summary>
	///     Predicate-free constructor used by the typed-match subclasses, which dispatch through
	///     <see cref="MockRegistry.IndexerGotTyped{T, T1}" /> / <see cref="MockRegistry.IndexerSetTyped{T, T1, TValue}" />
	///     and never consult the base predicates.
	/// </summary>
	private protected VerificationIndexerResult(TSubject subject, MockRegistry mockRegistry,
		int getMemberId, int setMemberId,
		Func<string> parametersDescription)
	{
		Subject = subject;
		MockRegistry = mockRegistry;
		GetMemberId = getMemberId;
		SetMemberId = setMemberId;
		_gotPredicate = null;
		_setPredicate = null;
		ParametersDescription = parametersDescription;
	}

	/// <summary>
	///     Verifies the indexer read access on the mock.
	/// </summary>
	public virtual VerificationResult<TSubject> Got()
		=> MockRegistry.IndexerGot(Subject, GetMemberId, _gotPredicate!, ParametersDescription);

	/// <summary>
	///     Verifies the indexer write access on the mock with the given <paramref name="value" />.
	/// </summary>
	public virtual VerificationResult<TSubject> Set(IParameter<TParameter> value)
		=> MockRegistry.IndexerSet(Subject, SetMemberId, _setPredicate!,
			(IParameterMatch<TParameter>)value, ParametersDescription);

	/// <summary>
	///     Verifies the indexer write access on the mock with the given <paramref name="value" />.
	/// </summary>
	[OverloadResolutionPriority(1)]
	public virtual VerificationResult<TSubject> Set(TParameter value,
		[CallerArgumentExpression(nameof(value))]
		string doNotPopulateThisValue = "")
		=> MockRegistry.IndexerSet(Subject, SetMemberId, _setPredicate!,
			(IParameterMatch<TParameter>)It.Is(value, doNotPopulateThisValue), ParametersDescription);
}

/// <summary>
///     Verifications on a 1-key indexer of type <typeparamref name="TParameter" />.
///     Bypasses the predicate-based <see cref="VerificationIndexerResult{TSubject, TParameter}" />
///     hot path and dispatches through the typed
///     <see cref="MockRegistry.IndexerGotTyped{T, T1}" /> /
///     <see cref="MockRegistry.IndexerSetTyped{T, T1, TValue}" /> overloads.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class VerificationIndexerResult<TSubject, T1, TParameter>
	: VerificationIndexerResult<TSubject, TParameter>
{
	private readonly IParameterMatch<T1> _match1;

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, T1, TParameter}" />
	public VerificationIndexerResult(TSubject subject, MockRegistry mockRegistry,
		int getMemberId, int setMemberId,
		IParameterMatch<T1> match1,
		Func<string> parametersDescription)
		: base(subject, mockRegistry, getMemberId, setMemberId, parametersDescription)
	{
		_match1 = match1;
	}

	/// <inheritdoc />
	public override VerificationResult<TSubject> Got()
		=> MockRegistry.IndexerGotTyped(Subject, GetMemberId, _match1, ParametersDescription);

	/// <inheritdoc />
	public override VerificationResult<TSubject> Set(IParameter<TParameter> value)
		=> MockRegistry.IndexerSetTyped(Subject, SetMemberId,
			_match1, (IParameterMatch<TParameter>)value, ParametersDescription);

	/// <inheritdoc />
	public override VerificationResult<TSubject> Set(TParameter value,
		[CallerArgumentExpression(nameof(value))]
		string doNotPopulateThisValue = "")
		=> MockRegistry.IndexerSetTyped(Subject, SetMemberId,
			_match1, (IParameterMatch<TParameter>)It.Is(value, doNotPopulateThisValue), ParametersDescription);
}

/// <summary>
///     Verifications on a 2-key indexer of type <typeparamref name="TParameter" />. See
///     <see cref="VerificationIndexerResult{TSubject, T1, TParameter}" /> for rationale.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class VerificationIndexerResult<TSubject, T1, T2, TParameter>
	: VerificationIndexerResult<TSubject, TParameter>
{
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, T1, T2, TParameter}" />
	public VerificationIndexerResult(TSubject subject, MockRegistry mockRegistry,
		int getMemberId, int setMemberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2,
		Func<string> parametersDescription)
		: base(subject, mockRegistry, getMemberId, setMemberId, parametersDescription)
	{
		_match1 = match1;
		_match2 = match2;
	}

	/// <inheritdoc />
	public override VerificationResult<TSubject> Got()
		=> MockRegistry.IndexerGotTyped(Subject, GetMemberId, _match1, _match2, ParametersDescription);

	/// <inheritdoc />
	public override VerificationResult<TSubject> Set(IParameter<TParameter> value)
		=> MockRegistry.IndexerSetTyped(Subject, SetMemberId,
			_match1, _match2, (IParameterMatch<TParameter>)value, ParametersDescription);

	/// <inheritdoc />
	public override VerificationResult<TSubject> Set(TParameter value,
		[CallerArgumentExpression(nameof(value))]
		string doNotPopulateThisValue = "")
		=> MockRegistry.IndexerSetTyped(Subject, SetMemberId,
			_match1, _match2, (IParameterMatch<TParameter>)It.Is(value, doNotPopulateThisValue), ParametersDescription);
}

/// <summary>
///     Verifications on a 3-key indexer of type <typeparamref name="TParameter" />. See
///     <see cref="VerificationIndexerResult{TSubject, T1, TParameter}" /> for rationale.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class VerificationIndexerResult<TSubject, T1, T2, T3, TParameter>
	: VerificationIndexerResult<TSubject, TParameter>
{
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, T1, T2, T3, TParameter}" />
	public VerificationIndexerResult(TSubject subject, MockRegistry mockRegistry,
		int getMemberId, int setMemberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3,
		Func<string> parametersDescription)
		: base(subject, mockRegistry, getMemberId, setMemberId, parametersDescription)
	{
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
	}

	/// <inheritdoc />
	public override VerificationResult<TSubject> Got()
		=> MockRegistry.IndexerGotTyped(Subject, GetMemberId,
			_match1, _match2, _match3, ParametersDescription);

	/// <inheritdoc />
	public override VerificationResult<TSubject> Set(IParameter<TParameter> value)
		=> MockRegistry.IndexerSetTyped(Subject, SetMemberId,
			_match1, _match2, _match3, (IParameterMatch<TParameter>)value, ParametersDescription);

	/// <inheritdoc />
	public override VerificationResult<TSubject> Set(TParameter value,
		[CallerArgumentExpression(nameof(value))]
		string doNotPopulateThisValue = "")
		=> MockRegistry.IndexerSetTyped(Subject, SetMemberId,
			_match1, _match2, _match3, (IParameterMatch<TParameter>)It.Is(value, doNotPopulateThisValue),
			ParametersDescription);
}

/// <summary>
///     Verifications on a 4-key indexer of type <typeparamref name="TParameter" />. See
///     <see cref="VerificationIndexerResult{TSubject, T1, TParameter}" /> for rationale.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class VerificationIndexerResult<TSubject, T1, T2, T3, T4, TParameter>
	: VerificationIndexerResult<TSubject, TParameter>
{
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;
	private readonly IParameterMatch<T4> _match4;

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, T1, T2, T3, T4, TParameter}" />
	public VerificationIndexerResult(TSubject subject, MockRegistry mockRegistry,
		int getMemberId, int setMemberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2,
		IParameterMatch<T3> match3, IParameterMatch<T4> match4,
		Func<string> parametersDescription)
		: base(subject, mockRegistry, getMemberId, setMemberId, parametersDescription)
	{
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
		_match4 = match4;
	}

	/// <inheritdoc />
	public override VerificationResult<TSubject> Got()
		=> MockRegistry.IndexerGotTyped(Subject, GetMemberId,
			_match1, _match2, _match3, _match4, ParametersDescription);

	/// <inheritdoc />
	public override VerificationResult<TSubject> Set(IParameter<TParameter> value)
		=> MockRegistry.IndexerSetTyped(Subject, SetMemberId,
			_match1, _match2, _match3, _match4, (IParameterMatch<TParameter>)value, ParametersDescription);

	/// <inheritdoc />
	public override VerificationResult<TSubject> Set(TParameter value,
		[CallerArgumentExpression(nameof(value))]
		string doNotPopulateThisValue = "")
		=> MockRegistry.IndexerSetTyped(Subject, SetMemberId,
			_match1, _match2, _match3, _match4,
			(IParameterMatch<TParameter>)It.Is(value, doNotPopulateThisValue), ParametersDescription);
}

/// <summary>
///     Verifications on a 1-key indexer that the mock only reads.
/// </summary>
/// <remarks>
///     Used instead of <see cref="VerificationIndexerResult{TSubject, T1, TParameter}" /> when the mock has no
///     setter to intercept, either because the indexer is declared without one or because its setter is not
///     accessible from the mock's assembly. Writes then never reach the mock, so offering <c>Set(...)</c> here
///     would always report zero interactions. The value type is not a type parameter because only <c>Set(...)</c>
///     needs it.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationIndexerGetterResult<TSubject, T1>
{
	private readonly int _getMemberId;
	private readonly IParameterMatch<T1> _match1;
	private readonly MockRegistry _mockRegistry;
	private readonly Func<string> _parametersDescription;
	private readonly TSubject _subject;

	/// <inheritdoc cref="VerificationIndexerGetterResult{TSubject, T1}" />
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="mockRegistry">The mock registry holding the recorded interactions.</param>
	/// <param name="getMemberId">Member id of the indexer getter, or <c>-1</c> when unknown.</param>
	/// <param name="match1">The matcher for the first indexer parameter.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	public VerificationIndexerGetterResult(TSubject subject, MockRegistry mockRegistry, int getMemberId,
		IParameterMatch<T1> match1, Func<string> parametersDescription)
	{
		_subject = subject;
		_mockRegistry = mockRegistry;
		_getMemberId = getMemberId;
		_match1 = match1;
		_parametersDescription = parametersDescription;
	}

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, TParameter}.Got()" />
	public VerificationResult<TSubject> Got()
		=> _mockRegistry.IndexerGotTyped(_subject, _getMemberId, _match1, _parametersDescription);
}

/// <summary>
///     Verifications on a 1-key indexer of type <typeparamref name="TParameter" /> that the mock only writes.
/// </summary>
/// <remarks>
///     The write-only counterpart of <see cref="VerificationIndexerGetterResult{TSubject, T1}" />: the mock has
///     no getter to intercept, so <c>Got()</c> is not offered.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationIndexerSetterResult<TSubject, T1, TParameter>
{
	private readonly IParameterMatch<T1> _match1;
	private readonly MockRegistry _mockRegistry;
	private readonly Func<string> _parametersDescription;
	private readonly int _setMemberId;
	private readonly TSubject _subject;

	/// <inheritdoc cref="VerificationIndexerSetterResult{TSubject, T1, TParameter}" />
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="mockRegistry">The mock registry holding the recorded interactions.</param>
	/// <param name="setMemberId">Member id of the indexer setter, or <c>-1</c> when unknown.</param>
	/// <param name="match1">The matcher for the first indexer parameter.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	public VerificationIndexerSetterResult(TSubject subject, MockRegistry mockRegistry, int setMemberId,
		IParameterMatch<T1> match1, Func<string> parametersDescription)
	{
		_subject = subject;
		_mockRegistry = mockRegistry;
		_setMemberId = setMemberId;
		_match1 = match1;
		_parametersDescription = parametersDescription;
	}

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, TParameter}.Set(IParameter{TParameter})" />
	public VerificationResult<TSubject> Set(IParameter<TParameter> value)
		=> _mockRegistry.IndexerSetTyped(_subject, _setMemberId, _match1,
			(IParameterMatch<TParameter>)value, _parametersDescription);

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, TParameter}.Set(TParameter, string)" />
	[OverloadResolutionPriority(1)]
	public VerificationResult<TSubject> Set(TParameter value,
		[CallerArgumentExpression(nameof(value))]
		string doNotPopulateThisValue = "")
		=> _mockRegistry.IndexerSetTyped(_subject, _setMemberId, _match1,
			(IParameterMatch<TParameter>)It.Is(value, doNotPopulateThisValue), _parametersDescription);
}

/// <summary>
///     Verifications on a 2-key indexer that the mock only reads. See
///     <see cref="VerificationIndexerGetterResult{TSubject, T1}" /> for rationale.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationIndexerGetterResult<TSubject, T1, T2>
{
	private readonly int _getMemberId;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly MockRegistry _mockRegistry;
	private readonly Func<string> _parametersDescription;
	private readonly TSubject _subject;

	/// <inheritdoc cref="VerificationIndexerGetterResult{TSubject, T1, T2}" />
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="mockRegistry">The mock registry holding the recorded interactions.</param>
	/// <param name="getMemberId">Member id of the indexer getter, or <c>-1</c> when unknown.</param>
	/// <param name="match1">The matcher for the first indexer parameter.</param>
	/// <param name="match2">The matcher for the second indexer parameter.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	public VerificationIndexerGetterResult(TSubject subject, MockRegistry mockRegistry, int getMemberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, Func<string> parametersDescription)
	{
		_subject = subject;
		_mockRegistry = mockRegistry;
		_getMemberId = getMemberId;
		_match1 = match1;
		_match2 = match2;
		_parametersDescription = parametersDescription;
	}

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, TParameter}.Got()" />
	public VerificationResult<TSubject> Got()
		=> _mockRegistry.IndexerGotTyped(_subject, _getMemberId, _match1, _match2, _parametersDescription);
}

/// <summary>
///     Verifications on a 2-key indexer of type <typeparamref name="TParameter" /> that the mock only writes. See
///     <see cref="VerificationIndexerSetterResult{TSubject, T1, TParameter}" /> for rationale.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationIndexerSetterResult<TSubject, T1, T2, TParameter>
{
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly MockRegistry _mockRegistry;
	private readonly Func<string> _parametersDescription;
	private readonly int _setMemberId;
	private readonly TSubject _subject;

	/// <inheritdoc cref="VerificationIndexerSetterResult{TSubject, T1, T2, TParameter}" />
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="mockRegistry">The mock registry holding the recorded interactions.</param>
	/// <param name="setMemberId">Member id of the indexer setter, or <c>-1</c> when unknown.</param>
	/// <param name="match1">The matcher for the first indexer parameter.</param>
	/// <param name="match2">The matcher for the second indexer parameter.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	public VerificationIndexerSetterResult(TSubject subject, MockRegistry mockRegistry, int setMemberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, Func<string> parametersDescription)
	{
		_subject = subject;
		_mockRegistry = mockRegistry;
		_setMemberId = setMemberId;
		_match1 = match1;
		_match2 = match2;
		_parametersDescription = parametersDescription;
	}

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, TParameter}.Set(IParameter{TParameter})" />
	public VerificationResult<TSubject> Set(IParameter<TParameter> value)
		=> _mockRegistry.IndexerSetTyped(_subject, _setMemberId, _match1, _match2,
			(IParameterMatch<TParameter>)value, _parametersDescription);

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, TParameter}.Set(TParameter, string)" />
	[OverloadResolutionPriority(1)]
	public VerificationResult<TSubject> Set(TParameter value,
		[CallerArgumentExpression(nameof(value))]
		string doNotPopulateThisValue = "")
		=> _mockRegistry.IndexerSetTyped(_subject, _setMemberId, _match1, _match2,
			(IParameterMatch<TParameter>)It.Is(value, doNotPopulateThisValue), _parametersDescription);
}

/// <summary>
///     Verifications on a 3-key indexer that the mock only reads. See
///     <see cref="VerificationIndexerGetterResult{TSubject, T1}" /> for rationale.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationIndexerGetterResult<TSubject, T1, T2, T3>
{
	private readonly int _getMemberId;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;
	private readonly MockRegistry _mockRegistry;
	private readonly Func<string> _parametersDescription;
	private readonly TSubject _subject;

	/// <inheritdoc cref="VerificationIndexerGetterResult{TSubject, T1, T2, T3}" />
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="mockRegistry">The mock registry holding the recorded interactions.</param>
	/// <param name="getMemberId">Member id of the indexer getter, or <c>-1</c> when unknown.</param>
	/// <param name="match1">The matcher for the first indexer parameter.</param>
	/// <param name="match2">The matcher for the second indexer parameter.</param>
	/// <param name="match3">The matcher for the third indexer parameter.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	public VerificationIndexerGetterResult(TSubject subject, MockRegistry mockRegistry, int getMemberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3,
		Func<string> parametersDescription)
	{
		_subject = subject;
		_mockRegistry = mockRegistry;
		_getMemberId = getMemberId;
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
		_parametersDescription = parametersDescription;
	}

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, TParameter}.Got()" />
	public VerificationResult<TSubject> Got()
		=> _mockRegistry.IndexerGotTyped(_subject, _getMemberId, _match1, _match2, _match3, _parametersDescription);
}

/// <summary>
///     Verifications on a 3-key indexer of type <typeparamref name="TParameter" /> that the mock only writes. See
///     <see cref="VerificationIndexerSetterResult{TSubject, T1, TParameter}" /> for rationale.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationIndexerSetterResult<TSubject, T1, T2, T3, TParameter>
{
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;
	private readonly MockRegistry _mockRegistry;
	private readonly Func<string> _parametersDescription;
	private readonly int _setMemberId;
	private readonly TSubject _subject;

	/// <inheritdoc cref="VerificationIndexerSetterResult{TSubject, T1, T2, T3, TParameter}" />
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="mockRegistry">The mock registry holding the recorded interactions.</param>
	/// <param name="setMemberId">Member id of the indexer setter, or <c>-1</c> when unknown.</param>
	/// <param name="match1">The matcher for the first indexer parameter.</param>
	/// <param name="match2">The matcher for the second indexer parameter.</param>
	/// <param name="match3">The matcher for the third indexer parameter.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	public VerificationIndexerSetterResult(TSubject subject, MockRegistry mockRegistry, int setMemberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3,
		Func<string> parametersDescription)
	{
		_subject = subject;
		_mockRegistry = mockRegistry;
		_setMemberId = setMemberId;
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
		_parametersDescription = parametersDescription;
	}

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, TParameter}.Set(IParameter{TParameter})" />
	public VerificationResult<TSubject> Set(IParameter<TParameter> value)
		=> _mockRegistry.IndexerSetTyped(_subject, _setMemberId, _match1, _match2, _match3,
			(IParameterMatch<TParameter>)value, _parametersDescription);

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, TParameter}.Set(TParameter, string)" />
	[OverloadResolutionPriority(1)]
	public VerificationResult<TSubject> Set(TParameter value,
		[CallerArgumentExpression(nameof(value))]
		string doNotPopulateThisValue = "")
		=> _mockRegistry.IndexerSetTyped(_subject, _setMemberId, _match1, _match2, _match3,
			(IParameterMatch<TParameter>)It.Is(value, doNotPopulateThisValue), _parametersDescription);
}

/// <summary>
///     Verifications on a 4-key indexer that the mock only reads. See
///     <see cref="VerificationIndexerGetterResult{TSubject, T1}" /> for rationale.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationIndexerGetterResult<TSubject, T1, T2, T3, T4>
{
	private readonly int _getMemberId;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;
	private readonly IParameterMatch<T4> _match4;
	private readonly MockRegistry _mockRegistry;
	private readonly Func<string> _parametersDescription;
	private readonly TSubject _subject;

	/// <inheritdoc cref="VerificationIndexerGetterResult{TSubject, T1, T2, T3, T4}" />
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="mockRegistry">The mock registry holding the recorded interactions.</param>
	/// <param name="getMemberId">Member id of the indexer getter, or <c>-1</c> when unknown.</param>
	/// <param name="match1">The matcher for the first indexer parameter.</param>
	/// <param name="match2">The matcher for the second indexer parameter.</param>
	/// <param name="match3">The matcher for the third indexer parameter.</param>
	/// <param name="match4">The matcher for the fourth indexer parameter.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	public VerificationIndexerGetterResult(TSubject subject, MockRegistry mockRegistry, int getMemberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4,
		Func<string> parametersDescription)
	{
		_subject = subject;
		_mockRegistry = mockRegistry;
		_getMemberId = getMemberId;
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
		_match4 = match4;
		_parametersDescription = parametersDescription;
	}

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, TParameter}.Got()" />
	public VerificationResult<TSubject> Got()
		=> _mockRegistry.IndexerGotTyped(_subject, _getMemberId, _match1, _match2, _match3, _match4,
			_parametersDescription);
}

/// <summary>
///     Verifications on a 4-key indexer of type <typeparamref name="TParameter" /> that the mock only writes. See
///     <see cref="VerificationIndexerSetterResult{TSubject, T1, TParameter}" /> for rationale.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationIndexerSetterResult<TSubject, T1, T2, T3, T4, TParameter>
{
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;
	private readonly IParameterMatch<T4> _match4;
	private readonly MockRegistry _mockRegistry;
	private readonly Func<string> _parametersDescription;
	private readonly int _setMemberId;
	private readonly TSubject _subject;

	/// <inheritdoc cref="VerificationIndexerSetterResult{TSubject, T1, T2, T3, T4, TParameter}" />
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="mockRegistry">The mock registry holding the recorded interactions.</param>
	/// <param name="setMemberId">Member id of the indexer setter, or <c>-1</c> when unknown.</param>
	/// <param name="match1">The matcher for the first indexer parameter.</param>
	/// <param name="match2">The matcher for the second indexer parameter.</param>
	/// <param name="match3">The matcher for the third indexer parameter.</param>
	/// <param name="match4">The matcher for the fourth indexer parameter.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	public VerificationIndexerSetterResult(TSubject subject, MockRegistry mockRegistry, int setMemberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4,
		Func<string> parametersDescription)
	{
		_subject = subject;
		_mockRegistry = mockRegistry;
		_setMemberId = setMemberId;
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
		_match4 = match4;
		_parametersDescription = parametersDescription;
	}

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, TParameter}.Set(IParameter{TParameter})" />
	public VerificationResult<TSubject> Set(IParameter<TParameter> value)
		=> _mockRegistry.IndexerSetTyped(_subject, _setMemberId, _match1, _match2, _match3, _match4,
			(IParameterMatch<TParameter>)value, _parametersDescription);

	/// <inheritdoc cref="VerificationIndexerResult{TSubject, TParameter}.Set(TParameter, string)" />
	[OverloadResolutionPriority(1)]
	public VerificationResult<TSubject> Set(TParameter value,
		[CallerArgumentExpression(nameof(value))]
		string doNotPopulateThisValue = "")
		=> _mockRegistry.IndexerSetTyped(_subject, _setMemberId, _match1, _match2, _match3, _match4,
			(IParameterMatch<TParameter>)It.Is(value, doNotPopulateThisValue), _parametersDescription);
}
