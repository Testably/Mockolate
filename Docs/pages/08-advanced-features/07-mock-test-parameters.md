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

Mockolate generates the attribute for the test framework of your project. The sections below show how to use it with [TUnit](#tunit), [xUnit](#xunit), [NUnit](#nunit) and [MSTest](#mstest).

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

## Return a parameter from the other mocks

Mark a parameter with `[AsDefault]` to make it the [default value](../04-create-mocks.md#default-values) of its type for all mocks of the test case. Their properties, methods and indexers without a setup return it, also wrapped in a `Task<T>` or `ValueTask<T>`, and so do their `out` parameters:

```csharp
public interface IVendingMachine
{
    IChocolateDispenser Dispenser { get; }
}

[Test]
[AutoMockArguments]
public async Task Dispenser_ShouldBeTheMockOfTheTest(IVendingMachine machine, [AsDefault] IChocolateDispenser dispenser)
{
    await That(machine.Dispenser).IsSameAs(dispenser);
}
```

A setup, a value written to a property or indexer, and `ThrowWhenNotSetup` still take precedence. The parameter only replaces the default value of its exact type, e.g. not of an interface that `IChocolateDispenser` implements unless you [name the interface](#use-a-fake-for-its-interfaces), and it wins over the default values of the [behavior](#configure-the-mocks) for that type. Every member of the type returns the same instance, also a method that creates a new one in the real implementation or an indexer for every key; set up such a member when it needs other instances. The parameter is still passed to the constructor of a class under test. When the test method does not use it, it gets no [IDE0060](../10-analyzers.md#mockolate1001) diagnostic.

## Use a fake for its interfaces

A test parameter only fills constructor parameters of exactly its type. To use a hand-written fake or another test double for an interface that it implements, name the interface in `[AsDefault<T>]`:

```csharp
public sealed class FakeChocolateDispenser : IChocolateDispenser
{
    public List<string> Dispensed { get; } = [];

    public bool Dispense(string type, int amount)
    {
        Dispensed.Add(type);
        return true;
    }
}

[Test]
[AutoMockArguments]
public async Task Buy_ShouldUseTheFake(VendingMachine sut,
    [AsDefault<IChocolateDispenser>] FakeChocolateDispenser dispenser)
{
    sut.Buy("Dark", 4);

    await That(dispenser.Dispensed).IsEqualTo(["Dark"]);
}
```

The parameter is then used for its own type and for each named type: constructor parameters of these types receive it, and it is the [default value](#return-a-parameter-from-the-other-mocks) of these types for all mocks of the test case. Name up to four types, e.g. `[AsDefault<IChocolateDispenser, IDisposable>]`.

The generic attribute needs .NET 7 or later, because .NET Framework cannot read generic attributes. On .NET Framework, or for more than four types, pass the types as arguments: `[AsDefault(typeof(IChocolateDispenser))]`.

When another test parameter has a named type, a constructor parameter of that type gets the one with its name, like with [several test parameters of a type](#what-each-parameter-gets). The [Mockolate0011](../10-analyzers.md#mockolate0011) analyzer reports a parameter that is not assignable to a named type, and two `[AsDefault]` parameters for the same type.

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

## Test frameworks

The attribute is in a namespace of its test framework and works like that framework's own data attributes.

### TUnit

With [TUnit](https://tunit.dev/) 1.0 or later, the attribute is in the `Mockolate.Integrations.TUnit` namespace. Use it on a `[Test]`, like TUnit's `[Arguments]`:

```csharp
using Mockolate.Integrations.TUnit;

[Test]
[AutoMockArguments("Dark", 4)]
public async Task Buy_ShouldDispenseTheRequestedChocolate(string type, int amount, VendingMachine sut,
    IChocolateDispenser dispenser)
```

### xUnit

With [xUnit](https://xunit.net/) v3 or v2, the attribute is in the `Mockolate.Integrations.Xunit` namespace. Use it on a `[Theory]`, like xUnit's `[InlineData]`:

```csharp
using Mockolate.Integrations.Xunit;

[Theory]
[AutoMockArguments("Dark", 4)]
public async Task Buy_ShouldDispenseTheRequestedChocolate(string type, int amount, VendingMachine sut,
    IChocolateDispenser dispenser)
```

An `[AsDefault]` parameter that the test method does not use gets no [xUnit1026](../10-analyzers.md#mockolate1002) warning. A project that references both v2 and v3 gets the attribute for v3. With v3, the attribute takes the settings of xUnit's data attributes, such as `Skip`, `Explicit`, `Timeout` and `Traits`. A theory with mocks shows up as one test in the test explorer, with a result for each attribute. xUnit v2 may already create the arguments of the first attribute when it discovers the tests.

xUnit describes the arguments in the name of a test case and disposes them after the test. To describe a mock of a collection interface, such as `ISet<T>`, it may enumerate the mock, and a mock of a disposable interface gets a call to `Dispose`. Both calls count as interactions of the mock. On a strict mock, set up `Dispose`, or xUnit reports a cleanup error.

### NUnit

With [NUnit](https://nunit.org/) 3.7 or later, the attribute is in the `Mockolate.Integrations.NUnit` namespace. Use it instead of `[Test]`, like NUnit's `[TestCase]`:

```csharp
using Mockolate.Integrations.NUnit;

[AutoMockArguments("Dark", 4)]
public async Task Buy_ShouldDispenseTheRequestedChocolate(string type, int amount, VendingMachine sut,
    IChocolateDispenser dispenser)
```

Mockolate creates the arguments after the `[SetUp]` methods, and `[Repeat]` and `[Retry]` give each run new mocks. With NUnit 4, a last `CancellationToken` parameter gets the token of `[CancelAfter]`.

The name of a test case only shows the given values, e.g. `Buy_ShouldDispenseTheRequestedChocolate("Dark", 4)`. The attribute has none of the settings of `[TestCase]`, such as `ExpectedResult` or `Ignore`; use NUnit's attributes like `[Category]` or `[Ignore]` on the test method instead. A generic test method works when the given values determine its type arguments.

With NUnit 4.5 or later, the `BeforeTestHook` and `AfterTestHook` of an execution hook on the test fixture or the assembly do not run for these tests. Put the hook attribute on the test method instead, but not between two hook attributes: place `[AutoMockArguments]` before or after all of them.

### MSTest

With [MSTest](https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-intro) 3.7 or later, the attribute is in the `Mockolate.Integrations.MSTest` namespace. Use it on a `[TestMethod]`, like MSTest's `[DataRow]`:

```csharp
using Mockolate.Integrations.MSTest;

[TestMethod]
[AutoMockArguments("Dark", 4)]
public async Task Buy_ShouldDispenseTheRequestedChocolate(string type, int amount, VendingMachine sut,
    IChocolateDispenser dispenser)
```

The name of a test case only shows the given values, e.g. `Buy_ShouldDispenseTheRequestedChocolate("Dark", 4)`.

With MSTest 4, add `[assembly: TestDataSourceOptions(TestDataSourceUnfoldingStrategy.Fold)]` to the test project, or set `[TestMethod(UnfoldingStrategy = TestDataSourceUnfoldingStrategy.Fold)]` on a test method. Then MSTest creates the arguments when the test runs, and `[Retry]` gets new mocks for every attempt. Otherwise, depending on the version of MSTest and the test runner, MSTest may create them already when it discovers the tests, run the test with a copy of values that are not mocks, or reuse the mocks of a failed attempt. With MSTest 3, the attribute always creates the arguments when the test runs.

<details>
<summary>Details and limitations</summary>

- Set `<MockolateTestFrameworkIntegration>false</MockolateTestFrameworkIntegration>` in the project file to skip the attribute.
- Mockolate recognizes MSTest 3 only through its NuGet package, because earlier versions use the same assembly name.
- A test parameter matches a constructor parameter of exactly its type. A constructor parameter of type `IChocolateDispenser` does not receive a test parameter of a type that derives from it, unless that parameter [names the interface](#use-a-fake-for-its-interfaces) with `[AsDefault<IChocolateDispenser>]`.
- A constructor parameter of a delegate or abstract class type only gets a mock from a test parameter of that type.
- When two classes under test depend on each other, only the first of the two test parameters receives the other one; the second gets a new instance or its default value.
- The given values must have the types of the parameters, e.g. `4` for an `int` and `4L` for a `long`. `[AutoMockArguments(null)]` passes `null` to the first parameter. A single array, e.g. `[AutoMockArguments(new object[] { "Dark", 4 })]` or `[AutoMockArguments(new[] { "Dark", "White" })]`, passes its elements as the values; to pass the array itself, wrap it, e.g. `[AutoMockArguments(new object[] { new[] { "Dark", "White" } })]`.
- The [Mockolate0010](../10-analyzers.md#mockolate0010) analyzer reports parameters that Mockolate cannot create, such as `IRepository<T>` in a generic test.
- On .NET Framework and .NET Standard, Mockolate declares the `ModuleInitializerAttribute` when neither the project, a referenced assembly, TUnit nor PolySharp does. If another source generator declares it, set `<MockolateUnionAttributePolyfills>ModuleInitializerAttribute</MockolateUnionAttributePolyfills>`. If the project excludes the analyzer assets of TUnit (`ExcludeAssets="analyzers"`), so that its source generator does not run, set `<EnableTUnitPolyfills>false</EnableTUnitPolyfills>`.

</details>
