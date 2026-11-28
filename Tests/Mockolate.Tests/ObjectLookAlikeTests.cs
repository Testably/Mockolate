using System.Runtime.CompilerServices;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests;

public sealed class ObjectLookAlikeTests
{
	[Test]
	public async Task EqualsWithOtherReturnType_ShouldBeSetupAndVerifiable()
	{
		object other = new();
		IObjectLookAlikes sut = IObjectLookAlikes.CreateMock();
		sut.Mock.Setup.Equals(It.Is(other)).Returns(3);

		int result = sut.Equals(other);

		await That(result).IsEqualTo(3);
		await That(sut.Mock.Verify.Equals(It.Is(other))).Once();
	}

	[Test]
	public async Task EqualsAndReferenceEqualsWithTwoParameters_ShouldBeSetupAndVerifiable()
	{
		IObjectLookAlikes sut = IObjectLookAlikes.CreateMock();
		sut.Mock.Setup.Equals(It.IsAny<object?>(), It.IsAny<object?>()).Returns(true);
		sut.Mock.Setup.ReferenceEquals(It.IsAny<object?>(), It.IsAny<object?>()).Returns(true);

		bool equals = sut.Equals(1, 2);
		bool referenceEquals = sut.ReferenceEquals(1, 2);

		await That(equals).IsTrue();
		await That(referenceEquals).IsTrue();
		await That(sut.Mock.Verify.Equals(It.IsAny<object?>(), It.IsAny<object?>())).Once();
		await That(sut.Mock.Verify.ReferenceEquals(It.IsAny<object?>(), It.IsAny<object?>())).Once();
	}

	[Test]
	public async Task ParameterlessLookAlikes_ShouldBeSetupAndVerifiable()
	{
		object clone = new();
		IObjectLookAlikes sut = IObjectLookAlikes.CreateMock();
		sut.Mock.Setup.GetHashCode().Returns("hash");
		sut.Mock.Setup.ToString().Returns(42);
		sut.Mock.Setup.GetType().Returns(typeof(string));
		sut.Mock.Setup.MemberwiseClone().Returns(clone);

		string hashCode = sut.GetHashCode();
		object text = sut.ToString();
		Type type = sut.GetType();
		object memberwiseClone = sut.MemberwiseClone();

		await That(hashCode).IsEqualTo("hash");
		await That(text).IsEqualTo(42);
		await That(type).IsEqualTo(typeof(string));
		await That(memberwiseClone).IsSameAs(clone);
		await That(sut.Mock.Verify.GetHashCode()).Once();
		await That(sut.Mock.Verify.ToString()).Once();
		await That(sut.Mock.Verify.GetType()).Once();
		await That(sut.Mock.Verify.MemberwiseClone()).Once();
	}

	[Test]
	public async Task ObjectMembers_ShouldKeepTheirBehavior()
	{
		IObjectLookAlikes sut = IObjectLookAlikes.CreateMock();
		sut.Mock.Setup.Equals(It.IsAny<object?>()).Returns(1);
		sut.Mock.Setup.GetHashCode().Returns("hash");
		sut.Mock.Setup.ToString().Returns(42);
		sut.Mock.Setup.GetType().Returns(typeof(string));
		object subject = sut;

		bool equals = subject.Equals(sut);
		int hashCode = subject.GetHashCode();
		string? text = subject.ToString();
		Type type = subject.GetType();

		await That(equals).IsTrue()
			.Because("object.Equals is not overridden by the look-alike and still compares references");
		await That(hashCode).IsEqualTo(RuntimeHelpers.GetHashCode(sut));
		await That(text).IsNotEqualTo("42");
		await That(typeof(IObjectLookAlikes).IsAssignableFrom(type)).IsTrue();
		await That(sut.Mock.Verify.Equals(It.IsAny<object?>())).Never()
			.Because("calls of the object members do not reach the hiding look-alikes");
		await That(sut.Mock.Verify.GetHashCode()).Never();
		await That(sut.Mock.Verify.ToString()).Never();
		await That(sut.Mock.Verify.GetType()).Never();
	}

	[Test]
	public async Task LookAlikes_WhenInherited_ShouldBeSetupAndVerifiable()
	{
		IObjectLookAlikesDerived sut = IObjectLookAlikesDerived.CreateMock();
		sut.Mock.Setup.GetHashCode().Returns("hash");

		string result = sut.GetHashCode();

		await That(result).IsEqualTo("hash");
		await That(sut.Mock.Verify.GetHashCode()).Once();
	}

