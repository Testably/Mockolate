using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed partial class MockBehaviorTests
{
	[Test]
	public async Task Default_ShouldInitializeCorrectly()
	{
		IMyService sut = IMyService.CreateMock();
		MockBehavior behavior = ((IMock)sut).MockRegistry.Behavior;

		await That(behavior.SkipBaseClass).IsFalse();
		await That(behavior.ThrowWhenNotSetup).IsFalse();
		await That(behavior.DefaultValue).IsNotNull();
	}

	[Test]
	public async Task ShouldSupportCustomDefaultValueGenerator()
	{
		MockBehavior sut = new(new MyDefaultValueGenerator());

		await That(sut.SkipBaseClass).IsFalse();
		await That(sut.ThrowWhenNotSetup).IsFalse();
		await That(sut.DefaultValue.Generate("")).IsEqualTo("foo");
		await That(sut.DefaultValue.Generate(0)).IsEqualTo(0);
	}

	[Test]
	public async Task ShouldSupportWithSyntax()
	{
		MockBehavior sut = MockBehavior.Default with
		{
			SkipBaseClass = true,
			ThrowWhenNotSetup = true,
			DefaultValue = new MyDefaultValueGenerator(),
		};

		await That(sut.SkipBaseClass).IsTrue();
		await That(sut.ThrowWhenNotSetup).IsTrue();
		await That(sut.DefaultValue.Generate("")).IsEqualTo("foo");
		await That(sut.DefaultValue.Generate(0)).IsEqualTo(0);
	}

	[Test]
	public async Task ToString_Default_ShouldReturnDefault()
	{
		MockBehavior sut = MockBehavior.Default;

		string result = sut.ToString();

		await That(result).IsEqualTo("Default");
	}

	[Test]
	public async Task ToString_SkippingBaseClass_ShouldReturnExpectedValue()
	{
		MockBehavior sut = MockBehavior.Default.SkippingBaseClass();

		string result = sut.ToString();

		await That(result).IsEqualTo("SkippingBaseClass");
	}

	[Test]
	public async Task ToString_SkippingBaseClassAndThrowingWhenNotSetup_ShouldReturnExpectedValue()
	{
		MockBehavior sut = MockBehavior.Default with
		{
			SkipBaseClass = true,
			ThrowWhenNotSetup = true,
		};

		string result = sut.ToString();

		await That(result).IsEqualTo("ThrowingWhenNotSetup and SkippingBaseClass");
	}

	[Test]
	public async Task ToString_ThrowingWhenNotSetup_ShouldReturnExpectedValue()
	{
		MockBehavior sut = MockBehavior.Default.ThrowingWhenNotSetup();

		string result = sut.ToString();

		await That(result).IsEqualTo("ThrowingWhenNotSetup");
	}

	[Test]
	public async Task ToString_WithSetups_ShouldReturnExpectedValue()
	{
		MockBehavior sut = MockBehavior.Default.SkippingBaseClass()
			.UseConstructorParametersFor<MyServiceBase>(1)
			.UseConstructorParametersFor<IChocolateDispenser>("foo", "bar")
			.Initialize<IChocolateDispenser>(_ => { });

		string result = sut.ToString();

		await That(result).IsEqualTo("SkippingBaseClass with 2 constructor parameter registrations with 1 setup registrations");
	}

	[Test]
	public async Task ToString_WithUseConstructorParametersFor_ShouldReturnExpectedValue()
	{
		MockBehavior sut = MockBehavior.Default
			.UseConstructorParametersFor<MyServiceBase>(1)
			.UseConstructorParametersFor<IChocolateDispenser>("foo", "bar");

		string result = sut.ToString();

		await That(result).IsEqualTo("Default with 2 constructor parameter registrations");
	}

	[Test]
	public async Task WithMultipleFlags_ShouldKeepAllPartsInOutput()
	{
		MockBehavior behavior = MockBehavior.Default
			.ThrowingWhenNotSetup()
			.SkippingBaseClass()
			.SkippingInteractionRecording();

		string result = behavior.ToString();

		await That(result).Contains("ThrowingWhenNotSetup");
		await That(result).Contains("SkippingBaseClass");
		await That(result).Contains("SkippingInteractionRecording");
	}

	private sealed class MyDefaultValueGenerator : IDefaultValueGenerator
	{
		/// <inheritdoc cref="IDefaultValueGenerator.GenerateValue(Type, object?[])" />
		public object? GenerateValue(Type type, params object?[] parameters)
		{
			if (type == typeof(string))
			{
				return "foo";
			}

			return null;
		}
	}
}
