using Mockolate.Exceptions;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed partial class MockBehaviorTests
{
	public sealed class SkipInteractionRecordingTests
	{
		[Fact]
		public async Task Default_ShouldBeFalse()
		{
			MockBehavior sut = MockBehavior.Default;

			await That(sut.SkipInteractionRecording).IsFalse();
		}

		[Fact]
		public async Task SkippingInteractionRecording_ShouldSupportFluentSyntax()
		{
			MockBehavior sut = MockBehavior.Default.SkippingInteractionRecording();

			await That(sut.SkipInteractionRecording).IsTrue();
		}

		[Fact]
		public async Task SkippingInteractionRecording_WithFalse_ShouldEnableRecording()
		{
			MockBehavior sut = MockBehavior.Default
				.SkippingInteractionRecording()
				.SkippingInteractionRecording(false);

			await That(sut.SkipInteractionRecording).IsFalse();
		}

		[Fact]
		public async Task WhenSkipping_Indexer_ShouldStillStoreAndReturnValue()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
			sut[7] = "hello";

			string result = sut[7];

			await That(result).IsEqualTo("hello");
		}

		[Fact]
		public async Task WhenSkipping_Method_ShouldStillReturnSetupValue()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
			sut.Mock.Setup.IsValid(It.IsAny<int>()).Returns(true);

			bool result = sut.IsValid(42);

			await That(result).IsTrue();
		}

		[Fact]
		public async Task WhenSkipping_NoInteractionsAreRecorded()
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

		[Fact]
		public async Task WhenSkipping_PropertyGetter_ShouldStillReturnSetupValue()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
			sut.Mock.Setup.SomeFlag.InitializeWith(true);

			bool result = sut.SomeFlag;

			await That(result).IsTrue();
		}

		[Fact]
		public async Task WhenSkipping_PropertySetter_ShouldStillStoreValue()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
			sut.Mock.Setup.SomeFlag.InitializeWith(false);

			sut.SomeFlag = true;
			bool result = sut.SomeFlag;

			await That(result).IsTrue();
		}

		[Fact]
		public async Task WhenSkipping_Verify_ShouldThrow()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
			sut.Mock.Setup.SomeFlag.InitializeWith(true);

			_ = sut.SomeFlag;

			await That(async () => await That(sut.Mock.Verify.SomeFlag.Got()).Exactly(1))
				.Throws<MockException>()
				.WithMessage("*SkipInteractionRecording*").AsWildcard();
		}

		[Fact]
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
