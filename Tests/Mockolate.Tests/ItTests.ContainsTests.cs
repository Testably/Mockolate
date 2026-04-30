using System.Collections.Generic;
using System.Linq;
using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class ContainsTests
	{
		[Test]
		public async Task ShouldMatchArrayParameter()
		{
			ICollectionConsumer sut = ICollectionConsumer.CreateMock();
			sut.Mock.Setup.WithArray(It.Contains(5)).Returns(42);

			int hit = sut.WithArray([1, 2, 5,]);
			int miss = sut.WithArray([1, 2, 3,]);

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchEnumerableParameter()
		{
			ICollectionConsumer sut = ICollectionConsumer.CreateMock();
			sut.Mock.Setup.WithEnumerable(It.Contains(5)).Returns(42);

			int hit = sut.WithEnumerable(Enumerable.Range(3, 5));
			int miss = sut.WithEnumerable(Enumerable.Range(100, 5));

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchHashSetParameter()
		{
			ICollectionConsumer sut = ICollectionConsumer.CreateMock();
			sut.Mock.Setup.WithHashSet(It.Contains(5)).Returns(42);

			int hit = sut.WithHashSet([4, 5, 6,]);
			int miss = sut.WithHashSet([1, 2, 3,]);

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchListParameter()
		{
			ICollectionConsumer sut = ICollectionConsumer.CreateMock();
			sut.Mock.Setup.WithList(It.Contains(5)).Returns(42);

			int hit = sut.WithList([1, 2, 5,]);
			int miss = sut.WithList([1, 2, 3,]);

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchQueueParameter()
		{
			ICollectionConsumer sut = ICollectionConsumer.CreateMock();
			sut.Mock.Setup.WithQueue(It.Contains(5)).Returns(42);

			int hit = sut.WithQueue(new Queue<int>([1, 2, 5,]));
			int miss = sut.WithQueue(new Queue<int>([1, 2, 3,]));

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchReadOnlyListParameter()
		{
			ICollectionConsumer sut = ICollectionConsumer.CreateMock();
			sut.Mock.Setup.WithReadOnlyList(It.Contains(5)).Returns(42);

			int hit = sut.WithReadOnlyList([1, 5, 9,]);
			int miss = sut.WithReadOnlyList([2, 3, 4,]);

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchSetParameter()
		{
			ICollectionConsumer sut = ICollectionConsumer.CreateMock();
			sut.Mock.Setup.WithSet(It.Contains(5)).Returns(42);

			int hit = sut.WithSet(new HashSet<int>
			{
				4,
				5,
				6,
			});
			int miss = sut.WithSet(new HashSet<int>
			{
				1,
				2,
				3,
			});

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchStackParameter()
		{
			ICollectionConsumer sut = ICollectionConsumer.CreateMock();
			sut.Mock.Setup.WithStack(It.Contains(5)).Returns(42);

			int hit = sut.WithStack(new Stack<int>([1, 2, 5,]));
			int miss = sut.WithStack(new Stack<int>([1, 2, 3,]));

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		[Arguments(1, false)]
		[Arguments(4, false)]
		[Arguments(5, true)]
		[Arguments(6, true)]
		[Arguments(42, false)]
		public async Task ShouldMatchWhenEnumerableContainsItem(int item, bool expectMatch)
		{
			IParameter<IEnumerable<int>> sut = It.Contains(item);

			bool result = ((IParameterMatch<IEnumerable<int>>)sut).Matches([5, 6, 7,]);

			await That(result).IsEqualTo(expectMatch);
		}

		[Test]
		public async Task ShouldNotMatchWhenValueIsNotCollection()
		{
			IParameter<IEnumerable<int>> sut = It.Contains(5);

			bool result = sut.Matches("not-a-collection");

			await That(result).IsFalse();
		}

		[Test]
		public async Task ShouldNotMatchWhenValueIsNull()
		{
			IParameter<IEnumerable<int>> sut = It.Contains(5);

			bool result = sut.Matches(null);

			await That(result).IsFalse();
		}

		[Test]
		public async Task TypedArrayMatch_WithNullValue_ShouldNotMatch()
		{
			IParameter<int[]> sut = It.Contains(5);

			bool result = ((IParameterMatch<int[]>)sut).Matches(null!);

			await That(result).IsFalse();
		}

		[Test]
		public async Task ShouldSupportVerify()
		{
			ICollectionConsumer sut = ICollectionConsumer.CreateMock();

			sut.WithArray([1, 2, 5,]);
			sut.WithArray([1, 2, 3,]);

			await That(sut.Mock.Verify.WithArray(It.Contains(5))).Once();
			await That(sut.Mock.Verify.WithArray(It.Contains(3))).Once();
			await That(sut.Mock.Verify.WithArray(It.Contains(99))).Never();
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameter<int[]> sut = It.Contains(5);
			string expectedValue = "It.Contains(5)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_Using_ShouldReturnExpectedValue()
		{
			IParameter<int[]> sut = It.Contains(5).Using(new AllEqualComparer());
			string expectedValue = "It.Contains(5).Using(new AllEqualComparer())";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task WithComparer_ShouldUseComparer()
		{
			IParameter<int[]> sut = It.Contains(42).Using(new AllEqualComparer());

			bool result = ((IParameterMatch<int[]>)sut).Matches([1, 2, 3,]);

			await That(result).IsTrue();
		}

		public sealed class DoTests
		{
			[Test]
			public async Task Do_RegistersCallbackForArray()
			{
				It.IContainsParameter<int> sut = It.Contains(1);
				int[]? captured = null;
				((IParameterWithCallback<int[]>)sut).Do(value => captured = value);

				int[] source = [1, 2, 3,];
				((IParameterMatch<int[]>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForHashSet()
			{
				It.IContainsParameter<int> sut = It.Contains(1);
				HashSet<int>? captured = null;
				((IParameterWithCallback<HashSet<int>>)sut).Do(value => captured = value);

				HashSet<int> source = [1, 2, 3,];
				((IParameterMatch<HashSet<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForICollection()
			{
				It.IContainsParameter<int> sut = It.Contains(1);
				ICollection<int>? captured = null;
				((IParameterWithCallback<ICollection<int>>)sut).Do(value => captured = value);

				List<int> source = [1, 2, 3,];
				((IParameterMatch<ICollection<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForIEnumerable()
			{
				It.IContainsParameter<int> sut = It.Contains(1);
				IEnumerable<int>? captured = null;
				((IParameterWithCallback<IEnumerable<int>>)sut).Do(value => captured = value);

				List<int> source = [1, 2, 3,];
				((IParameterMatch<IEnumerable<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForIList()
			{
				It.IContainsParameter<int> sut = It.Contains(1);
				IList<int>? captured = null;
				((IParameterWithCallback<IList<int>>)sut).Do(value => captured = value);

				List<int> source = [1, 2, 3,];
				((IParameterMatch<IList<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForIReadOnlyCollection()
			{
				It.IContainsParameter<int> sut = It.Contains(1);
				IReadOnlyCollection<int>? captured = null;
				((IParameterWithCallback<IReadOnlyCollection<int>>)sut).Do(value => captured = value);

				List<int> source = [1, 2, 3,];
				((IParameterMatch<IReadOnlyCollection<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForIReadOnlyList()
			{
				It.IContainsParameter<int> sut = It.Contains(1);
				IReadOnlyList<int>? captured = null;
				((IParameterWithCallback<IReadOnlyList<int>>)sut).Do(value => captured = value);

				List<int> source = [1, 2, 3,];
				((IParameterMatch<IReadOnlyList<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForISet()
			{
				It.IContainsParameter<int> sut = It.Contains(1);
				ISet<int>? captured = null;
				((IParameterWithCallback<ISet<int>>)sut).Do(value => captured = value);

				HashSet<int> source = [1, 2, 3,];
				((IParameterMatch<ISet<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForList()
			{
				It.IContainsParameter<int> sut = It.Contains(1);
				List<int>? captured = null;
				((IParameterWithCallback<List<int>>)sut).Do(value => captured = value);

				List<int> source = [1, 2, 3,];
				((IParameterMatch<List<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForQueue()
			{
				It.IContainsParameter<int> sut = It.Contains(1);
				Queue<int>? captured = null;
				((IParameterWithCallback<Queue<int>>)sut).Do(value => captured = value);

				Queue<int> source = new();
				source.Enqueue(1);
				((IParameterMatch<Queue<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForStack()
			{
				It.IContainsParameter<int> sut = It.Contains(1);
				Stack<int>? captured = null;
				((IParameterWithCallback<Stack<int>>)sut).Do(value => captured = value);

				Stack<int> source = new();
				source.Push(1);
				((IParameterMatch<Stack<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task InvokeCallbacks_OnNonEnumerableValue_ShortCircuits()
			{
				It.IContainsParameter<int> sut = It.Contains(1);
				int invocations = 0;
				((IParameterWithCallback<List<int>>)sut).Do(_ => invocations++);

				sut.InvokeCallbacks(new object());

				await That(invocations).IsEqualTo(0);
			}

			[Test]
			public async Task InvokeCallbacks_WithoutAnyRegistration_DoesNotThrow()
			{
				It.IContainsParameter<int> sut = It.Contains(1);

				void Act()
				{
					sut.InvokeCallbacks(new List<int>
					{
						1,
					});
				}

				await That(Act).DoesNotThrow();
			}
		}


		public interface ICollectionConsumer
		{
			int WithArray(int[] items);
			int WithList(List<int> items);
			int WithEnumerable(IEnumerable<int> items);
			int WithHashSet(HashSet<int> items);
			int WithQueue(Queue<int> items);
			int WithReadOnlyList(IReadOnlyList<int> items);
			int WithSet(ISet<int> items);
			int WithStack(Stack<int> items);
		}
	}
}
