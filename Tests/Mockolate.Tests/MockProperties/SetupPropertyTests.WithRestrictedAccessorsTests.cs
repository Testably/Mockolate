using System.Collections.Generic;
using Mockolate.Setup;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests.MockProperties;

public sealed partial class SetupPropertyTests
{
	public sealed class WithRestrictedAccessorsTests
	{
		[Test]
		public async Task OnSet_WithSetOnlyProperty_ShouldFireAndRecordTheWrite()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			List<string> written = new();
			sut.Mock.Setup.WriteOnly.OnSet.Do(value => written.Add(value));

			sut.WriteOnly = "Ada";

			await That(written).IsEqualTo(["Ada",]);
			await That(sut.Mock.Verify.WriteOnly.Set("Ada")).Once()
				.Because("the setter facade must be keyed on the setter member id, not the getter's");
		}

		[Test]
		public async Task Set_WithSetOnlyProperty_WhenTheValueDiffers_ShouldNotMatch()
		{
			IAccessorService sut = IAccessorService.CreateMock();

			sut.WriteOnly = "Ada";

			await That(sut.Mock.Verify.WriteOnly.Set("Grace")).Never();
		}

		[Test]
		public async Task Set_WithSetOnlyPropertyAndMatcher_ShouldMatch()
		{
			IAccessorService sut = IAccessorService.CreateMock();

			sut.WriteOnly = "Ada";

			await That(sut.Mock.Verify.WriteOnly.Set(It.IsAny<string>())).Once();
		}

		[Test]
		public async Task Register_WithSetOnlyProperty_ShouldAllowWriteWithoutSetup()
		{
			IAccessorService sut = IAccessorService.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
			sut.Mock.Setup.WriteOnly.Register();

			void Act() => sut.WriteOnly = "Ada";

			await That(Act).DoesNotThrow();
		}

		[Test]
		public async Task Register_WithGetOnlyProperty_ShouldAllowReadWithoutSetup()
		{
			IAccessorService sut = IAccessorService.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
			sut.Mock.Setup.ReadOnly.Register();

			int result = sut.ReadOnly;

			await That(result).IsEqualTo(0);
			await That(sut.Mock.Verify.ReadOnly.Got()).Once();
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task SkippingBaseClass_WithSetOnlyClassProperty_ShouldCallTheBaseClassUnlessSkipped(bool skipBaseClass,
			int expectedCallCount)
		{
			AccessorService sut = AccessorService.CreateMock();
			sut.Mock.Setup.WriteOnly.SkippingBaseClass(skipBaseClass);

			sut.WriteOnly = 1;

			await That(sut.WriteOnlySetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		public async Task Returns_WithGetOnlyProperty_WhenChained_ShouldStayOnGetterOnlySurface()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			IPropertyGetterOnlySetup<int> chained = sut.Mock.Setup.ReadOnly
				.Returns(1).OnlyOnce();
			chained.Returns(() => 2);

			await That(sut.ReadOnly).IsEqualTo(1);
			await That(sut.ReadOnly).IsEqualTo(2);
		}

		[Test]
		public async Task Forever_WithGetOnlyProperty_ShouldUseTheLastValueForever()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup.ReadOnly
				.Returns(2)
				.Returns(4).Forever();

			int[] result = new int[4];
			for (int i = 0; i < 4; i++)
			{
				result[i] = sut.ReadOnly;
			}

			await That(result).IsEqualTo([2, 4, 4, 4,]);
		}

		[Test]
		public async Task When_WithGetOnlyProperty_ShouldOnlyUseValueWhenPredicateIsTrue()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup.ReadOnly
				.Returns(() => 4).When(i => i > 0);

			int result1 = sut.ReadOnly;
			int result2 = sut.ReadOnly;

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(4);
		}

		[Test]
		public async Task Returns_WithGetOnlyProperty_ShouldPassTheCurrentValueToTheCallback()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup.ReadOnly
				.InitializeWith(3)
				.Returns(x => 4 * x);

			int result = sut.ReadOnly;

