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
///     the arguments (with their ref kinds): when no setup returns a value, or for a <c>void</c> member on every call
///     that neither a wrapped instance nor a base implementation handles.
/// </param>
/// <param name="IsIgnoredByVerifyAll">Whether <c>VerifyThatAllInteractionsAreVerified</c> ignores the calls of the member.</param>
/// <param name="WrappedResultHook">
///     The hook that an <see cref="MockAdapterMemberKind.AnsweredWithoutSetup" /> member returns instead of the result
///     of a wrapped instance or the base implementation, called with the registry and that result, if any.
/// </param>
/// <param name="HookReceivesWhetherSetupMatched">
///     Whether the hook of an <see cref="MockAdapterMemberKind.AnsweredWithoutSetup" /> member receives, after the
///     registry, whether a setup matched, e.g. so that a strict mock only throws without one.
/// </param>
internal sealed record MockAdapterMember(string Name, MockAdapterMemberKind Kind, string? Hook, bool IsIgnoredByVerifyAll,
	string? WrappedResultHook = null, bool HookReceivesWhetherSetupMatched = false);
