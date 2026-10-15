using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Tests.Entities;

public class MethodEqualityComparerTests
{
	[Test]
	public async Task WhenBothMethodsAreNull_ShouldReturnTrue()
	{
		IEqualityComparer<Method> comparer = Method.ContainingTypeIndependentEqualityComparer;

		bool result = comparer.Equals(null, null);

		await That(result).IsTrue();
	}

	[Test]
	public async Task WhenLeftMethodIsNull_ShouldReturnFalse()
	{
		IEqualityComparer<Method> comparer = Method.ContainingTypeIndependentEqualityComparer;
		Method right = CreateMethod("public class C { public void Foo() {} }", "Foo");

		bool result = comparer.Equals(null, right);

		await That(result).IsFalse();
	}

	[Test]
	public async Task WhenMethodsHaveDifferentNames_ShouldReturnFalse()
	{
		IEqualityComparer<Method> comparer = Method.ContainingTypeIndependentEqualityComparer;
		Method left = CreateMethod("public class C { public void Foo() {} }", "Foo");
		Method right = CreateMethod("public class C { public void Bar() {} }", "Bar");

		bool result = comparer.Equals(left, right);

		await That(result).IsFalse();
	}

	[Test]
	public async Task WhenMethodsHaveDifferentParameterCount_ShouldReturnFalse()
	{
		IEqualityComparer<Method> comparer = Method.ContainingTypeIndependentEqualityComparer;
		Method left = CreateMethod("public class C { public void Foo() {} }", "Foo");
		Method right = CreateMethod("public class C { public void Foo(int x) {} }", "Foo");

		bool result = comparer.Equals(left, right);

		await That(result).IsFalse();
	}

	[Test]
	public async Task WhenRightMethodIsNull_ShouldReturnFalse()
	{
		IEqualityComparer<Method> comparer = Method.ContainingTypeIndependentEqualityComparer;
		Method left = CreateMethod("public class C { public void Foo() {} }", "Foo");

		bool result = comparer.Equals(left, null);

		await That(result).IsFalse();
	}

	private static Method CreateMethod(string source, string methodName)
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CSharpCompilation.Create(
			"TestAssembly",
			[tree,],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location),],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		SemanticModel model = compilation.GetSemanticModel(tree);
		MethodDeclarationSyntax declaration = tree.GetRoot().DescendantNodes()
			.OfType<MethodDeclarationSyntax>()
			.First(m => m.Identifier.Text == methodName);
		IMethodSymbol symbol = model.GetDeclaredSymbol(declaration)!;
		return new Method(symbol, null);
	}
}
