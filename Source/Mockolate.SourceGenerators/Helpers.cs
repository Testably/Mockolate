using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mockolate.SourceGenerators.Entities;
using Mockolate.SourceGenerators.Internals;
using Mockolate.SourceGenerators.Sources;
using Attribute = Mockolate.SourceGenerators.Entities.Attribute;
using Type = Mockolate.SourceGenerators.Entities.Type;

namespace Mockolate.SourceGenerators;

#pragma warning disable MA0038 // Make method static (deprecated, use CA1822 instead)
internal static class Helpers
{
	public static SymbolDisplayFormat TypeDisplayFormat { get; } = new(
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
		                      SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
		                      SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

	/// <summary>
	///     <see cref="TypeDisplayFormat" /> without the nullable annotations of reference types, at any depth, and without
	///     the names of tuple elements, which do not tell signatures apart.
	/// </summary>
	public static SymbolDisplayFormat TypeIdentityDisplayFormat { get; } = new(
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
		                      SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
		                      SymbolDisplayMiscellaneousOptions.ExpandValueTuple,
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

	public static SymbolDisplayFormat TypeDisplayShortFormat { get; } = new(
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
		                      SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
		                      SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

	private static SymbolDisplayFormat CrefTypeFormat { get; } = new(
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
		                      SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
		                      SymbolDisplayMiscellaneousOptions.ExpandValueTuple,
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

	/// <summary>
	///     Formats <paramref name="type" /> for an XML documentation <c>cref</c> attribute.
	/// </summary>
	/// <remarks>
	///     Tuple syntax is not valid in a <c>cref</c>, so tuples are spelled as <c>ValueTuple{...}</c>. Nullable reference
	///     annotations are omitted because <c>T?</c> of an unconstrained type parameter does not resolve.
	/// </remarks>
	public static string ToCref(this ITypeSymbol type)
		=> type.ToQualifiedDisplayString(CrefTypeFormat).Replace('<', '{').Replace('>', '}');

	/// <summary>
	///     Builds the XML documentation <c>cref</c> of <paramref name="member" /> named <paramref name="name" />.
	/// </summary>
	/// <remarks>
	///     A <c>cref</c> accepts only type parameters, not type arguments, on the containing type
	///     (<c>IFoo{int}.Bar(int)</c> raises CS1584/CS1658), so pass the <see cref="ISymbol.OriginalDefinition" />
	///     for a link. The <c>cref</c> of a member of a closed generic type is only fit as display text.
	/// </remarks>
	public static string ToMemberCref(ISymbol member, string name)
	{
		string containingType = member.ContainingType.ToCref();
		return member switch
		{
			IMethodSymbol { MethodKind: MethodKind.Constructor, } constructor =>
				$"{containingType}({ToCref(constructor.Parameters)})",
			IMethodSymbol method =>
				$"{containingType}.{name.Replace('<', '{').Replace('>', '}')}({ToCref(method.Parameters)})",
			IPropertySymbol { IsIndexer: true, } indexer => $"{containingType}.this[{ToCref(indexer.Parameters)}]",
			_ => $"{containingType}.{name}",
		};

		static string ToCref(ImmutableArray<IParameterSymbol> parameters)
			=> string.Join(", ", parameters.Select(p => p.RefKind.GetString() + p.Type.ToCref()));
	}

	/// <summary>
	///     Builds the <c>&lt;see&gt;</c> element that links <paramref name="cref" /> of an open generic definition and
	///     shows the <paramref name="closedCref" /> of the mocked closed generic type as its text.
	/// </summary>
	/// <remarks>
	///     When both are equal, the element is self-closing so that <c>SourceTemplates.ExpandCrefs</c> derives the text as usual.
	/// </remarks>
	public static string ToXmlDocReference(string cref, string closedCref)
		=> string.Equals(cref, closedCref, StringComparison.Ordinal)
			? $"<see cref=\"{cref}\" />"
			: $"<see cref=\"{cref}\">{SourceTemplates.SimplifyCrefForDisplay(closedCref)}</see>";

	/// <summary>
	///     Formats <paramref name="symbol" /> like <see cref="ISymbol.ToDisplayString" />, but qualifies types from a
	///     reference with only extern aliases by that alias instead of <c>global::</c>, under which they do not resolve.
	/// </summary>
	public static string ToQualifiedDisplayString(this ISymbol symbol, SymbolDisplayFormat format)
	{
		EntityCache? cache = EntityCache.Current;
		if (cache is null || !cache.HasExternAliases)
		{
			return symbol.ToDisplayString(format);
		}

		ImmutableArray<SymbolDisplayPart> parts = symbol.ToDisplayParts(format);
		StringBuilder sb = new();
		for (int i = 0; i < parts.Length; i++)
		{
			if (parts[i].Symbol is INamespaceSymbol { IsGlobalNamespace: true, ContainingAssembly: { } assembly, } &&
			    cache.GetExternAlias(assembly) is { } alias)
			{
				sb.Append(alias);
			}
			else
			{
				sb.Append(parts[i]);
			}
		}

		return sb.ToString();
	}

	/// <summary>
	///     Returns the type that is identical to <paramref name="type" />, but is constructed with
	///     <see langword="object" /> wherever <paramref name="type" /> is constructed with <see langword="dynamic" />.
	/// </summary>
	/// <remarks>
	///     A type that is constructed with <see langword="dynamic" /> can neither be implemented nor be a constraint
	///     (CS1966, CS1968), so it is mocked as the identical type that is constructed with <see langword="object" />.
	///     Replacing the symbol instead of its name makes both spellings the same mock, including the types of the
	///     members.
	/// </remarks>
	public static ITypeSymbol WithDynamicAsObject(this ITypeSymbol type, Compilation compilation)
	{
		switch (type)
		{
			case IDynamicTypeSymbol:
				return compilation.ObjectType.WithNullableAnnotation(type.NullableAnnotation);
			case IArrayTypeSymbol array:
			{
				ITypeSymbol elementType = array.ElementType.WithDynamicAsObject(compilation);
				return IsSameType(elementType, array.ElementType)
					? type
					: compilation.CreateArrayTypeSymbol(elementType, array.Rank, array.ElementNullableAnnotation)
						.WithNullableAnnotation(type.NullableAnnotation);
			}
			case INamedTypeSymbol { TypeKind: not TypeKind.Error, } namedType:
				return WithDynamicTypeArgumentsAsObject(namedType, compilation);
			default:
				return type;
		}
	}

	private static INamedTypeSymbol WithDynamicTypeArgumentsAsObject(INamedTypeSymbol type, Compilation compilation)
	{
		INamedTypeSymbol? containingType = type.ContainingType?.WithDynamicAsObject(compilation) as INamedTypeSymbol;
		ImmutableArray<ITypeSymbol> typeArguments = type.TypeArguments
			.Select(typeArgument => typeArgument.WithDynamicAsObject(compilation)).ToImmutableArray();
		bool isSameContainingType = IsSameType(containingType, type.ContainingType);
		if (isSameContainingType && typeArguments.SequenceEqual(type.TypeArguments, IsSameType))
		{
			return type;
		}

		INamedTypeSymbol result;
		if (type.IsTupleType)
		{
			// The elements instead of the type arguments, because they keep the names and flatten a long tuple.
			ImmutableArray<IFieldSymbol> elements = type.TupleElements;
			result = compilation.CreateTupleTypeSymbol(
				elements.Select(element => element.Type.WithDynamicAsObject(compilation)).ToImmutableArray(),
				elements.Select(GetExplicitName).ToImmutableArray(),
				elementNullableAnnotations: elements.Select(element => element.NullableAnnotation).ToImmutableArray());
		}
		else
		{
			INamedTypeSymbol definition = isSameContainingType
				? type.ConstructedFrom
				: containingType!.GetTypeMembers(type.Name, type.Arity)[0];
			result = typeArguments.IsEmpty
				? definition
				: definition.Construct(typeArguments, type.TypeArgumentNullableAnnotations);
		}

		return (INamedTypeSymbol)result.WithNullableAnnotation(type.NullableAnnotation);

		static string? GetExplicitName(IFieldSymbol element)
			=> element.IsExplicitlyNamedTupleElement ? element.Name : null;
	}

	private static bool IsSameType(ITypeSymbol? a, ITypeSymbol? b)
		=> SymbolEqualityComparer.IncludeNullability.Equals(a, b);

	public static string EscapeIfKeyword(string name)
		=> SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

	/// <summary>
	///     Whether a property or event of this name on an interface mock hides the methods of the same name that the
	///     mock inherits from <see cref="object" /> (CS0108).
	/// </summary>
	public static bool IsObjectMethodName(string name)
		=> name is "Equals" or "GetHashCode" or "GetType" or "ToString" or "MemberwiseClone" or "ReferenceEquals";

	/// <summary>
	///     Generates a unique local variable name that does not conflict with any parameter names.
	/// </summary>
	/// <param name="baseName">The base name for the variable (e.g., "result", "methodExecution")</param>
	/// <param name="parameters">The parameters to check against for conflicts</param>
	/// <returns>A unique variable name that does not conflict with any parameter names</returns>
	public static string GetUniqueLocalVariableName(string baseName, EquatableArray<MethodParameter> parameters)
	{
		string candidateName = baseName;
		int suffix = 0;

		while (parameters.Any(p => string.Equals(p.Name, candidateName, StringComparison.Ordinal)))
		{
			suffix++;
			candidateName = FormattableString.Invariant($"{baseName}{suffix}");
		}

		return candidateName;
	}

	/// <summary>
	///     Generates a unique base name such that <c>{base}1</c>..<c>{base}N</c> do not conflict with
	///     any parameter names. Used for numerically suffixed cast variables like <c>outParam1</c>.
	/// </summary>
	public static string GetUniqueIndexedLocalVariableBase(string baseName, EquatableArray<MethodParameter> parameters)
	{
		string candidateName = baseName;
		int suffix = 0;

		while (parameters.Any(p => HasIndexedConflict(candidateName, p.Name, parameters.Count)))
		{
			suffix++;
			candidateName = FormattableString.Invariant($"{baseName}_{suffix}");
		}

		return candidateName;

		static bool HasIndexedConflict(string @base, string parameterName, int count)
		{
			if (!parameterName.StartsWith(@base, StringComparison.Ordinal))
			{
				return false;
			}

			string tail = parameterName.Substring(@base.Length);
			return tail.Length > 0 && int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) && index >= 1 && index <= count;
		}
	}

	/// <summary>
	///     A member (or accessor) declared in another assembly is overridable only if the overriding
	///     assembly can actually see it. `internal` and `private protected` are invisible across assembly
	///     boundaries unless the declaring assembly grants InternalsVisibleTo. `protected internal`
	///     (= protected OR internal) is always reachable via the protected half from a derived class.
	/// </summary>
	public static bool IsOverridableFrom(ISymbol member, IAssemblySymbol? sourceAssembly)
	{
		if (sourceAssembly is null ||
		    member.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.ProtectedAndInternal))
		{
			return true;
		}

		IAssemblySymbol containingAssembly = member.ContainingAssembly;
		if (SymbolEqualityComparer.Default.Equals(containingAssembly, sourceAssembly))
		{
			return true;
		}

		return containingAssembly.GivesAccessTo(sourceAssembly);
	}

	/// <summary>
	///     C# requires an override to match the base member's declared accessibility, with one
	///     exception: when overriding a `protected internal` member from an assembly that cannot see
	///     `internal` (i.e. neither the same assembly nor an InternalsVisibleTo target), the override
	///     must drop the internal half and use plain `protected`.
	/// </summary>
	public static string ResolveOverrideVisibility(Accessibility accessibility,
		IAssemblySymbol? containingAssembly, IAssemblySymbol? sourceAssembly)
		=> accessibility switch
		{
			Accessibility.Public => "public",
			Accessibility.Protected => "protected",
			Accessibility.Internal => "internal",
			Accessibility.ProtectedAndInternal => "private protected",
			Accessibility.ProtectedOrInternal => HasInternalAccess(containingAssembly, sourceAssembly)
				? "protected internal"
				: "protected",
			_ => "private",
		};

	/// <summary>
	///     Conservative visibility test for a type the generator names verbatim (attribute names,
	///     constructor parameter types, member signatures): every type in the containing chain and
	///     every composed type (array element, pointed-at type, type argument) must be public, or
	///     internal/protected internal with access granted. The <see langword="protected" /> half never counts,
	///     because the surfaces naming the type do not derive from the mocked type.
	/// </summary>
	public static bool IsAccessibleFrom(ITypeSymbol type, IAssemblySymbol? sourceAssembly)
	{
		switch (type)
		{
			case IArrayTypeSymbol array:
				return IsAccessibleFrom(array.ElementType, sourceAssembly);
			case IPointerTypeSymbol pointer:
				return IsAccessibleFrom(pointer.PointedAtType, sourceAssembly);
			case INamedTypeSymbol named:
				// For `Outer<TArg>.Inner` the arguments sit on the containing type, so
				// `named.TypeArguments` alone would miss them.
				for (INamedTypeSymbol? t = named; t is not null; t = t.ContainingType)
				{
					if (!IsDeclarationAccessible(t) ||
					    !t.TypeArguments.All(argument => IsAccessibleFrom(argument, sourceAssembly)))
					{
						return false;
					}
				}

				return true;
			default:
				return true;
		}

		bool IsDeclarationAccessible(INamedTypeSymbol candidate)
			=> candidate.DeclaredAccessibility switch
			{
				Accessibility.Public => true,
				Accessibility.Internal or Accessibility.ProtectedOrInternal =>
					sourceAssembly is null || HasInternalAccess(candidate.ContainingAssembly, sourceAssembly),
				_ => false,
			};
	}

	/// <summary>
	///     True when every type named in <paramref name="member" />'s signature (return/member type,
	///     parameter types, generic constraint types) is accessible per <see cref="IsAccessibleFrom" />.
	///     The mock restates them on surfaces that do <b>not</b> derive from the mocked type
	///     (<c>IMockSetupForXXX</c>, <c>IMockVerifyForXXX</c>, <c>MockExtensionsForXXX</c>), so a
	///     <see langword="protected" /> nested type causes CS0122 there even though the
	///     <see langword="override" /> inside the mock class itself would compile.
	/// </summary>
	public static bool HasAccessibleSignature(ISymbol member, IAssemblySymbol? sourceAssembly)
		=> member switch
		{
			IMethodSymbol method => IsAccessibleFrom(method.ReturnType, sourceAssembly) &&
			                        method.Parameters.All(p => IsAccessibleFrom(p.Type, sourceAssembly)) &&
			                        method.TypeParameters.All(p => p.ConstraintTypes
				                        .All(c => IsAccessibleFrom(c, sourceAssembly))),
			IPropertySymbol property => IsAccessibleFrom(property.Type, sourceAssembly) &&
			                            property.Parameters.All(p => IsAccessibleFrom(p.Type, sourceAssembly)),
			IEventSymbol @event => IsAccessibleFrom(@event.Type, sourceAssembly),
			_ => true,
		};

	/// <summary>
	///     False when closing the containing type turns the constraints of a method type parameter into a
	///     contradiction (see <see cref="ConstraintConflicts" />), so no mock can implement the method.
	/// </summary>
	public static bool HasSatisfiableConstraints(ISymbol member)
		=> member is not IMethodSymbol method ||
		   method.TypeParameters.All(typeParameter => ConstraintConflicts.Find(typeParameter) is null);

	/// <summary>
	///     Whether <paramref name="a" /> and <paramref name="b" /> are emitted as the same type, which also holds for
	///     <see langword="dynamic" /> and <see langword="object" /> (see <see cref="Type" />).
	/// </summary>
	public static bool IsSameEmittedType(ITypeSymbol a, ITypeSymbol b)
		=> SymbolEqualityComparer.Default.Equals(a, b) ||
		   (IsObjectOrDynamic(a) && IsObjectOrDynamic(b));

	/// <summary>
	///     The parameters of <paramref name="method" /> with its type parameters replaced by the type parameters of
	///     <paramref name="other" /> at the same position, as both methods declare their own type parameter symbols.
	///     Both methods must have the same number of type parameters.
	/// </summary>
	public static ImmutableArray<IParameterSymbol> GetParametersInTermsOf(IMethodSymbol method, IMethodSymbol other)
		=> method.TypeParameters.IsEmpty
			? method.Parameters
			: method.Construct(other.TypeParameters.Cast<ITypeSymbol>().ToArray()).Parameters;

	private static bool IsObjectOrDynamic(ITypeSymbol type)
		=> type.TypeKind == TypeKind.Dynamic || type.SpecialType == SpecialType.System_Object;

	private static bool HasInternalAccess(IAssemblySymbol? containingAssembly, IAssemblySymbol? sourceAssembly)
	{
		if (sourceAssembly is null || containingAssembly is null)
		{
			return false;
		}

		if (SymbolEqualityComparer.Default.Equals(containingAssembly, sourceAssembly))
		{
			return true;
		}

		return containingAssembly.GivesAccessTo(sourceAssembly);
	}

	public static string ToTypeOrWrapper(this Type type)
	{
		if (type.SpecialGenericType == SpecialGenericType.Span)
		{
			return $"global::Mockolate.Setup.SpanWrapper<{type.GenericTypeParameters!.Value.First().Fullname}>";
		}

		if (type.SpecialGenericType == SpecialGenericType.ReadOnlySpan)
		{
			return $"global::Mockolate.Setup.ReadOnlySpanWrapper<{type.GenericTypeParameters!.Value.First().Fullname}>";
		}

		return type.Fullname;
	}

	/// <summary>
	///     Returns true if the given type needs the ref-struct setup pipeline: it is a ref-like
	///     type AND the existing Span/ReadOnlySpan wrapper fallback doesn't apply.
	/// </summary>
	/// <remarks>
	///     <para>
	///         <c>System.Span&lt;T&gt;</c> and <c>System.ReadOnlySpan&lt;T&gt;</c> are themselves
	///         ref-like, but the generator already boxes them into <c>SpanWrapper&lt;T&gt;</c> /
	///         <c>ReadOnlySpanWrapper&lt;T&gt;</c> (a plain class), so their setup flows through the
	///         regular <c>VoidMethodSetup</c> hierarchy with a non-ref-struct <c>T</c>. Only types
	///         outside that wrapping need the <c>RefStructVoidMethodSetup</c> /
	///         <c>RefStructReturnMethodSetup</c> / <c>RefStructIndexerGetterSetup</c> path.
	///     </para>
	///     <para>
	///         In a <i>value</i> position - a method or delegate return, a property type, an indexer
	///         value - there is no such pipeline to route into: those positions parameterize types that
	///         store a <c>Func&lt;T&gt;</c> (<c>IReturnMethodSetup&lt;T&gt;</c>,
	///         <c>IPropertyGetterOnlySetup&lt;T&gt;</c>, <c>IIndexerGetterOnlySetup&lt;TValue, ...&gt;</c>),
	///         which is illegal for a ref struct, so they cannot carry the <c>allows ref struct</c>
	///         anti-constraint the parameter positions use. Members matching this in a value position get
	///         no setup/verify surface: a virtual class member forwards to the wrapped instance or to
	///         <see langword="base" />, everything else gets a <c>NotSupportedException</c> stub.
	///     </para>
	/// </remarks>
	public static bool NeedsRefStructPipeline(this Type type)
		=> type.IsRefStruct
		   && type.SpecialGenericType is not (SpecialGenericType.Span or SpecialGenericType.ReadOnlySpan);

	/// <summary>
	///     Returns true if the parameter must flow through the ref-struct setup pipeline. The
	///     <see cref="Type" /> overload excludes <c>Span&lt;T&gt;</c> and <c>ReadOnlySpan&lt;T&gt;</c>
	///     because <c>by-value</c>, <see langword="out" />, and <see langword="ref" /> positions use the
	///     <c>SpanWrapper&lt;T&gt;</c> / <c>ReadOnlySpanWrapper&lt;T&gt;</c> non-ref-struct carve-out.
	///     <c>ref readonly</c> Span/ROS, however, has no dedicated wrapper-based emit branch, so it
	///     falls back to the generic ref-struct pipeline.
	/// </summary>
	public static bool NeedsRefStructPipeline(this MethodParameter parameter)
	{
		if (parameter.Type.NeedsRefStructPipeline())
		{
			return true;
		}

		return parameter.RefKind == RefKind.RefReadOnlyParameter
		       && parameter.Type.IsRefStruct
		       && parameter.Type.SpecialGenericType is SpecialGenericType.Span or SpecialGenericType.ReadOnlySpan;
	}

	extension(ITypeSymbol typeSymbol)
	{
		public SpecialGenericType GetSpecialType()
		{
			if (typeSymbol.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true, })
			{
				if (string.Equals(typeSymbol.Name, "Span", StringComparison.Ordinal))
				{
					return SpecialGenericType.Span;
				}

				if (string.Equals(typeSymbol.Name, "ReadOnlySpan", StringComparison.Ordinal))
				{
					return SpecialGenericType.ReadOnlySpan;
				}

				if (typeSymbol is INamedTypeSymbol { Name: "Tuple", Arity: > 0, })
				{
					return SpecialGenericType.Tuple;
				}
			}
			else if (typeSymbol.ContainingNamespace is { Name: "Tasks", ContainingNamespace.Name: "Threading", } &&
			         string.Equals(typeSymbol.ContainingNamespace.ContainingNamespace?.ContainingNamespace.Name, "System", StringComparison.Ordinal) &&
			         typeSymbol.ContainingNamespace.ContainingNamespace?.ContainingNamespace?.ContainingNamespace
				         ?.IsGlobalNamespace == true)
			{
				if (string.Equals(typeSymbol.Name, "Task", StringComparison.Ordinal))
				{
					return SpecialGenericType.Task;
				}

				if (string.Equals(typeSymbol.Name, "ValueTask", StringComparison.Ordinal))
				{
					return SpecialGenericType.ValueTask;
				}
			}

			return SpecialGenericType.None;
		}
	}

	extension(Class @class)
	{
		public string GetUniqueName(string name, params string[] alternatives)
		{
			if (!@class.HasReservedName(name))
			{
				return name;
			}

			string? alternative = alternatives.FirstOrDefault(a => !@class.HasReservedName(a));
			if (alternative is not null)
			{
				return alternative;
			}

			int index = 1;
			while (@class.HasReservedName(FormattableString.Invariant($"{name}_{index}")))
			{
				index++;
			}

			return FormattableString.Invariant($"{name}_{index}");
		}

		public bool HasReservedName(string candidate)
			=> @class.AllProperties().Any(p => string.Equals(p.Name, candidate, StringComparison.Ordinal)) ||
			   @class.AllMethods().Any(m => string.Equals(m.Name, candidate, StringComparison.Ordinal)) ||
			   @class.AllEvents().Any(e => string.Equals(e.Name, candidate, StringComparison.Ordinal)) ||
			   @class.ReservedNames.Any(r => string.Equals(r, candidate, StringComparison.Ordinal));
	}

	extension(ImmutableArray<AttributeData> attributes)
	{
		public EquatableArray<Attribute>? ToAttributeArray(IAssemblySymbol? sourceAssembly = null)
		{
			// The attribute name is emitted verbatim, so an attribute class invisible to the mock's
			// assembly would cause CS0122. Drop it instead of emitting uncompilable code.
			Attribute[] consideredAttributes = attributes
				.Where(x => x.AttributeClass is not null
				            && !IsCompilerEmittedAttribute(x.AttributeClass)
				            && IsAccessibleFrom(x.AttributeClass, sourceAssembly))
				.Select(attr => new Attribute(attr))
				.ToArray();
			if (consideredAttributes.Length > 0)
			{
				return new EquatableArray<Attribute>(consideredAttributes);
			}

			return null;

			// Compiler-emitted attributes describe the shape of the original method body (nullability,
			// iterator/async state machine types) and MUST NOT be copied onto the generated override.
			// The state-machine attributes reference private nested compiler-generated types that are
			// invisible outside the declaring assembly (CS0103), and the override has its own,
			// synchronous body that is not a state machine anyway.
			static bool IsCompilerEmittedAttribute(INamedTypeSymbol attribute)
			{
				if (attribute.ContainingNamespace is not { Name: "CompilerServices", ContainingNamespace: { Name: "Runtime", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true, }, }, })
				{
					return false;
				}

				return attribute.Name is "NullableContextAttribute" or "NullableAttribute"
					or "AsyncStateMachineAttribute" or "IteratorStateMachineAttribute"
					or "AsyncIteratorStateMachineAttribute";
			}
		}
	}

