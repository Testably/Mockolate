namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Inherits members whose signatures differ only in nullable reference annotations, parameter names or tuple element
///     names. C# treats them as the same signature, so the generated mock implements the members of the second interface
///     explicitly.
/// </summary>
public interface IMembersDifferingInNullability : INonNullableQuery, INullableQuery
{
}

public interface INonNullableQuery
{
	string Name { get; set; }
	event System.EventHandler Changed;
	void Query(string value);
	void Filter(System.Collections.Generic.List<string> values);
	void Range((int Min, int Max) range);
}

public interface INullableQuery
{
	string? Name { get; set; }
	event System.EventHandler? Changed;
	void Query(string? value);
	void Filter(System.Collections.Generic.List<string?> items);
	void Range((int Low, int High) bounds);
}

/// <summary>
///     Inherits <c>Accept&lt;U&gt;(U? value)</c> twice, constrained once to a struct and once to a class, which C# also
///     treats as the same signature.
/// </summary>
public interface IGenericMembersDifferingInNullability : IAcceptConstrained<int>, IAcceptConstrained<string>
{
}

public interface IAcceptConstrained<T>
{
	void Accept<U>(U? value) where U : T;
}

/// <summary>
///     Both sides of a diamond hide the method of the common base, once with a nullable parameter.
/// </summary>
public interface IHidingQueries : IHidingQueryLeft, IHidingQueryRight
{
}

public interface IHidingQueryBase
{
	void Query(string value);
}

public interface IHidingQueryLeft : IHidingQueryBase
{
	new void Query(string value);
}

public interface IHidingQueryRight : IHidingQueryBase
{
	new void Query(string? value);
}
