using Mockolate.Exceptions;

namespace Mockolate.Tests.MockProperties;

public sealed class VerifyGotTests
{
	[Test]
	public async Task ShouldIncreaseInvocationCountOfGetter()
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();

		_ = sut.Counter;

		await That(sut.Mock.Verify.Counter.Got()).Once();
		await That(sut.Mock.Verify.Counter.Set(It.IsAny<int>())).Never();
	}

	[Test]
	public async Task ShouldReturnInitializedValue()
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();
		sut.Mock.Setup.Counter.InitializeWith(24);

		int result = sut.Counter;

		await That(result).IsEqualTo(24);
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task ShouldThrowMockNotSetupExceptionWhenBehaviorIsSetToThrow(bool throwWhenNotSetup)
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock(MockBehavior.Default with
		{
			ThrowWhenNotSetup = throwWhenNotSetup,
		});

		void Act()
		{
			_ = sut.IsValid;
		}

		await That(Act).Throws<MockNotSetupException>().OnlyIf(throwWhenNotSetup)
			.WithMessage("""
			             The property 'global::Mockolate.Tests.MockTests.IMyService.IsValid' was accessed without prior setup.
			             """);
	}
}
