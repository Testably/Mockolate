using System.Collections;
using System.Collections.Generic;

namespace Mockolate.SourceGenerators.Tests;

public sealed partial class MockTests
{
	[Test]
	public async Task DeeplyNestedClass_ShouldSetupAndVerifyForAllInheritedTypesExceptExplicitInterfaceMembers()
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
			          				throw new global::Mockolate.Exceptions.MockException("No parameterless constructor found for 'MyCode.MyBaseClass'. Please provide constructor parameters.");
			          			}
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		public MyBaseClass(global::Mockolate.MockRegistry mockRegistry, int value)
			          			: base(value)
			          		{
			          			this.MockRegistry = mockRegistry;
			          		}
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		public MyBaseClass(global::Mockolate.MockRegistry mockRegistry, int value, bool flag)
			          			: base(value, flag)
			          		{
			          			this.MockRegistry = mockRegistry;
			          		}
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
	public async Task ForTypesWithSealedOverrideEvent_ShouldNotOverrideEvent()
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
	public async Task ForTypesWithSealedOverrideIndexer_ShouldNotOverrideIndexer()
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
	public async Task ForTypesWithSealedOverrideMethod_ShouldNotOverrideMethod()
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
	public async Task ForTypesWithSealedOverrideProperty_ShouldNotOverrideProperty()
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
	public async Task HiddenGenericMethod_ShouldDelegateWrappingToDeclaringInterface()
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
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestParent
			     {
			     	IEnumerable<T> Get<T>() where T : notnull;
			     }

			     public interface ITest : ITestParent
			     {
			     	new IList<T> Get<T>() where T : notnull;
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestParent wraps)
			          				{
			          					wrappedResult = wraps.Get<T>();
			          """).IgnoringNewlineStyle()
			.Because(
				"the explicit implementation of the hidden member must delegate to the declaring interface, not to the hiding member");
	}

	[Test]
	public async Task HiddenGenericMethod_WithAdditionalConstraints_ShouldNotDelegateToHidingMember()
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
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestParent
			     {
			     	IEnumerable<T> Get<T>();
			     }

			     public interface ITest : ITestParent
			     {
			     	new IList<T> Get<T>() where T : class, new();
			     }
			     """);

		await That(result.Diagnostics).IsEmpty()
			.Because("CS0452: the hiding member requires a reference type, the hidden member does not");
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestParent wraps)
			          				{
			          					wrappedResult = wraps.Get<T>();
			          """).IgnoringNewlineStyle();
	}

	[Test]
	public async Task HiddenGenericMethod_WithNarrowedConstraints_ShouldNotDelegateToHidingMember()
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
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestParent
			     {
			     	IEnumerable<T> Get<T>() where T : notnull;
			     }

			     public interface ITest : ITestParent
			     {
			     	new IEnumerable<T> Get<T>() where T : struct;
			     }
			     """);

		await That(result.Diagnostics).IsEmpty()
			.Because("CS0453: the hiding member requires a non-nullable value type, the hidden member does not");
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestParent wraps)
			          				{
			          					wrappedResult = wraps.Get<T>();
			          """).IgnoringNewlineStyle();
	}

	[Test]
	public async Task HiddenGenericMethod_InDeepHierarchy_ShouldNotDelegateToHidingMember()
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
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface IGrandParent
			     {
			     	T Get<T>(int id) where T : notnull;
			     }

			     public interface ITestParent : IGrandParent
			     {
			     	new IEnumerable<T> Get<T>(int id) where T : notnull;
			     }

			     public interface ITest : ITestParent
			     {
			     	new IList<T> Get<T>(int id) where T : notnull;
			     }
			     """);

		await That(result.Diagnostics).IsEmpty()
			.Because("CS0266: each hidden member has its own return type");
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestParent wraps)
			          				{
			          					wrappedResult = wraps.Get<T>(id);
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.IGrandParent wraps)
			          				{
			          					wrappedResult = wraps.Get<T>(id);
			          """).IgnoringNewlineStyle()
			.Because("every level of the hierarchy must delegate to its own declaring interface");
	}

	[Test]
	public async Task HiddenProperty_ShouldDelegateWrappingToDeclaringInterface()
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
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestParent
			     {
			     	IEnumerable<int> Items { get; set; }
			     }

			     public interface ITest : ITestParent
			     {
			     	new IList<int> Items { get; set; }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty()
			.Because("CS0266: the hiding member has a narrower property type than the hidden member");
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("this.MockRegistry.Wraps is not global::MyCode.ITestParent wraps ? null : () => wraps.Items")
			.Because("the getter of the explicit implementation must read the declaring interface").And
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestParent wraps)
			          				{
			          					wraps.Items = value;
			          """).IgnoringNewlineStyle()
			.Because("the setter of the explicit implementation must write to the declaring interface");
	}

	[Test]
	public async Task HiddenProperty_WithUnrelatedType_ShouldNotDelegateToHidingMember()
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
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestParent
			     {
			     	string Label { get; set; }
			     }

			     public interface ITest : ITestParent
			     {
			     	new DateTime Label { get; set; }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty()
			.Because("CS0029: the hiding member has a type unrelated to the hidden member");
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("this.MockRegistry.Wraps is not global::MyCode.ITestParent wraps ? null : () => wraps.Label")
			.And
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestParent wraps)
			          				{
			          					wraps.Label = value;
			          """).IgnoringNewlineStyle();
	}

	[Test]
	public async Task HiddenGetOnlyProperty_ShouldDelegateWrappingToDeclaringInterface()
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
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestParent
			     {
			     	IEnumerable<int> Items { get; }
			     }

			     public interface ITest : ITestParent
			     {
			     	new IList<int> Items { get; }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("this.MockRegistry.Wraps is not global::MyCode.ITestParent wraps ? null : () => wraps.Items")
			.Because("a get-only hidden property must still read from the declaring interface");
	}

	[Test]
	public async Task HiddenInitOnlyProperty_ShouldOnlyDelegateGetterToDeclaringInterface()
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
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestParent
			     {
			     	IEnumerable<int> Items { get; init; }
			     }

			     public interface ITest : ITestParent
			     {
			     	new IList<int> Items { get; init; }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("this.MockRegistry.Wraps is not global::MyCode.ITestParent wraps ? null : () => wraps.Items")
			.Because("the getter of the explicit implementation must read the declaring interface").And
			.DoesNotContain("wraps.Items = value")
			.Because("an init accessor cannot be forwarded to an already constructed instance");
	}

	[Test]
	public async Task InitOnlyProperty_InClass_ShouldNotDelegateSetterToWrappedInstance()
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
	public async Task HiddenIndexer_ShouldDelegateWrappingToDeclaringInterface()
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
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestParent
			     {
			     	IEnumerable<int> this[int index] { get; set; }
			     }

			     public interface ITest : ITestParent
			     {
			     	new IList<int> this[int index] { get; set; }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty()
			.Because("CS0266: the hiding indexer has a narrower value type than the hidden indexer");
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("""
			          				if (this.MockRegistry.Wraps is not global::MyCode.ITestParent wraps)
			          """).IgnoringNewlineStyle()
			.Because("the getter of the explicit implementation must read the declaring interface").And
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestParent wraps)
			          				{
			          					wraps[index] = value;
			          """).IgnoringNewlineStyle()
			.Because("the setter of the explicit implementation must write to the declaring interface");
	}

	[Test]
	public async Task HiddenEvent_ShouldDelegateWrappingToDeclaringInterface()
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
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestParent
			     {
			     	event EventHandler Changed;
			     }

			     public interface ITest : ITestParent
			     {
			     	new event Action Changed;
			     }
			     """);

		await That(result.Diagnostics).IsEmpty()
			.Because("the hiding event has a delegate type unrelated to the hidden event");
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestParent wraps)
			          				{
			          					wraps.Changed += value;
			          """).IgnoringNewlineStyle()
			.Because("subscribing on the explicit implementation must subscribe on the declaring interface").And
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestParent wraps)
			          				{
			          					wraps.Changed -= value;
			          """).IgnoringNewlineStyle()
			.Because("unsubscribing on the explicit implementation must unsubscribe on the declaring interface");
	}

	[Test]
	public async Task HiddenProperty_InDeepHierarchy_ShouldNotDelegateToHidingMember()
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
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface IGrandParent
			     {
			     	object Items { get; set; }
			     }

			     public interface ITestParent : IGrandParent
			     {
			     	new IEnumerable<int> Items { get; set; }
			     }

			     public interface ITest : ITestParent
			     {
			     	new IList<int> Items { get; set; }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty()
			.Because("CS0266: each hidden member has its own property type");
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("this.MockRegistry.Wraps is not global::MyCode.ITestParent wraps ? null : () => wraps.Items")
			.And
			.Contains("this.MockRegistry.Wraps is not global::MyCode.IGrandParent wraps ? null : () => wraps.Items")
			.Because("every level of the hierarchy must delegate to its own declaring interface");
	}

	[Test]
	public async Task SiblingInterfaceProperty_ShouldDelegateWrappingToDeclaringInterface()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestLeft
			     {
			     	string Name { get; set; }
			     }

			     public interface ITestRight
			     {
			     	string Name { get; set; }
			     }

			     public interface ITest : ITestLeft, ITestRight
			     {
			     }
			     """);

		await That(result.Diagnostics).IsEmpty()
			.Because("CS0229: accessing the member on the mocked interface is ambiguous between the siblings");
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("this.MockRegistry.Wraps is not global::MyCode.ITestLeft wraps ? null : () => wraps.Name").And
			.Contains("this.MockRegistry.Wraps is not global::MyCode.ITestRight wraps ? null : () => wraps.Name").And
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestLeft wraps)
			          				{
			          					wraps.Name = value;
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestRight wraps)
			          				{
			          					wraps.Name = value;
			          """).IgnoringNewlineStyle()
			.Because("each sibling member must delegate to its own declaring interface");
	}

	[Test]
	public async Task SiblingInterfaceIndexer_ShouldDelegateWrappingToDeclaringInterface()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestLeft
			     {
			     	string this[int index] { get; set; }
			     }

			     public interface ITestRight
			     {
			     	string this[int index] { get; set; }
			     }

			     public interface ITest : ITestLeft, ITestRight
			     {
			     }
			     """);

		await That(result.Diagnostics).IsEmpty()
			.Because("CS0229: accessing the indexer on the mocked interface is ambiguous between the siblings");
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("""
			          				if (this.MockRegistry.Wraps is not global::MyCode.ITestLeft wraps)
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          				if (this.MockRegistry.Wraps is not global::MyCode.ITestRight wraps)
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestLeft wraps)
			          				{
			          					wraps[index] = value;
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestRight wraps)
			          				{
			          					wraps[index] = value;
			          """).IgnoringNewlineStyle()
			.Because("each sibling indexer must delegate to its own declaring interface");
	}

	[Test]
	public async Task SiblingInterfaceEvent_ShouldDelegateWrappingToDeclaringInterface()
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
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestLeft
			     {
			     	event EventHandler Changed;
			     }

			     public interface ITestRight
			     {
			     	event EventHandler Changed;
			     }

			     public interface ITest : ITestLeft, ITestRight
			     {
			     }
			     """);

		await That(result.Diagnostics).IsEmpty()
			.Because("CS0229: subscribing on the mocked interface is ambiguous between the siblings");
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestLeft wraps)
			          				{
			          					wraps.Changed += value;
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestLeft wraps)
			          				{
			          					wraps.Changed -= value;
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestRight wraps)
			          				{
			          					wraps.Changed += value;
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          				if (this.MockRegistry.Wraps is global::MyCode.ITestRight wraps)
			          				{
			          					wraps.Changed -= value;
			          """).IgnoringNewlineStyle()
			.Because("each sibling event must subscribe and unsubscribe on its own declaring interface");
	}

	[Test]
	public async Task SiblingInterfaceMethod_ShouldDelegateWrappingToDeclaringInterface()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = ITest.CreateMock();
			         }
			     }

			     public interface ITestLeft
			     {
			     	int Count(string value);
			     }

			     public interface ITestRight
			     {
			     	int Count(string value);
			     }

			     public interface ITest : ITestLeft, ITestRight
			     {
			     }
			     """);

		await That(result.Diagnostics).IsEmpty()
			.Because("CS0121: calling the method on the mocked interface is ambiguous between the siblings");
		await That(result.Sources["Mock.ITest.g.cs"])
			.Contains("if (this.MockRegistry.Wraps is global::MyCode.ITestLeft wraps)").And
			.Contains("if (this.MockRegistry.Wraps is global::MyCode.ITestRight wraps)")
			.Because("each sibling method must delegate to its own declaring interface");
	}

	[Test]
	public async Task MembersWithReservedNames_ShouldPrefixAtSymbol()
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

			     public interface IMyService
			     {
			         int @class { get; }
			         string @return();
			         void @event(int @params);
			         int @void<@class>(int @ref);
			         string this[int @params, string @void] { get; set; }
			         event EventHandler @event;
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.IMyService.g.cs");
		await That(result.Sources["Mock.IMyService.g.cs"])
			.Contains("public int @class").And
			.Contains("public string @return()").And
			.Contains("public void @event(int @params)").And
			.Contains("public int @void<@class>(int @ref)").And
			.Contains("public string this[int @params, string @void]").And
			.Contains("public event global::System.EventHandler @event").And
			.Contains("private global::System.EventHandler? _mockolateEvent_global__MyCode_IMyService_event;").And
			.DoesNotContain("_mockolateEvent_global__MyCode_IMyService_@event");
		;
	}

	[Test]
	public async Task MethodOrIndexerParametersWithReservedNames_ShouldPrefixAtSymbol()
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

			     public interface IMyService
			     {
			         string this[int @true] { get; set; }
			         void DoSomething(int @event);
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.IMyService.g.cs");
		await That(result.Sources["Mock.IMyService.g.cs"])
			.Contains("""
			          public string this[int @true]
			          """).And
			.Contains("""
			          public void DoSomething(int @event)
			          """);

		await That(result.Sources).ContainsKey("Mock.IMyService.g.cs");
		await That(result.Sources["Mock.IMyService.g.cs"])
			.Contains("""
			          		global::Mockolate.Setup.IVoidMethodSetupWithCallback<int> global::Mockolate.Mock.IMockSetupForIMyService.DoSomething(global::Mockolate.Parameters.IParameter<int>? @event)
			          		{
			          			var methodSetup = new global::Mockolate.Setup.VoidMethodSetup<int>.WithParameterCollection(MockRegistry, "global::MyCode.IMyService.DoSomething", CovariantParameterAdapter<int>.Wrap(@event ?? global::Mockolate.It.IsNull<int>("null")));
			          			this.MockRegistry.SetupMethod(global::Mockolate.Mock.IMyService.MemberId_DoSomething, methodSetup);
			          			return methodSetup;
			          		}
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		global::Mockolate.Setup.IndexerSetup<string, int> global::Mockolate.Mock.IMockSetupForIMyService.this[global::Mockolate.Parameters.IParameter<int>? parameter1]
			          		{
			          			get
			          			{
			          				var indexerSetup = new global::Mockolate.Setup.IndexerSetup<string, int>(MockRegistry, CovariantParameterAdapter<int>.Wrap(parameter1 ?? global::Mockolate.It.IsNull<int>("null")));
			          				this.MockRegistry.SetupIndexer(global::Mockolate.Mock.IMyService.MemberId_Indexer_int_Get, indexerSetup);
			          				return indexerSetup;
			          			}
			          		}
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		global::Mockolate.Verify.VerificationResult<IMockVerifyForIMyService> IMockVerifyForIMyService.DoSomething(global::Mockolate.Parameters.IParameter<int>? @event)
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		global::Mockolate.Verify.VerificationIndexerResult<IMockVerifyForIMyService, string> IMockVerifyForIMyService.this[global::Mockolate.Parameters.IParameter<int>? @true]
			          """).IgnoringNewlineStyle();
	}

	[Test]
	public async Task ShouldHandleComplexInheritanceWithSealedAndInternalMembers()
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
	public async Task ShouldSupportSpecialTypes()
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

			     public interface IMyService
			     {
			         void MyMethod(object v1, bool v2, string v3, char v4, byte v5, sbyte v6, short v7, ushort v8, int v9, uint v10, long v11, ulong v12, float v13, double v14, decimal v15);
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.IMyService.g.cs");
		await That(result.Sources["Mock.IMyService.g.cs"])
			.Contains("""
			          		public void MyMethod(object v1, bool v2, string v3, char v4, byte v5, sbyte v6, short v7, ushort v8, int v9, uint v10, long v11, ulong v12, float v13, double v14, decimal v15)
			          		{
			          """)
			.IgnoringNewlineStyle().And
			.Contains(
				"foreach (global::Mockolate.Setup.VoidMethodSetup<object, bool, string, char, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal> s_methodSetup in this.MockRegistry.GetMethodSetups<global::Mockolate.Setup.VoidMethodSetup<object, bool, string, char, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal>>(\"global::MyCode.IMyService.MyMethod\"))")
			.IgnoringNewlineStyle().And
			.Contains("""
			          			bool hasWrappedResult = false;
			          			if (this.MockRegistry.Behavior.SkipInteractionRecording == false)
			          			{
			          				this.MockolateBuffer_MyMethod.Append("global::MyCode.IMyService.MyMethod", v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15);
			          			}
			          			try
			          			{
			          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
			          				{
			          					wraps.MyMethod(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15);
			          					hasWrappedResult = true;
			          				}
			          			}
			          			finally
			          			{
			          				methodSetup?.TriggerCallbacks(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15);
			          			}
			          			if (methodSetup is null && !hasWrappedResult && this.MockRegistry.Behavior.ThrowWhenNotSetup)
			          			{
			          				throw new global::Mockolate.Exceptions.MockNotSetupException("The method 'global::MyCode.IMyService.MyMethod(object, bool, string, char, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal)' was invoked without prior setup.");
			          			}
			          		}
			          """).IgnoringNewlineStyle();
	}
}
