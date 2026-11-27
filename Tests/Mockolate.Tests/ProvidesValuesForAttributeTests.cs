using System.Reflection;

namespace Mockolate.Tests;

public sealed class ProvidesValuesForAttributeTests
{
	[Test]
	public async Task Types_ShouldBeTheGivenTypes()
	{
		ProvidesValuesForAttribute attribute = typeof(NumberAttribute).GetCustomAttribute<ProvidesValuesForAttribute>()!;

		await That(attribute.Types).IsEqualTo([typeof(int), typeof(long),]);
	}

	[Test]
	public async Task Types_WithNull_ShouldBeEmpty()
	{
		ProvidesValuesForAttribute attribute = new(null);

		await That(attribute.Types).IsEmpty();
	}

	[ProvidesValuesFor(typeof(int), typeof(long))]
	[AttributeUsage(AttributeTargets.Parameter)]
	private sealed class NumberAttribute : Attribute;
}
