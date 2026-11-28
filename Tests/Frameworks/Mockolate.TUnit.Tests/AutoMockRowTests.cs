using System.Collections.Generic;
using System.Reflection;
using Mockolate.Exceptions;
using Mockolate.Integrations;
using Mockolate.Integrations.TUnit;
using Mockolate.Frameworks.TestHelpers.Domain;

namespace Mockolate.TUnit.Tests;

public sealed class AutoMockRowTests
{
	[Test]
	public async Task Create_WithSeveralParametersOfTheTypeAndNoMatchingName_ShouldThrow()
	{
		AutoMockRow row = CreateRow(nameof(Ambiguous));

		void Act()
			=> row.Create();

		await That(Act).Throws<MockException>()
			.WithMessage("The constructor parameter 'source' of type 'Mockolate.Frameworks.TestHelpers.Domain.IAccount' matches the test parameters 'first', 'second'; name one of them 'source' or mark the others [NotShared].");
	}

	[Test]
	public async Task Create_WithNameDifferingInCase_ShouldMatchTheParameter()
	{
		AutoMockRow row = CreateRow(nameof(DifferentCase));

		object?[] values = row.Create();
		((TransferService)values[0]!).Transfer(10m);

		await That(((IAccount)values[1]!).Mock.Verify.Withdraw(10m)).Once();
		await That(((IAccount)values[2]!).Mock.Verify.Deposit(10m)).Once();
	}

	[Test]
	public async Task Create_WithMoreArgumentsThanParameters_ShouldThrow()
	{
		void Act()
			=> CreateRow(nameof(DifferentCase), 1, 2, 3, 4);

		await That(Act).Throws<MockException>()
			.WithMessage("AutoMockArguments has 4 arguments, but the test method has only 3 parameters.");
	}

	[Test]
	public async Task Create_WithParameterThatCannotBeCreated_ShouldThrow()
	{
		AutoMockRow row = CreateRow(nameof(Uncreatable));

		void Act()
			=> row.Create();

		await That(Act).Throws<MockException>()
			.WithMessage("Mockolate cannot create the parameter 'service' of type 'Mockolate.TUnit.Tests.AutoMockRowTests+IUncreatableService'. Pass it as an argument, or register a default value for its type through AutoMockArgumentsAttribute.DefaultBehavior.");
	}

	[Test]
	public async Task Create_WithDeferredParameterThatIsNoFunc_ShouldThrow()
	{
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetTypes(nameof(Uncreatable)),
			GetNames(nameof(Uncreatable)), _ => false, _ => true, _ => null, _ => null, [], null);

		void Act()
			=> row.Create();

