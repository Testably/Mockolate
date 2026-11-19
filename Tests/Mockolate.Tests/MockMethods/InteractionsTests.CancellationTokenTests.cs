using System.Threading;

namespace Mockolate.Tests.MockMethods;

public sealed partial class InteractionsTests
{
	public sealed class CancellationTokenTests
	{
		[Test]
		public async Task WithArrayParameter_WhenAnElementIsCanceled_ShouldReturnCanceledTask()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			Task<int> result = sut.ArrayParameterMethod(["test", canceledToken,]);

			await That(result.IsCanceled).IsTrue();
		}

		[Test]
		public async Task WithGenericParameter_WhenItIsCanceled_ShouldReturnCanceledTask()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			Task<int> result = sut.GenericParameterMethod(canceledToken);

			await That(result.IsCanceled).IsTrue();
		}

		[Test]
		public async Task WithMultipleTokens_ShouldOnlyConsiderTheFirst()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			Task<int> firstCanceled = sut.TwoTokensMethod(canceledToken, CancellationToken.None);
			Task<int> secondCanceled = sut.TwoTokensMethod(CancellationToken.None, canceledToken);

			await That(firstCanceled.IsCanceled).IsTrue();
			await That(secondCanceled.IsCanceled).IsFalse();
		}

		[Test]
		public async Task WithNullableToken_WhenItIsCanceled_ShouldReturnCanceledTask()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			Task<int> canceled = sut.NullableTokenMethod(canceledToken);
			Task<int> withoutToken = sut.NullableTokenMethod(null);

			await That(canceled.IsCanceled).IsTrue();
			await That(withoutToken.IsCanceled).IsFalse();
		}

		[Test]
		public async Task WithObjectParameter_WhenItIsACanceledToken_ShouldReturnCanceledTask()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			Task<int> canceled = sut.ObjectParameterMethod(canceledToken);
			Task<int> withoutToken = sut.ObjectParameterMethod("test");

			await That(canceled.IsCanceled).IsTrue();
			await That(withoutToken.IsCanceled).IsFalse();
		}

		[Test]
		public async Task WithoutTokenParameter_ShouldReturnDefaultValue()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();

			Task<int> result = sut.WithoutTokenMethod("test");

			await That(result.IsCanceled).IsFalse();
			await That(await result).IsEqualTo(0);
		}

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
		public async Task WithTask_WhenAnObjectParameterIsACanceledToken_ShouldReturnCanceledTask()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();
			CancellationToken canceledToken = new(true);

			Task canceled = sut.TaskWithObjectParameterMethod(canceledToken);
			Task withoutToken = sut.TaskWithObjectParameterMethod("test");

			await That(canceled.IsCanceled).IsTrue();
			await That(withoutToken.IsCanceled).IsFalse();
			await That(withoutToken.IsCompleted).IsTrue();
		}

		[Test]
		public async Task WithTask_WithoutTokenParameter_ShouldReturnCompletedTask()
		{
			IMockWithCancellationToken sut = IMockWithCancellationToken.CreateMock();

			Task withParameter = sut.TaskWithoutTokenMethod("test");
			Task withoutParameter = sut.TaskWithoutParameterMethod();

			await That(withParameter.IsCompleted).IsTrue();
			await That(withParameter.IsCanceled).IsFalse();
			await That(withoutParameter.IsCompleted).IsTrue();
			await That(withoutParameter.IsCanceled).IsFalse();
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
		Task<int> ArrayParameterMethod(object[] values);
		Task<int> GenericParameterMethod<T>(T value);
		Task<int> TwoTokensMethod(CancellationToken first, CancellationToken second);
		Task<int> NullableTokenMethod(CancellationToken? cancellationToken);
		Task<int> ObjectParameterMethod(object value);
		Task<int> WithoutTokenMethod(string value);
		Task TaskWithObjectParameterMethod(object value);
		Task TaskWithoutTokenMethod(string value);
		Task TaskWithoutParameterMethod();
	}
}
