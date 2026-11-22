using System.ComponentModel;

namespace Mockolate.Internals;

/// <summary>
///     Gives an adapter of a mocked type access to the registry of a mock, its setup or its verification.
/// </summary>
/// <remarks>
///     This type is public only so that generated code and adapters can use it; it is not intended to be used directly.
///     It can change in any version.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMockAdapterAccess
{
	/// <summary>
	///     The registry of the mock.
	/// </summary>
	MockRegistry MockRegistry { get; }

	/// <summary>
	///     The scenario that setups are registered for, or an empty string for the default scope.
	/// </summary>
	string Scenario { get; }

	/// <summary>
	///     The member id of the intercepted member <paramref name="memberName" /> in the mock, or <c>-1</c> when the
	///     mock has no such member.
	/// </summary>
	/// <param name="memberName">The name of the intercepted member of the adapted interface, without type parameters.</param>
	int GetMemberId(string memberName);
}
