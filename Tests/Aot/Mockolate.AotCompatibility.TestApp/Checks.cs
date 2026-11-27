#pragma warning disable CA1873 // Avoid potentially expensive logging
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Mockolate.Exceptions;
using Mockolate.Integrations;
using Mockolate.Internals;
using Mockolate.Parameters;
using Mockolate.Verify;
using Mockolate.Web;

namespace Mockolate.AotCompatibility.TestApp;

internal static class Checks
{
	public static readonly Check[] All =
	[
		new("a method setup returns the value for matching arguments", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Mock.Setup.Dispense(It.Is("Dark"), It.IsAny<int>()).Returns(true);
			return Equal((sut.Dispense("Dark", 1), sut.Dispense("Milk", 1)), (true, false));
		}),
		new("an initialized property returns the last set value", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Mock.Setup.Stock.InitializeWith(5);
			int initial = sut.Stock;
			sut.Stock = 7;
			return Equal((initial, sut.Stock), (5, 7));
		}),
		new("an indexer setup returns the value for matching keys", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Mock.Setup[It.Is("Dark")].Returns(3);
			return Equal((sut["Dark"], sut["Milk"]), (3, 0));
		}),
		new("a returns sequence with For and Forever returns the values in order", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
				.Returns(true).For(2)
				.Returns(false).Forever();
			return Equal(string.Join(",", Enumerable.Range(0, 4).Select(i => sut.Dispense("Dark", i))),
				"True,True,False,False");
		}),
		new("a throws setup limited with Only throws only once", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
				.Throws(new InvalidOperationException("empty")).OnlyOnce();
			return Throws<InvalidOperationException>(() => sut.Dispense("Dark", 1), "empty")
			       ?? Equal(sut.Dispense("Dark", 1), false);
		}),
		new("a Do callback receives the arguments", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			string? received = null;
			sut.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
				.Do((type, amount) => received = $"{type}:{amount}");
			sut.Dispense("Dark", 3);
			return Equal(received, "Dark:3");
		}),
		new("ReturnsAsync completes a Task", async () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Mock.Setup.CountAsync(It.IsAny<string>()).ReturnsAsync(4);
			return Equal(await sut.CountAsync("Dark"), 4);
		}),
		new("ReturnsAsync completes a ValueTask", async () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Mock.Setup.IsAvailableAsync(It.IsAny<string>()).ReturnsAsync(true);
			return Equal(await sut.IsAvailableAsync("Dark"), true);
		}),
		new("ref and out parameters are set by the setup", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Mock.Setup.Refill(It.IsRef<int>(v => v + 1), It.IsOut(() => true));
			int amount = 5;
			sut.Refill(ref amount, out bool overflow);
			return Equal((amount, overflow), (6, true));
		}),
		new("a member without setup returns the default value", async () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			return Equal((sut.Dispense("Dark", 1), sut.Stock, await sut.CountAsync("Dark")), (false, 0, 0));
		}),
		new("a member without setup returns an empty IAsyncEnumerable", async () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			List<string> result = [];
			await foreach (string item in sut.ListAsync(CancellationToken.None))
			{
				result.Add(item);
			}

			return Equal(result.Count, 0);
		}),
#if MOCKOLATE_AUTOMOCKING
		new("auto-mocking returns a configurable mock for an interface without setup", () =>
		{
			IChocolateShop sut = IChocolateShop.CreateMock();
			sut.GetDispenser().Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>()).Returns(true);
			sut["Main"].Mock.Setup.Stock.InitializeWith(5);
			return Equal((sut.GetDispenser().Dispense("Dark", 1), sut["Main"].Stock), (true, 5));
		}),
#else
		new("a member that returns an interface returns null without auto-mocking", () =>
		{
			IChocolateShop sut = IChocolateShop.CreateMock();
			return Equal((sut.GetDispenser(), sut["Main"]), ((IChocolateDispenser)null!, (IChocolateDispenser)null!));
		}),
