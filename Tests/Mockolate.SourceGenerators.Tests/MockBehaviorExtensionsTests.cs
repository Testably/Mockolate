using System.Collections.Generic;

namespace Mockolate.SourceGenerators.Tests;

public sealed class MockBehaviorExtensionsTests
{
	[Test]
	public async Task DefaultValueGenerator_ForValueTuples_ShouldEmitInlineExpressions()
	{
		GeneratorResult result = Generator
			.Run("""
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
			             (int V1, string V2) NamedValueTuple { get; }
			             (int, string, int, string, int, string, int, string) ValueTuple8 { get; }
			             (int, T1, T2) GenericValueTuple<T1, T2>();
			         }
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.IMyInterface.g.cs");
		await That(result.Sources["Mock.IMyInterface.g.cs"])
			.Contains("(b.DefaultValue.Generate(default(int)!), b.DefaultValue.Generate(default(string)!))").And
			.Contains(
				"(b.DefaultValue.Generate(default(int)!), b.DefaultValue.Generate(default(string)!), b.DefaultValue.Generate(default(int)!), b.DefaultValue.Generate(default(string)!), b.DefaultValue.Generate(default(int)!), b.DefaultValue.Generate(default(string)!), b.DefaultValue.Generate(default(int)!), b.DefaultValue.Generate(default(string)!))")
			.And
			.Contains(
				"(this.MockRegistry.Behavior.DefaultValue.Generate(default(int)!), this.MockRegistry.Behavior.DefaultValue.Generate(default(T1)!), this.MockRegistry.Behavior.DefaultValue.Generate(default(T2)!))");

		await That(result.Sources).ContainsKey("MockBehaviorExtensions.g.cs");
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.DoesNotContain("Generate<T1, T2>((T1, T2)").And
			.DoesNotContain("Generate<T1, T2, T3>((T1, T2, T3)").And
			.DoesNotContain("Generate<T1, T2, T3, T4, T5, T6, T7, T8>");
	}

	[Test]
	public async Task DefaultValueGenerator_ShouldRegisterArrays()
	{
		GeneratorResult result = Generator
			.Run("""
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
			             int[] SimpleArray { get; }
			             int[,,][,][] MultiDimensionalArray { get; }
			             T[] GenericArray<T>();
			         }
			     }
			     """);

		await That(result.Sources).ContainsKey("MockBehaviorExtensions.g.cs");
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("""
			          		public T[] Generate<T>(T[] nullValue, params object?[] parameters)
			          			=> global::System.Array.Empty<T>();
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		public T[,] Generate<T>(T[,] nullValue, params object?[] parameters)
			          			=> new T[,] { };
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		public T[,,] Generate<T>(T[,,] nullValue, params object?[] parameters)
			          			=> new T[,,] { };
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		public T[,,,] Generate<T>(T[,,,] nullValue, params object?[] parameters)
			          			=> new T[,,,] { };
			          """).IgnoringNewlineStyle();
	}

	[Test]
	public async Task DefaultValueGenerator_ShouldRegisterIEnumerables()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Collections.Generic;
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
			             IEnumerable<int> EnumerableOfInt { get; }
			             IEnumerable<int[]> EnumerableOfIntArray { get; }
			             IEnumerable<T> GenericEnumerable<T>();
			         }
			     }
			     """, typeof(IEnumerable<>));

		await That(result.Sources).ContainsKey("MockBehaviorExtensions.g.cs");
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("""
			          		public global::System.Collections.Generic.IEnumerable<T> Generate<T>(global::System.Collections.Generic.IEnumerable<T> nullValue, params object?[] parameters)
			          			=> global::System.Array.Empty<T>();
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		public global::System.Collections.Generic.List<T> Generate<T>(global::System.Collections.Generic.List<T> nullValue, params object?[] parameters)
			          			=> new global::System.Collections.Generic.List<T>();
			          """).IgnoringNewlineStyle();
	}

	[Test]
	public async Task DefaultValueGenerator_ShouldRegisterTasks()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
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
			             Task<int> IntTask { get; }
			             Task<int[]> IntArrayTask { get; }
			             Task<T> GenericValueTask<T>();
			         }
			     }
			     """, typeof(Task));

		await That(result.Sources).ContainsKey("MockBehaviorExtensions.g.cs");
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("""
			          		public global::System.Threading.Tasks.Task<T> Generate<T>(global::System.Threading.Tasks.Task<T> nullValue, T value, params object?[] parameters)
			          		{
			          			global::System.Threading.CancellationToken cancellationToken = global::System.Linq.Enumerable.FirstOrDefault(
			          				global::System.Linq.Enumerable.OfType<global::System.Threading.CancellationToken?>(parameters)) ?? global::System.Threading.CancellationToken.None;
			          			if (cancellationToken.IsCancellationRequested)
			          			{
			          				return global::System.Threading.Tasks.Task.FromCanceled<T>(cancellationToken);
			          			}

			          			return global::System.Threading.Tasks.Task.FromResult(value);
			          		}
			          """).IgnoringNewlineStyle().And
			.DoesNotContain("global::System.Func<T>");
	}

	[Test]
	public async Task DefaultValueGenerator_ShouldRegisterValueTasks()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
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
			             ValueTask<int> IntValueTask { get; }
			             ValueTask<int[]> IntArrayValueTask { get; }
			             ValueTask<T> GenericValueTask<T>();
			         }
			     }
			     """, typeof(ValueTask));

		await That(result.Sources).ContainsKey("MockBehaviorExtensions.g.cs");
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("""
			          		public global::System.Threading.Tasks.ValueTask<T> Generate<T>(global::System.Threading.Tasks.ValueTask<T> nullValue, T value, params object?[] parameters)
			          		{
			          			global::System.Threading.CancellationToken cancellationToken = global::System.Linq.Enumerable.FirstOrDefault(
			          				global::System.Linq.Enumerable.OfType<global::System.Threading.CancellationToken?>(parameters)) ?? global::System.Threading.CancellationToken.None;
			          			if (cancellationToken.IsCancellationRequested)
			          			{
			          				return global::System.Threading.Tasks.ValueTask.FromCanceled<T>(cancellationToken);
			          			}

			          			return global::System.Threading.Tasks.ValueTask.FromResult(value);
			          		}
			          """).IgnoringNewlineStyle().And
			.DoesNotContain("global::System.Func<T>");
	}
}
