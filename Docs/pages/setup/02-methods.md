# Methods

Use `sut.Mock.Setup.MethodName(…)` to set up methods. You can specify argument matchers for each parameter; with C# 15
you can also pass predicates directly (see *Union Parameters* under Parameter Matching).

## Returns / Throws

Use `.Returns(…)` to specify the value to return. You can provide a direct value, a callback, or a callback with
parameters.
Use `.Throws(…)` to specify an exception to throw. Supports direct exceptions, exception factories, or factories with
parameters.

You can call `.Returns(…)` and `.Throws(…)` multiple times to define a sequence of return values or exceptions (cycled
on each call).

```csharp
// Setup Dispense to decrease stock and raise event
sut.Mock.Setup.Dispense(It.Is("Dark"), It.IsAny<int>())
    .Returns((type, amount) =>
    {
        var current = sut[type];
        if (current >= amount)
        {
            sut[type] = current - amount;
            sut.Mock.Raise.ChocolateDispensed(type, amount);
            return true;
        }
        return false;
    });

// Setup method to throw
sut.Mock.Setup.Dispense(It.Is("Green"), It.IsAny<int>())
    .Throws<InvalidChocolateException>();

// Sequence of returns and throws
sut.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
    .Returns(true)
    .Throws(new Exception("Error"))
    .Returns(false);
```

### Async Methods

For async methods returning `Task`/`Task<T>` or `ValueTask`/`ValueTask<T>`, use `.ReturnsAsync(…)` or `ThrowsAsync(…)`
respectively:

```csharp
sut.Mock.Setup.DispenseAsync(It.IsAny<string>(), It.IsAny<int>())
    .ReturnsAsync((_, v) => v)            // First execution returns the value of the `int` parameter
    .ThrowsAsync(new TimeoutException())  // Second execution throws a TimeoutException
    .ReturnsAsync(0).Forever();           // Subsequent executions return 0
```

## Callbacks

Use `.Do(…)` to run code when the method is called. Supports parameterless or parameter callbacks.

```csharp
// Setup method with callback
sut.Mock.Setup.Dispense(It.Is("White"), It.IsAny<int>())
    .Do((type, amount) => Console.WriteLine($"Dispensed {amount} {type} chocolate."));
```

**Notes:**

- Use `.SkippingBaseClass(…)` to override the base class behavior for a specific method (only for class mocks).
- When you specify overlapping setups, the most recently defined setup takes precedence.
- All callbacks and return values support more advanced features like conditional execution, frequency control,
  parallel execution, and access to the invocation counter.
  See [Advanced callback features](../advanced-features/advanced-callback-features) for details.
