namespace Mockolate.Tests;

public sealed class DefaultValueFactoryTests
{
	[Test]
	public async Task CanGenerateValue_ShouldFallbackToFalse()
	{
		MyDefaultValueFactory defaultValueFactory = new();

		bool result = defaultValueFactory.CanGenerateValue(typeof(DefaultValueFactoryTests));

		await That(result).IsFalse();
	}

	[Test]
	public async Task GenerateValue_ShouldFallbackToAlwaysReturnNull()
	{
		MyDefaultValueFactory defaultValueFactory = new();

		object? result = defaultValueFactory.GenerateValue(typeof(int));

		await That(result).IsNull();
	}

	private class MyDefaultValueFactory : DefaultValueFactory
	{
	}
}
