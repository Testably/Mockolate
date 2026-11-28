using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Tests.Emission;

public sealed partial class ClassEmissionTests
{
	public sealed partial class ConstructorTests
	{
		[Test]
		public async Task ForTypesWithAdditionalConstructorsWithParameters_ShouldWorkForAllNonPrivateConstructors()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyBaseClass.CreateMock();
				         }
				     }

				     public class MyBaseClass
				     {
				         public MyBaseClass() { }
				         public MyBaseClass(int value) { }
				         protected MyBaseClass(int value, bool flag) { }
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.MyBaseClass.g.cs");
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.DoesNotContain("No parameterless constructor found");
		}

		[Test]
		public async Task ForTypesWithOnlyOptionalConstructorParameters_ShouldCallTheConstructorWithTheDeclaredDefaults()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyBaseClass.CreateMock();
				         }
				     }

				     public enum Level { Unknown = -1, Low = 0, }

				     public class MyBaseClass
				     {
				         public MyBaseClass(int value) { }
				         protected MyBaseClass(int value = 1, string text = "foo", Level level = Level.Unknown) { }
				         protected MyBaseClass(bool flag = true) { }
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.MyBaseClass.g.cs");
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.Contains("""
				          			if (constructorParameters is null || constructorParameters.Length == 0)
				          			{
				          				global::Mockolate.Mock.MyBaseClass.MockRegistryProvider.Value = mockRegistry;
				          				global::Mockolate.MockExtensionsForMyBaseClass.MockSetup? setupTarget = null;
				          				if (setup is not null)
				          				{
				          					setupTarget ??= new(mockRegistry);
				          					setup.Invoke(setupTarget);
				          				}
				          				int c2p1 = 1;
				          				string c2p2 = "foo";
				          				global::MyCode.Level c2p3 = (global::MyCode.Level)(-1);
				          				return new global::Mockolate.Mock.MyBaseClass(mockRegistry, c2p1, c2p2, c2p3);
				          			}
				          """).IgnoringNewlineStyle().Because("the first constructor in declaration order that needs no arguments is used").And
				.DoesNotContain("No parameterless constructor found");
		}

		[Test]
		public async Task ForTypesWithParameterlessAndOptionalConstructor_ShouldPreferTheParameterlessConstructor()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyBaseClass.CreateMock();
				         }
				     }

				     public class MyBaseClass
				     {
				         protected MyBaseClass(int value = 1) { }
				         protected MyBaseClass() { }
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.MyBaseClass.g.cs");
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.Contains("""
				          					setup.Invoke(setupTarget);
				          				}
				          				return new global::Mockolate.Mock.MyBaseClass(mockRegistry);
				          			}
				          """).IgnoringNewlineStyle().And
				.DoesNotContain("int c1p1 = 1;");
		}

		[Test]
		public async Task ForTypesWithParamsConstructor_ShouldCallItWithAnEmptyArrayAndTheDeclaredDefaults()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyBaseClass.CreateMock();
				         }
				     }

				     public class MyBaseClass
				     {
				         public MyBaseClass(int value) { }
				         protected MyBaseClass(int value = 1, params string[] texts) { }
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.MyBaseClass.g.cs");
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.Contains("""
				          					setup.Invoke(setupTarget);
				          				}
				          				int c2p1 = 1;
				          				string[] c2p2 = [];
				          				return new global::Mockolate.Mock.MyBaseClass(mockRegistry, c2p1, c2p2);
				          			}
				          """).IgnoringNewlineStyle().And
				.DoesNotContain("No parameterless constructor found");
		}

		[Test]
		public async Task ForTypesWithParamsSpanConstructor_ShouldCallItWithAnEmptySpan()
		{
			GeneratorResult result = Generator
				.Run("""
				     using System;
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyBaseClass.CreateMock().Implementing<IMyInterface>();
				         }
				     }

				     public class MyBaseClass
				     {
				         protected MyBaseClass(params ReadOnlySpan<int> values) { }
				     }

				     public interface IMyInterface
				     {
				         void DoWork();
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.Contains("""
				          				global::System.ReadOnlySpan<int> c1p1 = [];
				          				return new global::Mockolate.Mock.MyBaseClass(mockRegistry, c1p1);
				          """).IgnoringNewlineStyle().And
				.DoesNotContain("TryCast(").Because("a span cannot be passed in the object array of constructor parameters");
			await That(result.Sources["Mock.MyBaseClass__IMyInterface.g.cs"])
				.Contains("""
				          			global::System.ReadOnlySpan<int> c1p1 = [];
				          			value = new global::Mockolate.Mock.MyBaseClass__IMyInterface(mock.MockRegistry, c1p1);
				          """).IgnoringNewlineStyle().And
				.DoesNotContain("TryCast(");
		}

		[Test]
		[Arguments("protected MyBaseClass(params object[] values) { }", "protected MyBaseClass(params string[] values) { }", 2)]
		[Arguments("protected MyBaseClass(params int[] values) { }", "protected MyBaseClass(int value = 1) { }", 2)]
		[Arguments("protected MyBaseClass(params string[] values) { }", "protected MyBaseClass(int value = 1, params string[] values) { }", 1)]
		[Arguments("protected MyBaseClass(int value = 1, params object[] values) { }", "protected MyBaseClass(int value = 1, params string[] values) { }", 2)]
		[Arguments("protected MyBaseClass(params int[] values) { }", "protected MyBaseClass(params long[] values) { }", 1)]
		[Arguments("protected MyBaseClass(int value = 1, params string[] values) { }", "protected MyBaseClass(int value = 1, int other = 2, params object[] values) { }", 1)]
		[Arguments("protected MyBaseClass(params int[] values) { }", "protected MyBaseClass(params System.ReadOnlySpan<int> values) { }", 2)]
		[Arguments("protected MyBaseClass(params System.Span<int> values) { }", "protected MyBaseClass(params System.ReadOnlySpan<int> values) { }", 2)]
		[Arguments("protected MyBaseClass(params System.Collections.Generic.IEnumerable<int> values) { }", "protected MyBaseClass(params System.Span<int> values) { }", 2)]
		[Arguments("protected MyBaseClass(params System.Collections.Generic.IEnumerable<int> values) { }", "protected MyBaseClass(params System.Collections.Generic.List<int> values) { }", 2)]
		[Arguments("protected MyBaseClass(params System.Collections.Generic.List<int> values) { }", "protected MyBaseClass(params int[] values) { }", 1)]
		[Arguments("protected MyBaseClass(params object[] values) { }", "protected MyBaseClass(params System.ReadOnlySpan<string> values) { }", 1)]
		[Arguments("protected MyBaseClass(params System.Collections.Generic.List<int> values) { }", "protected MyBaseClass(params System.ReadOnlySpan<int> values) { }", 1)]
		[Arguments("protected MyBaseClass(params System.ReadOnlySpan<object> values) { }", "protected MyBaseClass(params System.ReadOnlySpan<string> values) { }", 1)]
		public async Task ForTypesWithSeveralConstructorsWithoutArguments_ShouldFollowCSharpOverloadResolution(
			string first, string second, int expectedConstructor)
		{
			GeneratorResult result = Generator
				.Run($$"""
				       using Mockolate;

				       namespace MyCode;

				       public class Program
				       {
				           public static void Main(string[] args)
				           {
				       		_ = MyBaseClass.CreateMock();
				           }
				       }

				       public class MyBaseClass
				       {
				           {{first}}
				           {{second}}
				       }
				       """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.Contains($" c{expectedConstructor}p1 = ")
				.Because("new MyBaseClass() resolves to the same constructor, or the first declared one where C# reports an ambiguity").And
				.DoesNotContain($" c{3 - expectedConstructor}p1 = ");
		}

		[Test]
		public async Task ForTypesWithConstructorWithParameters_ShouldWorkForAllNonPrivateConstructors()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyBaseClass.CreateMock();
				         }
				     }

				     public class MyBaseClass
				     {
				         public MyBaseClass(int value) { }
				         protected MyBaseClass(int value, bool flag) { }
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.MyBaseClass.g.cs");
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.Contains("""
				          			if (constructorParameters.Length == 1
				          			    && TryCast(constructorParameters, 0, mockRegistry.Behavior, out int c1p1))
				          			{
				          				global::Mockolate.Mock.MyBaseClass.MockRegistryProvider.Value = mockRegistry;
				          				global::Mockolate.MockExtensionsForMyBaseClass.MockSetup? setupTarget = null;
				          				if (setup is not null)
				          				{
				          					setupTarget ??= new(mockRegistry);
				          					setup.Invoke(setupTarget);
				          				}
				          				return new global::Mockolate.Mock.MyBaseClass(mockRegistry, c1p1);
				          			}
				          """.TrimStart()).IgnoringNewlineStyle().And
				.Contains("""
				          			if (constructorParameters.Length == 2
				          			    && TryCast(constructorParameters, 0, mockRegistry.Behavior, out int c2p1)
				          			    && TryCast(constructorParameters, 1, mockRegistry.Behavior, out bool c2p2))
				          			{
				          				global::Mockolate.Mock.MyBaseClass.MockRegistryProvider.Value = mockRegistry;
				          				global::Mockolate.MockExtensionsForMyBaseClass.MockSetup? setupTarget = null;
				          				if (setup is not null)
				          				{
				          					setupTarget ??= new(mockRegistry);
				          					setup.Invoke(setupTarget);
				          				}
				          				return new global::Mockolate.Mock.MyBaseClass(mockRegistry, c2p1, c2p2);
				          			}
				          """.TrimStart()).IgnoringNewlineStyle().And
				.Contains("""
				          			if (constructorParameters is null || constructorParameters.Length == 0)
				          			{
				          				int d1 = global::Mockolate.Internals.ConstructorDefaultValue.Get(mockRegistry.Behavior, static mockolateBehavior => mockolateBehavior.DefaultValue.Generate(default(int)!));
				          				bool d2 = global::Mockolate.Internals.ConstructorDefaultValue.Get(mockRegistry.Behavior, static mockolateBehavior => mockolateBehavior.DefaultValue.Generate(default(bool)!));
				          				mockRegistry = new global::Mockolate.Internals.MockRegistry(mockRegistry, new object?[] { d1, d2, });
				          """).IgnoringNewlineStyle().And
				.Contains("""
				          		public MyBaseClass(global::Mockolate.Internals.MockRegistry mockRegistry, int value)
				          			: base(value)
				          		{
				          			this.MockRegistry = mockRegistry;
				          			this.MockolateConstructorArguments = MockolateConstructorSignature0.Record(new object?[] { value, });
				          		}
				          """).IgnoringNewlineStyle().And
				.Contains("""
				          		public MyBaseClass(global::Mockolate.Internals.MockRegistry mockRegistry, int value, bool flag)
				          			: base(value, flag)
				          		{
				          			this.MockRegistry = mockRegistry;
				          			this.MockolateConstructorArguments = MockolateConstructorSignature1.Record(new object?[] { value, flag, });
				          		}
				          """).IgnoringNewlineStyle().And
				.Contains("""
				          		private static readonly global::Mockolate.Internals.ConstructorSignature MockolateConstructorSignature0 = new global::Mockolate.Internals.ConstructorSignature("MyCode.MyBaseClass(int)", new string[] { "value", }, new global::System.Type[] { typeof(int), });
				          """).IgnoringNewlineStyle().And
				.Contains("""
				          		private static readonly global::Mockolate.Internals.ConstructorSignature MockolateConstructorSignature1 = new global::Mockolate.Internals.ConstructorSignature("MyCode.MyBaseClass(int, bool)", new string[] { "value", "flag", }, new global::System.Type[] { typeof(int), typeof(bool), });
				          """).IgnoringNewlineStyle();
		}

		[Test]
		public async Task ForTypesWithOnlyParameterlessConstructor_ShouldOmitConstructorParametersOverloads()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyBaseClass.CreateMock();
				         }
				     }

				     public class MyBaseClass
				     {
				         public MyBaseClass() { }
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.MyBaseClass.g.cs");
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.DoesNotContain("CreateMock(object?[] constructorParameters)").And
				.DoesNotContain("CreateMock(global::Mockolate.MockBehavior mockBehavior, object?[] constructorParameters)").And
				.DoesNotContain("object?[] constructorParameters)").And
				.Contains(
					"private static global::MyCode.MyBaseClass CreateMock(global::Mockolate.MockBehavior? mockBehavior, global::System.Action<global::Mockolate.Mock.IMockSetupForMyBaseClass>? setup, object?[]? constructorParameters)");
		}

		[Test]
		public async Task ForTypesWithoutExplicitConstructor_ShouldOmitConstructorParametersOverloads()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyBaseClass.CreateMock();
				         }
				     }

				     public class MyBaseClass
				     {
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.MyBaseClass.g.cs");
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.DoesNotContain("CreateMock(object?[] constructorParameters)").And
				.DoesNotContain("object?[] constructorParameters)").And
				.Contains(
					"private static global::MyCode.MyBaseClass CreateMock(global::Mockolate.MockBehavior? mockBehavior, global::System.Action<global::Mockolate.Mock.IMockSetupForMyBaseClass>? setup, object?[]? constructorParameters)");
		}

		[Test]
		public async Task ForTypesWithoutPublicOrProtectedConstructor_ShouldOnlyGenerateMockThatThrowsException()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyBaseClass.CreateMock();
				         }
				     }

				     public class MyBaseClass
				     {
				         private MyBaseClass() { }
				     }
				     """);

			await That(result.Sources).DoesNotContainKey("Mock.MyBaseClass.g.cs");
		}

		[Test]
		public async Task ClassWithBaseConstructorAlreadyAnnotatedSetsRequiredMembers_ShouldNotDuplicateAttribute()
		{
			GeneratorResult result = Generator
				.Run("""
				     using System.Diagnostics.CodeAnalysis;
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = AnnotatedShape.CreateMock();
				         }
				     }

				     public abstract class AnnotatedShape
				     {
				         public required string Name { get; init; }

				         [SetsRequiredMembers]
				         protected AnnotatedShape() { }

				         public abstract int Compute();
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.AnnotatedShape.g.cs");
			await That(result.Sources["Mock.AnnotatedShape.g.cs"])
				.Contains("[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]").Exactly(2)
				.Because(
					"one occurrence per generator-emitted public ctor, the (MockRegistry, ...) overload and the typed (MockBehavior, ...) overload that subclasses use; the base ctor already carries the attribute and the dedup check suppresses an injected duplicate on each");
		}

		[Test]
		public async Task ClassWithInheritedRequiredMember_ShouldEmitSetsRequiredMembersOnGeneratedConstructor()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = DerivedShape.CreateMock();
				         }
				     }

				     public abstract class BaseShape
				     {
				         public required string Name { get; init; }
				     }

				     public abstract class DerivedShape : BaseShape
				     {
				         public abstract int Compute();
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.DerivedShape.g.cs");
			await That(result.Sources["Mock.DerivedShape.g.cs"])
				.Contains("[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]");
		}

		[Test]
		public async Task ClassWithoutRequiredMember_ShouldNotEmitSetsRequiredMembers()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = PlainShape.CreateMock();
				         }
				     }

				     public abstract class PlainShape
				     {
				         public string Name { get; init; } = "";
				         public abstract int Compute();
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.PlainShape.g.cs");
			await That(result.Sources["Mock.PlainShape.g.cs"])
				.DoesNotContain("SetsRequiredMembers");
		}

		[Test]
		public async Task ClassWithRequiredMember_ShouldEmitSetsRequiredMembersOnGeneratedConstructor()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = RequiredShape.CreateMock();
				         }
				     }

				     public abstract class RequiredShape
				     {
				         public required string Name { get; init; }
				         public abstract int Compute();
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.RequiredShape.g.cs");
			await That(result.Sources["Mock.RequiredShape.g.cs"])
				.Contains("""
				          		[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
				          		public RequiredShape(global::Mockolate.Internals.MockRegistry mockRegistry)
				          """)
				.IgnoringNewlineStyle();
		}

		[Test]
		public async Task Constructor_WithRegistrationsParameter_ShouldRenameAdditionalParameter()
		{
			GeneratorResult result = Generator
				.Run("""
				     using System.Collections.Generic;
				     using Mockolate;

				     namespace MyCode
				     {
				         public class Program
				         {
				             public static void Main(string[] args)
				             {
				     			var x = MyClass.CreateMock();
				             }
				         }

				         public class MyClass
				         {
				             public MyClass(int mockRegistry)
				             {
				             }
				         }
				     }
				     """, typeof(IList<>), typeof(HttpResponseMessage));

			await That(result.Diagnostics).IsEmpty();

			await That(result.Sources).ContainsKey("Mock.MyClass.g.cs")
				.WhoseValue
				.Contains("""
				          		public MyClass(global::Mockolate.Internals.MockRegistry mockRegistry_1, int mockRegistry)
				          			: base(mockRegistry)
				          		{
				          			this.MockRegistry = mockRegistry_1;
				          			this.MockolateConstructorArguments = MockolateConstructorSignature0.Record(new object?[] { mockRegistry, });
				          		}
				          """).And
				.Contains("""
				          		private static readonly global::Mockolate.Internals.ConstructorSignature MockolateConstructorSignature0 = new global::Mockolate.Internals.ConstructorSignature("MyCode.MyClass(int)", new string[] { "mockRegistry", }, new global::System.Type[] { typeof(int), });
				          """);
		}

		[Test]
		public async Task Constructor_WithParameterAndMemberNamedLikeTheSignatureField_ShouldCompile()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode
				     {
				         public class Program
				         {
				             public static void Main(string[] args)
				             {
				     			_ = MyClass.CreateMock();
				             }
				         }

				         public class MyClass
				         {
				             public MyClass(int MockolateConstructorSignature0)
				             {
				             }

				             public MyClass(int value, string text)
				             {
				             }

				             public virtual int MockolateConstructorSignature1 => 1;
				         }
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.MyClass.g.cs")
				.WhoseValue
				.Contains("MockolateConstructorSignature0_1.Record(new object?[] { MockolateConstructorSignature0, });").And
				.Contains("MockolateConstructorSignature1_1.Record(new object?[] { value, text, });");
		}

		[Test]
		public async Task Constructor_WithAlreadySuffixedBehaviorParameter_ShouldSuffixFromBaseName()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode
				     {
				         public class Program
				         {
				             public static void Main(string[] args)
				             {
				     			var x = MyClass.CreateMock();
				             }
				         }

				         public class MyClass
				         {
				             public MyClass(int behavior, int behavior_1)
				             {
				             }
				         }
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();

			await That(result.Sources).ContainsKey("Mock.MyClass.g.cs")
				.WhoseValue
				.Contains("public MyClass(global::Mockolate.MockBehavior behavior_2, int behavior, int behavior_1)")
				.Because("the suffix must be appended to the base name instead of the previous candidate");
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
				.Contains("public MyService(global::Mockolate.Internals.MockRegistry mockRegistry)").IgnoringNewlineStyle().And
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

		[GeneratedRegex(@"CreateMock\(global::Mockolate\.MockBehavior\s+\w+\)")]
		private static partial Regex CreateMockBehaviorSignatureRegex();

		[GeneratedRegex(@"CreateMock\(global::Mockolate\.MockBehavior\s+\w+,\s+int\s+\w+\)")]
		private static partial Regex CreateMockBehaviorIntSignatureRegex();
	}
}
