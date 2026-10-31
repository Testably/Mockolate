using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Entities;

[DebuggerDisplay("{DisplayString}")]
internal class Class : IEquatable<Class>
{
	private readonly IAssemblySymbol _sourceAssembly;
	private readonly int _surfaceHash;
	private List<Event>? _allEvents;
	private List<Method>? _allMethods;
	private List<Property>? _allProperties;
	private string? _classNameWithoutDots;

#pragma warning disable S107 // Methods should not have too many parameters
	public Class(ITypeSymbol type,
		IAssemblySymbol sourceAssembly,
		List<Method>? alreadyDefinedMethods = null,
		List<Property>? alreadyDefinedProperties = null,
		List<Event>? alreadyDefinedEvents = null,
		List<Method>? exceptMethods = null,
		List<Property>? exceptProperties = null,
		List<Event>? exceptEvents = null,
		List<Property>? filledProperties = null)
#pragma warning restore S107
	{
		_sourceAssembly = sourceAssembly;
		ClassFullName = type.ToQualifiedDisplayString(Helpers.TypeDisplayFormat);
		ClassName = GetTypeName(type);
		DisplayString = GetTypeFullName(type);

		INamedTypeSymbol? containingType = type.ContainingType;
		if (containingType is not null)
		{
			List<string> ancestors = new();
			while (containingType is not null)
			{
				ancestors.Add(containingType.Name);
				containingType = containingType.ContainingType;
			}

			StringBuilder nameBuilder = new();
			for (int i = ancestors.Count - 1; i >= 0; i--)
			{
				nameBuilder.Append(ancestors[i]).Append('.');
			}

			nameBuilder.Append(ClassName);
			ClassName = nameBuilder.ToString();
		}

		IsInterface = type.TypeKind == TypeKind.Interface;
		HasRequiredMembers = ComputeHasRequiredMembers(type);
		ImmutableArray<ISymbol> members = type.GetMembers();

		List<Method> methodIncludes = new();
		List<Property> propertyIncludes = new();
		List<Event> eventIncludes = new();
		List<Method> methodExceptCandidates = new();
		List<Property> propertyExceptCandidates = new();
		List<Event> eventExceptCandidates = new();
		List<Property> propertyFilledCandidates = new();

		foreach (ISymbol member in members)
		{
			if (FindPartlyReachableFilledSlot(member, sourceAssembly) is { } partlyReachableSlot)
			{
				propertyFilledCandidates.Add(new Property(partlyReachableSlot, null, sourceAssembly));
			}

			if (!FillsInaccessibleBaseSlot(member, sourceAssembly))
			{
				continue;
			}

			switch (member)
			{
				case IMethodSymbol methodSymbol:
					methodExceptCandidates.Add(new Method(methodSymbol, null, sourceAssembly));
					break;
				case IPropertySymbol propertySymbol:
					propertyExceptCandidates.Add(new Property(propertySymbol, null, sourceAssembly));
					break;
				case IEventSymbol { Type: INamedTypeSymbol { DelegateInvokeMethod: { } eventInvoke, }, } eventSymbol:
					eventExceptCandidates.Add(new Event(eventSymbol, eventInvoke, null, sourceAssembly));
					break;
			}
		}

		foreach (ISymbol member in members)
		{
			switch (member)
			{
				case IMethodSymbol methodSymbol when methodSymbol.MethodKind is MethodKind.Ordinary:
					{
						if (!methodSymbol.IsSealed && (IsInterface || methodSymbol.IsVirtual || methodSymbol.IsAbstract) &&
						    ShouldIncludeMember(methodSymbol))
						{
							methodIncludes.Add(new Method(methodSymbol, alreadyDefinedMethods, sourceAssembly));
						}

						if (methodSymbol.IsSealed || HidesBaseOverridable(methodSymbol, type))
						{
							methodExceptCandidates.Add(new Method(methodSymbol, null, sourceAssembly));
						}

						break;
					}

				case IPropertySymbol propertySymbol:
					{
						if (!propertySymbol.IsSealed && (IsInterface || propertySymbol.IsVirtual || propertySymbol.IsAbstract) &&
						    ShouldIncludeMember(propertySymbol))
						{
							propertyIncludes.Add(new Property(propertySymbol, alreadyDefinedProperties, sourceAssembly));
						}

						if (propertySymbol.IsSealed || HidesBaseOverridable(propertySymbol, type))
						{
							propertyExceptCandidates.Add(new Property(propertySymbol, null, sourceAssembly));
						}

						break;
					}

				case IEventSymbol eventSymbol:
					{
						IMethodSymbol? invoke = (eventSymbol.Type as INamedTypeSymbol)?.DelegateInvokeMethod;
						if (invoke is null)
						{
							break;
						}

						if (!eventSymbol.IsSealed && (IsInterface || eventSymbol.IsVirtual || eventSymbol.IsAbstract) &&
						    ShouldIncludeMember(eventSymbol))
						{
							eventIncludes.Add(new Event(eventSymbol, invoke, alreadyDefinedEvents, sourceAssembly));
						}

						if (eventSymbol.IsSealed || HidesBaseOverridable(eventSymbol, type))
						{
							eventExceptCandidates.Add(new Event(eventSymbol, invoke, null, sourceAssembly));
						}

						break;
					}
			}
		}

		List<Method> methods = ToListExcept(DistinctList(methodIncludes), exceptMethods, Method.ContainingTypeIndependentEqualityComparer);
		Methods = new EquatableArray<Method>(methods.ToArray());

		List<Property> properties = ToListExcept(DistinctList(propertyIncludes), exceptProperties, Property.ContainingTypeIndependentEqualityComparer);
		Properties = new EquatableArray<Property>(properties.ToArray());

		List<Event> events = ToListExcept(DistinctList(eventIncludes), exceptEvents, Event.ContainingTypeIndependentEqualityComparer);
		Events = new EquatableArray<Event>(events.ToArray());

		exceptProperties ??= new List<Property>();
		exceptProperties.AddRange(DistinctList(propertyExceptCandidates));

		exceptMethods ??= new List<Method>();
		exceptMethods.AddRange(DistinctList(methodExceptCandidates));

		exceptEvents ??= new List<Event>();
		exceptEvents.AddRange(DistinctList(eventExceptCandidates));

		filledProperties ??= new List<Property>();
		filledProperties.AddRange(DistinctList(propertyFilledCandidates));

		InheritedTypes = new EquatableArray<Class>(
			GetInheritedTypes(type).Select(t
					=> new Class(t, sourceAssembly, methods, properties, events, exceptMethods, exceptProperties,
						exceptEvents, filledProperties))
				.ToArray());

		ReservedNames = ComputeReservedNames(type);

		HasInaccessibleRequiredMember = ComputeHasInaccessibleRequiredMember(filledProperties) ||
		                                InheritedTypes.Any(inherited => inherited.HasInaccessibleRequiredMember);

		_surfaceHash = ComputeSurfaceHash();

		bool ShouldIncludeMember(ISymbol member)
		{
			if (FillsInaccessibleBaseSlot(member, _sourceAssembly))
			{
				return false;
			}

			if (IsInterface || member.IsAbstract)
			{
				// An abstract member is kept even when it cannot be restated;
				// `ComputeHasInaccessibleRequiredMember` then rejects the whole type.
				return true;
			}

			return Helpers.IsOverridableFrom(member, _sourceAssembly) &&
			       Helpers.HasAccessibleSignature(member, _sourceAssembly);
		}
	}

