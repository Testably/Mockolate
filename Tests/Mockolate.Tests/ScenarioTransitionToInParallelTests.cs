using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed class ScenarioTransitionToInParallelTests
{
	[Test]
	public async Task EventSubscription_TransitionsEvenAfterPrecedingCallbackRan()
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
	public async Task EventUnsubscription_TransitionsEvenAfterPrecedingCallbackRan()
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
	public async Task PropertyGetter_TransitionsEvenAfterPrecedingCallbackRan()
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
	public async Task PropertySetter_TransitionsEvenAfterPrecedingCallbackRan()
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
	public async Task ReturnMethod_Arity1_TransitionsEvenAfterPrecedingCallbackRan()
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
	public async Task ReturnMethod_Arity2_TransitionsEvenAfterPrecedingCallbackRan()
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
	public async Task ReturnMethod_Arity3_TransitionsEvenAfterPrecedingCallbackRan()
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
	public async Task ReturnMethod_Arity4_TransitionsEvenAfterPrecedingCallbackRan()
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
	public async Task VoidMethod_Arity0_TransitionsEvenAfterPrecedingCallbackRan()
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
	public async Task VoidMethod_Arity1_TransitionsEvenAfterPrecedingCallbackRan()
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
	public async Task VoidMethod_Arity2_TransitionsEvenAfterPrecedingCallbackRan()
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
	public async Task VoidMethod_Arity3_TransitionsEvenAfterPrecedingCallbackRan()
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
	public async Task VoidMethod_Arity4_TransitionsEvenAfterPrecedingCallbackRan()
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
