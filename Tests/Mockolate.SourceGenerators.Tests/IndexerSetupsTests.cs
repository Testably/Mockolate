using System.Threading;

namespace Mockolate.SourceGenerators.Tests;

public sealed class IndexerSetupsTests
{
	[Test]
	public async Task GenerateIndexerSetupsForIndexersWithMoreParameters()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Threading;
			     using System.Threading.Tasks;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			_ = IMyInterface.CreateMock();
			             }
			         }

			         public interface IMyInterface
			         {
			             int this[int v1, bool v2, double v3, long v4, CancellationToken v5];
			         }
			     }
			     """, typeof(CancellationToken));

		await That(result.Sources).ContainsKey("IndexerSetups.g.cs");
		await That(result.Sources["IndexerSetups.g.cs"])
			.Contains(
				"internal class IndexerSetup<TValue, T1, T2, T3, T4, T5>(global::Mockolate.MockRegistry mockRegistry, global::Mockolate.Parameters.IParameterMatch<T1> parameter1, global::Mockolate.Parameters.IParameterMatch<T2> parameter2, global::Mockolate.Parameters.IParameterMatch<T3> parameter3, global::Mockolate.Parameters.IParameterMatch<T4> parameter4, global::Mockolate.Parameters.IParameterMatch<T5> parameter5) : global::Mockolate.Setup.IndexerSetup(mockRegistry)");
	}

	[Test]
	public async Task WhenAllIndexersHaveUpTo4Parameters_ShouldNotGenerateIndexerSetups()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Threading;
			     using System.Threading.Tasks;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			_ = IMyInterface.CreateMock();
			             }
			         }

			         public interface IMyInterface
			         {
			             int this[int v1, bool v2, double v3, long v4];
			             int this[int v1, bool v2, double v3];
			             int this[int v1, bool v2];
			             int this[int v1];
			         }
			     }
			     """);

		await That(result.Sources).DoesNotContainKey("IndexerSetups.g.cs");
	}

	[Test]
	public async Task WithComplexIndexer_ShouldOnlyGenerateNecessaryExtensions()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Threading;
			     using System.Threading.Tasks;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			_ = IMyInterface.CreateMock();
			             }
			         }

			         public interface IMyInterface
			         {
			             int this[int v1, bool v2, double v3, long v4, string v5, int v6];
			         }
			     }
			     """);

		await That(result.Sources).ContainsKey("IndexerSetups.g.cs");
		await That(result.Sources["IndexerSetups.g.cs"])
			.Contains("class IndexerSetup<TValue, T1, T2, T3, T4, T5, T6>(").And
			.DoesNotContain("class IndexerSetup<TValue, T1, T2, T3, T4, T5>(").And
			.DoesNotContain("class IndexerSetup<TValue, T1, T2, T3, T4, T5, T6, T7>(");
	}
}
