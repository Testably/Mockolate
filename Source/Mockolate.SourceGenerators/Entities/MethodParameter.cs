using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mockolate.SourceGenerators.Internals;

namespace Mockolate.SourceGenerators.Entities;

internal readonly record struct MethodParameter
{
	public MethodParameter(IParameterSymbol parameterSymbol)
	{
		Type = Type.From(parameterSymbol.Type);
		Name = Helpers.EscapeIfKeyword(parameterSymbol.Name);
		RefKind = parameterSymbol.RefKind;
		IsNullableAnnotated = parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated &&
		                      !parameterSymbol.Type.IsValueType;
		IsParams = parameterSymbol.IsParams;
		HasExplicitDefaultValue = parameterSymbol.HasExplicitDefaultValue;
		if (HasExplicitDefaultValue)
		{
			string? explicitDefaultValue = SymbolDisplay.FormatPrimitive(parameterSymbol.ExplicitDefaultValue, quoteStrings: true, useHexadecimalNumbers: false);
			ITypeSymbol effectiveType = GetEffectiveType(parameterSymbol.Type);
			if (string.Equals(explicitDefaultValue, "null", StringComparison.Ordinal)
			    && parameterSymbol.Type is { IsValueType: true, } or ITypeParameterSymbol { IsReferenceType: false, }
			    && parameterSymbol.Type is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, })
			{
				ExplicitDefaultValue = "default";
			}
			else if (effectiveType.TypeKind == TypeKind.Enum && explicitDefaultValue is not (null or "null"))
			{
				string enumTypeName = effectiveType.ToQualifiedDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				ExplicitDefaultValue = explicitDefaultValue[0] == '-'
					? "(" + enumTypeName + ")(" + explicitDefaultValue + ")"
					: "(" + enumTypeName + ")" + explicitDefaultValue;
			}
			else if (GetNonFiniteLiteral(parameterSymbol.ExplicitDefaultValue) is { } nonFiniteLiteral)
			{
				ExplicitDefaultValue = nonFiniteLiteral;
			}
			else
			{
				ExplicitDefaultValue = AppendLiteralSuffix(explicitDefaultValue, effectiveType);
			}
		}
	}

	public string? ExplicitDefaultValue { get; }
	public bool HasExplicitDefaultValue { get; }
	public bool IsParams { get; }

	public bool IsNullableAnnotated { get; }
	public Type Type { get; }
	public string Name { get; }
	public RefKind RefKind { get; }

	internal static MethodParameter From(IParameterSymbol symbol)
	{
		EntityCache? cache = EntityCache.Current;
		if (cache is null)
		{
			return new MethodParameter(symbol);
		}

		return cache.GetOrAddParameter(symbol, static s => new MethodParameter(s));
	}

	/// <summary>
	///     SymbolDisplay.FormatPrimitive strips the 'm'/'f' type suffix from the literal, which would
	///     produce invalid C# (e.g. "decimal x = 19.95" is a narrowing conversion from double). Re-add
	///     the suffix based on the effective parameter type so the generated code compiles.
	/// </summary>
	private static string? AppendLiteralSuffix(string? value, ITypeSymbol effectiveType)
	{
		if (value is null or "null")
		{
			return value;
		}

		return effectiveType.SpecialType switch
		{
			SpecialType.System_Decimal => value + "m",
			SpecialType.System_Single => value + "f",
			_ => value,
		};
	}

	/// <summary>
	///     SymbolDisplay.FormatPrimitive spells NaN and the infinities as names that do not compile (CS0103).
	/// </summary>
	private static string? GetNonFiniteLiteral(object? value)
		=> value switch
		{
			double d when double.IsNaN(d) => "global::System.Double.NaN",
			double d when double.IsPositiveInfinity(d) => "global::System.Double.PositiveInfinity",
			double d when double.IsNegativeInfinity(d) => "global::System.Double.NegativeInfinity",
			float f when float.IsNaN(f) => "global::System.Single.NaN",
			float f when float.IsPositiveInfinity(f) => "global::System.Single.PositiveInfinity",
			float f when float.IsNegativeInfinity(f) => "global::System.Single.NegativeInfinity",
			_ => null,
		};

	private static ITypeSymbol GetEffectiveType(ITypeSymbol type)
		=> type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, } named
			? named.TypeArguments[0]
			: type;
}
