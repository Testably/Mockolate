using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Mockolate.SourceGenerators.Sources;

namespace Mockolate.SourceGenerators.Tests.Emission;

public sealed class MockBehaviorExtensionsTests
{
	[Test]
	public async Task DefaultValueGenerator_WithValueTuples_ShouldEmitInlineExpressions()
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
	public async Task DefaultValueGenerator_ShouldRegisterIAsyncEnumerables()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Collections.Generic;
			     using System.Threading;
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
			             IAsyncEnumerable<int> AsyncEnumerableOfInt { get; }
			             IAsyncEnumerable<T> GenericAsyncEnumerable<T>(CancellationToken cancellationToken);
			         }
			     }
			     """, typeof(IAsyncEnumerable<>));

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockBehaviorExtensions.g.cs"])
			.Contains("""
			          		public global::System.Collections.Generic.IAsyncEnumerable<T> Generate<T>(global::System.Collections.Generic.IAsyncEnumerable<T> nullValue, params object?[] parameters)
			          """).IgnoringNewlineStyle().And
			.Contains("private sealed class EmptyAsyncEnumerable<T>");
	}

	[Test]
	public async Task DefaultValueGenerator_WithIAsyncEnumerableForwardedByAReference_ShouldRegisterIAsyncEnumerables()
	{
		MetadataReference facade = ExternalAssembly.Compile(
			"[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Collections.Generic.IAsyncEnumerable<>))]",
			"Microsoft.Bcl.AsyncInterfaces");

		GeneratorResult result = Generator.RunWithReferences("""
		                                                     using Mockolate;

		                                                     namespace MyCode
		                                                     {
		                                                         public interface IMyInterface
		                                                         {
		                                                             System.Collections.Generic.IAsyncEnumerable<int> Values { get; }
		                                                         }

		                                                         public class Program
		                                                         {
		                                                             public static void Main(string[] args)
		                                                             {
		                                                     			_ = IMyInterface.CreateMock();
		                                                             }
		                                                         }
		                                                     }
		                                                     """, [facade,]);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockBehaviorExtensions.g.cs"]).Contains("private sealed class EmptyAsyncEnumerable<T>")
			.Because("on .NET Core, Microsoft.Bcl.AsyncInterfaces only forwards the type to the framework");
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task SourceTemplate_ShouldContainTheIAsyncEnumerableDefaultOnlyWhenAvailable(bool includeAsyncEnumerable)
	{
		string source = SourceTemplates.MockBehaviorExtensions(false, includeAsyncEnumerable, null);

		await That(source.Contains("IAsyncEnumerable")).IsEqualTo(includeAsyncEnumerable)
			.Because("IAsyncEnumerable<T> is missing on .NET Framework and .NET Standard 2.0 without Microsoft.Bcl.AsyncInterfaces");
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
			          			foreach (object? parameter in parameters)
			          			{
			          				if (parameter is global::System.Threading.CancellationToken cancellationToken)
			          				{
			          					return generator.Generate(nullValue, value, cancellationToken);
			          				}
			          			}

			          			return global::System.Threading.Tasks.Task.FromResult(value);
			          		}
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		public global::System.Threading.Tasks.Task<T> Generate<T>(global::System.Threading.Tasks.Task<T> nullValue, T value)
			          			=> global::System.Threading.Tasks.Task.FromResult(value);
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		public global::System.Threading.Tasks.Task<T> Generate<T>(global::System.Threading.Tasks.Task<T> nullValue, T value, global::System.Threading.CancellationToken cancellationToken)
			          			=> cancellationToken.IsCancellationRequested
			          				? global::System.Threading.Tasks.Task.FromCanceled<T>(cancellationToken)
			          				: global::System.Threading.Tasks.Task.FromResult(value);
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
			          			foreach (object? parameter in parameters)
			          			{
			          				if (parameter is global::System.Threading.CancellationToken cancellationToken)
			          				{
			          					return generator.Generate(nullValue, value, cancellationToken);
			          				}
			          			}

			          			return global::System.Threading.Tasks.ValueTask.FromResult(value);
			          		}
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		public global::System.Threading.Tasks.ValueTask<T> Generate<T>(global::System.Threading.Tasks.ValueTask<T> nullValue, T value)
			          			=> global::System.Threading.Tasks.ValueTask.FromResult(value);
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		public global::System.Threading.Tasks.ValueTask<T> Generate<T>(global::System.Threading.Tasks.ValueTask<T> nullValue, T value, global::System.Threading.CancellationToken cancellationToken)
			          			=> cancellationToken.IsCancellationRequested
			          				? global::System.Threading.Tasks.ValueTask.FromCanceled<T>(cancellationToken)
			          				: global::System.Threading.Tasks.ValueTask.FromResult(value);
			          """).IgnoringNewlineStyle().And
			.DoesNotContain("global::System.Func<T>");
	}
}