	private Class(Class original, EquatableArray<Method> methods, EquatableArray<Property> properties,
		EquatableArray<Event> events, EquatableArray<Class> inheritedTypes)
	{
		_sourceAssembly = original._sourceAssembly;
		ClassFullName = original.ClassFullName;
		ClassName = original.ClassName;
		DisplayString = original.DisplayString;
		IsInterface = original.IsInterface;
		HasRequiredMembers = original.HasRequiredMembers;
		HasInaccessibleRequiredMember = original.HasInaccessibleRequiredMember;
		ReservedNames = original.ReservedNames;
		Methods = methods;
		Properties = properties;
		Events = events;
		InheritedTypes = inheritedTypes;
		_surfaceHash = ComputeSurfaceHash();
	}

	/// <summary>
	///     Returns a copy of this interface whose members carry the containing type of the
	///     <paramref name="implementor" /> member that already implements them, so both surfaces address
	///     one mock member instead of two. The replaced members are recorded in
	///     <paramref name="aliases" />.
	/// </summary>
	/// <remarks>
	///     A combination mock re-implements the additional interface, which re-maps every interface slot
	///     to the mock's own explicit implementation. That is what makes a member the class implements
	///     non-virtually (unreachable through the class) configurable through the interface - but applied
	///     to a member the mock already overrides it forks one member into two, with separate setups and
	///     separate recorded interactions.
	///     <para />
	///     Which class member implements which interface member is not guessed from signatures: it comes
	///     from <see cref="MockClass.FindImplementation(Method)" />, i.e. from the compiler. Rebasing then
	///     happens only when that implementation is also part of the mock's own surface
	///     (<see cref="AllMethods" /> and friends, which carry only members the mock can override). An
	///     implementation the mock cannot override - non-virtual, sealed, explicit, or with an
	///     inaccessible signature - is therefore left alone, and the interface member keeps its own
	///     containing type and stays re-implemented, which is the only way to reach that slot.
	///     <para />
	///     Only <see cref="Method.ContainingType" /> is rewritten; <see cref="Method.DeclaredContainingType" />
	///     keeps naming the interface, so generated documentation still refers to the interface member.
	///     <para />
	///     Must be called on an interface; the copy carries only the surface <see cref="Class" /> itself
	///     declares, so calling it on a <see cref="MockClass" /> would drop the mock-only fields.
	/// </remarks>
	public Class RebaseOnto(MockClass implementor, MemberAliases aliases)
		=> new(this,
			new EquatableArray<Method>(Methods.AsArray().Select(method => Rebase(method, implementor, aliases))
				.ToArray()),
			new EquatableArray<Property>(Properties.AsArray()
				.Select(property => Rebase(property, implementor, aliases)).ToArray()),
			new EquatableArray<Event>(Events.AsArray().Select(@event => Rebase(@event, implementor, aliases))
				.ToArray()),
			new EquatableArray<Class>(InheritedTypes.AsArray()
				.Select(inherited => inherited.RebaseOnto(implementor, aliases)).ToArray()));

