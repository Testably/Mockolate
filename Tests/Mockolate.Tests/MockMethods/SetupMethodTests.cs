using System.Collections.Generic;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Parameters;
using Mockolate.Setup;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests.MockMethods;

public sealed partial class SetupMethodTests
{
	[Test]
	public async Task Equals_ShouldWork()
	{
		object obj = new();
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.Equals(It.IsAny<object?>()).Returns(true);

		bool result = sut.Equals(obj);

		await That(result).IsEqualTo(true);
	}

	[Test]
	public async Task GenericMethod_SetupShouldWork()
	{
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyGenericMethod(It.Is(0), "foo").Returns(42);

		int result1 = sut.MyGenericMethod(0, "foo");
		int result2 = sut.MyGenericMethod(0L, "foo");

		await That(sut.Mock.Verify.MyGenericMethod(It.IsAny<long>(), It.IsAny<string>())).Once();
		await That(result1).IsEqualTo(42);
		await That(result2).IsEqualTo(0);
	}

	[Test]
	public async Task GenericMethods_ShouldConsiderGenericParameter()
	{
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyGenericMethod<int>().Returns(42);

		int matchingResult = sut.MyGenericMethod<int>();
		int notMatchingResult = sut.MyGenericMethod<long>();

		await That(matchingResult).IsEqualTo(42);
		await That(notMatchingResult).IsEqualTo(0);
	}

	[Test]
	public async Task GetHashCode_ShouldWork()
	{
		int expectedResult = Guid.NewGuid().GetHashCode();
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.GetHashCode().Returns(expectedResult);

		int result = sut.GetHashCode();

		await That(result).IsEqualTo(expectedResult);
	}

	[Test]
	public async Task MultipleOnlyOnceCallbacks_ShouldExecuteInOrder()
	{
		List<int> receivedCalls = [];
		IMethodService sut = IMethodService.CreateMock();

		sut.Mock.Setup.MyVoidMethodWithoutParameters()
			.Do(() => receivedCalls.Add(1)).OnlyOnce()
			.Do(() => receivedCalls.Add(2)).OnlyOnce()
			.Do(() => receivedCalls.Add(3)).OnlyOnce()
			.Do(() => receivedCalls.Add(4)).OnlyOnce()
			.Do(() => receivedCalls.Add(5)).OnlyOnce();

		sut.MyVoidMethodWithoutParameters();
		sut.MyVoidMethodWithoutParameters();
		sut.MyVoidMethodWithoutParameters();
		sut.MyVoidMethodWithoutParameters();
		sut.MyVoidMethodWithoutParameters();

		await That(receivedCalls).IsEqualTo([1, 2, 3, 4, 5,]);
	}

	[Test]
	public async Task OverlappingSetups_ShouldUseLatestMatchingSetup()
	{
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyIntMethodWithParameters(It.IsAny<int>(), It.IsAny<string>()).Returns(1);
		sut.Mock.Setup.MyIntMethodWithParameters(It.Is(0), It.Is("foo")).Returns(2);

		int result1 = sut.MyIntMethodWithParameters(1, "foo");
		int result2 = sut.MyIntMethodWithParameters(0, "foo");
		int result3 = sut.MyIntMethodWithParameters(0, "bar");

		await That(result1).IsEqualTo(1);
		await That(result2).IsEqualTo(2);
		await That(result3).IsEqualTo(1);
	}

	[Test]
	public async Task OverlappingSetups_WhenGeneralSetupIsLater_ShouldOnlyUseGeneralSetup()
	{
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyIntMethodWithParameters(It.Is(0), It.Is("foo")).Returns(2);
		sut.Mock.Setup.MyIntMethodWithParameters(It.IsAny<int>(), It.IsAny<string>()).Returns(1);

		int result1 = sut.MyIntMethodWithParameters(1, "foo");
		int result2 = sut.MyIntMethodWithParameters(0, "foo");
		int result3 = sut.MyIntMethodWithParameters(0, "bar");

		await That(result1).IsEqualTo(1);
		await That(result2).IsEqualTo(1);
		await That(result3).IsEqualTo(1);
	}

	[Test]
	public async Task Parameter_Do_ShouldExecuteCallback()
	{
		List<int> capturedValues = [];
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyIntMethodWithParameters(It.IsAny<int>().Do(v => capturedValues.Add(v)),
			It.IsAny<string>());

		sut.MyIntMethodWithParameters(1, "foo");
		sut.MyIntMethodWithParameters(2, "foobar");
		sut.MyIntMethodWithParameters(3, "bar");

		await That(capturedValues).IsEqualTo([1, 2, 3,]);
	}

	[Test]
	public async Task Parameter_Do_ShouldOnlyExecuteCallbackWhenAllParametersMatch()
	{
		List<int> capturedValues = [];
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyIntMethodWithParameters(It.IsAny<int>().Do(v => capturedValues.Add(v)),
			It.Satisfies<string>(s => s.Length == 3));

		sut.MyIntMethodWithParameters(1, "foo");
		sut.MyIntMethodWithParameters(2, "foobar");
		sut.MyIntMethodWithParameters(3, "bar");

		await That(capturedValues).IsEqualTo([1, 3,]);
	}

	[Test]
	public async Task ParameterExplicitMixWithNull_ShouldWork()
	{
		IMethodService sut = IMethodService.CreateMock();
		MyMethodServiceType value = new(5);
		sut.Mock.Setup.Combine(value, null!).Returns(4);

		int result = sut.Combine(value, null!);

		await That(result).IsEqualTo(4);
		await That(sut.Mock.Verify.Combine(value, null!)).Once();
	}

	[Test]
	public async Task ParameterMixWithNull_ShouldWork()
	{
		IMethodService sut = IMethodService.CreateMock();
		MyMethodServiceType value = new(5);
		sut.Mock.Setup.Combine(It.IsAny<MyMethodServiceType>(), null).Returns(4);

		int result = sut.Combine(value, null!);

		await That(result).IsEqualTo(4);
		await That(sut.Mock.Verify.Combine(It.IsAny<MyMethodServiceType>(), null)).Once();
	}

	[Test]
	public async Task Register_AfterInvocation_ShouldBeAppliedForFutureUse()
	{
		IMethodService sut = IMethodService.CreateMock();

		int result0 = sut.MyIntMethodWithoutParameters();
		sut.Mock.Setup.MyIntMethodWithoutParameters().Returns(42);
		int result1 = sut.MyIntMethodWithoutParameters();

		await That(result0).IsEqualTo(0);
		await That(result1).IsEqualTo(42);
	}

	[Test]
	public async Task ReturnMethod_Callback_ShouldExecuteWhenInvoked()
	{
		int callCount = 0;
		IReturnMethodSetupWithParametersTest sut = IReturnMethodSetupWithParametersTest.CreateMock();

		sut.Mock.Setup.MethodWithoutOtherOverloads(Match.AnyParameters())
			.Do(() => { callCount++; })
			.Returns("foo");

		string result = sut.MethodWithoutOtherOverloads(1, 2, 3);

		await That(callCount).IsEqualTo(1);
		await That(result).IsEqualTo("foo");
	}

