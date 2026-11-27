using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     Describes how an adapter changes the mocks of a type that is or implements an interface: the generated mock
///     calls the static hooks of <see cref="HookClass" /> instead of the default behavior for the adapted
///     <see cref="Members" />.
/// </summary>
/// <param name="TargetNamespace">The namespace of the adapted interface.</param>
/// <param name="TargetMetadataName">The metadata name of the adapted (non-generic) interface, e.g. <c>ILogger</c>.</param>
/// <param name="Classes">Which mocks of classes that implement the interface are adapted.</param>
/// <param name="HookClass">The fully qualified name of the static class with the hooks.</param>
/// <param name="Members">The adapted members of the interface.</param>
/// <param name="ClearHook">The hook that clears the interactions of the mock instead of the registry, if any.</param>
/// <param name="MockedReturnTypesOf">The members whose return types are mocked as well, because the hooks create mocks of them.</param>
/// <param name="Context">
///     The metadata name of a generic variant of the adapted interface (in the same namespace) and an expression with
///     its type argument as <c>{0}</c>. The intercepting hooks receive the expression as context, or
///     <see langword="null" /> when the mocked type does not implement the generic variant.
/// </param>
internal sealed record MockAdapter(
	string TargetNamespace,
	string TargetMetadataName,
	MockAdapterClasses Classes,
	string HookClass,
	EquatableArray<MockAdapterMember> Members,
	string? ClearHook,
	EquatableArray<string> MockedReturnTypesOf,
	(string MetadataName, string Expression)? Context = null)
{
	internal static MockAdapter Logger { get; } = new(
		"Microsoft.Extensions.Logging", "ILogger", MockAdapterClasses.WithInterceptedMembers, "global::Mockolate.MockolateLogging",
		new EquatableArray<MockAdapterMember>([
			new MockAdapterMember("Log", MockAdapterMemberKind.Intercepted, "DispatchLog", IsIgnoredByVerifyAll: false),
			new MockAdapterMember("IsEnabled", MockAdapterMemberKind.AnsweredWithoutSetup, "IsEnabled", IsIgnoredByVerifyAll: true),
			new MockAdapterMember("BeginScope", MockAdapterMemberKind.AnsweredWithoutSetup, Hook: null, IsIgnoredByVerifyAll: true),
		]),
ClearHook: null, new EquatableArray<string>([]),
		("ILogger`1", "global::Mockolate.MockolateLogging.CategoryName<{0}>.Value"));

	internal static MockAdapter LoggerFactory { get; } = new(
		"Microsoft.Extensions.Logging", "ILoggerFactory", MockAdapterClasses.None, "global::Mockolate.MockolateLogging",
		new EquatableArray<MockAdapterMember>([
			new MockAdapterMember("CreateLogger", MockAdapterMemberKind.AnsweredWithoutSetup, "CreateLogger", IsIgnoredByVerifyAll: true),
		]),
		"ClearFactoryInteractions", new EquatableArray<string>(["CreateLogger",]));

	internal static MockAdapter MemoryCache { get; } = new(
		"Microsoft.Extensions.Caching.Memory", "IMemoryCache", MockAdapterClasses.WithAbstractMembers, "global::Mockolate.MockolateCaching",
		new EquatableArray<MockAdapterMember>([
			new MockAdapterMember("TryGetValue", MockAdapterMemberKind.AnsweredWithoutSetup, "TryGetValue", IsIgnoredByVerifyAll: false,
				HookReceivesWhetherSetupMatched: true),
			new MockAdapterMember("CreateEntry", MockAdapterMemberKind.AnsweredWithoutSetup, "CreateEntry", IsIgnoredByVerifyAll: true, "RecordEntry"),
			new MockAdapterMember("Remove", MockAdapterMemberKind.AnsweredWithoutSetup, "Remove", IsIgnoredByVerifyAll: false),
		]),
		ClearHook: null, new EquatableArray<string>([]));

	internal static MockAdapter[] All { get; } = [Logger, LoggerFactory, MemoryCache,];

	/// <summary>
	///     The fully qualified name of the adapted interface, as members state their containing type.
	/// </summary>
	internal string TargetFullName => "global::" + TargetNamespace + "." + TargetMetadataName;

	/// <summary>
	///     The adapted interface, when the <paramref name="type" /> is adapted by this adapter.
	/// </summary>
	internal INamedTypeSymbol? FindTarget(ITypeSymbol type)
		=> type.TypeKind == TypeKind.Interface || (type.TypeKind == TypeKind.Class && Classes != MockAdapterClasses.None)
			? FindInterface(type, TargetMetadataName)
			: null;

	/// <summary>
	///     The context of the intercepting hooks for a mock of the <paramref name="type" />, if any.
	/// </summary>
	internal string? GetContext(ITypeSymbol type)
		=> Context is { } context && FindInterface(type, context.MetadataName) is { } contextTarget
			? context.Expression.Replace("{0}",
				contextTarget.TypeArguments[0].ToQualifiedDisplayString(Helpers.TypeDisplayFormat))
			: null;

	private INamedTypeSymbol? FindInterface(ITypeSymbol type, string metadataName)
		=> new[] { type, }.Concat(type.AllInterfaces).OfType<INamedTypeSymbol>()
			.FirstOrDefault(candidate => candidate.TypeKind == TypeKind.Interface &&
			                             string.Equals(candidate.MetadataName, metadataName, StringComparison.Ordinal) &&
			                             string.Equals(candidate.ContainingNamespace.ToDisplayString(), TargetNamespace, StringComparison.Ordinal));
}

