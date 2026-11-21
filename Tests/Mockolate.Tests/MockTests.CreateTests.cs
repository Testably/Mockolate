using Mockolate.Exceptions;
using Mockolate.Tests.TestHelpers;
#if NET10_0_OR_GREATER
using System.Net.Http;
using Mockolate.Tests.GeneratorCoverage;
#endif

namespace Mockolate.Tests;

public sealed partial class MockTests
{
	public sealed class CreateTests
	{
		[Test]
		public async Task TypedOverload_ConstructorWithDecimalDefault_ShouldForwardExplicitValue()
		{
			// This test exercises the generator's default-value emission end-to-end: if the
			// generator dropped the 'm' suffix ("decimal price = 19.95"), the generated source
			// would fail to compile, taking the whole test project down with it.
			MyBaseClassWithDecimalDefault sut = MyBaseClassWithDecimalDefault.CreateMock(42.50m);

			await That(sut.Price).IsEqualTo(42.50m);
		}

		[Test]
		public async Task TypedOverload_ConstructorWithDefaultValue_ShouldApplyDefault()
		{
			MyBaseClassWithMultipleConstructors sut = MyBaseClassWithMultipleConstructors.CreateMock(42);

			await That(sut.Text).IsEqualTo("default");
			await That(sut.Number).IsEqualTo(42);
		}

		[Test]
		public async Task TypedOverload_MultipleConstructors_ShouldDispatchToMatchingConstructor()
		{
			MyBaseClassWithMultipleConstructors sutFromString =
				MyBaseClassWithMultipleConstructors.CreateMock("foo");
			MyBaseClassWithMultipleConstructors sutFromIntAndString =
				MyBaseClassWithMultipleConstructors.CreateMock(42, "bar");

			await That(sutFromString.Text).IsEqualTo("foo");
			await That(sutFromString.Number).IsEqualTo(0);
			await That(sutFromIntAndString.Text).IsEqualTo("bar");
			await That(sutFromIntAndString.Number).IsEqualTo(42);
		}

		[Test]
		public async Task TypedOverload_SingleConstructor_ShouldForwardToBaseClass()
		{
			MyBaseClassWithConstructor sut = MyBaseClassWithConstructor.CreateMock("foo");

			await That(sut.Text).IsEqualTo("foo");
		}

		[Test]
		public async Task TypedOverload_WithMockBehavior_ShouldUseBehavior()
		{
			MockBehavior behavior = MockBehavior.Default.ThrowingWhenNotSetup();

			MyBaseClassWithConstructor sut = MyBaseClassWithConstructor.CreateMock(behavior, "foo");

			await That(((IMock)sut).MockRegistry.Behavior).IsSameAs(behavior);
			await That(sut.Text).IsEqualTo("foo");
		}

		[Test]
		public async Task TypedOverload_WithMockBehaviorAndSetup_ShouldApplyBoth()
		{
			MockBehavior behavior = MockBehavior.Default;

			MyBaseClassWithConstructor sut = MyBaseClassWithConstructor.CreateMock(
				behavior,
				setup => setup.VirtualMethod().Returns("bar"),
				"foo");

			await That(((IMock)sut).MockRegistry.Behavior).IsSameAs(behavior);
			await That(sut.VirtualMethod()).IsEqualTo("bar");
		}

		[Test]
		public async Task TypedOverload_WithSetup_ShouldApplySetup()
		{
			MyBaseClassWithConstructor sut = MyBaseClassWithConstructor.CreateMock(
				setup => setup.VirtualMethod().Returns("bar"),
				"foo");

			await That(sut.VirtualMethod()).IsEqualTo("bar");
		}

		[Test]
		public async Task With2Arguments_OnlyFirstArgumentIsClass_ShouldForwardBehaviorToBaseClass()
		{
			MockBehavior behavior = MockBehavior.Default.ThrowingWhenNotSetup();

			MyServiceBase sut = MyServiceBase.CreateMock(behavior).Implementing<IMyService>();

			await That(((IMock)sut).MockRegistry.Behavior).IsSameAs(behavior);
		}

