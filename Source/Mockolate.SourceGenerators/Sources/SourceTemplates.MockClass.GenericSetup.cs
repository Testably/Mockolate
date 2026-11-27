using System.Text;
using Microsoft.CodeAnalysis;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class SourceTemplates
{
	/// <summary>
	///     The generic methods on <c>SetupGeneric</c>/<c>VerifyGeneric</c> (or, for <see cref="MemberType.Protected" />,
	///     on <c>SetupProtectedGeneric</c>/<c>VerifyProtectedGeneric</c>), with the types of their parameters there:
	///     <see langword="object" /> for a parameter that depends on a type argument, its own type otherwise.
	/// </summary>
	/// <remarks>
	///     Overloads whose counterparts share a signature, setup name and parameter names are offered once, as one setup
	///     matches the calls of all of them (e.g. both generic <c>Send</c> overloads of MediatR's <c>ISender</c>). Other
	///     methods whose counterparts would share a signature are left out, as their setups could not be told apart, and
	///     so are methods that an adapter intercepts, as their calls never reach the setups.
	/// </remarks>
	private static List<(Method Method, string[] ParameterTypes, Method[] Overloads, string IsCallOf)> GetGenericFacadeMethods(Class @class,
		MemberType memberType)
	{
		Method[] genericMethods = @class.AllMethods().Where(method => method.GenericParameters is not null).ToArray();
		return genericMethods
			.Where(method => method.MemberType == memberType && method.ExplicitImplementation is null &&
			                 HasGenericFacadeShape(method) &&
			                 FindAdapterMember(@class, method) is not { Member.Kind: MockAdapterMemberKind.Intercepted, })
			.GroupBy(GetGenericFacadeSignature)
			.Where(group => group.Select(method => GetGenericSetupName(method) + GetGenericParameterNames(method))
				.Distinct().Count() == 1)
			.Select(group => group.ToArray())
			.Select(overloads => (overloads[0], GetGenericFacadeParameterTypes(overloads[0]), overloads,
				GetGenericIsCallOf(overloads, genericMethods)))
			.ToList();
	}

	/// <summary>
	///     The expression that recognizes the calls of the <paramref name="overloads" /> by the types of their
	///     parameters, or <c>null</c> when their name and their numbers of type parameters and parameters already do.
	/// </summary>
	/// <remarks>
	///     A call is recorded with the name of the method, its type arguments and the types of its parameters. Another
	///     method of that name with as many type parameters and parameters is therefore told apart by a parameter type:
	///     the type itself when it does not depend on a type argument, the type argument when it is a type parameter.
	///     Any other type that depends on a type argument, e.g. <c>List&lt;T&gt;</c>, is not checked.
	/// </remarks>
	private static string GetGenericIsCallOf(Method[] overloads, Method[] genericMethods)
	{
		Method method = overloads[0];
		string setupName = GetGenericSetupName(method);
		if (!genericMethods.Any(other => !overloads.Contains(other) && other.Parameters.Count == method.Parameters.Count &&
		                                 string.Equals(GetGenericSetupName(other), setupName, StringComparison.Ordinal)))
		{
			return "null";
		}

		string invocation = Helpers.GetUniqueLocalVariableName("invocation", method.Parameters);
		List<string> alternatives = [];
		foreach (Method overload in overloads)
		{
			GenericParameter[] genericParameters = overload.GenericParameters!.Value.AsArray();
			List<string> checks = [];
			int index = 0;
			foreach (MethodParameter parameter in overload.Parameters)
			{
				if (!overload.UsesTypeParameter(parameter.Type))
				{
					checks.Add($"{invocation}.GetParameterTypeAt({index}) == typeof({parameter.Type.IdentityName})");
				}
				else if (Array.FindIndex(genericParameters, genericParameter
					         => string.Equals(parameter.Type.Fullname, genericParameter.Name, StringComparison.Ordinal) ||
					            string.Equals(parameter.Type.Fullname, genericParameter.Name + "?", StringComparison.Ordinal))
				         is >= 0 and int typeArgumentIndex)
				{
					bool isNullable = parameter.Type.Fullname.EndsWith("?", StringComparison.Ordinal) && genericParameters[typeArgumentIndex].IsValueType;
					checks.Add(
						$"global::Mockolate.Internals.GenericMethodInvocations.IsTypeArgument({invocation}, {setupName}, {index}, {typeArgumentIndex}{(isNullable ? ", true" : "")})");
				}

				index++;
			}

			if (checks.Count == 0)
			{
				return "null";
			}

			alternatives.Add(string.Join(" && ", checks));
		}

		return $"static {invocation} => {string.Join(" || ", alternatives.Select(alternative => $"({alternative})"))}";
	}

	/// <remarks>
	///     A nullable annotation of a reference type does not change the signature, unlike <see cref="System.Nullable{T}" />.
	/// </remarks>
	private static string GetGenericFacadeSignature(Method method)
		=> GetGenericFacadeName(method) + "(" + string.Join(",", method.Parameters.Select(parameter
			=> method.UsesTypeParameter(parameter.Type) ? "object" : parameter.Type.IdentityName)) + ")";

	private static bool HasGenericFacadeMethods(Class @class, MemberType memberType)
		=> GetGenericFacadeMethods(@class, memberType).Count > 0;

	private static string GetGenericSetupFacadeName(MemberType memberType, string name)
		=> memberType == MemberType.Protected ? $"IMockProtectedGenericSetupFor{name}" : $"IMockGenericSetupFor{name}";

	private static string GetGenericVerifyFacadeName(MemberType memberType, string name)
		=> memberType == MemberType.Protected ? $"IMockProtectedGenericVerifyFor{name}" : $"IMockGenericVerifyFor{name}";

	private static string GetGenericVerifyResultName(MemberType memberType, string name)
		=> memberType == MemberType.Protected ? $"IMockProtectedVerifyFor{name}" : $"IMockVerifyFor{name}";

	private static void AppendGenericFacadeInterfaces(StringBuilder sb, Class @class, string name)
	{
		foreach (MemberType memberType in new[] { MemberType.Public, MemberType.Protected, })
		{
			if (HasGenericFacadeMethods(@class, memberType))
			{
				sb.Append(", ").Append(GetGenericSetupFacadeName(memberType, name))
					.Append(", ").Append(GetGenericVerifyFacadeName(memberType, name));
			}
		}
	}

	private static void ImplementGenericFacade(StringBuilder sb, Class @class, string name, string mockRegistryName,
		MemberIdTable memberIds, string memberIdPrefix)
	{
		ImplementGenericSetupFacade(sb, @class, name, mockRegistryName, memberIds, memberIdPrefix, null);
		foreach (MemberType memberType in new[] { MemberType.Public, MemberType.Protected, })
		{
			ImplementGenericVerifyFacade(sb, @class, name, memberType, mockRegistryName);
		}
	}

	private static void ImplementGenericSetupFacade(StringBuilder sb, Class @class, string name, string mockRegistryName,
		MemberIdTable memberIds, string memberIdPrefix, string? scenario)
	{
		foreach (MemberType memberType in new[] { MemberType.Public, MemberType.Protected, })
		{
			List<(Method Method, string[] ParameterTypes, Method[] Overloads, string IsCallOf)> methods = GetGenericFacadeMethods(@class, memberType);
			if (methods.Count == 0)
			{
				continue;
			}

			string facadeName = GetGenericSetupFacadeName(memberType, name);
			sb.AppendLine();
			sb.Append("\t\t#region ").Append(facadeName).AppendLine();
			sb.AppendLine();
			ImplementGenericSetupInterface(sb, methods, facadeName, mockRegistryName, memberIds, memberIdPrefix, scenario);
			sb.Append("\t\t#endregion ").Append(facadeName).AppendLine();
		}
	}

	private static void ImplementGenericVerifyFacade(StringBuilder sb, Class @class, string name, MemberType memberType,
		string mockRegistryName)
	{
		List<(Method Method, string[] ParameterTypes, Method[] Overloads, string IsCallOf)> methods = GetGenericFacadeMethods(@class, memberType);
		if (methods.Count == 0)
		{
			return;
		}

		string facadeName = GetGenericVerifyFacadeName(memberType, name);
		sb.AppendLine();
		sb.Append("\t\t#region ").Append(facadeName).AppendLine();
		sb.AppendLine();
		ImplementGenericVerifyInterface(sb, methods, facadeName, GetGenericVerifyResultName(memberType, name),
			mockRegistryName);
		sb.Append("\t\t#endregion ").Append(facadeName).AppendLine();
	}

	private static void DefineGenericFacadeInterfaces(StringBuilder sb, Class @class, string name,
		string classReference, bool hasOverloadResolutionPriority)
	{
		foreach (MemberType memberType in new[] { MemberType.Public, MemberType.Protected, })
		{
			List<(Method Method, string[] ParameterTypes, Method[] Overloads, string IsCallOf)> methods = GetGenericFacadeMethods(@class, memberType);
			if (methods.Count == 0)
			{
				continue;
			}

			string members = memberType == MemberType.Protected
				? "<see langword=\"protected\" /> generic methods"
				: "generic methods";
			sb.AppendLine();
			DefineGenericSetupInterface(sb, methods, GetGenericSetupFacadeName(memberType, name),
				$"Set up the {members} of the mock of {classReference} for any type argument.",
				hasOverloadResolutionPriority);
			DefineGenericVerifyInterface(sb, methods, GetGenericVerifyFacadeName(memberType, name),
				GetGenericVerifyResultName(memberType, name),
				$"Verify interactions with the {members} of the mock of {classReference} for any type argument.",
				hasOverloadResolutionPriority);
		}
	}

	/// <remarks>
	///     Also true for an explicit implementation, which a <c>SetupGeneric</c> of the interface that declares it sets up.
	/// </remarks>
	internal static bool HasGenericFacadeShape(Method method)
		=> method is { GenericParameters.Count: > 0, IsStatic: false, } &&
		   method.Parameters.All(parameter
			   => parameter is { RefKind: RefKind.None, IsParams: false, Type.IsRefStruct: false, }) &&
		   !method.HasUnsupportedAllowsRefStructTypeParameter && !method.ReturnType.NeedsRefStructPipeline();

	private static string[] GetGenericFacadeParameterTypes(Method method)
		=> method.Parameters
			.Select(parameter => method.UsesTypeParameter(parameter.Type) ? "object?" : parameter.Type.Fullname)
			.ToArray();

	private static string GetGenericFacadeName(Method method)
		=> method.Name.Substring(0, method.Name.IndexOf('<'));

	/// <summary>
	///     The name of a setup for any type argument, e.g. <c>Ns.IFoo.Send&lt;,&gt;</c> for two type parameters.
	/// </summary>
	/// <remarks>
	///     It leaves out the names of the type parameters, which a class that implements an interface method may
	///     choose differently.
	/// </remarks>
	private static string GetGenericSetupName(Method method)
		=> $"\"{method.ContainingType}.{GetGenericFacadeName(method)}<{new string(',', method.GenericParameters!.Value.Count - 1)}>\"";

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
				=> string.Equals(parameterTypes[i], "object?", StringComparison.Ordinal) ? $"(object?){parameter.Name}" : parameter.Name)));
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

	/// <summary>
	///     The types of the parameters of the generic overload of a facade method, which takes a matcher of any type, e.g.
	///     <c>It.IsAny&lt;int&gt;()</c>, for each parameter that depends on a type argument; <see langword="null" /> when
	///     no parameter depends on one.
	/// </summary>
	private static (string[] ParameterTypes, string TypeParameters)? GetGenericMatcherOverload(Method method,
		string[] parameterTypes)
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		List<int> indices = Enumerable.Range(0, parameters.Length)
			.Where(index => method.UsesTypeParameter(parameters[index].Type))
			.ToList();
		if (indices.Count == 0)
		{
			return null;
		}

		HashSet<string> usedNames = new(parameters.Select(parameter => parameter.Name.TrimStart('@')));
		string[] names = indices
			.Select(index => parameters[index].Name.TrimStart('@'))
			.Select(name =>
			{
				string candidate = "T" + char.ToUpperInvariant(name[0]) + name.Substring(1);
				string unique = candidate;
				for (int suffix = 1; !usedNames.Add(unique); suffix++)
				{
					unique = candidate + suffix;
				}

				return unique;
			})
			.ToArray();

		string[] types = (string[])parameterTypes.Clone();
		for (int i = 0; i < indices.Count; i++)
		{
			types[indices[i]] = names[i];
		}

		return (types, string.Join(", ", names));
	}

	private static string GetGenericParameterMatch(Method method, MethodParameter parameter, string parameterType,
		bool isMatcherOverload)
		=> isMatcherOverload && method.UsesTypeParameter(parameter.Type)
			? $"new global::Mockolate.CovariantParameterAdapter<object?>((global::Mockolate.Parameters.IParameter?){parameter.Name} ?? global::Mockolate.Internals.Matchers.IsNull<object?>())"
			: $"global::Mockolate.CovariantParameterAdapter<{parameterType}>.Wrap({parameter.Name} ?? global::Mockolate.Internals.Matchers.IsNull<{parameterType}>())";

	private static string GetGenericParameterNames(Method method)
		=> string.Concat(method.Parameters.Select(parameter => $", \"{parameter.Name.TrimStart('@')}\""));

	private static string GetGenericParameterReferences(Method method)
		=> string.Join(" ", method.Parameters.Select(parameter => $"<paramref name=\"{parameter.Name.TrimStart('@')}\" />"));

	private static bool HasGenericParametersOverload(List<(Method Method, string[] ParameterTypes, Method[] Overloads, string IsCallOf)> methods,
		Method method)
		=> method.Parameters.Count > 0 &&
		   methods.Count(other => string.Equals(GetGenericFacadeName(other.Method), GetGenericFacadeName(method), StringComparison.Ordinal)) == 1;

	/// <summary>
	///     Lets a <see langword="null" /> literal for the only parameter choose the overloads that take a matcher, where
	///     it matches a <see langword="null" /> argument, over the one that takes <c>IParameters</c>.
	/// </summary>
	private static void AppendGenericMatcherPriority(StringBuilder sb,
		List<(Method Method, string[] ParameterTypes, Method[] Overloads, string IsCallOf)> methods, Method method,
		bool hasOverloadResolutionPriority)
	{
		if (hasOverloadResolutionPriority && method.Parameters.Count == 1 && HasGenericParametersOverload(methods, method))
		{
			sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(1)]").AppendLine();
		}
	}

	private static void DefineGenericSetupInterface(StringBuilder sb,
		List<(Method Method, string[] ParameterTypes, Method[] Overloads, string IsCallOf)> methods, string facadeName, string summary,
		bool hasOverloadResolutionPriority)
	{
		sb.AppendXmlSummary(summary, "\t");
		sb.Append("\tinternal interface ").Append(facadeName).AppendLine();
		sb.Append("\t{").AppendLine();
		foreach ((Method method, string[] parameterTypes, _, _) in methods)
		{
			string setupInterface = GetGenericSetupInterface(parameterTypes);
			if (HasGenericParametersOverload(methods, method))
			{
				sb.AppendXmlSummary($"Setup for the method {method.XmlDocReference} for any type argument with the given <paramref name=\"parameters\" />.");
				sb.Append("\t\t").Append(setupInterface).Append(' ').Append(GetGenericFacadeName(method))
					.Append("(global::Mockolate.Parameters.IParameters parameters);").AppendLine();
				sb.AppendLine();
			}

			string methodSummary = method.Parameters.Count == 0
				? $"Setup for the method {method.XmlDocReference} for any type argument."
				: $"Setup for the method {method.XmlDocReference} for any type argument with the given {GetGenericParameterReferences(method)}.";
			sb.AppendXmlSummary(methodSummary);
			AppendGenericMatcherPriority(sb, methods, method, hasOverloadResolutionPriority);
			sb.Append("\t\t").Append(setupInterface).Append(' ').Append(GetGenericFacadeName(method)).Append('(');
			AppendGenericFacadeParameters(sb, method, parameterTypes);
			sb.Append(");").AppendLine();
			sb.AppendLine();
			if (GetGenericMatcherOverload(method, parameterTypes) is (string[] matcherTypes, string typeParameters))
			{
				sb.AppendXmlSummary(methodSummary);
				AppendGenericMatcherPriority(sb, methods, method, hasOverloadResolutionPriority);
				sb.Append("\t\t").Append(setupInterface).Append(' ').Append(GetGenericFacadeName(method)).Append('<')
					.Append(typeParameters).Append(">(");
				AppendGenericFacadeParameters(sb, method, matcherTypes);
				sb.Append(");").AppendLine();
				sb.AppendLine();
			}
		}

		sb.Append("\t}").AppendLine();
	}

	private static void ImplementGenericSetupInterface(StringBuilder sb,
		List<(Method Method, string[] ParameterTypes, Method[] Overloads, string IsCallOf)> methods, string facadeName, string mockRegistryName,
		MemberIdTable memberIds, string memberIdPrefix, string? scenario)
	{
		foreach ((Method method, string[] parameterTypes, Method[] overloads, string isCallOf) in methods)
		{
			string setupType = GetGenericSetupType(parameterTypes);
			string setupInterface = GetGenericSetupInterface(parameterTypes);
			string[] memberIdRefs = overloads
				.Select(overload => memberIdPrefix + memberIds.GetMethodIdentifier(overload))
				.ToArray();
			string methodSetup = Helpers.GetUniqueLocalVariableName("methodSetup", method.Parameters);
			if (HasGenericParametersOverload(methods, method))
			{
				sb.Append("\t\t/// <inheritdoc />").AppendLine();
				sb.Append("\t\t").Append(setupInterface).Append(' ').Append(facadeName).Append('.')
					.Append(GetGenericFacadeName(method)).Append("(global::Mockolate.Parameters.IParameters parameters)")
					.AppendLine();
				sb.Append("\t\t{").AppendLine();
				sb.Append("\t\t\tvar methodSetup = new ").Append(setupType).Append(".WithParametersForAnyTypeArguments(this.")
					.Append(mockRegistryName).Append(", ").Append(GetGenericSetupName(method)).Append(", ").Append(isCallOf)
					.Append(", parameters")
					.Append(GetGenericParameterNames(method)).Append(");").AppendLine();
				AppendRegisterGenericSetup(sb, mockRegistryName, memberIdRefs, "methodSetup", scenario);
			}

			AppendSetup(parameterTypes, null);
			if (GetGenericMatcherOverload(method, parameterTypes) is (string[] matcherTypes, string typeParameters))
			{
				AppendSetup(matcherTypes, typeParameters);
			}

			void AppendSetup(string[] signatureTypes, string? genericTypeParameters)
			{
				sb.Append("\t\t/// <inheritdoc />").AppendLine();
				sb.Append("\t\t").Append(setupInterface).Append(' ').Append(facadeName).Append('.')
					.Append(GetGenericFacadeName(method));
				if (genericTypeParameters is not null)
				{
					sb.Append('<').Append(genericTypeParameters).Append('>');
				}

				sb.Append('(');
				AppendGenericFacadeParameters(sb, method, signatureTypes);
				sb.Append(')').AppendLine();
				sb.Append("\t\t{").AppendLine();
				sb.Append("\t\t\tvar ").Append(methodSetup).Append(" = new ").Append(setupType)
					.Append(".WithParameterCollectionForAnyTypeArguments(this.").Append(mockRegistryName).Append(", ")
					.Append(GetGenericSetupName(method));
				if (method.Parameters.Count > 0)
				{
					sb.Append(", ").Append(isCallOf);
				}

				int index = 0;
				foreach (MethodParameter parameter in method.Parameters)
				{
					sb.Append(", ").Append(GetGenericParameterMatch(method, parameter, parameterTypes[index++],
						genericTypeParameters is not null));
				}

				sb.Append(");").AppendLine();
				AppendRegisterGenericSetup(sb, mockRegistryName, memberIdRefs, methodSetup, scenario);
			}
		}
	}

	/// <remarks>
	///     A setup that matches several overloads is registered for each of them, so that it is searched together with the
	///     setups of each overload and the latest matching one wins. A scenario looks setups up by name, so there it is
	///     registered once.
	/// </remarks>
	private static void AppendRegisterGenericSetup(StringBuilder sb, string mockRegistryName, string[] memberIdRefs,
		string methodSetup, string? scenario)
	{
		sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(".SetupMethod(").Append(memberIdRefs[0]);
		if (scenario is not null)
		{
			sb.Append(", ").Append(scenario);
		}

		sb.Append(", ").Append(methodSetup).Append(");").AppendLine();
		if (memberIdRefs.Length > 1)
		{
			string indent = "\t\t\t";
			if (scenario is not null)
			{
				sb.Append("\t\t\tif (string.IsNullOrEmpty(").Append(scenario).Append("))").AppendLine();
				sb.Append("\t\t\t{").AppendLine();
				indent = "\t\t\t\t";
			}

			foreach (string memberIdRef in memberIdRefs.Skip(1))
			{
				sb.Append(indent).Append("this.").Append(mockRegistryName).Append(".SetupMethod(").Append(memberIdRef)
					.Append(", ").Append(methodSetup).Append(");").AppendLine();
			}

			if (scenario is not null)
			{
				sb.Append("\t\t\t}").AppendLine();
			}
		}

		sb.Append("\t\t\treturn ").Append(methodSetup).Append(';').AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	private static void DefineGenericVerifyInterface(StringBuilder sb,
		List<(Method Method, string[] ParameterTypes, Method[] Overloads, string IsCallOf)> methods, string facadeName, string resultName, string summary,
		bool hasOverloadResolutionPriority)
	{
		sb.AppendXmlSummary(summary, "\t");
		sb.Append("\tinternal interface ").Append(facadeName).AppendLine();
		sb.Append("\t{").AppendLine();
		foreach ((Method method, string[] parameterTypes, _, _) in methods)
		{
			if (HasGenericParametersOverload(methods, method))
			{
				sb.AppendXmlSummary($"Validates the invocations for the method {method.XmlDocReference} for any type argument with the given <paramref name=\"parameters\" />.");
				sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<").Append(resultName).Append("> ")
					.Append(GetGenericFacadeName(method)).Append("(global::Mockolate.Parameters.IParameters parameters);")
					.AppendLine();
				sb.AppendLine();
			}

			string methodSummary = method.Parameters.Count == 0
				? $"Validates the invocations for the method {method.XmlDocReference} for any type argument."
				: $"Validates the invocations for the method {method.XmlDocReference} for any type argument with the given {GetGenericParameterReferences(method)}.";
			sb.AppendXmlSummary(methodSummary);
			AppendGenericMatcherPriority(sb, methods, method, hasOverloadResolutionPriority);
			sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<").Append(resultName).Append("> ")
				.Append(GetGenericFacadeName(method)).Append('(');
			AppendGenericFacadeParameters(sb, method, parameterTypes);
			sb.Append(");").AppendLine();
			sb.AppendLine();
			if (GetGenericMatcherOverload(method, parameterTypes) is (string[] matcherTypes, string typeParameters))
			{
				sb.AppendXmlSummary(methodSummary);
				AppendGenericMatcherPriority(sb, methods, method, hasOverloadResolutionPriority);
				sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<").Append(resultName).Append("> ")
					.Append(GetGenericFacadeName(method)).Append('<').Append(typeParameters).Append(">(");
				AppendGenericFacadeParameters(sb, method, matcherTypes);
				sb.Append(");").AppendLine();
				sb.AppendLine();
			}
		}

		sb.Append("\t}").AppendLine();
	}

	private static void ImplementGenericVerifyInterface(StringBuilder sb,
		List<(Method Method, string[] ParameterTypes, Method[] Overloads, string IsCallOf)> methods, string facadeName, string resultName,
		string mockRegistryName)
	{
		foreach ((Method method, string[] parameterTypes, _, string isCallOf) in methods)
		{
			string invocation = Helpers.GetUniqueLocalVariableName("invocation", method.Parameters);
			if (HasGenericParametersOverload(methods, method))
			{
				sb.Append("\t\t/// <inheritdoc />").AppendLine();
				sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<").Append(resultName).Append("> ")
					.Append(facadeName).Append('.').Append(GetGenericFacadeName(method))
					.Append("(global::Mockolate.Parameters.IParameters parameters)").AppendLine();
				sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".VerifyGenericMethod<").Append(resultName)
					.Append(">(this, ").Append(GetGenericSetupName(method)).Append(", ")
					.Append(method.Parameters.Count).Append(", ").Append(isCallOf).Append(", ").Append(invocation)
					.Append(" => global::Mockolate.Internals.GenericMethodInvocations.Matches(parameters, ")
					.Append(invocation).Append(GetGenericParameterNames(method)).Append("), () => $\"")
					.Append(method.Name).Append("({parameters})\");").AppendLine();
				sb.AppendLine();
			}

			AppendVerify(parameterTypes, null);
			if (GetGenericMatcherOverload(method, parameterTypes) is (string[] matcherTypes, string typeParameters))
			{
				AppendVerify(matcherTypes, typeParameters);
			}

			void AppendVerify(string[] signatureTypes, string? genericTypeParameters)
			{
				sb.Append("\t\t/// <inheritdoc />").AppendLine();
				sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<").Append(resultName).Append("> ")
					.Append(facadeName).Append('.').Append(GetGenericFacadeName(method));
				if (genericTypeParameters is not null)
				{
					sb.Append('<').Append(genericTypeParameters).Append('>');
				}

				sb.Append('(');
				AppendGenericFacadeParameters(sb, method, signatureTypes);
				sb.Append(')').AppendLine();
				sb.Append("\t\t{").AppendLine();
				StringBuilder predicate = new();
				int index = 0;
				foreach (MethodParameter parameter in method.Parameters)
				{
					string match = Helpers.GetUniqueLocalVariableName($"match{index + 1}", method.Parameters);
					sb.Append("\t\t\tglobal::Mockolate.Parameters.IParameterMatch<").Append(parameterTypes[index])
						.Append("> ").Append(match).Append(" = ")
						.Append(GetGenericParameterMatch(method, parameter, parameterTypes[index],
							genericTypeParameters is not null))
						.Append(';').AppendLine();
					if (index > 0)
					{
						predicate.Append(" && ");
					}

					predicate.Append("global::Mockolate.Internals.GenericMethodInvocations.Matches(").Append(match)
						.Append(", ").Append(invocation).Append(", ").Append(index).Append(')');
					index++;
				}

				sb.Append("\t\t\treturn this.").Append(mockRegistryName).Append(".VerifyGenericMethod<").Append(resultName)
					.Append(">(this, ").Append(GetGenericSetupName(method)).Append(", ")
					.Append(method.Parameters.Count).Append(", ").Append(isCallOf).Append(", ")
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
			$"{mockRegistry}.GetMethodSetups<global::Mockolate.Setup.MethodSetup>({memberIdRef}, {nameVar}, {genericSetupName})");
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
