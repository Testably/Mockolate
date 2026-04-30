using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class SetupIndexerTests
{
	public sealed class InScenarioTests
	{
		[Test]
		public async Task OnGet_Arity1_ShouldTransitionEvenAfterPrecedingCallbackRan()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>()]
				.OnGet.Do(() => { })
				.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut[1];

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task OnGet_Arity2_ShouldTransitionEvenAfterPrecedingCallbackRan()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.Do(() => { })
				.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut[1, 2];

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task OnGet_Arity3_ShouldTransitionEvenAfterPrecedingCallbackRan()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.Do(() => { })
				.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut[1, 2, 3];

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task OnGet_Arity4_ShouldTransitionEvenAfterPrecedingCallbackRan()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.Do(() => { })
				.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut[1, 2, 3, 4];

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task OnSet_Arity1_ShouldTransitionEvenAfterPrecedingCallbackRan()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>()]
				.OnSet.Do(() => { })
				.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut[1] = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task OnSet_Arity2_ShouldTransitionEvenAfterPrecedingCallbackRan()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>()]
				.OnSet.Do(() => { })
				.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut[1, 2] = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task OnSet_Arity3_ShouldTransitionEvenAfterPrecedingCallbackRan()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnSet.Do(() => { })
				.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut[1, 2, 3] = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task OnSet_Arity4_ShouldTransitionEvenAfterPrecedingCallbackRan()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnSet.Do(() => { })
				.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut[1, 2, 3, 4] = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}
	}
}
