# Development Workflows

Commands run from the repository root. The examples use PowerShell; on Linux and macOS use `./build.sh` instead of `./build.ps1`.

## Build

```powershell
dotnet build Mockolate.slnx
```

The test projects reference the generator as an analyzer through a project reference, so a normal build regenerates their mocks. If an IDE keeps showing stale generated members, run `dotnet clean` and build again.

## Test

The tests use TUnit on the Microsoft.Testing.Platform, which `global.json` selects as the runner of `dotnet test`. Each test project is an executable, so `dotnet run` works as well. The explicit tests (the API snapshot acceptance test) run only when the filter selects nothing but explicit tests.

```powershell
# One project, all of its target frameworks
dotnet test --project Tests/Mockolate.Tests

# One framework and a tree node filter (/assembly/namespace/class/test, nested classes match with a wildcard)
dotnet test --project Tests/Mockolate.Tests -f net10.0 --treenode-filter "/*/*/*IsTrueTests/*"

# The same without dotnet test
dotnet run --project Tests/Mockolate.Tests -f net10.0 -- --treenode-filter "/*/*/*IsTrueTests/*"

# Everything in the solution
dotnet test --solution Mockolate.slnx
```

Target frameworks per test project:

| Project | Frameworks |
|---|---|
| `Mockolate.Tests`, `Mockolate.Internal.Tests`, `Mockolate.ExampleTests`, `Mockolate.TUnit.Tests` | `net11.0` (union mode); `net10.0`, `net8.0` and `net48` (Windows only) in classic mode |
| `Mockolate.SourceGenerators.Tests` | `net11.0`, `net10.0` (compiles against `MinimumRoslynVersion`, without the snapshot and union tests) |
| `Mockolate.Analyzers.Tests` | `net11.0` |
| `Build.Tests` | `net10.0` |
| `Mockolate.Api.Tests` | `net8.0` |

Every framework compiles with C# 15. The setup surface is decided by `MockolateDisableUnionParameters`, which `Tests/Directory.Build.props` sets for every framework except `net11.0`, not by the framework itself. Guard tests of union-only syntax (a lambda as an argument, `AnyParameters()` after matchers, values mixed with matchers on a member with more than four parameters) with `#if NET11_0_OR_GREATER`; write tests that hold in both modes so they compile in both. Classic failure messages describe a null argument as an empty string, union ones as `null`.

A newer language feature can compile for `net10.0` and fail for `net48`; build or test `net48` too when you use one.

### Which tests to run

| Change in | Run |
|---|---|
| `Source/Mockolate` | `Mockolate.Tests`, `Mockolate.Internal.Tests`, `Mockolate.Api.Tests` |
| `Source/Mockolate.SourceGenerators` | `Mockolate.SourceGenerators.Tests`, `Mockolate.Tests` (it compiles the generated mocks; `net11.0` for union mode and `net10.0` for classic mode) |
| `Source/Mockolate.Analyzers*` | `Mockolate.Analyzers.Tests` |
| Usage examples | `Mockolate.ExampleTests` |
| `AutoMockArguments` attributes (`Sources.TestFrameworks*.cs`, `Mockolate.targets`) | `Mockolate.TUnit.Tests`, `Mockolate.SourceGenerators.Tests` |
| `Pipeline/BenchmarkReport.cs` | `Build.Tests` |
| Generated code or reflection-sensitive library code | also the `AotSmokeTests` target |

## Snapshots

### Generator output

`Tests/Mockolate.SourceGenerators.Tests/Snapshot/MockGenerationSnapshotTests.cs` runs the generator for each scenario. The inputs are the files in `Tests/Mockolate.Tests/GeneratorCoverage`. Every scenario must compile without diagnostics: classic scenarios with C# 14, all with warnings and the documentation comments of the generated code checked, which the other test projects do not do.

Only the scenarios with `Snapshot` (the comprehensive types, a base class with additional interfaces, `HttpClient`, logging, auto-mocking and ref structs) compare the emitted `.g.cs` files with aweXpect snapshots in `Snapshot/MockGenerationSnapshotTests/GeneratorOutput_MatchesExpectedSnapshot/`. A special case is covered by targeted tests, so it adds a scenario without `Snapshot`. `[DebuggerNonUserCode]` is emitted only by a Release build of the generator and is stripped before comparing.

The files of the mocked types (`Mock.<Type>.g.cs`) of a scenario are compared as one snapshot named after the scenario, e.g. `ComprehensiveInterface_CanBeCreated.g`, with a `// <file name>` line before each file. Files that are not specific to a mocked type (`Mock.g`, `Mock.AsExtensions.g`, `MethodSetups.g`, `IndexerSetups.g` and the other files without a `Mock.` prefix) share the snapshot of a file with the same content, named after the file (`Mock.g`). When the scenarios generate several contents of a file, each is named after the first scenario (in ordinal order) that generates it, e.g. `MethodSetups.ComprehensiveInterface_CanBeCreated.g`. A change can therefore rename snapshots of other scenarios.

