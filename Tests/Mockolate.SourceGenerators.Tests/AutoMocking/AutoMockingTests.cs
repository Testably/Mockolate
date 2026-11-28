using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace Mockolate.SourceGenerators.Tests.AutoMocking;

public sealed class AutoMockingTests
{
	private static readonly Dictionary<string, string> AutoMockingEnabled = new();

	private static readonly Dictionary<string, string> AutoMockingSkipped = new()
	{
		["build_property.MockolateSkipAutoMocking"] = "true",
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
	public async Task ConstructorDependency_ShouldDiscoverTheMocksItReturns()
	{
		GeneratorResult result = Generator.Run([
			"""
			#nullable enable
			using Mockolate;

			namespace MyCode
			{
			    public interface IChild
			    {
			        int Value { get; }
			    }

			    public interface IRoot
			    {
			        IChild Child { get; }
			    }

			    public class Service(IRoot root)
			    {
			        public IRoot Root { get; } = root;
			    }

			    public class Program
			    {
			        public static void Main(string[] args)
			        {
			            _ = Service.CreateMock();
			        }
			    }
			}
			""",
		], [], LanguageVersion.CSharp14, AutoMockingEnabled);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources)
			.ContainsKey("Mock.IRoot.g.cs").And
			.ContainsKey("Mock.IChild.g.cs");
	}