		[Test]
		public async Task With2Arguments_WithConstructorParametersAndSetups_ShouldApplySetups()
		{
			MyBaseClassWithConstructor mock = MyBaseClassWithConstructor.CreateMock(
				setup => setup.VirtualMethod().Returns("bar"),
				["foo",]).Implementing<IMyService>();

			string result = mock.VirtualMethod();

			await That(result).IsEqualTo("bar");
		}

		[Test]
		public async Task With2Arguments_WithConstructorParametersMockBehaviorAndSetups_ShouldApplySetups()
		{
			MyBaseClassWithConstructor sut = MyBaseClassWithConstructor.CreateMock(
					MockBehavior.Default,
					setup => setup.VirtualMethod().Returns("bar"),
					["foo",])
				.Implementing<IMyService>();

			string result = sut.VirtualMethod();

			await That(result).IsEqualTo("bar");
		}

		[Test]
		public async Task With2Arguments_WithSetups_ShouldApplySetups()
		{
			IMyService sut = IMyService.CreateMock(setup =>
			{
				setup.Multiply(It.Is(1), It.IsAny<int?>()).Returns(2);
				setup.Multiply(It.Is(2), It.IsAny<int?>()).Returns(4);
				setup.Multiply(It.Is(3), It.IsAny<int?>()).Returns(8);
			});

			int result1 = sut.Multiply(1, null);
			int result2 = sut.Multiply(2, null);
			int result3 = sut.Multiply(3, null);

			await That(result1).IsEqualTo(2);
			await That(result2).IsEqualTo(4);
			await That(result3).IsEqualTo(8);
		}

		[Test]
		public async Task WithAdditionalInterfacesFromDifferentNamespaces_ShouldHaveUniqueName()
		{
			int invocationCount1 = 0;
			int invocationCount2 = 0;
			IChocolateDispenser sut1 = IChocolateDispenser.CreateMock().Implementing<TestHelpers.IMyService>();
			IChocolateDispenser sut2 = IChocolateDispenser.CreateMock().Implementing<TestHelpers.Other.IMyService>();

			sut1.Mock.As<TestHelpers.IMyService>().Setup
				.DoSomething(It.IsAny<int>())
				.Do(() => invocationCount1++);
			sut2.Mock.As<TestHelpers.Other.IMyService>().Setup
				.DoSomething(It.IsAny<int>())
				.Do(() => invocationCount2++);

			((TestHelpers.IMyService)sut1).DoSomething(1);
			((TestHelpers.IMyService)sut1).DoSomething(2);
			((TestHelpers.Other.IMyService)sut2).DoSomething(1);
			((TestHelpers.Other.IMyService)sut2).DoSomething(2);
			((TestHelpers.Other.IMyService)sut2).DoSomething(3);

			await That(invocationCount1).IsEqualTo(2);
			await That(invocationCount2).IsEqualTo(3);
			await That(sut1.Mock.As<TestHelpers.IMyService>().Verify
				.DoSomething(It.IsAny<int>())).Exactly(2);
			await That(() => sut1.Mock.As<TestHelpers.Other.IMyService>())
				.Throws<MockException>().WithMessage("The subject does not support type Mockolate.Tests.TestHelpers.Other.IMyService.");
			await That(() => sut2.Mock.As<TestHelpers.IMyService>())
				.Throws<MockException>().WithMessage("The subject does not support type Mockolate.Tests.TestHelpers.IMyService.");
			await That(sut2.Mock.As<TestHelpers.Other.IMyService>().Verify
				.DoSomething(It.IsAny<int>())).Exactly(3);
		}
	}

#if NET10_0_OR_GREATER
	/// <summary>
	///     Compile-and-create coverage for every special case in
	///     <c>Source/Mockolate.SourceGenerators</c>. The example types in this folder
	///     are designed so that this project compiling is itself the primary regression
	///     gate — the source generator must successfully process every shape — and each
	///     fact below additionally proves that <c>CreateMock</c> doesn't throw at runtime.
	/// </summary>
	public sealed class CreateSnapshotTests
	{
		[Test]
		public async Task AutoMock_CanBeCreated()
		{
			IAutoMockRoot sut1 = IAutoMockRoot.CreateMock();
			AutoMockChildFactory sut2 = AutoMockChildFactory.CreateMock();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
		}

