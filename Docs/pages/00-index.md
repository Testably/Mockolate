---
description: Mockolate is a source-generated, strongly-typed and AOT-compatible mocking library for .NET.
---

# Mockolate

<img align="right" width="200" src="https://raw.githubusercontent.com/Testably/Mockolate/main/Docs/Mockolate_256x256.png" alt="Mockolate logo" />

[**Mockolate**](https://github.com/Testably/Mockolate) is a modern, strongly-typed, AOT-compatible mocking library for .NET, powered by source generators.
It enables fast, compile-time validated mocking with .NET Standard 2.0, .NET 8, .NET 10 and .NET Framework 4.8.

- **Source generator-based**: No runtime proxy generation.
- **Fast**: Direct dispatch with no reflection or dynamic proxies.
- **Strongly-typed**: Compile-time safety and IntelliSense support.
- **AOT compatible**: Works with Native AOT and trimming.
- **Modern C#**: First-class support for ref structs, static interface members, and current language features.

```csharp
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
dispenser.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);

bool success = dispenser.Dispense("Dark", 4);

dispenser.Mock.Verify.Dispense("Dark", It.IsAny<int>()).Once();
```

## Why Mockolate

|                | Reflection-based mocks (Moq, NSubstitute, …) | Mockolate                  |
|----------------|----------------------------------------------|----------------------------|
| AOT / trimming | not supported                                | supported                  |
| Validation     | runtime exceptions                           | analyzers + compile errors |
| Setup API      | `Expression<Func<…>>` trees                  | regular method calls       |
| Hot path       | dynamic-proxy dispatch                       | direct dispatch            |

See the [side-by-side comparison](/Mockolate/migration/#side-by-side) with Moq, NSubstitute and FakeItEasy, and the
[benchmarks](12-benchmarks.mdx) for performance numbers.

## Where to go next

- **New to Mockolate?** Start with [Getting started](01-getting-started.mdx), then read the
  [core concepts](/Mockolate/core-concepts/) to learn how mocks, setups and verifications fit together.
- **Coming from Moq, NSubstitute or FakeItEasy?** The [migration guide](/Mockolate/migration/) translates the syntax
  you know, and [Mockolate.Migration](migration/11-migration.md) rewrites existing Moq and NSubstitute tests for you.
- **Looking for a specific API?** The [cheat sheet](core-concepts/02-cheat-sheet.md) lists the most common calls on one page.
