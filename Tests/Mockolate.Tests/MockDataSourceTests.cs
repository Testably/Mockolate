using System.Collections.Generic;
using System.Linq;
using Mockolate.Exceptions;
using Mockolate.Internals;
using Mockolate.Tests.GeneratorCoverage;
using TUnit.Core;

namespace Mockolate.Tests;

public sealed class MockDataSourceTests
{
	private static readonly System.Reflection.Assembly Assembly = typeof(MockDataSourceTests).Assembly;

	[Test]
	[Arguments(typeof(IMockDataDependency))]
	[Arguments(typeof(MockDataAbstractDependency))]
	[Arguments(typeof(MockDataDelegate))]
	[Arguments(typeof(IServiceProvider))]
	public async Task TryCreate_WithMockedType_ShouldReturnAMock(Type type)
	{
		bool result = MockDataSource.TryCreate(Assembly, type, null, NoArgument, out object? value);

		await That(result).IsTrue();
		await That(value).IsNotNull().And.Satisfies(x => type.IsInstanceOfType(x) && x is IMock or Delegate);
	}

	[Test]
	public async Task TryCreate_WithClass_ShouldCreateItsConcreteDependencies()
	{
		bool result = MockDataSource.TryCreate(Assembly, typeof(MockDataService), null, NoArgument,
			out object? value);

		await That(result).IsTrue();
		MockDataService service = (MockDataService)value!;
		await That(service.Dependency).IsNotNull();
		await That(service.Inner.Dependency).IsNotNull().And.IsNotSameAs(service.Dependency);
		await That(service.OptionalInner).IsNull();
		await That(service.Text).IsEqualTo("default");
		await That(service.Count).IsNull();
		await That(service.Values).IsEmpty();
	}

	[Test]
	public async Task TryCreate_WithBehavior_ShouldUseItForEveryMock()
	{
		MockDataSource.TryCreate(Assembly, typeof(MockDataService), MockBehavior.Default.ThrowingWhenNotSetup(),
			NoArgument, out object? value);
		MockDataService service = (MockDataService)value!;

		void ActOnDependency() => service.Dependency.GetValue();
		void ActOnInnerDependency() => service.Inner.Dependency.GetValue();

		await That(ActOnDependency).Throws<MockNotSetupException>();
		await That(ActOnInnerDependency).Throws<MockNotSetupException>();
	}

	[Test]
	public async Task TryCreate_WithResolvedArgument_ShouldUseItForEveryClass()
	{
		IMockDataDependency dependency = IMockDataDependency.CreateMock();
		List<string> requested = [];

		MockDataSource.TryCreate(Assembly, typeof(MockDataService), null, Resolve, out object? value);
		MockDataService service = (MockDataService)value!;

		await That(service.Dependency).IsSameAs(dependency);
		await That(service.Inner.Dependency).IsSameAs(dependency);
		await That(requested).IsEqualTo([
			"MockDataInner inner", "IMockDataDependency dependency", "IMockDataDependency dependency",
			"MockDataInner optionalInner", "String text", "Nullable`1 count", "Int32[] values",
		]).InAnyOrder();

		bool Resolve(Type type, string parameterName, out object? argument)
		{
			requested.Add($"{type.Name} {parameterName}");
			argument = dependency;
			return type == typeof(IMockDataDependency);
		}
	}

	[Test]
	public async Task TryCreate_WithResolvedNull_ShouldPassNull()
	{
		MockDataSource.TryCreate(Assembly, typeof(MockDataInner), null, ResolveNull, out object? value);

		await That(((MockDataInner)value!).Dependency).IsNull();

		static bool ResolveNull(Type type, string parameterName, out object? argument)
		{
			argument = null;
			return true;
		}
	}