	/// <summary>
	///     The constructor of an eight element <see cref="System.Tuple" /> throws when its last element is no tuple.
	/// </summary>
	private static bool IsConstructibleTuple(Type type)
		=> type.SpecialGenericType == SpecialGenericType.Tuple &&
		   (type.GenericTypeParameters!.Value.Count < 8 ||
		    IsConstructibleTuple(type.GenericTypeParameters.Value.ElementAt(7)));

	extension(StringBuilder sb)
	{
		/// <remarks>
		///     The <paramref name="taskSuffix" /> holds the arguments for the wrapping of a value in a task, which only
		///     looks for a cancellation token, or is <see langword="null" /> to use the <paramref name="suffix" />.
		///     <para />
		///     The <paramref name="autoMock" /> returns the code before and after the default value of an auto-mockable
		///     type (also the result type of a task or an element of a tuple), that replaces a <see langword="null" />
		///     with an auto-mock, or <see langword="null" /> when the type gets no auto-mock.
		/// </remarks>
		public StringBuilder AppendDefaultValueGeneratorFor(Type type,
			string defaultValueName, string suffix = "", string? taskSuffix = null,
			Func<Type, (string Prefix, string Suffix)?>? autoMock = null)
		{
			if (autoMock?.Invoke(type) is (string prefix, string autoMockSuffix))
			{
				return sb.Append(prefix)
					.AppendDefaultValueGeneratorFor(type, defaultValueName, suffix, taskSuffix)
					.Append(autoMockSuffix);
			}

			if (type.TupleTypes is not null)
			{
				sb.Append('(').AppendTupleFactoryValue(type, defaultValueName, suffix, isValueTuple: true).Append(" ?? ");
				switch (type.TupleTypes.Value.Count)
				{
					case 0:
						sb.Append("default(").Append(type.Fullname).Append(')');
						break;
					case 1:
						// A single element in parentheses is no tuple literal.
						sb.Append("new ").Append(type.Fullname).Append('(');
						sb.AppendTupleElementDefaultValues(type.TupleTypes.Value, defaultValueName, suffix, taskSuffix,
							autoMock);
						sb.Append(')');
						break;
					default:
						sb.Append('(');
						sb.AppendTupleElementDefaultValues(type.TupleTypes.Value, defaultValueName, suffix, taskSuffix,
							autoMock);
						sb.Append(')');
						break;
				}

				sb.Append(')');
				return sb;
			}

			if (IsConstructibleTuple(type))
			{
				sb.Append('(').AppendTupleFactoryValue(type, defaultValueName, suffix, isValueTuple: false)
					.Append(" ?? new ").Append(type.Fullname.TrimEnd('?')).Append('(');
				sb.AppendTupleElementDefaultValues(type.GenericTypeParameters!.Value, defaultValueName, suffix,
					taskSuffix, autoMock);
				sb.Append("))");
				return sb;
			}

			return sb.AppendGeneratedDefaultValueFor(type, defaultValueName, suffix, taskSuffix, autoMock);
		}

