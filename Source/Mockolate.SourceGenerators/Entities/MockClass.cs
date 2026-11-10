using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Entities;

internal sealed class MockClass : Class, IEquatable<MockClass>
{
	private readonly int _mockSurfaceHash;
	private readonly Dictionary<Event, Event> _eventImplementations;
	private readonly Dictionary<Method, Method> _methodImplementations;
	private readonly Dictionary<Property, Property> _propertyImplementations;

	public MockClass(ITypeSymbol[] types, IAssemblySymbol sourceAssembly) : base(types[0], sourceAssembly)
	{
		AdditionalImplementations = new EquatableArray<Class>(
			types.Skip(1).Select(x => new Class(x, sourceAssembly)).ToArray());

		(EquatableArray<ImplementedMember<Method>> implementedMethods,
			EquatableArray<ImplementedMember<Property>> implementedProperties,
			EquatableArray<ImplementedMember<Event>> implementedEvents) =
			ResolveImplementedMembers(types, sourceAssembly);
		ImplementedMethods = implementedMethods;
		ImplementedProperties = implementedProperties;
		ImplementedEvents = implementedEvents;
		_methodImplementations = BuildLookup(ImplementedMethods, Method.EqualityComparer);
		_propertyImplementations = BuildLookup(ImplementedProperties, Property.EqualityComparer);
		_eventImplementations = BuildLookup(ImplementedEvents, Event.EqualityComparer);

		HiddenBaseInterfaces = IsInterface
			? new EquatableArray<Class>(GetHiddenBaseInterfaces(types[0])
				.Select(x => new Class(x, sourceAssembly)).ToArray())
			: new EquatableArray<Class>([]);

		if (!IsInterface && types[0] is INamedTypeSymbol namedTypeSymbol)
		{
			Constructors =
				new EquatableArray<Method>(namedTypeSymbol.Constructors
					.Where(x => x.DeclaredAccessibility == Accessibility.Protected ||
					            x.DeclaredAccessibility == Accessibility.ProtectedOrInternal ||
					            x.DeclaredAccessibility == Accessibility.Public)
					// Parameter types are named verbatim in `MockExtensionsForXXX`, which does not derive
					// from the mocked type: a type reachable only through inheritance would cause CS0122
					// there and CS0051 on the generated `public` constructor.
					.Where(x => x.Parameters.All(p => Helpers.IsAccessibleFrom(p.Type, sourceAssembly)))
					.Select(x => new Method(x, null, sourceAssembly)).ToArray());
			if (namedTypeSymbol.DelegateInvokeMethod is not null)
			{
				Delegate = new Method(namedTypeSymbol.DelegateInvokeMethod, null, sourceAssembly);
			}
		}

		_mockSurfaceHash = ComputeMockSurfaceHash();
	}

	public Method? Delegate { get; }

	public EquatableArray<Method>? Constructors { get; }

	public EquatableArray<Class> AdditionalImplementations { get; }

	/// <summary>
	///     Members of the <see cref="AdditionalImplementations" /> (and of their base interfaces) that the
	///     mocked class already implements, each paired with the implementing class member. Such a member
	///     is not really additional, so its interface surface is rebased onto the class member that
	///     implements it (see <see cref="Class.RebaseOnto" />).
	/// </summary>
	public EquatableArray<ImplementedMember<Method>> ImplementedMethods { get; }

	/// <inheritdoc cref="ImplementedMethods" />
	public EquatableArray<ImplementedMember<Property>> ImplementedProperties { get; }

	/// <inheritdoc cref="ImplementedMethods" />
	public EquatableArray<ImplementedMember<Event>> ImplementedEvents { get; }

	/// <summary>
	///     True when the mocked class implements at least one member of an additional interface, i.e. when
	///     <see cref="Class.RebaseOnto" /> has anything to do.
	/// </summary>
	public bool HasImplementedMembers
		=> ImplementedMethods.Count > 0 || ImplementedProperties.Count > 0 || ImplementedEvents.Count > 0;

	/// <summary>
	///     Base interfaces whose members are hidden (via <see langword="new" />) by the mocked
	///     interface. Their setup/verify surfaces are generated and implemented so the hidden slots are
	///     reachable through <c>.Mock.As&lt;TBase&gt;()</c>. Distinct from
	///     <see cref="AdditionalImplementations" /> (the user's explicit <c>Implementing&lt;T&gt;()</c>).
	/// </summary>
	public EquatableArray<Class> HiddenBaseInterfaces { get; }

