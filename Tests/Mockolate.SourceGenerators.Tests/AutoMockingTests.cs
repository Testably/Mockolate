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
