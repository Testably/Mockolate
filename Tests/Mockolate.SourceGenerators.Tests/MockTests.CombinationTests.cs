namespace Mockolate.SourceGenerators.Tests;

public sealed partial class MockTests
{
	public sealed class CombinationTests
	{
		[Test]
		public async Task AbstractBaseWithParameterlessAndMixedConstructor_ShouldEmitBothTryCastHelpers()
		{
			// A constructor with a required parameter sets useTryCast; a defaulted parameter sets
			// useTryCastWithDefaultValue. Mixing both flips both flags so the extension method ends
			// up with both helper functions and the constructor-arity dispatch 'else if' chain.
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

				     public abstract class MyService
				     {
				         protected MyService() { }
				         protected MyService(int required, string text = "hi") { }
				     }

				     public interface IMyInterface
				     {
				         void DoWork();
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();

			await That(result.Sources).ContainsKey("Mock.MyService__IMyInterface.g.cs");
			await That(result.Sources["Mock.MyService__IMyInterface.g.cs"])
				.Contains(
					"static bool TryCast<TValue>(object?[] values, int index, global::Mockolate.MockBehavior behavior, out TValue result)")
				.And
				.Contains(
					"static bool TryCastWithDefaultValue<TValue>(object?[] values, int index, TValue defaultValue, global::Mockolate.MockBehavior behavior, out TValue result)")
				.And
				.Contains(
					"if (mock.MockRegistry.ConstructorParameters is null || mock.MockRegistry.ConstructorParameters.Length == 0)")
				.Because("the parameterless dispatch branch must be emitted").And
				.Contains(
					"else if (mock.MockRegistry.ConstructorParameters.Length >= 1 && mock.MockRegistry.ConstructorParameters.Length <= 2")
				.Because(
					"the 'else if' arity dispatch chain must include the optional-range branch for a mixed-required + defaulted ctor");
		}

		[Test]
		public async Task AbstractBaseWithRequiredOnlyConstructor_ShouldThrowNoParameterlessConstructorMessage()
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

				     public abstract class MyService
				     {
				         protected MyService(int value) { }
				     }

				     public interface IMyInterface
				     {
				         void DoWork();
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();

			await That(result.Sources).ContainsKey("Mock.MyService__IMyInterface.g.cs");
			await That(result.Sources["Mock.MyService__IMyInterface.g.cs"])
				.Contains("No parameterless constructor found for 'MyCode.MyService'").And
				.Contains("static bool TryCast<TValue>(object?[] values, int index, global::Mockolate.MockBehavior behavior, out TValue result)").And
				.DoesNotContain("static bool TryCastWithDefaultValue<TValue>");
		}

		[Test]
		public async Task
			CombinationOnConcreteClassWithStaticAbstractInterface_ShouldPrimeMockRegistryProviderInBaseClassConstructor()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyService.CreateMock().Implementing<IStaticAware>();
				         }
				     }

				     public class MyService
				     {
				         public MyService() { }
				     }

				     public interface IStaticAware
				     {
				         static abstract int Counter { get; }
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.MyService__IStaticAware.g.cs");
			await That(result.Sources["Mock.MyService__IStaticAware.g.cs"])
				.Contains("MockRegistryProvider.Value = mockRegistry;")
				.Because(
					"when a concrete base class is combined with an interface declaring static abstract members, the (MockRegistry) constructor must prime the AsyncLocal so virtual calls during base-class construction can resolve the registry");
		}