	private static Method Rebase(Method method, MockClass implementor, MemberAliases aliases)
	{
		if (method.ExplicitImplementation is not null || method.IsStatic ||
		    implementor.FindImplementation(method) is not { } implementation)
		{
			return method;
		}

		Method? target = implementor.AllMethods()
			.FirstOrDefault(candidate => Method.EqualityComparer.Equals(candidate, implementation));
		if (target is null || target.ContainingType == method.ContainingType)
		{
			return method;
		}

		Method rebased = method with { ContainingType = target.ContainingType, };
		aliases.Add(rebased, target);
		return rebased;
	}

	private static Property Rebase(Property property, MockClass implementor, MemberAliases aliases)
	{
		if (property.ExplicitImplementation is not null || property.IsStatic ||
		    implementor.FindImplementation(property) is not { } implementation)
		{
			return property;
		}

		Property? target = implementor.AllProperties()
			.FirstOrDefault(candidate => Property.EqualityComparer.Equals(candidate, implementation));
		if (target is null || target.ContainingType == property.ContainingType)
		{
			return property;
		}

		Property rebased = property with { ContainingType = target.ContainingType, };
		aliases.Add(rebased, target);
		return rebased;
	}

	private static Event Rebase(Event @event, MockClass implementor, MemberAliases aliases)
	{
		if (@event.ExplicitImplementation is not null || @event.IsStatic ||
		    implementor.FindImplementation(@event) is not { } implementation)
		{
			return @event;
		}

		Event? target = implementor.AllEvents()
			.FirstOrDefault(candidate => Event.EqualityComparer.Equals(candidate, implementation));
		if (target is null || target.ContainingType == @event.ContainingType)
		{
			return @event;
		}

		Event rebased = @event with { ContainingType = target.ContainingType, };
		aliases.Add(rebased, target);
		return rebased;
	}

