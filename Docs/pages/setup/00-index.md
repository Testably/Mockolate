---
description: Decide how the methods, properties, indexers and events of a Mockolate mock behave.
---

# Setup

A setup decides how a member of the mock behaves when it is called: what it returns, whether it throws, and which
callbacks run. All setups start at `dispenser.Mock.Setup` and mirror the members of the mocked type:

| Member kind                    | Example                                                                  |
|--------------------------------|--------------------------------------------------------------------------|
| [Methods](01-methods.md)       | `dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);`  |
| [Properties](02-properties.md) | `dispenser.Mock.Setup.TotalDispensed.InitializeWith(42);`                |
| [Indexers](03-indexers.md)     | `dispenser.Mock.Setup["Dark"].Returns(10);`                              |
| [Events](04-events.md)         | `dispenser.Mock.Raise.ChocolateDispensed("Dark", 3);`                    |

## Building blocks

All member kinds share the same vocabulary:

- **`Returns(…)` / `Throws(…)`** decide the result. Called several times, they form a
  [sequence](01-methods.md#sequences) that starts over after the last entry.
- **`Do(…)`** runs a callback. For properties and indexers, use `OnGet.Do(…)` and `OnSet.Do(…)`.
- **`InitializeWith(…)`** gives properties and indexers a start value, after which they behave like normal storage.
- **Values and matchers** such as `It.IsAny<int>()` decide which calls a setup applies to. See
  [Parameter matching](../06-parameter-matching.md).
- **Modifiers** such as `.For(n)`, `.Only(n)`, `.When(…)` and `.Forever()` control how often an entry applies. See
  [Advanced callback features](../advanced-features/03-advanced-callback-features.md).

:::warning[Chain what belongs together]

When several setups match a call, the one defined last wins. Every `dispenser.Mock.Setup.X` statement starts a new
setup, so chain everything that belongs to one setup instead of repeating the statement:

```csharp
dispenser.Mock.Setup.TotalDispensed
    .InitializeWith(42)
    .OnSet.Do(newValue => Console.WriteLine(newValue));
```

:::

Members without a setup fall back to the mock's [`MockBehavior`](../04-create-mocks.md#customizing-mock-behavior):
methods return default values, and properties and indexers store the values written to them. You can also pass the
setups directly to [`CreateMock`](../04-create-mocks.md#setups-during-creation).
