using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mockolate.SourceGenerators.Entities;
using Mockolate.SourceGenerators.Tests.TestHelpers;

namespace Mockolate.SourceGenerators.Tests;

public sealed class MockDataSourcesTests
{
	private const string DataAttribute = """
	                                     namespace MyCode
	                                     {
	                                         [System.AttributeUsage(System.AttributeTargets.All)]
	                                         public sealed class DataAttribute : System.Attribute, Mockolate.IMockDataSource;
	                                     }
	                                     """;

	[Test]
	public async Task CreateWithMocksWithoutDataSource_ShouldNotRegisterAFactory()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       #pragma warning disable CS9113
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public interface IDep { int Get(); }
		                                           public class Svc(IDep dep);

		                                           public class Program
		                                           {
		                                               public static void Main(string[] args)
		                                               {
		                                                   _ = Svc.CreateWithMocks(out _);
		                                               }
		                                           }
		                                       }
		                                       """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("CreateWithMocks.g.cs").And.DoesNotContainKey("MockDataSources.g.cs");
	}

	[Test]
	public async Task DataSourceOnMethodConstructorAndType_ShouldCreateTheirParameters()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         #pragma warning disable CS9113
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public interface IForMethod { int Get(); }
		                                             public interface IForConstructor { int Get(); }
		                                             public interface IForPrimaryConstructor { int Get(); }
		                                             public interface IForRecord { int Get(); }

		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public Tests(IForConstructor dependency) { }

		                                                 [Data]
		                                                 public void Test(IForMethod dependency) { }
		                                             }

		                                             [Data]
		                                             public class ClassTests(IForPrimaryConstructor dependency);

		                                             [Data]
		                                             public record RecordTests(IForRecord Dependency);

		                                             public class Program
		                                             {
		                                                 public static void Main(string[] args)
		                                                 {
		                                                 }
		                                             }
		                                         }

		                                         {{DataAttribute}}
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("type == typeof(global::MyCode.IForMethod)").And
			.Contains("type == typeof(global::MyCode.IForConstructor)").And
			.Contains("type == typeof(global::MyCode.IForPrimaryConstructor)").And
			.Contains("type == typeof(global::MyCode.IForRecord)").And
			.DoesNotContain("typeof(global::MyCode.RecordTests)");
	}

	[Test]
	public async Task DataSourceAttributeOfReferencedAssembly_ShouldBeFoundInEverySpelling()
	{
		MetadataReference reference = ExternalAssembly.Compile("""
		                                                       using System;

		                                                       namespace External
		                                                       {
		                                                           [AttributeUsage(AttributeTargets.Method)]
		                                                           public sealed class AutoDataAttribute : Attribute, Mockolate.IMockDataSource;
		                                                       }
		                                                       """);
		GeneratorResult result = Generator.RunWithReferences("""
		                                                     #nullable enable
		                                                     using External;
		                                                     using Alias = External.AutoDataAttribute;
		                                                     using SuffixAliasAttribute = External.AutoDataAttribute;

		                                                     namespace MyCode
		                                                     {
		                                                         public class Qualified;
		                                                         public class WithSuffix;
		                                                         public class AliasQualified;
		                                                         public class Aliased;
		                                                         public class SuffixAliased;

		                                                         public class Program
		                                                         {
		                                                             [External.AutoData]
		                                                             public static object A(Qualified sut) => sut;

		                                                             [AutoDataAttribute]
		                                                             public static object B(WithSuffix sut) => sut;

		                                                             [global::External.AutoData]
		                                                             public static object C(AliasQualified sut) => sut;

		                                                             [Alias]
		                                                             public static object D(Aliased sut) => sut;

		                                                             [SuffixAlias]
		                                                             public static object E(SuffixAliased sut) => sut;

		                                                             public static void Main(string[] args)
		                                                             {
		                                                             }
		                                                         }
		                                                     }
		                                                     """, [reference,]);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("typeof(global::MyCode.Qualified)").And
			.Contains("typeof(global::MyCode.WithSuffix)").And
			.Contains("typeof(global::MyCode.AliasQualified)").And
			.Contains("typeof(global::MyCode.Aliased)").And
			.Contains("typeof(global::MyCode.SuffixAliased)");
	}

	[Test]
	public async Task AttributeWithTheNameOfADataSourceAttribute_ShouldNotBeADataSource()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       #pragma warning disable CS9113
		                                       using System;
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public class Svc;

		                                           public class Program
		                                           {
		                                               [Other.Data]
		                                               public static object Test(Svc sut) => sut;

		                                               public static void Main(string[] args)
		                                               {
		                                               }
		                                           }
		                                       }

		                                       namespace DataSources
		                                       {
		                                           [AttributeUsage(AttributeTargets.Method)]
		                                           public sealed class DataAttribute : Attribute, IMockDataSource;
		                                       }

		                                       namespace Other
		                                       {
		                                           [AttributeUsage(AttributeTargets.Method)]
		                                           public sealed class DataAttribute : Attribute;
		                                       }
		                                       """);

		await That(result.Sources).DoesNotContainKey("MockDataSources.g.cs");
	}

	[Test]
	public async Task ParametersThatCannotBeCreated_ShouldBeSkipped()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         #pragma warning disable CS9113
		                                         using System.Collections.Generic;
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public interface IGen<T> { T Get(); }
		                                             public interface IDep { int Get(); }
		                                             public abstract record AbstractRecord;
		                                             public class Generic<T>;
		                                             public class Ambiguous { public Ambiguous(int a) { } public Ambiguous(string b) { } }

		                                             public class Tests<TClass>
		                                             {
		                                                 [Data]
		                                                 private void Test<TMethod>(IGen<TMethod> a, IGen<TClass> b, TMethod c,
		                                                     AbstractRecord d, Generic<int> e, Ambiguous f, Hidden g,
		                                                     System.Type h, List<int> i, int j, string k, IDep dep) { }

		                                                 private interface Hidden { }
		                                             }

		                                             public class Program
		                                             {
		                                                 public static void Main(string[] args)
		                                                 {
		                                                 }
		                                             }
		                                         }

		                                         {{DataAttribute}}
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("value = global::MyCode.IDep.CreateMock(").And
			.Contains("out global::MyCode.AbstractRecord createdValue").And
			.Contains("out global::MyCode.Generic<int> createdValue").And
			.Contains("out global::MyCode.Ambiguous createdValue").And
			.Contains("out global::System.Type createdValue").And
			.Contains("out global::System.Collections.Generic.List<int> createdValue").And
			.Contains("out int createdValue").And
			.Contains("out string createdValue").And
			.DoesNotContain("IGen").And
			.DoesNotContain("Hidden");
	}

	[Test]
	public async Task FrameworkParameters_ShouldGetAValueAndInterfacesOtherwiseAMock()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         #pragma warning disable CS9113
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(System.IServiceProvider a, System.IO.Stream b,
		                                                     System.Func<int> c, System.Text.StringBuilder d) { }
		                                             }

		                                             public class Program
		                                             {
		                                                 public static void Main(string[] args)
		                                                 {
		                                                 }
		                                             }
		                                         }

		                                         {{DataAttribute}}
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("out global::System.IServiceProvider createdValue) ? createdValue : global::System.IServiceProvider.CreateMock(").And
			.Contains("out global::System.IO.Stream createdValue);").And
			.Contains("out global::System.Func<int> createdValue);").And
			.Contains("out global::System.Text.StringBuilder createdValue);").And
			.DoesNotContain("global::System.IO.Stream.CreateMock").And
			.DoesNotContain("global::System.Func<int>.CreateMock");
	}

	[Test]
	public async Task ConcreteDependencies_ShouldBeCreatedUnlessTheyCloseACycle()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         #pragma warning disable CS9113
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public interface IDep { int Get(); }
		                                             public class Outer(Inner inner, Inner? nullableInner, Inner? optionalInner = null);
		                                             public class Inner(IDep dep, Node node);
		                                             public class Node(Node? next);

		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(Outer outer) { }
		                                             }

		                                             public class Program
		                                             {
		                                                 public static void Main(string[] args)
		                                                 {
		                                                 }
		                                             }
		                                         }

		                                         {{DataAttribute}}
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("type == typeof(global::MyCode.Inner)").And
			.Contains("type == typeof(global::MyCode.Node)").And
			.Contains("v0, \"inner\", \"MyCode.Outer\") : Create0(behavior, resolve, null)").And
			.Contains("v1, \"nullableInner\", \"MyCode.Outer\") : Create0(behavior, resolve, null)").And
			.Contains("v2, \"optionalInner\", \"MyCode.Outer\") : default").And
			.Contains("v1, \"node\", \"MyCode.Inner\") : Create1(behavior, resolve, null)").And
			.Contains("v0, \"next\", \"MyCode.Node\") : self.Contains(1) ? default : Create1(behavior, resolve, self)");
	}

	[Test]
	public async Task NullableObliviousParameter_ShouldNotGetNull()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public class Svc
		                                             {
		                                                 public Svc(string name) { }
		                                             }

		                                         #nullable enable
		                                         #pragma warning disable CS9113
		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(Svc sut) { }
		                                             }

		                                             public class Program
		                                             {
		                                                 public static void Main(string[] args)
		                                                 {
		                                                 }
		                                             }
		                                         }

		                                         {{DataAttribute}}
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("\"name\", \"MyCode.Svc\") : IsValue((behavior ?? global::Mockolate.MockBehavior.Default).DefaultValue.Generate(default(string)!), out string d0) ? d0 : throw new global::Mockolate.Exceptions.MockException(");
	}

	[Test]
	public async Task DependencyWithNullForgivingDefault_ShouldCompileWithoutWarnings()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         #pragma warning disable CS9113
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public class Dep;
		                                             public class Svc(Dep dep = null!, string name = null!);

		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(Svc sut) { }
		                                             }

		                                             public class Program
		                                             {
		                                                 public static void Main(string[] args)
		                                                 {
		                                                 }
		                                             }
		                                         }

		                                         {{DataAttribute}}
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("\"dep\", \"MyCode.Svc\") : default!").And
			.Contains("\"name\", \"MyCode.Svc\") : default!");
	}

	[Test]
	public async Task DependencyOfFrameworkInterface_ShouldGetAValueOrElseAMock()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         #pragma warning disable CS9113
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public abstract class AbstractDep { public abstract int Get(); }
		                                             public class Svc(System.IServiceProvider services, AbstractDep abstractDep,
		                                                 params System.Collections.Generic.IEnumerable<int> values);

		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(Svc sut) { }
		                                             }

		                                             public class Program
		                                             {
		                                                 public static void Main(string[] args)
		                                                 {
		                                                 }
		                                             }
		                                         }

		                                         {{DataAttribute}}
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("out global::System.IServiceProvider d0) ? d0 : global::System.IServiceProvider.CreateMock(").And
			.Contains("out global::MyCode.AbstractDep d1) ? d1 : throw new global::Mockolate.Exceptions.MockException(").And
			.Contains("\"values\", \"MyCode.Svc\") : [])");
	}

	[Test]
	public async Task AbstractClassWithoutParameterlessConstructor_ShouldNotBeMocked()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public abstract class WithParameter { protected WithParameter(int value) { } }
		                                             public abstract class WithPrivate { private WithPrivate() { } }
		                                             public abstract class WithProtected { protected WithProtected() { } }

		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(WithParameter a, WithPrivate b, WithProtected c) { }
		                                             }

		                                             public class Program
		                                             {
		                                                 public static void Main(string[] args)
		                                                 {
		                                                 }
		                                             }
		                                         }

		                                         {{DataAttribute}}
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("value = global::MyCode.WithProtected.CreateMock(").And
			.Contains("out global::MyCode.WithParameter createdValue").And
			.Contains("out global::MyCode.WithPrivate createdValue").And
			.DoesNotContain("global::MyCode.WithParameter.CreateMock").And
			.DoesNotContain("global::MyCode.WithPrivate.CreateMock");
	}

	[Test]
	public async Task ObsoleteInterface_ShouldBeSkipped()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             [System.Obsolete("old")]
		                                             public interface IOld { int Get(); }
		                                             public interface IDep { int Get(); }

		                                             public class Tests
		                                             {
		                                         #pragma warning disable CS0612, CS0618
		                                                 [Data]
		                                                 public void Test(IOld old, IDep dep) { }
		                                         #pragma warning restore CS0612, CS0618
		                                             }

		                                             public class Program
		                                             {
		                                                 public static void Main(string[] args)
		                                                 {
		                                                 }
		                                             }
		                                         }

		                                         {{DataAttribute}}
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("global::MyCode.IDep").And
			.DoesNotContain("IOld");
		await That(result.Sources).DoesNotContainKey("Mock.IOld.g.cs");
	}

	[Test]
	public async Task MethodTargetOnPrimaryConstructor_ShouldCreateItsParameters()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         #pragma warning disable CS9113
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public interface IDep { int Get(); }

		                                             [method: Data]
		                                             public class Tests(IDep dependency);

		                                             public class Program
		                                             {
		                                                 public static void Main(string[] args)
		                                                 {
		                                                 }
		                                             }
		                                         }

		                                         {{DataAttribute}}
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"]).Contains("type == typeof(global::MyCode.IDep)");
	}

	[Test]
	public async Task AttributeSpellings_ShouldBeFound()
	{
		GeneratorResult result = Generator.Run([
			"""
			global using GlobalAlias = MyCode.Outer.NestedDataAttribute;
			""",
			"""
			#nullable enable
			using MyCode;

			namespace MyCode
			{
			    public interface IForGlobalAlias { int Get(); }
			    public interface IForGeneric { int Get(); }

			    public static class Outer
			    {
			        [System.AttributeUsage(System.AttributeTargets.Method)]
			        public sealed class NestedDataAttribute : System.Attribute, Mockolate.IMockDataSource;
			    }

			    [System.AttributeUsage(System.AttributeTargets.Method)]
			    public sealed class GenericDataAttribute<T> : System.Attribute, Mockolate.IMockDataSource;

			    public class Tests
			    {
			        [GlobalAlias]
			        public void A(IForGlobalAlias a) { }

			        [GenericData<int>]
			        public void B(IForGeneric b) { }
			    }

			    public class Program
			    {
			        public static void Main(string[] args)
			        {
			        }
			    }
			}
			""",
		], [], LanguageVersion.CSharp14, null);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("type == typeof(global::MyCode.IForGlobalAlias)").And
			.Contains("type == typeof(global::MyCode.IForGeneric)");
	}

	[Test]
	public async Task DerivedAttributeOfAssemblyThatOnlyReferencesTheBase_ShouldBeFound()
	{
		MetadataReference baseReference = ExternalAssembly.Compile("""
		                                                           namespace External
		                                                           {
		                                                               [System.AttributeUsage(System.AttributeTargets.Method)]
		                                                               public class BaseDataAttribute : System.Attribute, Mockolate.IMockDataSource;
		                                                           }
		                                                           """, "ExternalBase");
		MetadataReference derivedReference = ExternalAssembly.Compile("""
		                                                              namespace External2
		                                                              {
		                                                                  public sealed class DerivedDataAttribute : External.BaseDataAttribute;
		                                                              }
		                                                              """, "ExternalDerived", references: [baseReference,]);
		GeneratorResult result = Generator.RunWithReferences("""
		                                                     #nullable enable
		                                                     namespace MyCode
		                                                     {
		                                                         public interface IDep { int Get(); }

		                                                         public class Program
		                                                         {
		                                                             [External2.DerivedData]
		                                                             public static object Test(IDep dep) => dep;

		                                                             public static void Main(string[] args)
		                                                             {
		                                                             }
		                                                         }
		                                                     }
		                                                     """, [baseReference, derivedReference,]);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"]).Contains("type == typeof(global::MyCode.IDep)");
	}

	[Test]
	public async Task KeywordClassName_ShouldNotSpellTheAtSignInMessages()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         #pragma warning disable CS9113
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public class @event(string name);

		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(@event sut) { }
		                                             }

		                                             public class Program
		                                             {
		                                                 public static void Main(string[] args)
		                                                 {
		                                                 }
		                                             }
		                                         }

		                                         {{DataAttribute}}
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("constructor parameter 'name' of MyCode.event.\"");
	}

	[Test]
	[Arguments(null, true)]
	[Arguments("false", false)]
	[Arguments("ModuleInitializer", false)]
	[Arguments("UnionAttribute", true)]
	public async Task ModuleInitializerPolyfill_ShouldFollowTheBuildProperty(string? configured, bool expectPolyfill)
	{
		// The attribute in two references is ambiguous, so the compilation lacks it like on .NET Framework.
		MetadataReference first = ExternalAssembly.Compile(ModuleInitializerAttributeSource, "First");
		MetadataReference second = ExternalAssembly.Compile(ModuleInitializerAttributeSource, "Second");
		GeneratorResult result = Generator.RunWithReferences($$"""
		                                                       #nullable enable
		                                                       using MyCode;

		                                                       namespace MyCode
		                                                       {
		                                                           public interface IDep { int Get(); }

		                                                           public class Program
		                                                           {
		                                                               [Data]
		                                                               public static object Test(IDep dep) => dep;

		                                                               public static void Main(string[] args)
		                                                               {
		                                                               }
		                                                           }
		                                                       }

		                                                       {{DataAttribute}}
		                                                       """, [first, second,],
			configured is null
				? null
				: new Dictionary<string, string> { ["build_property.MockolateUnionAttributePolyfills"] = configured, });

		await That(result.Sources["MockDataSources.g.cs"].Contains("internal sealed class ModuleInitializerAttribute"))
			.IsEqualTo(expectPolyfill);
	}

	[Test]
	public async Task UnrelatedEdit_ShouldNotGenerateTheFactoryAgain()
	{
		CSharpParseOptions parseOptions = new(LanguageVersion.CSharp14);
		SyntaxTree unrelated = CSharpSyntaxTree.ParseText("namespace MyCode { public class Unrelated { } }",
			parseOptions);
		CSharpCompilation compilation = CSharpCompilation.Create("TestAssembly",
			[
				CSharpSyntaxTree.ParseText($$"""
				                             #nullable enable
				                             #pragma warning disable CS9113
				                             namespace MyCode
				                             {
				                                 public interface IDep { int Get(); }
				                                 public class Svc(IDep dep, int count);

				                                 public class Program
				                                 {
				                                     [Data]
				                                     public static object Test(Svc sut, IDep dep, string text) => sut;

				                                     public static void Main(string[] args)
				                                     {
				                                     }
				                                 }
				                             }

				                             {{DataAttribute}}
				                             """, parseOptions),
				unrelated,
			],
			Generator.GetReferences([]),
			new CSharpCompilationOptions(OutputKind.ConsoleApplication));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new MockGenerator().AsSourceGenerator(),], [],
			parseOptions, null,
			new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

		driver = driver.RunGenerators(compilation);
		driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(unrelated,
			CSharpSyntaxTree.ParseText("namespace MyCode { public class Unrelated { public int Value; } }",
				parseOptions)));

		IncrementalStepRunReason[] reasons = driver.GetRunResult().Results.Single().TrackedOutputSteps
			.SelectMany(step => step.Value)
			.SelectMany(run => run.Outputs)
			.Select(output => output.Reason)
			.ToArray();
		await That(reasons).IsNotEmpty().And
			.All().Satisfy(reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)
			.Because("an edit that changes no data source member must not regenerate any file");
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task MockDataSourcesSource_ShouldContainThePolyfillOnlyWhenRequested(bool emitPolyfill)
	{
		string source = Sources.Sources.MockDataSources(new EquatableArray<DataSourceClass>([]),
			new EquatableArray<string>(["global::MyCode.IDep",]), new EquatableArray<string>([]),
			new EquatableArray<string>([]), emitPolyfill);

		await That(source.Contains("internal sealed class ModuleInitializerAttribute")).IsEqualTo(emitPolyfill);
	}

	private const string ModuleInitializerAttributeSource = """
	                                                        namespace System.Runtime.CompilerServices
	                                                        {
	                                                            [System.AttributeUsage(System.AttributeTargets.Method)]
	                                                            public sealed class ModuleInitializerAttribute : System.Attribute;
	                                                        }
	                                                        """;
}
