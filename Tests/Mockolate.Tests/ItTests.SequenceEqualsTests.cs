using System.Collections.Generic;
using System.Linq;
using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class SequenceEqualsTests
	{
		[Test]
		public async Task ShouldMatchArrayParameter()
		{
			ISequenceCollectionConsumer sut = ISequenceCollectionConsumer.CreateMock();
			sut.Mock.Setup.WithArray(It.SequenceEquals(1, 2, 3)).Returns(42);

			int hit = sut.WithArray([1, 2, 3,]);
			int miss = sut.WithArray([1, 2, 4,]);

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchEnumerableParameter()
		{
			ISequenceCollectionConsumer sut = ISequenceCollectionConsumer.CreateMock();
			sut.Mock.Setup.WithEnumerable(It.SequenceEquals(1, 2, 3)).Returns(42);

			int hit = sut.WithEnumerable(Enumerable.Range(1, 3));
			int miss = sut.WithEnumerable(Enumerable.Range(2, 3));

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchListParameter()
		{
			ISequenceCollectionConsumer sut = ISequenceCollectionConsumer.CreateMock();
			sut.Mock.Setup.WithList(It.SequenceEquals(1, 2, 3)).Returns(42);

			int hit = sut.WithList([1, 2, 3,]);
			int miss = sut.WithList([3, 2, 1,]);

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchQueueParameter()
		{
			ISequenceCollectionConsumer sut = ISequenceCollectionConsumer.CreateMock();
			sut.Mock.Setup.WithQueue(It.SequenceEquals(1, 2, 3)).Returns(42);

			int hit = sut.WithQueue(new Queue<int>([1, 2, 3,]));
			int miss = sut.WithQueue(new Queue<int>([1, 2, 4,]));

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchReadOnlyListParameter()
		{
			ISequenceCollectionConsumer sut = ISequenceCollectionConsumer.CreateMock();
			sut.Mock.Setup.WithReadOnlyList(It.SequenceEquals(1, 2, 3)).Returns(42);

			int hit = sut.WithReadOnlyList([1, 2, 3,]);
			int miss = sut.WithReadOnlyList([1, 2,]);

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchStackParameter()
		{
			ISequenceCollectionConsumer sut = ISequenceCollectionConsumer.CreateMock();
			sut.Mock.Setup.WithStack(It.SequenceEquals(3, 2, 1)).Returns(42);

			int hit = sut.WithStack(new Stack<int>([1, 2, 3,]));
			int miss = sut.WithStack(new Stack<int>([1, 2, 4,]));

			await That(hit).IsEqualTo(42);
			await That(miss).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldMatchWhenSequencesEqual()
		{
			IParameter<IEnumerable<int>> sut = It.SequenceEquals(1, 2, 3);

			bool result = ((IParameterMatch<IEnumerable<int>>)sut).Matches([1, 2, 3,]);

			await That(result).IsTrue();
		}

		[Test]
		public async Task ShouldNotMatchWhenLengthsDiffer()
		{
			IParameter<IEnumerable<int>> sut = It.SequenceEquals(1, 2, 3);

			bool result = ((IParameterMatch<IEnumerable<int>>)sut).Matches([1, 2,]);

			await That(result).IsFalse();
		}

		[Test]
		public async Task ShouldNotMatchWhenSequencesDiffer()
		{
			IParameter<IEnumerable<int>> sut = It.SequenceEquals(1, 2, 3);

			bool result = ((IParameterMatch<IEnumerable<int>>)sut).Matches([1, 2, 4,]);

			await That(result).IsFalse();
		}

		[Test]
		public async Task ShouldNotMatchWhenValueIsNotCollection()
		{
			IParameter<IEnumerable<int>> sut = It.SequenceEquals(1, 2, 3);

			bool result = sut.Matches(42);

			await That(result).IsFalse();
		}

		[Test]
		public async Task ShouldSupportVerify()
		{
			ISequenceCollectionConsumer sut = ISequenceCollectionConsumer.CreateMock();

			sut.WithArray([1, 2, 3,]);
			sut.WithArray([1, 2, 3,]);
			sut.WithArray([9, 9, 9,]);

			await That(sut.Mock.Verify.WithArray(It.SequenceEquals(1, 2, 3))).Exactly(2);
			await That(sut.Mock.Verify.WithArray(It.SequenceEquals(9, 9, 9))).Once();
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameter<int[]> sut = It.SequenceEquals(1, 2, 3);
			string expectedValue = "It.SequenceEquals(1, 2, 3)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_Using_ShouldReturnExpectedValue()
		{
			IParameter<int[]> sut = It.SequenceEquals(1, 2, 3).Using(new AllEqualComparer());
			string expectedValue = "It.SequenceEquals(1, 2, 3).Using(new AllEqualComparer())";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithNullElement_ShouldRenderNullToken()
		{
			IParameter<object?[]> sut = It.SequenceEquals<object?>("foo", null, 3);
			string expectedValue = "It.SequenceEquals(\"foo\", null, 3)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithStringValues_ShouldReturnExpectedValue()
		{
			IParameter<string[]> sut = It.SequenceEquals("foo", "bar");
			string expectedValue = "It.SequenceEquals(\"foo\", \"bar\")";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task WithComparer_ShouldUseComparer()
		{
			IParameter<int[]> sut = It.SequenceEquals(1, 2, 3).Using(new AllEqualComparer());

			bool result = ((IParameterMatch<int[]>)sut).Matches([9, 9, 9,]);

			await That(result).IsTrue();
		}

		[Test]
		public async Task TypedArrayMatch_WithNullValue_ShouldNotMatch()
		{
			IParameter<int[]> sut = It.SequenceEquals(1, 2, 3);

			bool result = ((IParameterMatch<int[]>)sut).Matches(null!);

			await That(result).IsFalse();
		}

		public sealed class DoTests
		{
			[Test]
			public async Task Do_MultipleRegistrations_ShouldAllInvoke()
			{
				It.ISequenceEqualsParameter<int> sut = It.SequenceEquals(1, 2, 3);
				int firstCount = 0;
				int secondCount = 0;
				((IParameterWithCallback<int[]>)sut).Do(_ => firstCount++);
				((IParameterWithCallback<int[]>)sut).Do(_ => secondCount++);

				int[] source = [1, 2, 3,];
				((IParameterMatch<int[]>)sut).InvokeCallbacks(source);

				await That(firstCount).IsEqualTo(1);
				await That(secondCount).IsEqualTo(1);
			}

			[Test]
			public async Task Do_RegistersCallbackForArray()
			{
				It.ISequenceEqualsParameter<int> sut = It.SequenceEquals(1, 2, 3);
				int[]? captured = null;
				((IParameterWithCallback<int[]>)sut).Do(value => captured = value);

				int[] source = [1, 2, 3,];
				((IParameterMatch<int[]>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForICollection()
			{
				It.ISequenceEqualsParameter<int> sut = It.SequenceEquals(1, 2, 3);
				ICollection<int>? captured = null;
				((IParameterWithCallback<ICollection<int>>)sut).Do(value => captured = value);

				List<int> source = [1, 2, 3,];
				((IParameterMatch<ICollection<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForIEnumerable()
			{
				It.ISequenceEqualsParameter<int> sut = It.SequenceEquals(1, 2, 3);
				IEnumerable<int>? captured = null;
				((IParameterWithCallback<IEnumerable<int>>)sut).Do(value => captured = value);

				List<int> source = [1, 2, 3,];
				((IParameterMatch<IEnumerable<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForIList()
			{
				It.ISequenceEqualsParameter<int> sut = It.SequenceEquals(1, 2, 3);
				IList<int>? captured = null;
				((IParameterWithCallback<IList<int>>)sut).Do(value => captured = value);

				List<int> source = [1, 2, 3,];
				((IParameterMatch<IList<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForIReadOnlyCollection()
			{
				It.ISequenceEqualsParameter<int> sut = It.SequenceEquals(1, 2, 3);
				IReadOnlyCollection<int>? captured = null;
				((IParameterWithCallback<IReadOnlyCollection<int>>)sut).Do(value => captured = value);

				List<int> source = [1, 2, 3,];
				((IParameterMatch<IReadOnlyCollection<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForIReadOnlyList()
			{
				It.ISequenceEqualsParameter<int> sut = It.SequenceEquals(1, 2, 3);
				IReadOnlyList<int>? captured = null;
				((IParameterWithCallback<IReadOnlyList<int>>)sut).Do(value => captured = value);

				List<int> source = [1, 2, 3,];
				((IParameterMatch<IReadOnlyList<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForList()
			{
				It.ISequenceEqualsParameter<int> sut = It.SequenceEquals(1, 2, 3);
				List<int>? captured = null;
				((IParameterWithCallback<List<int>>)sut).Do(value => captured = value);

				List<int> source = [1, 2, 3,];
				((IParameterMatch<List<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}

			[Test]
			public async Task Do_RegistersCallbackForQueue()
			{
				It.ISequenceEqualsParameter<int> sut = It.SequenceEquals(1, 2, 3);
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
				It.ISequenceEqualsParameter<int> sut = It.SequenceEquals(1, 2, 3);
				Stack<int>? captured = null;
				((IParameterWithCallback<Stack<int>>)sut).Do(value => captured = value);

				Stack<int> source = new();
				source.Push(1);
				((IParameterMatch<Stack<int>>)sut).InvokeCallbacks(source);

				await That(captured).IsSameAs(source);
			}
		}

		public interface ISequenceCollectionConsumer
		{
			int WithArray(int[] items);
			int WithList(List<int> items);
			int WithEnumerable(IEnumerable<int> items);
			int WithQueue(Queue<int> items);
			int WithReadOnlyList(IReadOnlyList<int> items);
			int WithStack(Stack<int> items);
		}
	}
}
