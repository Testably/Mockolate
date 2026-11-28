using Mockolate.TestHelpers;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class SetupIndexerTests
{
	public sealed class WhenInScenarioTests
	{
		[Test]
		public async Task OnGet_With1Key_ShouldTransitionEvenAfterPrecedingCallbackRan()
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
		public async Task OnGet_With2Keys_ShouldTransitionEvenAfterPrecedingCallbackRan()
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
		public async Task OnGet_With3Keys_ShouldTransitionEvenAfterPrecedingCallbackRan()
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
		public async Task OnGet_With4Keys_ShouldTransitionEvenAfterPrecedingCallbackRan()
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
		public async Task OnSet_With1Key_ShouldTransitionEvenAfterPrecedingCallbackRan()
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
		public async Task OnSet_With2Keys_ShouldTransitionEvenAfterPrecedingCallbackRan()
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
		public async Task OnSet_With3Keys_ShouldTransitionEvenAfterPrecedingCallbackRan()
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
		public async Task OnSet_With4Keys_ShouldTransitionEvenAfterPrecedingCallbackRan()
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
