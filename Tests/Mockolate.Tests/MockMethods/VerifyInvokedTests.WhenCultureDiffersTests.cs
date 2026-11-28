using System.Globalization;
using Mockolate.Exceptions;
using Mockolate.Verify;

namespace Mockolate.Tests.MockMethods;

public sealed partial class VerifyInvokedTests
{
	public sealed class WhenCultureDiffersTests
	{
		[Test]
		public async Task WhenVerificationFails_ShouldFormatArgumentsIndependentOfCurrentCulture()
		{
			CultureInfo previousCulture = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = new CultureInfo("de-AT");
				DateTime date = new(2026, 1, 2);
				ICultureService sut = ICultureService.CreateMock();
				sut.Charge("Alice", 4.5m);
				sut.Five(1.5m, 2.5, 3.5f, "x", date);
				sut.Optional(1);
				_ = sut[4.5m, "x"];
				_ = sut[1.5m, 2.5, 3.5f, "x", date];

				string[] messages =
				[
					Capture(() => sut.Mock.Verify.Charge("Alice", 4.5m).Never()),
					Capture(() => sut.Mock.Verify.Charge(It.IsAny<string>(), 4.5m).Never()),
					Capture(() => sut.Mock.Verify.Charge("Alice", It.IsAny<decimal>()).Never()),
					Capture(() => sut.Mock.Verify.Charge(It.IsAny<string>(), It.IsOneOf(4.5m, 5.5m)).Never()),
					Capture(() => sut.Mock.Verify.Charge(It.IsAny<string>(), It.IsNotOneOf(1.5m, 2.5m)).Never()),
					Capture(() => sut.Mock.Verify.Five(1.5m, 2.5, 3.5f, "x", date).Never()),
					Capture(() => sut.Mock.Verify.Optional(It.IsAny<int>()).Never()),
					Capture(() => sut.Mock.Verify[4.5m, "x"].Got().Never()),
					Capture(() => sut.Mock.Verify[1.5m, 2.5, 3.5f, "x", date].Got().Never()),
				];

				await That(messages).IsEqualTo([
					"Expected that mock never invoked method Charge(\"Alice\", 4.5), but it did once.",
					"Expected that mock never invoked method Charge(It.IsAny<string>(), 4.5), but it did once.",
					"Expected that mock never invoked method Charge(\"Alice\", It.IsAny<decimal>()), but it did once.",
					"Expected that mock never invoked method Charge(It.IsAny<string>(), It.IsOneOf(4.5, 5.5)), but it did once.",
					"Expected that mock never invoked method Charge(It.IsAny<string>(), It.IsNotOneOf(1.5, 2.5)), but it did once.",
					"Expected that mock never invoked method Five(1.5, 2.5, 3.5, \"x\", 01/02/2026 00:00:00), but it did once.",
					"Expected that mock never invoked method Optional(It.IsAny<int>(), 1.5, 0.25), but it did once.",
					"Expected that mock never got indexer [4.5, \"x\"], but it did once.",
					"Expected that mock never got indexer [1.5, 2.5, 3.5, \"x\", 01/02/2026 00:00:00], but it did once.",
				]).Because("the message must not depend on the machine, and a decimal comma would be indistinguishable from the argument separator");
			}
			finally
			{
				CultureInfo.CurrentCulture = previousCulture;
			}
		}

		[Test]
		public async Task WhenIndexerIsNotSetup_ShouldFormatKeysIndependentOfCurrentCulture()
		{
			CultureInfo previousCulture = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = new CultureInfo("de-AT");
				DateTime date = new(2026, 1, 2);
				ICultureService sut = ICultureService.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

				string[] messages =
				[
					Capture(() => _ = sut[4.5m, "x"]),
					Capture(() => _ = sut[1.5m, 2.5, 3.5f, "x", date]),
				];

				await That(messages).IsEqualTo([
					"get indexer [4.5, \"x\"] was accessed without prior setup.",
					"get indexer [1.5, 2.5, 3.5, \"x\", 01/02/2026 00:00:00] was accessed without prior setup.",
				]).Because("the message must not depend on the machine, and a decimal comma would be indistinguishable from the argument separator");
			}
			finally
			{
				CultureInfo.CurrentCulture = previousCulture;
			}
		}

		private static string Capture(Action act)
		{
			try
			{
				act();
				return "no exception";
			}
			catch (Exception exception) when (exception is MockVerificationException or MockNotSetupException)
			{
				return exception.Message;
			}
		}

		public interface ICultureService
		{
			decimal this[decimal amount, string name] { get; }
			decimal this[decimal a, double b, float c, string d, DateTime e] { get; }
			void Charge(string name, decimal amount);
			void Five(decimal a, double b, float c, string d, DateTime e);
			void Optional(int id, decimal amount = 1.5m, double rate = 0.25);
		}
	}
}