	public EquatableArray<Method> Methods { get; }
	public EquatableArray<Class> InheritedTypes { get; }
	public EquatableArray<Property> Properties { get; }
	public EquatableArray<Event> Events { get; }
	public EquatableArray<string> ReservedNames { get; }

	public bool IsInterface { get; }
	public bool HasRequiredMembers { get; }
	public bool HasInaccessibleRequiredMember { get; }
	public string ClassFullName { get; }
	public string ClassName { get; }
	public string DisplayString { get; }

	/// <summary>
	///     Equality is keyed on <see cref="ClassFullName" /> plus a content-derived hash of the
	///     member surface (<see cref="ComputeSurfaceHash" />). The full name alone is necessary
	///     but not sufficient as a Roslyn incremental cache key: across edits, a target type can
	///     keep its name while its members change, and a name-only comparison would let Roslyn
	///     skip downstream stages and persist stale generated source. Folding the surface into a
	///     precomputed hash keeps the comparison O(1) on the cache hot path while still
	///     invalidating on any change to the emitted member set. Hash collisions are theoretically
	///     possible but the leaf entities (<see cref="Method" />, <see cref="Property" />, and
	///     <see cref="Event" />) are records with content-based hashes that propagate through
	///     <see cref="EquatableArray{T}" />, so different surfaces almost always hash apart.
	/// </summary>
	public virtual bool Equals(Class? other)
		=> ReferenceEquals(this, other) ||
		   (other is not null &&
		    GetType() == other.GetType() &&
		    _surfaceHash == other._surfaceHash &&
		    ClassFullName == other.ClassFullName);

	/// <summary>
	///     Folds the member surface (methods, properties, events, recursive base/interface chain,
	///     reserved names, kind, required-member flag) into a single content-derived integer.
	///     Roslyn's incremental cache uses <see cref="Equals(Class?)" /> to decide whether a
	///     downstream stage's input changed; if equality only checked the type's name, an edit
	///     that altered the member surface but not the name would let stale generated source
	///     persist. Folding members into a hash keeps the comparison O(1) on the cache hot path
	///     while still invalidating on any surface change.
	/// </summary>
	private int ComputeSurfaceHash()
	{
		int hash = ClassFullName.GetHashCode();
		hash = unchecked((hash * 17) + Methods.GetHashCode());
		hash = unchecked((hash * 17) + Properties.GetHashCode());
		hash = unchecked((hash * 17) + Events.GetHashCode());
		hash = unchecked((hash * 17) + InheritedTypes.GetHashCode());
		hash = unchecked((hash * 17) + ReservedNames.GetHashCode());
		hash = unchecked((hash * 17) + (IsInterface ? 1 : 0));
		hash = unchecked((hash * 17) + (HasRequiredMembers ? 1 : 0));
		hash = unchecked((hash * 17) + (HasInaccessibleRequiredMember ? 1 : 0));
		return hash;
	}

	/// <summary>
	///     True when a member the mock is still obliged to implement is invisible to the mock's
	///     assembly - either the member itself, or a type named in its signature - leaving no valid code
	///     the generator could emit for it.
	/// </summary>
	/// <remarks>
	///     Deliberately reads the filtered <see cref="Methods" />/<see cref="Properties" />/
	///     <see cref="Events" /> rather than <c>type.GetMembers()</c>: an abstract member that a
	///     more-derived type already overrides has been dropped from those sets by
	///     <see cref="FillsInaccessibleBaseSlot" />, and it is no longer the mock's obligation. Judging
	///     the raw symbols instead would reject types that mock perfectly well.
	///     <para />
	///     Mirrored by <c>MockabilityAnalyzer.FindInaccessibleRequiredMember</c>, which reports
	///     Mockolate0002 for the same condition so the user gets a diagnostic instead of a silently
	///     missing mock. Keep both in sync.
	/// </remarks>
	private bool ComputeHasInaccessibleRequiredMember(List<Property> filledProperties)
		=> Methods.Any(method => method is { IsAbstract: true, IsOverridableFromMock: false, }) ||
		   Properties.Any(property => property is { IsAbstract: true, IsOverridableFromMock: false, } &&
		                              !filledProperties.Contains(property,
			                              Property.ContainingTypeIndependentEqualityComparer)) ||
		   Events.Any(@event => @event is { IsAbstract: true, IsOverridableFromMock: false, });

