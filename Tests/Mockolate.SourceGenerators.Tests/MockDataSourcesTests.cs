using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mockolate.Internals;
using Mockolate.SourceGenerators.Entities;
using Mockolate.SourceGenerators.Tests.TestHelpers;

namespace Mockolate.SourceGenerators.Tests;

public sealed class MockDataSourcesTests
{
	private const string DataAttribute = """
	                                     namespace MyCode
	                                     {
	                                         [System.AttributeUsage(System.AttributeTargets.All)]
	                                         public sealed class DataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource;
	                                     }
	                                     """;

	[Test]
	public async Task CreateMockWithConstructorDependenciesWithoutDataSource_ShouldNotRegisterAFactory()
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
		                                                   _ = Svc.CreateMock();
		                                               }
		                                           }
		                                       }
		                                       """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.IDep.g.cs").And.DoesNotContainKey("MockDataSources.g.cs");
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
		                                                           public sealed class AutoDataAttribute : Attribute, Mockolate.Internals.IMockDataSource;
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
		                                           public sealed class DataAttribute : Attribute, Mockolate.Internals.IMockDataSource;
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
			.Contains("\"name\", \"MyCode.Svc\") : IsValue(behavior ?? global::Mockolate.MockBehavior.Default, static mockolateBehavior => mockolateBehavior.DefaultValue.Generate(default(string)!), out string d0) ? d0 : throw new global::Mockolate.Exceptions.MockException(");
	}

	[Test]
	public async Task ValueFromParameterOnly_ShouldOnlyKeepTheMembersOfTheTestClass()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         namespace MyCode.@event
		                                         {
		                                             public interface IDep { int Get(); }

		                                             public class Outer<T>
		                                             {
		                                                 public class Tests
		                                                 {
		                                                     [MyCode.Data]
		                                                     public void Test([Mockolate.Integrations.ValueFrom(nameof(Dep))] IDep dep) { }

		                                                     private static IDep Dep => null!;
		                                                 }
		                                             }

		                                             public class Program
		                                             {
		                                                 public static void Main(string[] args)
		                                                 {
		                                                 }
		                                             }
		                                         }

		                                         namespace Mockolate.Integrations
		                                         {
		                                             public abstract class ValueFromParameterAttribute(string memberName) : System.Attribute
		                                             {
		                                                 public string MemberName { get; } = memberName;
		                                             }

		                                             public sealed class ValueFromAttribute(string memberName) : ValueFromParameterAttribute(memberName);
		                                         }

		                                         {{DataAttribute}}
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"]).Contains("NonPublicMethods, \"MyCode.event.Outer`1.Tests\", \"")
			.Because("the trimmer reads unescaped names with nested types separated by a dot");
		await That(result.Sources.Keys).All().Satisfy(name => !name.Contains("IDep"))
			.Because("Mockolate does not create a [ValueFrom] parameter, so it needs no mock");
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
			.Contains("\"values\", \"MyCode.Svc\") : [];");
	}

	[Test]
	public async Task AbstractClass_ShouldOnlyBeMockedWhenCreateMockCanCallAConstructor()
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
			.Contains("return global::MyCode.WithProtected.CreateMock(").And
			.Contains("return global::MyCode.WithParameter.CreateMock(").And
			.Contains("out global::MyCode.WithPrivate createdValue").And
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
			        public sealed class NestedDataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource;
			    }

			    [System.AttributeUsage(System.AttributeTargets.Method)]
			    public sealed class GenericDataAttribute<T> : System.Attribute, Mockolate.Internals.IMockDataSource;

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
		                                                               public class BaseDataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource;
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
			// The compilation references the TUnit of this test project, which must not provide the attribute here.
			configured is null
				? new Dictionary<string, string> { ["build_property.EnableTUnitPolyfills"] = "false", }
				: new Dictionary<string, string>
				{
					["build_property.MockolateUnionAttributePolyfills"] = configured,
					["build_property.EnableTUnitPolyfills"] = "false",
				});

		// With the opt-out, the attribute would come from another generator, which this compilation does not run.
		if (expectPolyfill)
		{
			await That(result.Diagnostics).IsEmpty();
		}

		await That(result.Sources["MockDataSources.g.cs"].Contains("internal sealed class ModuleInitializerAttribute"))
			.IsEqualTo(expectPolyfill);
	}

	[Test]
	[Arguments("true", null, null, null, false)]
	[Arguments(null, null, null, null, true)]
	[Arguments("false", null, null, null, true)]
	[Arguments("true", "false", null, null, true)]
	// PolySharp declares nothing, because two references declare the attribute, so Mockolate declares it.
	[Arguments("true", null, "", null, true)]
	[Arguments(null, null, "", null, true)]
	[Arguments("true", null, "System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute", null, false)]
	[Arguments("true", null, "", "System.Runtime.CompilerServices.ModuleInitializerAttribute", false)]
	public async Task ModuleInitializerPolyfill_WithTUnit_ShouldFollowItsBuildProperties(string? sourceGeneration,
		string? polyfills, string? polySharpIncluded, string? polySharpExcluded, bool expectPolyfill)
	{
		// The attribute in two references is ambiguous, so the compilation lacks it like on .NET Framework.
		MetadataReference first = ExternalAssembly.Compile(ModuleInitializerAttributeSource, "First");
		MetadataReference second = ExternalAssembly.Compile(ModuleInitializerAttributeSource, "Second");
		Dictionary<string, string> options = new();
		AddIfNotNull("build_property.EnableTUnitSourceGeneration", sourceGeneration);
		AddIfNotNull("build_property.EnableTUnitPolyfills", polyfills);
		AddIfNotNull("build_property.PolySharpIncludeGeneratedTypes", polySharpIncluded);
		AddIfNotNull("build_property.PolySharpExcludeGeneratedTypes", polySharpExcluded);

		// The compilation references the TUnit of this test project, whose generator declares the attribute.
		GeneratorResult result = Generator.RunWithReferences($$"""
		                                                       #nullable enable
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
		                                                       """, [first, second,], options, typeof(global::TUnit.Core.TestAttribute));

		await That(result.Sources["MockDataSources.g.cs"].Contains("internal sealed class ModuleInitializerAttribute"))
			.IsEqualTo(expectPolyfill);

		void AddIfNotNull(string key, string? value)
		{
			if (value is not null)
			{
				options[key] = value;
			}
		}
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
	public async Task AbstractClassConstructors_ShouldFollowTheEmittedMock()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public abstract class WithInternal { internal WithInternal() { } }
		                                             public abstract class WithOptional { protected WithOptional(int value = 1) { } }

		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(WithInternal a, WithOptional b) { }
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
			.Contains("return global::MyCode.WithOptional.CreateMock(").And
			.Contains("out global::MyCode.WithInternal createdValue").And
			.DoesNotContain("global::MyCode.WithInternal.CreateMock");
	}

	[Test]
	public async Task TypesWhoseMockWouldNotCompile_ShouldNotBeMocked()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public delegate ref int RefCallback(ref int value);
		                                             public interface IRefProperty { ref int Value { get; } }
		                                             [System.Obsolete("old")]
		                                             public class Old;
		                                         #pragma warning disable CS0612, CS0618
		                                             public interface IUsesOld { Old Get(); }

		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(RefCallback a, IRefProperty b, IUsesOld c) { }
		                                             }
		                                         #pragma warning restore CS0612, CS0618

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
		await That(result.Sources)
			.DoesNotContainKey("Mock.RefCallback.g.cs").And
			.DoesNotContainKey("Mock.IRefProperty.g.cs").And
			.DoesNotContainKey("Mock.IUsesOld.g.cs");
	}

	[Test]
	public async Task FileLocalTypeInATypeArgumentOrArray_ShouldBeSkipped()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public interface IGen<T> { T Get(); }
		                                             public interface IDep { int Get(); }
		                                             file class Local;

		                                             file class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(Local[] a, IGen<Local> b, IDep c) { }
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
			.DoesNotContain("Local");
	}

	[Test]
	public async Task MemberSealedInAnAbstractClass_ShouldNotPreventTheMock()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public abstract class Base
		                                             {
		                                                 public abstract ref int Value { get; }
		                                             }

		                                             public abstract class Derived : Base
		                                             {
		                                                 private int _value;
		                                                 public sealed override ref int Value => ref _value;
		                                             }

		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(Derived derived) { }
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
		await That(result.Sources["MockDataSources.g.cs"]).Contains("return global::MyCode.Derived.CreateMock(");
	}

	[Test]
	public async Task DynamicParameter_ShouldGetAValueAsObject()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(dynamic value) { }
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
		await That(result.Sources).ContainsKey("MockDataSources.g.cs");
		await That(result.Sources["MockDataSources.g.cs"]).Contains("type == typeof(object)");
	}

	[Test]
	public async Task AbstractClass_ShouldDependOnTheConstructorTheMockSelects()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         using System;
		                                         using MyCode;

		                                         namespace MyCode
		                                         {
		                                             public abstract class SelectsObsolete
		                                             {
		                                                 [Obsolete("old", true)] protected SelectsObsolete() { }
		                                                 protected SelectsObsolete(int value = 0) { }
		                                             }

		                                             public abstract class SelectsUsable
		                                             {
		                                                 protected SelectsUsable() { }
		                                                 [Obsolete("old", true)] protected SelectsUsable(int value = 0) { }
		                                             }

		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test(SelectsObsolete a, SelectsUsable b) { }
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

		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("return global::MyCode.SelectsUsable.CreateMock(").And
			.DoesNotContain("global::MyCode.SelectsObsolete.CreateMock");
	}

	[Test]
	public async Task AttributeNestedInAStructOfAReferencedAssembly_ShouldBeFound()
	{
		MetadataReference reference = ExternalAssembly.Compile("""
		                                                       namespace External
		                                                       {
		                                                           public struct Container
		                                                           {
		                                                               [System.AttributeUsage(System.AttributeTargets.Method)]
		                                                               public sealed class NestedDataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource;
		                                                           }
		                                                       }
		                                                       """);
		GeneratorResult result = Generator.RunWithReferences("""
		                                                     #nullable enable
		                                                     namespace MyCode
		                                                     {
		                                                         public interface IDep { int Get(); }

		                                                         public class Program
		                                                         {
		                                                             [External.Container.NestedData]
		                                                             public static object Test(IDep dep) => dep;

		                                                             public static void Main(string[] args)
		                                                             {
		                                                             }
		                                                         }
		                                                     }
		                                                     """, [reference,]);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"]).Contains("type == typeof(global::MyCode.IDep)");
	}

	[Test]
	public async Task TryCreate_BeforeAnyTypeOfTheAssemblyIsUsed_ShouldRunItsModuleInitializer()
	{
		Assembly assembly = Generator.CompileAndLoad($$"""
		                                               namespace MyCode
		                                               {
		                                                   public interface IDep { int Get(); }
		                                                   public abstract class WithOptional
		                                                   {
		                                                       protected WithOptional(int value = 1) { }
		                                                   }

		                                                   public static class Tests
		                                                   {
		                                                       [Data]
		                                                       public static void Test(IDep a, WithOptional b) { }
		                                                   }
		                                               }

		                                               {{DataAttribute}}
		                                               """, LanguageVersion.CSharp14);

		bool mocked = MockDataSource.TryCreate(assembly, assembly.GetType("MyCode.IDep")!, null, NoArgument,
			out object? mock);
		bool mockedClass = MockDataSource.TryCreate(assembly, assembly.GetType("MyCode.WithOptional")!, null, NoArgument,
			out object? classMock);

		await That(mocked).IsTrue();
		await That(mock).IsNotNull();
		await That(mockedClass).IsTrue();
		await That(classMock).IsNotNull();
	}

	[Test]
	public async Task Register_ShouldReplaceTheFactoryOfTheAssembly()
	{
		Assembly assembly = Generator.CompileAndLoad("namespace MyCode { public class Empty; }",
			LanguageVersion.CSharp14);
		bool withoutFactory = MockDataSource.TryCreate(assembly, typeof(string), null, NoArgument, out _);

		MockDataSourceRegistry.Register(assembly, CreateFirst);
		MockDataSource.TryCreate(assembly, typeof(string), null, NoArgument, out object? first);
		MockDataSourceRegistry.Register(assembly, CreateSecond);
		MockDataSource.TryCreate(assembly, typeof(string), null, NoArgument, out object? second);

		await That(withoutFactory).IsFalse();
		await That(first).IsEqualTo("first");
		await That(second).IsEqualTo("second");

		static bool CreateFirst(System.Type type, MockBehavior? behavior, MockArgumentResolver resolve, out object? value)
		{
			value = "first";
			return true;
		}

		static bool CreateSecond(System.Type type, MockBehavior? behavior, MockArgumentResolver resolve, out object? value)
		{
			value = "second";
			return true;
		}
	}

	[Test]
	public async Task DeferredFunc_ShouldRegisterAFactoryOnlyForTheMarkerOfMockolate()
	{
		GeneratorResult result = Generator.Run($$"""
		                                         #nullable enable
		                                         using System;
		                                         using MyCode;

		                                         namespace Mockolate.Integrations
		                                         {
		                                             public abstract class DeferredParameterAttribute : Attribute;
		                                         }

		                                         namespace Other
		                                         {
		                                             public abstract class DeferredParameterAttribute : Attribute;
		                                         }

		                                         namespace MyCode
		                                         {
		                                             public sealed class DeferredAttribute : Mockolate.Integrations.DeferredParameterAttribute;
		                                             public sealed class OtherAttribute : Other.DeferredParameterAttribute;
		                                             public interface IDeferred { int Get(); }
		                                             public interface IOther { int Get(); }

		                                             public class Tests
		                                             {
		                                                 [Data]
		                                                 public void Test([Deferred] Func<IDeferred> create, [Other] Func<IOther> other,
		                                                     [Deferred] Func<int, IOther> withArgument) { }
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
			.Contains("Register(typeof(MockDataSourceRegistration).Assembly, Create, CreateDeferred);").And
			.Contains("type == typeof(global::MyCode.IDeferred)").And
			.Contains("type == typeof(global::System.Func<global::MyCode.IDeferred>)").And
			.Contains("value = new global::System.Func<global::MyCode.IDeferred>(").And
			.DoesNotContain("new global::System.Func<global::MyCode.IOther>").And
			.DoesNotContain("new global::System.Func<int, global::MyCode.IOther>");
	}

	[Test]
	public async Task Register_WithoutDeferredFactory_ShouldReplaceTheDeferredFactoryOfTheAssembly()
	{
		Assembly assembly = Generator.CompileAndLoad("namespace MyCode { public class Empty; }",
			LanguageVersion.CSharp14);

		MockDataSourceRegistry.Register(assembly, CreateNothing, CreateDeferred);
		bool withDeferredFactory = MockDataSource.TryCreateDeferred(assembly, typeof(string), null, NoArgument,
			out object? deferred);
		MockDataSourceRegistry.Register(assembly, CreateNothing);
		bool withoutDeferredFactory = MockDataSource.TryCreateDeferred(assembly, typeof(string), null, NoArgument,
			out _);

		await That(withDeferredFactory).IsTrue();
		await That(deferred).IsEqualTo("deferred");
		await That(withoutDeferredFactory).IsFalse();

		static bool CreateNothing(System.Type type, MockBehavior? behavior, MockArgumentResolver resolve, out object? value)
		{
			value = null;
			return false;
		}

		static bool CreateDeferred(System.Type type, MockBehavior? behavior, MockArgumentResolver resolve, out object? value)
		{
			value = "deferred";
			return true;
		}
	}

	private static bool NoArgument(System.Type type, string parameterName, out object? value)
	{
		value = null;
		return false;
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task MockDataSourcesSource_ShouldContainThePolyfillOnlyWhenRequested(bool emitPolyfill)
	{
		string source = Sources.Sources.MockDataSources(new EquatableArray<DataSourceClass>([]),
			new EquatableArray<string>(["global::MyCode.IDep",]), new EquatableArray<string>([]),
			new EquatableArray<string>([]), new EquatableArray<string>([]), new EquatableArray<string>([]), emitPolyfill);

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
