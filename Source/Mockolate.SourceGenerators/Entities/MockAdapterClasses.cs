namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     Which mocks of classes that implement the adapted interface an adapter adapts.
/// </summary>
internal enum MockAdapterClasses
{
	/// <summary>
	///     Only mocks of interfaces are adapted.
	/// </summary>
	None,

	/// <summary>
	///     Mocks of classes are adapted when the mock overrides all intercepted members.
	/// </summary>
	WithInterceptedMembers,

	/// <summary>
	///     Mocks of classes are adapted when all adapted members are abstract, because the hooks share state that a base
	///     implementation would bypass.
	/// </summary>
	WithAbstractMembers,
}
