using System.Threading;

namespace Mockolate.SourceGenerators.Tests;

public sealed class MethodSetupsTests
{
	[Test]
	public async Task GenerateMethodSetupsForMethodsWithMoreParameters()
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
			     			_ = IMyOutermostInterface.CreateMock();
			             }
			         }

			         public interface IMyOutermostInterface : IMyOuterInterface;
			         public interface IMyOuterInterface : IMyInterface;
			         public interface IMyInterface
			         {
			             bool MyMethod1(int v1, bool v2, double v3, long v4, CancellationToken v5);
			             void MyMethod2(int v1, bool v2, double v3, long v4, CancellationToken v5);
			         }
			     }
			     """, typeof(DateTime), typeof(Task), typeof(CancellationToken));

		await That(result.Sources).ContainsKey("MethodSetups.g.cs");
		await That(result.Sources["MethodSetups.g.cs"])
			.Contains(
				"internal abstract class ReturnMethodSetup<TReturn, T1, T2, T3, T4, T5> : global::Mockolate.Setup.MethodSetup")
			.And
			.Contains(
				"internal abstract class VoidMethodSetup<T1, T2, T3, T4, T5> : global::Mockolate.Setup.MethodSetup");
	}

	[Test]
	public async Task WhenAllMethodsHaveUpTo4Parameters_ShouldNotGenerateMethodSetups()
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
			             Task<int> MyMethod(int v1, bool v2, double v3, long v4);
			             void MyVoidMethod(int v1, bool v2, double v3, long v4);
			         }
			     }
			     """, typeof(DateTime), typeof(Task), typeof(CancellationToken));

		await That(result.Sources).DoesNotContainKey("MethodSetups.g.cs");
	}

	[Test]
	[Arguments(
		"""
		string ToString();
		bool Equals(object? obj);
		int GetHashCode();
		""",
		"public override string ToString()",
		"public override int GetHashCode()",
		"public override bool Equals(object? obj)")]
	[Arguments(
		"""
		bool Equals(object obj);
		int GetHashCode();
		""",
		"public override bool Equals(object obj)",
		"public override int GetHashCode()")]
	[Arguments(
		"""
		string ToString();
		int GetHashCode();
		""",
		"public override string ToString()",
		"public override int GetHashCode()")]
	[Arguments(
		"""
		string ToString();
		bool Equals(object obj);
		""",
		"public override string ToString()",
		"public override bool Equals(object obj)")]
	[Arguments(
		"""
		string ToString();
		""",
		"public override string ToString()")]
	[Arguments(
		"""
		bool Equals(object obj);
		""",
		"public override bool Equals(object obj)")]
	[Arguments(
		"""
		bool Equals(object? obj);
		""",
		"public override bool Equals(object? obj)")]
	[Arguments(
		"""
		int GetHashCode();
		""",
		"public override int GetHashCode()")]
	public async Task WhenImplementingObjectMethods_ShouldUseSpecialInterfaces(string methods, params string[] expectedTypes)
	{
		GeneratorResult result = Generator
			.Run($$"""
			       using System;
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
			               {{methods}}
			           }
			       }
			       """);

		await That(result.Sources).ContainsKey("Mock.IMyInterface.g.cs");
		foreach (string expectedType in expectedTypes)
		{
			await That(result.Sources["Mock.IMyInterface.g.cs"]).Contains(expectedType);
		}
	}

	[Test]
	public async Task WithComplexMethodReturningAValue_ShouldOnlyGenerateNecessaryExtensions()
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
			             int MyMethod(int v1, bool v2, double v3, long v4, string v5, CancellationToken v6);
			         }
			     }
			     """, typeof(DateTime), typeof(Task), typeof(CancellationToken));

		await That(result.Sources).ContainsKey("MethodSetups.g.cs");
		await That(result.Sources["MethodSetups.g.cs"])
			.Contains("class ReturnMethodSetup<TReturn, T1, T2, T3, T4, T5, T6>").And
			.DoesNotContain("class VoidMethodSetup<T1, T2, T3, T4, T5, T6>").And
			.DoesNotContain("class ReturnMethodSetup<TReturn, T1, T2, T3, T4, T5>").And
			.DoesNotContain("class ReturnMethodSetup<TReturn, T1, T2, T3, T4, T5, T6, T7>");
	}

	[Test]
	public async Task WithComplexMethodReturningVoid_ShouldOnlyGenerateNecessaryExtensions()
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
			             void MyMethod(int v1, bool v2, double v3, long v4, string v5, CancellationToken v6);
			         }
			     }
			     """, typeof(DateTime), typeof(Task), typeof(CancellationToken));

		await That(result.Sources).ContainsKey("MethodSetups.g.cs");
		await That(result.Sources["MethodSetups.g.cs"])
			.Contains("class VoidMethodSetup<T1, T2, T3, T4, T5, T6>").And
			.DoesNotContain("class ReturnMethodSetup<TReturn, T1, T2, T3, T4, T5, T6>").And
			.DoesNotContain("class VoidMethodSetup<T1, T2, T3, T4, T5>").And
			.DoesNotContain("class VoidMethodSetup<T1, T2, T3, T4, T5, T6, T7>");
	}
}
