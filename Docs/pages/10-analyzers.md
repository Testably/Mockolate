---
description: The Roslyn analyzers that ship with Mockolate and catch mocking mistakes at compile time.
---

# Analyzers

Mockolate ships with Roslyn analyzers that catch mistakes at compile time. They are part of the `Mockolate` package,
so there is nothing to install.

## Mockolate0001

**A verification without a count assertion.**

`Verify` methods only return a `VerificationResult` and do not throw on their own. You have to specify how often you
expect the call to happen, e.g. `.AtLeastOnce()` or `.Exactly(n)`, or use the verification result in another way.

```csharp
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Dispense("Dark", 1);
// Mockolate0001: Add a count assertion like .AtLeastOnce() or use the result.
dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>());
```

The included code fix adds the `.AtLeastOnce()` count assertion:

```csharp
dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>()).AtLeastOnce();
```

## Mockolate0002

**A type that cannot be mocked.**

This rule prevents you from mocking unsupported types:

- `CreateMock()`: the type must be an interface, a delegate or a class that can be derived from (e.g. not `sealed`).
- `Implementing<T>()`: the type must be an interface.

It also fires when the type has a member that the mock would have to implement (an abstract member, or an interface
member without a default implementation), but that your assembly cannot access: for example an `internal abstract` or
`private protected abstract` member, or a `{ get; internal set; }` property, on a type from a referenced assembly that
does not grant `InternalsVisibleTo`. There is no valid code a mock could emit for such a member, so the type cannot be
mocked at all.

<details>
<summary>When a derived type already implements the inaccessible member</summary>

The rule only considers members that the mock still has to implement. If a more derived type in the referenced
assembly already overrides the inaccessible member, that type stays mockable:

```csharp
// In a referenced assembly that does not grant InternalsVisibleTo:
public abstract class Dispenser
{
    internal abstract void Refill();
}

public abstract class ChocolateDispenser : Dispenser
{
    internal override void Refill() { }
}

// Mockolate0002 fires for Dispenser, but ChocolateDispenser mocks fine.
ChocolateDispenser dispenser = ChocolateDispenser.CreateMock();
```

`Refill` itself is not part of the mock, since your assembly cannot see it. An `internal abstract override`
re-declaration does not count as an implementation, so the rule still fires for it.

The same applies per accessor. For a property such as `public abstract string Flavour { get; internal set; }`, an
override further down that implements the setter makes the type mockable. The mock then overrides the getter only, and
`Setup` and `Verify` only offer the getter, since writes never reach the mock. See
[properties with only one accessor](setup/02-properties.md#properties-with-only-one-accessor) and
[indexers with only one accessor](setup/03-indexers.md#indexers-with-only-one-accessor).

</details>

## Mockolate0003

**A member with a ref struct that Mockolate cannot set up.**

The mock still compiles and everything else on the type stays mockable, but the reported member gets no `Setup` or
`Verify` members, and calling it throws a `NotSupportedException`. The rule fires in two situations.

**1. The project does not meet the requirements for ref struct parameters**

Custom ref struct parameters (anything other than `Span<T>` and `ReadOnlySpan<T>`) need a target framework of .NET 9
or later and C# 13 or later. Upgrade the target framework or `<LangVersion>` to resolve the warning.

**2. The member uses a ref struct in a position that is never supported**

- A method or delegate that **returns** a custom ref struct, a property of a custom ref struct type, or an indexer
  whose value is a custom ref struct.
- A **delegate** with a custom ref struct parameter, or with a `ref readonly Span<T>` / `ref readonly ReadOnlySpan<T>`
  parameter.

`Span<T>` and `ReadOnlySpan<T>` are supported everywhere else, both as parameters and as values.

<details>
<summary>Virtual members of class mocks are not reported</summary>

A `virtual` member of a mocked class keeps working: the mock forwards the call to the wrapped instance, or to the base
class implementation, so there is nothing to fix. The forwarded call ignores the
[`MockBehavior`](04-create-mocks.md#customizing-mock-behavior) settings, because there is no setup to apply them to, and
it is not recorded.

Interface members, `abstract` members, `init` accessors and `ref`-returning methods have nothing to forward to. These
are the ones the warning reports.

</details>

See [Ref struct parameters](06-parameter-matching.md#ref-struct-parameters-net-9) for the supported features.
