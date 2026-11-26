using System;
using Mockolate.Internals;

namespace Mockolate.Tests.GeneratorCoverage;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class)]
public class MockDataAttribute : Attribute, IMockDataSource;

/// <summary>
///     An attribute that inherits <see cref="IMockDataSource" /> from its base class.
/// </summary>
public sealed class DerivedMockDataAttribute : MockDataAttribute;

public interface IMockDataDependency
{
	int GetValue();
}

public abstract class MockDataAbstractDependency
{
	public abstract int GetValue();
}

public delegate int MockDataDelegate(int value);

public interface IMockDataConstructorDependency;

public interface IMockDataClassDependency;

/// <summary>
///     Only a <c>[Deferred]</c> <see cref="Func{TResult}" /> asks for it, so only the deferred factory creates it.
/// </summary>
public interface IMockDataDeferredDependency
{
	int GetValue();
}

/// <summary>
///     Only a <c>[ValueFrom]</c> parameter asks for it, so no mock is generated for it.
/// </summary>
public interface IMockDataValueFromOnly
{
	int GetValue();
}

/// <summary>
///     A class whose concrete constructor dependency is created with the same resolver.
/// </summary>
public class MockDataService(
	IMockDataDependency dependency,
	MockDataInner inner,
	MockDataInner? optionalInner = null,
	string text = "default",
	int? count = null,
	params int[] values)
{
	public IMockDataDependency Dependency { get; } = dependency;
	public MockDataInner Inner { get; } = inner;
	public MockDataInner? OptionalInner { get; } = optionalInner;
	public string Text { get; } = text;
	public int? Count { get; } = count;
	public int[] Values { get; } = values;
}

public class MockDataInner(IMockDataDependency dependency)
{
	public IMockDataDependency Dependency { get; } = dependency;
}

/// <summary>
///     Two classes that depend on each other, so the dependency that closes the cycle is not created.
/// </summary>
public class MockDataCycleStart(MockDataCycleEnd end)
{
	public MockDataCycleEnd End { get; } = end;
}

public class MockDataCycleEnd(MockDataCycleStart? start)
{
	public MockDataCycleStart? Start { get; } = start;
}

public class MockDataRequiredValue(IMockDataDependency dependency, long value)
{
	public IMockDataDependency Dependency { get; } = dependency;
	public long Value { get; } = value;
}

#nullable disable
public class MockDataObliviousValue(string value)
{
	public string Value { get; } = value;
}
#nullable enable

public class MockDataConstructorTarget
{
	[MockData]
	public MockDataConstructorTarget(IMockDataConstructorDependency dependency)
	{
		Dependency = dependency;
	}

	public IMockDataConstructorDependency Dependency { get; }
}

public class MockDataDerivedTarget(IMockDataDependency dependency)
{
	public IMockDataDependency Dependency { get; } = dependency;
}

[MockData]
public class MockDataClassTarget(IMockDataClassDependency dependency)
{
	public IMockDataClassDependency Dependency { get; } = dependency;
}

/// <summary>
///     Values that Mockolate can neither mock nor construct come from the default values of the behavior; a
///     <c>null!</c> default stays <see langword="null" />.
/// </summary>
public class MockDataWithValues(
	IMockDataDependency dependency,
	string name,
	int count,
	string? description,
	string fallback = null!)
{
	public IMockDataDependency Dependency { get; } = dependency;
	public string Name { get; } = name;
	public int Count { get; } = count;
	public string? Description { get; } = description;
	public string Fallback { get; } = fallback;
}

/// <summary>
///     A framework interface gets a value from the default values of the behavior, or else a mock.
/// </summary>
public class MockDataFrameworkDependencies(
	IServiceProvider services,
	System.Collections.Generic.IEnumerable<int> values,
	params System.Collections.Generic.IEnumerable<string> names)
{
	public IServiceProvider Services { get; } = services;
	public System.Collections.Generic.IEnumerable<int> Values { get; } = values;
	public System.Collections.Generic.IEnumerable<string> Names { get; } = names;
}

/// <summary>
///     A reference type without a value from the default values of the behavior needs an argument.
/// </summary>
public class MockDataRequiredReference(Uri address)
{
	public Uri Address { get; } = address;
}

/// <summary>
///     A class mock is created without constructor parameters, so this abstract class gets no mock.
/// </summary>
public abstract class MockDataAbstractWithParameter(int value)
{
	public int Value { get; } = value;
	public abstract int GetValue();
}

/// <summary>
///     A collection from a configured default value wins over the empty collection.
/// </summary>
public class MockDataWithCollection(int[] values)
{
	public int[] Values { get; } = values;
}

/// <summary>
///     A class parameter is a mock that forwards to the real implementation.
/// </summary>
public class MockDataForwardingService(IMockDataDependency dependency)
{
	public IMockDataDependency Dependency { get; } = dependency;

	public virtual int Calculate() => 5;
}

public abstract class MockDataAbstractService(IMockDataDependency dependency)
{
	public IMockDataDependency Dependency { get; } = dependency;

	public abstract int GetValue();
}

public sealed class MockDataSealedService(IMockDataDependency dependency)
{
	public IMockDataDependency Dependency { get; } = dependency;
}

public static class MockDataSourceTargets
{
	[MockData]
	public static object ClassesUnderTest(MockDataForwardingService forwarding, MockDataAbstractService abstractService,
		MockDataSealedService sealedService)
		=> (forwarding, abstractService, sealedService);

	[MockData]
	public static object Collection(MockDataWithCollection sut, int[] values) => (sut, values);

	[MockData]
	public static object?[] Create(MockDataService service, IMockDataDependency dependency,
		MockDataAbstractDependency abstractDependency, MockDataDelegate @delegate, IServiceProvider serviceProvider,
		MockDataCycleStart cycle, MockDataRequiredValue requiredValue, MockDataObliviousValue obliviousValue,
		MockDataWithValues withValues, MockDataFrameworkDependencies frameworkDependencies,
		MockDataRequiredReference requiredReference,
#pragma warning disable Mockolate0010 // The class gets no mock, which the runtime tests check.
		MockDataAbstractWithParameter abstractWithParameter,
#pragma warning restore Mockolate0010
		string text, int number)
		=> [service, dependency, abstractDependency, @delegate, serviceProvider, cycle, requiredValue, obliviousValue,
			withValues, frameworkDependencies, requiredReference, abstractWithParameter, text, number,];

	[DerivedMockData]
	public static object Derived(MockDataDerivedTarget target) => target;

	[MockData]
	public static object Deferred([MockDataDeferred] Func<MockDataSealedService> createService,
		[MockDataDeferred] Func<IMockDataDeferredDependency> createDependency, [MockDataDeferred] Func<int> createNumber,
		[MockDataDeferred] Func<Uri> createAddress,
#pragma warning disable Mockolate0012 // The generator treats it like any other parameter, which the snapshot shows.
		[MockDataDeferred] MockDataInner notAFunc)
#pragma warning restore Mockolate0012
		=> (createService, createDependency, createNumber, createAddress, notAFunc);

	[MockData]
	public static object ValueFrom([MockDataValueFrom(nameof(ValueFromOnly))] IMockDataValueFromOnly value) => value;

	private static IMockDataValueFromOnly ValueFromOnly => null!;

	public static class Nested
	{
		[MockData]
		public static object ValueFrom([MockDataValueFrom(nameof(ValueFromOnly))] IMockDataValueFromOnly value) => value;

		private static IMockDataValueFromOnly ValueFromOnly => null!;
	}
}
