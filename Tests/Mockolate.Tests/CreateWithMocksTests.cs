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

	private sealed class UnmockableDependency : ICreateWithMocksUnmockableDependency
	{
		[Obsolete("Mocks of obsolete members do not compile without warnings.")]
		public int GetValue()
			=> 0;
	}
}
