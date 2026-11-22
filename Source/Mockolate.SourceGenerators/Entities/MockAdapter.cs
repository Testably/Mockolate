using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     Describes how an adapter changes the mocks of a type that is or implements the interface
///     <see cref="TargetFullName" />: the generated mock calls the static hooks of <see cref="HookClass" /> instead of
///     the default behavior for the adapted <see cref="Members" />.
/// </summary>
/// <param name="TargetNamespace">The namespace of the adapted interface.</param>
/// <param name="TargetMetadataName">The metadata name of the adapted interface, e.g. <c>ILogger</c>.</param>
/// <param name="TargetFullName">The fully qualified name of the adapted interface, as members state their containing type.</param>
/// <param name="InterfacesOnly">Whether only mocks of interfaces are adapted, not mocks of classes that implement the interface.</param>
/// <param name="HookClass">The fully qualified name of the static class with the hooks.</param>
/// <param name="Members">The adapted members of the interface.</param>
/// <param name="ClearHook">The hook that clears the interactions of the mock instead of the registry, if any.</param>
/// <param name="MockedReturnTypesOf">The members whose return types are mocked as well, because the hooks create mocks of them.</param>
/// <param name="ContextMetadataName">
///     The metadata name of a generic variant of the adapted interface (in the same namespace) whose type argument
///     <see cref="ContextExpression" /> turns into the context of the intercepting hooks.
/// </param>
/// <param name="ContextExpression">
///     The context expression with the type argument as <c>{0}</c>; the intercepting hooks receive it, or
///     <see langword="null" /> when the mocked type does not implement the generic variant.
/// </param>
internal sealed record MockAdapter(
	string TargetNamespace,
	string TargetMetadataName,
	string TargetFullName,
	bool InterfacesOnly,
	string HookClass,
	EquatableArray<MockAdapterMember> Members,
	string? ClearHook,
	EquatableArray<string> MockedReturnTypesOf,
	string? ContextMetadataName = null,
	string? ContextExpression = null)
{
	internal static MockAdapter Logger { get; } = new(
		"Microsoft.Extensions.Logging", "ILogger", "global::Microsoft.Extensions.Logging.ILogger",
		false, "global::Mockolate.MockolateLogging",
		new EquatableArray<MockAdapterMember>([
			new MockAdapterMember("Log", MockAdapterMemberKind.Intercepted, "DispatchLog", false),
			new MockAdapterMember("IsEnabled", MockAdapterMemberKind.AnsweredWithoutSetup, "IsEnabled", true),
			new MockAdapterMember("BeginScope", MockAdapterMemberKind.AnsweredWithoutSetup, null, true),
		]),
		null, new EquatableArray<string>([]),
		"ILogger`1", "global::Mockolate.MockolateLogging.CategoryName<{0}>.Value");

	internal static MockAdapter LoggerFactory { get; } = new(
		"Microsoft.Extensions.Logging", "ILoggerFactory", "global::Microsoft.Extensions.Logging.ILoggerFactory",
		true, "global::Mockolate.MockolateLogging",
		new EquatableArray<MockAdapterMember>([
			new MockAdapterMember("CreateLogger", MockAdapterMemberKind.AnsweredWithoutSetup, "CreateLogger", true),
		]),
		"ClearFactoryInteractions", new EquatableArray<string>(["CreateLogger",]));

	internal static MockAdapter[] All { get; } = [Logger, LoggerFactory,];

	/// <summary>
	///     Whether the <paramref name="type" /> is the adapted interface.
	/// </summary>
	internal bool IsTarget(ITypeSymbol type)
		=> IsInterface(type, TargetMetadataName);

	/// <summary>
	///     Whether the <paramref name="type" /> is the generic variant of the adapted interface that provides the context.
	/// </summary>
	internal bool IsContextTarget(ITypeSymbol type)
		=> ContextMetadataName is not null && IsInterface(type, ContextMetadataName);

	private bool IsInterface(ITypeSymbol type, string metadataName)
		=> type is INamedTypeSymbol { TypeKind: TypeKind.Interface, } namedType &&
		   namedType.MetadataName == metadataName &&
		   namedType.ContainingNamespace.ToDisplayString() == TargetNamespace;
}

/// <summary>
///     A member of an adapted interface.
/// </summary>
/// <param name="Name">The name of the member, without type parameters.</param>
/// <param name="Kind">How the adapter changes the member.</param>
/// <param name="Hook">
///     The hook method: it replaces the body of an intercepted member, and answers an
///     <see cref="MockAdapterMemberKind.AnsweredWithoutSetup" /> member instead of the default value when it is set.
/// </param>
/// <param name="IsIgnoredByVerifyAll">Whether <c>VerifyThatAllInteractionsAreVerified</c> ignores the calls of the member.</param>
internal sealed record MockAdapterMember(string Name, MockAdapterMemberKind Kind, string? Hook, bool IsIgnoredByVerifyAll);

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
	AnsweredWithoutSetup,
}

/// <summary>
///     An adapter of a mocked type: the type is or implements the adapted interface.
/// </summary>
/// <param name="Adapter">The adapter.</param>
/// <param name="Context">The context of the intercepting hooks, if any.</param>
/// <param name="ClassMembers">
///     The methods of a class that implement the adapted members, as the most derived overrides; empty for an interface.
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
		=> (method.ContainingType == Adapter.TargetFullName && GetBaseName(method.Name) == name) ||
		   ClassMembers.Any(classMember =>
			   classMember.Member == name && Method.EqualityComparer.Equals(classMember.Method, method));

	private static string GetBaseName(string name)
	{
		int index = name.IndexOf('<');
		return index < 0 ? name : name.Substring(0, index);
	}
}

/// <summary>
///     The <paramref name="Method" /> of a class that implements the adapted member <paramref name="Member" />.
/// </summary>
internal readonly record struct MockAdapterClassMember(string Member, Method Method);
