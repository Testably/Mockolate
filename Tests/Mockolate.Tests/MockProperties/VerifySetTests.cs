using Mockolate.Exceptions;

namespace Mockolate.Tests.MockProperties;

public sealed class VerifySetTests
{
	[Test]
	public async Task Set_WithExplicitParameter_ShouldCheckForEquality()
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();

		sut.Counter = 42;

		await That(sut.Mock.Verify.Counter.Set(41)).Never();
		await That(sut.Mock.Verify.Counter.Set(42)).Once();
		await That(sut.Mock.Verify.Counter.Set(43)).Never();
	}

	[Test]
	public async Task Set_WithExplicitReferenceTypeParameter_ShouldCheckForEquality()
	{
		IServiceWithStringProperty sut = IServiceWithStringProperty.CreateMock();

		sut.Name = "hello";

		await That(sut.Mock.Verify.Name.Set("goodbye")).Never();
		await That(sut.Mock.Verify.Name.Set("hello")).Once();
	}

	[Test]
	public async Task ShouldIncreaseInvocationCountOfGetter()
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();

		sut.Counter = 42;

		await That(sut.Mock.Verify.Counter.Got()).Never();
		await That(sut.Mock.Verify.Counter.Set(It.IsAny<int>())).Once();
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
			sut.IsValid = true;
		}

		await That(Act).Throws<MockNotSetupException>().OnlyIf(throwWhenNotSetup)
			.WithMessage("""
			             The property 'global::Mockolate.Tests.MockTests.IMyService.IsValid' was accessed without prior setup.
			             """);
	}

	[Test]
	public async Task ShouldUpdateValueForNextGet()
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();

		int result1 = sut.Counter;
		sut.Counter = 5;
		int result2 = sut.Counter;

		await That(result1).IsEqualTo(0);
		await That(result2).IsEqualTo(5);
	}

	[Test]
	public async Task WithNull_ShouldUpdateValueForNextGet()
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();
		sut.Mock.Setup.IsValid.InitializeWith(true);

		bool? result1 = sut.IsValid;
		sut.IsValid = null;
		bool? result2 = sut.IsValid;

		await That(result1).IsEqualTo(true);
		await That(result2).IsEqualTo(null);
	}

	public interface IServiceWithStringProperty
	{
		string Name { get; set; }
	}
}
