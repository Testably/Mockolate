using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Mockolate.Analyzers;

/// <summary>
///     An analyzer that reports a matcher passed to a <c>Setup</c> or <c>Verify</c> parameter that takes a value, where
///     it is compared as the expected value instead of matching.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MatcherAsValueAnalyzer : DiagnosticAnalyzer
{
	/// <summary>
	///     A matcher bound to a value parameter is compared as a value.
	/// </summary>
	/// <remarks>
	///     Declared here instead of in <see cref="Rules" />, because the release tracking analyzer reports RS2002 for an
	///     unshipped rule declared in another file.
	/// </remarks>
	public static readonly DiagnosticDescriptor MatcherAsValueRule = new(
		"Mockolate0007",
		new LocalizableResourceString(nameof(Resources.Mockolate0007Title),
			Resources.ResourceManager, typeof(Resources)),
		new LocalizableResourceString(nameof(Resources.Mockolate0007MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
		"Usage",
		DiagnosticSeverity.Warning,
isEnabledByDefault: true,
		new LocalizableResourceString(nameof(Resources.Mockolate0007Description),
			Resources.ResourceManager, typeof(Resources)));

	/// <summary>
	///     The prefixes of the generated interfaces that declare the <c>Setup</c> and <c>Verify</c> members, followed by
	///     the name of the mocked type, which can itself contain <c>SetupFor</c> or <c>VerifyFor</c>.
	/// </summary>
	private static readonly string[] SetupOrVerifyInterfacePrefixes =
	[
		"IMockSetupFor", "IMockProtectedSetupFor", "IMockStaticSetupFor",
		"IMockVerifyFor", "IMockProtectedVerifyFor", "IMockStaticVerifyFor",
	];

	/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics" />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
	[
		MatcherAsValueRule,
	];

	/// <inheritdoc cref="DiagnosticAnalyzer.Initialize(AnalysisContext)" />
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(compilationContext =>
		{
			Compilation compilation = compilationContext.Compilation;
			INamedTypeSymbol? parameter = compilation.GetTypeByMetadataName("Mockolate.Parameters.IParameter");
			INamedTypeSymbol? typedParameter = compilation.GetTypeByMetadataName("Mockolate.Parameters.IParameter`1");
			INamedTypeSymbol? parameters = compilation.GetTypeByMetadataName("Mockolate.Parameters.IParameters");
			if (parameter is null || typedParameter is null || parameters is null)
			{
				return;
			}

			MatcherTypes matcherTypes = new(compilation, parameter, typedParameter, parameters);
			compilationContext.RegisterOperationAction(c => AnalyzeArgument(c, matcherTypes), OperationKind.Argument);
		});
	}

	private static void AnalyzeArgument(OperationAnalysisContext context, MatcherTypes matcherTypes)
	{
		if (context.Operation is not IArgumentOperation { Parameter: { } parameter, } argument ||
		    !IsSetupOrVerifyMember(parameter.ContainingSymbol))
		{
			return;
		}

		ITypeSymbol declaredType = parameter.OriginalDefinition.Type;
		if (GetParamsElements(argument) is { } elements)
		{
			if (parameter.Type is IArrayTypeSymbol { ElementType: { } elementType, } &&
			    declaredType is IArrayTypeSymbol { ElementType: { } declaredElementType, })
			{
				foreach (IOperation element in elements)
				{
					AnalyzeValue(context, matcherTypes, parameter, argument, element, elementType, declaredElementType);
				}
			}

			return;
		}

		AnalyzeValue(context, matcherTypes, parameter, argument, argument.Value, parameter.Type, declaredType);
	}

	private static void AnalyzeValue(OperationAnalysisContext context, MatcherTypes matcherTypes,
		IParameterSymbol parameter, IArgumentOperation argument, IOperation value, ITypeSymbol slotType,
		ITypeSymbol declaredSlotType)
	{
		IOperation operand = Unwrap(value);

		// The declared type tells a value parameter from a matcher parameter: a type argument inferred from the
		// matcher turns a value parameter into one of a matcher type.
		if (operand.Type is not { } matcherType || !matcherTypes.IsMatcherOrParameters(matcherType) ||
		    ContainsErrorType(matcherType) || matcherTypes.IsMatcherOrParameters(declaredSlotType))
		{
			return;
		}

		// The value of a union parameter is a matcher only when the matcher is one for the value type.
		if (IsParameterArg(slotType, out ITypeSymbol valueType))
		{
			if (!matcherTypes.IsMatcherFor(matcherType, valueType))
			{
				Report(context, matcherTypes, argument, value, matcherType, valueType);
			}

			return;
		}

		// The generated code and the library recognize a matcher for the value type at runtime, when the value or the
		// element of a `params` array is typed with a type parameter that can be `object`: on a generic method closed
		// over `object` and on `Set` of a property or indexer of a reference type. A matcher for the whole array only
		// matches the array as the only element; next to other elements it matches one element.
		if (declaredSlotType is ITypeParameterSymbol typeParameter && CanBeObject(typeParameter) &&
		    !(slotType.IsValueType && IsVerificationSet(parameter.ContainingSymbol)) &&
		    matcherTypes.IsMatcherFor(matcherType, slotType))
		{
			if (value != argument.Value && GetParamsElements(argument) is { Length: > 1, } &&
			    matcherTypes.IsMatcherFor(matcherType, parameter.Type))
			{
				Report(context, matcherTypes, argument, value, matcherType, slotType, isWholeArrayElement: true);
			}

			return;
		}

		Report(context, matcherTypes, argument, value, matcherType, slotType);
	}

	private static void Report(OperationAnalysisContext context, MatcherTypes matcherTypes,
		IArgumentOperation argument, IOperation value, ITypeSymbol matcherType, ITypeSymbol valueType,
		bool isWholeArrayElement = false)
	{
		IOperation operand = Unwrap(value);
		string typeName = valueType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
		// A type argument inferred from the matcher is the matcher type, so the matcher is one for the intended value
		// type, which the other arguments decide.
		bool isInferred = matcherTypes.IsMatcher(valueType) && HasInferredTypeArguments(argument);
		bool isInExplicitArray = argument.ArgumentKind != ArgumentKind.ParamArray && value != argument.Value;
		string hint;
		if (isWholeArrayElement)
		{
			hint = Resources.Mockolate0007WholeArrayHint;
		}
		else if (!matcherTypes.IsMatcher(matcherType))
		{
			hint = string.Format(Resources.Mockolate0007ParametersHint, typeName);
		}
		else if (!isInferred && !matcherTypes.IsMatcherFor(matcherType, valueType))
		{
			hint = string.Format(Resources.Mockolate0007ValueHint, typeName);
		}
		// A matcher for the value type is only compared as a value when the overload that takes values wins: either the
		// other arguments do not fit a matcher overload, or the matcher also converts to the value type, and the value
		// overload has the higher priority.
		else if (HasBlockingNeighbor(matcherTypes, argument, value))
		{
			hint = isInExplicitArray
				? Resources.Mockolate0007ParamsArrayAndOtherArgumentsHint
				: Resources.Mockolate0007OtherArgumentsHint;
		}
		else if (isInExplicitArray)
		{
			hint = Resources.Mockolate0007ParamsArrayHint;
		}
		else
		{
			hint = isInferred || matcherTypes.IsValueOfItsMatcher(valueType)
				? Resources.Mockolate0007NoMatcherOverloadHint
				: string.Format(Resources.Mockolate0007CastHint, typeName);
		}

		context.ReportDiagnostic(Diagnostic.Create(MatcherAsValueRule, operand.Syntax.GetLocation(),
			operand.Syntax.ToString(), typeName, hint));
	}

	/// <summary>
	///     Whether another argument or <c>params</c> element than <paramref name="value" /> is a plain value for a value
	///     parameter, or a matcher that is no matcher for its parameter.
	/// </summary>
	private static bool HasBlockingNeighbor(MatcherTypes matcherTypes, IArgumentOperation argument, IOperation value)
	{
		ImmutableArray<IArgumentOperation> arguments = argument.Parent switch
		{
			IInvocationOperation invocation => invocation.Arguments,
			IPropertyReferenceOperation propertyReference => propertyReference.Arguments,
			_ => ImmutableArray<IArgumentOperation>.Empty,
		};
		foreach (IArgumentOperation neighbor in arguments)
		{
			if (neighbor is { ArgumentKind: ArgumentKind.DefaultValue, } or { Parameter: null, })
			{
				continue;
			}

			if (GetParamsElements(neighbor) is { } elements &&
			    neighbor.Parameter.Type is IArrayTypeSymbol { ElementType: { } elementType, })
			{
				if (elements.Any(element =>
					    element != value && IsBlocking(matcherTypes, element, elementType)))
				{
					return true;
				}
			}
			else if (neighbor.Value != value && IsBlocking(matcherTypes, neighbor.Value, neighbor.Parameter.Type))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsBlocking(MatcherTypes matcherTypes, IOperation value, ITypeSymbol slotType)
	{
		bool isMatcherSlot = matcherTypes.IsMatcher(slotType);
		if (Unwrap(value).Type is not { } type || !matcherTypes.IsMatcher(type))
		{
			return !isMatcherSlot;
		}

		if (isMatcherSlot)
		{
			return false;
		}

		IsParameterArg(slotType, out ITypeSymbol valueType);
		return !matcherTypes.IsMatcherFor(type, valueType);
	}

	/// <summary>
	///     The elements of a <c>params</c> array, passed one by one, in an explicit array that can need a covariant
	///     conversion, or in a collection expression.
	/// </summary>
	private static ImmutableArray<IOperation>? GetParamsElements(IArgumentOperation argument)
		=> argument.Parameter is { IsParams: true, }
			? Unwrap(argument.Value) switch
			{
				IArrayCreationOperation { Initializer: { } initializer, } => initializer.ElementValues,
				ICollectionExpressionOperation collection => collection.Elements,
				_ => null,
			}
			: null;

	private static bool HasInferredTypeArguments(IArgumentOperation argument)
		=> argument.Parent is IInvocationOperation
		   {
			   TargetMethod.IsGenericMethod: true,
			   Syntax: InvocationExpressionSyntax
			   {
				   Expression: not (GenericNameSyntax
				   or MemberAccessExpressionSyntax { Name: GenericNameSyntax, }
				   or MemberBindingExpressionSyntax { Name: GenericNameSyntax, }),
			   },
		   };

	// A matcher for a type that does not resolve already fails to compile.
	private static bool ContainsErrorType(ITypeSymbol type)
		=> type switch
		{
			{ TypeKind: TypeKind.Error, } => true,
			IArrayTypeSymbol array => ContainsErrorType(array.ElementType),
			INamedTypeSymbol named => named.TypeArguments.Any(ContainsErrorType),
			_ => false,
		};

	private static IOperation Unwrap(IOperation value)
	{
		while (value is IConversionOperation { IsImplicit: true, Operand: { } inner, })
		{
			value = inner;
		}

		return value;
	}

	/// <summary>
	///     The generated <c>Setup</c> and <c>Verify</c> members are declared in <c>IMock…SetupFor…</c> and
	///     <c>IMock…VerifyFor…</c> interfaces nested in <c>Mockolate.Mock</c>; property and indexer writes are verified
	///     with <c>Set</c> of a <c>Mockolate.Verify</c> type.
	/// </summary>
	private static bool IsSetupOrVerifyMember(ISymbol member)
	{
		if (IsVerificationSet(member))
		{
			return true;
		}

		if (member.ContainingType is not { } containingType || !HasSetupOrVerifyInterfacePrefix(containingType.Name))
		{
			return false;
		}

		for (INamedTypeSymbol? type = containingType.ContainingType; type is not null; type = type.ContainingType)
		{
			if (type is { Name: "Mock", ContainingType: null, } &&
			    AnalyzerHelpers.IsMockolateNamespace(type.ContainingNamespace))
			{
				return true;
			}
		}

		return false;
	}

	// A loop instead of Any, which would allocate a closure for every argument in the compilation.
	private static bool HasSetupOrVerifyInterfacePrefix(string name)
	{
		foreach (string prefix in SetupOrVerifyInterfacePrefixes)
		{
			if (name.StartsWith(prefix, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     <c>Set</c> of a property or indexer verification, which unlike the generated generic methods treats no value of
	///     a value type as a matcher at runtime.
	/// </summary>
	private static bool IsVerificationSet(ISymbol member)
		=> member is IMethodSymbol { Name: "Set", ContainingType.ContainingNamespace: { Name: "Verify", } ns, } &&
		   AnalyzerHelpers.IsMockolateNamespace(ns.ContainingNamespace);

	/// <summary>
	///     Mirrors the generator, which checks for a matcher at runtime exactly for these type parameters.
	/// </summary>
	private static bool CanBeObject(ITypeParameterSymbol typeParameter)
		=> !typeParameter.HasValueTypeConstraint && !typeParameter.AllowsRefLikeType &&
		   typeParameter.ConstraintTypes.All(constraintType => constraintType is ITypeParameterSymbol other
			   ? CanBeObject(other)
			   : constraintType.SpecialType == SpecialType.System_Object);

	private static bool IsParameterArg(ITypeSymbol type, out ITypeSymbol valueType)
	{
		if (type is INamedTypeSymbol { IsValueType: true, } nullable &&
		    nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
		{
			type = nullable.TypeArguments[0];
		}

		if (type is INamedTypeSymbol { Name: "ParameterArg", TypeArguments.Length: 1, } parameterArg &&
		    AnalyzerHelpers.IsMockolateNamespace(parameterArg.ContainingNamespace))
		{
			valueType = parameterArg.TypeArguments[0];
			return true;
		}

		valueType = type;
		return false;
	}

	private sealed class MatcherTypes(
		Compilation compilation,
		INamedTypeSymbol parameter,
		INamedTypeSymbol typedParameter,
		INamedTypeSymbol parameters)
	{
		public bool IsMatcher(ITypeSymbol type)
			=> Implements(type, parameter);

		/// <summary>
		///     Whether <paramref name="type" /> is a matcher or a parameters match such as <c>Match.AnyParameters()</c>,
		///     which only matches any parameters as the only argument.
		/// </summary>
		public bool IsMatcherOrParameters(ITypeSymbol type)
			=> Implements(type, parameter) || Implements(type, parameters);

		private static bool Implements(ITypeSymbol type, INamedTypeSymbol @interface)
			=> SymbolEqualityComparer.Default.Equals(type, @interface) ||
			   type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, @interface));

		/// <summary>
		///     Whether <paramref name="matcherType" /> converts to <c>IParameter&lt;T&gt;</c> of
		///     <paramref name="valueType" />, which is covariant for reference types.
		/// </summary>
		public bool IsMatcherFor(ITypeSymbol matcherType, ITypeSymbol valueType)
			=> compilation.ClassifyCommonConversion(matcherType, typedParameter.Construct(valueType)).IsImplicit;

		/// <summary>
		///     Whether <c>IParameter&lt;T&gt;</c> of <paramref name="valueType" /> converts to
		///     <paramref name="valueType" />, so that a cast to it still binds the overload that takes values.
		/// </summary>
		public bool IsValueOfItsMatcher(ITypeSymbol valueType)
			=> compilation.ClassifyCommonConversion(typedParameter.Construct(valueType), valueType).IsImplicit;
	}
}
