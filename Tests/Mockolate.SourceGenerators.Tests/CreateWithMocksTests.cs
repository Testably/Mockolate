using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Tests;

public sealed class CreateWithMocksTests
{
	[Test]
	public async Task DependencySpelledWithOtherNullableAnnotations_ShouldShareTheMockWithoutWarningsInUserCode()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       using System;
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public interface IGen<T>
		                                           {
		                                               T Get();
		                                           }

		                                           public class Svc(IGen<string?> gen)
		                                           {
		                                               public IGen<string?> Gen { get; } = gen;
		                                           }

		                                           public class Svc2(IGen<string> gen)
		                                           {
		                                               public IGen<string> Gen { get; } = gen;
		                                           }

		                                           [AttributeUsage(AttributeTargets.Method)]
		                                           public sealed class DataAttribute : Attribute, IMockDataSource;

		                                           public class Program
		                                           {
		                                               [Data]
		                                               public static object Test(Svc2 sut, IGen<string> gen) => sut;

		                                               public static void Main(string[] args)
		                                               {
		                                                   Svc.CreateWithMocks(out SvcMocks mocks);
		                                                   mocks.Gen.Mock.Setup.Get();
		                                               }
		                                           }
		                                       }
		                                       """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources.Keys.Count(key => key.StartsWith("Mock.IGen", StringComparison.Ordinal)))
			.IsEqualTo(1);
	}

	[Test]
	public async Task SmallIntegralDefaults_ShouldCompile()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public interface IDep
		                                           {
		                                               int Get();
		                                           }

		                                           public class Svc(IDep dep, byte b = 1, sbyte sb = -1, short s = 2, ushort u = 3, char c = 'x', float f = 1.5f)
		                                           {
		                                               public object[] All { get; } = [dep, b, sb, s, u, c, f,];
		                                           }

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
	}

	[Test]
	public async Task NullableObliviousNullDefault_ShouldCompile()
	{
		GeneratorResult result = Generator.Run("""
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public interface IDep
		                                           {
		                                               int Get();
		                                           }

		                                       #nullable disable
		                                           public class Svc(IDep dep, string name = null, object value = null)
		                                           {
		                                               public object[] All { get; } = [dep, name, value,];
		                                           }
		                                       #nullable enable

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
	}

	[Test]
	public async Task NullableObliviousParameter_ShouldAcceptNullAndFallBackToDefault()
	{
		GeneratorResult result = Generator.Run("""
		                                       using System;
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                       #nullable disable
		                                           public class Svc(string name)
		                                           {
		                                               public string Name { get; } = name;
		                                           }
		                                       #nullable enable

		                                           [AttributeUsage(AttributeTargets.Method)]
		                                           public sealed class DataAttribute : Attribute, IMockDataSource;

		                                           public class Program
		                                           {
		                                               [Data]
		                                               public static object Test(Svc sut) => sut;

		                                               public static void Main(string[] args)
		                                               {
		                                                   _ = Svc.CreateWithMocks(out _, null);
		                                               }
		                                           }
		                                       }
		                                       """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"]).Contains("(string)v0 : default");
	}

	[Test]
	public async Task FileLocalClass_ShouldNotGetCreateWithMocks()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public interface IDep
		                                           {
		                                               int Get();
		                                           }

		                                           file class Svc(IDep dep)
		                                           {
		                                               public IDep Dep { get; } = dep;
		                                           }

		                                           public class Program
		                                           {
		                                               public static void Main(string[] args)
		                                               {
		                                                   _ = Svc.CreateWithMocks(out _);
		                                               }
		                                           }
		                                       }
		                                       """);

		await That(result.Sources).DoesNotContainKey("MockDataSources.g.cs");
	}

	[Test]
	public async Task ContainerOfReferencedAssembly_ShouldNotRenameTheContainer()
	{
		MetadataReference reference = ExternalAssembly.Compile("""
		                                                       namespace MyCode
		                                                       {
		                                                           internal sealed class SvcMocks
		                                                           {
		                                                           }
		                                                       }
		                                                       """);
		GeneratorResult result = Generator.RunWithReferences("""
		                                                     #nullable enable
		                                                     using Mockolate;

		                                                     namespace MyCode
		                                                     {
		                                                         public interface IDep
		                                                         {
		                                                             int Get();
		                                                         }

		                                                         public class Svc(IDep dep)
		                                                         {
		                                                             public IDep Dep { get; } = dep;
		                                                         }

		                                                         public class Program
		                                                         {
		                                                             public static void Main(string[] args)
		                                                             {
		                                                                 _ = Svc.CreateWithMocks(out SvcMocks mocks);
		                                                             }
		                                                         }
		                                                     }
		                                                     """, [reference,]);

		await That(result.Diagnostics).IsEmpty();
	}

	[Test]
	public async Task DependencyMock_ShouldNotRenameAMockOfACreateMockCall()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       using Mockolate;

		                                       namespace A
		                                       {
		                                           public interface IDep
		                                           {
		                                               int Get();
		                                           }

		                                           public class Svc(IDep dep)
		                                           {
		                                               public IDep Dep { get; } = dep;
		                                           }
		                                       }

		                                       namespace B
		                                       {
		                                           public interface IDep
		                                           {
		                                               int Get();
		                                           }

		                                           public class Program
		                                           {
		                                               public static void Main(string[] args)
		                                               {
		                                                   _ = A.Svc.CreateWithMocks(out _);
		                                                   _ = B.IDep.CreateMock();
		                                               }
		                                           }
		                                       }
		                                       """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["Mock.IDep.g.cs"]).Contains("extension(global::B.IDep mock)");
		await That(result.Sources["Mock.IDep_1.g.cs"]).Contains("global::A.IDep");
	}

	[Test]
	public async Task LoggerProviderDependency_ShouldBeRequired()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       using Microsoft.Extensions.Logging;
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public class Svc(ILogger<Svc> logger, ILoggerProvider provider)
		                                           {
		                                               public object[] All { get; } = [logger, provider,];
		                                           }

		                                           public class Program
		                                           {
		                                               public static void Main(string[] args)
		                                               {
		                                                   _ = Svc.CreateWithMocks(out _, null!);
		                                               }
		                                           }
		                                       }
		                                       """, typeof(Microsoft.Extensions.Logging.ILogger));

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).DoesNotContainKey("Mock.ILoggerProvider.g.cs");
	}

	[Test]
	public async Task MockedTypeNamedCreateWithMocks_ShouldNotClashWithTheExtension()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public interface CreateWithMocks
		                                           {
		                                               int Get();
		                                           }

		                                           public class Svc(CreateWithMocks dep)
		                                           {
		                                               public CreateWithMocks Dep { get; } = dep;
		                                           }

		                                           public class Program
		                                           {
		                                               public static void Main(string[] args)
		                                               {
		                                                   _ = MyCode.CreateWithMocks.CreateMock();
		                                                   _ = Svc.CreateWithMocks(out _);
		                                               }
		                                           }
		                                       }
		                                       """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("MockDataSources.g.cs");
	}

	[Test]
	public async Task NestedNamesWithUnderscores_ShouldGetDistinctContainers()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public interface IDep
		                                           {
		                                               int Get();
		                                           }

		                                           public static class A_
		                                           {
		                                               public class B(IDep dep)
		                                               {
		                                                   public IDep Dep { get; } = dep;
		                                               }
		                                           }

		                                           public static class A
		                                           {
		                                               public class _B(IDep dep)
		                                               {
		                                                   public IDep Dep { get; } = dep;
		                                               }
		                                           }

		                                           public class Program
		                                           {
		                                               public static void Main(string[] args)
		                                               {
		                                                   _ = A_.B.CreateWithMocks(out A___0BMocks first);
		                                                   _ = A._B.CreateWithMocks(out A_0__BMocks second);
		                                               }
		                                           }
		                                       }
		                                       """);

		await That(result.Diagnostics).IsEmpty();
	}

	[Test]
	public async Task ClassInFrameworkNamespace_ShouldNotGetCreateWithMocks()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public class Program
		                                           {
		                                               public static void Main(string[] args)
		                                               {
		                                                   _ = System.Text.StringBuilder.CreateWithMocks(out _);
		                                               }
		                                           }
		                                       }
		                                       """);

		await That(result.Sources).DoesNotContainKey("MockDataSources.g.cs");
	}
}
