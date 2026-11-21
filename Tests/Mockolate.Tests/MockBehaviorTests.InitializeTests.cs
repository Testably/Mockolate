using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed partial class MockBehaviorTests
{
	public sealed class InitializeTests
	{
		[Test]
		public async Task Initialize_DirectSetupsTakePrecedence()
		{
			MockBehavior behavior = MockBehavior.Default.Initialize<IChocolateDispenser>(setup
				=> setup[It.Satisfies((string s) => s.StartsWith("da"))].InitializeWith(5));

			IChocolateDispenser sut = IChocolateDispenser.CreateMock(behavior,
				setup => setup[It.Satisfies<string>(s => s.EndsWith("rk"))].InitializeWith(16));

			int bothMatchResult = sut["dark"];
			int directMatchResult = sut["--rk"];
			int behaviorMatchResult = sut["da--"];
			int noneMatchResult = sut["foo"];

			await That(bothMatchResult).IsEqualTo(16);
			await That(directMatchResult).IsEqualTo(16);
			await That(behaviorMatchResult).IsEqualTo(5);
			await That(noneMatchResult).IsEqualTo(0);
		}

		[Test]
		public async Task Initialize_MultipleForSameTypeFollowedByOtherType_ShouldUseLatest()
		{
			MockBehavior behavior = MockBehavior.Default
				.Initialize<IChocolateDispenser>((Mock.IMockSetupForIChocolateDispenser setup) => setup[It.Is("Dark")].InitializeWith(1))
				.Initialize<IChocolateDispenser>((Mock.IMockSetupForIChocolateDispenser setup) => setup[It.Is("Dark")].InitializeWith(2))
				.Initialize<IMyService>((Mock.IMockSetupForIMyService _) => { });

			IChocolateDispenser sut = IChocolateDispenser.CreateMock(behavior);

			await That(sut["Dark"]).IsEqualTo(2)
				.Because("a later initialization for another type must not change which initialization for the same type wins");
		}

		[Test]
		public async Task Initialize_OtherType_ShouldIgnoreInitializations()
		{
			MockBehavior behavior =
				MockBehavior.Default.Initialize<IChocolateDispenser>(setup
					=> setup[It.Is("Dark")].InitializeWith(15));

			void Act()
			{
				_ = IMyService.CreateMock(behavior);
			}

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task Initialize_ShouldApplySetupToCreatedMock()
		{
			MockBehavior behavior =
				MockBehavior.Default.Initialize<IChocolateDispenser>(setup
					=> setup[It.Is("Dark")].InitializeWith(15));

			IChocolateDispenser sut = IChocolateDispenser.CreateMock(behavior);

			int setupResult = sut["Dark"];
			int otherResult = sut["Light"];

			await That(setupResult).IsEqualTo(15);
			await That(otherResult).IsEqualTo(0);
		}
	}
}
