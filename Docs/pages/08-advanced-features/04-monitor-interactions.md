---
description: Limit Mockolate verifications to the interactions within a scope, or clear recorded interactions.
---

# Monitor interactions

A mock records all interactions from the moment it is created. To only verify the interactions within a given scope,
use a monitor:

```csharp
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
MockMonitor<Mock.IMockVerifyForIChocolateDispenser> monitor = dispenser.Mock.Monitor();

dispenser.Dispense("Dark", 1); // Not monitored
using (monitor.Run())
{
    dispenser.Dispense("Dark", 2); // Monitored
}
dispenser.Dispense("Dark", 3); // Not monitored

// Verifications on the monitor only count the interactions while it was running
monitor.Verify.Dispense("Dark", It.IsAny<int>()).Once();
```

`MockMonitor<T>` lives in the `Mockolate.Monitor` namespace. You can call `monitor.Run()` multiple times; each run
adds to the monitored interactions.

## Clear all interactions

For simpler scenarios, you can clear all recorded interactions of a mock with `ClearAllInteractions()`:

```csharp
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();

dispenser.Dispense("Dark", 1);
// Clears all previously recorded interactions
dispenser.Mock.ClearAllInteractions();
dispenser.Dispense("Dark", 2);

dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>()).Once();
```

Setups are not affected by `ClearAllInteractions()`.
