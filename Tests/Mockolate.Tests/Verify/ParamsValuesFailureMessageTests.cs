using Mockolate.Exceptions;
using Mockolate.Verify;

namespace Mockolate.Tests.Verify;

public sealed class ParamsValuesFailureMessageTests
{
	[Test]
	public async Task WithOneParameter_ShouldListTheValues()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.One("a", "b").Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method One([\"a\", \"b\"]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithTwoParameters_ShouldListTheValues()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.Two(1, 2, 3).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Two(1, [2, 3]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithThreeParameters_ShouldListTheValues()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.Three(1, 2, "a").Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Three(1, 2, [\"a\"]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithFourParameters_ShouldListTheValues()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.Four(1, 2, 3, 4.5, 5.5).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Four(1, 2, 3, [4.5, 5.5]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithFiveParameters_ShouldListTheValues()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.Five(1, 2, 3, 4, "a", "b").Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Five(1, 2, 3, 4, [\"a\", \"b\"]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithObjectValues_ShouldListTheValues()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.Objects("a", 1).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Objects([\"a\", 1]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithNullArray_ShouldShowNull()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.One(null!).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method One(null) exactly once, but it never did.");
	}

	[Test]
	public async Task WithGenericElementType_ShouldListTheValues()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.Generic<object>(1, "a").Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Generic<T>([1, \"a\"]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithGenericElementTypeAndNullArray_ShouldShowNull()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.Generic<object>((object[])null!).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Generic<T>(null) exactly once, but it never did.");
	}

	[Test]
	public async Task WithMatcherAndValues_ShouldListTheValues()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.Two(It.IsAny<int>(), 2, 3).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Two(It.IsAny<int>(), [2, 3]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithNullableElements_ShouldListTheValues()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.Nullables(1, null).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Nullables([1, null]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithNullObjectArray_ShouldShowNull()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.Objects((object[])null!).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Objects(null) exactly once, but it never did.");
	}

	[Test]
	public async Task WithEmptyArray_ShouldShowEmptyBrackets()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.Two(1, Array.Empty<int>()).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Two(1, []) exactly once, but it never did.");
	}

	[Test]
	public async Task WithNullElement_ShouldListTheNullElement()
	{
		IParamsMethods sut = IParamsMethods.CreateMock();

		void Act()
		{
			sut.Mock.Verify.One("a", null!).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method One([\"a\", null]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithDelegateAndOneParameter_ShouldListTheValues()
	{
		OneDelegate sut = OneDelegate.CreateMock();

		void Act()
		{
			sut.Mock.Verify("a", "b").Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Invoke([\"a\", \"b\"]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithDelegateAndTwoParameters_ShouldListTheValues()
	{
		TwoDelegate sut = TwoDelegate.CreateMock();

		void Act()
		{
			sut.Mock.Verify(1, 2, 3).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Invoke(1, [2, 3]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithDelegateAndThreeParameters_ShouldListTheValues()
	{
		ThreeDelegate sut = ThreeDelegate.CreateMock();

		void Act()
		{
			sut.Mock.Verify(1, 2, "a").Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Invoke(1, 2, [\"a\"]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithDelegateAndFourParameters_ShouldListTheValues()
	{
		FourDelegate sut = FourDelegate.CreateMock();

		void Act()
		{
			sut.Mock.Verify(1, 2, 3, 4.5, 5.5).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Invoke(1, 2, 3, [4.5, 5.5]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithDelegateAndFiveParameters_ShouldListTheValues()
	{
		FiveDelegate sut = FiveDelegate.CreateMock();

		void Act()
		{
			sut.Mock.Verify(1, 2, 3, 4, "a", "b").Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Invoke(1, 2, 3, 4, [\"a\", \"b\"]) exactly once, but it never did.");
	}

	[Test]
	public async Task WithDelegateAndNullArray_ShouldShowNull()
	{
		OneDelegate sut = OneDelegate.CreateMock();

		void Act()
		{
			sut.Mock.Verify(null!).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Invoke(null) exactly once, but it never did.");
	}

	public interface IParamsMethods
	{
		int One(params string[] values);
		int Two(int a, params int[] values);
		int Three(int a, int b, params string[] values);
		int Four(int a, int b, int c, params double[] values);
		int Five(int a, int b, int c, int d, params string[] values);
		int Objects(params object[] values);
		int Generic<T>(params T[] values);
		int Nullables(params int?[] values);
	}

	public delegate int OneDelegate(params string[] values);

	public delegate int TwoDelegate(int a, params int[] values);

	public delegate int ThreeDelegate(int a, int b, params string[] values);

	public delegate int FourDelegate(int a, int b, int c, params double[] values);

	public delegate int FiveDelegate(int a, int b, int c, int d, params string[] values);
}
