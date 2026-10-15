namespace Mockolate.Tests;

public sealed class MockBehaviorExtensionsTests
{
	[Test]
	public async Task SkippingBaseClass_ShouldSetSkipBaseClass()
	{
		MockBehavior sut = MockBehavior.Default;

		MockBehavior result = sut.SkippingBaseClass();

		await That(result.SkipBaseClass).IsTrue();
	}

	[Test]
	public async Task SkippingBaseClass_WithFalse_ShouldUpdateSkipBaseClass()
	{
		bool initializedValue = true;
		MockBehavior sut = MockBehavior.Default.SkippingBaseClass(initializedValue);

		MockBehavior result = sut.SkippingBaseClass(false);

		await That(result.SkipBaseClass).IsFalse();
	}

	[Test]
	public async Task ThrowingWhenNotSetup_ShouldSetThrowWhenNotSetup()
	{
		MockBehavior sut = MockBehavior.Default;

		MockBehavior result = sut.ThrowingWhenNotSetup();

		await That(result.ThrowWhenNotSetup).IsTrue();
	}

	[Test]
	public async Task ThrowingWhenNotSetup_WithFalse_ShouldUpdateThrowWhenNotSetup()
	{
		bool initializedValue = true;
		MockBehavior sut = MockBehavior.Default.ThrowingWhenNotSetup(initializedValue);

		MockBehavior result = sut.ThrowingWhenNotSetup(false);

		await That(result.ThrowWhenNotSetup).IsFalse();
	}
}
