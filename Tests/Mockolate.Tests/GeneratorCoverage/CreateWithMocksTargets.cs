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

/// <summary>
///     Would share the container name of <see cref="CreateWithMocksOuter.Nested" /> if the nested names were joined
///     without doubling the <c>_</c>.
/// </summary>
public class CreateWithMocksOuter_Nested(ICreateWithMocksDependency dependency)
{
	public ICreateWithMocksDependency Dependency { get; } = dependency;
}

public interface ICreateWithMocksGenericDependency<T>
{
	T GetValue();
}

/// <summary>
///     The dependency spells the type argument differently than the <c>CreateMock</c> call, which must not add a second,
///     ambiguous mock.
/// </summary>
public class CreateWithMocksNullableGeneric(ICreateWithMocksGenericDependency<string?> dependency)
{
	public ICreateWithMocksGenericDependency<string?> Dependency { get; } = dependency;
}

/// <summary>
///     A framework interface is a required argument, because its mock would return <see langword="null" /> from
///     <c>GetEnumerator</c>; an <see langword="in" /> parameter is passed by value.
/// </summary>
public class CreateWithMocksFrameworkDependency(System.Collections.Generic.IEnumerable<int> values, in int count)
{
	public System.Collections.Generic.IEnumerable<int> Values { get; } = values;
	public int Count { get; } = count;
}

#if NET9_0_OR_GREATER
/// <summary>
///     A record uses its primary constructor, not the protected copy constructor.
/// </summary>
public record CreateWithMocksRecord(ICreateWithMocksDependency Dependency, string Name);

/// <summary>
///     The longest constructor takes a span, which can neither be stored in the container nor be optional, so the
///     shorter one is used.
/// </summary>
public class CreateWithMocksSpanConstructor
{
	public CreateWithMocksSpanConstructor(ICreateWithMocksDependency dependency, ReadOnlySpan<char> text)
	{
		Dependency = dependency;
		Text = text.ToString();
	}

	public CreateWithMocksSpanConstructor(ICreateWithMocksDependency dependency)
	{
		Dependency = dependency;
	}

	public ICreateWithMocksDependency Dependency { get; }
	public string? Text { get; }
}

/// <summary>
///     A constructor that leaves required members unset cannot be called, unless it sets them.
/// </summary>
public class CreateWithMocksSetsRequiredMembers
{
	[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
	public CreateWithMocksSetsRequiredMembers(ICreateWithMocksDependency dependency)
	{
		Dependency = dependency;
		Name = "set";
	}

	public ICreateWithMocksDependency Dependency { get; }
	public required string Name { get; init; }
}

/// <summary>
///     A <see langword="params" /> collection that is not an array is a required argument, because a nullable value
///     type could not be passed on.
/// </summary>
public class CreateWithMocksParamsCollection(
	ICreateWithMocksDependency dependency,
	params System.Collections.Immutable.ImmutableArray<int> values)
{
	public ICreateWithMocksDependency Dependency { get; } = dependency;
	public System.Collections.Immutable.ImmutableArray<int> Values { get; } = values;
}
#endif
