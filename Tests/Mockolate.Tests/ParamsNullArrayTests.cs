using Mockolate.Verify;

namespace Mockolate.Tests;

public sealed class ParamsNullArrayTests
{
	[Test]
	public async Task NullArray_OfReferenceTypeElements_ShouldMatchOnlyANullArray()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();
		sut.Mock.Setup.Strings(null!).Returns(1);

		await ThatAll(
			That(sut.Strings(null!)).IsEqualTo(1),
			That(sut.Strings((string)null!)).IsEqualTo(0),
			That(sut.Strings()).IsEqualTo(0),
			That(sut.Strings("a")).IsEqualTo(0));
	}

	[Test]
	public async Task NullArray_OfValueTypeElements_ShouldMatchOnlyANullArray()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();
		sut.Mock.Setup.Integers(null!).Returns(1);

		await ThatAll(
			That(sut.Integers(null!)).IsEqualTo(1),
			That(sut.Integers(0)).IsEqualTo(0),
			That(sut.Integers()).IsEqualTo(0));
	}

	[Test]
	public async Task NullArray_AfterAnotherParameter_ShouldMatchOnlyANullArray()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();
		sut.Mock.Setup.Leading(1, null!).Returns(1);

		await ThatAll(
			That(sut.Leading(1, null!)).IsEqualTo(1),
			That(sut.Leading(1, (string)null!)).IsEqualTo(0),
			That(sut.Leading(1)).IsEqualTo(0));
	}

	[Test]
	public async Task NullArray_AboveFourParameters_ShouldMatchOnlyANullArray()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();
		sut.Mock.Setup.Five(1, 2, 3, 4, null!).Returns(1);

		await ThatAll(
			That(sut.Five(1, 2, 3, 4, null!)).IsEqualTo(1),
			That(sut.Five(1, 2, 3, 4, (string)null!)).IsEqualTo(0),
			That(sut.Five(1, 2, 3, 4)).IsEqualTo(0));
	}

	[Test]
	public async Task NullArray_TypedAsObjectArray_ShouldMatchOnlyANullArray()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();
		sut.Mock.Setup.Objects((object[])null!).Returns(1);

		await ThatAll(
			That(sut.Objects(null!)).IsEqualTo(1),
			That(sut.Objects((object)null!)).IsEqualTo(0),
			That(sut.Objects()).IsEqualTo(0));
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
		_ = sut.Leading(1, (string)null!);
		_ = sut.Leading(1);
		_ = sut.Five(1, 2, 3, 4, null!);
		_ = sut.Five(1, 2, 3, 4, (string)null!);
		_ = sut.Five(1, 2, 3, 4);
		_ = sut.Objects(null!);
		_ = sut.Objects((object)null!);
		_ = sut.Objects();

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

		await ThatAll(
			That(sut(null!)).IsEqualTo(1),
			That(sut((string)null!)).IsEqualTo(0),
			That(sut()).IsEqualTo(0),
			That(sut("a")).IsEqualTo(0));
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
