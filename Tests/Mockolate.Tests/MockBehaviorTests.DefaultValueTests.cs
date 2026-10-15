using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Mockolate.Tests;

public sealed partial class MockBehaviorTests
{
	public sealed class DefaultValueTests
	{
		[Fact]
		public async Task Recursive_ShouldReturnNull()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			IMyRecursiveService result = sut.RecursiveService;

			await That(result).IsNull();
		}

		[Fact]
		public async Task WithArray_ShouldReturnEmptyArray()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			int[] result = sut.SimpleArray;

			await That(result).HasCount(0);
		}

		[Fact]
		public async Task WithCancellationToken_ShouldReturnNone()
		{
			MockBehavior sut = MockBehavior.Default;

			CancellationToken result = sut.DefaultValue.Generate(CancellationToken.None);

			await That(result).IsEqualTo(CancellationToken.None);
		}

		[Fact]
		public async Task WithCombination_ShouldReturnNotNullValues()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			(int, int[], string) result = await sut.ComplexTask();

			await That(result.Item1).IsEqualTo(0);
			await That(result.Item2).IsEmpty();
			await That(result.Item3).IsEqualTo("");
		}

		[Fact]
		public async Task WithHttpResponseMessage_ShouldReturnEmptyResponseWithNotImplementedStatusCode()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			HttpResponseMessage result = sut.HttpResponseMessage;

			await That(result.StatusCode).IsEqualTo(HttpStatusCode.NotImplemented);
			await That(result.Content.ReadAsStringAsync()).IsEmpty();
		}

		[Fact]
		public async Task WithIEnumerable_ShouldReturnEmptyEnumerable()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			IEnumerable result = sut.IEnumerable;

			await That(result).HasCount(0);
		}

		[Fact]
		public async Task WithIEnumerableOfInt_ShouldReturnEmptyEnumerable()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			IEnumerable<int> result = sut.IEnumerableOfInt;

			await That(result).HasCount(0);
		}

		[Fact]
		public async Task WithInt_ShouldReturnZero()
		{
			MockBehavior sut = MockBehavior.Default;

			int result = sut.DefaultValue.Generate(0);

			await That(result).IsEqualTo(0);
		}

		[Fact]
		public async Task WithMultidimensionalArray_ShouldReturnEmptyArray()
		{
			MockBehavior.Default.DefaultValue.Generate(default(int[,])!);

			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			int[,,][,][] result = sut.MultiDimensionalArray;

			await That(result).HasCount(0);
		}

		[Fact]
		public async Task WithNullableInt_ShouldReturnNull()
		{
			MockBehavior sut = MockBehavior.Default;

			int? result = sut.DefaultValue.Generate((int?)null);

			await That(result).IsNull();
		}

		[Fact]
		public async Task WithObject_ShouldReturnNull()
		{
			MockBehavior sut = MockBehavior.Default;

			object result = sut.DefaultValue.Generate((object)null!);

			await That(result).IsNull();
		}

		[Fact]
		public async Task WithString_ShouldReturnEmptyString()
		{
			MockBehavior sut = MockBehavior.Default;

			string result = sut.DefaultValue.Generate("");

			await That(result).IsEmpty();
		}

		[Fact]
		public async Task WithStruct_ShouldReturnDefault()
		{
			MockBehavior sut = MockBehavior.Default;

			DateTime result = sut.DefaultValue.Generate(default(DateTime));

			await That(result).IsEqualTo(DateTime.MinValue);
		}

		[Fact]
		public async Task WithTask_ShouldReturnCompletedTask()
		{
			MockBehavior sut = MockBehavior.Default;

			Task result = sut.DefaultValue.Generate(default(Task)!);

			await That<Task>(result).IsNotNull();
			await That(result.IsCompleted).IsTrue();
			await That(result.IsFaulted).IsFalse();
		}

		[Fact]
		public async Task WithTaskInt_ShouldReturnZero()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			Task<int> result = sut.IntTask;

			await That(result.IsCompleted).IsTrue();
			await That(result).IsEqualTo(0);
		}

		[Fact]
		public async Task WithTaskIntArray_ShouldReturnZero()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			Task<int[]> result = sut.IntArrayTask;

			await That(result.IsCompleted).IsTrue();
			await That(result).IsEmpty();
		}

		[Fact]
		public async Task WithValueTaskInt_ShouldReturnZero()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			ValueTask<int> result = sut.IntValueTask;

			await That(result.IsCompleted).IsTrue();
			await That(await result).IsEqualTo(0);
		}

#if NET8_0_OR_GREATER
		[Fact]
		public async Task WithValueTaskIntArray_ShouldReturnZero()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			ValueTask<int[]> result = sut.IntArrayValueTask;

			await That(result.IsCompleted).IsTrue();
			await That(await result).IsEmpty();
		}
#endif

		[Fact]
		public async Task WithValueTuple_ShouldReturnValueTupleWithDefaultValues()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			(int V1, string V2) result = sut.NamedValueTuple;

			await That(result.V1).IsEqualTo(0);
			await That(result.V2).IsEqualTo("");
		}

		[Fact]
		public async Task WithValueTuple8_ShouldReturnValueTupleWithDefaultValues()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			(int V1, string V2, int V3, string V4, int V5, string V6, int V7, string V8) result =
				sut.ValueTuple8;

			await That(result.V1).IsEqualTo(0);
			await That(result.V2).IsEqualTo("");
			await That(result.V3).IsEqualTo(0);
			await That(result.V4).IsEqualTo("");
			await That(result.V5).IsEqualTo(0);
			await That(result.V6).IsEqualTo("");
			await That(result.V7).IsEqualTo(0);
			await That(result.V8).IsEqualTo("");
		}

		public interface IDefaultValueGeneratorProperties
		{
			int[] SimpleArray { get; }
			int[,,][,][] MultiDimensionalArray { get; }
			IEnumerable IEnumerable { get; }
			IEnumerable<int> IEnumerableOfInt { get; }
			(int V1, string V2) NamedValueTuple { get; }
			(int, string, int, string, int, string, int, string) ValueTuple8 { get; }
			Task<int> IntTask { get; }
			Task<int[]> IntArrayTask { get; }
			ValueTask<int> IntValueTask { get; }
			ValueTask<int[]> IntArrayValueTask { get; }
			IMyRecursiveService RecursiveService { get; }
			HttpResponseMessage HttpResponseMessage { get; }

			Task<(int, int[], string)> ComplexTask();
			Type GetMyType();
		}

		public interface IMyRecursiveService
		{
			int GetCalled(int value);
		}
	}
}
