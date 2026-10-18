using Mockolate.Exceptions;
using Mockolate.Setup;
using static Mockolate.Tests.MockMethods.SetupMethodTests;

namespace Mockolate.Tests.MockMethods;

public sealed partial class VerifyInvokedTests
{
	[Test]
	public async Task Equals_ShouldWork()
	{
		object obj = new();
		IMethodService sut = IMethodService.CreateMock();

		_ = sut.Equals(obj);

		await That(sut.Mock.Verify.Equals(It.Is(obj))).Once();
	}

	[Test]
	public async Task Equals_ShouldWorkWithNull()
	{
		object? obj = null;
		IMethodService sut = IMethodService.CreateMock();

		_ = sut.Equals(null);

		await That(sut.Mock.Verify.Equals(It.Is(obj))).Once();
	}

	[Test]
	public async Task Equals_WithOtherOverload_ShouldWork()
	{
		object obj = new();
		IMethodService sut = IMethodService.CreateMock();

		_ = sut.Equals(3);

		await That(sut.Mock.Verify.Equals(It.Is(obj))).Never();
	}

	[Test]
	public async Task GetHashCode_ShouldWork()
	{
		IMethodService sut = IMethodService.CreateMock();

		_ = sut.GetHashCode();

		await That(sut.Mock.Verify.GetHashCode()).Once();
	}

	[Test]
	[Arguments(-1, 0)]
	[Arguments(1, 1)]
	public async Task InvokedSetup_ShouldVerifySameConditionAsSetup(int firstParameter, int expectedCallCount)
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();
		IMethodSetup setup = sut.Mock.Setup.Subtract(
			It.Satisfies<int>(x => x > 0),
			It.IsAny<int?>()).Returns(1);

		sut.Subtract(firstParameter, 4);

