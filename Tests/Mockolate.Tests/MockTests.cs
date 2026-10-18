using Mockolate.Exceptions;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed partial class MockTests
{
	[Test]
	public async Task Create_BaseClassWithMultipleConstructors()
	{
		void Act()
		{
			_ = MyServiceBaseWithMultipleConstructors.CreateMock([5,]);
		}

		await That(Act).DoesNotThrow();
	}

	[Test]
	public async Task Create_BaseClassWithoutConstructor_ShouldThrowMockException()
	{
		void Act()
		{
			_ = MyBaseClassWithoutConstructor.CreateMock();
		}

		await That(Act).Throws<MockException>()
			.WithMessage("This method should not be called directly. Either 'Mockolate.Tests.MockTests+MyBaseClassWithoutConstructor' is not mockable or the source generator did not run correctly.");
	}

	[Test]
	public async Task Create_BaseClassWithVirtualCallsInConstructor_AllowExplicitSetup()
	{
		MyServiceBaseWithVirtualCallsInConstructor sut =
			MyServiceBaseWithVirtualCallsInConstructor.CreateMock(setup => setup.VirtualMethod().Returns([5, 6,]));

		int value = sut.VirtualProperty;

		await That(sut.Mock.Verify.VirtualMethod()).Once();
		await That(value).IsEqualTo(5);
	}

	[Test]
	public async Task
		Create_BaseClassWithVirtualCallsInConstructor_WithUseBaseClassAsDefaultValue_ShouldUseBaseClassValuesInConstructor()
	{
		MyServiceBaseWithVirtualCallsInConstructor sut = MyServiceBaseWithVirtualCallsInConstructor.CreateMock();

		int value = sut.VirtualProperty;

		await That(sut.Mock.Verify.VirtualMethod()).Once();
		await That(value).IsEqualTo(1);
	}

	[Test]
	public async Task Create_SealedClass_ImplementingAdditionalInterface_ShouldThrowMockException()
	{
		void Act()
		{
#pragma warning disable Mockolate0002
			_ = MySealedClass.CreateMock().Implementing<IMyService>();
#pragma warning restore Mockolate0002
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"This method should not be called directly. Either 'Mockolate.Tests.MockTests+MySealedClass' is not mockable or the source generator did not run correctly.");
	}

	[Test]
	public async Task Create_SealedClass_ShouldThrowMockException()
	{
		void Act()
		{
#pragma warning disable Mockolate0002
			_ = MySealedClass.CreateMock();
#pragma warning restore Mockolate0002
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"This method should not be called directly. Either 'Mockolate.Tests.MockTests+MySealedClass' is not mockable or the source generator did not run correctly.");
	}

	[Test]
	public async Task Create_WithConstructorParameters_SealedClass_ShouldThrowMockException()
	{
		void Act()
		{
#pragma warning disable Mockolate0002
			_ = MySealedClass.CreateMock([]);
#pragma warning restore Mockolate0002
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"This method should not be called directly. Either 'Mockolate.Tests.MockTests+MySealedClass' is not mockable or the source generator did not run correctly.");
	}

	[Test]
	public async Task Create_WithConstructorParametersAndSetups_ShouldApplySetups()
	{
		MyBaseClassWithConstructor mock = MyBaseClassWithConstructor.CreateMock(
			setup => setup.VirtualMethod().Returns("bar"),
			["foo",]);

		string result = mock.VirtualMethod();

		await That(result).IsEqualTo("bar");
	}

	[Test]
	public async Task Create_WithConstructorParametersMockBehaviorAndSetups_ShouldApplySetups()
	{
		MyBaseClassWithConstructor mock = MyBaseClassWithConstructor.CreateMock(
			MockBehavior.Default,
			setup => setup.VirtualMethod().Returns("bar"),
			["foo",]);

		string result = mock.VirtualMethod();

		await That(result).IsEqualTo("bar");
	}

	[Test]
	public async Task Create_WithMatchingParameters_ShouldCreateMock()
	{
		MyBaseClassWithConstructor Act()
		{
			return _ = MyBaseClassWithConstructor.CreateMock(["foo",]);
		}

		await That(Act).DoesNotThrow().WhoseResult.IsNotNull();
	}

	[Test]
	public async Task Create_WithMockBehavior_SealedClass_ShouldThrowMockException()
	{
		void Act()
		{
#pragma warning disable Mockolate0002
			_ = MySealedClass.CreateMock(MockBehavior.Default);
#pragma warning restore Mockolate0002
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"This method should not be called directly. Either 'Mockolate.Tests.MockTests+MySealedClass' is not mockable or the source generator did not run correctly.");
	}

	[Test]
	public async Task Create_WithRequiredParameters_WithEmptyParameters_ShouldThrowMockException()
	{
		void Act()
		{
			_ = MyBaseClassWithConstructor.CreateMock([]);
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"No parameterless constructor found for 'Mockolate.Tests.MockTests.MyBaseClassWithConstructor'. Please provide constructor parameters.");
	}

	[Test]
	public async Task Create_WithRequiredParameters_WithoutParameters_ShouldThrowMockException()
	{
		void Act()
		{
			_ = MyBaseClassWithConstructor.CreateMock();
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"No parameterless constructor found for 'Mockolate.Tests.MockTests.MyBaseClassWithConstructor'. Please provide constructor parameters.");
	}

	[Test]
	public async Task Create_WithSetups_ShouldAllowChangingTheSetupSubjectInCallback()
	{
		IChocolateDispenser mock = IChocolateDispenser.CreateMock(setup => setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
			.Do((s, i) => ((IChocolateDispenser)setup)[s] -= i));

		mock["Dark"] = 10;
		mock.Dispense("Dark", 3);
		int remaining = mock["Dark"];

		await That(remaining).IsEqualTo(7);
	}

	[Test]
	public async Task Create_WithSetups_ShouldApplySetups()
	{
		IMyService mock = IMyService.CreateMock(setup =>
		{
			setup.Multiply(It.Is(1), It.IsAny<int?>()).Returns(2);
			setup.Multiply(It.Is(2), It.IsAny<int?>()).Returns(4);
			setup.Multiply(It.Is(3), It.IsAny<int?>()).Returns(8);
		});

		int result1 = mock.Multiply(1, null);
		int result2 = mock.Multiply(2, null);
		int result3 = mock.Multiply(3, null);

		await That(result1).IsEqualTo(2);
		await That(result2).IsEqualTo(4);
		await That(result3).IsEqualTo(8);
	}

	[Test]
	public async Task Create_WithTooManyParameters_ShouldThrowMockException()
	{
		void Act()
		{
			_ = MyBaseClassWithConstructor.CreateMock(["foo", 1, 2,]);
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"Could not find any constructor for 'Mockolate.Tests.MockTests.MyBaseClassWithConstructor' that matches the 3 given parameters (foo, 1, 2).");
	}

	[Test]
	public async Task DoubleNestedInterfaces_ShouldStillWork()
	{
		Nested.Nested2.IMyDoubleNestedService sut = Nested.Nested2.IMyDoubleNestedService.CreateMock();
		sut.Mock.Setup.IsValid.InitializeWith(true);

		bool result = sut.IsValid;

		await That(result).IsTrue();
	}

	[Test]
	public async Task GenericMethodWithWhereClause_ShouldWork()
	{
		IMyServiceWithGenericMethodsWithWhereClause sut = IMyServiceWithGenericMethodsWithWhereClause.CreateMock();

		sut.Mock.Setup.MyMethod<IChocolateDispenser>(It.IsTrue()).Returns(3);

		int result = sut.MyMethod<IChocolateDispenser>(true);

		await That(result).IsEqualTo(3);
		await That(sut.Mock.Verify.MyMethod<IChocolateDispenser>(It.IsTrue())).Once();
	}

	[Test]
	public async Task GenericMethodWithWhereClause_WhenImplementingAdditionalInterface_ShouldWork()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock()
			.Implementing<IMyServiceWithGenericMethodsWithWhereClause>();
		IMyServiceWithGenericMethodsWithWhereClause service = (IMyServiceWithGenericMethodsWithWhereClause)sut;

		sut.Mock.As<IMyServiceWithGenericMethodsWithWhereClause>().Setup.MyMethod<IChocolateDispenser>(It.IsTrue()).Returns(3);

		int result = service.MyMethod<IChocolateDispenser>(true);

		await That(result).IsEqualTo(3);
		await That(sut.Mock.As<IMyServiceWithGenericMethodsWithWhereClause>().Verify.MyMethod<IChocolateDispenser>(It.IsTrue())).Once();
	}

	[Test]
	public async Task ToString_ShouldReturnImplementedType()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		string result = ((IMock)sut).ToString();

		await That(result).IsEqualTo("Mockolate.Tests.TestHelpers.IChocolateDispenser mock");
	}

	[Test]
	public async Task ToString_WithAdditionalImplementations_ShouldReturnImplementedType()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock().Implementing<IMyService>();

		string result = ((IMock)sut).ToString();

		await That(result).IsEqualTo("Mockolate.Tests.TestHelpers.IChocolateDispenser mock that also implements Mockolate.Tests.MockTests.IMyService");
	}

	[Test]
	public async Task TypeWithMockRegistryMembers_ShouldUseUniqueName()
	{
		IServiceWithMockRegistryMembers sut = IServiceWithMockRegistryMembers.CreateMock();
		sut.Mock.Setup.MockRegistry_1.Returns("foo");

		string result = sut.MockRegistry_1;

		await That(result).IsEqualTo("foo");
	}

	[Test]
	public async Task WhenTypeHasMockAndMockolate_MockProperty_ShouldAppendNumbers()
	{
		MyInterfaceWithMockAndMockolate_MockProperty sut = MyInterfaceWithMockAndMockolate_MockProperty.CreateMock();

		sut.Mockolate_Mock__2.Setup.Mock.Returns(4);
		sut.Mockolate_Mock__2.Setup.Mockolate_Mock.Returns(true);
		sut.Mockolate_Mock__2.Setup.Mockolate_Mock__1.Returns("foo");

		await That(sut.Mock).IsEqualTo(4);
		await That(sut.Mockolate_Mock).IsTrue();
		await That(sut.Mockolate_Mock__1).IsEqualTo("foo");
	}

	[Test]
	public async Task WhenTypeHasMockProperty_ShouldUseMockolate_MockInstead()
	{
		MyInterfaceWithMockProperty sut = MyInterfaceWithMockProperty.CreateMock();

		sut.Mockolate_Mock.Setup.Mock.Returns(4);

		int result = sut.Mock;

		await That(result).IsEqualTo(4);
	}

	[Test]
	public async Task WithConstructorParameters_ShouldBeAccessibleViaMock()
	{
		MyBaseClassWithConstructor sut = MyBaseClassWithConstructor.CreateMock(["foo",]);

		IMock mock = (IMock)sut;

		await That(mock.MockRegistry.ConstructorParameters).HasCount(1).And.Contains("foo");
	}

	[Test]
	public async Task WithoutConstructorParameters_MockConstructorParametersShouldBeNull()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		IMock mock = (IMock)sut;

		await That(mock.MockRegistry.ConstructorParameters).IsNull();
	}

	public interface MyInterfaceWithMockProperty
	{
		int Mock { get; }
	}

	public interface MyInterfaceWithMockAndMockolate_MockProperty
	{
		int Mock { get; }
		bool Mockolate_Mock { get; }
		string Mockolate_Mock__1 { get; }
	}

	public class MyServiceBaseWithVirtualCallsInConstructor
	{
		// ReSharper disable VirtualMemberCallInConstructor
		public MyServiceBaseWithVirtualCallsInConstructor()
		{
			int[] values = VirtualMethod();
			VirtualProperty = values[0];
		}
		// ReSharper restore VirtualMemberCallInConstructor

		public virtual int VirtualProperty { get; set; }

		public virtual int[] VirtualMethod() => [1, 2,];
	}

	public class MyServiceBaseWithMultipleConstructors
	{
		// ReSharper disable once UnusedParameter.Local
		public MyServiceBaseWithMultipleConstructors(int initialValue)
		{
		}

		// ReSharper disable once UnusedParameter.Local
		public MyServiceBaseWithMultipleConstructors(DateTime initialValue)
		{
		}
	}

	public interface IMyService
	{
		bool? IsValid { get; set; }
		int Counter { get; set; }

		int Multiply(int value, int? multiplier);

		int Subtract(int minuend, int? subtrahend);

		int Subtract(int minuend, int? subtrahend, bool flag);

		void SetIsValid(bool isValid, Func<bool>? predicate);
	}

	public class MyBaseClassWithConstructor
	{
		public MyBaseClassWithConstructor(string text)
		{
			Text = text;
		}

		// ReSharper disable once UnassignedGetOnlyAutoProperty
		public int Number { get; }
		public string Text { get; }
		public virtual string VirtualMethod() => Text;
	}

	public class MyBaseClassWithoutConstructor
	{
		private MyBaseClassWithoutConstructor()
		{
		}

		// ReSharper disable once UnassignedGetOnlyAutoProperty
		public int Number { get; }
	}

	public class MyBaseClassWithMultipleConstructors
	{
		public MyBaseClassWithMultipleConstructors(string text)
		{
			Text = text;
		}

		public MyBaseClassWithMultipleConstructors(int number, string text = "default")
		{
			Number = number;
			Text = text;
		}

		public int Number { get; }
		public string Text { get; }
		public virtual string VirtualMethod() => Text;
	}

	public class MyBaseClassWithDecimalDefault
	{
		public MyBaseClassWithDecimalDefault(decimal price = 19.95m)
		{
			Price = price;
		}

		public decimal Price { get; }
	}

	public interface IMyServiceWithGenericMethodsWithWhereClause
	{
		int MyMethod<T>(bool flag) where T : IChocolateDispenser;
	}

	public interface IServiceWithMockRegistryMembers
	{
		// ReSharper disable once InconsistentNaming
		string MockRegistry_1 { get; }
		event EventHandler MockRegistry;
		int MockolateMockRegistry(bool value);
	}

	public sealed class Nested
	{
		public sealed class Nested2
		{
			public interface IMyDoubleNestedService
			{
				bool IsValid { get; }
			}
		}
	}

	public sealed class MySealedClass
	{
	}
}
