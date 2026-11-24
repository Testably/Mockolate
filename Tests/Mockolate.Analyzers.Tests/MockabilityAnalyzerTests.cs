using System.Threading.Tasks;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.MockabilityAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class MockabilityAnalyzerTests
{
	[Test]
	public async Task WhenCreateMockIsNotInMockolateNamespace_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace OtherNamespace
			{
				internal static class MockExtensions
				{
					public static MyNamespace.IMyInterface CreateMock(
						this MyNamespace.IMyInterface mock) => default!;
				}
			}

			namespace MyNamespace
			{
				public interface IMyInterface
				{
				}

				public class MyClass
				{
					public void MyTest()
					{
						IMyInterface sut = null!;
						sut.CreateMock();
					}
				}
			}
			"""
		);

	[Test]
	public async Task WhenImplementingAClass_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IMyInterface", true)}}

			  namespace MyNamespace
			  {
			  	public interface IMyInterface
			  	{
			  	}
			  	public class MyImplementingClass
			  	{
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IMyInterface.CreateMock().Implementing<{|#0:MyImplementingClass|}>();
			  		}
			  	}
			  }
			  """,
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("MyNamespace.MyImplementingClass", "You can only implement additional interfaces")
		);

	[Test]
	public async Task WhenInvocationIsNotMemberAccess_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace MyNamespace
			{
				public class MyClass
				{
					public void MyTest()
					{
						System.Action action = () => { };
						action();
					}
				}
			}
			"""
		);

	[Test]
	public async Task WhenMethodNameIsUnrelated_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace MyNamespace
			{
				public class MyClass
				{
					public static int Helper() => 0;

					public void MyTest()
					{
						MyClass.Helper();
					}
				}
			}
			"""
		);

	[Test]
	public async Task WhenMockingADelegate_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("System.Action")}}

			  namespace MyNamespace
			  {
			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			System.Action.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Test]
	public async Task WhenMockingArray_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IMyInterface", true)}}

			  namespace MyNamespace
			  {
			  	public interface IMyInterface
			  	{
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IMyInterface.CreateMock().Implementing<{|#0:IMyInterface[]|}>();
			  		}
			  	}
			  }
			  """,
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("MyNamespace.IMyInterface[]", "type kind 'Array' is not supported")
		);

	[Test]
	public async Task WhenMockingClass_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.MyBaseClass")}}

			  namespace MyNamespace
			  {
			  	public class MyBaseClass
			  	{
			  		public virtual void DoSomething() { }
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			MyBaseClass.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Test]
	public async Task WhenMockingDelegate_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.MyDelegate")}}

			  namespace MyNamespace
			  {
			  	public delegate void MyDelegate();

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			MyDelegate.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Test]
	public async Task WhenMockingEnum_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.MyEnum")}}

			  namespace MyNamespace
			  {
			  	public enum MyEnum
			  	{
			  		Value1,
			  		Value2
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:MyEnum|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("MyNamespace.MyEnum", "type is an enum")
		);

	[Test]
	public async Task WhenMockingGlobalNamespaceType_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("IGlobalInterface")}}

			  public interface IGlobalInterface
			  {
			  	void DoSomething();
			  }

			  namespace MyNamespace
			  {
			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:IGlobalInterface|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("IGlobalInterface", "type is declared in the global namespace")
		);

	[Test]
	public async Task WhenMockingInterface_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IMyInterface")}}

			  namespace MyNamespace
			  {
			  	public interface IMyInterface
			  	{
			  		void DoSomething();
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IMyInterface.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Test]
	public async Task WhenMockingOpenGenericClass_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace Mockolate
			{
				internal static partial class MockExtensionsForIGenericInterface
				{
					extension<T>(MyNamespace.IGenericInterface<T> mock)
					{
						public static MyNamespace.IGenericInterface<T> CreateMock() => default!;
					}
				}
			}

			namespace MyNamespace
			{
				public interface IGenericInterface<T>
				{
				}

				public class MyClass<T>
				{
					public void MyTest()
					{
						IGenericInterface<T>.CreateMock();
					}
				}
			}
			"""
		);

	[Test]
	public async Task WhenMockingRecord_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.MyRecord")}}

			  namespace MyNamespace
			  {
			  	public record MyRecord(int Value);

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:MyRecord|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("MyNamespace.MyRecord", "type is a record; use CreateMock(out ConstructorArguments arguments) to create a real instance with mocked dependencies")
		);

	[Test]
	public async Task WhenMockingSealedClass_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.MySealedClass")}}

			  namespace MyNamespace
			  {
			  	public sealed class MySealedClass
			  	{
			  		public void DoSomething() { }
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:MySealedClass|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("MyNamespace.MySealedClass", "type is sealed; use CreateMock(out ConstructorArguments arguments) to create a real instance with mocked dependencies")
		);

	[Test]
	public async Task WhenMockingStruct_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.MyStruct")}}

			  namespace MyNamespace
			  {
			  	public struct MyStruct
			  	{
			  		public int Value { get; set; }
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:MyStruct|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("MyNamespace.MyStruct", "type is a struct")
		);

	private static string GeneratedPrefix(string fullyQualifiedTypeName, bool includeImplementing = false)
	{
		string simpleName = fullyQualifiedTypeName.Split('.')[^1];
		if (includeImplementing)
		{
			return $$"""
			         namespace Mockolate
			         {
			         	internal static partial class MockExtensionsFor{{simpleName}}
			         	{
			         		extension({{fullyQualifiedTypeName}} mock)
			         		{
			         			public static {{fullyQualifiedTypeName}} CreateMock() => default!;
			         			public {{fullyQualifiedTypeName}} Implementing<TInterface>() => default!;
			         		}
			         	}
			         }
			         """;
		}

		return $$"""
		         namespace Mockolate
		         {
		         	internal static partial class MockExtensionsFor{{simpleName}}
		         	{
		         		extension({{fullyQualifiedTypeName}} mock)
		         		{
		         			public static {{fullyQualifiedTypeName}} CreateMock() => default!;
		         		}
		         	}
		         }
		         """;
	}

	[Test]
	[Arguments("public static int CreateMock;")]
	[Arguments("public void CreateMock() { }")]
	[Arguments("private static void CreateMock() { }")]
	public async Task WhenBaseClassHasMemberNamedCreateMockThatDoesNotHideTheExtension_ShouldNotBeFlagged(string member)
		=> await Verifier.VerifyAnalyzerAsync(
			$$"""
			  using Mockolate;

			  namespace MyNamespace
			  {
			  	public class Base { {{member}} }
			  	public sealed class MySealedClass(int value) : Base;

			  	public class Tests
			  	{
			  		public void Test()
			  		{
			  			_ = MySealedClass.CreateMock(out ConstructorArguments arguments);
			  		}
			  	}
			  }

			  namespace Mockolate
			  {
			  	public static class MockExtensions
			  	{
			  		extension(MyNamespace.MySealedClass)
			  		{
			  			public static MyNamespace.MySealedClass CreateMock(out ConstructorArguments arguments)
			  				=> throw new System.NotImplementedException();
			  		}
			  	}
			  }
			  """);

	[Test]
	[Arguments("public class Factory { public static int CreateMock(out int value, int seed) { value = seed; return seed; } }",
		"out int x")]
	[Arguments("public static class Factory { public static int CreateMock(out int value, int seed) { value = seed; return seed; } }",
		"out int x")]
	[Arguments("public class Factory { public static int CreateMock(out int value) { value = 0; return 0; } }",
		"out string x")]
	[Arguments("public sealed class Factory(int v) { public static int CreateMock(out int value, int seed) { value = seed; return seed; } }",
		"out int x")]
	[Arguments("public class Factory { public static int CreateMock(ref int seed) => seed; }", "out int x")]
	[Arguments("public sealed class Factory(int v) { public static int CreateMock(int seed) => seed; }", "out int x")]
	[Arguments("public class Factory { public static System.Func<int, int>? CreateMock; }", "out int x")]
	[Arguments("public sealed class Factory(int v) { public static dynamic? CreateMock; }", "out int x")]
	public async Task WhenCallingOwnCreateMockWithOutParameterThatDoesNotBind_ShouldNotBeFlagged(string declaration,
		string argument)
		=> await Verifier.VerifyAnalyzerAsync(
			$$"""
			  namespace MyNamespace
			  {
			  	{{declaration}}

			  	public class Tests
			  	{
			  		public void Test()
			  		{
			  			_ = Factory.CreateMock({{argument}});
			  		}
			  	}
			  }
			  """);

	[Test]
	public async Task WhenCreatingSealedClassWithOutParameter_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;

			namespace MyNamespace
			{
				public interface IDependency { }
				public sealed class MySealedClass(IDependency dependency);

				public class Tests
				{
					public void Test()
					{
						_ = MySealedClass.CreateMock(out ConstructorArguments arguments);
					}
				}
			}

			namespace Mockolate
			{
				public static class MockExtensions
				{
					extension(MyNamespace.MySealedClass)
					{
						public static MyNamespace.MySealedClass CreateMock(out ConstructorArguments arguments)
							=> throw new System.NotImplementedException();
					}
				}
			}
			"""
		);

	[Test]
	public async Task WhenCreatingMockableClassWithOutParameter_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;

			namespace MyNamespace
			{
				public class MyClass;

				public class Tests
				{
					public void Test()
					{
						_ = {|#0:MyClass|}.CreateMock(out ConstructorArguments arguments);
					}
				}
			}
			""",
			Verifier.Diagnostic(Rules.MockabilityRule).WithLocation(0)
				.WithArguments("MyNamespace.MyClass",
					"CreateMock(out ...) is only for sealed classes and records; use CreateMock() and Mock.ConstructorArgument<T>()")
		);

	[Test]
	public async Task WhenPassingTheOutArgumentByNameLater_ShouldAnalyzeTheSealedOverload() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;

			namespace MyNamespace
			{
				public class MyClass;

				public class Tests
				{
					public void Test()
					{
						_ = {|#0:MyClass|}.CreateMock(mockBehavior: null, constructorArguments: out ConstructorArguments arguments);
					}
				}
			}
			""",
			Verifier.Diagnostic(Rules.MockabilityRule).WithLocation(0)
				.WithArguments("MyNamespace.MyClass",
					"CreateMock(out ...) is only for sealed classes and records; use CreateMock() and Mock.ConstructorArgument<T>()")
		);

	[Test]
	[Arguments("public abstract record MyType(int Value);", "MyType", "MyNamespace.MyType", "type is static or abstract")]
	[Arguments("public sealed class MyType<T>(T value);", "MyType<int>", "MyNamespace.MyType<int>",
		"type is generic or nested in a generic type")]
	[Arguments("public class Outer<T> { public sealed class MyType(int value); }", "Outer<int>.MyType",
		"MyNamespace.Outer<int>.MyType", "type is generic or nested in a generic type")]
	[Arguments("internal class Outer { private sealed class MyType(int value); public static void Use() => _ = {|#0:MyType|}.CreateMock(out ConstructorArguments arguments); }",
		null, "MyNamespace.Outer.MyType", "type is not accessible from the generated code")]
	[Arguments("[System.Obsolete] public sealed class MyType(int value);", "MyType", "MyNamespace.MyType",
		"type is obsolete or experimental")]
	[Arguments("public interface ICreator { void CreateMock(); } public sealed class MyType(int value) : ICreator { void ICreator.CreateMock() { } }",
		"MyType", "MyNamespace.MyType", "type has a member named CreateMock, which would hide the generated extension")]
	[Arguments("public class Base { public static int CreateMock() => 0; } public sealed class MyType(int value) : Base;",
		"MyType", "MyNamespace.MyType", "type has a member named CreateMock, which would hide the generated extension")]
	[Arguments("public class Base { public System.Func<int>? CreateMock; } public sealed class MyType(int value) : Base;",
		"MyType", "MyNamespace.MyType", "type has a member named CreateMock, which would hide the generated extension")]
	[Arguments("public class Base { public event System.Action? CreateMock; } public sealed class MyType(int value) : Base;",
		"MyType", "MyNamespace.MyType", "type has a member named CreateMock, which would hide the generated extension")]
	[Arguments("public sealed class MyType(int value) { public static int CreateMock() => 0; }",
		"MyType", "MyNamespace.MyType", "type has a member named CreateMock, which would hide the generated extension")]
	[Arguments("public class Base { public static int CreateMock(out int x) { x = 0; return 0; } } public sealed class MyType(int value) : Base;",
		"MyType", "MyNamespace.MyType", "type has a member named CreateMock, which would hide the generated extension")]
	[Arguments("public delegate int Creator(out int x); public class Base { public static Creator? CreateMock; } public sealed class MyType(int value) : Base;",
		"MyType", "MyNamespace.MyType", "type has a member named CreateMock, which would hide the generated extension")]
	[Arguments("public sealed class MyType { public MyType(int value) { } public MyType(string text) { } }", "MyType",
		"MyNamespace.MyType", "type has no single accessible constructor with the most parameters that Mockolate can call")]
	[Arguments("public sealed class MyType(int value) { public required int Other { get; init; } }", "MyType",
		"MyNamespace.MyType", "type has no single accessible constructor with the most parameters that Mockolate can call")]
	public async Task WhenCreatingSealedClassThatCannotBeCreated_ShouldBeFlagged(string declaration, string? receiver,
		string typeName, string reason) => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  using Mockolate;

			  namespace MyNamespace
			  {
			  	{{declaration}}

			  	public class Tests
			  	{
			  		public void Test()
			  		{
			  			{{(receiver is null ? "" : $"_ = {{|#0:{receiver}|}}.CreateMock(out ConstructorArguments arguments);")}}
			  		}
			  	}
			  }
			  """,
			Verifier.Diagnostic(Rules.MockabilityRule).WithLocation(0).WithArguments(typeName, reason)
		);

	[Test]
	public async Task WhenCreatingSealedClassInFrameworkNamespace_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;

			namespace System.Chocolate
			{
				public sealed class MyType(int value);

				public class Tests
				{
					public void Test()
					{
						_ = {|#0:MyType|}.CreateMock(out ConstructorArguments arguments);
					}
				}
			}
			""",
			Verifier.Diagnostic(Rules.MockabilityRule).WithLocation(0)
				.WithArguments("System.Chocolate.MyType", "type is declared in the System, Microsoft or Windows namespace")
		);
}
