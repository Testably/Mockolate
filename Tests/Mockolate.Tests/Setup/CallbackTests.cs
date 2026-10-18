using System.Collections.Generic;
using Mockolate.Setup;

namespace Mockolate.Tests.Setup;

public class CallbackTests
{
	public sealed class InvokeForCallbacksTests
	{
		[Test]
		public async Task ShouldIncludeIndexWhenMatching()
		{
			bool wasInvoked = false;
			List<int> values = [];
			Callback<Action> sut = new(() => { });
			sut.Only(2);
			sut.When(v => v > 1);

			int index = 0;
			for (int i = 0; i < 5; i++)
			{
				sut.Invoke<object?>(wasInvoked, ref index, null, (v, _, _) => values.Add(v));
			}

			await That(values).IsEqualTo([2, 3,]);
		}

		[Test]
		[Arguments(2, 0, 0, 1, 2, 2, 2, 2, 2, 2, 2, 2)]
		[Arguments(2, 1, 0, 0, 1, 2, 2, 2, 2, 2, 2, 2)]
		public async Task ShouldIncrementIndexOnceWhenCallbackIsExhausted(
			int only, int when, params int[] expectResult)
		{
			bool wasInvoked = false;
			List<int> indexValues = [];
			Callback<Action> sut = new(() => { });
			sut.Only(only);
			sut.When(v => v > when);

			int index = 0;
			for (int iteration = 1; iteration <= 10; iteration++)
			{
				sut.Invoke<object?>(wasInvoked, ref index, null, static (_, _, _) => { });
				indexValues.Add(index);
			}

			await That(indexValues).IsEqualTo(expectResult);
		}

		[Test]
		[Arguments(2, 2, 0, 1, 1, 2, 2, 2, 2, 2, 2, 2)]
		[Arguments(2, 3, 0, 1, 1, 2, 2, 3, 3, 3, 3, 3)]
		public async Task ShouldIncrementIndexWheneverForIsExhausted(
			int @for, int only, params int[] expectResult)
		{
			bool wasInvoked = false;
			List<int> indexValues = [];
			Callback<Action> sut = new(() => { });
			sut.For(@for);
			sut.Only(only);

			int index = 0;
			for (int iteration = 1; iteration <= 10; iteration++)
			{
				sut.Invoke<object?>(wasInvoked, ref index, null, static (_, _, _) => { });
				indexValues.Add(index);
			}

			await That(indexValues).IsEqualTo(expectResult);
		}

		[Test]
		public async Task ShouldLimitExecutionWhenRunningInParallel()
		{
			bool wasInvoked = false;
			List<int> values = [];
			Callback<Action> sut = new(() => { });
			sut.Only(2);
			sut.InParallel();

			int index = 0;
			for (int i = 0; i < 5; i++)
			{
				sut.Invoke<object?>(wasInvoked, ref index, null, (v, _, _) => values.Add(v));
			}

			await That(values).IsEqualTo([0, 1,]);
		}

		[Test]
		public async Task WhenCheck_ShouldOnlyBeCalledWhenOnlyLimitIsNotReached()
		{
			bool wasInvoked = false;
			int[] expectResult = [0, 1, 2, 3, 4, 5, 6, 7,];
			List<int> invocationChecks = [];
			int invocationCount = 0;
			Callback<Action> sut = new(() => { invocationCount++; });
			sut.For(2);
			sut.When(v =>
			{
				invocationChecks.Add(v);
				return true;
			});
			sut.Only(4);

			int index = 0;
			for (int iteration = 1; iteration <= 10; iteration++)
			{
				sut.Invoke<object?>(wasInvoked, ref index, null, static (_, c, _) => c());
			}

			await That(invocationChecks).IsEqualTo(expectResult);
			await That(invocationCount).IsEqualTo(8);
		}

		[Test]
		[Arguments(1, false)]
		[Arguments(2, false)]
		[Arguments(3, true)]
		[Arguments(4, true)]
		[Arguments(5, false)]
		public async Task WithForAndWhen_ShouldMatchInExpectedIterations(
			int iterations, bool expectResult)
		{
			bool wasInvoked = false;
			Callback<Action> sut = new(() => { });
			sut.Only(2);
			sut.When(v => v > 1);

			int index = 0;
			bool result = false;
			for (int iteration = 1; iteration <= iterations; iteration++)
			{
				result = sut.Invoke<object?>(wasInvoked, ref index, null, static (_, _, _) => { });
			}

			await That(result).IsEqualTo(expectResult);
		}
	}

	public sealed class InvokeWithStateForCallbacksTests
	{
		[Test]
		public async Task ShouldIncludeIndexWhenMatching()
		{
			bool wasInvoked = false;
			List<int> values = [];
			Callback<Action> sut = new(() => { });
			sut.Only(2);
			sut.When(v => v > 1);

			int index = 0;
			for (int i = 0; i < 5; i++)
			{
				sut.Invoke(wasInvoked, ref index, values, static (v, _, list) => list.Add(v));
			}

			await That(values).IsEqualTo([2, 3,]);
		}

		[Test]
		[Arguments(2, 2, 0, 1, 1, 2, 2, 2, 2, 2, 2, 2)]
		[Arguments(2, 3, 0, 1, 1, 2, 2, 3, 3, 3, 3, 3)]
		public async Task ShouldIncrementIndexWheneverForIsExhausted(
			int @for, int only, params int[] expectResult)
		{
			bool wasInvoked = false;
			List<int> indexValues = [];
			Callback<Action> sut = new(() => { });
			sut.For(@for);
			sut.Only(only);

			int index = 0;
			for (int iteration = 1; iteration <= 10; iteration++)
			{
				sut.Invoke(wasInvoked, ref index, 0, static (_, _, _) => { });
				indexValues.Add(index);
			}

			await That(indexValues).IsEqualTo(expectResult);
		}

