using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests.GeneratorShapes;

public sealed class GenericMethodInferredTypeArgumentTests
{
	[Test]
	public async Task WithMatcher_ShouldSetUpTheMethodForTheTypeArgumentOfTheMatcher()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		IOptionalMethodParameters optional = IOptionalMethodParameters.CreateMock();
		List<string> received = [];
		sut.Mock.Setup.Plain(It.IsAny<int>()).Do(() => received.Add("plain"));
		sut.Mock.Setup.Log(It.IsAny<string>()).Do(() => received.Add("log"));
		sut.Mock.Setup.Get(It.Is(1)).Returns(1);
		sut.Mock.Setup.Two(1, It.IsAny<string>()).Returns(2);
		sut.Mock.Setup.Five(1, 2, 3, 4, It.IsAny<string>()).Returns(5);
		optional.Mock.Setup.Generic(It.IsAny<string>()).Returns(6);

		sut.Plain(1);
		sut.Plain("a");
		sut.Log("a");
		int[] results =
		[
			sut.Get(1), sut.Two(1, "a"), sut.Two(1, 2), sut.Five(1, 2, 3, 4, "a"), optional.Generic("a"),
		];

		await That(received).IsEqualTo(["plain", "log",]);
		await That(results).IsEqualTo([1, 2, 0, 5, 6,])
			.Because("the type argument is inferred from the matcher, as if it were stated explicitly");
	}

	[Test]
	public async Task WithMatcher_ShouldVerifyTheMethodForTheTypeArgumentOfTheMatcher()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		IOptionalMethodParameters optional = IOptionalMethodParameters.CreateMock();

		sut.Plain(1);
		sut.Plain("a");
		sut.Log("a");
		sut.Get(1);
		sut.Two(1, "a");
		sut.Five(1, 2, 3, 4, "a");
		optional.Generic("a");

		await That(sut.Mock.Verify.Plain(It.IsAny<int>())).Once();
		await That(sut.Mock.Verify.Log(It.IsAny<string>())).Once();
		await That(sut.Mock.Verify.Get(It.Is(1))).Once();
		await That(sut.Mock.Verify.Two(1, It.IsAny<string>())).Once();
		await That(sut.Mock.Verify.Five(1, 2, 3, 4, It.IsAny<string>())).Once();
		await That(optional.Mock.Verify.Generic(It.IsAny<string>())).Once();
	}

	[Test]
	public async Task WithMatcherAndLeftOutOptionalParameter_ShouldSetUpAndVerifyTheMethodForTheTypeArgumentOfTheMatcher()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.Trailing(It.IsAny<string>()).Returns(1);
		sut.Mock.Setup.KeptAboveFour(It.IsAny<string>(), 1, 2, 3).Returns(2);

		int[] results =
		[
			sut.Trailing("a"), sut.Trailing("a", "b"), sut.Trailing(1), sut.KeptAboveFour("a", 1, 2, 3),
			sut.KeptAboveFour("a", 1, 2, 3, 5), sut.KeptAboveFour(1, 1, 2, 3),
		];

		await That(results).IsEqualTo([1, 0, 0, 2, 0, 0,]);
		await That(sut.Mock.Verify.Trailing(It.IsAny<string>())).Once();
		await That(sut.Mock.Verify.KeptAboveFour(It.IsAny<string>(), 1, 2, 3)).Once();
	}

	[Test]
	public async Task WithMatcherNextToValueOfAnotherTypeParameterAndMoreThanFourParameters_ShouldSetUpAndVerifyTheMethod()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.TwoAboveFour(It.IsAny<int>(), "x", 1, 2, "y").Returns(1);
		sut.Mock.Setup.TwoAboveFour("a", It.IsAny<string>(), 1, 2).Returns(2);

		int[] results =
		[
			sut.TwoAboveFour(5, "x", 1, 2, "y"), sut.TwoAboveFour(5, "z", 1, 2, "y"),
			sut.TwoAboveFour("a", "b", 1, 2), sut.TwoAboveFour("a", "b", 1, 2, "c"),
		];

		await That(results).IsEqualTo([1, 0, 2, 0,])
			.Because("each type argument is inferred from its own matcher or value");
		await That(sut.Mock.Verify.TwoAboveFour(It.IsAny<int>(), "x", 1, 2, "y")).Once();
		await That(sut.Mock.Verify.TwoAboveFour("a", It.IsAny<string>(), 1, 2)).Once();
	}

	[Test]
	public async Task WithLeftOutArgumentOfATypeParameterAndMoreThanFourParameters_ShouldSetUpAndVerifyTheMethod()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.LeftOutAboveFour(1, 2, 3, 4, It.IsAny<string>()).Returns(1);
		sut.Mock.Setup.LeftOutAboveFour<int>(1, 2, 3, 4).Returns(2);

		int[] results =
		[
			sut.LeftOutAboveFour(1, 2, 3, 4, "a"), sut.LeftOutAboveFour(1, 2, 3, 4, 5),
			sut.LeftOutAboveFour<int>(1, 2, 3, 4),
		];

		await That(results).IsEqualTo([1, 0, 2,])
			.Because("the left-out argument stands for the declared default value of the type argument");
		await That(sut.Mock.Verify.LeftOutAboveFour(1, 2, 3, 4, It.IsAny<string>())).Once();
		await That(sut.Mock.Verify.LeftOutAboveFour<int>(1, 2, 3, 4)).Once();
	}

	[Test]
	public async Task WithFiveParametersOfTheTypeParameterAndLeftOutArgument_ShouldSetUpAndVerifyTheMethod()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.FiveAboveFour(1, 2, 3, 4).Returns(1);
		sut.Mock.Setup.FiveAboveFour(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.Is(9))
			.Returns(2);

		int[] results = [sut.FiveAboveFour(1, 2, 3, 4), sut.FiveAboveFour(5, 6, 7, 8, 9), sut.FiveAboveFour(1, 2, 3, 4, 5),];

		await That(results).IsEqualTo([1, 2, 0,]);
		await That(sut.Mock.Verify.FiveAboveFour(1, 2, 3, 4)).Once();
	}

	[Test]
	public async Task WithMatcherNextToMatcherForParamsArray_ShouldSetUpAndVerifyTheMethod()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.WithRest(It.IsAny<string>(), It.IsAny<object[]>()).Returns(1);

		int[] results = [sut.WithRest("a", 1, 2), sut.WithRest(1, 2),];

		await That(results).IsEqualTo([1, 0,])
			.Because("the type argument is inferred from the matcher, not as the matcher type");
		await That(sut.Mock.Verify.WithRest(It.IsAny<string>(), It.IsAny<object[]>())).Once();
	}

	[Test]
	public async Task WithValueAndNonGenericSibling_ShouldSetUpAndVerifyTheMethodThatTheCallBinds()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.Sibling("a").Returns(1);
		sut.Mock.Setup.Sibling((IComparable)"b").Returns(2);

		int[] results = [sut.Sibling("a"), sut.Sibling((IComparable)"a"), sut.Sibling((IComparable)"b"), sut.Sibling("b"),];

		await That(results).IsEqualTo([1, 0, 2, 0,])
			.Because("a value binds the same overload in the setup as in the call on the mock");
		await That(sut.Mock.Verify.Sibling("a")).Once();
		await That(sut.Mock.Verify.Sibling((IComparable)"b")).Once();
	}

	[Test]
	public async Task WithNullLiteralAndGenericSibling_ShouldMatchOnlyNull()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.Sibling(null!).Returns(1);

		int[] results = [sut.Sibling(null!), sut.Sibling((IComparable)"a"),];

		await That(results).IsEqualTo([1, 0,])
			.Because("a null literal stands for a null argument, not for any parameters");
		await That(sut.Mock.Verify.Sibling(null!)).Once();
	}

	[Test]
	public async Task BeginScope_WithMatcher_ShouldSetUpAndVerifyTheScope()
	{
		ILogger logger = ILogger.CreateMock();
		IDisposable scope = IDisposable.CreateMock();
		logger.Mock.Setup.BeginScope(It.IsAny<int>()).Returns(scope);

		IDisposable? result = logger.BeginScope(42);

		await That(result).IsSameAs(scope);
		await That(logger.Mock.Verify.BeginScope(It.IsAny<int>())).Once();
	}

	[Test]
	public async Task WithNullOrDefaultAndExplicitTypeArgument_ShouldMatchTheNullOrDefaultArgument()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		IOptionalMethodParameters optional = IOptionalMethodParameters.CreateMock();
		sut.Mock.Setup.Get<string>(null!).Returns(1);
		sut.Mock.Setup.Get<int>(default).Returns(2);
		sut.Mock.Setup.Count<string>(null!).Returns(3);
		sut.Mock.Setup.Two<string>(1, null!).Returns(4);
		optional.Mock.Setup.Generic<string>(null!).Returns(5);
		sut.Mock.Setup.Trailing<string>(null!).Returns(6);

		int[] results =
		[
			sut.Get<string>(null!), sut.Get("a"), sut.Get(0), sut.Get(1), sut.Count<string>(null!), sut.Count("a"),
			sut.Two<string>(1, null!), sut.Two(1, "a"), optional.Generic<string>(null!), optional.Generic("a"),
			sut.Trailing<string>(null!), sut.Trailing("a"), sut.Trailing<string>(null!, "b"),
		];

		await That(results).IsEqualTo([1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 6, 0, 0,])
			.Because("a null or default literal stands for that argument, not for any parameters");
		await That(sut.Mock.Verify.Get<string>(null!)).Once();
		await That(sut.Mock.Verify.Get<int>(default)).Once();
		await That(sut.Mock.Verify.Count<string>(null!)).Once();
		await That(sut.Mock.Verify.Two<string>(1, null!)).Once();
		await That(optional.Mock.Verify.Generic<string>(null!)).Once();
		await That(sut.Mock.Verify.Trailing<string>(null!)).Once();
	}
}
