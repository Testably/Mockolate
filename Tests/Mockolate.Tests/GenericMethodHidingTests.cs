using System.Collections.Generic;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests;

public sealed class GenericMethodHidingTests
{
	[Test]
	public async Task HidingInterfaceMethod_ShouldConfigureAndVerifyBaseSlotSeparately()
	{
		IGenericMethodHidingDerived sut = IGenericMethodHidingDerived.CreateMock();
		sut.Mock.Setup.Hidden<int>(It.IsAny<int>()).Returns("derived");
		sut.Mock.As<IGenericMethodHidingBase>().Setup.Hidden<int>(It.IsAny<int>()).Returns("base");

		string hiding = sut.Hidden(1);
		string hidden = ((IGenericMethodHidingBase)sut).Hidden(2);

		await That(hiding).IsEqualTo("derived");
		await That(hidden).IsEqualTo("base");
		await That(sut.Mock.Verify.Hidden(1)).Once();
		await That(sut.Mock.As<IGenericMethodHidingBase>().Verify.Hidden(2)).Once();
	}

	[Test]
	public async Task NewMethod_ShouldNotOverrideHiddenBaseMethod()
	{
		GenericMethodHidingDerived sut = GenericMethodHidingDerived.CreateMock();

		string hiding = sut.Hidden(1);
		string hidden = ((GenericMethodHidingBase)sut).Hidden(1);

		await That(hiding).IsEqualTo("derived");
		await That(hidden).IsEqualTo("base")
			.Because("the hidden base method cannot be overridden by the mock");
	}

	[Test]
	public async Task NewVirtualMethod_ShouldMockHidingMethodOnly()
	{
		GenericMethodHidingDerived sut = GenericMethodHidingDerived.CreateMock();
		sut.Mock.Setup.HiddenVirtual<int>(It.IsAny<int>()).Returns("mocked");

		string hiding = sut.HiddenVirtual(1);
		string hidden = ((GenericMethodHidingBase)sut).HiddenVirtual(2);

		await That(hiding).IsEqualTo("mocked");
		await That(hidden).IsEqualTo("base")
			.Because("the hidden base method cannot be overridden by the mock");
		await That(sut.Mock.Verify.HiddenVirtual<int>(It.IsAny<int>())).Once();
	}

	[Test]
	public async Task NewVirtualMethod_InGenericClass_ShouldMockHidingMethodOnly()
	{
		GenericMethodHidingDerived<int> sut = GenericMethodHidingDerived<int>.CreateMock();
		sut.Mock.Setup.Hidden<string>(It.IsAny<int>(), It.IsAny<string>()).Returns("mocked");

		string hiding = sut.Hidden(1, "a");
		string hidden = ((GenericMethodHidingBase<int>)sut).Hidden(2, "b");

		await That(hiding).IsEqualTo("mocked");
		await That(hidden).IsEqualTo("base")
			.Because("the hidden base method cannot be overridden by the mock");
		await That(sut.Mock.Verify.Hidden<string>(It.IsAny<int>(), It.IsAny<string>())).Once();
	}

	[Test]
	public async Task NewVirtualMethod_WithNestedTypeParameter_ShouldMockHidingMethodOnly()
	{
		GenericMethodHidingDerived sut = GenericMethodHidingDerived.CreateMock();
		sut.Mock.Setup.HiddenNested<int>(It.IsAny<List<int>>()).Returns("mocked");

		string hiding = sut.HiddenNested(new List<int>());
		string hidden = ((GenericMethodHidingBase)sut).HiddenNested(new List<int>());

		await That(hiding).IsEqualTo("mocked");
		await That(hidden).IsEqualTo("base")
			.Because("the hidden base method cannot be overridden by the mock");
		await That(sut.Mock.Verify.HiddenNested<int>(It.IsAny<List<int>>())).Once();
	}

	[Test]
	public async Task OverloadsThatDoNotHide_ShouldMockBothMethods()
	{
		GenericMethodHidingDerived sut = GenericMethodHidingDerived.CreateMock();
		sut.Mock.Setup.Arity<int>(It.IsAny<int>()).Returns("base arity");
		sut.Mock.Setup.Arity<int, string>(It.IsAny<int>()).Returns("derived arity");
		sut.Mock.Setup.Array<int>(It.IsAny<int[]>()).Returns("base array");
		sut.Mock.Setup.Array<int>(It.IsAny<int>()).Returns("derived array");

		string[] results =
		[
			sut.Arity<int>(1),
			sut.Arity<int, string>(1),
			sut.Array<int>(new[] { 1, }),
			sut.Array<int>(1),
		];

		await That(results).IsEqualTo(["base arity", "derived arity", "base array", "derived array",])
			.Because("a different number of type parameters or a different parameter type is an overload and not a hiding member");
	}
}
