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

### Across mocks

To verify the order of calls on different mocks, track their call order with `TrackCallOrder` before the calls, and pass the verifications of the other mocks to `Then`:

```csharp
IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();

using (warehouse.Mock.TrackCallOrder(dispenser.Mock))
{
    // Act
}

warehouse.Mock.Verify.Restock("Dark", 5)
    .Then(dispenser.Mock.Verify.Dispense("Dark", 1));
```

Only the calls until the scope is disposed are compared, so calls made before `TrackCallOrder` or after the `using` block do not count. Pass further mocks as additional arguments, e.g. `warehouse.Mock.TrackCallOrder(dispenser.Mock, shelf.Mock)`.

<details>
<summary>Details</summary>

- Recording the order of calls on several mocks has a cost, so a mock only records it while it is tracked. `Then` throws a `MockException` when the verifications belong to mocks whose call order was not tracked together.
- A mock can only be tracked by one scope at a time. After the scope is disposed, it can be tracked again; the calls of different scopes are never compared.
- Verifications of the same mock can also be passed to `Then` directly, e.g. `dispenser.Mock.Verify.Dispense("Dark", 1).Then(dispenser.Mock.Verify.Dispense("Milk", 2))`. Within one mock, no tracking is needed.
- When the verifications belong to different mocks, `Within(TimeSpan)` on any of them waits for calls from a background thread, as described [below](#waiting-for-background-calls).
- Verifications on a [monitor](08-advanced-features/04-monitor-interactions.md) (`monitor.Verify`) cannot be combined with other verifications in `Then`, not even with those of the monitored mock, because a monitor keeps its own copy of the calls.

</details>

<details>
<summary>Example types used in this section</summary>

```csharp
public interface IChocolateWarehouse
{
    void Restock(string type, int amount);
}
```

</details>

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

[aweXpect.Mockolate](/aweXpect/extensions/aweXpect.Mockolate/) offers awaitable verifications,
including an asynchronous `Within(TimeSpan)` that does not block the test thread:
`await That(dispenser.Mock.Verify.Dispense("Dark", 5)).AtLeastOnce().Within(TimeSpan.FromSeconds(1));`

:::

## Further checks

- [Monitor interactions](08-advanced-features/04-monitor-interactions.md) to only count the interactions within a scope.
- [Check for unexpected interactions](08-advanced-features/05-check-for-unexpected-interactions.md) to check that all
  interactions were verified or all setups were used.
