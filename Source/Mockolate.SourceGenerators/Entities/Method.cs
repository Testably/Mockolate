using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Entities;

[DebuggerDisplay("{ContainingType}.{Name}({Parameters})")]
internal record Method
{
	public Method(IMethodSymbol methodSymbol, List<Method>? alreadyDefinedMethods, IAssemblySymbol? sourceAssembly = null)
	{
		Accessibility = methodSymbol.DeclaredAccessibility;
		OverrideAccessibility = Helpers.ResolveOverrideVisibility(
			Accessibility, methodSymbol.ContainingAssembly, sourceAssembly);
		UseOverride = methodSymbol.IsVirtual || methodSymbol.IsAbstract;
		IsOverridableFromMock = Helpers.IsOverridableFrom(methodSymbol, sourceAssembly) &&
		                        Helpers.HasAccessibleSignature(methodSymbol, sourceAssembly);
		IsAbstract = methodSymbol.IsAbstract;
		IsStatic = methodSymbol.IsStatic;
		IsInitOnly = methodSymbol.IsInitOnly;
		IsRefReturn = methodSymbol.RefKind == RefKind.Ref;
		IsRefReadonlyReturn = methodSymbol.RefKind == RefKind.RefReadOnly;
		IsDelegateInvoke = methodSymbol.MethodKind == MethodKind.DelegateInvoke;
		ReturnType = methodSymbol.ReturnsVoid ? Type.Void : Type.From(methodSymbol.ReturnType);
		Name = Helpers.EscapeIfKeyword(methodSymbol.ExplicitInterfaceImplementations.Length > 0 ? methodSymbol.ExplicitInterfaceImplementations[0].Name : methodSymbol.Name);
		ContainingType = methodSymbol.ContainingType.ToQualifiedDisplayString(Helpers.TypeDisplayFormat);
		Parameters = new EquatableArray<MethodParameter>(
			methodSymbol.Parameters.Select(MethodParameter.From).ToArray());

		if (methodSymbol.IsGenericMethod)
		{
			GenericParameters = new EquatableArray<GenericParameter>(methodSymbol.TypeArguments
				.Select(x => new GenericParameter((ITypeParameterSymbol)x)).ToArray());
			Name += $"<{string.Join(", ", GenericParameters.Value.Select(x => x.Name))}>";
		}

		Cref = Helpers.ToMemberCref(methodSymbol.OriginalDefinition, Name);
		XmlDocReference = Helpers.ToXmlDocReference(Cref, Helpers.ToMemberCref(methodSymbol, Name));

		Attributes = methodSymbol.GetAttributes().ToAttributeArray(sourceAssembly);

		if (alreadyDefinedMethods is not null)
		{
			if (alreadyDefinedMethods.Any(m =>
				    string.Equals(m.Name, Name, StringComparison.Ordinal) &&
				    m.Parameters.Count == Parameters.Count &&
				    m.Parameters.Zip(Parameters, HaveSameIdentity).All(same => same)))
			{
				ExplicitImplementation = ContainingType;
			}

			alreadyDefinedMethods.Add(this);
		}
	}

	public static IEqualityComparer<Method> EqualityComparer { get; } = new MethodEqualityComparer();

	public static IEqualityComparer<Method> ContainingTypeIndependentEqualityComparer { get; } =
		new ContainingTypeIndependentMethodEqualityComparer();

	public EquatableArray<GenericParameter>? GenericParameters { get; }

	public bool UseOverride { get; }
	public bool IsOverridableFromMock { get; }
	public bool IsAbstract { get; init; }
	public bool IsStatic { get; }
	public bool IsInitOnly { get; }
	public bool IsRefReturn { get; }
	public bool IsRefReadonlyReturn { get; }
	public bool IsDelegateInvoke { get; }

