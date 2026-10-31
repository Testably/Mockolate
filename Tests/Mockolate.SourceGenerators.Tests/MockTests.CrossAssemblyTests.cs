using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Tests;

public sealed partial class MockTests
{
	public sealed class CrossAssemblyTests
	{
		private const string CreateMockForClientBase = """
		                                              using Mockolate;

		                                              namespace MyCode;

		                                              public class Program
		                                              {
		                                                  public static void Main(string[] args) => _ = Ext.ClientBase.CreateMock();
		                                              }
		                                              """;

		private const string CreateMockForMyBaseClass = """
		                                               using Mockolate;

		                                               namespace MyCode;

		                                               public class Program
		                                               {
		                                                   public static void Main(string[] args) => _ = Ext.MyBaseClass.CreateMock();
		                                               }
		                                               """;

		private const string CreateMockForMyExternalType = """
		                                                   using Mockolate;

		                                                   namespace MyCode;

		                                                   public class Program
		                                                   {
		                                                       public static void Main(string[] args) => _ = Ext.MyExternalType.CreateMock();
		                                                   }
		                                                   """;

		[Test]
		public async Task InternalVirtualMember_WithInternalsVisibleTo_ShouldBeOverridden()
		{
			MetadataReference external = CompileMyBaseClassAssembly(grantsInternalsVisibleTo: true);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyBaseClass, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.MyBaseClass.g.cs");
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.Contains("internal override void InternalVirtualMethod()").And
				.Contains("internal override int InternalVirtualProperty");
		}

		[Test]
		public async Task InternalVirtualMember_WithoutInternalsVisibleTo_ShouldNotBeOverridden()
		{
			MetadataReference external = CompileMyBaseClassAssembly(grantsInternalsVisibleTo: false);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyBaseClass, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.MyBaseClass.g.cs");
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.Contains("public override void PublicAbstractMethod()").And
				.DoesNotContain("InternalVirtualMethod").And
				.DoesNotContain("InternalVirtualProperty");
		}

		[Test]
		public async Task ProtectedInternalMember_WithInternalsVisibleTo_ShouldBeOverriddenAsProtectedInternal()
		{
			MetadataReference external = CompileMyBaseClassAssembly(grantsInternalsVisibleTo: true);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyBaseClass, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.MyBaseClass.g.cs");
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.Contains("protected internal override void ProtectedInternalMethod()").And
				.Contains("protected internal override int ProtectedInternalProperty").And
				.Contains("protected internal override event global::System.EventHandler? ProtectedInternalEvent").And
				.Contains("protected internal set");
		}

		[Test]
		public async Task ProtectedInternalMember_WithoutInternalsVisibleTo_ShouldBeOverriddenAsProtected()
		{
			MetadataReference external = CompileMyBaseClassAssembly(grantsInternalsVisibleTo: false);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyBaseClass, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.MyBaseClass.g.cs");
			await That(result.Sources["Mock.MyBaseClass.g.cs"])
				.Contains("protected override void ProtectedInternalMethod()").And
				.Contains("protected override int ProtectedInternalProperty").And
				.Contains("protected override event global::System.EventHandler? ProtectedInternalEvent").And
				.Contains("protected set").And
				.DoesNotContain("protected internal override").And
				.DoesNotContain("protected internal set");
		}

		[Test]
		public async Task ProtectedInternalNestedConstructorParameter_WithInternalsVisibleTo_ShouldEmitConstructor()
		{
			MetadataReference external = CompileClientBaseAssembly(grantsInternalsVisibleTo: true);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForClientBase, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.ClientBase.g.cs");
			await That(result.Sources["Mock.ClientBase.g.cs"])
				.Contains("CreateMock(global::Ext.ClientBase.ClientBaseConfiguration configuration)")
				.Because("InternalsVisibleTo makes the internal half of `protected internal` visible");
		}

