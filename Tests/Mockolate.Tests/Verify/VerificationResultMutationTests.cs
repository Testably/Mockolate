using aweXpect.Chronology;
using Mockolate.Exceptions;
using Mockolate.TestHelpers;
using Mockolate.Verify;

namespace Mockolate.Tests.Verify;

public sealed class VerificationResultMutationTests
{
	[Test]
	public async Task WithTimeout_WhenMatching_ShouldMarkMatchingInteractionsAsVerified()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.SomeFlag.InitializeWith(true);
		_ = sut.SomeFlag;

		sut.Mock.Verify.SomeFlag.Got().Within(10.Milliseconds()).Exactly(1);

		IMockInteractions interactions = ((IMock)sut).MockRegistry.Interactions;
		await That(interactions.GetUnverifiedInteractions()).IsEmpty();
	}

	[Test]
	public async Task WithTimeout_WhenVerifyingAPredicate_ShouldMarkMatchingInteractionsAsVerified()
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
	public async Task WithTimeoutAndRecordingDisabled_ShouldThrowMockException()
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
}
