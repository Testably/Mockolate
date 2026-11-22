namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     A member of an adapted interface.
/// </summary>
/// <param name="Name">The name of the member, without type parameters.</param>
/// <param name="Kind">How the adapter changes the member.</param>
/// <param name="Hook">
///     The hook method. For an intercepted member it is required and called with the registry, the member id, the
///     recorded name, the context, the arguments and, when there is a base implementation, a callback of it. For an
///     <see cref="MockAdapterMemberKind.AnsweredWithoutSetup" /> member it is optional and called with the registry and
///     the arguments instead of the default value.
/// </param>
/// <param name="IsIgnoredByVerifyAll">Whether <c>VerifyThatAllInteractionsAreVerified</c> ignores the calls of the member.</param>
internal sealed record MockAdapterMember(string Name, MockAdapterMemberKind Kind, string? Hook, bool IsIgnoredByVerifyAll);
