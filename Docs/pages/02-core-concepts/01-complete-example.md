---
description: An end-to-end Mockolate example that combines properties, indexers, events, methods and verification.
---

# A complete example

The following example combines indexers, methods, events, stateful setup and verification into a single end-to-end
scenario. The dispenser keeps track of its stock in the indexer, and every successful `Dispense` call reduces the
stock and raises the `ChocolateDispensed` event:

```csharp
using Mockolate;
using Mockolate.Verify;
using static aweXpect.Expect;

public delegate void ChocolateDispensedDelegate(string type, int amount);

public interface IChocolateDispenser
{
    int this[string type] { get; set; }
    int TotalDispensed { get; set; }
    bool Dispense(string type, int amount);
    event ChocolateDispensedDelegate ChocolateDispensed;
}

// Create a mock of IChocolateDispenser
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();

// Setup: initial stock of 10 for Dark chocolate
dispenser.Mock.Setup["Dark"].InitializeWith(10);
// Setup: Dispense decreases the stock if there is enough, and returns true/false
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>())
    .Returns((type, amount) =>
    {
        int current = dispenser[type];
        if (current >= amount)
        {
            dispenser[type] = current - amount;
            dispenser.Mock.Raise.ChocolateDispensed(type, amount);
            return true;
        }
        return false;
    });

// Track the dispensed amount via the event
int dispensedAmount = 0;
dispenser.ChocolateDispensed += (type, amount) =>
{
    dispensedAmount += amount;
};

// Act: try to dispense chocolates
bool gotChoc1 = dispenser.Dispense("Dark", 4);
bool gotChoc2 = dispenser.Dispense("Dark", 5);
bool gotChoc3 = dispenser.Dispense("Dark", 6);

// Assert: check the results and the interactions
await That(gotChoc1).IsTrue();
await That(gotChoc2).IsTrue();
await That(gotChoc3).IsFalse().Because("only 1 Dark chocolate is left");
await That(dispensedAmount).IsEqualTo(9);
await That(dispenser["Dark"]).IsEqualTo(1);
dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>()).Exactly(3);
dispenser.Mock.Verify["Dark"].Set(It.IsAny<int>()).Twice();
```

The individual features are described in detail on the following pages:

- [Create mocks](../04-create-mocks.md)
- Setup of [methods](../05-setup/01-methods.md), [properties](../05-setup/02-properties.md), [indexers](../05-setup/03-indexers.md)
  and [events](../05-setup/04-events.md)
- [Parameter matching](../06-parameter-matching.md)
- [Verify interactions](../07-verify-interactions.md)
