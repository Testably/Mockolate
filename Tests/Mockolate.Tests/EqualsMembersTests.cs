using System.Collections.Generic;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests;

public sealed class EqualsMembersTests
{
	[Test]
	public async Task Equals_ShouldBeSetupAndVerifiable()
	{
		object other = new();
		IEqualsOnly sut = IEqualsOnly.CreateMock();
		sut.Mock.Setup.Equals(It.Is(other)).Returns(true);

		bool matching = sut.Equals(other);
		bool notMatching = sut.Equals(new object());

		await That(matching).IsTrue();
		await That(notMatching).IsFalse();
		await That(sut.Mock.Verify.Equals(It.Is(other))).Once();
		await That(sut.Mock.Verify.Equals(It.IsAny<object?>())).Twice();
	}

	[Test]
	public async Task Equals_WhenInherited_ShouldBeSetupAndVerifiable()
	{
		IEqualsOnlyDerived sut = IEqualsOnlyDerived.CreateMock();
		sut.Mock.Setup.Equals(It.IsAny<object?>()).Returns(true);

		bool result = sut.Equals(null);

		await That(result).IsTrue();
		await That(sut.Mock.Verify.Equals(It.IsAny<object?>())).Once();
	}

	[Test]
	public async Task Equals_WithNotNullParameter_ShouldBeSetupAndVerifiable()
	{
		object other = new();
		IEqualsNotNull sut = IEqualsNotNull.CreateMock();
		sut.Mock.Setup.Equals(It.Is(other)).Returns(true);

		bool result = sut.Equals(other);

		await That(result).IsTrue();
		await That(sut.Mock.Verify.Equals(It.Is(other))).Once();
	}

	[Test]
	public async Task Equals_OfClass_ShouldBeSetupAndVerifiable()
	{
		EqualsOnlyBase sut = EqualsOnlyBase.CreateMock();
		sut.Mock.Setup.Equals(It.IsAny<object?>()).Returns(true);

		bool result = sut.Equals(new object());

		await That(result).IsTrue();
		await That(sut.GetHashCode()).IsEqualTo(42);
		await That(sut.Mock.Verify.Equals(It.IsAny<object?>())).Once();
	}

	[Test]
	public async Task EqualsAndGetHashCode_ShouldBeSetupAndVerifiable()
	{
		IEqualsAndGetHashCode sut = IEqualsAndGetHashCode.CreateMock();
		sut.Mock.Setup.Equals(It.IsAny<object?>()).Returns(true);
		sut.Mock.Setup.GetHashCode().Returns(7);

		bool equals = sut.Equals(null);
		int hashCode = sut.GetHashCode();

		await That(equals).IsTrue();
		await That(hashCode).IsEqualTo(7);
		await That(sut.Mock.Verify.Equals(It.IsAny<object?>())).Once();
		await That(sut.Mock.Verify.GetHashCode()).Once();
	}

	[Test]
	public async Task GetHashCode_WhenOnlyEqualsIsDeclared_ShouldBeStable()
	{
		IEqualsOnly sut = IEqualsOnly.CreateMock();

		int first = sut.GetHashCode();
		int second = sut.GetHashCode();

		await That(second).IsEqualTo(first);
	}

	[Test]
	public async Task Mock_WhenEqualsReturnsReferenceEquality_ShouldBeUsableAsDictionaryKey()
	{
		IEqualsOnly sut = IEqualsOnly.CreateMock();
		IEqualsOnly other = IEqualsOnly.CreateMock();
		sut.Mock.Setup.Equals(It.Satisfies<object?>(obj => ReferenceEquals(obj, sut))).Returns(true);
		Dictionary<IEqualsOnly, int> dictionary = new()
		{
			[sut] = 1,
			[other] = 2,
		};

		bool found = dictionary.TryGetValue(sut, out int value);

		await That(found).IsTrue();
		await That(value).IsEqualTo(1);
	}

	[Test]
	public async Task Implementing_WhenBaseDeclaresOnlyEquals_ShouldKeepBothSurfaces()
	{
		IEqualsOnly sut = IEqualsOnly.CreateMock().Implementing<IEqualsAndGetHashCode>();
		sut.Mock.Setup.Equals(It.IsAny<object?>()).Returns(true);
		sut.Mock.As<IEqualsAndGetHashCode>().Setup.GetHashCode().Returns(7);

		bool equals = sut.Equals(null);
		int hashCode = ((IEqualsAndGetHashCode)sut).GetHashCode();

		await That(equals).IsTrue();
		await That(hashCode).IsEqualTo(7);
	}
}
