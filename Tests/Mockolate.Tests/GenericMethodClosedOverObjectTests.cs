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
	public async Task ParamsMatchers_ShouldSetUpTheMethod()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		List<object?> received = [];
		sut.Mock.Setup.Only<object>(It.IsAny<string>().Do(value => received.Add(value)));
		sut.Mock.Setup.Only<object>(1, It.IsAny<object>()).Do(() => received.Add("pair"));
		sut.Mock.Setup.Sum<object>(1, It.Satisfies<object[]>(values => values.Length == 2)).Returns(2);
		sut.Mock.Setup.Sum<object>(2, "a", It.Is<object>(3)).Returns(3);
		sut.Mock.Setup.Both<object>(It.IsAny<string>(), It.IsAny<object>()).Returns(4);
		sut.Mock.Setup.Six<object>(1, 2, 3, 4, It.IsAny<object>()).Returns(5);

		sut.Only<object>("a");
		sut.Only<object>(1);
		sut.Only<object>(1, "b");
		int[] results =
		[
			sut.Sum<object>(1, "a", "b"), sut.Sum<object>(1, "a"), sut.Sum<object>(2, "a", 3), sut.Sum<object>(2, "b", 3),
			sut.Both<object>("a", 1), sut.Both<object>(1, 1), sut.Six<object>(1, 2, 3, 4, "a"), sut.Six<object>(1, 2, 3, 4, "a", "b"),
		];

		await That(received).IsEqualTo(["a", "pair",])
			.Because("a matcher for an element of a params array closed over object is no value");
		await That(results).IsEqualTo([2, 0, 3, 0, 4, 0, 5, 0,]);
	}

	[Test]
	public async Task ParamsNullArray_ShouldMatchNullArray()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.Sum<object>(1, null).Returns(1);

		int[] results = [sut.Sum<object>(1, null!), sut.Sum<object>(1), sut.Sum<object>(1, (object)null!),];
		sut.Only<string>(null!);
		sut.Only<object>();
		sut.Only<object>((object)null!);

		await That(results).IsEqualTo([1, 0, 0,]);
		await That(sut.Mock.Verify.Only<string>(null)).Once()
			.Because("a null array means a null array for any type argument");
		await That(sut.Mock.Verify.Only<object>(null)).Never();
	}

	[Test]
	public async Task ParamsLeadingDefault_ShouldStandForTheValue()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.Sum<object>(default).Returns(1);
		sut.Mock.Setup.Fixed(default).Returns(2);
		sut.Mock.Setup.Valued(default).Returns(3);

		int[] results =
		[
			sut.Sum<object>(0), sut.Sum<object>(0, "a"), sut.Sum<object>(5), sut.Fixed(0), sut.Fixed(1),
			sut.Fixed<object>(0), sut.Valued(0), sut.Valued(1), sut.Valued<object>(0),
		];
		sut.Sum<object>(0, null!);

		await That(results).IsEqualTo([1, 0, 0, 2, 0, 0, 3, 0, 0,])
			.Because("a lone default stands for the value of the first parameter, not for any parameters");
		await That(sut.Mock.Verify.Sum<object>(default)).Once();
		await That(sut.Mock.Verify.Sum<object>(default, null)).Once();
		await That(sut.Mock.Verify.Fixed(default)).Once();
	}

	[Test]
	public async Task ParamsBesideAnOptionalSibling_ShouldBindLikeTheCallOnTheMock()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		int count = 0;
		sut.Mock.Setup.Optional<object>().Do(() => count++);

		sut.Optional<object>();
		sut.Optional<object>(1, 2);

		await That(count).IsEqualTo(1);
		await That(sut.Mock.Verify.Optional<object>()).Once();
		await That(sut.Mock.Verify.Optional<object>(It.IsAny<object>(), It.IsAny<object>())).Once();
	}

	[Test]
	public async Task ParamsWithoutArguments_ShouldKeepAnyParametersBesideSiblings()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.Pick().AnyParameters().Returns(1);
		sut.Mock.Setup.Bare<object>().AnyParameters().Returns(2);
		sut.Mock.Setup.Bare().Returns(3);

		int[] results = [sut.Pick("a"), sut.Pick<object>("a"), sut.Bare<object>("a"), sut.Bare(),];

		await That(results).IsEqualTo([1, 0, 2, 3,]);
		await That(sut.Mock.Verify.Pick()).Never();
		await That(sut.Mock.Verify.Pick().AnyParameters()).Once();
		await That(sut.Mock.Verify.Bare<object>().AnyParameters()).Once();
	}

	[Test]
	public async Task SeveralLoneParamsSiblings_ShouldBindLikeTheCallOnTheMock()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.Pair("a").Returns(1);
		sut.Mock.Setup.Pair(1).Returns(2);
		sut.Mock.Setup.Triple("a").Returns(3);
		sut.Mock.Setup.Typed<object>(1).Returns(4);

		int[] results = [sut.Pair("a"), sut.Pair(1), sut.Pair<object>("a"), sut.Triple("a"), sut.Typed<object>(1),];

		await That(results).IsEqualTo([1, 2, 0, 3, 4,]);
		await That(sut.Mock.Verify.Pair(It.IsAny<string>())).Once();
		await That(sut.Mock.Verify.Typed<object>(It.IsAny<int>())).Once();
	}

	[Test]
	public async Task LoneParamsWithoutArguments_ShouldBindLikeTheCallOnTheMock()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.Hollow().AnyParameters().Returns(1);
		sut.Mock.Setup.Defaulted<object>().Returns(2);
		sut.Mock.Setup.Twin<object>().AnyParameters().Returns(3);

		int[] results =
		[
			sut.Hollow("a"), sut.Hollow<object>(), sut.Defaulted<object>(), sut.Defaulted<object>(1),
			sut.Defaulted<object>("a"), sut.Twin<object>("a"),
		];

		await That(results).IsEqualTo([1, 0, 2, 0, 0, 3,])
			.Because("a call without arguments binds the sibling that the call on the mock binds");
	}

	[Test]
	public async Task NullableLoneParamsBesideAGenericSibling_ShouldTakeALoneNullAsTheArray()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();

		void Act()
			=> sut.Mock.Setup.Nulled(null);

		await That(Act).Throws<ArgumentNullException>()
			.Because("a null array of another element type than a type parameter is no value to compare with");
	}

	[Test]
	public async Task ParamsNullElements_ShouldMatchNullElements()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		int count = 0;
		sut.Mock.Setup.Maybe<object>(It.IsAny<object>(), null).Do(() => count++);

		sut.Maybe<object>("a", null);
		sut.Maybe<object>("a", "b");
		sut.Only<object?>(null, null);

		await That(count).IsEqualTo(1);
		await That(sut.Mock.Verify.Only<object?>(null, null)).Once();
		await That(sut.Mock.Verify.Only<object?>(null, It.IsAny<object?>())).Once();
	}

	[Test]
	public async Task ParamsMatchers_ShouldInferTheTypeArgument()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		List<string> received = [];
		sut.Mock.Setup.Only(It.IsAny<string>().Do(value => received.Add(value)));
		sut.Mock.Setup.Sum(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()).Returns(1);

		sut.Only("a");
		sut.Only(1);
		int[] results = [sut.Sum(1, "a", "b"), sut.Sum(1, 2, 3), sut.Sum<object>(1, "a", "b"),];

		await That(received).IsEqualTo(["a",])
			.Because("the type argument is inferred from the matcher, not as the matcher type");
		await That(results).IsEqualTo([1, 0, 0,]);
		await That(sut.Mock.Verify.Only(It.IsAny<string>())).Once();
		await That(sut.Mock.Verify.Only(It.IsAny<int>())).Once();
		await That(sut.Mock.Verify.Sum(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Once();
	}

	[Test]
	public async Task ParamsParametersMatch_ShouldSetUpTheMethod()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		int count = 0;
		sut.Mock.Setup.Only<object>(Match.AnyParameters()).Do(() => count++);
		sut.Mock.Setup.Sum<object>(1).AnyParameters().Returns(2);

		sut.Only<object>();
		sut.Only<object>(1, "a");
		sut.Only<string>("a");
		int result = sut.Sum<object>(5, "a");

		await That(count).IsEqualTo(2);
		await That(result).IsEqualTo(2);
		await That(sut.Mock.Verify.Only<object>().AnyParameters()).Twice();
	}

	[Test]
	public async Task ParamsMatchers_ShouldVerifyTheMethod()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();

		sut.Only<object>("a");
		sut.Only<object>(1, "b");
		sut.Sum<object>(1, "a", 2);
		sut.Both<object>("a", 1);
		sut.Six<object>(1, 2, 3, 4);

		await That(sut.Mock.Verify.Only<object>(It.IsAny<object>())).Once();
		await That(sut.Mock.Verify.Only<object>(It.IsAny<object[]>())).Twice()
			.Because("a lone matcher for the array matches the whole array");
		await That(sut.Mock.Verify.Only<object>(It.IsAny<object>(), "b")).Once();
		await That(sut.Mock.Verify.Only<object>(Match.AnyParameters())).Twice();
		await That(sut.Mock.Verify.Only<object>(Match.Parameters(p => p[0] is object[] { Length: 2, }))).Once();
		await That(sut.Mock.Verify.Sum<object>(1, It.IsAny<object[]>())).Once();
		await That(sut.Mock.Verify.Sum<object>(1, "a", It.Is<object>(2))).Once();
		await That(sut.Mock.Verify.Sum<object>(1, It.IsAny<object>(), It.IsAny<object>())).Once();
		await That(sut.Mock.Verify.Sum<object>(1, It.IsAny<object>())).Never();
		await That(sut.Mock.Verify.Both<object>(Match.AnyParameters())).Once();
		await That(sut.Mock.Verify.Both<object>("a", It.IsAny<object>())).Once();
		await That(sut.Mock.Verify.Six<object>(1, 2, 3, 4, It.IsAny<object[]>())).Once();
	}

	[Test]
	public async Task ParamsMatchers_WhenVerificationFails_ShouldDescribeTheMatchers()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();

		void Act()
			=> sut.Mock.Verify.Only<object>(1, It.IsAny<string>()).Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Only<T>([1, It.IsAny<string>()]) exactly once, but it never did.");
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

	[Test]
	public async Task ParamsValues_ShouldStillMatchAsValues()
	{
		IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
		sut.Mock.Setup.Sum<object>(1, "a", 2).Returns(1);
		sut.Mock.Setup.Sum<string>(It.Is(2), It.IsAny<string>()).Returns(2);

		int[] results = [sut.Sum<object>(1, "a", 2), sut.Sum<object>(1, "a", 3), sut.Sum<string>(2, "b"),];
		sut.Only<object>();
		sut.Only<string>("a", "b");

		await That(results).IsEqualTo([1, 0, 2,]);
		await That(sut.Mock.Verify.Only<object>()).Once();
		await That(sut.Mock.Verify.Only<string>("a", "b")).Once();
		await That(sut.Mock.Verify.Only<string>(It.Is("a"), It.IsAny<string>())).Once();
		await That(sut.Mock.Verify.Only<string>(Match.AnyParameters())).Once();
	}
}
