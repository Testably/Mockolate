using System.Collections.Generic;
using Mockolate.Exceptions;
using Mockolate.Tests.GeneratorCoverage;
using Mockolate.Verify;

namespace Mockolate.Tests;

public sealed class ConstructorDependencyTests
{
	[Test]
	public async Task CreateMock_WithConstructorDependencies_ShouldMockThemAndUseTheDefaults()
	{
		ConstructorDependencyService sut = ConstructorDependencyService.CreateMock();

		await That(sut.Dependency).IsSameAs(sut.Mock.ConstructorArgument<IConstructorDependency>());
		await That(sut.Name).IsNull();
		await That(sut.Retries).IsEqualTo(3);
		await That(sut.Factor).IsEqualTo(1.5m);
		await That(sut.Tags).IsEmpty();
	}

	[Test]
	public async Task CreateMock_WithConstructorDependencies_ShouldAllowSetupsOfTheDependencyAndTheClass()
	{
		ConstructorDependencyService sut = ConstructorDependencyService.CreateMock();
		sut.Mock.ConstructorArgument<IConstructorDependency>().Mock.Setup.GetValue().Returns(2);

		int calculated = sut.Calculate();
		sut.Mock.ConstructorArgument<IConstructorDependency>().Mock.Verify.GetValue().Once();
		sut.Mock.Setup.Calculate().Returns(42);

		await That(calculated).IsEqualTo(6).Because("the class mock calls the base implementation by default");
		await That(sut.Calculate()).IsEqualTo(42);
	}

	[Test]
	public async Task CreateMock_WithMockBehavior_ShouldForwardItToTheDependencies()
	{
		ConstructorDependencyService sut =
			ConstructorDependencyService.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

		void Act()
			=> sut.Dependency.GetValue();

		await That(Act).Throws<MockNotSetupException>();
	}

	[Test]
	public async Task CreateMock_WithValuesThatCannotBeMocked_ShouldUseTheDefaultValuesOfTheBehavior()
	{
		ConstructorDependencyValues sut = ConstructorDependencyValues.CreateMock(MockBehavior.Default
			.WithDefaultValueFor<int>(() => 42));

		await That(sut.Text).IsEqualTo("");
		await That(sut.Number).IsEqualTo(42);
		await That(sut.Values).IsEmpty();
	}

