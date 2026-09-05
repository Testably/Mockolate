using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Mockolate.SourceGenerators.Entities;
using Type = Mockolate.SourceGenerators.Entities.Type;

namespace Mockolate.SourceGenerators.Sources;

/// <summary>
///     Union mode for the per-method setup and verify overloads. When the consuming compilation supports C# unions,
///     the classic matcher/value overload set (one overload per matcher-or-value assignment of the parameters) is
///     replaced by one overload per union-or-predicate assignment: a <c>ParameterArg&lt;T&gt;?</c> slot accepts an
///     <c>It</c> matcher or a literal value, a <c>Func&lt;T, bool&gt;</c> slot accepts a predicate. The overload count
///     stays at 2^n for n eligible parameters, but predicates become available at every call site.
/// </summary>
internal static partial class Sources
{
	/// <summary>
	///     How one parameter slot is rendered in a union-mode overload.
	/// </summary>
	private enum UnionSlot : byte
	{
		/// <summary>
		///     The parameter cannot carry a literal value (<c>ref</c>/<c>out</c>, Span): rendered exactly like the classic
		///     matcher slot.
		/// </summary>
		Fixed,

		/// <summary>
		///     <c>ParameterArg&lt;T&gt;?</c>: an <c>It</c> matcher or a literal value; <see langword="null" /> and
		///     <see langword="default" /> stand for the literal default value.
		/// </summary>
		Union,

		/// <summary>
		///     <c>Func&lt;T, bool&gt;</c>, forwarded to <c>It.Satisfies</c> together with the caller's argument expression.
		/// </summary>
		Predicate,

		/// <summary>
		///     The raw delegate type of a delegate-typed parameter: lambdas never convert to a union, so this keeps
		///     <c>Setup.Register(x =&gt; x &gt; 0)</c> binding as a literal value instead of being mistaken for a predicate.
		/// </summary>
		RawDelegate,
	}

	/// <summary>
	///     Whether <paramref name="method" /> gets the union-mode overload set instead of the classic one.
	///     Only methods whose name is unique on the mocked type qualify (<paramref name="hasUniqueName" />, see
	///     <see cref="HasUniqueMethodName" />): a union conversion loses to the identity and numeric conversions of a
	///     sibling overload, so <c>Setup.M(5)</c> on <c>M(int)</c>/<c>M(long)</c> would be ambiguous or bind to the
	///     wrong method. Generic methods keep the classic set because a union slot hides the type argument from type
	///     inference (<c>Setup.Method(1, "x")</c> would need explicit type arguments); <c>params</c> methods keep it
	///     because a <c>params T[]</c> value slot cannot survive inside a union type; ref-struct pipelines have no
	///     value overloads at all.
	/// </summary>
	private static bool UseUnionOverloads(Method method, bool hasUniqueName, bool useUnionOverloads)
		=> useUnionOverloads &&
		   hasUniqueName &&
		   !method.HasUnsupportedAllowsRefStructTypeParameter &&
		   (method.GenericParameters is null || method.GenericParameters.Value.Count == 0) &&
		   method.Parameters.Count > 0 &&
		   !method.Parameters.Any(p => p.NeedsRefStructPipeline() || p.IsParams) &&
		   method.Parameters.Any(p => p.CanUseNullableParameterOverload());

	/// <summary>
	///     Whether no other mockable method of <paramref name="class" /> shares the C# name of <paramref name="method" />.
	///     <see cref="Method.Name" /> carries the type parameter list of generic methods (<c>Foo&lt;T&gt;</c>), so the
	///     comparison strips it: a generic sibling is an overload for the compiler as well.
	/// </summary>
	private static bool HasUniqueMethodName(Class @class, Method method)
	{
		string bareName = BareName(method);
		return @class.AllMethods().Count(m => m.ExplicitImplementation is null && BareName(m) == bareName) == 1;

		static string BareName(Method m)
		{
			int typeParameterList = m.Name.IndexOf('<');
			return typeParameterList < 0 ? m.Name : m.Name.Substring(0, typeParameterList);
		}
	}

	/// <summary>
	///     Enumerates the slot assignments of the union-mode overload set, all-union first. Above
	///     <see cref="MaxExplicitParameters" /> only the all-union overload is emitted (it already covers matchers and
	///     values); predicates are not offered there.
	/// </summary>
	private static IEnumerable<UnionSlot[]> GenerateUnionSlotCombinations(EquatableArray<MethodParameter> parameters)
	{
		MethodParameter[] all = parameters.AsArray();
		int[] valueableIndices = all
			.Select((p, i) => (p, i))
			.Where(x => x.p.CanUseNullableParameterOverload())
			.Select(x => x.i)
			.ToArray();
		int totalCombos = all.Length <= MaxExplicitParameters ? 1 << valueableIndices.Length : 1;
		for (int combo = 0; combo < totalCombos; combo++)
		{
			UnionSlot[] slots = new UnionSlot[all.Length];
			for (int bit = 0; bit < valueableIndices.Length; bit++)
			{
				int index = valueableIndices[bit];
				UnionSlot predicateSlot = all[index].Type.IsDelegate ? UnionSlot.RawDelegate : UnionSlot.Predicate;
				slots[index] = (combo & (1 << bit)) == 0 ? UnionSlot.Union : predicateSlot;
			}

			yield return slots;
		}
	}

