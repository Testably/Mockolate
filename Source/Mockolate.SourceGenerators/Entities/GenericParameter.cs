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

		ConstraintTypes = new EquatableArray<Type>(typeSymbol.ConstraintTypes
			.Where(IsDeclarableConstraint).Select(Type.From).ToArray());
		HasInheritedOnlyConstraint = DependsOnInheritedOnlyConstraint(typeSymbol);
		DeclaresClassForInheritedOnlyConstraint = HasInheritedOnlyConstraint && IsReferenceType &&
		                                          !IsClass && !IsStruct && !IsUnmanaged &&
		                                          !typeSymbol.ConstraintTypes.Any(constraintType =>
			                                          constraintType.TypeKind == TypeKind.Class &&
			                                          IsDeclarableConstraint(constraintType));
	}

	/// <summary>
	///     The constraint types that can be written in a <c>where</c> clause, i.e. without those that C# accepts only
	///     when inherited (see <see cref="HasInheritedOnlyConstraint" />).
	/// </summary>
	public EquatableArray<Type> ConstraintTypes { get; }

	/// <summary>
	///     Whether the type parameter, directly or through another type parameter, is constrained to a type that C#
	///     accepts as a constraint only when it is inherited (CS0701, CS0702, CS0706): a sealed class, a struct or an
	///     array, which a <c>where U : T</c> becomes when the mocked type closes <c>T</c> over it.
	/// </summary>
	public bool HasInheritedOnlyConstraint { get; }

	/// <summary>
	///     Whether a declaration states <c>class</c> in place of the inherited-only constraints, so that the type
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
		else if (IsClass)
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

		if (AllowsRefStruct && !DeclaresClassForInheritedOnlyConstraint)
		{
			if (count++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append("allows ref struct");
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
			if (count > 0)
			{
				sb.Append(", ");
			}

			sb.Append("new()");
		}
	}

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
