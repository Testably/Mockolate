using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Mockolate.Tests;

public sealed partial class MockBehaviorTests
{
	public sealed class DefaultValueTests
	{
		[Test]
		public async Task Recursive_ShouldReturnNull()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock(MockBehavior.Default.SkippingAutoMocking());

			IMyRecursiveService result = sut.RecursiveService;

			await That(result).IsNull();
		}

		[Test]
		public async Task WithArray_ShouldReturnEmptyArray()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			int[] result = sut.SimpleArray;

			await That(result).HasCount(0);
		}

		[Test]
		public async Task WithCancellationToken_ShouldReturnNone()
		{
			MockBehavior sut = MockBehavior.Default;

			CancellationToken result = sut.DefaultValue.Generate(CancellationToken.None);

			await That(result).IsEqualTo(CancellationToken.None);
		}

		[Test]
		public async Task WithCombination_ShouldReturnNotNullValues()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			(int, int[], string) result = await sut.ComplexTask();

			await That(result.Item1).IsEqualTo(0);
			await That(result.Item2).IsEmpty();
			await That(result.Item3).IsEqualTo("");
		}

		[Test]
		public async Task WithAction_ShouldReturnActionThatDoesNothing()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			Action<List<int>> result = sut.Configure;

			List<int> values = [1,];
			result(values);
			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task WithFuncReturningItsArgumentType_ShouldReturnTheArgument()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			Func<string, string> result = sut.Options;

			await That(result("foo")).IsEqualTo("foo");
		}

		[Test]
		public async Task WithFuncReturningItsArgumentType_WhenRegistered_ShouldReturnTheRegisteredValue()
		{
			Func<string, string> registered = value => value + "bar";
			MockBehavior sut = MockBehavior.Default.WithDefaultValueFor(() => registered);

			Func<string, string> result = sut.DefaultValue.Generate(default(Func<string, string>)!);

			await That(result).IsSameAs(registered);
		}

		[Test]
		public async Task WithFuncReturningOtherType_ShouldReturnNull()
		{
			MockBehavior sut = MockBehavior.Default;

			Func<string, int> result = sut.DefaultValue.Generate(default(Func<string, int>)!);

			await That(result).IsNull();
		}

		[Test]
		public async Task WithHttpResponseMessage_ShouldReturnEmptyResponseWithNotImplementedStatusCode()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			HttpResponseMessage result = sut.HttpResponseMessage;

			await That(result.StatusCode).IsEqualTo(HttpStatusCode.NotImplemented);
			await That(result.Content.ReadAsStringAsync()).IsEmpty();
		}

		[Test]
		public async Task WithIEnumerable_ShouldReturnEmptyEnumerable()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			IEnumerable result = sut.IEnumerable;

			await That(result).HasCount(0);
		}

		[Test]
		public async Task WithIEnumerableOfInt_ShouldReturnEmptyEnumerable()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			IEnumerable<int> result = sut.IEnumerableOfInt;

			await That(result).HasCount(0);
		}

		[Test]
		public async Task WithIndexerOfFuncReturningItsArgumentType_ShouldReturnTheArgument()
		{
			IDefaultValueGeneratorIndexers sut = IDefaultValueGeneratorIndexers.CreateMock();

			Func<string, string> result = sut[1, 2];

			await That(result("foo")).IsEqualTo("foo");
		}

		[Test]
		public async Task WithIndexerOfMoreThanFourKeys_ShouldReturnEmptyList()
		{
			IDefaultValueGeneratorIndexers sut = IDefaultValueGeneratorIndexers.CreateMock();

			List<int> result = sut[1, 2, 3, 4, 5];

			await That(result).IsEmpty();
		}

		[Test]
		public async Task WithIndexerOfMoreThanFourKeys_WhenSetupWithoutValue_ShouldReturnEmptyList()
		{
			IDefaultValueGeneratorIndexers sut = IDefaultValueGeneratorIndexers.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.Do(() => { });

			List<int> result = sut[1, 2, 3, 4, 5];

			await That(result).IsEmpty();
		}

		[Test]
		public async Task WithIndexerOfTaskInt_ShouldReturnCompletedTask()
		{
			IDefaultValueGeneratorIndexers sut = IDefaultValueGeneratorIndexers.CreateMock();

			Task<int> result = sut[1];

			await That(result.IsCompleted).IsTrue();
			await That(result).IsEqualTo(0);
		}

		[Test]
		public async Task WithIndexerOfArray_WhenSetupWithoutValue_ShouldReturnEmptyArray()
		{
			IDefaultValueGeneratorIndexers sut = IDefaultValueGeneratorIndexers.CreateMock();
			sut.Mock.Setup[It.IsAny<string>()].OnGet.Do(() => { });

			int[] result = sut["foo"];

			await That(result).IsEmpty();
		}

		[Test]
		public async Task WithIndexerOfValueTuple_ShouldReturnValueTupleWithDefaultValues()
		{
			IDefaultValueGeneratorIndexers sut = IDefaultValueGeneratorIndexers.CreateMock();

			(int V1, string V2) result = sut["foo", "bar"];

			await That(result.V1).IsEqualTo(0);
			await That(result.V2).IsEqualTo("");
		}

		[Test]
		public async Task WithIndexerOfTuple_ShouldReturnTupleWithDefaultValues()
		{
			IDefaultValueGeneratorIndexers sut = IDefaultValueGeneratorIndexers.CreateMock();

			Tuple<int, string> result = sut["foo", 1];

			await That(result.Item1).IsEqualTo(0);
			await That(result.Item2).IsEqualTo("");
			await That(sut["foo", 1]).IsSameAs(result);
		}

		[Test]
		public async Task WithInt_ShouldReturnZero()
		{
			MockBehavior sut = MockBehavior.Default;

			int result = sut.DefaultValue.Generate(0);

			await That(result).IsEqualTo(0);
		}

		[Test]
		public async Task WithMultidimensionalArray_ShouldReturnEmptyArray()
		{
			MockBehavior.Default.DefaultValue.Generate(default(int[,])!);

			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			int[,,][,][] result = sut.MultiDimensionalArray;

			await That(result).HasCount(0);
		}

		[Test]
		public async Task WithNullableInt_ShouldReturnNull()
		{
			MockBehavior sut = MockBehavior.Default;

			int? result = sut.DefaultValue.Generate((int?)null);

			await That(result).IsNull();
		}

		[Test]
		public async Task WithObject_ShouldReturnNull()
		{
			MockBehavior sut = MockBehavior.Default;

			object result = sut.DefaultValue.Generate((object)null!);

			await That(result).IsNull();
		}

		[Test]
		public async Task WithString_ShouldReturnEmptyString()
		{
			MockBehavior sut = MockBehavior.Default;

			string result = sut.DefaultValue.Generate("");

			await That(result).IsEmpty();
		}

		[Test]
		public async Task WithStruct_ShouldReturnDefault()
		{
			MockBehavior sut = MockBehavior.Default;

			DateTime result = sut.DefaultValue.Generate(default(DateTime));

			await That(result).IsEqualTo(DateTime.MinValue);
		}

		[Test]
		public async Task WithTask_ShouldReturnCompletedTask()
		{
			MockBehavior sut = MockBehavior.Default;

			Task result = sut.DefaultValue.Generate(default(Task)!);

			await That<Task>(result).IsNotNull();
			await That(result.IsCompleted).IsTrue();
			await That(result.IsFaulted).IsFalse();
		}

		[Test]
		public async Task WithTaskInt_ShouldReturnZero()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			Task<int> result = sut.IntTask;

			await That(result.IsCompleted).IsTrue();
			await That(result).IsEqualTo(0);
		}

		[Test]
		public async Task WithTaskIntArray_ShouldReturnZero()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			Task<int[]> result = sut.IntArrayTask;

			await That(result.IsCompleted).IsTrue();
			await That(result).IsEmpty();
		}

		[Test]
		public async Task WithValueTaskInt_ShouldReturnZero()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			ValueTask<int> result = sut.IntValueTask;

			await That(result.IsCompleted).IsTrue();
			await That(await result).IsEqualTo(0);
		}