		[Test]
		public async Task BaseClass_WithMultipleAdditionalInterfaces_CanBeCreated()
		{
			ComprehensiveAbstractClass sut = ComprehensiveAbstractClass.CreateMock()
				.Implementing<ICombinationMockA>()
				.Implementing<ICombinationMockB>();
			await That(sut).IsNotNull();
		}

		[Test]
		public async Task ClosedGeneric_CanBeCreated()
		{
			IClosedGeneric<int> sut1 = IClosedGeneric<int>.CreateMock();
			ClosedGenericBase<string> sut2 = ClosedGenericBase<string>.CreateMock();
			ClosedGenericDelegate<int, string> sut3 = ClosedGenericDelegate<int, string>.CreateMock();
			IClosedGeneric<int> sut4 = IClosedGeneric<int>.CreateMock().Implementing<IClosedGeneric<string>>();
			IClosedGenericDerived sut5 = IClosedGenericDerived.CreateMock();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
			await That(sut3).IsNotNull();
			await That(sut4).IsNotNull();
			await That(sut5).IsNotNull();
		}

		[Test]
		public async Task ComprehensiveAbstractClass_CanBeCreated()
		{
			ComprehensiveAbstractClass sut = ComprehensiveAbstractClass.CreateMock();
			await That(sut).IsNotNull();
		}

		[Test]
		public async Task ComprehensiveDelegate_CanBeCreated()
		{
			ComprehensiveDelegate sut = ComprehensiveDelegate.CreateMock();
			await That(sut).IsNotNull();
		}

		[Test]
		public async Task ComprehensiveInterface_CanBeCreated()
		{
			IComprehensiveInterface sut = IComprehensiveInterface.CreateMock();
			// Casts to object instead of `await That(sut)` because CS8920 forbids interfaces
			// with unimplemented static abstract members as generic type arguments.
			await That((object)sut).IsNotNull();
		}

		[Test]
		public async Task EnumDefaultValues_CanBeCreated()
		{
			IEnumDefaultValues sut1 = IEnumDefaultValues.CreateMock();
			EnumDefaultValuesDelegate sut2 = EnumDefaultValuesDelegate.CreateMock();
			EnumDefaultValuesBase sut3 = EnumDefaultValuesBase.CreateMock();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
			await That(sut3).IsNotNull();
		}

		[Test]
		public async Task EqualsMembers_CanBeCreated()
		{
			IEqualsOnly sut1 = IEqualsOnly.CreateMock();
			IEqualsOnlyDerived sut2 = IEqualsOnlyDerived.CreateMock();
			IEqualsNotNull sut3 = IEqualsNotNull.CreateMock();
			IEqualsAndGetHashCode sut4 = IEqualsAndGetHashCode.CreateMock();
			EqualsOnlyBase sut5 = EqualsOnlyBase.CreateMock();
			IEqualsOnly sut6 = IEqualsOnly.CreateMock().Implementing<IEqualsAndGetHashCode>();
			HiddenGetHashCodeBase sut7 = HiddenGetHashCodeBase.CreateMock();
			HiddenGetHashCodeBase sut8 = HiddenGetHashCodeBase.CreateMock().Implementing<IEqualsAndGetHashCode>();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
			await That(sut3).IsNotNull();
			await That(sut4).IsNotNull();
			await That(sut5).IsNotNull();
			await That(sut6).IsNotNull();
			await That(sut7).IsNotNull();
			await That(sut8).IsNotNull();
		}

