using System.Collections.Generic;
using Mockolate.Exceptions;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests;

public sealed partial class CreateWithMocksTests
{
	[Test]
	public async Task CreateWithMocks_WithOptionalParameters_ShouldUseTheDefaults()
	{
		ICreateWithMocksUnmockableDependency unmockable = new UnmockableDependency();

		CreateWithMocksService sut = CreateWithMocksService.CreateWithMocks(out CreateWithMocksServiceMocks mocks,
			unmockable, null);

		await That(sut.Dependency).IsSameAs(mocks.Dependency);
		await That(sut.Unmockable).IsSameAs(unmockable);
		await That(sut.Retries).IsEqualTo(3);
		await That(sut.Factor).IsEqualTo(1.5m);
		await That(sut.Tags).IsEmpty();
	}

	[Test]
	public async Task CreateWithMocks_WithCollidingParameterNames_ShouldExposeEveryArgument()
	{
		CreateWithMocksNameCollisions sut = CreateWithMocksNameCollisions.CreateWithMocks(
			out CreateWithMocksNameCollisionsMocks mocks_1);

		await That(new[]
			{
				mocks_1.Mocks, mocks_1.MockBehavior, mocks_1.EqualsArgument,
				mocks_1.CreateWithMocksNameCollisionsMocksArgument, mocks_1.Value, mocks_1.ValueArgument,
			})
			.IsEqualTo(sut.All);
	}

	[Test]
	public async Task CreateWithMocks_WithNestedClasses_ShouldUseDistinctContainers()
	{
		CreateWithMocksOuter.Nested nested = CreateWithMocksOuter.Nested.CreateWithMocks(
			out CreateWithMocksOuter_0NestedMocks nestedMocks);
		CreateWithMocksOuter_Nested topLevel = CreateWithMocksOuter_Nested.CreateWithMocks(
			out CreateWithMocksOuter__NestedMocks topLevelMocks);

		await That(nested.Dependency).IsSameAs(nestedMocks.Dependency);
		await That(topLevel.Dependency).IsSameAs(topLevelMocks.Dependency);
	}

	[Test]
	public async Task CreateWithMocks_WithNullableGenericDependency_ShouldSetUpTheMock()
	{
		CreateWithMocksNullableGeneric sut = CreateWithMocksNullableGeneric.CreateWithMocks(
			out CreateWithMocksNullableGenericMocks mocks);
		mocks.Dependency.Mock.Setup.GetValue().Returns("value");

		await That(sut.Dependency.GetValue()).IsEqualTo("value");
	}

	[Test]
	public async Task CreateWithMocks_WithFrameworkDependency_ShouldRequireTheArgument()
	{
		List<int> values = [1, 2,];

		CreateWithMocksFrameworkDependency sut = CreateWithMocksFrameworkDependency.CreateWithMocks(out _, values, 2);

		await That(sut.Values).IsSameAs(values);
		await That(sut.Count).IsEqualTo(2);
	}

	[Test]
	public async Task CreateWithMocks_WithMockBehavior_ShouldForwardItToTheMocks()
	{
		CreateWithMocksService.CreateWithMocks(out CreateWithMocksServiceMocks mocks,
			new UnmockableDependency(), null, MockBehavior.Default.ThrowingWhenNotSetup());

		void Act()
			=> mocks.Dependency.GetValue();

		await That(Act).Throws<MockNotSetupException>();
	}

	[Test]
	public async Task CreateWithMocks_WithOwnInstanceByName_ShouldUseIt()
	{
		ICreateWithMocksDependency dependency = ICreateWithMocksDependency.CreateMock();

		CreateWithMocksService sut = CreateWithMocksService.CreateWithMocks(out CreateWithMocksServiceMocks mocks,
			new UnmockableDependency(), null, dependency: dependency);

		await That(sut.Dependency).IsSameAs(dependency);
		await That(mocks.Dependency).IsSameAs(dependency);
	}

	[Test]
	public async Task CreateWithMocks_WithExplicitNull_ShouldCreateAMock()
	{
		CreateWithMocksService sut = CreateWithMocksService.CreateWithMocks(out CreateWithMocksServiceMocks mocks,
			new UnmockableDependency(), null, dependency: null);

		await That(sut.Dependency).IsNotNull();
		await That(sut.Dependency).IsSameAs(mocks.Dependency);
	}

	[Test]
	public async Task CreateWithMocks_WithParamsValues_ShouldPassThemOn()
	{
		CreateWithMocksService sut = CreateWithMocksService.CreateWithMocks(out CreateWithMocksServiceMocks mocks,
			new UnmockableDependency(), null, tags: ["dark", "milk",]);

		await That(sut.Tags).IsEqualTo(["dark", "milk",]);
		await That(mocks.Tags).IsSameAs(sut.Tags);
	}

