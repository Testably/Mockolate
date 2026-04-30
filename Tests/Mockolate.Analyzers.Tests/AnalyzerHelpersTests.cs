using System.Linq;
using System.Threading.Tasks;
using aweXpect;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static aweXpect.Expect;

namespace Mockolate.Analyzers.Tests;

public class AnalyzerHelpersTests
{
	[Test]
	public async Task WhenInvocationHasGenericNameSyntax_ShouldReturnTypeArgumentLocation()
	{
		const string source = """
		                      public static class S
		                      {
		                      	public static T Make<T>() => default!;
		                      }

		                      public class C
		                      {
		                      	public void Foo() { S.Make<int>(); }
		                      }
		                      """;
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CreateCompilation(tree);
		SemanticModel model = compilation.GetSemanticModel(tree);
		InvocationExpressionSyntax invocation = tree.GetRoot().DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Single(i => i.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax, });
		IMethodSymbol method = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;
		ITypeSymbol typeArgument = method.TypeArguments[0];

		Location? result = AnalyzerHelpers.GetTypeArgumentLocation(invocation, typeArgument);

		await That(result).IsNotNull();
	}

	[Test]
	public async Task WhenInvokedMethodIsGeneric_ShouldReturnFirstTypeArgument()
	{
		const string source = """
		                      public class C
		                      {
		                      	public T Foo<T>() => default!;
		                      	public void Bar() { Foo<int>(); }
		                      }
		                      """;
		IMethodSymbol method = GetInvokedMethod(source, "Foo");

		ITypeSymbol? result = AnalyzerHelpers.GetSingleInvocationTypeArgumentOrNull(method);

		await That(result).IsNotNull();
		await That(result!.SpecialType).IsEqualTo(SpecialType.System_Int32);
	}

	[Test]
	public async Task WhenInvokedMethodIsNotGeneric_ShouldNotReturnAnyTypeArgument()
	{
		const string source = """
		                      public class C
		                      {
		                      	public void Foo() { }
		                      	public void Bar() { Foo(); }
		                      }
		                      """;
		IMethodSymbol method = GetInvokedMethod(source, "Foo");

		ITypeSymbol? result = AnalyzerHelpers.GetSingleInvocationTypeArgumentOrNull(method);

		await That(result).IsNull();
	}

	[Test]
	public async Task WhenSyntaxIsNotInvocationExpression_ShouldNotReturnAnyLocation()
	{
		const string source = """
		                      public class C
		                      {
		                      	public int Foo() => 0;
		                      }
		                      """;
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CreateCompilation(tree);
		SemanticModel model = compilation.GetSemanticModel(tree);
		MethodDeclarationSyntax declaration = tree.GetRoot().DescendantNodes()
			.OfType<MethodDeclarationSyntax>()
			.Single();
		IMethodSymbol symbol = (IMethodSymbol)model.GetDeclaredSymbol(declaration)!;

		Location? result = AnalyzerHelpers.GetTypeArgumentLocation(declaration, symbol.ReturnType);

		await That(result).IsNull();
	}

	private static IMethodSymbol GetInvokedMethod(string source, string methodName)
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CreateCompilation(tree);
		SemanticModel model = compilation.GetSemanticModel(tree);
		InvocationExpressionSyntax invocation = tree.GetRoot().DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Single(i => InvocationName(i) == methodName);
		return (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;
	}

	private static string? InvocationName(InvocationExpressionSyntax invocation) => invocation.Expression switch
	{
		IdentifierNameSyntax id => id.Identifier.Text,
		GenericNameSyntax generic => generic.Identifier.Text,
		MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
		_ => null,
	};

	private static CSharpCompilation CreateCompilation(SyntaxTree tree) => CSharpCompilation.Create(
		"TestAssembly",
		[tree,],
		[MetadataReference.CreateFromFile(typeof(object).Assembly.Location),],
		new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
