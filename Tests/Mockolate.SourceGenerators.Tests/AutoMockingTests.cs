using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

public sealed class AutoMockingTests
{
	private static readonly Dictionary<string, string> AutoMockingEnabled = new()
	{
		["build_property.MockolateAutoMocking"] = "true",
	};

	[Test]
	public async Task FrameworkInterfaces_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             using System;

		                             namespace Microsoft.Extensions.Something
		                             {
		                                 public interface IFrameworkService
		                                 {
		                                     int Value { get; }
		                                 }
		                             }

		                             namespace MyCode
		                             {
		                                 public interface IMyInterface
		                                 {
		                                     IServiceProvider GetServiceProvider();
		                                     Microsoft.Extensions.Something.IFrameworkService GetFrameworkService();
		                                 }
		                             }
		                             """, "IMyInterface");

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources)
			.ContainsKey("Mock.IMyInterface.g.cs").And
			.DoesNotContainKey("Mock.IServiceProvider.g.cs").And
			.DoesNotContainKey("Mock.IFrameworkService.g.cs");
	}

	[Test]
	public async Task ExcludedNamespaces_ShouldNotBeMocked()
	{
		GeneratorResult result = Generator.Run([
			"""
			using Mockolate;

			namespace MyCode
			{
			    public class Program
			    {
			        public static void Main(string[] args)
			        {
			            _ = IRoot.CreateMock();
			            _ = Sdk.Storage.IExplicit.CreateMock();
			        }
			    }

			    public interface IRoot
			    {
			        Sdk.IClient GetClient();
			        Sdk.Storage.IBlob GetBlob();
			        Sdk.Storage.IExplicit GetExplicit();
			        SdkExtensions.IExtension GetExtension();
			    }
			}

			namespace Sdk
			{
			    public interface IClient { }
			}

			namespace Sdk.Storage
			{
			    public interface IBlob { }
			    public interface IExplicit { }
			}

			namespace SdkExtensions
			{
			    public interface IExtension { }
			}
			""",
		], [], LanguageVersion.CSharp14, new Dictionary<string, string>
		{
			["build_property.MockolateAutoMocking"] = "true",
			["build_property.MockolateAutoMockingExcludedNamespaces"] = " Other ; Sdk",
		});

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources)
			.DoesNotContainKey("Mock.IClient.g.cs").And
			.DoesNotContainKey("Mock.IBlob.g.cs").And
			.ContainsKey("Mock.IExtension.g.cs")
			.Because("only the namespace and its sub-namespaces are excluded, not every namespace with the same prefix");
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("return global::Sdk.Storage.IExplicit.CreateMock(behavior);")
			.Because("a mock that the project creates is still used for auto-mocking");
	}

	[Test]
	public async Task ExternAliasedInterfacesWithTheSameName_ShouldBothBeMocked()
	{
		const string source = """
		                      namespace Ns;

		                      public interface IFoo { }
		                      """;
		MetadataReference first = ExternalAssembly.Compile(source, "First").WithAliases(["First",]);
		MetadataReference second = ExternalAssembly.Compile(source, "Second").WithAliases(["Second",]);

		GeneratorResult result = Generator.RunWithReferences("""
		                                                     extern alias First;
		                                                     extern alias Second;
		                                                     using Mockolate;

		                                                     namespace MyCode;

		                                                     public interface IRoot
		                                                     {
		                                                         First::Ns.IFoo GetFirst();
		                                                         Second::Ns.IFoo GetSecond();
		                                                     }

