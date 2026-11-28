using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests.SpecialCases;

public sealed class UnsatisfiableConstraintTests
{
	[Test]
	[Arguments("class, T", "int", "object", LanguageVersion.CSharp14)]
	[Arguments("struct, T", "string", "object", LanguageVersion.CSharp14)]
	[Arguments("unmanaged, T", "string", "object", LanguageVersion.CSharp14)]
	[Arguments("struct, T", "int?", "object", LanguageVersion.CSharp14)]
	[Arguments("T, V", "int", "string", LanguageVersion.CSharp14)]
	[Arguments("T, V", "string[]", "System.Exception[]", LanguageVersion.CSharp14)]
	[Arguments("T, V", "string[]", "string[,]", LanguageVersion.CSharp14)]
	[Arguments("T, V", "System.Text.StringBuilder[]", "System.IDisposable[]", LanguageVersion.CSharp14)]
#if NET11_0_OR_GREATER
	[Arguments("class, T", "int", "object", LanguageVersion.CSharp15)]
	[Arguments("struct, T", "string", "object", LanguageVersion.CSharp15)]
	[Arguments("unmanaged, T", "string", "object", LanguageVersion.CSharp15)]
	[Arguments("struct, T", "int?", "object", LanguageVersion.CSharp15)]
	[Arguments("T, V", "int", "string", LanguageVersion.CSharp15)]
#endif
	public async Task WhenClosedTypeArgumentContradictsAbstractMethodConstraint_ShouldNotGenerateMock(
		string constraint, string t, string v, LanguageVersion languageVersion)
	{
		GeneratorResult result = Generator.Run($$"""
		                                         using Mockolate;

		                                         namespace MyCode;

		                                         public class Program
		                                         {
		                                             public static void Main(string[] args)
		                                             {
		                                                 _ = IMyService<{{t}}, {{v}}>.CreateMock();
		                                                 _ = MyService<{{t}}, {{v}}>.CreateMock();
		                                             }
		                                         }

		                                         public interface IMyService<T, V>
		                                         {
		                                             void MyMethod<U>(U value) where U : {{constraint}};
		                                         }

		                                         public abstract class MyService<T, V>
		                                         {
		                                             public abstract void MyMethod<U>(U value) where U : {{constraint}};
		                                         }
		                                         """, languageVersion, null);

		await That(result.Diagnostics).IsEmpty()
			.Because("no implementation of the method compiles, so the generator must not emit one (CS0455)");
		await That(result.Sources.Keys).None().Satisfy(key => key.StartsWith("Mock.IMyService", StringComparison.Ordinal) ||
		                                                       key.StartsWith("Mock.MyService", StringComparison.Ordinal));
	}

	[Test]
	[Arguments("class, T", "System.Enum", "object")]
	[Arguments("struct, T", "System.ValueType", "object")]
	[Arguments("T, V", "System.IO.Stream[]", "System.IDisposable[]")]
	[Arguments("T, V", "(int a, int b)", "(int c, int d)")]
	[Arguments("T, V", "System.Action<object>", "System.Action<string>")]
	[Arguments("T, V", "System.Action<object>[]", "System.Action<string>[]")]
	[Arguments("T, V", "(int a, int b)[]", "(int, int)[]")]
	[Arguments("T, V", "System.DayOfWeek?", "System.Enum")]
	public async Task WhenClosedTypeArgumentSatisfiesAbstractMethodConstraint_ShouldGenerateMock(
		string constraint, string t, string v)
	{
		GeneratorResult result = Generator.Run($$"""
		                                         using Mockolate;

		                                         namespace MyCode;

		                                         public class Program
		                                         {
		                                             public static void Main(string[] args)
		                                             {
		                                                 _ = IMyService<{{t}}, {{v}}>.CreateMock();
		                                             }
		                                         }

		                                         public interface IMyService<T, V>
		                                         {
		                                             void MyMethod<U>(U value) where U : {{constraint}};
		                                         }
		                                         """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources.Keys.Any(key => key.StartsWith("Mock.IMyService", StringComparison.Ordinal))).IsTrue();
	}

