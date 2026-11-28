using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mockolate.SourceGenerators.Entities;
using Mockolate.SourceGenerators.Internals;

namespace Mockolate.SourceGenerators.Tests;

public sealed class AutoMockDiscoveryCacheTests
{
	private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp14);

	private static readonly Dictionary<string, string> Files = new()
	{
		["Root.cs"] = """
		              namespace MyCode.Sdk
		              {
		                  public interface IRoot : IRootBase
		                  {
		                      IChild GetChild();
		                  }
		              }
		              """,
		["Base.cs"] = """
		              namespace MyCode.Sdk
		              {
		                  public interface IRootBase
		                  {
		                  }
		              }
		              """,
		["Child.cs"] = """
		               namespace MyCode
		               {
		                   public interface IChild
		                   {
		                       int GetValue(int count = Limits.Max);
		                   }
		               }
		               """,
		["Limits.cs"] = """
		                namespace MyCode
		                {
		                    public static class Limits
		                    {
		                        public const int Max = 5;
		                    }
		                }
		                """,
		["Tests.cs"] = """
		               using Mockolate;

		               namespace MyCode.Tests
		               {
		                   public class Tests
		                   {
		                       public void Run()
		                       {
		                           _ = MyCode.Sdk.IRoot.CreateMock();
		                       }
		                   }
		               }
		               """,
	};

	[Test]
	public async Task EditInTestMethod_ShouldGenerateTheSameAsAFreshRun()
	{
		(string[] before, string[] incremental, string[] fresh) = RunEdit("Tests.cs", Files["Tests.cs"]
			.Replace("_ = MyCode.Sdk.IRoot.CreateMock();", "_ = MyCode.Sdk.IRoot.CreateMock(); int x = 1;"));

		await That(fresh).IsEqualTo(before);
		await That(incremental).IsEqualTo(fresh);
	}

	[Test]
	public async Task ObsoleteDiscoveredInterface_ShouldGenerateTheSameAsAFreshRun()
	{
		(string[] before, string[] incremental, string[] fresh) = RunEdit("Child.cs", Files["Child.cs"]
			.Replace("public interface IChild", "[System.Obsolete] public interface IChild"));

		await That(fresh).IsNotEqualTo(before).Because("an obsolete interface is no longer auto-mocked");
		await That(incremental).IsEqualTo(fresh);
	}

	[Test]
	public async Task NewMemberInBaseInterface_ShouldGenerateTheSameAsAFreshRun()
	{
		(string[] before, string[] incremental, string[] fresh) = RunEdit("Base.cs", """
			namespace MyCode.Sdk
			{
			    public interface INew
			    {
			    }

			    public interface IRootBase
			    {
			        INew GetNew();
			    }
			}
			""");

		await That(fresh).IsNotEqualTo(before).Because("the new return type of the base interface is auto-mocked");
		await That(incremental).IsEqualTo(fresh);
	}

	[Test]
	public async Task ChangedConstantOfDefaultValue_ShouldGenerateTheSameAsAFreshRun()
	{
		(string[] before, string[] incremental, string[] fresh) = RunEdit("Limits.cs", Files["Limits.cs"]
			.Replace("Max = 5", "Max = 10"));

		await That(fresh).IsNotEqualTo(before).Because("the default value of the discovered interface changed");
		await That(incremental).IsEqualTo(fresh);
	}

	[Test]
	public async Task NewTypeThatShadowsADependency_ShouldGenerateTheSameAsAFreshRun()
	{
		(string[] before, string[] incremental, string[] fresh) = RunEdit("Shadow.cs", """
			namespace MyCode.Sdk
			{
			    public interface IChild
			    {
			        string Name { get; }
			    }
			}
			""");

		await That(fresh).IsNotEqualTo(before).Because("IRoot.GetChild now returns the new MyCode.Sdk.IChild");
		await That(incremental).IsEqualTo(fresh);
	}

	[Test]
	public async Task EditInTestMethod_ShouldReuseTheResult()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(Edit(compilation, "Tests.cs", Files["Tests.cs"]
			.Replace("_ = MyCode.Sdk.IRoot.CreateMock();", "_ = MyCode.Sdk.IRoot.CreateMock(); int x = 1;")), counter);

		await That(counter.Calls).IsEqualTo(1)
			.Because("the namespace MyCode of the edited file contains the dependencies, but it keeps existing");
	}

	[Test]
	public async Task NewFileInTheNamespaceOfADependency_ShouldReuseTheResult()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(Edit(compilation, "Other.cs", "namespace MyCode.Sdk { public class Unrelated { } }"), counter);

		await That(counter.Calls).IsEqualTo(1).Because("the namespace MyCode.Sdk already exists");
	}

	[Test]
	public async Task RemovedLastFileOfANamespaceWithTheNameOfADependency_ShouldDiscoverAgain()
	{
		Compilation compilation = CreateCompilation(new Dictionary<string, string>(Files)
		{
			["Other.cs"] = "namespace Other.Sdk { }",
		});
		Counter counter = new();
		Discover(compilation, counter);

		Discover(compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Single(tree => tree.FilePath == "Other.cs")),
			counter);

		await That(counter.Calls).IsEqualTo(2);
	}

	[Test]
	public async Task AddedUnrelatedFile_ShouldReuseTheResult()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(Edit(compilation, "Other.cs", "namespace Other { public class Unrelated { } }"), counter);

		await That(counter.Calls).IsEqualTo(1);
	}

	[Test]
	public async Task RemovedUnrelatedFile_ShouldReuseTheResult()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Single(tree => tree.FilePath == "Tests.cs")),
			counter);

		await That(counter.Calls).IsEqualTo(1);
	}

	[Test]
	public async Task EditOfADependency_ShouldDiscoverAgain()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(Edit(compilation, "Child.cs", "namespace MyCode { public interface IChild { } }"), counter);

		await That(counter.Calls).IsEqualTo(2);
	}

	[Test]
	public async Task RemovedFileDeclaringADependency_ShouldDiscoverAgain()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Single(tree => tree.FilePath == "Limits.cs")),
			counter);

		await That(counter.Calls).IsEqualTo(2);
	}

	[Test]
	public async Task NewNamespaceWithTheNameOfADependency_ShouldDiscoverAgain()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(Edit(compilation, "Other.cs", "namespace Other.Sdk { }"), counter);

		await That(counter.Calls).IsEqualTo(2)
			.Because("a namespace with the name of a namespace of a dependency can change the binding of a qualified name");
	}

	[Test]
	public async Task NewGlobalUsing_ShouldDiscoverAgain()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(Edit(compilation, "Usings.cs", "global using Other = System.String;"), counter);

		await That(counter.Calls).IsEqualTo(2);
	}

	[Test]
	public async Task RemovedGlobalUsing_ShouldDiscoverAgain()
	{
		Compilation compilation = CreateCompilation(new Dictionary<string, string>(Files)
		{
			["Usings.cs"] = "global using Other = System.String;",
		});
		Counter counter = new();
		Discover(compilation, counter);

		Discover(compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Single(tree => tree.FilePath == "Usings.cs")),
			counter);

		await That(counter.Calls).IsEqualTo(2);
	}

	[Test]
	public async Task NewAssemblyAttribute_ShouldDiscoverAgain()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(Edit(compilation, "Tests.cs",
			"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"Other\")]"), counter);

		await That(counter.Calls).IsEqualTo(2);
	}

	[Test]
	public async Task OtherSettings_ShouldDiscoverAgain()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(compilation, counter, "other settings");

		await That(counter.Calls).IsEqualTo(2);
	}

	[Test]
	public async Task OtherOptions_ShouldDiscoverAgain()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(compilation.WithOptions(compilation.Options.WithOptimizationLevel(OptimizationLevel.Release)), counter);

		await That(counter.Calls).IsEqualTo(2);
	}

	[Test]
	public async Task NewResolverWhoseEqualsThrows_ShouldReuseTheResult()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(WithNewResolver(compilation), counter);

		Discover(WithNewResolver(compilation), counter);

		await That(counter.Calls).IsEqualTo(1)
			.Because("the command-line compiler creates a new resolver for every compilation, whose Equals throws");

		static Compilation WithNewResolver(Compilation compilation)
			=> CSharpCompilation.Create(compilation.AssemblyName, compilation.SyntaxTrees, compilation.References,
				(CSharpCompilationOptions)compilation.Options.WithMetadataReferenceResolver(new ThrowingResolver()));
	}

	[Test]
	public async Task OtherReferences_ShouldDiscoverAgain()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(compilation.RemoveReferences(compilation.References.Last()), counter);

		await That(counter.Calls).IsEqualTo(2);
	}

	[Test]
	public async Task ReplacedReference_ShouldDiscoverAgain()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);
		MetadataReference last = compilation.References.Last();

		Discover(compilation.ReplaceReference(last, MetadataReference.CreateFromFile(last.Display!)), counter);

		await That(counter.Calls).IsEqualTo(2).Because("an equal but new reference may stand for a changed project");
	}

	[Test]
	public async Task FilesWithTheSamePath_ShouldNotBeCached()
	{
		Compilation compilation = CreateCompilation(Files)
			.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class A { }", ParseOptions, "Tests.cs"));
		Counter counter = new();
		Discover(compilation, counter);

		Discover(compilation, counter);

		await That(counter.Calls).IsEqualTo(2).Because("an edited file cannot be found by its path");
	}

	[Test]
	public async Task EditOfAFileWithTheSamePathAsAnother_ShouldDiscoverAgain()
	{
		Compilation compilation = CreateCompilation(Files);
		Counter counter = new();
		Discover(compilation, counter);

		Discover(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("class A { }", ParseOptions, "Tests.cs")),
			counter);

		await That(counter.Calls).IsEqualTo(2);
	}

	private static CSharpCompilation CreateCompilation(Dictionary<string, string> files, string? assemblyName = null)
		=> CSharpCompilation.Create(assemblyName ?? $"Cache{Guid.NewGuid():N}",
			files.Select(file => CSharpSyntaxTree.ParseText(file.Value, ParseOptions, file.Key)),
			Generator.GetReferences([]),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

	private static Compilation Edit(Compilation compilation, string path, string source)
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source, ParseOptions, path);
		SyntaxTree? existing = compilation.SyntaxTrees.SingleOrDefault(existing => existing.FilePath == path);
		return existing is null ? compilation.AddSyntaxTrees(tree) : compilation.ReplaceSyntaxTree(existing, tree);
	}

	private static void Discover(Compilation compilation, Counter counter, string settings = "settings")
		=> AutoMockDiscoveryCache.GetOrDiscover(compilation, settings, dependencies =>
		{
			counter.Calls++;
			dependencies.Add(compilation.GetTypeByMetadataName("MyCode.Sdk.IRoot")!, true, CancellationToken.None);
			dependencies.Add(compilation.GetTypeByMetadataName("MyCode.IChild")!, true, CancellationToken.None);
			return new EquatableArray<MockClass>([]);
		}, CancellationToken.None);

	/// <summary>
	///     Generates the <see cref="Files" />, then again incrementally after replacing or adding the file at
	///     <paramref name="path" />, and from scratch with new references, so that the cache cannot be reused.
	/// </summary>
	private static (string[] Before, string[] Incremental, string[] Fresh) RunEdit(string path, string source)
	{
		string assemblyName = $"Cache{Guid.NewGuid():N}";
		Compilation compilation = CreateCompilation(Files, assemblyName);
		GeneratorDriver driver = CreateDriver().RunGenerators(compilation);
		string[] before = GetSources(driver);

		string[] incremental = GetSources(driver.RunGenerators(Edit(compilation, path, source)));

		Dictionary<string, string> editedFiles = new(Files) { [path] = source, };
		string[] fresh = GetSources(CreateDriver().RunGenerators(CreateCompilation(editedFiles, assemblyName)));
		return (before, incremental, fresh);
	}

	private static CSharpGeneratorDriver CreateDriver()
		=> CSharpGeneratorDriver.Create([new MockGenerator().AsSourceGenerator(),], [], ParseOptions);

	private static string[] GetSources(GeneratorDriver driver)
		=> driver.GetRunResult().Results
			.SelectMany(result => result.GeneratedSources)
			.OrderBy(source => source.HintName, StringComparer.Ordinal)
			.Select(source => source.HintName + "\n" + source.SourceText)
			.ToArray();

	private sealed class Counter
	{
		public int Calls { get; set; }
	}

	private sealed class ThrowingResolver : MetadataReferenceResolver
	{
		public override bool Equals(object? other) => throw new NotImplementedException();

		public override int GetHashCode() => throw new NotImplementedException();

		public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference,
			string? baseFilePath, MetadataReferenceProperties properties)
			=> throw new NotImplementedException();
	}
}