A special case whose members can share a type with an existing coverage type is added to that type, with member names that name the edge case (e.g. `IOptionalMethodParameters.WithSibling`); it only gets its own type when the edge case depends on the type, such as its base class, its other members or its kind. Union mode (`_Unions`) only changes the setups and verifications of non-generic methods with unique names and indexers with unique key counts, without `params` or ref struct parameters, so a special case only adds a `_Unions` scenario when it reaches that code.

Locally, a snapshot that is missing or different opens a diff tool through `aweXpect.Snapshots.DiffEngine` (e.g. Rider, Visual Studio or VS Code) that compares the pending file with the snapshot, so a single change can be reviewed and taken over there. It never opens on a build server; set `DiffEngine_Disabled=true` to turn it off locally, e.g. when a generator change touches many snapshots.

After an intended change, accept the snapshots and review the diff:

```powershell
$env:AWEXPECT_ACCEPT_SNAPSHOTS = "all"; dotnet test --project Tests/Mockolate.SourceGenerators.Tests; Remove-Item Env:AWEXPECT_ACCEPT_SNAPSHOTS
git diff -- Tests/Mockolate.SourceGenerators.Tests/Snapshot
```

The accepting run still fails, so that the next run compares with the accepted snapshots. A snapshot that no scenario uses any more is reported after the test run on a build server and deleted while accepting; to check locally, run all tests of the project with `CI=true`.

To add a scenario, add an entry to `Scenarios` and accept the snapshots.

### Public API

`Tests/Mockolate.Api.Tests` uses aweXpect.Snapshots.PublicApi. It reads the built `Mockolate.dll` of every target framework listed in `Source/Directory.Build.props` (in the same configuration as the tests) and compares it, without the `Mockolate.Internals` namespace, with `ApiApprovalTests/Mockolate/<framework>.awexpect-snapshot.cs`. On a change it writes `*.awexpect-snapshot.pending.cs` next to the snapshot. To accept:

- rename the pending file to the snapshot name, or
- run the explicit `AcceptApiChanges` test, or
- take over the change in the diff tool that opens locally through `aweXpect.Snapshots.DiffEngine` (e.g. Rider, Visual Studio or VS Code; never on a build server, and `DiffEngine_Disabled=true` turns it off), or
- run the tests with `AWEXPECT_ACCEPT_SNAPSHOTS=all`.

```powershell
dotnet test --project Tests/Mockolate.Api.Tests --treenode-filter "/*/*/*/AcceptApiChanges"
```

Commit the updated snapshots with the change; never commit pending files.

## AOT Smoke Test

`Tests/Aot/Mockolate.AotCompatibility.TestApp` is a console app with one named check per feature (setups, matchers, verifications, events, protected and static members, `HttpClient`, delegates and class mocks). It prints `PASS` or `FAIL` per check and exits with `1` when one fails. The `AotSmokeTests` target runs it from the compiled output, publishes it with Native AOT for the current platform to `Artifacts/Aot/<project>` and runs the published executable; CI does this on Ubuntu, Windows and macOS.

```powershell
./build.ps1 AotSmokeTests
# or directly
dotnet run --project Tests/Aot/Mockolate.AotCompatibility.TestApp
dotnet publish Tests/Aot/Mockolate.AotCompatibility.TestApp -c Release
```

A check that fails only in the published run points to code that behaves differently without the JIT. Compiler, trim and AOT warnings fail the build and the publish. The native link step needs the platform toolchain (on Windows the Visual Studio C++ build tools). Add a check when you add a feature.

## Benchmarks

`Benchmarks/Mockolate.Benchmarks` compares Mockolate with Moq, NSubstitute, FakeItEasy, TUnit.Mocks and Imposter using BenchmarkDotNet (`MediumRun`, in-process toolchain, `MemoryDiagnoser`). Run a single class in Release:

```powershell
./build.ps1 Benchmarks --benchmark-filter "*MockCreationBenchmarks*"
# or directly
dotnet run -c Release --project Benchmarks/Mockolate.Benchmarks -- --filter "*MockCreationBenchmarks*"
```

