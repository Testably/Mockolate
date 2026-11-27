using Mockolate.SourceGenerators.Entities;
using Type = Mockolate.SourceGenerators.Entities.Type;

namespace Mockolate.SourceGenerators.Internals;

/// <summary>
///     The namespaces of the <c>MockolateAutoMockingExcludedNamespaces</c> and
///     <c>MockolateAutoMockingIncludedNamespaces</c> build properties.
/// </summary>
/// <remarks>
///     Interfaces in the framework namespaces (<c>System</c>, <c>Microsoft</c>, <c>Windows</c>) and in the excluded
///     namespaces get no auto-mock, unless their namespace is included. A namespace also stands for its nested
///     namespaces.
/// </remarks>
internal sealed record AutoMockingOptions(
	EquatableArray<string> ExcludedNamespaces,
	EquatableArray<string> IncludedNamespaces)
{
	/// <summary>
	///     Whether an un-configured member returning the <paramref name="type" /> can return an auto-mock.
	/// </summary>
	public bool CanReturnAutoMock(Type type)
		=> type.IsAutoMockable && IsAllowed(type.IsInFrameworkNamespace ? type.Namespace : null);

	/// <summary>
	///     Whether the <paramref name="mock" /> can be returned as an auto-mock.
	/// </summary>
	/// <remarks>
	///     The excluded namespaces only keep the generator from discovering mocks, so a mock that the project creates
	///     with <c>CreateMock</c> is still used.
	/// </remarks>
	public bool CanReturnAutoMock(MockClass mock)
		=> mock.IsAutoMockable && IsAllowed(mock.FrameworkNamespace);

	/// <summary>
	///     Whether the generator may discover a mock for auto-mocking of an interface in the <paramref name="namespace" />.
	/// </summary>
	public bool CanDiscover(string @namespace, bool isFrameworkNamespace)
		=> (!isFrameworkNamespace && !Contains(ExcludedNamespaces, @namespace)) ||
		   Contains(IncludedNamespaces, @namespace);

	private bool IsAllowed(string? frameworkNamespace)
		=> frameworkNamespace is null || Contains(IncludedNamespaces, frameworkNamespace);

	private static bool Contains(EquatableArray<string> namespaces, string @namespace)
		=> namespaces.Any(item => string.Equals(@namespace, item, StringComparison.Ordinal) ||
		                          @namespace.StartsWith(item + ".", StringComparison.Ordinal));
}
