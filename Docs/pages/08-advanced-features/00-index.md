---
description: Advanced Mockolate features for protected and static members, callbacks, monitors, strict checks and scenarios.
---

# Advanced features

The basics of [creating](../04-create-mocks.md), [setting up](../05-setup/01-methods.md) and
[verifying](../07-verify-interactions.md) mocks cover most tests. The following features help in the remaining cases:

| Feature                                                                    | Use it when you want to …                                                       |
|----------------------------------------------------------------------------|---------------------------------------------------------------------------------|
| [Protected members](01-protected-members.md)                               | set up or verify `protected virtual` members of a class mock                    |
| [Static interface members](02-static-interface-members.md)                 | mock `static abstract` or `static virtual` interface members                    |
| [Advanced callback features](03-advanced-callback-features.md)             | control how often and when return values and callbacks apply                    |
| [Monitor interactions](04-monitor-interactions.md)                         | only verify the interactions within a scope, or reset the recorded interactions |
| [Check for unexpected interactions](05-check-for-unexpected-interactions.md) | make sure every interaction was verified, or every setup was used            |
| [Scenarios](06-scenarios.md)                                               | switch between sets of setups, for collaborators with an internal state         |

For example, [scenarios](06-scenarios.md) let a dispenser with a `Refill` method behave differently before and after a refill:

```csharp
public interface IChocolateDispenser
{
    // the members from the core concepts, and:
    bool Refill(int amount);
}

dispenser.Mock.InScenario("empty").Setup.Refill(It.IsAny<int>())
    .Returns(true)
    .TransitionTo("loaded");
dispenser.Mock.InScenario("loaded").Setup.Dispense(It.IsAny<string>(), It.IsAny<int>())
    .Returns(true);
```
