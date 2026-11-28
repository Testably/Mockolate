using Mockolate.Exceptions;
using Mockolate.TestHelpers;

namespace Mockolate.Tests.Behavior;

public sealed partial class MockBehaviorTests
{
	public sealed class SkipInteractionRecordingTests
	{
		[Test]
		public async Task Default_ShouldBeFalse()
		{
			MockBehavior sut = MockBehavior.Default;

			await That(sut.SkipInteractionRecording).IsFalse();
		}

		[Test]
		public async Task SkippingInteractionRecording_ShouldSupportFluentSyntax()
		{
			MockBehavior sut = MockBehavior.Default.SkippingInteractionRecording();

			await That(sut.SkipInteractionRecording).IsTrue();
		}

		[Test]
		public async Task SkippingInteractionRecording_WithFalse_ShouldEnableRecording()
		{
			MockBehavior sut = MockBehavior.Default
				.SkippingInteractionRecording()
				.SkippingInteractionRecording(false);

			await That(sut.SkipInteractionRecording).IsFalse();
		}

		[Test]
		public async Task Indexer_WhenSkipping_ShouldStillStoreAndReturnValue()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
			sut[7] = "hello";

			string result = sut[7];

			await That(result).IsEqualTo("hello");
		}

		[Test]
		public async Task Method_WhenSkipping_ShouldStillReturnSetupValue()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
			sut.Mock.Setup.IsValid(It.IsAny<int>()).Returns(true);

			bool result = sut.IsValid(42);

			await That(result).IsTrue();
		}

		[Test]
		public async Task WhenSkipping_ShouldNotRecordAnyInteraction()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
			sut.Mock.Setup.SomeFlag.InitializeWith(true);
			sut.Mock.Setup.IsValid(It.IsAny<int>()).Returns(true);

			_ = sut.SomeFlag;
			sut.SomeFlag = false;
			_ = sut.IsValid(1);

			IMockInteractions interactions = ((IMock)sut).MockRegistry.Interactions;
			await That(interactions.Count).IsEqualTo(0);
		}

		[Test]
		public async Task PropertyGetter_WhenSkipping_ShouldStillReturnSetupValue()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
			sut.Mock.Setup.SomeFlag.InitializeWith(true);

			bool result = sut.SomeFlag;

			await That(result).IsTrue();
		}

		[Test]
		public async Task PropertySetter_WhenSkipping_ShouldStillStoreValue()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
			sut.Mock.Setup.SomeFlag.InitializeWith(false);

			sut.SomeFlag = true;
			bool result = sut.SomeFlag;

			await That(result).IsTrue();
		}

		[Test]
		public async Task Verify_WhenSkipping_ShouldThrow()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
			sut.Mock.Setup.SomeFlag.InitializeWith(true);

			_ = sut.SomeFlag;

			await That(async () => await That(sut.Mock.Verify.SomeFlag.Got()).Exactly(1))
				.Throws<MockException>()
				.WithMessage("*SkipInteractionRecording*").AsWildcard();
		}

		[Test]
		public async Task WithRecordingEnabled_ShouldRecordAsBefore()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default);
			sut.Mock.Setup.SomeFlag.InitializeWith(true);

			_ = sut.SomeFlag;
			_ = sut.SomeFlag;

			await That(sut.Mock.Verify.SomeFlag.Got()).Exactly(2);
		}
	}
}