	/// <summary>
	///     MockClass equality is keyed on <see cref="Class.ClassFullName" /> plus a content-derived
	///     hash that folds the base surface together with the mock-only fields
	///     (<see cref="AdditionalImplementations" />, <see cref="ImplementedMethods" /> and its property and
	///     event counterparts, <see cref="HiddenBaseInterfaces" />, <see cref="Constructors" />,
	///     <see cref="Delegate" />). Two mocks of the same root with different additional interfaces,
	///     different constructor surfaces, or different delegate signatures must hash apart so Roslyn's
	///     incremental cache invalidates when any of those change.
	///     <para />
	///     The implemented-member pairings carry their own weight here: adding <c>: IFoo</c> to a class
	///     whose existing virtual members already satisfy <c>IFoo</c> leaves the base surface hash
	///     untouched, and without the pairings in the hash the combination mock's generated source would
	///     go stale.
	/// </summary>
	public bool Equals(MockClass? other)
		=> ReferenceEquals(this, other) ||
		   (other is not null &&
		    _mockSurfaceHash == other._mockSurfaceHash &&
		    ClassFullName == other.ClassFullName);

	public override bool Equals(Class? other) => other is MockClass mc && Equals(mc);

	public override bool Equals(object? obj) => Equals(obj as MockClass);

	public override int GetHashCode() => _mockSurfaceHash;

	/// <summary>
	///     The member of the mocked class that implements <paramref name="interfaceMember" />, or
	///     <see langword="null" /> when the mocked class does not implement it.
	/// </summary>
	/// <remarks>
	///     The returned member is the implementation as declared. It is not necessarily part of the mock's
	///     surface - a non-virtual or otherwise unreachable implementation is reported here too, so callers
	///     must still look it up in the surface they intend to address.
	/// </remarks>
	internal Method? FindImplementation(Method interfaceMember)
		=> _methodImplementations.TryGetValue(interfaceMember, out Method? implementation)
			? implementation
			: null;

	/// <inheritdoc cref="FindImplementation(Method)" />
	internal Property? FindImplementation(Property interfaceMember)
		=> _propertyImplementations.TryGetValue(interfaceMember, out Property? implementation)
			? implementation
			: null;

	/// <inheritdoc cref="FindImplementation(Method)" />
	internal Event? FindImplementation(Event interfaceMember)
		=> _eventImplementations.TryGetValue(interfaceMember, out Event? implementation)
			? implementation
			: null;

	/// <remarks>
	///     Keyed by the member's identity comparer (name, containing type, parameters) rather than by
	///     record equality: the interface member reached through <see cref="Class.InheritedTypes" /> can
	///     carry flags the freshly built key does not, and those flags do not change which member it is.
	/// </remarks>
	private static Dictionary<TMember, TMember> BuildLookup<TMember>(
		EquatableArray<ImplementedMember<TMember>> members, IEqualityComparer<TMember> comparer)
		where TMember : notnull
	{
		Dictionary<TMember, TMember> lookup = new(comparer);
		foreach (ImplementedMember<TMember> member in members)
		{
			lookup[member.InterfaceMember] = member.ClassMember;
		}

		return lookup;
	}

	/// <summary>
	///     Pairs every member of the additional interfaces - and of their base interfaces - with the member
	///     of the mocked class that implements it.
	/// </summary>
	/// <remarks>
	///     Asking Roslyn instead of matching signatures keeps the pairing exact: it is unaffected by
	///     nullability annotations, renamed generic type parameters and covariant returns, and it never
	///     pairs two members that merely look alike. Interface mocks and mocks without additional
	///     interfaces pair nothing - an interface's inherited member already carries its declaring
	///     interface as its containing type on both surfaces, so there is nothing to rebase.
	/// </remarks>
	private static (EquatableArray<ImplementedMember<Method>> Methods,
		EquatableArray<ImplementedMember<Property>> Properties,
		EquatableArray<ImplementedMember<Event>> Events) ResolveImplementedMembers(
			ITypeSymbol[] types, IAssemblySymbol sourceAssembly)
	{
		List<ImplementedMember<Method>> methods = new();
		List<ImplementedMember<Property>> properties = new();
		List<ImplementedMember<Event>> events = new();
		if (types.Length > 1 && types[0].TypeKind != TypeKind.Interface)
		{
			HashSet<INamedTypeSymbol> visited = new(SymbolEqualityComparer.Default);
			foreach (INamedTypeSymbol @interface in types.Skip(1).SelectMany(EnumerateInterfaces))
			{
				if (!visited.Add(@interface))
				{
					continue;
				}

				foreach (ISymbol member in @interface.GetMembers())
				{
					if (member.IsStatic ||
					    types[0].FindImplementationForInterfaceMember(member) is not { } implementation)
					{
						continue;
					}

					switch (member)
					{
						case IMethodSymbol { MethodKind: MethodKind.Ordinary, } interfaceMethod
							when implementation is IMethodSymbol classMethod:
							methods.Add(new ImplementedMember<Method>(
								new Method(interfaceMethod, null, sourceAssembly),
								new Method(classMethod, null, sourceAssembly)));
							break;
						case IPropertySymbol interfaceProperty
							when implementation is IPropertySymbol classProperty:
							properties.Add(new ImplementedMember<Property>(
								new Property(interfaceProperty, null, sourceAssembly),
								new Property(classProperty, null, sourceAssembly)));
							break;
						case IEventSymbol
							{
								Type: INamedTypeSymbol { DelegateInvokeMethod: { } interfaceInvoke, },
							} interfaceEvent
							when implementation is IEventSymbol
							{
								Type: INamedTypeSymbol { DelegateInvokeMethod: { } classInvoke, },
							} classEvent:
							events.Add(new ImplementedMember<Event>(
								new Event(interfaceEvent, interfaceInvoke, null, sourceAssembly),
								new Event(classEvent, classInvoke, null, sourceAssembly)));
							break;
					}
				}
			}
		}

		return (new EquatableArray<ImplementedMember<Method>>(methods.ToArray()),
			new EquatableArray<ImplementedMember<Property>>(properties.ToArray()),
			new EquatableArray<ImplementedMember<Event>>(events.ToArray()));
	}

