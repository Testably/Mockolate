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
			.Select(Type.From).ToArray());
	}

	public EquatableArray<Type> ConstraintTypes { get; }

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
		bool isUnconstrained = !ConstraintTypes.Any() && !IsStruct && !IsClass && !IsNotNull && !IsUnmanaged &&
		                       !HasConstructor && !AllowsRefStruct;

		if (isUnconstrained)
		{
			if (isExplicitInterfaceImpl)
			{
				// Explicit interface implementations need `where T : default` to disambiguate
				// nullability for unconstrained type parameters. Override methods don't.
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

		if (IsNotNull)
		{
			if (count++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append("notnull");
		}

		if (AllowsRefStruct)
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
}
