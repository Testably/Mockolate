using System;

namespace Mockolate.Tests.GeneratorCoverage;

public interface ICreateWithMocksDependency
{
	int GetValue();
}

/// <summary>
///     An interface whose mock would not compile without warnings is not a mockable dependency, so it becomes a
///     required argument instead of a mock that no <c>MockabilityAnalyzer</c> checks.
/// </summary>
public interface ICreateWithMocksUnmockableDependency
{
	[Obsolete("Mocks of obsolete members do not compile without warnings.")]
	int GetValue();
}

/// <summary>
///     Required, nullable, defaulted and <see langword="params" /> constructor parameters next to a mockable one.
/// </summary>
public class CreateWithMocksService(
	ICreateWithMocksDependency dependency,
	ICreateWithMocksUnmockableDependency unmockable,
	string? name,
	int retries = 3,
	decimal factor = 1.5m,
	params string[] tags)
{
	public ICreateWithMocksDependency Dependency { get; } = dependency;
	public ICreateWithMocksUnmockableDependency Unmockable { get; } = unmockable;
	public string? Name { get; } = name;
	public int Retries { get; } = retries;
	public decimal Factor { get; } = factor;
	public string[] Tags { get; } = tags;
}

/// <summary>
///     Constructor parameter names that collide with the generated parameters, with members of <see cref="object" />,
///     with the container name and with each other once they are PascalCase.
/// </summary>
public class CreateWithMocksNameCollisions(
	ICreateWithMocksDependency mocks,
	ICreateWithMocksDependency mockBehavior,
	ICreateWithMocksDependency equals,
	ICreateWithMocksDependency createWithMocksNameCollisionsMocks,
	ICreateWithMocksDependency value,
	ICreateWithMocksDependency Value)
{
	public ICreateWithMocksDependency[] All { get; } =
		[mocks, mockBehavior, equals, createWithMocksNameCollisionsMocks, value, Value,];
}

public static class CreateWithMocksOuter
{
	public class Nested(ICreateWithMocksDependency dependency)
	{
		public ICreateWithMocksDependency Dependency { get; } = dependency;
	}
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class CreateWithMocksDataAttribute : Attribute, IMockDataSource;

public static class CreateWithMocksDataSourceTargets
{
	[CreateWithMocksData]
	public static object[] Create(CreateWithMocksService sut, ICreateWithMocksDependency dependency,
		CreateWithMocksOuter.Nested nested)
		=> [sut, dependency, nested,];
}