	[Test]
	public async Task ReturnMethod_Verify_ShouldMatchAnyParameters()
	{
		int callCount = 0;
		IReturnMethodSetupWithParametersTest sut = IReturnMethodSetupWithParametersTest.CreateMock();

		sut.Mock.Setup.MethodWithoutOtherOverloads(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
			.Do(() => { callCount++; })
			.Returns("foo");

		string result = sut.MethodWithoutOtherOverloads(1, 2, 3);

		await That(callCount).IsEqualTo(1);
		await That(result).IsEqualTo("foo");
		await That(sut.Mock.Verify.MethodWithoutOtherOverloads(Match.AnyParameters())).Once();
	}

	[Test]
	public async Task ReturnMethod_WhenSetupWithNull_ShouldReturnDefaultValue()
	{
		int callCount = 0;
		IReturnMethodSetupWithParametersTest sut = IReturnMethodSetupWithParametersTest.CreateMock();

		sut.Mock.Setup.MethodWithoutOtherOverloads(Match.AnyParameters())
			.Do(() => { callCount++; })
			.Returns((string?)null!);

		string result = sut.MethodWithoutOtherOverloads(1, 2, 3);

		await That(callCount).IsEqualTo(1);
		await That(result).IsNull();
	}

	[Test]
	public async Task Setup_ShouldUseNewestMatchingSetup()
	{
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyIntMethodWithParameters(It.IsAny<int>(), It.IsAny<string>()).Returns(10);

		await That(sut.MyIntMethodWithParameters(1, "")).IsEqualTo(10);

		sut.Mock.Setup.MyIntMethodWithParameters(It.IsAny<int>(), It.IsAny<string>()).Returns(20);

		await That(sut.MyIntMethodWithParameters(1, "")).IsEqualTo(20);
	}

	[Test]
	public async Task Setup_WithOutParameter_ShouldUseCallbackToSetValue()
	{
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyMethodWithOutParameter(It.IsOut(() => 4));

		sut.MyMethodWithOutParameter(out int value);

		await That(value).IsEqualTo(4);
	}

	[Test]
	public async Task Setup_WithOutParameterWithoutCallback_ShouldUseDefaultValueSetValue()
	{
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyMethodWithOutParameter(It.IsAnyOut<int>());

		sut.MyMethodWithOutParameter(out int value);

		await That(value).IsEqualTo(0);
	}

	[Test]
	public async Task Setup_WithRefParameter_WithCallback_ShouldUseCallbackToSetValue()
	{
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyMethodWithRefParameter(It.IsRef<int>(_ => 4));
		int value = 2;

		sut.MyMethodWithRefParameter(ref value);

		await That(value).IsEqualTo(4);
	}

	[Test]
	public async Task Setup_WithRefParameter_WithoutPredicateOrCallback_ShouldNotChangeValue()
	{
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyMethodWithRefParameter(It.IsAnyRef<int>());
		int value = 2;

		sut.MyMethodWithRefParameter(ref value);

		await That(value).IsEqualTo(2);
	}

	[Test]
	public async Task Setup_WithRefParameter_WithPredicate_ShouldUseCallbackToSetValue()
	{
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyMethodWithRefParameter(It.IsRef<int>(v => v > 2));
		int value = 2;

		sut.MyMethodWithRefParameter(ref value);

		await That(value).IsEqualTo(2);
	}

	[Test]
	public async Task Setup_WithRefParameter_WithPredicateAndCallback_ShouldUseCallbackToSetValueWhenPredicateMatches()
	{
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.MyMethodWithRefParameter(It.IsRef<int>(v => v > 2, _ => 4));
		int value1 = 2;
		int value2 = 3;

		sut.MyMethodWithRefParameter(ref value1);
		sut.MyMethodWithRefParameter(ref value2);

		await That(value1).IsEqualTo(2);
		await That(value2).IsEqualTo(4);
	}

	[Test]
	public async Task ToString_OutParameter_ShouldReturnExpectedValue()
	{
		IVerifyOutParameter<int> sut = It.IsOut<int>();
		string expectedResult = "It.IsOut<int>()";

		string? result = sut.ToString();

		await That(result).IsEqualTo(expectedResult);
	}

	[Test]
	public async Task ToString_OutParameter_WithCallback_ShouldReturnExpectedValue()
	{
		IOutParameter<int> sut = It.IsOut(() => 4);
		string expectedResult = "It.IsOut<int>(() => 4)";

		string? result = sut.ToString();

		await That(result).IsEqualTo(expectedResult);
	}

	[Test]
	public async Task ToString_RefParameter_ShouldReturnExpectedValue()
	{
		IVerifyRefParameter<int> sut = It.IsRef<int>();
		string expectedResult = "It.IsRef<int>()";

		string? result = sut.ToString();

		await That(result).IsEqualTo(expectedResult);
	}

	[Test]
	public async Task ToString_RefParameter_WithCallback_ShouldReturnExpectedValue()
	{
		IRefParameter<int> sut = It.IsRef<int>(_ => 4);
		string expectedResult = "It.IsRef<int>(_ => 4)";

		string? result = sut.ToString();

		await That(result).IsEqualTo(expectedResult);
	}

	[Test]
	public async Task ToString_RefParameter_WithPredicate_ShouldReturnExpectedValue()
	{
		IRefParameter<int> sut = It.IsRef<int>(v => v > 4);
		string expectedResult = "It.IsRef<int>(v => v > 4)";

		string? result = sut.ToString();

		await That(result).IsEqualTo(expectedResult);
	}

	[Test]
	public async Task ToString_RefParameter_WithPredicateAndCallback_ShouldReturnExpectedValue()
	{
		IRefParameter<int> sut = It.IsRef<int>(v => v > 4, v => v * 5);
		string expectedResult = "It.IsRef<int>(v => v > 4, v => v * 5)";

		string? result = sut.ToString();

		await That(result).IsEqualTo(expectedResult);
	}

	[Test]
	public async Task ToString_ShouldWork()
	{
		string expectedResult = Guid.NewGuid().ToString();
		IMethodService sut = IMethodService.CreateMock();
		sut.Mock.Setup.ToString().Returns(expectedResult);

		string result = sut.ToString();

		await That(result).IsEqualTo(expectedResult);
	}

	[Test]
	public async Task VoidMethod_Callback_ShouldExecuteWhenInvoked()
	{
		int callCount = 0;
		IVoidMethodSetupWithParametersTest sut = IVoidMethodSetupWithParametersTest.CreateMock();

		sut.Mock.Setup.MethodWithoutOtherOverloads(Match.AnyParameters())
			.Do(() => { callCount++; });

		sut.MethodWithoutOtherOverloads(1, 2, 3);

		await That(callCount).IsEqualTo(1);
	}

	[Test]
	public async Task VoidMethod_Verify_ShouldMatchAnyParameters()
	{
		int callCount = 0;
		IVoidMethodSetupWithParametersTest sut = IVoidMethodSetupWithParametersTest.CreateMock();

		sut.Mock.Setup.MethodWithoutOtherOverloads(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
			.Do(() => { callCount++; });

		sut.MethodWithoutOtherOverloads(1, 2, 3);

		await That(callCount).IsEqualTo(1);
		await That(sut.Mock.Verify.MethodWithoutOtherOverloads(Match.AnyParameters())).Once();
	}

	[Test]
	public async Task WhenNotSetup_ShouldReturnDefaultValue()
	{
		IMethodService sut = IMethodService.CreateMock();

		int result1 = sut.MyIntMethodWithoutParameters();
		int result2 = sut.MyIntMethodWithParameters(0, "foo");

		await That(result1).IsEqualTo(0);
		await That(result2).IsEqualTo(0);
	}

	[Test]
	public async Task WhenNotSetup_ThrowWhenNotSetup_ShouldThrowMockNotSetupException()
	{
		IMethodService mock = IMethodService.CreateMock(MockBehavior.Default with
		{
			ThrowWhenNotSetup = true,
		});

		void Act()
		{
			mock.MyIntMethodWithoutParameters();
		}

		await That(Act).Throws<MockNotSetupException>()
			.WithMessage(
				"The method 'global::Mockolate.Tests.MockMethods.SetupMethodTests.IMethodService.MyIntMethodWithoutParameters()' was invoked without prior setup.");
	}

	[Test]
	public async Task WithInParameter_ShouldUseSetup()
	{
		IMethodSetupWithInAndRefReadonlyParameter sut = IMethodSetupWithInAndRefReadonlyParameter.CreateMock();
		MyReadonlyStruct value = new(3);
		sut.Mock.Setup.MethodWithInParameter(It.IsAny<MyReadonlyStruct>())
			.Returns(p1 => p1.Value);

		int result = sut.MethodWithInParameter(in value);

		await That(result).IsEqualTo(3);
	}

	[Test]
	public async Task WithOptionalParameters_ShouldUseOptionalValueWhenNotSet()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.MyMethodWithOptionalParameters(It.IsAny<int>()).Returns(true);

		bool result1 = sut.MyMethodWithOptionalParameters(5);
		bool result2 = sut.MyMethodWithOptionalParameters(5, 1);
		bool result3 = sut.MyMethodWithOptionalParameters(5, c: "bar");

		await That(result1).IsTrue();
		await That(result2).IsFalse();
		await That(result3).IsFalse();
	}

	[Test]
	public async Task WithParamsParameters_ExplicitArrayArgument_ShouldUseValueEquality()
	{
		IMyService sut = IMyService.CreateMock();
		bool[] flags = [true, false,];
		sut.Mock.Setup.MyMethodWithParams(1, flags).Returns(true);

		bool result1 = sut.MyMethodWithParams(1, flags);
		bool result2 = sut.MyMethodWithParams(1, true, false);
		bool result3 = sut.MyMethodWithParams(1, false, true);

		await That(result1).IsTrue();
		await That(result2).IsTrue();
		await That(result3).IsFalse();
	}

	[Test]
	public async Task WithParamsParameters_PerElementValues_ShouldMatchElementByElement()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.MyMethodWithParams(It.IsAny<int>(), true, false).Returns(true);

		bool result1 = sut.MyMethodWithParams(5, true, false);
		bool result2 = sut.MyMethodWithParams(5, true, true);
		bool result3 = sut.MyMethodWithParams(5, true);
		bool result4 = sut.MyMethodWithParams(5, true, false, true);

		await That(result1).IsTrue();
		await That(result2).IsFalse();
		await That(result3).IsFalse();
		await That(result4).IsFalse();
	}

