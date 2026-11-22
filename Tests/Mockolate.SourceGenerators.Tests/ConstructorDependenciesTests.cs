using Mockolate.SourceGenerators.Tests.TestHelpers;

namespace Mockolate.SourceGenerators.Tests;

public sealed class ConstructorDependenciesTests
{
	[Test]
	public async Task ClassWithDependencies_ShouldGenerateTheMocksOfTheDependencies()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       #pragma warning disable CS9113
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public interface IDep { int Get(); }
		                                           public class Svc(IDep dep, System.Collections.Generic.IEnumerable<int> values, string? name, string text);

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
		await That(result.Sources).ContainsKey("Mock.IDep.g.cs");
		await That(result.Sources["Mock.Svc.g.cs"])
			.Contains("global::MyCode.IDep d1 = global::MyCode.IDep.CreateMock(mockRegistry.Behavior);").And
			.Contains("global::System.Collections.Generic.IEnumerable<int> d2 = mockRegistry.Behavior.DefaultValue.Generate(default(global::System.Collections.Generic.IEnumerable<int>)!);").And
			.Contains("string? d3 = default;").And
			.Contains("string d4 = mockRegistry.Behavior.DefaultValue.Generate(default(string)!);");
	}

	[Test]
	public async Task ClassWithParameterlessConstructor_ShouldNotFillTheOtherConstructor()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public interface IDep { int Get(); }
		                                           public class Svc
		                                           {
		                                               public Svc() { }
		                                               public Svc(IDep dep) { }
		                                           }

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
		await That(result.Sources).DoesNotContainKey("Mock.IDep.g.cs");
		await That(result.Sources["Mock.Svc.g.cs"]).DoesNotContain("IDep.CreateMock(");
	}

	[Test]
	public async Task DefaultValuesOfTheFilledConstructor_ShouldCompile()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       #pragma warning disable CS9113
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public enum Kind { None, Some, }
		                                           public interface IDep { int Get(); }
		                                           public class Svc(IDep dep, byte small = 1, float nan = float.NaN, decimal factor = 1.5m,
		                                               Kind kind = Kind.Some, string fallback = null!, int? optional = null, params int[] values);

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
	}

	[Test]
	public async Task ConstructorWithRefParameter_ShouldNotBeFilled()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public interface IDep { int Get(); }
		                                           public class Svc
		                                           {
		                                               public Svc(IDep dep, ref int count, out int result) { result = count; }
		                                               public Svc(IDep dep) { }
		                                           }

		                                           public class Program
		                                           {
		                                               public static void Main(string[] args)
		                                               {
		                                                   _ = Svc.CreateMock();
		                                               }
		                                           }
		                                       }
		                                       """);

		await That(result.Sources["Mock.Svc.g.cs"])
			.Contains("return new global::Mockolate.Mock.Svc(mockRegistry, d1);");
	}

	[Test]
	public async Task DataSourceClass_ShouldBeAClassMockUnlessItCannotBeMocked()
	{
		GeneratorResult result = Generator.Run("""
		                                       #nullable enable
		                                       #pragma warning disable CS9113
		                                       using MyCode;

		                                       namespace MyCode
		                                       {
		                                           public interface IDep { int Get(); }
		                                           public class Open(IDep dep, params int[] values);
		                                           public sealed class Closed(IDep dep);
		                                           public record Rec(IDep Dep);
		                                           public class Overlapping
		                                           {
		                                               public Overlapping(IDep dep, int count) { }
		                                               public Overlapping(IDep dep, string text = "") { }
		                                               public Overlapping(IDep dep, int count, string text) { }
		                                               protected Overlapping(IDep dep, int count, string text, int other = 0) { }
		                                           }
		                                           public class Parameterless { }

		                                           public class Tests
		                                           {
		                                               [Data]
		                                               public void Test(Open open, Closed closed, Rec rec, Overlapping overlapping,
		                                                   Parameterless parameterless) { }

		                                               public static void Main(string[] args) { }
		                                           }

		                                           [System.AttributeUsage(System.AttributeTargets.All)]
		                                           public sealed class DataAttribute : System.Attribute, Mockolate.IMockDataSource;
		                                       }
		                                       """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["MockDataSources.g.cs"])
			.Contains("return global::MyCode.Open.CreateMock(behavior ?? global::Mockolate.MockBehavior.Default, new object?[] { a0, a1, });").And
			.Contains("return new global::MyCode.Closed(").And
			.Contains("return new global::MyCode.Rec(").And
			.Contains("return new global::MyCode.Overlapping(").And
			.Contains("return global::MyCode.Parameterless.CreateMock(behavior ?? global::Mockolate.MockBehavior.Default);");
	}
}
