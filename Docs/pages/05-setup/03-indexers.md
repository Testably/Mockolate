---
description: Initialize indexers, set up return values and register getter and setter callbacks on a Mockolate mock.
---

# Indexers

Indexers are set up like [properties](02-properties.md), but take a value or a
[parameter matcher](../06-parameter-matching.md) for each key. Without a setup, an indexer stores the values written
to it per key, and returns the [default value](../04-create-mocks.md#default-values) for keys that were never written.

```csharp
dispenser.Mock.Setup[It.IsAny<string>()]
    .InitializeWith(type => 20)
    .OnGet.Do((type, value) => Console.WriteLine($"Stock for {type} was read: {value}"));

dispenser.Mock.Setup["Dark"]
    .InitializeWith(10)
    .OnSet.Do((type, value) => Console.WriteLine($"Stock for {type} was set to {value}"));
```

## Initialization

Use `InitializeWith` to give the indexer a start value. It keeps behaving like a normal indexer, so the setter changes
the value and the getter returns the last value set for that key. The value can also be computed from the key:

```csharp
dispenser.Mock.Setup[It.IsAny<string>()].InitializeWith(42);
dispenser.Mock.Setup[It.IsAny<string>()].InitializeWith(type => type.Length);
```

## Returns / Throws

Set up the getter with `Returns` and `Throws`. Multiple calls define a [sequence](01-methods.md#sequences) that starts
over after the last entry:

```csharp
dispenser.Mock.Setup[It.IsAny<string>()]
    .Returns(1)
    .Returns(2)
    .Throws(new Exception("Error"))
    .Returns(4);
```

You can also compute the value from the key(s), or from the key(s) and the current value:

```csharp
dispenser.Mock.Setup[It.IsAny<string>()]
    .Returns(type => type.Length);

dispenser.Mock.Setup[It.IsAny<string>()]
    .Returns((type, current) => current + 10);  // Increment by 10 each read
```

## Callbacks

Register callbacks on the getter with `OnGet` and on the setter with `OnSet`. They can be parameterless, receive the
value, or receive the key(s) followed by the value. Chain them on the same setup, because a second setup with the same
matchers would [replace the first one](/Mockolate/core-concepts/#what-happens-when-the-mock-is-called):

```csharp
dispenser.Mock.Setup[It.IsAny<string>()]
    .OnGet.Do((type, value) => Console.WriteLine($"Read this[{type}]: {value}"))
    .OnSet.Do((type, newValue) => Console.WriteLine($"Set this[{type}] to {newValue}"));
```

Multiple callbacks form a sequence, like `Returns` and `Throws`:

```csharp
dispenser.Mock.Setup[It.IsAny<string>()]
    .OnGet.Do(() => Console.WriteLine("Runs on the 1st, 3rd, 5th, … read"))
    .OnGet.Do(() => Console.WriteLine("Runs on the 2nd, 4th, 6th, … read"));
```

When several setups match the same key, the one defined last wins.

## Class mocks

Use `.SkippingBaseClass()` to prevent calls to the base class implementation for a single indexer, or
[`MockBehavior.SkippingBaseClass()`](../04-create-mocks.md#customizing-mock-behavior) for the whole mock.

## Indexers with only one accessor

As for [properties](02-properties.md#properties-with-only-one-accessor), the setup and verify members only offer the
accessors that the mock actually intercepts: a get-only indexer has no `OnSet`, and a set-only indexer has neither
`OnGet`, `Returns`, `Throws` nor `InitializeWith`.

```csharp
public interface IChocolateStorage
{
    int this[string shelf] { get; }    // no setter
    string this[int box] { set; }      // no getter
}

IChocolateStorage storage = IChocolateStorage.CreateMock();

storage.Mock.Setup[It.IsAny<string>()].Returns(3);
storage.Mock.Setup[It.IsAny<int>()].OnSet.Do(value => Console.WriteLine(value));

storage.Mock.Verify["top"].Got().Once();
storage.Mock.Verify[7].Set("Ada").Once();

// ❌ These do not compile:
// storage.Mock.Setup[It.IsAny<string>()].OnSet
// storage.Mock.Setup[It.IsAny<int>()].Returns("Ada")
// storage.Mock.Verify["top"].Set(3)
// storage.Mock.Verify[7].Got()
```

:::tip

All callbacks and return values support conditional execution, repetition, parallel callbacks and access to the
invocation counter. See [Advanced callback features](../08-advanced-features/03-advanced-callback-features.md).

:::