		await That(Act).Throws<MockException>()
			.WithMessage("Mockolate cannot create the [Deferred] parameter 'service' of type 'Mockolate.TUnit.Tests.AutoMockRowTests+IUncreatableService'. Only a Func<T> of a type that Mockolate can create can be deferred.");
	}

	[Test]
	public async Task Create_ShouldOnlyAskForTheNotSharedMarkerOfParametersOfAMatchingType()
	{
		List<int> asked = [];
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetTypes(nameof(DifferentCase)),
			GetNames(nameof(DifferentCase)), index =>
			{
				asked.Add(index);
				return false;
			}, _ => false, _ => null, _ => null, [], null);

		row.Create();

		await That(asked).IsEqualTo([1, 2,]);
	}

	[Test]
	public async Task Create_WithAsDefaultParameter_ShouldTakePrecedenceOverTheBehavior()
	{
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetTypes(nameof(AsDefaultCount)),
			GetNames(nameof(AsDefaultCount)), _ => false, _ => false, index => index == 0 ? Type.EmptyTypes : null, _ => null, [42,],
			MockBehavior.Default.WithDefaultValueFor(() => 7));

		object?[] values = row.Create();

		await That(((IChocolateStock)values[1]!).Count).IsEqualTo(42);
	}

	[Test]
	public async Task Create_WithAsDefaultParameterFromTheDefaultValues_ShouldCreateItFromTheBehavior()
	{
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetTypes(nameof(AsDefaultCount)),
			GetNames(nameof(AsDefaultCount)), _ => false, _ => false, index => index == 0 ? Type.EmptyTypes : null, _ => null, [],
			MockBehavior.Default.WithDefaultValueFor(() => 7));

		object?[] values = row.Create();

		await That(values[0]).IsEqualTo(7);
		await That(((IChocolateStock)values[1]!).Count).IsEqualTo(7);
	}

	[Test]
	public async Task Create_WithAsDefaultTypeThatIsNotAssignable_ShouldThrow()
	{
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetTypes(nameof(AsDefaultCount)),
			GetNames(nameof(AsDefaultCount)), _ => false, _ => false, index => index == 0 ? [typeof(string),] : null, _ => null, [], null);

		void Act()
			=> row.Create();

		await That(Act).Throws<MockException>()
			.WithMessage("The [AsDefault] parameter 'count' of type 'System.Int32' is not assignable to 'System.String'.");
	}

	[Test]
	public async Task Create_WithNullAsDefaultType_ShouldThrow()
	{
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetTypes(nameof(AsDefaultCount)),
			GetNames(nameof(AsDefaultCount)), _ => false, _ => false, index => index == 0 ? [null!,] : null, _ => null, [], null);

		void Act()
			=> row.Create();

		await That(Act).Throws<MockException>()
			.WithMessage("The [AsDefault] parameter 'count' of type 'System.Int32' is not assignable to 'null'.");
	}

	[Test]
	public async Task Create_WithValueFrom_ShouldUseTheValueInsteadOfCreatingIt()
	{
		IUncreatableService service = new UncreatableService();
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetTypes(nameof(Uncreatable)),
			GetNames(nameof(Uncreatable)), _ => false, _ => false, _ => null, _ => () => service, [], null);

		object?[] values = row.Create();

		await That(values[0]).IsSameAs(service);
	}

	[Test]
	public async Task Create_WithValueFrom_ShouldReadItOnce()
	{
		int calls = 0;
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetTypes(nameof(StockIndicatorAndStock)),
			GetNames(nameof(StockIndicatorAndStock)), _ => false, _ => false, _ => null,
			index => index == 1
				? () =>
				{
					calls++;
					return IChocolateStock.CreateMock();
				}
				: null, [], null);

		object?[] values = row.Create();

		await That(((StockIndicator)values[0]!).Stock).IsSameAs(values[1]);
		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task Create_WithValueFromAndGivenArgument_ShouldUseTheArgumentWithoutReadingTheMember()
	{
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetTypes(nameof(FromMethod)),
			GetNames(nameof(FromMethod)), _ => false, _ => false, _ => null,
			_ => () => throw new InvalidOperationException("The member must not be read."), [5,], null);

		object?[] values = row.Create();

		await That(values[0]).IsEqualTo(5);
	}

	[Test]
	public async Task GetValueFrom_WithoutAttribute_ShouldReturnNull()
	{
		Func<object?>? valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(Uncreatable))[0]);

		await That(valueFrom is null).IsTrue();
	}

	[Test]
	public async Task GetValueFrom_WithProperty_ShouldReadItOnEachCall()
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(FromProperty))[0])!;

		object? first = valueFrom();
		object? second = valueFrom();

		await That(first).IsExactly<UncreatableService>();
		await That(second).IsNotSameAs(first);
	}

	[Test]
	public async Task GetValueFrom_WithMethod_ShouldCallIt()
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(FromMethod))[0])!;

		await That(valueFrom()).IsEqualTo(42);
	}

	[Test]
	public async Task GetValueFrom_WithMemberOfBaseClass_ShouldReadIt()
	{
		ParameterInfo parameter = typeof(DerivedTestClass).GetMethod(nameof(DerivedTestClass.FromBase))!.GetParameters()[0];

		Func<object?> valueFrom = AutoMockRow.GetValueFrom(parameter)!;

		await That(valueFrom()).IsEqualTo("base");
	}

	[Test]
	public async Task GetValueFrom_WithPropertyThatHidesOneOfTheBaseClass_ShouldReadTheHidingProperty()
	{
		ParameterInfo parameter = typeof(DerivedTestClass).GetMethod(nameof(DerivedTestClass.FromHiding))!.GetParameters()[0];

		Func<object?> valueFrom = AutoMockRow.GetValueFrom(parameter)!;

		await That(valueFrom()).IsEqualTo("derived");
	}

	[Test]
	public async Task GetValueFrom_WithGenericMethodOfTheTestClass_ShouldCallTheMethodOfTheBaseClass()
	{
		ParameterInfo parameter = typeof(DerivedTestClass).GetMethod(nameof(DerivedTestClass.FromGeneric))!.GetParameters()[0];

		Func<object?> valueFrom = AutoMockRow.GetValueFrom(parameter)!;

		await That(valueFrom()).IsEqualTo("base");
	}

	[Test]
	public async Task GetValueFrom_WithInstanceMemberThatHidesAStaticOneOfTheBaseClass_ShouldThrow()
	{
		ParameterInfo parameter = typeof(DerivedTestClass).GetMethod(nameof(DerivedTestClass.FromShadowed))!.GetParameters()[0];

		void Act()
			=> AutoMockRow.GetValueFrom(parameter);

		await That(Act).Throws<MockException>()
			.WithMessage("The [ValueFrom] parameter 'value' needs a constant, static property or parameterless static method 'Shadowed' in 'Mockolate.TUnit.Tests.AutoMockRowTests+DerivedTestClass'.")
			.Because("like C# name lookup, the instance member hides the static one of the base class");
	}

	[Test]
	public async Task GetValueFrom_WithClosedGenericTestClass_ShouldReadTheMember()
	{
		ParameterInfo parameter = typeof(GenericTestClass<int>).GetMethod(nameof(GenericTestClass<int>.FromMember))!.GetParameters()[0];

		Func<object?> valueFrom = AutoMockRow.GetValueFrom(parameter)!;

		await That(valueFrom()).IsEqualTo("Int32");
	}

	[Test]
	public async Task GetValueFrom_WithOpenGenericTestClass_ShouldThrow()
	{
		ParameterInfo parameter = typeof(GenericTestClass<>).GetMethod(nameof(GenericTestClass<int>.FromMember))!.GetParameters()[0];

		void Act()
			=> AutoMockRow.GetValueFrom(parameter);

		await That(Act).Throws<MockException>()
			.WithMessage("The [ValueFrom] parameter 'value' cannot read 'Value', because the test framework does not provide the type arguments of the generic test class 'Mockolate.TUnit.Tests.AutoMockRowTests+GenericTestClass`1[T]'.");
	}

	[Test]
	public async Task GetValueFrom_WithPrivateMemberOfBaseClass_ShouldThrow()
	{
		ParameterInfo parameter = typeof(DerivedTestClass).GetMethod(nameof(DerivedTestClass.FromPrivateBase))!.GetParameters()[0];

		void Act()
			=> AutoMockRow.GetValueFrom(parameter);

		await That(Act).Throws<MockException>()
			.WithMessage("The [ValueFrom] parameter 'value' needs a constant, static property or parameterless static method 'PrivateBaseValue' in 'Mockolate.TUnit.Tests.AutoMockRowTests+DerivedTestClass'.");
	}

	[Test]
	public async Task GetValueFrom_WithMissingMember_ShouldThrow()
	{
		void Act()
			=> AutoMockRow.GetValueFrom(GetParameters(nameof(FromMissingMember))[0]);

		await That(Act).Throws<MockException>()
			.WithMessage("The [ValueFrom] parameter 'value' needs a constant, static property or parameterless static method 'Missing' in 'Mockolate.TUnit.Tests.AutoMockRowTests'.");
	}

	[Test]
	public async Task GetValueFrom_WithInstanceProperty_ShouldThrow()
	{
		void Act()
			=> AutoMockRow.GetValueFrom(GetParameters(nameof(FromInstanceProperty))[0]);

		await That(Act).Throws<MockException>()
			.WithMessage("The [ValueFrom] parameter 'value' needs a constant, static property or parameterless static method 'InstanceValue' in 'Mockolate.TUnit.Tests.AutoMockRowTests'.");
	}

	[Test]
	[Arguments(nameof(FromStaticReadonlyField), "ReadonlyNumber")]
	[Arguments(nameof(FromStaticReadonlyDecimalField), "ReadonlyPrice")]
	[Arguments(nameof(FromInstanceField), "_instanceNumber")]
	public async Task GetValueFrom_WithFieldThatIsNotConstant_ShouldThrow(string methodName, string memberName)
	{
		void Act()
			=> AutoMockRow.GetValueFrom(GetParameters(methodName)[0]);

		await That(Act).Throws<MockException>()
			.WithMessage($"The [ValueFrom] parameter 'value' needs a constant, static property or parameterless static method '{memberName}' in 'Mockolate.TUnit.Tests.AutoMockRowTests'.")
			.Because("a field that is not constant would share one instance with all test cases");
	}

	[Test]
	[Arguments(nameof(DerivedTestClass.FromHiddenConstant), "HiddenConstant")]
	[Arguments(nameof(DerivedTestClass.FromFieldHiddenValue), "FieldHiddenValue")]
	public async Task GetValueFrom_WithFieldThatHidesAMemberOfTheBaseClass_ShouldThrow(string methodName,
		string memberName)
	{
		ParameterInfo parameter = typeof(DerivedTestClass).GetMethod(methodName)!.GetParameters()[0];

		void Act()
			=> AutoMockRow.GetValueFrom(parameter);

		await That(Act).Throws<MockException>()
			.WithMessage($"The [ValueFrom] parameter 'value' needs a constant, static property or parameterless static method '{memberName}' in 'Mockolate.TUnit.Tests.AutoMockRowTests+DerivedTestClass'.")
			.Because("like C# name lookup, the field hides the member of the base class");
	}

	[Test]
	public async Task GetValueFrom_WithEventThatHasTheNameOfAPropertyOfTheBaseClass_ShouldReadTheProperty()
	{
		ParameterInfo parameter = typeof(DerivedTestClass).GetMethod(nameof(DerivedTestClass.FromEventHiddenValue))!.GetParameters()[0];

		Func<object?> valueFrom = AutoMockRow.GetValueFrom(parameter)!;

		await That(valueFrom()).IsEqualTo("base")
			.Because("the compiler-generated backing field of the event is no member that C# name lookup finds, like the analyzer");
	}

	[Test]
	public async Task GetValueFrom_WithConstant_ShouldReadIt()
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(FromConstant))[0])!;

		await That(valueFrom()).IsEqualTo(7);
	}

	[Test]
	public async Task GetValueFrom_WithDecimalConstant_ShouldReadIt()
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(FromDecimalConstant))[0])!;

		await That(valueFrom()).IsEqualTo(1.5m)
			.Because("the compiler emits a decimal constant as a static readonly field with a DecimalConstantAttribute");
	}

	[Test]
	public async Task GetValueFrom_WithEnumConstant_ShouldReadTheEnumValue()
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(FromEnumConstant))[0])!;

		await That(valueFrom()).IsEqualTo(StringComparison.Ordinal)
			.Because("the parameter only accepts the enum type, not its underlying value");
	}

	[Test]
	public async Task GetValueFrom_WithNullConstant_ShouldReturnNull()
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(FromNullConstant))[0])!;

		await That(valueFrom()).IsNull();
	}

	[Test]
	public async Task GetValueFrom_WithConstantOfBaseClass_ShouldReadIt()
	{
		ParameterInfo parameter = typeof(DerivedTestClass).GetMethod(nameof(DerivedTestClass.FromBaseConstant))!.GetParameters()[0];

		Func<object?> valueFrom = AutoMockRow.GetValueFrom(parameter)!;

		await That(valueFrom()).IsEqualTo("base constant");
	}

	[Test]
	public async Task GetValueFrom_WithPrivateConstantOfBaseClass_ShouldThrow()
	{
		ParameterInfo parameter = typeof(DerivedTestClass).GetMethod(nameof(DerivedTestClass.FromPrivateBaseConstant))!.GetParameters()[0];

		void Act()
			=> AutoMockRow.GetValueFrom(parameter);

		await That(Act).Throws<MockException>()
			.WithMessage("The [ValueFrom] parameter 'value' needs a constant, static property or parameterless static method 'PrivateBaseConstant' in 'Mockolate.TUnit.Tests.AutoMockRowTests+DerivedTestClass'.");
	}

	[Test]
	public async Task GetValueFrom_WhenTheMemberThrows_ShouldThrowItsException()
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(FromThrowingMember))[0])!;

		void Act()
			=> valueFrom();

		await That(Act).Throws<InvalidOperationException>().WithMessage("member failed")
			.Because("the exception of the member is not wrapped in a TargetInvocationException");
	}

	[Test]
	public async Task GetValueFrom_WithValueOfAnotherType_ShouldThrow()
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(FromOtherType))[0])!;

		void Act()
			=> valueFrom();

		await That(Act).Throws<MockException>()
			.WithMessage("The member 'Text' of 'Mockolate.TUnit.Tests.AutoMockRowTests' returned a 'System.String', which is not assignable to the [ValueFrom] parameter 'value' of type 'System.Int32'.");
	}

	[Test]
	public async Task GetValueFrom_WithNull_ShouldReturnNull()
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(FromNull))[0])!;

		await That(valueFrom()).IsNull();
	}

	[Test]
	public async Task GetValueFrom_WithValueProvider_ShouldCallItWithTheParameter()
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(FromProvider))[0])!;

		await That(valueFrom()).IsEqualTo("fileName");
	}

	[Test]
	public async Task GetValueFrom_WithValueProviderOfAnotherType_ShouldThrow()
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(FromProviderOfOtherType))[0])!;

		void Act()
			=> valueFrom();

		await That(Act).Throws<MockException>()
			.WithMessage("The value provider 'Mockolate.TUnit.Tests.AutoMockRowTests+NumberAttribute' returned a 'System.Int32', which is not assignable to the parameter 'text' of type 'System.String'.");
	}

	[Test]
	[Arguments(nameof(FromTwoProviders))]
	[Arguments(nameof(FromProviderAndValueFrom))]
	public async Task GetValueFrom_WithMoreThanOneValueProvider_ShouldThrow(string methodName)
	{
		void Act()
			=> AutoMockRow.GetValueFrom(GetParameters(methodName)[0]);

		await That(Act).Throws<MockException>()
			.WithMessage("The parameter 'value' has more than one value provider or [ValueFrom].");
	}

	[Test]
	public async Task GetValueFrom_WithValueProviderReturningNullForAValueType_ShouldThrow()
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(nameof(FromNullProvider))[0])!;

		void Act()
			=> valueFrom();

		await That(Act).Throws<MockException>()
			.WithMessage("The value provider 'Mockolate.TUnit.Tests.AutoMockRowTests+NullAttribute' returned null for the parameter 'count' of the value type 'System.Int32'.");
	}

	[Test]
	[Arguments(nameof(FromNullProviderForNullable))]
	[Arguments(nameof(FromNullProviderForReference))]
	public async Task GetValueFrom_WithValueProviderReturningNullForANullableParameter_ShouldReturnNull(
		string methodName)
	{
		Func<object?> valueFrom = AutoMockRow.GetValueFrom(GetParameters(methodName)[0])!;

		await That(valueFrom()).IsNull();
	}

	private static AutoMockRow CreateRow(string methodName, params object?[] arguments)
		=> new(typeof(AutoMockRowTests).Assembly, GetTypes(methodName), GetNames(methodName), _ => false, _ => false, _ => null,
			_ => null, arguments, null);

	private static ParameterInfo[] GetParameters(string methodName)
		=> typeof(AutoMockRowTests).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!.GetParameters();

	private static Type[] GetTypes(string methodName)
		=> Array.ConvertAll(GetParameters(methodName), parameter => parameter.ParameterType);

	private static string[] GetNames(string methodName)
		=> Array.ConvertAll(GetParameters(methodName), parameter => parameter.Name!);

	private static void Ambiguous(TransferService sut, IAccount first, IAccount second)
	{
	}

	private static void DifferentCase(TransferService sut, IAccount Source, IAccount TARGET)
	{
	}

	private static void Uncreatable(IUncreatableService service)
	{
	}

	private static void AsDefaultCount(int count, IChocolateStock stock)
	{
	}

	private static void StockIndicatorAndStock(StockIndicator sut, IChocolateStock stock)
	{
	}

	private static void FromProperty([ValueFrom(nameof(NewService))] IUncreatableService service)
	{
	}

	private static void FromProvider([ParameterName] string fileName)
	{
	}

	private static void FromProviderOfOtherType([Number] string text)
	{
	}

	private static void FromNullProvider([Null] int count)
	{
	}

	private static void FromNullProviderForNullable([Null] int? count)
	{
	}

	private static void FromNullProviderForReference([Null] string? text)
	{
	}

	private static void FromTwoProviders([ParameterName] [Number] object value)
	{
	}

	private static void FromProviderAndValueFrom([ParameterName] [ValueFrom(nameof(Text))] object value)
	{
	}

	private static void FromMethod([ValueFrom(nameof(GetNumber))] int value)
	{
	}

	private static void FromMissingMember([ValueFrom("Missing")] int value)
	{
	}

	private static void FromInstanceProperty([ValueFrom(nameof(InstanceValue))] int value)
	{
	}

	private static void FromThrowingMember([ValueFrom(nameof(Throwing))] int value)
	{
	}

	private static void FromOtherType([ValueFrom(nameof(Text))] int value)
	{
	}

	private static void FromNull([ValueFrom(nameof(NullService))] IUncreatableService? service)
	{
	}

	private static void FromStaticReadonlyField([ValueFrom(nameof(ReadonlyNumber))] int value)
	{
	}

	private static void FromStaticReadonlyDecimalField([ValueFrom(nameof(ReadonlyPrice))] decimal value)
	{
	}

	private static void FromInstanceField([ValueFrom(nameof(_instanceNumber))] int value)
	{
	}

	private static void FromConstant([ValueFrom(nameof(Seven))] int value)
	{
	}

	private static void FromDecimalConstant([ValueFrom(nameof(Price))] decimal value)
	{
	}

	private static void FromEnumConstant([ValueFrom(nameof(Comparison))] StringComparison value)
	{
	}

	private static void FromNullConstant([ValueFrom(nameof(NoText))] string? value)
	{
	}