		await That(sut.Mock.VerifySetup(setup)).Exactly(expectedCallCount);
	}

	[Test]
	public async Task MethodWithDifferentName_ShouldNotMatch()
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();

		sut.Multiply(1, 4);
		sut.Multiply(2, 4);

		await That(sut.Mock.Verify.Subtract(It.IsAny<int>(), It.IsAny<int?>())).Never();
	}

	[Test]
	public async Task MethodWithDifferentName_WithParameters_ShouldNotMatch()
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();

		sut.Subtract(1, 4);
		sut.Subtract(2, 4);

		await That(sut.Mock.Verify.Multiply(Match.AnyParameters())).Never();
	}

	[Test]
	public async Task MethodWithDifferentOverload_ShouldNotMatch()
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();

		sut.Subtract(1, 4, false);
		sut.Subtract(2, 4, true);

		await That(sut.Mock.Verify.Subtract(It.IsAny<int>(), It.IsAny<int?>())).Never();
	}

	[Test]
	[Arguments(2)]
	[Arguments(42)]
	public async Task MethodWithReturnValue_ShouldBeRegistered(int numberOfInvocations)
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();
		sut.Mock.Setup.Multiply(It.IsAny<int>(), It.IsAny<int?>()).Returns(1);

		for (int i = 0; i < numberOfInvocations; i++)
		{
			sut.Multiply(i, 4);
		}

		await That(sut.Mock.Verify.Multiply(It.IsAny<int>(), It.IsAny<int?>())).Exactly(numberOfInvocations);
	}

	[Test]
	public async Task ToString_ShouldWork()
	{
		IMethodService sut = IMethodService.CreateMock();

		_ = sut.ToString();

		await That(sut.Mock.Verify.ToString()).Once();
	}

	[Test]
	[Arguments(2)]
	[Arguments(42)]
	public async Task VoidMethod_ShouldBeRegistered(int numberOfInvocations)
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();
		sut.Mock.Setup.SetIsValid(It.IsAny<bool>(), It.IsAny<Func<bool>?>());

		for (int i = 0; i < numberOfInvocations; i++)
		{
			sut.SetIsValid(i % 2 == 0, () => true);
		}

		await That(sut.Mock.Verify.SetIsValid(It.IsAny<bool>(), It.IsAny<Func<bool>?>()))
			.Exactly(numberOfInvocations);
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task VoidMethod_ShouldThrowMockNotSetupExceptionWhenBehaviorIsSetToThrow(
		bool throwWhenNotSetup)
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock(MockBehavior.Default with
		{
			ThrowWhenNotSetup = throwWhenNotSetup,
		});

		void Act()
		{
			sut.SetIsValid(true, null);
		}

		await That(Act).Throws<MockNotSetupException>().OnlyIf(throwWhenNotSetup)
			.WithMessage("""
			             The method 'global::Mockolate.Tests.MockTests.IMyService.SetIsValid(bool, Func<bool>?)' was invoked without prior setup.
			             """);
	}

	[Test]
	public async Task WhenBehaviorIsSetToThrow_ShouldThrowMockNotSetupException()
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock(MockBehavior.Default with
		{
			ThrowWhenNotSetup = true,
		});

		void Act()
		{
			sut.Multiply(3, null);
		}

		await That(Act).Throws<MockNotSetupException>()
			.WithMessage("""
			             The method 'global::Mockolate.Tests.MockTests.IMyService.Multiply(int, int?)' was invoked without prior setup.
			             """);
	}

	[Test]
	public async Task WhenNotSetup_ShouldReturnDefaultValue()
	{
		MockTests.IMyService sut = MockTests.IMyService.CreateMock();

		int result = sut.Multiply(3, 4);

		await That(result).IsEqualTo(default(int));
	}

	public class ReturnMethodWith1Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method1(1);
			sut.Method1(2);
			sut.Method2(1, 2);

			await That(sut.Mock.Verify.Method1(1).AnyParameters()).Twice();
		}

		[Test]
		public async Task WithExplicitParameter_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method1(1);

			await That(sut.Mock.Verify.Method1(1)).Once();
			await That(sut.Mock.Verify.Method1(2)).Never();
		}

		[Test]
		public async Task WithIParameterOverload_ShouldStillWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method1(5);

			await That(sut.Mock.Verify.Method1(It.Satisfies<int>(x => x > 0))).Once();
			await That(sut.Mock.Verify.Method1(It.Satisfies<int>(x => x > 10))).Never();
		}
	}

	public class ReturnMethodWith2Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method2(1, 2);
			sut.Method2(3, 4);

			await That(sut.Mock.Verify.Method2(1, 2).AnyParameters()).Twice();
		}

		[Test]
		public async Task WithExplicitParameter1_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method2(1, 10);

			await That(sut.Mock.Verify.Method2(1, It.IsAny<int>())).Once();
			await That(sut.Mock.Verify.Method2(2, It.IsAny<int>())).Never();
		}

		[Test]
		public async Task WithExplicitParameter2_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method2(10, 1);

			await That(sut.Mock.Verify.Method2(It.IsAny<int>(), 1)).Once();
			await That(sut.Mock.Verify.Method2(It.IsAny<int>(), 2)).Never();
		}

		[Test]
		public async Task WithExplicitParameters_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method2(1, 2);

			await That(sut.Mock.Verify.Method2(1, 2)).Once();
			await That(sut.Mock.Verify.Method2(1, 10)).Never();
			await That(sut.Mock.Verify.Method2(10, 2)).Never();
		}
	}

	public class ReturnMethodWith3Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method3(1, 2, 3);
			sut.Method3(4, 5, 6);

			await That(sut.Mock.Verify.Method3(1, 2, 3).AnyParameters()).Twice();
		}

		[Test]
		public async Task WithExplicitParameter1_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method3(1, 10, 20);

			await That(sut.Mock.Verify.Method3(1, It.IsAny<int>(), It.IsAny<int>())).Once();
			await That(sut.Mock.Verify.Method3(2, It.IsAny<int>(), It.IsAny<int>())).Never();
		}

		[Test]
		public async Task WithExplicitParameter2_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method3(10, 1, 20);

			await That(sut.Mock.Verify.Method3(It.IsAny<int>(), 1, It.IsAny<int>())).Once();
			await That(sut.Mock.Verify.Method3(It.IsAny<int>(), 2, It.IsAny<int>())).Never();
		}

		[Test]
		public async Task WithExplicitParameter3_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method3(10, 20, 1);

			await That(sut.Mock.Verify.Method3(It.IsAny<int>(), It.IsAny<int>(), 1)).Once();
			await That(sut.Mock.Verify.Method3(It.IsAny<int>(), It.IsAny<int>(), 2)).Never();
		}

		[Test]
		public async Task WithExplicitParameters_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method3(1, 2, 3);

			await That(sut.Mock.Verify.Method3(1, 2, 3)).Once();
			await That(sut.Mock.Verify.Method3(1, 10, 20)).Never();
			await That(sut.Mock.Verify.Method3(10, 2, 20)).Never();
			await That(sut.Mock.Verify.Method3(10, 20, 3)).Never();
		}
	}

	public class ReturnMethodWith5Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method5(1, 2, 3, 4, 5);
			sut.Method5(6, 7, 8, 9, 10);

			await That(sut.Mock.Verify.Method5(1, 2, 3, 4, 5).AnyParameters()).Twice();
		}

		[Test]
		public async Task WithExplicitParameters_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method5(1, 2, 3, 4, 5);

			await That(sut.Mock.Verify.Method5(1, 2, 3, 4, 5)).Once();
			await That(sut.Mock.Verify.Method5(10, 2, 3, 4, 5)).Never();
			await That(sut.Mock.Verify.Method5(1, 20, 3, 4, 5)).Never();
			await That(sut.Mock.Verify.Method5(1, 2, 30, 4, 5)).Never();
			await That(sut.Mock.Verify.Method5(1, 2, 3, 40, 5)).Never();
			await That(sut.Mock.Verify.Method5(1, 2, 3, 4, 50)).Never();
		}
	}

	public class ReturnMethodWith4Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method4(1, 2, 3, 4);
			sut.Method4(5, 6, 7, 8);

			await That(sut.Mock.Verify.Method4(1, 2, 3, 4).AnyParameters()).Twice();
		}

		[Test]
		public async Task WithExplicitParameter1_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method4(1, 10, 20, 30);

			await That(sut.Mock.Verify.Method4(1, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Once();
			await That(sut.Mock.Verify.Method4(2, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Never();
		}

		[Test]
		public async Task WithExplicitParameter2_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method4(10, 1, 20, 30);

			await That(sut.Mock.Verify.Method4(It.IsAny<int>(), 1, It.IsAny<int>(), It.IsAny<int>())).Once();
			await That(sut.Mock.Verify.Method4(It.IsAny<int>(), 2, It.IsAny<int>(), It.IsAny<int>())).Never();
		}

		[Test]
		public async Task WithExplicitParameter3_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method4(10, 20, 1, 30);

			await That(sut.Mock.Verify.Method4(It.IsAny<int>(), It.IsAny<int>(), 1, It.IsAny<int>())).Once();
			await That(sut.Mock.Verify.Method4(It.IsAny<int>(), It.IsAny<int>(), 2, It.IsAny<int>())).Never();
		}

		[Test]
		public async Task WithExplicitParameter4_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method4(10, 20, 30, 1);

			await That(sut.Mock.Verify.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), 1)).Once();
			await That(sut.Mock.Verify.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), 2)).Never();
		}

		[Test]
		public async Task WithExplicitParameters_ShouldWork()
		{
			IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

			sut.Method4(1, 2, 3, 4);

			await That(sut.Mock.Verify.Method4(1, 2, 3, 4)).Once();
			await That(sut.Mock.Verify.Method4(1, 10, 20, 30)).Never();
			await That(sut.Mock.Verify.Method4(10, 2, 20, 30)).Never();
			await That(sut.Mock.Verify.Method4(10, 20, 3, 30)).Never();
			await That(sut.Mock.Verify.Method4(10, 20, 30, 4)).Never();
		}
	}

	public class VoidMethodWith1Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

			sut.Method1(1);
			sut.Method1(2);

			await That(sut.Mock.Verify.Method1(1).AnyParameters()).Twice();
		}
	}

	public class VoidMethodWith2Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

			sut.Method2(1, 2);
			sut.Method2(3, 4);

			await That(sut.Mock.Verify.Method2(1, 2).AnyParameters()).Twice();
		}
	}

	public class VoidMethodWith3Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

			sut.Method3(1, 2, 3);
			sut.Method3(4, 5, 6);

			await That(sut.Mock.Verify.Method3(1, 2, 3).AnyParameters()).Twice();
		}
	}

	public class VoidMethodWith4Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

			sut.Method4(1, 2, 3, 4);
			sut.Method4(5, 6, 7, 8);

			await That(sut.Mock.Verify.Method4(1, 2, 3, 4).AnyParameters()).Twice();
		}
	}

	public class VoidMethodWith5Parameters
	{
		[Test]
		public async Task AnyParameters_ShouldIgnoreExplicitParameters()
		{
			IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

			sut.Method5(1, 2, 3, 4, 5);
			sut.Method5(6, 7, 8, 9, 10);

			await That(sut.Mock.Verify.Method5(1, 2, 3, 4, 5).AnyParameters()).Twice();
		}
	}
}