			await That(result).IsEqualTo(12);
		}

		[Test]
		public async Task Throws_WithGetOnlyProperty_ShouldIterateThroughAllRegisteredExceptions()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup.ReadOnly
				.Throws<InvalidOperationException>()
				.Throws(new Exception("foo"))
				.Throws(() => new Exception("bar"))
				.Throws(v => new Exception($"baz-{v}"));

			void Act() => _ = sut.ReadOnly;

			await That(Act).Throws<InvalidOperationException>();
			Exception? result2 = Catch.Exception(Act);
			Exception? result3 = Catch.Exception(Act);
			Exception? result4 = Catch.Exception(Act);
			await That(result2).HasMessage("foo");
			await That(result3).HasMessage("bar");
			await That(result4).HasMessage("baz-0");
		}

		[Test]
		public async Task OnGet_WithGetOnlyProperty_WhenChained_ShouldStayOnGetterOnlySurface()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			IPropertyGetterOnlySetup<int> chained = sut.Mock.Setup.ReadOnly
				.OnGet.Do(() => { callCount1++; })
				.OnGet.Do(v => { callCount2 += 1 + v; }).OnlyOnce();
			chained.Register();

			_ = sut.ReadOnly;
			_ = sut.ReadOnly;
			_ = sut.ReadOnly;
			_ = sut.ReadOnly;

			await That(callCount1).IsEqualTo(3);
			await That(callCount2).IsEqualTo(1);
		}

		[Test]
		public async Task When_WithGetOnlyProperty_ShouldOnlyInvokeCallbackWhenPredicateIsTrue()
		{
			int callCount = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup.ReadOnly
				.OnGet.Do(() => { callCount++; }).When(i => i > 0);

			_ = sut.ReadOnly;
			_ = sut.ReadOnly;
			_ = sut.ReadOnly;

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		public async Task InParallel_WithGetOnlyProperty_ShouldInvokeParallelCallbacksAlways()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			int callCount3 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup.ReadOnly
				.OnGet.Do(() => { callCount1++; })
				.OnGet.Do(v => { callCount2++; }).InParallel()
				.OnGet.Do((i, v) => { callCount3++; });

			_ = sut.ReadOnly;
			_ = sut.ReadOnly;
			_ = sut.ReadOnly;
			_ = sut.ReadOnly;

			await That(callCount1).IsEqualTo(2);
			await That(callCount2).IsEqualTo(4);
			await That(callCount3).IsEqualTo(2);
		}

		[Test]
		public async Task For_WithGetOnlyProperty_ShouldRepeatCallbackTheGivenNumberOfTimes()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup.ReadOnly
				.OnGet.Do(() => { callCount1++; }).For(2)
				.OnGet.Do(() => { callCount2++; });

			for (int i = 0; i < 6; i++)
			{
				_ = sut.ReadOnly;
			}

			await That(callCount1).IsEqualTo(4);
			await That(callCount2).IsEqualTo(2);
		}

		[Test]
		public async Task TransitionTo_WithGetOnlyProperty_ShouldSwitchScenario()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.InScenario("a").Setup.ReadOnly
				.OnGet.Do(() => { })
				.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut.ReadOnly;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task OnSet_WithSetOnlyProperty_WhenChained_ShouldStayOnSetterOnlySurface()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			IPropertySetterOnlySetup<string> chained = sut.Mock.Setup.WriteOnly
				.OnSet.Do(() => { callCount1++; })
				.OnSet.Do(v => { callCount2++; }).OnlyOnce();
			chained.Register();

			sut.WriteOnly = "a";
			sut.WriteOnly = "b";
			sut.WriteOnly = "c";
			sut.WriteOnly = "d";

			await That(callCount1).IsEqualTo(3);
			await That(callCount2).IsEqualTo(1);
		}

		[Test]
		public async Task When_WithSetOnlyProperty_ShouldOnlyInvokeCallbackWhenPredicateIsTrue()
		{
			int callCount = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup.WriteOnly
				.OnSet.Do(() => { callCount++; }).When(i => i > 0);

			sut.WriteOnly = "a";
			sut.WriteOnly = "b";
			sut.WriteOnly = "c";

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		public async Task InParallel_WithSetOnlyProperty_ShouldInvokeParallelCallbacksAlways()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			int callCount3 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup.WriteOnly
				.OnSet.Do(() => { callCount1++; })
				.OnSet.Do((i, v) => { callCount2++; }).InParallel()
				.OnSet.Do(() => { callCount3++; });

			sut.WriteOnly = "a";
			sut.WriteOnly = "b";
			sut.WriteOnly = "c";
			sut.WriteOnly = "d";

			await That(callCount1).IsEqualTo(2);
			await That(callCount2).IsEqualTo(4);
			await That(callCount3).IsEqualTo(2);
		}

		[Test]
		public async Task For_WithSetOnlyProperty_ShouldRepeatCallbackTheGivenNumberOfTimes()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup.WriteOnly
				.OnSet.Do(() => { callCount1++; }).For(2)
				.OnSet.Do(() => { callCount2++; });

			for (int i = 0; i < 6; i++)
			{
				sut.WriteOnly = "a";
			}

			await That(callCount1).IsEqualTo(4);
			await That(callCount2).IsEqualTo(2);
		}

		[Test]
		public async Task TransitionTo_WithSetOnlyProperty_ShouldSwitchScenario()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.InScenario("a").Setup.WriteOnly
				.OnSet.Do(() => { })
				.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.WriteOnly = "x";

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		public interface IAccessorService
		{
			int ReadOnly { get; }
			string WriteOnly { set; }
		}

		public class AccessorService
		{
			public int WriteOnlySetterCallCount { get; private set; }

			public virtual int WriteOnly
			{
				set => WriteOnlySetterCallCount += value;
			}
		}
	}
}
