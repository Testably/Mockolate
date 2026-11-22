using System.Collections.Generic;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests;

public sealed class CreateWithMocksTests
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
	public async Task TryCreate_WithDataOnlyClass_ShouldCreateItWithMockedDependency()
	{
		bool result = MockDataSources.TryCreate(typeof(CreateWithMocksTests).Assembly, typeof(CreateWithMocksDataOnly),
			null, (_, _) => null, out object? value);

		await That(result).IsTrue();
		await That(value).Is<CreateWithMocksDataOnly>().Whose(x => x.Dependency, it => it.IsNotNull());
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

	private sealed class UnmockableDependency : ICreateWithMocksUnmockableDependency
	{
		[Obsolete("Mocks of obsolete members do not compile without warnings.")]
		public int GetValue()
			=> 0;
	}
}
