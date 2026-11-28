using System.Text;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Entities;

internal readonly record struct GenericParameter
{
	public GenericParameter(ITypeParameterSymbol typeSymbol)
	{
		Name = Helpers.EscapeIfKeyword(typeSymbol.Name);
		IsUnmanaged = typeSymbol.HasUnmanagedTypeConstraint;
		IsClass = typeSymbol.HasReferenceTypeConstraint;
		IsStruct = typeSymbol.HasValueTypeConstraint;
		IsNotNull = typeSymbol.HasNotNullConstraint;
		HasConstructor = typeSymbol.HasConstructorConstraint;
		AllowsRefStruct = typeSymbol.AllowsRefLikeType;
		NullableAnnotation = typeSymbol.ReferenceTypeConstraintNullableAnnotation;
		IsReferenceType = typeSymbol.IsReferenceType;
		IsValueType = typeSymbol.IsValueType;

		ITypeSymbol[] declarableConstraintTypes = typeSymbol.ConstraintTypes.Where(IsDeclarableConstraint).ToArray();
		ITypeSymbol[] constraintTypes = declarableConstraintTypes
			.GroupBy<ITypeSymbol, ITypeSymbol>(constraintType => constraintType, SymbolEqualityComparer.Default)
			.Select(copies => copies.FirstOrDefault(copy => copy.NullableAnnotation != NullableAnnotation.Annotated) ??
			                  copies.First())
			.Where(constraintType => !declarableConstraintTypes.Any(other => DerivesFrom(other, constraintType)))
			.OrderBy(constraintType => constraintType.TypeKind != TypeKind.Class)
			.ToArray();
		ConstraintTypes = new EquatableArray<Type>(constraintTypes.Select(Type.From).ToArray());
		OmitsClassConstraint = IsClass && constraintTypes.Any(constraintType =>
			constraintType.TypeKind == TypeKind.Class && constraintType.SpecialType is not
				(SpecialType.System_Enum or SpecialType.System_Delegate or SpecialType.System_MulticastDelegate));
		OmitsConstraint = OmitsClassConstraint || OmitsAllowsRefStruct ||
		                  constraintTypes.Length < declarableConstraintTypes.Length;
		HasInheritedOnlyConstraint = DependsOnInheritedOnlyConstraint(typeSymbol);
		DeclaresClassForInheritedOnlyConstraint = HasInheritedOnlyConstraint && IsReferenceType &&
		                                          !IsClass && !IsStruct && !IsUnmanaged &&
		                                          !typeSymbol.ConstraintTypes.Any(constraintType =>
			                                          constraintType.TypeKind == TypeKind.Class &&
			                                          IsDeclarableConstraint(constraintType));
	}

	/// <summary>
	///     Whether the <c>where</c> clause omits the <see langword="class" /> constraint, because one of the
	///     <see cref="ConstraintTypes" /> is a class type that C# does not accept together with it (CS0450) and that
	///     already implies it. A <c>where U : class, T</c> gets such a class type when the mocked type closes <c>T</c>
	///     over a class.
	/// </summary>
	public bool OmitsClassConstraint { get; }

	/// <summary>
	///     Whether the <c>where</c> clause omits a constraint of the type parameter that C# does not accept once the mocked
	///     type closes a type parameter it depends on: the <see langword="class" /> constraint
	///     (see <see cref="OmitsClassConstraint" />), <c>allows ref struct</c> for a reference type (CS9243), or a
	///     constraint type that repeats another one, possibly with another nullable annotation (CS0405), or a class type
	///     that is a base class of another one (CS0406). An implicit implementation that omits it does not match the
	///     implemented member (CS0425, CS8633).
	/// </summary>
	public bool OmitsConstraint { get; }

	private bool OmitsAllowsRefStruct => AllowsRefStruct && IsReferenceType && !IsClass;

	/// <summary>
	///     The constraint types that can be written in a <c>where</c> clause, i.e. without those that C# accepts only
	///     when inherited (see <see cref="HasInheritedOnlyConstraint" />) and those that <see cref="OmitsConstraint" />
	///     leaves out, with the class type first.
	/// </summary>
	public EquatableArray<Type> ConstraintTypes { get; }

	/// <summary>
	///     Whether the type parameter, directly or through another type parameter, is constrained to a type that C#
	///     accepts as a constraint only when it is inherited (CS0701, CS0702, CS0706): a sealed class, a struct or an
	///     array, which a <c>where U : T</c> becomes when the mocked type closes <c>T</c> over it.
	/// </summary>
	public bool HasInheritedOnlyConstraint { get; }

	/// <summary>
	///     Whether a declaration states <see langword="class" /> in place of the inherited-only constraints, so that the type
	///     parameter stays a reference type, as the <c>where T : class</c> of its explicit implementations requires.
	/// </summary>
	public bool DeclaresClassForInheritedOnlyConstraint { get; }

	public string Name { get; }
	public bool IsStruct { get; }
	public bool IsNotNull { get; }
	public bool IsUnmanaged { get; }
	public bool HasConstructor { get; }
	public bool AllowsRefStruct { get; }
	public NullableAnnotation NullableAnnotation { get; }
	public bool IsClass { get; }
	public bool IsReferenceType { get; }
	public bool IsValueType { get; }

	public void AppendWhereConstraint(StringBuilder sb, string prefix, bool inheritsConstraints = false, bool isExplicitInterfaceImpl = false)
	{
		bool hasDeclarableConstraint = ConstraintTypes.Any() || IsStruct || IsClass || IsNotNull || IsUnmanaged ||
		                               HasConstructor || AllowsRefStruct;

		if (!hasDeclarableConstraint && !HasInheritedOnlyConstraint)
		{
			if (isExplicitInterfaceImpl)
			{
				// Explicit interface implementations and overrides need `where T : default` for
				// unconstrained type parameters, because `T?` otherwise means `Nullable<T>` there.
				sb.AppendLine().Append(prefix).Append("where ").Append(Name).Append(" : default");
			}

			return;
		}

		if (inheritsConstraints)
		{
			// Constrained type parameters are inherited in override and explicit interface implementation methods (CS0460)
			if (isExplicitInterfaceImpl && !IsValueType)
			{
				// Without `class` or `default` a `T?` in the signature would bind to `Nullable<T>`.
				sb.AppendLine().Append(prefix).Append("where ").Append(Name)
					.Append(IsReferenceType ? " : class" : " : default");
			}

			return;
		}

		if (!hasDeclarableConstraint && !DeclaresClassForInheritedOnlyConstraint)
		{
			return;
		}

		int count = 0;
		sb.AppendLine().Append(prefix).Append("where ").Append(Name).Append(" : ");

		if (IsUnmanaged)
		{
			count++;
			sb.Append("unmanaged");
		}
		else if (IsStruct)
		{
			count++;
			sb.Append("struct");
		}
		else if (IsClass && !OmitsClassConstraint)
		{
			count++;
			sb.Append("class");
			if (NullableAnnotation == NullableAnnotation.Annotated)
			{
				sb.Append('?');
			}
		}
		else if (DeclaresClassForInheritedOnlyConstraint)
		{
			count++;
			sb.Append("class");
		}

		if (IsNotNull && !DeclaresClassForInheritedOnlyConstraint)
		{
			if (count++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append("notnull");
		}

		foreach (Type? constraintType in ConstraintTypes)
		{
			if (count++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append(constraintType.Fullname);
		}

		if (HasConstructor)
		{
			if (count++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append("new()");
		}

		if (AllowsRefStruct && !OmitsAllowsRefStruct)
		{
			if (count > 0)
			{
				sb.Append(", ");
			}

			sb.Append("allows ref struct");
		}
	}

	private static bool DerivesFrom(ITypeSymbol type, ITypeSymbol baseType)
		=> type.BaseType is { } directBaseType &&
		   (SymbolEqualityComparer.Default.Equals(directBaseType, baseType) || DerivesFrom(directBaseType, baseType));

	private static bool DependsOnInheritedOnlyConstraint(ITypeParameterSymbol typeParameter)
		=> typeParameter.ConstraintTypes.Any(constraintType => constraintType is ITypeParameterSymbol other
			? DependsOnInheritedOnlyConstraint(other)
			: !IsDeclarableConstraint(constraintType));

	private static bool IsDeclarableConstraint(ITypeSymbol constraintType)
		=> constraintType.TypeKind switch
		{
			TypeKind.Class => !constraintType.IsSealed && constraintType.SpecialType is not
				(SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Array),
			TypeKind.Interface or TypeKind.TypeParameter or TypeKind.Error => true,
			_ => false,
		};
}
