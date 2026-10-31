---
description: Initialize properties, set up return values and register getter and setter callbacks on a Mockolate mock.
---

# Properties

Without a setup, a property of a mock behaves like an auto-property: the getter returns the value that was last
written, or the [default value](../04-create-mocks.md#default-values). Use `dispenser.Mock.Setup.PropertyName` to
change this.

## Initialization

Use `InitializeWith` to give the property a start value. It keeps behaving like a normal property, so the setter
changes the value and the getter returns the last value set:

```csharp
dispenser.Mock.Setup.TotalDispensed.InitializeWith(42);
```

On a mock created with [`ThrowingWhenNotSetup()`](../04-create-mocks.md#customizing-mock-behavior), every property
access without a setup throws. Use `Register()` to allow access without providing a value:

```csharp
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

// Register the property without a value, so that accessing it does not throw
dispenser.Mock.Setup.TotalDispensed.Register();
```

## Returns / Throws

Set up the getter with `Returns` and `Throws`. Multiple calls define a
[sequence](01-methods.md#sequences) that starts over after the last entry:

```csharp
dispenser.Mock.Setup.TotalDispensed
    .Returns(1)
    .Returns(2)
    .Throws(new Exception("Error"))
    .Returns(4);
```

You can also compute the value from the current value:

```csharp
dispenser.Mock.Setup.TotalDispensed
    .Returns(current => current + 10);  // Increment by 10 each read
```

## Callbacks

Register callbacks on the getter with `OnGet` and on the setter with `OnSet`. They can be parameterless or receive
the value. Chain them on the same setup, because a second `dispenser.Mock.Setup.TotalDispensed` statement would
[replace the first one](/Mockolate/core-concepts/#what-happens-when-the-mock-is-called):

```csharp
dispenser.Mock.Setup.TotalDispensed
    .OnGet.Do(value => Console.WriteLine($"TotalDispensed was read: {value}"))
    .OnSet.Do(newValue => Console.WriteLine($"TotalDispensed changed to {newValue}"));
```

Multiple callbacks form a sequence, like `Returns` and `Throws`:

```csharp
dispenser.Mock.Setup.TotalDispensed
    .OnGet.Do(() => Console.WriteLine("Runs on the 1st, 3rd, 5th, … read"))
    .OnGet.Do(() => Console.WriteLine("Runs on the 2nd, 4th, 6th, … read"));
```

## Class mocks

Use `.SkippingBaseClass()` to prevent calls to the base class implementation for a single property, or
[`MockBehavior.SkippingBaseClass()`](../04-create-mocks.md#customizing-mock-behavior) for the whole mock.

## Properties with only one accessor

The setup and verify members only offer the accessors that the mock actually intercepts: a get-only property has no
`OnSet`, and a set-only property has neither `OnGet`, `Returns`, `Throws` nor `InitializeWith`. `Register()` and
`SkippingBaseClass()` are available on both.

```csharp
public interface IChocolateInventory
{
    int RemainingBars { get; }        // no setter
    string LastCountedBy { set; }     // no getter
}

IChocolateInventory inventory = IChocolateInventory.CreateMock();

inventory.Mock.Setup.RemainingBars.Returns(3);
inventory.Mock.Setup.LastCountedBy.OnSet.Do(value => Console.WriteLine(value));

inventory.Mock.Verify.RemainingBars.Got().Once();
inventory.Mock.Verify.LastCountedBy.Set("Ada").Once();

// ❌ These do not compile:
// inventory.Mock.Setup.RemainingBars.OnSet
// inventory.Mock.Setup.LastCountedBy.Returns("Ada")
// inventory.Mock.Verify.RemainingBars.Set(3)
// inventory.Mock.Verify.LastCountedBy.Got()
```

The restriction also holds for the builders returned by `Returns`, `Throws`, `Do` and `TransitionTo`, so chaining never
reaches an accessor that the mock does not intercept.

<details>
<summary>Accessors that the mock cannot see</summary>

The same applies when a property declares an accessor that your test assembly cannot see, such as
`{ get; internal set; }` on a type from an assembly that does not grant `InternalsVisibleTo`. Writes never reach the
mock in that case, so only the getter is offered. See [Mockolate0002](../10-analyzers.md#mockolate0002) for when such
a type is mockable at all.

</details>

:::tip

All callbacks and return values support conditional execution, repetition, parallel callbacks and access to the
invocation counter. See [Advanced callback features](../08-advanced-features/03-advanced-callback-features.md).

:::
