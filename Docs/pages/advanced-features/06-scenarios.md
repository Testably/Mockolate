---
description: Define multiple sets of setups on a Mockolate mock and switch between them at runtime.
---

# Scenarios

Scenarios let you define multiple sets of setups on a single mock and switch between them at runtime. This is useful
when the collaborator behaves differently depending on its internal state, for example a chocolate dispenser that
starts empty, becomes loaded after `Refill(…)`, and runs out of stock after dispensing:

```csharp
public interface IChocolateDispenser
{
    bool Dispense(string type, int amount);
    bool Refill(int amount);
}
```

A mock always has an *active scenario* (the empty string `""` by default). When a member is called, Mockolate looks
for a matching setup in the active scenario first, and then in the default setups.

## Defining scenarios

Use `InScenario(name)` to scope setups to a named scenario. Its `Setup` property mirrors `dispenser.Mock.Setup`, but
the setups only apply while the scenario is active:

```csharp
dispenser.Mock.InScenario("empty").Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
    .Throws<OutOfStockException>();
dispenser.Mock.InScenario("loaded").Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
    .Returns(true);
```

A callback overload groups multiple setups of the same scenario:

```csharp
dispenser.Mock.InScenario("loaded", scope =>
{
    scope.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>()).Returns(true);
    scope.Setup.Refill(It.IsAny<int>()).Returns(true);
});
```

## Switching scenarios

Chain `.TransitionTo(name)` on any method, property, indexer or event subscription setup to change the active
scenario whenever the setup is used. The transition happens in addition to the return value or exception of the
setup:

```csharp
dispenser.Mock.InScenario("empty")
    .Setup.Refill(It.IsAny<int>())
    .Returns(true)
    .TransitionTo("loaded");

dispenser.Mock.InScenario("loaded")
    .Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
    .Returns(true)
    .TransitionTo("empty");
```

To set the starting state, change the active scenario directly with `dispenser.Mock.TransitionTo("empty");`.

## Resolution rules

When the mock is called, Mockolate looks for a matching setup in this order:

1. The setups of the active scenario.
2. The default setups (registered via `dispenser.Mock.Setup`).
3. The mock's [default behavior](/Mockolate/core-concepts/#what-happens-when-the-mock-is-called).

Scenario setups add to the default setups instead of replacing them: register catch-all setups in the default scope
and override specific members per scenario. Setups registered via `InScenario` never apply outside their scenario.