		[Test]
		public async Task ShouldLimitExecutionWhenRunningInParallel()
		{
			bool wasInvoked = false;
			List<int> values = [];
			Callback<Action> sut = new(() => { });
			sut.Only(2);
			sut.InParallel();

			int index = 0;
			for (int i = 0; i < 5; i++)
			{
				sut.Invoke(wasInvoked, ref index, values, static (v, _, list) => list.Add(v));
			}

			await That(values).IsEqualTo([0, 1,]);
		}
	}

	public sealed class InvokeForVoidThrowsTests
	{
		[Test]
		public async Task ShouldIncludeIndexWhenMatching()
		{
			List<int> values = [];
			Callback<Action> sut = new(() => { });
			sut.Only(2);
			sut.When(v => v > 1);

			int index = 0;
			for (int i = 0; i < 5; i++)
			{
				sut.Invoke<object?>(ref index, null, (v, _, _) => values.Add(v));
			}

			await That(values).IsEqualTo([2, 3,]);
		}

		[Test]
		public async Task WhenCheck_ShouldOnlyBeCalledWhenOnlyLimitIsNotReached()
		{
			int[] expectResult = [0, 1, 2, 3, 4, 5, 6, 7,];
			List<int> invocationChecks = [];
			int invocationCount = 0;
			Callback<Action> sut = new(() => { invocationCount++; });
			sut.For(2);
			sut.When(v =>
			{
				invocationChecks.Add(v);
				return true;
			});
			sut.Only(4);

			int index = 0;
			for (int iteration = 1; iteration <= 10; iteration++)
			{
				sut.Invoke<object?>(ref index, null, static (_, c, _) => c());
			}

			await That(invocationChecks).IsEqualTo(expectResult);
			await That(invocationCount).IsEqualTo(8);
		}

		[Test]
		[Arguments(1, false)]
		[Arguments(2, false)]
		[Arguments(3, true)]
		[Arguments(4, true)]
		[Arguments(5, false)]
		public async Task WithForAndWhen_ShouldMatchInExpectedIterations(
			int iterations, bool expectResult)
		{
			Callback<Action> sut = new(() => { });
			sut.Only(2);
			sut.When(v => v > 1);

			int index = 0;
			bool result = false;
			for (int iteration = 1; iteration <= iterations; iteration++)
			{
				result = sut.Invoke<object?>(ref index, null, static (_, _, _) => { });
			}

			await That(result).IsEqualTo(expectResult);
		}
	}

	public sealed class InvokeForReturnThrowsTests
	{
		[Test]
		public async Task ShouldIncludeIndexWhenMatching()
		{
			List<int> values = [];
			Callback<Action> sut = new(() => { });
			sut.Only(2);
			sut.When(v => v > 1);

			int index = 0;
			for (int i = 0; i < 5; i++)
			{
				sut.Invoke<object?, string>(ref index, null, (v, _, _) =>
				{
					values.Add(v);
					return $"foo-{v}";
				}, out string? _);
			}

			await That(values).IsEqualTo([2, 3,]);
		}

		[Test]
		public async Task WhenCheck_ShouldOnlyBeCalledWhenOnlyLimitIsNotReached()
		{
			int[] expectResult = [0, 1, 2, 3, 4, 5, 6, 7,];
			List<int> invocationChecks = [];
			int invocationCount = 0;
			Callback<Action> sut = new(() => { invocationCount++; });
			sut.For(2);
			sut.When(v =>
			{
				invocationChecks.Add(v);
				return true;
			});
			sut.Only(4);

			int index = 0;
			for (int iteration = 1; iteration <= 10; iteration++)
			{
				sut.Invoke<object?, string>(ref index, null, static (_, c, _) =>
				{
					c();
					return "foo";
				}, out string? _);
			}

			await That(invocationChecks).IsEqualTo(expectResult);
			await That(invocationCount).IsEqualTo(8);
		}

		[Test]
		[Arguments(1, false)]
		[Arguments(2, false)]
		[Arguments(3, true)]
		[Arguments(4, true)]
		[Arguments(5, false)]
		public async Task WithForAndWhen_ShouldMatchInExpectedIterations(
			int iterations, bool expectResult)
		{
			Callback<Action> sut = new(() => { });
			sut.Only(2);
			sut.When(v => v > 1);

			int index = 0;
			bool result = false;
			for (int iteration = 1; iteration <= iterations; iteration++)
			{
				result = sut.Invoke<object?, string>(ref index, null, static (_, _, _) => "foo", out string? _);
			}

			await That(result).IsEqualTo(expectResult);
		}
	}

	public sealed class InvokeWithStateForReturnThrowsTests
	{
		[Test]
		public async Task ShouldIncludeIndexWhenMatching()
		{
			List<int> values = [];
			Callback<Action> sut = new(() => { });
			sut.Only(2);
			sut.When(v => v > 1);

			int index = 0;
			for (int i = 0; i < 5; i++)
			{
				sut.Invoke<List<int>, string>(ref index, values,
					static (v, _, list) =>
					{
						list.Add(v);
						return "";
					}, out _);
			}

			await That(values).IsEqualTo([2, 3,]);
		}
	}
}
