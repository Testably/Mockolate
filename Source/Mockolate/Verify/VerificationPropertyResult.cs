using System.Runtime.CompilerServices;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate.Verify;

/// <summary>
///     Verifications on a property of type <typeparamref name="TParameter" />.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationPropertyResult<TSubject, TParameter>
{
	private readonly int _getMemberId;

	private readonly MockRegistry _mockRegistry;
	private readonly string _propertyName;
	private readonly int _setMemberId;
	private readonly TSubject _subject;

	/// <summary>
	///     Member-id-keyed constructor used by generated mocks to enable per-member fast Verify walks.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="mockRegistry">The mock registry holding the recorded interactions.</param>
	/// <param name="getMemberId">Member id of the property getter, or <c>-1</c> when unknown.</param>
	/// <param name="setMemberId">Member id of the property setter, or <c>-1</c> when unknown.</param>
	/// <param name="propertyName">The simple property name.</param>
	public VerificationPropertyResult(TSubject subject, MockRegistry mockRegistry,
		int getMemberId, int setMemberId, string propertyName)
	{
		_subject = subject;
		_mockRegistry = mockRegistry;
		_getMemberId = getMemberId;
		_setMemberId = setMemberId;
		_propertyName = propertyName;
	}

	/// <summary>
	///     Verifies the property read access on the mock.
	/// </summary>
	public VerificationResult<TSubject> Got()
		=> _mockRegistry.VerifyPropertyTyped(_subject, _getMemberId, _propertyName);

	/// <summary>
	///     Verifies the property write access on the mock with the given <paramref name="value" />.
	/// </summary>
	public VerificationResult<TSubject> Set(IParameter<TParameter> value)
		=> _mockRegistry.VerifyPropertyTyped(_subject, _setMemberId, _propertyName, value.AsParameterMatch());

	/// <summary>
	///     Verifies the property write access on the mock with the given <paramref name="value" />.
	/// </summary>
	[OverloadResolutionPriority(1)]
	public VerificationResult<TSubject> Set(TParameter value,
		[CallerArgumentExpression(nameof(value))]
		string doNotPopulateThisValue = "")
		=> _mockRegistry.VerifyPropertyTyped(_subject, _setMemberId, _propertyName,
			It.Is(value, doNotPopulateThisValue).AsParameterMatch());
}

/// <summary>
///     Verifications on a property that the mock only reads.
/// </summary>
/// <remarks>
///     Used instead of <see cref="VerificationPropertyResult{TSubject, TParameter}" /> when the mock has no
///     setter to intercept, either because the property is declared without one or because its setter is
///     not accessible from the mock's assembly. Writes then never reach the mock, so offering
///     <c>Set(...)</c> here would always report zero interactions. The property type is not a type
///     parameter because only <c>Set(...)</c> needs it.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationPropertyGetterResult<TSubject>
{
	private readonly int _getMemberId;
	private readonly MockRegistry _mockRegistry;
	private readonly string _propertyName;
	private readonly TSubject _subject;

	/// <inheritdoc cref="VerificationPropertyGetterResult{TSubject}" />
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="mockRegistry">The mock registry holding the recorded interactions.</param>
	/// <param name="getMemberId">Member id of the property getter, or <c>-1</c> when unknown.</param>
	/// <param name="propertyName">The simple property name.</param>
	public VerificationPropertyGetterResult(TSubject subject, MockRegistry mockRegistry, int getMemberId,
		string propertyName)
	{
		_subject = subject;
		_mockRegistry = mockRegistry;
		_getMemberId = getMemberId;
		_propertyName = propertyName;
	}

	/// <inheritdoc cref="VerificationPropertyResult{TSubject, TParameter}.Got()" />
	public VerificationResult<TSubject> Got()
		=> _mockRegistry.VerifyPropertyTyped(_subject, _getMemberId, _propertyName);
}

/// <summary>
///     Verifications on a property of type <typeparamref name="TParameter" /> that the mock only writes.
/// </summary>
/// <remarks>
///     The write-only counterpart of <see cref="VerificationPropertyGetterResult{TSubject}" />: the mock
///     has no getter to intercept, so <c>Got()</c> is not offered.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationPropertySetterResult<TSubject, TParameter>
{
	private readonly MockRegistry _mockRegistry;
	private readonly string _propertyName;
	private readonly int _setMemberId;
	private readonly TSubject _subject;

	/// <inheritdoc cref="VerificationPropertySetterResult{TSubject, TParameter}" />
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="mockRegistry">The mock registry holding the recorded interactions.</param>
	/// <param name="setMemberId">Member id of the property setter, or <c>-1</c> when unknown.</param>
	/// <param name="propertyName">The simple property name.</param>
	public VerificationPropertySetterResult(TSubject subject, MockRegistry mockRegistry, int setMemberId,
		string propertyName)
	{
		_subject = subject;
		_mockRegistry = mockRegistry;
		_setMemberId = setMemberId;
		_propertyName = propertyName;
	}

	/// <inheritdoc cref="VerificationPropertyResult{TSubject, TParameter}.Set(IParameter{TParameter})" />
	public VerificationResult<TSubject> Set(IParameter<TParameter> value)
		=> _mockRegistry.VerifyPropertyTyped(_subject, _setMemberId, _propertyName, value.AsParameterMatch());

	/// <inheritdoc cref="VerificationPropertyResult{TSubject, TParameter}.Set(TParameter, string)" />
	[OverloadResolutionPriority(1)]
	public VerificationResult<TSubject> Set(TParameter value,
		[CallerArgumentExpression(nameof(value))]
		string doNotPopulateThisValue = "")
		=> _mockRegistry.VerifyPropertyTyped(_subject, _setMemberId, _propertyName,
			It.Is(value, doNotPopulateThisValue).AsParameterMatch());
}
