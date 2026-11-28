using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Mockolate.SourceGenerators.Tests.Emission;

public sealed partial class InterfaceEmissionTests
{
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
	public async Task HiddenGenericMethod_WithDeepHierarchy_ShouldNotDelegateToHidingMember()
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
			.Contains("global::MyCode.ITestParent wraps = (global::MyCode.ITestParent)this.MockRegistry.Wraps;").And
			.Contains("() => wraps.Items);")
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
			.Contains("global::MyCode.ITestParent wraps = (global::MyCode.ITestParent)this.MockRegistry.Wraps;").And
			.Contains("() => wraps.Label);")
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
			.Contains("global::MyCode.ITestParent wraps = (global::MyCode.ITestParent)this.MockRegistry.Wraps;").And
			.Contains("() => wraps.Items);")
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
			.Contains("global::MyCode.ITestParent wraps = (global::MyCode.ITestParent)this.MockRegistry.Wraps;").And
			.Contains("() => wraps.Items);")
			.Because("the getter of the explicit implementation must read the declaring interface").And
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
	public async Task HiddenProperty_WithDeepHierarchy_ShouldNotDelegateToHidingMember()
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
			.Contains("global::MyCode.ITestParent wraps = (global::MyCode.ITestParent)this.MockRegistry.Wraps;").And
			.Contains("() => wraps.Items);")
			.And
			.Contains("global::MyCode.IGrandParent wraps = (global::MyCode.IGrandParent)this.MockRegistry.Wraps;").And
			.Contains("() => wraps.Items);")
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
			.Contains("global::MyCode.ITestLeft wraps = (global::MyCode.ITestLeft)this.MockRegistry.Wraps;").And
			.Contains("() => wraps.Name);").And
			.Contains("global::MyCode.ITestRight wraps = (global::MyCode.ITestRight)this.MockRegistry.Wraps;").And
			.Contains("() => wraps.Name);").And
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
			          			var methodSetup = new global::Mockolate.Setup.VoidMethodSetup<int>.WithParameterCollection(this.MockRegistry, "global::MyCode.IMyService.DoSomething", global::Mockolate.CovariantParameterAdapter<int>.Wrap(@event ?? global::Mockolate.Internals.Matchers.IsNull<int>()));
			          			this.MockRegistry.SetupMethod(global::Mockolate.Mock.IMyService.MemberId_DoSomething, methodSetup);
			          			return methodSetup;
			          		}
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		global::Mockolate.Setup.IndexerSetup<string, int> global::Mockolate.Mock.IMockSetupForIMyService.this[global::Mockolate.Parameters.IParameter<int>? parameter1]
			          		{
			          			get
			          			{
			          				var indexerSetup = new global::Mockolate.Setup.IndexerSetup<string, int>(MockRegistry, global::Mockolate.CovariantParameterAdapter<int>.Wrap(parameter1 ?? global::Mockolate.Internals.Matchers.IsNull<int>()));
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
	public async Task WithEvents_ShouldIncludeRaiseRemarkBullet()
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

	[Test]
	public async Task MockOfGenericTypes_ShouldHandleSpecialGenericParameters()
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
			                 _ = IMyInterface<object>.CreateMock();
			                 _ = IMyInterface<bool>.CreateMock();
			                 _ = IMyInterface<string>.CreateMock();
			                 _ = IMyInterface<char>.CreateMock();
			                 _ = IMyInterface<byte>.CreateMock();
			                 _ = IMyInterface<sbyte>.CreateMock();
			                 _ = IMyInterface<short>.CreateMock();
			                 _ = IMyInterface<ushort>.CreateMock();
			                 _ = IMyInterface<int>.CreateMock();
			                 _ = IMyInterface<uint>.CreateMock();
			                 _ = IMyInterface<long>.CreateMock();
			                 _ = IMyInterface<ulong>.CreateMock();
			                 _ = IMyInterface<float>.CreateMock();
			                 _ = IMyInterface<double>.CreateMock();
			                 _ = IMyInterface<decimal>.CreateMock();
			             }
			         }

			         public interface IMyInterface<T>
			         {
			             IEnumerable<T> MyEnumerable { get; }
			         }
			     }

			     """, typeof(IEnumerable<>));

		await That(result.Sources).ContainsKey("Mock.IMyInterface_object.g.cs");
		await That(result.Sources["Mock.IMyInterface_object.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<object> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_bool.g.cs");
		await That(result.Sources["Mock.IMyInterface_bool.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<bool> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_string.g.cs");
		await That(result.Sources["Mock.IMyInterface_string.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<string> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_char.g.cs");
		await That(result.Sources["Mock.IMyInterface_char.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<char> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_byte.g.cs");
		await That(result.Sources["Mock.IMyInterface_byte.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<byte> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_sbyte.g.cs");
		await That(result.Sources["Mock.IMyInterface_sbyte.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<sbyte> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_short.g.cs");
		await That(result.Sources["Mock.IMyInterface_short.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<short> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_ushort.g.cs");
		await That(result.Sources["Mock.IMyInterface_ushort.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<ushort> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_int.g.cs");
		await That(result.Sources["Mock.IMyInterface_int.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<int> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_uint.g.cs");
		await That(result.Sources["Mock.IMyInterface_uint.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<uint> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_long.g.cs");
		await That(result.Sources["Mock.IMyInterface_long.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<long> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_ulong.g.cs");
		await That(result.Sources["Mock.IMyInterface_ulong.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<ulong> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_float.g.cs");
		await That(result.Sources["Mock.IMyInterface_float.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<float> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_double.g.cs");
		await That(result.Sources["Mock.IMyInterface_double.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<double> MyEnumerable");
		await That(result.Sources).ContainsKey("Mock.IMyInterface_decimal.g.cs");
		await That(result.Sources["Mock.IMyInterface_decimal.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<decimal> MyEnumerable");
	}

	[Test]
	public async Task MockOfIList_ShouldContainGetEnumeratorFromIEnumerableAndIEnumerableOfT()
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
			     			var x = IList<int>.CreateMock();
			             }
			         }
			     }

			     """, typeof(IList<>));

		await That(result.Sources).ContainsKey("Mock.IList_int.g.cs");
		await That(result.Sources["Mock.IList_int.g.cs"])
			.Contains("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()").And
			.Contains("public global::System.Collections.Generic.IEnumerator<int> GetEnumerator()");
	}

	[Test]
	public async Task MockOfIList_ShouldIncludeFullNameAsGenericParameter()
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
			     			var x = IList<MyOtherCode.MyRecord>.CreateMock();
			             }
			         }
			     }

			     namespace MyOtherCode
			     {
			         public record MyRecord(int Id, string Name);
			     }

			     """, typeof(IList<>));

		await That(result.Sources).ContainsKey("Mock.IList_MyRecord.g.cs");
		await That(result.Sources["Mock.IList_MyRecord.g.cs"])
			.Contains("""
			          internal class IList_MyRecord :
			          		global::System.Collections.Generic.IList<global::MyOtherCode.MyRecord>
			          """);
	}

	[Test]
	public async Task MockOfTypesWithGenericTypes_ShouldHandleSpecialGenericParameters()
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
			     			_ = IMyInterface.CreateMock();
			             }
			         }

			         public interface IMyInterface
			         {
			             IEnumerable<object> EnumerableOfObject { get; }
			             IEnumerable<bool> EnumerableOfBool { get; }
			             IEnumerable<string> EnumerableOfString { get; }
			             IEnumerable<char> EnumerableOfChar { get; }
			             IEnumerable<byte> EnumerableOfByte { get; }
			             IEnumerable<sbyte> EnumerableOfSbyte { get; }
			             IEnumerable<short> EnumerableOfShort { get; }
			             IEnumerable<ushort> EnumerableOfUshort { get; }
			             IEnumerable<int> EnumerableOfInt { get; }
			             IEnumerable<uint> EnumerableOfUint { get; }
			             IEnumerable<long> EnumerableOfLong { get; }
			             IEnumerable<ulong> EnumerableOfUlong { get; }
			             IEnumerable<float> EnumerableOfFloat { get; }
			             IEnumerable<double> EnumerableOfDouble { get; }
			             IEnumerable<decimal> EnumerableOfDecimal { get; }
			         }
			     }

			     """, typeof(IEnumerable<>));

		await That(result.Sources).ContainsKey("Mock.IMyInterface.g.cs");
		await That(result.Sources["Mock.IMyInterface.g.cs"])
			.Contains("public global::System.Collections.Generic.IEnumerable<object> EnumerableOfObject").And
			.Contains("public global::System.Collections.Generic.IEnumerable<bool> EnumerableOfBool").And
			.Contains("public global::System.Collections.Generic.IEnumerable<string> EnumerableOfString").And
			.Contains("public global::System.Collections.Generic.IEnumerable<char> EnumerableOfChar").And
			.Contains("public global::System.Collections.Generic.IEnumerable<byte> EnumerableOfByte").And
			.Contains("public global::System.Collections.Generic.IEnumerable<sbyte> EnumerableOfSbyte").And
			.Contains("public global::System.Collections.Generic.IEnumerable<short> EnumerableOfShort").And
			.Contains("public global::System.Collections.Generic.IEnumerable<ushort> EnumerableOfUshort").And
			.Contains("public global::System.Collections.Generic.IEnumerable<int> EnumerableOfInt").And
			.Contains("public global::System.Collections.Generic.IEnumerable<uint> EnumerableOfUint").And
			.Contains("public global::System.Collections.Generic.IEnumerable<long> EnumerableOfLong").And
			.Contains("public global::System.Collections.Generic.IEnumerable<ulong> EnumerableOfUlong").And
			.Contains("public global::System.Collections.Generic.IEnumerable<float> EnumerableOfFloat").And
			.Contains("public global::System.Collections.Generic.IEnumerable<double> EnumerableOfDouble").And
			.Contains("public global::System.Collections.Generic.IEnumerable<decimal> EnumerableOfDecimal");
	}

	[Test]
	public async Task SameMethodInNestedInterfaces_ShouldUseExplicitImplementation()
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
			     			var x = IMyInterface1.CreateMock();
			             }
			         }

			         public interface IMyInterface1 : IMyInterface2
			         {
			             new void MyMethod(int v1);
			         }

			         public interface IMyInterface2
			         {
			             void MyMethod(int v1);
			         }
			     }

			     """, typeof(IList<>));

		await That(result.Diagnostics).IsEmpty();

		await That(result.Sources).ContainsKey("Mock.IMyInterface1.g.cs");
		await That(result.Sources["Mock.IMyInterface1.g.cs"])
			.Contains("public void MyMethod(int v1)").And
			.Contains("void global::MyCode.IMyInterface2.MyMethod(int v1)");
	}

	[Test]
	public async Task WithMethodNamedMock_ShouldRenameTheMockExtensionProperty()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = IMyInterface.CreateMock();
			         }
			     }

			     public interface IMyInterface
			     {
			         void Mock();
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.IMyInterface.g.cs");
		await That(result.Sources["Mock.IMyInterface.g.cs"])
			.Contains("Mockolate_Mock")
			.IgnoringNewlineStyle()
			.Because(
				"`Mock` is a method on the type, so the extension `Mock` property would shadow access and CreateUniquePropertyName must skip past it to a Mockolate_-prefixed alternative");
	}

	[Test]
	public async Task WithPropertiesNamedMockAndMockolateMock_ShouldNumberTheMockExtensionProperty()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = IMyInterface.CreateMock();
			         }
			     }

			     public interface IMyInterface
			     {
			         int Mock { get; }
			         bool Mockolate_Mock { get; }
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.IMyInterface.g.cs");
		await That(result.Sources["Mock.IMyInterface.g.cs"])
			.Contains("Mockolate_Mock__1")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenTheFirstNumberedNameIsAlsoTaken_ShouldUseTheNextNumberForTheMockExtensionProperty()
	{
		GeneratorResult result = Generator
			.Run("""
			     using Mockolate;

			     namespace MyCode;

			     public class Program
			     {
			         public static void Main(string[] args)
			         {
			     		_ = IMyInterface.CreateMock();
			         }
			     }

			     public interface IMyInterface
			     {
			         int Mock { get; }
			         bool Mockolate_Mock { get; }
			         string Mockolate_Mock__1 { get; }
			     }
			     """);

		await That(result.Sources).ContainsKey("Mock.IMyInterface.g.cs");
		await That(result.Sources["Mock.IMyInterface.g.cs"])
			.Contains("Mockolate_Mock__2")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WhenMembersOccupyAllMockRegistryNameCandidates_ShouldUseTheNextNumberedMockRegistryName()
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
			     		_ = IMyInterface.CreateMock();
			         }
			     }

			     public interface IMyInterface
			     {
			         string MockRegistry_1 { get; }
			         event EventHandler MockRegistry;
			         int MockolateMockRegistry(bool value);
			     }
			     """, typeof(EventHandler));

		await That(result.Sources).ContainsKey("Mock.IMyInterface.g.cs");
		await That(result.Sources["Mock.IMyInterface.g.cs"])
			.Contains("MockRegistry_2")
			.IgnoringNewlineStyle().And
			.DoesNotContain("private global::Mockolate.Internals.MockRegistry MockRegistry_1 { get; }")
			.IgnoringNewlineStyle();
	}

	[Test]
	public async Task WithArrayAttributeArguments_ShouldSerializeEachElementByItsKind()
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
			         [Marker(new[] { Color.Green, }, new[] { typeof(string), typeof(Payload), }, new object[] { Color.Green, typeof(int), 5, null, }, null)]
			         void MyMethod();
			     }

			     public enum Color { Red, Green, }

			     public class Payload { }

			     public class MarkerAttribute : Attribute
			     {
			         public MarkerAttribute(Color[] colors, Type[] types, object[] values, Color[] none) { }
			     }
			     """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["Mock.IMyService.g.cs"])
			.Contains(
				"[global::MyCode.Marker(new global::MyCode.Color[]{(global::MyCode.Color)1}, new global::System.Type[]{typeof(string), typeof(global::MyCode.Payload)}, new object[]{(global::MyCode.Color)1, typeof(int), 5, null}, null)]")
			.Because("array elements need the same enum casts and typeof expressions as single arguments, e.g. `{1}` does not convert to an enum (CS0266)");
	}

	[Test]
	public async Task WithAttributes_ShouldAddAttributesToGeneratedCode()
	{
		GeneratorResult result = Generator
			.Run("""
			     using System;
			     using System.ComponentModel;
			     using System.Diagnostics.CodeAnalysis;
			     using System.Data;
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
			         [AllowNull]
			         string SomeProperty { get; set; }
			         [Localizable(false)]
			         string MyMethod(string message);
			         [CustomAttributeWithWrongName(
			             true,
			             (byte)42,
			             'X',
			             3.14,
			             2.71f,
			             100,
			             999L,
			             (sbyte)-10,
			             (short)500,
			             "test",
			             123u,
			             456ul,
			             (ushort)789,
			             typeof(string),
			             MyEnum.Value2,
			             new int[] { 1, 2, 3 },
			             null,
			             BoolParam = false,
			             ByteParam = (byte)99,
			             CharParam = 'Y',
			             DoubleParam = 1.23,
			             FloatParam = 4.56f,
			             IntParam = 200,
			             LongParam = 888L,
			             SByteParam = (sbyte)-5,
			             ShortParam = (short)300,
			             StringParam = "named",
			             UIntParam = 111u,
			             ULongParam = 222ul,
			             UShortParam = (ushort)333,
			             ObjectParam = 42,
			             TypeParam = typeof(int),
			             EnumParam = MyFlagEnum.Value1 | MyFlagEnum.Value2,
			             ArrayParam = new string[] { "a", "b" },
			             OptionalIntParam = null
			         )]
			         event EventHandler<int>? MyEvent;
			     }

			     public enum MyEnum
			     {
			         Value1 = 1,
			         Value2 = 2
			     }

			     [Flags]
			     public enum MyFlagEnum
			     {
			         Value1 = 1,
			         Value2 = 2
			     }

			     [AttributeUsage(AttributeTargets.All)]
			     public class CustomAttributeWithWrongName : Attribute
			     {
			         public CustomAttribute(
			             bool boolArg,
			             byte byteArg,
			             char charArg,
			             double doubleArg,
			             float floatArg,
			             int intArg,
			             long longArg,
			             sbyte sbyteArg,
			             short shortArg,
			             string stringArg,
			             uint uintArg,
			             ulong ulongArg,
			             ushort ushortArg,
			             Type typeArg,
			             MyEnum enumArg,
			             int[] arrayArg,
			             int? optionalIntArg)
			         {
			         }

			         public bool BoolParam { get; set; }
			         public byte ByteParam { get; set; }
			         public char CharParam { get; set; }
			         public double DoubleParam { get; set; }
			         public float FloatParam { get; set; }
			         public int IntParam { get; set; }
			         public long LongParam { get; set; }
			         public sbyte SByteParam { get; set; }
			         public short ShortParam { get; set; }
			         public string StringParam { get; set; }
			         public uint UIntParam { get; set; }
			         public ulong ULongParam { get; set; }
			         public ushort UShortParam { get; set; }
			         public object ObjectParam { get; set; }
			         public Type TypeParam { get; set; }
			         public MyFlagEnum EnumParam { get; set; }
			         public string[] ArrayParam { get; set; }
			         public int? OptionalIntParam { get; set; }
			     }
			     """, typeof(AllowNullAttribute), typeof(IDataParameter), typeof(LocalizableAttribute),
				typeof(AttributeUsageAttribute));

		await That(result.Sources).ContainsKey("Mock.IMyService.g.cs");
		await That(result.Sources["Mock.IMyService.g.cs"])
			.Contains("""
			          		/// <inheritdoc cref="global::MyCode.IMyService.SomeProperty" />
			          		[global::System.Diagnostics.CodeAnalysis.AllowNull]
			          		public string SomeProperty
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		/// <inheritdoc cref="global::MyCode.IMyService.MyMethod(string)" />
			          		[global::System.ComponentModel.Localizable(false)]
			          		public string MyMethod(string message)
			          		{
			          			global::Mockolate.Setup.ReturnMethodSetup<string, string>? methodSetup = null;
			          """).IgnoringNewlineStyle().And
			.Contains(
				"if (methodSetup is null && this.MockRegistry.GetFallbackMethodSetups<global::Mockolate.Setup.ReturnMethodSetup<string, string>>(global::Mockolate.Mock.IMyService.MemberId_MyMethod, \"global::MyCode.IMyService.MyMethod\") is { } fallback_methodSetup)")
			.IgnoringNewlineStyle().And
			.Contains("""
			          			bool hasWrappedResult = false;
			          			string wrappedResult = default!;
			          			if (this.MockRegistry.Behavior.SkipInteractionRecording == false)
			          			{
			          				global::Mockolate.Internals.MethodRecords<string>.Append((global::Mockolate.Internals.MockInteractions)this.MockRegistry.Interactions, global::Mockolate.Mock.IMyService.MemberId_MyMethod, "global::MyCode.IMyService.MyMethod", message);
			          			}
			          			try
			          			{
			          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
			          				{
			          					wrappedResult = wraps.MyMethod(message);
			          					hasWrappedResult = true;
			          				}
			          			}
			          			finally
			          			{
			          				methodSetup?.TriggerCallbacks(message);
			          			}
			          			if (methodSetup is null && !hasWrappedResult && this.MockRegistry.Behavior.ThrowWhenNotSetup)
			          			{
			          				throw new global::Mockolate.Exceptions.MockNotSetupException("The method 'global::MyCode.IMyService.MyMethod(string)' was invoked without prior setup.");
			          			}
			          			if (methodSetup?.HasReturnCallbacks != true && hasWrappedResult)
			          			{
			          				return wrappedResult;
			          			}
			          			return methodSetup?.TryGetReturnValue(message, out var returnValue) == true ? returnValue : (this.MockRegistry.Behavior.DefaultValue.Probe<string>() switch { global::Mockolate.Internals.DefaultValueSource.None => default(string)!, global::Mockolate.Internals.DefaultValueSource.WithoutArguments => this.MockRegistry.Behavior.DefaultValue.Generate(default(string)!), _ => this.MockRegistry.Behavior.DefaultValue.Generate(default(string)!, message) });
			          		}
			          """).IgnoringNewlineStyle().And
			.Contains("""
			          		private global::System.EventHandler<int>? _mockolateEvent_global__MyCode_IMyService_MyEvent;
			          		/// <inheritdoc cref="global::MyCode.IMyService.MyEvent" />
			          		[global::MyCode.CustomAttributeWithWrongName(true, (byte)42, 'X', 3.14, 2.71F, 100, 999L, (sbyte)-10, (short)500, "test", 123u, 456uL, (ushort)789, typeof(string), (global::MyCode.MyEnum)2, new int[]{1, 2, 3}, null, BoolParam = false, ByteParam = (byte)99, CharParam = 'Y', DoubleParam = 1.23, FloatParam = 4.56F, IntParam = 200, LongParam = 888L, SByteParam = (sbyte)-5, ShortParam = (short)300, StringParam = "named", UIntParam = 111u, ULongParam = 222uL, UShortParam = (ushort)333, ObjectParam = 42, TypeParam = typeof(int), EnumParam = (global::MyCode.MyFlagEnum)3, ArrayParam = new string[]{"a", "b"}, OptionalIntParam = null)]
			          		public event global::System.EventHandler<int>? MyEvent
			          		{
			          			add
			          			{
			          				if (value is not null)
			          				{
			          					this.MockRegistry.AddEvent(global::Mockolate.Mock.IMyService.MemberId_MyEvent_Subscribe, "global::MyCode.IMyService.MyEvent", value.Target, value.Method);
			          				}
			          				this._mockolateEvent_global__MyCode_IMyService_MyEvent += value;
			          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
			          				{
			          					wraps.MyEvent += value;
			          				}
			          			}
			          			remove
			          			{
			          				if (value is not null)
			          				{
			          					this.MockRegistry.RemoveEvent(global::Mockolate.Mock.IMyService.MemberId_MyEvent_Unsubscribe, "global::MyCode.IMyService.MyEvent", value.Target, value.Method);
			          				}
			          				this._mockolateEvent_global__MyCode_IMyService_MyEvent -= value;
			          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
			          				{
			          					wraps.MyEvent -= value;
			          				}
			          			}
			          		}
			          """).IgnoringNewlineStyle();
	}
}
