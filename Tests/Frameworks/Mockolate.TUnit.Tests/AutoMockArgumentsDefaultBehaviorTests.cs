using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using Mockolate.Integrations.TUnit;

namespace Mockolate.TUnit.Tests;

public sealed class AutoMockArgumentsDefaultBehaviorTests
{
	private static readonly ConcurrentDictionary<int, byte> SeenBehaviors = new();
	private static int _behaviorCount;

	[ModuleInitializer]
	internal static void Configure()
		=> AutoMockArgumentsAttribute.DefaultBehavior = () =>
		{
			int id = Interlocked.Increment(ref _behaviorCount);
			return MockBehavior.Default.WithDefaultValueFor(() => new ConfiguredValue(id));
		};

	[Test]
	[AutoMockArguments]
	public async Task WhenAssignedInModuleInitializer_ShouldApplyToTheArguments(ConfiguredValue value)
		=> await That(value.Id).IsGreaterThan(0);

	[Test]
	[AutoMockArguments(1)]
	[AutoMockArguments(2)]
	public async Task ShouldBeCalledOncePerTestCase(int testCase, ConfiguredValue first,
		ConfiguredValue second)
	{
		await That(testCase).IsGreaterThan(0);
		await That(second.Id).IsEqualTo(first.Id);
		await That(SeenBehaviors.TryAdd(first.Id, 0)).IsTrue();
	}

	public readonly struct ConfiguredValue(int id)
	{
		public int Id { get; } = id;
	}
}