	private static IEnumerable<INamedTypeSymbol> EnumerateInterfaces(ITypeSymbol type)
	{
		if (type is INamedTypeSymbol namedType)
		{
			yield return namedType;
		}

		foreach (INamedTypeSymbol baseInterface in type.AllInterfaces)
		{
			yield return baseInterface;
		}
	}

	public IEnumerable<Class> AllImplementations()
	{
		yield return this;
		foreach (Class additionalImplementation in AdditionalImplementations)
		{
			yield return additionalImplementation;
		}

		foreach (Class hiddenBaseInterface in HiddenBaseInterfaces)
		{
			yield return hiddenBaseInterface;
		}
	}

	private int ComputeMockSurfaceHash()
	{
		int hash = base.GetHashCode();
		hash = unchecked((hash * 17) + AdditionalImplementations.GetHashCode());
		hash = unchecked((hash * 17) + ImplementedMethods.GetHashCode());
		hash = unchecked((hash * 17) + ImplementedProperties.GetHashCode());
		hash = unchecked((hash * 17) + ImplementedEvents.GetHashCode());
		hash = unchecked((hash * 17) + HiddenBaseInterfaces.GetHashCode());
		if (Constructors is { } constructors)
		{
			hash = unchecked((hash * 17) + constructors.GetHashCode());
		}

		if (Delegate is { } @delegate)
		{
			hash = unchecked((hash * 17) + @delegate.GetHashCode());
		}

		return hash;
	}

	/// <summary>
	///     Base interfaces of <paramref name="type" /> that declare a member which a more-derived
	///     interface in the hierarchy hides (a <see langword="new" /> member with a matching signature).
	///     The hidden base member is a separate interface slot, so its setup/verify surface must be
	///     generated explicitly. Ordinary (non-hidden) inheritance returns nothing.
	/// </summary>
	private static IEnumerable<INamedTypeSymbol> GetHiddenBaseInterfaces(ITypeSymbol type)
	{
		ImmutableArray<INamedTypeSymbol> allInterfaces = type.AllInterfaces;
		foreach (INamedTypeSymbol baseInterface in allInterfaces)
		{
			if (baseInterface.GetMembers().Any(member => member.IsStatic))
			{
				continue;
			}

			bool hasHiddenMember = false;
			foreach (ISymbol baseMember in baseInterface.GetMembers())
			{
				if (!IsHidableMember(baseMember))
				{
					continue;
				}

				if (HidesMember(type, baseMember) ||
				    allInterfaces.Any(intermediate =>
					    !SymbolEqualityComparer.Default.Equals(intermediate, baseInterface) &&
					    intermediate.AllInterfaces.Contains(baseInterface, SymbolEqualityComparer.Default) &&
					    HidesMember(intermediate, baseMember)))
				{
					hasHiddenMember = true;
					break;
				}
			}

			if (hasHiddenMember)
			{
				yield return baseInterface;
			}
		}
	}

	private static bool HidesMember(ITypeSymbol hidingType, ISymbol baseMember)
		=> hidingType.GetMembers(baseMember.Name)
			.Any(candidate => !SymbolEqualityComparer.Default.Equals(candidate.ContainingType, baseMember.ContainingType) &&
			                  SignatureMatches(candidate, baseMember));

	private static bool IsHidableMember(ISymbol member)
		=> member switch
		{
			IMethodSymbol { MethodKind: MethodKind.Ordinary, } => true,
			IPropertySymbol => true,
			IEventSymbol => true,
			_ => false,
		};

	private static bool SignatureMatches(ISymbol a, ISymbol b)
		=> a.Kind == b.Kind && (a, b) switch
		{
			(IMethodSymbol ma, IMethodSymbol mb) => ma.TypeParameters.Length == mb.TypeParameters.Length &&
			                                        ParametersMatch(ma.Parameters, mb.Parameters),
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
}
