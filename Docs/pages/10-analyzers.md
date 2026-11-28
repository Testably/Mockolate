---
description: The Roslyn analyzers and code fixes that ship with Mockolate and catch mocking mistakes at compile time.
---

import ApplyFix from '@site/src/components/ApplyFix';

# Analyzers

Mockolate ships with Roslyn analyzers and code fixes that catch mistakes at compile time. They are part of the `Mockolate` package,
so there is nothing to install.

## Mockolate0001

**A verification without a count assertion.**

`Verify` methods only return a `VerificationResult` and do not throw on their own. You have to specify how often you
expect the call to happen, e.g. `.AtLeastOnce()` or `.Exactly(n)`, or use the verification result in another way.

<ApplyFix id="Mockolate0001">

```csharp
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Dispense("Dark", 1);
// Mockolate0001: Add a count assertion like .AtLeastOnce() or use the result.
// highlight-next-line
dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>());
```

```csharp
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Dispense("Dark", 1);
// highlight-next-line
dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>()).AtLeastOnce();
```

</ApplyFix>

The included code fix adds the `.AtLeastOnce()` count assertion.

## Mockolate0002

**A type that cannot be mocked.**

This rule prevents you from mocking unsupported types:

- `CreateMock()`: the type must be an interface, a delegate or a class that can be derived from (e.g. not `sealed`).
- `CreateMock(out ConstructorArguments arguments)`: the type must be a sealed class or a record that your assembly can access, with a single accessible constructor with the most parameters that sets all `required` members. It must not be abstract, generic or nested in a generic type, obsolete, file-local, in the `System`, `Microsoft` or `Windows` namespaces, or have a member named `CreateMock` or inherit one that hides the generated one.
- `Implementing<T>()`: the type must be an interface.

It also fires when the type has a member that the mock would have to implement (an abstract member, or an interface
member without a default implementation), but that your assembly cannot access: for example an `internal abstract` or
`private protected abstract` member, or a `{ get; internal set; }` property, on a type from a referenced assembly that
does not grant `InternalsVisibleTo`. There is no valid code a mock could emit for such a member, so the type cannot be
mocked at all.

The same holds for such a member with a type parameter whose constraints contradict each other once the generic type is
closed, because every implementation inherits them and fails to compile (CS0455):

```csharp
public interface IDispenser<T>
{
    void Dispense<TChocolate>(TChocolate chocolate) where TChocolate : class, T;
}

// Mockolate0002: Type 'IDispenser<int>' cannot be mocked: the member 'IDispenser<int>.Dispense<TChocolate>(TChocolate)'
// must be implemented, but its type parameter 'TChocolate' has the conflicting constraints 'class' and 'int'
IDispenser<int> dispenser = IDispenser<int>.CreateMock();
```

`struct` or `unmanaged` together with a class other than `object`, `ValueType` or `Enum`, `class` or `struct` together
with a nullable value type, or two unrelated classes or structs, conflict the same way. A
virtual member or a default interface implementation with such constraints is not reported: the mock leaves it to the
base implementation.

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