	[Test]
	public async Task CreateMock_WithDefaultValueFactory_ShouldCallItOncePerParameter()
	{
		int calls = 0;

		ConstructorDependencyValues sut = ConstructorDependencyValues.CreateMock(MockBehavior.Default
			.WithDefaultValueFor<int>(() => ++calls));

		await That(sut.Number).IsEqualTo(1);
		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task CreateMock_WithOutParameterAndDefaultValueFactory_ShouldCallItOncePerParameter()
	{
		int calls = 0;

		ConstructorDependencySealed sut = ConstructorDependencySealed.CreateMock(out _, MockBehavior.Default
			.WithDefaultValueFor<int>(() => ++calls));

		await That(sut.Number).IsEqualTo(1);
		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task CreateMock_WithGenericClass_ShouldRecordTheArgumentsOfItsTypeArgument()
	{
		ConstructorDependencyGeneric<int> sut = ConstructorDependencyGeneric<int>.CreateMock(MockBehavior.Default
			.WithDefaultValueFor<int>(() => 7));

		await That(sut.Mock.ConstructorArgument<int>()).IsEqualTo(7);
		await That(sut.Mock.ConstructorArgument<IConstructorDependency>()).IsSameAs(sut.Dependency);
	}

	[Test]
	public async Task CreateMock_WithOutParameterAndNullForNullableParameterWithDefault_ShouldPassNull()
	{
		ConstructorDependencySealedNullableDefault sut = ConstructorDependencySealedNullableDefault.CreateMock(out _,
			title: null);

		await That(sut.Title).IsNull();
	}

	[Test]
	public async Task CreateMock_WithOutParameterOnSealedClassesWithTheSameParameters_ShouldCreateBoth()
	{
		ConstructorDependencySealedNamed first = ConstructorDependencySealedNamed.CreateMock(out _);
		ConstructorDependencySealedTwin second = ConstructorDependencySealedTwin.CreateMock(out _);

		await That(first.Dependency).IsNotSameAs(second.Dependency);
	}

	[Test]
	public async Task CreateMock_WithOutParameterAndParametersNamedLikeTheLocals_ShouldPassThem()
	{
		ConstructorDependencySealedLocalNames sut = ConstructorDependencySealedLocalNames.CreateMock(
			out ConstructorArguments arguments, value0: 1, argument0: 2, created0: 3, Signature: 4);

		await That(sut.Sum).IsEqualTo(10);
		await That(arguments.Get<int>("Signature")).IsEqualTo(4);
	}

	[Test]
	public async Task CreateMock_WithOutArgumentPassedByNameLater_ShouldCreateTheSealedClass()
	{
		ConstructorDependencySealedNamed sut = ConstructorDependencySealedNamed.CreateMock(mockBehavior: MockBehavior.Default,
			constructorArguments: out ConstructorArguments arguments);

		await That(sut.Dependency).IsSameAs(arguments.Get<IConstructorDependency>());
	}

	[Test]
	public async Task CreateMock_WithoutDefaultValueForADependency_ShouldThrowMockException()
	{
		void Act()
			=> ConstructorDependencyUnmockable.CreateMock();

		await That(Act).Throws<MockException>()
			.WithMessage("There is no argument for the constructor parameter 'unmockable' of *ConstructorDependencyUnmockable. Pass the constructor parameters, or register a default value for its type with WithDefaultValueFor.").AsWildcard();
	}

	[Test]
	public async Task CreateMock_WithDefaultValueForADependency_ShouldUseIt()
	{
		UnmockableDependency dependency = new();

		ConstructorDependencyUnmockable sut = ConstructorDependencyUnmockable.CreateMock(MockBehavior.Default
			.WithDefaultValueFor<IConstructorUnmockableDependency>(() => dependency));

		await That(sut.Unmockable).IsSameAs(dependency);
	}

	[Test]
	public async Task CreateMock_WithSeveralConstructors_ShouldFillTheOneWithTheMostParameters()
	{
		ConstructorDependencyOverloads sut = ConstructorDependencyOverloads.CreateMock();

		await That(sut.Dependency).IsSameAs(sut.Mock.ConstructorArgument<IConstructorDependency>());
		await That(sut.Other).IsSameAs(sut.Mock.ConstructorArgument<IConstructorOtherDependency>("other"));
	}

	[Test]
	public async Task CreateMock_WithTiedConstructors_ShouldThrowMockException()
	{
		void Act()
			=> ConstructorDependencyTie.CreateMock();

		await That(Act).Throws<MockException>()
			.WithMessage("No parameterless constructor found for '*ConstructorDependencyTie'. Please provide constructor parameters.").AsWildcard();
	}

	[Test]
	public async Task CreateMock_WithConstructorWithoutArguments_ShouldPreferIt()
	{
		ConstructorDependencyOptional sut = ConstructorDependencyOptional.CreateMock();

		void Act()
			=> sut.Mock.ConstructorArgument<IConstructorDependency>();

		await That(sut.Dependency).IsNull();
		await That(Act).Throws<MockException>()
			.WithMessage("The mock of *ConstructorDependencyOptional was created without constructor arguments.").AsWildcard();
	}

	[Test]
	public async Task CreateMock_WithAbstractClass_ShouldFillTheConstructor()
	{
		ConstructorDependencyAbstract sut = ConstructorDependencyAbstract.CreateMock();
		sut.Mock.Setup.Calculate().Returns(3);

		await That(sut.Dependency).IsSameAs(sut.Mock.ConstructorArgument<IConstructorDependency>());
		await That(sut.Calculate()).IsEqualTo(3);
	}

	[Test]
	public async Task CreateMock_WithNullableGenericDependency_ShouldSetUpTheMock()
	{
		ConstructorDependencyNullableGeneric sut = ConstructorDependencyNullableGeneric.CreateMock();
		sut.Mock.ConstructorArgument<IConstructorGenericDependency<string?>>().Mock.Setup.GetValue().Returns("value");

		await That(sut.Dependency.GetValue()).IsEqualTo("value");
	}

	[Test]
	public async Task CreateMock_WithKeywordParameterName_ShouldFindTheArgumentByItsName()
	{
		ConstructorDependencyKeyword sut = ConstructorDependencyKeyword.CreateMock();

		await That(sut.Mock.ConstructorArgument<IConstructorDependency>("class")).IsSameAs(sut.Class);
	}

	[Test]
	public async Task Implementing_ShouldReuseTheConstructorDependencies()
	{
		ConstructorDependencyService sut = ConstructorDependencyService.CreateMock()
			.Implementing<IConstructorOtherDependency>();

		await That(sut.Dependency).IsSameAs(sut.Mock.ConstructorArgument<IConstructorDependency>());
	}

	[Test]
	public async Task ConstructorArgument_WithExplicitArguments_ShouldReturnThem()
	{
		IConstructorOtherDependency other = IConstructorOtherDependency.CreateMock();

		ConstructorDependencyOverloads sut = ConstructorDependencyOverloads.CreateMock([other,]);

		await That(sut.Dependency).IsNull();
		await That(sut.Mock.ConstructorArgument<IConstructorOtherDependency>()).IsSameAs(other);
	}

	[Test]
	public async Task ConstructorArgument_WithoutArgumentOfTheType_ShouldThrowMockException()
	{
		ConstructorDependencyOverloads sut = ConstructorDependencyOverloads.CreateMock([
			IConstructorOtherDependency.CreateMock(),
		]);

		void Act()
			=> sut.Mock.ConstructorArgument<IConstructorDependency>();

		await That(Act).Throws<MockException>()
			.WithMessage("The constructor *ConstructorDependencyOverloads(IConstructorOtherDependency) received no argument of type *IConstructorDependency.").AsWildcard();
	}

	[Test]
	public async Task ConstructorArgument_WithSeveralArgumentsOfTheType_ShouldThrowMockException()
	{
		ConstructorDependencyTwice sut = ConstructorDependencyTwice.CreateMock();

		void Act()
			=> sut.Mock.ConstructorArgument<IConstructorDependency>();

		await That(Act).Throws<MockException>()
			.WithMessage("The constructor *ConstructorDependencyTwice(IConstructorDependency, IConstructorDependency) received several arguments of type *IConstructorDependency ('first', 'second'). Pass the name of the parameter.").AsWildcard();
	}

	[Test]
	public async Task ConstructorArgument_WithParameterName_ShouldReturnThatArgument()
	{
		ConstructorDependencyTwice sut = ConstructorDependencyTwice.CreateMock();

		await That(sut.Mock.ConstructorArgument<IConstructorDependency>("first")).IsSameAs(sut.First);
		await That(sut.Mock.ConstructorArgument<IConstructorDependency>("second")).IsSameAs(sut.Second);
	}

	[Test]
	public async Task ConstructorArgument_WithUnknownParameterName_ShouldThrowMockException()
	{
		ConstructorDependencyTwice sut = ConstructorDependencyTwice.CreateMock();

		void Act()
			=> sut.Mock.ConstructorArgument<IConstructorDependency>("third");

		await That(Act).Throws<MockException>()
			.WithMessage("The constructor *ConstructorDependencyTwice(IConstructorDependency, IConstructorDependency) has no parameter 'third'.").AsWildcard();
	}

	[Test]
	public async Task ConstructorArgument_WithOtherTypeThanTheArgument_ShouldThrowMockException()
	{
		ConstructorDependencyService sut = ConstructorDependencyService.CreateMock();

		void Act()
			=> sut.Mock.ConstructorArgument<string>("retries");

		await That(Act).Throws<MockException>()
			.WithMessage("The argument of the parameter 'retries' of the constructor *ConstructorDependencyService(*) is of type int, but must be of type string.").AsWildcard();
	}

	[Test]
	public async Task ConstructorArgument_WithNullArgument_ShouldReturnNull()
	{
		ConstructorDependencyService sut = ConstructorDependencyService.CreateMock();

		void Act()
			=> sut.Mock.ConstructorArgument<int>("name");

		await That(sut.Mock.ConstructorArgument<string?>("name")).IsNull();
		await That(Act).Throws<MockException>()
			.WithMessage("The argument of the parameter 'name' of the constructor *ConstructorDependencyService(*) is null, but must be of type int.").AsWildcard();
	}

#if NET9_0_OR_GREATER
	[Test]
	public async Task CreateMock_WithSpanConstructor_ShouldFillTheShorterConstructor()
	{
		ConstructorDependencySpan sut = ConstructorDependencySpan.CreateMock();

		await That(sut.Dependency).IsSameAs(sut.Mock.ConstructorArgument<IConstructorDependency>());
		await That(sut.Text).IsNull();
	}
#endif

	private sealed class UnmockableDependency : IConstructorUnmockableDependency
	{
		[Obsolete("Mocks of obsolete members do not compile without warnings.")]
		public int GetValue()
			=> 0;
	}
	[Test]
	public async Task CreateMock_WithOutParameterOnSealedClass_ShouldCreateARealInstanceWithMocks()
	{
		ConstructorDependencySealed sut = ConstructorDependencySealed.CreateMock(out ConstructorArguments arguments);
		arguments.Get<IConstructorDependency>().Mock.Setup.GetValue().Returns(42);

		await That(sut.GetType()).IsEqualTo(typeof(ConstructorDependencySealed));
		await That(sut.Dependency).IsSameAs(arguments.Get<IConstructorDependency>());
		await That(sut.Dependency.GetValue()).IsEqualTo(42);
		await That(sut.Other).IsSameAs(arguments.Get<IConstructorOtherDependency>("other"));
		await That(sut.Text).IsEqualTo("");
		await That(sut.Number).IsEqualTo(0);
		await That(sut.Description).IsNull();
		await That(sut.Retries).IsEqualTo(3);
		await That(sut.Values).IsEmpty();
	}

	[Test]
	public async Task CreateMock_WithOutParameterAndNamedArguments_ShouldUseThem()
	{
		IConstructorDependency dependency = IConstructorDependency.CreateMock();

		ConstructorDependencySealed sut = ConstructorDependencySealed.CreateMock(out ConstructorArguments arguments,
			dependency: dependency, text: "chocolate", number: 7, description: "dark", retries: 1, values: [1, 2,]);

		await That(sut.Dependency).IsSameAs(dependency);
		await That(arguments.Get<IConstructorDependency>()).IsSameAs(dependency);
		await That(sut.Text).IsEqualTo("chocolate");
		await That(sut.Number).IsEqualTo(7);
		await That(sut.Description).IsEqualTo("dark");
		await That(sut.Retries).IsEqualTo(1);
		await That(sut.Values).IsEqualTo([1, 2,]);
	}

	[Test]
	public async Task CreateMock_WithOutParameterAndBehavior_ShouldUseItForMocksAndValues()
	{
		MockBehavior behavior = MockBehavior.Default.ThrowingWhenNotSetup()
			.WithDefaultValueFor(() => "generated")
			.WithDefaultValueFor(() => 42);

		ConstructorDependencySealed sut = ConstructorDependencySealed.CreateMock(out _, behavior);

		void Act()
			=> sut.Dependency.GetValue();

		await That(Act).Throws<MockNotSetupException>();
		await That(sut.Text).IsEqualTo("generated");
		await That(sut.Number).IsEqualTo(42);
	}

	[Test]
	public async Task CreateMock_WithOutParameterOnRecord_ShouldCreateARealInstance()
	{
		ConstructorDependencyRecord sut = ConstructorDependencyRecord.CreateMock(out ConstructorArguments arguments);

		await That(sut.GetType()).IsEqualTo(typeof(ConstructorDependencyRecord));
		await That(sut.Dependency).IsSameAs(arguments.Get<IConstructorDependency>());
		await That(sut.Name).IsEqualTo("");
	}

	[Test]
	public async Task CreateMock_WithOutParameterWithoutValue_ShouldThrow()
	{
		void Act()
			=> ConstructorDependencySealedRequired.CreateMock(out _);

		await That(Act).Throws<MockException>().WithMessage(
			"There is no argument for the constructor parameter 'address' of Mockolate.Tests.GeneratorCoverage.ConstructorDependencySealedRequired. Pass it, or register a default value for its type with WithDefaultValueFor.");
	}

	[Test]
	public async Task ConstructorArgument_ShouldPreferTheDeclaredTypeOverAConvertibleArgument()
	{
		ConstructorDependencyHierarchy sut = ConstructorDependencyHierarchy.CreateMock();

		await That(sut.Mock.ConstructorArgument<IConstructorBaseDependency>()).IsSameAs(sut.Base);
		await That(sut.Mock.ConstructorArgument<IConstructorDerivedDependency>()).IsSameAs(sut.Derived);
	}

	[Test]
	public async Task CreateMock_WithConfiguredCollectionDefault_ShouldUseIt()
	{
		IEnumerable<int> configured = [42,];

		ConstructorDependencyValues sut =
			ConstructorDependencyValues.CreateMock(MockBehavior.Default.WithDefaultValueFor(() => configured));

		await That(sut.Mock.ConstructorArgument<IEnumerable<int>>()).IsSameAs(configured);
	}
}
