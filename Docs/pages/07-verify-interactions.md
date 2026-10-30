---
description: Verify how often methods, properties, indexers and events of a Mockolate mock were used, and in which order.
---

# Verify interactions

Every interaction with a mock is recorded. Use `dispenser.Mock.Verify` to check that methods, properties, indexers or
events were used with specific arguments, and how many times:

```csharp
dispenser.Mock.Verify.Dispense("Dark", 5).AtLeastOnce();
```

A verification fails with a `MockVerificationException` that describes the expected and the actual calls. Arguments
are matched with the same [values and matchers](06-parameter-matching.md) as in setups.

:::note

The count methods are extension methods in the `Mockolate.Verify` namespace, so add `using Mockolate.Verify;` to your
test files.

:::

## Call counts

| Method               | The interaction occurred …                     |
|----------------------|------------------------------------------------|
| `.Never()`           | never                                          |
| `.Once()`            | exactly once                                   |
| `.Twice()`           | exactly twice                                  |
| `.Exactly(n)`        | exactly `n` times                              |
| `.AtLeastOnce()`     | at least once                                  |
| `.AtLeastTwice()`    | at least twice                                 |
| `.AtLeast(n)`        | at least `n` times                             |
| `.AtMostOnce()`      | at most once                                   |
| `.AtMostTwice()`     | at most twice                                  |
| `.AtMost(n)`         | at most `n` times                              |
| `.Between(min, max)` | between `min` and `max` times (inclusive)      |
| `.Times(predicate)`  | a number of times that satisfies the predicate |

## Methods

```csharp
// Dispense("Dark", 5) was called at least once
dispenser.Mock.Verify.Dispense("Dark", 5).AtLeastOnce();

// Dispense was never called with "White" and any amount
dispenser.Mock.Verify.Dispense("White", It.IsAny<int>()).Never();

// Dispense was called exactly twice, with any arguments
dispenser.Mock.Verify.Dispense(Match.AnyParameters()).Exactly(2);

// Dispense was called an even number of times
dispenser.Mock.Verify.Dispense(It.IsAny<string>(), It.IsAny<int>()).Times(count => count % 2 == 0);
```

You can also verify how often a specific setup was used:

```csharp
IMethodSetup setup = dispenser.Mock.Setup.Dispense("Dark", 5).Returns(true);

// Act

dispenser.Mock.VerifySetup(setup).AtLeastOnce();
```

## Properties

```csharp
// The property 'TotalDispensed' was read at least once
dispenser.Mock.Verify.TotalDispensed.Got().AtLeastOnce();

// The property 'TotalDispensed' was set to 42 exactly once
dispenser.Mock.Verify.TotalDispensed.Set(42).Once();

// The property 'TotalDispensed' was set to a value greater than 10
dispenser.Mock.Verify.TotalDispensed.Set(It.Satisfies<int>(v => v > 10)).AtLeastOnce();
```

## Indexers

```csharp
// The indexer was read with the key "Dark" exactly once
dispenser.Mock.Verify["Dark"].Got().Once();

// The indexer was set for the key "Milk" to 7 at least once
dispenser.Mock.Verify["Milk"].Set(7).AtLeastOnce();
```

Both the keys and the value accept matchers.

## Events

```csharp
// A handler was subscribed to 'ChocolateDispensed' at least once
dispenser.Mock.Verify.ChocolateDispensed.Subscribed().AtLeastOnce();

// A handler was unsubscribed from 'ChocolateDispensed' exactly once
dispenser.Mock.Verify.ChocolateDispensed.Unsubscribed().Once();
```

## Call order

Use `Then` to verify that calls occurred in a specific order:

```csharp
dispenser.Mock.Verify.Dispense("Dark", 1).Then(
    m => m.Dispense("Milk", 2),
    m => m.Dispense("White", 3));
```

If the order is different or a call is missing, a `MockVerificationException` is thrown.

## Waiting for background calls

If the code under test calls the mock from a background thread, use `Within(TimeSpan)` to wait until the expected
interactions occurred:

```csharp
// Wait up to 1 second for Dispense("Dark", 5) to be called
dispenser.Mock.Verify.Dispense("Dark", 5)
    .Within(TimeSpan.FromSeconds(1))
    .AtLeastOnce();
```

Use `WithCancellation(CancellationToken)` to wait until the token is canceled instead. If you combine it with
`Within`, both the timeout and the token are respected.

Both block the test until the expected interactions occurred, or throw a `MockVerificationException` when the time is
up.

:::tip

[aweXpect.Mockolate](https://docs.testably.org/aweXpect/extensions/aweXpect.Mockolate) offers awaitable verifications,
including an asynchronous `Within(TimeSpan)` that does not block the test thread:
`await That(dispenser.Mock.Verify.Dispense("Dark", 5)).AtLeastOnce().Within(TimeSpan.FromSeconds(1));`

:::

## Further checks

- [Monitor interactions](advanced-features/04-monitor-interactions.md) to only count the interactions within a scope.
- [Check for unexpected interactions](advanced-features/05-check-for-unexpected-interactions.md) to check that all
  interactions were verified or all setups were used.