	// Priority hierarchy of the union-mode overloads, mirroring the classic set:
	//   all-union             : int.MaxValue (takes the classic all-values role: binds `Method(null, …)` / `Method(default, …)`)
	//   IParameters           : int.MaxValue - 1 (unchanged)
	//   all-union, object slot: parameterCount (an IParameters argument converts to object, so it must not outrank IParameters)
	//   with predicates       : count of union/fixed slots, so a null argument binds to the union-heavier overload
	private static string UnionOverloadPriority(EquatableArray<MethodParameter> parameters, UnionSlot[] slots)
	{
		int unionCount = slots.Count(s => s is UnionSlot.Union or UnionSlot.Fixed);
		if (unionCount < slots.Length)
		{
			return unionCount.ToString();
		}

		bool[] unionFlags = slots.Select(s => s == UnionSlot.Union).ToArray();
		return ParametersBlockAllValuesPromotion(parameters, unionFlags) ? unionCount.ToString() : "int.MaxValue";
	}

	private static string UnionArgumentLocalName(Method method, MethodParameter parameter)
		=> CreateUniqueParameterName(method.Parameters, $"{parameter.Name}Arg");

	private static string UnionExpressionParameterName(Method method, MethodParameter parameter)
		=> CreateUniqueParameterName(method.Parameters, $"{parameter.Name}Expression");

	private static void AppendUnionSummary(StringBuilder sb, Class @class, Method method, string? methodNameOverride,
		bool isVerify)
	{
		string action = isVerify ? "Verify invocations for" : "Setup for";
		sb.Append("\t\t/// <summary>").AppendLine();
		if (methodNameOverride is null)
		{
			sb.Append("\t\t///     ").Append(action).Append(" the method <see cref=\"")
				.Append(method.DeclaredContainingType.EscapeForXmlDoc()).Append(".")
				.Append(method.Name.EscapeForXmlDoc()).Append("(")
				.Append(string.Join(", ",
					method.Parameters.Select(p => p.RefKind.GetString() + p.Type.Fullname.EscapeForXmlDoc())))
				.Append(")\"/>");
		}
		else
		{
			sb.Append("\t\t///     ").Append(action).Append(" the delegate <see cref=\"")
				.Append(@class.ClassFullName.EscapeForXmlDoc())
				.Append("\"/>");
		}

		sb.Append(" with the given ")
			.Append(string.Join(", ", method.Parameters.Select(p => $"<paramref name=\"{p.Name}\"/>")))
			.Append(".").AppendLine();
		sb.Append("\t\t/// </summary>").AppendLine();
	}

	private static void AppendUnionOverloadRemark(StringBuilder sb, Method method, UnionSlot[] slots)
		=> AppendUnionOverloadRemark(sb, method.Parameters.AsArray().Select(p => p.Name).ToArray(), slots);

