# Indexers

Set up indexers with argument matchers. Supports initialization, returns/throws sequences, and callbacks.

```csharp
sut.Mock.Setup[It.IsAny<string>()]
    .InitializeWith(type => 20)
    .OnGet.Do(type => Console.WriteLine($"Stock for {type} was read"));

sut.Mock.Setup[It.Is("Dark")]
    .InitializeWith(10)
    .OnSet.Do((value, type) => Console.WriteLine($"Set [{type}] to {value}"));
```

## Initialization

You can initialize indexers so they work like normal indexers (setter changes the value, getter returns the last set
value):

```csharp
sut.Mock.Setup[It.IsAny<string>()].InitializeWith(42);
```

## Returns / Throws

Set up indexers with `Returns` and `Throws` (supports sequences):

```csharp
sut.Mock.Setup[It.IsAny<string>()]
    .Returns(1)
    .Returns(2)
    .Throws(new Exception("Error"))
    .Returns(4);
```

You can also return a value based on the previous value:

```csharp
sut.Mock.Setup[It.IsAny<string>()]
    .Returns(current => current + 10);  // Increment by 10 each read
```

## Callbacks

Register callbacks on the setter or getter of the indexer:

```csharp
sut.Mock.Setup[It.IsAny<string>()].OnGet
    .Do(() => Console.WriteLine("Indexer was read!"));
sut.Mock.Setup[It.IsAny<string>()].OnSet
    .Do(newValue => Console.WriteLine($"Changed indexer to {newValue}!") );
```

Callbacks can also receive the indexer parameters and the current value:

```csharp
// Getter with the current value
sut.Mock.Setup[It.IsAny<string>()]
    .OnGet.Do((string index, int value) => 
        Console.WriteLine($"Read this[{index}] current value: {value}"));

// Setter with the new value
sut.Mock.Setup[It.IsAny<string>()]
    .OnSet.Do((string index, int newValue) => 
        Console.WriteLine($"Set this[{index}] to {newValue}"));
```

Callbacks also support sequences, similar to `Returns` and `Throws`:

```csharp
sut.Mock.Setup[It.IsAny<string>()].OnGet
    .Do(() => Console.WriteLine("Execute on all even read interactions"))
    .Do(() => Console.WriteLine("Execute on all odd read interactions"));
```

## Indexers with only one accessor

The setup and verify surfaces only offer the accessors the mock actually intercepts.
`SkippingBaseClass(…)` stays available on both, but the accessor-specific members do not: a get-only
indexer has no `OnSet`, and a set-only indexer has neither `OnGet` nor the `Returns`/`Throws`
read-sequence nor `InitializeWith`, since there is no getter to read the value back.

```csharp
public interface IChocolateStorage
{
    int this[string shelf] { get; }    // no setter
    string this[int box] { set; }      // no getter
}

IChocolateStorage sut = IChocolateStorage.CreateMock();

sut.Mock.Setup[It.IsAny<string>()].Returns(3);
sut.Mock.Setup[It.IsAny<string>()].OnSet…                 // does not compile
sut.Mock.Setup[It.IsAny<string>()].Returns(3).OnSet…      // does not compile

sut.Mock.Setup[It.IsAny<int>()].OnSet.Do(value => { });
sut.Mock.Setup[It.IsAny<int>()].Returns("Ada")…           // does not compile
sut.Mock.Setup[It.IsAny<int>()].InitializeWith("Ada")…    // does not compile
sut.Mock.Setup[It.IsAny<int>()].OnSet.Do(value => { }).OnGet… // does not compile
```

The verify facade likewise offers the intercepted accessor only:

```csharp
_ = sut["top"];
sut[7] = "Ada";

await That(sut.Mock.Verify[It.Is("top")].Got()).Once();
await That(sut.Mock.Verify[It.Is(7)].Set("Ada")).Once();

sut.Mock.Verify[It.Is("top")].Set(3)…                     // does not compile
sut.Mock.Verify[It.Is(7)].Got()…                          // does not compile
```

This also applies when the indexer declares an accessor the mock cannot see, such as
`{ get; internal set; }` on a type from an assembly that does not grant `InternalsVisibleTo`. Writes
never reach the mock in that case, so configuring or verifying one could only ever report zero
interactions. See [Mockolate0002](../analyzers#mockolate0002) for when such a type is mockable at all.

Both facades are fully restricted: the fluent builders returned by `Returns`, `Throws`, `Do` and
`TransitionTo` stay on the narrowed surface, so no amount of chaining reaches the accessor the mock
does not intercept.

**Notes:**

- All callbacks support more advanced features like conditional execution, frequency control, parallel execution, and
  access to the invocation counter.
  See [Advanced callback features](../advanced-features/advanced-callback-features) for
  details.
- You can use the same [parameter matching](parameter-matching)
  and [interaction](parameter-matching#parameter-interaction) options as for
  methods.
- Use `.SkippingBaseClass(…)` to override the base class behavior for a specific indexer (only for class mocks).
- When you specify overlapping setups, the most recently defined setup takes precedence.

## Union Parameters (C# 15)

With C# 15, indexer keys accept predicates directly, in setups and verifications alike:

```csharp
sut.Mock.Setup[type => type.StartsWith("D")].Returns(10);

sut.Mock.Verify[type => type.StartsWith("D")].Got().Once();
```

Only an indexer that is the sole indexer of its key count on the type takes union-typed keys; indexers sharing a key
count keep the classic matcher/value overloads. See *Union Parameters* under Parameter Matching for the details.
