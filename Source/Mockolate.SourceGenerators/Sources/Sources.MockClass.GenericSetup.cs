using System.Text;
using Microsoft.CodeAnalysis;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	/// <summary>
	///     The generic methods on <c>SetupGeneric</c>/<c>VerifyGeneric</c>, with the types of their parameters there:
	///     <see langword="object" /> for a parameter that depends on a type argument, its own type otherwise.
	/// </summary>
	/// <remarks>
	///     Methods whose counterparts would share a signature are left out, as their setups could not be told apart, and
	///     so are methods that an adapter intercepts, as their calls never reach the setups.
	/// </remarks>
	private static List<(Method Method, string[] ParameterTypes)> GetGenericFacadeMethods(Class @class)
	{
		List<(Method Method, string[] ParameterTypes)> candidates = @class.AllMethods()
			.Where(method => IsGenericFacadeEligible(method) &&
			                 FindAdapterMember(@class, method) is not { Member.Kind: MockAdapterMemberKind.Intercepted, })
			.Select(method => (method, GetGenericFacadeParameterTypes(method)))
			.ToList();
		return candidates
			.Where(candidate => candidates.Count(other
				=> GetGenericFacadeSignature(other.Method, other.ParameterTypes) ==
				   GetGenericFacadeSignature(candidate.Method, candidate.ParameterTypes)) == 1)
			.ToList();
	}

	private static string GetGenericFacadeSignature(Method method, string[] parameterTypes)
		=> GetGenericFacadeName(method) + "(" + string.Join(",", parameterTypes.Select(type => type.TrimEnd('?'))) + ")";

	private static bool HasGenericFacadeMethods(Class @class)
		=> GetGenericFacadeMethods(@class).Count > 0;

	private static void AppendGenericFacadeInterfaces(StringBuilder sb, Class @class, string name)
	{
		if (HasGenericFacadeMethods(@class))
		{
			sb.Append(", IMockGenericSetupFor").Append(name).Append(", IMockGenericVerifyFor").Append(name);
		}
	}

	private static void ImplementGenericFacade(StringBuilder sb, Class @class, string name, string mockRegistryName,
		MemberIdTable memberIds, string memberIdPrefix)
	{
		List<(Method Method, string[] ParameterTypes)> methods = GetGenericFacadeMethods(@class);
		if (methods.Count == 0)
		{
			return;
		}

		sb.AppendLine();
		sb.Append("\t\t#region IMockGenericSetupFor").Append(name).AppendLine();
		sb.AppendLine();
		ImplementGenericSetupInterface(sb, methods, name, mockRegistryName, memberIds, memberIdPrefix);
		sb.Append("\t\t#endregion IMockGenericSetupFor").Append(name).AppendLine();
		sb.AppendLine();
		sb.Append("\t\t#region IMockGenericVerifyFor").Append(name).AppendLine();
		sb.AppendLine();
		ImplementGenericVerifyInterface(sb, methods, name, mockRegistryName);
		sb.Append("\t\t#endregion IMockGenericVerifyFor").Append(name).AppendLine();
	}

	private static bool IsGenericFacadeEligible(Method method)
		=> method.ExplicitImplementation is null && HasGenericFacadeShape(method);

	/// <remarks>
	///     Also true for an explicit implementation, which a <c>SetupGeneric</c> of the interface that declares it sets up.
	/// </remarks>
	internal static bool HasGenericFacadeShape(Method method)
		=> method is { GenericParameters.Count: > 0, IsStatic: false, IsProtected: false, } &&
		   method.Parameters.All(parameter
			   => parameter is { RefKind: RefKind.None, IsParams: false, Type.IsRefStruct: false, }) &&
		   !method.HasUnsupportedAllowsRefStructTypeParameter && !method.ReturnType.NeedsRefStructPipeline();

	private static string[] GetGenericFacadeParameterTypes(Method method)
		=> method.Parameters
			.Select(parameter => method.UsesTypeParameter(parameter.Type) ? "object?" : parameter.Type.Fullname)
			.ToArray();

	private static string GetGenericFacadeName(Method method)
		=> method.Name.Substring(0, method.Name.IndexOf('<'));

	private static string GetGenericSetupName(Method method)
		=> $"\"{method.ContainingType}.{method.Name}\"";

	private static string GetGenericSetupType(string[] parameterTypes)
		=> parameterTypes.Length == 0
			? "global::Mockolate.Setup.VoidMethodSetup"
			: $"global::Mockolate.Setup.VoidMethodSetup<{string.Join(", ", parameterTypes)}>";

	private static string GetGenericSetupInterface(string[] parameterTypes)
		=> parameterTypes.Length == 0
			? "global::Mockolate.Setup.IVoidMethodSetup"
			: $"global::Mockolate.Setup.IVoidMethodSetupWithCallback<{string.Join(", ", parameterTypes)}>";

	private static (string Type, string MatchArgs)? GetGenericDispatch(Method method)
	{
		if (!HasGenericFacadeShape(method))
		{
			return null;
		}

		string[] parameterTypes = GetGenericFacadeParameterTypes(method);
		return (GetGenericSetupType(parameterTypes),
			string.Join(", ", method.Parameters.Select((parameter, i)
				=> parameterTypes[i] == "object?" ? $"(object?){parameter.Name}" : parameter.Name)));
	}

	private static void AppendGenericFacadeParameters(StringBuilder sb, Method method, string[] parameterTypes)
	{
		int index = 0;
		foreach (MethodParameter parameter in method.Parameters)
		{
			if (index > 0)
			{
				sb.Append(", ");
			}

			sb.Append("global::Mockolate.Parameters.IParameter<").Append(parameterTypes[index++]).Append(">? ")
				.Append(parameter.Name);
		}
	}

	private static string GetGenericParameterMatch(MethodParameter parameter, string parameterType)
		=> $"global::Mockolate.CovariantParameterAdapter<{parameterType}>.Wrap({parameter.Name} ?? global::Mockolate.Internals.Matchers.IsNull<{parameterType}>())";

	private static string GetGenericParameterNames(Method method)
		=> string.Concat(method.Parameters.Select(parameter => $", \"{parameter.Name.TrimStart('@')}\""));

	private static string GetGenericParameterReferences(Method method)
		=> string.Join(" ", method.Parameters.Select(parameter => $"<paramref name=\"{parameter.Name.TrimStart('@')}\" />"));

	private static bool HasGenericParametersOverload(List<(Method Method, string[] ParameterTypes)> methods,
		Method method)
		=> method.Parameters.Count > 0 &&
		   methods.Count(other => GetGenericFacadeName(other.Method) == GetGenericFacadeName(method)) == 1;

	private static void DefineGenericSetupInterface(StringBuilder sb,
		List<(Method Method, string[] ParameterTypes)> methods, string name, string classReference)
	{
		sb.AppendXmlSummary($"Set up the generic methods of the mock of {classReference} for any type argument.", "\t");
		sb.Append("\tinternal interface IMockGenericSetupFor").Append(name).AppendLine();
		sb.Append("\t{").AppendLine();
		foreach ((Method method, string[] parameterTypes) in methods)
		{
			string setupInterface = GetGenericSetupInterface(parameterTypes);
			if (HasGenericParametersOverload(methods, method))
			{
				sb.AppendXmlSummary($"Setup for the method {method.XmlDocReference} for any type argument with the given <paramref name=\"parameters\" />.");
				sb.Append("\t\t").Append(setupInterface).Append(' ').Append(GetGenericFacadeName(method))
					.Append("(global::Mockolate.Parameters.IParameters parameters);").AppendLine();
				sb.AppendLine();
			}

			sb.AppendXmlSummary(method.Parameters.Count == 0
				? $"Setup for the method {method.XmlDocReference} for any type argument."
				: $"Setup for the method {method.XmlDocReference} for any type argument with the given {GetGenericParameterReferences(method)}.");
			sb.Append("\t\t").Append(setupInterface).Append(' ').Append(GetGenericFacadeName(method)).Append('(');
			AppendGenericFacadeParameters(sb, method, parameterTypes);
			sb.Append(");").AppendLine();
			sb.AppendLine();
		}

		sb.Append("\t}").AppendLine();
		sb.AppendLine();
	}

	private static void ImplementGenericSetupInterface(StringBuilder sb,
		List<(Method Method, string[] ParameterTypes)> methods, string name, string mockRegistryName,
		MemberIdTable memberIds, string memberIdPrefix)
	{
		foreach ((Method method, string[] parameterTypes) in methods)
		{
			string setupType = GetGenericSetupType(parameterTypes);
			string setupInterface = GetGenericSetupInterface(parameterTypes);
			string memberIdRef = memberIdPrefix + memberIds.GetMethodIdentifier(method);
			if (HasGenericParametersOverload(methods, method))
			{
				sb.Append("\t\t/// <inheritdoc />").AppendLine();
				sb.Append("\t\t").Append(setupInterface).Append(" IMockGenericSetupFor").Append(name).Append('.')
					.Append(GetGenericFacadeName(method)).Append("(global::Mockolate.Parameters.IParameters parameters)")
					.AppendLine();
				sb.Append("\t\t{").AppendLine();
				sb.Append("\t\t\tvar methodSetup = new ").Append(setupType).Append(".WithParametersForAnyTypeArguments(this.")
					.Append(mockRegistryName).Append(", ").Append(GetGenericSetupName(method)).Append(", parameters")
					.Append(GetGenericParameterNames(method)).Append(");").AppendLine();
				AppendRegisterGenericSetup(sb, mockRegistryName, memberIdRef);
			}

			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t").Append(setupInterface).Append(" IMockGenericSetupFor").Append(name).Append('.')
				.Append(GetGenericFacadeName(method)).Append('(');
			AppendGenericFacadeParameters(sb, method, parameterTypes);
			sb.Append(')').AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\tvar methodSetup = new ").Append(setupType)
				.Append(".WithParameterCollectionForAnyTypeArguments(this.").Append(mockRegistryName).Append(", ")
				.Append(GetGenericSetupName(method));
			int index = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				sb.Append(", ").Append(GetGenericParameterMatch(parameter, parameterTypes[index++]));
			}

			sb.Append(");").AppendLine();
			AppendRegisterGenericSetup(sb, mockRegistryName, memberIdRef);
		}
	}

	private static void AppendRegisterGenericSetup(StringBuilder sb, string mockRegistryName, string memberIdRef)
	{
		sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(".SetupMethod(").Append(memberIdRef)
			.Append(", methodSetup);").AppendLine();
		sb.Append("\t\t\treturn methodSetup;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	private static void DefineGenericVerifyInterface(StringBuilder sb,
		List<(Method Method, string[] ParameterTypes)> methods, string name, string classReference)
	{
		sb.AppendXmlSummary($"Verify interactions with the generic methods of the mock of {classReference} for any type argument.", "\t");
		sb.Append("\tinternal interface IMockGenericVerifyFor").Append(name).AppendLine();
		sb.Append("\t{").AppendLine();
		foreach ((Method method, string[] parameterTypes) in methods)
		{
			if (HasGenericParametersOverload(methods, method))
			{
				sb.AppendXmlSummary($"Validates the invocations for the method {method.XmlDocReference} for any type argument with the given <paramref name=\"parameters\" />.");
				sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<IMockVerifyFor").Append(name).Append("> ")
					.Append(GetGenericFacadeName(method)).Append("(global::Mockolate.Parameters.IParameters parameters);")
					.AppendLine();
				sb.AppendLine();
			}

			sb.AppendXmlSummary(method.Parameters.Count == 0
				? $"Validates the invocations for the method {method.XmlDocReference} for any type argument."
				: $"Validates the invocations for the method {method.XmlDocReference} for any type argument with the given {GetGenericParameterReferences(method)}.");
			sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<IMockVerifyFor").Append(name).Append("> ")
				.Append(GetGenericFacadeName(method)).Append('(');
			AppendGenericFacadeParameters(sb, method, parameterTypes);
			sb.Append(");").AppendLine();
			sb.AppendLine();
		}

		sb.Append("\t}").AppendLine();
		sb.AppendLine();
	}

	private static void ImplementGenericVerifyInterface(StringBuilder sb,
		List<(Method Method, string[] ParameterTypes)> methods, string name, string mockRegistryName)
	{
		foreach ((Method method, string[] parameterTypes) in methods)
		{
			string invocation = Helpers.GetUniqueLocalVariableName("invocation", method.Parameters);
			if (HasGenericParametersOverload(methods, method))
			{
				sb.Append("\t\t/// <inheritdoc />").AppendLine();
				sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<IMockVerifyFor").Append(name)
					.Append("> IMockGenericVerifyFor").Append(name).Append('.').Append(GetGenericFacadeName(method))
					.Append("(global::Mockolate.Parameters.IParameters parameters)").AppendLine();
				sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".VerifyGenericMethod<IMockVerifyFor")
					.Append(name).Append(">(this, ").Append(GetGenericSetupName(method)).Append(", ")
					.Append(method.Parameters.Count).Append(", ").Append(invocation)
					.Append(" => global::Mockolate.Internals.GenericMethodInvocations.Matches(parameters, ")
					.Append(invocation).Append(GetGenericParameterNames(method)).Append("), () => $\"")
					.Append(method.Name).Append("({parameters})\");").AppendLine();
				sb.AppendLine();
			}

			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<IMockVerifyFor").Append(name)
				.Append("> IMockGenericVerifyFor").Append(name).Append('.').Append(GetGenericFacadeName(method))
				.Append('(');
			AppendGenericFacadeParameters(sb, method, parameterTypes);
			sb.Append(')').AppendLine();
			sb.Append("\t\t{").AppendLine();
			StringBuilder predicate = new();
			int index = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				string match = Helpers.GetUniqueLocalVariableName($"match{index + 1}", method.Parameters);
				sb.Append("\t\t\tglobal::Mockolate.Parameters.IParameterMatch<").Append(parameterTypes[index])
					.Append("> ").Append(match).Append(" = ").Append(GetGenericParameterMatch(parameter, parameterTypes[index]))
					.Append(';').AppendLine();
				if (index > 0)
				{
					predicate.Append(" && ");
				}

				predicate.Append("global::Mockolate.Internals.GenericMethodInvocations.Matches(").Append(match)
					.Append(", ").Append(invocation).Append(", ").Append(index).Append(')');
				index++;
			}

			sb.Append("\t\t\treturn this.").Append(mockRegistryName).Append(".VerifyGenericMethod<IMockVerifyFor")
				.Append(name).Append(">(this, ").Append(GetGenericSetupName(method)).Append(", ")
				.Append(method.Parameters.Count).Append(", ")
				.Append(method.Parameters.Count == 0 ? "static " : "").Append(invocation).Append(" => ")
				.Append(method.Parameters.Count == 0 ? "true" : predicate.ToString()).Append(", () => $\"")
				.Append(method.Name).Append('(')
				.Append(string.Join(", ", method.Parameters.Select(parameter
					=> $"{{(global::Mockolate.Internals.ValueFormatter.Format((object?){parameter.Name}))}}")))
				.Append(")\");").AppendLine();
			sb.Append("\t\t}").AppendLine();
			sb.AppendLine();
		}
	}

	/// <summary>
	///     Emits the setup lookup of a generic method on <c>SetupGeneric</c>: setups of the closed method and setups for
	///     any type argument are searched together, so the latest matching one wins.
	/// </summary>
