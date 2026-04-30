using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Tests.Entities;

public class MockClassEqualityTests
{
	[Test]
	public async Task ReferenceEqualMockClasses_ShouldBeEqual()
	{
		(Compilation compilation, INamedTypeSymbol[] symbols) = ParseSymbols(Source, "IBase");

		MockClass a = new([symbols[0],], compilation.Assembly);

		await That(a.Equals(a)).IsTrue();
		await That(a.Equals(null)).IsFalse();
	}

	[Test]
	public async Task TwoMockClassesWithDifferentConstructors_ShouldNotBeEqual()
	{
		(Compilation compilation, INamedTypeSymbol[] symbols) = ParseSymbols(Source,
			"BaseWithCtor", "BaseWithIntCtor");

		MockClass a = new([symbols[0],], compilation.Assembly);
		MockClass b = new([symbols[1],], compilation.Assembly);

		await That(a.Equals(b)).IsFalse();
	}

	[Test]
	public async Task TwoMockClassesWithSameRootAndSameAdditionals_ShouldBeEqual()
	{
		(Compilation compilation, INamedTypeSymbol[] symbols) = ParseSymbols(Source,
			"IBase", "IExtraA");

		MockClass a = new([symbols[0], symbols[1],], compilation.Assembly);
		MockClass b = new([symbols[0], symbols[1],], compilation.Assembly);

		await That(a.Equals(b)).IsTrue();
		await That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
	}

	[Test]
	public async Task TwoMockClassesWithSameRootButDifferentAdditionals_ShouldNotBeEqual()
	{
		(Compilation compilation, INamedTypeSymbol[] symbols) = ParseSymbols(Source,
			"IBase", "IExtraA", "IExtraB");

		MockClass a = new([symbols[0], symbols[1],], compilation.Assembly);
		MockClass b = new([symbols[0], symbols[2],], compilation.Assembly);

		await That(a.Equals(b)).IsFalse();
		await That(a.Equals((object?)b)).IsFalse();
	}

	private const string Source = """
	                              public interface IBase { }
	                              public interface IExtraA { }
	                              public interface IExtraB { }
	                              public class BaseWithCtor
	                              {
	                                  public BaseWithCtor() { }
	                              }
	                              public class BaseWithIntCtor
	                              {
	                                  public BaseWithIntCtor(int v) { }
	                              }
	                              """;

	private static (Compilation Compilation, INamedTypeSymbol[] Symbols) ParseSymbols(string source,
		params string[] names)
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CSharpCompilation.Create(
			"TestAssembly",
			[tree,],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location),],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		SemanticModel model = compilation.GetSemanticModel(tree);
		INamedTypeSymbol[] symbols = names.Select(name =>
		{
			BaseTypeDeclarationSyntax declaration = tree.GetRoot().DescendantNodes()
				.OfType<BaseTypeDeclarationSyntax>()
				.First(t => t.Identifier.Text == name);
			return model.GetDeclaredSymbol(declaration)!;
		}).ToArray();
		return (compilation, symbols);
	}
}
