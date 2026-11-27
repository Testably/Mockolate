namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     An adapter of a mocked type: the type is or implements the adapted interface.
/// </summary>
/// <param name="Adapter">The adapter.</param>
/// <param name="Context">The context of the intercepting hooks, if any.</param>
/// <param name="ClassMembers">
///     The methods of a class mock that implement the adapted members, as the most derived overrides; empty for an
///     interface.
/// </param>
internal sealed record MockAdapterBinding(
	MockAdapter Adapter,
	string? Context,
	EquatableArray<MockAdapterClassMember> ClassMembers)
{
	/// <summary>
	///     Whether the <paramref name="method" /> of the mocked type is the adapted member <paramref name="name" />.
	/// </summary>
	internal bool IsMember(Method method, string name)
		=> (string.Equals(method.ContainingType, Adapter.TargetFullName, StringComparison.Ordinal) && string.Equals(GetBaseName(method.Name), name, StringComparison.Ordinal)) ||
		   ClassMembers.Any(classMember =>
			   string.Equals(classMember.Member, name, StringComparison.Ordinal) &&Method.EqualityComparer.Equals(classMember.Method, method));

	private static string GetBaseName(string name)
	{
		int index = name.IndexOf('<');
		return index < 0 ? name : name.Substring(0, index);
	}
}
