using System.Threading;

namespace Mockolate.Tests.MockMethods;

public sealed partial class InteractionsTests
{
	public sealed class CancellationTokenTests
	{
		[Test]
		public async Task WithMultipleParameters_WhenOneIsCanceled_ShouldReturnCanceledTask()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			Task<int> result = sut.MultipleParametersMethod("test", 123, canceledToken);

			await That(result.IsCanceled).IsTrue();
		}

		[Test]
		public async Task WithNonCanceledToken_ShouldReturnDefaultValue()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken nonCanceledToken = new(false);

			Task result = sut.TaskMethod(nonCanceledToken);

			await That(result.IsCompleted).IsTrue();
			await That(result.IsCanceled).IsFalse();
		}

		[Test]
		public async Task WithSetup_ShouldUseSetupValue()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);
			sut.Mock.Setup.TaskOfIntMethod(It.IsAny<CancellationToken>()).Returns(Task.FromResult(42));

			Task<int> result = sut.TaskOfIntMethod(canceledToken);

			await That(result.IsCompleted).IsTrue();
			await That(result.IsCanceled).IsFalse();
			await That(await result).IsEqualTo(42);
		}

		[Test]
		public async Task WithTask_ShouldReturnCanceledTask()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			Task result = sut.TaskMethod(canceledToken);

			await That(result.IsCanceled).IsTrue();
		}

		[Test]
		public async Task WithTaskOfInt_ShouldReturnCanceledTask()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			Task<int> result = sut.TaskOfIntMethod(canceledToken);

			await That(result.IsCanceled).IsTrue();
		}

		[Test]
		public async Task WithTaskOfString_ShouldReturnCanceledTask()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			Task<string> result = sut.TaskOfStringMethod(canceledToken);

			await That(result.IsCanceled).IsTrue();
		}

		[Test]
		public async Task WithTupleContainingTask_ShouldCancelTaskElement()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			(Task task, string text) result = sut.TupleWithTaskMethod(canceledToken);

			await That(result.task.IsCanceled).IsTrue();
			await That(result.text).IsEqualTo(string.Empty);
		}

#if NET8_0_OR_GREATER
		[Test]
		public async Task WithValueTask_ShouldReturnCanceledTask()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			ValueTask result = sut.ValueTaskMethod(canceledToken);

			await That(result.IsCanceled).IsTrue();
		}
#endif

#if NET8_0_OR_GREATER
		[Test]
		public async Task WithValueTaskOfInt_ShouldReturnCanceledTask()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			ValueTask<int> result = sut.ValueTaskOfIntMethod(canceledToken);

			await That(result.IsCanceled).IsTrue();
		}
#endif
	}

	public interface IMockWithCancellationToken
	{
		Task TaskMethod(CancellationToken cancellationToken);
		Task<int> TaskOfIntMethod(CancellationToken cancellationToken);
		Task<string> TaskOfStringMethod(CancellationToken cancellationToken);
		ValueTask ValueTaskMethod(CancellationToken cancellationToken);
		ValueTask<int> ValueTaskOfIntMethod(CancellationToken cancellationToken);
		Task<int> MultipleParametersMethod(string param1, int param2, CancellationToken cancellationToken);
		(Task, string) TupleWithTaskMethod(CancellationToken cancellationToken);
	}
}