#endif
		new("ThrowWhenNotSetup throws for a member without setup", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
			return Throws<MockNotSetupException>(() => sut.Dispense("Dark", 1), "Dispense");
		}),
		new("It.Is with Within matches values within the tolerance", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Mock.Setup.Temper(It.Is(31.5).Within(0.1)).Returns(true);
			return Equal((sut.Temper(31.45), sut.Temper(31.7)), (true, false));
		}),
		new("It.Satisfies matches values by the predicate", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Mock.Setup.Dispense(It.IsAny<string>(), It.Satisfies<int>(amount => amount > 2)).Returns(true);
			return Equal((sut.Dispense("Dark", 3), sut.Dispense("Dark", 1)), (true, false));
		}),
		new("Monitor records the passed values", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Mock.Setup.Dispense(It.Is("Dark"), It.IsAny<int>().Monitor(out IParameterMonitor<int> monitor));
			sut.Dispense("Dark", 5);
			sut.Dispense("Milk", 6);
			sut.Dispense("Dark", 7);
			return Equal(string.Join(",", monitor.Values), "5,7");
		}),
		new("a verification counts the matching interactions", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Dispense("Dark", 1);
			sut.Dispense("Dark", 2);
			_ = sut.Stock;
			return Passes(() =>
			{
				sut.Mock.Verify.Dispense(It.Is("Dark"), It.IsAny<int>()).AtLeast(2);
				sut.Mock.Verify.Dispense(It.Is("Milk"), It.IsAny<int>()).Never();
				sut.Mock.Verify.Stock.Got().Once();
			});
		}),
		new("a failing verification names the expected and the actual count", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Dispense("Dark", 1);
			return Throws<MockVerificationException>(
				() => sut.Mock.Verify.Dispense(It.Is("Dark"), It.IsAny<int>()).Twice(),
				"Expected that mock invoked method Dispense(\"Dark\", It.IsAny<int>()) exactly twice, but it did once.");
		}),
		new("Then verifies the order of the interactions", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Dispense("Dark", 1);
			sut.Dispense("Milk", 2);
			return Passes(() => sut.Mock.Verify.Dispense(It.Is("Dark"), It.IsAny<int>())
				       .Then(m => m.Dispense(It.Is("Milk"), It.IsAny<int>())))
			       ?? Throws<MockVerificationException>(() => sut.Mock.Verify.Dispense(It.Is("Milk"), It.IsAny<int>())
				       .Then(m => m.Dispense(It.Is("Dark"), It.IsAny<int>())), "Milk");
		}),
		new("Then verifies the order of the interactions across tracked mocks", () =>
		{
			IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
			Calculate calculate = Calculate.CreateMock();
			using (dispenser.Mock.TrackCallOrder(calculate.Mock))
			{
				dispenser.Dispense("Dark", 1);
				calculate(5, "add");
			}

			return Passes(() => dispenser.Mock.Verify.Dispense(It.Is("Dark"), It.IsAny<int>())
				       .Then(calculate.Mock.Verify(It.IsAny<int>(), It.Is("add"))))
			       ?? Throws<MockVerificationException>(() => calculate.Mock.Verify(It.IsAny<int>(), It.Is("add"))
				       .Then(dispenser.Mock.Verify.Dispense(It.Is("Dark"), It.IsAny<int>())), "too early");
		}),
		new("an interaction that was not verified is reported", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Dispense("Dark", 1);
			sut.Dispense("Milk", 2);
			sut.Mock.Verify.Dispense(It.Is("Dark"), It.IsAny<int>()).Once();
			return Equal(sut.Mock.VerifyThatAllInteractionsAreVerified(), false);
		}),
		new("a raised event reaches the subscribed handlers", () =>
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			int subscriptions = 0;
			int received = 0;
			sut.Mock.Setup.Dispensed.OnSubscribed.Do(() => subscriptions++);
			sut.Dispensed += (_, amount) => received += amount;
			sut.Mock.Raise.Dispensed(sut, 3);
			sut.Mock.Raise.Dispensed(sut, 4);
			return Equal((subscriptions, received), (1, 7))
			       ?? Passes(() => sut.Mock.Verify.Dispensed.Subscribed().Once());
		}),
		new("a protected member is set up and verified", () =>
		{
			ChocolateMachine sut = ChocolateMachine.CreateMock("Testably");
			sut.Mock.SetupProtected.DispenseInternal(It.Is("Dark")).Returns(3);
			return Equal((sut.Dispense("Dark"), sut.Dispense("Milk")), (3, 0))
			       ?? Passes(() => sut.Mock.VerifyProtected.DispenseInternal(It.Is("Dark")).Once());
		}),
		new("a class mock passes the constructor arguments to the base class", () =>
		{
			ChocolateMachine sut = ChocolateMachine.CreateMock("Testably");
			return Equal(sut.Describe(), "A machine by Testably");
		}),
		new("a class mock fills its constructor dependencies with mocks", () =>
		{
			ChocolateVendor sut = ChocolateVendor.CreateMock();
			IChocolateDispenser dispenser = sut.Mock.ConstructorArgument<IChocolateDispenser>();
			dispenser.Mock.Setup.Dispense(It.Is("Dark"), It.IsAny<int>()).Returns(true);
			return Equal((sut.Buy("Dark", 1), sut.Buy("Milk", 1), sut.Mock.ConstructorArgument<string>("brand")),
				       ("Enjoy your Testably Dark", "Sold out", "Testably"))
			       ?? Passes(() => dispenser.Mock.Verify.Dispense(It.IsAny<string>(), It.IsAny<int>()).Twice());
		}),
		new("a static abstract member is set up, raised and verified", () =>
		{
			IChocolateFactory sut = IChocolateFactory.CreateMock();
			sut.Mock.SetupStatic.ProduceBatch(It.Is("Dark"), It.IsAny<int>()).Returns(42);
			int completed = 0;
			Subscribe<Mock.IChocolateFactory>(amount => completed += amount);
			int produced = Produce<Mock.IChocolateFactory>("Dark", 10);
			sut.Mock.RaiseStatic.BatchCompleted(produced);
			return Equal((produced, completed), (42, 42))
			       ?? Passes(() =>
			       {
				       sut.Mock.VerifyStatic.ProduceBatch(It.Is("Dark"), It.IsAny<int>()).Once();
				       sut.Mock.VerifyStatic.BatchCompleted.Subscribed().Once();
			       });
		}),
		new("a mocked HttpClient matches the request URI and content", async () =>
		{
			HttpClient sut = HttpClient.CreateMock();
			sut.Mock.Setup.GetAsync(It.IsUri("*testably.org/stock*"))
				.ReturnsAsync(HttpStatusCode.OK, "42");
			sut.Mock.Setup.PostAsync(It.IsAny<string>(),
					It.IsHttpContent("application/json").WithStringMatching("*\"type\":\"Dark\"*"))
				.ReturnsAsync(HttpStatusCode.Created);

			HttpResponseMessage stock = await sut.GetAsync("https://testably.org/stock");
			HttpResponseMessage created = await sut.PostAsync("https://testably.org/order",
				new StringContent("{\"type\":\"Dark\"}", Encoding.UTF8, "application/json"));
			HttpResponseMessage other = await sut.PostAsync("https://testably.org/order",
				new StringContent("{\"type\":\"Milk\"}", Encoding.UTF8, "application/json"));

			return Equal((await stock.Content.ReadAsStringAsync(), created.StatusCode, other.StatusCode == HttpStatusCode.Created),
				       ("42", HttpStatusCode.Created, false))
			       ?? Passes(() => sut.Mock.Verify.PostAsync(It.IsUri("*testably.org/order").ForHttps(),
				       It.IsHttpContent("application/json").WithStringMatching("*Dark*")).Once());
		}),
		new("a mocked logger factory matches the messages and templates of its loggers", () =>
		{
			ILoggerFactory sut = ILoggerFactory.CreateMock();
			int errors = 0;
			sut.Mock.GetLogger<ChocolateMachine>().Mock.Setup.LogError().WithMessage(It.Matches("*failed*")).Do(() => errors++);
			ILogger<ChocolateMachine> logger = sut.CreateLogger<ChocolateMachine>();
			logger.LogError("Order {Id} failed", 42);
			logger.LogInformation("Order {Id} created", 43);
			return Equal((errors, logger.IsEnabled(LogLevel.Debug)), (1, true))
			       ?? Passes(() => sut.Mock.Verify.LogInformation().WithMessage(It.MatchesTemplate("Order {Id} created").WithProperty("Id", 43)).Once());
		}),
		new("a mocked delegate is set up and verified", () =>
		{
			Calculate sut = Calculate.CreateMock();
			sut.Mock.Setup(It.IsAny<int>(), It.Is("add")).Returns((x, _) => x + 10);
			return Equal((sut(5, "add"), sut(5, "sub")), (15, 0))
			       ?? Passes(() => sut.Mock.Verify(It.IsAny<int>(), It.Is("add")).Once());
		}),
		new("a mock data source creates a class with created dependencies and a resolved mock", () =>
		{
			IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
			dispenser.Mock.Setup.Dispense(It.Is("Dark"), It.IsAny<int>()).Returns(true);
			bool created = MockDataSource.TryCreate(typeof(Checks).Assembly, typeof(VendingMachine), null,
				(Type type, string _, out object? value) =>
				{
					value = dispenser;
					return type == typeof(IChocolateDispenser);
				}, out object? sut);
			VendingMachine machine = (VendingMachine)sut!;
			return Equal((created, machine.Buy("Dark", 2), machine.Counter.Total,
					       ReferenceEquals(machine.Counter.Dispenser, dispenser)), (true, true, 2, true))
			       ?? Equal(MockDataSource.TryCreate(typeof(Checks).Assembly, typeof(IChocolateShop), null,
				       (Type _, string _, out object? value) =>
				       {
					       value = null;
					       return false;
				       }, out _), false);
		}),
		new("the AutoMockArguments code shares the parameter mocks with the class under test", () =>
		{
			object?[] values = new AutoMockRow(typeof(Checks).Assembly,
				[typeof(string), typeof(VendingMachine), typeof(IChocolateDispenser),],
				["type", "sut", "dispenser",], _ => false, _ => false, _ => null, _ => null, ["Dark",], null).Create();
			VendingMachine machine = (VendingMachine)values[1]!;
			IChocolateDispenser dispenser = (IChocolateDispenser)values[2]!;
			dispenser.Mock.Setup.Dispense(It.Is("Dark"), It.IsAny<int>()).Returns(true);
			return Equal((values[0], machine.Buy("Dark", 1), ReferenceEquals(machine.Counter.Dispenser, dispenser)),
				((object?)"Dark", true, true));
		}),
		new("a deferred AutoMockArguments parameter creates the class under test with the parameter mocks when called", () =>
		{
			object?[] values = new AutoMockRow(typeof(Checks).Assembly,
				[typeof(Func<VendingMachine>), typeof(IChocolateDispenser),],
				["createSut", "dispenser",], _ => false, index => index == 0, _ => null, _ => null, [], null).Create();
			IChocolateDispenser dispenser = (IChocolateDispenser)values[1]!;
			dispenser.Mock.Setup.Dispense(It.Is("Dark"), It.IsAny<int>()).Returns(true);
			Func<VendingMachine> createSut = (Func<VendingMachine>)values[0]!;
			VendingMachine first = createSut();
			return Equal((first.Buy("Dark", 1), ReferenceEquals(first.Counter.Dispenser, dispenser),
				ReferenceEquals(first, createSut())), (true, true, false));
		}),
		new("a ValueFrom AutoMockArguments parameter reads a private static property that the trimmer keeps", () =>
		{
			System.Reflection.ParameterInfo parameter =
				new Func<string, string>(MockDataSourceTargets.FromMember).Method.GetParameters()[0];
			return Equal(AutoMockRow.GetValueFrom(parameter)?.Invoke(), (object?)"Lobby");
		}),
		new("a ValueFrom AutoMockArguments parameter of a nested test class reads a property that the trimmer keeps", () =>
		{
			System.Reflection.ParameterInfo parameter =
				new Func<string, string>(MockDataSourceTargets.Nested.FromNestedMember).Method.GetParameters()[0];
			return Equal(AutoMockRow.GetValueFrom(parameter)?.Invoke(), (object?)"Hall");
		}),
		new("a value provider AutoMockArguments parameter gets the value of its attribute", () =>
		{
			System.Reflection.ParameterInfo parameter =
				new Func<string, string>(MockDataSourceTargets.FromProvider).Method.GetParameters()[0];
			return Equal(AutoMockRow.GetValueFrom(parameter)?.Invoke(), (object?)"entrance hall");
		}),
		new("CreateMock with an out parameter creates a sealed class with mocked dependencies", () =>
		{
			SealedVendingMachine sut = SealedVendingMachine.CreateMock(out ConstructorArguments arguments, location: "Lobby");
			arguments.Get<IChocolateDispenser>().Mock.Setup.Dispense(It.Is("Dark"), It.IsAny<int>()).Returns(true);
			return Equal((sut.Buy("Dark", 1), sut.Buy("Milk", 1), sut.Location, arguments.Get<string>("location")),
				(true, false, "Lobby", "Lobby"));
		}),
		new("a mock data source fills other types from the default values and finds no code in other assemblies", () =>
		{
			MockArgumentResolver noArgument = (Type _, string _, out object? value) =>
			{
				value = null;
				return false;
			};
			bool createdText = MockDataSource.TryCreate(typeof(Checks).Assembly, typeof(string), null, noArgument,
				out object? text);
			bool createdForeign = MockDataSource.TryCreate(typeof(string).Assembly, typeof(VendingMachine), null,
				noArgument, out object? foreign);
			return Equal((createdText, text, createdForeign, foreign), (true, (object?)"", false, (object?)null));
		}),
	];

	private static string? Equal<T>(T actual, T expected)
		=> EqualityComparer<T>.Default.Equals(actual, expected) ? null : $"expected {expected}, but was {actual}";

	private static string? Passes(Action act)
	{
		try
		{
			act();
			return null;
		}
		catch (Exception ex)
		{
			return $"expected no exception, but {ex.GetType().Name} was thrown: {ex.Message}";
		}
	}

	private static string? Throws<TException>(Action act, string messagePart) where TException : Exception
	{
		try
		{
			act();
			return $"expected a {typeof(TException).Name}, but none was thrown";
		}
		catch (TException ex) when (ex.Message.Contains(messagePart, StringComparison.Ordinal))
		{
			return null;
		}
		catch (Exception ex)
		{
			return $"expected a {typeof(TException).Name} containing '{messagePart}', but {ex.GetType().Name} was thrown: {ex.Message}";
		}
	}

	private static int Produce<T>(string type, int amount) where T : IChocolateFactory
		=> T.ProduceBatch(type, amount);

	private static void Subscribe<T>(Action<int> handler) where T : IChocolateFactory
		=> T.BatchCompleted += handler;
}
