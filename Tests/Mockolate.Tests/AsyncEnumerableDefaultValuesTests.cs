using System.Collections.Generic;
using System.Threading;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests;

public sealed class AsyncEnumerableDefaultValuesTests
{
	[Test]
	public async Task Property_ShouldReturnEmptySequence()
	{
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock();

		List<int> result = await ToListAsync(sut.Numbers);

		await That(result).IsEmpty();
	}

	[Test]
	public async Task Method_ShouldReturnEmptySequence()
	{
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock();

		List<string> result = await ToListAsync(sut.Read("foo"));

		await That(result).IsEmpty();
	}

	[Test]
	public async Task Method_WithNestedGenericElementType_ShouldReturnEmptySequence()
	{
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock();

		List<Dictionary<string, List<int[]>>> result = await ToListAsync(sut.ReadNested());

		await That(result).IsEmpty();
	}

	[Test]
	public async Task GenericMethod_ShouldReturnEmptySequence()
	{
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock();

		List<string> result = await ToListAsync(sut.ReadGeneric<string>());

		await That(result).IsEmpty();
	}

	[Test]
	public async Task Task_ShouldReturnEmptySequence()
	{
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock();

		List<int> result = await ToListAsync(await sut.ReadAsync());

		await That(result).IsEmpty();
	}

	[Test]
	public async Task DerivedInterface_ShouldReturnNull()
	{
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock(MockBehavior.Default.SkippingAutoMocking());

		IDerivedAsyncEnumerable<int> result = sut.Derived;

		await That(result).IsNull().Because("only IAsyncEnumerable<T> itself has an empty default");
	}

	[Test]
	public async Task Method_WithCancellationTokenThatIsNotCanceled_ShouldReturnEmptySequence()
	{
		using CancellationTokenSource cts = new();
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock();

		List<string> result = await ToListAsync(sut.Read("foo", cts.Token));

		await That(result).IsEmpty();
	}

	[Test]
	public async Task Method_WithCanceledCancellationToken_ShouldThrowWhenEnumerated()
	{
		using CancellationTokenSource cts = new();
		cts.Cancel();
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock();

		IAsyncEnumerable<string> result = sut.Read("foo", cts.Token);

		await That(async () => await ToListAsync(result)).Throws<OperationCanceledException>()
			.Whose(e => e.CancellationToken, it => it.IsEqualTo(cts.Token));
	}

	[Test]
	public async Task Method_WhenCancellationTokenIsCanceledAfterTheCall_ShouldThrowWhenEnumerated()
	{
		using CancellationTokenSource cts = new();
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock();
		IAsyncEnumerable<string> result = sut.Read("foo", cts.Token);

		cts.Cancel();

		await That(async () => await ToListAsync(result)).Throws<OperationCanceledException>()
			.Because("like an async iterator, the empty sequence checks the token when it is enumerated");
	}

	[Test]
	public async Task WithCancellation_WhenTokenIsCanceled_ShouldThrowWhenEnumerated()
	{
		using CancellationTokenSource cts = new();
		cts.Cancel();
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock();

		async Task Act()
		{
			await foreach (int _ in sut.Numbers.WithCancellation(cts.Token))
			{
			}
		}

		await That(Act).Throws<OperationCanceledException>()
			.Whose(e => e.CancellationToken, it => it.IsEqualTo(cts.Token))
			.Because("like an async iterator, the empty sequence honors the token passed to GetAsyncEnumerator");
	}

	[Test]
	public async Task WithCancellation_WhenMethodTokenIsCanceled_ShouldStillThrowWhenEnumerated()
	{
		using CancellationTokenSource methodCts = new();
		using CancellationTokenSource enumeratorCts = new();
		methodCts.Cancel();
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock();

		async Task Act()
		{
			await foreach (string _ in sut.Read("foo", methodCts.Token).WithCancellation(enumeratorCts.Token))
			{
			}
		}

		await That(Act).Throws<OperationCanceledException>()
			.Whose(e => e.CancellationToken, it => it.IsEqualTo(methodCts.Token))
			.Because("the token passed to the method must still be honored when another one is passed to GetAsyncEnumerator");
	}

	[Test]
	public async Task WithCancellation_WhenNoTokenIsCanceled_ShouldReturnEmptySequence()
	{
		using CancellationTokenSource methodCts = new();
		using CancellationTokenSource enumeratorCts = new();
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock();
		int count = 0;

		await foreach (string _ in sut.Read("foo", methodCts.Token).WithCancellation(enumeratorCts.Token))
		{
			count++;
		}

		await That(count).IsEqualTo(0);
	}

	[Test]
	public async Task WithDefaultValueFor_ShouldTakePrecedence()
	{
		MockBehavior behavior = MockBehavior.Default
			.WithDefaultValueFor(() => ToAsyncEnumerable(1, 2));
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock(behavior);

		List<int> result = await ToListAsync(sut.Numbers);

		await That(result).IsEqualTo([1, 2,]);
	}

	[Test]
	public async Task WithDefaultValueGenerator_ShouldTakePrecedence()
	{
		MockBehavior behavior = MockBehavior.Default
			.WithDefaultValueGenerator(new AsyncEnumerableDefaultValueGenerator());
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock(behavior);

		List<string> result = await ToListAsync(sut.Read("foo"));

		await That(result).IsEqualTo(["foo",]);
	}

	[Test]
	public async Task Setup_ShouldTakePrecedence()
	{
		IAsyncEnumerableDefaultValues sut = IAsyncEnumerableDefaultValues.CreateMock();
		sut.Mock.Setup.Read(It.IsAny<string>(), It.IsAny<CancellationToken>()).Returns(ToAsyncEnumerable("a", "b"));

		List<string> result = await ToListAsync(sut.Read("foo"));

		await That(result).IsEqualTo(["a", "b",]);
	}

	private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
	{
		List<T> result = [];
		await foreach (T item in source)
		{
			result.Add(item);
		}

		return result;
	}

	private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(params T[] values)
	{
		await Task.Yield();
		foreach (T value in values)
		{
			yield return value;
		}
	}

	private sealed class AsyncEnumerableDefaultValueGenerator : IDefaultValueGenerator
	{
		public object? GenerateValue(Type type, params object?[] parameters)
			=> type == typeof(IAsyncEnumerable<string>) ? ToAsyncEnumerable((string)parameters[0]!) : null;
	}
}
