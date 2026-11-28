using Mockolate.TestHelpers;

namespace Mockolate.Tests.Scenarios;

public sealed partial class ScenarioTests
{
	public sealed class TransitionToInParallelTests
	{
		[Test]
		public async Task EventSubscription_WithPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Event.OnSubscribed
				.Do(() => { })
				.OnSubscribed.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.Event += (_, _) => { };

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task EventUnsubscription_WithPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Event.OnUnsubscribed
				.Do(() => { })
				.OnUnsubscribed.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.Event -= (_, _) => { };

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task PropertyGetter_WithPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Property.OnGet
				.Do(() => { })
				.OnGet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut.Property;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task PropertySetter_WithPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Property.OnSet
				.Do(() => { })
				.OnSet.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.Property = 42;

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task ReturnMethod_With1ParameterAndPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.ReturnMethod1(It.IsAny<int>())
				.Do(() => { })
				.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut.ReturnMethod1(42);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task ReturnMethod_With2ParametersAndPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.ReturnMethod2(It.IsAny<int>(), It.IsAny<int>())
				.Do(() => { })
				.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut.ReturnMethod2(1, 2);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task ReturnMethod_With3ParametersAndPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup
				.ReturnMethod3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => { })
				.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut.ReturnMethod3(1, 2, 3);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task ReturnMethod_With4ParametersAndPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup
				.ReturnMethod4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => { })
				.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			_ = sut.ReturnMethod4(1, 2, 3, 4);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task VoidMethod_With0ParametersAndPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.VoidMethod0()
				.Do(() => { })
				.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.VoidMethod0();

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task VoidMethod_With1ParameterAndPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.VoidMethod1(It.IsAny<int>())
				.Do(() => { })
				.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.VoidMethod1(42);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task VoidMethod_With2ParametersAndPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.VoidMethod2(It.IsAny<int>(), It.IsAny<int>())
				.Do(() => { })
				.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.VoidMethod2(1, 2);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task VoidMethod_With3ParametersAndPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.VoidMethod3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => { })
				.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.VoidMethod3(1, 2, 3);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}

		[Test]
		public async Task VoidMethod_With4ParametersAndPrecedingCallback_ShouldStillTransition()
		{
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup
				.VoidMethod4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => { })
				.TransitionTo("b");
			sut.Mock.TransitionTo("a");

			sut.VoidMethod4(1, 2, 3, 4);

			await That(((IMock)sut).MockRegistry.Scenario).IsEqualTo("b");
		}
	}
}
