using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Type = Mockolate.SourceGenerators.Entities.Type;

namespace Mockolate.SourceGenerators.Tests.Entities;

public class TypeIsFormattableTests
{
	[Fact]
	public async Task WhenSymbolIsObject_ShouldNotReportFormattable()
	{
		const string source = "public class Holder { public object Value; }";
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CSharpCompilation.Create(
			"TestAssembly",
			[tree,],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location),],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		SemanticModel model = compilation.GetSemanticModel(tree);
		FieldDeclarationSyntax declaration = tree.GetRoot().DescendantNodes()
			.OfType<FieldDeclarationSyntax>()
			.First();
		VariableDeclaratorSyntax variable = declaration.Declaration.Variables.Single();
		IFieldSymbol fieldSymbol = (IFieldSymbol)model.GetDeclaredSymbol(variable)!;

		Type type = Type.From(fieldSymbol.Type);

		await That(type.IsFormattable).IsFalse();
	}

	[Fact]
	public async Task WhenSymbolIsSystemIFormattableItself_ShouldReportFormattable()
	{
		// The IsFormattable self-check (Type.cs:87-94) handles the case where the symbol IS
		// System.IFormattable - AllInterfaces excludes the type itself, so the explicit
		// Containing-Namespace check is the only path that catches this.
		const string source = """
		                      using System;
		                      public class Holder
		                      {
		                          public IFormattable Value;
		                      }
		                      """;
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CSharpCompilation.Create(
			"TestAssembly",
			[tree,],
			[
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(IFormattable).Assembly.Location),
			],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		SemanticModel model = compilation.GetSemanticModel(tree);
		FieldDeclarationSyntax declaration = tree.GetRoot().DescendantNodes()
			.OfType<FieldDeclarationSyntax>()
			.First();
		VariableDeclaratorSyntax variable = declaration.Declaration.Variables.Single();
		IFieldSymbol fieldSymbol = (IFieldSymbol)model.GetDeclaredSymbol(variable)!;
		ITypeSymbol typeSymbol = fieldSymbol.Type;

		Type type = Type.From(typeSymbol);

		await That(type.IsFormattable).IsTrue();
	}
}
