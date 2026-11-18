using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Verify;

namespace Mockolate.Tests.MockMethods;

public sealed partial class VerifyInvokedTests
{
	public sealed class AnyParametersTests
	{
		[Test]
		public async Task AnyParameters_WhenVerificationFails_ShouldDescribeAnyParameters()
		{
			IAnyParametersService sut = IAnyParametersService.CreateMock();
			sut.Zero();
			sut.One(1);
			sut.Two("Bob", 1m);
			sut.Three(1, 2, 3);
			sut.Four(1, 2, 3, 4);
			sut.Five(1, 2, 3, 4, 5);
			sut.Generic(1);

			string[] messages =
			[
				Capture(() => sut.Mock.Verify.Zero().AnyParameters().Never()),
				Capture(() => sut.Mock.Verify.One(9).AnyParameters().Never()),
				Capture(() => sut.Mock.Verify.Two("Alice", 4.5m).AnyParameters().Never()),
				Capture(() => sut.Mock.Verify.Three(9, 9, 9).AnyParameters().Twice()),
				Capture(() => sut.Mock.Verify.Four(9, 9, 9, 9).AnyParameters().AtLeast(2)),
				Capture(() => sut.Mock.Verify.Five(9, 9, 9, 9, 9).AnyParameters().Exactly(3)),
				Capture(() => sut.Mock.Verify.Generic(9).AnyParameters().Never()),
			];

			await That(messages).IsEqualTo([
				"Expected that mock never invoked method Zero(Match.AnyParameters()), but it did once.",
				"Expected that mock never invoked method One(Match.AnyParameters()), but it did once.",
				"Expected that mock never invoked method Two(Match.AnyParameters()), but it did once.",
				"Expected that mock invoked method Three(Match.AnyParameters()) exactly twice, but it did once.",
				"Expected that mock invoked method Four(Match.AnyParameters()) at least twice, but it did once.",
				"Expected that mock invoked method Five(Match.AnyParameters()) exactly 3 times, but it did once.",
				"Expected that mock never invoked method Generic<T>(Match.AnyParameters()), but it did once.",
			]).Because("the verification counts the calls with any arguments, so the message must not name the original ones");
		}

		[Test]
		public async Task AnyParameters_WhenWaitingOrOrderedVerificationFails_ShouldDescribeAnyParameters()
		{
			IAnyParametersService sut = IAnyParametersService.CreateMock();
			sut.Two("Bob", 1m);
			sut.One(1);
			using CancellationTokenSource expired = new();
			expired.Cancel();

			string[] messages =
			[
				Capture(() => sut.Mock.Verify.Three(9, 9, 9).AnyParameters().Within(TimeSpan.FromMilliseconds(10)).Once()),
				Capture(() => sut.Mock.Verify.Three(9, 9, 9).AnyParameters().WithCancellation(expired.Token).Once()),
				Capture(() => sut.Mock.Verify.One(9).AnyParameters().Then(m => m.Two("Alice", 4.5m).AnyParameters())),
				Capture(() => sut.Mock.Verify.Two("Alice", 4.5m).AnyParameters().Then(m => m.One(9).AnyParameters())),
			];

			await That(messages).IsEqualTo([
				"Expected that mock invoked method Three(Match.AnyParameters()) exactly once, but it timed out after 00:00:00.0100000.",
				"Expected that mock invoked method Three(Match.AnyParameters()) exactly once, but it timed out.",
				"Expected that mock invoked method One(Match.AnyParameters()), then invoked method Two(Match.AnyParameters()) in order, but it invoked method Two(Match.AnyParameters()) too early.",
				"no exception",
			]).Because("the copies of the result for waiting and ordering keep the widening to any parameters");
		}

#if NET11_0_OR_GREATER
		[Test]
		public async Task AnyParameters_WithMatchersAndPredicates_WhenVerificationFails_ShouldDescribeAnyParameters()
		{
			IAnyParametersService sut = IAnyParametersService.CreateMock();
			sut.One(1);
			sut.Two("Bob", 1m);
			sut.Five(1, 2, 3, 4, 5);

			string[] messages =
			[
				Capture(() => sut.Mock.Verify.One(It.Is(9)).AnyParameters().Never()),
				Capture(() => sut.Mock.Verify.One(x => x > 5).AnyParameters().Never()),
				Capture(() => sut.Mock.Verify.Two(It.Is<string?>("Alice"), It.IsAny<decimal>()).AnyParameters().Never()),
				Capture(() => sut.Mock.Verify.Two("Alice", It.Is(4.5m)).AnyParameters().Never()),
				Capture(() => sut.Mock.Verify.Two(x => x == "Alice", 4.5m).AnyParameters().Never()),
				Capture(() => sut.Mock.Verify.Five(9, It.IsAny<int>(), 9, 9, 9).AnyParameters().Never()),
			];

			await That(messages).IsEqualTo([
				"Expected that mock never invoked method One(Match.AnyParameters()), but it did once.",
				"Expected that mock never invoked method One(Match.AnyParameters()), but it did once.",
				"Expected that mock never invoked method Two(Match.AnyParameters()), but it did once.",
				"Expected that mock never invoked method Two(Match.AnyParameters()), but it did once.",
				"Expected that mock never invoked method Two(Match.AnyParameters()), but it did once.",
				"Expected that mock never invoked method Five(Match.AnyParameters()), but it did once.",
			]).Because("the union overloads expose AnyParameters for matchers, predicates and mixed arguments as well");
		}
#endif

		private static string Capture(Action act)
		{
			try
			{
				act();
				return "no exception";
			}
			catch (MockVerificationException exception)
			{
				return exception.Message;
			}
		}

		public interface IAnyParametersService
		{
			void Zero();
			void One(int a);
			void Two(string? name, decimal amount);
			void Three(int a, int b, int c);
			void Four(int a, int b, int c, int d);
			void Five(int a, int b, int c, int d, int e);
			T Generic<T>(T value);
		}
	}
}
