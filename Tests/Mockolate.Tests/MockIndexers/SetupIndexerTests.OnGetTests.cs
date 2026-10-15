using System.Collections.Generic;
using Mockolate.Setup;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class SetupIndexerTests
{
	public sealed class OnGetTests
	{
		[Test]
		public async Task InParallel_ShouldInvokeParallelCallbacksAlways()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			int callCount3 = 0;
			IIndexerService sut = IIndexerService.CreateMock();

			sut.Mock.Setup[It.IsAny<int>()]
				.OnGet.Do(() => { callCount1++; })
				.OnGet.Do(v => { callCount2 += v; }).InParallel()
				.OnGet.Do(v => { callCount3 += v; });

			_ = sut[1];
			_ = sut[2];
			_ = sut[3];
			_ = sut[4];

			await That(callCount1).IsEqualTo(2);
			await That(callCount2).IsEqualTo(10);
			await That(callCount3).IsEqualTo(6);
		}

		[Test]
		[Arguments(1, 1)]
		[Arguments(2, 3)]
		[Arguments(3, 6)]
		public async Task Only_ShouldInvokeCallbacksOnlyTheGivenNumberOfTimes(int times, int expectedValue)
		{
			int sum = 0;
			IIndexerService sut = IIndexerService.CreateMock();

			sut.Mock.Setup[It.IsAny<int>()]
				.OnGet.Do(v => { sum += v; }).Only(times);

			_ = sut[1];
			_ = sut[2];
			_ = sut[3];
			_ = sut[4];

			await That(sum).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task Only_ShouldStopExecutingCallbackAfterTheGivenTimes()
		{
			List<int> invocations = [];
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.OnGet.Do((i, _) => { invocations.Add(i); })
				.Only(4);

			for (int i = 0; i < 20; i++)
			{
				_ = sut[i];
			}

			await That(invocations).IsEqualTo([0, 1, 2, 3,]);
		}

		[Test]
		public async Task Only_WithWhen_ShouldStopExecutingCallbackAfterTheGivenTimes()
		{
			List<int> invocations = [];
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.OnGet.Do((i, _) => { invocations.Add(i); })
				.When(x => x > 2)
				.Only(4);

			for (int i = 0; i < 20; i++)
			{
				_ = sut[i];
			}

			await That(invocations).IsEqualTo([3, 4, 5, 6,]);
		}

		[Test]
		public async Task OnlyOnce_ShouldDeactivateCallbackAfterFirstExecution()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IIndexerService sut = IIndexerService.CreateMock();

			sut.Mock.Setup[It.IsAny<int>()]
				.OnGet.Do(() => { callCount1++; })
				.OnGet.Do(v => { callCount2 += v; }).OnlyOnce();

			_ = sut[1];
			_ = sut[2];
			_ = sut[3];
			_ = sut[4];

			await That(callCount1).IsEqualTo(3);
			await That(callCount2).IsEqualTo(2);
		}

		[Test]
		public async Task ShouldExecuteGetterCallbacks()
		{
			int callCount = 0;
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.Satisfies<int>(i => i < 4)]
				.OnGet.Do(() => { callCount++; });

			_ = sut[1];
			_ = sut[2];
			_ = sut[3];
			_ = sut[4];
			_ = sut[5];
			_ = sut[6];

			await That(callCount).IsEqualTo(3);
		}

		[Test]
		public async Task ShouldExecuteGetterCallbacksWithValue()
		{
			int callCount = 0;
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.Satisfies<int>(i => i < 4)]
				.OnGet.Do(v => { callCount += v; });

			_ = sut[1];
			_ = sut[2];
			_ = sut[3];
			_ = sut[4];
			_ = sut[5];

			await That(callCount).IsEqualTo(6);
		}

		[Test]
		public async Task ShouldIncludeIncrementingAccessCounter()
		{
			List<int> invocations = [];
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.OnGet.Do((i, _, _) => { invocations.Add(i); });

			for (int i = 0; i < 10; i++)
			{
				_ = sut[10 * i];
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
				.OnGet.Do(() => { callCount1++; })
				.OnGet.Do(v => { callCount2 += v; });

			_ = sut[1];
			_ = sut[2];
			_ = sut[3];
			_ = sut[4];

			await That(callCount1).IsEqualTo(2);
			await That(callCount2).IsEqualTo(6);
		}

		[Test]
		public async Task When_ShouldOnlyExecuteCallbackWhenInvocationCountMatches()
		{
			List<int> invocations = [];
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.OnGet.Do((i, _) => { invocations.Add(i); })
				.When(x => x is > 3 and < 9);

			for (int i = 0; i < 20; i++)
			{
				_ = sut[i];
			}

			await That(invocations).IsEqualTo([4, 5, 6, 7, 8,]);
		}

		[Test]
		public async Task WhenLengthDoesNotMatch_ShouldIgnore()
		{
			int callCount = 0;
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.OnGet.Do(() => { callCount++; });

			_ = sut[1];
			_ = sut[2, 2];
			_ = sut[3, 3, 3];

			await That(callCount).IsEqualTo(1);
		}

		[Test]
		public async Task WithoutCallback_IIndexerGetterSetupCallbackBuilder_ShouldNotThrow()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			IIndexerGetterSetupCallbackBuilder<string, int> setup =
				sut.Mock.Setup[It.IsAny<int>()];

			void ActWhen()
			{
				setup.When(_ => true);
			}

			void ActFor()
			{
				setup.For(2);
			}

			void ActInParallel()
			{
				setup.InParallel();
			}

			await That(ActWhen).DoesNotThrow();
			await That(ActFor).DoesNotThrow();
			await That(ActInParallel).DoesNotThrow();
		}

		[Test]
		public async Task WithoutCallback_IIndexerGetterSetupCallbackWhenBuilder_ShouldNotThrow()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			IIndexerGetterSetupCallbackWhenBuilder<string, int> setup =
				sut.Mock.Setup[It.IsAny<int>()];

			void ActFor()
			{
				setup.For(2);
			}

			void ActOnly()
			{
				setup.Only(2);
			}

			await That(ActFor).DoesNotThrow();
			await That(ActOnly).DoesNotThrow();
		}

		public sealed class With2Levels
		{
			[Test]
			public async Task Callback_WhenLengthDoesNotMatch_ShouldIgnore()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do(() => { callCount++; });

				_ = sut[1];
				_ = sut[2, 2];
				_ = sut[3, 3, 3];

				await That(callCount).IsEqualTo(1);
			}

			[Test]
			public async Task InParallel_ShouldInvokeParallelCallbacksAlways()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				int callCount3 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do(() => { callCount1++; })
					.OnGet.Do((p1, _) => { callCount2 += p1; }).InParallel()
					.OnGet.Do((p1, _) => { callCount3 += p1; });

				_ = sut[1, 2];
				_ = sut[2, 2];
				_ = sut[3, 2];
				_ = sut[4, 2];

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(10);
				await That(callCount3).IsEqualTo(6);
			}

			[Test]
			[Arguments(1, 1)]
			[Arguments(2, 3)]
			[Arguments(3, 6)]
			public async Task Only_ShouldInvokeCallbacksOnlyTheGivenNumberOfTimes(int times, int expectedValue)
			{
				int sum = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((p1, _) => { sum += p1; }).Only(times);

				_ = sut[1, 2];
				_ = sut[2, 2];
				_ = sut[3, 2];
				_ = sut[4, 2];

				await That(sum).IsEqualTo(expectedValue);
			}

			[Test]
			public async Task Only_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _) => { invocations.Add(i); })
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					_ = sut[i, 2 * i];
				}

				await That(invocations).IsEqualTo([0, 1, 2, 3,]);
			}

			[Test]
			public async Task Only_WithWhen_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _) => { invocations.Add(i); })
					.When(x => x > 2)
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					_ = sut[i, 2 * i];
				}

				await That(invocations).IsEqualTo([3, 4, 5, 6,]);
			}

			[Test]
			public async Task OnlyOnce_ShouldDeactivateCallbackAfterFirstExecution()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do(() => { callCount1++; })
					.OnGet.Do((p1, _) => { callCount2 += p1; }).OnlyOnce();

				_ = sut[1, 2];
				_ = sut[2, 2];
				_ = sut[3, 2];
				_ = sut[4, 2];

				await That(callCount1).IsEqualTo(3);
				await That(callCount2).IsEqualTo(2);
			}

			[Test]
			public async Task ShouldExecuteGetterCallbacks()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 4), It.Satisfies<int>(i => i < 4)]
					.OnGet.Do(() => { callCount++; });

				_ = sut[5, 1]; // no
				_ = sut[3, 2]; // yes
				_ = sut[2, 3]; // yes
				_ = sut[1, 4]; // no
				_ = sut[1, -4]; // yes
				_ = sut[8, 6]; // no

				await That(callCount).IsEqualTo(3);
			}

			[Test]
			public async Task ShouldExecuteGetterCallbacksWithValue()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 4), It.Satisfies<int>(i => i < 4)]
					.OnGet.Do((v1, v2) => { callCount += v1 * v2; });

				_ = sut[5, 1]; // no
				_ = sut[3, 2]; // yes (6)
				_ = sut[2, 3]; // yes (6)
				_ = sut[1, 4]; // no
				_ = sut[1, -4]; // yes (-4)
				_ = sut[8, 6]; // no

				await That(callCount).IsEqualTo(8);
			}

			[Test]
			public async Task ShouldIncludeIncrementingAccessCounter()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _, _) => { invocations.Add(i); });

				for (int i = 0; i < 10; i++)
				{
					_ = sut[10 * i, 20 * i];
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
					.OnGet.Do(() => { callCount1++; })
					.OnGet.Do((p1, _) => { callCount2 += p1; });

				_ = sut[1, 2];
				_ = sut[2, 2];
				_ = sut[3, 2];
				_ = sut[4, 2];

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(6);
			}

			[Test]
			public async Task When_ShouldOnlyExecuteCallbackWhenInvocationCountMatches()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _) => { invocations.Add(i); })
					.When(x => x is > 3 and < 9);

				for (int i = 0; i < 20; i++)
				{
					_ = sut[i, 2 * i];
				}

				await That(invocations).IsEqualTo([4, 5, 6, 7, 8,]);
			}

			[Test]
			public async Task WithoutCallback_IIndexerGetterSetupCallbackBuilder_ShouldNotThrow()
			{
				IIndexerService sut = IIndexerService.CreateMock();
				IIndexerGetterSetupCallbackBuilder<string, int, int> setup =
					sut.Mock.Setup[It.IsAny<int>(),
						It.IsAny<int>()];

				void ActWhen()
				{
					setup.When(_ => true);
				}

				void ActFor()
				{
					setup.For(2);
				}

				void ActInParallel()
				{
					setup.InParallel();
				}

				await That(ActWhen).DoesNotThrow();
				await That(ActFor).DoesNotThrow();
				await That(ActInParallel).DoesNotThrow();
			}

			[Test]
			public async Task WithoutCallback_IIndexerGetterSetupCallbackWhenBuilder_ShouldNotThrow()
			{
				IIndexerService sut = IIndexerService.CreateMock();
				IIndexerGetterSetupCallbackWhenBuilder<string, int, int> setup =
					sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()];

				void ActFor()
				{
					setup.For(2);
				}

				void ActOnly()
				{
					setup.Only(2);
				}

				await That(ActFor).DoesNotThrow();
				await That(ActOnly).DoesNotThrow();
			}
		}

		public sealed class With3Levels
		{
			[Test]
			public async Task Callback_WhenLengthDoesNotMatch_ShouldIgnore()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do(() => { callCount++; });

				_ = sut[1];
				_ = sut[2, 2];
				_ = sut[3, 3, 3];
				_ = sut[4, 4, 4, 4];

				await That(callCount).IsEqualTo(1);
			}

			[Test]
			public async Task InParallel_ShouldInvokeParallelCallbacksAlways()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				int callCount3 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do(() => { callCount1++; })
					.OnGet.Do((p1, _, _) => { callCount2 += p1; }).InParallel()
					.OnGet.Do((p1, _, _) => { callCount3 += p1; });

				_ = sut[1, 2, 3];
				_ = sut[2, 2, 3];
				_ = sut[3, 2, 3];
				_ = sut[4, 2, 3];

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(10);
				await That(callCount3).IsEqualTo(6);
			}

			[Test]
			[Arguments(1, 1)]
			[Arguments(2, 3)]
			[Arguments(3, 6)]
			public async Task Only_ShouldInvokeCallbacksOnlyTheGivenNumberOfTimes(int times, int expectedValue)
			{
				int sum = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((p1, _, _) => { sum += p1; }).Only(times);

				_ = sut[1, 2, 3];
				_ = sut[2, 2, 3];
				_ = sut[3, 2, 3];
				_ = sut[4, 2, 3];

				await That(sum).IsEqualTo(expectedValue);
			}

			[Test]
			public async Task Only_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _, _) => { invocations.Add(i); })
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					_ = sut[i, 2 * i, 3 * i];
				}

				await That(invocations).IsEqualTo([0, 1, 2, 3,]);
			}

			[Test]
			public async Task Only_WithWhen_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _, _) => { invocations.Add(i); })
					.When(x => x > 2)
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					_ = sut[i, 2 * i, 3 * i];
				}

				await That(invocations).IsEqualTo([3, 4, 5, 6,]);
			}

			[Test]
			public async Task OnlyOnce_ShouldDeactivateCallbackAfterFirstExecution()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do(() => { callCount1++; })
					.OnGet.Do((p1, _, _) => { callCount2 += p1; }).OnlyOnce();

				_ = sut[1, 2, 3];
				_ = sut[2, 2, 3];
				_ = sut[3, 2, 3];
				_ = sut[4, 2, 3];

				await That(callCount1).IsEqualTo(3);
				await That(callCount2).IsEqualTo(2);
			}

			[Test]
			public async Task ShouldExecuteGetterCallbacks()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 4), It.Satisfies<int>(i => i < 4),
						It.Satisfies<int>(i => i < 4)]
					.OnGet.Do(() => { callCount++; });

				_ = sut[1, 5, 1]; // no
				_ = sut[3, 1, 2]; // yes
				_ = sut[2, 2, 3]; // yes
				_ = sut[1, 1, 4]; // no
				_ = sut[1, 1, -4]; // yes
				_ = sut[6, 2, 1]; // no
				_ = sut[6, 7, 8]; // no

				await That(callCount).IsEqualTo(3);
			}

			[Test]
			public async Task ShouldExecuteGetterCallbacksWithValue()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 4), It.Satisfies<int>(i => i < 4),
						It.Satisfies<int>(i => i < 4)]
					.OnGet.Do((v1, v2, v3) => { callCount += v1 * v2 * v3; });

				_ = sut[1, 5, 1]; // no
				_ = sut[3, 1, 2]; // yes (6)
				_ = sut[2, 2, 3]; // yes (12)
				_ = sut[1, 1, 4]; // no
				_ = sut[1, 1, -4]; // yes (-4)
				_ = sut[6, 2, 1]; // no
				_ = sut[6, 7, 8]; // no

				await That(callCount).IsEqualTo(14);
			}

			[Test]
			public async Task ShouldIncludeIncrementingAccessCounter()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _, _, _) => { invocations.Add(i); });

				for (int i = 0; i < 10; i++)
				{
					_ = sut[10 * i, 20 * i, 30 * i];
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
					.OnGet.Do(() => { callCount1++; })
					.OnGet.Do((p1, _, _) => { callCount2 += p1; });

				_ = sut[1, 2, 3];
				_ = sut[2, 2, 3];
				_ = sut[3, 2, 3];
				_ = sut[4, 2, 3];

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(6);
			}

			[Test]
			public async Task When_ShouldOnlyExecuteCallbackWhenInvocationCountMatches()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _, _) => { invocations.Add(i); })
					.When(x => x is > 3 and < 9);

				for (int i = 0; i < 20; i++)
				{
					_ = sut[i, 2 * i, 3 * i];
				}

				await That(invocations).IsEqualTo([4, 5, 6, 7, 8,]);
			}

			[Test]
			public async Task WithoutCallback_IIndexerGetterSetupCallbackBuilder_ShouldNotThrow()
			{
				IIndexerService sut = IIndexerService.CreateMock();
				IIndexerGetterSetupCallbackBuilder<string, int, int, int> setup =
					sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()];

				void ActWhen()
				{
					setup.When(_ => true);
				}

				void ActFor()
				{
					setup.For(2);
				}

				void ActInParallel()
				{
					setup.InParallel();
				}

				await That(ActWhen).DoesNotThrow();
				await That(ActFor).DoesNotThrow();
				await That(ActInParallel).DoesNotThrow();
			}

			[Test]
			public async Task WithoutCallback_IIndexerGetterSetupCallbackWhenBuilder_ShouldNotThrow()
			{
				IIndexerService sut = IIndexerService.CreateMock();
				IIndexerGetterSetupCallbackWhenBuilder<string, int, int, int> setup =
					sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()];

				void ActFor()
				{
					setup.For(2);
				}

				void ActOnly()
				{
					setup.Only(2);
				}

				await That(ActFor).DoesNotThrow();
				await That(ActOnly).DoesNotThrow();
			}
		}

		public sealed class With4Levels
		{
			[Test]
			public async Task Callback_WhenLengthDoesNotMatch_ShouldIgnore()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do(() => { callCount++; });

				_ = sut[1];
				_ = sut[2, 2];
				_ = sut[3, 3, 3];
				_ = sut[4, 4, 4, 4];

				await That(callCount).IsEqualTo(1);
			}

			[Test]
			public async Task InParallel_ShouldInvokeParallelCallbacksAlways()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				int callCount3 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do(() => { callCount1++; })
					.OnGet.Do((p1, _, _, _) => { callCount2 += p1; }).InParallel()
					.OnGet.Do((p1, _, _, _) => { callCount3 += p1; });

				_ = sut[1, 2, 3, 4];
				_ = sut[2, 2, 3, 4];
				_ = sut[3, 2, 3, 4];
				_ = sut[4, 2, 3, 4];

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(10);
				await That(callCount3).IsEqualTo(6);
			}

			[Test]
			[Arguments(1, 1)]
			[Arguments(2, 3)]
			[Arguments(3, 6)]
			public async Task Only_ShouldInvokeCallbacksOnlyTheGivenNumberOfTimes(int times, int expectedValue)
			{
				int sum = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((p1, _, _, _) => { sum += p1; }).Only(times);

				_ = sut[1, 2, 3, 4];
				_ = sut[2, 2, 3, 4];
				_ = sut[3, 2, 3, 4];
				_ = sut[4, 2, 3, 4];

				await That(sum).IsEqualTo(expectedValue);
			}

			[Test]
			public async Task Only_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _, _, _) => { invocations.Add(i); })
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					_ = sut[i, 2 * i, 3 * i, 4 * i];
				}

				await That(invocations).IsEqualTo([0, 1, 2, 3,]);
			}

			[Test]
			public async Task Only_WithWhen_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _, _, _) => { invocations.Add(i); })
					.When(x => x > 2)
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					_ = sut[i, 2 * i, 3 * i, 4 * i];
				}

				await That(invocations).IsEqualTo([3, 4, 5, 6,]);
			}

			[Test]
			public async Task OnlyOnce_ShouldDeactivateCallbackAfterFirstExecution()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do(() => { callCount1++; })
					.OnGet.Do((p1, _, _, _) => { callCount2 += p1; }).OnlyOnce();

				_ = sut[1, 2, 3, 4];
				_ = sut[2, 2, 3, 4];
				_ = sut[3, 2, 3, 4];
				_ = sut[4, 2, 3, 4];

				await That(callCount1).IsEqualTo(3);
				await That(callCount2).IsEqualTo(2);
			}

			[Test]
			public async Task ShouldExecuteGetterCallbacks()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 5), It.Satisfies<int>(i => i < 5),
						It.Satisfies<int>(i => i < 5), It.Satisfies<int>(i => i < 5)]
					.OnGet.Do(() => { callCount++; });

				_ = sut[1, 1, 5, 1]; // no
				_ = sut[3, 1, 2, 4]; // yes
				_ = sut[4, 2, 2, 3]; // yes
				_ = sut[1, 1, 1, 5]; // no
				_ = sut[1, 5, 1, 1]; // no
				_ = sut[1, 1, 1, -4]; // yes
				_ = sut[6, 2, 1, 3]; // no
				_ = sut[6, 7, 8, 9]; // no

				await That(callCount).IsEqualTo(3);
			}

			[Test]
			public async Task ShouldExecuteGetterCallbacksWithValue()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 5), It.Satisfies<int>(i => i < 5),
						It.Satisfies<int>(i => i < 5), It.Satisfies<int>(i => i < 5)]
					.OnGet.Do((v1, v2, v3, v4) => { callCount += v1 * v2 * v3 * v4; });

				_ = sut[1, 5, 1, 3]; // no
				_ = sut[3, 1, 2, 4]; // yes (24)
				_ = sut[2, 2, 3, 1]; // yes (12)
				_ = sut[1, 1, 5, 3]; // no
				_ = sut[1, 1, -4, 2]; // yes (-8)
				_ = sut[6, 2, 1, 3]; // no
				_ = sut[6, 7, 8, 9]; // no

				await That(callCount).IsEqualTo(28);
			}

			[Test]
			public async Task ShouldIncludeIncrementingAccessCounter()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _, _, _, _) => { invocations.Add(i); });

				for (int i = 0; i < 10; i++)
				{
					_ = sut[10 * i, 20 * i, 30 * i, 40 * i];
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
					.OnGet.Do(() => { callCount1++; })
					.OnGet.Do((p1, _, _, _) => { callCount2 += p1; });

				_ = sut[1, 2, 3, 4];
				_ = sut[2, 2, 3, 4];
				_ = sut[3, 2, 3, 4];
				_ = sut[4, 2, 3, 4];

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(6);
			}

			[Test]
			public async Task When_ShouldOnlyExecuteCallbackWhenInvocationCountMatches()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _, _, _) => { invocations.Add(i); })
					.When(x => x is > 3 and < 9);

				for (int i = 0; i < 20; i++)
				{
					_ = sut[i, 2 * i, 3 * i, 4 * i];
				}

				await That(invocations).IsEqualTo([4, 5, 6, 7, 8,]);
			}

			[Test]
			public async Task WithoutCallback_IIndexerGetterSetupCallbackBuilder_ShouldNotThrow()
			{
				IIndexerService sut = IIndexerService.CreateMock();
				IIndexerGetterSetupCallbackBuilder<string, int, int, int, int> setup =
					sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()];

				void ActWhen()
				{
					setup.When(_ => true);
				}

				void ActFor()
				{
					setup.For(2);
				}

				void ActInParallel()
				{
					setup.InParallel();
				}

				await That(ActWhen).DoesNotThrow();
				await That(ActFor).DoesNotThrow();
				await That(ActInParallel).DoesNotThrow();
			}

			[Test]
			public async Task WithoutCallback_IIndexerGetterSetupCallbackWhenBuilder_ShouldNotThrow()
			{
				IIndexerService sut = IIndexerService.CreateMock();
				IIndexerGetterSetupCallbackWhenBuilder<string, int, int, int, int> setup =
					sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()];

				void ActFor()
				{
					setup.For(2);
				}

				void ActOnly()
				{
					setup.Only(2);
				}

				await That(ActFor).DoesNotThrow();
				await That(ActOnly).DoesNotThrow();
			}
		}

		public sealed class With5Levels
		{
			[Test]
			public async Task Callback_WhenLengthDoesNotMatch_ShouldIgnore()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>()]
					.OnGet.Do(() => { callCount++; });

				_ = sut[1];
				_ = sut[2, 2];
				_ = sut[3, 3, 3];
				_ = sut[4, 4, 4, 4];
				_ = sut[5, 5, 5, 5, 5];

				await That(callCount).IsEqualTo(1);
			}

			[Test]
			public async Task InParallel_ShouldInvokeParallelCallbacksAlways()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				int callCount3 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do(() => { callCount1++; })
					.OnGet.Do((p1, _, _, _, _) => { callCount2 += p1; }).InParallel()
					.OnGet.Do((p1, _, _, _, _) => { callCount3 += p1; });

				_ = sut[1, 2, 3, 4, 5];
				_ = sut[2, 2, 3, 4, 5];
				_ = sut[3, 2, 3, 4, 5];
				_ = sut[4, 2, 3, 4, 5];

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(10);
				await That(callCount3).IsEqualTo(6);
			}

			[Test]
			[Arguments(1, 1)]
			[Arguments(2, 3)]
			[Arguments(3, 6)]
			public async Task Only_ShouldInvokeCallbacksOnlyTheGivenNumberOfTimes(int times, int expectedValue)
			{
				int sum = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((p1, _, _, _, _) => { sum += p1; }).Only(times);

				_ = sut[1, 2, 3, 4, 5];
				_ = sut[2, 2, 3, 4, 5];
				_ = sut[3, 2, 3, 4, 5];
				_ = sut[4, 2, 3, 4, 5];

				await That(sum).IsEqualTo(expectedValue);
			}

			[Test]
			public async Task Only_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _, _, _, _) => { invocations.Add(i); })
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					_ = sut[i, 2 * i, 3 * i, 4 * i, 5 * i];
				}

				await That(invocations).IsEqualTo([0, 1, 2, 3,]);
			}

			[Test]
			public async Task Only_WithWhen_ShouldStopExecutingCallbackAfterTheGivenTimes()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _, _, _, _) => { invocations.Add(i); })
					.When(x => x > 2)
					.Only(4);

				for (int i = 0; i < 20; i++)
				{
					_ = sut[i, 2 * i, 3 * i, 4 * i, 5 * i];
				}

				await That(invocations).IsEqualTo([3, 4, 5, 6,]);
			}

			[Test]
			public async Task OnlyOnce_ShouldDeactivateCallbackAfterFirstExecution()
			{
				int callCount1 = 0;
				int callCount2 = 0;
				IIndexerService sut = IIndexerService.CreateMock();

				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do(() => { callCount1++; })
					.OnGet.Do((p1, _, _, _, _) => { callCount2 += p1; }).OnlyOnce();

				_ = sut[1, 2, 3, 4, 5];
				_ = sut[2, 2, 3, 4, 5];
				_ = sut[3, 2, 3, 4, 5];
				_ = sut[4, 2, 3, 4, 5];

				await That(callCount1).IsEqualTo(3);
				await That(callCount2).IsEqualTo(2);
			}

			[Test]
			public async Task ShouldExecuteGetterCallbacks()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 6), It.Satisfies<int>(i => i < 6),
						It.Satisfies<int>(i => i < 6), It.Satisfies<int>(i => i < 6), It.Satisfies<int>(i => i < 6)]
					.OnGet.Do(() => { callCount++; });

				_ = sut[1, 1, 1, 6, 1]; // no
				_ = sut[1, 3, 1, 2, 4]; // yes
				_ = sut[2, 4, 2, 1, 3]; // yes
				_ = sut[1, 1, 1, 1, 6]; // no
				_ = sut[1, 1, 6, 1, 1]; // no
				_ = sut[1, 1, 1, 1, -4]; // yes
				_ = sut[1, 6, 2, 1, 3]; // no
				_ = sut[6, 7, 8, 9, 10]; // no

				await That(callCount).IsEqualTo(3);
			}

			[Test]
			public async Task ShouldExecuteGetterCallbacksWithValue()
			{
				int callCount = 0;
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.Satisfies<int>(i => i < 6), It.Satisfies<int>(i => i < 6),
						It.Satisfies<int>(i => i < 6), It.Satisfies<int>(i => i < 6), It.Satisfies<int>(i => i < 6)]
					.OnGet.Do((v1, v2, v3, v4, v5) => { callCount += v1 * v2 * v3 * v4 * v5; });

				_ = sut[1, 1, 1, 7, 1]; // no
				_ = sut[1, 3, 1, 2, 4]; // yes (24)
				_ = sut[2, 4, 2, 1, 3]; // yes (48)
				_ = sut[1, 1, 1, 1, 7]; // no
				_ = sut[1, 1, 7, 1, 1]; // no
				_ = sut[1, 3, 3, 1, -4]; // yes (-36)
				_ = sut[1, 7, 2, 1, 3]; // no
				_ = sut[6, 7, 8, 9, 10]; // no

				await That(callCount).IsEqualTo(36);
			}

			[Test]
			public async Task ShouldIncludeIncrementingAccessCounter()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((i, _, _, _, _, _, _) => { invocations.Add(i); });

				for (int i = 0; i < 10; i++)
				{
					_ = sut[10 * i, 20 * i, 30 * i, 40 * i, 50 * i];
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
					.OnGet.Do(() => { callCount1++; })
					.OnGet.Do((p1, _, _, _, _) => { callCount2 += p1; });

				_ = sut[1, 2, 3, 4, 5];
				_ = sut[2, 2, 3, 4, 5];
				_ = sut[3, 2, 3, 4, 5];
				_ = sut[4, 2, 3, 4, 5];

				await That(callCount1).IsEqualTo(2);
				await That(callCount2).IsEqualTo(6);
			}

			[Test]
			public async Task When_ShouldOnlyExecuteCallbackWhenInvocationCountMatches()
			{
				List<int> invocations = [];
				IIndexerService sut = IIndexerService.CreateMock();
				sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
					.OnGet.Do((p1, _, _, _, _, _) => { invocations.Add(p1); })
					.When(x => x is > 3 and < 9);

				for (int i = 0; i < 20; i++)
				{
					_ = sut[i, 2 * i, 3 * i, 4 * i, 5 * i];
				}

				await That(invocations).IsEqualTo([4, 5, 6, 7, 8,]);
			}

			[Test]
			public async Task WithoutCallback_IIndexerGetterSetupCallbackBuilder_ShouldNotThrow()
			{
				IIndexerService sut = IIndexerService.CreateMock();
				IIndexerGetterSetupCallbackBuilder<string, int, int, int, int, int> setup =
					sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()];

				void ActWhen()
				{
					setup.When(_ => true);
				}

				void ActFor()
				{
					setup.For(2);
				}

				void ActInParallel()
				{
					setup.InParallel();
				}

				await That(ActWhen).DoesNotThrow();
				await That(ActFor).DoesNotThrow();
				await That(ActInParallel).DoesNotThrow();
			}

			[Test]
			public async Task WithoutCallback_IIndexerGetterSetupCallbackWhenBuilder_ShouldNotThrow()
			{
				IIndexerService sut = IIndexerService.CreateMock();
				IIndexerGetterSetupCallbackWhenBuilder<string, int, int, int, int, int> setup =
					sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()];

				void ActFor()
				{
					setup.For(2);
				}

				void ActOnly()
				{
					setup.Only(2);
				}

				await That(ActFor).DoesNotThrow();
				await That(ActOnly).DoesNotThrow();
			}
		}
	}
}
