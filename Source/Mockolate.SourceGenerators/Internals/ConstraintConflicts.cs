using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Internals;

/// <summary>
///     Finds the constraints of a method type parameter that contradict each other once the containing type is
///     closed, such as <c>where U : class, T</c> with <c>T</c> closed over <see langword="int" />. Every implementation
///     inherits the constraints and fails with CS0455, so no mock can implement the method.
/// </summary>
/// <remarks>
///     Linked into the analyzers, so that Mockolate0002 reports exactly the methods the generator cannot implement.
///     Follows the compiler's check of the class constraints, but only reports the conflicts it is sure of: interfaces
///     and type parameters never count, and constructions of the same generic type never conflict, as variance or
///     tuple element names may make them compatible.
/// </remarks>
internal static class ConstraintConflicts
{
	/// <summary>
	///     The pair of constraints of <paramref name="typeParameter" /> that no type argument can satisfy at once, or
	///     <see langword="null" /> when there is none.
	/// </summary>
	internal static (string First, string Second)? Find(ITypeParameterSymbol typeParameter)
	{
		ITypeSymbol? deducedBase = null;
		foreach (ITypeSymbol constraint in typeParameter.ConstraintTypes)
		{
			if (constraint.TypeKind is TypeKind.Interface or TypeKind.TypeParameter or TypeKind.Dynamic or TypeKind.Error)
			{
				continue;
			}

			if (FindKindConflict(typeParameter, constraint) is { } kind)
			{
				return (kind, constraint.ToDisplayString());
			}

			if (deducedBase is null || IsEncompassedBy(constraint, deducedBase))
			{
				deducedBase = constraint;
			}
			else if (!IsEncompassedBy(deducedBase, constraint))
			{
				return (deducedBase.ToDisplayString(), constraint.ToDisplayString());
			}
		}

		return null;
	}

	private static string? FindKindConflict(ITypeParameterSymbol typeParameter, ITypeSymbol constraint)
	{
		if (typeParameter.HasReferenceTypeConstraint && constraint.IsValueType)
		{
			return "class";
		}

		if (typeParameter.HasValueTypeConstraint && !CanBeValueType(constraint))
		{
			return typeParameter.HasUnmanagedTypeConstraint ? "unmanaged" : "struct";
		}

		return null;
	}

	private static bool CanBeValueType(ITypeSymbol constraint)
		=> constraint.IsValueType
			? !IsNullable(constraint)
			: constraint.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum;

	private static bool IsEncompassedBy(ITypeSymbol type, ITypeSymbol other)
	{
		if (type is IArrayTypeSymbol array && other is IArrayTypeSymbol otherArray)
		{
			return array.Rank == otherArray.Rank &&
			       IsArrayElementEncompassedBy(array.ElementType, otherArray.ElementType);
		}

		// A nullable value type boxes to whatever its underlying type boxes to.
		if (other.IsReferenceType && type is INamedTypeSymbol { TypeArguments.Length: 1, } nullable && IsNullable(nullable))
		{
			type = nullable.TypeArguments[0];
		}

		for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
		{
			if (IsSameOrSameGenericDefinition(current, other))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsArrayElementEncompassedBy(ITypeSymbol element, ITypeSymbol other)
	{
		if (IsSameOrSameGenericDefinition(element, other))
		{
			return true;
		}

		if (!element.IsReferenceType || !other.IsReferenceType)
		{
			return false;
		}

		if (element.TypeKind == TypeKind.TypeParameter || other.TypeKind == TypeKind.TypeParameter)
		{
			return true;
		}

		if (element.TypeKind == TypeKind.Interface)
		{
			return other.TypeKind == TypeKind.Interface || other.SpecialType == SpecialType.System_Object;
		}

		return other.TypeKind == TypeKind.Interface
			? element.AllInterfaces.Any(@interface
				=> SymbolEqualityComparer.Default.Equals(@interface.OriginalDefinition, other.OriginalDefinition))
			: IsEncompassedBy(element, other);
	}

	private static bool IsSameOrSameGenericDefinition(ITypeSymbol type, ITypeSymbol other)
		=> SymbolEqualityComparer.Default.Equals(type, other) ||
		   (type is INamedTypeSymbol { IsGenericType: true, } &&
		    SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, other.OriginalDefinition));

	private static bool IsNullable(ITypeSymbol type)
		=> type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
}
