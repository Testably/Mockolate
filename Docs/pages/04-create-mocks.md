---
description: Create mocks of interfaces and classes, customize their default behavior, add interfaces and wrap real instances.
---

# Create mocks

You can create mocks of interfaces, classes and [delegates](09-special-types/02-delegates.md). The source generator adds
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
| `AutoMocking()`                       | off               | Members that return an interface return a mock instead of `null`; needs the `MockolateAutoMocking` build property (see [Auto-mocking](#auto-mocking)).                         |
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

To match more than one exact type, or to use the arguments of the call, pass a `DefaultValueFactory` with a type predicate and a generator that receives the type and the arguments:

```csharp
MockBehavior behavior = MockBehavior.Default
    .WithDefaultValueFor(new DefaultValueFactory(
        type => type == typeof(int) || type == typeof(long),
        (type, _) => Convert.ChangeType(-1, type)));
```

For full control, implement `IDefaultValueGenerator` and pass it to the `MockBehavior(IDefaultValueGenerator)` constructor, or replace the generator of an existing behavior with `.WithDefaultValueGenerator(…)`. Factories registered with `WithDefaultValueFor` are kept and take precedence over the generator, in whichever order you call the methods.

### Auto-mocking

With `AutoMocking()`, a method, property, indexer or delegate that returns an interface returns a mock of that interface instead of `null` when no setup and no default value factory provides a value. This lets the code under test walk a chain like `context.Services.GetRepository().Find(id)` without a setup for every link.

Auto-mocking needs mocks of the returned interfaces, so enable it in the project file first. The source generator then also creates mocks for the interfaces that the members of your mocks return, transitively:

```xml
<PropertyGroup>
    <MockolateAutoMocking>true</MockolateAutoMocking>
</PropertyGroup>
```

Then opt in per mock with the behavior:

```csharp
IShop shop = IShop.CreateMock(MockBehavior.Default.AutoMocking());

IChocolateDispenser dispenser = shop.GetDispenser();
dispenser.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>()).Returns(true);

await That(shop.GetDispenser().Dispense("Dark", 5)).IsTrue();
```

- Each method, property and delegate creates its auto-mock once and then keeps returning it, independent of the arguments, so you can configure and verify it through its own `Mock` property after the first call. An indexer stores one auto-mock per key, like any other value it returns.
- The auto-mock uses the same `MockBehavior`, so its own interface members are auto-mocked as well, and `Initialize<T>(…)` applies to it.
- A method or property that returns a `Task<T>` or `ValueTask<T>` of an interface returns a completed task with an auto-mock. On .NET Framework and .NET Standard, a `ValueTask<T>` keeps returning `default`, like every other un-configured `ValueTask<T>` there.
- A setup, a `WithDefaultValueFor` factory and the base class implementation of a class mock take precedence. A factory that returns `null` counts as no value, so the member returns an auto-mock instead. `ThrowingWhenNotSetup()` still throws.
- Without the `MockolateAutoMocking` property, `AutoMocking()` has no effect, and the [Mockolate0005](10-analyzers.md#mockolate0005) analyzer warns about it. The property applies to the project whose `CreateMock` creates the mock: a behavior with `AutoMocking()` that is passed to mocks created in a project without the property returns no auto-mocks.
- An interface that members return in several spellings, e.g. `IRepository<string?>` and `IRepository<string>`, gets one auto-mock without nullable annotations and tuple element names. Its `Mock` property accepts every spelling.

The generator does not create auto-mocks for:

- interfaces in the global namespace or in the `System`, `Microsoft` and `Windows` namespaces and their sub-namespaces, to avoid generating mocks for large parts of the framework. This also applies to other packages in these namespaces, e.g. `IFileSystem` of `System.IO.Abstractions`; set up such members yourself or register a factory with `WithDefaultValueFor`,
- interfaces that the project cannot access, that are obsolete, that still contain a type parameter of a generic method, that have static abstract members or whose members use such a type, a pointer or a ref struct other than `Span<T>` and `ReadOnlySpan<T>`,
- `out` parameters, interfaces inside a tuple such as `(IFoo, IBar)`, indexers with ref struct keys, indexers that return a task, and members that the mock cannot override or that have no getter.

A member that returns such an interface still returns `null`.

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
