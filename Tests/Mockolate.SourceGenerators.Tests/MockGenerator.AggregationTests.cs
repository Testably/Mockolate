using System.Linq;
using System.Net.Http;

namespace Mockolate.SourceGenerators.Tests;

public sealed class MockGeneratorAggregationTests
{
	[Test]
	public async Task AllVoidMethods_ShouldNotEmitReturnsThrowsAsyncExtensions()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = IService.CreateMock();
			         }
			     }

			     public interface IService
			     {
			         void DoWork(int v1, int v2, int v3, int v4, int v5);
			         void DoMore(int v1, int v2, int v3, int v4, int v5, int v6);
			     }
			     """);

		await That(result.Sources).DoesNotContainKey("ReturnsThrowsAsyncExtensions.g.cs");
	}

	[Test]
	public async Task AsExtensionsForChainOfTwoAdditionalInterfaces_ShouldEmitBothPairs()
	{
		// Implementing<IA>().Implementing<IB>() must register two pairs in Mock.AsExtensions.g.cs:
		// the (parent, last) pair (IBase ↔ IB) and the (intermediate, last) pair (IA ↔ IB).
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = IBase.CreateMock().Implementing<IA>().Implementing<IB>();
			         }
			     }

			     public interface IBase { }
			     public interface IA { }
			     public interface IB { }
			     """);

		await That(result.Sources).ContainsKey("Mock.AsExtensions.g.cs");
		await That(result.Sources["Mock.AsExtensions.g.cs"])
			.Contains("internal static partial class MockExtensionsForIB")
			.Because("the (IBase ↔ IB) bridge from a typed-as-IBase mock to IMockForIB must be emitted").And
			.Contains("extension(global::Mockolate.Mock.IMockForIBase mock)").And
			.Contains("extension(global::Mockolate.Mock.IMockForIA mock)")
			.Because("the (IA ↔ IB) bridge from a typed-as-IA mock to IMockForIB must be emitted").And
			.Contains("internal static partial class MockExtensionsForIBase")
			.Because("the reverse direction must also be emitted").And
			.Contains("internal static partial class MockExtensionsForIA");
	}

	[Test]
	public async Task HttpClientMock_ShouldEmitHttpResponseMessageFactoryInBehaviorExtensions()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System.Net.Http;
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = HttpClient.CreateMock();
			         }
			     }
			     """, typeof(HttpClient));

		await That(result.Sources).ContainsKey("MockBehaviorExtensions.g.cs");
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("HttpResponseMessageFactory").And
			.Contains("new HttpResponseMessageFactory(global::System.Net.HttpStatusCode.NotImplemented)");
	}

	[Test]
	public async Task NonHttpClientMock_ShouldNotEmitHttpResponseMessageFactoryInBehaviorExtensions()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = IService.CreateMock();
			         }
			     }

			     public interface IService
			     {
			         void DoWork();
			     }
			     """);

		await That(result.Sources).ContainsKey("MockBehaviorExtensions.g.cs");
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.DoesNotContain("HttpResponseMessageFactory");
	}

	[Test]
	public async Task NonVoidMethodWithArity5_ShouldEmitReturnsThrowsAsyncExtensions()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = IService.CreateMock();
			         }
			     }

			     public interface IService
			     {
			         int Compute(int v1, int v2, int v3, int v4, int v5);
			     }
			     """);

		await That(result.Sources).ContainsKey("ReturnsThrowsAsyncExtensions.g.cs");
	}

	[Test]
	public async Task SameRootWithDifferentAdditionalImplementations_ShouldEmitBothCombinations()
	{
		// Two MockClass entries have identical ClassFullName (IBase) but different
		// AdditionalImplementations. The Distinct comparator's per-element tie-break must order
		// them deterministically rather than collapsing them.
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = IBase.CreateMock().Implementing<IExtraA>();
			     		_ = IBase.CreateMock().Implementing<IExtraB>();
			         }
			     }

			     public interface IBase { }
			     public interface IExtraA { }
			     public interface IExtraB { }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources)
			.ContainsKey("Mock.IBase__IExtraA.g.cs").And
			.ContainsKey("Mock.IBase__IExtraB.g.cs");
	}

	[Test]
	public async Task TwoCombinationsWithCollidingCombinedName_ShouldSuffixSecondCombination()
	{
		// Each combination resolves to combinedName "IBase__IExtra" because GetClassNameWithoutDots
		// uses the simple class name (no namespace), so pass 2 of CreateNamedMocks must increment
		// the suffix counter to disambiguate the second combination's emitted file.
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			                 _ = Foo.IBase.CreateMock().Implementing<Foo.IExtra>();
			                 _ = Bar.IBase.CreateMock().Implementing<Bar.IExtra>();
			             }
			         }
			     }

			     namespace Foo
			     {
			         public interface IBase { }
			         public interface IExtra { }
			     }

			     namespace Bar
			     {
			         public interface IBase { }
			         public interface IExtra { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();

		string[] combinationKeys = result.Sources.Keys
			.Where(k => k.Contains("__"))
			.ToArray();

		await That(combinationKeys).Contains("Mock.IBase__IExtra.g.cs").And
			.Contains("Mock.IBase__IExtra_1.g.cs");
	}
}