CI runs one job per class: `AsyncBenchmarks`, `CallbackBenchmarks`, `CompleteEventBenchmarks`, `CompleteIndexerBenchmarks`, `CompleteMethodBenchmarks`, `CompletePropertyBenchmarks`, `CompleteUnconfiguredBenchmarks`, `CompleteWorkflowBenchmarks`, `MockCreationBenchmarks`, `SequenceBenchmarks`. A new benchmark class must be added to the matrix in both `.github/workflows/ci.yml` and `build.yml`. On PRs each job also runs the Mockolate benchmarks of `main` (`BenchmarkMain`) on the same runner, and the comment compares with them; on `main` the results are appended to `Docs/pages/static/js/data.js` on the `benchmarks` branch, which feeds the benchmarks page of the docs site.

## Mutation Tests

Stryker mutates `Source/Mockolate` against `Mockolate.Tests` and `Mockolate.Internal.Tests` on `net8.0` with mutation level `Advanced`, through its runner for the Microsoft.Testing.Platform (`--test-runner mtp`). The version is the `dotnet-stryker` `PackageDownload` in `Pipeline/Build.csproj`. Outside `main` it only mutates code changed since `main`. A full run takes long; run it only when asked:

```powershell
./build.ps1 MutationTests
```

Reports go to `Artifacts/Stryker`, the Markdown body of the PR comment to `Artifacts/MutationTest_Mockolate.md`. `MutationTestsComment` and `MutationTestsDashboard` post the comment and upload the report, and expect to run in GitHub Actions.

## Build Pipeline (Fallout)

`Pipeline/Build.csproj` is a Fallout build, started through `build.ps1`, `build.sh` or `build.cmd`. Always name a target: without one, Fallout runs the defaults `ApiChecks`, `Benchmarks` and `CodeAnalysis`. `./build.ps1 --help` lists the targets. The configuration is Debug locally and Release on CI. Every target that compiles, and `UpdateReadme`, cleans `Artifacts/` and `TestResults/` first.

| Target | Purpose |
|---|---|
| `Compile` | Restore and build the solution with the GitVersion-derived version |
| `UnitTests` | Run `Build.Tests`, `Mockolate.Tests`, `Mockolate.Internal.Tests`, `Mockolate.Analyzers.Tests`, `Mockolate.SourceGenerators.Tests`, `Mockolate.ExampleTests` and `Mockolate.TUnit.Tests` for each framework (`net48` only on Windows), with a TRX and a Cobertura coverage file per project and framework in `TestResults` |
| `CodeCoverage` | `UnitTests` plus a ReportGenerator SonarQube report in `TestResults/reports` |
| `ApiChecks` | Run `Mockolate.Api.Tests` |
| `AotSmokeTests` | Run the AOT smoke test app from the compiled output and published with Native AOT |
| `CodeAnalysis` | SonarCloud analysis (needs `SonarToken`) |
| `Pack` | Build the NuGet package into `Artifacts/Packages`, with a release-specific README |
| `Benchmarks` | Run the benchmarks (`--benchmark-filter`, default `*`) and print the reports |
| `BenchmarkMain` | Run the Mockolate benchmarks of the `main` that `HEAD` is based on into `Artifacts/MainBenchmarks` |
| `BenchmarkComment`, `PublishBenchmarkReport` | CI only: PR comment and docs site data |
| `MutationTests`, `MutationTestsComment`, `MutationTestsDashboard` | Stryker run, PR comment and dashboard upload |
| `UpdateReadme` | Write the packaged README to `Artifacts/README.md` |

## CI Workflows

| Workflow | Trigger | Jobs |
|---|---|---|
| `ci.yml` | PR to `main` | Unit tests (`CodeCoverage`) and AOT smoke tests on Ubuntu, Windows and macOS; API tests; mutation tests; benchmark matrix; SonarCloud (same-repo PRs, not Dependabot); test result publishing |
| `ci-analysis.yml` | After `CI` completes | Posts the mutation and benchmark comments on the PR |
| `build.yml` | Push to `main`, `v*` tags | Same checks as CI plus pack; uploads the mutation dashboard; on `main` publishes the benchmark report; on a tag pushes to nuget.org (trusted publishing), creates the GitHub release for non-prerelease tags and comments on released issues |
| `notify-docs-site.yml` | After a successful `Build` on `main` | Triggers a rebuild of the docs site in `Testably/Testably.Site` |

Mergify (`.github/mergify.yml`) squash-merges Dependabot PRs once the three unit test jobs and the API tests pass.

## Documentation

User docs are Markdown pages in `Docs/pages/` for Docusaurus that follow the [docs style guide](https://github.com/Testably/Testably.Site/blob/main/Docs/style-guide.md) and the [conventions](conventions.md#documentation). They are published at https://docs.testably.org/Mockolate/ by `Testably/Testably.Site` after each successful build on `main`; there is no local docs build in this repository. Code samples in the docs are not compiled, so check them against `Mockolate.ExampleTests` or a quick test. The README is the NuGet landing page; `Pack` adds version-specific badges to the packaged copy.
