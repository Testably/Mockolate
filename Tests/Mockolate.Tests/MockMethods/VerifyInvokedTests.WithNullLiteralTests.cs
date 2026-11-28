using System.Collections.Generic;
using Mockolate.Exceptions;
using Mockolate.Parameters;
using Mockolate.Verify;

namespace Mockolate.Tests.MockMethods;

public sealed partial class VerifyInvokedTests
{
	public sealed class WithNullLiteralTests
	{
		[Test]
		public async Task WhenOnlyNonNullCallsHappened_ShouldFail()
		{
			INullLiteralService sut = INullLiteralService.CreateMock();
			sut.OneObject("a");
			sut.OneString("a");
			sut.OneObjectNotAnnotated("a");
			sut.OneStringNotAnnotated("a");
			sut.OneInterface("a");
			sut.OneNullableInt(1);
			sut.TwoObject("a", 1);
			sut.TwoString("a", 1);
			sut.TwoNullableInt(1, 1);
			sut.OptionalObject("a");
			sut.OptionalString("a");
			sut.OptionalWithNonNullDefault("x");
			sut.WithParams("a");

			string[] messages =
			[
				Capture(() => sut.Mock.Verify.OneObject(null).Once()),
				Capture(() => sut.Mock.Verify.OneString(null).Once()),
				Capture(() => sut.Mock.Verify.OneObjectNotAnnotated(null).Once()),
				Capture(() => sut.Mock.Verify.OneStringNotAnnotated(null!).Once()),
				Capture(() => sut.Mock.Verify.OneInterface(null).Once()),
				Capture(() => sut.Mock.Verify.OneNullableInt(null).Once()),
				Capture(() => sut.Mock.Verify.TwoObject(null, 1).Once()),
				Capture(() => sut.Mock.Verify.TwoString(null, 1).Once()),
				Capture(() => sut.Mock.Verify.TwoNullableInt(null, 1).Once()),
				Capture(() => sut.Mock.Verify.OptionalObject(null).Once()),
				Capture(() => sut.Mock.Verify.OptionalString(null).Once()),
				Capture(() => sut.Mock.Verify.OptionalWithNonNullDefault(null).Once()),
				Capture(() => sut.Mock.Verify.WithParams(null).Once()),
			];

			await That(messages).IsEqualTo([
				"Expected that mock invoked method OneObject(null) exactly once, but it never did.",
				"Expected that mock invoked method OneString(null) exactly once, but it never did.",
				"Expected that mock invoked method OneObjectNotAnnotated(null) exactly once, but it never did.",
				"Expected that mock invoked method OneStringNotAnnotated(null) exactly once, but it never did.",
				"Expected that mock invoked method OneInterface(null) exactly once, but it never did.",
				"Expected that mock invoked method OneNullableInt(null) exactly once, but it never did.",
				"Expected that mock invoked method TwoObject(null, 1) exactly once, but it never did.",
				"Expected that mock invoked method TwoString(null, 1) exactly once, but it never did.",
				"Expected that mock invoked method TwoNullableInt(null, 1) exactly once, but it never did.",
				"Expected that mock invoked method OptionalObject(null, 5) exactly once, but it never did.",
				"Expected that mock invoked method OptionalString(null, 5) exactly once, but it never did.",
				"Expected that mock invoked method OptionalWithNonNullDefault(null, 5) exactly once, but it never did.",
				"Expected that mock invoked method WithParams(null, []) exactly once, but it never did.",
			]).Because("a null literal means that the argument was null, not that any argument matches");
		}

