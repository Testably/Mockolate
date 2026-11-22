using System.Threading.Tasks;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.MockDataSourceAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class MockDataSourceAnalyzerTests
{
	private const string DataAttribute = """
	                                     [System.AttributeUsage(System.AttributeTargets.All)]
	                                     public class DataAttribute : System.Attribute, Mockolate.IMockDataSource;

	                                     public sealed class DerivedDataAttribute : DataAttribute;

	                                     [System.AttributeUsage(System.AttributeTargets.All)]
	                                     public sealed class OtherAttribute : System.Attribute;
	                                     """;

	[Test]
	public async Task WhenParametersCanBeCreated_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  #nullable enable
			  namespace MyCode
			  {
			      public interface IDep { int Get(); }
			      public abstract class AbstractDep { public abstract int Get(); }
			      public delegate int Callback(int value);
			      public class Svc(IDep dep, string name);
			      public record Rec(IDep Dep);
			      public struct Value;
			      public enum Kind { A, }

			      public class Tests
			      {
			          [Data]
			          public void Test(IDep a, AbstractDep b, Callback c, Svc d, Rec e, Value f, Kind g, string h,
			              int i, System.Type j, System.Collections.Generic.List<int> k, IDep[] l, IDep? m) { }
			      }

			      {{DataAttribute}}
			  }
			  """
		);

	[Test]
	public async Task WhenParametersCannotBeCreated_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  #nullable enable
			  namespace MyCode
			  {
			      public interface IGen<T> { T Get(); }
			      public abstract record AbstractRecord;
			      public sealed class Generic<T>;
			      public class Ambiguous { public Ambiguous(int a) { } public Ambiguous(string b) { } }
			      [System.Obsolete("old")]
			      public class Old;
			      public class Outer<T> { public class Inner; }

			      public class Tests<TClass>
			      {
			          [Data]
			          private void Test<TMethod>(TMethod {|#0:a|}, IGen<TClass> {|#1:b|}, AbstractRecord {|#2:c|},
			              Generic<int> {|#3:d|}, Ambiguous {|#4:e|}, Hidden {|#5:f|}, Outer<int>.Inner {|#6:g|}) { }

			  #pragma warning disable CS0612
			          [Data]
			          private void Obsolete(Old {|#7:a|}) { }
			  #pragma warning restore CS0612

			          private interface Hidden { }
			      }

			      {{DataAttribute}}
			  }
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(0)
				.WithArguments("a", "TMethod", "it is a type parameter"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(1)
				.WithArguments("b", "MyCode.IGen<TClass>", "its type arguments contain a type parameter"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(2)
				.WithArguments("c", "MyCode.AbstractRecord", "it cannot be mocked (type is a record)"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(3)
				.WithArguments("d", "MyCode.Generic<int>", "it is a generic class"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(4)
				.WithArguments("e", "MyCode.Ambiguous",
					"it has no single accessible constructor with the most parameters that Mockolate can call"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(5)
				.WithArguments("f", "MyCode.Tests<TClass>.Hidden", "it is not accessible from the generated code"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(6)
				.WithArguments("g", "MyCode.Outer<int>.Inner", "it is nested in a generic type"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(7)
				.WithArguments("a", "MyCode.Old", "it is obsolete or experimental")
		);

	[Test]
	public async Task WhenTypeIsNotAccessible_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  namespace MyCode
			  {
			      public class Tests
			      {
			          [Data]
			          private void Test(Hidden {|#0:a|}) { }

			          private interface Hidden { }
			      }

			      {{DataAttribute}}
			  }
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(0)
				.WithArguments("a", "MyCode.Tests.Hidden", "it is not accessible from the generated code")
		);

	[Test]
	public async Task WhenAttributeIsOnConstructorTypeOrDerived_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  namespace MyCode
			  {
			      public abstract record AbstractRecord;

			      public class ConstructorTests
			      {
			          [Data]
			          public ConstructorTests(AbstractRecord {|#0:a|}) { }
			      }

			      [Data]
			      public class TypeTests(AbstractRecord {|#1:b|});

			      public class DerivedTests
			      {
			          [DerivedData]
			          public void Test(AbstractRecord {|#2:c|}) { }
			      }

			      {{DataAttribute}}
			  }
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(0)
				.WithArguments("a", "MyCode.AbstractRecord", "it cannot be mocked (type is a record)"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(1)
				.WithArguments("b", "MyCode.AbstractRecord", "it cannot be mocked (type is a record)"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(2)
				.WithArguments("c", "MyCode.AbstractRecord", "it cannot be mocked (type is a record)")
		);

	[Test]
	public async Task WhenMockedTypeCannotBeMocked_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  namespace MyCode
			  {
			      public abstract class WithParameter { protected WithParameter(int value) { } }
			      public abstract class WithPrivate { private WithPrivate() { } }
			      public abstract class WithProtected { protected WithProtected() { } }
			      public interface IStatic { static abstract int Create(); }
			      [System.Obsolete("old")]
			      public interface IOld { }

			      public class Tests
			      {
			  #pragma warning disable CS0612, CS0618
			          [Data]
			          public void Test(WithParameter {|#0:a|}, WithPrivate {|#1:b|}, WithProtected c, IStatic {|#2:d|},
			              IOld {|#3:e|}) { }
			  #pragma warning restore CS0612, CS0618
			      }

			      {{DataAttribute}}
			  }
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(0)
				.WithArguments("a", "MyCode.WithParameter", "its mock needs an accessible constructor without parameters"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(1)
				.WithArguments("b", "MyCode.WithPrivate", "its mock needs an accessible constructor without parameters"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(2)
				.WithArguments("d", "MyCode.IStatic", "it has static abstract members"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(3)
				.WithArguments("e", "MyCode.IOld", "it is obsolete or experimental")
		);

	[Test]
	public async Task WhenAttributeIsOnPartialMethodOrPrimaryConstructor_ShouldBeFlaggedOnce() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  namespace MyCode
			  {
			      public abstract record AbstractRecord;

			      public partial class Tests
			      {
			          [Data]
			          public partial void Test(AbstractRecord {|#0:a|});

			          public partial void Test(AbstractRecord a) { }
			      }

			      [method: Data]
			      public class PrimaryTests(AbstractRecord {|#1:b|});

			      [Data]
			      public class BothTests
			      {
			          [Data]
			          public BothTests(AbstractRecord {|#2:c|}) { }
			      }

			      {{DataAttribute}}
			  }
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(0)
				.WithArguments("a", "MyCode.AbstractRecord", "it cannot be mocked (type is a record)"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(1)
				.WithArguments("b", "MyCode.AbstractRecord", "it cannot be mocked (type is a record)"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(2)
				.WithArguments("c", "MyCode.AbstractRecord", "it cannot be mocked (type is a record)")
		);

	[Test]
	public async Task WhenAttributeIsNoMockDataSource_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  namespace MyCode
			  {
			      public abstract record AbstractRecord;

			      public class Tests
			      {
			          [Other]
			          public void Test(AbstractRecord a) { }
			      }

			      {{DataAttribute}}
			  }
			  """
		);
}
