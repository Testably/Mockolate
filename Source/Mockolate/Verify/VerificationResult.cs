using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mockolate.Exceptions;
using Mockolate.Interactions;

namespace Mockolate.Verify;

/// <summary>
///     The intermediate result of a <c>sut.Mock.Verify.MemberName(...)</c> call - carries the recorded interactions
///     that match the verification predicate, waiting for a terminating count assertion.
/// </summary>
/// <remarks>
///     Obtained from <c>sut.Mock.Verify</c>, <c>sut.Mock.VerifyProtected</c>, <c>sut.Mock.VerifyStatic</c> or
///     <c>sut.Mock.InScenario(...).Verify</c>. Nothing is asserted until a terminator runs - until then the object
///     can still be composed:
///     <list type="bullet">
///         <item>
///             <description>Terminate with a count assertion from <see cref="VerificationResultExtensions" /> &#8212; <c>Once()</c>, <c>Never()</c>, <c>Exactly(n)</c>, <c>AtLeast(n)</c>, <c>AtMost(n)</c>, <c>Between(min, max)</c>, <c>Times(n)</c> &#8212; to turn the result into a pass/fail check that throws <see cref="MockVerificationException" /> on mismatch.</description>
///         </item>
///         <item>
///             <description>Chain <c>.Then(next)</c> to assert an ordering between two verifications.</description>
///         </item>
///         <item>
///             <description>Call <see cref="Within(TimeSpan)" /> and/or <see cref="WithCancellation(CancellationToken)" /> before the terminator to wait for interactions produced on a background thread (synchronous wait; use the <c>aweXpect.Mockolate</c> package for the asynchronous variant).</description>
///         </item>
///         <item>
///             <description>When the verification matcher uses <c>It.*</c> parameter matchers, the nested <see cref="IgnoreParameters" /> subtype also exposes <c>AnyParameters()</c> to widen the match to any argument list.</description>
///         </item>
///     </list>
/// </remarks>
/// <typeparam name="TVerify">The mock's verification facade, used to continue verification chains (e.g. <c>.Then(...)</c>).</typeparam>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public class VerificationResult<TVerify> : IVerificationResult<TVerify>, IVerificationResult, IFastVerifyCountResult
{
	private readonly Func<string> _expectationFactory;
	private readonly IMockInteractions _interactions;
	private readonly IFastMemberBuffer? _buffer;
	private readonly IFastCountSource? _fastCountSource;
	private Func<IInteraction, bool> _predicate;
	private readonly TVerify _verify;

	/// <inheritdoc cref="VerificationResult{TVerify}" />
	public VerificationResult(TVerify verify,
		IMockInteractions interactions,
		Func<IInteraction, bool> predicate,
		string expectation)
	{
		_verify = verify;
		_interactions = interactions;
		_predicate = predicate;
		_expectationFactory = () => expectation;
	}

	/// <inheritdoc cref="VerificationResult{TVerify}" />
	public VerificationResult(TVerify verify,
		IMockInteractions interactions,
		Func<IInteraction, bool> predicate,
		Func<string> expectation)
	{
		_verify = verify;
		_interactions = interactions;
		_predicate = predicate;
		_expectationFactory = expectation;
	}

	internal VerificationResult(TVerify verify,
		IMockInteractions interactions,
		IFastMemberBuffer? buffer,
		Func<IInteraction, bool> predicate,
		Func<string> expectation)
	{
		_verify = verify;
		_interactions = interactions;
		_buffer = buffer;
		_predicate = predicate;
		_expectationFactory = expectation;
	}

	internal VerificationResult(TVerify verify,
		IMockInteractions interactions,
		IFastMemberBuffer? buffer,
		IFastCountSource? fastCountSource,
		Func<IInteraction, bool> predicate,
		Func<string> expectation)
	{
		_verify = verify;
		_interactions = interactions;
		_buffer = buffer;
		_fastCountSource = fastCountSource;
		_predicate = predicate;
		_expectationFactory = expectation;
	}

	#region IVerificationResult<TVerify>

	/// <inheritdoc cref="IVerificationResult{TVerify}.Object" />
	TVerify IVerificationResult<TVerify>.Object
		=> _verify;

	#endregion

	internal VerificationResult<T> Map<T>(T mock)
	{
		if (_buffer is null && _fastCountSource is null)
		{
			return new VerificationResult<T>(mock, _interactions, _predicate, _expectationFactory);
		}

		return new VerificationResult<T>(mock, _interactions, _buffer, _fastCountSource, _predicate, _expectationFactory);
	}

	private protected bool HasBuffer => _buffer is not null;

	private bool _useCountAll;

	private protected void EnableCountAll() => _useCountAll = true;

	private void ReplacePredicate(Func<IInteraction, bool> predicate)
		=> _predicate = predicate;

	private IInteraction[] CollectMatching()
	{
		if (_buffer is null)
		{
			return _interactions.Where(_predicate).ToArray();
		}

		List<(long Seq, IInteraction Interaction)> records = new();
		_buffer.AppendBoxed(records);
		if (records.Count == 0)
		{
			return [];
		}

		if (records.Count > 1)
		{
			records.Sort(static (left, right) => left.Seq.CompareTo(right.Seq));
		}

		List<IInteraction>? matching = null;
		foreach ((long _, IInteraction interaction) in records)
		{
			if (_predicate(interaction))
			{
				(matching ??= new List<IInteraction>(records.Count)).Add(interaction);
			}
		}

		return matching is null ? [] : matching.ToArray();
	}

	private static void ThrowIfRecordingDisabled(IMockInteractions interactions)
	{
		if (interactions.SkipInteractionRecording)
		{
			throw new MockException(
				"""
				Cannot verify interactions because interaction recording is disabled. To re-enable verifications, set MockBehavior.SkipInteractionRecording to false.
				"""
			);
		}
	}

	/// <summary>
	///     Returns a verification result that, when terminated with a count assertion, waits up to
	///     <paramref name="timeout" /> for the expected interactions before throwing.
	/// </summary>
	/// <param name="timeout">How long to wait for interactions before reporting a timeout failure.</param>
	/// <remarks>
	///     The wait is synchronous: the terminating count assertion blocks the calling thread until the expectation
	///     is satisfied, the timeout elapses, or the <see cref="WithCancellation(CancellationToken)" /> token (if any)
	///     fires. If a non-blocking wait is needed, use the asynchronous <c>Within(TimeSpan)</c> variant provided by
	///     the <c>aweXpect.Mockolate</c> extension package.
	///     <para />
	///     On timeout, a <see cref="MockVerificationTimeoutException" /> is raised internally and surfaces as a
	///     <see cref="MockVerificationException" /> from the terminator.
	/// </remarks>
	/// <seealso cref="WithCancellation(CancellationToken)" />
	public virtual VerificationResult<TVerify> Within(TimeSpan timeout)
		=> new Awaitable(this, timeout);

	/// <summary>
	///     Returns a verification result that, when terminated with a count assertion, waits for the expected
	///     interactions until <paramref name="cancellationToken" /> is cancelled.
	/// </summary>
	/// <param name="cancellationToken">Token that signals when to stop waiting.</param>
	/// <remarks>
	///     The wait is synchronous: the terminating count assertion blocks the calling thread. Combine with
	///     <see cref="Within(TimeSpan)" /> to apply both a timeout and an external cancellation; whichever fires
	///     first wins.
	///     <para />
	///     For a non-blocking wait, use the asynchronous variant provided by the <c>aweXpect.Mockolate</c>
	///     extension package.
	/// </remarks>
	/// <seealso cref="Within(TimeSpan)" />
	public virtual VerificationResult<TVerify> WithCancellation(CancellationToken cancellationToken)
		=> new Awaitable(this, cancellationToken);

	/// <summary>
	///     An awaitable <see cref="VerificationResult{TVerify}" /> that uses the timeout or cancellation token to wait for the
	///     expected interactions to occur.
	/// </summary>
	internal class Awaitable : VerificationResult<TVerify>, IAsyncVerificationResult, IFastVerifyCountResult
	{
		private CancellationToken? _cancellationToken;
		private TimeSpan? _timeout;

		/// <summary>
		///     An awaitable <see cref="VerificationResult{TVerify}" /> that uses the <paramref name="timeout" /> to wait for the
		///     expected interactions to occur.
		/// </summary>
		public Awaitable(VerificationResult<TVerify> inner, TimeSpan timeout)
			: base(inner._verify, inner._interactions, inner._buffer, inner._fastCountSource, inner._predicate, inner._expectationFactory)
		{
			_timeout = timeout;
			if (inner._useCountAll)
			{
				EnableCountAll();
			}
		}

		/// <summary>
		///     An awaitable <see cref="VerificationResult{TVerify}" /> that uses the <paramref name="cancellationToken" /> to wait
		///     for the
		///     expected interactions to occur.
		/// </summary>
		public Awaitable(VerificationResult<TVerify> inner, CancellationToken cancellationToken)
			: base(inner._verify, inner._interactions, inner._buffer, inner._fastCountSource, inner._predicate, inner._expectationFactory)
		{
			_cancellationToken = cancellationToken;
			if (inner._useCountAll)
			{
				EnableCountAll();
			}
		}

		/// <inheritdoc cref="IVerificationResult.Verify(Func{IInteraction[], Boolean})" />
		bool IVerificationResult.Verify(Func<IInteraction[], bool> predicate)
		{
			ThrowIfRecordingDisabled(_interactions);
			IInteraction[] matchingInteractions = CollectMatching();
			_interactions.Verified(matchingInteractions);
			bool result = predicate(matchingInteractions);
			if (result)
			{
				return true;
			}

			return VerifyAsync(predicate).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		/// <inheritdoc cref="IAsyncVerificationResult.VerifyAsync(Func{IInteraction[], Boolean})" />
		public Task<bool> VerifyAsync(Func<IInteraction[], bool> predicate)
			=> VerifyAsync(predicate, CancellationToken.None);

		/// <inheritdoc cref="IAsyncVerificationResult.VerifyAsync(Func{IInteraction[], Boolean}, CancellationToken)" />
		public async Task<bool> VerifyAsync(Func<IInteraction[], bool> predicate, CancellationToken cancellationToken)
		{
			ThrowIfRecordingDisabled(_interactions);
			IInteraction[] matchingInteractions = CollectMatching();
			_interactions.Verified(matchingInteractions);
			bool result = predicate(matchingInteractions);
			if (result)
			{
				return true;
			}

			try
			{
				CancellationToken token = CreateWaitToken(cancellationToken, out CancellationTokenSource? cts);

				SemaphoreSlim semaphore = new(0);
				try
				{
					_interactions.InteractionAdded += OnInteractionAdded;
					do
					{
						matchingInteractions = CollectMatching();
						_interactions.Verified(matchingInteractions);
						if (predicate(matchingInteractions))
						{
							return true;
						}

						await semaphore.WaitAsync(token).ConfigureAwait(false);
					} while (true);
				}
				finally
				{
					_interactions.InteractionAdded -= OnInteractionAdded;
					cts?.Cancel();
					cts?.Dispose();
					semaphore.Dispose();
				}

				void OnInteractionAdded(object? sender, EventArgs eventArgs)
				{
					try
					{
						// ReSharper disable once AccessToDisposedClosure
						semaphore.Release();
					}
					catch (ObjectDisposedException)
					{
						// Ignore if the semaphore has already been disposed
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					// The wait was canceled on the linked token, but the caller expects its own token
					throw new OperationCanceledException(ex.Message, ex, cancellationToken);
				}

				if (_cancellationToken?.IsCancellationRequested == true)
				{
					throw new MockVerificationTimeoutException(null, ex);
				}

				throw new MockVerificationTimeoutException(_timeout, ex);
			}
		}

		private CancellationToken CreateWaitToken(CancellationToken cancellationToken, out CancellationTokenSource? cts)
		{
			if (_timeout is null && !cancellationToken.CanBeCanceled)
			{
				cts = null;
				return _cancellationToken!.Value;
			}

			cts = CancellationTokenSource.CreateLinkedTokenSource(
				_cancellationToken ?? CancellationToken.None, cancellationToken);
			if (_timeout is not null)
			{
				cts.CancelAfter(_timeout.Value);
			}

			return cts.Token;
		}

		/// <inheritdoc cref="IFastVerifyCountResult.VerifyCount(Func{int, Boolean})" />
		bool IFastVerifyCountResult.VerifyCount(Func<int, bool> countPredicate)
		{
			ThrowIfRecordingDisabled(_interactions);
			if (_fastCountSource is not null)
			{
				int count = _useCountAll
					? ((IFastMethodCountSource)_fastCountSource).CountAll()
					: _fastCountSource.Count();
				if (countPredicate(count))
				{
					return true;
				}

				return VerifyCountAsync(countPredicate).ConfigureAwait(false).GetAwaiter().GetResult();
			}

			IInteraction[] matchingInteractions = CollectMatching();
			_interactions.Verified(matchingInteractions);
			if (countPredicate(matchingInteractions.Length))
			{
				return true;
			}

			return VerifyAsync(arr => countPredicate(arr.Length)).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		private async Task<bool> VerifyCountAsync(Func<int, bool> countPredicate)
		{
			try
			{
				CancellationToken token = CreateWaitToken(CancellationToken.None, out CancellationTokenSource? cts);

				SemaphoreSlim semaphore = new(0);
				try
				{
					_interactions.InteractionAdded += OnInteractionAdded;
					do
					{
						int count = _useCountAll
							? ((IFastMethodCountSource)_fastCountSource!).CountAll()
							: _fastCountSource!.Count();
						if (countPredicate(count))
						{
							return true;
						}

						await semaphore.WaitAsync(token).ConfigureAwait(false);
					} while (true);
				}
				finally
				{
					_interactions.InteractionAdded -= OnInteractionAdded;
					cts?.Cancel();
					cts?.Dispose();
					semaphore.Dispose();
				}

				void OnInteractionAdded(object? sender, EventArgs eventArgs)
				{
					try
					{
						semaphore.Release();
					}
					catch (ObjectDisposedException)
					{
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				if (_cancellationToken?.IsCancellationRequested == true)
				{
					throw new MockVerificationTimeoutException(null, ex);
				}

				throw new MockVerificationTimeoutException(_timeout, ex);
			}
		}

		/// <inheritdoc cref="VerificationResult{TVerify}.Within(TimeSpan)" />
		public override VerificationResult<TVerify> Within(TimeSpan timeout)
		{
			_timeout = timeout;
			return this;
		}

		/// <inheritdoc cref="VerificationResult{TVerify}.WithCancellation(CancellationToken)" />
		public override VerificationResult<TVerify> WithCancellation(CancellationToken cancellationToken)
		{
			_cancellationToken = cancellationToken;
			return this;
		}
	}

	#region IVerificationResult

	/// <inheritdoc cref="IVerificationResult.Expectation" />
	string IVerificationResult.Expectation
		=> _expectationFactory();

	/// <inheritdoc cref="IVerificationResult.Interactions" />
	IMockInteractions IVerificationResult.Interactions
		=> _interactions;

	/// <inheritdoc cref="IVerificationResult.Verify(Func{IInteraction[], Boolean})" />
	bool IVerificationResult.Verify(Func<IInteraction[], bool> predicate)
	{
		ThrowIfRecordingDisabled(_interactions);
		IInteraction[] matchingInteractions = CollectMatching();
		_interactions.Verified(matchingInteractions);
		return predicate(matchingInteractions);
	}

	/// <inheritdoc cref="IFastVerifyCountResult.VerifyCount(Func{int, Boolean})" />
	bool IFastVerifyCountResult.VerifyCount(Func<int, bool> countPredicate)
	{
		ThrowIfRecordingDisabled(_interactions);
		if (_fastCountSource is not null)
		{
			int count = _useCountAll
				? ((IFastMethodCountSource)_fastCountSource).CountAll()
				: _fastCountSource.Count();
			return countPredicate(count);
		}

		IInteraction[] matchingInteractions = CollectMatching();
		_interactions.Verified(matchingInteractions);
		return countPredicate(matchingInteractions.Length);
	}

	#endregion

	/// <summary>
	///     Represents the result of a verification that contains the matching interactions and allows ignoring explicit parameters.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	public class IgnoreParameters : VerificationResult<TVerify>
	{
		private readonly string _methodName;
		private readonly Func<IInteraction, bool>? _overloadFilter;

		internal IgnoreParameters(
			TVerify verify,
			IMockInteractions interactions,
			string methodName,
			Func<IInteraction, bool> predicate,
			Func<string> expectation)
			: this(verify, interactions, methodName, predicate, null, expectation)
		{
		}

		internal IgnoreParameters(
			TVerify verify,
			IMockInteractions interactions,
			string methodName,
			Func<IInteraction, bool> predicate,
			Func<IInteraction, bool>? overloadFilter,
			Func<string> expectation)
			: base(verify, interactions,
				interaction => MatchesMethodName(interaction, methodName) && predicate(interaction),
				expectation)
		{
			_methodName = methodName;
			_overloadFilter = overloadFilter;
		}

		internal IgnoreParameters(
			TVerify verify,
			IMockInteractions interactions,
			IFastMemberBuffer buffer,
			string methodName,
			Func<IInteraction, bool> predicate,
			Func<string> expectation)
			: base(verify, interactions, buffer, predicate, expectation)
		{
			_methodName = methodName;
		}

		internal IgnoreParameters(
			TVerify verify,
			IMockInteractions interactions,
			IFastMemberBuffer? buffer,
			IFastCountSource? fastCountSource,
			string methodName,
			Func<IInteraction, bool> predicate,
			Func<string> expectation)
			: base(verify, interactions, buffer, fastCountSource, predicate, expectation)
		{
			_methodName = methodName;
		}

		/// <summary>
		///     Replaces the explicit parameter matcher with <see cref="Match.AnyParameters()" />.
		/// </summary>
		public VerificationResult<TVerify> AnyParameters()
		{
			EnableCountAll();
			if (HasBuffer)
			{
				ReplacePredicate(static _ => true);
			}
			else
			{
				string methodName = _methodName;
				Func<IInteraction, bool>? overloadFilter = _overloadFilter;
				ReplacePredicate(overloadFilter is null
					? interaction => MatchesMethodName(interaction, methodName)
					: interaction => MatchesMethodName(interaction, methodName) && overloadFilter(interaction));
			}

			return this;
		}

		private static bool MatchesMethodName(IInteraction interaction, string methodName)
			=> interaction is IMethodInteraction method && method.Name == methodName;
	}
}
