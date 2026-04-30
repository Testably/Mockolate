using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Event = Mockolate.SourceGenerators.Entities.Event;

namespace Mockolate.SourceGenerators.Tests.Entities;

public class EventEqualityComparerTests
{
	[Test]
	public async Task BothNull_ShouldReturnTrue()
	{
		await That(Event.EqualityComparer.Equals(null, null)).IsTrue();
		await That(Event.ContainingTypeIndependentEqualityComparer.Equals(null, null)).IsTrue();
	}

	[Test]
	public async Task ContainingTypeIndependent_DifferentNames_ShouldReturnFalse()
	{
		IEqualityComparer<Event> comparer = Event.ContainingTypeIndependentEqualityComparer;
		Event e1 = CreateEvent(
			"using System; public class C { public event Action Foo; }",
			"Foo");
		Event e2 = CreateEvent(
			"using System; public class D { public event Action Bar; }",
			"Bar");

		await That(comparer.Equals(e1, e2)).IsFalse();
	}

	[Test]
	public async Task ContainingTypeIndependent_SameNameDifferentContainingType_ShouldReturnTrue()
	{
		IEqualityComparer<Event> comparer = Event.ContainingTypeIndependentEqualityComparer;
		Event fromC = CreateEvent(
			"using System; public class C { public event Action Foo; }",
			"Foo");
		Event fromD = CreateEvent(
			"using System; public class D { public event Action Foo; }",
			"Foo");

		await That(comparer.Equals(fromC, fromD)).IsTrue();
	}

	[Test]
	public async Task EventsWithDifferentContainingType_ShouldReturnFalse()
	{
		IEqualityComparer<Event> comparer = Event.EqualityComparer;
		Event fromC = CreateEvent(
			"using System; public class C { public event Action Foo; }",
			"Foo");
		Event fromD = CreateEvent(
			"using System; public class D { public event Action Foo; }",
			"Foo");

		await That(comparer.Equals(fromC, fromD)).IsFalse();
	}

	[Test]
	public async Task EventsWithDifferentNames_ShouldReturnFalse()
	{
		IEqualityComparer<Event> comparer = Event.EqualityComparer;
		Event e1 = CreateEvent(
			"using System; public class C { public event Action Foo; }",
			"Foo");
		Event e2 = CreateEvent(
			"using System; public class C { public event Action Bar; }",
			"Bar");

		await That(comparer.Equals(e1, e2)).IsFalse();
	}

	[Test]
	public async Task LeftOrRightNull_ShouldReturnFalse()
	{
		IEqualityComparer<Event> comparer = Event.EqualityComparer;
		Event e = CreateEvent(
			"using System; public class C { public event Action Foo; }",
			"Foo");

		await That(comparer.Equals(null, e)).IsFalse();
		await That(comparer.Equals(e, null)).IsFalse();
	}

	private static Event CreateEvent(string source, string eventName)
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CSharpCompilation.Create(
			"TestAssembly",
			[tree,],
			[
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(Action).Assembly.Location),
			],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		SemanticModel model = compilation.GetSemanticModel(tree);
		EventFieldDeclarationSyntax declaration = tree.GetRoot().DescendantNodes()
			.OfType<EventFieldDeclarationSyntax>()
			.First(d => d.Declaration.Variables.Any(v => v.Identifier.Text == eventName));
		VariableDeclaratorSyntax variable = declaration.Declaration.Variables
			.First(v => v.Identifier.Text == eventName);
		IEventSymbol symbol = (IEventSymbol)model.GetDeclaredSymbol(variable)!;
		IMethodSymbol invoke = ((INamedTypeSymbol)symbol.Type).DelegateInvokeMethod!;
		return new Event(symbol, invoke, null);
	}
}
