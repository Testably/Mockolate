#if NET10_0_OR_GREATER
using System;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Members, parameters, and a generic type parameter all named after C# keywords —
///     forces the generator's <c>@</c>-escaping logic across every identifier slot.
///     Lives in a child namespace ending in a name that collides with <c>System</c>'s
///     conventions to also exercise <c>global::</c> qualification.
/// </summary>
public interface IKeywordEdgeCases
{
	int @class { get; }

	string this[int @params, string @void] { get; set; }

	string @return();

	void @if(int @params);

	int @void<@class>(int @ref);

	event EventHandler @event;
}

public abstract class KeywordEdgeCasesBase
{
	protected KeywordEdgeCasesBase(string @class, int @event) { }

	public abstract void @if(int @in);
}
#endif
