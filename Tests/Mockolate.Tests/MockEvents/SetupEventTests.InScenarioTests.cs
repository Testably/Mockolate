using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests.MockEvents;

public sealed partial class SetupEventTests
{
	public sealed class InScenarioTests
	{
		[Test]
		public async Task WithMatchingScopedAndGlobalSubscribedSetups_OnlyScopedShouldFire()
		{
			int scopedCount = 0;
			int globalCount = 0;
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Event.OnSubscribed.Do(() => { scopedCount++; });
			sut.Mock.Setup.Event.OnSubscribed.Do(() => { globalCount++; });
			sut.Mock.TransitionTo("a");

			sut.Event += (_, _) => { };

			await That(scopedCount).IsEqualTo(1);
			await That(globalCount).IsEqualTo(0);
		}

		[Test]
		public async Task WithMatchingScopedAndGlobalUnsubscribedSetups_OnlyScopedShouldFire()
		{
			int scopedCount = 0;
			int globalCount = 0;
			IScenarioService sut = IScenarioService.CreateMock();

			sut.Mock.InScenario("a").Setup.Event.OnUnsubscribed.Do(() => { scopedCount++; });
			sut.Mock.Setup.Event.OnUnsubscribed.Do(() => { globalCount++; });
			sut.Mock.TransitionTo("a");

			// ReSharper disable once EventUnsubscriptionViaAnonymousDelegate
			sut.Event -= (_, _) => { };

			await That(scopedCount).IsEqualTo(1);
			await That(globalCount).IsEqualTo(0);
		}

		[Test]
		public async Task WithScopedBucketButNoScopedEventSetup_GlobalShouldStillFire()
		{
			int globalCount = 0;
			IScenarioService sut = IScenarioService.CreateMock();

			// Register something in scope "a" that isn't the event so the scoped bucket exists
			// but has no OnSubscribed setups.
			sut.Mock.InScenario("a").Setup.Property.OnGet.Do(() => { });
			sut.Mock.Setup.Event.OnSubscribed.Do(() => { globalCount++; });
			sut.Mock.TransitionTo("a");

			sut.Event += (_, _) => { };

			await That(globalCount).IsEqualTo(1);
		}
	}
}
