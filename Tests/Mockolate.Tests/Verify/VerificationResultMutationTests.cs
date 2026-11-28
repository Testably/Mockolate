using System.Collections.Generic;
using aweXpect.Chronology;
using Mockolate.Exceptions;
using Mockolate.TestHelpers;
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
	public async Task AwaitableVerifyCount_NoRecordCountSource_AfterSuccess_MarksMatchingInteractionsAsVerified()
	{
		// Pins the `_interactions.Verified(matchingInteractions);` call inside Awaitable's
		// ICountVerifiable.VerifyCount second branch (when `_recordCountSource is null`).
		// IndexerGot(memberId, ...) returns a VerificationResult with a buffer but no fast-count
		// source, so wrapping it with .Within(...) walks exactly that branch.
		MockInteractions store = new(1);
		IndexerGetterRecords<int>.Append(store, 0, 1);
		MockRegistry registry = new(MockBehavior.Default, store);

		registry.IndexerGot(new object(), 0,
				interaction => interaction is IndexerGetterAccess<int> g && g.Parameter1 == 1,
				() => "[1]")
			.Within(50.Milliseconds()).Once();

		await That(((IMockInteractions)store).GetUnverifiedInteractions()).IsEmpty();
	}

	[Test]
	public async Task CollectMatching_WithMultipleRecords_ShouldReturnThemInRecordedOrder()
	{
		MockInteractions store = new(1);
		MethodRecords<int>.Append(store, 0, "Foo", 1);
		MethodRecords<int>.Append(store, 0, "Foo", 3);
		MethodRecords<int>.Append(store, 0, "Foo", 5);
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
	public async Task SyncVerifyCount_NoRecordCountSource_AfterSuccess_MarksMatchingInteractionsAsVerified()
	{
		// Pins the `_interactions.Verified(matchingInteractions);` call inside the non-Awaitable
		// ICountVerifiable.VerifyCount second branch (when `_recordCountSource is null`).
		MockInteractions store = new(1);
		IndexerGetterRecords<int>.Append(store, 0, 1);
		MockRegistry registry = new(MockBehavior.Default, store);

		registry.IndexerGot(new object(), 0,
			interaction => interaction is IndexerGetterAccess<int> g && g.Parameter1 == 1,
			() => "[1]").Once();

		await That(((IMockInteractions)store).GetUnverifiedInteractions()).IsEmpty();
	}

}
