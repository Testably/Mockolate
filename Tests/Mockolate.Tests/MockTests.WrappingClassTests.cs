using System.Collections.Generic;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed partial class MockTests
{
	public sealed class WrappingClassTests
	{
		[Test]
		public async Task Wrap_ClassWithoutParameterlessConstructor_ShouldDelegateToWrappedInstance()
		{
			ServiceWithConstructor myService = new("real");
			ServiceWithConstructor wrappedService = ServiceWithConstructor.CreateMock("mock").Wrapping(myService);

			string result = wrappedService.Format("value");

			await That(result).IsEqualTo("real:value");
			await That(wrappedService.Mock.Verify.Format("value")).Once();
		}

		[Test]
		public async Task Wrap_Events_ForwardEventsFromWrappedInstance()
		{
			MyChocolateDispenser myDispenser = new();
			MyChocolateDispenser wrappedDispenser = MyChocolateDispenser.CreateMock().Wrapping(myDispenser);

			string? eventType = null;
			int eventAmount = 0;

			wrappedDispenser.ChocolateDispensed += (type, amt) =>
			{
				eventType = type;
				eventAmount = amt;
			};

			myDispenser.Dispense("Milk", 3);

			await That(eventType).IsEqualTo("Milk");
			await That(eventAmount).IsEqualTo(3);
		}

		[Test]
		public async Task Wrap_Events_ForwardsFromWrapper()
		{
			MyChocolateDispenser myDispenser = new();
			MyChocolateDispenser wrappedDispenser = MyChocolateDispenser.CreateMock().Wrapping(myDispenser);

			string? eventType = null;
			int eventAmount = 0;

			myDispenser.ChocolateDispensed += (type, amt) =>
			{
				eventType = type;
				eventAmount = amt;
			};

			wrappedDispenser.Dispense("Dark", 1);

			await That(eventType).IsEqualTo("Dark");
			await That(eventAmount).IsEqualTo(1);
		}

		[Test]
		public async Task Wrap_Events_Unsubscribe_ShouldRemoveSubscription()
		{
			MyChocolateDispenser myDispenser = new();
			MyChocolateDispenser wrappedDispenser = MyChocolateDispenser.CreateMock().Wrapping(myDispenser);

			string? eventType = null;
			int eventAmount = -1;

			wrappedDispenser.ChocolateDispensed += Handler;

			myDispenser.Dispense("Milk", 3);

			await That(eventType).IsEqualTo("Milk");
			await That(eventAmount).IsEqualTo(3);

			wrappedDispenser.ChocolateDispensed -= Handler;

			myDispenser.Dispense("Dark", 6);

			await That(eventType).IsEqualTo("Milk");
			await That(eventAmount).IsEqualTo(3);

			void Handler(string type, int amount)
			{
				eventType = type;
				eventAmount = amount;
			}
		}

		[Test]
		public async Task Wrap_Indexer_ShouldDelegateToWrappedInstance()
		{
			MyChocolateDispenser myDispenser = new();
			MyChocolateDispenser wrappedDispenser = MyChocolateDispenser.CreateMock().Wrapping(myDispenser);

			wrappedDispenser["Dark"] = 12;

			await That(wrappedDispenser["Dark"]).IsEqualTo(12);
			await That(myDispenser["Dark"]).IsEqualTo(12);
			await That(wrappedDispenser["White"]).IsEqualTo(8);
			await That(myDispenser["White"]).IsEqualTo(8);
		}

		[Test]
		public async Task Wrap_Method_ShouldDelegateToWrappedInstance()
		{
			MyChocolateDispenser myDispenser = new();
			MyChocolateDispenser wrappedDispenser = MyChocolateDispenser.CreateMock().Wrapping(myDispenser);

			bool result = wrappedDispenser.Dispense("Dark", 4);

			await That(result).IsTrue();
			await That(wrappedDispenser["Dark"]).IsEqualTo(1);
			await That(myDispenser["Dark"]).IsEqualTo(1);
		}

		[Test]
		public async Task Wrap_Property_ShouldDelegateToWrappedInstance()
		{
			MyChocolateDispenser myDispenser = new();
			MyChocolateDispenser wrappedDispenser = MyChocolateDispenser.CreateMock().Wrapping(myDispenser);

			wrappedDispenser.TotalDispensed = 12;

			await That(wrappedDispenser.TotalDispensed).IsEqualTo(12);
			await That(myDispenser.TotalDispensed).IsEqualTo(12);
		}

		[Test]
		public async Task Wrap_WithSetup_ShouldOverrideMethod()
		{
			MyChocolateDispenser myDispenser = new();
			MyChocolateDispenser wrappedDispenser = MyChocolateDispenser.CreateMock().Wrapping(myDispenser);
			wrappedDispenser.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>()).Returns(false);

			bool result = wrappedDispenser.Dispense("Dark", 4);

			await That(result).IsFalse();
			await That(wrappedDispenser["Dark"]).IsEqualTo(1);
			await That(myDispenser.TotalDispensed).IsEqualTo(4);
		}

		[Test]
		public async Task Wrapping_ShouldIgnoreProtectedMembers()
		{
			ServiceWithProtectedMembers myClass = new();
			ServiceWithProtectedMembers myMock = ServiceWithProtectedMembers.CreateMock().Wrapping(myClass);
			myMock.Mock.Setup.MyPublicMethod().Returns(5);

			int result = myMock.MyPublicMethod();

			await That(result).IsEqualTo(5);
			await That(myMock.Mock.Verify.MyPublicMethod()).Once();
		}

		internal class ServiceWithConstructor
		{
			private readonly string _prefix;

			public ServiceWithConstructor(string prefix)
			{
				_prefix = prefix;
			}

			public virtual string Format(string value)
				=> $"{_prefix}:{value}";
		}

		internal class ServiceWithProtectedMembers
		{
			public virtual int MyPropertyWithProtectedSetter { get; protected set; } = 10;
			public virtual int MyPropertyWithProtectedGetter { protected get; set; } = 20;

			public virtual int this[string indexerWithProtectedGetter]
			{
				protected get => 30;
				// ReSharper disable once ValueParameterNotUsed
				set { }
			}

			public virtual int this[int indexerWithProtectedSetter]
			{
				get => 30;
				// ReSharper disable once ValueParameterNotUsed
				protected set { }
			}

			protected virtual int MyProtectedMethod()
				=> 42;

			public virtual int MyPublicMethod() => MyProtectedMethod();
		}

		internal class MyChocolateDispenser : IChocolateDispenser
		{
			private readonly Dictionary<string, int> _inventory = new()
			{
				{
					"Milk", 10
				},
				{
					"Dark", 5
				},
				{
					"White", 8
				},
			};

			public virtual int this[string type]
			{
				get => _inventory[type];
				set => _inventory[type] = value;
			}

			public virtual int TotalDispensed { get; set; }

			public virtual bool Dispense(string type, int amount)
			{
				if (_inventory[type] >= amount)
				{
					TotalDispensed += amount;
					_inventory[type] -= amount;
					ChocolateDispensed?.Invoke(type, amount);
					return true;
				}

				return false;
			}

			public virtual event ChocolateDispensedDelegate? ChocolateDispensed;
		}
	}
}
