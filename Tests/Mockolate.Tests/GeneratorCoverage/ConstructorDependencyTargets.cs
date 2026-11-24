using System;
using System.Collections.Generic;

namespace Mockolate.Tests.GeneratorCoverage;

public interface IConstructorDependency
{
	int GetValue();
}

public interface IConstructorOtherDependency
{
	string GetName();
}

/// <summary>
///     An interface whose mock would not compile without warnings is not mocked as a constructor dependency, so it gets a
///     value from the default values of the behavior instead.
/// </summary>
public interface IConstructorUnmockableDependency
{
	[Obsolete("Mocks of obsolete members do not compile without warnings.")]
	int GetValue();
}

public interface IConstructorGenericDependency<T>
{
	T GetValue();
}

/// <summary>
///     Nullable, defaulted and <see langword="params" /> constructor parameters next to a mockable one.
/// </summary>
public class ConstructorDependencyService(
	IConstructorDependency dependency,
	string? name,
	int retries = 3,
	decimal factor = 1.5m,
	params string[] tags)
{
	public IConstructorDependency Dependency { get; } = dependency;
	public string? Name { get; } = name;
	public int Retries { get; } = retries;
	public decimal Factor { get; } = factor;
	public string[] Tags { get; } = tags;

	public virtual int Calculate()
		=> Dependency.GetValue() * Retries;
}

/// <summary>
///     Values that cannot be mocked come from the default values of the behavior.
/// </summary>
public class ConstructorDependencyValues(
	IConstructorDependency dependency,
	string text,
	int number,
	IEnumerable<int> values)
{
	public IConstructorDependency Dependency { get; } = dependency;
	public string Text { get; } = text;
	public int Number { get; } = number;
	public IEnumerable<int> Values { get; } = values;
}

public class ConstructorDependencyUnmockable(IConstructorUnmockableDependency unmockable)
{
	public IConstructorUnmockableDependency Unmockable { get; } = unmockable;
}

public class ConstructorDependencyTwice(IConstructorDependency first, IConstructorDependency second)
{
	public IConstructorDependency First { get; } = first;
	public IConstructorDependency Second { get; } = second;
}

/// <summary>
///     <c>CreateMock()</c> fills the constructor with the most parameters; the other one needs arguments.
/// </summary>
public class ConstructorDependencyOverloads
{
	public ConstructorDependencyOverloads(IConstructorDependency dependency, IConstructorOtherDependency other)
	{
		Dependency = dependency;
		Other = other;
	}

	public ConstructorDependencyOverloads(IConstructorOtherDependency other)
	{
		Other = other;
	}

	public IConstructorDependency? Dependency { get; }
	public IConstructorOtherDependency Other { get; }
}

/// <summary>
///     Two constructors with the most parameters leave <c>CreateMock()</c> without a constructor to fill.
/// </summary>
public class ConstructorDependencyTie
{
	public ConstructorDependencyTie(IConstructorDependency dependency)
	{
		Value = dependency;
	}

	public ConstructorDependencyTie(IConstructorOtherDependency other)
	{
		Value = other;
	}

	public object Value { get; }
}

/// <summary>
///     A constructor that can be called without arguments wins over the one with dependencies.
/// </summary>
public class ConstructorDependencyOptional
{
	public ConstructorDependencyOptional()
	{
	}

	public ConstructorDependencyOptional(IConstructorDependency dependency)
	{
		Dependency = dependency;
	}

	public IConstructorDependency? Dependency { get; }
}

public abstract class ConstructorDependencyAbstract(IConstructorDependency dependency)
{
	public IConstructorDependency Dependency { get; } = dependency;

	public abstract int Calculate();
}

/// <summary>
///     The dependency spells the type argument differently than the <c>CreateMock</c> call, which must not add a second,
///     ambiguous mock.
/// </summary>
public class ConstructorDependencyNullableGeneric(IConstructorGenericDependency<string?> dependency)
{
	public IConstructorGenericDependency<string?> Dependency { get; } = dependency;
}

public class ConstructorDependencyKeyword(IConstructorDependency @class)
{
	public IConstructorDependency Class { get; } = @class;
}

#if NET9_0_OR_GREATER
/// <summary>
///     The longest constructor takes a span, which cannot be stored in the constructor parameters, so the shorter one is
///     filled.
/// </summary>
public class ConstructorDependencySpan
{
	public ConstructorDependencySpan(IConstructorDependency dependency, ReadOnlySpan<char> text)
	{
		Dependency = dependency;
		Text = text.ToString();
	}

	public ConstructorDependencySpan(IConstructorDependency dependency)
	{
		Dependency = dependency;
	}

	public IConstructorDependency Dependency { get; }
	public string? Text { get; }
}
#endif

/// <summary>
///     A sealed class cannot be mocked, so <c>CreateMock(out ...)</c> creates a real instance with mocked dependencies.
/// </summary>
public sealed class ConstructorDependencySealed(
	IConstructorDependency dependency,
	IConstructorOtherDependency other,
	string text,
	int number,
	string? description,
	int retries = 3,
	params int[] values)
{
	public IConstructorDependency Dependency { get; } = dependency;
	public IConstructorOtherDependency Other { get; } = other;
	public string Text { get; } = text;
	public int Number { get; } = number;
	public string? Description { get; } = description;
	public int Retries { get; } = retries;
	public int[] Values { get; } = values;
}

public record ConstructorDependencyRecord
{
	public ConstructorDependencyRecord(IConstructorDependency dependency, string name)
	{
		Dependency = dependency;
		Name = name;
	}

	public IConstructorDependency Dependency { get; }
	public string Name { get; }
}

public sealed class ConstructorDependencySealedRequired(Uri address)
{
	public Uri Address { get; } = address;
}

/// <summary>
///     The mock of a generic class describes its constructor with the type argument.
/// </summary>
public class ConstructorDependencyGeneric<T>(IConstructorDependency dependency, T value)
{
	public IConstructorDependency Dependency { get; } = dependency;
	public T Value { get; } = value;
}

/// <summary>
///     A nullable parameter with a default value that is not <see langword="null" /> still accepts <see langword="null" />.
/// </summary>
public sealed class ConstructorDependencySealedNullableDefault(string? title = "Dark")
{
	public string? Title { get; } = title;
}

/// <summary>
///     Only created by a call that passes the <see langword="out" /> argument by name after another argument.
/// </summary>
public sealed class ConstructorDependencySealedNamed(IConstructorDependency dependency)
{
	public IConstructorDependency Dependency { get; } = dependency;
}

public interface IConstructorBaseDependency;

public interface IConstructorDerivedDependency : IConstructorBaseDependency;

/// <summary>
///     The argument of the derived type also converts to the base type of the other parameter.
/// </summary>
public class ConstructorDependencyHierarchy(IConstructorDerivedDependency derived, IConstructorBaseDependency @base)
{
	public IConstructorDerivedDependency Derived { get; } = derived;
	public IConstructorBaseDependency Base { get; } = @base;
}