	[Test]
	public async Task CreateWithMocks_WithOptionalValues_ShouldPassThemOn()
	{
		CreateWithMocksService sut = CreateWithMocksService.CreateWithMocks(out CreateWithMocksServiceMocks mocks,
			new UnmockableDependency(), "name", retries: 5, factor: 2.5m);

		await That(sut.Name).IsEqualTo("name");
		await That(sut.Retries).IsEqualTo(5);
		await That(sut.Factor).IsEqualTo(2.5m);
		await That(mocks.Retries).IsEqualTo(5);
		await That(mocks.Factor).IsEqualTo(2.5m);
	}

	[Test]
	public async Task CreateWithMocks_WithCallerInfoParameter_ShouldPassTheCaller()
	{
		CreateWithMocksCallerInfo sut = CreateWithMocksCallerInfo.CreateWithMocks(out _);

		await That(sut.Caller).IsEqualTo(nameof(CreateWithMocks_WithCallerInfoParameter_ShouldPassTheCaller));
	}

#if NET9_0_OR_GREATER
	[Test]
	public async Task CreateWithMocks_WithRecord_ShouldUseThePrimaryConstructor()
	{
		CreateWithMocksRecord sut = CreateWithMocksRecord.CreateWithMocks(out CreateWithMocksRecordMocks mocks, "name");

		await That(sut.Dependency).IsSameAs(mocks.Dependency);
		await That(sut.Name).IsEqualTo("name");
	}

	[Test]
	public async Task CreateWithMocks_WithSpanConstructor_ShouldUseTheShorterConstructor()
	{
		CreateWithMocksSpanConstructor sut = CreateWithMocksSpanConstructor.CreateWithMocks(
			out CreateWithMocksSpanConstructorMocks mocks);

		await That(sut.Dependency).IsSameAs(mocks.Dependency);
		await That(sut.Text).IsNull();
	}

	[Test]
	public async Task CreateWithMocks_WithSetsRequiredMembers_ShouldUseTheConstructor()
	{
		CreateWithMocksSetsRequiredMembers sut = CreateWithMocksSetsRequiredMembers.CreateWithMocks(
			out CreateWithMocksSetsRequiredMembersMocks mocks);

		await That(sut.Dependency).IsSameAs(mocks.Dependency);
		await That(sut.Name).IsEqualTo("set");
	}

	[Test]
	public async Task CreateWithMocks_WithParamsCollection_ShouldRequireTheArgument()
	{
		CreateWithMocksParamsCollection sut = CreateWithMocksParamsCollection.CreateWithMocks(
			out CreateWithMocksParamsCollectionMocks mocks, [1, 2,]);

		await That(sut.Dependency).IsSameAs(mocks.Dependency);
		await That(sut.Values).IsEqualTo([1, 2,]);
	}
#endif

	[Test]
	public async Task TryCreate_WithDataOnlyClass_ShouldCreateItWithMockedDependency()
	{
		bool result = MockDataSources.TryCreate(typeof(CreateWithMocksTests).Assembly, typeof(CreateWithMocksDataOnly),
			null, (_, _) => null, out object? value);

		await That(result).IsTrue();
		await That(value).Is<CreateWithMocksDataOnly>().Whose(x => x.Dependency, it => it.IsNotNull());
	}

	[Test]
	public async Task TryCreate_WithUnmockableParameters_ShouldUseTheDefaultValuesOfTheBehavior()
	{
		MockBehavior behavior = MockBehavior.Default.WithDefaultValueFor(() => "generated");

		bool result = MockDataSources.TryCreate(typeof(CreateWithMocksTests).Assembly,
			typeof(CreateWithMocksDataWithValues), behavior, (_, _) => null, out object? value);

		CreateWithMocksDataWithValues sut = (CreateWithMocksDataWithValues)value!;
		await That(result).IsTrue();
		await That(sut.Dependency).IsNotNull();
		await That(sut.Name).IsEqualTo("generated")
			.Because("a non-nullable parameter without a default value gets a value from the behavior");
		await That(sut.Count).IsEqualTo(0);
		await That(sut.Description).IsNull().Because("a nullable parameter accepts null");
		await That(sut.Fallback).IsNull().Because("an explicit default value wins over the behavior");
	}

	[Test]
	public async Task TryCreateValue_ShouldUseTheDefaultValuesOfTheBehavior()
	{
		MockBehavior behavior = MockBehavior.Default.WithDefaultValueFor(() => 42);

		bool intResult = MockDataSources.TryCreateValue(behavior, out int intValue);
		bool stringResult = MockDataSources.TryCreateValue(MockBehavior.Default, out string stringValue);

		await That(intResult).IsTrue();
		await That(intValue).IsEqualTo(42);
		await That(stringResult).IsTrue();
		await That(stringValue).IsEqualTo("");
	}

