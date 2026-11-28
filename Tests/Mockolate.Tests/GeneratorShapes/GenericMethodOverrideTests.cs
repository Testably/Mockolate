using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests.GeneratorShapes;

public sealed class GenericMethodOverrideTests
{
	[Test]
	public async Task ConstrainedNullableTypeParameter_ShouldBeSetUpAndVerifiable()
	{
		GenericMethodOverrideBase sut = GenericMethodOverrideBase.CreateMock();
		int nullCount = 0;
		sut.Mock.Setup.AcceptClass<string>(It.IsNull<string?>()).Do(() => nullCount++);
		sut.Mock.Setup.GetNotNull<int>().Returns(5);
		sut.Mock.Setup.GetComparable<int>(It.IsAny<int>()).Returns(6);
		sut.Mock.Setup.GetComparable<string>(It.IsAny<string?>()).Returns("a");

		sut.AcceptClass<string>(null);
		sut.AcceptClass("b");
		int result1 = sut.GetNotNull<int>();
		long result2 = sut.GetNotNull<long>();
		int result3 = sut.GetComparable(3);
		string? result4 = sut.GetComparable<string>(null);

		await That(nullCount).IsEqualTo(1);
		await That(result1).IsEqualTo(5).Because("U? of a type parameter that is not constrained to a value type is the plain int and not Nullable<int>");
		await That(result2).IsEqualTo(0L).Because("a method that is not set up returns the default value of the type argument");
		await That(result3).IsEqualTo(6);
		await That(result4).IsEqualTo("a");
		await That(sut.Mock.Verify.AcceptClass<string>(It.IsNull<string?>())).Once();
		await That(sut.Mock.Verify.AcceptClass("b")).Once();
		await That(sut.Mock.Verify.GetNotNull<int>()).Once();
		await That(sut.Mock.Verify.GetComparable(3)).Once();
	}

	[Test]
	public async Task NullableTypeParameterParameter_ShouldBeSetUp()
	{
		GenericMethodOverrideBase sut = GenericMethodOverrideBase.CreateMock();
		int sum = 0;
		int nullCount = 0;
		sut.Mock.Setup.Accept<int>(It.IsAny<int>()).Do(value => sum += value);
		sut.Mock.Setup.AcceptVirtual<int>(It.IsAny<int>()).Do(value => sum += value);
		sut.Mock.Setup.Accept<string>(It.IsNull<string?>()).Do(() => nullCount++);

		sut.Accept(3);
		sut.AcceptVirtual(4);
		sut.Accept<string>(null);
		sut.Accept("a");

		await That(sum).IsEqualTo(7).Because("U? of an unconstrained type parameter is the plain int and not Nullable<int>");
		await That(nullCount).IsEqualTo(1);
	}

	[Test]
	public async Task NullableTypeParameterParameter_ShouldBeVerifiable()
	{
		GenericMethodOverrideBase sut = GenericMethodOverrideBase.CreateMock();

		sut.Accept(3);
		sut.Accept<string>(null);
		sut.AcceptVirtual("a");

		await That(sut.Mock.Verify.Accept(3)).Once();
		await That(sut.Mock.Verify.Accept<int>(It.IsAny<int>())).Once();
		await That(sut.Mock.Verify.Accept<string>(It.IsNull<string?>())).Once();
		await That(sut.Mock.Verify.Accept(4)).Never();
		await That(sut.Mock.Verify.AcceptVirtual("a")).Once();
		await That(sut.Mock.Verify.AcceptVirtual<int>(It.IsAny<int>())).Never();
	}

	[Test]
	public async Task NullableTypeParameterReturn_ShouldBeSetUp()
	{
		GenericMethodOverrideBase sut = GenericMethodOverrideBase.CreateMock();
		sut.Mock.Setup.Get<int>().Returns(5);
		sut.Mock.Setup.Get<string>().Returns("a");
		sut.Mock.Setup.GetVirtual<int>().Returns(6);

		int result1 = sut.Get<int>();
		string? result2 = sut.Get<string>();
		long result3 = sut.Get<long>();
		int result4 = sut.GetVirtual<int>();
		string? result5 = sut.GetVirtual<string>();

		await That(result1).IsEqualTo(5);
		await That(result2).IsEqualTo("a");
		await That(result3).IsEqualTo(0L).Because("a method that is not set up returns the default value of the type argument");
		await That(result4).IsEqualTo(6);
		await That(result5).IsNull();
		await That(sut.Mock.Verify.Get<int>()).Once();
		await That(sut.Mock.Verify.GetVirtual<string>()).Once();
	}

	[Test]
	public async Task ProtectedMethod_WithNullableTypeParameter_ShouldBeSetUpAndVerifiable()
	{
		GenericMethodOverrideBase sut = GenericMethodOverrideBase.CreateMock();
		sut.Mock.SetupProtected.Combine<int, string>(It.IsAny<int>(), It.Is("a")).Returns(7);

		int result1 = sut.InvokeCombine(1, "a");
		int result2 = sut.InvokeCombine(1, "b");

		await That(result1).IsEqualTo(7);
		await That(result2).IsEqualTo(0);
		await That(sut.Mock.VerifyProtected.Combine<int, string>(1, It.IsAny<string>())).Twice();
	}
}