	/// <summary>
	///     True when <paramref name="member" /> fills a base slot (by <see langword="override" /> or by
	///     explicit interface implementation) that the mock must leave alone entirely, because the base
	///     declaration, one of its accessors, or a type in its signature is invisible to
	///     <paramref name="sourceAssembly" />.
	/// </summary>
	private static bool FillsInaccessibleBaseSlot(ISymbol member, IAssemblySymbol? sourceAssembly)
	{
		if (FindPartlyReachableFilledSlot(member, sourceAssembly) is not null)
		{
			return false;
		}

		return EnumerateFilledSlots(member).Any(slot => !IsSlotReachable(slot, sourceAssembly));
	}

	/// <summary>
	///     A filled slot the mock can still restate in part, as in
	///     <c>public abstract string P { get; internal set; }</c>: it keeps the reachable accessor and
	///     drops the other, which the filling member already implements.
	/// </summary>
	/// <remarks>
	///     Only an <see langword="override" /> qualifies; an explicit interface implementation must supply
	///     every accessor (CS0551) and cannot reach an internal one (CS0122).
	/// </remarks>
	private static IPropertySymbol? FindPartlyReachableFilledSlot(ISymbol member,
		IAssemblySymbol? sourceAssembly)
		=> member is IPropertySymbol { IsAbstract: false, OverriddenProperty: { } slot, } &&
		   Helpers.IsOverridableFrom(slot, sourceAssembly) &&
		   Helpers.HasAccessibleSignature(slot, sourceAssembly) &&
		   HasUnreachableAccessor(slot, sourceAssembly)
			? slot
			: null;

	private static IEnumerable<ISymbol> EnumerateFilledSlots(ISymbol member)
	{
		if (member.IsAbstract)
		{
			yield break;
		}

		switch (member)
		{
			case IMethodSymbol method:
				if (method.OverriddenMethod is { } overriddenMethod)
				{
					yield return overriddenMethod;
				}

				foreach (IMethodSymbol implemented in method.ExplicitInterfaceImplementations)
				{
					yield return implemented;
				}

				break;
			case IPropertySymbol property:
				if (property.OverriddenProperty is { } overriddenProperty)
				{
					yield return overriddenProperty;
				}

				foreach (IPropertySymbol implemented in property.ExplicitInterfaceImplementations)
				{
					yield return implemented;
				}

				break;
			case IEventSymbol @event:
				if (@event.OverriddenEvent is { } overriddenEvent)
				{
					yield return overriddenEvent;
				}

				foreach (IEventSymbol implemented in @event.ExplicitInterfaceImplementations)
				{
					yield return implemented;
				}

				break;
		}
	}

	private static bool IsSlotReachable(ISymbol slot, IAssemblySymbol? sourceAssembly)
		=> Helpers.IsOverridableFrom(slot, sourceAssembly) &&
		   Helpers.HasAccessibleSignature(slot, sourceAssembly) &&
		   (slot is not IPropertySymbol property || !HasUnreachableAccessor(property, sourceAssembly));

	private static bool HasUnreachableAccessor(IPropertySymbol property, IAssemblySymbol? sourceAssembly)
		=> IsUnreachableAccessor(property.GetMethod, sourceAssembly) ||
		   IsUnreachableAccessor(property.SetMethod, sourceAssembly);

	private static bool IsUnreachableAccessor(IMethodSymbol? accessor, IAssemblySymbol? sourceAssembly)
		=> accessor is not null && !Helpers.IsOverridableFrom(accessor, sourceAssembly);

	/// <summary>
	///     Identifiers that the mock class shares its scope with but that aren't surfaced through
	///     Methods/Properties/Events: generic type parameters of the type itself, nested types, and
	///     fields declared on the type. A generated member colliding with any of these would either
	///     fail to compile (CS0102 / type-parameter shadowing) or hide an inherited field (CS0108).
	/// </summary>
	private static EquatableArray<string> ComputeReservedNames(ITypeSymbol type)
	{
		HashSet<string> names = new();
		if (type is INamedTypeSymbol namedType)
		{
			foreach (ITypeParameterSymbol typeParameter in namedType.TypeParameters)
			{
				names.Add(typeParameter.Name);
			}
		}

		foreach (INamedTypeSymbol nested in type.GetTypeMembers())
		{
			names.Add(nested.Name);
		}

		foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>())
		{
			if (field.IsImplicitlyDeclared)
			{
				continue;
			}

			names.Add(field.Name);
		}

