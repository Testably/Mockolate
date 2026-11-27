using System.Linq;
using System.Threading.Tasks;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.MockDataSourceAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class MockDataSourceAnalyzerTests
{
	private const string TypeArgumentReason =
		"a type argument or array element is obsolete, experimental, a ref struct, a pointer, nested in a generic type or an interface with static abstract members";

	private const string DataAttribute = """
	                                     [System.AttributeUsage(System.AttributeTargets.All)]
	                                     public class DataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource;

	                                     public sealed class DerivedDataAttribute : DataAttribute;

	                                     [System.AttributeUsage(System.AttributeTargets.All)]
	                                     public sealed class OtherAttribute : System.Attribute;
	                                     """;

	private const string Markers = """
	                               namespace Mockolate.Integrations
	                               {
	                                   public abstract class AsDefaultParameterAttribute : System.Attribute;
	                                   public abstract class NotSharedParameterAttribute : System.Attribute;
	                                   public sealed class AsDefaultAttribute(params System.Type[] types) : AsDefaultParameterAttribute;
	                                   public sealed class AsDefaultAttribute<T1> : AsDefaultParameterAttribute;
	                                   public sealed class AsDefaultAttribute<T1, T2> : AsDefaultParameterAttribute;
	                                   public sealed class NotSharedAttribute : NotSharedParameterAttribute;
	                                   public abstract class DeferredParameterAttribute : System.Attribute;
	                                   public sealed class DeferredAttribute : DeferredParameterAttribute;
	                                   public abstract class ValueFromParameterAttribute(string memberName) : System.Attribute;
	                                   public sealed class ValueFromAttribute(string memberName) : ValueFromParameterAttribute(memberName);
	                               }
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
	public async Task WhenEveryAttributeSuppliesTheValue_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			#nullable enable
			namespace MyCode
			{
			    public sealed class Generic<T>;

			    public class Tests
			    {
			        [ArgsData(null)]
			        [ArgsData(null, null)]
			        public void Test(Generic<int>? a, Generic<int>? {|#0:b|}) { }
			    }

			    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
			    public sealed class ArgsDataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource
			    {
			        public ArgsDataAttribute(params object?[]? arguments) { }
			    }
			}
			""",
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(0)
				.WithArguments("b", "MyCode.Generic<int>?", "it is a generic class")
		);

	[Test]
	[Arguments("[ArgsData(new object[] { null, null, })]", 2)]
	[Arguments("[ArgsData][ArgsData(null)]", 0)]
	[Arguments("[OtherData(true)]", 0)]
	[Arguments("[ArgsData(null)][OtherData(true)]", 0)]
	[Arguments("[MixedData(true, null, null)]", 0)]
	[Arguments("[TagsData(\"a\", \"b\")]", 0)]
	[Arguments("[ArgsData(new[] { \"a\", \"b\", })]", 2)]
	[Arguments("[ArgsData(new object[] { new[] { \"a\", \"b\", }, })]", 1)]
	[Arguments("[ArgsData(new int[] { 1, 2, })]", 1)]
	[Arguments("[ArrayData(new object[] { null, null, })]", 0)]
	public async Task WhenAttributesSupplySomeValues_ShouldOnlyFlagTheOthers(string attributes, int supplied)
	{
		string[] names = ["a", "b", "c",];
		await Verifier.VerifyAnalyzerAsync(
			$$"""
			  #nullable enable
			  namespace MyCode
			  {
			      public sealed class Generic<T>;

			      public class Tests
			      {
			          {{attributes}}
			          public void Test({{string.Join(", ", names.Select((name, index) => index < supplied ? $"Generic<int>? {name}" : $"Generic<int>? {{|#{index}:{name}|}}"))}}) { }
			      }

			      [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
			      public sealed class ArgsDataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource
			      {
			          public ArgsDataAttribute(params object?[]? arguments) { }
			      }

			      [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
			      public sealed class OtherDataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource
			      {
			          public OtherDataAttribute(bool strict) { }
			      }

			      [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
			      public sealed class MixedDataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource
			      {
			          public MixedDataAttribute(bool strict, params object?[]? arguments) { }
			      }

			      [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
			      public sealed class TagsDataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource
			      {
			          public TagsDataAttribute(params string[] tags) { }
			      }

			      [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
			      public sealed class ArrayDataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource
			      {
			          public ArrayDataAttribute(object?[]? arguments) { }
			      }
			  }
			  """,
			names.Select((name, index) => (name, index)).Where(parameter => parameter.index >= supplied)
				.Select(parameter => Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule)
					.WithLocation(parameter.index).WithArguments(parameter.name, "MyCode.Generic<int>?", "it is a generic class"))
				.ToArray());
	}

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
			      public abstract class WithTie { protected WithTie(int value) { } protected WithTie(string text) { } }
			      public abstract class WithPrivate { private WithPrivate() { } }
			      public abstract class WithProtected { protected WithProtected() { } }
			      public interface IStatic { static abstract int Create(); }
			      [System.Obsolete("old")]
			      public interface IOld { }

			      public class Tests
			      {
			  #pragma warning disable CS0612, CS0618
			          [Data]
			          public void Test(WithParameter a, WithTie {|#0:t|}, WithPrivate {|#1:b|}, WithProtected c, IStatic {|#2:d|},
			              IOld {|#3:e|}) { }
			  #pragma warning restore CS0612, CS0618
			      }

			      {{DataAttribute}}
			  }
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(0)
				.WithArguments("t", "MyCode.WithTie", "its mock needs an accessible constructor that can be called without arguments, or a single one with the most parameters, which CreateMock() fills"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(1)
				.WithArguments("b", "MyCode.WithPrivate", "its mock needs an accessible constructor that can be called without arguments, or a single one with the most parameters, which CreateMock() fills"),
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
			      public abstract class HidingBase { public static int CreateMock() => 0; }
			      public abstract class InheritsHiding : HidingBase { public abstract int Get(); }
			      public abstract class FieldBase { public static int CreateMock; }
			      public abstract class InheritsField : FieldBase { public abstract int Get(); }
			      public abstract class DelegateBase { public System.Func<int>? CreateMock; }
			      public abstract class InheritsDelegate : DelegateBase { public abstract int Get(); }
			      public abstract class EventBase { public event System.Action? CreateMock; }
			      public abstract class InheritsEvent : EventBase { public abstract int Get(); }

			      public class Tests
			      {
			          [Data]
			          public void Test(WithInternal {|#0:a|}, WithOptional b, RefCallback {|#1:c|}, IRefProperty {|#2:d|},
			              IRefStruct {|#3:e|}, ICreateMock {|#4:f|}, InheritsHiding {|#5:g|}, InheritsField h,
			              InheritsDelegate {|#6:i|}, InheritsEvent {|#7:j|}) { }
			      }

			      {{DataAttribute}}
			  }
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(0)
				.WithArguments("a", "MyCode.WithInternal", "its mock needs an accessible constructor that can be called without arguments, or a single one with the most parameters, which CreateMock() fills"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(1)
				.WithArguments("c", "MyCode.RefCallback", "its mock cannot implement the member 'MyCode.RefCallback.Invoke(ref int)'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(2)
				.WithArguments("d", "MyCode.IRefProperty", "its mock cannot implement the member 'MyCode.IRefProperty.Value'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(3)
				.WithArguments("e", "MyCode.IRefStruct", "its mock cannot implement the member 'MyCode.IRefStruct.Get()'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(4)
				.WithArguments("f", "MyCode.ICreateMock", "it has a member named CreateMock, which would hide the generated extension"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(5)
				.WithArguments("g", "MyCode.InheritsHiding", "it has a member named CreateMock, which would hide the generated extension"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(6)
				.WithArguments("i", "MyCode.InheritsDelegate", "it has a member named CreateMock, which would hide the generated extension"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(7)
				.WithArguments("j", "MyCode.InheritsEvent", "it has a member named CreateMock, which would hide the generated extension")
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
				.WithArguments("a", "MyCode.SelectsObsolete", "its mock needs an accessible constructor that can be called without arguments, or a single one with the most parameters, which CreateMock() fills")
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

	[Test]
	public async Task WhenDeferredFuncCreatesATypeThatCannotBeCreated_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  #nullable enable
			  namespace MyCode
			  {
			      using System;
			      using Mockolate.Integrations;

			      public interface IDep { int Get(); }
			      public sealed class Generic<T>;
			      public class Svc(IDep dep);

			      public class Tests
			      {
			          [Data]
			          public void Test([Deferred] Func<Generic<int>> {|#0:a|}, Func<Generic<int>> b) { }

			          [Data]
			          public void Valid([Deferred] Func<Svc> a, [Deferred] Func<IDep> b, [Deferred] Func<int> c) { }
			      }

			      {{DataAttribute}}
			  }

			  {{Markers}}
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.UnsupportedParameterRule).WithLocation(0)
				.WithArguments("a", "MyCode.Generic<int>", "it is a generic class")
		);

	[Test]
	public async Task WhenDeferredParameterIsNoFuncOrAlsoAsDefaultOrNotShared_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  #nullable enable
			  namespace MyCode
			  {
			      using System;
			      using Mockolate.Integrations;

			      public interface IDep { int Get(); }

			      public class Tests
			      {
			          [Data]
			          public void Test([Deferred] IDep {|#0:a|}, [Deferred] Func<int, IDep> {|#1:b|},
			              [Deferred, AsDefault] Func<IDep> {|#2:c|}, [Deferred, NotShared] Func<IDep> {|#3:d|}) { }

			          [Other]
			          public void NoDataSource([Deferred] IDep a) { }
			      }

			      {{DataAttribute}}
			  }

			  {{Markers}}
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.DeferredParameterRule).WithLocation(0)
				.WithArguments("a", "is not a Func<T>"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.DeferredParameterRule).WithLocation(1)
				.WithArguments("b", "is not a Func<T>"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.DeferredParameterRule).WithLocation(2)
				.WithArguments("c", "is also marked [AsDefault]"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.DeferredParameterRule).WithLocation(3)
				.WithArguments("d", "is also marked [NotShared], which has no effect")
		);

	[Test]
	public async Task WhenValueFromNamesAValidMember_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  #nullable enable
			  namespace MyCode
			  {
			      using Mockolate.Integrations;

			      public interface IDep { int Get(); }
			      public sealed class Generic<T>;
			      public sealed class Rule { public required string Code { get; init; } }

			      public class BaseTests
			      {
			          protected static IDep InheritedDep => null!;
			      }

			      public class Tests : BaseTests
			      {
			          [Data]
			          public void Test([ValueFrom(nameof(CreateRule))] Rule a, [ValueFrom(nameof(GenericValue))] Generic<int> b,
			              [ValueFrom(nameof(InheritedDep))] IDep c, [ValueFrom(nameof(Number))] int? d,
			              [ValueFrom(nameof(Boxed))] object e, [ValueFrom(nameof(CreateRule))] Rule? f,
			              [AsDefault, ValueFrom(nameof(InheritedDep))] IDep g,
			              [NotShared, ValueFrom(nameof(GenericValue))] Generic<int> h) { }

			          private static Rule CreateRule() => new() { Code = "a", };
			          private static Rule CreateRule(int value) => new() { Code = "b", };
			          private static Generic<int> GenericValue => new();
			          private static int Number => 1;
			          private static int Boxed => 1;
			      }

			      {{DataAttribute}}
			  }

			  {{Markers}}
			  """
		);

	[Test]
	public async Task WhenValueFromNamesAnInvalidMember_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  #nullable enable
			  namespace MyCode
			  {
			      using System;
			      using Mockolate.Integrations;

			      public interface IDep { int Get(); }

			      public class BaseTests
			      {
			          private static IDep PrivateBaseDep => null!;
			          protected static IDep PrivateGetterDep { private get => null!; set { } }
			          protected static IDep HiddenDep => null!;
			      }

			      public class Tests : BaseTests
			      {
			          [Data]
			          public void Test([ValueFrom("Missing")] IDep {|#0:a|}, [ValueFrom(nameof(Field))] IDep {|#1:b|},
			              [ValueFrom(nameof(Instance))] IDep {|#2:c|}, [ValueFrom(nameof(Text))] IDep {|#3:d|},
			              [ValueFrom(nameof(WithParameter))] IDep {|#4:e|}, [ValueFrom("PrivateBaseDep")] IDep {|#5:f|},
			              [ValueFrom(nameof(Nothing))] IDep {|#6:g|}, [ValueFrom(nameof(Dep)), Deferred] Func<IDep> {|#7:h|},
			              [ValueFrom("PrivateGetterDep")] IDep {|#8:i|}, [ValueFrom(nameof(HiddenDep))] IDep {|#9:j|}) { }

			          private new IDep HiddenDep => null!;

			          [Other]
			          public void NoDataSource([ValueFrom("Missing")] IDep a) { }

			          public static readonly IDep Field = null!;
			          private IDep Instance => null!;
			          private static string Text => "";
			          private static IDep WithParameter(int value) => null!;
			          private static void Nothing() { }
			          private static IDep Dep => null!;
			      }

			      {{DataAttribute}}
			  }

			  {{Markers}}
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.ValueFromParameterRule).WithLocation(0)
				.WithArguments("a", "names no static property or parameterless static method 'Missing' of 'MyCode.Tests'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.ValueFromParameterRule).WithLocation(1)
				.WithArguments("b", "names the field 'Field' of 'MyCode.Tests'; use a property or method, so that each test case gets a new value"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.ValueFromParameterRule).WithLocation(2)
				.WithArguments("c", "names 'Instance', which is not static"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.ValueFromParameterRule).WithLocation(3)
				.WithArguments("d", "names 'Text' of type 'string', which is not assignable to 'MyCode.IDep'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.ValueFromParameterRule).WithLocation(4)
				.WithArguments("e", "names no static property or parameterless static method 'WithParameter' of 'MyCode.Tests'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.ValueFromParameterRule).WithLocation(5)
				.WithArguments("f", "names no static property or parameterless static method 'PrivateBaseDep' of 'MyCode.Tests'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.ValueFromParameterRule).WithLocation(6)
				.WithArguments("g", "names 'Nothing' of type 'void', which is not assignable to 'MyCode.IDep'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.ValueFromParameterRule).WithLocation(7)
				.WithArguments("h", "is also marked [Deferred]"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.ValueFromParameterRule).WithLocation(8)
				.WithArguments("i", "names no static property or parameterless static method 'PrivateGetterDep' of 'MyCode.Tests'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.ValueFromParameterRule).WithLocation(9)
				.WithArguments("j", "names 'HiddenDep', which is not static")
		);

	[Test]
	public async Task WhenValueFromParameterGetsAGivenArgument_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  #nullable enable
			  namespace MyCode
			  {
			      using Mockolate.Integrations;

			      public class Tests
			      {
			          [ArgsData(1)]
			          [ArgsData]
			          public void Test([ValueFrom("Missing")] int {|#0:a|}, [ValueFrom(nameof(Number))] int b) { }

			          private static int Number => 1;
			      }

			      [ArgsData(1)]
			      public class ClassTests
			      {
			          public ClassTests([ValueFrom(nameof(Number))] int {|#1:a|}, [ValueFrom(nameof(Number))] int b) { }

			          private static int Number => 1;
			      }

			      [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Class, AllowMultiple = true)]
			      public sealed class ArgsDataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource
			      {
			          public ArgsDataAttribute(params object?[]? arguments) { }
			      }
			  }

			  {{Markers}}
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.ValueFromParameterRule).WithLocation(0)
				.WithArguments("a", "gets a given argument of the attribute, which replaces the value"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.ValueFromParameterRule).WithLocation(1)
				.WithArguments("a", "gets a given argument of the attribute, which replaces the value")
		);

	[Test]
	public async Task WhenAsDefaultParametersHaveTheSameTypeOrAreNotShared_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  #nullable enable
			  namespace MyCode
			  {
			      using Mockolate.Integrations;

			      public interface IDep { }

			      public class Tests
			      {
			          [Data]
			          public void Test([AsDefault] IDep a, [AsDefault] IDep? {|#0:b|}, [AsDefault, NotShared] int {|#1:c|}) { }

			          [Data]
			          public void Valid([AsDefault] IDep a, [AsDefault] int b, [AsDefault] int? c, [NotShared] IDep d) { }

			          [Other]
			          public void NoDataSource([AsDefault] IDep a, [AsDefault] IDep b) { }
			      }

			      {{DataAttribute}}
			  }

			  {{Markers}}
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.AsDefaultParameterRule).WithLocation(0)
				.WithArguments("b", "is used for 'MyCode.IDep' like the [AsDefault] parameter 'a'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.AsDefaultParameterRule).WithLocation(1)
				.WithArguments("c", "is also marked [NotShared]")
		);

	[Test]
	public async Task WhenAsDefaultParametersShareADeclaredType_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  #nullable enable
			  namespace MyCode
			  {
			      using Mockolate.Integrations;

			      public interface IDep { }
			      public interface IOther { }
			      public sealed class Fake : IDep, IOther { }

			      public class Tests
			      {
			          [Data]
			          public void SameDeclaredType([AsDefault(typeof(IDep))] Fake a, [AsDefault<IOther, IDep>] Fake {|#0:b|}) { }

			          [Data]
			          public void DeclaredTypeOfAnother([AsDefault] IDep a, [AsDefault<IDep>] Fake {|#1:b|}) { }

			          [Data]
			          public void SameOwnType([AsDefault] Fake a, [AsDefault(typeof(IDep))] Fake {|#2:b|}) { }

			          [Data]
			          public void Valid([AsDefault(typeof(IDep))] Fake a, [AsDefault<IOther>] IOther? b, IDep c) { }
			      }

			      {{DataAttribute}}
			  }

			  {{Markers}}
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.AsDefaultParameterRule).WithLocation(0)
				.WithArguments("b", "is used for 'MyCode.Fake' like the [AsDefault] parameter 'a'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.AsDefaultParameterRule).WithLocation(1)
				.WithArguments("b", "is used for 'MyCode.IDep' like the [AsDefault] parameter 'a'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.AsDefaultParameterRule).WithLocation(2)
				.WithArguments("b", "is used for 'MyCode.Fake' like the [AsDefault] parameter 'a'")
		);

	[Test]
	public async Task WhenAsDefaultParameterIsNotAssignableToADeclaredType_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  #nullable enable
			  namespace MyCode
			  {
			      using Mockolate.Integrations;

			      public interface IDep { }
			      public sealed class Fake : IDep { }

			      public class Tests
			      {
			          [Data]
			          public void Test([AsDefault(typeof(IDep), typeof(string))] Fake {|#0:a|}, [AsDefault<IDep>] int {|#1:b|},
			              [AsDefault<long?>] int {|#2:c|}, [AsDefault<System.IComparable>] int? {|#3:d|},
			              [AsDefault(typeof(IDep), null)] Fake {|#4:e|}) { }

			          [Data]
			          public void Valid([AsDefault(typeof(IDep), typeof(object))] Fake a,
			              [AsDefault<System.IComparable>] int b, [AsDefault(typeof(long?))] long c,
			              [AsDefault(null)] string d) { }

			          [Data]
			          public void ValidNullable([AsDefault<object>] int? a) { }

			          [Data]
			          public void ValidNullableValueType([AsDefault<System.ValueType>] int? a) { }

			          [Data]
			          public void ValidForeignAttribute([AsDefault, Other.Foreign(typeof(string))] IDep a) { }
			      }

			      {{DataAttribute}}
			  }

			  namespace Other
			  {
			      public abstract class AsDefaultParameterAttribute : System.Attribute;

			      public sealed class ForeignAttribute : AsDefaultParameterAttribute
			      {
			          public ForeignAttribute(System.Type type) { }
			      }
			  }

			  {{Markers}}
			  """,
			Verifier.Diagnostic(MockDataSourceAnalyzer.AsDefaultParameterRule).WithLocation(0)
				.WithArguments("a", "is not assignable to 'string'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.AsDefaultParameterRule).WithLocation(1)
				.WithArguments("b", "is not assignable to 'MyCode.IDep'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.AsDefaultParameterRule).WithLocation(2)
				.WithArguments("c", "is not assignable to 'long?'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.AsDefaultParameterRule).WithLocation(3)
				.WithArguments("d", "is not assignable to 'System.IComparable'"),
			Verifier.Diagnostic(MockDataSourceAnalyzer.AsDefaultParameterRule).WithLocation(4)
				.WithArguments("e", "is not assignable to 'null'")
		);
}
