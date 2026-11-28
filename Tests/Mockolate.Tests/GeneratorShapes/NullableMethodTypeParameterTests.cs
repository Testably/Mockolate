using System.Collections.Generic;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests.GeneratorShapes;

public sealed class NullableMethodTypeParameterTests
{
	[Test]
	public async Task Method_WithConstrainedTypeParameter_ShouldInvokeSetups()
	{
		INullableMethodTypeParameter sut = INullableMethodTypeParameter.CreateMock();
		List<object?> received = [];
		sut.Mock.Setup.AcceptClass<string>(It.IsAny<string?>()).Do(value => received.Add(value));
		sut.Mock.Setup.AcceptNotNull<int>(It.IsAny<int>()).Do(value => received.Add(value));
		sut.Mock.Setup.AcceptStruct<int>(It.IsAny<int?>()).Do(value => received.Add(value));
		sut.Mock.Setup.AcceptDependent<string, IComparable>(It.IsAny<string?>(), It.IsAny<IComparable?>())
			.Do((value, other) => received.Add($"{value}-{other}"));
		sut.Mock.Setup.AcceptMany<int>(It.IsAny<int>(), It.Is(2), It.IsAny<int>(), It.Is(4), It.IsAny<int>())
			.Do((a, _, c, _, e) => received.Add(a + c + e));

		sut.AcceptClass("a");
		sut.AcceptClass<string>(null);
		sut.AcceptNotNull(1);
		sut.AcceptStruct<int>(null);
		sut.AcceptDependent<string, IComparable>("b", 2);
		sut.AcceptMany(1, 2, 3, 4, 5);

		await That(received).IsEqualTo(["a", null, 1, null, "b-2", 9,]);
	}

	[Test]
	public async Task Method_WithConstrainedTypeParameter_ShouldReturnSetupValues()
	{
		INullableMethodTypeParameter sut = INullableMethodTypeParameter.CreateMock();
		sut.Mock.Setup.GetClass<string>("a").Returns("b");
		sut.Mock.Setup.GetNotNull<int>(1).Returns(2);
		sut.Mock.Setup.GetNotNull<string>(It.IsAny<string?>()).Returns("c");
		sut.Mock.Setup.GetAsync<string>(It.IsAny<List<string?>>()).ReturnsAsync("d");
		sut.Mock.Setup.TryGet<string>("key", It.IsOut<string?>(() => "out"), It.IsAnyRef<string?>(), It.IsAny<string?[]>())
			.Returns(true);

		string? other = "other";
		string? result1 = sut.GetClass("a");
		string? result2 = sut.GetClass("x");
		int result3 = sut.GetNotNull(1);
		string? result4 = sut.GetNotNull<string>(null);
		string? result5 = await sut.GetAsync<string>([]);
		bool result6 = sut.TryGet("key", out string? value, ref other, "r1", null);

		await That(result1).IsEqualTo("b");
		await That(result2).IsEmpty().Because("an unmatched call falls back to the default value of string");
		await That(result3).IsEqualTo(2);
		await That(result4).IsEqualTo("c");
		await That(result5).IsEqualTo("d");
		await That(result6).IsTrue();
		await That(value).IsEqualTo("out");
	}

	[Test]
	public async Task Method_WithConstrainedTypeParameter_ShouldBeVerifiable()
	{
		INullableMethodTypeParameter sut = INullableMethodTypeParameter.CreateMock();

		sut.AcceptClass("a");
		sut.AcceptClass<string>(null);
		sut.AcceptNullableClass<string?>(null);
		sut.AcceptNotNull(1);
		sut.AcceptInterface(2);
		sut.AcceptBaseClass(new InvalidOperationException("foo"));
		sut.AcceptConstructor(3);
		sut.AcceptStruct<int>(4);
		sut.AcceptDependent<string, IComparable>("b", 5);
		sut.AcceptMany(1, 2, 3, 4, 5);
		sut.GetClass("c");
		sut.GetNotNull(6);

		await That(sut.Mock.Verify.AcceptClass<string>("a")).Once();
		await That(sut.Mock.Verify.AcceptClass<string>(It.IsNull<string?>())).Once();
		await That(sut.Mock.Verify.AcceptClass<string>(It.IsAny<string?>())).Twice();
		await That(sut.Mock.Verify.AcceptClass<Exception>(It.IsAny<Exception?>())).Never();
		await That(sut.Mock.Verify.AcceptNullableClass<string?>(It.IsAny<string?>())).Once();
		await That(sut.Mock.Verify.AcceptNotNull<int>(1)).Once();
		await That(sut.Mock.Verify.AcceptInterface<int>(It.IsAny<int>())).Once();
		await That(sut.Mock.Verify.AcceptBaseClass<InvalidOperationException>(It.IsAny<InvalidOperationException?>())).Once();
		await That(sut.Mock.Verify.AcceptConstructor<int>(3)).Once();
		await That(sut.Mock.Verify.AcceptStruct<int>(4)).Once();
		await That(sut.Mock.Verify.AcceptDependent<string, IComparable>("b", It.IsAny<IComparable?>())).Once();
		await That(sut.Mock.Verify.AcceptMany<int>(It.Is(1), It.IsAny<int>(), It.Is(3), It.IsAny<int>(), It.Is(5))).Once();
		await That(sut.Mock.Verify.AcceptMany<int>(2, 2, 3, 4, 5)).Never();
		await That(sut.Mock.Verify.GetClass<string>("c")).Once();
		await That(sut.Mock.Verify.GetNotNull<int>(6)).Once();
	}
}
