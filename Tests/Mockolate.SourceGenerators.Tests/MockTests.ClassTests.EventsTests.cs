namespace Mockolate.SourceGenerators.Tests;

public sealed partial class MockTests
{
	public sealed partial class ClassTests
	{
		public sealed class EventsTests
		{
			[Test]
			public async Task EventWithRefOutDelegate_ShouldPreserveModifiersOnRaise()
			{
				GeneratorResult result = Generator
					.Run("""
					     using Mockolate;

					     namespace MyCode;
					     public class Program
					     {
					         public static void Main(string[] args)
					         {
					     		_ = IMyService.CreateMock();
					         }
					     }

					     public delegate void MyEventDelegate(ref int value, out string message);

					     public interface IMyService
					     {
					         event MyEventDelegate SomeEvent;
					     }
					     """);

				await That(result.Diagnostics).IsEmpty();
			}

			[Test]
			public async Task MultipleImplementations_ShouldOnlyHaveOneExplicitImplementation()
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
					     		_ = IMyService.CreateMock().Implementing<IMyServiceBase1>().Implementing<IMyServiceBase2>();
					         }
					     }

					     public interface IMyService : IMyServiceBase1
					     {
					         new event EventHandler<string>? SomeEvent;
					     }

					     public interface IMyServiceBase1 : IMyServiceBase2
					     {
					         new event EventHandler<int>? SomeEvent;
					     }

