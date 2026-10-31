using System.Diagnostics;
using System.Threading;
using aweXpect.Chronology;
using Mockolate.Exceptions;
using Mockolate.Tests.TestHelpers;
using Mockolate.Verify;

namespace Mockolate.Tests.Verify;

public sealed partial class VerificationResultTests
{
	public class AsyncTests
	{
		[Test]
		public async Task MultipleWithin_ShouldOverwritePreviousTimeout()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();

			void Act()
			{
				sut.Mock.Verify.Dispense(Match.AnyParameters())
					.Within(100.Milliseconds())
					.Within(200.Milliseconds())
					.AtLeastOnce();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage(
					"Expected that mock invoked method Dispense(Match.AnyParameters()) at least once, but it timed out after 00:00:00.2000000.");
		}

		[Test]
		public async Task Verify_OnAwaitable_WhenPredicateBecomesSatisfied_ShouldReturnTrue()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();

			VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result = sut.Mock.Verify.Dispense(Match.AnyParameters())
				.Within(30.Seconds());
			using CancellationTokenSource cts = new();
			CancellationToken token = cts.Token;

			Task backgroundTask = Task.Run(async () =>
			{
				for (int i = 0; i < 1000; i++)
				{
					await Task.Delay(10, CancellationToken.None).ConfigureAwait(false);
					sut.Dispense("Dark", i);
					if (token.IsCancellationRequested)
					{
						break;
					}
				}
			}, token);

			await That(((IVerificationResult)result).Verify(l => l.Length > 0)).IsTrue();
			cts.Cancel();
			await backgroundTask;
		}

		[Test]
		public async Task Verify_OnAwaitable_WhenPredicateIsAlreadySatisfied_ShouldReturnTrue()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Dispense("Dark", 1);
			sut.Dispense("Dark", 2);

			VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result = sut.Mock.Verify.Dispense(Match.AnyParameters())
				.Within(500.Milliseconds());