	[Test]
	public async Task LookAlikes_OnAdditionalInterface_ShouldBeSetupAndVerifiable()
	{
		IVoidObjectLookAlikes sut = IVoidObjectLookAlikes.CreateMock()
			.Implementing<IObjectLookAlikes>();
		sut.Mock.As<IObjectLookAlikes>().Setup.GetHashCode().Returns("hash");

		string result = ((IObjectLookAlikes)sut).GetHashCode();

		await That(result).IsEqualTo("hash");
		await That(sut.Mock.As<IObjectLookAlikes>().Verify.GetHashCode()).Once();
	}

	[Test]
	public async Task VoidLookAlikes_ShouldBeSetupAndVerifiable()
	{
		int equalsCalls = 0;
		int getHashCodeCalls = 0;
		int finalizeCalls = 0;
		IVoidObjectLookAlikes sut = IVoidObjectLookAlikes.CreateMock();
		sut.Mock.Setup.Equals(It.IsAny<object?>()).Do(() => equalsCalls++);
		sut.Mock.Setup.GetHashCode().Do(() => getHashCodeCalls++);
		sut.Mock.Setup.Finalize().Do(() => finalizeCalls++);

		sut.Equals(null);
		sut.GetHashCode();
		sut.Finalize();

		await That(equalsCalls).IsEqualTo(1);
		await That(getHashCodeCalls).IsEqualTo(1);
		await That(finalizeCalls).IsEqualTo(1);
		await That(sut.Mock.Verify.Equals(It.IsNull<object?>())).Once();
		await That(sut.Mock.Verify.GetHashCode()).Once();
		await That(sut.Mock.Verify.Finalize()).Once();
	}

	[Test]
	public async Task RefLookAlikes_ShouldBeSetupAndVerifiable()
	{
		object? other = new();
		IRefObjectLookAlikes sut = IRefObjectLookAlikes.CreateMock();
		sut.Mock.Setup.Equals(It.Is(other)).Returns(true);
		sut.Mock.Setup.GetHashCode().Returns(7);

		bool equals = sut.Equals(in other);
		int hashCode = sut.GetHashCode();

		await That(equals).IsTrue();
		await That(hashCode).IsEqualTo(7);
		await That(sut.Mock.Verify.Equals(It.Is(other))).Once();
		await That(sut.Mock.Verify.GetHashCode()).Once();
	}

	[Test]
	public async Task RefLookAlikes_WithMatcherForDerivedType_ShouldBeSetupAndVerifiable()
	{
		object? text = "a";
		object? other = new();
		IRefObjectLookAlikes sut = IRefObjectLookAlikes.CreateMock();
		sut.Mock.Setup.Equals(It.IsAny<string>()).Returns(true);

		bool[] results = [sut.Equals(in text), sut.Equals(in other),];

		await That(results).IsEqualTo([true, false,])
			.Because("a matcher for a derived type is wrapped covariantly for an in parameter as well");
		await That(sut.Mock.Verify.Equals(It.IsAny<string>())).Once();
	}

	[Test]
	public async Task PropertyAndEventNamedLikeObjectMethods_ShouldBeSetupAndVerifiable()
	{
		int raised = 0;
		IObjectLookAlikeProperties sut = IObjectLookAlikeProperties.CreateMock();
		sut.Mock.Setup.ToString.InitializeWith(5);
		sut.GetHashCode += () => raised++;

		int value = sut.ToString;
		sut.Mock.Raise.GetHashCode();

		await That(value).IsEqualTo(5);
		await That(raised).IsEqualTo(1);
		await That(sut.Mock.Verify.ToString.Got()).Once();
		await That(sut.Mock.Verify.GetHashCode.Subscribed()).Once();
	}

#if NET10_0_OR_GREATER
	[Test]
	public async Task StaticLookAlikes_ShouldBeSetupAndVerifiable()
	{
		IStaticObjectLookAlikes sut = IStaticObjectLookAlikes.CreateMock();
		sut.Mock.SetupStatic.Equals(It.IsAny<object?>()).Returns(true);
		sut.Mock.SetupStatic.GetHashCode().Returns(7);
		sut.Mock.SetupStatic.ToString().Returns("static");
		sut.Mock.SetupStatic.ReferenceEquals(It.IsAny<object?>(), It.IsAny<object?>()).Returns(true);

		string result = Call<Mock.IStaticObjectLookAlikes>();

		await That(result).IsEqualTo("True, 7, static, True");
		await That(sut.Mock.VerifyStatic.Equals(It.IsAny<object?>())).Once();
		await That(sut.Mock.VerifyStatic.GetHashCode()).Once();
		await That(sut.Mock.VerifyStatic.ToString()).Once();
		await That(sut.Mock.VerifyStatic.ReferenceEquals(It.IsAny<object?>(), It.IsAny<object?>())).Once();

		static string Call<T>() where T : IStaticObjectLookAlikes
			=> $"{T.Equals(null)}, {T.GetHashCode()}, {T.ToString()}, {T.ReferenceEquals(null, null)}";
	}
#endif
}
