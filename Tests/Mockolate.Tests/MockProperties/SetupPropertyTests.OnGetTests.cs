using System.Collections.Generic;
using Mockolate.Setup;

namespace Mockolate.Tests.MockProperties;

public sealed partial class SetupPropertyTests
{
	public sealed class OnGetTests
	{
		[Test]
		[Arguments(-2)]
		[Arguments(0)]
		public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
		{
			IPropertyService sut = IPropertyService.CreateMock();

			void Act()
			{
				sut.Mock.Setup.MyProperty
					.OnGet.Do(() => { }).For(times);
			}

			await That(Act).Throws<ArgumentOutOfRangeException>()
				.WithMessage("Times must be greater than zero.").AsPrefix();
		}

		[Test]
		public async Task MultipleOnGet_InParallel_ShouldInvokeParallelCallbacksAlways()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			int callCount3 = 0;
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.OnGet.Do(() => { callCount1++; })
				.OnGet.Do(v => { callCount2 += v; }).InParallel()
				.OnGet.Do(v => { callCount3 += v; });

			sut.MyProperty = 1;
			_ = sut.MyProperty;
			sut.MyProperty = 2;
			_ = sut.MyProperty;
			sut.MyProperty = 3;
			_ = sut.MyProperty;
			sut.MyProperty = 4;
			_ = sut.MyProperty;

			await That(callCount1).IsEqualTo(2);
			await That(callCount2).IsEqualTo(10);
			await That(callCount3).IsEqualTo(6);
		}

		[Test]
		[Arguments(1, 1)]
		[Arguments(2, 3)]
		[Arguments(3, 6)]
		public async Task MultipleOnGet_Only_ShouldInvokeCallbacksOnlyTheGivenNumberOfTimes(int times,
			int expectedValue)
		{
			int sum = 0;
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.OnGet.Do(v => { sum += v; }).Only(times);

			sut.MyProperty = 1;
			_ = sut.MyProperty;
			sut.MyProperty = 2;
			_ = sut.MyProperty;
			sut.MyProperty = 3;
			_ = sut.MyProperty;
			sut.MyProperty = 4;
			_ = sut.MyProperty;
			sut.MyProperty = 5;
			_ = sut.MyProperty;

			await That(sum).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task MultipleOnGet_OnlyOnce_ShouldDeactivateCallbackAfterFirstExecution()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.OnGet.Do(() => { callCount1++; })
				.OnGet.Do(v => { callCount2 += v; }).OnlyOnce();

			sut.MyProperty = 1;
			_ = sut.MyProperty;
			sut.MyProperty = 2;
			_ = sut.MyProperty;
			sut.MyProperty = 3;
			_ = sut.MyProperty;
			sut.MyProperty = 4;
			_ = sut.MyProperty;

			await That(callCount1).IsEqualTo(3);
			await That(callCount2).IsEqualTo(2);
		}

		[Test]
		public async Task MultipleOnGet_ShouldInvokeCallbacksInSequence()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.OnGet.Do(() => { callCount1++; })
				.OnGet.Do(v => { callCount2 += v; });

			sut.MyProperty = 1;
			_ = sut.MyProperty;
			sut.MyProperty = 2;
			_ = sut.MyProperty;
			sut.MyProperty = 3;
			_ = sut.MyProperty;
			sut.MyProperty = 4;
			_ = sut.MyProperty;

