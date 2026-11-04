---
description: What changed in Mockolate v4 and how to upgrade from Mockolate 3.x.
sidebar_label: From Mockolate 3.x
---

# What's new in v4

Mockolate v4 supports .NET 11 and C# 15: with C# 15, the setup and verify methods take a value, a matcher or a predicate in the same parameter, so predicates no longer need `It.Satisfies`. This page covers what you need to know to upgrade. The complete list of changes, pull request by pull request, is in the [GitHub releases](https://github.com/Testably/Mockolate/releases).

To upgrade:

1. Update Mockolate to 4.x and fix the new [Mockolate0001](#mockolate0001-reports-more-verifications) warnings.
2. If your test project compiles with C# 15, e.g. because it targets `net11.0`, fix the compile errors with the [code fix](#arguments-that-need-an-explicit-type) and review the [changes the compiler does not catch](#changes-the-compiler-does-not-catch).
3. Run your tests.

A test project below C# 15 keeps the setup and verify methods of v3, so only the first step applies to it.

:::tip[Keep the v3 methods]
Set `<MockolateUnionParameters>false</MockolateUnionParameters>` in the project file to keep the setup and verify methods of v3 on any compiler, e.g. to retarget to .NET 11 first and switch to the new methods later.
:::

## Changes the compiler does not catch

These changes compile unchanged with C# 15, but an existing test may now check something different than before:

- **`null` for an optional value type parameter.** For a non-nullable value type parameter with a declared default value, `null` and `default` match `default(T)`, not the declared default. Leave out the argument to match the declared default value.
- **Delegate parameters.** For a parameter whose type is a delegate, a lambda is matched as a value, not as a predicate. For an `object` parameter, a lambda literal is a predicate, while a delegate stored in a variable is a value.

See [union parameter semantics](../06-parameter-matching.md#union-parameters-c-15).

## Mockolate0001 reports more verifications

[Mockolate0001](../10-analyzers.md#mockolate0001) now also reports an unused verification with only values, such as `Verify.Dispense("Dark", 1)`, and one without parameters, such as `Verify.Restock()`. Such a statement asserts nothing, so a build that treats warnings as errors fails where it was silently passing before. The included code fix adds the `.AtLeastOnce()` count assertion.

## Union parameters (C# 15)

When the test project compiles with C# 15, every value-capable parameter of a setup or verify method takes a value or an `It` matcher, and an overload takes a predicate:

```diff
- dispenser.Mock.Setup.Dispense(It.Satisfies<string>(type => type.StartsWith("D")), It.IsAny<int>()).Returns(true);
+ dispenser.Mock.Setup.Dispense(type => type.StartsWith("D"), It.IsAny<int>()).Returns(true);
```

`It.Satisfies` keeps working. Overloaded methods, generic methods, `params` methods, members with custom ref struct parameters and some indexers keep the methods of v3; see [union parameters](../06-parameter-matching.md#union-parameters-c-15) for the complete list.

### Arguments that need an explicit type

A union-typed parameter does not pass its type on to the argument, so a collection expression, a target-typed `new(…)` or a value that relies on a user-defined implicit conversion no longer compiles (CS1503, CS1729 or CS9174). The [included code fix](../10-analyzers.md#explicit-types-for-union-parameters) adds the type:

```diff
- timer.Mock.Setup.Wait(new(0, 0, 5));
+ timer.Mock.Setup.Wait(new TimeSpan(0, 0, 5));
```

You can also name the type in a matcher, e.g. `It.Is<TimeSpan>(new(0, 0, 5))`.

### Attributes

The generated `ParameterArg<T>` type relies on `UnionAttribute`, `OverloadResolutionPriorityAttribute` and `CallerArgumentExpressionAttribute`, and Mockolate declares those the compilation lacks. If another generator such as PolySharp declares them too, set `<MockolateUnionAttributePolyfills>false</MockolateUnionAttributePolyfills>`. See [union parameters](../06-parameter-matching.md#union-parameters-c-15).

## Removed APIs

- **`IMockBehaviorInitializer`:** the empty marker interface in the `Mockolate.Behavior` namespace was never used by Mockolate and is removed. Remove any implementation of it; it had no effect.
- **`toString` parameter of `It.IsNull<T>()` and `It.IsNotNull<T>()`:** the optional parameter only replaced the matcher's description in failure messages and is removed. Remove the argument; failure messages show `It.IsNull<T>()` or `It.IsNotNull<T>()`.
- **Infrastructure for generated code:** `MockRegistry`, `FastMockInteractions`, the `Fast*Buffer` types, `Callbacks<T>` and the other types that only generated code calls moved to the `Mockolate.Internals` namespace, and members that generated code does not need are no longer public. `It.IsValue` is removed. These types have no compatibility guarantee; use `sut.Mock` instead. If you access `IMock.MockRegistry`, add `using Mockolate.Internals;`.

## New in v4

These additions need no change in your code.

- **.NET 11:** Mockolate targets `net11.0`.
- **Predicates as arguments:** with C# 15, setup and verify methods take a predicate directly, see [union parameters](#union-parameters-c-15).
- **Extern aliases:** types from a reference with an extern alias can be mocked.
