# Parameter Matching

Mockolate provides flexible parameter matching for method setups and verifications:

## Parameter Matchers

### Basic Matchers

- `It.IsAny<T>()`: Matches any value of type `T`.
- `It.Is<T>(value)`: Matches a specific value.
- `It.IsNot<T>(value)`: Matches any value not equal to `value`.
- `It.IsOneOf<T>(params IEnumerable<T> values)`: Matches any of the given values.
- `It.IsNotOneOf<T>(params IEnumerable<T> values)`: Matches any value that is not in the given set.
- `It.IsNull<T>()`: Matches null.
- `It.IsNotNull<T>()`: Matches any non-null value.
- `It.IsTrue()`/`It.IsFalse()`: Matches boolean true/false.
- `It.IsInRange(min, max)`: Matches a number within the given range. You can append `.Exclusive()` to exclude the
  minimum and maximum value.
- `It.Satisfies<T>(predicate)`: Matches values based on a predicate.

**Note:**  
You can also directly use a value (equivalent to wrapping it in `It.Is<T>(value)`). For methods and indexers with
up to 4 parameters, you can freely mix matchers with direct values in any position. For members with more than
4 parameters, Mockolate still supports explicit values, but limits arbitrary per-parameter mixing to avoid a
combinatorial explosion of overloads. You may need to use direct values for all explicit-value-capable parameters
and wrap the remaining ones, or use matchers for all parameters.

### String Matching

- `It.Matches(pattern)`: Matches strings using wildcard patterns (`*` and `?`).

#### Regular Expressions

Use `.AsRegex()` to enable regular expression matching for `It.Matches()`:

```csharp
// Example: Match email addresses
sut.Mock.Setup.ValidateEmail(It.Matches(@"^\w+@\w+\.\w+$").AsRegex())
    .Returns(true);

bool result = sut.ValidateEmail("user@example.com");

// Case-sensitive regex
sut.Mock.Setup.Process(It.Matches("^[A-Z]+$").AsRegex().CaseSensitive())
    .Returns(1);
```

### Ref and Out Parameters

- `It.IsRef<T>(setter)`: Matches any `ref` parameter and sets a new value using the setter function.
- `It.IsRef<T>(predicate, setter)`: Matches `ref` parameters that satisfy the predicate and sets a new value.
- `It.IsRef<T>(predicate)`: Matches `ref` parameters that satisfy the predicate without changing the value.
- `It.IsAnyRef<T>()`: Matches any `ref` parameter without restrictions.
- `It.IsOut<T>(setter)`: Matches any `out` parameter and sets a value using the setter function.
- `It.IsAnyOut<T>()`: Matches any `out` parameter without restrictions and sets the parameter to the default value of
  `T`.

```csharp
// Example: Setup with out parameter
sut.Mock.Setup.TryParse(It.IsAny<string>(), It.IsOut(() => 42))
    .Returns(true);

int result;
bool success = sut.TryParse("abc", out result);
// result == 42, success == true

// Example: Setup with ref parameter
sut.Mock.Setup.Increment(It.IsRef<int>(v => v + 1))
    .Returns(true);

int value = 5;
sut.Increment(ref value);
// value == 6
```

### Collection Matchers

- `It.Contains<T>(item)`: Matches a collection parameter that contains `item`.
- `It.SequenceEquals<T>(params IEnumerable<T> values)`: Matches a collection parameter whose elements equal `values` in
  the same order.

Both matchers support method parameters declared as `IEnumerable<T>`, `ICollection<T>`, `IList<T>`,
`IReadOnlyCollection<T>`, `IReadOnlyList<T>`, `T[]`, `List<T>`, `Queue<T>` or `Stack<T>`.
`It.Contains<T>` additionally supports the unordered shapes `ISet<T>` and `HashSet<T>`; `It.SequenceEquals<T>` intentionally
does not, because their enumeration order is not guaranteed.

Append `.Using(IEqualityComparer<T>)` to either matcher to control element equality.

```csharp
// Example: Match a list that contains a specific item
sut.Mock.Setup.Process(It.Contains(5))
    .Returns(true);

bool result = sut.Process(new[] { 1, 2, 5 });
// result == true

// Example: Match a sequence of items in order
sut.Mock.Setup.Process(It.SequenceEquals("a", "b", "c"))
    .Returns(true);

bool match = sut.Process(new[] { "a", "b", "c" });
// match == true

// Example: Case-insensitive containment
sut.Mock.Setup.Process(It.Contains("HELLO").Using(StringComparer.OrdinalIgnoreCase))
    .Returns(true);
```

### Custom Equality Comparers

Use `.Using(IEqualityComparer<T>)` to provide custom equality comparison for `It.Is()` and `It.IsOneOf()`:

```csharp
// Example: Case-insensitive string comparison
var comparer = StringComparer.OrdinalIgnoreCase;
sut.Mock.Setup.Process(It.Is("hello").Using(comparer))
    .Returns(42);

int result = sut.Process("HELLO");
// result == 42
```