		[Test]
		public async Task GenericMethodClosedOverObject_CanBeCreated()
		{
			IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
			await That(sut).IsNotNull();
		}

		[Test]
		public async Task GenericParameterDefaultValues_CanBeCreated()
		{
			IGenericParameterDefaultValues sut1 = IGenericParameterDefaultValues.CreateMock();
			GenericParameterDefaultValuesBase sut2 = GenericParameterDefaultValuesBase.CreateMock();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
		}

		[Test]
		public async Task HttpClient_CanBeCreated()
		{
			HttpClient sut = HttpClient.CreateMock();
			await That(sut).IsNotNull();
		}

		[Test]
		public async Task KeywordEdgeCases_CanBeCreated()
		{
			IKeywordEdgeCases sut1 = IKeywordEdgeCases.CreateMock();
			KeywordEdgeCasesBase sut2 = KeywordEdgeCasesBase.CreateMock("class", 1);
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
		}

		[Test]
		public async Task MethodConstrainedToTypeArgument_CanBeCreated()
		{
			IMethodConstrainedToTypeArgument<int> sut1 = IMethodConstrainedToTypeArgument<int>.CreateMock();
			IMethodConstrainedToTypeArgument<string> sut2 = IMethodConstrainedToTypeArgument<string>.CreateMock();
			MethodConstrainedToTypeArgumentBase<string> sut3 = MethodConstrainedToTypeArgumentBase<string>.CreateMock();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
			await That(sut3).IsNotNull();
		}

		[Test]
		public async Task NullableMethodTypeParameter_CanBeCreated()
		{
			INullableMethodTypeParameter sut = INullableMethodTypeParameter.CreateMock();
			await That(sut).IsNotNull();
		}

		[Test]
		public async Task NullableTypeParameter_CanBeCreated()
		{
			INullableTypeParameter<int> sut1 = INullableTypeParameter<int>.CreateMock();
			INullableTypeParameter<string> sut2 = INullableTypeParameter<string>.CreateMock();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
		}

		[Test]
		public async Task ObjectLookAlikes_CanBeCreated()
		{
			IObjectLookAlikes sut1 = IObjectLookAlikes.CreateMock();
			IObjectLookAlikesDerived sut2 = IObjectLookAlikesDerived.CreateMock();
			IVoidObjectLookAlikes sut3 = IVoidObjectLookAlikes.CreateMock();
			IRefObjectLookAlikes sut4 = IRefObjectLookAlikes.CreateMock();
			IObjectLookAlikeProperties sut5 = IObjectLookAlikeProperties.CreateMock();
			IStaticObjectLookAlikes sut6 = IStaticObjectLookAlikes.CreateMock();
			IVoidObjectLookAlikes sut7 = IVoidObjectLookAlikes.CreateMock().Implementing<IObjectLookAlikes>();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
			await That(sut3).IsNotNull();
			await That(sut4).IsNotNull();
			await That(sut5).IsNotNull();
			// Casts to object instead of `await That(sut6)` because CS8920 forbids interfaces
			// with unimplemented static abstract members as generic type arguments.
			await That((object)sut6).IsNotNull();
			await That(sut7).IsNotNull();
		}

