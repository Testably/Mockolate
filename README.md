# Mockolate

<img align="right" width="200" src="Docs/Mockolate_256x256.png" alt="Mockolate logo" />

[![Nuget](https://img.shields.io/nuget/v/Mockolate)](https://www.nuget.org/packages/Mockolate)
[![Build](https://github.com/Testably/Mockolate/actions/workflows/build.yml/badge.svg)](https://github.com/Testably/Mockolate/actions/workflows/build.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Testably_Mockolate&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Testably_Mockolate)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Testably_Mockolate&metric=coverage)](https://sonarcloud.io/summary/overall?id=Testably_Mockolate)
[![Mutation testing badge](https://img.shields.io/endpoint?style=flat&url=https%3A%2F%2Fbadge-api.stryker-mutator.io%2Fgithub.com%2FTestably%2FMockolate%2Fmain)](https://dashboard.stryker-mutator.io/reports/github.com/Testably/Mockolate/main)

**Mockolate** is a modern, strongly-typed, AOT-compatible mocking library for .NET, powered by source generators.
It enables fast, compile-time validated mocking with .NET Standard 2.0, .NET 8, .NET 10, .NET 11 and .NET Framework 4.8.

- **Source generator-based**: No runtime proxy generation.
- **Fast**: Direct dispatch with no reflection or dynamic proxies.
- **Strongly-typed**: Compile-time safety and IntelliSense support.
- **AOT compatible**: Works with Native AOT and trimming.
- **Modern C#**: First-class support for ref structs, static interface members, and current language features.

## Why Mockolate

|                | Reflection-based mocks (Moq, NSubstitute, …) | Mockolate                  |
|----------------|----------------------------------------------|----------------------------|
| AOT / trimming | not supported                                | supported                  |
| Validation     | runtime exceptions                           | analyzers + compile errors |
| Setup API      | `Expression<Func<…>>` trees                  | regular method calls       |
| Hot path       | dynamic-proxy dispatch                       | direct dispatch            |

For side-by-side setup, usage, and verification syntax against Moq, NSubstitute, and FakeItEasy, see the
[full code comparison](https://docs.testably.org/Mockolate/comparison); for performance, see the
[benchmarks](https://docs.testably.org/Mockolate/benchmarks).

Already on Moq or NSubstitute? The companion package [`Mockolate.Migration`](https://github.com/Testably/Mockolate.Migration)
ships analyzers and code fixers that translate common Moq and NSubstitute patterns to Mockolate syntax in-place: point it
at an existing test project and apply the suggested fixes.

## Installation

Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). Mockolate leverages C# 14
extension members (the projects can still target any supported framework). Then add the package:

```powershell
dotnet add package Mockolate
```

## Quick Start

```csharp
using Mockolate;

public interface IChocolateDispenser
{
    bool Dispense(string type, int amount);
}

// Create a mock
IChocolateDispenser sut = IChocolateDispenser.CreateMock();

// Setup: Dispense returns true for any Dark chocolate request
sut.Mock.Setup.Dispense("Dark", It.IsAny<int>()).Returns(true);

// Act
bool success = sut.Dispense("Dark", 4);

// Verify
sut.Mock.Verify.Dispense("Dark", It.IsAny<int>()).Once();
```

## Documentation

Full reference docs at **[docs.testably.org/Mockolate](https://docs.testably.org/Mockolate/)**:

- [Create mocks](https://docs.testably.org/Mockolate/create-mocks)
- Setup: [properties](https://docs.testably.org/Mockolate/setup/properties), [methods](https://docs.testably.org/Mockolate/setup/methods), [indexers](https://docs.testably.org/Mockolate/setup/indexers), [parameter matching](https://docs.testably.org/Mockolate/setup/parameter-matching)
- [Mock events](https://docs.testably.org/Mockolate/mock-events)
- [Verify interactions](https://docs.testably.org/Mockolate/verify-interactions)
- Advanced features: [protected members](https://docs.testably.org/Mockolate/advanced-features/protected-members), [static interface members](https://docs.testably.org/Mockolate/advanced-features/static-interface-members), [callbacks](https://docs.testably.org/Mockolate/advanced-features/advanced-callback-features), [monitors](https://docs.testably.org/Mockolate/advanced-features/monitor-interactions), [scenarios](https://docs.testably.org/Mockolate/advanced-features/scenarios), [unexpected-interaction checks](https://docs.testably.org/Mockolate/advanced-features/check-for-unexpected-interactions)
- Special types: [`HttpClient`](https://docs.testably.org/Mockolate/special-types/httpclient), [delegates](https://docs.testably.org/Mockolate/special-types/delegates)
