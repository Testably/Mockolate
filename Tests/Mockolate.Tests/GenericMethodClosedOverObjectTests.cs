using System.Collections.Generic;
using Mockolate.Exceptions;
using Mockolate.Tests.GeneratorCoverage;
using Mockolate.Verify;

namespace Mockolate.Tests;

public sealed class GenericMethodClosedOverObjectTests
{
	[Test]
	public async Task Matcher_ShouldSetUpTheMethod()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		List<object?> received = [];
		sut.Mock.Setup.Plain<object>(It.IsAny<string>()).Do(value => received.Add(value));
		sut.Mock.Setup.Two<object>(1, It.Satisfies<object>(x => x is int)).Returns(2);
		sut.Mock.Setup.Five<object>(1, 2, 3, 4, It.Is<object>("a")).Returns(5);

		sut.Plain<object>("a");
		sut.Plain<object>(1);
		sut.Plain<string>("b");
		int[] results = [sut.Two<object>(1, 2), sut.Two<object>(1, "a"), sut.Five<object>(1, 2, 3, 4, "a"), sut.Five<object>(1, 2, 3, 4, "b"),];

		await That(received).IsEqualTo(["a",])
			.Because("a matcher for a type parameter closed over object is no value, and Plain<string> is another closed method");
		await That(results).IsEqualTo([2, 0, 5, 0,]);
	}

	[Test]
	public async Task Matcher_ShouldVerifyTheMethod()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();

		sut.Plain<object>("x");
		sut.Plain<object>(1);
		sut.Two<object>(1, "x");
		sut.Dependent<string, object>("b", 5);
		sut.Five<object>(1, 2, 3, 4, "x");

		await That(sut.Mock.Verify.Plain<object>(It.IsAny<object>())).Twice();
		await That(sut.Mock.Verify.Plain<object>(It.IsAny<string>())).Once();
		await That(sut.Mock.Verify.Plain<object>(It.Is<object>(1))).Once();
		await That(sut.Mock.Verify.Plain<object>(It.Satisfies<object>(x => x is int))).Once();
		await That(sut.Mock.Verify.Plain<object>(Match.AnyParameters())).Twice();
		await That(sut.Mock.Verify.Two<object>(1, It.IsAny<object>())).Once();
		await That(sut.Mock.Verify.Dependent<string, object>("b", It.IsAny<object?>())).Once();
		await That(sut.Mock.Verify.Five<object>(1, 2, 3, 4, It.IsAny<object>())).Once();
	}

	[Test]
	public async Task Matcher_WhenVerificationFails_ShouldDescribeTheMatcher()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Plain<object>(1);

		void Act()
			=> sut.Mock.Verify.Plain<object>(It.IsAny<string>()).Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Plain<T>(It.IsAny<string>()) exactly once, but it never did.");
	}

	[Test]
	public async Task ParametersMatch_WhenVerificationFails_ShouldDescribeTheParametersMatch()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();

		void Act()
			=> sut.Mock.Verify.Plain<object>(Match.AnyParameters()).Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Plain<T>(Match.AnyParameters()) exactly once, but it never did.");
	}

	[Test]
	public async Task ParametersMatch_ShouldSetUpTheMethod()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		List<string> received = [];
		sut.Mock.Setup.Plain<object>(Match.Parameters(p => p[0] is "a")).Do(() => received.Add("plain"));
		sut.Mock.Setup.Get<object>(Match.Parameters(p => p[0] is "a")).Returns(1);
		sut.Mock.Setup.Log<object>(Match.Parameters(p => p[1] is int[] { Length: 1, })).Do(() => received.Add("log"));
		sut.Mock.Setup.Count<object>(Match.AnyParameters()).Returns(2);

		sut.Plain<object>("a");
		sut.Plain<object>("b");
		sut.Log<object>("a", 1);
		sut.Log<object>("a");
		int[] results = [sut.Get<object>("a"), sut.Get<object>("b"), sut.Count<object>("a"), sut.Count<object>("b", 1, 2),];

		await That(received).IsEqualTo(["plain", "log",]);
		await That(results).IsEqualTo([1, 0, 2, 2,])
			.Because("a lone parameters match stands for all parameters, also for the elements of a params array");
	}

	[Test]
	public async Task ParametersMatch_ShouldVerifyTheMethod()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();

		sut.Log<object>("a");
		sut.Log<object>("a", 1, 2);
		sut.Count<object>(1);

		await That(sut.Mock.Verify.Log<object>(Match.AnyParameters())).Twice();
		await That(sut.Mock.Verify.Log<object>(Match.Parameters(p => p[1] is int[] { Length: 2, }))).Once();
#pragma warning disable Mockolate0007
		await That(sut.Mock.Verify.Log<object>(Match.AnyParameters(), 1)).Never()
			.Because("a parameters match next to elements of the params array is a value, as for an object parameter");
#pragma warning restore Mockolate0007
		await That(sut.Mock.Verify.Count<object>(Match.AnyParameters())).Once();
		await That(sut.Mock.Verify.Count<string>(Match.AnyParameters())).Never();
	}

	[Test]
	public async Task ParametersMatchWithParamsArray_WhenVerificationFails_ShouldDescribeTheParametersMatch()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();

		void Act()
			=> sut.Mock.Verify.Log<object>(Match.AnyParameters()).Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Log<T>(Match.AnyParameters()) exactly once, but it never did.");
	}

	[Test]
	public async Task Value_ShouldStillMatchAsValue()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		object value = new List<int>();
		List<string> received = [];
		sut.Mock.Setup.Plain<object>(value).Do(() => received.Add("value"));
		sut.Mock.Setup.Two<object>(default, default!).Returns(1);

		sut.Plain<object>(value);
		sut.Plain<object>(new List<int>());
		int[] results = [sut.Two<object>(0, null!), sut.Two<object>(1, "b"),];

		await That(received).IsEqualTo(["value",])
			.Because("a value that is no matcher is compared by equality");
		await That(results).IsEqualTo([1, 0,]);
		await That(sut.Mock.Verify.Plain<object>(value)).Once();
		await That(sut.Mock.Verify.Two<object>(default, default!)).Once();
		await That(sut.Mock.Verify.Two<object>(1, "b")).Once();
		await That(sut.Mock.Verify.Two<object>(1, "a").AnyParameters()).Twice();
	}

	[Test]
	public async Task OtherTypeArguments_ShouldMatchAsBefore()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.Two<string>(1, It.IsAny<string>()).Returns(1);
		sut.Mock.Setup.Two<int>(default, default).Returns(2);

		int[] results = [sut.Two<string>(1, "a"), sut.Two<int>(0, 0), sut.Two<int>(0, 1),];
		sut.Struct(1);

		await That(results).IsEqualTo([1, 2, 0,]);
		await That(sut.Mock.Verify.Two<string>(1, null!)).Never();
		await That(sut.Mock.Verify.Two<int>(It.IsAny<int>(), 0)).Once();
		await That(sut.Mock.Verify.Struct<int>(1)).Once();
		await That(sut.Mock.Verify.Struct<int>(It.IsAny<int>())).Once();
	}
}
