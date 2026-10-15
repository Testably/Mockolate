using Mockolate.Monitor;
using Mockolate.Tests.Protected;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed class ScenarioTests
{
	public sealed class InScenarioTests
	{
		[Test]
		public async Task InScenario_IndexerArity2_ShouldScopeTheSetup()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[1, 2].Returns(42);
			sut.Mock.TransitionTo("a");

			int result = sut[1, 2];

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task InScenario_IndexerArity3_ShouldScopeTheSetup()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[1, 2, 3].Returns(42);
			sut.Mock.TransitionTo("a");

			int result = sut[1, 2, 3];

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task InScenario_IndexerArity4_ShouldScopeTheSetup()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[1, 2, 3, 4].Returns(42);
			sut.Mock.TransitionTo("a");

			int result = sut[1, 2, 3, 4];

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task InScenario_IndexerGetter_ShouldScopeTheSetup()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[1].Returns(42);
			sut.Mock.Setup[1].Returns(7);

			int resultDefault = sut[1];
			sut.Mock.TransitionTo("a");
			int resultScoped = sut[1];

			await That(resultDefault).IsEqualTo(7);
			await That(resultScoped).IsEqualTo(42);
		}

		[Test]
		public async Task InScenario_LambdaOverload_ShouldRegisterAllSetupsInTheScope()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("scoped", scope =>
			{
				scope.Setup.ReturnMethod1(1).Returns(42);
				scope.Setup.ReturnMethod2(1, 2).Returns(99);
			});
			sut.Mock.TransitionTo("scoped");

			int r1 = sut.ReturnMethod1(1);
			int r2 = sut.ReturnMethod2(1, 2);

			await That(r1).IsEqualTo(42);
			await That(r2).IsEqualTo(99);
		}

		[Test]
		public async Task InScenario_LambdaOverload_ShouldReturnMockToAllowFurtherChaining()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock
				.InScenario("a", s => s.Setup.ReturnMethod0().Returns(1))
				.InScenario("b", s => s.Setup.ReturnMethod0().Returns(2));

			sut.Mock.TransitionTo("a");
			int ra = sut.ReturnMethod0();
			sut.Mock.TransitionTo("b");
			int rb = sut.ReturnMethod0();

			await That(ra).IsEqualTo(1);
			await That(rb).IsEqualTo(2);
		}

		[Test]
		public async Task InScenario_SetupsShouldNotLeakIntoDefaultScope()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("scoped").Setup.ReturnMethod1(1).Returns(42);
			sut.Mock.Setup.ReturnMethod1(1).Returns(7);

			int result = sut.ReturnMethod1(1);

			await That(result).IsEqualTo(7);
		}

		[Test]
		public async Task InScenario_ShouldActivateWhenScenarioMatches()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("scoped").Setup.ReturnMethod1(1).Returns(42);
			sut.Mock.Setup.ReturnMethod1(1).Returns(7);
			sut.Mock.TransitionTo("scoped");

			int result = sut.ReturnMethod1(1);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task InScenario_ShouldFallBackToDefaultScopeWhenNoMatchingSetup()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.Setup.ReturnMethod1(1).Returns(7);
			sut.Mock.TransitionTo("scoped");

			int result = sut.ReturnMethod1(1);

			await That(result).IsEqualTo(7);
		}
	}

	public sealed class TransitionToTests
	{
		[Test]
		public async Task EventSubscription_ShouldNotFireOnUnsubscribe()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Event.OnSubscribed.TransitionTo("b");
			sut.Mock.TransitionTo("a");
			EventHandler handler = (_, _) => { };
			sut.Mock.TransitionTo("a");

			sut.Event -= handler;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("a");
		}

		[Test]
		public async Task EventSubscription_ShouldSwitchScenarioWhenSubscribed()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Event.OnSubscribed.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.Event += (_, _) => { };

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task EventUnsubscription_ShouldNotFireOnSubscribe()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Event.OnUnsubscribed.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.Event += (_, _) => { };

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("a");
		}

		[Test]
		public async Task EventUnsubscription_ShouldSwitchScenarioWhenUnsubscribed()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Event.OnUnsubscribed.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.Event -= (_, _) => { };

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task IndexerGetter_Arity1_ShouldSwitchScenarioOnRead()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>()].OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut[1];

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task IndexerGetter_Arity2_ShouldSwitchScenarioOnRead()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>()].OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut[1, 2];

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task IndexerGetter_Arity3_ShouldSwitchScenarioOnRead()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut[1, 2, 3];

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task IndexerGetter_Arity4_ShouldSwitchScenarioOnRead()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut[1, 2, 3, 4];

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task IndexerGetter_Arity5_ShouldSwitchScenarioOnRead()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut[1, 2, 3, 4, 5];

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task IndexerGetter_ShouldNotFireOnWrite()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>()].OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut[1] = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("a");
		}

		[Test]
		public async Task IndexerSetter_Arity1_ShouldSwitchScenarioOnWrite()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>()].OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut[1] = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task IndexerSetter_Arity2_ShouldSwitchScenarioOnWrite()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>()].OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut[1, 2] = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task IndexerSetter_Arity3_ShouldSwitchScenarioOnWrite()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut[1, 2, 3] = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task IndexerSetter_Arity4_ShouldSwitchScenarioOnWrite()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut[1, 2, 3, 4] = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task IndexerSetter_Arity5_ShouldSwitchScenarioOnWrite()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut[1, 2, 3, 4, 5] = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task IndexerSetter_ShouldNotFireOnRead()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>()].OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut[1];

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("a");
		}

		[Test]
		public async Task Method_ReturnArity0_ShouldSwitchScenarioWhenInvoked()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.ReturnMethod0().Returns(1).TransitionTo("b");
			sut.Mock.TransitionTo("a");

			int result = sut.ReturnMethod0();

			await That(result).IsEqualTo(1);
			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Method_ReturnArity1_ShouldSwitchScenarioWhenInvoked()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.ReturnMethod1(It.IsAny<int>()).Returns(1).TransitionTo("b");
			sut.Mock.TransitionTo("a");

			int result = sut.ReturnMethod1(42);

			await That(result).IsEqualTo(1);
			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Method_ReturnArity2_ShouldSwitchScenarioWhenInvoked()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.ReturnMethod2(It.IsAny<int>(), It.IsAny<int>()).Returns(1).TransitionTo("b");
			sut.Mock.TransitionTo("a");

			int result = sut.ReturnMethod2(1, 2);

			await That(result).IsEqualTo(1);
			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Method_ReturnArity3_ShouldSwitchScenarioWhenInvoked()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup
				.ReturnMethod3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns(1).TransitionTo("b");
			sut.Mock.TransitionTo("a");

			int result = sut.ReturnMethod3(1, 2, 3);

			await That(result).IsEqualTo(1);
			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Method_ReturnArity4_ShouldSwitchScenarioWhenInvoked()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup
				.ReturnMethod4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns(1).TransitionTo("b");
			sut.Mock.TransitionTo("a");

			int result = sut.ReturnMethod4(1, 2, 3, 4);

			await That(result).IsEqualTo(1);
			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Method_ReturnArity5_ShouldSwitchScenarioWhenInvoked()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup
				.ReturnMethod5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns(1).TransitionTo("b");
			sut.Mock.TransitionTo("a");

			int result = sut.ReturnMethod5(1, 2, 3, 4, 5);

			await That(result).IsEqualTo(1);
			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Method_VoidArity0_ShouldSwitchScenarioWhenInvoked()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.VoidMethod0().TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.VoidMethod0();

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Method_VoidArity1_ShouldSwitchScenarioWhenInvoked()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.VoidMethod1(It.IsAny<int>()).TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.VoidMethod1(42);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Method_VoidArity2_ShouldSwitchScenarioWhenInvoked()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.VoidMethod2(It.IsAny<int>(), It.IsAny<int>()).TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.VoidMethod2(1, 2);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Method_VoidArity3_ShouldSwitchScenarioWhenInvoked()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup
				.VoidMethod3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.VoidMethod3(1, 2, 3);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Method_VoidArity4_ShouldSwitchScenarioWhenInvoked()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup
				.VoidMethod4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.VoidMethod4(1, 2, 3, 4);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Method_VoidArity5_ShouldSwitchScenarioWhenInvoked()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup
				.VoidMethod5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.VoidMethod5(1, 2, 3, 4, 5);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Mock_TransitionTo_ShouldReturnMockForChaining()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.ReturnMethod0().Returns(42);
			Mock.IMockForIScenarioService changedSut = sut.Mock.TransitionTo("a");

			int result = sut.ReturnMethod0();

			await That(result).IsEqualTo(42);
			await That(changedSut).IsSameAs(sut);
		}

		[Test]
		public async Task Mock_TransitionTo_ShouldSetActiveScenario()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.TransitionTo("scoped");

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("scoped");
		}

		[Test]
		public async Task Property_Getter_ShouldNotFireOnWrite()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Property.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.Property = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("a");
		}

		[Test]
		public async Task Property_Getter_ShouldSwitchScenarioOnRead()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Property.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut.Property;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task Property_Setter_ShouldNotFireOnRead()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Property.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut.Property;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("a");
		}

		[Test]
		public async Task Property_Setter_ShouldSwitchScenarioOnWrite()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Property.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.Property = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}
	}

	public sealed class TransitionToConditionalTests
	{
		[Test]
		public async Task For_ShouldLimitTransitionsToGivenCount()
		{
			IScenarioService sut = IScenarioService.CreateMock();
			sut.Mock.Setup.ReturnMethod0().Returns(1).TransitionTo("b").When(_ => true).For(1);

			_ = sut.ReturnMethod0();
			string afterFirst = ((IMock)sut).MockRegistry.Scenario;
			((IMock)sut).MockRegistry.TransitionTo("reset");
			_ = sut.ReturnMethod0();
			string afterSecond = ((IMock)sut).MockRegistry.Scenario;

			await That(afterFirst).IsEqualTo("b");
			await That(afterSecond).IsEqualTo("reset");
		}

		[Test]
		public async Task IndexerGetter_Arity5_WithWhenAndFor_ShouldRespectBothConstraints()
		{
			IScenarioService sut = IScenarioService.CreateMock();
			sut.Mock.Setup[
					It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.TransitionTo("b").When(i => i > 0).For(1);

			_ = sut[1, 2, 3, 4, 5];
			string afterFirst = ((IMock)sut).MockRegistry.Scenario;
			_ = sut[1, 2, 3, 4, 5];
			string afterSecond = ((IMock)sut).MockRegistry.Scenario;
			((IMock)sut).MockRegistry.TransitionTo("reset");
			_ = sut[1, 2, 3, 4, 5];
			string afterThird = ((IMock)sut).MockRegistry.Scenario;

			await That(afterFirst).IsEqualTo("");
			await That(afterSecond).IsEqualTo("b");
			await That(afterThird).IsEqualTo("reset");
		}

		[Test]
		public async Task IndexerSetter_Arity5_WithWhenAndFor_ShouldRespectBothConstraints()
		{
			IScenarioService sut = IScenarioService.CreateMock();
			sut.Mock.Setup[
					It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>(), It.IsAny<int>()]
				.OnSet.TransitionTo("b").When(i => i > 0).For(1);

			sut[1, 2, 3, 4, 5] = 10;
			string afterFirst = ((IMock)sut).MockRegistry.Scenario;
			sut[1, 2, 3, 4, 5] = 20;
			string afterSecond = ((IMock)sut).MockRegistry.Scenario;
			((IMock)sut).MockRegistry.TransitionTo("reset");
			sut[1, 2, 3, 4, 5] = 30;
			string afterThird = ((IMock)sut).MockRegistry.Scenario;

			await That(afterFirst).IsEqualTo("");
			await That(afterSecond).IsEqualTo("b");
			await That(afterThird).IsEqualTo("reset");
		}

		[Test]
		public async Task When_PredicateNeverTrue_ShouldNotTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();
			sut.Mock.InScenario("a").Setup.ReturnMethod0().Returns(1).TransitionTo("b").When(_ => false);
			sut.Mock.TransitionTo("a");

			_ = sut.ReturnMethod0();
			_ = sut.ReturnMethod0();
			_ = sut.ReturnMethod0();

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("a");
		}

		[Test]
		public async Task When_ShouldDeferTransitionUntilPredicateMatches()
		{
			IScenarioService sut = IScenarioService.CreateMock();
			sut.Mock.InScenario("a").Setup.ReturnMethod0().Returns(1).TransitionTo("b").When(i => i > 0);
			sut.Mock.TransitionTo("a");

			_ = sut.ReturnMethod0();
			string afterFirst = ((IMock)sut).MockRegistry.Scenario;
			_ = sut.ReturnMethod0();
			string afterSecond = ((IMock)sut).MockRegistry.Scenario;

			await That(afterFirst).IsEqualTo("a");
			await That(afterSecond).IsEqualTo("b");
		}

		[Test]
		public async Task WhenAndFor_ShouldRespectBothConstraints()
		{
			IScenarioService sut = IScenarioService.CreateMock();
			sut.Mock.Setup.ReturnMethod0().Returns(1).TransitionTo("b").When(i => i > 0).For(1);

			_ = sut.ReturnMethod0();
			string afterFirst = ((IMock)sut).MockRegistry.Scenario;
			_ = sut.ReturnMethod0();
			string afterSecond = ((IMock)sut).MockRegistry.Scenario;
			((IMock)sut).MockRegistry.TransitionTo("reset");
			_ = sut.ReturnMethod0();
			string afterThird = ((IMock)sut).MockRegistry.Scenario;

			await That(afterFirst).IsEqualTo("");
			await That(afterSecond).IsEqualTo("b");
			await That(afterThird).IsEqualTo("reset");
		}
	}

	public sealed class ProtectedTests
	{
		[Test]
		public async Task InScenario_LambdaOverload_ShouldSupportSetupProtected()
		{
			ProtectedMockTests.MyProtectedClass sut = ProtectedMockTests.MyProtectedClass.CreateMock();

			sut.Mock.InScenario("a", s =>
			{
				s.SetupProtected.MyProtectedMethod(It.IsAny<string>()).Returns("scoped");
			});
			sut.Mock.TransitionTo("a");

			string result = sut.InvokeMyProtectedMethod("anything");

			await That(result).IsEqualTo("scoped");
		}

		[Test]
		public async Task InScenario_SetupProtectedMethod_ShouldScopeTheSetup()
		{
			ProtectedMockTests.MyProtectedClass sut = ProtectedMockTests.MyProtectedClass.CreateMock();

			sut.Mock.InScenario("a").SetupProtected.MyProtectedMethod(It.IsAny<string>()).Returns("scoped");
			sut.Mock.SetupProtected.MyProtectedMethod(It.IsAny<string>()).Returns("default");

			string defaultResult = sut.InvokeMyProtectedMethod("x");
			sut.Mock.TransitionTo("a");
			string scopedResult = sut.InvokeMyProtectedMethod("x");

			await That(defaultResult).IsEqualTo("default");
			await That(scopedResult).IsEqualTo("scoped");
		}

		[Test]
		public async Task InScenario_SetupProtectedProperty_ShouldScopeTheSetup()
		{
			ProtectedMockTests.MyProtectedClass sut = ProtectedMockTests.MyProtectedClass.CreateMock();

			sut.Mock.InScenario("a").SetupProtected.MyProtectedProperty.InitializeWith(99);
			sut.Mock.SetupProtected.MyProtectedProperty.InitializeWith(1);
			sut.Mock.TransitionTo("a");

			int result = sut.GetMyProtectedProperty();

			await That(result).IsEqualTo(99);
		}

		[Test]
		public async Task TransitionTo_FromProtectedMethod_ShouldSwitchScenario()
		{
			ProtectedMockTests.MyProtectedClass sut = ProtectedMockTests.MyProtectedClass.CreateMock();

			sut.Mock.InScenario("a").SetupProtected.MyProtectedMethod(It.IsAny<string>())
				.Returns("scoped").TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut.InvokeMyProtectedMethod("anything");

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}
	}

	public sealed class SharedScenarioStateTests
	{
		[Test]
		public async Task InScenarioSetup_ShouldApplyForDispatchDuringMonitorScope()
		{
			IScenarioService sut = IScenarioService.CreateMock();
			sut.Mock.InScenario("scoped").Setup.ReturnMethod0().Returns(42);
			sut.Mock.Setup.ReturnMethod0().Returns(7);
			MockMonitor<Mock.IMockVerifyForIScenarioService> monitor = sut.Mock.Monitor();

			int defaultResult;
			int scopedResult;
			using (monitor.Run())
			{
				defaultResult = sut.ReturnMethod0();
				sut.Mock.TransitionTo("scoped");
				scopedResult = sut.ReturnMethod0();
			}

			await That(defaultResult).IsEqualTo(7);
			await That(scopedResult).IsEqualTo(42);
		}

		[Test]
		public async Task InScenarioSetup_ShouldApplyWhenDispatchingViaWrappedMock()
		{
			IChocolateDispenser original = IChocolateDispenser.CreateMock();
			original.Mock.InScenario("scoped").Setup.Dispense(It.IsAny<string>(), It.IsAny<int>()).Returns(true);
			original.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>()).Returns(false);
			IChocolateDispenser wrapped = original.Wrapping(new ScenarioDispenser());

			bool defaultResult = wrapped.Dispense("Milk", 1);
			((IMock)original).MockRegistry.TransitionTo("scoped");
			bool scopedResult = wrapped.Dispense("Milk", 1);

			await That(defaultResult).IsFalse();
			await That(scopedResult).IsTrue();
		}

		[Test]
		public async Task TransitionTo_OnOriginal_ShouldBeVisibleOnWrappedMock()
		{
			IChocolateDispenser original = IChocolateDispenser.CreateMock();
			IChocolateDispenser wrapped = original.Wrapping(new ScenarioDispenser());

			((IMock)original).MockRegistry.TransitionTo("a");

			await That(((IMock)wrapped).MockRegistry.Scenario).IsEqualTo("a");
		}

		[Test]
		public async Task TransitionTo_OnWrappedMock_ShouldBeVisibleOnOriginal()
		{
			IChocolateDispenser original = IChocolateDispenser.CreateMock();
			IChocolateDispenser wrapped = original.Wrapping(new ScenarioDispenser());

			((IMock)wrapped).MockRegistry.TransitionTo("b");

			await That(((IMock)original).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task TransitionToFromSetup_CreatedAfterWrapping_ShouldStillPropagateToWrappedMock()
		{
			IChocolateDispenser original = IChocolateDispenser.CreateMock();
			IChocolateDispenser wrapped = original.Wrapping(new ScenarioDispenser());
			original.Mock.InScenario("a").Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
				.Returns(true).TransitionTo("b");
			((IMock)original).MockRegistry.TransitionTo("a");

			wrapped.Dispense("Milk", 1);

			await That(((IMock)wrapped).MockRegistry.Scenario).IsEqualTo("b");
			await That(((IMock)original).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task TransitionToFromSetup_ShouldFireWhenDispatchingDuringMonitorScope()
		{
			IScenarioService sut = IScenarioService.CreateMock();
			sut.Mock.InScenario("a").Setup.ReturnMethod0().Returns(1).TransitionTo("b");
			sut.Mock.TransitionTo("a");
			MockMonitor<Mock.IMockVerifyForIScenarioService> monitor = sut.Mock.Monitor();

			using (monitor.Run())
			{
				_ = sut.ReturnMethod0();
			}

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task TransitionToFromSetup_ShouldPropagateToWrappedMock()
		{
			IChocolateDispenser original = IChocolateDispenser.CreateMock();
			original.Mock.InScenario("a").Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
				.Returns(true).TransitionTo("b");
			((IMock)original).MockRegistry.TransitionTo("a");
			IChocolateDispenser wrapped = original.Wrapping(new ScenarioDispenser());

			wrapped.Dispense("Milk", 1);

			await That(((IMock)wrapped).MockRegistry.Scenario).IsEqualTo("b");
			await That(((IMock)original).MockRegistry.Scenario).IsEqualTo("b");
		}

		private sealed class ScenarioDispenser : IChocolateDispenser
		{
			public int this[string type]
			{
				get => 0;
				set { }
			}

			public int TotalDispensed { get; set; }

#pragma warning disable CS0067
			public event ChocolateDispensedDelegate? ChocolateDispensed;
#pragma warning restore CS0067

			public bool Dispense(string type, int amount) => false;
		}
	}
}