	[Test]
	public async Task TryCreateValue_WithoutValueOfTheType_ShouldReturnFalseForReferenceTypes()
	{
		MockBehavior behavior = MockBehavior.Default.WithDefaultValueGenerator(new WrongTypeGenerator());

		bool interfaceResult = MockDataSources.TryCreateValue(MockBehavior.Default,
			out ICreateWithMocksDependency interfaceValue);
		bool wrongTypeResult = MockDataSources.TryCreateValue(behavior, out Uri wrongTypeValue);
		bool valueTypeResult = MockDataSources.TryCreateValue(behavior, out int valueTypeValue);

		await That(interfaceResult).IsFalse();
		await That(interfaceValue).IsNull();
		await That(wrongTypeResult).IsFalse();
		await That(wrongTypeValue).IsNull();
		await That(valueTypeResult).IsTrue().Because("the default of a value type is a valid value");
		await That(valueTypeValue).IsEqualTo(0);
	}

	[Test]
	public async Task TryCreateValue_WithoutBehavior_ShouldThrowArgumentNullException()
	{
		void Act()
			=> MockDataSources.TryCreateValue(null!, out int _);

		await That(Act).Throws<ArgumentNullException>().WithParamName("behavior");
	}

	[Test]
	public async Task TryCreate_WithOtherTypeOfATestMethodParameter_ShouldUseTheDefaultValuesOfTheBehavior()
	{
		bool defaultResult = MockDataSources.TryCreate(typeof(CreateWithMocksTests).Assembly, typeof(string), null,
			(_, _) => null, out object? defaultValue);
		bool behaviorResult = MockDataSources.TryCreate(typeof(CreateWithMocksTests).Assembly, typeof(string),
			MockBehavior.Default.WithDefaultValueFor(() => "generated"), (_, _) => null, out object? behaviorValue);
		bool intResult = MockDataSources.TryCreate(typeof(CreateWithMocksTests).Assembly, typeof(int), null,
			(_, _) => null, out object? intValue);

		await That(defaultResult).IsTrue();
		await That(defaultValue).IsEqualTo("").Because("without a behavior, the default behavior of the assembly applies");
		await That(behaviorResult).IsTrue();
		await That(behaviorValue).IsEqualTo("generated");
		await That(intResult).IsTrue();
		await That(intValue).IsEqualTo(0);
	}

	[Test]
	public async Task TryCreate_WithoutArgumentForRequiredParameter_ShouldThrow()
	{
		void Act()
			=> MockDataSources.TryCreate(typeof(CreateWithMocksTests).Assembly, typeof(CreateWithMocksService), null,
				(_, _) => null, out _);

		await That(Act).Throws<InvalidOperationException>()
			.WithMessage("There is no argument for the constructor parameter 'unmockable' of Mockolate.Tests.GeneratorCoverage.CreateWithMocksService.");
	}

	[Test]
	public async Task TryCreate_WithNullArguments_ShouldThrowArgumentNullException()
	{
		void ActWithoutAssembly()
			=> MockDataSources.TryCreate(null!, typeof(CreateWithMocksDataOnly), null, (_, _) => null, out _);

		void ActWithoutType()
			=> MockDataSources.TryCreate(typeof(CreateWithMocksTests).Assembly, null!, null, (_, _) => null, out _);

		void ActWithoutResolve()
			=> MockDataSources.TryCreate(typeof(CreateWithMocksTests).Assembly, typeof(CreateWithMocksDataOnly), null,
				null!, out _);

		await That(ActWithoutAssembly).Throws<ArgumentNullException>().WithParamName("assembly");
		await That(ActWithoutType).Throws<ArgumentNullException>().WithParamName("type");
		await That(ActWithoutResolve).Throws<ArgumentNullException>().WithParamName("resolve");
	}

	[Test]
	public async Task TryCreate_WithAssemblyWithoutGeneratedCode_ShouldReturnFalse()
	{
		bool result = MockDataSources.TryCreate(typeof(string).Assembly, typeof(CreateWithMocksDataOnly), null,
			(_, _) => null, out object? value);

		await That(result).IsFalse();
		await That(value).IsNull();
	}

	[Test]
	public async Task TryCreate_WithUnknownType_ShouldReturnFalse()
	{
		bool result = MockDataSources.TryCreate(typeof(CreateWithMocksTests).Assembly, typeof(UnmockableDependency),
			null, (_, _) => null, out object? value);

		await That(result).IsFalse();
		await That(value).IsNull();
	}

	private sealed class WrongTypeGenerator : IDefaultValueGenerator
	{
		public object? GenerateValue(Type type, params object?[] parameters)
			=> "not of the requested type";
	}

	private sealed class UnmockableDependency : ICreateWithMocksUnmockableDependency
	{
		[Obsolete("Mocks of obsolete members do not compile without warnings.")]
		public int GetValue()
			=> 0;
	}
}