#if NET8_0_OR_GREATER
		[Test]
		public async Task WithValueTaskIntArray_ShouldReturnZero()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			ValueTask<int[]> result = sut.IntArrayValueTask;

			await That(result.IsCompleted).IsTrue();
			await That(await result).IsEmpty();
		}
#endif

		[Test]
		public async Task WithTuple_ShouldReturnTupleWithDefaultValues()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			Tuple<int, string> result = sut.ReferenceTuple;
			Tuple<int, string>? nullableResult = sut.NullableReferenceTuple;

			await That(result.Item1).IsEqualTo(0);
			await That(result.Item2).IsEqualTo("");
			await That(nullableResult).IsNotNull();
		}

		[Test]
		public async Task WithTuple_WhenRegistered_ShouldReturnTheRegisteredValue()
		{
			Tuple<int, string> registered = Tuple.Create(1, "registered");
			IDefaultValueGeneratorProperties sut =
				IDefaultValueGeneratorProperties.CreateMock(MockBehavior.Default.WithDefaultValueFor(() => registered));

			Tuple<int, string> result = sut.ReferenceTuple;

			await That(result).IsSameAs(registered);
		}

		[Test]
		public async Task WithTuple8_ShouldReturnTupleWithDefaultValues()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			Tuple<int, string, int, string, int, string, int, Tuple<string>> result = sut.ReferenceTuple8;

			await That(result.Item7).IsEqualTo(0);
			await That(result.Rest.Item1).IsEqualTo("");
		}

		[Test]
		public async Task WithTuple8WithoutTupleAsLastElement_ShouldReturnNull()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			Tuple<int, int, int, int, int, int, int, int> result = sut.ReferenceTuple8WithoutTupleRest;

			await That(result).IsNull();
		}

		[Test]
		public async Task WithTupleInTask_ShouldReturnTupleWithDefaultValues()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			Tuple<int, int[]> result = await sut.ReferenceTupleTask();

			await That(result.Item1).IsEqualTo(0);
			await That(result.Item2).IsEmpty();
		}

		[Test]
		public async Task WithValueTuple_ShouldReturnValueTupleWithDefaultValues()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			(int V1, string V2) result = sut.NamedValueTuple;

			await That(result.V1).IsEqualTo(0);
			await That(result.V2).IsEqualTo("");
		}

		[Test]
		public async Task WithValueTuple_WhenRegistered_ShouldReturnTheRegisteredValue()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock(
				MockBehavior.Default.WithDefaultValueFor(() => (1, "registered")));

			(int V1, string V2) property = sut.NamedValueTuple;
			(int V1, string V2) method = sut.GetNamedValueTuple(2);

			await That(property).IsEqualTo((1, "registered"));
			await That(method).IsEqualTo((1, "registered"));
		}

		[Test]
		public async Task WithValueTuple_WhenAPredicateMatches_ShouldReturnTheValueOfTheFactory()
		{
			int[]? receivedArguments = null;
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock(
				MockBehavior.Default.WithDefaultValueFor(new DefaultValueFactory(
					type => type == typeof((int, string)),
					(_, arguments) =>
					{
						receivedArguments = arguments.OfType<int>().ToArray();
						return (1, "predicate");
					})));

			(int V1, string V2) result = sut.GetNamedValueTuple(2);

			await That(result).IsEqualTo((1, "predicate"));
			await That(receivedArguments).IsEqualTo([2,]);
		}

		[Test]
		public async Task WithValueTupleOfOneOrNoElement_ShouldReturnValueTupleWithDefaultValues()
		{
			IDefaultValueGeneratorProperties sut = IDefaultValueGeneratorProperties.CreateMock();

			ValueTuple<string> single = sut.GetSingleValueTuple(1);
			ValueTuple empty = sut.GetEmptyValueTuple();

			await That(single.Item1).IsEqualTo("");
			await That(empty).IsEqualTo(default(ValueTuple));
		}

		[Test]
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
			Tuple<int, string> ReferenceTuple { get; }
			Tuple<int, string>? NullableReferenceTuple { get; }
			Tuple<int, string, int, string, int, string, int, Tuple<string>> ReferenceTuple8 { get; }
			Tuple<int, int, int, int, int, int, int, int> ReferenceTuple8WithoutTupleRest { get; }
			Task<int> IntTask { get; }
			Task<int[]> IntArrayTask { get; }
			ValueTask<int> IntValueTask { get; }
			ValueTask<int[]> IntArrayValueTask { get; }
			IMyRecursiveService RecursiveService { get; }
			HttpResponseMessage HttpResponseMessage { get; }
			Func<string, string> Options { get; }
			Action<List<int>> Configure { get; }

			Task<(int, int[], string)> ComplexTask();
			Task<Tuple<int, int[]>> ReferenceTupleTask();
			(int V1, string V2) GetNamedValueTuple(int value);
			ValueTuple<string> GetSingleValueTuple(int value);
			ValueTuple GetEmptyValueTuple();
			Type GetMyType();
		}

		public interface IDefaultValueGeneratorIndexers
		{
			Task<int> this[int key] { get; }
			int[] this[string key] { get; }
			Func<string, string> this[int key1, int key2] { get; }
			(int V1, string V2) this[string key1, string key2] { get; }
			Tuple<int, string> this[string key1, int key2] { get; }
			List<int> this[int key1, int key2, int key3, int key4, int key5] { get; }
		}

		public interface IMyRecursiveService
		{
			int GetCalled(int value);
		}
	}
}
