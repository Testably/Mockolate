---
description: Get the class under test and its mocks as parameters of your test method with the AutoMockArguments attribute.
---

# Mock test parameters

Declare the class under test and its dependencies as parameters of your test method and add `[AutoMockArguments]`. Mockolate fills the parameters, and the class under test uses the same mocks that your test receives, so you can set them up and verify them directly:

```csharp
public class VendingMachine(IChocolateDispenser dispenser)
{
    public string Buy(string type, int amount)
        => dispenser.Dispense(type, amount) ? "Enjoy!" : "Sold out";
}

public class VendingMachineTests
{
    [Test]
    [AutoMockArguments]
    public async Task Buy_WhenChocolateIsAvailable_ShouldDispenseIt(VendingMachine sut, IChocolateDispenser dispenser)
    {
        dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);

        string result = sut.Buy("Dark", 4);

        await That(result).IsEqualTo("Enjoy!");
        await That(dispenser.Mock.Verify.Dispense("Dark", 4)).Once();
    }
}
```

Mockolate generates the attribute for the test framework of your project. The sections below show how to use it with [TUnit](#tunit) and [xUnit](#xunit).

## What each parameter gets

- An interface, delegate or abstract class gets a **mock**.
- A class gets a **mock that calls the real class**. It behaves like the real class until you set up one of its virtual members, e.g. `sut.Mock.Setup.GetPrice().Returns(1m)` for a virtual `GetPrice` method.
- A sealed class or a record gets a **real instance**.
- Any other type, such as `string` or `int`, gets a [default value](../04-create-mocks.md#default-values).

The order of the parameters does not matter. A class receives the other parameters of the test method as its constructor arguments. A constructor parameter without a matching test parameter gets a new mock or a default value, which you can reach with `sut.Mock.ConstructorArgument<IChocolateDispenser>()`.

A test parameter can fill several constructor parameters of its type. When several test parameters have the type of a constructor parameter, give one of them the name of the constructor parameter. A test parameter of a value type or `string` is only passed to a constructor parameter with its name.

## Pass values

The attribute takes values for the first parameters of the test method. Each attribute is one test case:

```csharp
[Test]
[AutoMockArguments("Dark", 4)]
[AutoMockArguments("White", 2)]
public async Task Buy_ShouldDispenseTheRequestedChocolate(string type, int amount, VendingMachine sut,
    IChocolateDispenser dispenser)
{
    sut.Buy(type, amount);

    await That(dispenser.Mock.Verify.Dispense(type, amount)).Once();
}
```

The values are only passed to the test method. A class under test does not receive them as constructor arguments.

## Keep a mock out of the class under test

Mark a parameter with `[Transient]` to get a mock that the class under test does not receive:

```csharp
[Test]
[AutoMockArguments]
public async Task Buy_ShouldNotUseAnotherDispenser(VendingMachine sut, [Transient] IChocolateDispenser other)
{
    sut.Buy("Dark", 4);

    await That(other.Mock.Verify.Dispense(It.IsAny<string>(), It.IsAny<int>())).Never();
}
```

## Configure the mocks

Assign `AutoMockArgumentsAttribute.DefaultBehavior` to choose the [behavior](../04-create-mocks.md#customizing-mock-behavior) of the mocks and the default values, for example with a default value generator backed by a library like AutoFixture. Assign it in a module initializer, so that it is set before the first test case is created:

```csharp
using System.Runtime.CompilerServices;

internal static class MockolateConfiguration
{
    [ModuleInitializer]
    internal static void Configure()
        => AutoMockArgumentsAttribute.DefaultBehavior =
            () => MockBehavior.Default.WithDefaultValueGenerator(new AutoFixtureGenerator());
}
```

## TUnit

With [TUnit](https://tunit.dev/) 1.0 or later, the attribute is in the `Mockolate.Integrations.TUnit` namespace. Use it on a `[Test]`, like TUnit's `[Arguments]`:

```csharp
using Mockolate.Integrations.TUnit;

[Test]
[AutoMockArguments("Dark", 4)]
public async Task Buy_ShouldDispenseTheRequestedChocolate(string type, int amount, VendingMachine sut,
    IChocolateDispenser dispenser)
```

## xUnit

With [xUnit](https://xunit.net/) v3 or v2, the attribute is in the `Mockolate.Integrations.Xunit` namespace. Use it on a `[Theory]`, like xUnit's `[InlineData]`:

```csharp
using Mockolate.Integrations.Xunit;

[Theory]
[AutoMockArguments("Dark", 4)]
public async Task Buy_ShouldDispenseTheRequestedChocolate(string type, int amount, VendingMachine sut,
    IChocolateDispenser dispenser)
```

A project that references both v2 and v3 gets the attribute for v3. With v3, the attribute takes the settings of xUnit's data attributes, such as `Skip`, `Explicit`, `Timeout` and `Traits`. With both versions, the attributes of a theory show up as one test in the test explorer, with a result for each of them. When xUnit v2 discovers the tests, it may also create the arguments of the first attribute once and discard them.

xUnit describes the arguments in the name of a test case and disposes them afterwards. To describe a mock of a collection interface, such as `ISet<T>` or `IGrouping<TKey, TElement>`, it enumerates the mock before the test starts; xUnit v2 before 2.5 enumerates a mock of any enumerable interface. A mock of a disposable interface gets a call to `Dispose` after the test. A strict mock rejects these calls without a setup, and they count as interactions.

<details>
<summary>Details and limitations</summary>

- Set `<MockolateTestFrameworkIntegration>false</MockolateTestFrameworkIntegration>` in the project file to skip the attribute.
- A test parameter matches a constructor parameter of exactly its type. A constructor parameter of type `IChocolateDispenser` does not receive a test parameter of a type that derives from it.
- A constructor parameter of a delegate or abstract class type only gets a mock from a test parameter of that type.
- When two classes under test depend on each other, only the first of the two test parameters receives the other one; the second gets a new instance or its default value.
- The given values must have the types of the parameters, e.g. `4` for an `int` and `4L` for a `long`. `[AutoMockArguments(null)]` passes `null` to the first parameter. A single array, e.g. `[AutoMockArguments(new object[] { "Dark", 4 })]`, passes its elements as the values.
- The [Mockolate0010](../10-analyzers.md#mockolate0010) analyzer reports parameters that Mockolate cannot create, such as `IRepository<T>` in a generic test.
- On .NET Framework and .NET Standard, Mockolate declares the `ModuleInitializerAttribute` when neither the project nor TUnit does. If another package such as PolySharp declares it too, set `<MockolateUnionAttributePolyfills>false</MockolateUnionAttributePolyfills>`.

</details>
