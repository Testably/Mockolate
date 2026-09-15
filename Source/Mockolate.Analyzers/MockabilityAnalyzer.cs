using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mockolate.Analyzers;

/// <summary>
///     Analyzer that ensures all types used with <c>CreateMock()</c> and <c>Implementing&lt;T&gt;()</c> are mockable.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MockabilityAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor s_refStructRule = new(
		"Mockolate0003",
		new LocalizableResourceString(nameof(Resources.Mockolate0003Title),
			Resources.ResourceManager, typeof(Resources)),
		new LocalizableResourceString(nameof(Resources.Mockolate0003MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
		"Usage",
		DiagnosticSeverity.Warning,
		true,
		new LocalizableResourceString(nameof(Resources.Mockolate0003Description),
			Resources.ResourceManager, typeof(Resources)));

	/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics" />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		=> ImmutableArray.Create(Rules.MockabilityRule, s_refStructRule);

	/// <inheritdoc cref="DiagnosticAnalyzer.Initialize(AnalysisContext)" />
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess, } invocation)
		{
			return;
		}

		string methodName = memberAccess.Name.Identifier.ValueText;
		if (methodName != "CreateMock" && methodName != "Implementing")
		{
			return;
		}

		// Resolve the method if possible. When C# 14 extension member syntax is not fully
		// supported by the host Roslyn version, the call may be unresolved (null symbol).
		SymbolInfo invocationInfo = context.SemanticModel.GetSymbolInfo(invocation);
		IMethodSymbol? method = invocationInfo.Symbol as IMethodSymbol
		                        ?? invocationInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

		// If the method resolves to something outside the Mockolate namespace, skip it.
		if (method is not null && !IsInMockolateNamespace(method))
		{
			return;
		}

		if (methodName == "CreateMock")
		{
			AnalyzeCreateMock(context, memberAccess, method);
		}
		else
		{
			AnalyzeImplementing(context, invocation, memberAccess, method);
		}
	}

	private static void AnalyzeCreateMock(SyntaxNodeAnalysisContext context,
		MemberAccessExpressionSyntax memberAccess,
		IMethodSymbol? method)
	{
		// The receiver of CreateMock() is always a type name, not a value.
		// GetSymbolInfo on the receiver returns the type symbol directly.
		ITypeSymbol? receiverType = GetReceiverType(context, memberAccess, method);
		if (receiverType is null || receiverType is ITypeParameterSymbol || AnalyzerHelpers.IsOpenGeneric(receiverType))
		{
			return;
		}

		if (!IsMockable(receiverType, context.Compilation.Assembly, out string? reason))
		{
			context.ReportDiagnostic(Diagnostic.Create(
				Rules.MockabilityRule,
				memberAccess.Expression.GetLocation(),
				receiverType.ToDisplayString(),
				reason));
			return;
		}

		ReportRefStructIssuesForType(context, receiverType, memberAccess.Expression.GetLocation());
	}

	/// <summary>
	///     Emits <see cref="s_refStructRule" /> for each method/indexer on
	///     <paramref name="type" /> (and its base/interface hierarchy) that the ref-struct generator
	///     pipeline cannot safely emit on the current compilation. Multiple issues are reported per
	///     <c>CreateMock</c> site; users get a complete picture of what to fix.
	/// </summary>
	private static void ReportRefStructIssuesForType(SyntaxNodeAnalysisContext context, ITypeSymbol type,
		Location location)
	{
		string? pipelineUnsupportedReason = GetRefStructPipelineUnsupportedReason(context.Compilation);

		// For delegates only the synthesized Invoke matters — BeginInvoke/EndInvoke are legacy
		// helpers the generator does not emit overrides for, so we must not flag them.
		if (type.TypeKind == TypeKind.Delegate)
		{
			if (type is INamedTypeSymbol { DelegateInvokeMethod: { } invoke, } &&
			    TryGetRefStructIssue(invoke, pipelineUnsupportedReason, out string? delegateIssue, isDelegate: true))
			{
				context.ReportDiagnostic(Diagnostic.Create(
					s_refStructRule,
					location,
					type.ToDisplayString(),
					"Invoke",
					delegateIssue));
			}

			return;
		}

		foreach (ISymbol member in EnumerateAllMembers(type))
		{
			switch (member)
			{
				case IMethodSymbol { MethodKind: MethodKind.Ordinary, } m
					when TryGetRefStructIssue(m, pipelineUnsupportedReason, out string? issue):
					context.ReportDiagnostic(Diagnostic.Create(
						s_refStructRule,
						location,
						type.ToDisplayString(),
						m.Name,
						issue));
					break;
				case IPropertySymbol { IsIndexer: true, } p
					when TryGetRefStructIssueForIndexer(p, pipelineUnsupportedReason, out string? issue):
					context.ReportDiagnostic(Diagnostic.Create(
						s_refStructRule,
						location,
						type.ToDisplayString(),
						"this[]",
						issue));
					break;
				case IPropertySymbol { IsIndexer: false, } p when NeedsRefStructPipeline(p.Type):
					context.ReportDiagnostic(Diagnostic.Create(
						s_refStructRule,
						location,
						type.ToDisplayString(),
						p.Name,
						"properties of a non-span ref struct type are not supported"));
					break;
			}
		}
	}

	/// <summary>
	///     Yields the members of <paramref name="type" /> that the generator will actually emit a mock
	///     override for. Mirrors <c>Source/Mockolate.SourceGenerators/Entities/Class.cs</c>: every
	///     non-sealed member on an interface; every non-sealed <c>virtual</c>/<c>abstract</c> member on
	///     a class (including its base hierarchy). Members are de-duplicated by signature so an
	///     override and its virtual base don't produce two diagnostics for the same logical method.
	/// </summary>
	private static IEnumerable<ISymbol> EnumerateAllMembers(ITypeSymbol type)
	{
		bool isInterface = type.TypeKind == TypeKind.Interface;
		HashSet<string> seen = new(StringComparer.Ordinal);

		foreach ((ISymbol member, _) in GetCandidateMembers(type, isInterface)
			         .Where(c => IsOverriddenByGenerator(c.member, c.isInterface) &&
			                     seen.Add(GetSignatureKey(c.member))))
		{
			yield return member;
		}
	}

	private static IEnumerable<(ISymbol member, bool isInterface)> GetCandidateMembers(ITypeSymbol type,
		bool isInterface)
	{
		foreach (ISymbol m in type.GetMembers())
		{
			yield return (m, isInterface);
		}

		foreach (INamedTypeSymbol iface in type.AllInterfaces)
		{
			foreach (ISymbol m in iface.GetMembers())
			{
				yield return (m, true);
			}
		}

		if (isInterface)
		{
			yield break;
		}

		for (ITypeSymbol? t = type.BaseType;
		     t is not null && t.SpecialType != SpecialType.System_Object;
		     t = t.BaseType)
		{
			foreach (ISymbol m in t.GetMembers())
			{
				yield return (m, false);
			}
		}
	}

	/// <summary>
	///     Matches the member filter in <c>Class.cs</c>: <c>!IsSealed</c> for all kinds, and for class
	///     mocks the member must be abstract or virtual (since the generator can only override
	///     abstract/virtual members).
	/// </summary>
	private static bool IsOverriddenByGenerator(ISymbol member, bool isInterface)
	{
		if (member.IsSealed)
		{
			return false;
		}

		return isInterface || member.IsAbstract || member.IsVirtual;
	}

	/// <summary>
	///     Produces a containing-type-independent signature key so members are de-duplicated across
	///     the type hierarchy. Covers methods (name + generic arity + parameter RefKind/type) and
	///     indexers (parameter RefKind/type). RefKind is part of C#'s overload signature
	///     (<c>M(int)</c> vs <c>M(ref int)</c> vs <c>M(in int)</c> vs <c>M(out int)</c> are
	///     distinct), so collapsing on type alone would hide ref-struct diagnostics on one of the
	///     overloads.
	/// </summary>
	private static string GetSignatureKey(ISymbol member)
	{
		switch (member)
		{
			case IMethodSymbol method:
				return "M:" + method.Name + "`" + method.Arity + "(" +
				       string.Join(",", method.Parameters.Select(FormatParameter)) + ")";
			case IPropertySymbol { IsIndexer: true, } indexer:
				return "I:(" +
				       string.Join(",", indexer.Parameters.Select(FormatParameter)) + ")";
			default:
				return member.Kind + ":" + member.Name;
		}
	}

	private static string FormatParameter(IParameterSymbol parameter)
		=> parameter.RefKind == RefKind.None
			? parameter.Type.ToDisplayString()
			: parameter.RefKind.ToString().ToLowerInvariant() + " " + parameter.Type.ToDisplayString();

	/// <summary>
	///     Mirrors <c>Helpers.NeedsRefStructPipeline</c> from the source generator: a type is
	///     ref-like AND it's not <c>Span&lt;T&gt;</c>/<c>ReadOnlySpan&lt;T&gt;</c> (which the
	///     generator handles via wrapper boxing).
	/// </summary>
	private static bool NeedsRefStructPipeline(ITypeSymbol type)
	{
		if (!type.IsRefLikeType)
		{
			return false;
		}

		return !(type.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true, } &&
		         type.Name is "Span" or "ReadOnlySpan");
	}

	/// <summary>
	///     Returns a human-readable reason when the current compilation cannot host the
	///     ref-struct pipeline, or <see langword="null" /> when it is supported. Both the target
	///     framework (Mockolate's ref-struct types are <c>#if NET9_0_OR_GREATER</c>-gated) and the
	///     effective C# language version (<c>allows ref struct</c> is a C# 13 feature) must admit
	///     the pipeline — a net9.0+ project with <c>&lt;LangVersion&gt;</c> pinned below 13 would
	///     otherwise silently slip past the type-presence check and then fail to compile generated
	///     output.
	/// </summary>
	private static string? GetRefStructPipelineUnsupportedReason(Compilation compilation)
	{
		if (compilation.GetTypeByMetadataName("Mockolate.Setup.IRefStructVoidMethodSetup`1") is null)
		{
			return ".NET 9 or later (the referenced Mockolate assembly does not ship the ref-struct pipeline)";
		}

		if (compilation is CSharpCompilation { LanguageVersion: < LanguageVersion.CSharp13, } cs)
		{
			return
				$"C# 13 or later (uses the 'allows ref struct' anti-constraint; current LangVersion is {cs.LanguageVersion.ToDisplayString()})";
		}

		return null;
	}

	private static bool TryGetRefStructIssue(IMethodSymbol method, string? pipelineUnsupportedReason,
		out string? issue, bool isDelegate = false)
	{
		// Reported ahead of the parameter checks below, mirroring the generator's branch order: a
		// ref-struct return is out of scope on every target, so naming it is more actionable than
		// telling the user to upgrade to .NET 9 for a parameter that would still not be enough.
		// Span/ReadOnlySpan returns go through the wrapper and are fine.
		if (NeedsRefStructPipeline(method.ReturnType))
		{
			issue = "methods returning a non-span ref struct are not supported";
			return true;
		}

		bool hasRefStructParam = false;
		foreach (IParameterSymbol p in method.Parameters)
		{
			if (!NeedsRefStructPipeline(p.Type))
			{
				continue;
			}

			hasRefStructParam = true;

			// Delegates don't go through the ref-struct setup pipeline at all, so any ref-struct
			// parameter (by-value or by-ref) is unsupported on delegate Invoke methods — the
			// emitted VoidMethodSetup<T> / ReturnMethodSetup<T> lacks an 'allows ref struct'
			// constraint. Interface/class methods route through the IOutRefStructParameter /
			// IRefRefStructParameter pipeline.
			if (isDelegate)
			{
				issue = "ref-struct parameters are not supported on delegate types";
				return true;
			}
		}

		if (hasRefStructParam && pipelineUnsupportedReason is not null)
		{
			issue = $"ref-struct parameter mocking requires {pipelineUnsupportedReason}";
			return true;
		}

		// Note: no arity ceiling for ref-struct methods. Arities 1-4 are hand-written types in
		// Source/Mockolate/Setup/; arity 5+ are emitted by the generator into
		// RefStructMethodSetups.g.cs.

		issue = null;
		return false;
	}

	private static bool TryGetRefStructIssueForIndexer(IPropertySymbol indexer, string? pipelineUnsupportedReason,
		out string? issue)
	{
		// The value type is out of scope regardless of the keys: IIndexerGetterOnlySetup<TValue, ...>
		// stores a Func<TValue>, so TValue carries no 'allows ref struct' anti-constraint.
		if (NeedsRefStructPipeline(indexer.Type))
		{
			issue = "indexers returning a non-span ref struct are not supported";
			return true;
		}

		if (!indexer.Parameters.Any(p => NeedsRefStructPipeline(p.Type)))
		{
			issue = null;
			return false;
		}

		if (pipelineUnsupportedReason is not null)
		{
			issue = $"ref-struct-keyed indexers require {pipelineUnsupportedReason}";
			return true;
		}

		// Ref-struct-keyed indexers (getter-only, setter-only, and get+set) are fully supported
		// via the ref-struct pipeline.
		issue = null;
		return false;
	}

	private static void AnalyzeImplementing(SyntaxNodeAnalysisContext context,
		InvocationExpressionSyntax invocation,
		MemberAccessExpressionSyntax memberAccess,
		IMethodSymbol? method)
	{
		// Get the type argument — from the resolved method if available, otherwise from syntax.
		ITypeSymbol? typeArgument = method is not null
			? AnalyzerHelpers.GetSingleInvocationTypeArgumentOrNull(method)
			: GetTypeArgumentFromSyntax(context, memberAccess);

		if (typeArgument is null || typeArgument is ITypeParameterSymbol || AnalyzerHelpers.IsOpenGeneric(typeArgument))
		{
			return;
		}

		Location typeArgumentLocation = AnalyzerHelpers.GetTypeArgumentLocation(invocation, typeArgument) ??
		                                invocation.GetLocation();

		if (!IsMockable(typeArgument, context.Compilation.Assembly, out string? reason))
		{
			context.ReportDiagnostic(Diagnostic.Create(
				Rules.MockabilityRule,
				typeArgumentLocation,
				typeArgument.ToDisplayString(),
				reason));
			return;
		}

		if (typeArgument.TypeKind != TypeKind.Interface)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				Rules.MockabilityRule,
				typeArgumentLocation,
				typeArgument.ToDisplayString(),
				"You can only implement additional interfaces"));
		}
	}

	private static ITypeSymbol? GetReceiverType(SyntaxNodeAnalysisContext context,
		MemberAccessExpressionSyntax memberAccess,
		IMethodSymbol? method)
	{
		// For a static call on a type (e.g. IFoo.CreateMock()), the receiver is a type
		// expression. GetSymbolInfo returns the ITypeSymbol directly.
		SymbolInfo receiverInfo = context.SemanticModel.GetSymbolInfo(memberAccess.Expression);
		if (receiverInfo.Symbol is ITypeSymbol typeFromReceiver)
		{
			return typeFromReceiver;
		}

		// Fallback: derive from the C# 14 extension parameter on the resolved method.
		return method?.ContainingType.ExtensionParameter?.Type;
	}

	private static ITypeSymbol? GetTypeArgumentFromSyntax(SyntaxNodeAnalysisContext context,
		MemberAccessExpressionSyntax memberAccess)
	{
		if (memberAccess.Name is GenericNameSyntax { TypeArgumentList.Arguments: { Count: > 0, } args, })
		{
			return context.SemanticModel.GetTypeInfo(args[0]).Type;
		}

		return null;
	}

	private static bool IsInMockolateNamespace(ISymbol symbol)
		=> symbol.ContainingNamespace is { Name: "Mockolate", ContainingNamespace.IsGlobalNamespace: true, };

	private static bool IsMockable(ITypeSymbol typeSymbol, IAssemblySymbol sourceAssembly, out string? reason)
	{
		if (typeSymbol.TypeKind == TypeKind.Struct)
		{
			reason = "type is a struct";
			return false;
		}

		if (typeSymbol.TypeKind == TypeKind.Enum)
		{
			reason = "type is an enum";
			return false;
		}

		if (typeSymbol.IsRecord)
		{
			reason = "type is a record";
			return false;
		}

		if (typeSymbol.IsSealed && typeSymbol.TypeKind != TypeKind.Delegate)
		{
			reason = "type is sealed";
			return false;
		}

		if (typeSymbol.ContainingNamespace?.IsGlobalNamespace == true)
		{
			reason = "type is declared in the global namespace";
			return false;
		}

		if (typeSymbol.TypeKind != TypeKind.Interface &&
		    typeSymbol.TypeKind != TypeKind.Class &&
		    typeSymbol.TypeKind != TypeKind.Delegate)
		{
			reason = $"type kind '{typeSymbol.TypeKind}' is not supported";
			return false;
		}

		if (FindInaccessibleRequiredMember(typeSymbol, sourceAssembly) is { } inaccessible)
		{
			reason = inaccessible.InaccessibleType is { } inaccessibleType
				? $"the member '{inaccessible.Member.ToDisplayString()}' must be implemented, but its signature uses the type '{inaccessibleType.ToDisplayString()}', which is not accessible from this assembly"
				: $"the member '{inaccessible.Member.ToDisplayString()}' must be implemented, but it is not accessible from this assembly";
			return false;
		}

		reason = null;
		return true;
	}

	private static (ISymbol Member, ITypeSymbol? InaccessibleType)? FindInaccessibleRequiredMember(ITypeSymbol type,
		IAssemblySymbol sourceAssembly)
	{
		HashSet<string> filledSlots = new(StringComparer.Ordinal);

		foreach (ITypeSymbol implementedType in EnumerateImplementedTypes(type))
		{
			foreach (ISymbol member in implementedType.GetMembers())
			{
				foreach (ISymbol slot in EnumerateFilledSlots(member))
				{
					filledSlots.Add(GetSignatureKey(slot));
				}
			}
		}

		foreach (ITypeSymbol implementedType in EnumerateImplementedTypes(type))
		{
			foreach (ISymbol member in implementedType.GetMembers())
			{
				if (filledSlots.Contains(GetSignatureKey(member)))
				{
					continue;
				}

				if (FindInaccessibleMember(member, sourceAssembly) is { } inaccessibleMember)
				{
					return inaccessibleMember;
				}
			}
		}

		return null;
	}

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

	private static (ISymbol Member, ITypeSymbol? InaccessibleType)? FindInaccessibleMember(ISymbol member,
		IAssemblySymbol sourceAssembly)
	{
		switch (member)
		{
			case IMethodSymbol { MethodKind: MethodKind.Ordinary, IsAbstract: true, } method:
				return !IsAccessibleFrom(method, sourceAssembly)
					? (method, null)
					: Combine(method, FindInaccessibleSignatureType(method, sourceAssembly));
			case IPropertySymbol { IsAbstract: true, } property:
				return FindInaccessibleAccessor(property, sourceAssembly) is { } accessor
					? (accessor, null)
					: Combine(property, FindInaccessibleSignatureType(property, sourceAssembly));
			case IEventSymbol { IsAbstract: true, } @event:
				return !IsAccessibleFrom(@event, sourceAssembly)
					? (@event, null)
					: Combine(@event, FindInaccessibleSignatureType(@event, sourceAssembly));
			default:
				return null;
		}

		static (ISymbol Member, ITypeSymbol? InaccessibleType)? Combine(ISymbol member, ITypeSymbol? inaccessibleType)
			=> inaccessibleType is null ? null : (member, inaccessibleType);
	}

	/// <summary>
	///     The first type named in <paramref name="member" />'s signature that the mock cannot restate,
	///     or <see langword="null" /> when the whole signature is reachable. Mirrors
	///     <c>Helpers.HasAccessibleSignature</c> in the source generator; keep both in sync.
	/// </summary>
	private static ITypeSymbol? FindInaccessibleSignatureType(ISymbol member, IAssemblySymbol sourceAssembly)
	{
		switch (member)
		{
			case IMethodSymbol method:
				return FirstInaccessible([
					method.ReturnType,
					..method.Parameters.Select(parameter => parameter.Type),
					..method.TypeParameters.SelectMany(typeParameter => typeParameter.ConstraintTypes),
				]);
			case IPropertySymbol property:
				return FirstInaccessible([
					property.Type, ..property.Parameters.Select(parameter => parameter.Type),
				]);
			case IEventSymbol @event:
				return FirstInaccessible([@event.Type,]);
			default:
				return null;
		}

		ITypeSymbol? FirstInaccessible(IEnumerable<ITypeSymbol> types)
			=> types.FirstOrDefault(type => !IsTypeAccessibleFrom(type, sourceAssembly));
	}

	/// <summary>
	///     Mirror of <c>Helpers.IsAccessibleFrom</c>: every type in the containing chain and every
	///     composed type must be public, or internal/protected internal with access granted. The
	///     <see langword="protected" /> half never counts, because the surfaces naming the type do not
	///     derive from the mocked type.
	/// </summary>
	private static bool IsTypeAccessibleFrom(ITypeSymbol type, IAssemblySymbol sourceAssembly)
	{
		switch (type)
		{
			case IArrayTypeSymbol array:
				return IsTypeAccessibleFrom(array.ElementType, sourceAssembly);
			case IPointerTypeSymbol pointer:
				return IsTypeAccessibleFrom(pointer.PointedAtType, sourceAssembly);
			case INamedTypeSymbol named:
				for (INamedTypeSymbol? t = named; t is not null; t = t.ContainingType)
				{
					if (!IsDeclarationAccessible(t) ||
					    !t.TypeArguments.All(argument => IsTypeAccessibleFrom(argument, sourceAssembly)))
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
					SymbolEqualityComparer.Default.Equals(candidate.ContainingAssembly, sourceAssembly) ||
					candidate.ContainingAssembly?.GivesAccessTo(sourceAssembly) == true,
				_ => false,
			};
	}

	private static ISymbol? FindInaccessibleAccessor(IPropertySymbol property, IAssemblySymbol sourceAssembly)
	{
		if (property.GetMethod is { } getter && !IsAccessibleFrom(getter, sourceAssembly))
		{
			return getter;
		}

		if (property.SetMethod is { } setter && !IsAccessibleFrom(setter, sourceAssembly))
		{
			return setter;
		}

		return null;
	}

	/// <summary>
	///     Yields the types that declare the members a mock of <paramref name="type" /> is obliged to
	///     implement: the type itself, plus its base interfaces (for an interface) or its base class
	///     chain (for a class).
	/// </summary>
	/// <remarks>
	///     Deliberately narrower than <see cref="GetCandidateMembers" />, which also walks
	///     <see cref="ITypeSymbol.AllInterfaces" /> for classes. A class must already implement every
	///     interface member it inherits (CS0535), so those members are not the mock's obligation and
	///     folding them in here would reject types that mock perfectly well. The two walkers are not
	///     interchangeable; keep them separate.
	/// </remarks>
	private static IEnumerable<ITypeSymbol> EnumerateImplementedTypes(ITypeSymbol type)
	{
		yield return type;

		if (type.TypeKind == TypeKind.Interface)
		{
			foreach (INamedTypeSymbol @interface in type.AllInterfaces)
			{
				yield return @interface;
			}

			yield break;
		}

		for (ITypeSymbol? baseType = type.BaseType;
		     baseType is not null && baseType.SpecialType != SpecialType.System_Object;
		     baseType = baseType.BaseType)
		{
			yield return baseType;
		}
	}

	/// <summary>
	///     A member (or accessor) declared in another assembly is overridable only if the overriding
	///     assembly can actually see it. <c>internal</c> and <c>private protected</c> are invisible
	///     across assembly boundaries unless the declaring assembly grants InternalsVisibleTo.
	///     <c>protected internal</c> (= protected OR internal) is always reachable via the protected
	///     half from a derived class.
	/// </summary>
	/// <remarks>
	///     Must stay in sync with <c>Helpers.IsOverridableFrom</c> in
	///     <c>Source/Mockolate.SourceGenerators/Helpers.cs</c>, which gates the generator on the same
	///     rule. The two projects share no sources, so the logic is duplicated by necessity: if this
	///     check and the generator's disagree, the analyzer either reports a type the generator mocks
	///     fine or stays silent on one it refuses to emit.
	/// </remarks>
	private static bool IsAccessibleFrom(ISymbol member, IAssemblySymbol sourceAssembly)
	{
		if (member.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.ProtectedAndInternal))
		{
			return true;
		}

		IAssemblySymbol containingAssembly = member.ContainingAssembly;
		return SymbolEqualityComparer.Default.Equals(containingAssembly, sourceAssembly) ||
		       containingAssembly.GivesAccessTo(sourceAssembly);
	}
}