	[Test]
	public async Task TryCreate_WithResolvedArgumentOfOtherType_ShouldThrow()
	{
		void Act()
			=> MockDataSource.TryCreate(Assembly, typeof(MockDataRequiredValue), null, ResolveInt, out _);

		await That(Act).Throws<MockException>()
			.WithMessage("The argument for the constructor parameter 'value' of Mockolate.Tests.GeneratorCoverage.MockDataRequiredValue is of type System.Int32, but must be of type System.Int64.");

		static bool ResolveInt(Type type, string parameterName, out object? argument)
		{
			argument = 1;
			return parameterName == "value";
		}
	}

	[Test]
	public async Task TryCreate_WithResolvedNullForValueType_ShouldThrow()
	{
		void Act()
			=> MockDataSource.TryCreate(Assembly, typeof(MockDataRequiredValue), null, ResolveNull, out _);

		await That(Act).Throws<MockException>()
			.WithMessage("The argument for the constructor parameter 'value' of Mockolate.Tests.GeneratorCoverage.MockDataRequiredValue is null, but must be of type System.Int64.");

		static bool ResolveNull(Type type, string parameterName, out object? argument)
		{
			argument = null;
			return parameterName == "value";
		}
	}

	[Test]
	public async Task TryCreate_WithoutArgumentForRequiredReference_ShouldThrow()
	{
		void Act()
			=> MockDataSource.TryCreate(Assembly, typeof(MockDataRequiredReference), null, NoArgument, out _);

		await That(Act).Throws<MockException>()
			.WithMessage("There is no argument for the constructor parameter 'address' of Mockolate.Tests.GeneratorCoverage.MockDataRequiredReference.");
	}

	[Test]
	public async Task TryCreate_WithUnmockableParameters_ShouldUseTheDefaultValuesOfTheBehavior()
	{
		MockBehavior behavior = MockBehavior.Default.WithDefaultValueFor(() => "generated");

		bool result = MockDataSource.TryCreate(Assembly, typeof(MockDataWithValues), behavior, NoArgument,
			out object? value);

		MockDataWithValues sut = (MockDataWithValues)value!;
		await That(result).IsTrue();
		await That(sut.Dependency).IsNotNull();
		await That(sut.Name).IsEqualTo("generated")
			.Because("a non-nullable parameter without a default value gets a value from the behavior");
		await That(sut.Count).IsEqualTo(0);
		await That(sut.Description).IsNull().Because("a nullable parameter accepts null");
		await That(sut.Fallback).IsNull().Because("an explicit default value wins over the behavior");
	}

	[Test]
	public async Task TryCreate_WithoutArgumentForValue_ShouldUseTheDefaultValueOfTheBehavior()
	{
		MockDataSource.TryCreate(Assembly, typeof(MockDataRequiredValue), null, NoArgument, out object? required);
		MockDataSource.TryCreate(Assembly, typeof(MockDataObliviousValue), null, NoArgument, out object? oblivious);

		await That(((MockDataRequiredValue)required!).Value).IsEqualTo(0L);
		await That(((MockDataObliviousValue)oblivious!).Value).IsEqualTo("")
			.Because("a nullable-oblivious parameter does not state that it accepts null");
	}

	[Test]
	public async Task TryCreate_WithFrameworkInterfaceDependencies_ShouldUseAValueOrElseAMock()
	{
		bool result = MockDataSource.TryCreate(Assembly, typeof(MockDataFrameworkDependencies), null, NoArgument,
			out object? value);

		MockDataFrameworkDependencies sut = (MockDataFrameworkDependencies)value!;
		await That(result).IsTrue();
		await That(sut.Services).Is<IMock>().Because("the default values provide no IServiceProvider");
		await That(sut.Values).IsEmpty();
		await That(sut.Names).IsEmpty();
	}

	[Test]
	[Arguments(typeof(string), "")]
	[Arguments(typeof(int), 0)]
	public async Task TryCreate_WithOtherTypeOfAParameter_ShouldUseTheDefaultValuesOfTheBehavior(Type type,
		object expected)
	{
		bool defaultResult = MockDataSource.TryCreate(Assembly, type, null, NoArgument, out object? defaultValue);