		/// <summary>
		///     The value of a factory for the tuple <paramref name="type" />, or <see langword="null" />, so that the
		///     factory takes precedence over the tuple of default values.
		/// </summary>
		/// <remarks>
		///     The probe avoids collecting the arguments when no factory can match.
		/// </remarks>
		private StringBuilder AppendTupleFactoryValue(Type type, string defaultValueName, string suffix,
			bool isValueTuple)
		{
			string generateWithoutArguments = isValueTuple
				? $"{defaultValueName}.GenerateOrNull<{type.Fullname}>("
				: $"{defaultValueName}.Generate(default({type.Fullname})!";
			string argumentSeparator = isValueTuple ? "" : ", ";
			sb.Append('(').Append(defaultValueName).Append(".Probe<").Append(type.Fullname)
				.Append(">() switch { global::Mockolate.Internals.DefaultValueSource.None => null, ");
			if (!string.IsNullOrWhiteSpace(suffix))
			{
				sb.Append("global::Mockolate.Internals.DefaultValueSource.WithoutArguments => ")
					.Append(generateWithoutArguments).Append("), ");
				sb.Append("_ => ").Append(generateWithoutArguments).Append(argumentSeparator).Append(suffix)
					.Append(") })");
			}
			else
			{
				sb.Append("_ => ").Append(generateWithoutArguments).Append(") })");
			}

			return sb;
		}

