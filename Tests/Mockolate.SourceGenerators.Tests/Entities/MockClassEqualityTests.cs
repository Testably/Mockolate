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

		MockClass a = new([symbols[0],], compilation);

		await That(a.Equals(a)).IsTrue();
		await That(a.Equals(null)).IsFalse();
	}

	[Test]
	public async Task TwoMockClassesWithDifferentConstructors_ShouldNotBeEqual()
	{
		(Compilation compilation, INamedTypeSymbol[] symbols) = ParseSymbols(Source,
			"BaseWithCtor", "BaseWithIntCtor");

		MockClass a = new([symbols[0],], compilation);
		MockClass b = new([symbols[1],], compilation);

		await That(a.Equals(b)).IsFalse();
	}

	[Test]
	public async Task TwoMockClassesWithDifferentConstructorsWithoutArguments_ShouldNotBeEqual()
	{
		(Compilation unrelatedCompilation, INamedTypeSymbol[] unrelated) = ParseSymbols(
			"public class A { } public class B { }" + ParamsConstructorsSource, "ParamsBase");
		(Compilation derivedCompilation, INamedTypeSymbol[] derived) = ParseSymbols(
			"public class A : B { } public class B { }" + ParamsConstructorsSource, "ParamsBase");

		MockClass a = new([unrelated[0],], unrelatedCompilation);
		MockClass b = new([derived[0],], derivedCompilation);

		await That(a.ConstructorWithoutArguments).IsEqualTo(0)
			.Because("C# reports new ParamsBase() as ambiguous, so the first declared constructor is used");
		await That(b.ConstructorWithoutArguments).IsEqualTo(1)
			.Because("the implicit conversion from A[] to B[] makes the params A[] constructor better");
		await That(a.Equals(b)).IsFalse();
	}

	[Test]
	public async Task TwoMockClassesWithSameRootAndSameAdditionals_ShouldBeEqual()
	{
		(Compilation compilation, INamedTypeSymbol[] symbols) = ParseSymbols(Source,
			"IBase", "IExtraA");

		MockClass a = new([symbols[0], symbols[1],], compilation);
		MockClass b = new([symbols[0], symbols[1],], compilation);

		await That(a.Equals(b)).IsTrue();
		await That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
	}

	[Test]
	public async Task TwoMockClassesWithSameRootButDifferentAdditionals_ShouldNotBeEqual()
	{
		(Compilation compilation, INamedTypeSymbol[] symbols) = ParseSymbols(Source,
			"IBase", "IExtraA", "IExtraB");

		MockClass a = new([symbols[0], symbols[1],], compilation);
		MockClass b = new([symbols[0], symbols[2],], compilation);

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

	private const string ParamsConstructorsSource = """
	                                                public abstract class ParamsBase
	                                                {
	                                                    protected ParamsBase(params B[] values) { }
	                                                    protected ParamsBase(params A[] values) { }
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