#pragma warning disable S107 // Methods should not have too many parameters
	private static void EmitFastMethodSetupLookupWithGeneric(StringBuilder sb, string indent, string mockRegistry,
		string methodSetup, string methodSetupType, string memberIdRef, string uniqueNameString, string matchArgs,
		string genericSetup, string genericSetupType, string genericSetupName, string genericMatchArgs,
		EquatableArray<MethodParameter> parameters)
#pragma warning restore S107
	{
		string snapshotVar = Helpers.GetUniqueLocalVariableName($"snapshot_{methodSetup}", parameters);
		string nameVar = Helpers.GetUniqueLocalVariableName($"name_{methodSetup}", parameters);
		string indexVar = Helpers.GetUniqueLocalVariableName($"i_{methodSetup}", parameters);
		string itemVar = Helpers.GetUniqueLocalVariableName($"s_{methodSetup}", parameters);
		string typedVar = Helpers.GetUniqueLocalVariableName($"t_{methodSetup}", parameters);
		string genericVar = Helpers.GetUniqueLocalVariableName($"g_{methodSetup}", parameters);
		string fallbackVar = Helpers.GetUniqueLocalVariableName($"fallback_{methodSetup}", parameters);

		sb.Append(indent).Append(methodSetupType).Append("? ").Append(methodSetup).Append(" = null;").AppendLine();
		sb.Append(indent).Append(genericSetupType).Append("? ").Append(genericSetup).Append(" = null;").AppendLine();
		sb.Append(indent).Append("string ").Append(nameVar).Append(" = ").Append(uniqueNameString).Append(';').AppendLine();
		sb.Append(indent).Append("if (string.IsNullOrEmpty(").Append(mockRegistry).Append(".Scenario))").AppendLine();
		sb.Append(indent).Append('{').AppendLine();
		sb.Append(indent).Append("\tglobal::Mockolate.Setup.MethodSetup[]? ").Append(snapshotVar)
			.Append(" = ").Append(mockRegistry).Append(".GetMethodSetupSnapshot(").Append(memberIdRef).Append(");")
			.AppendLine();
		sb.Append(indent).Append("\tif (").Append(snapshotVar).Append(" is not null)").AppendLine();
		sb.Append(indent).Append("\t{").AppendLine();
		sb.Append(indent).Append("\t\tfor (int ").Append(indexVar).Append(" = ").Append(snapshotVar)
			.Append(".Length - 1; ").Append(indexVar).Append(" >= 0; ").Append(indexVar).Append("--)").AppendLine();
		sb.Append(indent).Append("\t\t{").AppendLine();
		AppendMatch(indent + "\t\t\t", $"{snapshotVar}[{indexVar}]");
		sb.Append(indent).Append("\t\t}").AppendLine();
		sb.Append(indent).Append("\t}").AppendLine();
		sb.Append(indent).Append("\tif (").Append(methodSetup).Append(" is null && ").Append(genericSetup)
			.Append(" is null && ").Append(mockRegistry).Append(".GetFallbackMethodSetups<global::Mockolate.Setup.MethodSetup>(")
			.Append(memberIdRef).Append(", ").Append(nameVar).Append(", ").Append(genericSetupName).Append(") is { } ")
			.Append(fallbackVar).Append(')').AppendLine();
		sb.Append(indent).Append("\t{").AppendLine();
		AppendLoop(indent + "\t\t", fallbackVar);
		sb.Append(indent).Append("\t}").AppendLine();
		sb.Append(indent).Append('}').AppendLine();
		sb.Append(indent).Append("else").AppendLine();
		sb.Append(indent).Append('{').AppendLine();
		AppendLoop(indent + "\t",
			$"{mockRegistry}.GetMethodSetups<global::Mockolate.Setup.MethodSetup>({nameVar}, {genericSetupName})");
		sb.Append(indent).Append('}').AppendLine();

		void AppendLoop(string loopIndent, string setups)
		{
			sb.Append(loopIndent).Append("foreach (global::Mockolate.Setup.MethodSetup ").Append(itemVar).Append(" in ")
				.Append(setups).Append(')').AppendLine();
			sb.Append(loopIndent).Append('{').AppendLine();
			AppendMatch(loopIndent + "\t", itemVar);
			sb.Append(loopIndent).Append('}').AppendLine();
		}

		void AppendMatch(string matchIndent, string candidate)
		{
			sb.Append(matchIndent).Append("if (").Append(candidate).Append(" is ").Append(methodSetupType).Append(' ')
				.Append(typedVar).Append(" && ").Append(typedVar).Append(".Name == ").Append(nameVar).Append(" && ")
				.Append(typedVar).Append(".Matches(").Append(matchArgs).Append("))").AppendLine();
			sb.Append(matchIndent).Append('{').AppendLine();
			sb.Append(matchIndent).Append('\t').Append(methodSetup).Append(" = ").Append(typedVar).Append(';').AppendLine();
			sb.Append(matchIndent).Append("\tbreak;").AppendLine();
			sb.Append(matchIndent).Append('}').AppendLine();
			sb.Append(matchIndent).Append("if (").Append(candidate).Append(" is ").Append(genericSetupType).Append(' ')
				.Append(genericVar).Append(" && ").Append(genericVar).Append(".Name == ").Append(genericSetupName)
				.Append(" && ").Append(genericVar).Append(".Matches(").Append(genericMatchArgs).Append("))").AppendLine();
			sb.Append(matchIndent).Append('{').AppendLine();
			sb.Append(matchIndent).Append('\t').Append(genericSetup).Append(" = ").Append(genericVar).Append(';').AppendLine();
			sb.Append(matchIndent).Append("\tbreak;").AppendLine();
			sb.Append(matchIndent).Append('}').AppendLine();
		}
	}
}