			await That(((IVerificationResult)result).Verify(l => l.Length > 0)).IsTrue();
		}

		[Test]
		public async Task Verify_OnAwaitable_WhenPredicateIsNeverSatisfied_ShouldThrowTimeoutException()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();

			VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result = sut.Mock.Verify.Dispense(Match.AnyParameters())
				.Within(50.Milliseconds());

			void Act()
			{
				((IVerificationResult)result).Verify(l => l.Length > 0);
			}

			await That(Act).Throws<MockVerificationTimeoutException>();
		}

		[Test]
		public async Task VerifyAsync_WhenAlreadySuccessful_ShouldReturnTrue()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Dispense("Dark", 1);
			sut.Dispense("Dark", 2);

			VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result = sut.Mock.Verify.Dispense(Match.AnyParameters())
				.Within(500.Milliseconds());

			await That(((IAsyncVerificationResult)result).VerifyAsync(l => l.Length > 0, CancellationToken.None)).IsTrue();
		}

		[Test]
		public async Task VerifyAsync_WhenMultipleIterationsAreNecessary_ShouldStopWhenSuccessful()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();

			VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result = sut.Mock.Verify.Dispense(Match.AnyParameters())
				.Within(30.Seconds());
			using CancellationTokenSource cts = new();
			CancellationToken token = cts.Token;

			Task backgroundTask = Task.Run(async () =>
			{
				for (int i = 0; i < 1000; i++)
				{
					await Task.Delay(10, CancellationToken.None).ConfigureAwait(false);
					sut.Dispense("Dark", i);
					if (token.IsCancellationRequested)
					{
						break;
					}
				}
			}, token);

			await That(((IAsyncVerificationResult)result).VerifyAsync(l => l.Length > 20, CancellationToken.None)).IsTrue();
			cts.Cancel();
			await backgroundTask;
		}

		[Test]
		public async Task VerifyAsync_WithCancellationToken_ShouldKeepConfiguredTimeout()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			using CancellationTokenSource cts = new(30.Seconds());

			VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result = sut.Mock.Verify.Dispense(Match.AnyParameters())
				.Within(50.Milliseconds());

			Task Act()
				=> ((IAsyncVerificationResult)result).VerifyAsync(l => l.Length > 0, cts.Token);

			await That(Act).Throws<MockVerificationTimeoutException>()
				.Whose(e => e.Timeout, t => t.IsEqualTo(50.Milliseconds()));
		}

		[Test]
		public async Task VerifyAsync_WithCancellationToken_ShouldLeaveResultUnchanged()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			CancellationToken canceledToken = new(true);

			VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result = sut.Mock.Verify.Dispense(Match.AnyParameters())
				.Within(50.Milliseconds());

			Task ActWithToken()
				=> ((IAsyncVerificationResult)result).VerifyAsync(l => l.Length > 0, canceledToken);

			Task ActWithoutToken()
				=> ((IAsyncVerificationResult)result).VerifyAsync(l => l.Length > 0, CancellationToken.None);

			await That(ActWithToken).Throws<OperationCanceledException>();
			await That(ActWithoutToken).Throws<MockVerificationTimeoutException>()
				.Whose(e => e.Timeout, t => t.IsEqualTo(50.Milliseconds()));
		}

		[Test]
		public async Task VerifyAsync_WithCancellationToken_ShouldStopWaitingWhenConfiguredTokenIsCanceled()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			using CancellationTokenSource ownCts = new(50.Milliseconds());
			using CancellationTokenSource cts = new(30.Seconds());

			VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result = sut.Mock.Verify.Dispense(Match.AnyParameters())
				.WithCancellation(ownCts.Token);

			Task Act()
				=> ((IAsyncVerificationResult)result).VerifyAsync(l => l.Length > 0, cts.Token);

			await That(Act).Throws<MockVerificationTimeoutException>()
				.Whose(e => e.Timeout, t => t.IsNull());
		}

		[Test]
		public async Task VerifyAsync_WithCancellationToken_WhenCanceled_ShouldThrowOperationCanceledException()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			using CancellationTokenSource cts = new(50.Milliseconds());
			CancellationToken token = cts.Token;

			VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result = sut.Mock.Verify.Dispense(Match.AnyParameters())
				.Within(30.Seconds());

			Task Act()
				=> ((IAsyncVerificationResult)result).VerifyAsync(l => l.Length > 0, token);

			await That(Act).Throws<OperationCanceledException>()
				.Whose(e => e.CancellationToken, t => t.IsEqualTo(token));
		}

		[Test]
		public async Task VerifyAsync_WithCancellationToken_WhenCanceledWithoutTimeout_ShouldThrowOperationCanceledException()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			using CancellationTokenSource cts = new(50.Milliseconds());
			CancellationToken token = cts.Token;

			VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result = sut.Mock.Verify.Dispense(Match.AnyParameters())
				.WithCancellation(CancellationToken.None);

			Task Act()
				=> ((IAsyncVerificationResult)result).VerifyAsync(l => l.Length > 0, token);

			await That(Act).Throws<OperationCanceledException>()
				.Whose(e => e.CancellationToken, t => t.IsEqualTo(token));
		}

		[Test]
		public async Task WithCancellation_ShouldReturnAsyncVerificationResult()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();

			VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result = sut.Mock.Verify.Dispense(Match.AnyParameters())
				.WithCancellation(CancellationToken.None);

			await That(result).Is<IAsyncVerificationResult>();
		}

		[Test]
		public async Task WithCancellationAndTimeout_ShouldCombineBoth()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			using CancellationTokenSource cts = new(50);
			CancellationToken token = cts.Token;

			void Act()
			{
				sut.Mock.Verify.Dispense(Match.AnyParameters())
					.Within(30000.Milliseconds())
					.WithCancellation(token)
					.AtLeastOnce();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage(
					"Expected that mock invoked method Dispense(Match.AnyParameters()) at least once, but it timed out.");
		}

		[Test]
		public async Task WithCancellationAndTimeout_ShouldIncludeTimeoutInExceptionWhenLessThanCancellationToken()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			using CancellationTokenSource cts = new(30000);
			CancellationToken token = cts.Token;

			void Act()
			{
				sut.Mock.Verify.Dispense(Match.AnyParameters())
					.Within(50.Milliseconds())
					.WithCancellation(token)
					.AtLeastOnce();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage(
					"Expected that mock invoked method Dispense(Match.AnyParameters()) at least once, but it timed out after 00:00:00.0500000.");
		}

		[Test]
		public async Task WithCancellationToken_ShouldIncludeTimeoutInException()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			using CancellationTokenSource cts = new(100);
			CancellationToken token = cts.Token;

			void Act()
			{
				sut.Mock.Verify.Dispense(Match.AnyParameters()).WithCancellation(token).AtLeastOnce();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage(
					"Expected that mock invoked method Dispense(Match.AnyParameters()) at least once, but it timed out.");
		}

		[Test]
		public async Task Within_ShouldAbortAsSoonAsConditionIsSatisfied()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			using CancellationTokenSource cts = new();
			CancellationToken token = cts.Token;

			Thread producer = new(() =>
			{
				for (int i = 0; i < 100 && !token.IsCancellationRequested; i++)
				{
					Thread.Sleep(100);
					sut.Dispense("Dark", i);
				}
			});
			producer.Start();

			Stopwatch sw = Stopwatch.StartNew();
			sut.Mock.Verify.Dispense(Match.AnyParameters()).Within(30.Seconds())
				.AtLeastOnce();
			sw.Stop();
			cts.Cancel();
			producer.Join();

			await That(sw.Elapsed).IsLessThan(10.Seconds())
				.Because("the verification should stop waiting on the first interaction instead of running into the timeout");
		}

		[Test]
		public async Task Within_ShouldIncludeTimeoutInException()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();

			void Act()
			{
				sut.Mock.Verify.Dispense(Match.AnyParameters()).Within(100.Milliseconds())
					.AtLeastOnce();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage(
					"Expected that mock invoked method Dispense(Match.AnyParameters()) at least once, but it timed out after 00:00:00.1000000.");
		}

		[Test]
		public async Task Within_ShouldReturnAsyncVerificationResult()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();

			VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result = sut.Mock.Verify.Dispense(Match.AnyParameters())
				.Within(100.Milliseconds());

			await That(result).Is<IAsyncVerificationResult>();
		}

		[Test]
		public async Task Within_WhenInvokedMultipleTimesInBackground_ShouldNotThrow()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();

			Task backgroundTask = Task.Delay(50, CancellationToken.None)
				.ContinueWith(_ =>
				{
					for (int i = 0; i < 15; i++)
					{
						sut.Dispense("dark", i);
					}
				}, CancellationToken.None);

			sut.Mock.Verify.Dispense(Match.AnyParameters()).Within(30.Seconds()).AtLeast(8);

			await backgroundTask;
		}
	}
}
