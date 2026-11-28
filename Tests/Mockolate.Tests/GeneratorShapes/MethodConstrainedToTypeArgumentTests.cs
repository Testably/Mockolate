using System.Collections.Generic;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests.GeneratorShapes;

public sealed class MethodConstrainedToTypeArgumentTests
{
	[Test]
	public async Task WhenClosedOverAValueType_ShouldInvokeSetupsAndBeVerifiable()
	{
		IMethodConstrainedToTypeArgument<int> sut = IMethodConstrainedToTypeArgument<int>.CreateMock();
		List<int> received = [];
		sut.Mock.Setup.Accept<int>(It.IsAny<int>()).Do(value => received.Add(value));
		sut.Mock.Setup.Get<int>(2).Returns(5);
		sut.Mock.Setup.GetNotNull<int>(It.IsAny<int>()).Returns(7);
		sut.Mock.Setup.AcceptDependent<int, int>(It.IsAny<int>(), It.Is(2)).Do((value, other) => received.Add(value + other));

		sut.Accept(3);
		int result1 = sut.Get<int>(2);
		int result2 = sut.Get<int>();
		int result3 = sut.GetNotNull(1);
		sut.AcceptDependent<int, int>(4, 2);

		await That(received).IsEqualTo([3, 6,]);
		await That(result1).IsEqualTo(5);
		await That(result2).IsEqualTo(0).Because("the default value of the key does not match the setup");
		await That(result3).IsEqualTo(7);
		await That(sut.Mock.Verify.Accept<int>(3)).Once();
		await That(sut.Mock.Verify.Accept<int>(4)).Never();
		await That(sut.Mock.Verify.Get<int>(It.IsAny<int>())).Twice();
		await That(sut.Mock.Verify.Get<int>(1)).Once();
		await That(sut.Mock.Verify.GetNotNull<int>(1)).Once();
		await That(sut.Mock.Verify.AcceptDependent<int, int>(4, It.IsAny<int>())).Once();
	}

	[Test]
	public async Task WhenClosedOverASealedClass_ShouldInvokeSetupsAndBeVerifiable()
	{
		IMethodConstrainedToTypeArgument<string> sut = IMethodConstrainedToTypeArgument<string>.CreateMock();
		List<string?> received = [];
		sut.Mock.Setup.Accept<string>(It.IsAny<string?>()).Do(value => received.Add(value));
		sut.Mock.Setup.Get<string>(2).Returns("a");
		sut.Mock.Setup.GetNotNull<string>(It.IsNull<string?>()).Returns("b");
		sut.Mock.Setup.AcceptDependent<string, string>(It.IsAny<string?>(), It.IsAny<string?>())
			.Do((value, other) => received.Add($"{value}-{other}"));

		sut.Accept("x");
		sut.Accept<string>(null);
		string result1 = sut.Get<string>(2);
		string? result2 = sut.GetNotNull<string>(null);
		string? result3 = sut.GetNotNull("y");
		sut.AcceptDependent<string, string>("c", null);

		await That(received).IsEqualTo(["x", null, "c-",]);
		await That(result1).IsEqualTo("a");
		await That(result2).IsEqualTo("b");
		await That(result3).IsEmpty().Because("an unmatched call falls back to the default value of string");
		await That(sut.Mock.Verify.Accept<string>(It.IsNull<string?>())).Once();
		await That(sut.Mock.Verify.Accept<string>(It.IsAny<string?>())).Twice();
		await That(sut.Mock.Verify.Get<string>(2)).Once();
		await That(sut.Mock.Verify.GetNotNull<string>("y")).Once();
		await That(sut.Mock.Verify.AcceptDependent<string, string>("c", It.IsNull<string?>())).Once();
	}

	[Test]
	public async Task WithClass_WhenClosedOverASealedClass_ShouldInvokeSetupsAndBeVerifiable()
	{
		MethodConstrainedToTypeArgumentBase<string> sut = MethodConstrainedToTypeArgumentBase<string>.CreateMock();
		List<string?> received = [];
		sut.Mock.Setup.Accept<string>(It.IsAny<string?>()).Do(value => received.Add(value));
		sut.Mock.Setup.Get<string>().Returns("a");

		sut.Accept("x");
		string? result = sut.Get<string>();

		await That(received).IsEqualTo(["x",]);
		await That(result).IsEqualTo("a");
		await That(sut.Mock.Verify.Accept<string>("x")).Once();
		await That(sut.Mock.Verify.Get<string>()).Once();
	}
}