		                                                     public class Program
		                                                     {
		                                                         public static void Main(string[] args) => _ = IRoot.CreateMock();
		                                                     }
		                                                     """, [first, second,], AutoMockingEnabled);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("typeof(First::Ns.IFoo)").And
			.Contains("typeof(Second::Ns.IFoo)");
	}

	[Test]
	public async Task DeeplyNestedTypeArguments_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public interface IRoot
		                                 {
		                                     IA<IB<int>> GetShallow();
		                                     IA<IB<IC<ID<IE<IF<int>>>>>> GetDeep();
		                                 }

		                                 public interface IA<T> { }
		                                 public interface IB<T> { }
		                                 public interface IC<T> { }
		                                 public interface ID<T> { }
		                                 public interface IE<T> { }
		                                 public interface IF<T> { }
		                             }
		                             """, "IRoot");

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("typeof(global::MyCode.IA<global::MyCode.IB<int>>)").And
			.DoesNotContain("global::MyCode.IF<int>")
			.Because("a cap on the nesting bounds how many generic combinations the transitive walk creates");
	}

	[Test]
	public async Task InterfacesWithNonPublicAbstractMembers_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public interface IRoot
		                                 {
		                                     IProtected GetProtected();
		                                     IDerived GetDerived();
		                                 }

		                                 public interface IProtected
		                                 {
		                                     protected int Get();
		                                 }

		                                 public interface IDerived : IProtected { }
		                             }
		                             """, "IRoot");

		await That(result.Diagnostics).IsEmpty().Because("the mock cannot implement a protected member (CS0737)");
		await That(result.Sources.Keys).None().Satisfy(key => key.Contains("IProtected") || key.Contains("IDerived"));
	}

	[Test]
	public async Task InterfacesWithUnsupportedMemberShapes_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public interface IRoot
		                                 {
		                                     IRefProperty GetRefProperty();
		                                     IRefIndexer GetRefIndexer();
		                                     IAllowsRefStruct GetAllowsRefStruct();
		                                     IRefMethod GetRefMethod();
		                                 }

		                                 public interface IRefProperty
		                                 {
		                                     ref int Value { get; }
		                                 }

		                                 public interface IRefIndexer
		                                 {
		                                     ref readonly int this[int index] { get; }
		                                 }

		                                 public interface IAllowsRefStruct
		                                 {
		                                     void Use<T>(T value) where T : allows ref struct;
		                                 }

		                                 public interface IRefMethod
		                                 {
		                                     ref int Get();
		                                 }
		                             }
		                             """, "IRoot");

		await That(result.Diagnostics).IsEmpty()
			.Because("the generator emits no compilable mock for these members (CS8152, CS9244)");
		await That(result.Sources.Keys).None().Satisfy(key =>
			key.Contains("IRefProperty") || key.Contains("IRefIndexer") || key.Contains("IAllowsRefStruct"));
		await That(result.Sources).ContainsKey("Mock.IRefMethod.g.cs");
	}

	[Test]
	public async Task InterfacesWithRefReadOnlySpanParameters_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public interface IRoot
		                                 {
		                                     IReader GetReader();
		                                     ISpan GetSpan();
		                                 }

		                                 public interface IReader
		                                 {
		                                     int Read(ref readonly System.Span<byte> buffer);
		                                 }

		                                 public interface ISpan
		                                 {
		                                     int Read(System.Span<byte> buffer);
		                                 }
		                             }
		                             """, "IRoot");

		await That(result.Diagnostics).IsEmpty()
			.Because("a ref readonly span needs the ref-struct pipeline, which does not compile below .NET 9");
		await That(result.Sources).DoesNotContainKey("Mock.IReader.g.cs").And
			.ContainsKey("Mock.ISpan.g.cs");
	}

	[Test]
	public async Task InaccessibleInterfaces_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public abstract class MyService
		                                 {
		                                     protected abstract IHidden GetHidden();
		                                     public abstract IVisible GetVisible();

		                                     protected interface IHidden { }
		                                 }

		                                 public interface IVisible { }
		                             }
		                             """, "MyService");

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.IVisible.g.cs");
		await That(result.Sources.Keys).None().Satisfy(key => key.Contains("IHidden"));
	}

	[Test]
	public async Task InterfaceWithStaticAbstractMemberInSignature_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public interface IRoot
		                                 {
		                                     IMiddle GetMiddle();
		                                 }

		                                 public interface IMiddle
		                                 {
		                                     IStaticAbstract GetStaticAbstract();
		                                 }

		                                 public interface IStaticAbstract
		                                 {
		                                     static abstract int Create();
		                                 }
		                             }
		                             """, "IRoot");

		await That(result.Diagnostics).IsEmpty()
			.Because("a mock of IMiddle would use IStaticAbstract as a type argument (CS8920)");
		await That(result.Sources.Keys).None().Satisfy(key => key.Contains("IMiddle") || key.Contains("IStaticAbstract"));
	}

	[Test]
	public async Task MembersThatCannotBeOverridden_ShouldNotBeFollowed()
	{
		MetadataReference external = ExternalAssembly.Compile("""
		                                                      namespace Ext;

		                                                      public abstract class MyBase
		                                                      {
		                                                          public abstract IAbstractResult GetAbstract();
		                                                          public virtual IVirtualResult GetVirtual() => null!;
		                                                          public INonVirtualResult GetNonVirtual() => null!;
		                                                          internal virtual IInternalResult GetInternal() => null!;
		                                                          public virtual ISetterOnly SetterOnly { set { } }
		                                                      }

		                                                      public interface IAbstractResult { }
		                                                      public interface IVirtualResult { }
		                                                      public interface INonVirtualResult { }
		                                                      internal interface IInternalResult { }
		                                                      public interface ISetterOnly { }
		                                                      """);

		GeneratorResult result = Generator.RunWithReferences("""
		                                                     using Mockolate;

		                                                     namespace MyCode;

		                                                     public class Program
		                                                     {
		                                                         public static void Main(string[] args) => _ = Ext.MyBase.CreateMock();
		                                                     }
		                                                     """, [external,], AutoMockingEnabled);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources)
			.ContainsKey("Mock.IAbstractResult.g.cs").And
			.ContainsKey("Mock.IVirtualResult.g.cs").And
			.DoesNotContainKey("Mock.INonVirtualResult.g.cs").And
			.DoesNotContainKey("Mock.IInternalResult.g.cs").And
			.DoesNotContainKey("Mock.ISetterOnly.g.cs")
			.Because("the mock never returns a value from a member it cannot override or that has no getter");
	}

	[Test]
	public async Task InterfacesWithPointersOrRefStructs_ShouldNotBeMocked()
	{
		MetadataReference external = ExternalAssembly.Compile("""
		                                                      namespace Ext;

		                                                      public interface IRoot
		                                                      {
		                                                          IPointer GetPointer();
		                                                          IFunctionPointer GetFunctionPointer();
		                                                          IRefStruct GetRefStruct();
		                                                          IPlain GetPlain();
		                                                      }

		                                                      public unsafe interface IPointer
		                                                      {
		                                                          int* Get();
		                                                      }

		                                                      public unsafe interface IFunctionPointer
		                                                      {
		                                                          delegate*<int> Get();
		                                                      }

		                                                      public ref struct MyRefStruct { }

		                                                      public interface IRefStruct
		                                                      {
		                                                          MyRefStruct Get();
		                                                      }

		                                                      public interface IPlain { }
		                                                      """, allowUnsafe: true);

		GeneratorResult result = Generator.RunWithReferences("""
		                                                     using Mockolate;

		                                                     namespace MyCode;

		                                                     public class Program
		                                                     {
		                                                         public static void Main(string[] args) => _ = Ext.IRoot.CreateMock();
		                                                     }
		                                                     """, [external,], AutoMockingEnabled);

		await That(result.Diagnostics).IsEmpty()
			.Because("the generator emits no compilable mock for these signatures without an analyzer diagnostic");
		await That(result.Sources).ContainsKey("Mock.IPlain.g.cs");
		await That(result.Sources.Keys).None().Satisfy(key =>
			key.Contains("IPointer") || key.Contains("IFunctionPointer") || key.Contains("IRefStruct"));
	}

	[Test]
	public async Task MockedTypeNamedLikeTheAutoMockFactory_ShouldNotClash()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public interface CreateAutoMock
		                                 {
		                                     CreateAutoMock GetSelf();
		                                 }
		                             }
		                             """, "CreateAutoMock");

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.CreateAutoMock_1.g.cs");
	}

	[Test]
	public async Task MockNamedInCreateMock_ShouldKeepItsNameWhenADiscoveredMockHasTheSameName()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode.A
		                             {
		                                 public interface IFoo { }
		                             }

		                             namespace MyCode.B
		                             {
		                                 public interface IFoo
		                                 {
		                                     MyCode.A.IFoo GetOther();
		                                 }
		                             }
		                             """, "B.IFoo");

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["Mock.IFoo.g.cs"]).Contains("global::MyCode.B.IFoo")
			.Because("turning on auto-mocking must not rename the generated types of an explicit mock");
		await That(result.Sources["Mock.IFoo_1.g.cs"]).Contains("global::MyCode.A.IFoo");
	}

	[Test]
	public async Task ObsoleteInterfaces_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             using System;

		                             namespace MyCode
		                             {
		                                 public interface IRoot
		                                 {
		                                     IMiddle GetMiddle();
		                                 }

		                                 public interface IMiddle
		                                 {
		                                     [Obsolete("old")]
		                                     IOld GetOld();
		                                 }

		                                 [Obsolete("old")]
		                                 public interface IOld { }
		                             }
		                             """, "IRoot");

		await That(result.Diagnostics).IsEmpty().Because("a mock of IMiddle or IOld would report CS0618");
		await That(result.Sources.Keys).None().Satisfy(key => key.Contains("IMiddle") || key.Contains("IOld"));
	}

	[Test]
	public async Task OpenGenericReturnTypes_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             using System.Collections.Generic;

		                             namespace MyCode
		                             {
		                                 public interface IFactory
		                                 {
		                                     IRepository<List<T>> Get<T>();
		                                     IRepository<T[]> GetArray<T>();
		                                 }

		                                 public interface IRepository<T> { }
		                             }
		                             """, "IFactory");

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources.Keys).None().Satisfy(key => key.StartsWith("Mock.IRepository"));
	}

	[Test]
	public async Task SelfExpandingGenericInterface_ShouldNotBeMockedEndlessly()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public interface INode<T>
		                                 {
		                                     INode<INode<T>> Wrap();
		                                 }
		                             }
		                             """, "INode<int>");

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources.Keys.Where(key => key.StartsWith("Mock.INode"))).HasCount(1);
	}

	[Test]
	public async Task SelfExpandingInterfaceNestedInGenericType_ShouldNotBeMockedEndlessly()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public class Box<T>
		                                 {
		                                     public interface INode
		                                     {
		                                         Box<Box<T>>.INode Next();
		                                     }
		                                 }

		                                 public interface IRoot
		                                 {
		                                     Box<int>.INode Get();
		                                 }
		                             }
		                             """, "IRoot", "Box<int>.INode");

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources.Keys.Where(key => key.Contains("INode"))).HasCount(1)
			.Because("only the mock that CreateMock names is generated, so the walk cannot expand the containing type");
	}

	[Test]
	public async Task InterfacesNestedInGenericTypes_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public class Outer<T>
		                                 {
		                                     public interface IInner
		                                     {
		                                         T Value { get; }
		                                     }
		                                 }

		                                 public interface IGeneric<T> { }

		                                 public interface IRoot
		                                 {
		                                     Outer<string?>.IInner GetNullable();
		                                     Outer<string>.IInner GetNotNullable();
		                                     IGeneric<Outer<string?>.IInner> GetWrapped();
		                                 }
		                             }
		                             """, "IRoot");

		await That(result.Diagnostics).IsEmpty()
			.Because("the nullable spelling in the containing type would reach the oblivious receiver (CS8669)");
		await That(result.Sources.Keys).None().Satisfy(key => key.Contains("IInner"));
	}

	[Test]
	public async Task UnrelatedEdit_ShouldKeepTheGeneratedOutputCached()
	{
		CSharpParseOptions parseOptions = new(LanguageVersion.CSharp14);
		SyntaxTree unrelated = CSharpSyntaxTree.ParseText("namespace MyCode { public class Unrelated { } }",
			parseOptions);
		CSharpCompilation compilation = CSharpCompilation.Create("TestAssembly",
			[
				CSharpSyntaxTree.ParseText("""
				                           using Mockolate;

				                           namespace MyCode
				                           {
				                               public class Program
				                               {
				                                   public static void Main(string[] args) => _ = IRoot.CreateMock();
				                               }

				                               public interface IRoot
				                               {
				                                   IChild Get();
				                               }

				                               public interface IChild { }
				                           }
				                           """, parseOptions),
				unrelated,
			],
			Generator.GetReferences([]),
			new CSharpCompilationOptions(OutputKind.ConsoleApplication));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new MockGenerator().AsSourceGenerator(),], [],
			parseOptions, new TestAnalyzerConfigOptionsProvider(AutoMockingEnabled),
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
			.Because("an edit that changes no mocked type must not regenerate any file");
	}

	[Test]
	public async Task SpellingsOfTheSameType_ShouldShareOneMock()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public interface IService
		                                 {
		                                     IRepository<string?> GetNullable();
		                                     IRepository<(int A, int B)> GetNamedTuple();
		                                     IRepository<(int, int)> GetTuple();
		                                 }

		                                 public interface IRepository<T> { }
		                             }
		                             """, "IService", "IRepository<string>");

		await That(result.Diagnostics).IsEmpty()
			.Because("a second mock of the same runtime type would make its CreateMock ambiguous (CS0121)");
	}

	[Test]
	public async Task WithBuildProperty_ShouldMockReturnTypesTransitively()
	{
		GeneratorResult result = Run("""
		                             using System.Threading.Tasks;

		                             namespace MyCode
		                             {
		                                 public interface IMyInterface1
		                                 {
		                                     IMyInterface2 MyProperty { get; }
		                                 }

		                                 public interface IMyInterface2
		                                 {
		                                     Task<IMyInterface3> MyMethod(int v1);
		                                 }

		                                 public interface IMyInterface3
		                                 {
		                                     IMyInterface1 MyInnerMethod(int v1);
		                                 }
		                             }
		                             """, "IMyInterface1");

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources)
			.ContainsKey("Mock.IMyInterface1.g.cs").And
			.ContainsKey("Mock.IMyInterface2.g.cs").And
			.ContainsKey("Mock.IMyInterface3.g.cs")
			.Because("the cycle back to IMyInterface1 ends the walk");
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("return global::MyCode.IMyInterface3.CreateMock(behavior);");
	}

	[Test]
	public async Task SpellingsOfTheSameType_ShouldBeConfigurableThroughEverySpellingIndependentOfFileOrder()
	{
		const string program = """
		                       #nullable enable
		                       using Mockolate;

		                       namespace MyCode.Driver
		                       {
		                           public class Program
		                           {
		                               public static void Main(string[] args)
		                               {
		                                   MyCode.IRoot root = MyCode.IRoot.CreateMock(MockBehavior.Default.AutoMocking());
		                                   root.GetNullable().Mock.Setup.Get().Returns("x");
		                                   root.GetNotNullable().Mock.Setup.Get().Returns("y");
		                               }
		                           }
		                       }
		                       """;
		const string nullableFile = """
		                            #nullable enable
		                            namespace MyCode
		                            {
		                                public interface IRoot
		                                {
		                                    IRepository<string?> GetNullable();
		                                    IRepository<string> GetNotNullable();
		                                }
		                            }
		                            """;
		const string repositoryFile = """
		                              #nullable enable
		                              namespace MyCode
		                              {
		                                  public interface IRepository<T>
		                                  {
		                                      T Get();
		                                  }
		                              }
		                              """;

		GeneratorResult result = Generator.Run([program, nullableFile, repositoryFile,], [], LanguageVersion.CSharp14,
			AutoMockingEnabled);
		GeneratorResult swapped = Generator.Run([program, repositoryFile, nullableFile,], [], LanguageVersion.CSharp14,
			AutoMockingEnabled);

		await That(result.Diagnostics).IsEmpty()
			.Because("the discovered mock accepts every nullable spelling without CS8620");
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("typeof(global::MyCode.IRepository<string>)");
		await That(swapped.Sources["MockBehaviorExtensions.g.cs"])
			.IsEqualTo(result.Sources["MockBehaviorExtensions.g.cs"]);
	}

	[Test]
	public async Task TypeOnlyImplementedInAddition_ShouldStillBeAutoMocked()
	{
		GeneratorResult result = Generator.Run([
			"""
			using Mockolate;

			namespace MyCode
			{
			    public class Program
			    {
			        public static void Main(string[] args)
			        {
			            _ = IRoot.CreateMock();
			            _ = IChild.CreateMock().Implementing<IGeneric<int>>();
			        }
			    }

			    public interface IRoot
			    {
			        IGeneric<int> Get();
			    }

			    public interface IChild { }

			    public interface IGeneric<out T>
			    {
			        T Value { get; }
			    }
			}
			""",
		], [], LanguageVersion.CSharp14, AutoMockingEnabled);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("return global::MyCode.IGeneric<int>.CreateMock(behavior);")
			.Because("the combination mock of IChild does not make IGeneric<int> a mock on its own");
	}

	[Test]
	public async Task TogglingTheBuildProperty_ShouldUpdateTheOutput()
	{
		CSharpParseOptions parseOptions = new(LanguageVersion.CSharp14);
		CSharpCompilation compilation = CSharpCompilation.Create("TestAssembly",
			[
				CSharpSyntaxTree.ParseText("""
				                           using Mockolate;

				                           namespace MyCode
				                           {
				                               public class Program
				                               {
				                                   public static void Main(string[] args) => _ = IRoot.CreateMock();
				                               }

				                               public interface IRoot
				                               {
				                                   IChild Get();
				                               }

				                               public interface IChild { }
				                           }
				                           """, parseOptions),
			],
			Generator.GetReferences([]),
			new CSharpCompilationOptions(OutputKind.ConsoleApplication));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new MockGenerator().AsSourceGenerator(),], [],
			parseOptions, new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string>()));

		driver = driver.RunGenerators(compilation);
		string[] withoutProperty = GetHintNames(driver);
		driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(AutoMockingEnabled))
			.RunGenerators(compilation);
		string[] withProperty = GetHintNames(driver);
		driver = driver.WithUpdatedAnalyzerConfigOptions(
				new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string>()))
			.RunGenerators(compilation);
		string[] withoutPropertyAgain = GetHintNames(driver);

		await That(withoutProperty).DoesNotContain("Mock.IChild.g.cs");
		await That(withProperty).Contains("Mock.IChild.g.cs");
		await That(withoutPropertyAgain).IsEqualTo(withoutProperty).InAnyOrder();

		static string[] GetHintNames(GeneratorDriver driver)
			=> driver.GetRunResult().Results.SelectMany(result => result.GeneratedSources)
				.Select(source => source.HintName).ToArray();
	}

	[Test]
	public async Task WithoutBuildProperty_ShouldNotReserveTheNameOfTheAutoMockFactory()
	{
		GeneratorResult result = Generator.Run("""
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public class Program
		                                           {
		                                               public static void Main(string[] args) => _ = CreateAutoMock.CreateMock();
		                                           }

		                                           public interface CreateAutoMock { }
		                                       }
		                                       """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.CreateAutoMock.g.cs")
			.Because("the generated output without the build property must not change");
	}

	[Test]
	public async Task WithoutBuildProperty_ShouldGenerateNoAutoMockingCode()
	{
		GeneratorResult result = Generator.Run("""
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public class Program
		                                           {
		                                               public static void Main(string[] args) => _ = IMyInterface1.CreateMock();
		                                           }

		                                           public interface IMyInterface1
		                                           {
		                                               IMyInterface2 Get();
		                                           }

		                                           public interface IMyInterface2 { }
		                                       }
		                                       """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).DoesNotContainKey("Mock.IMyInterface2.g.cs");
		await That(result.Sources["Mock.IMyInterface1.g.cs"]).DoesNotContain("AutoMock");
		await That(result.Sources["MockBehaviorExtensions.g.cs"]).DoesNotContain("AutoMock");
	}

	private static GeneratorResult Run(string types, params string[] mockedTypes)
	{
		string program = $$"""
		                   #nullable enable
		                   using Mockolate;

		                   namespace MyCode.Driver
		                   {
		                       public class Program
		                       {
		                           public static void Main(string[] args)
		                           {
		                               {{string.Join("\n", mockedTypes.Select(type => $"_ = MyCode.{type}.CreateMock();"))}}
		                           }
		                       }
		                   }
		                   """;
		return Generator.Run([program, "#nullable enable\n" + types,], [], LanguageVersion.CSharp14,
			AutoMockingEnabled);
	}
}