			await That(callCount1).IsEqualTo(2);
			await That(callCount2).IsEqualTo(6);
		}

		[Test]
		[Arguments(-2)]
		[Arguments(0)]
		public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
		{
			IPropertyService sut = IPropertyService.CreateMock();

			void Act()
			{
				sut.Mock.Setup.MyProperty
					.OnGet.Do(() => { }).Only(times);
			}

			await That(Act).Throws<ArgumentOutOfRangeException>()
				.WithMessage("Times must be greater than zero.").AsPrefix();
		}

		[Test]
		public async Task Only_ShouldStopExecutingCallbackAfterTheGivenTimes()
		{
			List<int> invocations = [];
			IPropertyService sut = IPropertyService.CreateMock();
			sut.Mock.Setup.MyProperty
				.OnGet.Do((i, _) => { invocations.Add(i); }).Only(4);

			for (int i = 0; i < 20; i++)
			{
				_ = sut.MyProperty;
			}

			await That(invocations).IsEqualTo([0, 1, 2, 3,]);
		}

		[Test]
		public async Task Only_WithWhen_ShouldStopExecutingCallbackAfterTheGivenTimes()
		{
			List<int> invocations = [];
			IPropertyService sut = IPropertyService.CreateMock();
			sut.Mock.Setup.MyProperty
				.OnGet.Do((i, _) => { invocations.Add(i); })
				.When(x => x > 2)
				.Only(4);

			for (int i = 0; i < 20; i++)
			{
				_ = sut.MyProperty;
			}

			await That(invocations).IsEqualTo([3, 4, 5, 6,]);
		}

		[Test]
		public async Task ShouldExecuteWhenPropertyIsRead()
		{
			int callCount = 0;
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.OnGet.Do(() => { callCount++; });

			_ = sut.MyProperty;

			await That(callCount).IsEqualTo(1);
		}

		[Test]
		public async Task ShouldIncludeIncrementingAccessCounter()
		{
			List<int> invocations = [];
			IPropertyService sut = IPropertyService.CreateMock();
			sut.Mock.Setup.MyProperty
				.OnGet.Do((i, _) => { invocations.Add(i); });

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyProperty;
			}

			await That(invocations).IsEqualTo([0, 1, 2, 3, 4, 5, 6, 7, 8, 9,]);
		}

		[Test]
		public async Task ShouldNotExecuteWhenPropertyIsWrittenToOrOtherPropertyIsRead()
		{
			int callCount = 0;
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.OnGet.Do(() => { callCount++; });

			_ = sut.MyOtherProperty;
			sut.MyProperty = 1;

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task When_ShouldOnlyExecuteCallbackWhenInvocationCountMatches()
		{
			List<int> invocations = [];
			IPropertyService sut = IPropertyService.CreateMock();
			sut.Mock.Setup.MyProperty
				.OnGet.Do(p => { invocations.Add(p); })
				.When(x => x is > 3 and < 9);

			for (int i = 0; i < 20; i++)
			{
				sut.MyProperty = 10 * i;
				_ = sut.MyProperty;
			}

			await That(invocations).IsEqualTo([40, 50, 60, 70, 80,]);
		}

		[Test]
		public async Task WithoutCallback_IPropertyGetterSetupCallbackBuilder_ShouldNotThrow()
		{
			IPropertyService sut = IPropertyService.CreateMock();
			IPropertyGetterSetupCallbackBuilder<int> setup =
				sut.Mock.Setup.MyProperty;

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
		public async Task WithoutCallback_IPropertyGetterSetupCallbackWhenBuilder_ShouldNotThrow()
		{
			IPropertyService sut = IPropertyService.CreateMock();
			IPropertyGetterSetupCallbackWhenBuilder<int> setup =
				sut.Mock.Setup.MyProperty;

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

		[Test]
		public async Task WithValue_ShouldExecuteWhenPropertyIsRead()
		{
			int callCount = 0;
			int receivedValue = 0;
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.InitializeWith(4)
				.OnGet.Do(v =>
				{
					callCount++;
					receivedValue = v;
				});

			_ = sut.MyProperty;

			await That(callCount).IsEqualTo(1);
			await That(receivedValue).IsEqualTo(4);
		}

		[Test]
		public async Task WithValue_ShouldNotExecuteWhenPropertyIsWrittenToOrOtherPropertyIsRead()
		{
			int callCount = 0;
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.OnGet.Do(_ => { callCount++; });

			_ = sut.MyOtherProperty;
			sut.MyProperty = 1;

			await That(callCount).IsEqualTo(0);
		}
	}
}