	[Test]
	[Arguments(LanguageVersion.CSharp14)]
#if NET11_0_OR_GREATER
	[Arguments(LanguageVersion.CSharp15)]
#endif
	public async Task WhenClosedTypeArgumentContradictsVirtualMethodConstraint_ShouldSkipOnlyThatMethod(
		LanguageVersion languageVersion)
	{
		GeneratorResult result = Generator.Run("""
		                                       using Mockolate;

		                                       namespace MyCode;

		                                       public class Program
		                                       {
		                                           public static void Main(string[] args)
		                                           {
		                                               _ = IMyService<int>.CreateMock();
		                                               _ = MyService<int>.CreateMock();
		                                           }
		                                       }

		                                       public interface IMyService<T>
		                                       {
		                                           void Unsatisfiable<U>(U value) where U : class, T { }
		                                           void Satisfiable<U>(U value) where U : struct, T;
		                                       }

		                                       public abstract class MyService<T>
		                                       {
		                                           public virtual void Unsatisfiable<U>(U value) where U : class, T { }
		                                           public abstract void Satisfiable<U>(U value) where U : struct, T;
		                                       }
		                                       """, languageVersion, null);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.IMyService_int.g.cs");
		await That(result.Sources).ContainsKey("Mock.MyService_int.g.cs");
		await That(result.Sources["Mock.IMyService_int.g.cs"])
			.DoesNotContain("Unsatisfiable").And
			.Contains("Satisfiable<U>")
			.Because("the default implementation is kept, as no implementation of the method compiles");
		await That(result.Sources["Mock.MyService_int.g.cs"])
			.DoesNotContain("Unsatisfiable").And
			.Contains("Satisfiable<U>")
			.Because("the base implementation is kept, as no override of the method compiles");
	}

	[Test]
	public async Task WhenConflictingAbstractMethodIsAlreadyOverridden_ShouldGenerateMock()
	{
		GeneratorResult result = Generator.Run("""
		                                       using Mockolate;

		                                       namespace MyCode;

		                                       public class Program
		                                       {
		                                           public static void Main(string[] args)
		                                           {
		                                               _ = MyService<int>.CreateMock();
		                                           }
		                                       }

		                                       public abstract class MyBase<T>
		                                       {
		                                           public abstract void MyMethod<U>(U value) where U : class, T;
		                                       }

		                                       public abstract class MyService<T> : MyBase<T>
		                                       {
		                                           public override void MyMethod<U>(U value) { }
		                                           public abstract int Other();
		                                       }
		                                       """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService_int.g.cs");
		await That(result.Sources["Mock.MyService_int.g.cs"]).DoesNotContain("MyMethod")
			.Because("the override cannot be overridden again with the conflicting constraints");
	}

	[Test]
	public async Task WhenAutoMockedInterfaceHasUnsatisfiableDefaultImplementation_ShouldGenerateAutoMock()
	{
		GeneratorResult result = Generator.Run("""
		                                       using Mockolate;

		                                       namespace MyCode;

		                                       public class Program
		                                       {
		                                           public static void Main(string[] args)
		                                           {
		                                               _ = IMyFactory.CreateMock();
		                                           }
		                                       }

		                                       public interface IMyFactory
		                                       {
		                                           IMyService<int> Create();
		                                       }

		                                       public interface IMyService<T>
		                                       {
		                                           void MyMethod<U>(U value) where U : class, T { }
		                                           int Other();
		                                       }
		                                       """, LanguageVersion.CSharp14, null);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.IMyService_int.g.cs");
		await That(result.Sources["Mock.IMyService_int.g.cs"]).DoesNotContain("MyMethod");
	}

	[Test]
	public async Task WhenAutoMockedInterfaceHasUnsatisfiableConstraint_ShouldNotGenerateAutoMock()
	{
		GeneratorResult result = Generator.Run("""
		                                       using Mockolate;

		                                       namespace MyCode;

		                                       public class Program
		                                       {
		                                           public static void Main(string[] args)
		                                           {
		                                               _ = IMyFactory.CreateMock();
		                                           }
		                                       }

		                                       public interface IMyFactory
		                                       {
		                                           IMyService<int> CreateUnsatisfiable();
		                                           IMyService<string> CreateSatisfiable();
		                                       }

		                                       public interface IMyService<T>
		                                       {
		                                           void MyMethod<U>(U value) where U : class, T;
		                                       }
		                                       """, LanguageVersion.CSharp14, null);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.IMyFactory.g.cs").And
			.ContainsKey("Mock.IMyService_string.g.cs").And
			.DoesNotContainKey("Mock.IMyService_int.g.cs");
	}
}