		[Test]
		public async Task ProtectedInternalNestedConstructorParameter_WithoutInternalsVisibleTo_ShouldNotEmitConstructor()
		{
			MetadataReference external = CompileClientBaseAssembly(grantsInternalsVisibleTo: false);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForClientBase, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.ClientBase.g.cs");
			await That(result.Sources["Mock.ClientBase.g.cs"])
				.Contains("public ClientBase(global::Mockolate.MockRegistry mockRegistry)").And
				.DoesNotContain("ClientBaseConfiguration")
				.Because(
					"`protected internal` degrades to `protected` outside the declaring assembly, so the type is only reachable through inheritance and cannot be named on the generated public constructor (CS0051) or in MockExtensionsForClientBase (CS0122)");
		}

		[Test]
		[Arguments(true)]
		[Arguments(false)]
		public async Task ProtectedInternalNestedMemberSignature_ShouldFollowInternalsVisibleTo(
			bool grantsInternalsVisibleTo)
		{
			MetadataReference external = CompileClientBaseAssembly(grantsInternalsVisibleTo);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForClientBase, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.ClientBase.g.cs");
			if (grantsInternalsVisibleTo)
			{
				await That(result.Sources["Mock.ClientBase.g.cs"])
					.Contains("ApplyOptions")
					.Because("InternalsVisibleTo makes the internal half of `protected internal` visible");
			}
			else
			{
				await That(result.Sources["Mock.ClientBase.g.cs"])
					.DoesNotContain("ApplyOptions")
					.Because(
						"the setup and verify surfaces would have to name `ClientBaseConfiguration`, which degrades to `protected` outside the declaring assembly (CS0122)");
			}
		}

		[Test]
		public async Task AttributeWithInaccessibleTypeArgument_ShouldNotBeEmitted()
		{
			MetadataReference external = ExternalAssembly.Compile("""
			                                                      namespace Ext;

			                                                      internal class Secret { }

			                                                      public class MyMarkerAttribute<T> : System.Attribute { }

			                                                      public class ClientBase
			                                                      {
			                                                      	[MyMarkerAttribute<Secret>]
			                                                      	public virtual void Send() { }
			                                                      }
			                                                      """);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForClientBase, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.ClientBase.g.cs"])
				.Contains("Send").And
				.DoesNotContain("MyMarkerAttribute")
				.Because(
					"the attribute name is emitted verbatim, so an inaccessible type argument makes it unusable even though the attribute class itself is public");
		}

