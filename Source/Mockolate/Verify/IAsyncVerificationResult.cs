using System;
using System.Threading;
using System.Threading.Tasks;
using Mockolate.Exceptions;
using Mockolate.Interactions;

namespace Mockolate.Verify;

/// <summary>
///     An awaitable <see cref="VerificationResult{TVerify}" /> that uses the timeout or cancellation token to wait for the
///     expected interactions to occur.
/// </summary>
public interface IAsyncVerificationResult : IVerificationResult
{
	/// <summary>
	///     Asynchronously waits until the specified <paramref name="predicate" /> holds true for the current set of
	///     interactions, or until the timeout or cancellation token is triggered.
	/// </summary>
	Task<bool> VerifyAsync(Func<IInteraction[], bool> predicate);

	/// <summary>
	///     Asynchronously waits until the specified <paramref name="predicate" /> holds true for the current set of
	///     interactions, or until the configured timeout or cancellation token, or the given
	///     <paramref name="cancellationToken" /> is triggered.
	/// </summary>
	/// <remarks>
	///     The <paramref name="cancellationToken" /> only applies to this call and leaves the configured timeout and
	///     cancellation token unchanged.
	/// </remarks>
	/// <exception cref="MockVerificationTimeoutException">
	///     Thrown when the configured timeout or cancellation token is triggered first.
	/// </exception>
	/// <exception cref="OperationCanceledException">
	///     Thrown when the <paramref name="cancellationToken" /> is canceled.
	/// </exception>
	Task<bool> VerifyAsync(Func<IInteraction[], bool> predicate, CancellationToken cancellationToken);
}
