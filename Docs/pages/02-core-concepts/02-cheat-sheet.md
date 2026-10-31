---
description: The most common Mockolate calls on one page.
---

# Cheat sheet

The most common calls at a glance. Follow the links for details.

```csharp
using Mockolate;
using Mockolate.Verify;
```

## [Create](../04-create-mocks.md)

```csharp
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
IChocolateDispenser strict = IChocolateDispenser.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
ChocolateDispenser classMock = ChocolateDispenser.CreateMock(["constructor", "arguments"]);
IChocolateDispenser spy = IChocolateDispenser.CreateMock().Wrapping(realDispenser);
IChocolateDispenser multi = IChocolateDispenser.CreateMock().Implementing<ILemonadeDispenser>();
```

## [Set up methods](../05-setup/01-methods.md)

Each line is an alternative: a later setup with the same arguments replaces an earlier one, so chain everything that
belongs to one setup.

```csharp
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns((type, amount) => amount < 10);
dispenser.Mock.Setup.Dispense("Green", It.IsAny<int>()).Throws<InvalidOperationException>();
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true).Returns(false);           // sequence, repeats
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true).Returns(false).Forever(); // then always false
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Do((type, amount) => Console.WriteLine(amount));
dispenser.Mock.Setup.DispenseAsync("Dark", It.IsAny<int>()).ReturnsAsync(3);                   // for Task<int> methods
```

## [Set up properties](../05-setup/02-properties.md) and [indexers](../05-setup/03-indexers.md)

```csharp
dispenser.Mock.Setup.TotalDispensed.InitializeWith(42);                  // behaves like a property, starting at 42
dispenser.Mock.Setup.TotalDispensed.Returns(42);                         // always returns 42
dispenser.Mock.Setup.TotalDispensed.InitializeWith(42).OnSet.Do(newValue => Console.WriteLine(newValue));
dispenser.Mock.Setup["Dark"].InitializeWith(10);
dispenser.Mock.Setup[It.IsAny<string>()].Returns(type => type.Length);
```

## [Events](../05-setup/04-events.md)

```csharp
dispenser.Mock.Raise.ChocolateDispensed("Dark", 3);
```

## [Match parameters](../06-parameter-matching.md)

| Matcher                                         | Matches                                  |
|-------------------------------------------------|------------------------------------------|
| `"Dark"` or `It.Is("Dark")`                     | an equal value                           |
| `It.IsAny<int>()`                               | any value                                |
| `It.Satisfies<int>(x => x > 0)`                 | values that satisfy the predicate        |
| `It.IsOneOf("Dark", "Milk")`                    | one of the values                        |
| `It.IsInRange(1, 10)`                           | a value in the range                     |
| `It.IsNull<string>()`, `It.IsNotNull<string>()` | `null`, anything but `null`              |
| `It.Matches("Da*")`                             | a wildcard pattern                       |
| `It.Contains(5)`, `It.SequenceEquals(1, 2)`     | collections                              |
| `It.IsOut(() => 42)`, `It.IsRef<int>(v => v + 1)` | `out` and `ref` parameters             |
| `Match.AnyParameters()`                         | any combination of parameters            |

## [Verify](../07-verify-interactions.md)

```csharp
dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>()).Once();
dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>()).Never();
dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>()).AtLeast(2);
dispenser.Mock.Verify.TotalDispensed.Got().AtLeastOnce();
dispenser.Mock.Verify.TotalDispensed.Set(42).Once();
dispenser.Mock.Verify["Dark"].Got().Twice();
dispenser.Mock.Verify.ChocolateDispensed.Subscribed().Once();
dispenser.Mock.Verify.Dispense("Dark", 1).Then(m => m.Dispense("Milk", 2));
dispenser.Mock.Verify.Dispense("Dark", 1).Within(TimeSpan.FromSeconds(1)).Once();
```
