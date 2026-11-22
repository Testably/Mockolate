using System.Text;
using Mockolate.SourceGenerators.Entities;
using Type = Mockolate.SourceGenerators.Entities.Type;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	internal static bool HasAdapter(Class @class, MockAdapter adapter)
		=> @class.Adapters.Any(binding => binding.Adapter == adapter);

	internal static (MockAdapterBinding Binding, MockAdapterMember Member)? FindAdapterMember(Class @class,
		Method method)
	{
		foreach (MockAdapterBinding binding in @class.Adapters)
		{
			foreach (MockAdapterMember member in binding.Adapter.Members)
			{
				if (binding.IsMember(method, member.Name))
				{
					return (binding, member);
				}
			}
		}

		return null;
	}

	/// <summary>
	///     The recorded names of the calls that <c>VerifyThatAllInteractionsAreVerified</c> ignores, or
	///     <see langword="null" /> when none of the <paramref name="classes" /> is adapted.
	/// </summary>
	/// <remarks>
	///     A name that ends with <c>&lt;</c> is the prefix of the names of a generic method.
	/// </remarks>
	internal static string[]? GetAdapterIgnoredNames(IEnumerable<Class> classes)
	{
		List<string>? names = null;
		foreach (Class @class in classes)
		{
			if (@class.Adapters.Count == 0)
			{
				continue;
			}

			names ??= [];
			foreach (Method method in @class.AllMethods())
			{
				if (FindAdapterMember(@class, method) is { Member: { IsIgnoredByVerifyAll: true, } member, })
				{
					names.Add(method.ContainingType + "." + member.Name +
					          (method.GenericParameters?.Count > 0 ? "<" : ""));
				}
			}
		}

		return names?.Distinct().ToArray();
	}

	/// <summary>
	///     The call of the hook that clears the interactions of a mock of the <paramref name="classes" />, if an adapter
	///     replaces it, without its argument list.
	/// </summary>
	internal static string? GetAdapterClearHook(IEnumerable<Class> classes)
		=> classes.SelectMany(@class => @class.Adapters)
			.Select(binding => binding.Adapter)
			.FirstOrDefault(adapter => adapter.ClearHook is not null) is { } adapter
			? adapter.HookClass + "." + adapter.ClearHook
			: null;

	private static void AppendAdapterAccessDeclaration(StringBuilder sb, IEnumerable<Class> classes)
	{
		if (classes.Any(@class => @class.Adapters.Count > 0))
		{
			sb.Append(", global::Mockolate.Internals.IMockAdapterAccess");
		}
	}

	private static void AppendAdapterAccessImplementation(StringBuilder sb, IReadOnlyCollection<Class> classes,
		string mockRegistryName, string scenario, MemberIdTable memberIds, string memberIdPrefix)
	{
		if (classes.All(@class => @class.Adapters.Count == 0))
		{
			return;
		}

		List<(string Name, string Id)> members = [];
		foreach (Class @class in classes)
		{
			foreach (Method method in @class.AllMethods())
			{
				if (FindAdapterMember(@class, method) is { Member: { } member, } &&
				    members.All(existing => existing.Name != member.Name))
				{
					members.Add((member.Name, memberIdPrefix + memberIds.GetMethodIdentifier(method)));
				}
			}
		}

		sb.AppendLine();
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Internals.MockRegistry global::Mockolate.Internals.IMockAdapterAccess.MockRegistry => this.")
			.Append(mockRegistryName).Append(';').AppendLine();
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tstring global::Mockolate.Internals.IMockAdapterAccess.Scenario => ").Append(scenario).Append(';')
			.AppendLine();
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tint global::Mockolate.Internals.IMockAdapterAccess.GetMemberId(string memberName)").AppendLine();
		sb.Append("\t\t\t=> memberName switch").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		foreach ((string name, string id) in members)
		{
			sb.Append("\t\t\t\t\"").Append(name).Append("\" => ").Append(id).Append(',').AppendLine();
		}

		sb.Append("\t\t\t\t_ => -1,").AppendLine();
		sb.Append("\t\t\t};").AppendLine();
	}

	/// <summary>
	///     Emits the body of an intercepted member: it calls the hook with the registry, the member id, the recorded
	///     name of the member, the context (for an adapter with one), the arguments and, when the member has a base
	///     implementation, a callback of the base implementation.
	/// </summary>
	private static void AppendAdapterInterception(StringBuilder sb, Method method, MockAdapterBinding binding,
		MockAdapterMember member, string mockRegistry, string memberId, bool callsBase)
	{
		string arguments = string.Join(", ", method.Parameters.Select(p => p.Name));
		sb.Append("\t\t\t");
		if (method.ReturnType != Type.Void)
		{
			sb.Append("return ");
		}

		sb.Append(binding.Adapter.HookClass).Append('.').Append(member.Hook).Append('(').Append(mockRegistry)
			.Append(", ").Append(memberId).Append(", \"").Append(method.ContainingType).Append('.').Append(member.Name)
			.Append('"');
		if (binding.Adapter.ContextExpression is not null)
		{
			sb.Append(", ").Append(binding.Context ?? "null");
		}

		if (arguments.Length > 0)
		{
			sb.Append(", ").Append(arguments);
		}

		if (callsBase)
		{
			sb.Append(", () => base.").Append(method.Name).Append('(').Append(arguments).Append(')');
		}

		sb.Append(");").AppendLine();
	}
}