### Covariant Type Matching

Parameter matchers are covariant in their type argument: when a method declares a parameter of a base type, you can
narrow the match by supplying a matcher for a derived type. Only calls whose actual argument is an instance of that
derived type (or a further-derived type) match the setup; calls passing other runtime types fall through.

```csharp
public abstract class Chocolate { }
public class DarkChocolate : Chocolate { }
public class MilkChocolate : Chocolate { }

public interface IChocolateBox
{
    bool Add(Chocolate chocolate);
}

IChocolateBox sut = IChocolateBox.CreateMock();

// Narrow the match to DarkChocolate, even though Add accepts any Chocolate.
sut.Mock.Setup.Add(It.IsAny<DarkChocolate>()).Returns(true);

bool dark = sut.Add(new DarkChocolate());  // true, matched the DarkChocolate setup
bool milk = sut.Add(new MilkChocolate());  // false, no setup matched -> default

// Verifications are covariant too: only dark-chocolate additions are counted.
sut.Mock.Verify.Add(It.IsAny<DarkChocolate>()).Once();
```

### Span Parameters (.NET 8+)

- `It.IsSpan<T>(predicate)`: Matches `Span<T>` parameters that satisfy the predicate.
- `It.IsAnySpan<T>()`: Matches any `Span<T>` parameter.
- `It.IsReadOnlySpan<T>(predicate)`: Matches `ReadOnlySpan<T>` parameters that satisfy the predicate.
- `It.IsAnyReadOnlySpan<T>()`: Matches any `ReadOnlySpan<T>` parameter.

**Note:**  
As `ref struct` types cannot be stored directly, it is converted to an array internally and the `predicate` receives
this array for evaluation.

```csharp
// Example: Setup with Span parameter
sut.Mock.Setup.Process(It.IsSpan<byte>(data => data.Length > 0))
    .Returns(true);

Span<byte> buffer = new byte[] { 1, 2, 3 };
bool result = sut.Process(buffer);
// result == true
```

### Ref Struct Parameters (.NET 9+)

You can mock methods and indexers that take custom `ref struct` parameters (e.g. `Utf8JsonReader`
or your own `ref struct Packet`) using these matchers:

- `It.IsAnyRefStruct<T>()`: Matches any ref-struct value of type `T`.
- `It.IsRefStruct<T>(predicate)`: Matches ref-struct values that satisfy the predicate. The
  predicate can read the struct's fields at the time the call is made.
- `It.IsRefStructBy<T, TKey>(projection)` / `It.IsRefStructBy<T, TKey>(projection, predicate)`:
  For ref-struct-keyed indexers, projects the key to an equatable value so writes and reads can
  be correlated. Works at any arity - apply it to every ref-struct slot and non-ref-struct slots
  contribute their raw value to the composite dispatch key (see *Indexer storage* in the remarks).

```csharp
public readonly ref struct Packet(int id, ReadOnlySpan<byte> payload)
{
    public int Id { get; } = id;
    public ReadOnlySpan<byte> Payload { get; } = payload;
}

public interface IPacketSink
{
    void Consume(Packet packet);
    int TryParse(Packet packet);
    string this[Packet key] { get; set; }
}

// Match on the live ref struct, including its Span contents.
sut.Mock.Setup.Consume(It.IsRefStruct<Packet>(p =>
        p.Payload.Length > 0 && p.Payload[0] == 0xFF))
    .Throws<InvalidOperationException>();

// Return a value from a ref-struct-parameter method.
sut.Mock.Setup.TryParse(It.IsAnyRefStruct<Packet>()).Returns(42);

// Ref-struct-keyed indexer: Returns for get, OnSet for observed writes.
sut.Mock.Setup[It.IsAnyRefStruct<Packet>()]
    .Returns("got")
    .OnSet(value => { /* observed write */ });

// Correlate writes and reads by projecting the key to an equatable value.
sut.Mock.Setup[It.IsRefStructBy<Packet, int>(p => p.Id)].Returns("fallback");
sut[new Packet(1, [])] = "written";
string a = sut[new Packet(1, [])];  // "written" matched by Id
string b = sut[new Packet(2, [])];  // "fallback" no write under Id=2
```

**Remarks**

The ref-struct pipeline uses a narrower API than the rest of Mockolate. A handful of fluent
features are unavailable because the C# language does not let `ref struct` values flow through
generic delegates:

- **Setup surface.** Only `Returns(value)`, `Returns(factory)`, `Throws*`, `OnSet(Action<TValue>)`
  (indexer setters), and `SkippingBaseClass` are available. The `.Do(...)` callback and the
  `Callbacks<T>` builder (`InParallel`, `When`, `For`, `Only`, `TransitionTo`) are not offered
  for ref-struct parameters.
