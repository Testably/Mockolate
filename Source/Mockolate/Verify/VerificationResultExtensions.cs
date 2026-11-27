using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Internals;

namespace Mockolate.Verify;

/// <summary>
///     Count-assertion extensions on <see cref="VerificationResult{TVerify}" /> that turn a recorded interaction set
///     into a pass/fail check (for example <c>.Once()</c>, <c>.AtLeast(3)</c>, <c>.Then(...)</c>).
/// </summary>
/// <remarks>
///     These methods are the terminators of a verification chain: each one either returns normally when the observed
///     interactions match the expectation, or throws a <see cref="MockVerificationException" />. Use <c>Within</c> or
///     <c>WithCancellation</c> on the <see cref="VerificationResult{TVerify}" /> before a terminator to wait for
///     interactions produced on a background thread.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public static class VerificationResultExtensions
{
	private static string ToTimes(this int amount, string verb = "")
		=> (amount, verb) switch
		{
			(0, "") => "never",
			(1, "") => "once",
			(2, "") => "twice",
			(_, "") => $"{amount} times",
			(0, _) => $"never {verb}",
			(1, _) => $"{verb} once",
			(2, _) => $"{verb} twice",
			(_, _) => $"{verb} {amount} times",
		};

	/// <summary>
	///     Routes count terminators to the allocation-free fast path when the result implements
	///     <see cref="ICountVerifiable" />; otherwise falls back to materialising the matching
	///     interactions through <see cref="IVerificationResult.Verify" /> and counting them. Lets
	///     external implementers of <see cref="IVerificationResult" /> remain whole-interface
	///     implementable while still letting the framework's own results take the fast path.
	/// </summary>
	internal static bool VerifyCount(this IVerificationResult result, Func<int, bool> countPredicate)
		=> result is ICountVerifiable countVerifiable
			? countVerifiable.VerifyCount(countPredicate)
			: result.Verify(arr => countPredicate(arr.Length));

	/// <summary>
	///     Checks that the count of the matching interactions lies between <paramref name="minimum" /> and
	///     <paramref name="maximum" /> (both inclusive), without a delegate or a closure on the fast path.
	/// </summary>
	private static bool VerifyCount(this IVerificationResult result, int minimum, int maximum, out int found)
		=> result is ICountVerifiable countVerifiable
			? countVerifiable.VerifyCount(minimum, maximum, out found)
			: VerifyCountByInteractions(result, minimum, maximum, out found);

	private static bool VerifyCountByInteractions(IVerificationResult result, int minimum, int maximum,
		out int found)
	{
		int count = 0;
		bool isValid = result.Verify(arr =>
		{
			count = arr.Length;
			return count >= minimum && count <= maximum;
		});
		found = count;
		return isValid;
	}

	private static void AssertCount(IVerificationResult result, int minimum, int maximum, string qualifier,
		int times)
	{
		try
		{
			if (!result.VerifyCount(minimum, maximum, out int found))
			{
				throw new MockVerificationException(
					$"Expected that mock {result.Expectation} {qualifier} {times.ToTimes()}, but it {found.ToTimes("did")}.");
			}
		}
		catch (MockVerificationTimeoutException timeoutException)
		{
			throw new MockVerificationException(
				$"Expected that mock {result.Expectation} {qualifier} {times.ToTimes()}, but {timeoutException.Message}.");
		}
	}

	extension<TMock>(VerificationResult<TMock> verificationResult)
	{
		/// <summary>
		///     Asserts that the verified interaction occurred at least <paramref name="times" /> times.
		/// </summary>
		/// <exception cref="MockVerificationException">
		///     Thrown when the verified interaction occurred fewer than <paramref name="times" /> times,
		///     or when a <see cref="VerificationResult{TVerify}.Within(TimeSpan)" /> timeout elapses first.
		/// </exception>
		public void AtLeast(int times)
			=> AssertCount(verificationResult, times, int.MaxValue, "at least", times);

		/// <summary>
		///     Asserts that the verified interaction occurred at least once.
		/// </summary>
		/// <exception cref="MockVerificationException">
		///     Thrown when the verified interaction did not occur,
		///     or when a <see cref="VerificationResult{TVerify}.Within(TimeSpan)" /> timeout elapses first.
		/// </exception>
		public void AtLeastOnce()
			=> AssertCount(verificationResult, 1, int.MaxValue, "at least", 1);

		/// <summary>
		///     Asserts that the verified interaction occurred at least twice.
		/// </summary>
		/// <exception cref="MockVerificationException">
		///     Thrown when the verified interaction occurred fewer than two times,
		///     or when a <see cref="VerificationResult{TVerify}.Within(TimeSpan)" /> timeout elapses first.
		/// </exception>
		public void AtLeastTwice()
			=> AssertCount(verificationResult, 2, int.MaxValue, "at least", 2);

		/// <summary>
		///     Asserts that the verified interaction occurred at most <paramref name="times" /> times.
		/// </summary>
		/// <exception cref="MockVerificationException">
		///     Thrown when the verified interaction occurred more than <paramref name="times" /> times,
		///     or when a <see cref="VerificationResult{TVerify}.Within(TimeSpan)" /> timeout elapses first.
		/// </exception>
		public void AtMost(int times)
			=> AssertCount(verificationResult, int.MinValue, times, "at most", times);

		/// <summary>
		///     Asserts that the verified interaction occurred between <paramref name="minimum" /> and
		///     <paramref name="maximum" /> times, inclusive.
		/// </summary>
		/// <exception cref="MockVerificationException">
		///     Thrown when the verified interaction count falls outside the
		///     <paramref name="minimum" />-<paramref name="maximum" /> range,
		///     or when a <see cref="VerificationResult{TVerify}.Within(TimeSpan)" /> timeout elapses first.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when <paramref name="minimum" /> is negative or <paramref name="maximum" /> is less than
		///     <paramref name="minimum" />.
		/// </exception>
		public void Between(int minimum, int maximum)
		{
			if (minimum < 0)
			{
				// ReSharper disable once LocalizableElement
				throw new ArgumentOutOfRangeException(nameof(minimum), "Minimum value must be non-negative.");
			}

			if (maximum < minimum)
			{
				throw new ArgumentOutOfRangeException(nameof(maximum),
					// ReSharper disable once LocalizableElement
					"Maximum value must be greater than or equal to minimum.");
			}

			IVerificationResult result = verificationResult;
			try
			{
				if (!result.VerifyCount(minimum, maximum, out int found))
				{
					throw new MockVerificationException(
						$"Expected that mock {result.Expectation} between {minimum} and {maximum} times, but it {found.ToTimes("did")}.");
				}
			}
			catch (MockVerificationTimeoutException timeoutException)
			{
				throw new MockVerificationException(
					$"Expected that mock {result.Expectation} between {minimum} and {maximum} times, but {timeoutException.Message}.");
			}
		}

		/// <summary>
		///     Asserts that the verified interaction occurred at most once.
		/// </summary>
		/// <exception cref="MockVerificationException">
		///     Thrown when the verified interaction occurred more than once, or when a preceding <c>Within</c> or
		///     <c>WithCancellation</c> causes verification to time out or be cancelled.
		/// </exception>
		public void AtMostOnce()
			=> AssertCount(verificationResult, int.MinValue, 1, "at most", 1);

		/// <summary>
		///     Asserts that the verified interaction occurred at most twice.
		/// </summary>
		/// <exception cref="MockVerificationException">
		///     Thrown when the verified interaction occurred more than two times, or when verification fails because a
		///     configured wait or cancellation condition times out or is cancelled.
		/// </exception>
		public void AtMostTwice()
			=> AssertCount(verificationResult, int.MinValue, 2, "at most", 2);

		/// <summary>
		///     Asserts that the verified interaction occurred exactly <paramref name="times" /> times.
		/// </summary>
		/// <exception cref="MockVerificationException">
		///     Thrown when the verified interaction count is not equal to <paramref name="times" />,
		///     or when a <see cref="VerificationResult{TVerify}.Within(TimeSpan)" /> timeout elapses first.
		/// </exception>
		public void Exactly(int times)
			=> AssertCount(verificationResult, times, times, "exactly", times);

		/// <summary>
		///     Asserts that the verified interaction never occurred.
		/// </summary>
		/// <exception cref="MockVerificationException">
		///     Thrown when the verified interaction occurred at least once,
		///     or when a <see cref="VerificationResult{TVerify}.Within(TimeSpan)" /> timeout elapses first.
		/// </exception>
		public void Never()
		{
			IVerificationResult result = verificationResult;
			try
			{
				if (!result.VerifyCount(0, 0, out int found))
				{
					throw new MockVerificationException(
						$"Expected that mock {0.ToTimes()} {result.Expectation}, but it {found.ToTimes("did")}.");
				}
			}
			catch (MockVerificationTimeoutException timeoutException)
			{
				throw new MockVerificationException(
					$"Expected that mock {0.ToTimes()} {result.Expectation}, but {timeoutException.Message}.");
			}
		}

		/// <summary>
		///     Asserts that the verified interaction occurred exactly once.
		/// </summary>
		/// <exception cref="MockVerificationException">
		///     Thrown when the verified interaction did not occur exactly once,
		///     or when a <see cref="VerificationResult{TVerify}.Within(TimeSpan)" /> timeout elapses first.
		/// </exception>
		public void Once()
			=> AssertCount(verificationResult, 1, 1, "exactly", 1);

		/// <summary>
		///     Asserts that the verified interaction occurred exactly twice.
		/// </summary>
		/// <exception cref="MockVerificationException">
		///     Thrown when the verified interaction did not occur exactly two times,
		///     or when a <see cref="VerificationResult{TVerify}.Within(TimeSpan)" /> timeout elapses first.
		/// </exception>
		public void Twice()
			=> AssertCount(verificationResult, 2, 2, "exactly", 2);

		/// <summary>
		///     Verifies that the mock was invoked according to the <paramref name="predicate" />.
		/// </summary>
		/// <param name="predicate">
		///     Receives the actual number of matching interactions and returns <see langword="true" /> if that count is
		///     acceptable.
		/// </param>
		/// <param name="doNotPopulateThisValue">
		///     Populated by the compiler via <see cref="System.Runtime.CompilerServices.CallerArgumentExpressionAttribute" />
		///     to include the source expression of <paramref name="predicate" /> in failure messages.
		/// </param>
		/// <exception cref="MockVerificationException">
		///     Thrown when <paramref name="predicate" /> returns <see langword="false" /> for the observed count,
		///     or when a <see cref="VerificationResult{TVerify}.Within(TimeSpan)" /> timeout elapses first.
		/// </exception>
		public void Times(Func<int, bool> predicate,
			[CallerArgumentExpression("predicate")]
			string doNotPopulateThisValue = "")
		{
			IVerificationResult result = verificationResult;
			try
			{
				int found = 0;
				if (!result.VerifyCount(count =>
				    {
					    found = count;
					    return predicate(count);
				    }))
				{
					throw new MockVerificationException(
						$"Expected that mock {result.Expectation} according to the predicate {doNotPopulateThisValue}, but it {found.ToTimes("did")}.");
				}
			}
			catch (MockVerificationTimeoutException timeoutException)
			{
				throw new MockVerificationException(
					$"Expected that mock {result.Expectation} according to the predicate {doNotPopulateThisValue}, but {timeoutException.Message}.");
			}
		}

		/// <summary>
		///     Asserts that the current verification and each of the <paramref name="orderedChecks" /> occurred in the
		///     specified order.
		/// </summary>
		/// <param name="orderedChecks">
		///     Each callback returns a follow-up <see cref="VerificationResult{TVerify}" />; that interaction must have been
		///     recorded strictly after the previous verification in the chain for the assertion to pass.
		/// </param>
		/// <exception cref="MockVerificationException">
		///     Thrown when the expected interactions did not occur in the given order
		///     (for example, a later interaction was recorded before an earlier one, or was missing).
		/// </exception>
		public void Then(params Func<TMock, VerificationResult<TMock>>[] orderedChecks)
		{
			TMock mockVerify = ((IVerificationResult<TMock>)verificationResult).Object;
			VerifyInOrderOnOneMock(verificationResult, orderedChecks.Length, i => orderedChecks[i](mockVerify));
		}

		/// <summary>
		///     Asserts that the current verification, <paramref name="next" /> and each of the <paramref name="more" />
		///     verifications occurred in the specified order, also when they belong to different mocks.
		/// </summary>
		/// <remarks>
		///     The order of calls on different mocks is only known while it is tracked: call <c>TrackCallOrder</c> on
		///     one of the mocks with the others (<c>sut.Mock.TrackCallOrder(other.Mock)</c>) before the calls. Only the
		///     calls until the returned scope is disposed are compared.
		/// </remarks>
		/// <param name="next">The verification whose interaction must have been recorded after the current one.</param>
		/// <param name="more">Further verifications, each recorded after the previous one.</param>
		/// <exception cref="MockVerificationException">
		///     Thrown when the expected interactions did not occur in the given order.
		/// </exception>
		/// <exception cref="MockException">
		///     Thrown when the verifications belong to different mocks whose call order was not tracked together.
		/// </exception>
		public void Then(IVerificationResult next, params IVerificationResult[] more)
		{
			IVerificationResult[] steps = [verificationResult, next, ..more,];
			if (steps.All(step => ReferenceEquals(step.Interactions, steps[0].Interactions)))
			{
				VerifyInOrderOnOneMock(steps[0], steps.Length - 1, i => steps[i + 1]);
			}
			else
			{
				CallOrderTracker.VerifyInOrder(steps);
			}
		}
	}

	private static void VerifyInOrderOnOneMock(IVerificationResult first, int followingCount,
		Func<int, IVerificationResult> getFollowing)
	{
		string? error = null;
		List<string> expectations = [];
		IVerificationResult result = first;
		IInteraction[] snapshot = result.Interactions.ToArray();

		int after = -1;
		for (int i = 0; i < followingCount; i++)
		{
			expectations.Add(result.Expectation);
			IVerificationResult currentResult = result;
			// In case of an error, `after` is set to int.MaxValue and all following verifications will fail
			_ = result.Verify(interactions => VerifyInteractions(interactions, currentResult));
			result = getFollowing(i);
		}

		expectations.Add(result.Expectation);
		if (!result.Verify(interactions => VerifyInteractions(interactions, result)))
		{
			string separator = ", then ";
			throw new MockVerificationException(
				$"Expected that mock {string.Join(separator, expectations)} in order, but it {error}.");
		}

		bool VerifyInteractions(IInteraction[] interactions, IVerificationResult currentResult)
		{
			// Walk the snapshot from `after + 1` and stop on the first slot whose interaction
			// is in the verification's matched set. The membership check uses reference
			// equality, but the search is positional — repeated entries with the same
			// reference (e.g. shared property-getter access singletons) still resolve to
			// distinct positions because each call to this lambda picks up where the
			// previous one stopped.
			int firstPos = -1;
			// `after == int.MaxValue` is the "earlier step already failed" signal — skip the
			// walk so subsequent steps cascade to failure without going through the loop
			// (and without triggering int overflow on `after + 1`).
			if (after < snapshot.Length)
			{
				HashSet<IInteraction> matched = new(interactions);
				for (int i = after + 1; i < snapshot.Length; i++)
				{
					if (matched.Contains(snapshot[i]))
					{
						firstPos = i;
						break;
					}
				}
			}

			bool hasInteractionAfter = firstPos >= 0;
			after = hasInteractionAfter ? firstPos : int.MaxValue;
			if (!hasInteractionAfter && error is null)
			{
				error = interactions.Length > 0
					? $"{currentResult.Expectation} too early"
					: $"{currentResult.Expectation} not at all";
			}

			return hasInteractionAfter;
		}
	}
}
