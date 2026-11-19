using System;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Declares members that only look like the members of <see cref="object" />: the mock hides them with <c>new</c>
///     instead of overriding them.
/// </summary>
public interface IObjectMemberLookAlikes
{
	int Equals(object? other);
	bool Equals(object? left, object? right);
	bool ReferenceEquals(object? left, object? right);
	string GetHashCode();
	object ToString();
	Type GetType();
	object MemberwiseClone();
}

public interface IObjectMemberLookAlikesDerived : IObjectMemberLookAlikes
{
}

public interface IVoidObjectMemberLookAlikes
{
	void Equals(object? other);
	void GetHashCode();
}

public interface IRefObjectMemberLookAlikes
{
	bool Equals(in object? other);
	ref int GetHashCode();
}

public interface IObjectMemberLookAlikeProperties
{
	int ToString { get; set; }
	event Action? GetHashCode;
}

#if NET10_0_OR_GREATER
public interface IStaticObjectMemberLookAlikes
{
	static abstract bool Equals(object? other);
	static abstract int GetHashCode();
	static abstract string ToString();
	static virtual bool ReferenceEquals(object? left, object? right) => false;
}
#endif