#pragma warning disable CS0414 // The field is assigned but its value is never used
	private static readonly int ReadonlyNumber = 1;

	private static readonly decimal ReadonlyPrice = 1.5m;

	private readonly int _instanceNumber = 1;
#pragma warning restore CS0414

	private const int Seven = 7;

	private const decimal Price = 1.5m;

	private const StringComparison Comparison = StringComparison.Ordinal;

	private const string? NoText = null;

	private static IUncreatableService NewService => new UncreatableService();

	private static IUncreatableService? NullService => null;

	private static int GetNumber() => 42;

	private static object Text => "text";

	private static int Throwing => throw new InvalidOperationException("member failed");

#pragma warning disable CA1822 // Mark members as static
	private int InstanceValue => 1;
#pragma warning restore CA1822

	public interface IUncreatableService;

	[AttributeUsage(AttributeTargets.Parameter)]
	internal sealed class ParameterNameAttribute : Attribute, IParameterValueProvider
	{
		public object? ProvideValue(ParameterInfo parameter) => parameter.Name;
	}

	[AttributeUsage(AttributeTargets.Parameter)]
	internal sealed class NumberAttribute : Attribute, IParameterValueProvider
	{
		public object? ProvideValue(ParameterInfo parameter) => 42;
	}

	[AttributeUsage(AttributeTargets.Parameter)]
	internal sealed class NullAttribute : Attribute, IParameterValueProvider
	{
		public object? ProvideValue(ParameterInfo parameter) => null;
	}

	private sealed class UncreatableService : IUncreatableService;

	public class BaseTestClass
	{
		protected static string BaseValue => "base";

		protected static object Hidden => "base";

		protected static string Generic() => "base";

		private static string PrivateBaseValue => "base";

		protected static string Shadowed => "base";

		protected const string BaseConstant = "base constant";

		private const string PrivateBaseConstant = "base";

		protected const string HiddenConstant = "base";

		protected static string FieldHiddenValue => "base";

		protected static string EventHiddenValue => "base";
	}

	public sealed class DerivedTestClass : BaseTestClass
	{
		private static new string Hidden => "derived";

#pragma warning disable CA1822 // Mark members as static
		private new string Shadowed => "derived";
#pragma warning restore CA1822

		public static void FromShadowed([ValueFrom(nameof(Shadowed))] string value)
		{
		}

		public static void FromBase([ValueFrom(nameof(BaseValue))] string value)
		{
		}

		public static void FromHiding([ValueFrom(nameof(Hidden))] string value)
		{
		}

		public static void FromGeneric([ValueFrom(nameof(Generic))] string value)
		{
		}

		public static void FromPrivateBase([ValueFrom("PrivateBaseValue")] string value)
		{
		}

		public static void FromBaseConstant([ValueFrom(nameof(BaseConstant))] string value)
		{
		}

		public static void FromPrivateBaseConstant([ValueFrom("PrivateBaseConstant")] string value)
		{
		}

		public static void FromHiddenConstant([ValueFrom(nameof(HiddenConstant))] string value)
		{
		}

		public static void FromFieldHiddenValue([ValueFrom(nameof(FieldHiddenValue))] string value)
		{
		}

		public static void FromEventHiddenValue([ValueFrom(nameof(EventHiddenValue))] string value)
		{
		}

#pragma warning disable CS0414 // The field is assigned but its value is never used
		private static new readonly string HiddenConstant = "derived";

		private static new readonly string FieldHiddenValue = "derived";
#pragma warning restore CS0414

#pragma warning disable CS0067 // The event is never used
		private static new event Action? EventHiddenValue;
#pragma warning restore CS0067

		private static string Generic<T>() => typeof(T).Name;
	}

	public sealed class GenericTestClass<T>
	{
		public static void FromMember([ValueFrom(nameof(Value))] string value)
		{
		}

		private static string Value => typeof(T).Name;
	}
}
