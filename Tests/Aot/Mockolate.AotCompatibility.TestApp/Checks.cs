using System.Net;
using System.Text;
using Mockolate.Exceptions;
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
		new("a mocked delegate is set up and verified", () =>
		{
			Calculate sut = Calculate.CreateMock();
			sut.Mock.Setup(It.IsAny<int>(), It.Is("add")).Returns((x, _) => x + 10);
			return Equal((sut(5, "add"), sut(5, "sub")), (15, 0))
			       ?? Passes(() => sut.Mock.Verify(It.IsAny<int>(), It.Is("add")).Once());
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