	/// <summary>
	///     Delegates have no ref-struct setup pipeline: a delegate mock projects its single <c>Invoke</c>
	///     onto <c>VoidMethodSetup&lt;T&gt;</c> / <c>ReturnMethodSetup&lt;T&gt;</c>, neither of which carries
	///     the <c>allows ref struct</c> anti-constraint that the interface and class pipelines get from
	///     <c>RefStructVoidMethodSetup&lt;T&gt;</c>. Such delegates get a <c>NotSupportedException</c> stub
	///     and no setup/verify surface; <c>Span&lt;T&gt;</c>/<c>ReadOnlySpan&lt;T&gt;</c> parameters are
	///     exempt because they flow through their wrappers.
	/// </summary>
	/// <remarks>
	///     Deliberately the <c>MethodParameter</c> overload of <c>NeedsRefStructPipeline</c>, so
	///     <c>ref readonly</c> Span/ReadOnlySpan is caught too: that ref kind has no wrapper-based emit
	///     branch, so <c>AppendMethodSetupDefinition</c> would hand it the .NET 9-gated
	///     <c>IRefStruct*Setup</c> surface while the delegate's <c>Invoke</c> body dispatches through
	///     <c>VoidMethodSetup&lt;SpanWrapper&lt;T&gt;&gt;</c> - a setup the mock could never match.
	/// </remarks>
	public bool IsDelegateWithUnsupportedRefStructParameter
		=> IsDelegateInvoke && Parameters.Any(parameter => parameter.NeedsRefStructPipeline());

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
	public Type ReturnType { get; }
	public string Name { get; }
	public string ContainingType { get; init; }

	/// <summary>
	///     The XML documentation <c>cref</c> of the declared member. Unlike <see cref="ContainingType" /> it is not
	///     rewritten by <see cref="Class.RebaseOnto" />, so documentation keeps referring to the interface
	///     member even when the member is addressed through the implementing class member.
	/// </summary>
	public string Cref { get; }

	/// <summary>
	///     The <c>&lt;see&gt;</c> element that references the declared member in generated documentation.
	/// </summary>
	public string XmlDocReference { get; }

	public EquatableArray<MethodParameter> Parameters { get; }
	public string? ExplicitImplementation { get; }
	public EquatableArray<Attribute>? Attributes { get; }

	public bool HasInheritedOnlyConstraint
		=> GenericParameters?.Any(genericParameter => genericParameter.HasInheritedOnlyConstraint) == true;

	/// <summary>
	///     A method has an unsupported <c>allows ref struct</c> type parameter when one of its
	///     own generic parameters declares the anti-constraint <i>and</i> is referenced in the
	///     return type or any parameter type. The standard setup pipeline parameterizes
	///     <c>ReturnMethodSetup&lt;T&gt;</c> / <c>IReturnMethodSetup&lt;T&gt;</c> on <c>T</c>, but
	///     those runtime types do not carry <c>allows ref struct</c>, so the generated source
	///     would fail with CS9244. Methods that match this predicate get a NotSupportedException
	///     stub instead — see the carve-out for unsupported ref-struct shapes for the same shape.
	/// </summary>
	public bool HasUnsupportedAllowsRefStructTypeParameter
	{
		get
		{
			if (GenericParameters is null || GenericParameters.Value.Count == 0)
			{
				return false;
			}

			GenericParameter[] refStructParameters = GenericParameters.Value
				.Where(g => g.AllowsRefStruct).ToArray();
			if (refStructParameters.Length == 0)
			{
				return false;
			}

			if (ReturnType != Type.Void && ContainsAnyTypeParameter(ReturnType.Fullname, refStructParameters))
			{
				return true;
			}

			foreach (MethodParameter parameter in Parameters)
			{
				if (ContainsAnyTypeParameter(parameter.Type.Fullname, refStructParameters))
				{
					return true;
				}
			}

			return false;
		}
	}

	internal string GetUniqueNameString()
	{
		if (GenericParameters != null)
		{
			string name = Name.Substring(0, Name.IndexOf('<'));
			string parameters = string.Join(", ",
				GenericParameters.Value.Select(genericParameter => $"{{typeof({genericParameter.Name})}}"));
			return $"$\"{ContainingType}.{name}<{parameters}>\"";
		}

		return $"\"{ContainingType}.{Name}\"";
	}

	internal bool UsesTypeParameter(Type type)
		=> GenericParameters is not null && ContainsAnyTypeParameter(type.Fullname, GenericParameters.Value.AsArray());

	private static bool ContainsAnyTypeParameter(string text, GenericParameter[] genericParameters)
	{
		foreach (GenericParameter gp in genericParameters)
		{
			if (ContainsAsToken(text, gp.Name))
			{
				return true;
			}
		}

		return false;
	}

	private static bool ContainsAsToken(string text, string name)
	{
		int idx = 0;
		while ((idx = text.IndexOf(name, idx, StringComparison.Ordinal)) >= 0)
		{
			bool startBoundary = idx == 0 || !IsIdentifierChar(text[idx - 1]);
			bool endBoundary = idx + name.Length == text.Length || !IsIdentifierChar(text[idx + name.Length]);
			if (startBoundary && endBoundary)
			{
				return true;
			}

			idx++;
		}

		return false;
	}

