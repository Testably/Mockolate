using System.Collections.Generic;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed partial class MockTests
{
	public sealed class WrappingInterfaceTests
	{
		[Test]
		public async Task Wrap_Events_ForwardEventsFromWrappedInstance()
		{
			MyChocolateDispenser myDispenser = new();
			IChocolateDispenser wrappedDispenser = IChocolateDispenser.CreateMock().Wrapping(myDispenser);

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
			IChocolateDispenser wrappedDispenser = IChocolateDispenser.CreateMock().Wrapping(myDispenser);

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
			IChocolateDispenser wrappedDispenser = IChocolateDispenser.CreateMock().Wrapping(myDispenser);

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
		public async Task Wrap_HiddenEvent_ShouldSubscribeOnDeclaringInterface()
		{
			MyChocolateShelf myShelf = new();
			IChocolateShelf wrappedShelf = IChocolateShelf.CreateMock().Wrapping(myShelf);

			int baseInvocations = 0;
			int shelfInvocations = 0;
			((IChocolateShelfBase)wrappedShelf).Restocked += (_, _) => baseInvocations++;
			wrappedShelf.Restocked += () => shelfInvocations++;

			myShelf.RaiseBaseRestocked();
			myShelf.RaiseShelfRestocked();

			await That(baseInvocations).IsEqualTo(1);
			await That(shelfInvocations).IsEqualTo(1);
		}

		[Test]
		public async Task Wrap_HiddenGenericMethod_ShouldDelegateToDeclaringInterface()
		{
			MyChocolateCatalog myCatalog = new();
			IChocolateCatalog wrappedCatalog = IChocolateCatalog.CreateMock().Wrapping(myCatalog);

			_ = wrappedCatalog.Get<string>();
			_ = ((IChocolateSource)wrappedCatalog).Get<string>();

			await That(myCatalog.ReceivedCalls).IsEqualTo(["catalog", "source",]);
		}

		[Test]
		public async Task Wrap_HiddenGetOnlyProperty_ShouldDelegateToDeclaringInterface()
		{
			MyChocolateShelf myShelf = new();
			IChocolateShelf wrappedShelf = IChocolateShelf.CreateMock().Wrapping(myShelf);

			await That(wrappedShelf.Featured).IsEqualTo(["Dark",]);
			await That(((IChocolateShelfBase)wrappedShelf).Featured).IsEqualTo(["Praline",]);
			await That(myShelf.ReceivedCalls).IsEqualTo(["get:shelf-featured", "get:base-featured",]);
		}

		[Test]
		public async Task Wrap_HiddenIndexer_ShouldDelegateGetterToDeclaringInterface()
		{
			MyChocolateShelf myShelf = new();
			IChocolateShelf wrappedShelf = IChocolateShelf.CreateMock().Wrapping(myShelf);

			await That(wrappedShelf[1]).IsEqualTo(["Truffle",]);
			await That(((IChocolateShelfBase)wrappedShelf)[1]).IsEqualTo(["Ganache",]);
			await That(myShelf.ReceivedCalls).IsEqualTo(["get:shelf-item", "get:base-item",]);
		}

		[Test]
		public async Task Wrap_HiddenIndexer_ShouldDelegateSetterToDeclaringInterface()
		{
			MyChocolateShelf myShelf = new();
			IChocolateShelf wrappedShelf = IChocolateShelf.CreateMock().Wrapping(myShelf);

			wrappedShelf[2] = ["Nougat",];
			((IChocolateShelfBase)wrappedShelf)[2] = ["Marzipan",];

			await That(myShelf.ReceivedCalls).IsEqualTo(["set:shelf-item", "set:base-item",]);
			await That(myShelf.ShelfItems[2]).IsEqualTo(["Nougat",]);
			await That(myShelf.BaseItems[2]).IsEqualTo(["Marzipan",]);
		}

		[Test]
		public async Task Wrap_HiddenProperty_ShouldDelegateGetterToDeclaringInterface()
		{
			MyChocolateShelf myShelf = new();
			IChocolateShelf wrappedShelf = IChocolateShelf.CreateMock().Wrapping(myShelf);

			await That(wrappedShelf.Assortment).IsEqualTo(["Milk", "Dark",]);
			await That(((IChocolateShelfBase)wrappedShelf).Assortment).IsEqualTo(["Praline",]);
			await That(myShelf.ReceivedCalls).IsEqualTo(["get:shelf", "get:base",]);
		}

		[Test]
		public async Task Wrap_HiddenProperty_ShouldDelegateSetterToDeclaringInterface()
		{
			MyChocolateShelf myShelf = new();
			IChocolateShelf wrappedShelf = IChocolateShelf.CreateMock().Wrapping(myShelf);

			wrappedShelf.Assortment = ["Truffle",];
			((IChocolateShelfBase)wrappedShelf).Assortment = ["Ganache",];

			await That(myShelf.ReceivedCalls).IsEqualTo(["set:shelf", "set:base",]);
			await That(myShelf.ShelfAssortment).IsEqualTo(["Truffle",]);
			await That(myShelf.BaseAssortment).IsEqualTo(["Ganache",]);
		}

		[Test]
		public async Task Wrap_HiddenPropertyWithUnrelatedType_ShouldDelegateToDeclaringInterface()
		{
			MyChocolateShelf myShelf = new();
			IChocolateShelf wrappedShelf = IChocolateShelf.CreateMock().Wrapping(myShelf);

			wrappedShelf.Label = 7;
			((IChocolateShelfBase)wrappedShelf).Label = "Seasonal";

			await That(wrappedShelf.Label).IsEqualTo(7);
			await That(((IChocolateShelfBase)wrappedShelf).Label).IsEqualTo("Seasonal");
			await That(myShelf.ShelfLabel).IsEqualTo(7);
			await That(myShelf.BaseLabel).IsEqualTo("Seasonal");
		}

		[Test]
		public async Task Wrap_Indexer_ShouldDelegateToWrappedInstance()
		{
			MyChocolateDispenser myDispenser = new();
			IChocolateDispenser wrappedDispenser = IChocolateDispenser.CreateMock().Wrapping(myDispenser);

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
			IChocolateDispenser wrappedDispenser = IChocolateDispenser.CreateMock().Wrapping(myDispenser);

			bool result = wrappedDispenser.Dispense("Dark", 4);

			await That(result).IsTrue();
			await That(wrappedDispenser["Dark"]).IsEqualTo(1);
			await That(myDispenser["Dark"]).IsEqualTo(1);
		}

		[Test]
		public async Task Wrap_Property_ShouldDelegateToWrappedInstance()
		{
			MyChocolateDispenser myDispenser = new();
			IChocolateDispenser wrappedDispenser = IChocolateDispenser.CreateMock().Wrapping(myDispenser);

			wrappedDispenser.TotalDispensed = 12;

			await That(wrappedDispenser.TotalDispensed).IsEqualTo(12);
			await That(myDispenser.TotalDispensed).IsEqualTo(12);
		}

		[Test]
		public async Task Wrap_SiblingInterfaceEvent_ShouldSubscribeOnDeclaringInterface()
		{
			MyChocolateGiftSet myGiftSet = new();
			IChocolateGiftSet wrappedGiftSet = IChocolateGiftSet.CreateMock().Wrapping(myGiftSet);

			int trayInvocations = 0;
			int boxInvocations = 0;
			((IChocolateTray)wrappedGiftSet).Refilled += (_, _) => trayInvocations++;
			((IChocolateBox)wrappedGiftSet).Refilled += (_, _) => boxInvocations++;

			myGiftSet.RaiseTrayRefilled();
			myGiftSet.RaiseBoxRefilled();

			await That(trayInvocations).IsEqualTo(1);
			await That(boxInvocations).IsEqualTo(1);
		}

		[Test]
		public async Task Wrap_SiblingInterfaceEvent_ShouldUnsubscribeOnDeclaringInterface()
		{
			MyChocolateGiftSet myGiftSet = new();
			IChocolateGiftSet wrappedGiftSet = IChocolateGiftSet.CreateMock().Wrapping(myGiftSet);

			int trayInvocations = 0;
			int boxInvocations = 0;
			EventHandler trayHandler = (_, _) => trayInvocations++;
			EventHandler boxHandler = (_, _) => boxInvocations++;
			((IChocolateTray)wrappedGiftSet).Refilled += trayHandler;
			((IChocolateBox)wrappedGiftSet).Refilled += boxHandler;

			((IChocolateTray)wrappedGiftSet).Refilled -= trayHandler;
			myGiftSet.RaiseTrayRefilled();
			myGiftSet.RaiseBoxRefilled();

			await That(trayInvocations).IsEqualTo(0);
			await That(boxInvocations).IsEqualTo(1);
		}

		[Test]
		public async Task Wrap_SiblingInterfaceIndexer_ShouldDelegateGetterToDeclaringInterface()
		{
			MyChocolateGiftSet myGiftSet = new();
			IChocolateGiftSet wrappedGiftSet = IChocolateGiftSet.CreateMock().Wrapping(myGiftSet);

			await That(((IChocolateTray)wrappedGiftSet)[1]).IsEqualTo(5);
			await That(((IChocolateBox)wrappedGiftSet)[1]).IsEqualTo(7);
			await That(myGiftSet.ReceivedCalls).IsEqualTo(["get:tray-slot", "get:box-slot",]);
		}

		[Test]
		public async Task Wrap_SiblingInterfaceIndexer_ShouldDelegateSetterToDeclaringInterface()
		{
			MyChocolateGiftSet myGiftSet = new();
			IChocolateGiftSet wrappedGiftSet = IChocolateGiftSet.CreateMock().Wrapping(myGiftSet);

			((IChocolateTray)wrappedGiftSet)[2] = 50;
			((IChocolateBox)wrappedGiftSet)[2] = 70;

			await That(myGiftSet.ReceivedCalls).IsEqualTo(["set:tray-slot", "set:box-slot",]);
			await That(myGiftSet.TraySlots[2]).IsEqualTo(50);
			await That(myGiftSet.BoxSlots[2]).IsEqualTo(70);
		}

		[Test]
		public async Task Wrap_SiblingInterfaceMethod_ShouldDelegateToDeclaringInterface()
		{
			MyChocolateGiftSet myGiftSet = new();
			IChocolateGiftSet wrappedGiftSet = IChocolateGiftSet.CreateMock().Wrapping(myGiftSet);

			int trayCount = ((IChocolateTray)wrappedGiftSet).Count("Dark");
			int boxCount = ((IChocolateBox)wrappedGiftSet).Count("Dark");

			await That(trayCount).IsEqualTo(2);
			await That(boxCount).IsEqualTo(1);
			await That(myGiftSet.ReceivedCalls).IsEqualTo(["count:tray", "count:box",]);
		}

		[Test]
		public async Task Wrap_SiblingInterfaceProperty_ShouldDelegateGetterToDeclaringInterface()
		{
			MyChocolateGiftSet myGiftSet = new();
			IChocolateGiftSet wrappedGiftSet = IChocolateGiftSet.CreateMock().Wrapping(myGiftSet);

			await That(((IChocolateTray)wrappedGiftSet).Flavor).IsEqualTo("Dark");
			await That(((IChocolateBox)wrappedGiftSet).Flavor).IsEqualTo("Milk");
			await That(myGiftSet.ReceivedCalls).IsEqualTo(["get:tray", "get:box",]);
		}

		[Test]
		public async Task Wrap_SiblingInterfaceProperty_ShouldDelegateSetterToDeclaringInterface()
		{
			MyChocolateGiftSet myGiftSet = new();
			IChocolateGiftSet wrappedGiftSet = IChocolateGiftSet.CreateMock().Wrapping(myGiftSet);

			((IChocolateTray)wrappedGiftSet).Flavor = "Ruby";
			((IChocolateBox)wrappedGiftSet).Flavor = "White";

			await That(myGiftSet.ReceivedCalls).IsEqualTo(["set:tray", "set:box",]);
			await That(myGiftSet.TrayFlavor).IsEqualTo("Ruby");
			await That(myGiftSet.BoxFlavor).IsEqualTo("White");
		}

		[Test]
		public async Task Wrap_WithSetup_ShouldOverrideMethod()
		{
			MyChocolateDispenser myDispenser = new();
			IChocolateDispenser wrappedDispenser = IChocolateDispenser.CreateMock().Wrapping(myDispenser);
			wrappedDispenser.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>()).Returns(false);

			bool result = wrappedDispenser.Dispense("Dark", 4);

			await That(result).IsFalse();
			await That(wrappedDispenser["Dark"]).IsEqualTo(1);
			await That(myDispenser.TotalDispensed).IsEqualTo(4);
		}

		private class MyChocolateDispenser : IChocolateDispenser
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

			public int this[string type]
			{
				get => _inventory[type];
				set => _inventory[type] = value;
			}

			public int TotalDispensed { get; set; }

			public bool Dispense(string type, int amount)
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

			public event ChocolateDispensedDelegate? ChocolateDispensed;
		}

		private class MyChocolateCatalog : IChocolateCatalog
		{
			public List<string> ReceivedCalls { get; } = [];

			public IList<T> Get<T>() where T : notnull
			{
				ReceivedCalls.Add("catalog");
				return [];
			}

			IEnumerable<T> IChocolateSource.Get<T>()
			{
				ReceivedCalls.Add("source");
				return [];
			}
		}

		private class MyChocolateShelf : IChocolateShelf
		{
			private IEnumerable<string> _baseAssortment = ["Praline",];
			private string _baseLabel = "Classic";
			private event EventHandler? _baseRestocked;

			public List<string> ReceivedCalls { get; } = [];

			public Dictionary<int, IList<string>> ShelfItems { get; } = new()
			{
				{
					1, ["Truffle",]
				},
			};

			public Dictionary<int, IEnumerable<string>> BaseItems { get; } = new()
			{
				{
					1, ["Ganache",]
				},
			};

			public IList<string> ShelfAssortment { get; private set; } = ["Milk", "Dark",];

			public IEnumerable<string> BaseAssortment => _baseAssortment;

			public int ShelfLabel { get; private set; }

			public string BaseLabel => _baseLabel;

			public IList<string> this[int index]
			{
				get
				{
					ReceivedCalls.Add("get:shelf-item");
					return ShelfItems[index];
				}
				set
				{
					ReceivedCalls.Add("set:shelf-item");
					ShelfItems[index] = value;
				}
			}

			public IList<string> Assortment
			{
				get
				{
					ReceivedCalls.Add("get:shelf");
					return ShelfAssortment;
				}
				set
				{
					ReceivedCalls.Add("set:shelf");
					ShelfAssortment = value;
				}
			}

			public IList<string> Featured
			{
				get
				{
					ReceivedCalls.Add("get:shelf-featured");
					return ["Dark",];
				}
			}

			public int Label
			{
				get => ShelfLabel;
				set => ShelfLabel = value;
			}

			public event Action? Restocked;

			IEnumerable<string> IChocolateShelfBase.this[int index]
			{
				get
				{
					ReceivedCalls.Add("get:base-item");
					return BaseItems[index];
				}
				set
				{
					ReceivedCalls.Add("set:base-item");
					BaseItems[index] = value;
				}
			}

			IEnumerable<string> IChocolateShelfBase.Assortment
			{
				get
				{
					ReceivedCalls.Add("get:base");
					return _baseAssortment;
				}
				set
				{
					ReceivedCalls.Add("set:base");
					_baseAssortment = value;
				}
			}

			IEnumerable<string> IChocolateShelfBase.Featured
			{
				get
				{
					ReceivedCalls.Add("get:base-featured");
					return ["Praline",];
				}
			}

			string IChocolateShelfBase.Label
			{
				get => _baseLabel;
				set => _baseLabel = value;
			}

			event EventHandler IChocolateShelfBase.Restocked
			{
				add => _baseRestocked += value;
				remove => _baseRestocked -= value;
			}

			public void RaiseBaseRestocked() => _baseRestocked?.Invoke(this, EventArgs.Empty);

			public void RaiseShelfRestocked() => Restocked?.Invoke();
		}

		private class MyChocolateGiftSet : IChocolateGiftSet
		{
			private event EventHandler? _boxRefilled;
			private event EventHandler? _trayRefilled;

			public List<string> ReceivedCalls { get; } = [];

			public Dictionary<int, int> BoxSlots { get; } = new()
			{
				{
					1, 7
				},
			};

			public Dictionary<int, int> TraySlots { get; } = new()
			{
				{
					1, 5
				},
			};

			public string BoxFlavor { get; private set; } = "Milk";

			public string TrayFlavor { get; private set; } = "Dark";

			int IChocolateBox.this[int slot]
			{
				get
				{
					ReceivedCalls.Add("get:box-slot");
					return BoxSlots[slot];
				}
				set
				{
					ReceivedCalls.Add("set:box-slot");
					BoxSlots[slot] = value;
				}
			}

			int IChocolateTray.this[int slot]
			{
				get
				{
					ReceivedCalls.Add("get:tray-slot");
					return TraySlots[slot];
				}
				set
				{
					ReceivedCalls.Add("set:tray-slot");
					TraySlots[slot] = value;
				}
			}

			string IChocolateBox.Flavor
			{
				get
				{
					ReceivedCalls.Add("get:box");
					return BoxFlavor;
				}
				set
				{
					ReceivedCalls.Add("set:box");
					BoxFlavor = value;
				}
			}

			string IChocolateTray.Flavor
			{
				get
				{
					ReceivedCalls.Add("get:tray");
					return TrayFlavor;
				}
				set
				{
					ReceivedCalls.Add("set:tray");
					TrayFlavor = value;
				}
			}

			event EventHandler IChocolateBox.Refilled
			{
				add => _boxRefilled += value;
				remove => _boxRefilled -= value;
			}

			event EventHandler IChocolateTray.Refilled
			{
				add => _trayRefilled += value;
				remove => _trayRefilled -= value;
			}

			int IChocolateBox.Count(string flavor)
			{
				ReceivedCalls.Add("count:box");
				return 1;
			}

			int IChocolateTray.Count(string flavor)
			{
				ReceivedCalls.Add("count:tray");
				return 2;
			}

			public void RaiseBoxRefilled() => _boxRefilled?.Invoke(this, EventArgs.Empty);

			public void RaiseTrayRefilled() => _trayRefilled?.Invoke(this, EventArgs.Empty);
		}

		public delegate void MyDelegate();
	}
}
