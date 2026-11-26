---
description: Create mocks of interfaces and classes, customize their default behavior, add interfaces and wrap real instances.
---

# Create mocks

You can create mocks of interfaces, classes and [delegates](09-special-types/01-delegates.md). The source generator adds
a static `CreateMock()` method to every type you mock.

<details>
<summary>Example types used on this page</summary>

Besides [`IChocolateDispenser`](/Mockolate/core-concepts/#mocks-and-the-system-under-test), this page uses:

```csharp
public abstract class ChocolateDispenser
{
    public abstract bool Dispense(string type, int amount);
}

public class MyChocolateDispenser : IChocolateDispenser
{
    // a real implementation of IChocolateDispenser
}

public interface ILemonadeDispenser
{
    bool DispenseLemonade(int amount);
}

public interface IShop
{
    IChocolateDispenser GetDispenser();
}
```

</details>

```csharp
// Create a mock of an interface
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();

// Create a mock of a class
ChocolateDispenser classMock = ChocolateDispenser.CreateMock();
```

For classes whose constructors need arguments, pass the constructor arguments as an array:

```csharp
public abstract class ChocolateDispenserWithCtor(string defaultType, int capacity)
{
    public abstract bool Dispense(string type, int amount);
}

ChocolateDispenserWithCtor classMock = ChocolateDispenserWithCtor.CreateMock(["Dark", 42]);
```

<details>
<summary>Constructors with optional or params parameters</summary>

Without constructor arguments, `CreateMock()` calls the constructor that `new T()` would call. Besides a parameterless constructor, this can be one whose parameters are all optional, or one that ends with a `params` array or collection, such as `params ReadOnlySpan<T>` or `params List<T>`. Mockolate passes the declared default values and an empty collection:

```csharp
public abstract class ChocolateBox(int capacity = 12, params string[] types)
{
    public abstract bool Contains(string type);
}

ChocolateBox box = ChocolateBox.CreateMock(); // calls ChocolateBox(12, [])
```

When several constructors qualify, Mockolate follows the C# rules: a parameterless constructor wins, then one whose parameters are all optional, then a `params` constructor. Among `params` constructors, one that needs no default values wins, and then the one with the more specific `params` type, for example a `ReadOnlySpan<T>` over a `T[]`, or a `string[]` over an `object[]`. Where C# reports `new T()` as ambiguous, Mockolate picks the first declared of the ambiguous constructors instead.

</details>

:::note

A class mock overrides the `virtual` and `abstract` members of the class. Non-virtual members cannot be intercepted
and always run the real implementation. Sealed classes cannot be mocked; the
[Mockolate0002](10-analyzers.md#mockolate0002) analyzer reports this at compile time.

:::

## Setups during creation

You can pass the setups directly to `CreateMock`. These setups are applied before the constructor of a class mock runs,
so they also apply to virtual members that the constructor calls:

```csharp
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock(setup =>
{
    setup.Dispense(It.IsAny<string>(), It.IsAny<int>()).Returns(true);
    setup.TotalDispensed.InitializeWith(0);
});
```

The setup callback can be combined with a `MockBehavior` and with constructor arguments in the same call.

## Customizing mock behavior

A `MockBehavior` controls what the mock does when no setup matches. `MockBehavior.Default` returns default values and
calls the base class implementation of class mocks (see [Core concepts](/Mockolate/core-concepts/#what-happens-when-the-mock-is-called)).
Change it with the fluent `-ing` methods:

```csharp
// Throw a MockNotSetupException for every call without a matching setup
IChocolateDispenser strictMock = IChocolateDispenser.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

// Combine a behavior with constructor arguments
MockBehavior behavior = MockBehavior.Default.ThrowingWhenNotSetup();
ChocolateDispenserWithCtor classMock = ChocolateDispenserWithCtor.CreateMock(behavior, ["Dark", 42]);
```

`MockBehavior` is an immutable record, so every method returns a new instance and you can safely share one behavior
between many mocks and tests:

```csharp
MockBehavior strict = MockBehavior.Default.ThrowingWhenNotSetup();

IChocolateDispenser dispenser1 = IChocolateDispenser.CreateMock(strict);
ILemonadeDispenser dispenser2 = ILemonadeDispenser.CreateMock(strict);
```

The flags can also be set with a `with` expression, e.g. `MockBehavior.Default with { SkipBaseClass = true, ThrowWhenNotSetup = true }`.

| Option                                | Default           | Purpose                                                                                                                                                                         |
|---------------------------------------|-------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ThrowingWhenNotSetup()`              | off               | Throws a `MockNotSetupException` when no matching setup is found, instead of returning a default value.                                                                         |
| `SkippingBaseClass()`                 | off               | Class mocks no longer call the base class implementation; without a setup they return a default value instead.                                                                  |
| `SkippingInteractionRecording()`      | off               | Interactions are not recorded. Setups, return values, callbacks and base class calls still work, but every verification throws a `MockException`. Useful in performance-sensitive scenarios. |
| `SkippingAutoMocking()`               | off               | Members that return an interface return `null` instead of an auto-mock in projects with the `MockolateAutoMocking` build property (see [Auto-mocking](#auto-mocking)).        |
| `WithDefaultValueFor<T>(…)`           | sensible defaults | Customizes the default value for type `T` (see below).                                                                                                                          |
| `Initialize<T>(…)`                    | -                 | Applies the given setups to every mock of type `T` created with this behavior.                                                                                                  |
| `UseConstructorParametersFor<T>(…)`   | -                 | Default constructor arguments for mocks of class `T`, used unless you pass explicit arguments to `CreateMock([…])`. The `Func<object?[]>` overload creates them anew for each mock. |

### Default values

When no setup matches, the mock returns:

- an empty collection for collection types (e.g. `IEnumerable<T>`, `List<T>`),
- an empty sequence for `IAsyncEnumerable<T>`, whose enumeration throws an `OperationCanceledException` when the first `CancellationToken` among the arguments is canceled,
- an empty string for `string`,
- a completed task for `Task`, `Task<T>`, `ValueTask` and `ValueTask<T>`,
- a tuple with recursively defaulted values for value tuples and `Tuple<…>`, except for an eight element `Tuple<…>` whose last element is no `Tuple<…>`,
- a function that returns its argument for `Func<T, T>` and an action that does nothing for `Action<T>`, so that an options callback like `Func<ChocolateOptions, ChocolateOptions>` keeps the default options,
- `default(T)` for value types and `null` for other reference types.

You can register custom factories per type using `.WithDefaultValueFor<T>()`:

```csharp
MockBehavior behavior = MockBehavior.Default
    .WithDefaultValueFor<string>(() => "default")
    .WithDefaultValueFor<int>(() => 42);
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock(behavior);
```

To match more than one exact type, or to use the arguments of the call, pass a `DefaultValueFactory` with a type predicate and a generator that receives the type and the arguments:

```csharp
MockBehavior behavior = MockBehavior.Default
    .WithDefaultValueFor(new DefaultValueFactory(
        type => type == typeof(int) || type == typeof(long),
        (type, _) => Convert.ChangeType(-1, type)));
```

A factory that matches a tuple type provides the whole tuple instead of the tuple of default values. This applies to `.WithDefaultValueFor<(string Type, int Amount)>(() => ("Dark", 1))` and to a `DefaultValueFactory` whose predicate matches the tuple type, e.g. `type => type.IsValueType`.

For full control, implement `IDefaultValueGenerator` and pass it to the `MockBehavior(IDefaultValueGenerator)` constructor, or replace the generator of an existing behavior with `.WithDefaultValueGenerator(…)`. Factories registered with `WithDefaultValueFor` are kept and take precedence over the generator, in whichever order you call the methods.

### Auto-mocking

With auto-mocking, a method, property, indexer or delegate that returns an interface returns a mock of that interface instead of `null` when no setup and no default value factory provides a value. This lets the code under test walk a chain like `context.Services.GetRepository().Find(id)` without a setup for every link.

Enable it in the project file. The source generator then also creates mocks for the interfaces that the members of your mocks return, transitively:

```xml
<PropertyGroup>
    <MockolateAutoMocking>true</MockolateAutoMocking>
</PropertyGroup>
```

Every mock that the project creates then returns them:

```csharp
IShop shop = IShop.CreateMock();

IChocolateDispenser dispenser = shop.GetDispenser();
dispenser.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>()).Returns(true);

await That(shop.GetDispenser().Dispense("Dark", 5)).IsTrue();
```

- A member keeps returning the same auto-mock, so you can configure and verify it through its `Mock` property after the first call. An indexer keeps one auto-mock per key.
- The auto-mock uses the same `MockBehavior`, so its own members are auto-mocked as well.
- Setups and `WithDefaultValueFor` factories take precedence over auto-mocks.
- Opt out per mock with `MockBehavior.Default.SkippingAutoMocking()`.

Interfaces in the `System`, `Microsoft` and `Windows` namespaces are not auto-mocked. A mocked type from a large SDK can pull in mocks for much of the SDK, so exclude such namespaces with a `;`-separated list. An interface in a namespace of this list is still auto-mocked when the project creates a mock of it with `CreateMock`:

```xml
<PropertyGroup>
    <MockolateAutoMocking>true</MockolateAutoMocking>
    <MockolateAutoMockingExcludedNamespaces>Azure;Amazon.S3</MockolateAutoMockingExcludedNamespaces>
</PropertyGroup>
```

To auto-mock the interfaces of some namespaces anyway, list them as exceptions in `MockolateAutoMockingIncludedNamespaces`. An included namespace wins over the excluded namespaces and over `System`, `Microsoft` and `Windows`. By default, the list contains `System.IO.Abstractions`, so that the file system interfaces of Testably.Abstractions and TestableIO.System.IO.Abstractions are auto-mocked. To keep this default, add your namespaces to `$(MockolateAutoMockingIncludedNamespaces)` in the project file:

```xml
<PropertyGroup>
    <MockolateAutoMocking>true</MockolateAutoMocking>
    <MockolateAutoMockingIncludedNamespaces>$(MockolateAutoMockingIncludedNamespaces);Microsoft.Extensions.Logging</MockolateAutoMockingIncludedNamespaces>
</PropertyGroup>
```

To turn the default off, set `MockolateAutoMockingIncludedNamespaces` to an empty value in the project file. Adding `System.IO.Abstractions` to the excluded namespaces has no effect, because the included namespace wins.

Both lists also apply to the nested namespaces, e.g. listing `Sdk` also covers `Sdk.Storage`.

<details>
<summary>Details and limitations</summary>

- A method or property that returns a `Task<T>` or `ValueTask<T>` of an interface returns a completed task with an auto-mock. On .NET Framework and .NET Standard, a `ValueTask<T>` keeps returning `default`.
- A member that returns a tuple, e.g. `(int Stock, IChocolateDispenser Dispenser)` or `Tuple<int, IChocolateDispenser>`, returns an auto-mock for each interface element, also in a nested tuple and in a tuple wrapped in a `Task<T>` or `ValueTask<T>`. Each element has its own auto-mock, also when two elements have the same type. A `WithDefaultValueFor` factory for the tuple type takes precedence over the elements. A nullable value tuple, e.g. `(int, IChocolateDispenser)?`, returns `null`.
- The base class implementation of a class mock also takes precedence, and `ThrowingWhenNotSetup()` still throws. A `WithDefaultValueFor` factory that returns `null` counts as no value, so the member returns an auto-mock instead.
- The auto-mock is created when the member computes its default value, so `Initialize<T>(…)` can run for an auto-mock that the member never returns, e.g. when a setup provides the value of a property.
- The property applies to the project whose `CreateMock` creates the mock: mocks created in a project without the property return no auto-mocks.
- `Directory.Build.props` is read before the Mockolate package sets the default included namespaces, so `$(MockolateAutoMockingIncludedNamespaces)` is still empty there: a list set there replaces the default, and an empty value there does not turn the default off. Set the list in the project file or in `Directory.Build.targets` instead.
- An interface that members return in several spellings, e.g. `IRepository<string?>` and `IRepository<string>`, gets one auto-mock whose `Mock` property accepts every spelling, unless the project creates a mock of one spelling with `CreateMock`.
- The generator skips interfaces whose mock would not compile cleanly, e.g. interfaces that are inaccessible, obsolete or experimental, nested in a generic type, have static abstract members or a member named `CreateMock`, or use pointers or unsupported ref structs. It also skips interfaces in the global namespace, `out` parameters and indexers that return a task. Such members still return `null`, or a task with a `null` result; set them up yourself or register a factory with `WithDefaultValueFor`.
- With the property, the generator discovers the auto-mocks again when you edit a file that declares one of the mocked or returned types, so with many mocked interfaces the IDE may take longer to update the generated code after such an edit. Edits in other files, e.g. in your tests, reuse the auto-mocks.

</details>

## Implementing additional interfaces

Use `.Implementing<T>()` to create a mock that also implements further interfaces:

```csharp
// A ChocolateDispenser that also implements ILemonadeDispenser
ChocolateDispenser dispenser = ChocolateDispenser.CreateMock().Implementing<ILemonadeDispenser>();

// With setups for the additional interface
IChocolateDispenser dispenser2 = IChocolateDispenser.CreateMock()
    .Implementing<ILemonadeDispenser>(setup => setup.DispenseLemonade(It.IsAny<int>()).Returns(true));
```

Use `Mock.As<T>()` to reach the `Setup` and `Verify` members of the additional interface:

```csharp
dispenser.Mock.As<ILemonadeDispenser>().Setup.DispenseLemonade(It.IsAny<int>()).Returns(true);
dispenser.Mock.As<ILemonadeDispenser>().Verify.DispenseLemonade(5).Once();
```

When the mock is already typed as the additional interface, its own `Mock` property reaches the same members, so you
can skip `As<T>()`:

```csharp
((ILemonadeDispenser)dispenser).Mock.Setup.DispenseLemonade(It.IsAny<int>()).Returns(true);
```

All interfaces share the same mock: recorded interactions, scenario state and setups apply to every view of it.

:::warning

- `.Implementing<T>()` returns a **new** mock instead of changing the one it was called on. Keep the returned
  reference; the original one does not implement `T`.
- Only the first type can be a class; additional types must be interfaces.
- `Mock.As<T>()` throws a `MockException` when the mock does not implement `T`, for example because it was not
  created with `.Implementing<T>()`.

:::

On class mocks, `.Implementing<T>()` reuses the constructor arguments the mock was created with.

<details>
<summary>When the class already implements the interface</summary>

`.Implementing<T>()` is also useful when the mocked class itself implements `T`, because it makes non-virtual members
that the class implements mockable through the interface:

```csharp
public interface ICalculator
{
    int Add(int a, int b);
    int Multiply(int a, int b);
}

public abstract class Calculator : ICalculator
{
    public int Add(int a, int b) => a + b;          // not virtual
    public abstract int Multiply(int a, int b);
}

Calculator calculator = Calculator.CreateMock().Implementing<ICalculator>();

// `Multiply` is overridable, so the class and the interface share one member:
// either surface configures it, and both calls are recorded against it.
calculator.Mock.Setup.Multiply(It.IsAny<int>(), It.IsAny<int>()).Returns(7);
int viaClass = calculator.Multiply(3, 4);
int viaInterface = ((ICalculator)calculator).Multiply(3, 4);

await That(viaClass).IsEqualTo(7);
await That(viaInterface).IsEqualTo(7);

// `Add` is not virtual, so only the call through the interface can be intercepted.
calculator.Mock.As<ICalculator>().Setup.Add(It.IsAny<int>(), It.IsAny<int>()).Returns(99);
int addViaClass = calculator.Add(1, 2);
int addViaInterface = ((ICalculator)calculator).Add(1, 2);

await That(addViaClass).IsEqualTo(3).Because("the class call runs the real implementation");
await That(addViaInterface).IsEqualTo(99);
```

</details>

<details>
<summary>Members hidden with <code>new</code></summary>

`Mock.As<T>()` also reaches a base interface whose members the mocked interface hides with `new`. No
`.Implementing<T>()` is needed, because the base interface is already part of the mocked type. Methods, properties and
events stay separate members, each with its own setups and recorded interactions:

```csharp
public interface IChocolateShelfBase
{
    int Restock();
}

public interface IChocolateShelf : IChocolateShelfBase
{
    new int Restock();
}

IChocolateShelf shelf = IChocolateShelf.CreateMock();
shelf.Mock.Setup.Restock().Returns(42);
shelf.Mock.As<IChocolateShelfBase>().Setup.Restock().Returns(43);

int viaDerived = shelf.Restock();
int viaBase = ((IChocolateShelfBase)shelf).Restock();

await That(viaDerived).IsEqualTo(42);
await That(viaBase).IsEqualTo(43);

shelf.Mock.Verify.Restock().Once();
shelf.Mock.As<IChocolateShelfBase>().Verify.Restock().Once();
```

Indexers are the exception: they are keyed by their parameter signature rather than by the declaring interface, so a
`new` indexer and the base indexer it hides share the same storage.

</details>

## Wrapping existing instances

Use `.Wrapping(instance)` to forward all calls to a real object while still recording them, like a spy:

```csharp
MyChocolateDispenser realDispenser = new MyChocolateDispenser();
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock().Wrapping(realDispenser);

// The call is forwarded to the real instance
dispenser.Dispense("Dark", 5);

// You can still verify interactions
dispenser.Mock.Verify.Dispense("Dark", 5).Once();
```

- Both interface and class types can be wrapped.
- Setups take precedence over the wrapped instance, so you can still override individual members.
- Interface members are forwarded through the interface that declares them. This also covers members that hide a base
  member with `new`, and same-named members declared by several base interfaces.
- Protected members are not forwarded; the base class implementation is used instead.
- Init-only properties are not forwarded, since the wrapped instance is already constructed.

## Create the class under test with mocks

`CreateMock()` on a class without a constructor that can be called without arguments fills the constructor with the most parameters. The result is a mock that forwards to the real implementation, so nothing changes until you set up a virtual member. Get the dependencies with `Mock.ConstructorArgument<T>()` to set them up and verify them:

```csharp
public class VendingMachine(IChocolateDispenser dispenser)
{
    public string Buy(string type, int amount)
        => dispenser.Dispense(type, amount) ? "Enjoy!" : "Sold out";
}

VendingMachine sut = VendingMachine.CreateMock();
IChocolateDispenser dispenser = sut.Mock.ConstructorArgument<IChocolateDispenser>();
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);

string result = sut.Buy("Dark", 4);

await That(result).IsEqualTo("Enjoy!");
dispenser.Mock.Verify.Dispense("Dark", 4).Once();
```

Every constructor parameter gets an argument, by the first rule that applies:

- A **mockable interface** gets a new mock with the same behavior, even when the parameter is optional or nullable.
- A **`params` parameter** gets an empty collection, and an **optional parameter** its declared default value.
- A **nullable parameter** gets `null`.
- **Any other parameter** gets a value from the [default values](#default-values) of the behavior: a value registered with `WithDefaultValueFor` or a default value generator, for example one backed by a library like AutoFixture, and otherwise `""` for a `string`, an empty collection, a callback that keeps the options for `Func<T, T>` or `Action<T>`, or the default of a value type. Without a value for a reference type, the creation fails with a `MockException`.

`ConstructorArgument<T>()` returns the argument of the parameter declared with type `T`, or else the only argument that converts to `T`. When several match, pass the name of the parameter: `sut.Mock.ConstructorArgument<IChocolateDispenser>("source")`.

To skip this arrange code in every test, declare the class under test and its dependencies as [parameters of the test method](04-mocks-as-test-parameters.md).

### Sealed classes and records

A sealed class or a record cannot be mocked. Pass an `out` parameter to `CreateMock` to create a **real instance** whose constructor receives mocks. The required `out` parameter shows that the result is not a mock, so it has no `Mock` property and its members cannot be set up:

```csharp
public sealed class VendingMachine(IChocolateDispenser dispenser, string location)
{
    public string Buy(string type, int amount)
        => dispenser.Dispense(type, amount) ? "Enjoy!" : "Sold out";
}

VendingMachine sut = VendingMachine.CreateMock(out ConstructorArguments arguments);
arguments.Get<IChocolateDispenser>().Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);

string result = sut.Buy("Dark", 4);

await That(result).IsEqualTo("Enjoy!");
```

The arguments are filled by the same rules. Pass your own by name, and the others are still filled, e.g. `VendingMachine.CreateMock(out _, location: "Lobby")`. Passing `null` for a mockable parameter gives it a new mock. A `MockBehavior` passed as `mockBehavior` applies to every new mock and to the default values.

<details>
<summary>Details and limitations</summary>

- A parameter is mockable when its type is an interface outside the global, `System`, `Microsoft` and `Windows` namespaces that the generator can mock without a `CreateMock` call of its own, by the same mockability rules as an auto-mock (see the limitations of [auto-mocking](#auto-mocking)). `ILogger`, `ILogger<T>` and `ILoggerFactory` are mockable too, because Mockolate has [dedicated mocks](09-special-types/03-logging.md) for them.
- A class with a constructor that can be called without arguments uses that constructor, so its optional parameters keep their defaults instead of getting mocks.
- When several constructors share the most parameters, `CreateMock()` fails with a `MockException`; pass the constructor parameters yourself. Constructors with `ref`, `out`, pointer or ref struct parameters and obsolete constructors are not filled.
- A class dependency is not created for you: it needs a [default value](#default-values) or an argument.
- An interface of the `System`, `Microsoft` or `Windows` namespaces, such as `IServiceProvider`, needs a [default value](#default-values) in `CreateMock()`, while `CreateMock(out ...)` creates a mock for it when the default values provide none.
- `CreateMock(out ...)` is available for sealed classes and records that your assembly can access, with a single accessible constructor with the most parameters that sets all `required` members. They must not be abstract, generic or nested in a generic type, obsolete, file-local, in the `System`, `Microsoft` or `Windows` namespaces, or have a member named `CreateMock` or inherit one that hides the generated one, and [Mockolate0002](10-analyzers.md#mockolate0002) reports the reason at the call. On a class that can be mocked, use `CreateMock()` and `Mock.ConstructorArgument<T>()`.
- The mocks of the constructor dependencies are shared with mocks that `CreateMock` creates of the same type. With [auto-mocking](#auto-mocking), the generator also creates the auto-mocks of the interfaces that the dependencies return.

</details>
