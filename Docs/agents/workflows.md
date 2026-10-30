# Development Workflows

Commands run from the repository root. The examples use PowerShell; on Linux and macOS use `./build.sh` instead of `./build.ps1`.

## Build

```powershell
dotnet build Mockolate.slnx
```

The test projects reference the generator as an analyzer through a project reference, so a normal build regenerates their mocks. If an IDE keeps showing stale generated members, run `dotnet clean` and build again.

## Test

The tests use TUnit on the Microsoft.Testing.Platform, which `global.json` selects as the runner of `dotnet test`. Each test project is an executable, so `dotnet run` works as well. The explicit tests (the snapshot acceptance tests) run only when the filter selects nothing but explicit tests.

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
| Usage examples | `Mockolate.ExampleTests` |
| `Pipeline/BenchmarkReport.cs` | `Build.Tests` |
| Generated code or reflection-sensitive library code | also the `AotSmokeTests` target |

## Snapshots

### Generator output

`Tests/Mockolate.SourceGenerators.Tests/Snapshot/MockGenerationSnapshotTests.cs` runs the generator for each scenario and compares every emitted `.g.cs` file with `Snapshot/Expected/<scenario>/`. The inputs are the files in `Tests/Mockolate.Tests/GeneratorCoverage`. `[DebuggerNonUserCode]` is emitted only by a Release build of the generator and is stripped before comparing.

After an intended change, overwrite the expected files with the explicit acceptance test and review the diff:

```powershell
dotnet test --project Tests/Mockolate.SourceGenerators.Tests --treenode-filter "/*/*/*/AcceptSnapshotChanges"
git diff -- Tests/Mockolate.SourceGenerators.Tests/Snapshot
```

To add a scenario, add an entry to `Scenarios` and run the acceptance test.

### Public API

`Tests/Mockolate.Api.Tests` uses aweXpect.Snapshots.PublicApi. It reads the built `Mockolate.dll` of every target framework listed in `Source/Directory.Build.props` (in the same configuration as the tests) and compares it with `ApiApprovalTests.Mockolate.<framework>.awexpect-snapshot.txt`. On a change it writes `*.awexpect-snapshot.pending.txt` next to the snapshot. To accept:

- rename the pending file to the snapshot name, or
- run the explicit `AcceptApiChanges` test, or
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

CI runs one job per class: `CallbackBenchmarks`, `CompleteEventBenchmarks`, `CompleteIndexerBenchmarks`, `CompleteMethodBenchmarks`, `CompletePropertyBenchmarks`, `MockCreationBenchmarks`. A new benchmark class must be added to the matrix in both `.github/workflows/ci.yml` and `build.yml`. On PRs the results are posted as a comment with the latest `main` run as baseline; on `main` they are appended to `Docs/pages/static/js/data.js` on the `benchmarks` branch, which feeds the benchmarks page of the docs site.

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
| `UnitTests` | Run `Build.Tests`, `Mockolate.Tests`, `Mockolate.Internal.Tests`, `Mockolate.Analyzers.Tests`, `Mockolate.SourceGenerators.Tests` and `Mockolate.ExampleTests` for each framework (`net48` only on Windows), with a TRX and a Cobertura coverage file per project and framework in `TestResults` |
| `CodeCoverage` | `UnitTests` plus a ReportGenerator SonarQube report in `TestResults/reports` |
| `ApiChecks` | Run `Mockolate.Api.Tests` |
| `AotSmokeTests` | Run the AOT smoke test app from the compiled output and published with Native AOT |
| `CodeAnalysis` | SonarCloud analysis (needs `SonarToken`) |
| `Pack` | Build the NuGet package into `Artifacts/Packages`, with a release-specific README |
| `Benchmarks` | Run the benchmarks (`--benchmark-filter`, default `*`) and print the reports |
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

User docs are Markdown pages in `Docs/pages/` for Docusaurus (a `# Title` heading, numbered file names for ordering, `_category_.json` per folder; see [conventions](conventions.md#documentation)). They are published at https://docs.testably.org/Mockolate/ by `Testably/Testably.Site` after each successful build on `main`; there is no local docs build in this repository. Code samples in the docs are not compiled, so check them against `Mockolate.ExampleTests` or a quick test. The README is the NuGet landing page; `Pack` adds version-specific badges to the packaged copy.
