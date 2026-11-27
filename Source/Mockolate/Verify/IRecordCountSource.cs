using Mockolate.Interactions;

namespace Mockolate.Verify;

/// <summary>
///     Allocation-free count source for the count-only verify fast path. Implementations call into a
///     typed view's <c>ConsumeMatching</c> overload so verification terminators that only
///     need a count (<c>Once</c>, <c>Exactly</c>, <c>AtLeast</c>, ...) can avoid materializing
///     <see cref="Mockolate.Interactions.IInteraction" /> instances.
/// </summary>
internal interface IRecordCountSource
{
	/// <summary>
	///     Counts recorded interactions whose parameters satisfy the source's captured matchers.
	/// </summary>
	int Count();

	/// <summary>
	///     Checks a boxed interaction against the source's captured matchers, for the paths that walk
	///     <see cref="IInteraction" /> instances (ordering, failure output, waiting).
	/// </summary>
	bool Matches(IInteraction interaction);
}

/// <summary>
///     Method-only extension to <see cref="IRecordCountSource" />. The <c>AnyParameters()</c> widener is
///     exposed by <see cref="VerificationResult{TVerify}.IgnoreParameters" />, which is only produced by
///     <c>VerifyMethod*</c> overloads — so only method count sources need an unmatched-count path.
/// </summary>
internal interface IMethodRecordCountSource : IRecordCountSource
{
	/// <summary>
	///     Counts every recorded interaction, ignoring captured matchers.
	///     Used when <c>AnyParameters()</c> widens the verification to any argument list.
	/// </summary>
	int CountAll();
}