		[Test]
		public async Task OptionalConstructorParameters_CanBeCreated()
		{
			OptionalConstructorBase sut1 = OptionalConstructorBase.CreateMock();
			OptionalAndParameterlessConstructorBase sut2 = OptionalAndParameterlessConstructorBase.CreateMock();
			SeveralOptionalConstructorsBase sut3 = SeveralOptionalConstructorsBase.CreateMock();
			OptionalConstructorBase sut4 = OptionalConstructorBase.CreateMock().Implementing<ICombinationMockA>();
			ParamsConstructorBase sut5 = ParamsConstructorBase.CreateMock();
			OptionalAndParamsConstructorBase sut6 = OptionalAndParamsConstructorBase.CreateMock();
			ParamsConstructorSelectionBase sut7 = ParamsConstructorSelectionBase.CreateMock();
			OptionalOverParamsConstructorBase sut8 = OptionalOverParamsConstructorBase.CreateMock();
			OptionalAndParamsConstructorBase sut9 = OptionalAndParamsConstructorBase.CreateMock().Implementing<ICombinationMockA>();
			ParamsCollectionConstructorBase sut10 = ParamsCollectionConstructorBase.CreateMock();
			ParamsSpanConstructorBase sut11 = ParamsSpanConstructorBase.CreateMock();
			OnlyParamsSpanConstructorBase sut12 = OnlyParamsSpanConstructorBase.CreateMock().Implementing<ICombinationMockA>();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
			await That(sut3).IsNotNull();
			await That(sut4).IsNotNull();
			await That(sut5).IsNotNull();
			await That(sut6).IsNotNull();
			await That(sut7).IsNotNull();
			await That(sut8).IsNotNull();
			await That(sut9).IsNotNull();
			await That(sut10).IsNotNull();
			await That(sut11).IsNotNull();
			await That(sut12).IsNotNull();
		}

		[Test]
		public async Task OptionalMethodParameters_CanBeCreated()
		{
			IOptionalMethodParameters sut1 = IOptionalMethodParameters.CreateMock();
			OptionalParametersDelegate sut2 = OptionalParametersDelegate.CreateMock();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
		}

		[Test]
		public async Task OptionalIndexerKeys_CanBeCreated()
		{
			IOptionalIndexerKeys sut1 = IOptionalIndexerKeys.CreateMock();
			IOptionalIndexerKeysGetterOnly sut2 = IOptionalIndexerKeysGetterOnly.CreateMock();
			IOptionalIndexerKeysSetterOnly sut3 = IOptionalIndexerKeysSetterOnly.CreateMock();
			IOptionalIndexerKeysWithSixKeys sut4 = IOptionalIndexerKeysWithSixKeys.CreateMock();
			IOptionalIndexerKeysWithSibling sut5 = IOptionalIndexerKeysWithSibling.CreateMock();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
			await That(sut3).IsNotNull();
			await That(sut4).IsNotNull();
			await That(sut5).IsNotNull();
		}

		[Test]
		public async Task ParameterNameCollisions_CanBeCreated()
		{
			IParameterNameCollisions sut1 = IParameterNameCollisions.CreateMock();
			ParameterNameCollisionsBase sut2 = ParameterNameCollisionsBase.CreateMock();
			ParameterNameCollisionsDelegate sut3 = ParameterNameCollisionsDelegate.CreateMock();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
			await That(sut3).IsNotNull();
		}

		[Test]
		public async Task ParamsObjectArray_CanBeCreated()
		{
			IParamsObjectArray sut1 = IParamsObjectArray.CreateMock();
			ParamsObjectArrayDelegate sut2 = ParamsObjectArrayDelegate.CreateMock();
			ParamsObjectArrayOnlyDelegate sut3 = ParamsObjectArrayOnlyDelegate.CreateMock();
			ParamsArrayDelegate sut4 = ParamsArrayDelegate.CreateMock();
			await That(sut1).IsNotNull();
			await That(sut2).IsNotNull();
			await That(sut3).IsNotNull();
			await That(sut4).IsNotNull();
		}

		[Test]
		public async Task RefStructConsumer_CanBeCreated()
		{
			IRefStructConsumer sut = IRefStructConsumer.CreateMock();
			await That(sut).IsNotNull();
		}

		[Test]
		public async Task StaticAbstractMembers_CanBeCreated()
		{
			IStaticAbstractMembers sut = IStaticAbstractMembers.CreateMock();
			// Casts to object instead of `await That(sut)` because CS8920 forbids interfaces
			// with unimplemented static abstract members as generic type arguments.
			await That((object)sut).IsNotNull();
		}
	}
#endif
}
