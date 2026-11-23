using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Internals;

/// <summary>
///     Selects the constructor that a class mock calls without arguments.
/// </summary>
/// <remarks>
///     Linked into the analyzers, so that Mockolate0010 judges the constructor that the generator selects.
/// </remarks>
internal static class MockConstructorSelector
{
	/// <summary>
	///     Resolves <c>new T()</c> like the C# compiler: the first declared constructor that can be called without
	///     arguments and that no other such constructor is better than. Where C# reports an ambiguity, this is the
	///     first declared of the ambiguous constructors.
	/// </summary>
	internal static int? FindConstructorWithoutArguments(IMethodSymbol[] constructors, Compilation compilation)
	{
		IMethodSymbol[] candidates = constructors.Where(IsCallableWithoutArguments).ToArray();
		for (int i = 0; i < constructors.Length; i++)
		{
			IMethodSymbol current = constructors[i];
			if (IsCallableWithoutArguments(current) &&
			    !candidates.Any(candidate => IsBetterWithoutArguments(candidate, current, compilation)))
			{
				return i;
			}
		}

		return null;

		static bool IsCallableWithoutArguments(IMethodSymbol constructor)
			=> constructor.Parameters.All(p => p.HasExplicitDefaultValue || p.IsParams);
	}

	/// <remarks>
	///     The better function member rules for a call without arguments: a constructor applicable in its normal form
	///     beats one with a <see langword="params" /> parameter, which is only applicable in its expanded form. Then one
	///     that needs no default values beats one that does. Two expanded forms with the same number of parameters
	///     are decided by the better collection conversion from the empty collection to their
	///     <see langword="params" /> types.
	/// </remarks>
	private static bool IsBetterWithoutArguments(IMethodSymbol constructor, IMethodSymbol other, Compilation compilation)
	{
		bool isExpanded = constructor.Parameters.Any(p => p.IsParams);
		bool otherIsExpanded = other.Parameters.Any(p => p.IsParams);
		if (isExpanded != otherIsExpanded)
		{
			return otherIsExpanded;
		}

		bool needsDefaults = constructor.Parameters.Any(p => p.HasExplicitDefaultValue);
		bool otherNeedsDefaults = other.Parameters.Any(p => p.HasExplicitDefaultValue);
		if (needsDefaults != otherNeedsDefaults)
		{
			return otherNeedsDefaults;
		}

		if (!isExpanded || constructor.Parameters.Length != other.Parameters.Length)
		{
			return false;
		}

		ITypeSymbol paramsType = constructor.Parameters[constructor.Parameters.Length - 1].Type;
		ITypeSymbol otherParamsType = other.Parameters[other.Parameters.Length - 1].Type;
		return IsBetterCollectionConversion(paramsType, otherParamsType, compilation) &&
		       !IsBetterCollectionConversion(otherParamsType, paramsType, compilation);
	}

	/// <remarks>
	///     A <c>ReadOnlySpan&lt;E&gt;</c> beats a <c>Span&lt;E&gt;</c>, a span beats an array or array interface of the
	///     identical element type, and of two other types the one that implicitly converts to the other wins.
	/// </remarks>
	private static bool IsBetterCollectionConversion(ITypeSymbol type, ITypeSymbol other, Compilation compilation)
	{
		INamedTypeSymbol? span = compilation.GetTypeByMetadataName("System.Span`1");
		INamedTypeSymbol? readOnlySpan = compilation.GetTypeByMetadataName("System.ReadOnlySpan`1");
		ITypeSymbol? readOnlySpanElement = GetElementType(type, readOnlySpan);
		ITypeSymbol? element = readOnlySpanElement ?? GetElementType(type, span);
		if (element is null)
		{
			return GetElementType(other, span) is null && GetElementType(other, readOnlySpan) is null &&
			       compilation.ClassifyCommonConversion(type, other).IsImplicit;
		}

		ITypeSymbol? otherElement = GetArrayOrArrayInterfaceElementType(other) ??
		                            (readOnlySpanElement is not null ? GetElementType(other, span) : null);
		return otherElement is not null && compilation.ClassifyCommonConversion(element, otherElement).IsIdentity;

		static ITypeSymbol? GetElementType(ITypeSymbol type, INamedTypeSymbol? genericType)
			=> genericType is not null && type is INamedTypeSymbol named &&
			   SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, genericType)
				? named.TypeArguments[0]
				: null;

		static ITypeSymbol? GetArrayOrArrayInterfaceElementType(ITypeSymbol type)
			=> type switch
			{
				IArrayTypeSymbol { Rank: 1, } array => array.ElementType,
				INamedTypeSymbol
				{
					OriginalDefinition.SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T
					or SpecialType.System_Collections_Generic_IReadOnlyCollection_T
					or SpecialType.System_Collections_Generic_IReadOnlyList_T
					or SpecialType.System_Collections_Generic_ICollection_T
					or SpecialType.System_Collections_Generic_IList_T,
				} named => named.TypeArguments[0],
				_ => null,
			};
	}
}
