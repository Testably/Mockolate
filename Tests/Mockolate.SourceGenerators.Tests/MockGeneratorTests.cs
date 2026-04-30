using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Tests;

public partial class MockGeneratorTests
{
	[Test]
	public async Task SameMethodDifferingOnlyByNullability_ShouldUseExplicitImplementationForConflictingInterface()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     #nullable enable
			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = IInterface1.CreateMock().Implementing<IInterface2>();
			         }
			     }

			     public interface IInterface1
			     {
			         void Method(string? value);
			     }

			     public interface IInterface2
			     {
			         void Method(string value);
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();

		await That(result.Sources).ContainsKey("Mock.IInterface1__IInterface2.g.cs");
		await That(result.Sources["Mock.IInterface1__IInterface2.g.cs"])
			.Contains("public void Method(string? value)").And
			.Contains("void global::MyCode.IInterface2.Method(string value)");
	}

	[Test]
	public async Task SealedClass_ShouldNotBeIncluded()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Threading;
			     using System.Threading.Tasks;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			_ = MyService.CreateMock();
			             }
			         }

			         public sealed class MyService { }
			     }
			     """);

		await ThatAll(
			That(result.Sources.Keys).DoesNotContain("Mock.MyService.g.cs"),
			That(result.Diagnostics).IsEmpty()
		);
	}

	[Test]
	public async Task WhenClassHasConstructorWithParameters_ShouldGenerateTypedCreateMockOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock(42);
			         }
			     }

			     public class MyService
			     {
			         public MyService(int value) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("public static global::MyCode.MyService CreateMock(int value)")
			.IgnoringNewlineStyle().And
			.Contains("=> CreateMock(null, null, new object?[] { value });")
			.IgnoringNewlineStyle().And
			.Contains("public static global::MyCode.MyService CreateMock(global::Mockolate.MockBehavior mockBehavior, int value)")
			.IgnoringNewlineStyle().And
			.Contains("public static global::MyCode.MyService CreateMock(global::System.Action<global::Mockolate.Mock.IMockSetupForMyService> setup, int value)")
			.IgnoringNewlineStyle().And
			.Contains(
				"public static global::MyCode.MyService CreateMock(global::Mockolate.MockBehavior mockBehavior, global::System.Action<global::Mockolate.Mock.IMockSetupForMyService> setup, int value)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenClassHasMultipleConstructors_ShouldGenerateTypedOverloadPerConstructor()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock("foo");
			     		_ = MyService.CreateMock(1, "foo");
			         }
			     }

			     public class MyService
			     {
			         public MyService(string text) { }
			         public MyService(int number, string text) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("public static global::MyCode.MyService CreateMock(string text)")
			.IgnoringNewlineStyle().And
			.Contains("public static global::MyCode.MyService CreateMock(int number, string text)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenClassHasOnlyParameterlessConstructor_ShouldNotEmitAdditionalTypedOverloads()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			     	}
			     }

			     public class MyService { }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("public static global::MyCode.MyService CreateMock()")
			.IgnoringNewlineStyle()
			.Because(
				"the hand-written parameterless CreateMock() overload is still emitted, and no typed overload is added beyond it");
	}

	[Test]
	public async Task WhenConstructorHasCharDefault_ShouldEscapeInTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         public MyService(char c = '\n') { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("CreateMock(char c = '\\n')")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenConstructorHasDecimalDefault_ShouldAppendMSuffixInTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         public MyService(decimal price = 19.95m) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("CreateMock(decimal price = 19.95m)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenConstructorHasEnumDefault_ShouldCastToFullyQualifiedEnumTypeInTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public enum MyKind { Alpha, Beta }

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         public MyService(MyKind kind = MyKind.Beta) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("CreateMock(global::MyCode.MyKind kind = (global::MyCode.MyKind)1)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenConstructorHasFloatDefault_ShouldAppendFSuffixInTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         public MyService(float factor = 3.14f) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("CreateMock(float factor = 3.14f)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenConstructorHasInParameter_ShouldNotEmitTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		int value = 42;
			     		_ = MyService.CreateMock([value]);
			         }
			     }

			     public class MyService
			     {
			         public MyService(in int value) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.DoesNotContain("CreateMock(in int value)")
			.IgnoringNewlineStyle().And
			.DoesNotContain("CreateMock(int value)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenConstructorHasLongAndDoubleDefaults_ShouldPreserveLiteralsInTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         public MyService(long big = 9999999999L, double pi = 3.14) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("CreateMock(long big = 9999999999, double pi = 3.14)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenConstructorHasNullableDecimalDefault_ShouldAppendMSuffixInTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     #nullable enable
			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         public MyService(decimal? price = 19.95m) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("CreateMock(decimal? price = 19.95m)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenConstructorHasNullableReferenceDefaultNull_ShouldEmitNullLiteralInTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     #nullable enable
			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         public MyService(string? text = null) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("CreateMock(string? text = null)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenConstructorHasRefOrParamsParameter_ShouldNotEmitTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock([1, 2, 3]);
			         }
			     }

			     public class MyService
			     {
			         public MyService(params int[] values) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.DoesNotContain("public static global::MyCode.MyService CreateMock(int[] values)")
			.IgnoringNewlineStyle().And
			.DoesNotContain("public static global::MyCode.MyService CreateMock(params int[] values)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenConstructorHasStringDefaultWithQuotes_ShouldEscapeInTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         public MyService(string text = "has \"quotes\"") { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("CreateMock(string text = \"has \\\"quotes\\\"\")")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenConstructorHasValueTypeDefaultNull_ShouldEmitDefaultInTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public struct MyToken { public int Value; }

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         public MyService(MyToken token = default) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("CreateMock(global::MyCode.MyToken token = default)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenConstructorParameterIsMockBehavior_ShouldNotEmitAmbiguousTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock(Mockolate.MockBehavior.Default);
			         }
			     }

			     public class MyService
			     {
			         public MyService(Mockolate.MockBehavior behavior) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		int matches = CreateMockBehaviorSignatureRegex().Count(result.Sources["Mock.MyService.g.cs"]);
		await That(matches).IsEqualTo(1)
			.Because(
				"the hand-written CreateMock(MockBehavior) overload already covers this signature, so a typed single-parameter overload would produce a duplicate definition");
	}

	[Test]
	public async Task WhenConstructorParameterIsUnrelatedAction_ShouldEmitTypedOverload()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;
			     using System;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		Action<string> callback = s => { };
			     		_ = MyService.CreateMock(callback);
			         }
			     }

			     public class MyService
			     {
			         public MyService(Action<string> callback) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("CreateMock(global::System.Action<string> callback)")
			.IgnoringNewlineStyle()
			.Because(
				"Action<string> does not collide with the setup action type (Action<IMockSetupForMyService>)");
	}

	[Test]
	public async Task WhenConstructorParameterTypeIsProtectedInternalNestedType_ShouldEmitConstructor()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock(new MyService.Configuration());
			         }
			     }

			     public class MyService
			     {
			         protected MyService(Configuration configuration) { }
			         protected internal class Configuration { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("CreateMock(global::MyCode.MyService.Configuration configuration)")
			.IgnoringNewlineStyle()
			.Because("the internal half of `protected internal` is visible within the declaring assembly");
	}

	[Test]
	public async Task WhenConstructorParameterTypeIsProtectedNestedType_ShouldNotEmitConstructor()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         public MyService() { }
			         protected MyService(Configuration configuration) { }
			         protected class Configuration { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("public MyService(global::Mockolate.MockRegistry mockRegistry)").IgnoringNewlineStyle().And
			.DoesNotContain("Configuration")
			.Because(
				"a protected nested type is only reachable through inheritance, so naming it on the generated public constructor (CS0051) or in MockExtensionsForMyService (CS0122) would not compile");
	}

	[Test]
	public async Task WhenConstructorParameterTypeIsProtectedNestedTypeArgument_ShouldNotEmitConstructor()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System.Collections.Generic;
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         public MyService() { }
			         protected MyService(List<Configuration> configurations) { }
			         protected MyService(Configuration[] configurations) { }
			         protected class Configuration { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.DoesNotContain("Configuration")
			.Because("an inaccessible type is equally unusable when composed into an array or a type argument");
	}

	[Test]
	public async Task WhenOnlyConstructorHasProtectedNestedParameterType_ShouldNotGenerateMock()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         protected MyService(Configuration configuration) { }
			         protected class Configuration { }
			     }
			     """);

		await That(result.Sources).DoesNotContainKey("Mock.MyService.g.cs")
			.Because("no constructor remains that the generated mock could invoke");
	}

	[Test]
	public async Task WhenConstructorParameterTypeNestsInProtectedTypeArgumentOfContainingType_ShouldNotEmitConstructor()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class Wrapper<T>
			     {
			         public class Inner { }
			     }

			     public class MyService
			     {
			         public MyService() { }
			         protected MyService(Wrapper<Configuration>.Inner inner) { }
			         protected class Configuration { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.DoesNotContain("Configuration")
			.Because(
				"`Wrapper<Configuration>.Inner` carries the type argument on its containing type, so checking only the innermost type's arguments would let the inaccessible type through");
	}

	[Test]
	public async Task WhenConstructorParameterTypeIsInternalInSameAssembly_ShouldEmitConstructor()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock(new Configuration());
			         }
			     }

			     internal class Configuration { }

			     internal class MyService
			     {
			         protected MyService(Configuration configuration) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("CreateMock(global::MyCode.Configuration configuration)")
			.IgnoringNewlineStyle()
			.Because("the generated mock lives in the same assembly, so an internal parameter type is nameable");
	}

	[Test]
	public async Task WhenVirtualMemberSignatureUsesProtectedNestedType_ShouldNotMockMember()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         protected virtual void Consume(Configuration configuration) { }
			         protected virtual Configuration Produce() => new Configuration();
			         protected virtual Configuration Current { get; set; }
			         protected virtual void Constrained<T>() where T : Configuration { }
			         protected class Configuration { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.DoesNotContain("Configuration").And
			.DoesNotContain("Consume").And
			.DoesNotContain("Produce").And
			.DoesNotContain("Current").And
			.DoesNotContain("Constrained")
			.Because(
				"the setup and verify surfaces restate the signature verbatim but do not derive from the mocked type, so a protected nested type would cause CS0122 there");
	}

	[Test]
	public async Task WhenVirtualEventTypeUsesProtectedNestedType_ShouldNotMockEvent()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public class MyService
			     {
			         protected virtual event Handler Changed;
			         protected void Fire() => Changed?.Invoke();
			         protected delegate void Handler();
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.DoesNotContain("Changed").And
			.DoesNotContain("Handler")
			.Because("the raise surface names the delegate type verbatim");
	}

	[Test]
	public async Task WhenAbstractMemberSignatureUsesProtectedNestedType_ShouldNotGenerateMock()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }

			     public abstract class MyService
			     {
			         protected abstract void Consume(Configuration configuration);
			         protected class Configuration { }
			     }
			     """);

		await That(result.Sources).DoesNotContainKey("Mock.MyService.g.cs")
			.Because("the member must be implemented, but the mock cannot restate its signature");
	}

	[Test]
	[Arguments("protected abstract void Consume(Configuration configuration);",
		"protected override void Consume(Configuration configuration) { }")]
	[Arguments("protected abstract Configuration Current { get; set; }",
		"protected override Configuration Current { get; set; }")]
	[Arguments("protected abstract event Handler Changed;",
		"protected override event Handler Changed; protected void Fire() => Changed?.Invoke();")]
	public async Task WhenAbstractMemberWithInaccessibleSignatureIsAlreadyOverridden_ShouldGenerateMock(
		string baseMember, string derivedOverride)
	{
		GeneratorResult result = Generator
			.Run($$"""
			       using Mockolate;

			       namespace MyCode;

			       public class Program
			       {
			           public static void Main(string[] args)
			           {
			       		_ = Derived.CreateMock();
			           }
			       }

			       public abstract class Base
			       {
			           {{baseMember}}
			           protected class Configuration { }
			           protected delegate void Handler();
			       }

			       public class Derived : Base
			       {
			           {{derivedOverride}}
			       }
			       """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.Derived.g.cs")
			.Because("`Derived` already fills the slot, so restating the signature is not the mock's obligation");
		await That(result.Sources["Mock.Derived.g.cs"])
			.DoesNotContain("Configuration").And
			.DoesNotContain("Handler");
	}

	[Test]
	public async Task WhenConstructorsDifferOnlyByNullableValueType_ShouldEmitBothTypedOverloads()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     #nullable enable
			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock(1);
			     		_ = MyService.CreateMock((int?)1);
			         }
			     }

			     public class MyService
			     {
			         public MyService(int value) { }
			         public MyService(int? value) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("public static global::MyCode.MyService CreateMock(int value)")
			.IgnoringNewlineStyle().And
			.Contains("public static global::MyCode.MyService CreateMock(int? value)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenDerivedClassHasBaseClassConstructorParameters_ShouldEmitTypedOverloadForDerived()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = DerivedService.CreateMock("foo");
			         }
			     }

			     public class BaseService
			     {
			         public BaseService(string text) { }
			     }

			     public class DerivedService : BaseService
			     {
			         public DerivedService(string text) : base(text) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.DerivedService.g.cs");
		await That(result.Sources["Mock.DerivedService.g.cs"])
			.Contains("public static global::MyCode.DerivedService CreateMock(string text)")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenGenericClassHasTypedConstructor_ShouldEmitTypedOverloadForClosedType()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService<string>.CreateMock("foo");
			         }
			     }

			     public class MyService<T>
			     {
			         public MyService(T value) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		string generated = string.Concat(result.Sources.Values);
		await That(generated)
			.Contains("CreateMock(string value)")
			.IgnoringNewlineStyle()
			.Because(
				"the mock for the closed generic MyService<string> must carry the typed overload with the substituted type argument");
	}

	[Test]
	public async Task WhenImplementingAdditionalClass_ShouldCreateCombinationMock()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Threading;
			     using System.Threading.Tasks;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			                 _ = MyService.CreateMock()
			                     .Implementing<IMyInterface1>()
			                     .Implementing<IMyInterface2>();
			             }
			         }

			     	public interface IMyInterface1 { }
			     	public class MyService { }
			     	public interface IMyInterface2 { }
			     }
			     """);

		await That(result.Sources.Keys).IsEqualTo([
			"Mock.g.cs",
			"Mock.AsExtensions.g.cs",
			"MockBehaviorExtensions.g.cs",
			"Mock.IMyInterface1.g.cs",
			"Mock.IMyInterface2.g.cs",
			"Mock.MyService.g.cs",
			"Mock.MyService__IMyInterface1.g.cs",
			"Mock.MyService__IMyInterface1__IMyInterface2.g.cs",
		]).InAnyOrder();
	}

	[Test]
	public async Task WhenImplementingAdditionalInterface_WithBaseClassHavingOptionalConstructorParameter_ShouldGenerateTryCastWithDefaultValue()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock().Implementing<IMyInterface>();
			         }
			     }

			     public class MyService
			     {
			         public MyService(int value = 0) { }
			     }

			     public interface IMyInterface
			     {
			         void DoWork();
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();

		await That(result.Sources).ContainsKey("Mock.MyService__IMyInterface.g.cs");
		await That(result.Sources["Mock.MyService__IMyInterface.g.cs"])
			.Contains("static bool TryCastWithDefaultValue<TValue>(object?[] values, int index, TValue defaultValue, global::Mockolate.MockBehavior behavior, out TValue result)")
			.IgnoringNewlineStyle().And
			.Contains("mock.MockRegistry.ConstructorParameters.Length >= 0 && mock.MockRegistry.ConstructorParameters.Length <= 1")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenImplementingAdditionalInterface_WithBaseClassHavingRequiredConstructorParameter_ShouldGenerateTryCast()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock([42]).Implementing<IMyInterface>();
			         }
			     }

			     public class MyService
			     {
			         public MyService(int value) { }
			     }

			     public interface IMyInterface
			     {
			         void DoWork();
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();

		await That(result.Sources).ContainsKey("Mock.MyService__IMyInterface.g.cs");
		await That(result.Sources["Mock.MyService__IMyInterface.g.cs"])
			.Contains("static bool TryCast<TValue>(object?[] values, int index, global::Mockolate.MockBehavior behavior, out TValue result)")
			.IgnoringNewlineStyle().And
			.Contains("No parameterless constructor found for 'MyCode.MyService'")
			.IgnoringNewlineStyle().And
			.Contains("mock.MockRegistry.ConstructorParameters.Length == 1")
			.IgnoringNewlineStyle().And
			.DoesNotContain("TryCastWithDefaultValue")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenMethodContainsMoreThan16Parameters_ShouldAddCustomAction()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Threading;
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
			             void MyMethod(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8, int v9, int v10, int v11, int v12, int v13, int v14, int v15, int v16, int v17);
			         }
			     }
			     """, typeof(DateTime), typeof(Task));

		await That(result.Sources).ContainsKey("ActionFunc.g.cs");
		await That(result.Sources["ActionFunc.g.cs"])
			.Contains(
				"public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, in T16, in T17>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17);")
			.And
			.Contains(
				"public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, in T13, in T14, in T15, in T16, in T17, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17);");
	}

	[Test]
	public async Task WhenNamesConflict_ShouldBeDistinguishable()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Threading;
			     using System.Threading.Tasks;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			_ = IMyInt.CreateMock();
			     			_ = I.MyInt.CreateMock();
			     			_ = IMy<int>.CreateMock();
			             }
			         }

			         public interface IMyInt { }

			         public class I
			         {
			     		public interface MyInt { }
			         }

			         public interface IMy<T> { }
			     }
			     """);

		await ThatAll(
			That(result.Sources.Keys).Contains([
				"Mock.IMyInt.g.cs",
				"Mock.I_MyInt.g.cs",
				"Mock.IMy_int.g.cs",
			]).InAnyOrder().IgnoringCase(),
			That(result.Diagnostics).IsEmpty()
		);
	}

	[Test]
	public async Task WhenNamesConflictForAdditionalClasses_ShouldBeDistinguishable()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Threading;
			     using System.Threading.Tasks;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			_ = I.CreateMock()
			     				.Implementing<IMyInt>()
			     				.Implementing<I.MyInt>()
			     				.Implementing<IMy<int>>();
			             }
			         }

			         public interface IMyInt { }

			         public class I
			         {
			     		public interface MyInt { }
			         }

			         public interface IMy<T> { }
			     }
			     """, typeof(HttpResponseMessage));

		await ThatAll(
			That(result.Sources.Keys).Contains([
				"Mock.I.g.cs",
				"Mock.IMyInt.g.cs",
				"Mock.I_MyInt.g.cs",
				"Mock.IMy_int.g.cs",
				"Mock.I__IMyInt__I_MyInt__IMy_int.g.cs",
				"Mock.I__IMyInt__I_MyInt.g.cs",
				"Mock.I__IMyInt.g.cs",
			]).InAnyOrder().IgnoringCase()
		);
	}

	[Test]
	public async Task WhenNamesConflictForAdditionalClassesInDifferentNamespaces_ShouldBeDistinguishable()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Threading;
			     using System.Threading.Tasks;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			_ = IMyService.CreateMock()
			     				.Implementing<MyCode.IMyInt>();
			     			_ = IMyService.CreateMock()
			     				.Implementing<OtherNamespace.IMyInt>();
			             }
			         }

			         public interface IMyInt { }

			         public interface IMyService { }
			     }
			     namespace OtherNamespace
			     {
			         public interface IMyInt { }
			     }
			     """, typeof(HttpResponseMessage));

		await ThatAll(
			That(result.Sources.Keys).Contains([
				"Mock.IMyService.g.cs",
				"Mock.IMyInt.g.cs",
				"Mock.IMyInt_1.g.cs",
				"Mock.IMyService__IMyInt.g.cs",
				"Mock.IMyService__IMyInt_1.g.cs",
			]).InAnyOrder(),
			That(result.Sources["Mock.IMyService__IMyInt.g.cs"])
				.Contains("global::MyCode.IMyInt, IMockForIMyInt, IMockSetupForIMyInt, IMockVerifyForIMyInt,"),
			That(result.Sources["Mock.IMyService__IMyInt_1.g.cs"])
				.Contains("global::OtherNamespace.IMyInt, IMockForIMyInt_1, IMockSetupForIMyInt_1, IMockVerifyForIMyInt_1,"),
			That(result.Diagnostics).IsEmpty()
		);
	}

	[Test]
	public async Task WhenOneCtorStartsWithMockBehaviorAndAnotherOmitsIt_ShouldNotEmitDuplicateTypedOverloads()
	{
		// Two constructors that could produce the same C# signature after the generator's
		// mockBehavior-prefixed overload expansion:
		//   - ctor A: (MockBehavior, int)      → typed overload: CreateMock(MockBehavior, int)
		//   - ctor B: (int) + mockBehavior    → typed overload: CreateMock(MockBehavior, int)
		// The dedup key is built in emitted-signature order (prefix first, then ctor params), so
		// the second emission must be skipped to avoid a duplicate method definition.
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock(MockBehavior.Default, 42);
			         }
			     }

			     public class MyService
			     {
			         public MyService(Mockolate.MockBehavior behavior, int value) { }
			         public MyService(int value) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		int matches = CreateMockBehaviorIntSignatureRegex().Count(result.Sources["Mock.MyService.g.cs"]);
		await That(matches).IsEqualTo(1);
	}

	[Test]
	public async Task WhenSameCombinationIsDeclaredTwice_ShouldDeduplicateToSingleEmittedFile()
	{
		// Same root mock with the same .Implementing<T>() chain in different call sites must
		// dedupe. The Distinct comparator iterates AdditionalImplementations; identical
		// sequences must collapse to a single emitted combination file.
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = IMyService.CreateMock().Implementing<IMyExtra>();
			     		_ = IMyService.CreateMock().Implementing<IMyExtra>();
			         }
			     }

			     public interface IMyService { }
			     public interface IMyExtra { }
			     """);

		await That(result.Diagnostics).IsEmpty();

		string[] combinationKeys = result.Sources.Keys
			.Where(k => k.Contains("__"))
			.ToArray();
		await That(combinationKeys).IsEqualTo(["Mock.IMyService__IMyExtra.g.cs",]).InAnyOrder();
	}

	[Test]
	public async Task WhenSameMethodInTwoInterfacesDiffersOnlyInRefKind_ShouldUseExplicitImplementation()
	{
		// Two interfaces declare a method with the same name and parameter type but different
		// RefKind (ref vs in). The ContainingTypeIndependentMethodEqualityComparer must treat
		// these as different, so the second method is emitted as an explicit implementation
		// rather than collapsed onto the first.
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = IInterface1.CreateMock().Implementing<IInterface2>();
			         }
			     }

			     public interface IInterface1
			     {
			         void Method(ref int value);
			     }

			     public interface IInterface2
			     {
			         void Method(in int value);
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();

		await That(result.Sources).ContainsKey("Mock.IInterface1__IInterface2.g.cs");
		await That(result.Sources["Mock.IInterface1__IInterface2.g.cs"])
			.Contains("public void Method(ref int value)").And
			.Contains("void global::MyCode.IInterface2.Method(in int value)");
	}

	[Test]
	public async Task WhenSameMockIsDeclaredTwice_ShouldDeduplicateToSingleEmittedFile()
	{
		// Two identical Mock.Create<IFoo>() declarations must collapse to a single emitted
		// Mock.IFoo.g.cs. The dedup pipeline relies on the Distinct comparator returning 0 when
		// ClassFullName and AdditionalImplementations match across the two MockClass entries.
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = IMyInterface.CreateMock();
			     		_ = IMyInterface.CreateMock();
			         }
			     }

			     public interface IMyInterface
			     {
			         void DoWork();
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();

		string[] interfaceMockKeys = result.Sources.Keys
			.Where(k => k.StartsWith("Mock.IMyInterface", StringComparison.Ordinal))
			.ToArray();
		await That(interfaceMockKeys).IsEqualTo(["Mock.IMyInterface.g.cs",]).InAnyOrder();
	}

	[Test]
	public async Task WhenSameTypeImplementsDifferentCombinationsOfSameInterface_ShouldGenerateAllCombinations()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Threading;
			     using System.Threading.Tasks;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			_ = IBaseInterface.CreateMock().Implementing<ICommonInterface>();
			     			_ = IBaseInterface.CreateMock().Implementing<ICommonInterface>().Implementing<IAdditionalInterface1>();
			     			_ = IBaseInterface.CreateMock().Implementing<IAdditionalInterface2>().Implementing<ICommonInterface>();
			     			_ = IBaseInterface.CreateMock().Implementing<IAdditionalInterface1>().Implementing<IAdditionalInterface2>().Implementing<ICommonInterface>();
			             }
			         }

			         public interface IBaseInterface { }
			         public interface ICommonInterface { }
			         public interface IAdditionalInterface1 { }
			         public interface IAdditionalInterface2 { }
			     }
			     """);

		await That(result.Sources)
			.ContainsKey("Mock.IBaseInterface__ICommonInterface.g.cs").And
			.ContainsKey("Mock.IBaseInterface__ICommonInterface__IAdditionalInterface1.g.cs").And
			.ContainsKey("Mock.IBaseInterface__IAdditionalInterface2__ICommonInterface.g.cs").And
			.ContainsKey("Mock.IBaseInterface__IAdditionalInterface1__IAdditionalInterface2__ICommonInterface.g.cs").And
			.ContainsKey("Mock.IBaseInterface__IAdditionalInterface1__IAdditionalInterface2.g.cs").And
			.ContainsKey("Mock.IBaseInterface__IAdditionalInterface1.g.cs");
	}

	[Test]
	public async Task WhenTypedCreateMockOverloadIsEmitted_ConstructorCrefShouldResolve()
	{
		const string expectedCref = "global::MyCode.MyService(int, string)";

		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock(42, "x");
			         }
			     }

			     public class MyService
			     {
			         public MyService(int value, string text) { }
			     }
			     """, DocumentationMode.Diagnose);

		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains($"to invoke the <see cref=\"{expectedCref}\">MyService(int, string)</see> constructor.")
			.IgnoringNewlineStyle();

		string[] crefFailures = result.Diagnostics
			.Where(d => d.Contains(expectedCref))
			.ToArray();
		await That(crefFailures).IsEmpty()
			.Because(
				"CS1574/CS1584/CS1658 messages embed the offending cref text, so a cref that resolves cleanly under DocumentationMode.Diagnose is mentioned by no diagnostic");
	}

	[Test]
	public async Task WhenTypedCreateMockOverloadIsEmittedForExternalType_ConstructorCrefShouldResolve()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System.Net.Http;
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = HttpClient.CreateMock(default(HttpMessageHandler));
			         }
			     }
			     """, DocumentationMode.Diagnose, typeof(HttpClient));

		await That(result.Sources).ContainsKey("Mock.HttpClient.g.cs");
		await That(result.Sources["Mock.HttpClient.g.cs"])
			.Contains("<see cref=\"global::System.Net.Http.HttpClient(global::System.Net.Http.HttpMessageHandler)\">")
			.IgnoringNewlineStyle().And
			.DoesNotContain("HttpClient.HttpClient(")
			.IgnoringNewlineStyle();

		string[] crefFailures = result.Diagnostics
			.Where(d => d.Contains("CS1574") || d.Contains("CS1584") || d.Contains("CS1658"))
			.ToArray();
		await That(crefFailures).IsEmpty();
	}

	[Test]
	public async Task WhenTypedCreateMockOverloadIsEmittedForGenericClass_ShouldNotEmitConstructorCref()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService<int>.CreateMock(42);
			         }
			     }

			     public class MyService<T>
			     {
			         public MyService(T value) { }
			     }
			     """, DocumentationMode.Diagnose);

		await That(result.Sources).ContainsKey("Mock.MyService_int.g.cs");
		await That(result.Sources["Mock.MyService_int.g.cs"])
			.Contains("to invoke the base-class constructor.")
			.IgnoringNewlineStyle().And
			.DoesNotContain("MyService.MyService(")
			.IgnoringNewlineStyle()
			.Because(
				"closed-generic constructor crefs (e.g. MyService{int}.MyService(int)) aren't valid C# cref syntax, so generic classes fall back to the unlinked phrasing rather than surface CS1584 on the consumer side");
	}

	[Test]
	public async Task WhenUsingCreateMockFromNonMockolateNamespace_ShouldNotBeIncluded()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using MockolateExtensions;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			                 _ = new MyService().CreateMock();
			             }
			         }

			         public class MyService { }
			     }

			     namespace MockolateExtensions
			     {
			         public static class MockExtensions
			         {
			             public static T CreateMock<T>(this T value)
			                 where T : class
			                 => value;
			         }
			     }
			     """);

		await ThatAll(
			That(result.Sources.Keys).IsEqualTo([
				"Mock.g.cs",
				"MockBehaviorExtensions.g.cs",
			]).InAnyOrder().IgnoringCase(),
			That(result.Diagnostics).IsEmpty()
		);
	}

	[Test]
	public async Task WhenUsingSetups_ShouldGenerateMocksAndExtensions()
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
			     			_ = IMyInterface.CreateMock(setup => setup.Method.MyMethod().Returns(42));
			             }
			         }

			         public interface IMyInterface
			         {
			             int MyMethod();
			         }
			     }
			     """, typeof(DateTime), typeof(Task), typeof(HttpResponseMessage));

		await That(result.Sources).HasCount().GreaterThanOrEqualTo(3);
		await That(result.Sources).ContainsKey("Mock.IMyInterface.g.cs");
	}

	[Test]
	public async Task WithClassAsAdditionalImplementation_ShouldNotThrow()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Threading;
			     using System.Threading.Tasks;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			                 _ = IMyInterface1.CreateMock()
			                    .Implementing<MyService>()
			                    .Implementing<IMyInterface2>()
			                    .Implementing<MyOtherService>();
			             }
			         }

			     	public interface IMyInterface1 { }
			     	public class MyService { }
			     	public interface IMyInterface2 { }
			     	public class MyOtherService { }
			     }
			     """);

		await That(result.Sources.Keys).IsEqualTo([
			"Mock.g.cs",
			"MockBehaviorExtensions.g.cs",
			"Mock.IMyInterface1.g.cs",
		]).InAnyOrder();
	}

	[Test]
	public async Task WithHttpClient_ShouldAlsoGenerateMockForHttpMessageHandler()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Net.Http;
			     using System.Threading;
			     using System.Threading.Tasks;
			     using Mockolate;
			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			_ = HttpClient.CreateMock();
			             }
			         }
			     }
			     """, typeof(HttpClient));

		await That(result.Sources).ContainsKey("Mock.HttpMessageHandler.g.cs").And
			.ContainsKey("Mock.HttpClient.g.cs");
	}

	[GeneratedRegex(@"CreateMock\(global::Mockolate\.MockBehavior\s+\w+\)")]
	private static partial Regex CreateMockBehaviorSignatureRegex();

	[GeneratedRegex(@"CreateMock\(global::Mockolate\.MockBehavior\s+\w+,\s+int\s+\w+\)")]
	private static partial Regex CreateMockBehaviorIntSignatureRegex();
}
