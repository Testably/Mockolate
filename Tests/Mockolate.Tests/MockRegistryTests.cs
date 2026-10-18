using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Mockolate.Setup;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed class MockRegistryTests
{
	[Test]
	public async Task AddEvent_WithoutMemberIdAndMatchingSetup_ShouldInvokeSubscribedCallback()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		int subscribed = 0;
		EventSetup setup = new(registry, "OnFoo");
		setup.OnSubscribed.Do(() => subscribed++);
		registry.SetupEvent(setup);

		registry.AddEvent("OnFoo", this, GetMethodInfo());

		await That(subscribed).IsEqualTo(1);
	}

	[Test]
	public async Task GetProperty_WhenBaseValueAccessorThrows_ShouldRethrowException()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		InvalidOperationException expected = new("base failed");

		void Act()
		{
			registry.GetProperty("Foo", () => 0, () => throw expected);
		}

		await That(Act).Throws<InvalidOperationException>().WithMessage("base failed");
	}

	[Test]
	public async Task GetUnusedSetups_IndexerSetup_ShouldHaveCorrectString()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup["Dark"].Returns(5);
		MockRegistry registry = ((IMock)sut).MockRegistry;

		IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

		ISetup setup = await That(result).HasSingle();
		await That(setup.ToString()).IsEqualTo("int this[\"Dark\"]");
	}

	[Test]
	public async Task GetUnusedSetups_MethodSetup_ShouldHaveCorrectString()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.DoSomething(null, 3.5, It.IsAny<string>()).DoesNotThrow();
		MockRegistry registry = ((IMock)sut).MockRegistry;

		IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

		ISetup setup = await That(result).HasSingle();
		await That(setup.ToString()).IsEqualTo("void DoSomething(null, 3.5, It.IsAny<string>())");
	}

	[Test]
	public async Task GetUnusedSetups_PropertySetup_ShouldHaveCorrectString()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup.TotalDispensed.InitializeWith(4);
		MockRegistry registry = ((IMock)sut).MockRegistry;

		IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

		ISetup setup = await That(result).HasSingle();
		await That(setup.ToString()).IsEqualTo("int TotalDispensed");
	}

	[Test]
	public async Task GetUnusedSetups_WithNullableDouble_ShouldHaveCorrectString()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.DoSomething(new DateTime(2026, 4, 1, 12, 0, 0), 3.5).DoesNotThrow();
		MockRegistry registry = ((IMock)sut).MockRegistry;

		IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

		ISetup setup = await That(result).HasSingle();
		await That(setup.ToString()).IsEqualTo("void DoSomething(04/01/2026 12:00:00, 3.5)");
	}

	[Test]
	public async Task RegisterInteraction_ShouldBeThreadSafe()
	{
		MockRegistry sut = new(MockBehavior.Default, new FastMockInteractions(0));
		Task[] tasks = new Task[50];
		for (int i = 0; i < 50; i++)
		{
			tasks[i] = Task.Run(async () =>
			{
				for (int j = 0; j < 20; j++)
				{
					sut.GetProperty<string>("foo", () => "", null);
					await Task.Delay(1);
				}
			}, CancellationToken.None);
		}

		await Task.WhenAll(tasks);

		await That(sut.Interactions.Count).IsEqualTo(1000);
	}

	[Test]
	public async Task RemoveEvent_WithoutMemberIdAndMatchingSetup_ShouldInvokeUnsubscribedCallback()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
		int unsubscribed = 0;
		EventSetup setup = new(registry, "OnFoo");
		setup.OnUnsubscribed.Do(() => unsubscribed++);
		registry.SetupEvent(setup);

		registry.RemoveEvent("OnFoo", this, GetMethodInfo());

		await That(unsubscribed).IsEqualTo(1);
	}

	[Test]
	public async Task SetProperty_WithMemberIdAndNoFastBuffer_ShouldRecordAndStore()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

		bool result = registry.SetProperty(7, "Foo", 42);

		await That(result).IsFalse();
		await That(registry.Interactions.Count).IsEqualTo(1);
	}

	[Test]
	public async Task SetProperty_WithoutMemberId_SkippingBaseClass_ShouldReturnTrue()
	{
		MockBehavior behavior = MockBehavior.Default.SkippingBaseClass();
		MockRegistry registry = new(behavior, new FastMockInteractions(0, behavior.SkipInteractionRecording));

		bool result = registry.SetProperty("Foo", 42);

		await That(result).IsTrue();
	}

	private static MethodInfo GetMethodInfo()
		=> typeof(MockRegistryTests).GetMethod(nameof(GetMethodInfo),
			BindingFlags.Static | BindingFlags.NonPublic)!;
}
