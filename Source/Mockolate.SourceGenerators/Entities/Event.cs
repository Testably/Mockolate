using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Entities;

[DebuggerDisplay("{ContainingType}.{Name}")]
internal record Event
{
	public Event(IEventSymbol eventSymbol, IMethodSymbol delegateInvokeMethod, List<Event>? alreadyDefinedEvents, IAssemblySymbol? sourceAssembly = null)
	{
		Accessibility = eventSymbol.DeclaredAccessibility;
		OverrideAccessibility = Helpers.ResolveOverrideVisibility(
			Accessibility, eventSymbol.ContainingAssembly, sourceAssembly);
		UseOverride = eventSymbol.IsVirtual || eventSymbol.IsAbstract;
		IsOverridableFromMock = Helpers.IsOverridableFrom(eventSymbol, sourceAssembly) &&
		                        Helpers.HasAccessibleSignature(eventSymbol, sourceAssembly);
		IsAbstract = eventSymbol.IsAbstract;
		Name = Helpers.EscapeIfKeyword(eventSymbol.ExplicitInterfaceImplementations.Length > 0 ? eventSymbol.ExplicitInterfaceImplementations[0].Name : eventSymbol.Name);
		Type = Type.From(eventSymbol.Type);
		ContainingType = eventSymbol.ContainingType.ToQualifiedDisplayString(Helpers.TypeDisplayFormat);
		DeclaredContainingType = ContainingType;
		Delegate = new Method(delegateInvokeMethod, null, sourceAssembly);
		Attributes = eventSymbol.GetAttributes().ToAttributeArray(sourceAssembly);
		IsStatic = eventSymbol.IsStatic;

		if (alreadyDefinedEvents is not null)
		{
			if (alreadyDefinedEvents.Any(p => p.Name == Name))
			{
				ExplicitImplementation = ContainingType;
			}

			alreadyDefinedEvents.Add(this);
		}
	}

	public EquatableArray<Attribute>? Attributes { get; }

	public static IEqualityComparer<Event> EqualityComparer { get; } = new EventEqualityComparer();

	public static IEqualityComparer<Event> ContainingTypeIndependentEqualityComparer { get; } =
		new ContainingTypeIndependentEventEqualityComparer();

	public Method Delegate { get; }

	public Type Type { get; }
	public string ContainingType { get; init; }

	/// <inheritdoc cref="Method.DeclaredContainingType" />
	public string DeclaredContainingType { get; }

	public bool UseOverride { get; }
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

	public Accessibility Accessibility { get; }
	public string OverrideAccessibility { get; }
	public string Name { get; }
	public string? ExplicitImplementation { get; }

	internal string GetUniqueNameString()
		=> $"\"{ContainingType}.{Name}\"";

	internal string GetBackingFieldName()
	{
		char[] sanitized = ContainingType.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
		string nameForField = Name.StartsWith("@") ? Name.Substring(1) : Name;
		return $"_mockolateEvent_{new string(sanitized)}_{nameForField}";
	}

	private sealed class EventEqualityComparer : IEqualityComparer<Event>
	{
		public bool Equals(Event? x, Event? y)
			=> (x is null && y is null) ||
			   (x is not null && y is not null &&
			    x.Name.Equals(y.Name) &&
			    x.ContainingType.Equals(y.ContainingType));

		public int GetHashCode(Event obj) => obj.Name.GetHashCode();
	}

	private sealed class ContainingTypeIndependentEventEqualityComparer : IEqualityComparer<Event>
	{
		public bool Equals(Event? x, Event? y)
			=> (x is null && y is null) ||
			   (x is not null && y is not null &&
			    x.Name.Equals(y.Name));

		public int GetHashCode(Event obj) => obj.Name.GetHashCode();
	}
}
