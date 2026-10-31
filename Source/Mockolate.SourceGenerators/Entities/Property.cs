using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Entities;

[DebuggerDisplay("{ContainingType}.{Name}")]
internal record Property
{
	public Property(IPropertySymbol propertySymbol, List<Property>? alreadyDefinedProperties, IAssemblySymbol? sourceAssembly = null)
	{
		Accessibility = propertySymbol.DeclaredAccessibility;
		OverrideAccessibility = Helpers.ResolveOverrideVisibility(
			Accessibility, propertySymbol.ContainingAssembly, sourceAssembly);
		UseOverride = propertySymbol.IsVirtual || propertySymbol.IsAbstract;
		string rawName = propertySymbol.ExplicitInterfaceImplementations.Length > 0 ? propertySymbol.ExplicitInterfaceImplementations[0].Name : propertySymbol.Name;
		Name = propertySymbol.IsIndexer ? rawName : Helpers.EscapeIfKeyword(rawName);
		Type = Type.From(propertySymbol.Type);
		ContainingType = propertySymbol.ContainingType.ToQualifiedDisplayString(Helpers.TypeDisplayFormat);
		DeclaredContainingType = ContainingType;
		IsIndexer = propertySymbol.IsIndexer;
		IsAbstract = propertySymbol.IsAbstract;
		IsStatic = propertySymbol.IsStatic;
		if (IsIndexer && propertySymbol.Parameters.Length > 0)
		{
			IndexerParameters = new EquatableArray<MethodParameter>(
				propertySymbol.Parameters.Select(MethodParameter.From).ToArray());
		}

		Attributes = propertySymbol.GetAttributes().ToAttributeArray(sourceAssembly);

		if (alreadyDefinedProperties is not null)
		{
			if (alreadyDefinedProperties.Any(p => p.Name == Name))
			{
				ExplicitImplementation = ContainingType;
			}

			alreadyDefinedProperties.Add(this);
		}

		bool getterOverridable = propertySymbol.GetMethod is not { } getterSymbol ||
		                         Helpers.IsOverridableFrom(getterSymbol, sourceAssembly);
		bool setterOverridable = propertySymbol.SetMethod is not { } setterSymbol ||
		                         Helpers.IsOverridableFrom(setterSymbol, sourceAssembly);
		IsOverridableFromMock = getterOverridable && setterOverridable &&
		                        Helpers.IsOverridableFrom(propertySymbol, sourceAssembly) &&
		                        Helpers.HasAccessibleSignature(propertySymbol, sourceAssembly);

		Getter = propertySymbol.GetMethod is { } getter && getterOverridable
			? new Method(getter, null, sourceAssembly)
			: null;
		Setter = propertySymbol.SetMethod is { } setter && setterOverridable
			? new Method(setter, null, sourceAssembly)
			: null;
	}

	public static IEqualityComparer<Property> EqualityComparer { get; } = new PropertyEqualityComparer();

	public static IEqualityComparer<Property> ContainingTypeIndependentEqualityComparer { get; } =
		new ContainingTypeIndependentPropertyEqualityComparer();

	public bool IsIndexer { get; }
	public bool IsOverridableFromMock { get; }
	public bool IsAbstract { get; }
	public bool IsStatic { get; }
	public bool IsProtected => Accessibility is Accessibility.Protected or Accessibility.ProtectedOrInternal
		or Accessibility.ProtectedAndInternal;

	public MemberType MemberType => (IsStatic, IsProtected) switch
	{
		(true, _) => MemberType.Static,
		(_, true) => MemberType.Protected,
		(_, _) => MemberType.Public,
	};

	public EquatableArray<MethodParameter>? IndexerParameters { get; }
	public Type Type { get; }
	public string ContainingType { get; init; }

	/// <inheritdoc cref="Method.DeclaredContainingType" />
	public string DeclaredContainingType { get; }

	public Method? Setter { get; }

	public Method? Getter { get; }

	public bool UseOverride { get; }

	public EquatableArray<Attribute>? Attributes { get; }

	public Accessibility Accessibility { get; }
	public string OverrideAccessibility { get; }
	public string Name { get; }
	public string? ExplicitImplementation { get; }

	internal string GetUniqueNameString()
		=> $"\"{ContainingType}.{Name}\"";

	private sealed class PropertyEqualityComparer : IEqualityComparer<Property>
	{
		public bool Equals(Property? x, Property? y)
		{
			if (x is null)
			{
				return y is null;
			}

			if (y is null || x.IsIndexer != y.IsIndexer || !x.ContainingType.Equals(y.ContainingType))
			{
				return false;
			}

			if (x.IsIndexer)
			{
				return x.IndexerParameters!.Value.Equals(y.IndexerParameters!.Value);
			}

			return x.Name.Equals(y.Name);
		}

		public int GetHashCode(Property obj) => obj.Name.GetHashCode();
	}

	private sealed class ContainingTypeIndependentPropertyEqualityComparer : IEqualityComparer<Property>
	{
		public bool Equals(Property? x, Property? y)
		{
			if (x is null)
			{
				return y is null;
			}

			if (y is null || x.IsIndexer != y.IsIndexer)
			{
				return false;
			}

			if (x.IsIndexer)
			{
				return x.IndexerParameters!.Value.Equals(y.IndexerParameters!.Value);
			}

			return x.Name.Equals(y.Name);
		}

		public int GetHashCode(Property obj) => obj.Name.GetHashCode();
	}
}
