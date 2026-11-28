using System.Collections.Generic;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests.GeneratorShapes;

public sealed class MethodConstrainedToClassTypeArgumentTests
{
	[Test]
	public async Task WhenClosedOverAClass_ShouldInvokeSetupsAndBeVerifiable()
	{
		IMethodConstrainedToClassTypeArgument<Exception> sut = IMethodConstrainedToClassTypeArgument<Exception>.CreateMock();
		List<string?> received = [];
		sut.Mock.Setup.Accept<ArgumentException>(It.IsAny<ArgumentException?>())
			.Do(value => received.Add(value?.ParamName));
		sut.Mock.Setup.Get<InvalidOperationException>(2).Returns(new InvalidOperationException("a"));

		sut.Accept(new ArgumentException("x", "p"));
		sut.Accept<ArgumentException>(null);
		InvalidOperationException result = sut.Get<InvalidOperationException>(2);

		await That(received).IsEqualTo(["p", null,]);
		await That(result.Message).IsEqualTo("a");
		await That(sut.Mock.Verify.Accept<ArgumentException>(It.IsNull<ArgumentException?>())).Once();
		await That(sut.Mock.Verify.Accept<ArgumentException>(It.IsAny<ArgumentException?>())).Twice();
		await That(sut.Mock.Verify.Get<InvalidOperationException>(It.IsAny<int>())).Once();
	}

	[Test]
	public async Task WhenClosedOverAnInterface_ShouldInvokeSetupsAndBeVerifiable()
	{
		IMethodConstrainedToClassTypeArgument<IComparable> sut = IMethodConstrainedToClassTypeArgument<IComparable>.CreateMock();
		List<string?> received = [];
		sut.Mock.Setup.Accept<string>(It.IsAny<string?>()).Do(value => received.Add(value));
		sut.Mock.Setup.Create<Version>().Returns(new Version(1, 2));

		sut.Accept("x");
		Version? result = sut.Create<Version>();

		await That(received).IsEqualTo(["x",]);
		await That(result).IsEqualTo(new Version(1, 2));
		await That(sut.Mock.Verify.Accept<string>("x")).Once();
		await That(sut.Mock.Verify.Create<Version>()).Once();
	}

	[Test]
	public async Task WithClass_WhenClosedOverAClass_ShouldInvokeSetupsAndBeVerifiable()
	{
		MethodConstrainedToClassTypeArgumentBase<Exception> sut = MethodConstrainedToClassTypeArgumentBase<Exception>.CreateMock();
		List<string?> received = [];
		sut.Mock.Setup.Accept<ArgumentException>(It.IsAny<ArgumentException?>())
			.Do(value => received.Add(value?.ParamName));

		sut.Accept(new ArgumentException("x", "p"));

		await That(received).IsEqualTo(["p",]);
		await That(sut.Mock.Verify.Accept<ArgumentException>(It.IsAny<ArgumentException?>())).Once();
	}
}
