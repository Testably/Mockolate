using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests.GeneratorShapes;

public sealed class EnumDefaultValuesTests
{
	[Test]
	public async Task Method_WithOmittedNegativeEnumArgument_ShouldUseTheDeclaredDefault()
	{
		IEnumDefaultValues sut = IEnumDefaultValues.CreateMock();
		sut.Mock.Setup.Negative(It.IsAny<int>()).Returns(3);

		int forDeclaredDefault = sut.Negative(1);
		int forOther = sut.Negative(1, EnumWithNegativeValue.High);

		await That(forDeclaredDefault).IsEqualTo(3);
		await That(forOther).IsEqualTo(0);
		await That(sut.Mock.Verify.Negative(It.IsAny<int>())).Once();
	}

	[Test]
	public async Task Method_WithOmittedNegativeLongEnumArgument_ShouldUseTheDeclaredDefault()
	{
		IEnumDefaultValues sut = IEnumDefaultValues.CreateMock();
		sut.Mock.Setup.NegativeLong(It.IsAny<int>()).Returns(3);

		int forDeclaredDefault = sut.NegativeLong(1);
		int forOther = sut.NegativeLong(1, LongEnumWithNegativeValue.Low);

		await That(forDeclaredDefault).IsEqualTo(3);
		await That(forOther).IsEqualTo(0);
	}

	[Test]
	public async Task Method_WithOmittedNullableEnumArgument_ShouldUseTheDeclaredDefault()
	{
		IEnumDefaultValues sut = IEnumDefaultValues.CreateMock();
		sut.Mock.Setup.NullableNegative(It.IsAny<int>()).Returns(3);

		int forDeclaredDefault = sut.NullableNegative(1);
		int forNull = sut.NullableNegative(1, null);
		int forOther = sut.NullableNegative(1, EnumWithNegativeValue.High);

		await That(forDeclaredDefault).IsEqualTo(3);
		await That(forNull).IsEqualTo(0).Because("only an omitted argument stands for the declared default");
		await That(forOther).IsEqualTo(0);
		await That(sut.Mock.Verify.NullableNegative(It.IsAny<int>())).Once();
	}

	[Test]
	public async Task Indexer_WithOmittedEnumArguments_ShouldUseTheDeclaredDefaults()
	{
		IEnumDefaultValues sut = IEnumDefaultValues.CreateMock();
		sut.Mock.Setup[It.IsAny<int>()].Returns(3);

		int forDeclaredDefaults = sut[1];
		int forOther = sut[1, EnumWithNegativeValue.High];

		await That(forDeclaredDefaults).IsEqualTo(3);
		await That(forOther).IsEqualTo(0);
		await That(sut.Mock.Verify[It.IsAny<int>()].Got()).Once();
	}

	[Test]
	public async Task Delegate_WithOmittedEnumArguments_ShouldUseTheDeclaredDefaults()
	{
		EnumDefaultValuesDelegate sut = EnumDefaultValuesDelegate.CreateMock();
		sut.Mock.Setup().Returns(3);

		int forDeclaredDefaults = sut();
		int forOther = sut(EnumWithNegativeValue.High);

		await That(forDeclaredDefaults).IsEqualTo(3);
		await That(forOther).IsEqualTo(0);
	}

	[Test]
	public async Task Constructor_WithOmittedEnumConstructorParameters_ShouldCreateTheMock()
	{
		EnumDefaultValuesBase sut = EnumDefaultValuesBase.CreateMock();
		sut.Mock.Setup.Method().Returns(3);

		int forDeclaredDefault = sut.Method();
		int forOther = sut.Method(EnumWithNegativeValue.Low);

		await That(forDeclaredDefault).IsEqualTo(3);
		await That(forOther).IsEqualTo(0);
	}
}