		[Test]
		public async Task PublicMembers_ShouldBeMockedAcrossAssemblyBoundary()
		{
			MetadataReference external = ExternalAssembly.Compile("""
			                                                      namespace Ext;

			                                                      public interface IMyService
			                                                      {
			                                                      	int GetValue();
			                                                      }

			                                                      public abstract class MyAbstractService
			                                                      {
			                                                      	public abstract string Name { get; }
			                                                      	public virtual int Compute() => 0;
			                                                      }
			                                                      """);

			GeneratorResult result = Generator.RunWithReferences("""
			                                                     using Mockolate;

			                                                     namespace MyCode;

			                                                     public class Program
			                                                     {
			                                                         public static void Main(string[] args)
			                                                         {
			                                                     		_ = Ext.IMyService.CreateMock();
			                                                     		_ = Ext.MyAbstractService.CreateMock();
			                                                         }
			                                                     }
			                                                     """, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.IMyService.g.cs").And
				.ContainsKey("Mock.MyAbstractService.g.cs");
			await That(result.Sources["Mock.IMyService.g.cs"])
				.Contains("public int GetValue()");
			await That(result.Sources["Mock.MyAbstractService.g.cs"])
				.Contains("public override string Name").And
				.Contains("public override int Compute()");
		}

		[Test]
		[Arguments("public abstract string MyProperty { get; internal set; }", "public override string MyProperty")]
		[Arguments("internal abstract void MyMethod();", "internal override void MyMethod()")]
		[Arguments("internal abstract int MyProperty { get; set; }", "internal override int MyProperty")]
		[Arguments("internal abstract event System.EventHandler MyEvent;",
			"internal override event global::System.EventHandler MyEvent")]
		[Arguments("private protected abstract void MyMethod();", "private protected override void MyMethod()")]
		[Arguments("private protected abstract int MyProperty { get; set; }",
			"private protected override int MyProperty")]
		[Arguments("private protected abstract event System.EventHandler MyEvent;",
			"private protected override event global::System.EventHandler MyEvent")]
		[Arguments("public abstract string MyProperty { get; private protected set; }",
			"public override string MyProperty")]
		public async Task InaccessibleAbstractClassMember_WithInternalsVisibleTo_ShouldBeMocked(
			string member, string expectedOverride)
		{
			MetadataReference external = CompileMyExternalTypeAssembly("abstract class", member,
				grantsInternalsVisibleTo: true);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.MyExternalType.g.cs");
			await That(result.Sources["Mock.MyExternalType.g.cs"]).Contains(expectedOverride)
				.Because(
					"the override must repeat the base member's declared accessibility, and the member name alone would also match the setup and verify surfaces");
		}

		[Test]
		[Arguments("private protected abstract void MyMethod();")]
		[Arguments("private protected abstract event System.EventHandler MyEvent;")]
		public async Task PrivateProtectedMember_WithInternalsVisibleTo_ShouldNotBeWrapped(string member)
		{
			MetadataReference external = CompileMyExternalTypeAssembly("abstract class", member,
				grantsInternalsVisibleTo: true);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyExternalType.g.cs"]).DoesNotContain(".Wraps is global::Ext.MyExternalType")
				.Because(
					"a `private protected` member cannot be reached through a base-typed qualifier from the derived mock (CS1540)");
		}

		[Test]
		[Arguments("interface", "string MyProperty { get; internal set; }")]
		[Arguments("interface", "internal void MyMethod();")]
		[Arguments("abstract class", "public abstract string MyProperty { get; internal set; }")]
		[Arguments("abstract class", "internal abstract void MyMethod();")]
		[Arguments("abstract class", "internal abstract int MyProperty { get; set; }")]
		[Arguments("abstract class", "internal abstract event System.EventHandler MyEvent;")]
		[Arguments("abstract class", "private protected abstract void MyMethod();")]
		[Arguments("abstract class", "private protected abstract int MyProperty { get; set; }")]
		[Arguments("abstract class", "private protected abstract event System.EventHandler MyEvent;")]
		public async Task InaccessibleRequiredMember_WithoutInternalsVisibleTo_ShouldNotBeMocked(
			string typeKeyword, string member)
		{
			MetadataReference external = CompileMyExternalTypeAssembly(typeKeyword, member,
				grantsInternalsVisibleTo: false);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty()
				.Because(
					"the call binds to the generic `CreateMock` fallback in Mock.g.cs, so the program still compiles and throws MockException at runtime instead");
			await That(result.Sources).DoesNotContainKey("Mock.MyExternalType.g.cs")
				.Because(
					"the generator must refuse to emit a mock whose overrides could not compile; the user-facing guarantee is the Mockolate0002 error");
		}

		[Test]
		[Arguments("internal abstract void Hidden();", "internal override void Hidden() { }")]
		[Arguments("internal abstract int Hidden { get; set; }", "internal override int Hidden { get; set; }")]
		[Arguments("internal abstract event System.EventHandler Hidden;",
			"internal override event System.EventHandler Hidden;")]
		[Arguments("private protected abstract int Hidden { get; set; }",
			"private protected override int Hidden { get; set; }")]
		public async Task InaccessibleAbstractMemberAlreadyOverridden_ShouldStillBeMocked(
			string baseMember, string derivedOverride)
		{
			MetadataReference external = ExternalAssembly.Compile($$"""
			                                                       namespace Ext;

			                                                       public abstract class MyBaseType
			                                                       {
			                                                       	{{baseMember}}
			                                                       	public abstract int Visible();
			                                                       }

			                                                       public abstract class MyExternalType : MyBaseType
			                                                       {
			                                                       	{{derivedOverride}}
			                                                       }
			                                                       """);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.MyExternalType.g.cs");
			await That(result.Sources["Mock.MyExternalType.g.cs"])
				.Contains("public override int Visible()").And
				.DoesNotContain("Hidden")
				.Because("the slot is already filled, so neither the unreachable override nor the base declaration may be restated");
		}

		[Test]
		[Arguments("internal void Hidden();", "void IMyBaseType.Hidden() { }")]
		[Arguments("internal int Hidden { get; set; }",
			"int IMyBaseType.Hidden { get => 0; set { } }")]
		[Arguments("internal event System.EventHandler Hidden;",
			"event System.EventHandler IMyBaseType.Hidden { add { } remove { } }")]
		public async Task InaccessibleInterfaceMemberWithDefaultImplementation_ShouldStillBeMocked(
			string baseMember, string defaultImplementation)
		{
			MetadataReference external = ExternalAssembly.Compile($$"""
			                                                       namespace Ext;

			                                                       public interface IMyBaseType
			                                                       {
			                                                       	{{baseMember}}
			                                                       }

			                                                       public interface MyExternalType : IMyBaseType
			                                                       {
			                                                       	{{defaultImplementation}}
			                                                       	int Visible();
			                                                       }
			                                                       """);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.MyExternalType.g.cs");
			await That(result.Sources["Mock.MyExternalType.g.cs"])
				.Contains("public int Visible()").And
				.DoesNotContain("Hidden")
				.Because("the default implementation fills the slot, so the mock inherits it rather than restating it");
		}

		[Test]
		[Arguments("public abstract string Mixed { get; internal set; }",
			"public override string Mixed { get => null!; internal set { } }", "internal set")]
		[Arguments("public abstract string Mixed { get; private protected set; }",
			"public override string Mixed { get => null!; private protected set { } }", "private protected set")]
		public async Task MixedAccessorSlotAlreadyOverridden_ShouldBeMockedWithTheAccessibleAccessorOnly(
			string baseMember, string derivedOverride, string inaccessibleAccessor)
		{
			MetadataReference external = ExternalAssembly.Compile($$"""
			                                                       namespace Ext;

			                                                       public abstract class MyBaseType
			                                                       {
			                                                       	{{baseMember}}
			                                                       }

			                                                       public abstract class MyExternalType : MyBaseType
			                                                       {
			                                                       	{{derivedOverride}}
			                                                       }
			                                                       """);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyExternalType.g.cs"])
				.Contains("global::Mockolate.Setup.IPropertyGetterOnlySetup<string> Mixed { get; }").And
				.Contains("global::Mockolate.Verify.VerificationPropertyGetterResult<IMockVerifyForMyExternalType> Mixed { get; }")
				.Because("the mock does not intercept the inaccessible setter, so neither surface may offer it").And
				.Contains("public override string Mixed").And
				.Contains(".GetProperty").And
				.DoesNotContain(inaccessibleAccessor)
				.Because("the accessor is invisible to the mock's assembly, so only the reachable one may be overridden");
		}

		[Test]
		[Arguments("public abstract string Mixed { internal get; set; }",
			"public override string Mixed { internal get => null!; set { } }", "internal get")]
		public async Task MixedAccessorSlotAlreadyOverridden_WithOnlyTheSetterReachable_ShouldBeMockedWithTheSetter(
			string baseMember, string derivedOverride, string inaccessibleAccessor)
		{
			MetadataReference external = ExternalAssembly.Compile($$"""
			                                                       namespace Ext;

			                                                       public abstract class MyBaseType
			                                                       {
			                                                       	{{baseMember}}
			                                                       }

			                                                       public abstract class MyExternalType : MyBaseType
			                                                       {
			                                                       	{{derivedOverride}}
			                                                       }
			                                                       """);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyExternalType.g.cs"])
				.Contains("global::Mockolate.Setup.IPropertySetterOnlySetup<string> Mixed { get; }").And
				.Contains(
					"global::Mockolate.Verify.VerificationPropertySetterResult<IMockVerifyForMyExternalType, string> Mixed { get; }")
				.Because("the mock does not intercept the inaccessible getter, so neither surface may offer it").And
				.Contains("public override string Mixed").And
				.Contains(".SetProperty").And
				.DoesNotContain(inaccessibleAccessor)
				.Because("the accessor is invisible to the mock's assembly, so only the reachable one may be overridden");
		}

		[Test]
		public async Task MixedAccessorIndexerSlotAlreadyOverridden_ShouldBeMockedWithTheAccessibleAccessorOnly()
		{
			MetadataReference external = ExternalAssembly.Compile("""
			                                                      namespace Ext;

			                                                      public abstract class MyBaseType
			                                                      {
			                                                      	public abstract int this[int index] { get; internal set; }
			                                                      }

			                                                      public abstract class MyExternalType : MyBaseType
			                                                      {
			                                                      	public override int this[int index] { get => 0; internal set { } }
			                                                      }
			                                                      """);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyExternalType.g.cs"])
				.Contains("public override int this[int index]").And
				.Contains("global::Mockolate.Setup.IIndexerGetterOnlySetup<int, int> this[").And
				.Contains("global::Mockolate.Verify.VerificationIndexerGetterResult<IMockVerifyForMyExternalType, int> this[").And
				.DoesNotContain("global::Mockolate.Setup.IndexerSetup<int, int> this[").And
				.DoesNotContain("internal set")
				.Because("the setter is invisible to the mock's assembly, so only the getter may be overridden");
		}

		[Test]
		public async Task MixedAccessorIndexerSlotAlreadyOverridden_WithOnlyTheSetterReachable_ShouldBeMockedWithTheSetter()
		{
			MetadataReference external = ExternalAssembly.Compile("""
			                                                      namespace Ext;

			                                                      public abstract class MyBaseType
			                                                      {
			                                                      	public abstract int this[int index] { internal get; set; }
			                                                      }

			                                                      public abstract class MyExternalType : MyBaseType
			                                                      {
			                                                      	public override int this[int index] { internal get => 0; set { } }
			                                                      }
			                                                      """);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyExternalType.g.cs"])
				.Contains("public override int this[int index]").And
				.Contains("global::Mockolate.Setup.IIndexerSetterOnlySetup<int, int> this[").And
				.Contains("global::Mockolate.Verify.VerificationIndexerSetterResult<IMockVerifyForMyExternalType, int, int> this[").And
				.DoesNotContain("global::Mockolate.Setup.IndexerSetup<int, int> this[").And
				.DoesNotContain("internal get")
				.Because("the getter is invisible to the mock's assembly, so only the setter may be overridden");
		}

		[Test]
		public async Task MixedAccessorSlotFilledByAnIntermediateType_ShouldBeMockedWithTheAccessibleAccessorOnly()
		{
			MetadataReference external = ExternalAssembly.Compile("""
			                                                      namespace Ext;

			                                                      public abstract class MyBaseType
			                                                      {
			                                                      	public abstract string Mixed { get; internal set; }
			                                                      }

			                                                      public abstract class MyMiddleType : MyBaseType
			                                                      {
			                                                      	public override string Mixed { get => null!; internal set { } }
			                                                      }

			                                                      public abstract class MyExternalType : MyMiddleType
			                                                      {
			                                                      	public abstract int Visible();
			                                                      }
			                                                      """);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyExternalType.g.cs"])
				.Contains("public override string Mixed").And
				.DoesNotContain("internal set")
				.Because("the slot is filled further up the chain, which discharges the setter just as a direct override does");
		}

		[Test]
		public async Task MixedAccessorSlot_WithInternalsVisibleTo_ShouldBeMockedWithBothAccessors()
		{
			MetadataReference external = ExternalAssembly.Compile("""
			                                                      [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestAssembly")]
			                                                      namespace Ext;

			                                                      public abstract class MyBaseType
			                                                      {
			                                                      	public abstract string Mixed { get; internal set; }
			                                                      }

			                                                      public abstract class MyExternalType : MyBaseType
			                                                      {
			                                                      	public override string Mixed { get => null!; internal set { } }
			                                                      }
			                                                      """);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyExternalType.g.cs"]).Contains("internal set");
		}

		[Test]
		public async Task MixedAccessorInterfaceSlotWithDefaultImplementation_ShouldNotBeRestated()
		{
			MetadataReference external = ExternalAssembly.Compile("""
			                                                      namespace Ext;

			                                                      public interface IMyBaseType
			                                                      {
			                                                      	string Mixed { get; internal set; }
			                                                      }

			                                                      public interface MyExternalType : IMyBaseType
			                                                      {
			                                                      	string IMyBaseType.Mixed { get => null!; set { } }
			                                                      	int Visible();
			                                                      }
			                                                      """);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.MyExternalType.g.cs"])
				.Contains("public int Visible()").And
				.DoesNotContain("Mixed")
				.Because(
					"an explicit interface implementation must supply every accessor (CS0551) and cannot reach the internal one (CS0122), so interface slots stay all-or-nothing");
		}

		[Test]
		public async Task InaccessibleAbstractMemberReDeclaredAsAbstract_ShouldNotBeMocked()
		{
			MetadataReference external = ExternalAssembly.Compile("""
			                                                      namespace Ext;

			                                                      public abstract class MyBaseType
			                                                      {
			                                                      	internal abstract void Hidden();
			                                                      }

			                                                      public abstract class MyExternalType : MyBaseType
			                                                      {
			                                                      	internal abstract override void Hidden();
			                                                      }
			                                                      """);

			GeneratorResult result = Generator.RunWithReferences(CreateMockForMyExternalType, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).DoesNotContainKey("Mock.MyExternalType.g.cs")
				.Because(
					"an `abstract override` re-declaration continues the slot without filling it, so the member is still the mock's obligation");
		}

		[Test]
		public async Task PartlyReachableSlot_WithInaccessibleSignatureType_ShouldSkipMember()
		{
			MetadataReference external = ExternalAssembly.Compile("""
			                                                      namespace Ext;

			                                                      public abstract class Base
			                                                      {
			                                                      	protected abstract Configuration Current { get; private protected set; }
			                                                      	protected class Configuration { }
			                                                      }

			                                                      public class Derived : Base
			                                                      {
			                                                      	protected override Configuration Current { get; private protected set; }
			                                                      }
			                                                      """);

			GeneratorResult result = Generator.RunWithReferences("""
			                                                     using Mockolate;

			                                                     namespace MyCode;

			                                                     public class Program
			                                                     {
			                                                         public static void Main(string[] args) => _ = Ext.Derived.CreateMock();
			                                                     }
			                                                     """, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.Derived.g.cs");
			await That(result.Sources["Mock.Derived.g.cs"])
				.DoesNotContain("Configuration").And
				.DoesNotContain("Current")
				.Because(
					"the reachable getter could be restated in the mock class, but the setup and verify surfaces would have to name `Configuration`, which is only reachable through inheritance (CS0122)");
		}

		[Test]
		public async Task ExternAliasedReference_ShouldQualifyItsTypesWithTheAlias()
		{
			MetadataReference external = CompileExternAliasAssembly().WithAliases(["ExtAlias",]);

			GeneratorResult result = Generator.RunWithReferences("""
			                                                     extern alias ExtAlias;
			                                                     using Mockolate;

			                                                     namespace MyCode;

			                                                     public interface ILocal
			                                                     {
			                                                     	ExtAlias::Ext.Payload Echo(ExtAlias::Ext.Payload payload);
			                                                     }

			                                                     public class Program
			                                                     {
			                                                         public static void Main(string[] args)
			                                                         {
			                                                     		_ = ExtAlias::Ext.IMyService.CreateMock();
			                                                     		_ = ExtAlias::Ext.MyAbstractService.CreateMock();
			                                                     		_ = ExtAlias::Ext.MyCallback.CreateMock();
			                                                     		_ = ILocal.CreateMock().Implementing<ExtAlias::Ext.IOther>();
			                                                         }
			                                                     }
			                                                     """, [external,]);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.IMyService.g.cs").And
				.ContainsKey("Mock.MyAbstractService.g.cs").And
				.ContainsKey("Mock.MyCallback.g.cs").And
				.ContainsKey("Mock.ILocal.g.cs").And
				.ContainsKey("Mock.AsExtensions.g.cs");
			await That(result.Sources["Mock.AsExtensions.g.cs"]).Contains("extern alias ExtAlias;");
			await That(result.Sources["Mock.IMyService.g.cs"])
				.Contains("""
				          // </auto-generated>
				          //----------------------
				          extern alias ExtAlias;

				          #nullable enable annotations
				          """).IgnoringNewlineStyle()
				.Because("extern alias directives must precede all other code, but the auto-generated header must stay first").And
				.Contains("ExtAlias::Ext.Payload").And
				.Contains("(ExtAlias::Ext.Color)1");
			await That(result.Sources["Mock.MyAbstractService.g.cs"])
				.Contains(
					"[ExtAlias::Ext.Marker(typeof(ExtAlias::Ext.Payload), typeof(global::System.Collections.Generic.Dictionary<,>), (ExtAlias::Ext.Color)1, new ExtAlias::Ext.Color[]{(ExtAlias::Ext.Color)1}, new global::System.Type[]{typeof(ExtAlias::Ext.Payload)})]");
			await That(string.Concat(result.Sources.Values)).DoesNotContain("global::Ext.")
				.Because("types of a reference without the global alias do not resolve under `global::` (CS0400)");
			foreach (string source in result.Sources.Values.Where(source => source.Contains("ExtAlias::")))
			{
				await That(source).Contains("extern alias ExtAlias;")
					.Because("an extern alias is only usable in a file that declares it");
			}
		}

		[Test]
		[Arguments(true)]
		[Arguments(false)]
		public async Task ExternAliasedReference_AlsoReachableThroughGlobal_ShouldKeepGlobalQualification(
			bool asSeparateReference)
		{
			MetadataReference external = CompileExternAliasAssembly();
			MetadataReference[] references = asSeparateReference
				? [external.WithAliases(["ExtAlias",]), external,]
				: [external.WithAliases(["global", "ExtAlias",]),];

			GeneratorResult result = Generator.RunWithReferences("""
			                                                     using Mockolate;

			                                                     namespace MyCode;

			                                                     public class Program
			                                                     {
			                                                         public static void Main(string[] args) => _ = Ext.IMyService.CreateMock();
			                                                     }
			                                                     """, references);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.IMyService.g.cs"])
				.Contains("global::Ext.Payload").And
				.DoesNotContain("ExtAlias");
		}

		[Test]
		public async Task ExternAliasedReference_WithUnresolvedTypeInMockedMember_ShouldStillGenerateMocks()
		{
			MetadataReference external = CompileExternAliasAssembly().WithAliases(["ExtAlias",]);

			GeneratorResult result = Generator.RunWithReferences("""
			                                                     extern alias ExtAlias;
			                                                     using Mockolate;

			                                                     namespace MyCode;

			                                                     public interface ILocal
			                                                     {
			                                                     	System.Missing Foo(ExtAlias::Ext.Payload payload);
			                                                     }

			                                                     public class Program
			                                                     {
			                                                         public static void Main(string[] args)
			                                                         {
			                                                     		_ = ILocal.CreateMock();
			                                                     		_ = ExtAlias::Ext.IOther.CreateMock();
			                                                         }
			                                                     }
			                                                     """, [external,]);

			await That(string.Join(Environment.NewLine, result.Diagnostics)).DoesNotContain("CS8785")
				.Because("an error type qualified by a namespace has no containing assembly, which must not make the generator throw");
			await That(result.Sources).ContainsKey("Mock.IOther.g.cs")
				.Because("an unresolved type in one mocked member must not suppress the other mocks");
		}

		private static MetadataReference CompileExternAliasAssembly()
			=> ExternalAssembly.Compile("""
			                            using System;
			                            using System.Threading.Tasks;

			                            namespace Ext;

			                            public enum Color { Red, Green, }

			                            public class Payload { }

			                            public class MarkerAttribute(Type type, Type openType, Color color, Color[] colors, Type[] types) : Attribute { }

			                            public class PayloadEventArgs : EventArgs { }

			                            public delegate Payload MyCallback(Payload payload);

			                            public interface IMyService
			                            {
			                            	Payload Value { get; set; }
			                            	Payload this[Color color] { get; set; }
			                            	event EventHandler<PayloadEventArgs> Changed;
			                            	Payload Get(Color color = Color.Green);
			                            	Task<Payload[]> GetAllAsync();
			                            }

			                            public interface IOther
			                            {
			                            	void Take(Payload payload);
			                            }

			                            public abstract class MyAbstractService
			                            {
			                            	[Marker(typeof(Payload), typeof(System.Collections.Generic.Dictionary<,>), Color.Green, new[] { Color.Green, }, new[] { typeof(Payload), })]
			                            	public abstract Payload Compute(Payload payload);
			                            }
			                            """);

		private static MetadataReference CompileClientBaseAssembly(bool grantsInternalsVisibleTo)
		{
			string internalsVisibleTo = grantsInternalsVisibleTo
				? """[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestAssembly")]"""
				: "";
			return ExternalAssembly.Compile($$"""
			                                 {{internalsVisibleTo}}
			                                 namespace Ext;

			                                 public class ClientBase
			                                 {
			                                 	protected ClientBase() { }
			                                 	protected ClientBase(ClientBaseConfiguration configuration) { }
			                                 	protected virtual void ApplyOptions(ClientBaseConfiguration configuration) { }
			                                 	protected internal class ClientBaseConfiguration { }
			                                 }
			                                 """);
		}

		private static MetadataReference CompileMyExternalTypeAssembly(string typeKeyword, string member,
			bool grantsInternalsVisibleTo)
		{
			string internalsVisibleTo = grantsInternalsVisibleTo
				? """[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestAssembly")]"""
				: "";
			return ExternalAssembly.Compile($$"""
			                                 {{internalsVisibleTo}}
			                                 namespace Ext;

			                                 public {{typeKeyword}} MyExternalType
			                                 {
			                                 	{{member}}
			                                 }
			                                 """);
		}

		private static MetadataReference CompileMyBaseClassAssembly(bool grantsInternalsVisibleTo)
		{
			string internalsVisibleTo = grantsInternalsVisibleTo
				? """[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestAssembly")]"""
				: "";
			return ExternalAssembly.Compile($$"""
			                                 {{internalsVisibleTo}}
			                                 namespace Ext;

			                                 public abstract class MyBaseClass
			                                 {
			                                 	public abstract void PublicAbstractMethod();
			                                 	internal virtual void InternalVirtualMethod() { }
			                                 	internal virtual int InternalVirtualProperty { get; set; }
			                                 	protected internal virtual void ProtectedInternalMethod() { }
			                                 	protected internal virtual int ProtectedInternalProperty { get; set; }
			                                 	protected internal virtual event System.EventHandler? ProtectedInternalEvent;
			                                 	public virtual int MixedAccessorProperty { get; protected internal set; }
			                                 }
			                                 """);
		}
	}
}
