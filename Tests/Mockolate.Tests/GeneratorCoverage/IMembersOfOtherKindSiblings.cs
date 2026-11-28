namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Inherits the method <c>Start(int)</c> and the property <c>Start</c> from two base interfaces, so the generated
///     mock implements the member of the second interface explicitly.
/// </summary>
public interface IMembersOfOtherKindSiblings : IMembersHiddenByOtherKind, IMembersOfOtherKindSibling
{
}
