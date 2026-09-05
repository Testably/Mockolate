# Mockolate

<img align="right" width="200" src="https://raw.githubusercontent.com/Testably/Mockolate/main/Docs/Mockolate_256x256.png" alt="Mockolate logo" />

[![Nuget](https://img.shields.io/nuget/v/Mockolate)](https://www.nuget.org/packages/Mockolate)

[**Mockolate**](https://github.com/Testably/Mockolate) is a modern, strongly-typed, AOT-compatible mocking library for .NET, powered by source generators.
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

For side-by-side setup, usage, and verification syntax against Moq, NSubstitute, and FakeItEasy, see the [full code comparison](08-comparison.md).

Already on Moq or NSubstitute? The companion package [`Mockolate.Migration`](https://github.com/Testably/Mockolate.Migration)
ships analyzers and code fixers that translate common Moq and NSubstitute patterns to Mockolate syntax in-place: point it
at an existing test project and apply the suggested fixes.

## Getting Started

1. **Check prerequisites**  
   Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), because Mockolate leverages
   C# 14 extension members (the projects can still target any supported framework).

2. **Install the package**
   ```powershell
   dotnet add package Mockolate
   ```

3. **Create and use a mock**
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

   For a richer walkthrough combining properties, indexers, events, and stateful setup,
   see [A complete example](07-complete-example.md).