	[Test]
	public async Task WithParamsParameters_PerElementValues_ShouldSupportVerify()
	{
		IMyService sut = IMyService.CreateMock();

		_ = sut.MyMethodWithParams(5, true, false);

		await That(sut.Mock.Verify.MyMethodWithParams(It.IsAny<int>(), true, false)).Once();
		await That(sut.Mock.Verify.MyMethodWithParams(It.IsAny<int>(), true, true)).Never();
	}

	[Test]
	public async Task WithParamsParameters_ShouldSupportParams()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.MyMethodWithParams(It.IsAny<int>(), It.Satisfies<bool[]>(x => x.Length > 1)).Returns(true);

		bool result1 = sut.MyMethodWithParams(5);
		bool result2 = sut.MyMethodWithParams(5, true);
		bool result3 = sut.MyMethodWithParams(5, true, false);
		bool result4 = sut.MyMethodWithParams(5, true, false, true);

		await That(result1).IsFalse();
		await That(result2).IsFalse();
		await That(result3).IsTrue();
		await That(result4).IsTrue();
	}

	[Test]
	public async Task WithParamsParameters_PerElementMatchers_ShouldMatchElementByElement()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.MyMethodWithParams(It.IsAny<int>(), It.IsAny<bool>(), It.IsFalse()).Returns(true);

		bool result1 = sut.MyMethodWithParams(5);
		bool result2 = sut.MyMethodWithParams(5, true);
		bool result3 = sut.MyMethodWithParams(5, true, false);
		bool result4 = sut.MyMethodWithParams(5, true, true);
		bool result5 = sut.MyMethodWithParams(5, true, false, true);

		await That(result1).IsFalse();
		await That(result2).IsFalse();
		await That(result3).IsTrue();
		await That(result4).IsFalse();
		await That(result5).IsFalse();
	}

	[Test]
	public async Task WithParamsParameters_PerElementMatchers_ShouldSupportVerify()
	{
		IMyService sut = IMyService.CreateMock();

		_ = sut.MyMethodWithParams(5);
		_ = sut.MyMethodWithParams(5, true);
		_ = sut.MyMethodWithParams(5, true, false);
		_ = sut.MyMethodWithParams(5, true, true);
		_ = sut.MyMethodWithParams(5, true, false, true);

		await That(sut.Mock.Verify.MyMethodWithParams(
			It.IsAny<int>(), It.IsAny<bool>(), It.IsFalse())).Once();
	}

	[Test]
	public async Task WithRefReadonlyParameter_ShouldUseSetup()
	{
		IMethodSetupWithInAndRefReadonlyParameter sut = IMethodSetupWithInAndRefReadonlyParameter.CreateMock();
		MyReadonlyStruct value = new(3);
		sut.Mock.Setup.MethodWithRefReadonlyParameter(It.IsAnyRef<MyReadonlyStruct>())
			.Returns(p1 => p1.Value);

		int result = sut.MethodWithRefReadonlyParameter(in value);

		await That(result).IsEqualTo(3);
	}

	public class ReturnMethodWith0Parameters
	{
		[Test]
		public async Task Setup_ShouldBeVerifiable()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
			IMethodSetup setup = sut.Mock.Setup.Method0();

			sut.Method0();

			await That(sut.Mock.VerifySetup(setup)).Once();
		}

		[Test]
		public async Task ToString_ShouldReturnMethodSignature()
		{
			ReturnMethodSetup<int>.WithParameterCollection setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo");

			string result = setup.ToString();

			await That(result).IsEqualTo("int Foo()");
		}

		[Test]
		public async Task WhenSetupWithNull_ShouldReturnDefaultValue()
		{
			int callCount = 0;
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method0()
				.Do(() => { callCount++; })
				.Returns((string?)null!);

			string result = sut.Method0();

			await That(callCount).IsEqualTo(1);
			await That(result).IsNull();
		}
	}

	public class ReturnMethodWith1Parameters
	{
		[Test]
		public async Task AnyParameters_OnUntypedDefaultArgument_ShouldBindToValuesOverload()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method1(default).AnyParameters()
				.Returns("foo");

			await That(sut.Method1(0)).IsEqualTo("foo");
			await That(sut.Method1(42)).IsEqualTo("foo");
		}

		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method1(1).AnyParameters()
				.Returns("foo");

			string result1 = sut.Method1(1);
			string result2 = sut.Method1(2);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEqualTo("foo");
		}

		[Test]
		[Arguments(-1, 0)]
		[Arguments(1, 1)]
		public async Task Setup_ShouldBeVerifiable(int firstParameter, int expectedCallCount)
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
			IMethodSetup setup = sut.Mock.Setup.Method1(It.Satisfies<int>(x => x > 0));

			sut.Method1(firstParameter);

			await That(sut.Mock.VerifySetup(setup)).Exactly(expectedCallCount);
		}

		[Test]
		public async Task ToString_AnyParameters_ShouldReturnMethodSignature()
		{
			ReturnMethodSetup<int, string>.WithParameters setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo", Match.AnyParameters(), "p1");

			string result = setup.ToString();

			await That(result).IsEqualTo("int Foo(Match.AnyParameters())");
		}

		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
			sut.Mock.Setup.Method1(1).Returns("foo");
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("string Method1(1)");
		}

		[Test]
		public async Task ToString_ShouldReturnMethodSignature()
		{
			ReturnMethodSetup<int, string>.WithParameterCollection setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo", (IParameterMatch<string>)It.IsAny<string>());

			string result = setup.ToString();

			await That(result).IsEqualTo("int Foo(It.IsAny<string>())");
		}

		[Test]
		public async Task WhenSetupWithNull_ShouldReturnDefaultValue()
		{
			int callCount = 0;
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method1(It.IsAny<int>())
				.Do(() => { callCount++; })
				.Returns((string?)null!);

			string result = sut.Method1(1);

			await That(callCount).IsEqualTo(1);
			await That(result).IsNull();
		}

		[Test]
		public async Task WithExplicitParameter_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method1(1)
				.Returns("foo");

			string result1 = sut.Method1(1);
			string result2 = sut.Method1(2);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}
	}

	public class ReturnMethodWith2Parameters
	{
		[Test]
		public async Task AnyParameters_OnUntypedDefaultArguments_ShouldBindToValuesOverload()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method2(default, default).AnyParameters()
				.Returns("foo");

			await That(sut.Method2(0, 0)).IsEqualTo("foo");
			await That(sut.Method2(7, 9)).IsEqualTo("foo");
		}

		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method2(1, 2).AnyParameters()
				.Returns("foo");

			string result1 = sut.Method2(1, 2);
			string result2 = sut.Method2(2, 3);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEqualTo("foo");
		}

		[Test]
		[Arguments(-1, 0)]
		[Arguments(1, 1)]
		public async Task Setup_ShouldBeVerifiable(int firstParameter, int expectedCallCount)
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
			IMethodSetup setup = sut.Mock.Setup.Method2(
				It.Satisfies<int>(x => x > 0), It.IsAny<int>());

			sut.Method2(firstParameter, 2);

			await That(sut.Mock.VerifySetup(setup)).Exactly(expectedCallCount);
		}

		[Test]
		public async Task ToString_AnyParameters_ShouldReturnMethodSignature()
		{
			ReturnMethodSetup<int, string, long>.WithParameters setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo", Match.AnyParameters(), "p1", "p2");

			string result = setup.ToString();

			await That(result).IsEqualTo("int Foo(Match.AnyParameters())");
		}

		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
			sut.Mock.Setup.Method2(1, 2).Returns("foo");
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("string Method2(1, 2)");
		}

		[Test]
		public async Task ToString_ShouldReturnMethodSignature()
		{
			ReturnMethodSetup<int, string, long>.WithParameterCollection setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo",
				(IParameterMatch<string>)It.IsAny<string>(),
				(IParameterMatch<long>)It.IsAny<long>());

			string result = setup.ToString();

			await That(result).IsEqualTo("int Foo(It.IsAny<string>(), It.IsAny<long>())");
		}

		[Test]
		public async Task WhenSetupWithNull_ShouldReturnDefaultValue()
		{
			int callCount = 0;
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
				.Do(() => { callCount++; })
				.Returns((string?)null!);

			string result = sut.Method2(1, 2);

			await That(callCount).IsEqualTo(1);
			await That(result).IsNull();
		}

		[Test]
		public async Task WithExplicitParameter1_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method2(1, It.IsAny<int>())
				.Returns("foo");

			string result1 = sut.Method2(1, 10);
			string result2 = sut.Method2(2, 10);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameter2_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method2(It.IsAny<int>(), 1)
				.Returns("foo");

			string result1 = sut.Method2(10, 1);
			string result2 = sut.Method2(10, 2);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method2(1, 2)
				.Returns("foo");

			string result1 = sut.Method2(1, 2);
			string result2 = sut.Method2(1, 10);
			string result3 = sut.Method2(10, 2);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
		}
	}

	public class ReturnMethodWith3Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method3(1, 2, 3).AnyParameters()
				.Returns("foo");

			string result1 = sut.Method3(1, 2, 3);
			string result2 = sut.Method3(2, 3, 4);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEqualTo("foo");
		}

		[Test]
		[Arguments(-1, 0)]
		[Arguments(1, 1)]
		public async Task Setup_ShouldBeVerifiable(int firstParameter, int expectedCallCount)
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
			IMethodSetup setup = sut.Mock.Setup.Method3(
				It.Satisfies<int>(x => x > 0), It.IsAny<int>(), It.IsAny<int>());

			sut.Method3(firstParameter, 2, 3);

			await That(sut.Mock.VerifySetup(setup)).Exactly(expectedCallCount);
		}

		[Test]
		public async Task ToString_AnyParameters_ShouldReturnMethodSignature()
		{
			ReturnMethodSetup<int, string, long, int>.WithParameters setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo", Match.AnyParameters(), "p1", "p2", "p3");

			string result = setup.ToString();

			await That(result).IsEqualTo("int Foo(Match.AnyParameters())");
		}

		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
			sut.Mock.Setup.Method3(1, 2, 3).Returns("foo");
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("string Method3(1, 2, 3)");
		}

		[Test]
		public async Task ToString_ShouldReturnMethodSignature()
		{
			ReturnMethodSetup<int, string, long, int>.WithParameterCollection setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo",
				(IParameterMatch<string>)It.IsAny<string>(),
				(IParameterMatch<long>)It.IsAny<long>(),
				(IParameterMatch<int>)It.IsAny<int>());

			string result = setup.ToString();

			await That(result).IsEqualTo("int Foo(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>())");
		}

		[Test]
		public async Task WhenSetupWithNull_ShouldReturnDefaultValue()
		{
			int callCount = 0;
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => { callCount++; })
				.Returns((string?)null!);

			string result = sut.Method3(1, 2, 3);

			await That(callCount).IsEqualTo(1);
			await That(result).IsNull();
		}

		[Test]
		public async Task WithExplicitParameter1_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method3(1, It.IsAny<int>(), It.IsAny<int>())
				.Returns("foo");

			string result1 = sut.Method3(1, 10, 20);
			string result2 = sut.Method3(2, 10, 20);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameter2_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method3(It.IsAny<int>(), 1, It.IsAny<int>())
				.Returns("foo");

			string result1 = sut.Method3(10, 1, 20);
			string result2 = sut.Method3(10, 2, 20);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameter3_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), 1)
				.Returns("foo");

			string result1 = sut.Method3(10, 20, 1);
			string result2 = sut.Method3(10, 20, 2);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method3(1, 2, 3)
				.Returns("foo");

			string result1 = sut.Method3(1, 2, 3);
			string result2 = sut.Method3(1, 10, 20);
			string result3 = sut.Method3(10, 2, 20);
			string result4 = sut.Method3(10, 20, 3);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
			await That(result4).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters1And2_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method3(1, 2, It.IsAny<int>())
				.Returns("foo");

			string result1 = sut.Method3(1, 2, 20);
			string result2 = sut.Method3(1, 10, 20);
			string result3 = sut.Method3(10, 2, 20);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters1And3_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method3(1, It.IsAny<int>(), 2)
				.Returns("foo");

			string result1 = sut.Method3(1, 20, 2);
			string result2 = sut.Method3(1, 20, 10);
			string result3 = sut.Method3(10, 20, 2);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters2And3_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method3(It.IsAny<int>(), 1, 2)
				.Returns("foo");

			string result1 = sut.Method3(20, 1, 2);
			string result2 = sut.Method3(20, 1, 10);
			string result3 = sut.Method3(20, 10, 2);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
		}
	}

	public class ReturnMethodWith4Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(1, 2, 3, 4).AnyParameters()
				.Returns("foo");

			string result1 = sut.Method4(1, 2, 3, 4);
			string result2 = sut.Method4(2, 3, 4, 5);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEqualTo("foo");
		}

		[Test]
		[Arguments(-1, 0)]
		[Arguments(1, 1)]
		public async Task Setup_ShouldBeVerifiable(int firstParameter, int expectedCallCount)
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
			IMethodSetup setup = sut.Mock.Setup.Method4(
				It.Satisfies<int>(x => x > 0), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

			sut.Method4(firstParameter, 2, 3, 4);

			await That(sut.Mock.VerifySetup(setup)).Exactly(expectedCallCount);
		}

		[Test]
		public async Task ToString_AnyParameters_ShouldReturnMethodSignature()
		{
			ReturnMethodSetup<int, string, long, int, int>.WithParameters setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo", Match.AnyParameters(), "p1", "p2", "p3", "p4");

			string result = setup.ToString();

			await That(result).IsEqualTo("int Foo(Match.AnyParameters())");
		}

		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
			sut.Mock.Setup.Method4(1, 2, 3, 4).Returns("foo");
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("string Method4(1, 2, 3, 4)");
		}

		[Test]
		public async Task ToString_ShouldReturnMethodSignature()
		{
			ReturnMethodSetup<int, string, long, int, int>.WithParameterCollection setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo",
				(IParameterMatch<string>)It.IsAny<string>(),
				(IParameterMatch<long>)It.IsAny<long>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());

			string result = setup.ToString();

			await That(result)
				.IsEqualTo(
					"int Foo(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>())");
		}

		[Test]
		public async Task WhenSetupWithNull_ShouldReturnDefaultValue()
		{
			int callCount = 0;
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => { callCount++; })
				.Returns((string?)null!);

			string result = sut.Method4(1, 2, 3, 4);

			await That(callCount).IsEqualTo(1);
			await That(result).IsNull();
		}

		[Test]
		public async Task WithExplicitParameter1_2And3_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(1, 2, 3, It.IsAny<int>())
				.Returns("foo");

			string result1 = sut.Method4(1, 2, 3, 40);
			string result2 = sut.Method4(10, 2, 3, 40);
			string result3 = sut.Method4(1, 20, 3, 40);
			string result4 = sut.Method4(1, 2, 30, 40);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
			await That(result4).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameter1_2And4_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(1, 2, It.IsAny<int>(), 4)
				.Returns("foo");

			string result1 = sut.Method4(1, 2, 30, 4);
			string result2 = sut.Method4(10, 2, 30, 4);
			string result3 = sut.Method4(1, 20, 30, 4);
			string result4 = sut.Method4(1, 2, 30, 40);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
			await That(result4).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameter1_3And4_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(1, It.IsAny<int>(), 3, 4)
				.Returns("foo");

			string result1 = sut.Method4(1, 20, 3, 4);
			string result2 = sut.Method4(10, 20, 3, 4);
			string result3 = sut.Method4(1, 20, 3, 40);
			string result4 = sut.Method4(1, 20, 30, 4);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
			await That(result4).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameter1_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(1, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns("foo");

			string result1 = sut.Method4(1, 10, 20, 30);
			string result2 = sut.Method4(2, 10, 20, 30);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameter2_3And4_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(It.IsAny<int>(), 2, 3, 4)
				.Returns("foo");

			string result1 = sut.Method4(10, 2, 3, 4);
			string result2 = sut.Method4(10, 2, 3, 40);
			string result3 = sut.Method4(10, 20, 3, 4);
			string result4 = sut.Method4(10, 2, 30, 4);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
			await That(result4).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameter2_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(It.IsAny<int>(), 1, It.IsAny<int>(), It.IsAny<int>())
				.Returns("foo");

			string result1 = sut.Method4(10, 1, 20, 30);
			string result2 = sut.Method4(10, 2, 20, 30);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameter3_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), 1, It.IsAny<int>())
				.Returns("foo");

			string result1 = sut.Method4(10, 20, 1, 30);
			string result2 = sut.Method4(10, 20, 2, 30);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameter4_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), 1)
				.Returns("foo");

			string result1 = sut.Method4(10, 20, 30, 1);
			string result2 = sut.Method4(10, 20, 30, 2);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(1, 2, 3, 4)
				.Returns("foo");

			string result1 = sut.Method4(1, 2, 3, 4);
			string result2 = sut.Method4(1, 10, 20, 30);
			string result3 = sut.Method4(10, 2, 20, 30);
			string result4 = sut.Method4(10, 20, 3, 30);
			string result5 = sut.Method4(10, 20, 30, 4);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
			await That(result4).IsEmpty();
			await That(result5).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters1And2_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(1, 2, It.IsAny<int>(), It.IsAny<int>())
				.Returns("foo");

			string result1 = sut.Method4(1, 2, 20, 30);
			string result2 = sut.Method4(1, 10, 20, 30);
			string result3 = sut.Method4(10, 2, 20, 30);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters1And3_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(1, It.IsAny<int>(), 2, It.IsAny<int>())
				.Returns("foo");

			string result1 = sut.Method4(1, 20, 2, 30);
			string result2 = sut.Method4(1, 20, 10, 30);
			string result3 = sut.Method4(10, 20, 2, 30);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters1And4_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(1, It.IsAny<int>(), It.IsAny<int>(), 2)
				.Returns("foo");

			string result1 = sut.Method4(1, 20, 30, 2);
			string result2 = sut.Method4(1, 20, 30, 10);
			string result3 = sut.Method4(10, 20, 30, 2);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters2And3_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(It.IsAny<int>(), 1, 2, It.IsAny<int>())
				.Returns("foo");

			string result1 = sut.Method4(20, 1, 2, 30);
			string result2 = sut.Method4(20, 1, 10, 30);
			string result3 = sut.Method4(20, 10, 2, 30);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters2And4_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(It.IsAny<int>(), 1, It.IsAny<int>(), 2)
				.Returns("foo");

			string result1 = sut.Method4(10, 1, 20, 2);
			string result2 = sut.Method4(10, 1, 20, 10);
			string result3 = sut.Method4(10, 10, 20, 2);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters3And4_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), 1, 2)
				.Returns("foo");

			string result1 = sut.Method4(10, 20, 1, 2);
			string result2 = sut.Method4(10, 20, 1, 10);
			string result3 = sut.Method4(10, 20, 10, 2);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
		}
	}

	public class ReturnMethodWith5Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method5(1, 2, 3, 4, 5).AnyParameters()
				.Returns("foo");

			string result1 = sut.Method5(1, 2, 3, 4, 5);
			string result2 = sut.Method5(2, 3, 4, 5, 6);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEqualTo("foo");
		}

		[Test]
		[Arguments(-1, 0)]
		[Arguments(1, 1)]
		public async Task Setup_ShouldBeVerifiable(int firstParameter, int expectedCallCount)
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
			IMethodSetup setup = sut.Mock.Setup.Method5(
				It.Satisfies<int>(x => x > 0), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

			sut.Method5(firstParameter, 2, 3, 4, 5);

			await That(sut.Mock.VerifySetup(setup)).Exactly(expectedCallCount);
		}

		[Test]
		public async Task ToString_AnyParameters_ShouldReturnMethodSignature()
		{
			ReturnMethodSetup<int, string, long, int, int, int>.WithParameters setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo", Match.AnyParameters(), "p1", "p2", "p3", "p4", "p5");

			string result = setup.ToString();

			await That(result).IsEqualTo("int Foo(Match.AnyParameters())");
		}

		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
			sut.Mock.Setup.Method5(1, 2, 3, 4, 5).Returns("foo");
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("string Method5(1, 2, 3, 4, 5)");
		}

		[Test]
		public async Task ToString_ShouldReturnMethodSignature()
		{
			ReturnMethodSetup<int, string, long, int, int, int>.WithParameterCollection setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo",
				(IParameterMatch<string>)It.IsAny<string>(),
				(IParameterMatch<long>)It.IsAny<long>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());

			string result = setup.ToString();

			await That(result)
				.IsEqualTo(
					"int Foo(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())");
		}

		[Test]
		public async Task WhenSetupWithNull_ShouldReturnDefaultValue()
		{
			int callCount = 0;
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.Do(() => { callCount++; })
				.Returns((string?)null!);

			string result = sut.Method5(1, 2, 3, 4, 5);

			await That(callCount).IsEqualTo(1);
			await That(result).IsNull();
		}

		[Test]
		public async Task WithExplicitParameters_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method5(1, 2, 3, 4, 5)
				.Returns("foo");

			string result1 = sut.Method5(1, 2, 3, 4, 5);
			string result2 = sut.Method5(10, 2, 3, 4, 5);
			string result3 = sut.Method5(1, 20, 3, 4, 5);
			string result4 = sut.Method5(1, 2, 30, 4, 5);
			string result5 = sut.Method5(1, 2, 3, 40, 5);
			string result6 = sut.Method5(1, 2, 3, 4, 50);
			string result7 = sut.Method5(10, 20, 30, 40, 50);

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
			await That(result4).IsEmpty();
			await That(result5).IsEmpty();
			await That(result6).IsEmpty();
			await That(result7).IsEmpty();
		}
	}

	public class VoidMethodWith0Parameters
	{
		[Test]
		public async Task Setup_ShouldBeVerifiable()
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
			IMethodSetup setup = sut.Mock.Setup.Method0();

			sut.Method0();

			await That(sut.Mock.VerifySetup(setup)).Once();
		}

		[Test]
		public async Task ToString_ShouldReturnMethodSignature()
		{
			VoidMethodSetup.WithParameterCollection setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo");

			string result = setup.ToString();

			await That(result).IsEqualTo("void Foo()");
		}
	}

	public class VoidMethodWith1Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			int callCount = 0;
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method1(1).AnyParameters()
				.Do(() => callCount++);

			sut.Method1(1);
			sut.Method1(2);

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		[Arguments(-1, 0)]
		[Arguments(1, 1)]
		public async Task Setup_ShouldBeVerifiable(int firstParameter, int expectedCallCount)
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
			IMethodSetup setup = sut.Mock.Setup.Method1(It.Satisfies<int>(x => x > 0));

			sut.Method1(firstParameter);

			await That(sut.Mock.VerifySetup(setup)).Exactly(expectedCallCount);
		}

		[Test]
		public async Task ToString_AnyParameters_ShouldReturnMethodSignature()
		{
			VoidMethodSetup<string>.WithParameters setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo", Match.AnyParameters(), "p1");

			string result = setup.ToString();

			await That(result).IsEqualTo("void Foo(Match.AnyParameters())");
		}

		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
			sut.Mock.Setup.Method1(1).DoesNotThrow();
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("void Method1(1)");
		}

		[Test]
		public async Task ToString_ShouldReturnMethodSignature()
		{
			VoidMethodSetup<string>.WithParameterCollection setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo",
				(IParameterMatch<string>)It.IsAny<string>());

			string result = setup.ToString();

			await That(result).IsEqualTo("void Foo(It.IsAny<string>())");
		}
	}

	public class VoidMethodWith2Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			int callCount = 0;
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method2(1, 2).AnyParameters()
				.Do(() => callCount++);

			sut.Method2(1, 2);
			sut.Method2(2, 3);

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		[Arguments(-1, 0)]
		[Arguments(1, 1)]
		public async Task Setup_ShouldBeVerifiable(int firstParameter, int expectedCallCount)
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
			IMethodSetup setup = sut.Mock.Setup.Method2(
				It.Satisfies<int>(x => x > 0), It.IsAny<int>());

			sut.Method2(firstParameter, 2);

			await That(sut.Mock.VerifySetup(setup)).Exactly(expectedCallCount);
		}

		[Test]
		public async Task ToString_AnyParameters_ShouldReturnMethodSignature()
		{
			VoidMethodSetup<string, long>.WithParameters setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo", Match.AnyParameters(), "p1", "p2");

			string result = setup.ToString();

			await That(result).IsEqualTo("void Foo(Match.AnyParameters())");
		}

		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
			sut.Mock.Setup.Method2(1, 2).DoesNotThrow();
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("void Method2(1, 2)");
		}

		[Test]
		public async Task ToString_ShouldReturnMethodSignature()
		{
			VoidMethodSetup<string, long>.WithParameterCollection setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo",
				(IParameterMatch<string>)It.IsAny<string>(),
				(IParameterMatch<long>)It.IsAny<long>());

			string result = setup.ToString();

			await That(result).IsEqualTo("void Foo(It.IsAny<string>(), It.IsAny<long>())");
		}
	}

	public class VoidMethodWith3Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			int callCount = 0;
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method3(1, 2, 3).AnyParameters()
				.Do(() => callCount++);

			sut.Method3(1, 2, 3);
			sut.Method3(2, 3, 4);

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		[Arguments(-1, 0)]
		[Arguments(1, 1)]
		public async Task Setup_ShouldBeVerifiable(int firstParameter, int expectedCallCount)
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
			IMethodSetup setup = sut.Mock.Setup.Method3(
				It.Satisfies<int>(x => x > 0), It.IsAny<int>(), It.IsAny<int>());

			sut.Method3(firstParameter, 2, 3);

			await That(sut.Mock.VerifySetup(setup)).Exactly(expectedCallCount);
		}

		[Test]
		public async Task ToString_AnyParameters_ShouldReturnMethodSignature()
		{
			VoidMethodSetup<string, long, int>.WithParameters setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo", Match.AnyParameters(), "p1", "p2", "p3");

			string result = setup.ToString();

			await That(result).IsEqualTo("void Foo(Match.AnyParameters())");
		}

		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
			sut.Mock.Setup.Method3(1, 2, 3).DoesNotThrow();
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("void Method3(1, 2, 3)");
		}

		[Test]
		public async Task ToString_ShouldReturnMethodSignature()
		{
			VoidMethodSetup<string, long, int>.WithParameterCollection setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo",
				(IParameterMatch<string>)It.IsAny<string>(),
				(IParameterMatch<long>)It.IsAny<long>(),
				(IParameterMatch<int>)It.IsAny<int>());

			string result = setup.ToString();

			await That(result).IsEqualTo("void Foo(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>())");
		}
	}

	public class VoidMethodWith4Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			int callCount = 0;
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method4(1, 2, 3, 4).AnyParameters()
				.Do(() => callCount++);

			sut.Method4(1, 2, 3, 4);
			sut.Method4(2, 3, 4, 5);

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		[Arguments(-1, 0)]
		[Arguments(1, 1)]
		public async Task Setup_ShouldBeVerifiable(int firstParameter, int expectedCallCount)
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
			IMethodSetup setup = sut.Mock.Setup.Method4(
				It.Satisfies<int>(x => x > 0), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

			sut.Method4(firstParameter, 2, 3, 4);

			await That(sut.Mock.VerifySetup(setup)).Exactly(expectedCallCount);
		}

		[Test]
		public async Task ToString_AnyParameters_ShouldReturnMethodSignature()
		{
			VoidMethodSetup<string, long, int, int>.WithParameters setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo", Match.AnyParameters(), "p1", "p2", "p3", "p4");

			string result = setup.ToString();

			await That(result).IsEqualTo("void Foo(Match.AnyParameters())");
		}

		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
			sut.Mock.Setup.Method4(1, 2, 3, 4).DoesNotThrow();
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("void Method4(1, 2, 3, 4)");
		}

		[Test]
		public async Task ToString_ShouldReturnMethodSignature()
		{
			VoidMethodSetup<string, long, int, int>.WithParameterCollection setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo",
				(IParameterMatch<string>)It.IsAny<string>(),
				(IParameterMatch<long>)It.IsAny<long>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());

			string result = setup.ToString();

			await That(result)
				.IsEqualTo(
					"void Foo(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>())");
		}
	}

	public class VoidMethodWith5Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			int callCount = 0;
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

			sut.Mock.Setup.Method5(1, 2, 3, 4, 5).AnyParameters()
				.Do(() => callCount++);

			sut.Method5(1, 2, 3, 4, 5);
			sut.Method5(2, 3, 4, 5, 6);

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		[Arguments(-1, 0)]
		[Arguments(1, 1)]
		public async Task Setup_ShouldBeVerifiable(int firstParameter, int expectedCallCount)
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
			IMethodSetup setup = sut.Mock.Setup.Method5(
				It.Satisfies<int>(x => x > 0), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

			sut.Method5(firstParameter, 2, 3, 4, 5);

			await That(sut.Mock.VerifySetup(setup)).Exactly(expectedCallCount);
		}

		[Test]
		public async Task ToString_AnyParameters_ShouldReturnMethodSignature()
		{
			VoidMethodSetup<string, long, int, int, int>.WithParameters setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo", Match.AnyParameters(), "p1", "p2", "p3", "p4", "p5");

			string result = setup.ToString();

			await That(result).IsEqualTo("void Foo(Match.AnyParameters())");
		}

		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
			sut.Mock.Setup.Method5(1, 2, 3, 4, 5).DoesNotThrow();
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("void Method5(1, 2, 3, 4, 5)");
		}

		[Test]
		public async Task ToString_ShouldReturnMethodSignature()
		{
			VoidMethodSetup<string, long, int, int, int>.WithParameterCollection setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "Foo",
				(IParameterMatch<string>)It.IsAny<string>(),
				(IParameterMatch<long>)It.IsAny<long>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());

			string result = setup.ToString();

			await That(result)
				.IsEqualTo(
					"void Foo(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())");
		}
	}

	public class MethodWithDefaultParameter
	{
		[Test]
		public async Task ParametersWithDefaultAsDefaultValue_ShouldNotHaveAnyErrors()
		{
			IMethodSetupWithDefaultParameter sut = IMethodSetupWithDefaultParameter.CreateMock();

			sut.Mock.Setup.MethodWithDefaultParameter(It.IsAny<int>()).Returns(4);

			int result = sut.MethodWithDefaultParameter(2, CancellationToken.None);

			await That(result).IsEqualTo(4);
			await That(sut.Mock.Verify.MethodWithDefaultParameter(It.IsAny<int>())).Once();
		}
	}

	public interface IVoidMethodSetupWithParametersTest
	{
		void MethodWithMultipleOverloads(int p1, int p2);
		void MethodWithMultipleOverloads(int p1, bool p2);
		void MethodWithOutParameter(int p1, out int p2);
		void MethodWithRefParameter(int p1, ref int p2);
		void MethodWithoutOtherOverloads(int p1, int p2, int p3);
		void MethodWithSingleParameter(int p1);
	}

	public interface IMethodSetupWithInAndRefReadonlyParameter
	{
		int MethodWithInParameter(in MyReadonlyStruct p1);
		int MethodWithRefReadonlyParameter(ref readonly MyReadonlyStruct p1);
	}

	public interface IMethodSetupWithDefaultParameter
	{
		int MethodWithDefaultParameter(int value, CancellationToken cancellationToken = default);
	}

	public readonly struct MyReadonlyStruct(int value)
	{
		public int Value { get; } = value;
	}

	public interface IReturnMethodSetupWithParametersTest
	{
		string MethodWithMultipleOverloads(int p1, int p2);
		string MethodWithMultipleOverloads(int p1, bool p2);
		string MethodWithOutParameter(int p1, out int p2);
		string MethodWithRefParameter(int p1, ref int p2);
		string MethodWithoutOtherOverloads(int p1, int p2, int p3);
		string MethodWithSingleParameter(int p1);
	}

	public class MyMethodServiceType(int value)
	{
		public int Value { get; } = value;
	}

	public interface IMethodService
	{
		int Combine(MyMethodServiceType service1, MyMethodServiceType service2);
		void MyVoidMethodWithoutParameters();
		void MyVoidMethodWithParameters(int x, string y);
		int MyIntMethodWithoutParameters();
		int MyIntMethodWithParameters(int x, string y);
		int MyGenericMethod<T1, T2>(T1 x, T2 y) where T1 : struct where T2 : class;
		int MyGenericMethod<T>();
		void MyMethodWithRefParameter(ref int value);
		void MyMethodWithOutParameter(out int value);
		string ToString();
		bool Equals(object? obj);
		bool Equals(int other);
		int GetHashCode();
	}

	public interface IMyServiceWithMethodsWithMoreThan16Parameters
	{
		int ReturnMethod17(
			int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8,
			int p9, int p10, int p11, int p12, int p13, int p14, int p15, int p16,
			int p17);

		int ReturnMethod18(
			int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8,
			int p9, int p10, int p11, int p12, int p13, int p14, int p15, int p16,
			int p17, int p18);

		void VoidMethod17(
			int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8,
			int p9, int p10, int p11, int p12, int p13, int p14, int p15, int p16,
			int p17);

		void VoidMethod18(
			int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8,
			int p9, int p10, int p11, int p12, int p13, int p14, int p15, int p16,
			int p17, int p18);
	}

	public interface IReturnMethodSetupTest
	{
		string Method0();
		string Method0(bool withOtherParameter);
		string Method1(int p1);
		string Method1(int p1, bool withOtherParameter);
		string Method1WithOutParameter(out int p1);
		string Method1WithRefParameter(ref int p1);
		string Method2(int p1, int p2);
		string Method2(int p1, int p2, bool withOtherParameter);
		string Method2WithOutParameter(out int p1, out int p2);
		string Method2WithRefParameter(ref int p1, ref int p2);
		string Method3(int p1, int p2, int p3);
		string Method3(int p1, int p2, int p3, bool withOtherParameter);
		string Method3WithOutParameter(out int p1, out int p2, out int p3);
		string Method3WithRefParameter(ref int p1, ref int p2, ref int p3);
		string Method4(int p1, int p2, int p3, int p4);
		string Method4(int p1, int p2, int p3, bool withOtherParameter);
		string Method4WithOutParameter(out int p1, out int p2, out int p3, out int p4);
		string Method4WithRefParameter(ref int p1, ref int p2, ref int p3, ref int p4);
		string Method5(int p1, int p2, int p3, int p4, int p5);
		string Method5(int p1, int p2, int p3, int p4, int p5, bool withOtherParameter);
		string Method5WithOutParameter(out int p1, out int p2, out int p3, out int p4, out int p5);
		string Method5WithRefParameter(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5);
		string UniqueMethodWithParameters(int p1, int p2);
		string UniqueMethodWith5Parameters(int p1, int p2, int p3, int p4, int p5);
	}

	public class ReturnMethodSetupTest
	{
		public virtual string Method0()
			=> "foo";

		public virtual string Method1(int p1)
			=> $"foo-{p1}";

		public virtual string Method2(int p1, int p2)
			=> $"foo-{p1}-{p2}";

		public virtual string Method3(int p1, int p2, int p3)
			=> $"foo-{p1}-{p2}-{p3}";

		public virtual string Method4(int p1, int p2, int p3, int p4)
			=> $"foo-{p1}-{p2}-{p3}-{p4}";

		public virtual string Method5(int p1, int p2, int p3, int p4, int p5)
			=> $"foo-{p1}-{p2}-{p3}-{p4}-{p5}";
	}

	public interface IVoidMethodSetupTest
	{
		void Method0();
		void Method0(bool withOtherParameter);
		void Method1(int p1);
		void Method1(int p1, bool withOtherParameter);
		void Method1WithOutParameter(out int p1);
		void Method1WithRefParameter(ref int p1);
		void Method2(int p1, int p2);
		void Method2(int p1, int p2, bool withOtherParameter);
		void Method2WithOutParameter(out int p1, out int p2);
		void Method2WithRefParameter(ref int p1, ref int p2);
		void Method3(int p1, int p2, int p3);
		void Method3(int p1, int p2, int p3, bool withOtherParameter);
		void Method3WithOutParameter(out int p1, out int p2, out int p3);
		void Method3WithRefParameter(ref int p1, ref int p2, ref int p3);
		void Method4(int p1, int p2, int p3, int p4);
		void Method4(int p1, int p2, int p3, bool withOtherParameter);
		void Method4WithOutParameter(out int p1, out int p2, out int p3, out int p4);
		void Method4WithRefParameter(ref int p1, ref int p2, ref int p3, ref int p4);
		void Method5(int p1, int p2, int p3, int p4, int p5);
		void Method5(int p1, int p2, int p3, int p4, int p5, bool withOtherParameter);
		void Method5WithOutParameter(out int p1, out int p2, out int p3, out int p4, out int p5);
		void Method5WithRefParameter(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5);
		void UniqueMethodWithParameters(int p1, int p2);
		void UniqueMethodWith5Parameters(int p1, int p2, int p3, int p4, int p5);
	}

	[Test]
	public async Task ReturnMethodWith17Parameters_ShouldStillAllowCallbackAndReturns()
	{
		int isCalled = 0;
		IMyServiceWithMethodsWithMoreThan16Parameters sut =
			IMyServiceWithMethodsWithMoreThan16Parameters.CreateMock();

		sut.Mock.Setup.ReturnMethod17(
				It.Is(1), It.Is(2), It.Is(3), It.Is(4), It.Is(5), It.Is(6), It.Is(7), It.Is(8),
				It.Is(9), It.Is(10), It.Is(11), It.Is(12), It.Is(13), It.Is(14), It.Is(15), It.Is(16),
				It.Is(17))
			.Do((_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _)
				=> isCalled++)
			.Returns((p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16, p17)
				=> p1 + p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9 + p10 + p11 + p12 + p13 + p14 + p15 + p16 + p17);

		int result = sut.ReturnMethod17(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17);

		await That(isCalled).IsEqualTo(1);
		await That(result).IsEqualTo(153);
	}

	[Test]
	public async Task ReturnMethodWith18Parameters_ShouldStillAllowCallbackAndReturns()
	{
		int isCalled = 0;
		IMyServiceWithMethodsWithMoreThan16Parameters sut =
			IMyServiceWithMethodsWithMoreThan16Parameters.CreateMock();

		sut.Mock.Setup.ReturnMethod18(
				It.Is(1), It.Is(2), It.Is(3), It.Is(4), It.Is(5), It.Is(6), It.Is(7), It.Is(8),
				It.Is(9), It.Is(10), It.Is(11), It.Is(12), It.Is(13), It.Is(14), It.Is(15), It.Is(16),
				It.Is(17), It.Is(18))
			.Do((_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _)
				=> isCalled++)
			.Returns((p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16, p17, p18)
				=> p1 + p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9 + p10 + p11 + p12 + p13 + p14 + p15 + p16 + p17 + p18);

		int result = sut.ReturnMethod18(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);

		await That(isCalled).IsEqualTo(1);
		await That(result).IsEqualTo(171);
	}

	[Test]
	public async Task VoidMethodWith17Parameters_ShouldStillAllowCallbackAndReturns()
	{
		int isCalled = 0;
		IMyServiceWithMethodsWithMoreThan16Parameters sut =
			IMyServiceWithMethodsWithMoreThan16Parameters.CreateMock();

		sut.Mock.Setup.VoidMethod17(
				It.Is(1), It.Is(2), It.Is(3), It.Is(4), It.Is(5), It.Is(6), It.Is(7), It.Is(8),
				It.Is(9), It.Is(10), It.Is(11), It.Is(12), It.Is(13), It.Is(14), It.Is(15), It.Is(16),
				It.Is(17))
			.Do((_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _)
				=> isCalled++);

		sut.VoidMethod17(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17);

		await That(isCalled).IsEqualTo(1);
	}

	[Test]
	public async Task VoidMethodWith18Parameters_ShouldStillAllowCallbackAndReturns()
	{
		int isCalled = 0;
		IMyServiceWithMethodsWithMoreThan16Parameters sut =
			IMyServiceWithMethodsWithMoreThan16Parameters.CreateMock();

		sut.Mock.Setup.VoidMethod18(
				It.Is(1), It.Is(2), It.Is(3), It.Is(4), It.Is(5), It.Is(6), It.Is(7), It.Is(8),
				It.Is(9), It.Is(10), It.Is(11), It.Is(12), It.Is(13), It.Is(14), It.Is(15), It.Is(16),
				It.Is(17), It.Is(18))
			.Do((_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _)
				=> isCalled++);

		sut.VoidMethod18(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);

		await That(isCalled).IsEqualTo(1);
	}
}
