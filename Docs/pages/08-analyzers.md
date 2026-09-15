# Analyzers

Mockolate ships with some Roslyn analyzers to help you adopt best practices and catch issues early, at compile time.
All rules provide actionable messages and link to identifiers for easy filtering.

## Mockolate0001

`Verify` methods only return a `VerificationResult` and do not directly throw. You have to specify how often you expect
the call to happen, e.g. `.AtLeastOnce()`, `.Exactly(n)`, etc. or use the verification result in any other way.

**Example:**

```csharp
IChocolateDispenser sut = IChocolateDispenser.CreateMock();
sut.Dispense("Dark", 1);
// Analyzer Mockolate0001: Add a count assertion like .AtLeastOnce() or use the result.
sut.Mock.Verify.Dispense(It.Is("Dark"), It.IsAny<int>());
```

The included code fixer suggests to add the `.AtLeastOnce()` count assertion:

```csharp
sut.Mock.Verify.Dispense(It.Is("Dark"), It.IsAny<int>()).AtLeastOnce();
```

## Mockolate0002

Mocked types must be mockable. This rule will prevent you from using unsupported types:

- `CreateMock()`  
  Type must be an interface, a delegate or a supported class (e.g. not sealed)
- `Implementing<T>()`  
  Type must be an interface

It also fires when the type has a member that the mock would have to implement (an abstract member,
or an interface member without a default implementation) but that is not accessible from your
assembly: for example an `internal abstract` or `private protected abstract` member, or a
`{ get; internal set; }` property, on a type from a referenced assembly that does not grant
`InternalsVisibleTo`. There is no valid code a mock could emit for such a member, so the type cannot
be mocked at all.

The rule only considers members the mock is still obliged to implement. If a more derived type in the
referenced assembly already overrides the inaccessible member, the obligation is discharged and the
type stays mockable:

```csharp
// In a referenced assembly that does not grant InternalsVisibleTo:
public abstract class Dispenser
{
    internal abstract void Refill();
}

public abstract class ChocolateDispenser : Dispenser
{
    internal override void Refill() { }  // obligation discharged
}

// Mockolate0002 fires for Dispenser, but ChocolateDispenser mocks fine.
ChocolateDispenser sut = ChocolateDispenser.CreateMock();
```

`Refill` itself is not part of the mock's surface, since your assembly cannot see it. An
`internal abstract override` re-declaration is not a discharge: it continues the obligation without
implementing it, so the rule still fires.

The same applies per accessor. For a property whose accessors differ in accessibility, such as
`public abstract string Flavour { get; internal set; }`, an override further down discharges only the
inaccessible half. The mock then overrides the accessor it can see and leaves the other one to the
referenced assembly's implementation. Because writes never reach the mock, `Setup` and `Verify` expose
only the getter for such a property, so a write that could never be recorded is not offered for
configuration or verification. The same narrowing applies to indexers. See
[properties with only one accessor](setup/properties#properties-with-only-one-accessor) and
[indexers with only one accessor](setup/indexers#indexers-with-only-one-accessor) for details.

## Mockolate0003

A mocked member uses a ref struct in a way Mockolate can't emit setup surface for. The warning fires
in two distinct situations.

**1. Compilation prerequisites not met**

The ref-struct setup pipeline requires both:

- A target framework of .NET 9 or later (Mockolate's ref-struct setup types are
  `#if NET9_0_OR_GREATER`-gated).
- An effective C# language version of 13 or later (uses the `allows ref struct` anti-constraint).

When either prerequisite is missing, the warning fires for any member that passes a non-`Span<T>` /
non-`ReadOnlySpan<T>` ref struct by value, or uses one as an indexer key. Upgrade the target
framework and/or `<LangVersion>` to resolve it.

**2. Signature shapes that are never supported**

These fire on every compilation target, including .NET 9+ / C# 13+, because the shape has no setup
surface to route into rather than because the pipeline is missing.

A non-`Span<T>` / non-`ReadOnlySpan<T>` ref struct in a **value position**:

- Methods and delegates returning one.
- Properties typed as one.
- Indexers whose value is one.

Value positions parameterize types that store a `Func<T>` (`IReturnMethodSetup<T>`,
`IPropertyGetterOnlySetup<T>`, `IIndexerGetterOnlySetup<TValue, ...>`), which is illegal for a ref
struct, so they can't carry the `allows ref struct` anti-constraint the parameter positions use.

Also never supported:

- Ref-struct **parameters on delegate types**. A delegate mock projects its single `Invoke` onto
  `VoidMethodSetup<T>` / `ReturnMethodSetup<T>`, neither of which carries the anti-constraint the
  interface and class pipelines get from `RefStructVoidMethodSetup<T>`.

Members matching any of these still compile: the generated mock keeps the member, but no setup or
verify surface is emitted for it. A `virtual` member on a mocked class forwards to the wrapped
instance (or to `base`) so it keeps behaving like the real one; an interface or `abstract` member has
nothing to forward to and throws `NotSupportedException`. Everything else on the type stays mockable.

**Note:**
`Span<T>` and `ReadOnlySpan<T>` flow through the existing `SpanWrapper` / `ReadOnlySpanWrapper`
fallback and are never flagged - in parameter *and* value positions. On .NET 9+ with C# 13+, custom
ref-struct parameters (by value, `out`, `ref`, and `ref readonly`) and ref-struct-keyed indexers
(getter-only, setter-only, and get+set) on interfaces and classes are fully supported.

See the [Ref Struct Parameters](setup/parameter-matching#ref-struct-parameters-net-9) section
for the supported surface.
