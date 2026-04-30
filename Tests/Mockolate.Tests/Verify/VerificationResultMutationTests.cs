using System.Collections.Generic;
using aweXpect.Chronology;
using Mockolate.Exceptions;
using Mockolate.Tests.TestHelpers;
using Mockolate.Verify;

namespace Mockolate.Tests.Verify;

public sealed class VerificationResultMutationTests
{
	[Test]
	public async Task AwaitableVerify_AfterMatching_ShouldMarkMatchingInteractionsAsVerified()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.SomeFlag.InitializeWith(true);
		_ = sut.SomeFlag;

		sut.Mock.Verify.SomeFlag.Got().Within(10.Milliseconds()).Exactly(1);

		IMockInteractions interactions = ((IMock)sut).MockRegistry.Interactions;
		await That(interactions.GetUnverifiedInteractions()).IsEmpty();
	}

	[Test]
	public async Task AwaitableVerify_RecordsArrayPath_AfterSuccess_MarksMatchingInteractionsAsVerified()
	{
		// Pins the `_interactions.Verified(matchingInteractions);` call inside Awaitable's
		// IVerificationResult.Verify (the records-array path). Statement-removal would skip the
		// bookkeeping and leave the matching record in the unverified set — but the predicate
		// still returns true, so the failure surfaces only via GetUnverifiedInteractions().
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Dispense("Dark", 1);

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result =
			sut.Mock.Verify.Dispense(Match.AnyParameters()).Within(50.Milliseconds());

		bool verified = ((IVerificationResult)result).Verify(arr => arr.Length > 0);

		await That(verified).IsTrue();
		IMockInteractions interactions = ((IMock)sut).MockRegistry.Interactions;
		await That(interactions.GetUnverifiedInteractions()).IsEmpty();
	}

	[Test]
	public async Task AwaitableVerify_WithRecordingDisabled_ShouldThrowMockException()
	{
		IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
		sut.Mock.Setup.SomeFlag.InitializeWith(true);
		_ = sut.SomeFlag;

		void Act()
		{
			sut.Mock.Verify.SomeFlag.Got().Within(10.Milliseconds()).Exactly(1);
		}

		await That(Act).Throws<MockException>()
			.WithMessage("*interaction recording is disabled*").AsWildcard();
	}

	[Test]
	public async Task AwaitableVerifyCount_NoFastCountSource_AfterSuccess_MarksMatchingInteractionsAsVerified()
	{
		// Pins the `_interactions.Verified(matchingInteractions);` call inside Awaitable's
		// IFastVerifyCountResult.VerifyCount second branch (when `_fastCountSource is null`).
		// IndexerGot(memberId, ...) returns a VerificationResult with a buffer but no fast-count
		// source, so wrapping it with .Within(...) walks exactly that branch.
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int> buffer = store.GetOrCreateBuffer<FastIndexerGetterBuffer<int>>(0,
			static f => new FastIndexerGetterBuffer<int>(f));
		buffer.Append(1);
		MockRegistry registry = new(MockBehavior.Default, store);

		registry.IndexerGot(new object(), 0,
				interaction => interaction is IndexerGetterAccess<int> g && g.Parameter1 == 1,
				() => "[1]")
			.Within(50.Milliseconds()).Once();

		await That(((IMockInteractions)store).GetUnverifiedInteractions()).IsEmpty();
	}

	[Test]
	public async Task CollectMatching_WithMultipleRecordsOutOfSeqOrder_SortsBySeqBeforeReturning()
	{
		// Kills the `records.Sort(...)` statement removal and the `records.Count > 1` → `<= 1`
		// flip in CollectMatching's buffer-walking branch. With a custom buffer that emits
		// records in non-Seq order, the sort is the only thing that puts them back in seq
		// order — without it, the user predicate sees the buffer's natural order and the
		// assertion below fails.
		FastMockInteractions store = new(1);
		MethodInvocation<int> rec5 = new("Foo", 5);
		MethodInvocation<int> rec1 = new("Foo", 1);
		MethodInvocation<int> rec3 = new("Foo", 3);
		OutOfOrderBuffer buffer = new(
		[
			(5L, rec5),
			(1L, rec1),
			(3L, rec3),
		]);
		store.Buffers[0] = buffer;
		MockRegistry registry = new(MockBehavior.Default, store);

		VerificationResult<object>.IgnoreParameters result =
			registry.VerifyMethod<object, MethodInvocation<int>>(
				new object(), 0, "Foo", _ => true, () => "Foo");

		List<int> values = [];
		bool verified = ((IVerificationResult)result).Verify(arr =>
		{
			foreach (IInteraction interaction in arr)
			{
				values.Add(((MethodInvocation<int>)interaction).Parameter1);
			}

			return arr.Length == 3;
		});

		await That(verified).IsTrue();
		await That(values).IsEqualTo([1, 3, 5,]);
	}

	[Test]
	public async Task SyncVerifyCount_NoFastCountSource_AfterSuccess_MarksMatchingInteractionsAsVerified()
	{
		// Pins the `_interactions.Verified(matchingInteractions);` call inside the non-Awaitable
		// IFastVerifyCountResult.VerifyCount second branch (when `_fastCountSource is null`).
		FastMockInteractions store = new(1);
		FastIndexerGetterBuffer<int> buffer = store.GetOrCreateBuffer<FastIndexerGetterBuffer<int>>(0,
			static f => new FastIndexerGetterBuffer<int>(f));
		buffer.Append(1);
		MockRegistry registry = new(MockBehavior.Default, store);

		registry.IndexerGot(new object(), 0,
			interaction => interaction is IndexerGetterAccess<int> g && g.Parameter1 == 1,
			() => "[1]").Once();

		await That(((IMockInteractions)store).GetUnverifiedInteractions()).IsEmpty();
	}

	private sealed class OutOfOrderBuffer : IFastMemberBuffer
	{
		private readonly List<(long Seq, IInteraction Interaction)> _records;

		public OutOfOrderBuffer(IEnumerable<(long, IInteraction)> records)
		{
			_records = new List<(long, IInteraction)>();
			foreach ((long seq, IInteraction interaction) in records)
			{
				_records.Add((seq, interaction));
			}
		}

		public int Count => _records.Count;

		public void Clear() => _records.Clear();

		public void AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
		{
			foreach ((long Seq, IInteraction Interaction) record in _records)
			{
				dest.Add(record);
			}
		}

		public void AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
			=> AppendBoxed(dest);
	}
}