	[Test]
	public async Task MockDataSourceParameter_ShouldDiscoverTheMocksItReturns()
	{
		GeneratorResult result = Generator.Run([
			"""
			#nullable enable
			using System;
			using Mockolate;

			namespace MyCode
			{
			    public interface IChild
			    {
			        int Value { get; }
			    }

			    public interface IRoot
			    {
			        IChild Child { get; }
			    }

			    [AttributeUsage(AttributeTargets.Method)]
			    public sealed class DataAttribute : Attribute, Mockolate.Internals.IMockDataSource;

			    public static class Tests
			    {
			        [Data]
			        public static IRoot Test(IRoot root) => root;

			        public static void Main(string[] args)
			        {
			        }
			    }
			}
			""",
		], [], LanguageVersion.CSharp14, AutoMockingEnabled);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources)
			.ContainsKey("Mock.IRoot.g.cs").And
			.ContainsKey("Mock.IChild.g.cs");
	}

	[Test]
	public async Task DerivedLoggerFactory_ShouldAlsoMockTheLogger()
	{
		GeneratorResult result = Generator.Run([
			"""
			#nullable enable
			using Mockolate;

			namespace MyCode
			{
			    public class Program
			    {
			        public static void Main(string[] args)
			        {
			            _ = IServices.CreateMock();
			        }
			    }
			}
			""",
			"""
			#nullable enable
			using Microsoft.Extensions.Logging;

			namespace MyCode
			{
			    public interface IMyLoggerFactory : ILoggerFactory
			    {
			    }

			    public interface IServices
			    {
			        IMyLoggerFactory LoggerFactory { get; }
			    }
			}
			""",
		], [], LanguageVersion.CSharp14, AutoMockingEnabled, typeof(ILoggerFactory));

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources)
			.ContainsKey("Mock.IMyLoggerFactory.g.cs").And
			.ContainsKey("Mock.ILogger.g.cs").And
			.ContainsKey("Logging.g.cs");
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
	public async Task IncludedNamespaces_ShouldWinOverExcludedNamespaces()
	{
		GeneratorResult result = Generator.Run([
			"""
			using Mockolate;

			namespace MyCode
			{
			    public class Program
			    {
			        public static void Main(string[] args) => _ = IRoot.CreateMock();
			    }

			    public interface IRoot
			    {
			        Sdk.IClient GetClient();
			        Sdk.Storage.IBlob GetBlob();
			        Sdk.Storage.Blobs.IContainer GetContainer();
			    }
			}

			namespace Sdk
			{
			    public interface IClient { }
			}

			namespace Sdk.Storage
			{
			    public interface IBlob { }
			}

			namespace Sdk.Storage.Blobs
			{
			    public interface IContainer { }
			}
			""",
		], [], LanguageVersion.CSharp14, new Dictionary<string, string>
		{
			["build_property.MockolateAutoMockingExcludedNamespaces"] = "Sdk",
			["build_property.MockolateAutoMockingIncludedNamespaces"] = " Other ; Sdk.Storage. ; Sdk.Storage",
		});

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources)
			.DoesNotContainKey("Mock.IClient.g.cs").And
			.ContainsKey("Mock.IBlob.g.cs").And
			.ContainsKey("Mock.IContainer.g.cs")
			.Because("an included namespace (also with a trailing dot) and its sub-namespaces win over the excluded namespace");
	}

	[Test]
	public async Task IncludedFrameworkNamespaces_ShouldBeMocked()
	{
		MetadataReference external = ExternalAssembly.Compile("""
		                                                      namespace System.IO.Abstractions;

		                                                      public interface IFile
		                                                      {
		                                                          bool Exists(string path);
		                                                      }

		                                                      public interface IFileSystem
		                                                      {
		                                                          IFile File { get; }
		                                                      }
		                                                      """);

		GeneratorResult result = Generator.RunWithReferences("""
		                                                     using System;
		                                                     using Mockolate;

		                                                     namespace MyCode;

		                                                     public interface IMyInterface
		                                                     {
		                                                         System.IO.Abstractions.IFileSystem GetFileSystem();
		                                                         IServiceProvider GetServiceProvider();
		                                                     }

		                                                     public class Program
		                                                     {
		                                                         public static void Main(string[] args) => _ = IMyInterface.CreateMock();
		                                                     }
		                                                     """, [external,], new Dictionary<string, string>
		{
			["build_property.MockolateAutoMockingExcludedNamespaces"] = "System.IO.Abstractions",
			["build_property.MockolateAutoMockingIncludedNamespaces"] = "System.IO.Abstractions",
		});

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources)
			.ContainsKey("Mock.IFileSystem.g.cs").And
			.ContainsKey("Mock.IFile.g.cs").And
			.DoesNotContainKey("Mock.IServiceProvider.g.cs")
			.Because("the included namespace wins even when the same namespace is excluded");
		await That(result.Sources["Mock.IMyInterface.g.cs"])
			.Contains("AutoMockOr<global::System.IO.Abstractions.IFileSystem>").And
			.DoesNotContain("AutoMockOr<global::System.IServiceProvider>");
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("return global::System.IO.Abstractions.IFile.CreateMock(behavior);");
	}

	[Test]
	public async Task IncludedRootNamespace_ShouldIncludeAllNestedNamespaces()
	{
		GeneratorResult result = Generator.Run([
			"""
			using System;
			using Mockolate;

			namespace MyCode
			{
			    public class Program
			    {
			        public static void Main(string[] args)
			        {
			            _ = IRoot.CreateMock();
			            _ = IServiceProvider.CreateMock();
			        }
			    }

			    public interface IRoot
			    {
			        IServiceProvider GetServiceProvider();
			        Microsoft.Extensions.Something.IService GetService();
			    }
			}

			namespace Microsoft.Extensions.Something
			{
			    public interface IService { }
			}
			""",
		], [], LanguageVersion.CSharp14, new Dictionary<string, string>
		{
			["build_property.MockolateAutoMockingIncludedNamespaces"] = "Microsoft",
		});

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.IService.g.cs");
		await That(result.Sources["Mock.IRoot.g.cs"])
			.Contains("AutoMockOr<global::Microsoft.Extensions.Something.IService>").And
			.DoesNotContain("AutoMockOr<global::System.IServiceProvider>");
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("return global::Microsoft.Extensions.Something.IService.CreateMock(behavior);").And
			.DoesNotContain("global::System.IServiceProvider.CreateMock(behavior)")
			.Because("a mock that the project creates of a framework interface is only used when its namespace is included");
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
	public async Task ExperimentalTypesInSignatures_ShouldNotBeMocked()
	{
		MetadataReference external = ExternalAssembly.Compile("""
		                                                      #pragma warning disable EXP0001
		                                                      namespace Ext;

		                                                      [System.Diagnostics.CodeAnalysis.Experimental("EXP0001")]
		                                                      public class Gadget { }

		                                                      public interface IChild
		                                                      {
		                                                          Gadget Get();
		                                                      }

		                                                      public interface IRoot
		                                                      {
		                                                          IChild Child { get; }
		                                                      }
		                                                      """);

		GeneratorResult result = Generator.RunWithReferences("""
		                                                     using Mockolate;

		                                                     namespace MyCode;

		                                                     public class Program
		                                                     {
		                                                         public static void Main(string[] args) => _ = Ext.IRoot.CreateMock();
		                                                     }
		                                                     """, [external,], AutoMockingEnabled);

		await That(result.Diagnostics).IsEmpty().Because("a mock of IChild would report EXP0001 as an error");
		await That(result.Sources).DoesNotContainKey("Mock.IChild.g.cs");
	}

	[Test]
	public async Task ObsoleteTypesInGenericConstraints_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             #pragma warning disable CS0618
		                             using System;

		                             namespace MyCode
		                             {
		                                 public interface IRoot
		                                 {
		                                     IFactory GetFactory();
		                                 }

		                                 public interface IFactory
		                                 {
		                                     T Make<T>() where T : IOld;
		                                 }

		                                 [Obsolete("old")]
		                                 public interface IOld { }
		                             }
		                             """, "IRoot");

		await That(result.Diagnostics).IsEmpty().Because("a mock of IFactory would repeat the constraint (CS0618)");
		await That(result.Sources).DoesNotContainKey("Mock.IFactory.g.cs");
	}

	[Test]
	public async Task InterfaceMembersImplementedByAClass_ShouldNotBeFollowed()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public interface IHasChildren
		                                 {
		                                     IUnused Unused { get; }
		                                     IUsed Used { get; }
		                                 }

		                                 public class MyService : IHasChildren
		                                 {
		                                     public IUnused Unused => null!;
		                                     public virtual IUsed Used => null!;
		                                 }

		                                 public interface IUnused { }
		                                 public interface IUsed { }
		                             }
		                             """, "MyService");

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.IUsed.g.cs").And
			.DoesNotContainKey("Mock.IUnused.g.cs")
			.Because("the mock cannot override the non-virtual implementation, so it never returns an auto-mock there");
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
	public async Task MockNamedInCreateMock_WhenADiscoveredMockHasTheSameName_ShouldKeepItsName()
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
	public async Task InterfacesWithAMemberNamedCreateMock_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             namespace MyCode
		                             {
		                                 public interface IRoot
		                                 {
		                                     IWithMethod GetWithMethod();
		                                     IWithNestedType GetWithNestedType();
		                                     IInherited GetInherited();
		                                 }

		                                 public interface IWithMethod
		                                 {
		                                     static IWithMethod CreateMock(int value) => null!;
		                                 }

		                                 public interface IWithNestedType
		                                 {
		                                     public class CreateMock { }
		                                 }

		                                 public interface IInherited : IWithMethod { }
		                             }
		                             """, "IRoot");

		await That(result.Diagnostics).IsEmpty()
			.Because("the member would hide the CreateMock extension that the auto-mock factory calls");
		await That(result.Sources.Keys).None().Satisfy(key =>
			key.Contains("IWithMethod") || key.Contains("IWithNestedType") || key.Contains("IInherited"));
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
	public async Task WhenAnUnrelatedFileIsEdited_ShouldNotRegenerateAnyFile()
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
	public async Task TupleElements_ShouldBeMocked()
	{
		GeneratorResult result = Run("""
		                             using System;
		                             using System.Threading.Tasks;

		                             namespace MyCode
		                             {
		                                 public interface IMyInterface1
		                                 {
		                                     Task<(int Stock, IMyInterface2 Value)> MyMethod();
		                                     Tuple<int, Tuple<IMyInterface3>> MyProperty { get; }
		                                 }

		                                 public interface IMyInterface2;

		                                 public interface IMyInterface3;
		                             }
		                             """, "IMyInterface1");

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources)
			.ContainsKey("Mock.IMyInterface2.g.cs").And
			.ContainsKey("Mock.IMyInterface3.g.cs");
		await That(result.Sources["Mock.IMyInterface1.g.cs"])
			.Contains("AutoMockOr<global::MyCode.IMyInterface2>(").And
			.Contains("new global::System.Tuple<int, global::System.Tuple<global::MyCode.IMyInterface3>>(");
	}

	[Test]
	public async Task TupleElements_WhenTheyGetNoAutoMock_ShouldNotBeMocked()
	{
		GeneratorResult result = Run("""
		                             using System;
		                             using System.Threading.Tasks;

		                             namespace MyCode
		                             {
		                                 public interface IMyInterface1
		                                 {
		                                     Task<(int Stock, IMyInterface2 Value)> this[int key] { get; }
		                                     Tuple<int, int, int, int, int, int, int, ValueTuple<IMyInterface3>> MyMethod();
		                                     (int Stock, Task<IMyInterface4> Value) this[string key] { get; }
		                                 }

		                                 public interface IMyInterface2;

		                                 public interface IMyInterface3;

		                                 public interface IMyInterface4;
		                             }
		                             """, "IMyInterface1");

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources)
			.DoesNotContainKey("Mock.IMyInterface2.g.cs").And
			.DoesNotContainKey("Mock.IMyInterface3.g.cs").And
			.ContainsKey("Mock.IMyInterface4.g.cs");
	}

	[Test]
	public async Task ShouldMockReturnTypesTransitively()
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
		                                   MyCode.IRoot root = MyCode.IRoot.CreateMock();
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
	public async Task WhenSkipAutoMockingIsToggled_ShouldUpdateTheOutput()
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
			parseOptions, new TestAnalyzerConfigOptionsProvider(AutoMockingSkipped));

		driver = driver.RunGenerators(compilation);
		string[] skipped = GetHintNames(driver);
		driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(AutoMockingEnabled))
			.RunGenerators(compilation);
		string[] enabled = GetHintNames(driver);
		driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(AutoMockingSkipped))
			.RunGenerators(compilation);
		string[] skippedAgain = GetHintNames(driver);

		await That(skipped).DoesNotContain("Mock.IChild.g.cs");
		await That(enabled).Contains("Mock.IChild.g.cs");
		await That(skippedAgain).IsEqualTo(skipped).InAnyOrder();

		static string[] GetHintNames(GeneratorDriver driver)
			=> driver.GetRunResult().Results.SelectMany(result => result.GeneratedSources)
				.Select(source => source.HintName).ToArray();
	}

	[Test]
	public async Task WhenTheIncludedNamespacesChange_ShouldUpdateTheOutput()
	{
		CSharpParseOptions parseOptions = new(LanguageVersion.CSharp14);
		CSharpCompilation compilation = CreateCompilationWithFrameworkChild(parseOptions);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new MockGenerator().AsSourceGenerator(),], [],
			parseOptions, new TestAnalyzerConfigOptionsProvider(AutoMockingEnabled));

		driver = driver.RunGenerators(compilation);
		Dictionary<string, string> withoutInclude = GetSources(driver);
		driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(
				new Dictionary<string, string>
				{
					["build_property.MockolateAutoMockingIncludedNamespaces"] = "Microsoft.Sdk",
				}))
			.RunGenerators(compilation);
		Dictionary<string, string> withInclude = GetSources(driver);

		await That(withoutInclude).DoesNotContainKey("Mock.IChild.g.cs");
		await That(withoutInclude["Mock.IRoot.g.cs"]).DoesNotContain("AutoMockOr<global::Microsoft.Sdk.IChild>");
		await That(withInclude).ContainsKey("Mock.IChild.g.cs");
		await That(withInclude["Mock.IRoot.g.cs"]).Contains("AutoMockOr<global::Microsoft.Sdk.IChild>");
		await That(withInclude["MockBehaviorExtensions.g.cs"])
			.Contains("return global::Microsoft.Sdk.IChild.CreateMock(behavior);");

		static Dictionary<string, string> GetSources(GeneratorDriver driver)
			=> driver.GetRunResult().Results.SelectMany(result => result.GeneratedSources)
				.ToDictionary(source => source.HintName, source => source.SourceText.ToString());
	}

	[Test]
	public async Task WhenOnlyTheExcludedNamespacesChange_ShouldNotRegenerateAnyFile()
	{
		CSharpParseOptions parseOptions = new(LanguageVersion.CSharp14);
		CSharpCompilation compilation = CreateCompilationWithFrameworkChild(parseOptions);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new MockGenerator().AsSourceGenerator(),], [],
			parseOptions, new TestAnalyzerConfigOptionsProvider(AutoMockingEnabled),
			new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

		driver = driver.RunGenerators(compilation);
		driver = driver.WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(
				new Dictionary<string, string>
				{
					["build_property.MockolateAutoMockingExcludedNamespaces"] = "Other",
				}))
			.RunGenerators(compilation);

		IncrementalStepRunReason[] reasons = driver.GetRunResult().Results.Single().TrackedOutputSteps
			.SelectMany(step => step.Value)
			.SelectMany(run => run.Outputs)
			.Select(output => output.Reason)
			.ToArray();
		await That(reasons).IsNotEmpty().And
			.All().Satisfy(reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)
			.Because("the excluded namespaces only affect the discovery, which finds the same mocks");
	}

	private static CSharpCompilation CreateCompilationWithFrameworkChild(CSharpParseOptions parseOptions)
		=> CSharpCompilation.Create("TestAssembly",
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
				                                   Microsoft.Sdk.IChild Get();
				                               }
				                           }

				                           namespace Microsoft.Sdk
				                           {
				                               public interface IChild { }
				                           }
				                           """, parseOptions),
			],
			Generator.GetReferences([]),
			new CSharpCompilationOptions(OutputKind.ConsoleApplication));

	[Test]
	public async Task WhenSkippingAutoMocking_ShouldNotReserveTheNameOfTheAutoMockFactory()
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
		                                       """, LanguageVersion.CSharp14, AutoMockingSkipped);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.CreateAutoMock.g.cs")
			.Because("without auto-mocking no factory named CreateAutoMock is generated that the mock would clash with");
	}

	[Test]
	public async Task WhenSkippingAutoMocking_ShouldGenerateNoAutoMockingCode()
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
		                                       """, LanguageVersion.CSharp14, AutoMockingSkipped);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).DoesNotContainKey("Mock.IMyInterface2.g.cs");
		await That(result.Sources["Mock.IMyInterface1.g.cs"]).DoesNotContain("CreateAutoMock").And.DoesNotContain("AutoMockOr")
			.And.Contains("global::Mockolate.Internals.AutoMocks.UseFactory(mockBehavior, null);")
			.Because("a behavior of another project may carry its auto-mock factory");
		await That(result.Sources["MockBehaviorExtensions.g.cs"]).DoesNotContain("CreateAutoMock").And
			.DoesNotContain("AutoMocks.UseFactory");
	}

	[Test]
	public async Task InterfaceProperty_WhenSkippingAutoMocking_ShouldNotRecursivelyCreateMocks()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System.Collections.Generic;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			_ = IMyInterface1.CreateMock();
			             }
			         }

			         public interface IMyInterface1
			         {
			             IMyInterface2 MyProperty { get; }
			         }

			         public interface IMyInterface2
			         {
			             IMyInterface3 MyMethod(int v1);
			         }

			         public interface IMyInterface3
			         {
			             IMyInterface1 MyInnerMethod(int v1);
			         }
			     }

			     """, LanguageVersion.CSharp14,
				new Dictionary<string, string> { ["build_property.MockolateSkipAutoMocking"] = "true", });

		await That(result.Sources)
			.ContainsKey("Mock.IMyInterface1.g.cs").And
			.DoesNotContainKey("Mock.IMyInterface2.g.cs").And
			.DoesNotContainKey("Mock.IMyInterface3.g.cs");
	}

	[Test]
	[Arguments("", true)]
	[Arguments("false", true)]
	[Arguments(" True ", false)]
	public async Task WithSkipAutoMockingValue_ShouldOnlySkipAutoMockingForTrue(string value, bool expectAutoMock)
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
		                                       """, LanguageVersion.CSharp14,
			new Dictionary<string, string> { ["build_property.MockolateSkipAutoMocking"] = value, });

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources.ContainsKey("Mock.IMyInterface2.g.cs")).IsEqualTo(expectAutoMock)
			.Because("a project without the property gets an empty value, and only true skips auto-mocking");
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
