using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests;

public sealed class GenericMethodInferredTypeArgumentTests
{
	[Test]
	public async Task Matcher_ShouldSetUpTheMethodForTheTypeArgumentOfTheMatcher()
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
	public async Task Matcher_ShouldVerifyTheMethodForTheTypeArgumentOfTheMatcher()
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
	public async Task Matcher_WithLeftOutOptionalParameter_ShouldSetUpAndVerifyTheMethodForTheTypeArgumentOfTheMatcher()
	{
		IGenericMethodInferredFromMatcher sut = IGenericMethodInferredFromMatcher.CreateMock();
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
	public async Task Matcher_ForLoggerScope_ShouldSetUpAndVerifyTheScope()
	{
		ILogger logger = ILogger.CreateMock();
		IDisposable scope = IDisposable.CreateMock();
		logger.Mock.Setup.BeginScope(It.IsAny<int>()).Returns(scope);

		IDisposable? result = logger.BeginScope(42);

		await That(result).IsSameAs(scope);
		await That(logger.Mock.Verify.BeginScope(It.IsAny<int>())).Once();
	}

	[Test]
	public async Task NullOrDefault_WithExplicitTypeArgument_ShouldMatchTheNullOrDefaultArgument()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		IOptionalMethodParameters optional = IOptionalMethodParameters.CreateMock();
		IGenericMethodInferredFromMatcher trailing = IGenericMethodInferredFromMatcher.CreateMock();
		sut.Mock.Setup.Get<string>(null!).Returns(1);
		sut.Mock.Setup.Get<int>(default).Returns(2);
		sut.Mock.Setup.Count<string>(null!).Returns(3);
		sut.Mock.Setup.Two<string>(1, null!).Returns(4);
		optional.Mock.Setup.Generic<string>(null!).Returns(5);
		trailing.Mock.Setup.Trailing<string>(null!).Returns(6);

		int[] results =
		[
			sut.Get<string>(null!), sut.Get("a"), sut.Get(0), sut.Get(1), sut.Count<string>(null!), sut.Count("a"),
			sut.Two<string>(1, null!), sut.Two(1, "a"), optional.Generic<string>(null!), optional.Generic("a"),
			trailing.Trailing<string>(null!), trailing.Trailing("a"), trailing.Trailing<string>(null!, "b"),
		];

		await That(results).IsEqualTo([1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 6, 0, 0,])
			.Because("a null or default literal stands for that argument, not for any parameters");
		await That(sut.Mock.Verify.Get<string>(null!)).Once();
		await That(sut.Mock.Verify.Get<int>(default)).Once();
		await That(sut.Mock.Verify.Count<string>(null!)).Once();
		await That(sut.Mock.Verify.Two<string>(1, null!)).Once();
		await That(optional.Mock.Verify.Generic<string>(null!)).Once();
		await That(trailing.Mock.Verify.Trailing<string>(null!)).Once();
	}
}
