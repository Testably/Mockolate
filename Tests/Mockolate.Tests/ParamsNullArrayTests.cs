using Mockolate.Verify;

namespace Mockolate.Tests;

public sealed class ParamsNullArrayTests
{
	[Test]
	public async Task NullArray_ShouldMatchOnlyANullArray()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();
		sut.Mock.Setup.Strings(null!).Returns(1);
		sut.Mock.Setup.Integers(null!).Returns(2);
		sut.Mock.Setup.Leading(1, null!).Returns(3);
		sut.Mock.Setup.Five(1, 2, 3, 4, null!).Returns(4);
		sut.Mock.Setup.Objects((object[])null!).Returns(5);

		int[] results =
		[
			sut.Strings(null!), sut.Strings((string)null!), sut.Strings(), sut.Strings("a"),
			sut.Integers(null!), sut.Integers(0), sut.Integers(),
			sut.Leading(1, null!), sut.Leading(1, (string)null!), sut.Leading(1),
			sut.Five(1, 2, 3, 4, null!), sut.Five(1, 2, 3, 4, (string)null!), sut.Five(1, 2, 3, 4),
			sut.Objects(null!), sut.Objects((object)null!), sut.Objects(),
		];

		await That(results).IsEqualTo([1, 0, 0, 0, 2, 0, 0, 3, 0, 0, 4, 0, 0, 5, 0, 0,])
			.Because("a null argument for a params array passes a null array to the method");
	}

	[Test]
	public async Task NullArray_ShouldSupportVerify()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();
		_ = sut.Strings(null!);
		_ = sut.Strings((string)null!);
		_ = sut.Integers(null!);
		_ = sut.Integers(0);
		_ = sut.Leading(1, null!);
		_ = sut.Five(1, 2, 3, 4, null!);
		_ = sut.Objects(null!);

		await That(sut.Mock.Verify.Strings(null!)).Once();
		await That(sut.Mock.Verify.Integers(null!)).Once();
		await That(sut.Mock.Verify.Leading(1, null!)).Once();
		await That(sut.Mock.Verify.Five(1, 2, 3, 4, null!)).Once();
		await That(sut.Mock.Verify.Objects((object[])null!)).Once();
	}

	[Test]
	public async Task NullArray_OnDelegate_ShouldMatchOnlyANullArray()
	{
		StringsDelegate sut = StringsDelegate.CreateMock();
		sut.Mock.Setup(null!).Returns(1);

		int[] results = [sut(null!), sut((string)null!), sut(), sut("a"),];

		await That(results).IsEqualTo([1, 0, 0, 0,]);
		await That(sut.Mock.Verify(null!)).Once();
	}

	public interface IParamsMethods
	{
		int Strings(params string[] values);
		int Integers(params int[] values);
		int Leading(int a, params string[] values);
		int Five(int a, int b, int c, int d, params string[] values);
		int Objects(params object[] values);
	}

	public delegate int StringsDelegate(params string[] values);
}
