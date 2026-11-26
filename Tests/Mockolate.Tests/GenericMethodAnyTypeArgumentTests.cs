using System.Collections.Generic;
using Mockolate.Exceptions;
using Mockolate.Setup;
using Mockolate.Tests.GeneratorCoverage;
using Mockolate.Verify;

namespace Mockolate.Tests;

public sealed class GenericMethodAnyTypeArgumentTests
{
	[Test]
	public async Task SetupGeneric_ShouldMatchEveryTypeArgument()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		List<object?> received = [];
		sut.Mock.SetupGeneric.Plain(It.IsAny<string>()).Do(value => received.Add(value));

		sut.Plain("a");
		sut.Plain<object>("b");
		sut.Plain(1);

		await That(received).IsEqualTo(["a", "b",])
			.Because("the matcher is checked against the runtime value, whatever the type argument");
	}

	[Test]
	public async Task SetupGeneric_ShouldMatchValueTypesWithAnObjectMatcher()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		List<object?> received = [];
		sut.Mock.SetupGeneric.Plain(It.Satisfies<object>(value => value is int)).Do(value => received.Add(value));

		sut.Plain(1);
		sut.Plain(2L);
		sut.Plain("a");

		await That(received).IsEqualTo([1,]);
	}

	[Test]
	public async Task SetupGeneric_ShouldKeepTheTypeOfAParameterThatDoesNotDependOnATypeArgument()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		sut.Mock.SetupGeneric.Deserialize(It.Is("invalid")).Throws<InvalidOperationException>();

		void ActInt() => sut.Deserialize<int>("invalid");
		void ActString() => sut.Deserialize<string>("invalid");
		int result = sut.Deserialize<int>("valid");

		await That(ActInt).Throws<InvalidOperationException>();
		await That(ActString).Throws<InvalidOperationException>();
		await That(result).IsEqualTo(0)
			.Because("a setup for any type argument returns no value, so an unmatched call returns the default value");
	}

	[Test]
	public async Task SetupGeneric_WithParametersMatch_ShouldMatchEveryTypeArgument()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		sut.Mock.SetupGeneric.Send(Match.AnyParameters()).Throws<TimeoutException>();

		void ActInt() => sut.Send(new IntRequest());
		void ActString() => sut.Send(new StringRequest());

		await That(ActInt).Throws<TimeoutException>();
		await That(ActString).Throws<TimeoutException>();
	}

	[Test]
	public async Task SetupGeneric_ShouldIgnoreConstraints()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		int count = 0;
		sut.Mock.SetupGeneric.Struct(Match.AnyParameters()).Do(() => count++);
		sut.Mock.SetupGeneric.Constrained(It.IsAny<IntRequest>()).Do(() => count++);

		sut.Struct(1);
		sut.Struct(2.5);
		sut.Constrained(new IntRequest());

		await That(count).IsEqualTo(3);
	}

	[Test]
	public async Task SetupGeneric_WhenDefinedLater_ShouldWinOverASetupOfASpecificTypeArgument()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		List<string> received = [];
		sut.Mock.Setup.Plain(It.IsAny<string>()).Do(() => received.Add("specific"));
		sut.Mock.SetupGeneric.Plain(It.IsAny<string>()).Do(() => received.Add("any"));
		sut.Mock.Setup.Plain(It.IsAny<int>()).Do(() => received.Add("latest specific"));

		sut.Plain("a");
		sut.Plain(1);

		await That(received).IsEqualTo(["any", "latest specific",])
			.Because("setups of a specific type argument and for any type argument are searched together");
	}

	[Test]
	public async Task SetupGeneric_ShouldCountAsSetupForThrowWhenNotSetup()
	{
		IGenericMethodAnyTypeArgument sut =
			IGenericMethodAnyTypeArgument.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		sut.Mock.SetupGeneric.Plain(It.IsAny<string>());

		void ActMatching() => sut.Plain("a");
		void ActNotMatching() => sut.Plain(1);

		await That(ActMatching).DoesNotThrow();
		await That(ActNotMatching).Throws<MockNotSetupException>();
	}

	[Test]
	public async Task SetupGeneric_OnAClass_ShouldKeepCallingTheBaseClassUnlessSkipped()
	{
		GenericMethodAnyTypeArgumentBase sut = GenericMethodAnyTypeArgumentBase.CreateMock();
		int count = 0;
		sut.Mock.SetupGeneric.Virtual(It.IsAny<string>()).Do(() => count++);
		sut.Mock.SetupGeneric.Virtual(It.Satisfies<object>(value => value is int)).SkippingBaseClass().Do(() => count++);

		int[] results = [sut.Virtual("a"), sut.Virtual(1),];

		await That(count).IsEqualTo(2);
		await That(results).IsEqualTo([42, 0,])
			.Because("a setup for any type argument returns no value, so the base result stands unless it is skipped");
	}

	[Test]
	public async Task SetupGeneric_OnALogger_ShouldSetUpTheScopeForEveryStateType()
	{
		Microsoft.Extensions.Logging.ILogger sut = Microsoft.Extensions.Logging.ILogger.CreateMock();
		int count = 0;
		sut.Mock.SetupGeneric.BeginScope(Match.AnyParameters()).Do(() => count++);

		sut.BeginScope("a");
		sut.BeginScope(1);

		await That(count).IsEqualTo(2);
		await That(sut.Mock.VerifyGeneric.BeginScope(It.IsAny<string>())).Once();
	}

	[Test]
	public async Task SetupGeneric_InScenario_ShouldStillApply()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		int count = 0;
		sut.Mock.SetupGeneric.Plain(Match.AnyParameters()).Do(() => count++);
		sut.Mock.TransitionTo("other");

		sut.Plain("a");

		await That(count).IsEqualTo(1)
			.Because("a scenario falls back to the setups of the default scope");
	}

	[Test]
	public async Task VerifyGeneric_ShouldCountEveryTypeArgument()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();

		sut.Plain("a");
		sut.Plain<object>("b");
		sut.Plain(1);
		sut.Deserialize<int>("x");
		_ = sut.Send(new IntRequest());
		_ = sut.Send(new StringRequest());

		await That(sut.Mock.VerifyGeneric.Plain(It.IsAny<string>())).Twice();
		await That(sut.Mock.VerifyGeneric.Plain(It.Satisfies<object>(value => value is int))).Once();
		await That(sut.Mock.VerifyGeneric.Plain(Match.AnyParameters())).Exactly(3);
		await That(sut.Mock.VerifyGeneric.Deserialize(It.Is("x"))).Once();
		await That(sut.Mock.VerifyGeneric.Deserialize(It.Is("y"))).Never();
		await That(sut.Mock.VerifyGeneric.Send(Match.AnyParameters())).Twice();
		await That(sut.Mock.VerifyGeneric.Send(It.IsAny<StringRequest>())).Once();
	}

	[Test]
	public async Task VerifyGeneric_ShouldNotCountOtherMethods()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();

		sut.Sibling(1);
		sut.Two(1, 2);

		await That(sut.Mock.VerifyGeneric.Sibling(Match.AnyParameters())).Never()
			.Because("the non-generic sibling is another method");
		await That(sut.Mock.VerifyGeneric.Plain(Match.AnyParameters())).Never();
	}

	[Test]
	public async Task VerifyGeneric_OnAClosedGenericInterface_ShouldNotCountNonGenericMethods()
	{
		IGenericMethodAnyTypeArgumentOf<int> sut = IGenericMethodAnyTypeArgumentOf<int>.CreateMock();
		IVoidMethodSetupWithCallback<object?> setup = sut.Mock.SetupGeneric.Plain(Match.AnyParameters());

		sut.Other(1);
		sut.Plain("a");

		await That(sut.Mock.VerifyGeneric.Plain(Match.AnyParameters())).Once()
			.Because("the type arguments of the interface precede those of the method in the recorded name");
		await That(sut.Mock.VerifySetup(setup)).Once();
	}

	[Test]
	public async Task SetupGeneric_OfAHiddenBaseMethod_ShouldApplyToTheHiddenMethod()
	{
		IGenericMethodHidingDerived sut = IGenericMethodHidingDerived.CreateMock();
		sut.Mock.As<IGenericMethodHidingBase>().SetupGeneric.Hidden(Match.AnyParameters())
			.Throws<InvalidOperationException>();

		void ActHidden() => ((IGenericMethodHidingBase)sut).Hidden(1);
		string hiding = sut.Hidden(1);

		await That(ActHidden).Throws<InvalidOperationException>();
		await That(hiding).IsEmpty()
			.Because("the hiding method is another method");
	}

	[Test]
	public async Task VerifyGeneric_ShouldMarkTheInteractionsAsVerified()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();

		sut.Plain("a");
		sut.Plain(1);
		await That(sut.Mock.VerifyGeneric.Plain(Match.AnyParameters())).Twice();

		await That(sut.Mock.VerifyThatAllInteractionsAreVerified()).IsTrue();
	}

	[Test]
	public async Task VerifySetup_ShouldCountCallsOfEveryTypeArgument()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		IVoidMethodSetupWithCallback<object?> plain = sut.Mock.SetupGeneric.Plain(It.IsAny<string>());
		IVoidMethodSetupWithCallback<object?> any = sut.Mock.SetupGeneric.Plain(Match.AnyParameters());

		sut.Plain("a");
		sut.Plain<object>("b");
		sut.Plain(1);
		sut.Sibling(1);

		await That(sut.Mock.VerifySetup(plain)).Twice();
		await That(sut.Mock.VerifySetup(any)).Exactly(3)
			.Because("the non-generic sibling is another method");
	}

	[Test]
	public async Task VerifyThatAllSetupsAreUsed_ShouldConsiderCallsOfEveryTypeArgument()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		sut.Mock.SetupGeneric.Plain(It.IsAny<string>());
		sut.Mock.SetupGeneric.Deserialize(Match.AnyParameters());

		sut.Plain("a");
		bool afterPlain = sut.Mock.VerifyThatAllSetupsAreUsed();
		sut.Deserialize<int>("x");
		bool afterDeserialize = sut.Mock.VerifyThatAllSetupsAreUsed();

		await That(afterPlain).IsFalse();
		await That(afterDeserialize).IsTrue();
	}

	[Test]
	public async Task WithoutParameters_ShouldSetUpAndVerifyEveryTypeArgument()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		IVoidMethodSetup setup = sut.Mock.SetupGeneric.Create();
		setup.Throws<InvalidOperationException>();

		void ActInt() => sut.Create<int>();
		void ActList() => sut.Create<List<string>>();

		await That(ActInt).Throws<InvalidOperationException>();
		await That(ActList).Throws<InvalidOperationException>();
		await That(sut.Mock.VerifyGeneric.Create()).Twice();
		await That(sut.Mock.VerifySetup(setup)).Twice();
	}

	[Test]
	public async Task WithTwoTypeParameters_ShouldMatchEveryParameter()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		List<string> received = [];
		sut.Mock.SetupGeneric.Pair(It.IsAny<string>(), It.Satisfies<object>(value => value is int))
			.Do((key, value) => received.Add($"{key}={value}"));

		sut.Pair("a", 1);
		sut.Pair("b", "x");
		sut.Pair(1, 1);

		await That(received).IsEqualTo(["a=1",]);
		await That(sut.Mock.VerifyGeneric.Pair(Match.AnyParameters())).Exactly(3);
		await That(sut.Mock.VerifyGeneric.Pair(It.IsAny<object>(), It.IsAny<string>())).Once();
	}

	[Test]
	public async Task WithParametersThatDoNotDependOnATypeArgument_ShouldKeepTheirTypes()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		sut.Mock.SetupGeneric.Two(It.IsAny<object>(), It.Is(2)).Throws<InvalidOperationException>();
		sut.Mock.SetupGeneric.Three(Match.Parameters(values => values[1] is "b")).Throws<TimeoutException>();

		void ActTwo() => sut.Two("a", 2);
		void ActThree() => sut.Three(1, "b", 3);
		sut.Two("a", 3);
		int result = sut.Three(1, "c", 3);

		await That(ActTwo).Throws<InvalidOperationException>();
		await That(ActThree).Throws<TimeoutException>();
		await That(result).IsEqualTo(0);
		await That(sut.Mock.VerifyGeneric.Two(It.IsAny<string>(), It.Is(3))).Once();
		await That(sut.Mock.VerifyGeneric.Three(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<int>())).Exactly(2)
			.Because("a call that throws is recorded as well");
	}

	[Test]
	public async Task WithFourParameters_ShouldMatchEveryTypeArgument()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		int count = 0;
		sut.Mock.SetupGeneric.Four(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>())
			.Do(() => count++);

		sut.Four("a", "b", "c", "d");
		sut.Four<object>("a", 1, 2, "d");
		sut.Four(1, 2, 3, 4);

		await That(count).IsEqualTo(2);
		await That(sut.Mock.VerifyGeneric.Four(Match.AnyParameters())).Exactly(3);
	}

	[Test]
	public async Task AboveFourParameters_ShouldUseTheGeneratedSetups()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		IVoidMethodSetupWithCallback<object?, int, int, int, string> five = sut.Mock.SetupGeneric.Five(
			It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.Is("x"));
		five.Throws<InvalidOperationException>();
		sut.Mock.SetupGeneric.Six(Match.AnyParameters()).Throws<TimeoutException>();

		void ActFive() => sut.Five("a", 1, 2, 3, "x");
		void ActSix() => sut.Six(1, 2, 3, 4, 5, 6);
		int result = sut.Five(1, 1, 2, 3, "x");

		await That(ActFive).Throws<InvalidOperationException>();
		await That(ActSix).Throws<TimeoutException>();
		await That(result).IsEqualTo(0);
		await That(sut.Mock.VerifySetup(five)).Once();
		await That(sut.Mock.VerifyGeneric.Five(It.IsAny<object>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
			It.Is("x"))).Twice();
		await That(sut.Mock.VerifyGeneric.Six(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<object>(),
			It.IsAny<object>(), It.IsAny<object>(), It.Satisfies<object>(value => value is 6))).Once();
	}

	[Test]
	public async Task VerifyGeneric_WithSeveralParameters_WhenVerificationFails_ShouldDescribeTheMatchers()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();

		void Act()
			=> sut.Mock.VerifyGeneric.Pair(It.IsAny<string>(), It.Is("a")).Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Pair<TKey, TValue>(It.IsAny<string>(), \"a\") exactly once, but it never did.");
	}

	[Test]
	public async Task ParameterNamesOfGeneratedLocals_ShouldNotClash()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		int count = 0;
		sut.Mock.SetupGeneric.Collide(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>()).Do(() => count++);

		sut.Collide("a", "b", "c");
		sut.Collide(1, 2, 3);

		await That(count).IsEqualTo(1);
		await That(sut.Mock.VerifyGeneric.Collide(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<object>())).Twice();
	}

	[Test]
	public async Task OverloadsWithAnotherNumberOfTypeParameters_ShouldBeToldApart()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		IVoidMethodSetupWithCallback<object?> one = sut.Mock.SetupGeneric.Arity(It.IsAny<object>());
		IVoidMethodSetupWithCallback<int> two = sut.Mock.SetupGeneric.Arity(It.IsAny<int>());

		sut.Arity<string, string>(5);

		await That(sut.Mock.VerifyGeneric.Arity(It.IsAny<object>())).Never()
			.Because("the call has two type arguments, unlike the overload with one type parameter");
		await That(sut.Mock.VerifyGeneric.Arity(It.IsAny<int>())).Once();
		await That(sut.Mock.VerifySetup(one)).Never();
		await That(sut.Mock.VerifySetup(two)).Once();
	}

	[Test]
	public async Task OverloadsThatDifferInANullableValueType_ShouldBothBeOffered()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();

		sut.Nullable<string>(1);
		sut.Nullable<string>((int?)null);

		await That(sut.Mock.VerifyGeneric.Nullable(It.IsAny<int>())).Once();
		await That(sut.Mock.VerifyGeneric.Nullable(It.Is<int?>(null))).Once();
	}

	[Test]
	public async Task Combination_WithAnotherNameForTheTypeParameter_ShouldApplyTheSetup()
	{
		GenericMethodAnyTypeArgumentImplementor sut = GenericMethodAnyTypeArgumentImplementor.CreateMock()
			.Implementing<IGenericMethodAnyTypeArgumentImplemented>();
		sut.Mock.As<IGenericMethodAnyTypeArgumentImplemented>().SetupGeneric.Generic(Match.AnyParameters())
			.Throws<InvalidOperationException>();

		void Act() => ((IGenericMethodAnyTypeArgumentImplemented)sut).Generic(1);

		await That(Act).Throws<InvalidOperationException>()
			.Because("the name of a setup for any type argument does not depend on the names of the type parameters");
	}

	[Test]
	public async Task SetupProtectedGeneric_ShouldSetUpAndVerifyEveryTypeArgument()
	{
		GenericMethodAnyTypeArgumentBase sut = GenericMethodAnyTypeArgumentBase.CreateMock();
		sut.Mock.SetupProtectedGeneric.Protected(It.IsAny<string>()).Throws<InvalidOperationException>();

		void Act() => sut.CallProtected("a");
		int result = sut.CallProtected(1);

		await That(Act).Throws<InvalidOperationException>();
		await That(result).IsEqualTo(42);
		await That(sut.Mock.VerifyProtectedGeneric.Protected(Match.AnyParameters())).Twice();
		await That(sut.Mock.VerifyProtectedGeneric.Protected(It.IsAny<string>())).Once();
	}

	[Test]
	public async Task SetupGeneric_InScenario_ShouldOnlyApplyInTheScenario()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		List<string> received = [];
		sut.Mock.InScenario("special").SetupGeneric.Plain(Match.AnyParameters()).Do(() => received.Add("special"));
		sut.Mock.SetupGeneric.Plain(Match.AnyParameters()).Do(() => received.Add("default"));

		sut.Plain("a");
		sut.Mock.TransitionTo("special");
		sut.Plain(1);

		await That(received).IsEqualTo(["default", "special",]);
	}

	[Test]
	public async Task SetupProtectedGeneric_InScenario_ShouldOnlyApplyInTheScenario()
	{
		GenericMethodAnyTypeArgumentBase sut = GenericMethodAnyTypeArgumentBase.CreateMock();
		sut.Mock.InScenario("special").SetupProtectedGeneric.Protected(Match.AnyParameters())
			.Throws<InvalidOperationException>();

		int result = sut.CallProtected("a");
		sut.Mock.TransitionTo("special");
		void Act() => sut.CallProtected("a");

		await That(result).IsEqualTo(42);
		await That(Act).Throws<InvalidOperationException>();
	}

	[Test]
	public async Task VerifyGeneric_OnAMonitor_ShouldOnlyCountTheMonitoredCalls()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		global::Mockolate.Monitor.MockMonitor<global::Mockolate.Mock.IMockVerifyForIGenericMethodAnyTypeArgument> monitor =
			sut.Mock.Monitor();

		sut.Plain("a");
		using (monitor.Run())
		{
			sut.Plain("b");
			sut.Plain(1);
		}

		await That(monitor.VerifyGeneric.Plain(Match.AnyParameters())).Twice();
		await That(monitor.VerifyGeneric.Plain(It.IsAny<string>())).Once();
	}

	[Test]
	public async Task VerifyGeneric_WhenVerificationFails_ShouldDescribeTheMatcher()
	{
		IGenericMethodAnyTypeArgument sut = IGenericMethodAnyTypeArgument.CreateMock();
		sut.Plain(1);

		void Act()
			=> sut.Mock.VerifyGeneric.Plain(It.IsAny<string>()).Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Plain<T>(It.IsAny<string>()) exactly once, but it never did.");
	}

	private sealed class IntRequest : IAnyTypeRequest<int>;

	private sealed class StringRequest : IAnyTypeRequest<string>;
}
