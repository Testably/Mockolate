using Mockolate.Exceptions;
using Mockolate.Tests.Mocks;
using Mockolate.Verify;

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
	public async Task Set_WithMatcherOnObjectProperty_ShouldUseTheMatcher()
	{
		IServiceWithObjectProperties sut = IServiceWithObjectProperties.CreateMock();

		sut.Value = 5;
		sut.Value = "foo";

		await That(sut.Mock.Verify.Value.Set(It.IsAny<object>())).Twice()
			.Because("a matcher also converts to object, but must not be compared as a value");
		await That(sut.Mock.Verify.Value.Set(It.Is<object>(5))).Once();
		await That(sut.Mock.Verify.Value.Set(It.IsAny<string>())).Once();
		await That(sut.Mock.Verify.Value.Set(5)).Once();
		await That(sut.Mock.Verify.Value.Set("foo")).Once();
		await That(sut.Mock.Verify.Value.Set(6)).Never();
	}

	[Test]
	public async Task Set_WithMatcherOnNullableObjectProperty_ShouldUseTheMatcher()
	{
		IServiceWithObjectProperties sut = IServiceWithObjectProperties.CreateMock();

		sut.NullableValue = 5;
		sut.NullableValue = null;

		await That(sut.Mock.Verify.NullableValue.Set(It.IsAny<object?>())).Twice();
		await That(sut.Mock.Verify.NullableValue.Set(It.IsNull<object?>())).Once();
		await That(sut.Mock.Verify.NullableValue.Set(null)).Once();
		await That(sut.Mock.Verify.NullableValue.Set(5)).Once();
	}

	[Test]
	public async Task Set_WithMatcherOnWriteOnlyObjectProperty_ShouldUseTheMatcher()
	{
		IServiceWithObjectProperties sut = IServiceWithObjectProperties.CreateMock();

		sut.WriteOnly = 5;
		sut.WriteOnly = "foo";

		await That(sut.Mock.Verify.WriteOnly.Set(It.IsAny<object>())).Twice();
		await That(sut.Mock.Verify.WriteOnly.Set(It.Is<object>(5))).Once();
		await That(sut.Mock.Verify.WriteOnly.Set(5)).Once();
	}

	[Test]
	public async Task Set_WithWrappedMatcherOnObjectProperty_ShouldCompareTheMatcherAsValue()
	{
		IServiceWithObjectProperties sut = IServiceWithObjectProperties.CreateMock();
		object matcher = It.IsAny<object>();

		sut.Value = matcher;
		sut.Value = 5;

		await That(sut.Mock.Verify.Value.Set(It.Is(matcher))).Once();
	}

	[Test]
	public async Task Set_WithMatcherOnObjectProperty_WhenNotMatching_ShouldDescribeTheMatcher()
	{
		IServiceWithObjectProperties sut = IServiceWithObjectProperties.CreateMock();

		void Act()
		{
			sut.Mock.Verify.Value.Set(It.Is<object>(6)).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock set property Value to 6 exactly once, but it never did.")
			.Because("the matcher describes itself, while a value is described by its source expression");
	}

	[Test]
	public async Task ShouldIncreaseInvocationCountOfSetter()
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();

		sut.Counter = 42;

		await That(sut.Mock.Verify.Counter.Got()).Never();
		await That(sut.Mock.Verify.Counter.Set(It.IsAny<int>())).Once();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task WhenNotSetup_ShouldThrowMockNotSetupExceptionOnlyIfConfigured(bool throwWhenNotSetup)
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
			             The property 'global::Mockolate.Tests.Mocks.MockTests.IMyService.IsValid' was accessed without prior setup.
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

	public interface IServiceWithObjectProperties
	{
		object Value { get; set; }
		object? NullableValue { get; set; }
		object WriteOnly { set; }
	}
}
