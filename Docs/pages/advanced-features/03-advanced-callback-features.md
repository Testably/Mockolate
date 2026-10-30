---
description: Control when and how often Mockolate callbacks and return values apply with When, Only, For, Forever and InParallel.
---

# Advanced callback features

`Returns`, `Throws` and `Do` entries form a [sequence](../setup/01-methods.md#sequences): each call uses the next
entry, and after the last entry the sequence starts over. The following modifiers change how an entry takes part in
the sequence. They work on methods, properties, indexers and events alike.

| Modifier          | Effect                                                                           |
|-------------------|----------------------------------------------------------------------------------|
| `.When(predicate)` | applies the entry only when the predicate on the invocation counter is `true`   |
| `.For(n)`         | uses the entry for `n` calls in a row before moving on                           |
| `.Only(n)`        | uses the entry at most `n` times in total; `.OnlyOnce()` is short for `.Only(1)` |
| `.Forever()`      | uses the entry for all further calls; the sequence no longer starts over         |
| `.InParallel()`   | runs a `Do` callback on every call, alongside the sequence                       |

## Conditional callbacks (`When`)

Execute a callback or return a value only when the zero-based invocation counter satisfies a predicate:

```csharp
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>())
    .Do(() => Console.WriteLine("Called!")).When(count => count >= 2);  // The first two calls are skipped
```

## Repeat entries (`For`)

Use an entry for several calls in a row before moving on to the next one:

```csharp
dispenser.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
    .Do(() => Console.WriteLine("First three calls")).For(3)
    .Do(() => Console.WriteLine("Next three calls")).For(3);

dispenser.Mock.Setup.TotalDispensed
    .Returns(10).For(1)
    .Returns(20).For(2)
    .Returns(30).For(3);
// Reads: 10, 20, 20, 30, 30, 30, 10, 20, 20, …
```

## Limit entries (`Only`)

Stop using an entry after it was used a number of times:

```csharp
// Execute the callback up to 3 times
dispenser.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
    .Do(() => Console.WriteLine("Up to 3 times")).Only(3);

// Throw only on the first read
dispenser.Mock.Setup.TotalDispensed
    .Throws(new Exception("This exception is thrown only once")).OnlyOnce()
    .Returns(42);
// Reads: throws, 42, 42, 42, …
```

## Repeat forever (`Forever`)

Mark the last entry with `.Forever()` to use it for all further calls instead of starting the sequence over:

```csharp
dispenser.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
    .Returns(true).For(2)      // Returns true the first two times
    .Returns(false).Forever(); // Then always returns false
```

## Parallel callbacks (`InParallel`)

Multiple `Do` callbacks take turns, like the entries of a `Returns` sequence. Mark a callback with `.InParallel()` to
run it on every call instead:

```csharp
dispenser.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
    .Do(() => Console.WriteLine("Runs on the 1st, 3rd, 5th, … call"))
    .Do(() => Console.WriteLine("Runs on every call")).InParallel()
    .Do(() => Console.WriteLine("Runs on the 2nd, 4th, 6th, … call"));
```

`.InParallel()` only applies to `Do` callbacks, not to `Returns` or `Throws`.

## Invocation counter

Callbacks can receive the zero-based invocation counter as their first parameter:

```csharp
dispenser.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
    .Do((count, type, amount) => Console.WriteLine($"Call #{count}: {amount} {type}"));

dispenser.Mock.Setup.TotalDispensed.OnGet
    .Do((count, value) => Console.WriteLine($"Read #{count}, value: {value}"));

// Indexer setter: the counter, then the key(s), then the new value
dispenser.Mock.Setup[It.IsAny<string>()].OnSet
    .Do((count, type, newValue) => Console.WriteLine($"Set #{count}: this[{type}] = {newValue}"));
```
