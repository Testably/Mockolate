namespace Mockolate.SourceGenerators.Tests;

public sealed partial class MockTests
{
	public sealed partial class ClassTests
	{
		[Test]
		public async Task BaseClassMethodWithParameterNamedBaseResult_ShouldGenerateUniqueLocalVariableName()
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
				         public virtual int ProcessData(int baseResult) => baseResult;
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
			await That(result.Sources["Mock.MyService.g.cs"])
				.Contains(
					"foreach (global::Mockolate.Setup.ReturnMethodSetup<int, int> s_methodSetup in this.MockRegistry.GetMethodSetups<global::Mockolate.Setup.ReturnMethodSetup<int, int>>(\"global::MyCode.MyService.ProcessData\"))")
				.IgnoringNewlineStyle().And
				.Contains("wrappedResult = base.ProcessData(baseResult);")
				.IgnoringNewlineStyle().And
				.Contains("methodSetup?.TriggerCallbacks(baseResult);")
				.IgnoringNewlineStyle().And
				.Contains(
					"return methodSetup?.TryGetReturnValue(baseResult, out var returnValue) == true ? returnValue : this.MockRegistry.Behavior.DefaultValue.Generate(default(int)!, baseResult);")
				.IgnoringNewlineStyle();
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
				          		public RequiredShape(global::Mockolate.MockRegistry mockRegistry)
				          """)
				.IgnoringNewlineStyle();
		}

		[Test]
		public async Task InheritedInterface_ShouldHaveCorrectReferenceInXMLDocumentation()
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
				     		_ = IMyService.CreateMock();
				         }
				     }

				     public interface IMyService : IMyBaseService
				     {
				     }
				     public interface IMyBaseService
				     {
				         int BaseProperty { get; }
				         event EventHandler<int> BaseEvent;
				         int BaseMethod();
				         int this[string baseIndexer] { get; }
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.IMyService.g.cs");
			await That(result.Sources["Mock.IMyService.g.cs"])
				.Contains(
					"Setup for the int property <see cref=\"global::MyCode.IMyBaseService.BaseProperty\">BaseProperty</see>.")
				.And
				.Contains(
					"Verify interactions with the int property <see cref=\"global::MyCode.IMyBaseService.BaseProperty\">BaseProperty</see>.")
				.And
				.Contains(
					"Setup for the int indexer <see cref=\"global::MyCode.IMyBaseService.this[string]\">this[string]</see>")
				.And
				.Contains(
					"Verify interactions with the int indexer <see cref=\"global::MyCode.IMyBaseService.this[string]\">this[string]</see>.")
				.And
				.Contains(
					"Setup for the method <see cref=\"global::MyCode.IMyBaseService.BaseMethod()\">BaseMethod()</see>.")
				.And
				.Contains(
					"Verify invocations for the method <see cref=\"global::MyCode.IMyBaseService.BaseMethod()\">BaseMethod()</see>.")
				.And
				.Contains("Raise the <see cref=\"global::MyCode.IMyBaseService.BaseEvent\">BaseEvent</see> event.").And
				.Contains(
					"Verify subscriptions on the BaseEvent event of <see cref=\"global::MyCode.IMyBaseService.BaseEvent\">BaseEvent</see>.");
		}

		[Test]
		public async Task InterfaceWithEvents_ShouldIncludeRaiseRemarkBullet()
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
				     		_ = IHasEvent.CreateMock();
				         }
				     }

				     public interface IHasEvent
				     {
				         event EventHandler Changed;
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.IHasEvent.g.cs");
			await That(result.Sources["Mock.IHasEvent.g.cs"])
				.Contains("<c>Raise</c> - trigger events declared on the mocked type.").And
				.Contains("<c>.Mock.Raise</c> triggers events declared on the mocked type.").And
				.DoesNotContain("<c>SetupProtected</c> / <c>VerifyProtected</c> / <c>RaiseProtected</c>").And
				.DoesNotContain("<c>SetupStatic</c> / <c>VerifyStatic</c> / <c>RaiseStatic</c>");
		}

		[Test]
		public async Task PlainInterface_ShouldOmitConditionalRemarkBullets()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = IPlain.CreateMock();
				         }
				     }

				     public interface IPlain
				     {
				         int GetValue();
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.IPlain.g.cs");
			await That(result.Sources["Mock.IPlain.g.cs"])
				.DoesNotContain("<c>Raise</c> - trigger events declared on the mocked type.").And
				.DoesNotContain("<c>SetupProtected</c> / <c>VerifyProtected</c> / <c>RaiseProtected</c>").And
				.DoesNotContain("<c>SetupStatic</c> / <c>VerifyStatic</c> / <c>RaiseStatic</c>").And
				.DoesNotContain("<c>.Mock.Raise</c> triggers events declared on the mocked type.");
		}
	}
}