		await That(defaultResult).IsTrue();
		await That(defaultValue).IsEqualTo(expected);
	}

	[Test]
	public async Task TryCreate_WithOtherTypeOfAParameter_ShouldUseTheGivenBehavior()
	{
		bool result = MockDataSource.TryCreate(Assembly, typeof(string),
			MockBehavior.Default.WithDefaultValueFor(() => "generated"), NoArgument, out object? value);

		await That(result).IsTrue();
		await That(value).IsEqualTo("generated");
	}

	[Test]
	public async Task TryCreate_WithAbstractClassWithParameters_ShouldMockItWithTheDefaultValues()
	{
		bool result = MockDataSource.TryCreate(Assembly, typeof(MockDataAbstractWithParameter), null, NoArgument,
			out object? value);

		await That(result).IsTrue();
		await That(((MockDataAbstractWithParameter)value!).Value).IsEqualTo(0);
	}

	[Test]
	public async Task TryCreate_WithConfiguredCollectionDefault_ShouldUseIt()
	{
		int[] configured = [42,];
		MockBehavior behavior = MockBehavior.Default.WithDefaultValueFor(() => configured);

		MockDataSource.TryCreate(Assembly, typeof(int[]), behavior, NoArgument, out object? parameter);
		MockDataSource.TryCreate(Assembly, typeof(MockDataWithCollection), behavior, NoArgument, out object? sut);
		MockDataSource.TryCreate(Assembly, typeof(int[]), null, NoArgument, out object? unconfigured);

		await That(parameter).IsSameAs(configured);
		await That(((MockDataWithCollection)sut!).Values).IsSameAs(configured);
		await That((int[])unconfigured!).IsEmpty();
	}

	[Test]
	public async Task TryCreate_WithClassParameter_ShouldForwardToTheRealImplementationAndAllowSetups()
	{
		MockDataSource.TryCreate(Assembly, typeof(MockDataForwardingService), MockBehavior.Default.SkippingBaseClass(),
			NoArgument, out object? value);
		MockDataForwardingService sut = (MockDataForwardingService)value!;

		int forwarded = sut.Calculate();
		sut.Mock.Setup.Calculate().Returns(42);

		await That(forwarded).IsEqualTo(5).Because("the class under test keeps its real implementation");
		await That(sut.Calculate()).IsEqualTo(42);
	}

	[Test]
	public async Task TryCreate_WithAbstractClass_ShouldUseTheResolver()
	{
		IMockDataDependency dependency = IMockDataDependency.CreateMock();

		MockDataSource.TryCreate(Assembly, typeof(MockDataAbstractService), null, Resolve, out object? value);

		await That(((MockDataAbstractService)value!).Dependency).IsSameAs(dependency);

		bool Resolve(Type type, string parameterName, out object? argument)
		{
			argument = dependency;
			return type == typeof(IMockDataDependency);
		}
	}

	[Test]
	public async Task TryCreate_WithSealedClass_ShouldCreateARealInstance()
	{
		MockDataSource.TryCreate(Assembly, typeof(MockDataSealedService), null, NoArgument, out object? value);

		await That(value!.GetType()).IsEqualTo(typeof(MockDataSealedService));
		await That(((MockDataSealedService)value).Dependency).IsNotNull();
	}

	[Test]
	public async Task TryCreate_WithCyclicDependencies_ShouldNotCreateTheDependencyThatClosesTheCycle()
	{
		MockDataSource.TryCreate(Assembly, typeof(MockDataCycleStart), null, NoArgument, out object? value);

		MockDataCycleStart start = (MockDataCycleStart)value!;
		await That(start.End).IsNotNull();
		await That(start.End.Start).IsNull();
	}

	[Test]
	[Arguments(typeof(IMockDataConstructorDependency))]
	[Arguments(typeof(IMockDataClassDependency))]
	[Arguments(typeof(MockDataDerivedTarget))]
	public async Task TryCreate_WithParameterOfConstructorTypeOrDerivedAttribute_ShouldCreateIt(Type type)
	{
		bool result = MockDataSource.TryCreate(Assembly, type, null, NoArgument, out object? value);

		await That(result).IsTrue();
		await That(value).IsNotNull().And.Satisfies(type.IsInstanceOfType);
	}

