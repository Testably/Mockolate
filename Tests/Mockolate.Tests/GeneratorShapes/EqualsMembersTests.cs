using System.Collections.Generic;
using Mockolate.Exceptions;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests.GeneratorShapes;

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
	public async Task Equals_WithClass_ShouldBeSetupAndVerifiable()
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
	public async Task EqualsAndHiddenGetHashCode_WithClass_ShouldBeSetupAndVerifiable()
	{
		HiddenGetHashCodeBase sut = HiddenGetHashCodeBase.CreateMock();
		sut.Mock.Setup.Equals(It.IsAny<object?>()).Returns(true);
		sut.Mock.Setup.GetHashCode().Returns(7);

		bool equals = sut.Equals(new object());
		int hashCode = sut.GetHashCode();
		int objectHashCode = ((object)sut).GetHashCode();

		await That(equals).IsTrue();
		await That(hashCode).IsEqualTo(7);
		await That(objectHashCode).IsEqualTo(42)
			.Because("the hiding GetHashCode does not override object.GetHashCode");
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
	public async Task Equals_WithoutSetup_ShouldUseReferenceEquality()
	{
		IEqualsOnly sut = IEqualsOnly.CreateMock();
		IEqualsOnly other = IEqualsOnly.CreateMock();

		bool self = sut.Equals(sut);
		bool otherMock = sut.Equals(other);
		bool nullValue = sut.Equals(null);

		await That(self).IsTrue();
		await That(otherMock).IsFalse();
		await That(nullValue).IsFalse();
		await That(sut.Mock.Verify.Equals(It.IsAny<object?>())).Exactly(3);
	}

	[Test]
	public async Task Equals_WithClassWithoutSetup_ShouldUseReferenceEquality()
	{
		EqualsOnlyBase sut = EqualsOnlyBase.CreateMock();

		bool self = sut.Equals(sut);
		bool otherObject = sut.Equals(new object());

		await That(self).IsTrue();
		await That(otherObject).IsFalse();
	}

	[Test]
	public async Task Equals_WithoutSetup_WhenThrowingWhenNotSetup_ShouldThrow()
	{
		IEqualsOnly sut = IEqualsOnly.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

		void Act() => sut.Equals(sut);

		await That(Act).Throws<MockNotSetupException>();
	}

	[Test]
	public async Task Mock_WithoutSetup_ShouldBeUsableInCollections()
	{
		IEqualsOnly sut = IEqualsOnly.CreateMock();
		IEqualsOnly other = IEqualsOnly.CreateMock();
		List<IEqualsOnly> list = [sut,];
		HashSet<IEqualsOnly> set = [sut,];

		await That(list.Contains(sut)).IsTrue();
		await That(list.Contains(other)).IsFalse();
		await That(set.Contains(sut)).IsTrue();
		await That(set.Contains(other)).IsFalse();
	}

	[Test]
	public async Task Mock_WithoutSetup_ShouldBeUsableAsDictionaryKey()
	{
		IEqualsOnly sut = IEqualsOnly.CreateMock();
		IEqualsOnly other = IEqualsOnly.CreateMock();
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
	public async Task Implementing_WhenBaseDeclaresOnlyEquals_ShouldSetUpEqualsAndTheGetHashCodeOfTheAddedInterface()
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