		private void AppendTupleElementDefaultValues(EquatableArray<Type> elementTypes,
			string defaultValueName, string suffix, string? taskSuffix,
			Func<Type, (string Prefix, string Suffix)?>? autoMock)
		{
			bool first = true;
			foreach (Type? elementType in elementTypes)
			{
				if (!first)
				{
					sb.Append(", ");
				}

				first = false;
				sb.AppendDefaultValueGeneratorFor(elementType, defaultValueName, suffix, taskSuffix, autoMock);
			}
		}

		private StringBuilder AppendGeneratedDefaultValueFor(Type type,
			string defaultValueName, string suffix, string? taskSuffix,
			Func<Type, (string Prefix, string Suffix)?>? autoMock)
		{
			// The arguments are only collected for a type that binds to the general Generate<T> overload, and
			// only when the generator may have a value for it.
			bool isLazy = !string.IsNullOrWhiteSpace(suffix) && type.ElementType is null &&
			              !(type.GenericTypeParameters?.Count > 0);
			// A task without a result only depends on a cancellation token, so the built-in one is created inline.
			string? completedTask = null;
			string? tokenArgument = taskSuffix ?? (string.IsNullOrWhiteSpace(suffix) ? "" : null);
			if (tokenArgument is not null && type.ElementType is null && !(type.GenericTypeParameters?.Count > 0))
			{
				if (type.SpecialGenericType == SpecialGenericType.Task)
				{
					completedTask = tokenArgument.Length == 0
						? "global::System.Threading.Tasks.Task.CompletedTask"
						: $"({tokenArgument}.IsCancellationRequested ? global::System.Threading.Tasks.Task.FromCanceled({tokenArgument}) : global::System.Threading.Tasks.Task.CompletedTask)";
				}
				else if (type.SpecialGenericType == SpecialGenericType.ValueTask)
				{
					completedTask = tokenArgument.Length == 0
						? "default(global::System.Threading.Tasks.ValueTask)"
						: $"({tokenArgument}.IsCancellationRequested ? new global::System.Threading.Tasks.ValueTask(global::System.Threading.Tasks.Task.FromCanceled({tokenArgument})) : default(global::System.Threading.Tasks.ValueTask))";
				}
			}

			if (isLazy)
			{
				sb.Append('(').Append(defaultValueName).Append(".Probe<").Append(type.Fullname)
					.Append(">() switch { global::Mockolate.Internals.DefaultValueSource.None => default(")
					.Append(type.Fullname)
					.Append(")!, global::Mockolate.Internals.DefaultValueSource.WithoutArguments => ")
					.Append(defaultValueName).Append(".Generate(default(").Append(type.Fullname).Append(")!), ");
				if (completedTask is not null)
				{
					sb.Append("global::Mockolate.Internals.DefaultValueSource.CancellationToken => ")
						.Append(completedTask).Append(", ");
				}

				sb.Append("_ => ");
			}
			else if (completedTask is not null)
			{
				sb.Append('(').Append(defaultValueName).Append(".Probe<").Append(type.Fullname)
					.Append(">() == global::Mockolate.Internals.DefaultValueSource.CancellationToken ? ")
					.Append(completedTask).Append(" : ");
			}

			sb.Append(defaultValueName);
			sb.Append(".Generate(default(");

			if (type.SpecialGenericType == SpecialGenericType.Span)
			{
				sb.Append("global::Mockolate.Setup.SpanWrapper<").Append(type.GenericTypeParameters!.Value.First().Fullname).Append('>');
			}
			else if (type.SpecialGenericType == SpecialGenericType.ReadOnlySpan)
			{
				sb.Append("global::Mockolate.Setup.ReadOnlySpanWrapper<").Append(type.GenericTypeParameters!.Value.First().Fullname).Append('>');
			}
			else
			{
				sb.Append(type.Fullname);
			}

			sb.Append(")!");

			string appendedSuffix = suffix;
			if (type.SpecialGenericType is SpecialGenericType.Task or SpecialGenericType.ValueTask
			    && type.GenericTypeParameters?.Count > 0)
			{
				foreach (Type? genericType in type.GenericTypeParameters.Value)
				{
					sb.Append(", ")
						.AppendDefaultValueGeneratorFor(genericType, defaultValueName, suffix, taskSuffix, autoMock);
				}

				appendedSuffix = taskSuffix ?? suffix;
			}

			if (!string.IsNullOrWhiteSpace(appendedSuffix))
			{
				sb.Append(", ").Append(appendedSuffix);
			}

			sb.Append(')');
			if (isLazy)
			{
				sb.Append(" })");
			}
			else if (completedTask is not null)
			{
				sb.Append(')');
			}

			return sb;
		}

