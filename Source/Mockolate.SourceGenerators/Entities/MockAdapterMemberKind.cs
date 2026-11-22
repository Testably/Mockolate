namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     How an adapter changes a member of the adapted interface.
/// </summary>
internal enum MockAdapterMemberKind
{
	/// <summary>
	///     The hook replaces the body of the member.
	/// </summary>
	Intercepted,

	/// <summary>
	///     The member answers without a setup, also on a mock that throws when no setup matches.
	/// </summary>
	/// <remarks>
	///     The code around the adapted calls makes these calls itself, e.g. <c>ILogger.IsEnabled</c> before
	///     <c>ILogger.Log</c>, while an unexpected adapted call on a strict mock still throws.
	/// </remarks>
	AnsweredWithoutSetup,
}
