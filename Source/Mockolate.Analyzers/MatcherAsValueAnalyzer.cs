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
		true,
		new LocalizableResourceString(nameof(Resources.Mockolate0007Description),
			Resources.ResourceManager, typeof(Resources)));

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
			if (parameter is null || typedParameter is null)
			{
				return;
			}

			MatcherTypes matcherTypes = new(compilation, parameter, typedParameter);
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
		if (argument.ArgumentKind == ArgumentKind.ParamArray)
		{
			if (argument.Value is IArrayCreationOperation { Initializer: { } initializer, } &&
			    parameter.Type is IArrayTypeSymbol { ElementType: { } elementType, } &&
			    declaredType is IArrayTypeSymbol { ElementType: { } declaredElementType, })
			{
				foreach (IOperation element in initializer.ElementValues)
				{
					AnalyzeValue(context, matcherTypes, parameter, argument, element, elementType, declaredElementType,
						true);
				}
			}

			return;
		}

		AnalyzeValue(context, matcherTypes, parameter, argument, argument.Value, parameter.Type, declaredType, false);
	}

	private static void AnalyzeValue(OperationAnalysisContext context, MatcherTypes matcherTypes,
		IParameterSymbol parameter, IArgumentOperation argument, IOperation value, ITypeSymbol slotType,
		ITypeSymbol declaredSlotType, bool isElement)
	{
		IOperation operand = Unwrap(value);

		// The declared type tells a value parameter from a matcher parameter: a type argument inferred from the
		// matcher turns a value parameter into one of a matcher type.
		if (operand.Type is not { } matcherType || !matcherTypes.IsMatcher(matcherType) ||
		    HasErrorTypeArgument(matcherType) || matcherTypes.IsMatcher(declaredSlotType))
		{
			return;
		}

		// The value of a union parameter is a matcher only when the matcher is one for the value type.
		if (IsParameterArg(slotType, out ITypeSymbol valueType))
		{
			if (!matcherTypes.IsMatcherFor(matcherType, valueType))
			{
				Report(context, matcherTypes, parameter, argument, value, operand, matcherType, valueType);
			}

			return;
		}

		// The generated code and the library recognize a matcher for the value type at runtime, when the value is typed
		// with a type parameter that can be `object`: on a generic method closed over `object` and on `Set` of an
		// `object` property or indexer.
		if (!isElement && declaredSlotType is ITypeParameterSymbol typeParameter && CanBeObject(typeParameter) &&
		    matcherTypes.IsMatcherFor(matcherType, slotType))
		{
			return;
		}

		Report(context, matcherTypes, parameter, argument, value, operand, matcherType, slotType);
	}

	private static void Report(OperationAnalysisContext context, MatcherTypes matcherTypes,
		IParameterSymbol parameter, IArgumentOperation argument, IOperation value, IOperation operand,
		ITypeSymbol matcherType, ITypeSymbol valueType)
	{
		string typeName = valueType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
		string hint;
		if (HasInferredMatcherTypeArgument(matcherTypes, parameter.ContainingSymbol, operand.Syntax))
		{
			hint = Resources.Mockolate0007InferredTypeArgumentsHint;
		}
		else if (!matcherTypes.IsMatcherFor(matcherType, valueType))
		{
			hint = string.Format(Resources.Mockolate0007ValueHint, typeName);
		}
		// A matcher for the value type is only compared as a value when the overload that takes values wins: either the
		// other arguments do not fit a matcher overload, or the matcher also converts to the value type, and the value
		// overload has the higher priority.
		else if (HasBlockingNeighbor(matcherTypes, argument, value))
		{
			hint = Resources.Mockolate0007OtherArgumentsHint;
		}
		else
		{
			hint = matcherTypes.IsValueOfItsMatcher(valueType)
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

			if (neighbor is
			    {
				    ArgumentKind: ArgumentKind.ParamArray,
				    Value: IArrayCreationOperation { Initializer: { } initializer, },
				    Parameter.Type: IArrayTypeSymbol { ElementType: { } elementType, },
			    })
			{
				if (initializer.ElementValues.Any(element =>
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

	// A matcher for a type that does not resolve already fails to compile.
	private static bool HasErrorTypeArgument(ITypeSymbol type)
		=> type is INamedTypeSymbol named && named.TypeArguments.Any(t => t.TypeKind == TypeKind.Error);

	private static IOperation Unwrap(IOperation value)
	{
		while (value is IConversionOperation { IsImplicit: true, Operand: { } inner, })
		{
			value = inner;
		}

		return value;
	}

	private static bool HasInferredMatcherTypeArgument(MatcherTypes matcherTypes, ISymbol member,
		SyntaxNode argumentSyntax)
		=> member is IMethodSymbol { IsGenericMethod: true, } method &&
		   method.TypeArguments.Any(matcherTypes.IsMatcher) &&
		   argumentSyntax.FirstAncestorOrSelf<ArgumentSyntax>()?.Parent?.Parent is InvocationExpressionSyntax
		   {
			   Expression: not (GenericNameSyntax
			   or MemberAccessExpressionSyntax { Name: GenericNameSyntax, }
			   or MemberBindingExpressionSyntax { Name: GenericNameSyntax, }),
		   };

	/// <summary>
	///     The generated <c>Setup</c> and <c>Verify</c> members are declared in <c>IMock…SetupFor…</c> and
	///     <c>IMock…VerifyFor…</c> interfaces nested in <c>Mockolate.Mock</c>; property and indexer writes are verified
	///     with <c>Set</c> of a <c>Mockolate.Verify</c> type.
	/// </summary>
	private static bool IsSetupOrVerifyMember(ISymbol member)
	{
		if (member is IMethodSymbol { Name: "Set", ContainingType.ContainingNamespace: { Name: "Verify", } ns, } &&
		    IsMockolateNamespace(ns.ContainingNamespace))
		{
			return true;
		}

		if (member.ContainingType is not { } containingType ||
		    !(containingType.Name.Contains("SetupFor") || containingType.Name.Contains("VerifyFor")))
		{
			return false;
		}

		for (INamedTypeSymbol? type = containingType.ContainingType; type is not null; type = type.ContainingType)
		{
			if (type is { Name: "Mock", ContainingType: null, } && IsMockolateNamespace(type.ContainingNamespace))
			{
				return true;
			}
		}

		return false;
	}

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
		    IsMockolateNamespace(parameterArg.ContainingNamespace))
		{
			valueType = parameterArg.TypeArguments[0];
			return true;
		}

		valueType = type;
		return false;
	}

	private static bool IsMockolateNamespace(INamespaceSymbol? ns)
		=> ns is { Name: "Mockolate", ContainingNamespace.IsGlobalNamespace: true, };

	private sealed class MatcherTypes(
		Compilation compilation,
		INamedTypeSymbol parameter,
		INamedTypeSymbol typedParameter)
	{
		public bool IsMatcher(ITypeSymbol type)
			=> SymbolEqualityComparer.Default.Equals(type, parameter) ||
			   type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, parameter));

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
