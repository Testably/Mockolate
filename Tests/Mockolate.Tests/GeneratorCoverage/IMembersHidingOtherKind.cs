using System;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Hides each inherited member with a member of another kind and the same name, so the generated mock implements
///     the hidden members explicitly.
/// </summary>
public interface IMembersHidingOtherKind : IMembersHiddenByOtherKind
{
	new int ToString { get; set; }
	new event EventHandler? Start;
	new int Size();
	new int Stop { get; set; }
	new event EventHandler? Level;
	new void Reset();
}
