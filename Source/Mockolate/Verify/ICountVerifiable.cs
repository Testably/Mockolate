using System;

namespace Mockolate.Verify;

/// <summary>
///     Internal fast-path contract: a verification result that can answer count terminators
///     (<c>Once</c>, <c>Exactly</c>, <c>AtLeast</c>, ...) without materialising the matching
///     <see cref="Mockolate.Interactions.IInteraction" /> array. Implemented by
///     <see cref="VerificationResult{TVerify}" /> (and its awaitable variant) so the count
///     extensions can skip the fallback through <see cref="IVerificationResult.Verify" />.
/// </summary>
internal interface ICountVerifiable
{
	/// <summary>
	///     Counts the matching interactions and returns whether <paramref name="countPredicate" />
	///     accepts the count. Implementations route through the typed view's
	///     <c>ConsumeMatching</c> when available; otherwise fall back to materialising and counting.
	/// </summary>
	bool VerifyCount(Func<int, bool> countPredicate);

	/// <summary>
	///     Counts the matching interactions and returns whether the count lies between
	///     <paramref name="minimum" /> and <paramref name="maximum" /> (both inclusive). The count terminators
	///     use this overload, so that a verification needs neither a delegate nor a closure.
	/// </summary>
	/// <param name="minimum">The smallest accepted count.</param>
	/// <param name="maximum">The largest accepted count.</param>
	/// <param name="found">The count that was checked last.</param>
	bool VerifyCount(int minimum, int maximum, out int found);
}