		[Test]
		public async Task CombinationWithProtectedEventOnBaseClass_ShouldEmitProtectedRaiseRegion()
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
				     		_ = MyService.CreateMock().Implementing<IExtra>();
				         }
				     }

				     public class MyService
				     {
				         protected virtual event EventHandler? Pinged;
				     }

				     public interface IExtra
				     {
				         void DoWork();
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.MyService__IExtra.g.cs");
			await That(result.Sources["Mock.MyService__IExtra.g.cs"])
				.Contains("IMockProtectedRaiseOnMyService").And
				.Contains("#region IMockProtectedRaiseOnMyService");
		}

		[Test]
		public async Task CombinationWithRequiredMemberOnBase_ShouldEmitSetsRequiredMembersAttribute()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyService.CreateMock().Implementing<IExtra>();
				         }
				     }

				     public class MyService
				     {
				         public required int Value { get; init; }
				         public MyService() { }
				     }

				     public interface IExtra
				     {
				         void DoWork();
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.MyService__IExtra.g.cs");
			await That(result.Sources["Mock.MyService__IExtra.g.cs"])
				.Contains("[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]")
				.Because(
					"AppendMockSubject_BaseClassConstructor must stamp SetsRequiredMembers on the generated constructor when the base class declares any `required` member");
		}

		[Test]
		public async Task CombinationWithStaticInterfaceEvents_ShouldEmitStaticRaiseRegion()
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
				     		_ = IBase.CreateMock().Implementing<IStaticEvents>();
				         }
				     }

				     public interface IBase
				     {
				         void DoWork();
				     }

				     public interface IStaticEvents
				     {
				         static abstract event EventHandler Pinged;
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.IBase__IStaticEvents.g.cs");
			await That(result.Sources["Mock.IBase__IStaticEvents.g.cs"])
				.Contains("IMockStaticRaiseOnIStaticEvents").And
				.Contains("#region IMockStaticRaiseOnIStaticEvents").And
				.Contains(
					"internal static readonly global::System.Threading.AsyncLocal<global::Mockolate.MockRegistry> MockRegistryProvider");
		}

		[Test]
		public async Task CombinationWithStaticInterfaceMembers_ShouldEmitStaticSetupAndVerifyRegions()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = IBase.CreateMock().Implementing<IStaticAware>();
				         }
				     }

				     public interface IBase
				     {
				         void DoWork();
				     }

				     public interface IStaticAware
				     {
				         static abstract int Counter { get; }
				         static abstract void Reset();
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.IBase__IStaticAware.g.cs");
			await That(result.Sources["Mock.IBase__IStaticAware.g.cs"])
				.Contains("IMockStaticSetupForIStaticAware").And
				.Contains("IMockStaticVerifyForIStaticAware").And
				.Contains("#region IMockStaticSetupForIStaticAware").And
				.Contains("#region IMockStaticVerifyForIStaticAware").And
				.Contains(
					"internal static readonly global::System.Threading.AsyncLocal<global::Mockolate.MockRegistry> MockRegistryProvider")
				.Because("the AsyncLocal field is required so static accessors can find the registry");
		}

		[Test]
		public async Task InterfaceTheClassAlreadyImplements_ShouldOnlyReImplementNonOverridableMembers()
		{
			// Re-implementing the interface is what makes the non-virtual `Add` reachable through the
			// interface slot, but for `Multiply`, `Precision` and `Calculated` the mock already overrides
			// the class member: re-implementing those too would fork one member into two, with separate
			// setups and separate recorded interactions per surface.
			GeneratorResult result = Generator
				.Run("""
				     #nullable enable
				     using System;
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyCalculator.CreateMock().Implementing<ICalculator>();
				         }
				     }

				     public interface ICalculator
				     {
				         int Precision { get; set; }
				         event EventHandler? Calculated;
				         int Add(int a, int b);
				         int Multiply(int a, int b);
				     }

				     public abstract class MyCalculator : ICalculator
				     {
				         public abstract int Precision { get; set; }
				         public abstract event EventHandler? Calculated;
				         public int Add(int a, int b) => a + b;
				         public abstract int Multiply(int a, int b);
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.MyCalculator__ICalculator.g.cs");
			await That(result.Sources["Mock.MyCalculator__ICalculator.g.cs"])
				.Contains("int global::MyCode.ICalculator.Add(int a, int b)")
				.Because("the class implements it non-virtually, so only the interface slot can be mocked").And
				.DoesNotContain("int global::MyCode.ICalculator.Multiply(int a, int b)").And
				.DoesNotContain("int global::MyCode.ICalculator.Precision").And
				.DoesNotContain("event global::System.EventHandler? global::MyCode.ICalculator.Calculated")
				.Because("the mock's own override already serves those interface slots").And
				.Contains("<see cref=\"global::MyCode.ICalculator.Calculated\">")
				.Because("documentation keeps referring to the interface member the shared member implements");
		}

		[Test]
		public async Task InterfaceTheClassAlreadyImplements_ShouldShareMemberIdsAndKeys()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyCalculator.CreateMock().Implementing<ICalculator>();
				         }
				     }

				     public interface ICalculator
				     {
				         int Multiply(int a, int b);
				     }

				     public abstract class MyCalculator : ICalculator
				     {
				         public abstract int Multiply(int a, int b);
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyCalculator__ICalculator.g.cs"])
				.Contains("internal const int MemberId_Multiply = 0;").And
				.Contains("internal const int MemberCount = 1;")
				.Because("both surfaces address a single member").And
				.DoesNotContain("\"global::MyCode.ICalculator.Multiply\"")
				.Because("the interface surface registers setups under the class member's key");
		}

		[Test]
		public async Task InterfaceTheClassAlreadyImplements_ShouldShareIndexers()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyCalculator.CreateMock().Implementing<ICalculator>();
				         }
				     }

				     public interface ICalculator
				     {
				         int this[int index] { get; set; }
				     }

				     public abstract class MyCalculator : ICalculator
				     {
				         public abstract int this[int index] { get; set; }
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyCalculator__ICalculator.g.cs"])
				.Contains("internal const int MemberCount = 2;")
				.Because("both surfaces address a single indexer").And
				.DoesNotContain("MemberId_Indexer_int_Get_2").And
				.DoesNotContain("global::MyCode.ICalculator.this[int index]")
				.Because("the mock's own override already serves the interface slot, and a second "
				         + "implementation would get its own value storage");
		}

		[Test]
		public async Task ImplementedMemberDifferingOnlyInNullabilityOrTypeParameterName_ShouldStillBeShared()
		{
			// The class member is the compiler-resolved implementation of the interface member in both
			// cases, so the two surfaces must share one mock member - a signature comparison would see
			// `string` vs `string?` and `TValue` vs `T` and keep them apart.
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyCalculator.CreateMock().Implementing<ICalculator>();
				         }
				     }

				     #nullable enable
				     public interface ICalculator
				     {
				         string? Describe();
				         T Convert<T>(T value);
				     }
				     #nullable disable

				     public abstract class MyCalculator : ICalculator
				     {
				         public abstract string Describe();
				         public abstract TValue Convert<TValue>(TValue value);
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyCalculator__ICalculator.g.cs"])
				.Contains("internal const int MemberCount = 2;").And
				.DoesNotContain("string? global::MyCode.ICalculator.Describe()").And
				.DoesNotContain("global::MyCode.ICalculator.Convert<T>(T value)");
		}

		[Test]
		public async Task InterfaceTheClassDoesNotImplement_ShouldKeepCollidingMembersSeparate()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = MyCalculator.CreateMock().Implementing<ICalculator>();
				         }
				     }

				     public interface ICalculator
				     {
				         int this[int index] { get; set; }
				         int Multiply(int a, int b);
				     }

				     public abstract class MyCalculator
				     {
				         public abstract int this[int index] { get; set; }
				         public abstract int Multiply(int a, int b);
				     }
				     """);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyCalculator__ICalculator.g.cs"])
				.Contains("int global::MyCode.ICalculator.Multiply(int a, int b)").And
				.Contains("int global::MyCode.ICalculator.this[int index]").And
				.Contains("MemberId_Indexer_int_Get_2")
				.Because("the class does not implement the interface, so the two members are unrelated");
		}
	}
}
