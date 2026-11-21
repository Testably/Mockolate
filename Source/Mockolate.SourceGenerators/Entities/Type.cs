using Microsoft.CodeAnalysis;
using Mockolate.SourceGenerators.Internals;

namespace Mockolate.SourceGenerators.Entities;

internal record Type
{
	private Type(string fullname)
	{
		Fullname = fullname;
		DisplayName = fullname;
	}

	internal Type(ITypeSymbol typeSymbol)
	{
		// A dynamic argument would bind the generated calls at runtime, so it is emitted as the identical object type.
		if (typeSymbol.TypeKind == TypeKind.Dynamic)
		{
			CanBeNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
			Fullname = CanBeNullable ? "object?" : "object";
			DisplayName = Fullname;
			Namespace = "System";
			SpecialType = SpecialType.System_Object;
			GenericTypeParameters = new EquatableArray<Type>([]);
			MayHoldCancellationToken = true;
			return;
		}

		// Removes '*' from multi-dimensional array types
		Fullname = typeSymbol.ToQualifiedDisplayString(Helpers.TypeDisplayFormat).Replace("*", "");
		DisplayName = typeSymbol.ToDisplayString(Helpers.TypeDisplayShortFormat).Replace("*", "");
		Namespace = typeSymbol.ContainingNamespace?.ToString();
		if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
		{
			if (typeSymbol.IsTupleType)
			{
				TupleTypes = new EquatableArray<Type>(namedTypeSymbol.TupleElements
					.Select(x => From(x.Type))
					.ToArray());
			}

			GenericTypeParameters = new EquatableArray<Type>(namedTypeSymbol.TypeArguments
				.Select(From)
				.ToArray());
		}

		if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
		{
			ElementType = From(arrayTypeSymbol.ElementType);
		}

		SpecialGenericType = typeSymbol.GetSpecialType();
		SpecialType = typeSymbol.SpecialType;
		CanBeNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated ||
		                typeSymbol is INamedTypeSymbol
		                {
			                OriginalDefinition.SpecialType: SpecialType.System_Nullable_T,
		                };
		IsValueType = typeSymbol.IsValueType;
		IsTypeParameterThatCanBeObject = typeSymbol is ITypeParameterSymbol typeParameter &&
		                                 CanBeObject(typeParameter);
		// IsRefLikeType is true for ref structs (e.g. Span<T>, ReadOnlySpan<T>, Utf8JsonReader,
		// and any user-defined `ref struct`). Used by the generator to switch to the ref-struct-safe
		// setup pipeline for parameters that cannot flow through the regular IParameter<T> path
		// on TFMs predating C# 13's `allows ref struct` anti-constraint.
		IsRefStruct = typeSymbol.IsRefLikeType;
		// A lambda never converts to a union type, so union-mode setups keep the raw delegate type as the
		// value alternative of a delegate-typed parameter instead of offering a predicate overload.
		// System.Delegate / System.MulticastDelegate have TypeKind.Class but accept the same conversions.
		IsDelegate = typeSymbol.TypeKind == TypeKind.Delegate ||
		             typeSymbol.SpecialType is SpecialType.System_Delegate or SpecialType.System_MulticastDelegate;
		IsCancellationToken = IsCancellationTokenSymbol(typeSymbol);
		IsAutoMockable = MockGeneratorHelpers.IsAutoMockable(typeSymbol);
		IsInFrameworkNamespace = IsAutoMockable &&
		                         MockGeneratorHelpers.IsFrameworkNamespace(typeSymbol.ContainingNamespace);
		// An array counts, because a single array argument can bind to a `params object?[]` parameter as the array itself.
		MayHoldCancellationToken = IsCancellationToken ||
		                           typeSymbol.TypeKind is TypeKind.TypeParameter or TypeKind.Array ||
		                           typeSymbol.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType ||
		                           typeSymbol is INamedTypeSymbol
		                           {
			                           TypeArguments.Length: 1,
			                           Name: "Nullable" or "IEquatable",
		                           } wrapper && IsCancellationTokenSymbol(wrapper.TypeArguments[0]);
	}

	private static bool IsCancellationTokenSymbol(ITypeSymbol typeSymbol)
		=> typeSymbol is
		{
			Name: "CancellationToken",
			ContainingNamespace:
			{
				Name: "Threading", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true, },
			},
		};

	/// <summary>
	///     Whether the type is <see cref="System.Threading.CancellationToken" />.
	/// </summary>
	public bool IsCancellationToken { get; }

	/// <summary>
	///     Whether an un-configured member returning this type can return an auto-mock, apart from its namespace.
	/// </summary>
	public bool IsAutoMockable { get; }

	/// <summary>
	///     Whether the type is auto-mockable and lies in a framework namespace, so that it is only auto-mocked when its
	///     <see cref="Namespace" /> is included.
	/// </summary>
	public bool IsInFrameworkNamespace { get; }

	/// <summary>
	///     Whether an argument of the type can be or hold a <see cref="System.Threading.CancellationToken" /> at
	///     runtime.
	/// </summary>
	public bool MayHoldCancellationToken { get; }

	/// <summary>
	///     Whether the type is a type parameter that a call can close over <see langword="object" />, so that an argument
	///     for it accepts every matcher as a value.
	/// </summary>
	public bool IsTypeParameterThatCanBeObject { get; }

	public bool IsValueType { get; }
	public bool CanBeNullable { get; }
	public bool IsRefStruct { get; }
	public bool IsDelegate { get; }
	public SpecialType SpecialType { get; }
	public SpecialGenericType SpecialGenericType { get; }
	public EquatableArray<Type>? TupleTypes { get; }
	public EquatableArray<Type>? GenericTypeParameters { get; }

	/// <summary>
	///     The element type when this type is an array (e.g. <c>bool</c> for <c>bool[]</c>); otherwise <see langword="null" />.
	/// </summary>
	public Type? ElementType { get; }
	public string? Namespace { get; }

	internal static Type Void { get; } = new("void");

	public string Fullname { get; }

	public string DisplayName { get; }

	/// <summary>
	///     Shared factory: route through the per-compilation cache (when one is on this thread's
	///     scope) so identical ITypeSymbol references reuse a single Type record.
	/// </summary>
	internal static Type From(ITypeSymbol typeSymbol)
	{
		EntityCache? cache = EntityCache.Current;
		if (cache is null)
		{
			return new Type(typeSymbol);
		}

		return cache.GetOrAddType(typeSymbol, static s => new Type(s));
	}

	private static bool CanBeObject(ITypeParameterSymbol typeParameter)
		=> !typeParameter.HasValueTypeConstraint && !typeParameter.AllowsRefLikeType &&
		   typeParameter.ConstraintTypes.All(constraintType => constraintType is ITypeParameterSymbol other
			   ? CanBeObject(other)
			   : constraintType.SpecialType == SpecialType.System_Object);

	public override string ToString() => Fullname;
}
