---
description: Create mocks of interfaces and classes, customize their default behavior, add interfaces and wrap real instances.
---

# Create mocks

You can create mocks of interfaces, classes and [delegates](special-types/02-delegates.md). The source generator adds
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
```

</details>

```csharp
// Create a mock of an interface
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();

// Create a mock of a class
ChocolateDispenser classMock = ChocolateDispenser.CreateMock();
```

For classes without a parameterless constructor, pass the constructor arguments as an array:

```csharp
public abstract class ChocolateDispenserWithCtor(string defaultType, int capacity)
{
    public abstract bool Dispense(string type, int amount);
}

ChocolateDispenserWithCtor classMock = ChocolateDispenserWithCtor.CreateMock(["Dark", 42]);
```

:::info

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

| Option                                | Default           | Purpose                                                                                                                                                                         |
|---------------------------------------|-------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ThrowingWhenNotSetup()`              | off               | Throws a `MockNotSetupException` when no matching setup is found, instead of returning a default value.                                                                         |
| `SkippingBaseClass()`                 | off               | Class mocks no longer call the base class implementation; without a setup they return a default value instead.                                                                  |
| `SkippingInteractionRecording()`      | off               | Interactions are not recorded. Setups, return values, callbacks and base class calls still work, but every verification throws a `MockException`. Useful in performance-sensitive scenarios. |
| `WithDefaultValueFor<T>(…)`           | sensible defaults | Customizes the default value for type `T` (see below).                                                                                                                          |
| `Initialize<T>(…)`                    | -                 | Applies the given setups to every mock of type `T` created with this behavior.                                                                                                  |
| `UseConstructorParametersFor<T>(…)`   | -                 | Default constructor arguments for mocks of class `T`, used unless you pass explicit arguments to `CreateMock([…])`. The `Func<object?[]>` overload creates them anew for each mock. |

### Default values

When no setup matches, the mock returns:

- an empty collection for collection types (e.g. `IEnumerable<T>`, `List<T>`),
- an empty string for `string`,
- a completed task for `Task`, `Task<T>`, `ValueTask` and `ValueTask<T>`,
- a tuple with recursively defaulted values for tuples,
- `default(T)` for value types and `null` for other reference types.

You can register custom factories per type using `.WithDefaultValueFor<T>()`:

```csharp
MockBehavior behavior = MockBehavior.Default
    .WithDefaultValueFor<string>(() => "default")
    .WithDefaultValueFor<int>(() => 42);
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock(behavior);
```

For full control, implement `IDefaultValueGenerator` and pass it to the `MockBehavior(IDefaultValueGenerator)`
constructor.

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
int viaClass = calculator.Multiply(3, 4);                    // 7
int viaInterface = ((ICalculator)calculator).Multiply(3, 4); // 7

// `Add` is not virtual, so only the call through the interface can be intercepted.
calculator.Mock.As<ICalculator>().Setup.Add(It.IsAny<int>(), It.IsAny<int>()).Returns(99);
int addViaClass = calculator.Add(1, 2);                      // 3, the real implementation
int addViaInterface = ((ICalculator)calculator).Add(1, 2);   // 99
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

int viaDerived = shelf.Restock();                       // 42
int viaBase = ((IChocolateShelfBase)shelf).Restock();   // 43

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
