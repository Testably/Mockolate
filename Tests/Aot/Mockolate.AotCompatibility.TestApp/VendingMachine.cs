using Mockolate.Internals;

namespace Mockolate.AotCompatibility.TestApp;

public class VendingMachine(IChocolateDispenser dispenser, ChocolateCounter counter)
{
	public ChocolateCounter Counter { get; } = counter;

	public bool Buy(string type, int amount)
	{
		Counter.Count(amount);
		return dispenser.Dispense(type, amount);
	}
}

public class ChocolateCounter(IChocolateDispenser dispenser)
{
	public IChocolateDispenser Dispenser { get; } = dispenser;
	public int Total { get; private set; }

	public void Count(int amount) => Total += amount;
}

public sealed class SealedVendingMachine(IChocolateDispenser dispenser, string location)
{
	public string Location { get; } = location;

	public bool Buy(string type, int amount) => dispenser.Dispense(type, amount);
}

/// <summary>
///     A mock data source like that of a test framework integration, which the generator finds by its interface.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MockArgumentsAttribute : Attribute, IMockDataSource;

/// <summary>
///     A <c>[Deferred]</c> marker like that of a test framework integration.
/// </summary>
internal sealed class DeferredAttribute : Mockolate.Integrations.DeferredParameterAttribute;

/// <summary>
///     A <c>[ValueFrom]</c> marker like that of a test framework integration.
/// </summary>
internal sealed class ValueFromAttribute(string memberName) : Mockolate.Integrations.ValueFromParameterAttribute(memberName);

/// <summary>
///     A value provider like one that a user writes.
/// </summary>
[ProvidesValuesFor(typeof(string))]
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class LocationAttribute : Attribute, IParameterValueProvider
{
	public object? ProvideValue(System.Reflection.ParameterInfo parameter) => $"{parameter.Name} hall";
}

public static class MockDataSourceTargets
{
	[MockArguments]
	internal static string FromProvider([Location] string entrance) => entrance;

	[MockArguments]
	public static bool Buy(VendingMachine sut, IChocolateDispenser dispenser, string type) => sut.Buy(type, 1) && dispenser.Stock > 0;

	[MockArguments]
	internal static VendingMachine CreateLater([Deferred] Func<VendingMachine> createSut, IChocolateDispenser dispenser)
		=> createSut();

	[MockArguments]
	internal static string FromMember([ValueFrom(nameof(Lobby))] string location) => location;

	private static string Lobby => "Lobby";

	internal static class Nested
	{
		[MockArguments]
		internal static string FromNestedMember([ValueFrom(nameof(Hall))] string location) => location;

		private static string Hall => "Hall";
	}
}
