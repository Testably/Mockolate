# Project Structure

## Layout

| Folder | Content |
|---|---|
| `Source/` | The shipped projects |
| `Tests/` | Test projects and the AOT smoke test app |
| `Benchmarks/` | BenchmarkDotNet comparison with other mocking libraries |
| `Pipeline/` | Nuke build (`Build.csproj`) and the benchmark report generator |
| `Docs/pages/` | User docs published to https://docs.testably.org/Mockolate/ |
| `Docs/agents/` | Guides for contributors and agents (this folder) |
| `.github/` | CI workflows, Dependabot and Mergify configuration |

## Source Projects

Only the `Mockolate` package is published. It carries the generator, analyzers and code fixers in `analyzers/dotnet/cs`.

### `Source/Mockolate`

The runtime library (`netstandard2.0`, `net8.0`, `net10.0`).

| Location | Responsibility |
|---|---|
| `MockRegistry*.cs` | Per-mock store for the behavior, setups and recorded interactions; generated mocks delegate every member access to it |
| `MockBehavior.cs`, `MockBehaviorExtensions.cs`, `Behavior/` | Loose or strict behavior, default values, base class handling, constructor parameters |
| `Setup/` | Method, property, indexer and event setups, callbacks, ref struct setups |
| `Interactions/` | Recorded interactions; `FastMockInteractions` and the `Fast*Buffer` types are the allocation-sensitive recording path |
| `Verify/` | Verification results and count sources |
| `It*.cs`, `Match*.cs`, `Parameters/` | Parameter matchers (`It.IsAny<T>()`, `It.Is(...)`, `It.Matches(...)`, ...) |
| `Monitor/` | `MockMonitor` for interactions within a scope |
| `Web/` | `HttpClient` support: setups, verifications and `It` extensions for requests and content |
| `Exceptions/` | `MockException`, `MockNotSetupException`, `MockVerificationException`, `MockVerificationTimeoutException` |
| `Internals/` | Helpers and polyfills for older frameworks |

### `Source/Mockolate.SourceGenerators`

An `IIncrementalGenerator` (`MockGenerator.cs`, `netstandard2.0`).

- It always emits `Mock.g.cs`, which declares `CreateMock` in a C# 14 extension block.
- It finds `CreateMock()` invocations in the global `Mockolate` namespace (`MockGeneratorHelpers.cs`), including chained `.Implementing<T>()` calls, and emits one `Mock.<Name>.g.cs` per mocked type or type combination.
- Shared files depend on what the mocks need: `MethodSetups.g.cs`, `IndexerSetups.g.cs`, `ActionFunc.g.cs`, `ReturnsThrowsAsyncExtensions.g.cs`, `RefStructMethodSetups.g.cs`, `MockBehaviorExtensions.g.cs`, `Mock.AsExtensions.g.cs`.
- `Entities/` holds the equatable model extracted from symbols (`Class`, `Method`, `Property`, `Event`, `EquatableArray`, ...); `Internals/EntityCache.cs` deduplicates it per compilation.
- `Sources/` turns the model into C# with `StringBuilder`.
- Output depends on the consumer's compilation, for example whether `OverloadResolutionPriorityAttribute` exists.

### `Source/Mockolate.Analyzers` and `Source/Mockolate.Analyzers.CodeFixers`

`netstandard2.0`, compiled against `MinimumRoslynVersion`.

| Rule | Analyzer | Meaning |
|---|---|---|
| `Mockolate0001` | `UseVerificationAnalyzer` | A verification result must be used (code fix: `UseVerificationCodeFixProvider`) |
| `Mockolate0002` | `MockabilityAnalyzer` | The type passed to `CreateMock()` or `Implementing<T>()` must be mockable |
| `Mockolate0003` | `MockabilityAnalyzer` | Ref struct usage is not supported on this compilation (descriptor declared in the analyzer for release tracking) |

## How the Pieces Connect

```
User code:     IFoo sut = IFoo.CreateMock();
               sut.Mock.Setup.Bar(It.IsAny<int>()).Returns(42);
               sut.Bar(1);
               sut.Mock.Verify.Bar(It.IsAny<int>()).Once();

Compile time:  generator  -> Mock.IFoo.g.cs implements IFoo and routes every member to MockRegistry
               analyzers  -> IFoo is mockable, the verification result is used

Run time:      member call -> recorded in MockRegistry, answered by the matching setup or the behavior
               Verify      -> counts the matching recorded interactions
```

Other entry points on `sut.Mock` include `Raise` (events), `As<T>()` (other implemented interfaces), `Monitor()` and `VerifySetup`.

## Test Projects

| Project | Purpose |
|---|---|
| `Tests/Mockolate.Tests` | Behavior tests against generated mocks (methods, properties, indexers, events, delegates, ref structs, `HttpClient`). `GeneratorCoverage/` holds the types that exercise generator special cases |
| `Tests/Mockolate.Internal.Tests` | Tests of internal types (`InternalsVisibleTo`) |
| `Tests/Mockolate.SourceGenerators.Tests` | Generator tests on in-memory compilations, incremental caching tests and the output snapshots in `Snapshot/` |
| `Tests/Mockolate.Analyzers.Tests` | Analyzer and code fix tests with `Microsoft.CodeAnalysis.Testing` verifiers in `Verifiers/` |
| `Tests/Mockolate.Api.Tests` | Public API snapshots per target framework |
| `Tests/Mockolate.ExampleTests` | Realistic usage examples (file system, Azure SDK clients) |
| `Tests/Build.Tests` | Tests for `Pipeline/BenchmarkReport.cs`, which it compiles in |
| `Tests/Aot/Mockolate.AotCompatibility.TestApp` | Native AOT publish smoke test |
