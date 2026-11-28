---
description: Declare the class under test and its mocks as parameters of your test method, and let the AutoMockArguments attribute create them for each test case.
---

# Mocks as test parameters

Instead of creating the class under test and its mocks at the start of every test, declare them as parameters of the test method and add `[AutoMockArguments]`. Mockolate creates the parameters, and the class under test receives the same mocks that your test receives, so you can set them up and verify them directly:

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

The attribute replaces the arrange code of [creating the class under test with mocks](03-create-mocks.md#create-the-class-under-test-with-mocks). Every test case gets new instances, which Mockolate creates before the test runs.

## Set up your test framework

Mockolate generates the attribute for the test framework that your project references, so there is no other package to install. Import the namespace of your framework and use the attribute like the framework's own data attribute:

| Test framework                                                                     | Versions     | Namespace                       | Put it              | Works like       |
|------------------------------------------------------------------------------------|--------------|---------------------------------|---------------------|------------------|
| [TUnit](https://tunit.dev/)                                                        | 1.0 or later | `Mockolate.Integrations.TUnit`  | on a `[Test]`       | `[Arguments]`    |
| [xUnit](https://xunit.net/)                                                        | v2 and v3    | `Mockolate.Integrations.Xunit`  | on a `[Theory]`     | `[InlineData]`   |
| [NUnit](https://nunit.org/)                                                        | 3.7 or later | `Mockolate.Integrations.NUnit`  | instead of `[Test]` | `[TestCase]`     |
| [MSTest](https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-intro) | 3.7 or later | `Mockolate.Integrations.MSTest` | on a `[TestMethod]` | `[DataRow]`      |

The examples on this page use TUnit. With NUnit, the attribute takes the place of `[Test]`:

```csharp
using Mockolate.Integrations.NUnit;

public class VendingMachineTests
{
    [AutoMockArguments]
    public async Task Buy_WhenChocolateIsAvailable_ShouldDispenseIt(VendingMachine sut, IChocolateDispenser dispenser)
    {
        dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);

        await That(sut.Buy("Dark", 4)).IsEqualTo("Enjoy!");
    }
}
```

:::warning[MSTest 4]

Add `[assembly: TestDataSourceOptions(TestDataSourceUnfoldingStrategy.Fold)]` to the test project, or set `[TestMethod(UnfoldingStrategy = TestDataSourceUnfoldingStrategy.Fold)]` on a test method. Then MSTest creates the arguments when the test runs, and `[Retry]` gets new mocks for every attempt. Otherwise, depending on the version of MSTest and the test runner, MSTest may create them already when it discovers the tests, run the test with a copy of values that are not mocks, or reuse the mocks of a failed attempt. MSTest 3 always creates the arguments when the test runs.

:::

The [framework notes](#framework-notes) list the other differences between the frameworks.

## What each parameter gets

- An interface, delegate or abstract class gets a **mock**.
- A class gets a **mock that calls the real class**. It behaves like the real class, and you can still set up its virtual members, e.g. `sut.Mock.Setup.GetPrice().Returns(1m)` for a virtual `GetPrice` method.
- A sealed class or a record gets a **real instance**.
- Any other type, such as `string` or `int`, gets a [default value](03-create-mocks.md#default-values).

Each parameter is created once per test case. The [Mockolate0010](10-analyzers.md#mockolate0010) analyzer reports a parameter that Mockolate cannot create, such as `IChocolateRepository<TFlavor>` in a generic test class.

## How the class under test receives the parameters

A class receives the other parameters of the test method as its constructor arguments, in any order. A constructor parameter without a matching test parameter gets a new mock or a default value, which you can reach with `sut.Mock.ConstructorArgument<IChocolateDispenser>()`.

Test parameters match constructor parameters by exactly their type, not by a base type or an interface, and one test parameter fills every constructor parameter of its type. When the test method has several parameters of the same type, a constructor parameter gets the one with its name:

```csharp
public class ChocolateShop(IChocolateDispenser dark, IChocolateDispenser white)
{
    public bool Buy(string type, int amount)
        => (type == "White" ? white : dark).Dispense(type, amount);
}

[Test]
[AutoMockArguments]
public async Task Buy_WhiteChocolate_ShouldUseTheWhiteDispenser(ChocolateShop sut,
    IChocolateDispenser dark, IChocolateDispenser white)
{
    sut.Buy("White", 2);

    await That(white.Mock.Verify.Dispense("White", 2)).Once();
    await That(dark.Mock.Verify.Dispense(It.IsAny<string>(), It.IsAny<int>())).Never();
}
```

A test parameter of a value type or `string` is only passed to a constructor parameter with its name.

## Pass values

The attribute takes values for the first parameters of the test method. Each attribute is one test case:

```csharp
[Test]
[AutoMockArguments("Dark", 4)]
[AutoMockArguments("White", 2)]
public async Task Buy_ShouldDispenseTheRequestedChocolate(string type, int amount,
    VendingMachine sut, IChocolateDispenser dispenser)
{
    sut.Buy(type, amount);

    await That(dispenser.Mock.Verify.Dispense(type, amount)).Once();
}
```

The values are only passed to the test method. A class under test does not receive them as constructor arguments, even when the names match.

## Take a value from a member

The values of the attribute must be constants, so an object such as a configuration with `required` or `init` properties cannot be passed. Mark the parameter with `[ValueFrom]` and name a static property or method of the test class that returns it:

```csharp
public sealed class Discount
{
    public required string Type { get; init; }
    public required decimal Percentage { get; init; }
}

public interface IChocolatePrices
{
    decimal GetPrice(string type);
}

public class ChocolateCashier(Discount discount, IChocolatePrices prices)
{
    public decimal GetPrice(string type)
        => type == discount.Type ? prices.GetPrice(type) * (100 - discount.Percentage) / 100 : prices.GetPrice(type);
}

[Test]
[AutoMockArguments]
public async Task GetPrice_ShouldApplyTheDiscount(ChocolateCashier sut, IChocolatePrices prices,
    [ValueFrom(nameof(TenPercentOffDark))] Discount discount)
{
    prices.Mock.Setup.GetPrice(discount.Type).Returns(20m);

    await That(sut.GetPrice(discount.Type)).IsEqualTo(18m);
}

private static Discount TenPercentOffDark => new() { Type = "Dark", Percentage = 10m };
```

- The member is a static property or a static method without parameters of the test class or one of its base classes. Mockolate reads it once per test case, so a property or method that creates the value gives each test case its own instance. A field is not supported, because it would share one instance with all test cases.
- The class under test receives the value like a parameter that Mockolate creates. Combine it with [`[NotShared]`](#keep-a-mock-out-of-the-class-under-test) to keep it from the class under test, or with [`[AsDefault]`](#return-the-same-instance-from-every-mock) to let the mocks return it.
- The values of the attribute still fill the first parameters, so a `[ValueFrom]` parameter comes after them.

The [Mockolate0013](10-analyzers.md#mockolate0013) analyzer reports a member that does not exist, is not static, is a field or has a type that is not assignable to the parameter.

## Provide a value with an attribute

To reuse how a value is made across test classes, write an attribute that implements `IParameterValueProvider` and put it on the parameter. Mockolate calls `ProvideValue` with the parameter, so the attribute can use its type or name:

```csharp
using System.Reflection;

[ProvidesValuesFor(typeof(Discount))]
public sealed class DiscountedAttribute(string type, int percentage) : Attribute, IParameterValueProvider
{
    public object? ProvideValue(ParameterInfo parameter) => new Discount { Type = type, Percentage = percentage };
}

[Test]
[AutoMockArguments]
public async Task GetPrice_ShouldApplyTheDiscount(ChocolateCashier sut, IChocolatePrices prices,
    [Discounted("Dark", 10)] Discount discount)
{
    prices.Mock.Setup.GetPrice(discount.Type).Returns(20m);

    await That(sut.GetPrice(discount.Type)).IsEqualTo(18m);
}
```

- Mockolate calls the attribute once per test case. The class under test receives the value like a `[ValueFrom]` value, so [`[NotShared]`](#keep-a-mock-out-of-the-class-under-test) and [`[AsDefault]`](#return-the-same-instance-from-every-mock) work the same way.
- `[ProvidesValuesFor]` declares the types of the values. It is optional, but without it the analyzer cannot check the parameter type. An attribute that serves several types lists them all, e.g. `[ProvidesValuesFor(typeof(int), typeof(long))]`, and an open generic type such as `typeof(List<>)` stands for all of its constructed types.
- A parameter has at most one value provider or `[ValueFrom]`, and the values of `[AutoMockArguments]` still fill the first parameters.

The [Mockolate0014](10-analyzers.md#mockolate0014) analyzer reports a parameter whose type cannot hold any of the declared types, a parameter with more than one value provider, and a value provider on a `[Deferred]` parameter or on a parameter that gets a value of `[AutoMockArguments]`.

## Keep a mock out of the class under test

Mark a parameter with `[NotShared]` to get a mock that the class under test does not receive:

```csharp
[Test]
[AutoMockArguments]
public async Task Buy_ShouldNotUseAnotherDispenser(VendingMachine sut, [NotShared] IChocolateDispenser other)
{
    sut.Buy("Dark", 4);

    await That(other.Mock.Verify.Dispense(It.IsAny<string>(), It.IsAny<int>())).Never();
}
```

## Return the same instance from every mock

Without a setup, a member of a mock that returns an interface returns `null`, and a member like `Task<IChocolateDispenser> GetDispenserAsync()` returns a task with a `null` result. A class under test that uses the result fails with a `NullReferenceException`.

Mark a parameter with `[AsDefault]` to make it the [default value](03-create-mocks.md#default-values) of its type for all mocks of the test case. Their properties, methods and indexers without a setup return it, also wrapped in a `Task<T>` or `ValueTask<T>`, and so do their `out` parameters:

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

- A setup, a value written to a property or indexer, and [`ThrowingWhenNotSetup()`](03-create-mocks.md#customizing-mock-behavior) still take precedence.
- The parameter wins over the default values of the [behavior](#choose-the-mock-behavior) for its type.
- It only replaces the default value of its exact type, e.g. not of an interface that `IChocolateDispenser` implements, unless you [name the interface](#use-a-fake-for-an-interface).
- Every member of the type returns the same instance, also a method that creates a new one in the real implementation or an indexer for every key. Set up such a member when it needs other instances.
- The parameter is still passed to the constructor of a class under test.

Members that return another interface return an [auto-mock](03-create-mocks.md#auto-mocking) instead of `null`, so keep `[AsDefault]` for the instances that the test needs.

## Use a fake for an interface

To use a hand-written fake or another test double for an interface that it implements, name the interface in `[AsDefault<T>]`:

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

The parameter is then used for its own type and for each named type: constructor parameters of these types receive it, and it is the [default value](#return-the-same-instance-from-every-mock) of these types for all mocks of the test case. Name up to four types, e.g. `[AsDefault<IChocolateDispenser, IDisposable>]`, or pass any number of types as arguments: `[AsDefault(typeof(IChocolateDispenser))]`.

:::info[.NET 7 or later]

The generic `[AsDefault<T>]` needs .NET 7 or later, because .NET Framework cannot read generic attributes. On .NET Framework, pass the types as arguments.

:::

When another test parameter has a named type, a constructor parameter of that type gets the one with its name, like with [several test parameters of a type](#how-the-class-under-test-receives-the-parameters). The [Mockolate0011](10-analyzers.md#mockolate0011) analyzer reports a parameter that is not assignable to a named type, and two `[AsDefault]` parameters for the same type.

## Create the class under test in the test

Mockolate creates the parameters before the test runs, so a constructor that reads from its dependencies only sees mocks without setups. Mark a `Func<T>` parameter with `[Deferred]` to create the class under test after your setups instead:

```csharp
public interface IChocolateStock
{
    int Count(string type);
}

public class ChocolateDisplay
{
    public ChocolateDisplay(IChocolateStock stock)
    {
        IsSoldOut = stock.Count("Dark") == 0;
    }

    public bool IsSoldOut { get; }
}

[Test]
[AutoMockArguments]
public async Task IsSoldOut_WhenDarkChocolateIsInStock_ShouldBeFalse([Deferred] Func<ChocolateDisplay> createSut,
    IChocolateStock stock)
{
    stock.Mock.Setup.Count("Dark").Returns(5);

    ChocolateDisplay sut = createSut();

    await That(sut.IsSoldOut).IsFalse();
}
```

Each call creates a new instance, exactly like a parameter of type `T`: a class under test receives the other test parameters as its constructor arguments, and the `[AsDefault]` parameters and the [behavior](#choose-the-mock-behavior) apply. An exception of the constructor reaches the test, so you can assert it with `await That(() => createSut()).Throws<InvalidOperationException>()`. `T` can be any type that a parameter can have, e.g. `[Deferred] Func<IChocolateStock>` creates a new mock on each call.

The test framework does not dispose what the delegate creates, so dispose it in the test, e.g. with `using ChocolateDisplay sut = createSut();`.

The delegate is not passed to the constructor of a class under test. Without `[Deferred]`, a `Func<T>` parameter gets a [default value](03-create-mocks.md#default-values) like other framework types. The [Mockolate0012](10-analyzers.md#mockolate0012) analyzer reports a `[Deferred]` parameter that is not a `Func<T>`, or that is also `[AsDefault]` or `[NotShared]`.

## Choose the mock behavior

Assign `AutoMockArgumentsAttribute.DefaultBehavior` to choose the [behavior](03-create-mocks.md#customizing-mock-behavior) of the mocks and the default values for all test cases of the project. Assign it in a module initializer, so that it is set before the first test case is created. For example, to fill the other types with a [default value generator](03-create-mocks.md#default-values) backed by a library like AutoFixture:

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

Here `AutoFixtureGenerator` stands for your own implementation of `IDefaultValueGenerator`.

## Framework notes

TUnit needs nothing beyond the [setup](#set-up-your-test-framework). The other frameworks differ as follows.

### xUnit

- With v3, the attribute takes the settings of xUnit's data attributes, such as `Skip`, `Explicit`, `Timeout` and `Traits`. A project that references both v2 and v3 gets the attribute for v3.
- A theory with mocks shows up as one test in the test explorer, with a result for each attribute. xUnit v2 may already create the arguments of the first attribute when it discovers the tests.
- xUnit describes the arguments in the name of a test case and disposes them after the test. To describe a mock of a collection interface, such as `ISet<T>`, it may enumerate the mock, and a mock of a disposable interface gets a call to `Dispose`. Both calls count as interactions of the mock. On a strict mock, set up `Dispose`, or xUnit reports a cleanup error.

### NUnit

- Mockolate creates the arguments after the `[SetUp]` methods, and `[Repeat]` and `[Retry]` give each run new mocks. With NUnit 4, a last `CancellationToken` parameter gets the token of `[CancelAfter]`.
- The name of a test case only shows the given values, e.g. `Buy_ShouldDispenseTheRequestedChocolate("Dark", 4)`.
- The attribute has none of the settings of `[TestCase]`, such as `ExpectedResult` or `Ignore`. Use NUnit's attributes like `[Category]` or `[Ignore]` on the test method instead.
- A generic test method works when the given values determine its type arguments.
- [`[ValueFrom]`](#take-a-value-from-a-member) is not NUnit's `[ValueSource]`: it takes one value for the parameter in each test case instead of a test case for each element of a collection.
- With NUnit 4.5 or later, the `BeforeTestHook` and `AfterTestHook` of an execution hook on the test fixture or the assembly do not run for these tests. Put the hook attribute on the test method instead, but not between two hook attributes: place `[AutoMockArguments]` before or after all of them.

### MSTest

- With MSTest 4, set the unfolding strategy described in [Set up your test framework](#set-up-your-test-framework).
- The name of a test case only shows the given values, e.g. `Buy_ShouldDispenseTheRequestedChocolate("Dark", 4)`.
- Mockolate recognizes MSTest 3 only through its NuGet package, because earlier versions use the same assembly name.

<details>
<summary>Details and limitations</summary>

- Set `<MockolateTestFrameworkIntegration>false</MockolateTestFrameworkIntegration>` in the project file to skip the attribute.
- A constructor parameter of a delegate or abstract class type only gets a mock from a test parameter of that type.
- With TUnit, a [`[ValueFrom]`](#take-a-value-from-a-member) parameter does not work in a generic test class, because TUnit does not pass the type arguments of the class to the attribute.
- When two classes under test depend on each other, only the first of the two test parameters receives the other one; the second gets a new instance or its default value.
- The given values must have the types of the parameters, e.g. `4` for an `int` and `4L` for a `long`. `[AutoMockArguments(null)]` passes `null` to the first parameter. A single array, e.g. `[AutoMockArguments(new object[] { "Dark", 4 })]` or `[AutoMockArguments(new[] { "Dark", "White" })]`, passes its elements as the values; to pass the array itself, wrap it, e.g. `[AutoMockArguments(new object[] { new[] { "Dark", "White" } })]`.
- On .NET Framework and .NET Standard, the attribute needs a `ModuleInitializerAttribute`. When the build reports it as missing or declared twice, see [Troubleshooting](13-troubleshooting.md#the-build-fails-because-of-moduleinitializerattribute).

</details>
