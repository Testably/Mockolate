---
description: Answers to common problems with Mockolate, and the types and members that cannot be mocked.
---

# Troubleshooting

Each section starts with a symptom and gives a short answer with a link to the detailed page. The [limitations](#limitations) at the end list what Mockolate cannot mock and why.

## Creating mocks

### `CreateMock()` or the `Mock` property does not exist

`CreateMock()` and the `Mock` property are C# 14 extension members that the source generator emits into the `Mockolate` namespace. When the compiler does not find them, check:

- **The `using` directive**: add `using Mockolate;` (or a global using) to the test file.
- **The SDK and the language version**: the generator needs the compiler of the [.NET 10 SDK](./01-getting-started.mdx#requirements). If your test project pins `<LangVersion>`, it must be `14` or later (or `latest`). An older compiler cannot load the generator, so it emits no mocks.
- **The package reference**: the generator and the analyzers are in the `analyzers` folder of the `Mockolate` package. A `PackageReference` that excludes the `analyzers` asset (with `ExcludeAssets`, or an `IncludeAssets` without it) also excludes the generator.
- **The project language**: the generator is packaged for C# projects only.

If the IDE keeps showing outdated generated members after you fixed the cause, run `dotnet clean` and build again.

### `CreateMock()` returns `Mock.IMockGenerationDidNotRun`

Besides the mocks, the generator emits a fallback `CreateMock()` that accepts any type. The compiler picks it when the generator emitted no mock for your type, usually because the type cannot be mocked. Assigning its result to your type does not compile, and calling it throws a `MockException`:

```csharp
public class LockedChocolateDispenser
{
    private LockedChocolateDispenser() { }
}

_ = LockedChocolateDispenser.CreateMock();
```

```text title="Failure message"
This method should not be called directly. Either 'MyNamespace.LockedChocolateDispenser' is not mockable or the source generator did not run correctly.
```

Check the type against the [limitations](#types-that-cannot-be-mocked). For most of them, the [Mockolate0002](./10-analyzers.md#mockolate0002) analyzer already fails the build with the reason:

```csharp
public sealed class SealedChocolateDispenser { }

// Mockolate0002: Type 'MyNamespace.SealedChocolateDispenser' cannot be mocked: type is sealed
SealedChocolateDispenser dispenser = SealedChocolateDispenser.CreateMock();
```

### The mock has `Mockolate_Mock` instead of `Mock`

When the mocked type already has a member named `Mock`, the generator names the accessor `Mockolate_Mock` instead, so that your member stays reachable. If that name is taken as well, it appends a number (`Mockolate_Mock__1`, `Mockolate_Mock__2`, …).

```csharp
public interface IChocolateMachine
{
    int Mock { get; }
}

IChocolateMachine machine = IChocolateMachine.CreateMock();
machine.Mockolate_Mock.Setup.Mock.Returns(4);

await That(machine.Mock).IsEqualTo(4);
```

### `.Implementing<T>()` has no effect on my mock

`.Implementing<T>()` returns a new mock and leaves the original one unchanged. Keep the returned reference, and reach the setups of the additional interface with `Mock.As<T>()`. See [Implementing additional interfaces](./04-create-mocks.md#implementing-additional-interfaces).

## Setups and default values

### A member without setup returns a value instead of failing

By default, a mock never fails for a missing setup. Methods return a [default value](./04-create-mocks.md#default-values) (`false`, `0`, an empty string, an empty collection, a completed task, or `null` for other reference types), and properties and indexers store the values written to them. A method that returns an interface or a class therefore returns `null` until you set it up.

To fail on every call without a matching setup, create the mock with [`ThrowingWhenNotSetup()`](./04-create-mocks.md#customizing-mock-behavior). To change the default value of a type, use `WithDefaultValueFor<T>(…)`.

### A strict mock throws `MockNotSetupException`

A mock created with `ThrowingWhenNotSetup()` throws a `MockNotSetupException` for every call that no setup matches, also when other setups of the same member exist:

```csharp
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);

dispenser.Dispense("Milk", 1);
```

```text title="Failure message"
The method 'global::MyNamespace.IChocolateDispenser.Dispense(string, int)' was invoked without prior setup.
```

For a property, the message reads `The property '…' was accessed without prior setup.` Add a setup for the reported member, or use a [matcher](./06-parameter-matching.md#basic-matchers) such as `It.IsAny<string>()` so that the existing setup covers the call.

### A setup is ignored

When several setups match a call, the one defined last wins. A general setup defined after a specific one therefore hides it. Every `dispenser.Mock.Setup.X` statement also starts a new setup, so chain everything that belongs to one setup. See [What happens when the mock is called](./02-core-concepts/00-index.md#what-happens-when-the-mock-is-called).

### My class mock runs the real implementation

A class mock can only override `virtual` and `abstract` members. Other members always run the real implementation (see [members that are not mocked](#members-that-are-not-mocked)). A virtual member also calls the base class implementation when a setup matches; the setup's return value takes precedence. Use [`SkippingBaseClass()`](./04-create-mocks.md#customizing-mock-behavior) to turn the base class calls off.

## Verifications

### `.Once()` and the other count methods do not exist

The count methods are extension methods in the `Mockolate.Verify` namespace. Add `using Mockolate.Verify;` to the test file. See [Verify interactions](./07-verify-interactions.md).

### The build fails with Mockolate0001

A verification without a count method checks nothing. Add a count such as `.Once()` or `.AtLeastOnce()`; the code fix adds `.AtLeastOnce()` for you. See [Mockolate0001](./10-analyzers.md#mockolate0001).

### A verification fails although the method was called

The `MockVerificationException` states the expected call with its arguments and how often it actually happened:

```csharp
dispenser.Dispense("Dark", 1);
dispenser.Dispense("Milk", 2);

dispenser.Mock.Verify.Dispense(It.IsAny<string>(), It.IsAny<int>()).AtLeast(3);
```

```text title="Failure message"
Expected that mock invoked method Dispense(It.IsAny<string>(), It.IsAny<int>()) at least 3 times, but it did twice.
```

When the count is lower than you expect, check:

- **The arguments**: a plain value matches with `EqualityComparer<T>.Default`, so a class without its own `Equals` only matches the same instance. Use `It.Satisfies<T>(…)` or a [custom equality comparer](./06-parameter-matching.md#custom-equality) instead.
- **Background threads**: if the code under test calls the mock asynchronously, the call may not have happened yet. Use [`Within(…)`](./07-verify-interactions.md#waiting-for-background-calls).
- **A monitor or a reset**: interactions outside a [monitor](./08-advanced-features/04-monitor-interactions.md) scope, or before `ClearAllInteractions()`, are not counted.

A mock created with `SkippingInteractionRecording()` records nothing, so every verification on it throws a `MockException`.

## Limitations

Mockolate generates mocks at compile time, so it can only override what C# lets a derived class override, and only what your test assembly can access. The generator skips what it cannot mock; the analyzers report most of these cases.

### Types that cannot be mocked

| Type                                                                                                                             | Reported by                                                                                         |
|----------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| A struct, an enum, a record or a sealed class (delegates are supported)                                                          | [Mockolate0002](./10-analyzers.md#mockolate0002)                                                    |
| A type declared in the global namespace                                                                                          | [Mockolate0002](./10-analyzers.md#mockolate0002)                                                    |
| Any other kind of type, such as an array                                                                                         | [Mockolate0002](./10-analyzers.md#mockolate0002)                                                    |
| A type with an abstract member that your assembly cannot access, or whose signature uses a type your assembly cannot access     | [Mockolate0002](./10-analyzers.md#mockolate0002)                                                    |
| A class in `.Implementing<T>()`: additional types must be interfaces                                                             | [Mockolate0002](./10-analyzers.md#mockolate0002)                                                    |
| A class without a `public`, `protected` or `protected internal` constructor whose parameter types your assembly can access      | no diagnostic; the fallback `CreateMock()` throws a [`MockException`](#createmock-returns-mockimockgenerationdidnotrun) |
| A nested type declared `private` or `private protected`                                                                          | no diagnostic; the fallback `CreateMock()` throws a [`MockException`](#createmock-returns-mockimockgenerationdidnotrun) |

### Members that are not mocked

These members keep their real behavior, or throw, and get no `Setup` or `Verify` members:

- **Non-virtual members of a class** cannot be overridden and always run the real implementation. If the class implements them for an interface, [`.Implementing<T>()`](./04-create-mocks.md#implementing-additional-interfaces) makes the calls through the interface mockable.
- **Sealed members** (`sealed override`) cannot be overridden either.
- **Static members of a class** cannot be virtual. `static abstract` and `static virtual` interface members are supported on .NET 8 or later; see [Static interface members](./08-advanced-features/02-static-interface-members.md).
- **`internal` and `private protected` virtual members** declared in another assembly are only overridden when that assembly grants your test assembly `InternalsVisibleTo`.
- **Virtual members whose signature uses an inaccessible type**: when the return type, a parameter type or a generic constraint names a type your test assembly cannot access (including a `protected` nested type), the member is not overridden, because the setup and verification members would have to name that type.
- **Members with ref structs** other than `Span<T>` and `ReadOnlySpan<T>` in an unsupported position, such as a returned ref struct, have no setup; calling them throws a `NotSupportedException`, unless a virtual class member can forward to the base class. The [Mockolate0003](./10-analyzers.md#mockolate0003) warning lists the cases and the requirements for [ref struct parameters](./06-parameter-matching.md#ref-struct-parameters-net-9).
- **Generic methods with an `allows ref struct` type parameter** that is used in the return type or in a parameter type throw a `NotSupportedException` when called. No analyzer reports them.

For the abstract counterparts of the inaccessible members above, the mock cannot be created at all, and [Mockolate0002](./10-analyzers.md#mockolate0002) reports the type.

:::note[Members with more than four parameters]

For members with more than four parameters, a setup or verification takes either values for all parameters or matchers for all of them. See [Mixing values and matchers](./06-parameter-matching.md#basic-matchers).

:::
