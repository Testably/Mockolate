using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Verify;

namespace Mockolate.Internals;

/// <summary>
///     Tracks the order of the calls across several mocks, from its creation until it is disposed.
/// </summary>
/// <remarks>
///     The tracker keeps its own list of the calls of the tracked mocks instead of numbering the records of the mocks,
///     so that a mock whose call order is not tracked needs no memory for it, and the calls of different trackers are
///     never compared.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class CallOrderTracker : IDisposable
{
	private readonly TrackedMock[] _mocks;
	private int _isRunning = 1;
	private TrackedCall? _newest;

	private CallOrderTracker(IReadOnlyList<(string Name, MockInteractions Interactions)> mocks)
	{
		_mocks = new TrackedMock[mocks.Count];
		for (int i = 0; i < mocks.Count; i++)
		{
			(string name, MockInteractions interactions) = mocks[i];
			string displayName = mocks.Where(mock => string.Equals(mock.Name, name, StringComparison.Ordinal)).Skip(1).Any()
				? $"{name} #{mocks.Take(i + 1).Count(mock => string.Equals(mock.Name, name, StringComparison.Ordinal))}"
				: name;
			_mocks[i] = new TrackedMock(this, displayName, interactions);
		}
	}

	private bool IsRunning => Volatile.Read(ref _isRunning) == 1;

	private int NewestOrder => Volatile.Read(ref _newest)?.Order ?? 0;

	/// <inheritdoc cref="IDisposable.Dispose()" />
	public void Dispose()
		=> Volatile.Write(ref _isRunning, 0);

	/// <summary>
	///     Starts tracking the order of the calls across the <paramref name="mocks" />.
	/// </summary>
	/// <exception cref="MockException">The call order of one of the <paramref name="mocks" /> is already tracked.</exception>
	internal static CallOrderTracker Track(IReadOnlyList<(string Name, MockInteractions Interactions)> mocks)
	{
		CallOrderTracker tracker = new(mocks);
		TrackedMock?[] previous = new TrackedMock?[tracker._mocks.Length];
		for (int i = 0; i < tracker._mocks.Length; i++)
		{
			TrackedMock mock = tracker._mocks[i];
			if (!mock.Interactions.TryTrackCallOrder(mock, out previous[i]))
			{
				for (int j = 0; j < i; j++)
				{
					tracker._mocks[j].Interactions.RestoreCallOrder(tracker._mocks[j], previous[j]);
				}

				tracker.Dispose();
				throw new MockException(
					$"The call order of the {mocks[i].Name} is already tracked. Dispose the previous scope before tracking it again.");
			}
		}

		return tracker;
	}

	/// <summary>
	///     Verifies that each of the <paramref name="steps" />, which belong to different mocks, matched a call after the
	///     call that the previous step matched.
	/// </summary>
	/// <exception cref="MockException">The call order of the mocks was not tracked together.</exception>
	/// <exception cref="MockVerificationException">The calls did not occur in the order of the <paramref name="steps" />.</exception>
	internal static void VerifyInOrder(IVerificationResult[] steps)
	{
		TrackedMock[] stepMocks = GetTrackedMocks(steps);
		CallOrderTracker tracker = stepMocks[0].Tracker;

		int after = 0;
		string? error = null;
		for (int i = 0; i < steps.Length && error is null; i++)
		{
			error = tracker.VerifyStep(steps[i], stepMocks[i], ref after);
		}

		if (error is not null)
		{
			string expectations = string.Join(", then ",
				steps.Select((step, i) => $"{stepMocks[i].Name} {step.Expectation}"));
			throw new MockVerificationException(
				$"Expected that {expectations} in order, but {error}.{tracker.DescribeRecordedOrder()}");
		}
	}

	private string? VerifyStep(IVerificationResult step, TrackedMock mock, ref int after)
	{
		int previous = after;
		bool hasMatches = false;
		bool hasTrackedMatches = false;
		int found = 0;
		try
		{
			if (step.Verify(matching =>
			    {
				    hasMatches = matching.Length > 0;
				    found = FindFirstAfter(mock, matching, previous, out hasTrackedMatches);
				    return found > 0;
			    }))
			{
				after = found;
				return null;
			}
		}
		catch (MockVerificationTimeoutException timeoutException)
		{
			return $"{timeoutException.Message} waiting until {mock.Name} {step.Expectation}";
		}

		string reason = (hasMatches, hasTrackedMatches) switch
		{
			(false, _) => "not at all",
			(true, false) => "not while the call order was tracked",
			(true, true) => "too early",
		};
		return $"{mock.Name} {step.Expectation} {reason}";
	}

	private int FindFirstAfter(TrackedMock mock, IInteraction[] matching, int after, out bool hasTrackedMatches)
	{
		HashSet<IInteraction> matched = new(matching);
		hasTrackedMatches = false;
		int found = 0;
		foreach (TrackedCall call in GetCalls().Where(call =>
			         call.Mock == mock && call.Node.Boxed is { } interaction && matched.Contains(interaction)))
		{
			hasTrackedMatches = true;
			if (call.Order > after && (found == 0 || call.Order < found))
			{
				found = call.Order;
			}
		}

		return found;
	}

	private static TrackedMock[] GetTrackedMocks(IVerificationResult[] steps)
	{
		TrackedMock?[] mocks = steps
			.Select(step => (step.Interactions as MockInteractions)?.TrackedCallOrder)
			.ToArray();
		IVerificationResult[] untracked = steps.Where((_, i) => mocks[i] is null).ToArray();
		if (untracked.Length > 0)
		{
			throw new MockException(
				$"The call order of the {GetMockNames(untracked)} was not tracked. Call `TrackCallOrder` on one of the mocks with the others before the calls, e.g. `sut.Mock.TrackCallOrder(other.Mock)`.");
		}

		if (mocks.Any(mock => mock!.Tracker != mocks[0]!.Tracker))
		{
			throw new MockException(
				$"The call order of the {GetMockNames(steps)} was not tracked in the same scope. A mock only keeps the call order of the latest scope that tracked it.");
		}

		return mocks!;

		static string GetMockNames(IEnumerable<IVerificationResult> steps)
			=> string.Join(" and the ", steps
				.GroupBy(step => step.Interactions)
				.Select(group => group.First() is IVerificationResult<object> { Object: IMock mock, }
					? mock.ToString()
					: "mock"));
	}

	private IEnumerable<TrackedCall> GetCalls()
	{
		for (TrackedCall? call = Volatile.Read(ref _newest); call is not null; call = call.Next)
		{
			if (call.Order > call.Mock.ClearedOrder)
			{
				yield return call;
			}
		}
	}

	private void Record(TrackedMock mock, RecordNode node)
	{
		TrackedCall call = new(mock, node);
		TrackedCall? newest = Volatile.Read(ref _newest);
		while (true)
		{
			call.Next = newest;
			call.Order = newest is null ? 1 : newest.Order + 1;
			TrackedCall? previous = Interlocked.CompareExchange(ref _newest, call, newest);
			if (ReferenceEquals(previous, newest))
			{
				return;
			}

			newest = previous;
		}
	}

	private string DescribeRecordedOrder()
	{
		string[] calls = GetCalls().Reverse().Select(call => $"{call.Mock.Name}: {call.Node.Interaction}").ToArray();
		if (calls.Length == 0)
		{
			return " No calls were recorded while the call order was tracked.";
		}

		StringBuilder sb = new();
		sb.Append(Environment.NewLine).Append("Recorded order:");
		for (int i = 0; i < calls.Length; i++)
		{
			sb.Append(Environment.NewLine).Append("  ").Append(i + 1).Append(". ").Append(calls[i]);
		}

		return sb.ToString();
	}

	/// <summary>
	///     A mock whose call order a <see cref="CallOrderTracker" /> tracks.
	/// </summary>
	internal sealed class TrackedMock(CallOrderTracker tracker, string name, MockInteractions interactions)
	{
		private int _clearedOrder;

		/// <summary>The tracker of the call order.</summary>
		public CallOrderTracker Tracker { get; } = tracker;

		/// <summary>The name of the mock in failure messages.</summary>
		public string Name { get; } = name;

		/// <summary>The interactions of the mock.</summary>
		public MockInteractions Interactions { get; } = interactions;

		/// <summary>The order of the newest call that was removed by clearing the interactions of the mock.</summary>
		public int ClearedOrder => Volatile.Read(ref _clearedOrder);

		/// <summary>Whether the <see cref="Tracker" /> still records the calls of the mock.</summary>
		public bool IsRunning => Tracker.IsRunning;

		/// <summary>Records the <paramref name="node" /> while the <see cref="Tracker" /> is running.</summary>
		public void Record(RecordNode node)
		{
			if (Tracker.IsRunning)
			{
				Tracker.Record(this, node);
			}
		}

		/// <summary>Ignores the calls that were recorded so far, because the interactions of the mock were cleared.</summary>
		public void Clear()
			=> Volatile.Write(ref _clearedOrder, Tracker.NewestOrder);
	}

	private sealed class TrackedCall(TrackedMock mock, RecordNode node)
	{
		public TrackedMock Mock { get; } = mock;
		public RecordNode Node { get; } = node;
		public int Order { get; set; }
		public TrackedCall? Next { get; set; }
	}
}