		public StringBuilder Append(EquatableArray<Attribute>? attributes, string prefix = "")
		{
			if (attributes is null)
			{
				return sb;
			}

			foreach (Attribute? attribute in attributes)
			{
				sb.Append(prefix).Append('[').Append(attribute.Name);
				if (attribute.Parameters != null || attribute.NamedArguments != null)
				{
					sb.Append('(');
					if (attribute.Parameters != null)
					{
						sb.Append(string.Join(", ", attribute.Parameters.Value.Select(parameter => parameter.Value)));
					}

					if (attribute.NamedArguments != null)
					{
						if (attribute.Parameters != null)
						{
							sb.Append(", ");
						}

						sb.Append(string.Join(", ", attribute.NamedArguments.Value
							.Select(item => $"{item.Name} = {item.Parameter.Value}")));
					}

					sb.Append(')');
				}

				sb.Append(']').AppendLine();
			}

			return sb;
		}

		public StringBuilder AppendTypeOrWrapper(Type type)
		{
			if (type.SpecialGenericType == SpecialGenericType.Span)
			{
				sb.Append("global::Mockolate.Setup.SpanWrapper<").Append(type.GenericTypeParameters!.Value.First().Fullname).Append('>');
			}
			else if (type.SpecialGenericType == SpecialGenericType.ReadOnlySpan)
			{
				sb.Append("global::Mockolate.Setup.ReadOnlySpanWrapper<").Append(type.GenericTypeParameters!.Value.First().Fullname).Append('>');
			}
			else
			{
				sb.Append(type.Fullname);
			}

			return sb;
		}