- **Verify.** `Verify` counts calls to the method but cannot match on the parameter value after
  the fact - the ref-struct value isn't retained past the call. Use a setup-time matcher to
  filter at call time.
- **Indexer storage.** By default, values written through a ref-struct-keyed indexer setter are
  not read back by the getter. Apply `It.IsRefStructBy<T, TKey>(projection)` to every ref-struct
  slot to enable write-then-read correlation keyed by the projections; non-ref-struct slots
  contribute their raw value as part of the composite dispatch key. If any ref-struct slot is
  matched without a projection, storage stays inactive for that setup.

The following cases are rejected at compile time with diagnostic `Mockolate0003`:

- Targeting older than .NET 9 (the feature relies on `allows ref struct`, a .NET 9 / C# 13
  feature).
- `out` / `ref` / `ref readonly` parameters of a ref-struct type.
- Methods that return a custom ref struct. (`Span<T>` / `ReadOnlySpan<T>` returns are supported.)

## Union Parameters (C# 15)

When the consuming project compiles with C# 15, Mockolate generates the setup and verify overloads with union-typed
parameters: every value-capable parameter becomes a `ParameterArg<T>?` that accepts an `It` matcher or a direct value,
and a `Func<T, bool>` overload accepts a predicate. The number of overloads stays the same as before, but predicates no
longer need `It.Satisfies`:

```csharp
sut.Mock.Setup.Dispense(type => type.StartsWith("D"), It.IsAny<int>()).Returns(true);

sut.Mock.Verify.Dispense("Dark", amount => amount > 10).Once();
```

Union mode is enabled automatically once the compiler ships C# 15 and the project's effective language version is
C# 15 or later. On a preview compiler, opt in with `<MockolateUnionParameters>true</MockolateUnionParameters>` in the
project file; `false` keeps the classic overloads on any compiler.

Behaviour of the union-typed parameters:

- `null` and `default` stand for the parameter's declared default value, or for `default(T)` when it has none, exactly
  as with the classic value overloads.
- A parameter whose type is a delegate offers the raw delegate instead of a predicate, so a lambda is still matched as
  a value.
- For an `object` parameter, a lambda literal is a predicate, while a delegate stored in a variable is a value.
- Within a union-typed member, `ref`, `out` and `ref readonly` parameters as well as `Span<T>` and `ReadOnlySpan<T>`
  parameters keep their matcher slot (no predicate for that parameter); `in` parameters behave like by-value ones.
- Overloaded method names (including a generic sibling; a same-named member in another scope — public, protected or
  static — does not count), generic methods, `params` methods, members with custom ref-struct parameters, and indexers
  that share their key count with another same-scope indexer keep the classic matcher/value overloads. Use
  `It.Satisfies` for predicates there.

The generated `ParameterArg<T>` type relies on `UnionAttribute`, `OverloadResolutionPriorityAttribute` and
`CallerArgumentExpressionAttribute`. Mockolate declares whichever of them the compilation lacks (neither the referenced
frameworks nor the project itself provide it). Another generator such as PolySharp can provide them without Mockolate
noticing; set `<MockolateUnionAttributePolyfills>false</MockolateUnionAttributePolyfills>` to declare that
`OverloadResolutionPriorityAttribute` and `CallerArgumentExpressionAttribute` come from elsewhere (`UnionAttribute`
keeps following the compilation), or list the attribute names the project provides.

## Parameter Predicates

When the method name is unique (no overloads), you can use argument matchers from the `Match` class for more flexible parameters matching:

- `Match.AnyParameters()`: Matches any parameter combination.
- `Match.Parameters(Func<object?[], bool> predicate)`: Matches parameters based on a custom predicate.

```csharp
// Example: Custom parameter predicate
sut.Mock.Setup.Process(Match.Parameters(args =>
    args.Length == 2 &&
    args[0] is string s && s.StartsWith("test") &&
    args[1] is int i && i > 0))
    .Returns(true);

bool result = sut.Process("test123", 5);
// result == true
```

## Parameter Interaction

### Callbacks

With `.Do`, you can register a callback for individual parameters of a method setup. This allows you to implement side
effects or checks directly when the method or indexer is called.

**Example: Do for method parameter**

```csharp
int lastAmount = 0;
sut.Mock.Setup.Dispense(It.Is("Dark"), It.IsAny<int>().Do(amount => lastAmount = amount));
sut.Dispense("Dark", 42);
// lastAmount == 42
```

### Monitor

With `.Monitor(out monitor)`, you can track the actual
values passed during test execution and analyze them afterward.

**Example: Monitor for method parameter**

```csharp
Mockolate.ParameterMonitor<int> monitor;
sut.Mock.Setup.Dispense(It.Is("Dark"), It.IsAny<int>().Monitor(out monitor));
sut.Dispense("Dark", 5);
sut.Dispense("Dark", 7);
// monitor.Values == [5, 7]
```
