using System.Collections.Generic;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests;

public sealed class ParamsObjectArrayTests
{
	[Test]
	public async Task ArrayMatcher_ShouldMatchTheWholeArray()
	{
		IParamsObjectArray sut = IParamsObjectArray.CreateMock();
		sut.Mock.Setup.Log(1, It.IsAny<object[]>()).Returns(1);
		sut.Mock.Setup.Only(It.Satisfies<object[]>(args => args.Length == 2)).Returns(2);

		int[] results = [sut.Log(1, "a", 2), sut.Log(1), sut.Log(2, "a"), sut.Only("a", 2), sut.Only("a"),];

		await That(results).IsEqualTo([1, 1, 0, 2, 0,])
			.Because("a matcher for the array type must not be captured as one element of the params array");
		await That(sut.Mock.Verify.Log(1, It.IsAny<object[]>())).Twice();
		await That(sut.Mock.Verify.Only(It.IsAny<object[]>())).Twice();
	}

	[Test]
	public async Task DefaultLiteral_ShouldMeanTheValue()
	{
		IParamsObjectArray sut = IParamsObjectArray.CreateMock();
		sut.Mock.Setup.Log(default, "a").Returns(1);
		sut.Mock.Setup.Log(default).Returns(2);

		int[] results = [sut.Log(0, "a"), sut.Log(0), sut.Log(1, "a"), sut.Log(1),];

		await That(results).IsEqualTo([1, 2, 0, 0,])
			.Because("a default literal next to a params object[] binds the overload that takes values");
		await That(sut.Mock.Verify.Log(default, "a")).Once();
		await That(sut.Mock.Verify.Log(default)).Once();
		await That(sut.Mock.Verify.Log(default).AnyParameters()).Exactly(4);
	}

	[Test]
	public async Task ElementMatchers_ShouldMatchElementByElement()
	{
		IParamsObjectArray sut = IParamsObjectArray.CreateMock();
		sut.Mock.Setup.Only(It.IsAny<object>()).Returns(1);
		sut.Mock.Setup.Log(It.IsAny<int>(), It.Is<object>("a"), It.IsAny<object>()).Returns(2);

		int[] results = [sut.Only("a"), sut.Only(), sut.Only("a", 2), sut.Log(1, "a", 2), sut.Log(1, "b", 2),];

		await That(results).IsEqualTo([1, 0, 0, 2, 0,]);
		await That(sut.Mock.Verify.Only(It.IsAny<object>())).Once();
		await That(sut.Mock.Verify.Log(It.IsAny<int>(), It.IsAny<object>(), It.IsAny<object>())).Twice();
	}

	[Test]
	public async Task NoArguments_ShouldMatchAnEmptyArrayAndKeepAnyParameters()
	{
		IParamsObjectArray sut = IParamsObjectArray.CreateMock();
		IParamsObjectArray any = IParamsObjectArray.CreateMock();
		List<string> written = [];
		sut.Mock.Setup.Only().Returns(1);
		any.Mock.Setup.Only().AnyParameters().Returns(2);
		any.Mock.Setup.Write().AnyParameters().Do(() => written.Add("array"));

		int[] results = [sut.Only(), sut.Only("a"), any.Only(), any.Only("a", 2),];
		any.Write();
		any.Write("a", "b");
		any.Write("a");

		await That(results).IsEqualTo([1, 0, 2, 2,]);
		await That(written).HasCount(2)
			.Because("a single argument calls Write(object), which the setup of Write(params object[]) does not cover");
		await That(sut.Mock.Verify.Only()).Once();
		await That(sut.Mock.Verify.Only().AnyParameters()).Twice();
		await That(any.Mock.Verify.Write()).Once();
		await That(any.Mock.Verify.Write().AnyParameters()).Twice();
	}

	[Test]
	public async Task NullLiterals_ShouldMeanANullArrayOrNullElements()
	{
		IParamsObjectArray sut = IParamsObjectArray.CreateMock();
		sut.Mock.Setup.Only(null).Returns(1);
		sut.Mock.Setup.Only(null, null).Returns(2);
		sut.Mock.Setup.Log(1, null).Returns(3);

		int[] results =
		[
			sut.Only(null!), sut.Only(null!, null!), sut.Only((object)null!), sut.Only(), sut.Only("a", null!),
			sut.Log(1, null!), sut.Log(1), sut.Log(1, (object)null!),
		];

		await That(results).IsEqualTo([1, 2, 0, 0, 0, 3, 0, 0,]);
		await That(sut.Mock.Verify.Only(null)).Once();
		await That(sut.Mock.Verify.Only(null, null)).Once();
		await That(sut.Mock.Verify.Log(1, null)).Once();
	}

	[Test]
	public async Task ObjectOverload_ShouldBeSetUpLikeItIsCalled()
	{
		IParamsObjectArray sut = IParamsObjectArray.CreateMock();
		List<string> written = [];
		sut.Mock.Setup.Write("a").Do(() => written.Add("object"));
		sut.Mock.Setup.Write(It.IsAny<object[]>()).Do(() => written.Add("array"));

		sut.Write("a");
		sut.Write("a", "b");

		await That(written).IsEqualTo(["object", "array",])
			.Because("Write(\"a\") calls Write(object), so its setup must bind the overloads of that method");
		await That(sut.Mock.Verify.Write("a")).Once();
		await That(sut.Mock.Verify.Write(It.IsAny<object>())).Once();
		await That(sut.Mock.Verify.Write(It.IsAny<object[]>())).Once();
	}

	[Test]
	public async Task ParametersMatch_ShouldMatchAnyCall()
	{
		IParamsObjectArray sut = IParamsObjectArray.CreateMock();
		sut.Mock.Setup.Only(Match.AnyParameters()).Returns(1);

		int[] results = [sut.Only(), sut.Only("a", 2), sut.Only(null!),];

		await That(results).IsEqualTo([1, 1, 1,]);
		await That(sut.Mock.Verify.Only(Match.AnyParameters())).Exactly(3);
	}
}