		public StringBuilder AppendVerifyParameter(MethodParameter parameter)
		{
			sb.Append((parameter.RefKind, parameter.Type.SpecialGenericType) switch
			{
				(RefKind.Ref, _) => "global::Mockolate.Parameters.IVerifyRefParameter<",
				(RefKind.Out, _) => "global::Mockolate.Parameters.IVerifyOutParameter<",
				(_, SpecialGenericType.Span) => "global::Mockolate.Parameters.IVerifySpanParameter<",
				(_, SpecialGenericType.ReadOnlySpan) => "global::Mockolate.Parameters.IVerifyReadOnlySpanParameter<",
				(_, _) => "global::Mockolate.Parameters.IParameter<",
			});
			sb.Append((parameter.Type.SpecialGenericType,
					parameter.IsNullableAnnotated && !parameter.Type.Fullname.EndsWith("?", StringComparison.Ordinal)) switch
				{
					(SpecialGenericType.Span, _) => parameter.Type.GenericTypeParameters!.Value.First().Fullname,
					(SpecialGenericType.ReadOnlySpan, _) => parameter.Type.GenericTypeParameters!.Value.First().Fullname,
					(_, false) => parameter.Type.Fullname,
					(_, true) => $"{parameter.Type.Fullname}?",
				}).Append('>');

			return sb;
		}
	}