	private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

	/// <summary>
	///     C# signatures ignore parameter names and nullable reference annotations, so two members that differ only there
	///     cannot both be implemented implicitly.
	/// </summary>
	private static bool HaveSameIdentity(MethodParameter x, MethodParameter y)
		=> x.RefKind == y.RefKind && string.Equals(x.Type.IdentityName, y.Type.IdentityName, StringComparison.Ordinal);

	public bool IsToString()
		=> string.Equals(Name, "ToString", StringComparison.Ordinal) &&Parameters.Count == 0 && CanOverrideObjectMember(SpecialType.System_String);

	public bool IsGetHashCode()
		=> string.Equals(Name, "GetHashCode", StringComparison.Ordinal) &&Parameters.Count == 0 && CanOverrideObjectMember(SpecialType.System_Int32);

	public bool IsEquals()
		=> string.Equals(Name, "Equals", StringComparison.Ordinal) &&Parameters.Count == 1 && IsObjectParameter(Parameters.Single()) &&
		   CanOverrideObjectMember(SpecialType.System_Boolean);

	/// <summary>
	///     Whether the method has the signature of a member the mock inherits from <see cref="object" />, but cannot
	///     override it, so an implicit implementation on an interface mock hides it (CS0108, CS0114).
	/// </summary>
	public bool HidesObjectMember()
		=> Parameters.All(IsObjectParameter) &&
		   (Name, Parameters.Count) is ("Equals", 1 or 2) or ("ReferenceEquals", 2)
			   or ("GetHashCode" or "GetType" or "ToString" or "MemberwiseClone", 0) &&
		   !IsEquals() && !IsGetHashCode() && !IsToString();

	private bool CanOverrideObjectMember(SpecialType returnType)
		=> !IsStatic && !IsRefReturn && !IsRefReadonlyReturn && ReturnType.SpecialType == returnType;

	private static bool IsObjectParameter(MethodParameter parameter)
		=> parameter.RefKind == RefKind.None && string.Equals(parameter.Type.Namespace, "System", StringComparison.Ordinal) &&
		   (string.Equals(parameter.Type.Fullname, "object", StringComparison.Ordinal) || string.Equals(parameter.Type.Fullname, "object?", StringComparison.Ordinal));

	private sealed class MethodEqualityComparer : IEqualityComparer<Method>
	{
		public bool Equals(Method? x, Method? y)
			=> (x is null && y is null) ||
			   (x is not null && y is not null &&
			    x.Name.Equals(y.Name, StringComparison.Ordinal) && x.ContainingType.Equals(y.ContainingType, StringComparison.Ordinal) &&
			    x.Parameters.Count == y.Parameters.Count &&
			    x.Parameters.SequenceEqual(y.Parameters));

		public int GetHashCode(Method obj) => StringComparer.Ordinal.GetHashCode(obj.Name);
	}

	private sealed class ContainingTypeIndependentMethodEqualityComparer : IEqualityComparer<Method>
	{
		public bool Equals(Method? x, Method? y)
		{
			if (x is null && y is null)
			{
				return true;
			}

			if (x is null || y is null)
			{
				return false;
			}

			if (!x.Name.Equals(y.Name, StringComparison.Ordinal) || x.Parameters.Count != y.Parameters.Count)
			{
				return false;
			}

			// Compare parameters ignoring nullability annotations
			MethodParameter[] xParams = x.Parameters.AsArray();
			MethodParameter[] yParams = y.Parameters.AsArray();

			for (int i = 0; i < xParams.Length; i++)
			{
				MethodParameter xParam = xParams[i];
				MethodParameter yParam = yParams[i];

				if (xParam.RefKind != yParam.RefKind)
				{
					return false;
				}

				// Normalize type names by removing nullable annotation
				string xTypeName = xParam.Type.Fullname;
				string yTypeName = yParam.Type.Fullname;

				if (xTypeName.EndsWith("?", StringComparison.Ordinal))
				{
					xTypeName = xTypeName.Substring(0, xTypeName.Length - 1);
				}

				if (yTypeName.EndsWith("?", StringComparison.Ordinal))
				{
					yTypeName = yTypeName.Substring(0, yTypeName.Length - 1);
				}

				if (!xTypeName.Equals(yTypeName, StringComparison.Ordinal))
				{
					return false;
				}
			}

			return true;
		}

		public int GetHashCode(Method obj) => StringComparer.Ordinal.GetHashCode(obj.Name);
	}
}
