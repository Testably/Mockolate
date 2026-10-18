using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Tests.Entities;

public class PropertyEqualityComparerTests
{
	[Test]
	public async Task BothNull_ShouldReturnTrue()
	{
		await That(Property.EqualityComparer.Equals(null, null)).IsTrue();
		await That(Property.ContainingTypeIndependentEqualityComparer.Equals(null, null)).IsTrue();
	}

	[Test]
	public async Task ContainingTypeIndependent_IndexersWithDifferentArity_ShouldReturnFalse()
	{
		IEqualityComparer<Property> comparer = Property.ContainingTypeIndependentEqualityComparer;
		Property arity1 = CreateIndexer("public class C { public int this[int i] => 0; }");
		Property arity2 = CreateIndexer("public class D { public int this[int i, int j] => 0; }");

		await That(comparer.Equals(arity1, arity2)).IsFalse();
	}

	[Test]
	public async Task ContainingTypeIndependent_IndexerVsNonIndexer_ShouldReturnFalse()
	{
		IEqualityComparer<Property> comparer = Property.ContainingTypeIndependentEqualityComparer;
		Property indexer = CreateIndexer("public class C { public int this[int i] => 0; }");
		Property nonIndexer = CreateProperty("public class C { public int Foo => 0; }", "Foo");

		await That(comparer.Equals(indexer, nonIndexer)).IsFalse();
		await That(comparer.Equals(nonIndexer, indexer)).IsFalse();
	}

	[Test]
	public async Task IndexersWithDifferentArity_ShouldReturnFalse()
	{
		IEqualityComparer<Property> comparer = Property.EqualityComparer;
		Property arity1 = CreateIndexer("public class C { public int this[int i] => 0; }");
		Property arity2 = CreateIndexer("public class C { public int this[int i, int j] => 0; }");

		await That(comparer.Equals(arity1, arity2)).IsFalse();
	}

	[Test]
	public async Task IndexersWithDifferentContainingType_ShouldReturnFalse()
	{
		IEqualityComparer<Property> comparer = Property.EqualityComparer;
		Property fromC = CreateIndexer("public class C { public int this[int i] => 0; }");
		Property fromD = CreateIndexer("public class D { public int this[int i] => 0; }");

		await That(comparer.Equals(fromC, fromD)).IsFalse();
	}

	[Test]
	public async Task IndexerVsNonIndexer_ShouldReturnFalse()
	{
		IEqualityComparer<Property> comparer = Property.EqualityComparer;
		Property indexer = CreateIndexer("public class C { public int this[int i] => 0; }");
		Property nonIndexer = CreateProperty("public class C { public int Foo => 0; }", "Foo");

		await That(comparer.Equals(indexer, nonIndexer)).IsFalse();
		await That(comparer.Equals(nonIndexer, indexer)).IsFalse();
	}

	[Test]
	public async Task NonIndexersWithDifferentContainingType_ShouldReturnFalse()
	{
		IEqualityComparer<Property> comparer = Property.EqualityComparer;
		Property fromC = CreateProperty("public class C { public int Foo => 0; }", "Foo");
		Property fromD = CreateProperty("public class D { public int Foo => 0; }", "Foo");

		await That(comparer.Equals(fromC, fromD)).IsFalse();
	}

	private static Property CreateProperty(string source, string propertyName)
		=> new(ParsePropertySymbol(source, propertyName, false), null);

	private static Property CreateIndexer(string source)
		=> new(ParsePropertySymbol(source, null, true), null);

	private static IPropertySymbol ParsePropertySymbol(string source, string? propertyName, bool isIndexer)
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CSharpCompilation.Create(
			"TestAssembly",
			[tree,],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location),],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		SemanticModel model = compilation.GetSemanticModel(tree);
		if (isIndexer)
		{
			IndexerDeclarationSyntax declaration = tree.GetRoot().DescendantNodes()
				.OfType<IndexerDeclarationSyntax>()
				.First();
			return model.GetDeclaredSymbol(declaration)!;
		}

		PropertyDeclarationSyntax propertyDeclaration = tree.GetRoot().DescendantNodes()
			.OfType<PropertyDeclarationSyntax>()
			.First(p => p.Identifier.Text == propertyName);
		return model.GetDeclaredSymbol(propertyDeclaration)!;
	}
}
