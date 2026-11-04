using Mockolate.Exceptions;
using Mockolate.Setup;

namespace Mockolate.Tests.MockProperties;

public sealed partial class SetupPropertyTests
{
	[Test]
	public async Task InvokeGetter_InvalidType_ShouldFallBackToDefaultValueGenerator()
	{
		MyPropertySetup<int> setup = new();

		string? result = setup.InvokeGetter<string?>(() => "fallback");

		await That(result).IsEqualTo("fallback");
	}

	[Test]
	public async Task InvokeSetter_InvalidType_ShouldThrowMockException()
	{
		MyPropertySetup<int> setup = new();

		void Act()
		{
			setup.InvokeSetter<string>("foo");
		}

		await That(Act).Throws<MockException>()
			.WithMessage("""
			             The property value only supports 'int', but is 'string'.
			             """);
	}

	[Test]
	public async Task Register_MultipleProperties_ShouldAllStoreValues()
	{
		IPropertyService sut = IPropertyService.CreateMock();

		sut.MyProperty = 1;
		sut.MyOtherProperty = 2;

		int myResult1 = sut.MyProperty;
		int myOtherResult1 = sut.MyOtherProperty;

		sut.MyProperty = 10;
		sut.MyOtherProperty = 20;

		int myResult2 = sut.MyProperty;
		int myOtherResult2 = sut.MyOtherProperty;

		await That(myResult1).IsEqualTo(1);
		await That(myOtherResult1).IsEqualTo(2);
		await That(myResult2).IsEqualTo(10);
		await That(myOtherResult2).IsEqualTo(20);
	}

	[Test]
	public async Task Register_SamePropertyTwice_ShouldOverwritePreviousSetup()
	{
		IPropertyService sut = IPropertyService.CreateMock();
		sut.Mock.Setup.MyProperty.InitializeWith(4);

		int result1 = sut.MyProperty;
		sut.MyProperty = 5;
		int result2 = sut.MyProperty;

		sut.Mock.Setup.MyProperty.Returns(6);

		int result3 = sut.MyProperty;
		sut.MyProperty = 7;
		int result4 = sut.MyProperty;

		await That(result1).IsEqualTo(4);
		await That(result2).IsEqualTo(5);
		await That(result3).IsEqualTo(6);
		await That(result4).IsEqualTo(6);
	}

	[Test]
	public async Task ToString_ShouldReturnType()
	{
		PropertySetup<int> setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo");

		string result = setup.ToString();

		await That(result).IsEqualTo("int Foo");
	}

	[Test]
	public async Task WhenMockInheritsPropertyMultipleTimes()
	{
		IMyPropertyService sut = IMyPropertyService.CreateMock().Implementing<IMyPropertyServiceBase1>();
		sut.Mock.Setup.Value.InitializeWith("Hello");

		string result = sut.Value;

		await That(sut.Mock.Verify.Value.Got()).Once();
		await That(result).IsEqualTo("Hello");
	}

	public interface IMyPropertyService : IMyPropertyServiceBase1
	{
		new string Value { get; set; }
	}

	public interface IMyPropertyServiceBase1
	{
		int Value { get; set; }
	}

	public interface IPropertyService
	{
		int MyProperty { get; set; }
		int MyOtherProperty { get; set; }
		string? MyStringProperty { get; set; }
	}

	private sealed class MyPropertySetup<T>()
		: PropertySetup<T>(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "My.Property")
	{
		public void InvokeSetter<TValue>(TValue value)
			=> InvokeSetter(value, MockBehavior.Default);

		public TResult InvokeGetter<TResult>()
			=> InvokeGetter<TResult>(MockBehavior.Default, () => default!);

		public TResult InvokeGetter<TResult>(Func<TResult> defaultValueGenerator)
			=> InvokeGetter(MockBehavior.Default, defaultValueGenerator);

		public void MyInitializeValue(object? value)
			=> InitializeValue(value);
	}
}