		[Test]
		public async Task ShouldCountOnlyTheNullCalls()
		{
			INullLiteralService sut = INullLiteralService.CreateMock();
			sut.OneObject("a");
			sut.OneObject(null);
			sut.OneString("a");
			sut.OneString(null);
			sut.OneObjectNotAnnotated("a");
			sut.OneObjectNotAnnotated(null!);
			sut.OneStringNotAnnotated("a");
			sut.OneStringNotAnnotated(null!);
			sut.OneInterface("a");
			sut.OneInterface(null);
			sut.OneNullableInt(1);
			sut.OneNullableInt(null);
			sut.TwoObject("a", 1);
			sut.TwoObject(null, 1);
			sut.TwoObject(null, 2);
			sut.TwoString("a", 1);
			sut.TwoString(null, 1);
			sut.TwoString(null, 2);
			sut.TwoNullableInt(1, 1);
			sut.TwoNullableInt(null, 1);
			sut.TwoNullableInt(null, 2);
			sut.OptionalObject("a");
			sut.OptionalObject(null);
			sut.OptionalObject(null, 6);
			sut.OptionalString("a");
			sut.OptionalString(null);
			sut.OptionalString(null, 6);
			sut.OptionalWithNonNullDefault();
			sut.OptionalWithNonNullDefault(null);
			sut.WithParams("a");
			sut.WithParams(null);
			sut.WithParams(null, 1);

			string[] messages =
			[
				Capture(() => sut.Mock.Verify.OneObject(null).Once()),
				Capture(() => sut.Mock.Verify.OneString(null).Once()),
				Capture(() => sut.Mock.Verify.OneObjectNotAnnotated(null).Once()),
				Capture(() => sut.Mock.Verify.OneStringNotAnnotated(null!).Once()),
				Capture(() => sut.Mock.Verify.OneInterface(null).Once()),
				Capture(() => sut.Mock.Verify.OneNullableInt(null).Once()),
				Capture(() => sut.Mock.Verify.TwoObject(null, 1).Once()),
				Capture(() => sut.Mock.Verify.TwoString(null, 1).Once()),
				Capture(() => sut.Mock.Verify.TwoNullableInt(null, 1).Once()),
				Capture(() => sut.Mock.Verify.OptionalObject(null).Once()),
				Capture(() => sut.Mock.Verify.OptionalString(null).Once()),
				Capture(() => sut.Mock.Verify.OptionalWithNonNullDefault(null).Once()),
				Capture(() => sut.Mock.Verify.WithParams(null).Once()),
			];

			await That(messages).All().AreEqualTo("no exception")
				.Because("a null literal matches exactly the calls whose argument was null");
		}

		[Test]
		public async Task Setup_ShouldMatchOnlyNullCalls()
		{
			INullLiteralService sut = INullLiteralService.CreateMock();
			sut.Mock.Setup.OneObject(null).Returns(1);
			sut.Mock.Setup.OneString(null).Returns(1);
			sut.Mock.Setup.OneObjectNotAnnotated(null).Returns(1);
			sut.Mock.Setup.OneStringNotAnnotated(null!).Returns(1);
			sut.Mock.Setup.OneInterface(null).Returns(1);
			sut.Mock.Setup.OneNullableInt(null).Returns(1);
			sut.Mock.Setup.TwoObject(null, 1).Returns(1);
			sut.Mock.Setup.TwoString(null, 1).Returns(1);
			sut.Mock.Setup.TwoNullableInt(null, 1).Returns(1);
			sut.Mock.Setup.OptionalObject(null).Returns(1);
			sut.Mock.Setup.OptionalString(null).Returns(1);
			sut.Mock.Setup.OptionalWithNonNullDefault(null).Returns(1);
			sut.Mock.Setup.WithParams(null).Returns(1);

			int[] forNull =
			[
				sut.OneObject(null),
				sut.OneString(null),
				sut.OneObjectNotAnnotated(null!),
				sut.OneStringNotAnnotated(null!),
				sut.OneInterface(null),
				sut.OneNullableInt(null),
				sut.TwoObject(null, 1),
				sut.TwoString(null, 1),
				sut.TwoNullableInt(null, 1),
				sut.OptionalObject(null),
				sut.OptionalString(null),
				sut.OptionalWithNonNullDefault(null),
				sut.WithParams(null),
			];
			int[] forOther =
			[
				sut.OneObject("a"),
				sut.OneString("a"),
				sut.OneObjectNotAnnotated("a"),
				sut.OneStringNotAnnotated("a"),
				sut.OneInterface("a"),
				sut.OneNullableInt(1),
				sut.TwoObject("a", 1),
				sut.TwoString("a", 1),
				sut.TwoNullableInt(1, 1),
				sut.TwoObject(null, 2),
				sut.OptionalObject("a"),
				sut.OptionalString("a"),
				sut.OptionalObject(null, 6),
				sut.OptionalString(null, 6),
				sut.OptionalWithNonNullDefault(),
				sut.WithParams("a"),
				sut.WithParams(null, 1),
			];

			await That(forNull).All().AreEqualTo(1)
				.Because("a setup with a null literal matches the calls whose argument was null");
			await That(forOther).All().AreEqualTo(0)
				.Because("a setup with a null literal must not match a non-null argument");
		}

