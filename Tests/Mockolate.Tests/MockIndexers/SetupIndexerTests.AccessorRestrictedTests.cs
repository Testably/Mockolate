using System.Collections.Generic;
using Mockolate.Setup;
using Mockolate.Tests.TestHelpers;
using Mockolate.Verify;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class SetupIndexerTests
{
	public sealed class AccessorRestrictedTests
	{
		[Test]
		public async Task SetOnlyIndexer_OnSet_ShouldFireAndRecordTheWrite()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			List<string> written = new();
			sut.Mock.Setup[It.IsAny<string>()].OnSet.Do(value => written.Add(value));

			sut["name"] = "Ada";

			await That(written).IsEqualTo(["Ada",]);
			await That(sut.Mock.Verify[It.Is("name")].Set("Ada")).Once()
				.Because("the setter facade must be keyed on the setter member id, not the getter's");
		}

		[Test]
		public async Task SetOnlyIndexer_VerifyOtherValue_ShouldNotMatch()
		{
			IAccessorService sut = IAccessorService.CreateMock();

			sut["name"] = "Ada";

			await That(sut.Mock.Verify[It.Is("name")].Set("Grace")).Never();
		}

		[Test]
		public async Task SetOnlyIndexer_VerifyWithParameterMatcher_ShouldMatch()
		{
			IAccessorService sut = IAccessorService.CreateMock();

			sut["name"] = "Ada";

			await That(sut.Mock.Verify[It.IsAny<string>()].Set(It.IsAny<string>())).Once();
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task SetOnlyClassIndexer_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			AccessorService sut = AccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].SkippingBaseClass(skipBaseClass);

			sut[4] = 1;

			await That(sut.SetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		public async Task GetOnlyIndexer_ChainedReturns_ShouldStayOnGetterOnlySurface()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			IIndexerGetterOnlySetup<int, int> chained = sut.Mock.Setup[It.IsAny<int>()]
				.Returns(1).OnlyOnce();
			chained.Returns(() => 2);

			await That(sut[1]).IsEqualTo(1);
			await That(sut[1]).IsEqualTo(2);
		}

		[Test]
		public async Task GetOnlyIndexer_ReturnsForever_ShouldUseTheLastValueForever()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.Returns(2)
				.Returns(4).Forever();

			int[] result = new int[4];
			for (int i = 0; i < 4; i++)
			{
				result[i] = sut[i];
			}

			await That(result).IsEqualTo([2, 4, 4, 4,]);
		}

		[Test]
		public async Task GetOnlyIndexer_ReturnsWhen_ShouldOnlyUseValueWhenPredicateIsTrue()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.Returns(() => 4).When(i => i > 0);

			int result1 = sut[1];
			int result2 = sut[1];

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(4);
		}

		[Test]
		public async Task GetOnlyIndexer_ReturnsCallbackWithValue_ShouldReturnExpectedValue()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.InitializeWith(3)
				.Returns((_, v) => 4 * v);

			int result = sut[7];

			await That(result).IsEqualTo(12);
		}

		[Test]
		public async Task GetOnlyIndexer_InitializeWithGenerator_ShouldSeedTheReadValue()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.InitializeWith(key => 10 * key);

			await That(sut[3]).IsEqualTo(30);
			await That(sut[5]).IsEqualTo(50);
		}

		[Test]
		public async Task GetOnlyIndexer_Throws_ShouldIterateThroughAllRegisteredExceptions()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.Throws<InvalidOperationException>()
				.Throws(new Exception("foo"))
				.Throws(() => new Exception("bar"))
				.Throws(k => new Exception($"baz-{k}"))
				.Throws((k, v) => new Exception($"qux-{k}-{v}"));

			void Act() => _ = sut[6];

			await That(Act).Throws<InvalidOperationException>();
			Exception? result2 = Catch.Exception(Act);
			Exception? result3 = Catch.Exception(Act);
			Exception? result4 = Catch.Exception(Act);
			Exception? result5 = Catch.Exception(Act);
			await That(result2).HasMessage("foo");
			await That(result3).HasMessage("bar");
			await That(result4).HasMessage("baz-6");
			await That(result5).HasMessage("qux-6-0");
		}

		[Test]
		public async Task GetOnlyIndexer_OnGetChain_ShouldStayOnGetterOnlySurface()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			IIndexerGetterOnlySetup<int, int> chained = sut.Mock.Setup[It.IsAny<int>()]
				.OnGet.Do(() => { callCount1++; })
				.OnGet.Do(k => { callCount2++; }).OnlyOnce();
			chained.Returns(9);

			int result = sut[1];
			_ = sut[2];
			_ = sut[3];
			_ = sut[4];

			await That(result).IsEqualTo(9);
			await That(callCount1).IsEqualTo(3);
			await That(callCount2).IsEqualTo(1);
		}

		[Test]
		public async Task GetOnlyIndexer_OnGetWhen_ShouldOnlyInvokeCallbackWhenPredicateIsTrue()
		{
			int callCount = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.OnGet.Do(() => { callCount++; }).When(i => i > 0);

			_ = sut[1];
			_ = sut[1];
			_ = sut[1];

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		public async Task GetOnlyIndexer_OnGetInParallel_ShouldInvokeParallelCallbacksAlways()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			int callCount3 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.OnGet.Do(() => { callCount1++; })
				.OnGet.Do(k => { callCount2++; }).InParallel()
				.OnGet.Do((k, v) => { callCount3++; });

			_ = sut[1];
			_ = sut[2];
			_ = sut[3];
			_ = sut[4];

			await That(callCount1).IsEqualTo(2);
			await That(callCount2).IsEqualTo(4);
			await That(callCount3).IsEqualTo(2);
		}

		[Test]
		public async Task GetOnlyIndexer_OnGetFor_ShouldRepeatCallbackTheGivenNumberOfTimes()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()]
				.OnGet.Do(() => { callCount1++; }).For(2)
				.OnGet.Do(() => { callCount2++; });

			for (int i = 0; i < 6; i++)
			{
				_ = sut[1];
			}

			await That(callCount1).IsEqualTo(4);
			await That(callCount2).IsEqualTo(2);
		}

		[Test]
		public async Task GetOnlyIndexer_OnGetTransitionTo_ShouldSwitchScenario()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.InScenario("a").Setup[It.IsAny<int>()]
				.OnGet.Do(() => { })
				.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut[1];

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task SetOnlyIndexer_OnSetChain_ShouldStayOnSetterOnlySurface()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			IIndexerSetterOnlySetup<string, string> chained = sut.Mock.Setup[It.IsAny<string>()]
				.OnSet.Do(() => { callCount1++; })
				.OnSet.Do(v => { callCount2++; }).OnlyOnce();
			chained.SkippingBaseClass();

			sut["k"] = "a";
			sut["k"] = "b";
			sut["k"] = "c";
			sut["k"] = "d";

			await That(callCount1).IsEqualTo(3);
			await That(callCount2).IsEqualTo(1);
		}

		[Test]
		public async Task SetOnlyIndexer_OnSetWhen_ShouldOnlyInvokeCallbackWhenPredicateIsTrue()
		{
			int callCount = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<string>()]
				.OnSet.Do(() => { callCount++; }).When(i => i > 0);

			sut["k"] = "a";
			sut["k"] = "b";
			sut["k"] = "c";

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		public async Task SetOnlyIndexer_OnSetInParallel_ShouldInvokeParallelCallbacksAlways()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			int callCount3 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<string>()]
				.OnSet.Do(() => { callCount1++; })
				.OnSet.Do((k, v) => { callCount2++; }).InParallel()
				.OnSet.Do(() => { callCount3++; });

			sut["k"] = "a";
			sut["k"] = "b";
			sut["k"] = "c";
			sut["k"] = "d";

			await That(callCount1).IsEqualTo(2);
			await That(callCount2).IsEqualTo(4);
			await That(callCount3).IsEqualTo(2);
		}

		[Test]
		public async Task SetOnlyIndexer_OnSetFor_ShouldRepeatCallbackTheGivenNumberOfTimes()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<string>()]
				.OnSet.Do(() => { callCount1++; }).For(2)
				.OnSet.Do(() => { callCount2++; });

			for (int i = 0; i < 6; i++)
			{
				sut["k"] = "a";
			}

			await That(callCount1).IsEqualTo(4);
			await That(callCount2).IsEqualTo(2);
		}

		[Test]
		public async Task SetOnlyIndexer_OnSetTransitionTo_ShouldSwitchScenario()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.InScenario("a").Setup[It.IsAny<string>()]
				.OnSet.Do(() => { })
				.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut["k"] = "x";

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task GetOnlyIndexer_Got_ShouldMatchOnlyMatchingKeys()
		{
			IAccessorService sut = IAccessorService.CreateMock();

			_ = sut[1];

			await That(sut.Mock.Verify[It.Is(1)].Got()).Once();
			await That(sut.Mock.Verify[It.Is(2)].Got()).Never();
		}

		[Test]
		public async Task GetOnlyTwoKeyIndexer_ReturnsAndGot_ShouldUseTheNarrowedSurface()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()]
				.Returns((k1, k2) => k1 + k2);

			int result = sut[20, 22];

			await That(result).IsEqualTo(42);
			await That(sut.Mock.Verify[It.Is(20), It.Is(22)].Got()).Once();
			await That(sut.Mock.Verify[It.Is(20), It.Is(23)].Got()).Never();
		}

		[Test]
		public async Task SetOnlyTwoKeyIndexer_OnSetAndVerify_ShouldUseTheNarrowedSurface()
		{
			List<string> written = new();
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<string>(), It.IsAny<string>()]
				.OnSet.Do((k1, k2, v) => written.Add($"{k1}-{k2}-{v}"));

			sut["a", "b"] = "c";

			await That(written).IsEqualTo(["a-b-c",]);
			await That(sut.Mock.Verify[It.Is("a"), It.Is("b")].Set("c")).Once();
			await That(sut.Mock.Verify[It.Is("a"), It.Is("b")].Set("d")).Never();
		}

		[Test]
		public async Task SetOnlyFourKeyIndexer_Set_ShouldVerifyTheWrite()
		{
			IAccessorService sut = IAccessorService.CreateMock();

			sut[1, 2, 3, 4] = "x";

			await That(sut.Mock.Verify[It.Is(1), It.Is(2), It.Is(3), It.Is(4)].Set("x")).Once();
			await That(sut.Mock.Verify[It.Is(1), It.Is(2), It.Is(3), It.Is(5)].Set(It.IsAny<string>())).Never();
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_ChainedReturns_ShouldStayOnGetterOnlySurface()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			IIndexerGetterOnlySetup<int, int, int, int, int, int> chained = sut.Mock
				.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.Returns(1).OnlyOnce();
			chained.Returns((k1, k2, k3, k4, k5) => k1 + k2 + k3 + k4 + k5);

			await That(sut[1, 2, 3, 4, 5]).IsEqualTo(1);
			await That(sut[1, 2, 3, 4, 5]).IsEqualTo(15);
			await That(sut.Mock.Verify[It.Is(1), It.Is(2), It.Is(3), It.Is(4), It.Is(5)].Got()).Exactly(2)
				.Because("the narrowed surface is generated per-compilation for indexers with more than four keys");
			await That(sut.Mock.Verify[It.Is(1), It.Is(2), It.Is(3), It.Is(4), It.Is(6)].Got()).Never();
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_ReturnsForever_ShouldUseTheLastValueForever()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.Returns(2)
				.Returns(4).Forever();

			int[] result = new int[4];
			for (int i = 0; i < 4; i++)
			{
				result[i] = sut[i, i, i, i, i];
			}

			await That(result).IsEqualTo([2, 4, 4, 4,]);
		}

		[Test]
		public async Task SetOnlyFiveKeyIndexer_OnSetAndVerify_ShouldUseTheNarrowedSurface()
		{
			List<string> written = new();
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock
				.Setup[It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()]
				.OnSet.Do((k1, k2, k3, k4, k5, v) => written.Add($"{k1}{k2}{k3}{k4}{k5}-{v}"));

			sut["a", "b", "c", "d", "e"] = "x";

			await That(written).IsEqualTo(["abcde-x",]);
			await That(sut.Mock.Verify[It.Is("a"), It.Is("b"), It.Is("c"), It.Is("d"), It.Is("e")].Set("x")).Once();
			await That(sut.Mock.Verify[It.Is("a"), It.Is("b"), It.Is("c"), It.Is("d"), It.Is("e")].Set("y")).Never();
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_Throws_ShouldIterateThroughAllRegisteredExceptions()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.Throws<InvalidOperationException>()
				.Throws(new Exception("foo"))
				.Throws(() => new Exception("bar"))
				.Throws((k1, k2, k3, k4, k5) => new Exception($"baz-{k1 + k2 + k3 + k4 + k5}"))
				.Throws((k1, k2, k3, k4, k5, v) => new Exception($"qux-{k1 + k2 + k3 + k4 + k5}-{v}"));

			void Act() => _ = sut[1, 2, 3, 4, 5];

			await That(Act).Throws<InvalidOperationException>();
			Exception? result2 = Catch.Exception(Act);
			Exception? result3 = Catch.Exception(Act);
			Exception? result4 = Catch.Exception(Act);
			Exception? result5 = Catch.Exception(Act);
			await That(result2).HasMessage("foo");
			await That(result3).HasMessage("bar");
			await That(result4).HasMessage("baz-15");
			await That(result5).HasMessage("qux-15-0");
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_InitializeWith_ShouldSeedTheReadValue()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.InitializeWith(3)
				.Returns((_, _, _, _, _, v) => 4 * v);

			await That(sut[1, 2, 3, 4, 5]).IsEqualTo(12);
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_InitializeWithGenerator_ShouldSeedTheReadValue()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.InitializeWith((k1, k2, k3, k4, k5) => 10 * (k1 + k2 + k3 + k4 + k5));

			await That(sut[1, 2, 3, 4, 5]).IsEqualTo(150);
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_ReturnsWhen_ShouldOnlyUseValueWhenPredicateIsTrue()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.Returns(() => 4).When(i => i > 0);

			int result1 = sut[1, 2, 3, 4, 5];
			int result2 = sut[1, 2, 3, 4, 5];

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(4);
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_SkippingBaseClass_ShouldStayOnGetterOnlySurface()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			IIndexerGetterOnlySetup<int, int, int, int, int, int> chained = sut.Mock
				.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.SkippingBaseClass();
			chained.Returns(7);

			await That(sut[1, 2, 3, 4, 5]).IsEqualTo(7);
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_OnGetChain_ShouldStayOnGetterOnlySurface()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			IIndexerGetterOnlySetup<int, int, int, int, int, int> chained = sut.Mock
				.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.Do(() => { callCount1++; })
				.OnGet.Do((k1, k2, k3, k4, k5) => { callCount2++; }).OnlyOnce();
			chained.Returns(9);

			int result = sut[1, 2, 3, 4, 5];
			_ = sut[1, 2, 3, 4, 5];
			_ = sut[1, 2, 3, 4, 5];
			_ = sut[1, 2, 3, 4, 5];

			await That(result).IsEqualTo(9);
			await That(callCount1).IsEqualTo(3);
			await That(callCount2).IsEqualTo(1);
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_OnGetWhen_ShouldOnlyInvokeCallbackWhenPredicateIsTrue()
		{
			int callCount = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.Do(() => { callCount++; }).When(i => i > 0);

			_ = sut[1, 2, 3, 4, 5];
			_ = sut[1, 2, 3, 4, 5];
			_ = sut[1, 2, 3, 4, 5];

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_OnGetInParallel_ShouldInvokeParallelCallbacksAlways()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			int callCount3 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.Do(() => { callCount1++; })
				.OnGet.Do((k1, k2, k3, k4, k5) => { callCount2++; }).InParallel()
				.OnGet.Do((k1, k2, k3, k4, k5, v) => { callCount3++; });

			_ = sut[1, 2, 3, 4, 5];
			_ = sut[1, 2, 3, 4, 5];
			_ = sut[1, 2, 3, 4, 5];
			_ = sut[1, 2, 3, 4, 5];

			await That(callCount1).IsEqualTo(2);
			await That(callCount2).IsEqualTo(4);
			await That(callCount3).IsEqualTo(2);
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_OnGetFor_ShouldRepeatCallbackTheGivenNumberOfTimes()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.Do(() => { callCount1++; }).For(2)
				.OnGet.Do(() => { callCount2++; });

			for (int i = 0; i < 6; i++)
			{
				_ = sut[1, 2, 3, 4, 5];
			}

			await That(callCount1).IsEqualTo(4);
			await That(callCount2).IsEqualTo(2);
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_OnGetDoWithCounter_ShouldReceiveTheAccessCounter()
		{
			List<int> invocations = [];
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.Do((i, _, _, _, _, _, _) => { invocations.Add(i); });

			for (int i = 0; i < 3; i++)
			{
				_ = sut[1, 2, 3, 4, 5];
			}

			await That(invocations).IsEqualTo([0, 1, 2,]);
		}

		[Test]
		public async Task GetOnlyFiveKeyIndexer_OnGetTransitionTo_ShouldSwitchScenario()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.InScenario("a")
				.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.Do(() => { })
				.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut[1, 2, 3, 4, 5];

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task SetOnlyFiveKeyIndexer_OnSetChain_ShouldStayOnSetterOnlySurface()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			IIndexerSetterOnlySetup<string, string, string, string, string, string> chained = sut.Mock
				.Setup[It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
					It.IsAny<string>()]
				.OnSet.Do(() => { callCount1++; })
				.OnSet.Do(v => { callCount2++; }).OnlyOnce();
			chained.SkippingBaseClass();

			sut["a", "b", "c", "d", "e"] = "1";
			sut["a", "b", "c", "d", "e"] = "2";
			sut["a", "b", "c", "d", "e"] = "3";
			sut["a", "b", "c", "d", "e"] = "4";

			await That(callCount1).IsEqualTo(3);
			await That(callCount2).IsEqualTo(1);
		}

		[Test]
		public async Task SetOnlyFiveKeyIndexer_OnSetWhen_ShouldOnlyInvokeCallbackWhenPredicateIsTrue()
		{
			int callCount = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock
				.Setup[It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
					It.IsAny<string>()]
				.OnSet.Do(() => { callCount++; }).When(i => i > 0);

			sut["a", "b", "c", "d", "e"] = "1";
			sut["a", "b", "c", "d", "e"] = "2";
			sut["a", "b", "c", "d", "e"] = "3";

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		public async Task SetOnlyFiveKeyIndexer_OnSetInParallel_ShouldInvokeParallelCallbacksAlways()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			int callCount3 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock
				.Setup[It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
					It.IsAny<string>()]
				.OnSet.Do(() => { callCount1++; })
				.OnSet.Do((k1, k2, k3, k4, k5, v) => { callCount2++; }).InParallel()
				.OnSet.Do(() => { callCount3++; });

			sut["a", "b", "c", "d", "e"] = "1";
			sut["a", "b", "c", "d", "e"] = "2";
			sut["a", "b", "c", "d", "e"] = "3";
			sut["a", "b", "c", "d", "e"] = "4";

			await That(callCount1).IsEqualTo(2);
			await That(callCount2).IsEqualTo(4);
			await That(callCount3).IsEqualTo(2);
		}

		[Test]
		public async Task SetOnlyFiveKeyIndexer_OnSetFor_ShouldRepeatCallbackTheGivenNumberOfTimes()
		{
			int callCount1 = 0;
			int callCount2 = 0;
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock
				.Setup[It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
					It.IsAny<string>()]
				.OnSet.Do(() => { callCount1++; }).For(2)
				.OnSet.Do(() => { callCount2++; });

			for (int i = 0; i < 6; i++)
			{
				sut["a", "b", "c", "d", "e"] = "1";
			}

			await That(callCount1).IsEqualTo(4);
			await That(callCount2).IsEqualTo(2);
		}

		[Test]
		public async Task SetOnlyFiveKeyIndexer_OnSetDoWithCounter_ShouldReceiveTheAccessCounter()
		{
			List<int> invocations = [];
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock
				.Setup[It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
					It.IsAny<string>()]
				.OnSet.Do((i, _, _, _, _, _, _) => { invocations.Add(i); });

			for (int i = 0; i < 3; i++)
			{
				sut["a", "b", "c", "d", "e"] = "1";
			}

			await That(invocations).IsEqualTo([0, 1, 2,]);
		}

		[Test]
		public async Task SetOnlyFiveKeyIndexer_OnSetTransitionTo_ShouldSwitchScenario()
		{
			IAccessorService sut = IAccessorService.CreateMock();
			sut.Mock.InScenario("a")
				.Setup[It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
					It.IsAny<string>()]
				.OnSet.Do(() => { })
				.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut["a", "b", "c", "d", "e"] = "x";

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task SetOnlyFiveKeyIndexer_SetExpectation_ShouldFormatTheValueLikeTheShippedSurface()
		{
			IAccessorService sut = IAccessorService.CreateMock();

			IVerificationResult result = (IVerificationResult)sut.Mock
				.Verify[It.Is("a"), It.Is("b"), It.Is("c"), It.Is("d"), It.Is("e")].Set("x");

			await That(result.Expectation)
				.IsEqualTo("set indexer [\"a\", \"b\", \"c\", \"d\", \"e\"] to \"x\"")
				.Because("the generated narrowed result must render the value like the shipped one");
		}

		public interface IAccessorService
		{
			int this[int key] { get; }
			string this[string key] { set; }
			int this[int key1, int key2] { get; }
			string this[string key1, string key2] { set; }
			string this[int key1, int key2, int key3, int key4] { set; }
			int this[int key1, int key2, int key3, int key4, int key5] { get; }
			string this[string key1, string key2, string key3, string key4, string key5] { set; }
		}

		public class AccessorService
		{
			public int SetterCallCount { get; private set; }

			public virtual int this[int key]
			{
				set => SetterCallCount += value;
			}
		}
	}
}