	[Test]
	public async Task TryCreate_WithCyclicDependencyThatCannotBeNull_ShouldThrow()
	{
		void Act()
			=> MockDataSource.TryCreate(Assembly, typeof(MockDataCycleEnd), null, NoArgument, out _);

		await That(Act).Throws<MockException>()
			.WithMessage("There is no argument for the constructor parameter 'end' of Mockolate.Tests.GeneratorCoverage.MockDataCycleStart.");
	}

	[Test]
	public async Task TryCreate_WithUnknownType_ShouldReturnFalse()
	{
		bool result = MockDataSource.TryCreate(Assembly, typeof(System.Text.StringBuilder), null, NoArgument, out object? value);

		await That(result).IsFalse();
		await That(value).IsNull();
	}

	[Test]
	public async Task TryCreate_WithAssemblyWithoutGeneratedCode_ShouldReturnFalseEveryTime()
	{
		bool first = MockDataSource.TryCreate(typeof(string).Assembly, typeof(MockDataInner), null, NoArgument,
			out object? firstValue);
		bool second = MockDataSource.TryCreate(typeof(string).Assembly, typeof(MockDataInner), null, NoArgument,
			out object? secondValue);

		await That(first).IsFalse();
		await That(second).IsFalse();
		await That(firstValue).IsNull();
		await That(secondValue).IsNull();
	}

	[Test]
	public async Task TryCreate_WithNullArguments_ShouldThrowArgumentNullException()
	{
		void ActWithoutAssembly()
			=> MockDataSource.TryCreate(null!, typeof(MockDataInner), null, NoArgument, out _);

		void ActWithoutType()
			=> MockDataSource.TryCreate(Assembly, null!, null, NoArgument, out _);

		void ActWithoutResolve()
			=> MockDataSource.TryCreate(Assembly, typeof(MockDataInner), null, null!, out _);

		await That(ActWithoutAssembly).Throws<ArgumentNullException>().WithParamName("assembly");
		await That(ActWithoutType).Throws<ArgumentNullException>().WithParamName("type");
		await That(ActWithoutResolve).Throws<ArgumentNullException>().WithParamName("resolve");
	}

	[Test]
	[MockArguments]
	public async Task MockArguments_ShouldFillEveryParameter(MockDataService service, IMockDataDependency dependency,
		MockDataAbstractDependency abstractDependency)
	{
		await That(service.Inner).IsNotNull();
		await That(dependency).IsNotNull().And.IsNotSameAs(service.Dependency);
		await That(abstractDependency).IsNotNull();
	}

	[Test]
	[MockArguments]
	[Repeat(2)]
	public async Task MockArguments_ShouldCreateNewMocksForEveryInvocation(IMockDataDependency dependency)
	{
		dependency.GetValue();

		await That(dependency.Mock.Verify.GetValue()).Once();
	}

	private static bool NoArgument(Type type, string parameterName, out object? value)
	{
		value = null;
		return false;
	}

	/// <summary>
	///     A minimal TUnit integration that fills every parameter of the test method through
	///     <see cref="MockDataSource" />.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class MockArgumentsAttribute : UntypedDataSourceGeneratorAttribute, IMockDataSource
	{
		protected override IEnumerable<Func<object?[]?>> GenerateDataSources(
			DataGeneratorMetadata dataGeneratorMetadata)
		{
			Type[] types = dataGeneratorMetadata.MembersToGenerate.OfType<ParameterMetadata>()
				.Select(parameter => parameter.Type)
				.ToArray();
			yield return () => Array.ConvertAll(types, type =>
				MockDataSource.TryCreate(Assembly, type, null, NoArgument, out object? value)
					? value
					: throw new InvalidOperationException($"Cannot create a {type}."));
		}
	}
}
