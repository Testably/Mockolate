---
description: Let your test framework pass the class under test and its mocks as test method parameters with the AutoMockArguments attribute.
---

# Mock test parameters

Instead of creating the class under test and its mocks in every test, declare them as parameters of the test method. Mockolate generates an `[AutoMockArguments]` attribute for the test framework of your project, which fills these parameters. The class under test receives the same mocks as the test, so you can set them up and verify them directly.

## What each parameter gets

The order of the parameters does not matter. Each parameter gets:

- A **mock** for an interface, delegate or abstract class.
- A **mock that forwards to the real class** for a non-sealed class. Nothing changes until you set up a virtual member, e.g. `sut.Mock.Setup.GetPrice().Returns(1m)`.
- A **real instance** for a sealed class or record.
- A **value from the [default values](../04-create-mocks.md#default-values) of the behavior** for everything else, such as a `string` or an `int`.

A class gets an argument for each constructor parameter:

1. The test parameter of the same type, if there is one.
2. Otherwise a new mock for an interface, by the [rules of `CreateMock`](../04-create-mocks.md#create-the-class-under-test-with-mocks).
3. Otherwise the declared default value of an optional parameter, a new instance of a concrete class, `null` for a nullable parameter, or a value from the default values of the behavior.

On a non-sealed class, reach a dependency without a test parameter with `sut.Mock.ConstructorArgument<IChocolateDispenser>()`.

When several test parameters have the type of a constructor parameter, the one with the name of the constructor parameter is used, ignoring case. Without one, the test fails with a message that names the candidates.

## TUnit

When a project references [TUnit](https://tunit.dev/), the attribute is available in the `Mockolate.Integrations.TUnit` namespace:

```csharp
using Mockolate.Integrations.TUnit;

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

Like TUnit's `[Arguments]`, the attribute takes arguments for the first parameters of the test method. Each attribute is one test case, and a class under test also receives these arguments:

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

Mark a parameter with `[Transient]` to keep it out of the classes under test:

```csharp
[Test]
[AutoMockArguments]
public async Task Buy_ShouldNotUseAnotherDispenser(VendingMachine sut, [Transient] IChocolateDispenser other)
{
    sut.Buy("Dark", 4);

    await That(other.Mock.Verify.Dispense(It.IsAny<string>(), It.IsAny<int>())).Never();
}
```

Assign `AutoMockArgumentsAttribute.DefaultBehavior` to control the mocks and the values, for example with a default value generator backed by a library like AutoFixture. Mockolate calls it once per test case. Assign it in a module initializer, because TUnit creates the arguments before it runs any hook, and on another thread:

```csharp
internal static class MockolateConfiguration
{
    [ModuleInitializer]
    internal static void Configure()
        => AutoMockArgumentsAttribute.DefaultBehavior =
            () => MockBehavior.Default.WithDefaultValueGenerator(new AutoFixtureGenerator());
}
```

<details>
<summary>Details and limitations</summary>

- Mockolate detects the test framework from the references of the project. Set `<MockolateTestFrameworkIntegration>false</MockolateTestFrameworkIntegration>` to skip the generated attribute.
- The attribute is `internal` and generated into each test project.
- Test parameters are matched by their exact type. A dependency of type `IChocolateDispenser` does not receive a test parameter of a type that derives from it.
- The given arguments are passed as they are, without conversion, so their types must match the parameters, e.g. `4` for an `int` and `4L` for a `long`. A single `null`, `[AutoMockArguments(null)]`, is the argument of the first parameter.
- Types of the `System`, `Microsoft` and `Windows` namespaces get a value from the default values of the behavior. An interface among them, such as `IServiceProvider`, gets a mock when the default values provide none. `ILogger`, `ILogger<T>` and `ILoggerFactory` as constructor dependencies get a mock.
- A delegate or an abstract class as a constructor parameter gets no mock, unless a test parameter of that type provides one.
- A constructor dependency that depends on a class that is being created is not created again. It gets `null` when it is nullable, and otherwise a value from the default values of the behavior or a `MockException`.
- Parameters of a type that contains a type parameter, such as `IRepository<T>` in a generic test, cannot be created. The [Mockolate0010](../10-analyzers.md#mockolate0010) analyzer reports these and other parameters that Mockolate cannot create.
- The generated code registers itself from a module initializer. On .NET Framework and .NET Standard, Mockolate declares the `ModuleInitializerAttribute` when the compilation lacks it. If another generator such as PolySharp declares it too, set `<MockolateUnionAttributePolyfills>false</MockolateUnionAttributePolyfills>`, or add `ModuleInitializerAttribute` to the list of attributes in this property.

</details>
