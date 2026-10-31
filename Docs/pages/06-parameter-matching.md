---
description: Match method, indexer and delegate parameters in setups and verifications with the It and Match classes.
---

# Parameter matching

Setups and verifications take one argument per parameter of the mocked member. Each argument decides which calls the
setup applies to, or which calls the verification counts. You can pass:

- a **value**, which matches by equality (the same as `It.Is(value)`), or
- a **matcher** from the `It` class, such as `It.IsAny<int>()`.

```csharp
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);
dispenser.Mock.Verify.Dispense(It.IsOneOf("Dark", "Milk"), It.IsInRange(1, 5)).AtLeastOnce();
```

The same matchers work everywhere: in method and indexer setups, in verifications, and for
[delegates](09-special-types/02-delegates.md).

<details>
<summary>Example types used on this page</summary>

Besides [`IChocolateDispenser`](/Mockolate/core-concepts/#mocks-and-the-system-under-test), this page uses:

```csharp
public interface IChocolateKitchen
{
    int Process(string recipe);
    bool ValidateEmail(string email);
    bool TryParse(string input, out int amount);
    bool Increment(ref int value);
    bool Fill(IEnumerable<int> amounts);
    bool Pack(IList<string> types);
    bool SetTemperature(double celsius);
    bool Scan(Span<byte> data);
}

IChocolateKitchen kitchen = IChocolateKitchen.CreateMock();
```

</details>

## Basic matchers

| Matcher                              | Matches                                                                                      |
|--------------------------------------|----------------------------------------------------------------------------------------------|
| `It.IsAny<T>()`                      | any value of type `T`                                                                        |
| `It.Is<T>(value)`                    | a value equal to `value`                                                                     |
| `It.IsNot<T>(value)`                 | any value not equal to `value`                                                               |
| `It.IsOneOf<T>(params values)`       | any of the given values                                                                      |
| `It.IsNotOneOf<T>(params values)`    | any value that is not one of the given values                                                |
| `It.IsNull<T>()`                     | `null`                                                                                       |
| `It.IsNotNull<T>()`                  | any value except `null`                                                                      |
| `It.IsTrue()` / `It.IsFalse()`       | `true` / `false`                                                                             |
| `It.IsInRange(min, max)`             | a value between `min` and `max` (inclusive); append `.Exclusive()` to exclude both bounds    |
| `It.Satisfies<T>(predicate)`         | any value for which the predicate returns `true`                                             |

```csharp
dispenser.Mock.Setup.Dispense(It.Satisfies<string>(type => type.StartsWith("D")), It.IsInRange(1, 10).Exclusive())
    .Returns(true);
```

:::note[Mixing values and matchers]

For members with up to 4 parameters, you can freely mix values and matchers in any position. For members with more
parameters, Mockolate limits the combinations to avoid an explosion of generated overloads: either pass values for all
parameters, or use matchers for all of them (wrap a value in `It.Is(value)`).

With [union parameters](#union-parameters-c-15), values and matchers mix freely for any number of parameters. Direct predicates are only offered for members with up to 4 parameters; above that, use `It.Satisfies`.

:::

### Custom equality

Use `.Using(IEqualityComparer<T>)` on `It.Is()`, `It.IsOneOf()`, `It.Contains()` or `It.SequenceEquals()` to provide
a custom equality comparison:

```csharp
kitchen.Mock.Setup.Process(It.Is("hello").Using(StringComparer.OrdinalIgnoreCase))
    .Returns(42);

int result = kitchen.Process("HELLO");

await That(result).IsEqualTo(42);
```

### Tolerance

Use `.Within(tolerance)` on `It.Is()` to accept values that differ from the expected value by at most the given
tolerance. It is available for `double`, `float` and `decimal` (numeric tolerance) as well as `DateTime`,
`DateTimeOffset` and `TimeSpan` (`TimeSpan` tolerance), and for their nullable counterparts:

```csharp
// Accept a tempering temperature that differs from 31.5 degrees by at most 0.1
kitchen.Mock.Setup.SetTemperature(It.Is(31.5).Within(0.1))
    .Returns(true);

bool result = kitchen.SetTemperature(31.45);

await That(result).IsTrue();
```

<details>
<summary>Tolerance semantics</summary>

- The bound is inclusive, but for `double` and `float` it is subject to the usual binary representation error, so
  values that lie exactly on the bound may or may not match.
- The tolerance must not be negative (or `NaN`); otherwise an `ArgumentOutOfRangeException` is thrown.
- `NaN` and the infinities keep matching themselves, just like without a tolerance.
- For nullable parameters, `null` only matches `null`; a tolerance never bridges the gap between `null` and a value.
- `.Within(tolerance)` cannot be combined with `.Using(IEqualityComparer<T>)`, because a custom comparer would
  replace the tolerance.

</details>

### Derived types

Matchers are covariant in their type argument: when a method declares a parameter of a base type, a matcher for a
derived type only matches calls whose argument is an instance of that derived type (or a further-derived type):

```csharp
public abstract class Chocolate { }
public class DarkChocolate : Chocolate { }
public class MilkChocolate : Chocolate { }

public interface IChocolateBox
{
    bool Add(Chocolate chocolate);
}

IChocolateBox box = IChocolateBox.CreateMock();
box.Mock.Setup.Add(It.IsAny<DarkChocolate>()).Returns(true);

bool dark = box.Add(new DarkChocolate());
bool milk = box.Add(new MilkChocolate());

await That(dark).IsTrue();
await That(milk).IsFalse().Because("no setup matches a MilkChocolate");

// Verifications are covariant too: only dark-chocolate additions are counted.
box.Mock.Verify.Add(It.IsAny<DarkChocolate>()).Once();
```

## String matching

`It.Matches(pattern)` matches strings with the wildcards `*` (any number of characters) and `?` (a single character).
Matching is case-insensitive by default:

```csharp
kitchen.Mock.Setup.Process(It.Matches("foo*")).Returns(1);   // starts with "foo"
kitchen.Mock.Setup.Process(It.Matches("*foo")).Returns(2);   // ends with "foo"
kitchen.Mock.Setup.Process(It.Matches("*foo*")).Returns(3);  // contains "foo"
```

Append `.CaseSensitive()` to require an exact case match, or `.AsRegex()` to treat the pattern as a regular
expression:

```csharp
kitchen.Mock.Setup.Process(It.Matches("Foo*").CaseSensitive())
    .Returns(1);

kitchen.Mock.Setup.ValidateEmail(It.Matches(@"^\w+@\w+\.\w+$").AsRegex())
    .Returns(true);

// Case-sensitive regex
kitchen.Mock.Setup.Process(It.Matches("^[A-Z]+$").AsRegex().CaseSensitive())
    .Returns(1);
```

To match a literal `*` or `?`, use a regular expression and escape the text:
`It.Matches(Regex.Escape("a*b") + "$").AsRegex()` matches strings that end with `a*b`.

## Collection matching

| Matcher                                 | Matches                                                          |
|-----------------------------------------|------------------------------------------------------------------|
| `It.Contains<T>(item)`                  | a collection that contains `item`                                |
| `It.SequenceEquals<T>(params values)`   | a collection whose elements equal `values` in the same order     |

Both matchers support parameters declared as `IEnumerable<T>`, `ICollection<T>`, `IList<T>`,
`IReadOnlyCollection<T>`, `IReadOnlyList<T>`, `T[]`, `List<T>`, `Queue<T>` or `Stack<T>`.
`It.Contains<T>` additionally supports `ISet<T>` and `HashSet<T>`; `It.SequenceEquals<T>` does not, because their
enumeration order is not guaranteed.

```csharp
kitchen.Mock.Setup.Fill(It.Contains(5))
    .Returns(true);
kitchen.Mock.Setup.Pack(It.SequenceEquals("Dark", "Milk", "White"))
    .Returns(true);
kitchen.Mock.Setup.Pack(It.Contains("DARK").Using(StringComparer.OrdinalIgnoreCase))
    .Returns(true);

bool filled = kitchen.Fill([1, 2, 5]);
bool packed = kitchen.Pack(["Dark", "Milk", "White"]);

await That(filled).IsTrue();
await That(packed).IsTrue();
```

## Ref and out parameters

| Matcher                              | In a setup                                                                       |
|--------------------------------------|----------------------------------------------------------------------------------|
| `It.IsOut<T>(setter)`                | matches any `out` parameter and assigns the value returned by `setter`           |
| `It.IsAnyOut<T>()`                   | matches any `out` parameter and assigns `default(T)`                             |
| `It.IsRef<T>(setter)`                | matches any `ref` parameter and replaces its value with `setter(value)`          |
| `It.IsRef<T>(predicate, setter)`     | matches `ref` parameters that satisfy the predicate and replaces their value     |
| `It.IsRef<T>(predicate)`             | matches `ref` parameters that satisfy the predicate, without changing the value  |
| `It.IsAnyRef<T>()`                   | matches any `ref` parameter, without changing the value                          |

```csharp
kitchen.Mock.Setup.TryParse(It.IsAny<string>(), It.IsOut(() => 42))
    .Returns(true);

bool success = kitchen.TryParse("abc", out int amount);

await That(success).IsTrue();
await That(amount).IsEqualTo(42);

kitchen.Mock.Setup.Increment(It.IsRef<int>(v => v + 1))
    .Returns(true);

int value = 5;
kitchen.Increment(ref value);

await That(value).IsEqualTo(6);
```

In verifications, use `It.IsOut<T>()` and `It.IsRef<T>()` without arguments to match any `out` or `ref` parameter:

```csharp
kitchen.Mock.Verify.TryParse("abc", It.IsOut<int>()).Once();
```

## Span parameters (.NET 8+)

:::info[.NET 8 or later]

The span matchers are available on .NET 8 or later.

:::

| Matcher                                | Matches                                                       |
|----------------------------------------|---------------------------------------------------------------|
| `It.IsSpan<T>(predicate)`              | `Span<T>` parameters that satisfy the predicate               |
| `It.IsAnySpan<T>()`                    | any `Span<T>` parameter                                       |
| `It.IsReadOnlySpan<T>(predicate)`      | `ReadOnlySpan<T>` parameters that satisfy the predicate       |
| `It.IsAnyReadOnlySpan<T>()`            | any `ReadOnlySpan<T>` parameter                               |

Spans cannot be stored, so Mockolate copies the span into an array and passes that array to the predicate:

```csharp
kitchen.Mock.Setup.Scan(It.IsSpan<byte>(data => data.Length > 0))
    .Returns(true);

Span<byte> buffer = new byte[] { 1, 2, 3 };
bool result = kitchen.Scan(buffer);

await That(result).IsTrue();
```

## Ref struct parameters (.NET 9+)

:::info[.NET 9 and C# 13 or later]

Custom `ref struct` parameters need a target framework of .NET 9 or later and C# 13 or later.

:::

You can also mock members that take custom `ref struct` parameters (e.g. `Utf8JsonReader` or your own `ref struct Packet`), by value and as `out`, `ref` and `ref readonly`:

| Matcher                                         | Matches                                                                               |
|-------------------------------------------------|---------------------------------------------------------------------------------------|
| `It.IsAnyRefStruct<T>()`                        | any ref struct value of type `T`                                                      |
| `It.IsRefStruct<T>(predicate)`                  | ref struct values that satisfy the predicate, evaluated at the time of the call       |
| `It.IsRefStructBy<T, TKey>(projection)`         | for indexers keyed by a ref struct: projects the key to a value that can be stored    |

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

IPacketSink sink = IPacketSink.CreateMock();

// Match on the live ref struct, including its span contents
sink.Mock.Setup.Consume(It.IsRefStruct<Packet>(p => p.Payload.Length > 0 && p.Payload[0] == 0xFF))
    .Throws<InvalidOperationException>();

// Return a value from a method with a ref struct parameter
sink.Mock.Setup.TryParse(It.IsAnyRefStruct<Packet>()).Returns(42);

// Indexer keyed by a ref struct: Returns for the getter, OnSet to observe writes
sink.Mock.Setup[It.IsAnyRefStruct<Packet>()]
    .Returns("got")
    .OnSet(value => Console.WriteLine($"written: {value}"));

// Correlate writes and reads by projecting the key to a storable value
sink.Mock.Setup[It.IsRefStructBy<Packet, int>(p => p.Id)].Returns("fallback");
sink[new Packet(1, [])] = "written";
string a = sink[new Packet(1, [])];
string b = sink[new Packet(2, [])];

await That(a).IsEqualTo("written").Because("the key is matched by Id");
await That(b).IsEqualTo("fallback").Because("nothing was written under Id 2");
```

:::warning[Limitations]

C# does not allow ref struct values in generic delegates, so members with ref struct parameters offer a narrower API:

- **Setup**: only `Returns`, `Throws`, `OnSet(…)` (for indexer setters) and `SkippingBaseClass` are available. `.Do(…)`
  and the advanced callback features (`InParallel`, `When`, `For`, `Only`, `TransitionTo`) are not.
- **Verify**: verifications count the calls, but cannot match on the ref struct value, because the value is not kept
  after the call. Use a matcher in the setup to filter at call time.
- **Indexer storage**: values written through an indexer keyed by a ref struct are not read back by the getter, unless
  every ref struct key uses `It.IsRefStructBy<T, TKey>(projection)`.

:::

Members that Mockolate cannot support (for example methods that *return* a custom ref struct, or targets older than
.NET 9) are reported by the [Mockolate0003](10-analyzers.md#mockolate0003) analyzer.

## Union parameters (C# 15)

When the consuming project compiles with C# 15, Mockolate generates the setup and verify overloads with union-typed parameters: every value-capable parameter becomes a `ParameterArg<T>?` that accepts an `It` matcher or a direct value, and a `Func<T, bool>` overload accepts a predicate. The number of overloads stays the same as before, but predicates no longer need `It.Satisfies`:

```csharp
dispenser.Mock.Setup.Dispense(type => type.StartsWith("D"), It.IsAny<int>()).Returns(true);

dispenser.Mock.Verify.Dispense("Dark", amount => amount > 10).Once();
```

Union mode is enabled automatically once the compiler ships C# 15 and the project's effective language version is C# 15 or later. On a preview compiler, opt in with `<MockolateUnionParameters>true</MockolateUnionParameters>` together with `<LangVersion>preview</LangVersion>` in the project file; below that language version the property has no effect. `false` keeps the classic overloads on any compiler.

<details>
<summary>Union parameter semantics</summary>

- `null` and `default` stand for `default(T)` of the parameter type. Leaving out an optional parameter stands for its declared default value. Unlike the classic overloads, `null` for a non-nullable value type parameter with a declared default value matches `default(T)`, not the declared default.
- A union-typed parameter does not pass its type on to the argument: a collection expression, a target-typed `new(…)` or a value that relies on a user-defined implicit conversion needs an explicit type, for example `new TimeSpan(0, 0, 5)` or `(ReadOnlyMemory<byte>)bytes`.
- A parameter whose type is a delegate offers the raw delegate instead of a predicate, so a lambda is still matched as a value.
- For an `object` parameter, a lambda literal is a predicate, while a delegate stored in a variable is a value.
- Within a union-typed member, `ref`, `out` and `ref readonly` parameters as well as `Span<T>` and `ReadOnlySpan<T>` parameters keep their matcher slot (no predicate for that parameter); `in` parameters behave like by-value ones.
- Overloaded method names (including a generic sibling; a same-named member in another scope, whether public, protected or static, does not count), generic methods, `params` methods, members with custom ref struct parameters, and indexers that share their key count with another same-scope indexer keep the classic matcher/value overloads. Use `It.Satisfies` for predicates there.

</details>

The generated `ParameterArg<T>` type relies on `UnionAttribute`, `OverloadResolutionPriorityAttribute` and `CallerArgumentExpressionAttribute`. Mockolate declares whichever of them the compilation lacks (neither the referenced frameworks nor the project itself provide it). Another generator such as PolySharp can provide them without Mockolate noticing; set `<MockolateUnionAttributePolyfills>false</MockolateUnionAttributePolyfills>` to declare that `OverloadResolutionPriorityAttribute` and `CallerArgumentExpressionAttribute` come from elsewhere (`UnionAttribute` keeps following the compilation), or list the attribute names the project provides.

## Matching all parameters at once

For methods without overloads, the `Match` class lets you match all parameters together:

| Matcher                                         | Matches                                                         |
|-------------------------------------------------|-----------------------------------------------------------------|
| `Match.AnyParameters()`                         | any combination of parameters                                   |
| `Match.Parameters(Func<object?[], bool>)`       | parameter combinations for which the predicate returns `true`   |

```csharp
dispenser.Mock.Setup.Dispense(Match.Parameters(args =>
        args[0] is string type && type.StartsWith("D") &&
        args[1] is int amount && amount > 0))
    .Returns(true);

dispenser.Mock.Verify.Dispense(Match.AnyParameters()).Twice();
```

## Access parameter values

### Callbacks

Use `.Do(…)` on a matcher to run a callback with the value of that parameter whenever the setup matches:

```csharp
int lastAmount = 0;
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>().Do(amount => lastAmount = amount));

dispenser.Dispense("Dark", 42);

await That(lastAmount).IsEqualTo(42);
```

### Monitor

Use `.Monitor(out monitor)` on a matcher to collect all values passed for that parameter and inspect them afterwards:

```csharp
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>().Monitor(out IParameterMonitor<int> monitor));

dispenser.Dispense("Dark", 5);
dispenser.Dispense("Dark", 7);

await That(monitor.Values).IsEqualTo([5, 7]);
```

`IParameterMonitor<T>` lives in the `Mockolate.Parameters` namespace.
