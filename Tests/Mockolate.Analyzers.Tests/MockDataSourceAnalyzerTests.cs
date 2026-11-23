using System.Threading.Tasks;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.MockDataSourceAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class MockDataSourceAnalyzerTests
{
	private const string TypeArgumentReason =
		"a type argument or array element is obsolete, experimental, a ref struct, a pointer, nested in a generic type or an interface with static abstract members";

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
				.WithArguments("a", "MyCode.WithParameter", "its mock needs an accessible constructor that can be called without arguments"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(1)
				.WithArguments("b", "MyCode.WithPrivate", "its mock needs an accessible constructor that can be called without arguments"),
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
	public async Task WhenMockCannotImplementAMember_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  namespace MyCode
			  {
			      public abstract class WithInternal { internal WithInternal() { } }
			      public abstract class WithOptional { protected WithOptional(int value = 1) { } }
			      public delegate ref int RefCallback(ref int value);
			      public interface IRefProperty { ref int Value { get; } }
			      public ref struct Buffer { }
			      public interface IRefStruct { Buffer Get(); }
			      public interface ICreateMock { int CreateMock(); }

			      public class Tests
			      {
			          [Data]
			          public void Test(WithInternal {|#0:a|}, WithOptional b, RefCallback {|#1:c|}, IRefProperty {|#2:d|},
			              IRefStruct {|#3:e|}, ICreateMock {|#4:f|}) { }
			      }

			      {{DataAttribute}}
			  }
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(0)
				.WithArguments("a", "MyCode.WithInternal", "its mock needs an accessible constructor that can be called without arguments"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(1)
				.WithArguments("c", "MyCode.RefCallback", "its mock cannot implement the member 'MyCode.RefCallback.Invoke(ref int)'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(2)
				.WithArguments("d", "MyCode.IRefProperty", "its mock cannot implement the member 'MyCode.IRefProperty.Value'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(3)
				.WithArguments("e", "MyCode.IRefStruct", "its mock cannot implement the member 'MyCode.IRefStruct.Get()'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(4)
				.WithArguments("f", "MyCode.ICreateMock", "it has a member named CreateMock, which would hide the generated extension")
		);

	[Test]
	public async Task WhenTypeArgumentOrArrayElementIsUnusable_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  namespace MyCode
			  {
			      public interface IGen<T> { T Get(); }
			      [System.Obsolete("old")]
			      public class Old;
			      public class Outer<T> { public class Inner; }

			      public class Tests
			      {
			  #pragma warning disable CS0612, CS0618
			          [Data]
			          public void Test(IGen<Old> {|#0:a|}, IGen<Outer<int>.Inner> {|#1:b|}, Old[] {|#2:c|}) { }
			  #pragma warning restore CS0612, CS0618
			      }

			      {{DataAttribute}}
			  }
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(0)
				.WithArguments("a", "MyCode.IGen<MyCode.Old>", TypeArgumentReason),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(1)
				.WithArguments("b", "MyCode.IGen<MyCode.Outer<int>.Inner>", TypeArgumentReason),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(2)
				.WithArguments("c", "MyCode.Old[]", TypeArgumentReason)
		);

	[Test]
	public async Task WhenMemberIsSealedOrTheSelectedConstructorIsUsable_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  namespace MyCode
			  {
			      public abstract class Base { public abstract ref int Value { get; } }
			      public abstract class Derived : Base
			      {
			          private int _value;
			          public sealed override ref int Value => ref _value;
			      }
			      public abstract class SelectsUsable
			      {
			          protected SelectsUsable() { }
			          [System.Obsolete("old", true)] protected SelectsUsable(int value = 0) { }
			      }

			      public class Tests
			      {
			          [Data]
			          public void Test(Derived a, SelectsUsable b) { }
			      }

			      {{DataAttribute}}
			  }
			  """
		);

	[Test]
	public async Task WhenTheSelectedConstructorIsObsolete_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  namespace MyCode
			  {
			      public abstract class SelectsObsolete
			      {
			          [System.Obsolete("old", true)] protected SelectsObsolete() { }
			          protected SelectsObsolete(int value = 0) { }
			      }

			      public class Tests
			      {
			          [Data]
			          public void Test(SelectsObsolete {|#0:a|}) { }
			      }

			      {{DataAttribute}}
			  }
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(0)
				.WithArguments("a", "MyCode.SelectsObsolete", "its mock needs an accessible constructor that can be called without arguments")
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
