# Development Workflows

Commands run from the repository root. The examples use PowerShell; on Linux and macOS use `./build.sh` instead of `./build.ps1`.

## Build

```powershell
dotnet build Mockolate.slnx
```

The test projects reference the generator as an analyzer through a project reference, so a normal build regenerates their mocks. If an IDE keeps showing stale generated members, run `dotnet clean` and build again.

## Test

```powershell
# One project, all of its target frameworks
dotnet test Tests/Mockolate.Tests

# One framework and a name filter (VSTest filter syntax)
dotnet test Tests/Mockolate.Tests -f net10.0 --filter "FullyQualifiedName~IsTrueTests"

# Everything in the solution
dotnet test Mockolate.slnx
```

Target frameworks per test project:

| Project | Frameworks |
|---|---|
| `Mockolate.Tests`, `Mockolate.Internal.Tests`, `Mockolate.ExampleTests` | `net10.0`, `net8.0`, `net48` (Windows only) |
| `Mockolate.SourceGenerators.Tests`, `Mockolate.Analyzers.Tests`, `Build.Tests` | `net10.0` |
| `Mockolate.Api.Tests` | `net8.0` |

A newer language feature can compile for `net10.0` and fail for `net48`; build or test `net48` too when you use one.

### Which tests to run

| Change in | Run |
|---|---|
| `Source/Mockolate` | `Mockolate.Tests`, `Mockolate.Internal.Tests`, `Mockolate.Api.Tests` |
| `Source/Mockolate.SourceGenerators` | `Mockolate.SourceGenerators.Tests`, `Mockolate.Tests` (it compiles the generated mocks) |
| `Source/Mockolate.Analyzers*` | `Mockolate.Analyzers.Tests` |
| Usage examples | `Mockolate.ExampleTests` (not part of the `UnitTests` target, so CI does not run it) |
| `Pipeline/BenchmarkReport.cs` | `Build.Tests` |

## Snapshots

### Generator output

`Tests/Mockolate.SourceGenerators.Tests/Snapshot/MockGenerationSnapshotTests.cs` runs the generator for each scenario and compares every emitted `.g.cs` file with `Snapshot/Expected/<scenario>/`. The inputs are the files in `Tests/Mockolate.Tests/GeneratorCoverage`. `[DebuggerNonUserCode]` is emitted only by a Release build of the generator and is stripped before comparing.

After an intended change, overwrite the expected files with the explicit acceptance test and review the diff:

```powershell
dotnet test Tests/Mockolate.SourceGenerators.Tests --filter "FullyQualifiedName~AcceptSnapshotChanges" -- xUnit.Explicit=only
git diff -- Tests/Mockolate.SourceGenerators.Tests/Snapshot
```

To add a scenario, add an entry to `Scenarios` and run the acceptance test.

### Public API

`Tests/Mockolate.Api.Tests` uses aweXpect.Snapshots.PublicApi. It reads the built `Mockolate.dll` of every target framework listed in `Source/Directory.Build.props` (in the same configuration as the tests) and compares it with `ApiApprovalTests.Mockolate.<framework>.awexpect-snapshot.txt`. On a change it writes `*.awexpect-snapshot.pending.txt` next to the snapshot. To accept:

- rename the pending file to the snapshot name, or
- run the explicit `AcceptApiChanges` test, or
- run the tests with `AWEXPECT_ACCEPT_SNAPSHOTS=all`.

```powershell
dotnet test Tests/Mockolate.Api.Tests --filter "FullyQualifiedName~AcceptApiChanges" -- xUnit.Explicit=only
```

Commit the updated snapshots with the change; never commit pending files.

## AOT Smoke Test

`Tests/Aot/Mockolate.AotCompatibility.TestApp` publishes a mock with Native AOT. CI does not run it, so publish it manually after changes to generated code or reflection-sensitive library code:

```powershell
dotnet publish Tests/Aot/Mockolate.AotCompatibility.TestApp -c Release -f net10.0
```

The native link step needs the platform toolchain (on Windows the Visual Studio C++ build tools). Trim and AOT warnings appear before linking.

## Benchmarks

`Benchmarks/Mockolate.Benchmarks` compares Mockolate with Moq, NSubstitute, FakeItEasy, TUnit.Mocks and Imposter using BenchmarkDotNet (`MediumRun`, in-process toolchain, `MemoryDiagnoser`). Run a single class in Release:

```powershell
./build.ps1 Benchmarks --benchmark-filter "*MockCreationBenchmarks*"
# or directly
dotnet run -c Release --project Benchmarks/Mockolate.Benchmarks -- --filter "*MockCreationBenchmarks*"
```

CI runs one job per class: `CallbackBenchmarks`, `CompleteEventBenchmarks`, `CompleteIndexerBenchmarks`, `CompleteMethodBenchmarks`, `CompletePropertyBenchmarks`, `MockCreationBenchmarks`. A new benchmark class must be added to the matrix in both `.github/workflows/ci.yml` and `build.yml`. On PRs the results are posted as a comment with the latest `main` run as baseline; on `main` they are appended to `Docs/pages/static/js/data.js` on the `benchmarks` branch, which feeds the benchmarks page of the docs site.

## Mutation Tests

Stryker mutates `Source/Mockolate` against `Mockolate.Tests` and `Mockolate.Internal.Tests` on `net8.0` with mutation level `Advanced`. Outside `main` it only mutates code changed since `main`. A full run takes long; run it only when asked:

```powershell
./build.ps1 MutationTestExecution
```

Reports go to `Artifacts/Stryker`. The `MutationTests` target also prepares the PR comment and expects to run in GitHub Actions.

## Build Pipeline (Nuke)

`Pipeline/Build.csproj` is a Nuke build, started through `build.ps1`, `build.sh` or `build.cmd`. Always name a target: without one, Nuke runs the defaults `ApiChecks`, `Benchmarks` and `CodeAnalysis`. `./build.ps1 --help` lists the targets. The configuration is Debug locally and Release on CI. Every run cleans `Artifacts/` and `TestResults/` first.

| Target | Purpose |
|---|---|
| `Compile` | Restore and build the solution with the GitVersion-derived version |
| `UnitTests` | Run `Build.Tests`, `Mockolate.Tests`, `Mockolate.Internal.Tests`, `Mockolate.Analyzers.Tests` and `Mockolate.SourceGenerators.Tests` for each framework (`net48` only on Windows) with coverage |
| `CodeCoverage` | `UnitTests` plus a ReportGenerator OpenCover report in `TestResults/reports` |
| `ApiChecks` | Run `Mockolate.Api.Tests` |
| `CodeAnalysis` | SonarCloud analysis (needs `SonarToken`) |
| `Pack` | Build the NuGet package into `Artifacts/Packages`, with a release-specific README |
| `Benchmarks` | Run the benchmarks (`--benchmark-filter`, default `*`) and print the reports |
| `BenchmarkComment`, `PublishBenchmarkReport` | CI only: PR comment and docs site data |
| `MutationTests`, `MutationTestExecution`, `MutationTestComment`, `MutationTestDashboard` | Stryker run, PR comment and dashboard upload |
| `UpdateReadme` | Write the packaged README to `Artifacts/README.md` |

## CI Workflows

| Workflow | Trigger | Jobs |
|---|---|---|
| `ci.yml` | PR to `main` | Unit tests (`CodeCoverage`) on Ubuntu, Windows and macOS; API tests; mutation tests; benchmark matrix; SonarCloud (same-repo PRs, not Dependabot); test result publishing |
| `ci-analysis.yml` | After `CI` completes | Posts the mutation and benchmark comments on the PR |
| `build.yml` | Push to `main`, `v*` tags | Same checks as CI plus pack; on `main` publishes the benchmark report and uploads the mutation dashboard; on a tag pushes to nuget.org (trusted publishing), creates the GitHub release for non-prerelease tags and comments on released issues |
| `notify-docs-site.yml` | After a successful `Build` on `main` | Triggers a rebuild of the docs site in `Testably/Testably.Site` |

Mergify (`.github/mergify.yml`) squash-merges Dependabot PRs once the three unit test jobs and the API tests pass.

## Documentation

User docs are Markdown pages in `Docs/pages/` (Docusaurus front matter, numbered file names for ordering, `_category_.json` per folder). They are published at https://docs.testably.org/Mockolate/ by `Testably/Testably.Site` after each successful build on `main`; there is no local docs build in this repository. Code samples in the docs are not compiled, so check them against `Mockolate.ExampleTests` or a quick test. The README is the NuGet landing page; `Pack` adds version-specific badges to the packaged copy.
