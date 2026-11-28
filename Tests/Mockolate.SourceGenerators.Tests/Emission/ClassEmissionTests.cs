using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Mockolate.SourceGenerators.Tests.Emission;

public sealed partial class ClassEmissionTests
{
	[Test]
	public async Task WithDeepInheritance_ShouldSetUpAndVerifyAllInheritedMembersExceptExplicitInterfaceImplementations()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Collections.Generic;
			     using System.Threading.Tasks;
			     using Mockolate;

			     namespace MyCode;

			     public interface INestedInterface
			     {
			     	int NestedValue { get; }
			     	int NestedMethod();
			     	event EventHandler NestedEvent;
			     }
			     public interface IParentInterface : INestedInterface
			     {
			     	int ParentValue { get; }
			     	int ParentMethod();
			     	event EventHandler ParentEvent;
			     }
			     public interface IDirectInterface
			     {
			     	int DirectValue { get; }
			     	int DirectMethod();
			     	event EventHandler DirectEvent;
			     }
			     public abstract class BaseClass : IParentInterface
			     {
			     	public virtual int BaseClassValue { get; } = 1;
			     	public virtual int BaseClassMethod() => 1;
			     	public virtual event EventHandler? BaseClassEvent;
			     	int IParentInterface.ParentValue { get; } = 2;
			     	int IParentInterface.ParentMethod() => 2;
			     	event EventHandler IParentInterface.ParentEvent;
			     	int INestedInterface.NestedValue { get; } = 3;
			     	int INestedInterface.NestedMethod() => 3;
			     	event EventHandler INestedInterface.NestedEvent;
			     }
			     public class OuterClass : BaseClass, IDirectInterface
			     {
			     	public virtual int OuterValue { get; } = 4;
			     	public virtual int OuterMethod() => 1;
			     	public virtual event EventHandler? OuterEvent;
			     	int IDirectInterface.DirectValue { get; } = 5;
			     	int IDirectInterface.DirectMethod() => 5;
			     	event EventHandler IDirectInterface.DirectEvent;
			     }
			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = OuterClass.CreateMock();
			         }
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.OuterClass.g.cs");
		await That(result.Sources["Mock.OuterClass.g.cs"])
			.Contains("global::Mockolate.Setup.IPropertyGetterOnlySetup<int> global::Mockolate.Mock.IMockSetupForOuterClass.OuterValue").And
			.Contains("global::Mockolate.Setup.IPropertyGetterOnlySetup<int> global::Mockolate.Mock.IMockSetupForOuterClass.BaseClassValue").And
			.DoesNotContain("IMockSetupForOuterClass.DirectValue").And
			.DoesNotContain("IMockSetupForOuterClass.ParentValue").And
			.DoesNotContain("IMockSetupForOuterClass.NestedValue").And
			.Contains("global::Mockolate.Setup.IReturnMethodSetup<int> global::Mockolate.Mock.IMockSetupForOuterClass.OuterMethod()").And
			.Contains("global::Mockolate.Setup.IReturnMethodSetup<int> global::Mockolate.Mock.IMockSetupForOuterClass.BaseClassMethod()").And
			.DoesNotContain("global::Mockolate.Setup.IReturnMethodSetup<int> global::Mockolate.Mock.IMockSetupForOuterClass.DirectMethod()").And
			.DoesNotContain("global::Mockolate.Setup.IReturnMethodSetup<int> global::Mockolate.Mock.IMockSetupForOuterClass.ParentMethod()").And
			.DoesNotContain("global::Mockolate.Setup.IReturnMethodSetup<int> global::Mockolate.Mock.IMockSetupForOuterClass.NestedMethod()").And
			.Contains("void IMockRaiseOnOuterClass.OuterEvent(object? sender, global::System.EventArgs e)").And
			.Contains("void IMockRaiseOnOuterClass.BaseClassEvent(object? sender, global::System.EventArgs e)").And
			.DoesNotContain("void IMockRaiseOnOuterClass.DirectEvent(object? sender, global::System.EventArgs e)").And
			.DoesNotContain("void IMockRaiseOnOuterClass.ParentEvent(object? sender, global::System.EventArgs e)").And
			.DoesNotContain("void IMockRaiseOnOuterClass.NestedEvent(object? sender, global::System.EventArgs e)").And
			.Contains("global::Mockolate.Verify.VerificationPropertyGetterResult<IMockVerifyForOuterClass> IMockVerifyForOuterClass.OuterValue").And
			.Contains("global::Mockolate.Verify.VerificationPropertyGetterResult<IMockVerifyForOuterClass> IMockVerifyForOuterClass.BaseClassValue").And
			.DoesNotContain("IMockVerifyForOuterClass.DirectValue").And
			.DoesNotContain("IMockVerifyForOuterClass.ParentValue").And
			.DoesNotContain("IMockVerifyForOuterClass.NestedValue").And
			.Contains("global::Mockolate.Verify.VerificationResult<IMockVerifyForOuterClass>.IgnoreParameters IMockVerifyForOuterClass.OuterMethod()").And
			.Contains("global::Mockolate.Verify.VerificationResult<IMockVerifyForOuterClass>.IgnoreParameters IMockVerifyForOuterClass.BaseClassMethod()").And
			.DoesNotContain("IMockVerifyForOuterClass.DirectMethod()").And
			.DoesNotContain("IMockVerifyForOuterClass.ParentMethod()").And
			.DoesNotContain("IMockVerifyForOuterClass.NestedMethod()").And
			.Contains("global::Mockolate.Verify.VerificationEventResult<IMockVerifyForOuterClass> IMockVerifyForOuterClass.OuterEvent").And
			.Contains("global::Mockolate.Verify.VerificationEventResult<IMockVerifyForOuterClass> IMockVerifyForOuterClass.BaseClassEvent").And
			.DoesNotContain("global::Mockolate.Verify.VerificationEventResult<IMockVerifyForOuterClass> IMockVerifyForOuterClass.DirectEvent").And
			.DoesNotContain("global::Mockolate.Verify.VerificationEventResult<IMockVerifyForOuterClass> IMockVerifyForOuterClass.ParentEvent").And
			.DoesNotContain("global::Mockolate.Verify.VerificationEventResult<IMockVerifyForOuterClass> IMockVerifyForOuterClass.NestedEvent");
	}

	[Test]
	public async Task ExplicitInterfaceImplementation_ShouldNotAddAccessibility()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Collections;
			     using System.Collections.Generic;
			     using System.Threading.Tasks;
			     using Mockolate;

			     namespace MyCode;

			     public abstract class MyService : IEnumerable<int>
			     {
			     	public IEnumerator<int> GetEnumerator()
			     	{
			     		return new List<int>().GetEnumerator();
			     	}

			     	IEnumerator IEnumerable.GetEnumerator()
			     	{
			     		return GetEnumerator();
			     	}
			     }
			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyService.CreateMock();
			         }
			     }
			     """, typeof(IEnumerator), typeof(IEnumerable<int>));

		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.DoesNotContain("private global::System.Collections.IEnumerator GetEnumerator()");
	}

	[Test]
	public async Task WithSealedOverrideEvent_ShouldNotOverrideEvent()
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
			     		_ = MyClassWithSealedEvents.CreateMock();
			         }
			     }

			     public class MyClassWithSealedEvents : MySubClass
			     {
			     	public sealed override event EventHandler<long> SomeEvent;
			     }

			     public class MySubClass
			     {
			     	public virtual event EventHandler<long> SomeEvent;
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.MyClassWithSealedEvents.g.cs");
		await That(result.Sources["Mock.MyClassWithSealedEvents.g.cs"])
			.DoesNotContain("event System.EventHandler<long>? SomeEvent");
	}

	[Test]
	public async Task WithSealedOverrideIndexer_ShouldNotOverrideIndexer()
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
			     		_ = MyClassWithSealedIndexers.CreateMock();
			         }
			     }

			     public class MyClassWithSealedIndexers : MySubClass
			     {
			     	public sealed override int this[int index] { get => 3 * index; }
			     }

			     public class MySubClass
			     {
			     	public virtual int this[int index] { get => 2 * index; }
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.MyClassWithSealedIndexers.g.cs");
		await That(result.Sources["Mock.MyClassWithSealedIndexers.g.cs"])
			.DoesNotContain("override int this[int index]");
	}

	[Test]
	public async Task WithSealedOverrideMethod_ShouldNotOverrideMethod()
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
			     		_ = MyClassWithSealedMethods.CreateMock();
			         }
			     }

			     public class MyClassWithSealedMethods : MySubClass
			     {
			     	public sealed override void MyMethod(int value)
			     		=> base.MyMethod(value);
			     }

			     public class MySubClass
			     {
			     	public virtual void MyMethod(int value) { }
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.MyClassWithSealedMethods.g.cs");
		await That(result.Sources["Mock.MyClassWithSealedMethods.g.cs"])
			.DoesNotContain("override void MyMethod(int value)");
	}

	[Test]
	public async Task WithSealedOverrideProperty_ShouldNotOverrideProperty()
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
			     		_ = MyClassWithSealedProperties.CreateMock();
			         }
			     }

			     public class MyClassWithSealedProperties : MySubClass
			     {
			     	public sealed override int MyProperty { get; set; }
			     }

			     public class MySubClass
			     {
			     	public virtual int MyProperty { get; set; }
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.MyClassWithSealedProperties.g.cs");
		await That(result.Sources["Mock.MyClassWithSealedProperties.g.cs"])
			.DoesNotContain("override int MyProperty");
	}

	[Test]
	public async Task InitOnlyProperty_ShouldNotDelegateSetterToWrappedInstance()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyClass.CreateMock();
			         }
			     }

			     public class MyClass
			     {
			     	public virtual int Items { get; init; }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["Mock.MyClass.g.cs"])
			.Contains("base.Items = value;").And
			.DoesNotContain("wraps.Items = value")
			.Because("an init accessor cannot be forwarded to an already constructed instance");
	}

	[Test]
	public async Task WithSealedAndInternalMembersInTheHierarchy_ShouldOverrideAllButTheSealedMembers()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyDerivedClass.CreateMock();
			         }
			     }

			     public class MyDerivedClass : MyMiddleClass
			     {
			     }

			     public class MyMiddleClass : MyBaseClass
			     {
			     	public sealed override void SealedMethod() { }
			     	protected internal override void ProtectedInternalMethod() { }
			     }

			     public class MyBaseClass
			     {
			     	public virtual void SealedMethod() { }
			     	public virtual void NormalMethod() { }
			     	protected internal virtual void ProtectedInternalMethod() { }
			     	internal virtual void InternalMethod() { }
			     	protected virtual void ProtectedMethod() { }
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.MyDerivedClass.g.cs");
		await That(result.Sources["Mock.MyDerivedClass.g.cs"])
			.DoesNotContain("override void SealedMethod").And
			.Contains("ProtectedInternalMethod").And
			.Contains("InternalMethod").And
			.Contains("override void NormalMethod").And
			.Contains("override void ProtectedMethod");
	}

	[Test]
	public async Task ShouldNotIncludeSealedOverrideSpecialMethods()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyDerivedClass.CreateMock();
			         }
			     }

			     public class MyDerivedClass : MyMiddleClass
			     {
			     }

			     public class MyMiddleClass : MyBaseClass
			     {
			     	public sealed override bool Equals(object? obj) => base.Equals(obj);
			     	public sealed override int GetHashCode() => base.GetHashCode();
			     	public sealed override string? ToString() => base.ToString();
			     }

			     public class MyBaseClass
			     {
			     	public virtual void SomeMethod() { }
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.MyDerivedClass.g.cs");
		await That(result.Sources["Mock.MyDerivedClass.g.cs"])
			.DoesNotContain("override bool Equals").And
			.DoesNotContain("override int GetHashCode").And
			.DoesNotContain("override string ToString");
	}

	[Test]
	public async Task ShouldNotIncludeSealedOverrideSpecialMethodsWithNonNullableParameters()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = MyDerivedClass.CreateMock();
			         }
			     }

			     public class MyDerivedClass : MyMiddleClass
			     {
			     }

			     public class MyMiddleClass : MyBaseClass
			     {
			     	public sealed override bool Equals(object obj) => base.Equals(obj);
			     }

			     public class MyBaseClass
			     {
			     	public virtual void SomeMethod() { }
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.MyDerivedClass.g.cs");
		await That(result.Sources["Mock.MyDerivedClass.g.cs"])
			.DoesNotContain("override bool Equals")
			.Because(
				"MyMiddleClass.Equals(object) must still match and filter out object.Equals(object?) despite the nullability difference");
	}

	[Test]
	public async Task ShouldPreserveProtectedInternalAccessibilityOnOverriddenMembers()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;
			     using System;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args) => _ = MyClass.CreateMock();
			     }

			     public class MyClass
			     {
			     	protected internal virtual void ProtectedInternalMethod() { }
			     	protected internal virtual int ProtectedInternalProperty { get; set; }
			     	protected internal virtual event EventHandler? ProtectedInternalEvent;
			     	public virtual int MixedAccessorProperty { get; protected internal set; }
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.MyClass.g.cs");
		string generated = result.Sources["Mock.MyClass.g.cs"];
		await That(generated)
			.Contains("protected internal override void ProtectedInternalMethod()").And
			.Contains("protected internal override int ProtectedInternalProperty").And
			.Contains("protected internal override event global::System.EventHandler? ProtectedInternalEvent").And
			.Contains("protected internal set").And
			.DoesNotContain("protected override void ProtectedInternalMethod").And
			.DoesNotContain("protected override int ProtectedInternalProperty").And
			.DoesNotContain("protected override event");
	}

	[Test]
	public async Task HttpMessageHandler_ShouldOverrideOnlyTheProtectedDispose()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System.Net.Http;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			var messageHandler = HttpMessageHandler.CreateMock();
			     			var httpClient = new HttpClient(messageHandler);
			             }
			         }
			     }
			     """, typeof(HttpMessageHandler));

		await That(result.Sources).ContainsKey("Mock.HttpMessageHandler.g.cs");
		await That(result.Sources["Mock.HttpMessageHandler.g.cs"])
			.Contains("protected override void Dispose(bool disposing)").And
			.DoesNotContain("void Dispose()");
	}

	[Test]
	public async Task ObsoleteAttributes_ShouldBeRepeatedInMock()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.Collections.Generic;
			     using Mockolate;

			     namespace MyCode
			     {
			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			var x = MyBaseClass.CreateMock();
			             }
			         }

			         public class MyBaseClass
			         {
			              [Obsolete()]
			              public MyBaseClass()
			              {
			                 SomeProperty = 1;
			              }
			              
			              [Obsolete("This constructor is obsolete")]
			              public MyBaseClass(int value)
			              {
			                 SomeProperty = value;
			              }
			              
			              [Obsolete("This event is obsolete")]
			              public virtual event EventHandler<int> SomeEvent;
			              
			              [Obsolete("This property is obsolete")]
			              public virtual int SomeProperty { get; }
			              
			              [Obsolete("This method is obsolete")]
			              public virtual int SomeMethod()
			              {
			                SomeEvent?.Invoke(this, SomeProperty);
			                return 3;
			              }
			              
			              [Obsolete("This indexer is obsolete")]
			              public virtual int this[int index] => 4;
			         }
			     }

			     """, typeof(ObsoleteAttribute));

		await That(result.Sources).ContainsKey("Mock.MyBaseClass.g.cs");
		await That(result.Sources["Mock.MyBaseClass.g.cs"])
			.Contains("""
			          		[global::System.Obsolete]
			          		public MyBaseClass(global::Mockolate.Internals.MockRegistry mockRegistry)
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		[global::System.Obsolete("This constructor is obsolete")]
			          		public MyBaseClass(global::Mockolate.Internals.MockRegistry mockRegistry, int value)
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		[global::System.Obsolete("This event is obsolete")]
			          		public override event global::System.EventHandler<int> SomeEvent
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		[global::System.Obsolete("This property is obsolete")]
			          		public override int SomeProperty
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		[global::System.Obsolete("This indexer is obsolete")]
			          		public override int this[int index]
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		[global::System.Obsolete("This method is obsolete")]
			          		public override int SomeMethod()
			          """).IgnoringNewlineStyle();
	}

	[Test]
	public async Task WithFieldNamedMockRegistry_ShouldFallBackToAlternative()
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
			         protected int MockRegistry;
			         public virtual int Run() => 0;
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("MockolateMockRegistry")
			.IgnoringNewlineStyle().And
			.DoesNotContain("private global::Mockolate.Internals.MockRegistry MockRegistry { get; }")
			.IgnoringNewlineStyle()
			.Because(
				"a mock subclass declaring a property `MockRegistry` would hide the inherited field (CS0108), so the dedup pipeline must skip past the conflicting member name");
	}

	[Test]
	public async Task WithNestedTypeNamedMockRegistry_ShouldFallBackToAlternative()
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
			         public class MockRegistry { }
			         public virtual int Run() => 0;
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.MyService.g.cs");
		await That(result.Sources["Mock.MyService.g.cs"])
			.Contains("MockolateMockRegistry")
			.IgnoringNewlineStyle().And
			.DoesNotContain("private global::Mockolate.Internals.MockRegistry MockRegistry { get; }")
			.IgnoringNewlineStyle();
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
	public async Task WithClassesImplementingLogger_ShouldDispatchTheirLogCalls()
	{
		GeneratorResult result = Generator
			.Run("""
			     #nullable enable
			     using System;
			     using Microsoft.Extensions.Logging;
			     using Mockolate;
			     namespace MyCode
			     {
			         public abstract class AbstractLogger : ILogger
			         {
			             public abstract IDisposable? BeginScope<TState>(TState state) where TState : notnull;
			             public abstract bool IsEnabled(LogLevel logLevel);
			             public abstract void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter);
			         }

			         public class VirtualLogger : ILogger
			         {
			             public virtual IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
			             public virtual bool IsEnabled(LogLevel logLevel) => true;
			             public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
			         }

			         public class Program
			         {
			             public static void Main(string[] args)
			             {
			     			_ = AbstractLogger.CreateMock();
			     			_ = VirtualLogger.CreateMock();
			             }
			         }
			     }
			     """, typeof(ILogger));

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Logging.g.cs");
		await That(result.Sources["Mock.AbstractLogger.g.cs"])
			.Contains("global::Mockolate.MockolateLogging.DispatchLog(this.MockRegistry, global::Mockolate.Mock.AbstractLogger.MemberId_Log_TState_, \"global::MyCode.AbstractLogger.Log\", null, logLevel, eventId, state, exception, formatter);");
		await That(result.Sources["Mock.VirtualLogger.g.cs"])
			.Contains(", () => base.Log<TState>(logLevel, eventId, state, exception, formatter));");
	}
}
