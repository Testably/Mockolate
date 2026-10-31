---
description: Set up return values, exceptions and callbacks for methods of a Mockolate mock.
---

# Methods

Use `dispenser.Mock.Setup.MethodName(…)` to set up a method. Pass a value or a [parameter matcher](../06-parameter-matching.md) for each parameter to decide which calls the setup applies to. With C# 15 you can also pass a predicate directly (see [Union parameters](../06-parameter-matching.md#union-parameters-c-15)).

## Returns / Throws

Use `.Returns(…)` to specify the value to return. You can provide a value, a factory, or a factory that receives the
method parameters.
Use `.Throws(…)` to specify an exception to throw, either directly, as a factory or as a factory that receives the
method parameters.

```csharp
// Return a fixed value
dispenser.Mock.Setup.Dispense("Milk", It.IsAny<int>())
    .Returns(true);

// Compute the return value from the parameters
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>())
    .Returns((type, amount) => amount <= 10);

// Throw an exception
dispenser.Mock.Setup.Dispense("Green", It.IsAny<int>())
    .Throws<InvalidChocolateException>();
```

### Sequences

Call `.Returns(…)` and `.Throws(…)` multiple times to define a sequence. Each call uses the next entry; after the last
entry the sequence starts again from the beginning:

```csharp
dispenser.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
    .Returns(true)
    .Throws(new Exception("Error"))
    .Returns(false);

// Calls: true, throws, false, true, throws, false, …
```

Mark the last entry with `.Forever()` to repeat it instead of starting over:

```csharp
dispenser.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
    .Returns(true)
    .Returns(false).Forever();

// Calls: true, false, false, false, …
```

See [Advanced callback features](../08-advanced-features/03-advanced-callback-features.md) to repeat entries (`.For(n)`),
limit them (`.Only(n)`) or apply them conditionally (`.When(…)`).

### Async methods

For methods returning `Task`/`Task<T>` or `ValueTask`/`ValueTask<T>`, use `.ReturnsAsync(…)` and `.ThrowsAsync(…)` so
that you don't have to wrap the value in a task yourself:

```csharp
public interface IChocolateDispenser
{
    // the members from the core concepts, and:
    Task<int> DispenseAsync(string type, int amount);
}

dispenser.Mock.Setup.DispenseAsync(It.IsAny<string>(), It.IsAny<int>())
    .ReturnsAsync((_, amount) => amount)      // First call returns the value of the `amount` parameter
    .ThrowsAsync(new TimeoutException())      // Second call returns a faulted task
    .ReturnsAsync(0).Forever();               // Subsequent calls return 0
```

## Callbacks

Use `.Do(…)` to run code when the method is called. The callback can be parameterless or receive the method
parameters:

```csharp
dispenser.Mock.Setup.Dispense("White", It.IsAny<int>())
    .Do((type, amount) => Console.WriteLine($"Dispensed {amount} {type} chocolate."));
```

`.Do(…)` can be combined with `.Returns(…)` and `.Throws(…)` on the same setup.

## Class mocks

Class mocks call the base class implementation of a virtual method, and use its return value when the setup does not
provide one. Use `.SkippingBaseClass()` on a setup to prevent this for a single method, or
[`MockBehavior.SkippingBaseClass()`](../04-create-mocks.md#customizing-mock-behavior) for the whole mock.

:::tip

All callbacks and return values support conditional execution, repetition, parallel callbacks and access to the
invocation counter. See [Advanced callback features](../08-advanced-features/03-advanced-callback-features.md).

:::
