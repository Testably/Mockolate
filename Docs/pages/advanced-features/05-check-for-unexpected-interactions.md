---
description: Check that all interactions with a Mockolate mock were verified and all setups were used.
---

# Check for unexpected interactions

## All interactions are verified

`VerifyThatAllInteractionsAreVerified()` returns `true` when every recorded interaction was matched by at least one
verification. Use it to make sure that the code under test made no calls you didn't expect:

```csharp
dispenser.Dispense("Dark", 1);
dispenser.Mock.Verify.Dispense("Dark", 1).Once();

bool allVerified = dispenser.Mock.VerifyThatAllInteractionsAreVerified();  // true
```

## All setups are used

`VerifyThatAllSetupsAreUsed()` returns `true` when every setup was matched by at least one interaction. Use it to make
sure that your test setup and the test execution match:

```csharp
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);
dispenser.Mock.Setup.Dispense("Milk", It.IsAny<int>()).Returns(true);

dispenser.Dispense("Dark", 1);

bool allUsed = dispenser.Mock.VerifyThatAllSetupsAreUsed();  // false, the "Milk" setup was not used
```

:::note

Both methods return a `bool` instead of throwing, so assert the result with your test framework, for example
`Assert.True(allVerified)`.

:::
