---
title: Automated migration
slug: automated
sidebar_position: 1
description: Migrate existing Moq and NSubstitute tests to Mockolate automatically with the Mockolate.Migration code fixers.
---

import PackageInstall from '@site/src/components/PackageInstall';

The [Mockolate.Migration](https://www.nuget.org/packages/Mockolate.Migration) package contains a Roslyn analyzer and
code fixes that migrate [Moq](https://github.com/devlooped/moq) and [NSubstitute](https://nsubstitute.github.io/)
mocks to Mockolate. The analyzer reports every mock it can migrate, and the code fix rewrites the whole mock in one
step: the construction, all its setups and verifications, the event wiring and, for Moq, every `.Object` access.

| Diagnostic      | Source library | Code fix title                     |
|-----------------|----------------|------------------------------------|
| `MockolateM001` | Moq            | *Migrate Moq to Mockolate*         |
| `MockolateM002` | NSubstitute    | *Migrate NSubstitute to Mockolate* |

## Install the package

Install the package into the test project you want to migrate, next to [Mockolate](../01-getting-started.mdx) itself:

<PackageInstall package="Mockolate.Migration" developmentDependency />

The package contains only the analyzer and the code fixes, no runtime code. You need it only while you migrate: once
a project no longer uses Moq or NSubstitute, remove the reference again.

## Migrate your tests

After you install the package, each mock that can be migrated is reported as a warning (`MockolateM001` or
`MockolateM002`). Apply the code fix in your IDE (Visual Studio, Rider or VS Code with C# Dev Kit). To migrate many
mocks at once, use *Fix all occurrences* for the document, project or solution, or run the code fix from the command
line:

```shell
dotnet format analyzers --diagnostics MockolateM001 MockolateM002
```

Then build, run your tests and check the [cases that need manual review](#what-needs-manual-review).

The examples on this page use the `IChocolateDispenser` interface from the [migration overview](/Mockolate/migration/) and the
`VendingMachine` from [Getting started](../01-getting-started.mdx). The code fix keeps your variable names, so the
mock keeps its name and `sut` stays the system under test.

## Migrating from Moq

The code fix rewrites `new Mock<T>(…)` to `T.CreateMock(…)`, changes the declared type from `Mock<T>` to `T`, drops
every `.Object` access and moves setups and verifications to `Mock.Setup` and `Mock.Verify`:

```csharp title="Before (Moq)"
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>();
dispenser.Setup(m => m.Dispense("Dark", It.IsAny<int>())).Returns(true);
VendingMachine sut = new VendingMachine(dispenser.Object);

sut.Buy("Dark", 4);

dispenser.Verify(m => m.Dispense("Dark", It.IsAny<int>()), Times.Once());
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);
VendingMachine sut = new VendingMachine(dispenser);

sut.Buy("Dark", 4);

dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>()).Once();
```

Strict mocks get a [behavior](../04-create-mocks.md#customizing-mock-behavior) that throws when no setup matches. Properties, raising events and
verifying subscriptions are migrated as well:

```csharp title="Before (Moq)"
Mock<IChocolateDispenser> dispenser = new Mock<IChocolateDispenser>(MockBehavior.Strict);
dispenser.SetupProperty(m => m.TotalDispensed, 10);
dispenser.Raise(m => m.ChocolateDispensed += null, "Dark", 2);
dispenser.VerifyAdd(m => m.ChocolateDispensed += It.IsAny<ChocolateDispensedDelegate>(), Times.Once());
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
dispenser.Mock.Setup.TotalDispensed.InitializeWith(10);
dispenser.Mock.Raise.ChocolateDispensed("Dark", 2);
dispenser.Mock.Verify.ChocolateDispensed.Subscribed().Once();
```

### Supported Moq constructs

| Moq                                                       | Mockolate                                                              |
|-----------------------------------------------------------|------------------------------------------------------------------------|
| `new Mock<T>()`                                           | `T.CreateMock()`                                                       |
| `new Mock<T>(MockBehavior.Strict)`                        | `T.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup())`            |
| `mock.Object`                                             | `mock` (the `.Object` access is dropped)                               |
| `mock.Setup(m => m.Method(args))`                         | `mock.Mock.Setup.Method(args)`                                         |
| `mock.Setup(m => m.Prop)`, `mock.SetupGet(m => m.Prop)`   | `mock.Mock.Setup.Prop`                                                 |
| `mock.SetupProperty(m => m.Prop)`                         | `mock.Mock.Setup.Prop.Register()`                                      |
| `mock.SetupProperty(m => m.Prop, value)`                  | `mock.Mock.Setup.Prop.InitializeWith(value)`                           |
| `mock.SetupSequence(…).Returns(a).Returns(b)`             | `mock.Mock.Setup.Method(…).Returns(a).Returns(b)`                      |
| `.Returns(…)`, `.ReturnsAsync(…)`, `.Throws(…)`           | kept on the Mockolate setup                                            |
| `.Callback(…)`                                            | `.Do(…)`                                                               |
| `mock.InSequence(sequence).Setup(…)`                      | `mock.Mock.Setup.Method(…)` (the `InSequence` wrapper is removed)      |
| `mock.Verify(m => m.Method(args), Times.Once())`          | `mock.Mock.Verify.Method(args).Once()`                                 |
| `mock.VerifyGet(…)`, `mock.VerifySet(…)`                  | `mock.Mock.Verify.Prop.Got(…)`, `mock.Mock.Verify.Prop.Set(…)`         |
| `mock.Raise(m => m.Event += null, args)`                  | `mock.Mock.Raise.Event(sender, args)`                                  |
| `mock.VerifyAdd(m => m.Event += …, times)`                | `mock.Mock.Verify.Event.Subscribed()` with the count                   |
| `mock.VerifyRemove(m => m.Event -= …, times)`             | `mock.Mock.Verify.Event.Unsubscribed()` with the count                 |
| `It.IsAny<T>()`                                           | `It.IsAny<T>()`                                                        |
| `It.Is<T>(predicate)`                                     | `It.Satisfies<T>(predicate)`                                           |
| `It.IsInRange(a, b, Range.Inclusive)`                     | `It.IsInRange(a, b)`                                                   |
| `It.IsRegex(pattern, options)`                            | `It.Matches(pattern).AsRegex(options)`                                 |
| `It.Ref<T>.IsAny`, `out` parameters                       | `It.IsAnyRef<T>()`, `It.IsOut(() => value)`                            |
| `mock.Setup(m => m.Child.Prop)`                           | `mock.Child.Mock.Setup.Prop` and a `// TODO` comment                   |

See [parameter matching](../06-parameter-matching.md) for the Mockolate matchers.

## Migrating from NSubstitute

The code fix rewrites `Substitute.For<T>(…)` to `T.CreateMock(…)` and moves the setups
(`substitute.Method(args).Returns(…)`) and verifications (`substitute.Received().Method(…)`) to `Mock.Setup` and
`Mock.Verify`:

```csharp title="Before (NSubstitute)"
IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
dispenser.Dispense("Dark", Arg.Any<int>()).Returns(true);
VendingMachine sut = new VendingMachine(dispenser);

sut.Buy("Dark", 4);

dispenser.Received(1).Dispense("Dark", Arg.Any<int>());
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);
VendingMachine sut = new VendingMachine(dispenser);

sut.Buy("Dark", 4);

dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>()).Once();
```

Multiple return values become a [sequence](../setup/01-methods.md#sequences) that ends with `.Forever()`, so the last
value repeats as in NSubstitute. `When(…).Do(…)` becomes a setup with a `Do` callback, and raised events go through
`Mock.Raise`:

```csharp title="Before (NSubstitute)"
IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
int refills = 0;
dispenser.Dispense("Dark", 1).Returns(true, true, false);
dispenser.When(x => x.Dispense("Milk", Arg.Any<int>())).Do(_ => refills++);
dispenser.ChocolateDispensed += Raise.Event<ChocolateDispensedDelegate>("Dark", 2);
```

```csharp title="After (Mockolate)"
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
int refills = 0;
dispenser.Mock.Setup.Dispense("Dark", 1).Returns(true).Returns(true).Returns(false).Forever();
dispenser.Mock.Setup.Dispense("Milk", It.IsAny<int>()).Do(() => refills++);
dispenser.Mock.Raise.ChocolateDispensed("Dark", 2);
```

### CallInfo callbacks

In NSubstitute, the callbacks of `Returns(call => …)`, `When(…).Do(call => …)` and `AndDoes(call => …)` receive a
`CallInfo` with the arguments of the call. The Mockolate callbacks take the parameters of the method instead, so the
code fix rewrites the lambda:

- If the lambda does not use `call`, the parameter is dropped: `Do(call => count++)` becomes `Do(() => count++)`.
- If the lambda only reads arguments at a constant position (`call.ArgAt<T>(0)`, `call[0]`) or by a unique type
  (`call.Arg<T>()`), it gets the parameters of the method, and each access is replaced with the parameter:
  `Returns(call => call.ArgAt<int>(1) > 0)` becomes `Returns((string type, int amount) => amount > 0)`.
- Otherwise the lambda is kept as it is and marked with a `// TODO` comment. See
  [What needs manual review](#what-needs-manual-review).

### Supported NSubstitute constructs

| NSubstitute                                                 | Mockolate                                                                    |
|-------------------------------------------------------------|------------------------------------------------------------------------------|
| `Substitute.For<T>()`, `Substitute.For<T>(args)`            | `T.CreateMock()`, `T.CreateMock(args)`                                       |
| `Substitute.For<T, IOther>()`                               | `T.CreateMock().Implementing<IOther>()`                                      |
| `Substitute.ForPartsOf<T>()`                                | `T.CreateMock()` (class mocks call the base class by default)                |
| `Substitute.ForTypeForwardingTo<TInterface, TClass>(args)`  | `TInterface.CreateMock().Wrapping(new TClass(args))`                         |
| `sub.Method(args).Returns(value)`                           | `sub.Mock.Setup.Method(args).Returns(value)`                                 |
| `sub.Method(args).Returns(a, b, c)`                         | `sub.Mock.Setup.Method(args).Returns(a).Returns(b).Returns(c).Forever()`     |
| `.Throws<TException>()`, `.Throws(exception)`               | `.Throws<TException>()`, `.Throws(exception)`                                |
| `.ReturnsForAnyArgs(value)`, `.ThrowsForAnyArgs<E>()`       | `.AnyParameters().Returns(value)`, `.AnyParameters().Throws<E>()`            |
| `.Returns(value).AndDoes(callback)`                         | `.Returns(value).Do(callback)`                                               |
| `sub.Property.Returns(value)`                               | `sub.Mock.Setup.Property.Returns(value)`                                     |
| `sub.Received().Method(args)`                               | `sub.Mock.Verify.Method(args).AtLeastOnce()`                                 |
| `sub.Received(n).Method(args)`                              | `sub.Mock.Verify.Method(args).Exactly(n)` (`.Once()` for 1)                  |
| `sub.DidNotReceive().Method(args)`                          | `sub.Mock.Verify.Method(args).Never()`                                       |
| `sub.ReceivedWithAnyArgs().Method(…)`                       | `sub.Mock.Verify.Method(…).AnyParameters().AtLeastOnce()`                    |
| `sub.DidNotReceiveWithAnyArgs().Method(…)`                  | `sub.Mock.Verify.Method(…).AnyParameters().Never()`                          |
| `_ = sub.Received().Prop`, `sub.Received().Prop = value`    | `sub.Mock.Verify.Prop.Got().AtLeastOnce()`, `….Set(value).AtLeastOnce()`     |
| `sub.ClearReceivedCalls()`                                  | `sub.Mock.ClearAllInteractions()`                                            |
| `sub.Event += Raise.Event()`                                | `sub.Mock.Raise.Event(null, EventArgs.Empty)`                                |
| `sub.Event += Raise.EventWith(sender, args)`                | `sub.Mock.Raise.Event(sender, args)` (`null` without a sender)               |
| `sub.Event += Raise.Event<TDelegate>(args)`                 | `sub.Mock.Raise.Event(args)`                                                 |
| `sub.When(x => x.Method(args)).Do(callback)`                | `sub.Mock.Setup.Method(args).Do(callback)`                                   |
| `sub.When(x => x.Method(args)).DoNotCallBase()`             | `sub.Mock.Setup.Method(args).SkippingBaseClass()`                            |
| `Arg.Any<T>()`                                              | `It.IsAny<T>()`                                                              |
| `Arg.Is<T>(predicate)`                                      | `It.Satisfies<T>(predicate)`                                                 |
| `Arg.Is(value)`, `Arg.Is<T>(value)`                         | `It.Is(value)`, `It.Is<T>(value)`                                            |
| `Arg.Compat.Any<T>()` and the other `Arg.Compat` matchers   | the same as the matching `Arg` matcher                                       |
| `sub.Child.Method(args).Returns(value)`                     | `sub.Child.Mock.Setup.Method(args).Returns(value)` and a `// TODO` comment   |

## What needs manual review

The code fix leaves a `// TODO(MockolateM001)` or `// TODO(MockolateM002)` comment where it cannot finish the
migration on its own. Search for them after you apply the fix:

- **Nested mocks**: Moq and NSubstitute create mocks for nested members (`m => m.Child.Prop`) automatically.
  Mockolate does not, so set up the parent to return a mock for `Child` yourself.
- **CallInfo callbacks** that cannot be rewritten safely, for example when they use `call` directly, compute the
  index at runtime, assign `out` or `ref` arguments, call an ambiguous `call.Arg<T>()`, or declare local variables
  with the same names as the parameters of the method.

The code fix does not mark these cases:

- **Call order**: the code fix removes Moq's `InSequence(…)` wrapper from the setups. Mockolate checks the order
  after the calls instead: add a [`Then(…)`](../07-verify-interactions.md#call-order) chain to the verification.
- **Constructs that are not in the tables above**, for example Moq's `Verifiable()`, `VerifyAll()`, `Protected()`,
  `As<T>()` or `Mock.Of<T>()`, and NSubstitute's `Arg.Do<T>(…)`, `Received.InOrder(…)` or `Configure()`. Rewrite
  them by hand; the [translation table](/Mockolate/migration/#translation-table) shows the Mockolate equivalents.