		[Test]
		public async Task WithNullParametersVariable_ShouldMeanTheArgumentIsNull()
		{
			INullLiteralService sut = INullLiteralService.CreateMock();
			IParameters? parameters = null;
			sut.Mock.Setup.OneObject(parameters).Returns(1);
			sut.OneObject("a");
			sut.OneObject(null);

			await That(sut.OneObject("b")).IsEqualTo(0)
				.Because("a null IParameters stands for a null argument, not for any arguments");
			await That(sut.Mock.Verify.OneObject(parameters)).Once();
		}

		[Test]
		public async Task AnyParameters_ShouldStillMatchEveryCall()
		{
			INullLiteralService sut = INullLiteralService.CreateMock();
			sut.Mock.Setup.OneObject(Match.AnyParameters()).Returns(1);
			sut.OneObject("a");
			sut.OneObject(null);
			sut.OptionalObject("a", 6);

			await That(sut.OneObject("b")).IsEqualTo(1);
			await That(sut.Mock.Verify.OneObject(Match.AnyParameters())).Exactly(3);
			await That(sut.Mock.Verify.OptionalObject(Match.AnyParameters())).Once();
		}

		// With a generic sibling the lone null binds the IParameters overload, which matches any parameters (Mockolate0006).
#pragma warning disable Mockolate0006
		[Test]
		public async Task WithGenericSibling_ShouldMatchTheNullCallInSetupAndVerify()
		{
			IGenericSiblingService sut = IGenericSiblingService.CreateMock();
			sut.Mock.Setup.M(null!).Returns(1);

			int result = sut.M((object?)null);

			await That(result).IsEqualTo(1)
				.Because("a lone null must not bind to the generic sibling M<T>");
			await That(sut.Mock.Verify.M(null!)).Once();
		}
#pragma warning restore Mockolate0006

		[Test]
		public async Task WithNonGenericSiblingAndTypeArgument_ShouldMatchTheNullCallInSetupAndVerify()
		{
			IGenericSiblingService sut = IGenericSiblingService.CreateMock();
			sut.Mock.Setup.M<string>(null!).Returns(1);

			int[] results = [sut.M<string>(null!), sut.M("a"),];

			await That(results).IsEqualTo([1, 0,])
				.Because("with type arguments the non-generic sibling M(object?) cannot capture the lone null");
			await That(sut.Mock.Verify.M<string>(null!)).Once();
		}

#if NET11_0_OR_GREATER
		// In classic mode a null literal is ambiguous here: unlike for methods, the delegate overloads carry no OverloadResolutionPriority.
		[Test]
		public async Task Delegate_ShouldMatchOnlyNullCallsInSetupAndVerify()
		{
			Func<object?, int> sut = Func<object?, int>.CreateMock();
			sut.Mock.Setup(null).Returns(1);

			int forValue = sut("a");

			void Act()
				=> sut.Mock.Verify(null).Once();

			await That(forValue).IsEqualTo(0)
				.Because("a setup with a null literal must not match a non-null argument");
			await That(Act).Throws<MockVerificationException>()
				.WithMessage("Expected that mock invoked method Invoke(null) exactly once, but it never did.");

			int forNull = sut(null);

			await That(forNull).IsEqualTo(1)
				.Because("a setup with a null literal matches a null argument");
			await That(sut.Mock.Verify(null)).Once();
		}
#endif

