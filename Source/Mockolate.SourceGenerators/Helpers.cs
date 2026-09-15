using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mockolate.SourceGenerators.Entities;
using Attribute = Mockolate.SourceGenerators.Entities.Attribute;
using Type = Mockolate.SourceGenerators.Entities.Type;

namespace Mockolate.SourceGenerators;

internal static class Helpers
{
	public static SymbolDisplayFormat TypeDisplayFormat { get; } = new(
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
		                      SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
		                      SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
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

	public static string EscapeIfKeyword(string name)
		=> SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

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

		while (parameters.Any(p => p.Name == candidateName))
		{
			suffix++;
			candidateName = $"{baseName}{suffix}";
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
			candidateName = $"{baseName}_{suffix}";
		}

		return candidateName;

		static bool HasIndexedConflict(string @base, string parameterName, int count)
		{
			if (!parameterName.StartsWith(@base, StringComparison.Ordinal))
			{
				return false;
			}

			string tail = parameterName.Substring(@base.Length);
			return tail.Length > 0 && int.TryParse(tail, out int index) && index >= 1 && index <= count;
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
	///     internal/protected internal with access granted. The <c>protected</c> half never counts,
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
	///         <c>base</c>, everything else gets a <c>NotSupportedException</c> stub.
	///     </para>
	/// </remarks>
	public static bool NeedsRefStructPipeline(this Type type)
		=> type.IsRefStruct
		   && type.SpecialGenericType is not (SpecialGenericType.Span or SpecialGenericType.ReadOnlySpan);

	/// <summary>
	///     Returns true if the parameter must flow through the ref-struct setup pipeline. The
	///     <see cref="Type" /> overload excludes <c>Span&lt;T&gt;</c> and <c>ReadOnlySpan&lt;T&gt;</c>
	///     because <c>by-value</c>, <c>out</c>, and <c>ref</c> positions use the
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
				if (typeSymbol.Name == "Span")
				{
					return SpecialGenericType.Span;
				}

				if (typeSymbol.Name == "ReadOnlySpan")
				{
					return SpecialGenericType.ReadOnlySpan;
				}

				if (typeSymbol.Name == "ValueTuple")
				{
					return SpecialGenericType.Tuple;
				}
			}
			else if (typeSymbol.ContainingNamespace is { Name: "Tasks", ContainingNamespace.Name: "Threading", } &&
			         typeSymbol.ContainingNamespace.ContainingNamespace?.ContainingNamespace.Name == "System" &&
			         typeSymbol.ContainingNamespace.ContainingNamespace?.ContainingNamespace?.ContainingNamespace
				         ?.IsGlobalNamespace == true)
			{
				if (typeSymbol.Name == "Task")
				{
					return SpecialGenericType.Task;
				}

				if (typeSymbol.Name == "ValueTask")
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
			while (@class.HasReservedName($"{name}_{index}"))
			{
				index++;
			}

			return $"{name}_{index}";
		}

		public bool HasReservedName(string candidate)
			=> @class.AllProperties().Any(p => p.Name == candidate) ||
			   @class.AllMethods().Any(m => m.Name == candidate) ||
			   @class.AllEvents().Any(e => e.Name == candidate) ||
			   @class.ReservedNames.Any(r => r == candidate);
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

	extension(StringBuilder sb)
	{
		public StringBuilder AppendDefaultValueGeneratorFor(Type type,
			string defaultValueName, string suffix = "")
		{
			if (type.TupleTypes is not null)
			{
				sb.Append("(");
				bool first = true;
				foreach (Type? genericType in type.TupleTypes.Value)
				{
					if (!first)
					{
						sb.Append(", ");
					}

					first = false;
					sb.AppendDefaultValueGeneratorFor(genericType, defaultValueName, suffix);
				}

				sb.Append(")");
				return sb;
			}

			sb.Append(defaultValueName);
			sb.Append(".Generate(default(");

			if (type.SpecialGenericType == SpecialGenericType.Span)
			{
				sb.Append("global::Mockolate.Setup.SpanWrapper<").Append(type.GenericTypeParameters!.Value.First().Fullname).Append(">");
			}
			else if (type.SpecialGenericType == SpecialGenericType.ReadOnlySpan)
			{
				sb.Append("global::Mockolate.Setup.ReadOnlySpanWrapper<").Append(type.GenericTypeParameters!.Value.First().Fullname).Append(">");
			}
			else
			{
				sb.Append(type.Fullname);
			}

			sb.Append(")!");

			if (type.SpecialGenericType is SpecialGenericType.Task or SpecialGenericType.ValueTask
			    && type.GenericTypeParameters?.Count > 0)
			{
				foreach (Type? genericType in type.GenericTypeParameters.Value)
				{
					sb.Append(", ")
						.AppendDefaultValueGeneratorFor(genericType, defaultValueName, suffix);
				}
			}

			if (!string.IsNullOrWhiteSpace(suffix))
			{
				sb.Append(", ").Append(suffix);
			}

			sb.Append(")");
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

				sb.Append("]").AppendLine();
			}

			return sb;
		}

		public StringBuilder AppendTypeOrWrapper(Type type)
		{
			if (type.SpecialGenericType == SpecialGenericType.Span)
			{
				sb.Append("global::Mockolate.Setup.SpanWrapper<").Append(type.GenericTypeParameters!.Value.First().Fullname).Append(">");
			}
			else if (type.SpecialGenericType == SpecialGenericType.ReadOnlySpan)
			{
				sb.Append("global::Mockolate.Setup.ReadOnlySpanWrapper<").Append(type.GenericTypeParameters!.Value.First().Fullname).Append(">");
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
					parameter.IsNullableAnnotated && !parameter.Type.Fullname.EndsWith("?")) switch
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
						parameter.IsNullableAnnotated && !parameter.Type.Fullname.EndsWith("?")) switch
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
			if (parameter.IsNullableAnnotated && !parameter.Type.Fullname.EndsWith("?"))
			{
				return $"{parameter.Type.Fullname}?";
			}

			return parameter.Type.Fullname;
		}

		/// <summary>
		///     Returns true if the parameter can be used as an explicit value in a T overload.
		///     Ambiguity with the <c>IParameter&lt;T&gt;?</c> overload when <c>null</c> is passed is
		///     resolved via the OverloadResolutionPriorityAttribute.
		/// </summary>
		public bool CanUseNullableParameterOverload()
			=> parameter.RefKind is not RefKind.Ref and not RefKind.Out and not RefKind.RefReadOnlyParameter &&
			   parameter.Type.SpecialGenericType is not (SpecialGenericType.Span or SpecialGenericType.ReadOnlySpan);
	}
}
