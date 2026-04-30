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
				.WithArguments("MyNamespace.MyRecord", "type is a record")
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
				.WithArguments("MyNamespace.MySealedClass", "type is sealed")
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
}
