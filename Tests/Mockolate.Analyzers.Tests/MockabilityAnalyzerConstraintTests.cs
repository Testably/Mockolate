using System.Threading.Tasks;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.MockabilityAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class MockabilityAnalyzerConstraintTests
{
	[Test]
	[Arguments("class, T", "int", "object", "class", "int")]
	[Arguments("class, T", "int?", "object", "class", "int?")]
	[Arguments("class, T", "System.DayOfWeek", "object", "class", "System.DayOfWeek")]
	[Arguments("struct, T", "string", "object", "struct", "string")]
	[Arguments("struct, T", "int[]", "object", "struct", "int[]")]
	[Arguments("struct, T", "System.Array", "object", "struct", "System.Array")]
	[Arguments("struct, T", "int?", "object", "struct", "int?")]
	[Arguments("unmanaged, T", "string", "object", "unmanaged", "string")]
	[Arguments("unmanaged, T", "MyNamespace.MyClass", "object", "unmanaged", "MyNamespace.MyClass")]
	[Arguments("T, V", "int", "string", "int", "string")]
	[Arguments("T, V", "int", "long", "int", "long")]
	[Arguments("T, V", "int[]", "long[]", "int[]", "long[]")]
	[Arguments("T, V", "MyNamespace.MyClass", "string", "MyNamespace.MyClass", "string")]
	[Arguments("T, V", "string[]", "System.Exception[]", "string[]", "System.Exception[]")]
	[Arguments("T, V", "int[][]", "long[][]", "int[][]", "long[][]")]
	[Arguments("T, V", "MyNamespace.MyClass[]", "System.IDisposable[]", "MyNamespace.MyClass[]", "System.IDisposable[]")]
	public async Task WhenClosedTypeArgumentContradictsMethodConstraint_ShouldBeFlagged(string constraint,
		string t, string v, string first, string second) => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix($"MyNamespace.IMyService<{t}, {v}>")}}

			  namespace MyNamespace
			  {
			  	public class MyClass { }

			  	public interface IMyService<T, V>
			  	{
			  		void MyMethod<U>(U value) where U : {{constraint}};
			  	}

			  	public class MyTest
			  	{
			  		public void Test()
			  		{
			  			_ = {|#0:IMyService<{{t}}, {{v}}>|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments($"MyNamespace.IMyService<{t}, {v}>",
					$"the member 'MyNamespace.IMyService<{t}, {v}>.MyMethod<U>(U)' must be implemented, but its type parameter 'U' has the conflicting constraints '{first}' and '{second}'")
		);

	[Test]
	[Arguments("class, T", "object", "object")]
	[Arguments("class, T", "string", "object")]
	[Arguments("class, T", "System.ValueType", "object")]
	[Arguments("class, T", "System.Enum", "object")]
	[Arguments("class, T", "System.IComparable", "object")]
	[Arguments("struct, T", "int", "object")]
	[Arguments("struct, T", "object", "object")]
	[Arguments("struct, T", "System.ValueType", "object")]
	[Arguments("struct, T", "System.Enum", "object")]
	[Arguments("struct, T", "System.IComparable", "object")]
	[Arguments("unmanaged, T", "int", "object")]
	[Arguments("notnull, T", "int?", "object")]
	[Arguments("T, V", "int", "int")]
	[Arguments("T, V", "object", "string")]
	[Arguments("T, V", "string", "object")]
	[Arguments("T, V", "System.IComparable", "string")]
	[Arguments("T, V", "object[]", "string[]")]
	[Arguments("T, V", "System.Array", "int[]")]
	[Arguments("T, V", "string[]", "System.IComparable[]")]
	[Arguments("T, V", "string[][]", "object[][]")]
	[Arguments("T, V", "System.IO.Stream[]", "System.IDisposable[]")]
	[Arguments("T, V", "(int a, int b)", "(int c, int d)")]
	[Arguments("T, V", "System.Collections.Generic.List<(int a, int b)>", "System.Collections.Generic.List<(int c, int d)>")]
	[Arguments("T, V", "System.Action<object>", "System.Action<string>")]
	[Arguments("T, V", "System.Func<string>", "System.Func<object>")]
	[Arguments("T, V", "System.Action<object>[]", "System.Action<string>[]")]
	[Arguments("T, V", "(int a, int b)[]", "(int, int)[]")]
	[Arguments("T, V", "System.DayOfWeek?", "System.Enum")]
	public async Task WhenClosedTypeArgumentSatisfiesMethodConstraint_ShouldNotBeFlagged(string constraint,
		string t, string v) => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix($"MyNamespace.IMyService<{t}, {v}>")}}

			  namespace MyNamespace
			  {
			  	public interface IMyService<T, V>
			  	{
			  		void MyMethod<U>(U value) where U : {{constraint}};
			  	}

			  	public class MyTest
			  	{
			  		public void Test()
			  		{
			  			_ = IMyService<{{t}}, {{v}}>.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Test]
	public async Task WhenAbstractMethodOfBaseClassHasConflictingConstraints_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.MyService")}}

			  namespace MyNamespace
			  {
			  	public abstract class MyBase<T>
			  	{
			  		public abstract void MyMethod<U>(U value) where U : class, T;
			  	}

			  	public abstract class MyService : MyBase<int>
			  	{
			  	}

			  	public class MyTest
			  	{
			  		public void Test()
			  		{
			  			_ = {|#0:MyService|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("MyNamespace.MyService",
					"the member 'MyNamespace.MyBase<int>.MyMethod<U>(U)' must be implemented, but its type parameter 'U' has the conflicting constraints 'class' and 'int'")
		);

	[Test]
	public async Task WhenConflictingAbstractMethodIsAlreadyOverridden_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.MyService<int>")}}

			  namespace MyNamespace
			  {
			  	public abstract class MyBase<T>
			  	{
			  		public abstract void MyMethod<U>(U value) where U : class, T;
			  	}

			  	public abstract class MyService<T> : MyBase<T>
			  	{
			  		public override void MyMethod<U>(U value) { }
			  		public abstract int Other();
			  	}

			  	public class MyTest
			  	{
			  		public void Test()
			  		{
			  			_ = MyService<int>.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Test]
	public async Task WhenInheritedInterfaceMethodHasConflictingConstraints_ShouldBeFlaggedOnImplementing() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IMyService", true)}}

			  namespace MyNamespace
			  {
			  	public interface IMyBase<T>
			  	{
			  		void MyMethod<U>(U value) where U : struct, T;
			  	}

			  	public interface IMyOther : IMyBase<string>
			  	{
			  	}

			  	public interface IMyService
			  	{
			  	}

			  	public class MyTest
			  	{
			  		public void Test()
			  		{
			  			_ = IMyService.CreateMock().Implementing<{|#0:IMyOther|}>();
			  		}
			  	}
			  }
			  """,
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("MyNamespace.IMyOther",
					"the member 'MyNamespace.IMyBase<string>.MyMethod<U>(U)' must be implemented, but its type parameter 'U' has the conflicting constraints 'struct' and 'string'")
		);

	[Test]
	public async Task WhenOnlyVirtualMembersHaveConflictingConstraints_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.MyService<int>")}}
			  {{GeneratedPrefix("MyNamespace.IMyService<int>")}}

			  namespace MyNamespace
			  {
			  	public abstract class MyService<T>
			  	{
			  		public virtual void MyMethod<U>(U value) where U : class, T { }
			  	}

			  	public interface IMyService<T>
			  	{
			  		void MyMethod<U>(U value) where U : class, T { }
			  	}

			  	public class MyTest
			  	{
			  		public void Test()
			  		{
			  			_ = MyService<int>.CreateMock();
			  			_ = IMyService<int>.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	private static string GeneratedPrefix(string fullyQualifiedTypeName, bool includeImplementing = false)
	{
		string implementing = includeImplementing
			? $"public {fullyQualifiedTypeName} Implementing<TInterface>() => default!;"
			: "";
		return $$"""
		         namespace Mockolate
		         {
		         	internal static partial class MockExtensions
		         	{
		         		extension({{fullyQualifiedTypeName}} mock)
		         		{
		         			public static {{fullyQualifiedTypeName}} CreateMock() => default!;
		         			{{implementing}}
		         		}
		         	}
		         }
		         """;
	}
}
