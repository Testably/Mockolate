using System.Collections.Generic;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class SetupIndexerTests
{
	public sealed class OnSetTests
	{
		[Test]
		public async Task InParallel_ShouldInvokeCallbacksInSequence()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			int callCount3 = 0;
			IIndexerService sut = IIndexerService.CreateMock();

			sut.Mock.Setup[It.IsAny<int>()]
				.OnSet.Do(() => { callCount1++; })
				.OnSet.Do((p1, _) => { callCount2 += p1; }).InParallel()
				.OnSet.Do((p1, _) => { callCount3 += p1; });

			sut[4] = "foo";
			sut[6] = "foo";
			sut[8] = "foo";
			sut[10] = "foo";

			await That(callCount1).IsEqualTo(2);
			await That(callCount2).IsEqualTo(4 + 6 + 8 + 10);
			await That(callCount3).IsEqualTo(6 + 10);
		}

		[Test]
		[Arguments(1, 1)]
		[Arguments(2, 3)]
		[Arguments(3, 6)]
		public async Task Only_ShouldInvokeCallbacksOnlyTheGivenNumberOfTimes(int times,
			int expectedValue)
		{
			int sum = 0;
			IIndexerService sut = IIndexerService.CreateMock();

			sut.Mock.Setup[It.IsAny<int>()]
				.OnSet.Do((p1, _) => { sum += p1; }).Only(times);

			sut[1] = "foo";
			sut[2] = "foo";
			sut[3] = "foo";
			sut[4] = "foo";
			sut[5] = "foo";

			await That(sum).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task Only_ShouldStopExecutingCallbackAfterTheGivenTimes()
		{
			List<int> invocations = [];
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.OnSet.Do((i, _, _) => { invocations.Add(i); })
				.Only(4);

			for (int i = 0; i < 20; i++)
			{
				sut[i] = $"{i}";
			}

			await That(invocations).IsEqualTo([0, 1, 2, 3,]);
		}

		[Test]
		public async Task Only_WithWhen_ShouldStopExecutingCallbackAfterTheGivenTimes()
		{
			List<int> invocations = [];
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.OnSet.Do((i, _, _) => { invocations.Add(i); })
				.When(x => x > 2)
				.Only(4);

			for (int i = 0; i < 20; i++)
			{
				sut[i] = $"{i}";
			}

			await That(invocations).IsEqualTo([3, 4, 5, 6,]);
		}

		[Test]
		public async Task OnlyOnce_ShouldDeactivateCallbackAfterFirstExecution()
		{
			int callCount = 0;
			int sum = 0;
			IIndexerService sut = IIndexerService.CreateMock();

			sut.Mock.Setup[It.IsAny<int>()]
				.OnSet.Do(() => { callCount++; })
				.OnSet.Do((p1, _) => { sum += p1; }).OnlyOnce();

			sut[1] = "foo";
			sut[2] = "foo";
			sut[3] = "foo";
			sut[4] = "foo";

			await That(callCount).IsEqualTo(3);
			await That(sum).IsEqualTo(2);
		}

		[Test]
		public async Task ShouldExecuteSetterCallbacks()
		{
			int callCount = 0;
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.Satisfies<int>(i => i < 4)]
				.OnSet.Do(_ => { callCount++; });

			sut[1] = "";
			sut[2] = "";
			sut[3] = "";
			sut[4] = "";
			sut[5] = "";
			sut[6] = "";

			await That(callCount).IsEqualTo(3);
		}

		[Test]
		public async Task ShouldExecuteSetterCallbacksWithoutAnyValue()
		{
			int callCount = 0;
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.Satisfies<int>(i => i < 4)]
				.OnSet.Do(() => { callCount++; });

			sut[1] = "";
			sut[2] = "";
			sut[3] = "";
			sut[4] = "";
			sut[5] = "";

			await That(callCount).IsEqualTo(3);
		}

		[Test]
		public async Task ShouldIncludeIncrementingAccessCounter()
		{
			List<int> invocations = [];
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.OnSet.Do((i, _, _) => { invocations.Add(i); });

			for (int i = 0; i < 10; i++)
			{
				sut[10 * i] = $"{i}";
			}

			await That(invocations).IsEqualTo([0, 1, 2, 3, 4, 5, 6, 7, 8, 9,]);
		}

		[Test]
		public async Task ShouldInvokeCallbacksInSequence()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IIndexerService sut = IIndexerService.CreateMock();

			sut.Mock.Setup[It.IsAny<int>()]
				.OnSet.Do(() => { callCount1++; })
				.OnSet.Do((p1, _) => { callCount2 += p1; });

			sut[4] = "foo";
			sut[6] = "foo";
			sut[8] = "foo";
			sut[10] = "foo";

			await That(callCount1).IsEqualTo(2);
			await That(callCount2).IsEqualTo(6 + 10);
		}

		[Test]
		public async Task When_ShouldOnlyExecuteCallbackWhenInvocationCountMatches()
		{
			List<int> invocations = [];
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.OnSet.Do((i, _) => { invocations.Add(i); })
				.When(x => x is > 3 and < 9);

			for (int i = 0; i < 20; i++)
			{
				sut[i] = $"{i}";
			}

			await That(invocations).IsEqualTo([4, 5, 6, 7, 8,]);
		}

		public sealed class With2Levels
		{
			[Test]
			public async Task InParallel_ShouldInvokeCallbacksInSequence()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				int callCount3 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do(() => { callCount1++; })
					.OnSet.Do((p1, _, _) => { callCount2 += p1; }).InParallel()
					.OnSet.Do((p1, _, _) => { callCount3 += p1; });

				sut[4, 2] = "foo";
				sut[6, 2] = "foo";
				sut[8, 2] = "foo";
				sut[10, 2] = "foo";

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(4 + 6 + 8 + 10);
				await That(callCount3).IsEqualTo(6 + 10);
			}

			[Test]
			[Arguments(1, 1)]
			[Arguments(2, 3)]
			[Arguments(3, 6)]
			public async Task Only_ShouldInvokeCallbacksOnlyTheGivenNumberOfTimes(int times,
				int expectedValue)
			{
				int sum = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((p1, _, _) => { sum += p1; }).Only(times);

				sut[1, 2] = "foo";
				sut[2, 2] = "foo";
				sut[3, 2] = "foo";
				sut[4, 2] = "foo";
				sut[5, 2] = "foo";

				await That(sum).IsEqualTo(expectedValue);
			}

			[Test]
			public async Task Only_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _) => { invocations.Add(i); })
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					sut[i, 2 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([0, 1, 2, 3,]);
			}

			[Test]
			public async Task Only_WithWhen_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _) => { invocations.Add(i); })
					.When(x => x > 2)
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					sut[i, 2 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([3, 4, 5, 6,]);
			}

			[Test]
			public async Task OnlyOnce_ShouldDeactivateCallbackAfterFirstExecution()
			{
				int callCount = 0;
				int sum = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do(() => { callCount++; })
					.OnSet.Do((p1, _, _) => { sum += p1; }).OnlyOnce();

				sut[1, 2] = "foo";
				sut[2, 2] = "foo";
				sut[3, 2] = "foo";
				sut[4, 2] = "foo";

				await That(callCount).IsEqualTo(3);
				await That(sum).IsEqualTo(2);
			}

			[Test]
			public async Task ShouldExecuteSetterCallbacks()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 4), It.Satisfies<int>(i => i < 4)]
					.OnSet.Do(v => { callCount += v.Length; });

				sut[1, 1] = "a"; // yes (1)
				sut[1, 2] = "bb"; // yes (2)
				sut[1, 3] = "ccc"; // yes (3)
				sut[1, 4] = "dddd"; // no
				sut[1, 5] = "eeeee"; // no
				sut[6, 1] = "ffffff"; // no
				sut[6, 7] = "ggggggg"; // no
				sut[8, -9] = "hhhhhhhh"; // no
				sut[3, 3] = "iiiiiiiii"; // yes (9)

				await That(callCount).IsEqualTo(15);
			}

			[Test]
			public async Task ShouldExecuteSetterCallbacksWithoutAnyValue()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 4), It.Satisfies<int>(i => i < 4)]
					.OnSet.Do(() => { callCount++; });

				sut[1, 1] = ""; // yes
				sut[1, 2] = ""; // yes
				sut[1, 3] = ""; // yes
				sut[1, 4] = ""; // no
				sut[5, 1] = ""; // no
				sut[2, 1] = ""; // yes

				await That(callCount).IsEqualTo(4);
			}

			[Test]
			public async Task ShouldIncludeIncrementingAccessCounter()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _) => { invocations.Add(i); });

				for (int i = 0; i < 10; i++)
				{
					sut[10 * i, 20 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([0, 1, 2, 3, 4, 5, 6, 7, 8, 9,]);
			}

			[Test]
			public async Task ShouldInvokeCallbacksInSequence()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do(() => { callCount1++; })
					.OnSet.Do((p1, _, _) => { callCount2 += p1; });

				sut[4, 2] = "foo";
				sut[6, 2] = "foo";
				sut[8, 2] = "foo";
				sut[10, 2] = "foo";

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(6 + 10);
			}

			[Test]
			public async Task When_ShouldOnlyExecuteCallbackWhenInvocationCountMatches()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _) => { invocations.Add(i); })
					.When(x => x is > 3 and < 9);

				for (int i = 0; i < 20; i++)
				{
					sut[i, 2 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([4, 5, 6, 7, 8,]);
			}
		}

		public sealed class With3Levels
		{
			[Test]
			public async Task InParallel_ShouldInvokeCallbacksInSequence()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				int callCount3 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do(() => { callCount1++; })
					.OnSet.Do((p1, _, _, _) => { callCount2 += p1; }).InParallel()
					.OnSet.Do((p1, _, _, _) => { callCount3 += p1; });

				sut[4, 2, 3] = "foo";
				sut[6, 2, 3] = "foo";
				sut[8, 2, 3] = "foo";
				sut[10, 2, 3] = "foo";

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(4 + 6 + 8 + 10);
				await That(callCount3).IsEqualTo(6 + 10);
			}

			[Test]
			[Arguments(1, 1)]
			[Arguments(2, 3)]
			[Arguments(3, 6)]
			public async Task Only_ShouldInvokeCallbacksOnlyTheGivenNumberOfTimes(int times,
				int expectedValue)
			{
				int sum = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((p1, _, _, _) => { sum += p1; }).Only(times);

				sut[1, 2, 3] = "foo";
				sut[2, 2, 3] = "foo";
				sut[3, 2, 3] = "foo";
				sut[4, 2, 3] = "foo";
				sut[5, 2, 3] = "foo";

				await That(sum).IsEqualTo(expectedValue);
			}

			[Test]
			public async Task Only_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _, _) => { invocations.Add(i); })
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					sut[i, 2 * i, 3 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([0, 1, 2, 3,]);
			}

			[Test]
			public async Task Only_WithWhen_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _, _) => { invocations.Add(i); })
					.When(x => x > 2)
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					sut[i, 2 * i, 3 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([3, 4, 5, 6,]);
			}

			[Test]
			public async Task OnlyOnce_ShouldDeactivateCallbackAfterFirstExecution()
			{
				int callCount = 0;
				int sum = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do(() => { callCount++; })
					.OnSet.Do((p1, _, _, _) => { sum += p1; }).OnlyOnce();

				sut[1, 2, 3] = "foo";
				sut[2, 2, 3] = "foo";
				sut[3, 2, 3] = "foo";
				sut[4, 2, 3] = "foo";

				await That(callCount).IsEqualTo(3);
				await That(sum).IsEqualTo(2);
			}

			[Test]
			public async Task ShouldExecuteSetterCallbacks()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 4), It.Satisfies<int>(i => i < 4),
						It.Satisfies<int>(i => i < 4)]
					.OnSet.Do(v => { callCount += v.Length; });

				sut[1, 1, 1] = "a"; // yes (1)
				sut[1, 2, 1] = "bb"; // yes (2)
				sut[3, 1, 2] = "ccc"; // yes (3)
				sut[1, 1, 4] = "dddd"; // no
				sut[1, 5, 1] = "eeeee"; // no
				sut[6, 1, 1] = "ffffff"; // no
				sut[6, 7, 8] = "ggggggg"; // no
				sut[8, -9, 1] = "hhhhhhhh"; // no
				sut[3, 3, 3] = "iiiiiiiii"; // yes (9)

				await That(callCount).IsEqualTo(15);
			}

			[Test]
			public async Task ShouldExecuteSetterCallbacksWithoutAnyValue()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 4), It.Satisfies<int>(i => i < 4),
						It.Satisfies<int>(i => i < 4)]
					.OnSet.Do(() => { callCount++; });

				sut[1, 1, 1] = ""; // yes
				sut[1, 1, 2] = ""; // yes
				sut[1, 3, 1] = ""; // yes
				sut[1, 1, 4] = ""; // no
				sut[1, 5, 1] = ""; // no
				sut[6, 1, 1] = ""; // no
				sut[2, 1, 1] = ""; // yes

				await That(callCount).IsEqualTo(4);
			}

			[Test]
			public async Task ShouldIncludeIncrementingAccessCounter()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _, _) => { invocations.Add(i); });

				for (int i = 0; i < 10; i++)
				{
					sut[10 * i, 20 * i, 30 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([0, 1, 2, 3, 4, 5, 6, 7, 8, 9,]);
			}

			[Test]
			public async Task ShouldInvokeCallbacksInSequence()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do(() => { callCount1++; })
					.OnSet.Do((p1, _, _, _) => { callCount2 += p1; });

				sut[4, 2, 3] = "foo";
				sut[6, 2, 3] = "foo";
				sut[8, 2, 3] = "foo";
				sut[10, 2, 3] = "foo";

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(6 + 10);
			}

			[Test]
			public async Task When_ShouldOnlyExecuteCallbackWhenInvocationCountMatches()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _) => { invocations.Add(i); })
					.When(x => x is > 3 and < 9);

				for (int i = 0; i < 20; i++)
				{
					sut[i, 2 * i, 3 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([4, 5, 6, 7, 8,]);
			}
		}

		public sealed class With4Levels
		{
			[Test]
			public async Task InParallel_ShouldInvokeCallbacksInSequence()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				int callCount3 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do(() => { callCount1++; })
					.OnSet.Do((p1, _, _, _, _) => { callCount2 += p1; }).InParallel()
					.OnSet.Do((p1, _, _, _, _) => { callCount3 += p1; });

				sut[4, 2, 3, 4] = "foo";
				sut[6, 2, 3, 4] = "foo";
				sut[8, 2, 3, 4] = "foo";
				sut[10, 2, 3, 4] = "foo";

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(4 + 6 + 8 + 10);
				await That(callCount3).IsEqualTo(6 + 10);
			}

			[Test]
			[Arguments(1, 1)]
			[Arguments(2, 3)]
			[Arguments(3, 6)]
			public async Task Only_ShouldInvokeCallbacksOnlyTheGivenNumberOfTimes(int times,
				int expectedValue)
			{
				int sum = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((p1, _, _, _, _) => { sum += p1; }).Only(times);

				sut[1, 2, 3, 4] = "foo";
				sut[2, 2, 3, 4] = "foo";
				sut[3, 2, 3, 4] = "foo";
				sut[4, 2, 3, 4] = "foo";
				sut[5, 2, 3, 4] = "foo";

				await That(sum).IsEqualTo(expectedValue);
			}

			[Test]
			public async Task Only_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _, _, _) => { invocations.Add(i); })
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					sut[i, 2 * i, 3 * i, 4 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([0, 1, 2, 3,]);
			}

			[Test]
			public async Task Only_WithWhen_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _, _, _) => { invocations.Add(i); })
					.When(x => x > 2)
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					sut[i, 2 * i, 3 * i, 4 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([3, 4, 5, 6,]);
			}

			[Test]
			public async Task OnlyOnce_ShouldDeactivateCallbackAfterFirstExecution()
			{
				int callCount = 0;
				int sum = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do(() => { callCount++; })
					.OnSet.Do((p1, _, _, _, _) => { sum += p1; }).OnlyOnce();

				sut[1, 2, 3, 4] = "foo";
				sut[2, 2, 3, 4] = "foo";
				sut[3, 2, 3, 4] = "foo";
				sut[4, 2, 3, 4] = "foo";

				await That(callCount).IsEqualTo(3);
				await That(sum).IsEqualTo(2);
			}

			[Test]
			public async Task ShouldExecuteSetterCallbacks()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 5), It.Satisfies<int>(i => i < 5),
						It.Satisfies<int>(i => i < 5), It.Satisfies<int>(i => i < 5)]
					.OnSet.Do(v => { callCount += v.Length; });

				sut[1, 1, 1, 1] = "a"; // yes (1)
				sut[1, 2, 1, 3] = "bb"; // yes (2)
				sut[3, 1, 2, 4] = "ccc"; // yes (3)
				sut[1, 1, 4, 3] = "dddd"; // yes (4)
				sut[1, 5, 1, 1] = "eeeee"; // no
				sut[6, 1, 1, 1] = "ffffff"; // no
				sut[6, 7, 8, 9] = "ggggggg"; // no
				sut[8, -9, 1, 3] = "hhhhhhhh"; // no
				sut[4, 4, 4, 4] = "iiiiiiiii"; // yes (9)

				await That(callCount).IsEqualTo(19);
			}

			[Test]
			public async Task ShouldExecuteSetterCallbacksWithoutAnyValue()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 5), It.Satisfies<int>(i => i < 5),
						It.Satisfies<int>(i => i < 5), It.Satisfies<int>(i => i < 5)]
					.OnSet.Do(() => { callCount++; });

				sut[1, 1, 1, 1] = ""; // yes
				sut[1, 1, 2, 2] = ""; // yes
				sut[1, 3, 1, 3] = ""; // yes
				sut[1, 1, 4, 4] = ""; // yes
				sut[1, 5, 1, 1] = ""; // no
				sut[6, 1, 1, 1] = ""; // no
				sut[2, 1, 1, 3] = ""; // yes

				await That(callCount).IsEqualTo(5);
			}

			[Test]
			public async Task ShouldIncludeIncrementingAccessCounter()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _, _, _) => { invocations.Add(i); });

				for (int i = 0; i < 10; i++)
				{
					sut[10 * i, 20 * i, 30 * i, 40 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([0, 1, 2, 3, 4, 5, 6, 7, 8, 9,]);
			}

			[Test]
			public async Task ShouldInvokeCallbacksInSequence()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do(() => { callCount1++; })
					.OnSet.Do((p1, _, _, _, _) => { callCount2 += p1; });

				sut[4, 2, 3, 4] = "foo";
				sut[6, 2, 3, 4] = "foo";
				sut[8, 2, 3, 4] = "foo";
				sut[10, 2, 3, 4] = "foo";

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(6 + 10);
			}

			[Test]
			public async Task When_ShouldOnlyExecuteCallbackWhenInvocationCountMatches()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((p1, _, _, _, _) => { invocations.Add(p1); })
					.When(x => x is > 3 and < 9);

				for (int i = 0; i < 20; i++)
				{
					sut[i, 2 * i, 3 * i, 4 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([4, 5, 6, 7, 8,]);
			}
		}

		public sealed class With5Levels
		{
			[Test]
			public async Task InParallel_ShouldInvokeCallbacksInSequence()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				int callCount3 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do(() => { callCount1++; })
					.OnSet.Do((p1, _, _, _, _, _) => { callCount2 += p1; }).InParallel()
					.OnSet.Do((p1, _, _, _, _, _) => { callCount3 += p1; });

				sut[4, 2, 3, 4, 5] = "foo";
				sut[6, 2, 3, 4, 5] = "foo";
				sut[8, 2, 3, 4, 5] = "foo";
				sut[10, 2, 3, 4, 5] = "foo";

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(4 + 6 + 8 + 10);
				await That(callCount3).IsEqualTo(6 + 10);
			}

			[Test]
			[Arguments(1, 1)]
			[Arguments(2, 3)]
			[Arguments(3, 6)]
			public async Task Only_ShouldInvokeCallbacksOnlyTheGivenNumberOfTimes(int times,
				int expectedValue)
			{
				int sum = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((p1, _, _, _, _, _) => { sum += p1; }).Only(times);

				sut[1, 2, 3, 4, 5] = "foo";
				sut[2, 2, 3, 4, 5] = "foo";
				sut[3, 2, 3, 4, 5] = "foo";
				sut[4, 2, 3, 4, 5] = "foo";
				sut[5, 2, 3, 4, 5] = "foo";

				await That(sum).IsEqualTo(expectedValue);
			}

			[Test]
			public async Task Only_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _, _, _, _) => { invocations.Add(i); })
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					sut[i, 2 * i, 3 * i, 4 * i, 5 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([0, 1, 2, 3,]);
			}

			[Test]
			public async Task Only_WithWhen_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _, _, _, _) => { invocations.Add(i); })
					.When(x => x > 2)
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					sut[i, 2 * i, 3 * i, 4 * i, 5 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([3, 4, 5, 6,]);
			}

			[Test]
			public async Task OnlyOnce_ShouldDeactivateCallbackAfterFirstExecution()
			{
				int callCount = 0;
				int sum = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do(() => { callCount++; })
					.OnSet.Do((p1, _, _, _, _, _) => { sum += p1; }).OnlyOnce();

				sut[1, 2, 3, 4, 5] = "foo";
				sut[2, 2, 3, 4, 5] = "foo";
				sut[3, 2, 3, 4, 5] = "foo";
				sut[4, 2, 3, 4, 5] = "foo";

				await That(callCount).IsEqualTo(3);
				await That(sum).IsEqualTo(2);
			}

			[Test]
			public async Task ShouldExecuteSetterCallbacks()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 6), It.Satisfies<int>(i => i < 6),
						It.Satisfies<int>(i => i < 6), It.Satisfies<int>(i => i < 6), It.Satisfies<int>(i => i < 6)]
					.OnSet.Do(v => { callCount += v.Length; });

				sut[1, 1, 1, 1, 1] = "a"; // yes (1)
				sut[4, 1, 2, 1, 3] = "bb"; // yes (2)
				sut[1, 3, 1, 2, 4] = "ccc"; // yes (3)
				sut[2, 1, 1, 4, 3] = "dddd"; // yes (4)
				sut[1, 1, 5, 1, 1] = "eeeee"; // yes (5)
				sut[1, 6, 1, 1, 1] = "ffffff"; // no
				sut[5, 6, 7, 8, 9] = "ggggggg"; // no
				sut[8, 8, -9, 1, 3] = "hhhhhhhh"; // no
				sut[5, 5, 5, 5, 5] = "iiiiiiiii"; // yes (9)

				await That(callCount).IsEqualTo(24);
			}

			[Test]
			public async Task ShouldExecuteSetterCallbacksWithoutAnyValue()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 6), It.Satisfies<int>(i => i < 6),
						It.Satisfies<int>(i => i < 6), It.Satisfies<int>(i => i < 6), It.Satisfies<int>(i => i < 6)]
					.OnSet.Do(() => { callCount++; });

				sut[1, 1, 1, 1, 1] = ""; // yes
				sut[1, 1, 1, 2, 2] = ""; // yes
				sut[1, 1, 3, 1, 3] = ""; // yes
				sut[1, 1, 1, 4, 4] = ""; // yes
				sut[1, 1, 6, 1, 1] = ""; // no
				sut[1, 6, 1, 1, 1] = ""; // no
				sut[1, 2, 1, 1, 3] = ""; // yes

				await That(callCount).IsEqualTo(5);
			}

			[Test]
			public async Task ShouldIncludeIncrementingAccessCounter()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((i, _, _, _, _, _, _) => { invocations.Add(i); });

				for (int i = 0; i < 10; i++)
				{
					sut[10 * i, 20 * i, 30 * i, 40 * i, 50 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([0, 1, 2, 3, 4, 5, 6, 7, 8, 9,]);
			}

			[Test]
			public async Task ShouldInvokeCallbacksInSequence()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do(() => { callCount1++; })
					.OnSet.Do((p1, _, _, _, _, _) => { callCount2 += p1; });

				sut[4, 2, 3, 4, 5] = "foo";
				sut[6, 2, 3, 4, 5] = "foo";
				sut[8, 2, 3, 4, 5] = "foo";
				sut[10, 2, 3, 4, 5] = "foo";

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(6 + 10);
			}

			[Test]
			public async Task When_ShouldOnlyExecuteCallbackWhenInvocationCountMatches()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnSet.Do((p1, _, _, _, _, _) => { invocations.Add(p1); })
					.When(x => x is > 3 and < 9);

				for (int i = 0; i < 20; i++)
				{
					sut[i, 2 * i, 3 * i, 4 * i, 5 * i] = $"{i}";
				}

				await That(invocations).IsEqualTo([4, 5, 6, 7, 8,]);
			}
		}
	}
}