		return new EquatableArray<string>(names.ToArray());
	}

	private string GetTypeName(ITypeSymbol type)
	{
		if (type is INamedTypeSymbol namedType)
		{
			if (namedType.IsGenericType)
			{
				return namedType.Name + "<" + string.Join(",",
					namedType.TypeArguments.Select(GetTypeName)) + ">";
			}

			if (TryExtractSpecialName(namedType, out string? name))
			{
				return name!;
			}
		}

		return type.Name;
	}

	private string GetTypeFullName(ITypeSymbol type)
	{
		string GetPrefix(ITypeSymbol s)
		{
			string p = "";
			INamedTypeSymbol? containingType = s.ContainingType;
			while (containingType is not null)
			{
				p = $"{containingType.Name}.{p}";
				containingType = containingType.ContainingType;
			}

			return $"{s.ContainingNamespace}.{p}";
		}

		if (type is INamedTypeSymbol namedType)
		{
			if (namedType.IsGenericType)
			{
				return GetPrefix(namedType) + namedType.Name + "<" + string.Join(",",
					namedType.TypeArguments.Select(t
						=> t.TypeKind == TypeKind.TypeParameter ? t.Name : GetTypeFullName(t))) + ">";
			}

			if (TryExtractSpecialName(namedType, out string? name))
			{
				return name!;
			}
		}

		return GetPrefix(type) + type.Name;
	}

	private static bool TryExtractSpecialName(INamedTypeSymbol namedType, out string? specialName)
	{
		(specialName, bool hasSpecialType) = namedType.SpecialType switch
		{
			SpecialType.System_Object => ("object", true),
			SpecialType.System_Boolean => ("bool", true),
			SpecialType.System_String => ("string", true),
			SpecialType.System_Char => ("char", true),
			SpecialType.System_Byte => ("byte", true),
			SpecialType.System_SByte => ("sbyte", true),
			SpecialType.System_Int16 => ("short", true),
			SpecialType.System_UInt16 => ("ushort", true),
			SpecialType.System_Int32 => ("int", true),
			SpecialType.System_UInt32 => ("uint", true),
			SpecialType.System_Int64 => ("long", true),
			SpecialType.System_UInt64 => ("ulong", true),
			SpecialType.System_Single => ("float", true),
			SpecialType.System_Double => ("double", true),
			SpecialType.System_Decimal => ("decimal", true),
			_ => (null, false),
		};
		return hasSpecialType;
	}

	private static List<T> ToListExcept<T>(IEnumerable<T> source, IEnumerable<T>? except, IEqualityComparer<T> comparer)
	{
		if (except is null)
		{
			return source.ToList();
		}

		return source.Except(except, comparer).ToList();
	}

	/// <summary>
	///     In-place deduplication of the <paramref name="list" /> that preserves insertion order and uses default equality.
	/// </summary>
	private static List<T> DistinctList<T>(List<T> list) where T : notnull
	{
		if (list.Count <= 1)
		{
			return list;
		}

		HashSet<T> seen = new();
		List<T> result = new(list.Count);
		foreach (T item in list)
		{
			if (seen.Add(item))
			{
				result.Add(item);
			}
		}

		return result;
	}

	/// <summary>
	///     True when `member` (declared on `thisType`) hides an overridable member of the same
	///     signature on a base class. The hidden base cannot be overridden from a class deriving from
	///     `thisType` — the compiler resolves the override target to the hiding member first and fails
	///     with CS0506.<br />
	///     Overrides are not hiding: they continue the virtual slot.
	/// </summary>
	private static bool HidesBaseOverridable(ISymbol member, ITypeSymbol thisType)
	{
		if (member is IMethodSymbol { IsOverride: true, } or IPropertySymbol { IsOverride: true, } or IEventSymbol { IsOverride: true, })
		{
			return false;
		}

		for (INamedTypeSymbol? b = thisType.BaseType; b is not null; b = b.BaseType)
		{
			foreach (ISymbol candidate in b.GetMembers(member.Name))
			{
				if (candidate.Kind != member.Kind || candidate.IsStatic != member.IsStatic)
				{
					continue;
				}

				if (!(candidate.IsVirtual || candidate.IsAbstract || candidate.IsOverride) || candidate.IsSealed)
				{
					continue;
				}

				if (SignatureMatches(member, candidate))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool SignatureMatches(ISymbol a, ISymbol b)
		=> (a, b) switch
		{
			(IMethodSymbol ma, IMethodSymbol mb) => ParametersMatch(ma.Parameters, mb.Parameters) &&
			                                        ma.TypeParameters.Length == mb.TypeParameters.Length,
			(IPropertySymbol pa, IPropertySymbol pb) => ParametersMatch(pa.Parameters, pb.Parameters),
			(IEventSymbol, IEventSymbol) => true,
			_ => false,
		};

	private static bool ParametersMatch(ImmutableArray<IParameterSymbol> a, ImmutableArray<IParameterSymbol> b)
	{
		if (a.Length != b.Length)
		{
			return false;
		}

		for (int i = 0; i < a.Length; i++)
		{
			if (a[i].RefKind != b[i].RefKind ||
			    !SymbolEqualityComparer.Default.Equals(a[i].Type, b[i].Type))
			{
				return false;
			}
		}

		return true;
	}

	private static bool ComputeHasRequiredMembers(ITypeSymbol type)
	{
		for (ITypeSymbol? current = type;
		     current is not null && current.SpecialType != SpecialType.System_Object;
		     current = current.BaseType)
		{
			foreach (ISymbol member in current.GetMembers())
			{
				if (member is IPropertySymbol { IsRequired: true, } or IFieldSymbol { IsRequired: true, })
				{
					return true;
				}
			}
		}

		return false;
	}

	public static IEnumerable<ITypeSymbol> GetInheritedTypes(ITypeSymbol type)
	{
		ITypeSymbol? current = type;
		while (current != null && current.SpecialType != SpecialType.System_Object)
		{
			if (!SymbolEqualityComparer.Default.Equals(current, type))
			{
				yield return current;
			}

			if (current.TypeKind == TypeKind.Interface)
			{
				foreach (INamedTypeSymbol? @interface in current.AllInterfaces)
				{
					yield return @interface;
				}
			}

			current = current.BaseType;
		}
	}

	public IEnumerable<Property> AllProperties()
	{
		_allProperties ??= AllClasses().SelectMany(c => c.Properties).Distinct(Property.EqualityComparer).ToList();
		return _allProperties;
	}

	public IEnumerable<Method> AllMethods()
	{
		_allMethods ??= AllClasses().SelectMany(c => c.Methods).Distinct(Method.EqualityComparer).ToList();
		return _allMethods;
	}

	public IEnumerable<Event> AllEvents()
	{
		_allEvents ??= AllClasses().SelectMany(c => c.Events).Distinct(Event.EqualityComparer).ToList();
		return _allEvents;
	}

	private IEnumerable<Class> AllClasses()
	{
		yield return this;
		foreach (Class inherited in InheritedTypes)
		{
			yield return inherited;
		}
	}

	public string GetClassNameWithoutDots()
	{
		if (_classNameWithoutDots is not null)
		{
			return _classNameWithoutDots;
		}

		StringBuilder sb = new(ClassName.Length);
		foreach (char c in ClassName)
		{
			switch (c)
			{
				case ',':
				case '.':
				case '<':
					sb.Append('_');
					break;
				case '>':
				case ' ':
					break;
				default:
					sb.Append(c);
					break;
			}
		}

		return _classNameWithoutDots = sb.ToString();
	}

	public override bool Equals(object? obj) => Equals(obj as Class);

	public override int GetHashCode() => _surfaceHash;

	public static bool operator ==(Class? left, Class? right)
	{
		return left?.Equals(right) ?? right is null;
	}

	public static bool operator !=(Class? left, Class? right)
	{
		return !(left == right);
	}
}