	private static void AppendUnionOverloadRemark(StringBuilder sb, string[] parameterNames, UnionSlot[] slots)
	{
		List<string> parts = [];
		AddPart(UnionSlot.Union, "an <see cref=\"global::Mockolate.It\" /> matcher or a direct value for {0}");
		AddPart(UnionSlot.Predicate, "a predicate for {0}");
		AddPart(UnionSlot.RawDelegate, "a delegate value for {0}");
		AddPart(UnionSlot.Fixed, "an <see cref=\"global::Mockolate.It\" /> matcher for {0}");
		string scope = slots.All(s => s == UnionSlot.Union) ? "every parameter" : string.Join(" and ", parts);
		sb.AppendXmlRemarks(
			$"This overload accepts {(slots.All(s => s == UnionSlot.Union) ? "an <see cref=\"global::Mockolate.It\" /> matcher or a direct value for " : "")}{scope}. A <see langword=\"null\" /> or <see langword=\"default\" /> argument stands for the literal default value.");

		void AddPart(UnionSlot slot, string format)
		{
			string[] names = parameterNames
				.Where((_, i) => slots[i] == slot)
				.Select(name => $"<paramref name=\"{name}\" />")
				.ToArray();
			if (names.Length > 0)
			{
				parts.Add(string.Format(format, string.Join(", ", names)));
			}
		}
	}

	private static void AppendUnionPriority(StringBuilder sb, Method method, UnionSlot[] slots)
		=> sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
			.Append(UnionOverloadPriority(method.Parameters, slots))
			.Append(")]").AppendLine();

	/// <summary>
	///     <c>&lt;TReturn, T1, …&gt;</c> for returning methods, <c>&lt;T1, …&gt;</c> for void methods.
	/// </summary>
	private static void AppendSetupTypeArguments(StringBuilder sb, Method method)
	{
		sb.Append('<');
		bool first = true;
		if (method.ReturnType != Type.Void)
		{
			AppendSetupReturnType(sb, method);
			first = false;
		}

		foreach (MethodParameter parameter in method.Parameters)
		{
			if (!first)
			{
				sb.Append(", ");
			}

			sb.AppendTypeOrWrapper(parameter.Type);
			first = false;
		}

		sb.Append('>');
	}

	private static void AppendUnionParameters(StringBuilder sb, Method method, UnionSlot[] slots, bool isDefinition,
		bool isVerify)
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		// Predicate and raw delegate slots have no default, so they end the optional suffix like value slots do.
		bool[] breaksDefaults = slots.Select(s => s is UnionSlot.Predicate or UnionSlot.RawDelegate).ToArray();
		bool[] hasTrailingDefault = isDefinition
			? ComputeTrailingDefaults(method.Parameters.AsSpan(), breaksDefaults)
			: new bool[parameters.Length];
		string[] names = parameters.Select(p => p.Name).ToArray();
		string[] expressionNames = parameters.Select(p => UnionExpressionParameterName(method, p)).ToArray();
		for (int i = 0; i < parameters.Length; i++)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}

			AppendUnionSlotParameter(sb, parameters[i], names[i], slots[i], isVerify);
			if (hasTrailingDefault[i])
			{
				sb.Append(" = null");
			}
		}

		AppendUnionExpressionParameters(sb, names, expressionNames, slots, isDefinition);
	}

	private static void AppendUnionSlotParameter(StringBuilder sb, MethodParameter parameter, string name,
		UnionSlot slot, bool isVerify)
	{
		switch (slot)
		{
			case UnionSlot.Union:
				sb.Append("global::Mockolate.ParameterArg<").Append(parameter.ToNullableType()).Append(">? ").Append(name);
				break;
			case UnionSlot.Predicate:
				sb.Append("global::System.Func<").Append(parameter.ToNullableType()).Append(", bool> ").Append(name);
				break;
			case UnionSlot.RawDelegate:
				sb.Append(parameter.ToNullableType()).Append(' ').Append(name);
				break;
			default:
				if (isVerify)
				{
					sb.AppendVerifyParameter(parameter);
				}
				else
				{
					sb.Append(parameter.ToParameter());
				}

				sb.Append(' ').Append(name);
				break;
		}
	}

	/// <summary>
	///     One trailing <c>string</c> per predicate slot carrying the caller's argument text; the definition adds the
	///     <c>[CallerArgumentExpression]</c> and the default, the explicit implementation just the parameter.
	/// </summary>
	private static void AppendUnionExpressionParameters(StringBuilder sb, string[] names, string[] expressionNames,
		UnionSlot[] slots, bool isDefinition)
	{
		for (int i = 0; i < names.Length; i++)
		{
			if (slots[i] != UnionSlot.Predicate)
			{
				continue;
			}

			sb.Append(", ");
			if (isDefinition)
			{
				// Parameter names are stored escaped (`@params`); the attribute needs the bare identifier.
				sb.Append("[global::System.Runtime.CompilerServices.CallerArgumentExpression(\"")
					.Append(names[i].TrimStart('@')).Append("\")] ");
			}

			sb.Append("string ").Append(expressionNames[i]);
			if (isDefinition)
			{
				sb.Append(" = \"\"");
			}
		}
	}

	/// <summary>
	///     <c>ParameterArg&lt;T&gt; xArg = x ?? …;</c> per union slot: an omitted or <see langword="null" /> argument falls
	///     back to the parameter's declared default value when it has one, otherwise to the literal <c>default(T)</c>.
	/// </summary>
	private static void AppendUnionArgumentLocals(StringBuilder sb, Method method, UnionSlot[] slots)
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		for (int i = 0; i < parameters.Length; i++)
		{
			if (slots[i] != UnionSlot.Union)
			{
				continue;
			}

			MethodParameter parameter = parameters[i];
			string type = parameter.ToNullableType();
			sb.Append("\t\t\tglobal::Mockolate.ParameterArg<").Append(type).Append("> ")
				.Append(UnionArgumentLocalName(method, parameter)).Append(" = ").Append(parameter.Name).Append(" ?? ");
			if (parameter.HasExplicitDefaultValue)
			{
				sb.Append("new global::Mockolate.ParameterArg<").Append(type).Append(">((").Append(type).Append(")(")
					.Append(parameter.ExplicitDefaultValue).Append("))");
			}
			else
			{
				sb.Append("default");
			}

			sb.Append(';').AppendLine();
		}
	}

	private static string UnionLiteralExpression(Method method, MethodParameter parameter, UnionSlot slot)
		=> slot == UnionSlot.Union ? $"{UnionArgumentLocalName(method, parameter)}.Literal!" : parameter.Name;

	private static void AppendUnionMatchExpression(StringBuilder sb, Method method, MethodParameter parameter,
		UnionSlot slot)
	{
		switch (slot)
		{
			case UnionSlot.Union:
				sb.Append(UnionArgumentLocalName(method, parameter)).Append(".ToParameterMatch()");
				break;
			case UnionSlot.Predicate:
				sb.Append("(global::Mockolate.Parameters.IParameterMatch<").Append(parameter.ToTypeOrWrapper())
					.Append(">)global::Mockolate.It.Satisfies<").Append(parameter.ToNullableType()).Append(">(")
					.Append(parameter.Name).Append(", ").Append(UnionExpressionParameterName(method, parameter)).Append(')');
				break;
			case UnionSlot.RawDelegate:
				AppendNamedValueParameter(sb, parameter);
				break;
			default:
				AppendNamedParameter(sb, parameter);
				break;
		}
	}

	private static string UnionExpectationLambda(Method method, UnionSlot[] slots)
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		IEnumerable<string> placeholders = parameters.Select((p, i) => slots[i] switch
		{
			UnionSlot.Union => $"{{{UnionArgumentLocalName(method, p)}}}",
			UnionSlot.Predicate => $"{{{UnionExpressionParameterName(method, p)}}}",
			_ => $"{{{p.Name}}}",
		});
		return $"() => $\"{method.Name}({string.Join(", ", placeholders)})\"";
	}

	private static string UnionLiteralCondition(Method method, UnionSlot[] slots)
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		return string.Join(" && ", parameters
			.Where((_, i) => slots[i] == UnionSlot.Union)
			.Select(p => $"{UnionArgumentLocalName(method, p)}.IsLiteral"));
	}

	private static void AppendUnionMethodSetupDefinition(StringBuilder sb, Class @class, Method method,
		UnionSlot[] slots, string? methodNameOverride = null)
	{
		AppendUnionSummary(sb, @class, method, methodNameOverride, isVerify: false);
		AppendUnionOverloadRemark(sb, method, slots);
		AppendUnionPriority(sb, method, slots);
		sb.Append(method.ReturnType != Type.Void
			? "\t\tglobal::Mockolate.Setup.IReturnMethodSetupParameterIgnorer"
			: "\t\tglobal::Mockolate.Setup.IVoidMethodSetupParameterIgnorer");
		AppendSetupTypeArguments(sb, method);
		sb.Append(' ').Append(methodNameOverride ?? method.Name).Append('(');
		AppendUnionParameters(sb, method, slots, isDefinition: true, isVerify: false);
		sb.Append(");").AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendUnionMethodSetupImplementation(StringBuilder sb, Method method, string mockRegistryName,
		string setupName, MemberIdTable memberIds, string memberIdPrefix, UnionSlot[] slots,
		string? methodNameOverride = null, string? scopeExpression = null)
#pragma warning restore S107
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		bool isVoid = method.ReturnType == Type.Void;
		string scopePrefix = scopeExpression is null ? "" : scopeExpression + ", ";
		string ignorerType = isVoid
			? "global::Mockolate.Setup.IVoidMethodSetupParameterIgnorer"
			: "global::Mockolate.Setup.IReturnMethodSetupParameterIgnorer";
		StringBuilder typeArguments = new();
		AppendSetupTypeArguments(typeArguments, method);
		string setupType = (isVoid ? "global::Mockolate.Setup.VoidMethodSetup" : "global::Mockolate.Setup.ReturnMethodSetup") +
		                   typeArguments;

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\t").Append(ignorerType).Append(typeArguments).Append(" global::Mockolate.Mock.").Append(setupName)
			.Append('.').Append(methodNameOverride ?? method.Name).Append('(');
		AppendUnionParameters(sb, method, slots, isDefinition: false, isVerify: false);
		sb.Append(')').AppendLine();
		sb.AppendLine("\t\t{");
		AppendUnionArgumentLocals(sb, method, slots);

		string methodSetupVar = Helpers.GetUniqueLocalVariableName("methodSetup", method.Parameters);
		string memberIdRef = memberIdPrefix + memberIds.GetMethodIdentifier(method);
		// Same fast path as the classic all-values overload: when every argument turns out to be a literal value the
		// setup stores the values directly (WithLiteralValues, arity 1..4) instead of allocating one matcher per slot.
		bool literalEligible = parameters.Length <= MaxExplicitParameters &&
		                       slots.All(s => s is UnionSlot.Union or UnionSlot.RawDelegate);
		string literalCondition = literalEligible ? UnionLiteralCondition(method, slots) : "";
		if (literalEligible && literalCondition.Length > 0)
		{
			sb.Append("\t\t\t").Append(setupType).Append(' ').Append(methodSetupVar).Append(';').AppendLine();
			sb.Append("\t\t\tif (").Append(literalCondition).Append(')').AppendLine();
			sb.AppendLine("\t\t\t{");
			sb.Append("\t\t\t\t").Append(methodSetupVar).Append(" = new ").Append(setupType);
			AppendLiteralSetup(sb);
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t\telse");
			sb.AppendLine("\t\t\t{");
			sb.Append("\t\t\t\t").Append(methodSetupVar).Append(" = new ").Append(setupType);
			AppendCollectionSetup(sb);
			sb.AppendLine("\t\t\t}");
		}
		else
		{
			sb.Append("\t\t\tvar ").Append(methodSetupVar).Append(" = new ").Append(setupType);
			if (literalEligible)
			{
				AppendLiteralSetup(sb);
			}
			else
			{
				AppendCollectionSetup(sb);
			}
		}

		sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(".SetupMethod(").Append(memberIdRef).Append(", ")
			.Append(scopePrefix).Append(methodSetupVar).Append(");").AppendLine();
		sb.Append("\t\t\treturn (").Append(ignorerType).Append(typeArguments).Append(')').Append(methodSetupVar)
			.Append(';').AppendLine();
		sb.AppendLine("\t\t}");
		sb.AppendLine();

		void AppendLiteralSetup(StringBuilder target)
		{
			target.Append(".WithLiteralValues(").Append(mockRegistryName).Append(", ")
				.Append(method.GetUniqueNameString());
			for (int i = 0; i < parameters.Length; i++)
			{
				target.Append(", ").Append(UnionLiteralExpression(method, parameters[i], slots[i]));
			}

			target.Append(");").AppendLine();
		}

		void AppendCollectionSetup(StringBuilder target)
		{
			target.Append(".WithParameterCollection(").Append(mockRegistryName).Append(", ")
				.Append(method.GetUniqueNameString());
			for (int i = 0; i < parameters.Length; i++)
			{
				target.Append(", ");
				AppendUnionMatchExpression(target, method, parameters[i], slots[i]);
			}

			target.Append(");").AppendLine();
		}
	}

	private static void AppendUnionMethodVerifyDefinition(StringBuilder sb, Class @class, Method method,
		string verifyName, UnionSlot[] slots, string? methodNameOverride = null)
	{
		AppendUnionSummary(sb, @class, method, methodNameOverride, isVerify: true);
		AppendUnionOverloadRemark(sb, method, slots);
		AppendUnionPriority(sb, method, slots);
		sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<").Append(verifyName)
			.Append(">.IgnoreParameters ").Append(methodNameOverride ?? method.Name).Append('(');
		AppendUnionParameters(sb, method, slots, isDefinition: true, isVerify: true);
		sb.Append(");").AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendUnionMethodVerifyImplementation(StringBuilder sb, Method method,
		string mockRegistryName, string verifyName, MemberIdTable memberIds, string memberIdPrefix,
		bool useFastBuffers, UnionSlot[] slots, string? methodNameOverride = null)
#pragma warning restore S107
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		bool useFastForMethod = useFastBuffers && IsFastBufferEligibleMethod(method);
		string methodMemberId = useFastForMethod
			? memberIdPrefix + memberIds.GetMethodIdentifier(method)
			: "-1";
		string typeArguments = string.Join(", ", parameters.Select(p => p.ToTypeOrWrapper()));
		string expectation = UnionExpectationLambda(method, slots);

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<").Append(verifyName).Append(">.IgnoreParameters ")
			.Append(verifyName).Append('.').Append(methodNameOverride ?? method.Name).Append('(');
		AppendUnionParameters(sb, method, slots, isDefinition: false, isVerify: true);
		sb.Append(')').AppendLine();
		sb.AppendLine("\t\t{");
		AppendUnionArgumentLocals(sb, method, slots);

		// Mirrors the classic verify paths: literal values go through the allocation-free VerifyMethod overload,
		// matchers through the typed overload when the member has a fast buffer, everything else through the
		// MethodInvocation predicate.
		bool noFixedSlots = slots.All(s => s != UnionSlot.Fixed);
		bool literalEligible = parameters.Length <= 4 && noFixedSlots &&
		                       slots.All(s => s is UnionSlot.Union or UnionSlot.RawDelegate);
		bool typedEligible = useFastForMethod && parameters.Length <= 4 && noFixedSlots;
		if (literalEligible)
		{
			string literalCondition = UnionLiteralCondition(method, slots);
			string indent = "\t\t\t";
			if (literalCondition.Length > 0)
			{
				sb.Append("\t\t\tif (").Append(literalCondition).Append(')').AppendLine();
				sb.AppendLine("\t\t\t{");
				indent = "\t\t\t\t";
			}

			sb.Append(indent).Append("return this.").Append(mockRegistryName).Append(".VerifyMethod<").Append(verifyName)
				.Append(", ").Append(typeArguments).Append(">(this, ").Append(methodMemberId).Append(", ")
				.Append(method.GetUniqueNameString());
			for (int i = 0; i < parameters.Length; i++)
			{
				sb.Append(", ").Append(UnionLiteralExpression(method, parameters[i], slots[i]));
			}

			sb.Append(", ").Append(expectation).Append(");").AppendLine();
			if (literalCondition.Length == 0)
			{
				sb.AppendLine("\t\t}");
				sb.AppendLine();
				return;
			}

			sb.AppendLine("\t\t\t}");
		}

		if (typedEligible)
		{
			sb.Append("\t\t\treturn this.").Append(mockRegistryName).Append(".VerifyMethod<").Append(verifyName)
				.Append(", ").Append(typeArguments).Append(">(this, ").Append(methodMemberId).Append(", ")
				.Append(method.GetUniqueNameString());
			for (int i = 0; i < parameters.Length; i++)
			{
				sb.Append(", ");
				AppendUnionMatchExpression(sb, method, parameters[i], slots[i]);
			}

			sb.Append(", ").Append(expectation).Append(");").AppendLine();
		}
		else
		{
			for (int i = 0; i < parameters.Length; i++)
			{
				if (slots[i] is not (UnionSlot.Union or UnionSlot.Predicate))
				{
					continue;
				}

				sb.Append("\t\t\tglobal::Mockolate.Parameters.IParameterMatch<").Append(parameters[i].ToTypeOrWrapper())
					.Append("> ").Append(UnionMatchLocalName(method, parameters[i])).Append(" = ");
				AppendUnionMatchExpression(sb, method, parameters[i], slots[i]);
				sb.Append(';').AppendLine();
			}

			sb.Append("\t\t\treturn this.").Append(mockRegistryName).Append(".VerifyMethod<").Append(verifyName)
				.Append(", global::Mockolate.Interactions.MethodInvocation<").Append(typeArguments).Append(">>(this, ")
				.Append(methodMemberId).Append(", ").Append(method.GetUniqueNameString()).Append(", __i => ");
			for (int i = 0; i < parameters.Length; i++)
			{
				if (i > 0)
				{
					sb.Append(" && ");
				}

				sb.AppendLine().Append("\t\t\t\t");
				MethodParameter parameter = parameters[i];
				string type = parameter.ToTypeOrWrapper();
				string invocationValue = $"__i.Parameter{i + 1}";
				switch (slots[i])
				{
					case UnionSlot.Union:
					case UnionSlot.Predicate:
						sb.Append('(').Append(UnionMatchLocalName(method, parameter)).Append(".Matches(")
							.Append(invocationValue).Append("))");
						break;
					case UnionSlot.RawDelegate:
						sb.Append("(global::System.Collections.Generic.EqualityComparer<").Append(type)
							.Append(">.Default.Equals(").Append(parameter.Name).Append(", ").Append(invocationValue).Append("))");
						break;
					default:
						if (parameter.RefKind is RefKind.Out or RefKind.Ref or RefKind.RefReadOnlyParameter)
						{
							// out/ref verify parameters use IVerifyOutParameter<T> / IVerifyRefParameter<T>, which don't inherit
							// from IParameter<T>; keep the direct IParameterMatch<T> check like the classic overloads.
							sb.Append(
								$"({parameter.Name} is global::Mockolate.Parameters.IParameterMatch<{type}> {parameter.Name}Match ? {parameter.Name}Match.Matches({invocationValue}) : global::System.Collections.Generic.EqualityComparer<{type}>.Default.Equals({invocationValue}, default({type})))");
						}
						else
						{
							sb.Append(
								$"({parameter.Name} is not null ? CovariantParameterAdapter<{type}>.Wrap({parameter.Name}).Matches({invocationValue}) : global::System.Collections.Generic.EqualityComparer<{type}>.Default.Equals({invocationValue}, default({type})))");
						}

						break;
				}
			}

			sb.Append(", ").Append(expectation).Append(");").AppendLine();
		}

		sb.AppendLine("\t\t}");
		sb.AppendLine();
	}

	private static string UnionMatchLocalName(Method method, MethodParameter parameter)
		=> CreateUniqueParameterName(method.Parameters, $"{parameter.Name}Match");

	#region Indexers

	/// <summary>
	///     Whether the indexer gets the union-mode key overloads. Only an indexer that is the sole one of its key count
	///     qualifies (<paramref name="hasUniqueKeyCount" />, see <see cref="HasUniqueIndexerKeyCount" />): two same-arity
	///     indexers with convertible key types (<c>this[int]</c>/<c>this[long]</c>) would make <c>Setup[5]</c> ambiguous
	///     because a union conversion loses to the numeric conversion, while indexers of different arity never compete.
	///     Ref-struct keys have no value overloads at all and a <c>params</c> key cannot survive inside a union type.
	/// </summary>
	private static bool UseUnionIndexer(Property indexer, bool hasUniqueKeyCount, bool useUnionOverloads)
		=> useUnionOverloads &&
		   hasUniqueKeyCount &&
		   indexer.IndexerParameters is { } parameters &&
		   !parameters.Any(p => p.NeedsRefStructPipeline() || p.IsParams) &&
		   parameters.Any(p => p.CanUseNullableParameterOverload());

	private static bool HasUniqueIndexerKeyCount(Class @class, Property indexer)
	{
		int keyCount = indexer.IndexerParameters!.Value.Count;
		return @class.AllProperties().Count(p =>
			p.IsIndexer && p.ExplicitImplementation is null && p.IndexerParameters?.Count == keyCount) == 1;
	}

	// Indexers have no IParameters overload, so the all-union indexer simply takes the top priority the classic
	// all-matcher indexer has; combinations with predicates rank by their union/fixed slot count.
	private static string UnionIndexerPriority(UnionSlot[] slots)
	{
		int unionCount = slots.Count(s => s is UnionSlot.Union or UnionSlot.Fixed);
		return unionCount == slots.Length ? "int.MaxValue" : unionCount.ToString();
	}

	private static string[] UnionIndexerSetupNames(Property indexer)
		=> Enumerable.Range(1, indexer.IndexerParameters!.Value.Count).Select(i => $"parameter{i}").ToArray();

	private static string[] UnionIndexerVerifyNames(Property indexer)
		=> indexer.IndexerParameters!.Value.AsArray().Select(p => p.Name).ToArray();

	private static string[] UnionIndexerExpressionNames(Property indexer, string[] names)
		=> names.Select(name => CreateUniqueParameterName(indexer.IndexerParameters!.Value, $"{name}Expression"))
			.ToArray();

	private static void AppendUnionIndexerParameters(StringBuilder sb, Property indexer, string[] names,
		string[] expressionNames, UnionSlot[] slots, bool isDefinition, bool isVerify)
	{
		MethodParameter[] parameters = indexer.IndexerParameters!.Value.AsArray();
		for (int i = 0; i < parameters.Length; i++)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}

			AppendUnionSlotParameter(sb, parameters[i], names[i], slots[i], isVerify);
		}

		AppendUnionExpressionParameters(sb, names, expressionNames, slots, isDefinition);
	}

	/// <summary>
	///     The <c>IParameterMatch&lt;T&gt;</c> for one indexer key. Indexers have no literal fast path, so a union slot
	///     always goes through <c>ToParameterMatch()</c>; an omitted or <see langword="null" /> key is the literal
	///     <c>default(T)</c>.
	/// </summary>
	private static void AppendUnionIndexerKeyMatch(StringBuilder sb, MethodParameter parameter, string name,
		string expressionName, UnionSlot slot)
	{
		switch (slot)
		{
			case UnionSlot.Union:
				sb.Append('(').Append(name).Append(" ?? default).ToParameterMatch()");
				break;
			case UnionSlot.Predicate:
				sb.Append("(global::Mockolate.Parameters.IParameterMatch<").Append(parameter.ToTypeOrWrapper())
					.Append(">)global::Mockolate.It.Satisfies<").Append(parameter.ToNullableType()).Append(">(")
					.Append(name).Append(", ").Append(expressionName).Append(')');
				break;
			case UnionSlot.RawDelegate:
				AppendNamedValueParameter(sb, parameter, name);
				break;
			default:
				sb.Append("CovariantParameterAdapter<").Append(parameter.ToTypeOrWrapper()).Append(">.Wrap(")
					.Append(name).Append(')');
				break;
		}
	}

	private static void AppendUnionIndexerSetupDefinition(StringBuilder sb, Property indexer, UnionSlot[] slots)
	{
		string[] names = UnionIndexerSetupNames(indexer);
		sb.AppendXmlSummary(
			$"Setup for the {indexer.Type.Fullname.EscapeForXmlDoc()} indexer <see cref=\"{indexer.DeclaredContainingType.EscapeForXmlDoc()}.this[{string.Join(", ", indexer.IndexerParameters!.Value.Select(p => p.RefKind.GetString() + p.Type.Fullname.EscapeForXmlDoc()))}]\" />");
		AppendUnionOverloadRemark(sb, names, slots);
		sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
			.Append(UnionIndexerPriority(slots)).Append(")]").AppendLine();
		sb.Append("\t\t").Append(GetIndexerSetupType(indexer)).AppendTypeOrWrapper(indexer.Type);
		foreach (MethodParameter parameter in indexer.IndexerParameters!)
		{
			sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
		}

		sb.Append("> this[");
		AppendUnionIndexerParameters(sb, indexer, names, UnionIndexerExpressionNames(indexer, names), slots,
			isDefinition: true, isVerify: false);
		sb.Append("] { get; }").AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendUnionIndexerSetupImplementation(StringBuilder sb, Property indexer,
		string mockRegistryName, string setupName, MemberIdTable memberIds, string memberIdPrefix,
		UnionSlot[] slots, string? scopeExpression = null)
#pragma warning restore S107
	{
		MethodParameter[] parameters = indexer.IndexerParameters!.Value.AsArray();
		string[] names = UnionIndexerSetupNames(indexer);
		string[] expressionNames = UnionIndexerExpressionNames(indexer, names);
		string scopePrefix = scopeExpression is null ? "" : scopeExpression + ", ";
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append(
				"\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]")
			.AppendLine();
		sb.Append("\t\t").Append(GetIndexerSetupType(indexer)).AppendTypeOrWrapper(indexer.Type);
		foreach (MethodParameter parameter in parameters)
		{
			sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
		}

		sb.Append("> global::Mockolate.Mock.").Append(setupName).Append(".this[");
		AppendUnionIndexerParameters(sb, indexer, names, expressionNames, slots, isDefinition: false,
			isVerify: false);
		sb.Append("]").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tget").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tvar indexerSetup = new global::Mockolate.Setup.IndexerSetup<")
			.AppendTypeOrWrapper(indexer.Type);
		foreach (MethodParameter parameter in parameters)
		{
			sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
		}

		sb.Append(">(").Append(mockRegistryName);
		for (int i = 0; i < parameters.Length; i++)
		{
			sb.Append(", ");
			AppendUnionIndexerKeyMatch(sb, parameters[i], names[i], expressionNames[i], slots[i]);
		}

		sb.Append(");").AppendLine();
		sb.Append("\t\t\t\tthis.").Append(mockRegistryName).Append(".SetupIndexer(")
			.Append(memberIdPrefix).Append(memberIds.GetIndexerGetIdentifier(indexer)).Append(", ")
			.Append(scopePrefix).Append("indexerSetup);").AppendLine();
		sb.Append("\t\t\t\treturn indexerSetup;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	private static void AppendUnionIndexerVerifyDefinition(StringBuilder sb, Property indexer, string verifyName,
		UnionSlot[] slots)
	{
		string[] names = UnionIndexerVerifyNames(indexer);
		sb.AppendXmlSummary(
			$"Verify interactions with the {indexer.Type.Fullname.EscapeForXmlDoc()} indexer <see cref=\"{indexer.DeclaredContainingType.EscapeForXmlDoc()}.this[{string.Join(", ", indexer.IndexerParameters!.Value.Select(p => p.RefKind.GetString() + p.Type.Fullname.EscapeForXmlDoc()))}]\" />.");
		AppendUnionOverloadRemark(sb, names, slots);
		sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
			.Append(UnionIndexerPriority(slots)).Append(")]").AppendLine();
		sb.Append("\t\t");
		AppendIndexerVerifyType(sb, indexer, verifyName);
		sb.Append(" this[");
		AppendUnionIndexerParameters(sb, indexer, names, UnionIndexerExpressionNames(indexer, names), slots,
			isDefinition: true, isVerify: true);
		sb.Append("] { get; }").AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendUnionIndexerVerifyImplementation(StringBuilder sb, Property indexer,
		string mockRegistryName, string verifyName, MemberIdTable memberIds, string memberIdPrefix,
		bool useFastBuffers, UnionSlot[] slots)
#pragma warning restore S107
	{
		MethodParameter[] parameters = indexer.IndexerParameters!.Value.AsArray();
		string[] names = UnionIndexerVerifyNames(indexer);
		string[] expressionNames = UnionIndexerExpressionNames(indexer, names);
		bool useFastForIndexer = useFastBuffers && IsFastBufferEligibleIndexer(indexer);
		string indexerGetMemberId = useFastForIndexer
			? memberIdPrefix + memberIds.GetIndexerGetIdentifier(indexer)
			: "-1";
		string indexerSetMemberId = useFastForIndexer
			? memberIdPrefix + memberIds.GetIndexerSetIdentifier(indexer)
			: "-1";
		PropertyAccessors interceptedAccessors = GetInterceptedAccessors(indexer);

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append(
				"\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]")
			.AppendLine();
		sb.Append("\t\t");
		AppendIndexerVerifyType(sb, indexer, verifyName);
		sb.Append(' ').Append(verifyName).Append(".this[");
		AppendUnionIndexerParameters(sb, indexer, names, expressionNames, slots, isDefinition: false,
			isVerify: true);
		sb.Append("]").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tget").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		if (parameters.Length <= 4)
		{
			// Typed path: one IParameterMatch<T> per key, exactly like the classic matcher indexer.
			string typedVerifyType = interceptedAccessors switch
			{
				PropertyAccessors.GetOnly => "VerificationIndexerGetterResult",
				PropertyAccessors.SetOnly => "VerificationIndexerSetterResult",
				_ => "VerificationIndexerResult",
			};
			string verifyMemberIds = interceptedAccessors switch
			{
				PropertyAccessors.GetOnly => indexerGetMemberId,
				PropertyAccessors.SetOnly => indexerSetMemberId,
				_ => $"{indexerGetMemberId}, {indexerSetMemberId}",
			};
			sb.Append("\t\t\t\treturn new global::Mockolate.Verify.").Append(typedVerifyType).Append('<')
				.Append(verifyName);
			foreach (MethodParameter parameter in parameters)
			{
				sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
			}

			if (interceptedAccessors != PropertyAccessors.GetOnly)
			{
				sb.Append(", ").AppendTypeOrWrapper(indexer.Type);
			}

			sb.Append(">(this, this.").Append(mockRegistryName)
				.Append(", ").Append(verifyMemberIds).Append(",").AppendLine();
			for (int i = 0; i < parameters.Length; i++)
			{
				sb.Append("\t\t\t\t\t");
				AppendUnionIndexerKeyMatch(sb, parameters[i], names[i], expressionNames[i], slots[i]);
				sb.Append(',').AppendLine();
			}
		}
		else
		{
			switch (interceptedAccessors)
			{
				case PropertyAccessors.GetOnly:
					sb.Append("\t\t\t\treturn new global::Mockolate.Verify.VerificationIndexerGetterResult<")
						.Append(verifyName);
					foreach (MethodParameter parameter in parameters)
					{
						sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
					}

					sb.Append(">(this, this.").Append(mockRegistryName)
						.Append(", ").Append(indexerGetMemberId).Append(",").AppendLine();
					break;
				case PropertyAccessors.SetOnly:
					sb.Append("\t\t\t\treturn new global::Mockolate.Verify.VerificationIndexerSetterResult<")
						.Append(verifyName);
					foreach (MethodParameter parameter in parameters)
					{
						sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
					}

					sb.Append(", ").AppendTypeOrWrapper(indexer.Type).Append(">(this, this.").Append(mockRegistryName)
						.Append(", ").Append(indexerSetMemberId).Append(",").AppendLine();
					break;
				default:
					sb.Append("\t\t\t\treturn new global::Mockolate.Verify.VerificationIndexerResult<").Append(verifyName)
						.Append(", ").AppendTypeOrWrapper(indexer.Type).Append(">(this, this.").Append(mockRegistryName)
						.Append(", ").Append(indexerGetMemberId).Append(", ").Append(indexerSetMemberId).Append(",").AppendLine();
					break;
			}

			if (interceptedAccessors != PropertyAccessors.SetOnly)
			{
				sb.Append("\t\t\t\t\tinteraction => interaction is global::Mockolate.Interactions.IndexerGetterAccess<")
					.Append(string.Join(", ", parameters.Select(p => p.ToTypeOrWrapper()))).Append("> g");
				AppendUnionIndexerKeyMatches(sb, parameters, names, expressionNames, slots, "g");
				sb.Append(",").AppendLine();
			}

			if (interceptedAccessors != PropertyAccessors.GetOnly)
			{
				sb.Append(
					"\t\t\t\t\t(interaction, value) => interaction is global::Mockolate.Interactions.IndexerSetterAccess<");
				foreach (MethodParameter parameter in parameters)
				{
					sb.AppendTypeOrWrapper(parameter.Type).Append(", ");
				}

				sb.AppendTypeOrWrapper(indexer.Type).Append("> s");
				AppendUnionIndexerKeyMatches(sb, parameters, names, expressionNames, slots, "s");
				sb.Append(" && value.Matches(s.TypedValue),").AppendLine();
			}
		}

		sb.Append("\t\t\t\t\t() => global::System.String.Format(\"[")
			.Append(string.Join(", ", Enumerable.Range(0, parameters.Length).Select(k => $"{{{k}}}")))
			.Append("]\"");
		for (int i = 0; i < parameters.Length; i++)
		{
			sb.Append(", ");
			switch (slots[i])
			{
				case UnionSlot.Union:
					sb.Append("(object?)(").Append(names[i]).Append(" ?? default)");
					break;
				case UnionSlot.Predicate:
					sb.Append("(object?)").Append(expressionNames[i]);
					break;
				default:
					sb.Append("(object?)").Append(names[i]).Append(" ?? \"null\"");
					break;
			}
		}

		sb.Append("));").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	private static void AppendUnionIndexerKeyMatches(StringBuilder sb, MethodParameter[] parameters, string[] names,
		string[] expressionNames, UnionSlot[] slots, string interactionVar)
	{
		for (int i = 0; i < parameters.Length; i++)
		{
			sb.Append(" && ");
			AppendUnionIndexerKeyMatch(sb, parameters[i], names[i], expressionNames[i], slots[i]);
			sb.Append(".Matches(").Append(interactionVar).Append(".Parameter").Append(i + 1).Append(')');
		}
	}

	#endregion Indexers
}
