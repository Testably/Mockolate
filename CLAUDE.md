# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

Mockolate is a strongly-typed .NET mocking library powered by **Roslyn source generators**. Unlike reflection-based mocking libraries, it generates mock implementations at compile time, providing full IntelliSense support and AOT compatibility.

**Supported targets**: .NET Standard 2.0, .NET 8, .NET 10, .NET 11, .NET Framework 4.8

## Build and Test Commands

```bash
# Build
dotnet build Mockolate.slnx

# Run all tests
dotnet test Mockolate.slnx

# Run a specific test project
dotnet test Tests/Mockolate.Tests/Mockolate.Tests.csproj

# Run a single test (by name filter)
dotnet test Tests/Mockolate.Tests/Mockolate.Tests.csproj --filter "FullyQualifiedName~MethodName"

# Full build with coverage (requires Nuke)
./build.sh CodeCoverage       # Linux/macOS
./build.ps1 CodeCoverage      # Windows PowerShell

# Other Nuke targets
./build.sh ApiChecks          # API compatibility tests
./build.sh MutationTests      # Stryker mutation tests
./build.sh Pack               # Create NuGet packages
```

**Requires**: .NET 11 SDK (currently a preview, see `global.json`). For source generator changes, run `dotnet clean && dotnet build` to force regeneration.

## Architecture

The repository has four source projects that work together:

### Source/Mockolate
The main runtime library. Users reference this package directly. Key responsibilities:
- `MockRegistry` — central store for behaviors, setups, and recorded interactions per mock instance
- `MockBehavior` — controls strict vs. loose behavior
- `Setup*` / `Verify*` extension methods — fluent API for configuring and verifying mocks
- `It.*` parameter matchers (e.g., `It.IsAny<T>()`, `It.Is<T>(predicate)`)
- Exception types: `MockException`, `MockNotSetupException`, `MockVerificationException`, `MockVerificationTimeoutException`
- `Web/` — special support for mocking `HttpClient` via its handler

### Source/Mockolate.SourceGenerators
An `IIncrementalGenerator` that runs at compile time and generates `Mock.{TypeName}.g.cs` files in `obj/`. Given a `[GenerateMock]` attribute or `Mock.Create<T>()` usage, it generates a concrete class implementing the target interface or class. The generator:
- Handles interfaces, abstract classes, and delegates
- Emits method overrides that delegate to `MockRegistry`
- Targets .NET Standard 2.0 (Roslyn constraint)
- Union mode (`Sources.MockClass.Unions.cs`): with C# 15 (or `MockolateUnionParameters=true`) the setup/verify overloads take `ParameterArg<T>?` or `Func<T, bool>` per parameter instead of the classic matcher/value set; below that the output is unchanged

### Source/Mockolate.Analyzers + Source/Mockolate.Analyzers.CodeFixers
Roslyn analyzers that validate mock usage at compile time:
- `MockabilityAnalyzer` — checks that `T` in `Mock.Create<T>()` can actually be mocked
- `UseVerificationAnalyzer` — enforces that verifications are properly structured
Both target .NET Standard 2.0. Code fixers provide IDE quick-fixes for diagnostics.

### How the pieces connect

```
User writes:  IFoo sut = IFoo.CreateMock();
              sut.Mock.Setup.Bar(...).Returns(...);
              sut.Mock.Verify.Bar(...).Once();

At compile time:
  SourceGenerator  →  generates Mock.IFoo.g.cs (implements IFoo, wires into MockRegistry)
  Analyzers        →  validate that IFoo is mockable and setup/verify calls are correct

At runtime:
  IFoo.CreateMock()    →  returns a IFoo instance
  Method calls         →  recorded in MockRegistry, returns configured behavior
  Verify calls         →  query MockRegistry for recorded interactions
```

### Test Projects

| Project | Purpose |
|---|---|
| `Mockolate.Tests` | Core behavioral tests (methods, properties, indexers, events, delegates, HttpClient) |
| `Mockolate.SourceGenerators.Tests` | Generator output tests using `Microsoft.CodeAnalysis.Testing` |
| `Mockolate.Analyzers.Tests` | Analyzer diagnostic tests using `CSharpAnalyzerVerifier` |
| `Mockolate.Api.Tests` | API surface snapshots (PublicApiGenerator) — catches unintentional breaking changes |
| `Mockolate.ExampleTests` | Living documentation showing idiomatic usage |
| `Mockolate.AotCompatibility.TestApp` | AOT publish smoke test |

## Coding Standards

- **No `var`** — always use explicit types
- **Tabs** for indentation (width 4), per `.editorconfig`
- **Private fields**: `_camelCase` prefix
- **Public members**: PascalCase
- No `this.` qualification
- Expression-bodied members for single-expression accessors/methods
- Pattern matching preferred over `is`/`as` casts
- `using` directives: alphabetical, `System.*` first
- Language keywords (`int`, `string`) over BCL type names

## Quality Gates

All PRs must pass:
- Code coverage **> 90%**
- **Zero SonarCloud issues**
- Multi-platform test run (Ubuntu, Windows, macOS via GitHub Actions)
- API compatibility check (snapshot diff in `Mockolate.Api.Tests`)

## Key Conventions

- **Conventional commits** required for PR titles (`feat:`, `fix:`, `chore:`, etc.)
- New public API must have XML doc comments
- All assemblies are **strong-named** (`Directory.Build.props`)
- Package versions managed centrally in `Directory.Packages.props`
- Documentation lives in `Docs/pages/`; the README links to it for reference content. Published at https://docs.testably.org/Mockolate/
- Examples go in `Mockolate.ExampleTests`, not inline in test helper files