	extension(MethodParameter parameter)
	{
		public string ToNameOrWrapper()
		{
			if (parameter.Type.SpecialGenericType == SpecialGenericType.Span)
			{
				return $"new global::Mockolate.Setup.SpanWrapper<{parameter.Type.GenericTypeParameters!.Value.First().Fullname}>({parameter.Name})";
			}

			if (parameter.Type.SpecialGenericType == SpecialGenericType.ReadOnlySpan)
			{
				return
					$"new global::Mockolate.Setup.ReadOnlySpanWrapper<{parameter.Type.GenericTypeParameters!.Value.First().Fullname}>({parameter.Name})";
			}

			return parameter.Name;
		}

		public string ToTypeOrWrapper()
		{
			if (parameter.Type.SpecialGenericType == SpecialGenericType.Span)
			{
				return $"global::Mockolate.Setup.SpanWrapper<{parameter.Type.GenericTypeParameters!.Value.First().Fullname}>";
			}

			if (parameter.Type.SpecialGenericType == SpecialGenericType.ReadOnlySpan)
			{
				return
					$"global::Mockolate.Setup.ReadOnlySpanWrapper<{parameter.Type.GenericTypeParameters!.Value.First().Fullname}>";
			}

			return parameter.Type.Fullname;
		}

		public string ToParameter()
		{
			bool needsRefStructPipeline = parameter.NeedsRefStructPipeline();
			return (parameter.RefKind, parameter.Type.SpecialGenericType, needsRefStructPipeline) switch
			{
				(RefKind.Ref, _, true) =>
					$"global::Mockolate.Parameters.IRefRefStructParameter<{GetMethodParameterType(parameter)}>",
				(RefKind.Out, _, true) =>
					$"global::Mockolate.Parameters.IOutRefStructParameter<{GetMethodParameterType(parameter)}>",
				(RefKind.RefReadOnlyParameter, _, true) =>
					$"global::Mockolate.Parameters.IRefRefStructParameter<{GetMethodParameterType(parameter)}>",
				(RefKind.Out, SpecialGenericType.Span, _) =>
					$"global::Mockolate.Parameters.IOutParameter<global::Mockolate.Setup.SpanWrapper<{parameter.Type.GenericTypeParameters!.Value.First().Fullname}>>",
				(RefKind.Out, SpecialGenericType.ReadOnlySpan, _) =>
					$"global::Mockolate.Parameters.IOutParameter<global::Mockolate.Setup.ReadOnlySpanWrapper<{parameter.Type.GenericTypeParameters!.Value.First().Fullname}>>",
				(RefKind.Ref, SpecialGenericType.Span, _) =>
					$"global::Mockolate.Parameters.IRefParameter<global::Mockolate.Setup.SpanWrapper<{parameter.Type.GenericTypeParameters!.Value.First().Fullname}>>",
				(RefKind.Ref, SpecialGenericType.ReadOnlySpan, _) =>
					$"global::Mockolate.Parameters.IRefParameter<global::Mockolate.Setup.ReadOnlySpanWrapper<{parameter.Type.GenericTypeParameters!.Value.First().Fullname}>>",
				(RefKind.Ref, _, _) => $"global::Mockolate.Parameters.IRefParameter<{GetMethodParameterType(parameter)}>",
				(RefKind.Out, _, _) => $"global::Mockolate.Parameters.IOutParameter<{GetMethodParameterType(parameter)}>",
				(RefKind.RefReadOnlyParameter, _, _) =>
					$"global::Mockolate.Parameters.IRefParameter<{GetMethodParameterType(parameter)}>",
				(_, SpecialGenericType.Span, _) =>
					$"global::Mockolate.Parameters.ISpanParameter<{GetMethodParameterType(parameter)}>",
				(_, SpecialGenericType.ReadOnlySpan, _) =>
					$"global::Mockolate.Parameters.IReadOnlySpanParameter<{GetMethodParameterType(parameter)}>",
				(_, _, _) => $"global::Mockolate.Parameters.IParameter<{GetMethodParameterType(parameter)}>",
			};

			static string GetMethodParameterType(MethodParameter parameter)
			{
				// The non-ref-struct routes for Span/ROS use the SpanWrapper / ReadOnlySpanWrapper
				// matcher, so the relevant generic argument is the element type. The ref-struct
				// pipeline, by contrast, handles the bare Span<T> / ReadOnlySpan<T> directly, so we
				// must preserve the full type there.
				bool stripSpanToElement = !parameter.NeedsRefStructPipeline();
				return (parameter.Type.SpecialGenericType,
						parameter.IsNullableAnnotated && !parameter.Type.Fullname.EndsWith("?", StringComparison.Ordinal)) switch
					{
						(SpecialGenericType.Span, _) when stripSpanToElement =>
							parameter.Type.GenericTypeParameters!.Value.First().Fullname,
						(SpecialGenericType.ReadOnlySpan, _) when stripSpanToElement =>
							parameter.Type.GenericTypeParameters!.Value.First().Fullname,
						(_, false) => parameter.Type.Fullname,
						(_, true) => $"{parameter.Type.Fullname}?",
					};
			}
		}

		public string ToNullableType()
		{
			if (parameter.IsNullableAnnotated && !parameter.Type.Fullname.EndsWith("?", StringComparison.Ordinal))
			{
				return $"{parameter.Type.Fullname}?";
			}

			return parameter.Type.Fullname;
		}

		/// <summary>
		///     Returns true if the parameter can be used as an explicit value in a T overload.
		///     Ambiguity with the <c>IParameter&lt;T&gt;?</c> overload when <see langword="null" /> is passed is
		///     resolved via the OverloadResolutionPriorityAttribute.
		/// </summary>
		public bool CanUseNullableParameterOverload()
			=> parameter.RefKind is not RefKind.Ref and not RefKind.Out and not RefKind.RefReadOnlyParameter &&
			   parameter.Type.SpecialGenericType is not (SpecialGenericType.Span or SpecialGenericType.ReadOnlySpan);
	}
}
#pragma warning restore MA0038 // Make method static (deprecated, use CA1822 instead)