		[Test]
		public async Task WithValueTypeNextToMatcher_ShouldMatchTheDefaultValueInSetupAndVerify()
		{
			IValueTypeService sut = IValueTypeService.CreateMock();
			sut.Mock.Setup.Add(null, It.IsAny<int>()).Returns(7);
			sut.Mock.Setup.Five(null, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Returns(7);
			sut.Mock.Setup[null, It.IsAny<int>()].Returns(7);

			int[] forDefault = [sut.Add(0, 1), sut.Five(0, 1, 2, 3, 4), sut[0, 1],];
			int[] forOther = [sut.Add(1, 1), sut.Five(1, 1, 2, 3, 4), sut[1, 1],];
			sut[0, 2] = 3;
			sut[1, 2] = 3;

			await That(forDefault).All().AreEqualTo(7)
				.Because("a null literal for a non-nullable value type stands for its default value");
			await That(forOther).All().AreEqualTo(0);
			await That(sut.Mock.Verify.Add(null, It.IsAny<int>())).Once();
			await That(sut.Mock.Verify.Five(null, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
				.Once();
			await That(sut.Mock.Verify[null, It.IsAny<int>()].Got()).Once();
			await That(sut.Mock.Verify[null, It.IsAny<int>()].Set(It.IsAny<int>())).Once();
		}

		[Test]
		public async Task IndexerSetter_WithValueTypeNextToMatcher_ShouldMatchTheDefaultValue()
		{
			IValueTypeService sut = IValueTypeService.CreateMock();
			List<int> written = [];
			sut.Mock.Setup[null, It.IsAny<int>()].OnSet.Do(value => written.Add(value));

			sut[0, 1] = 2;
			sut[1, 1] = 3;

			await That(written).IsEqualTo([2,]);
		}

		[Test]
		public async Task WithGenericValueTypeNextToMatcher_ShouldMatchTheDefaultValueInSetupAndVerify()
		{
			IValueTypeService sut = IValueTypeService.CreateMock();
			sut.Mock.Setup.Generic<int>(null, It.IsAny<int>()).Returns(7);

			int[] results = [sut.Generic(0, 1), sut.Generic(1, 1),];

			await That(results).IsEqualTo([7, 0,]);
			await That(sut.Mock.Verify.Generic<int>(null, It.IsAny<int>())).Once();
		}

		[Test]
		public async Task WithNullMatcherVariableForValueType_ShouldMatchTheDefaultValueInSetupAndVerify()
		{
			IValueTypeService sut = IValueTypeService.CreateMock();
			IParameter<int> matcher = null!;
			sut.Mock.Setup.Add(matcher, It.IsAny<int>()).Returns(7);

			int[] results = [sut.Add(0, 1), sut.Add(1, 1),];

			await That(results).IsEqualTo([7, 0,])
				.Because("a null matcher stands for the null literal, which means default(T) for a value type");
			await That(sut.Mock.Verify.Add(matcher, It.IsAny<int>())).Once();
		}

		[Test]
		public async Task WithValueTypeNextToMatcher_WhenVerificationFails_ShouldDescribeTheArgument()
		{
			IValueTypeService sut = IValueTypeService.CreateMock();
			sut.Add(0, 1);

			string[] messages =
			[
				Capture(() => sut.Mock.Verify.Add(null, It.IsAny<int>()).Never()),
				Capture(() => sut.Mock.Verify[null, It.IsAny<int>()].Got().Once()),
			];

#if NET11_0_OR_GREATER
			const string expectedKey = "0";
#else
			const string expectedKey = "null";
#endif
			await That(messages).IsEqualTo([
				$"Expected that mock never invoked method Add({expectedKey}, It.IsAny<int>()), but it did once.",
				$"Expected that mock got indexer [{expectedKey}, It.IsAny<int>()] exactly once, but it never did.",
			]).Because("union mode converts the null literal to the default value before it renders the argument");
		}

		private static string Capture(Action act)
		{
			try
			{
				act();
				return "no exception";
			}
			catch (Exception exception)
			{
				return exception.Message;
			}
		}

		public interface INullLiteralService
		{
			int OneObject(object? value);
			int OneString(string? value);
			int OneObjectNotAnnotated(object value);
			int OneStringNotAnnotated(string value);
			int OneInterface(IComparable? value);
			int OneNullableInt(int? value);
			int TwoObject(object? value, int number);
			int TwoString(string? value, int number);
			int TwoNullableInt(int? value, int number);
			int OptionalObject(object? value, int number = 5);
			int OptionalString(string? value, int number = 5);
			int OptionalWithNonNullDefault(string? value = "x", int number = 5);
			int WithParams(object? value, params int[] rest);
		}

		public interface IValueTypeService
		{
			int this[int a, int b] { get; set; }
			int Add(int a, int b);
			int Five(int a, int b, int c, int d, int e);
			int Generic<T>(T a, int b);
		}

		public interface IGenericSiblingService
		{
			int M(object? value);
			int M<T>(T value);
		}
	}
}