The same applies per accessor. For a property such as `public abstract string Flavor { get; internal set; }`, an
override further down that implements the setter makes the type mockable. The mock then overrides the getter only, and
`Setup` and `Verify` only offer the getter, since writes never reach the mock. See
[properties with only one accessor](05-setup/02-properties.md#properties-with-only-one-accessor) and
[indexers with only one accessor](05-setup/03-indexers.md#indexers-with-only-one-accessor).

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
[`MockBehavior`](03-create-mocks.md#customizing-mock-behavior) settings, because there is no setup to apply them to, and
it is not recorded.

Interface members, `abstract` members, `init` accessors and `ref`-returning methods have nothing to forward to. These
are the ones the warning reports.

</details>

See [Ref struct parameters](06-parameter-matching.md#ref-struct-parameters-net-9) for the supported features.

## Mockolate0004

**A type that is mocked in more than one spelling.**

Spellings of a generic type that differ only in tuple element names or in nullable annotations are the same type, so they cannot have separate mocks. The rule reports every spelling when one compilation mocks more than one of them, through `CreateMock()` or `Implementing<T>()`. Use the same spelling everywhere:

```csharp
public interface IRepository<T>
{
    T Get(T key);
}

// Mockolate0004: Type 'IRepository<string?>' is also mocked as 'IRepository<string>', which differs only in tuple element names or nullable annotations; use a single spelling
IRepository<string?> nullable = IRepository<string?>.CreateMock();
// Mockolate0004: Type 'IRepository<string>' is also mocked as 'IRepository<string?>', ...
IRepository<string> notNullable = IRepository<string>.CreateMock();
```

The same applies to `IRepository<(int Id, string Name)>` and `IRepository<(int, string)>`, also when the difference is nested, as in `IRepository<List<string?>>` and `IRepository<List<string>>`. The calls on these mocks also fail with CS0121 or CS9339, because the mock of each spelling applies to the other one too. `dynamic` and `object` do not count as different spellings: they share one mock.

## Mockolate0005

**Logging setups and verifications on the mock of a class that cannot intercept the log calls.**

The [logging](09-special-types/03-logging.md) setups and verifications intercept the calls of `ILogger.Log`. The mock of a class that implements `ILogger` supports them when the class implements `Log` with a virtual or abstract method. When `Log` is non-virtual or sealed, or is implemented explicitly, the mock cannot intercept the calls, so the rule reports the logging members:

```csharp
public class ConsoleLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => Console.WriteLine(formatter(state, exception));

    // IsEnabled and BeginScope
}

ConsoleLogger logger = ConsoleLogger.CreateMock();
// Mockolate0005: The logging setups and verifications are not supported on the mock of class 'ConsoleLogger', because it cannot intercept the log calls
logger.Mock.Setup.LogError().Throws<InvalidOperationException>();
```

Make `Log` virtual, or mock `ILogger` and wrap the class with `Wrapping(…)`. A mock that also implements `ILogger` with `Implementing<ILogger>()` intercepts the calls through that interface: use its logging members through `mock.Mock.As<ILogger>()`. The rule also reports the logging verifications on the mock of a class that implements `ILoggerFactory`, which only mocks of `ILoggerFactory` and interfaces that derive from it support.

## Mockolate0006

**A `null` argument that matches any parameters.**

`Setup` and `Verify` methods have an overload that takes the parameters as a whole, such as `Match.AnyParameters()`. Where no parameter of the member can be `null`, for example a single `int` parameter, a lone `null` or `default` literal binds to this overload. It then means any parameters, so the setup applies to every call and the verification counts every call, although it reads like a call with `null`:

<ApplyFix id="Mockolate0006">

```csharp
public interface IChocolateDispenser
{
    void Refill(int amount);
}

dispenser.Refill(5);
// Mockolate0006: 'null' matches any parameters, not a null argument; use Match.AnyParameters() if that is intended
// highlight-next-line
dispenser.Mock.Verify.Refill(null).Once();
```

```csharp
public interface IChocolateDispenser
{
    void Refill(int amount);
}

dispenser.Refill(5);
// highlight-next-line
dispenser.Mock.Verify.Refill(Match.AnyParameters()).Once();
```

</ApplyFix>

The included code fix replaces the literal with `Match.AnyParameters()`. If you meant a specific argument, pass the value or a matcher instead, for example `Refill(5)` or `Refill(It.IsAny<int>())`.

Where the first parameter can be `null` and every further parameter is optional or `params`, a lone `null` stands for the `null` argument instead, and the rule does not fire.

## Mockolate0007

**A matcher that is compared as a value.**

Every matcher converts to `object`, so a matcher passed where `Setup` or `Verify` takes an `object` value can bind as the value itself. The setup or verification then expects a call with the matcher object as argument and does not match the calls you meant:

```csharp
public interface IChocolateDispenser
{
    void Dispense(object flavor);
    void Log(int level, params object[] args);
}

dispenser.Dispense(5);
dispenser.Log(1, "Dark", 2);
// Mockolate0007: 'It.IsAny<int>()' is compared as a value of type 'object' instead of matching; use a matcher for 'object', or It.Is<object>(…) if the matcher itself is the expected value
await That(dispenser.Mock.Verify.Dispense(It.IsAny<int>())).Once();
// Mockolate0007: 'It.IsAny<object>()' is compared as a value of type 'object' instead of matching; pass matchers of the parameter types for the other arguments as well, for example It.Is(…)
await That(dispenser.Mock.Verify.Log(1, "Dark", It.IsAny<object>())).Once();
```

The rule fires when no overload takes the matcher as a matcher, for example when it binds to:

- an `object` or `dynamic` parameter, property or indexer value, or indexer key, with a matcher that is no matcher for `object`, such as `It.IsAny<int>()`;
- an element of a `params object[]` array next to plain values, or with a matcher that is no matcher for `object`, and a matcher for the whole array, such as `It.IsAny<object[]>()`, next to other elements of the array or next to four or more other arguments;
- an element of a `params T[]` array whose type argument is inferred from the matcher next to plain values, such as `Gen(1, It.IsAny<object>())` on `void Gen<T>(int level, params T[] args)`, which infers `T` as the matcher type;
- a matcher for the whole array of a `params T[]` array of a generic method closed over `object`, such as `It.IsAny<object[]>()`, next to other elements of the array;
- an element of a `params object[]` array passed as an explicit array or a collection expression, such as `Log(It.IsAny<int>(), new object[] { It.IsAny<object>() })` or `Log(1, ["Dark", It.IsAny<object>()])`;
- an `object` parameter of a method with more than four parameters next to plain values, where no overload mixes values and matchers (without [union parameters](06-parameter-matching.md#union-parameters-c-15));
- a parameters match such as `Match.AnyParameters()` next to other arguments, as a property or indexer value, or as an indexer key, such as `Pair(Match.AnyParameters(), 1)`, because it only applies as the only argument of a method.

Use a matcher for the parameter type instead, for example `It.IsAny<object>()` or `It.Satisfies<object>(f => f is int)`, or pass matchers for the other arguments as well, as in `Log(It.Is(1), It.Is<object>("Dark"), It.IsAny<object>())`. If the matcher object itself is the expected value, wrap it in `It.Is<object>(…)` or cast it to `object`; the rule does not report either. For a parameter or a `params` element typed with a type parameter closed over `object`, a matcher that converts to `IParameter<object>`, such as `It.IsAny<object>()` or `It.IsAny<string>()`, is still recognized as a matcher at runtime after a cast; use `It.Is<object>(…)` there when the matcher object itself is the expected value.

The message says which of these fixes applies. For an explicit `params` array, it suggests passing the elements as separate arguments, together with matchers for the other arguments where they are plain values. For a custom matcher that is also a value of the parameter type, it suggests a cast to `IParameter<T>`, and it says so when no spelling passes the matcher as a matcher. For a matcher for the whole array of a `params T[]` array next to other elements, it says that such a matcher only matches the array as its only element, and that next to other elements it matches one element.

## Mockolate0010

**A mock test parameter that cannot be created.**

[`AutoMockArguments`](04-mocks-as-test-parameters.md) fills the parameters of a test method. An interface, delegate or abstract class asks for a mock, and a concrete class for an instance with mocked dependencies. When Mockolate cannot create such a type and the default values of the behavior provide no value, the attribute cannot fill the parameter and the test fails at runtime:

```csharp
public interface IChocolateRepository<TFlavor>
{
    TFlavor Find(string name);
}

public class ChocolateTests<TFlavor>
{
    // Mockolate0010: Mockolate cannot create the parameter 'repository' of type 'IChocolateRepository<TFlavor>': its type arguments contain a type parameter
    [Test]
    [AutoMockArguments]
    public void Find_ShouldReturnTheFlavor(IChocolateRepository<TFlavor> repository) { }
}
```

The rule fires for a type parameter or a type that contains one, a type that the generated code cannot access, a file-local, obsolete or experimental type, a type nested in a generic type, an interface, delegate or abstract class that cannot be mocked (see [Mockolate0002](#mockolate0002)), has static abstract members or a member its mock cannot implement or, for an abstract class, has neither a public or protected constructor that can be called without arguments nor a single one with the most parameters, a type argument or array element that the generated code cannot use, and a generic class or a class without a single accessible constructor with the most parameters. Types of the `System`, `Microsoft` and `Windows` namespaces are never reported, because the test framework often passes them as arguments. For a reported parameter whose type the generated factory can name, a default value generator of the behavior can still provide a value.

## Mockolate0011

**An `[AsDefault]` parameter that is ambiguous or invalid.**

An [`[AsDefault]`](04-mocks-as-test-parameters.md#return-the-same-instance-from-every-mock) parameter is the default value of its type and of the [types it names](04-mocks-as-test-parameters.md#use-a-fake-for-an-interface) for all mocks of the test case. Two of them for the same type are ambiguous, the parameter must be assignable to each named type, and a `[NotShared]` marker contradicts it:

```csharp
[Test]
[AutoMockArguments]
public void Buy_ShouldUseTheDispenser(IVendingMachine machine, [AsDefault] IChocolateDispenser dispenser,
    // Mockolate0011: The [AsDefault] parameter 'other' is used for 'IChocolateDispenser' like the [AsDefault] parameter 'dispenser'
    [AsDefault<IChocolateDispenser>] FakeChocolateDispenser other,
    // Mockolate0011: The [AsDefault] parameter 'count' is not assignable to 'IChocolateDispenser'
    [AsDefault<IChocolateDispenser>] int count) { }
```

Use each type in only one `[AsDefault]` parameter, name only types that the parameter implements or derives from, and remove either `[AsDefault]` or `[NotShared]` from a parameter that has both.

## Mockolate0012

**A `[Deferred]` parameter that is invalid.**

A [`[Deferred]`](04-mocks-as-test-parameters.md#create-the-class-under-test-in-the-test) parameter must be a `Func<T>`, whose calls each create a new `T`. Mockolate cannot create any other type for it, so the test fails at runtime. The delegate is never passed to a class under test, so `[NotShared]` has no effect, and the mocks cannot return it, so it cannot be `[AsDefault]`:

```csharp
[Test]
[AutoMockArguments]
public void Buy_ShouldDispenseTheChocolate(
    // Mockolate0012: The [Deferred] parameter 'sut' is not a Func<T>
    [Deferred] VendingMachine sut,
    // Mockolate0012: The [Deferred] parameter 'createDispenser' is also marked [AsDefault]
    [Deferred, AsDefault] Func<IChocolateDispenser> createDispenser) { }
```

Declare the parameter as a `Func<T>` of the type to create, and remove `[AsDefault]` and `[NotShared]` from it. [Mockolate0010](#mockolate0010) reports a `T` that Mockolate cannot create.

## Mockolate0013

**A `[ValueFrom]` parameter that is invalid.**

A [`[ValueFrom]`](04-mocks-as-test-parameters.md#take-a-value-from-a-member) parameter takes its value from a static property or a static method without parameters of the test class or one of its base classes, whose type must be assignable to the parameter. Otherwise, the test fails at runtime:

```csharp
public class DiscountTests
{
    [Test]
    [AutoMockArguments]
    public void GetPrice_ShouldApplyTheDiscount(
        // Mockolate0013: The [ValueFrom] parameter 'discount' names 'Text' of type 'string', which is not assignable to 'Discount'
        [ValueFrom(nameof(Text))] Discount discount,
        // Mockolate0013: The [ValueFrom] parameter 'other' names the field 'Shared' of 'DiscountTests'; use a property or method, so that each test case gets a new value
        [ValueFrom(nameof(Shared))] Discount other) { }

    private static string Text => "Dark";
    private static readonly Discount Shared = new() { Type = "Dark", Percentage = 10m };
}
```

The rule also fires for a member that does not exist or is not static, for a parameter that is also `[Deferred]`, whose delegate Mockolate creates, and for a parameter that gets a value of the attribute, which replaces the value of the member. Name a static property or method that returns the type of the parameter, and turn a field into a property, e.g. `private static Discount Shared => new() { … };`.

## Mockolate0014

**A parameter with a value provider that is invalid.**

An attribute that implements `IParameterValueProvider` [provides the value](04-mocks-as-test-parameters.md#provide-a-value-with-an-attribute) of a test parameter. When it declares the types of its values with `[ProvidesValuesFor]`, one of them must be assignable to the parameter. Otherwise, the test fails at runtime:

```csharp
using System.Reflection;

[ProvidesValuesFor(typeof(string))]
public sealed class FileNameAttribute : Attribute, IParameterValueProvider
{
    public object? ProvideValue(ParameterInfo parameter) => $"{Guid.NewGuid():N}.txt";
}

[Test]
[AutoMockArguments]
public void Export_ShouldWriteTheFile(
    // Mockolate0014: The parameter 'count' has the value provider 'FileNameAttribute', which provides only values of type 'string'
    [FileName] int count) { }
```

The rule also fires for a parameter with more than one value provider or `[ValueFrom]`, for a parameter that is also `[Deferred]`, whose delegate Mockolate creates, and for a parameter that gets a value of the attribute, which replaces the provided value. Use a value provider that declares the type of the parameter, and keep one value provider per parameter.

## Mockolate0015

**Memory cache setups and verifications on the mock of a class that does not behave like a cache.**

The [memory cache](09-special-types/04-memory-cache.md) setups and verifications need a mock that behaves like a cache. The mock of a class that implements `IMemoryCache` only does so when `TryGetValue`, `CreateEntry` and `Remove` are abstract. Otherwise, it calls the base implementations, so the rule reports the cache members:

```csharp
public class InMemoryCache : IMemoryCache
{
    public virtual bool TryGetValue(object key, out object? value) { … }

    // CreateEntry, Remove and Dispose
}

InMemoryCache cache = InMemoryCache.CreateMock();
// Mockolate0015: The memory cache setups and verifications are not supported on the mock of class 'InMemoryCache', because it calls the base implementations of TryGetValue, CreateEntry or Remove
cache.Mock.Setup.Get("price-Dark").Returns(2.5m);
```

Mock `IMemoryCache` instead, or wrap the class with `Wrapping(…)` on a mock of `IMemoryCache`.

## Mockolate1001

**IDE0060 is suppressed for an `[AsDefault]` parameter.**

[IDE0060](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0060) reports a parameter that the method does not use. An [`[AsDefault]`](04-mocks-as-test-parameters.md#return-the-same-instance-from-every-mock) parameter of a test with `AutoMockArguments` may be used without the test method referencing it, because the other mocks can return it, here `machine.Dispenser`, so Mockolate suppresses IDE0060 for it:

```csharp
[Test]
[AutoMockArguments]
public async Task Dispense_WithoutSetup_ShouldFail(IVendingMachine machine,
    [AsDefault] IChocolateDispenser dispenser) // no IDE0060
{
    await That(machine.Dispenser.Dispense("Dark", 4)).IsFalse();
}
```

Other unused parameters still get the diagnostic, also one that is only passed to the constructor of a class under test.

## Mockolate1002

**xUnit1026 is suppressed for an `[AsDefault]` parameter.**

The xUnit analyzers report [xUnit1026](https://xunit.net/xunit.analyzers/rules/xUnit1026) for a theory parameter that the test method does not use. For the same reason as with [Mockolate1001](#mockolate1001), Mockolate suppresses the warning for an `[AsDefault]` parameter, but not for other unused parameters:

```csharp
[Theory]
[AutoMockArguments]
public async Task Dispense_WithoutSetup_ShouldFail(IVendingMachine machine,
    [AsDefault] IChocolateDispenser dispenser) // no xUnit1026
{
    await That(machine.Dispenser.Dispense("Dark", 4)).IsFalse();
}
```

## Explicit types for union parameters

With [union parameters](06-parameter-matching.md#union-parameters-c-15), a collection expression, a target-typed `new(…)` or a value that relies on a user-defined implicit conversion does not compile as a `Setup` or `Verify` argument (CS1503, CS1729 or CS9174). The included code fix adds the parameter type:

<ApplyFix id="CS1729">

```csharp
// CS1729: 'ParameterArg<TimeSpan>' does not contain a constructor that takes 3 arguments
// highlight-next-line
timer.Mock.Setup.Wait(new(0, 0, 5));
```

```csharp
// highlight-next-line
timer.Mock.Setup.Wait(new TimeSpan(0, 0, 5));
```

</ApplyFix>