					     public interface IMyServiceBase2
					     {
					         event EventHandler<long>? SomeEvent;
					     }
					     """);

				await That(result.Sources).ContainsKey("Mock.IMyService__IMyServiceBase1__IMyServiceBase2.g.cs");
				await That(result.Sources["Mock.IMyService__IMyServiceBase1__IMyServiceBase2.g.cs"])
					.Contains("public event global::System.EventHandler<string>? SomeEvent").Once().And
					.Contains("event global::System.EventHandler<int>? global::MyCode.IMyServiceBase1.SomeEvent").Once().And
					.Contains("event global::System.EventHandler<long>? global::MyCode.IMyServiceBase2.SomeEvent").Once();
			}

			[Test]
			public async Task ShouldGenerateEventSetupInSetupInterface()
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
					         event EventHandler SomeEvent;
					     }
					     """);

				await That(result.Sources).ContainsKey("Mock.IMyService.g.cs");
				await That(result.Sources["Mock.IMyService.g.cs"])
					.Contains("""
					          		/// <summary>
					          		///     Setup for the event <see cref="global::MyCode.IMyService.SomeEvent">SomeEvent</see>.
					          		/// </summary>
					          		global::Mockolate.Setup.EventSetup SomeEvent { get; }
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		/// <inheritdoc />
					          		[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
					          		global::Mockolate.Setup.EventSetup global::Mockolate.Mock.IMockSetupForIMyService.SomeEvent
					          		{
					          			get
					          			{
					          				global::Mockolate.Setup.EventSetup eventSetup = new global::Mockolate.Setup.EventSetup(MockRegistry, "global::MyCode.IMyService.SomeEvent");
					          				this.MockRegistry.SetupEvent(global::Mockolate.Mock.IMyService.MemberId_SomeEvent_Subscribe, eventSetup);
					          				return eventSetup;
					          			}
					          		}
					          """).IgnoringNewlineStyle();
			}

			[Test]
			public async Task ShouldImplementAllEventsFromInterfaces()
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
					         event EventHandler SomeEvent;
					         event EventHandler? SomeOtherEvent;
					     }
					     """);

				await That(result.Sources).ContainsKey("Mock.IMyService.g.cs");
				await That(result.Sources["Mock.IMyService.g.cs"])
					.Contains("""
					          		private global::System.EventHandler? _mockolateEvent_global__MyCode_IMyService_SomeEvent;
					          		/// <inheritdoc cref="global::MyCode.IMyService.SomeEvent" />
					          		public event global::System.EventHandler SomeEvent
					          		{
					          			add
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.AddEvent(global::Mockolate.Mock.IMyService.MemberId_SomeEvent_Subscribe, "global::MyCode.IMyService.SomeEvent", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyService_SomeEvent += value;
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
					          				{
					          					wraps.SomeEvent += value;
					          				}
					          			}
					          			remove
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.RemoveEvent(global::Mockolate.Mock.IMyService.MemberId_SomeEvent_Unsubscribe, "global::MyCode.IMyService.SomeEvent", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyService_SomeEvent -= value;
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
					          				{
					          					wraps.SomeEvent -= value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		private global::System.EventHandler? _mockolateEvent_global__MyCode_IMyService_SomeOtherEvent;
					          		/// <inheritdoc cref="global::MyCode.IMyService.SomeOtherEvent" />
					          		public event global::System.EventHandler? SomeOtherEvent
					          		{
					          			add
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.AddEvent(global::Mockolate.Mock.IMyService.MemberId_SomeOtherEvent_Subscribe, "global::MyCode.IMyService.SomeOtherEvent", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyService_SomeOtherEvent += value;
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
					          				{
					          					wraps.SomeOtherEvent += value;
					          				}
					          			}
					          			remove
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.RemoveEvent(global::Mockolate.Mock.IMyService.MemberId_SomeOtherEvent_Unsubscribe, "global::MyCode.IMyService.SomeOtherEvent", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyService_SomeOtherEvent -= value;
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
					          				{
					          					wraps.SomeOtherEvent -= value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle();
			}

			[Test]
			public async Task ShouldImplementImplicitlyInheritedEvents()
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

					     public interface IMyService : IMyServiceBase1
					     {
					         event EventHandler MyDirectEvent;
					     }

					     public interface IMyServiceBase1 : IMyServiceBase2
					     {
					         event EventHandler MyBaseEvent1;
					     }

					     public interface IMyServiceBase2 : IMyServiceBase3
					     {
					         event EventHandler MyBaseEvent2;
					     }

					     public interface IMyServiceBase3
					     {
					         event EventHandler MyBaseEvent3;
					     }
					     """);

				await That(result.Sources).ContainsKey("Mock.IMyService.g.cs");
				await That(result.Sources["Mock.IMyService.g.cs"])
					.Contains("""
					          		private global::System.EventHandler? _mockolateEvent_global__MyCode_IMyService_MyDirectEvent;
					          		/// <inheritdoc cref="global::MyCode.IMyService.MyDirectEvent" />
					          		public event global::System.EventHandler MyDirectEvent
					          		{
					          			add
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.AddEvent(global::Mockolate.Mock.IMyService.MemberId_MyDirectEvent_Subscribe, "global::MyCode.IMyService.MyDirectEvent", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyService_MyDirectEvent += value;
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
					          				{
					          					wraps.MyDirectEvent += value;
					          				}
					          			}
					          			remove
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.RemoveEvent(global::Mockolate.Mock.IMyService.MemberId_MyDirectEvent_Unsubscribe, "global::MyCode.IMyService.MyDirectEvent", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyService_MyDirectEvent -= value;
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyService wraps)
					          				{
					          					wraps.MyDirectEvent -= value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		private global::System.EventHandler? _mockolateEvent_global__MyCode_IMyServiceBase1_MyBaseEvent1;
					          		/// <inheritdoc cref="global::MyCode.IMyServiceBase1.MyBaseEvent1" />
					          		public event global::System.EventHandler MyBaseEvent1
					          		{
					          			add
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.AddEvent(global::Mockolate.Mock.IMyService.MemberId_MyBaseEvent1_Subscribe, "global::MyCode.IMyServiceBase1.MyBaseEvent1", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyServiceBase1_MyBaseEvent1 += value;
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyServiceBase1 wraps)
					          				{
					          					wraps.MyBaseEvent1 += value;
					          				}
					          			}
					          			remove
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.RemoveEvent(global::Mockolate.Mock.IMyService.MemberId_MyBaseEvent1_Unsubscribe, "global::MyCode.IMyServiceBase1.MyBaseEvent1", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyServiceBase1_MyBaseEvent1 -= value;
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyServiceBase1 wraps)
					          				{
					          					wraps.MyBaseEvent1 -= value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		private global::System.EventHandler? _mockolateEvent_global__MyCode_IMyServiceBase2_MyBaseEvent2;
					          		/// <inheritdoc cref="global::MyCode.IMyServiceBase2.MyBaseEvent2" />
					          		public event global::System.EventHandler MyBaseEvent2
					          		{
					          			add
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.AddEvent(global::Mockolate.Mock.IMyService.MemberId_MyBaseEvent2_Subscribe, "global::MyCode.IMyServiceBase2.MyBaseEvent2", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyServiceBase2_MyBaseEvent2 += value;
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyServiceBase2 wraps)
					          				{
					          					wraps.MyBaseEvent2 += value;
					          				}
					          			}
					          			remove
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.RemoveEvent(global::Mockolate.Mock.IMyService.MemberId_MyBaseEvent2_Unsubscribe, "global::MyCode.IMyServiceBase2.MyBaseEvent2", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyServiceBase2_MyBaseEvent2 -= value;
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyServiceBase2 wraps)
					          				{
					          					wraps.MyBaseEvent2 -= value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.Contains("""
					          		private global::System.EventHandler? _mockolateEvent_global__MyCode_IMyServiceBase3_MyBaseEvent3;
					          		/// <inheritdoc cref="global::MyCode.IMyServiceBase3.MyBaseEvent3" />
					          		public event global::System.EventHandler MyBaseEvent3
					          		{
					          			add
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.AddEvent(global::Mockolate.Mock.IMyService.MemberId_MyBaseEvent3_Subscribe, "global::MyCode.IMyServiceBase3.MyBaseEvent3", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyServiceBase3_MyBaseEvent3 += value;
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyServiceBase3 wraps)
					          				{
					          					wraps.MyBaseEvent3 += value;
					          				}
					          			}
					          			remove
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.RemoveEvent(global::Mockolate.Mock.IMyService.MemberId_MyBaseEvent3_Unsubscribe, "global::MyCode.IMyServiceBase3.MyBaseEvent3", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyServiceBase3_MyBaseEvent3 -= value;
					          				if (this.MockRegistry.Wraps is global::MyCode.IMyServiceBase3 wraps)
					          				{
					          					wraps.MyBaseEvent3 -= value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle();
			}

			[Test]
			public async Task ShouldImplementVirtualEventsOfClassesAndAllExplicitlyFromAdditionalInterfaces()
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
					     		_ = MyService.CreateMock().Implementing<IMyOtherService>();
					     		_ = MyProtectedService.CreateMock();
					         }
					     }

					     public class MyService
					     {
					         public virtual event EventHandler? SomeEvent;
					         public event EventHandler? SomeOtherEvent;
					         protected virtual event EventHandler SomeProtectedEvent;
					     }

					     public class MyProtectedService
					     {
					         protected virtual event EventHandler SomeProtectedEvent;
					     }

					     public interface IMyOtherService
					     {
					         event EventHandler SomeThirdEvent;
					     }
					     """);

				await That(result.Sources).ContainsKey("Mock.MyService__IMyOtherService.g.cs");
				await That(result.Sources["Mock.MyService__IMyOtherService.g.cs"])
					.Contains("""
					          		private global::System.EventHandler? _mockolateEvent_global__MyCode_MyService_SomeEvent;
					          		/// <inheritdoc cref="global::MyCode.MyService.SomeEvent" />
					          		public override event global::System.EventHandler? SomeEvent
					          		{
					          			add
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.AddEvent("global::MyCode.MyService.SomeEvent", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_MyService_SomeEvent += value;
					          				if (this.MockRegistry.Wraps is global::MyCode.MyService wraps)
					          				{
					          					wraps.SomeEvent += value;
					          				}
					          				if (!this.MockRegistry.Behavior.SkipBaseClass)
					          				{
					          					base.SomeEvent += value;
					          				}
					          			}
					          			remove
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.RemoveEvent("global::MyCode.MyService.SomeEvent", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_MyService_SomeEvent -= value;
					          				if (this.MockRegistry.Wraps is global::MyCode.MyService wraps)
					          				{
					          					wraps.SomeEvent -= value;
					          				}
					          				if (!this.MockRegistry.Behavior.SkipBaseClass)
					          				{
					          					base.SomeEvent -= value;
					          				}
					          			}
					          		}
					          """).IgnoringNewlineStyle().And
					.DoesNotContain("SomeOtherEvent").Because("The event is not virtual!").And
					.Contains("""
					          		private global::System.EventHandler? _mockolateEvent_global__MyCode_IMyOtherService_SomeThirdEvent;
					          		/// <inheritdoc cref="global::MyCode.IMyOtherService.SomeThirdEvent" />
					          		event global::System.EventHandler global::MyCode.IMyOtherService.SomeThirdEvent
					          		{
					          			add
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.AddEvent("global::MyCode.IMyOtherService.SomeThirdEvent", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyOtherService_SomeThirdEvent += value;
					          			}
					          			remove
					          			{
					          				if (value is not null)
					          				{
					          					this.MockRegistry.RemoveEvent("global::MyCode.IMyOtherService.SomeThirdEvent", value.Target, value.Method);
					          				}
					          				this._mockolateEvent_global__MyCode_IMyOtherService_SomeThirdEvent -= value;
					          			}
					          		}
					          """).IgnoringNewlineStyle();
			}
		}
	}
}
