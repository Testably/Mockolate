using System.ComponentModel;
using Mockolate.Interactions;
using Mockolate.Verify;

namespace Mockolate.Internals;

/// <summary>
///     A verification result whose filter is refined with fluent members after it was created, e.g. the logging
///     verifications.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly.
/// </remarks>
/// <typeparam name="TVerify">The mock's verification facade, used to continue verification chains.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public abstract class FilteredVerificationResult<TVerify> : VerificationResult<TVerify>
{
	/// <inheritdoc cref="FilteredVerificationResult{TVerify}" />
	protected FilteredVerificationResult(TVerify verify, MockRegistry mockRegistry)
		: base(verify, mockRegistry.Interactions)
	{
	}

	/// <summary>
	///     Whether the <paramref name="interaction" /> matches the filter.
	/// </summary>
	protected abstract bool Matches(IInteraction interaction);

	/// <summary>
	///     Describes the filter for failure messages, e.g. <c>LogWarning().WithMessage("failed")</c>.
	/// </summary>
	protected abstract string Describe();

	private protected sealed override bool IsFilteredMatch(IInteraction interaction)
		=> Matches(interaction);

	private protected sealed override string GetFilteredExpectation()
		=> $"invoked method {Describe()}";
}
