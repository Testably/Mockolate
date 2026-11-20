using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Internals;

/// <summary>
///     The simple names that the discovery of auto-mocks depends on, so that <see cref="AutoMockDiscoveryCache" />
///     knows which edits can change its result.
/// </summary>
/// <remarks>
///     An edit to a file that declares one of the names invalidates the result, so a type only adds what its
///     declaration reads from other files: its name, the names of its containing types and namespaces (a new type or
///     namespace can hijack the binding of a qualified name), its type arguments and base types and, when declared in
///     source, every identifier of its declaration (a new type can shadow a name it uses, and a constant of another
///     type can be one of its default values). Only a type whose members are inspected also adds the types its
///     members use. A type from a reference can only change with the references.
/// </remarks>
internal sealed class AutoMockDependencies
{
	private readonly HashSet<ISymbol> _declarations = new(SymbolEqualityComparer.Default);
	private readonly HashSet<ISymbol> _members = new(SymbolEqualityComparer.Default);

	public HashSet<string> Names { get; } = new(StringComparer.Ordinal);

	/// <summary>
	///     Adds the <paramref name="type" /> and, with <paramref name="withMembers" />, the types its members use.
	/// </summary>
	public void Add(ITypeSymbol type, bool withMembers, CancellationToken cancellationToken)
	{
		AddDeclaration(type, cancellationToken);
		if (withMembers && type is INamedTypeSymbol named && _members.Add(named))
		{
			foreach (ITypeSymbol used in GetMemberTypes(named))
			{
				AddDeclaration(used, cancellationToken);
			}
		}
	}

	private void AddDeclaration(ITypeSymbol type, CancellationToken cancellationToken)
	{
		switch (type)
		{
			case IArrayTypeSymbol array:
				AddDeclaration(array.ElementType, cancellationToken);
				break;
			case IPointerTypeSymbol pointer:
				AddDeclaration(pointer.PointedAtType, cancellationToken);
				break;
			case INamedTypeSymbol named when _declarations.Add(named):
				AddNames(named);
				foreach (ITypeSymbol typeArgument in named.TypeArguments)
				{
					AddDeclaration(typeArgument, cancellationToken);
				}

				foreach (SyntaxReference reference in named.OriginalDefinition.DeclaringSyntaxReferences)
				{
					foreach (SyntaxToken token in reference.GetSyntax(cancellationToken).DescendantTokens())
					{
						if (token.IsKind(SyntaxKind.IdentifierToken))
						{
							Names.Add(token.ValueText);
						}
					}
				}

				if (named.BaseType is not null)
				{
					AddDeclaration(named.BaseType, cancellationToken);
				}

				foreach (INamedTypeSymbol @interface in named.Interfaces)
				{
					AddDeclaration(@interface, cancellationToken);
				}

				break;
		}
	}

	private void AddNames(INamedTypeSymbol type)
	{
		for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
		{
			Names.Add(current.Name);
		}

		for (INamespaceSymbol? @namespace = type.ContainingNamespace;
		     @namespace is { IsGlobalNamespace: false, };
		     @namespace = @namespace.ContainingNamespace)
		{
			Names.Add(@namespace.Name);
		}
	}

	private static IEnumerable<ITypeSymbol> GetMemberTypes(INamedTypeSymbol type)
	{
		foreach (ITypeSymbol constraint in type.TypeParameters.SelectMany(parameter => parameter.ConstraintTypes))
		{
			yield return constraint;
		}

		foreach (INamedTypeSymbol attributeClass in GetAttributeClasses(type))
		{
			yield return attributeClass;
		}

		IEnumerable<ITypeSymbol> types = [type, ..type.AllInterfaces,];
		for (INamedTypeSymbol? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
		{
			types = types.Append(baseType);
		}

		foreach (ISymbol member in types.SelectMany(t => t.GetMembers()))
		{
			foreach (INamedTypeSymbol attributeClass in GetAttributeClasses(member))
			{
				yield return attributeClass;
			}

			switch (member)
			{
				case IMethodSymbol method:
					yield return method.ReturnType;
					foreach (IParameterSymbol parameter in method.Parameters)
					{
						yield return parameter.Type;
						foreach (INamedTypeSymbol attributeClass in GetAttributeClasses(parameter))
						{
							yield return attributeClass;
						}
					}

					foreach (ITypeSymbol constraint in method.TypeParameters.SelectMany(parameter => parameter.ConstraintTypes))
					{
						yield return constraint;
					}

					break;
				case IPropertySymbol property:
					yield return property.Type;
					foreach (IParameterSymbol parameter in property.Parameters)
					{
						yield return parameter.Type;
					}

					break;
				case IEventSymbol @event:
					yield return @event.Type;
					break;
				case IFieldSymbol field:
					yield return field.Type;
					break;
			}
		}
	}

	private static IEnumerable<INamedTypeSymbol> GetAttributeClasses(ISymbol symbol)
		=> symbol.GetAttributes()
			.Select(attribute => attribute.AttributeClass)
			.Where(attributeClass => attributeClass is not null)!;
}
