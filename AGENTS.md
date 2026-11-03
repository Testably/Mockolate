# Mockolate Agent Guide

Mockolate is a source-generated mocking library for .NET. Users write `IFoo.CreateMock()`; a Roslyn incremental generator emits the mock at compile time, analyzers validate the usage, and the runtime library records interactions, evaluates setups and answers verifications. Read the docs under [References](#references) only when a task needs them.

## SDK and Build

- Use the SDK pinned in `global.json` (currently a .NET 11 preview, `rollForward: latestPatch`). Tests also run on .NET 10 and .NET 8, and on .NET Framework 4.8 on Windows only. On .NET 11 the test projects compile with the preview language version and set `MockolateUnionParameters`, so the union-typed setup surface is exercised there.
- Build from the root with `dotnet build Mockolate.slnx`. Shared settings live in the root `Directory.Build.props` (strong naming, `MinimumRoslynVersion`), `Source/Directory.Build.props` (target frameworks, warnings as errors, AOT and trimming flags) and `Tests/Directory.Build.props` (test frameworks, TUnit, aweXpect).
- Package versions belong only in `Directory.Packages.props`. `VersionOverride` is used only to pin Roslyn packages to `$(MinimumRoslynVersion)`.

## Design Goals

Correctness is a must. The other goals are strong defaults and win in this order when they conflict: generated code compiles in every consumer, stable and compatible (including the generated surface), AOT and trimming by construction, errors at compile time, one consistent API under `sut.Mock`, an allocation-free hot path, extensible, documented. [Docs/agents/design-goals.md](Docs/agents/design-goals.md) explains each goal and the checks before a commit; [Docs/agents/conventions.md](Docs/agents/conventions.md) lists the concrete rules for the library, the generator, the analyzers, the tests and the docs.

## Project Constraints

- **Target frameworks**: `Mockolate` ships `netstandard2.0`, `net8.0`, `net10.0` and `net11.0`. The generator, analyzers and code fixers target `netstandard2.0` only. Code compiled for `netstandard2.0` or `net48` has no `IsExternalInit`, `System.Index` or `System.Range` unless polyfilled, so records, `init` and list patterns need care: `Mockolate` polyfills `IsExternalInit`, `Mockolate.Tests` does not, and guards such code with `#if NET8_0_OR_GREATER`.
- **Roslyn version**: the generator, analyzers and code fixers compile against `MinimumRoslynVersion` (5.0.0), the oldest compiler they must load in. Never use a newer Roslyn API. It cannot drop below 5.0 because the emitted `Mock.g.cs` uses a C# 14 extension block.
- **Generated code** runs inside every consumer's compilation. Keep it reflection-free, fully qualified with `global::`, clash-free with user member names, valid under nullable and warnings-as-errors, and deterministic. The generator reports no diagnostics: it skips what it cannot mock and `MockabilityAnalyzer` reports it, so mockability rules change in both. Keep the generator incremental: pipeline entities must have value equality (`EquatableArray`, records) so caching works; `MockGenerator.EqualityTests` covers this.
- **Arity**: the library hand-writes setup and verification types for up to four parameters; the generator emits the same shapes for more. A change to one arity applies to every arity in both places.
- **Analyzers**: rule IDs are `MockolateNNNN`. Add the descriptor in `Rules.cs`, its texts in `Resources.resx`, the row in `AnalyzerReleases.Unshipped.md` and the user docs in `Docs/pages/10-analyzers.md`.
- **AOT and trimming**: the runtime library sets `IsAotCompatible` and `IsTrimmable` for `net8.0`+ with warnings as errors. Do not add reflection-based dispatch to the library or the generated code.
- **Performance**: recording, setup matching and verification are hot paths with allocation-free fast paths (`FastMockInteractions`, the `Fast*Buffer` types). Measure hot-path changes with the benchmarks before and after.
- **Public API**: every change to the public surface of `Mockolate` shows up in the API snapshots. Document new public members with XML comments. Mark breaking changes with `!` in the PR title (`feat!:`, `refactor!:`).
- **Strong naming**: all assemblies are signed with `strongname.snk`; an `InternalsVisibleTo` needs `PublicKey="$(PublicKey)"`.
- **Style**: follow `.editorconfig`: tabs, explicit types instead of `var`, no `this.`, `System` usings first; private fields are `_camelCase`. New extension members use C# 14 `extension` blocks. Tests use TUnit with aweXpect (`await That(...)`) and are preferably named `<Subject>_Should<Outcome>`.

## Validation

- Run the tests for what you changed before pushing; see [workflows](Docs/agents/workflows.md#which-tests-to-run).
- Generator output changes must update the snapshots in `Tests/Mockolate.SourceGenerators.Tests/Snapshot/Expected`: run the explicit `AcceptSnapshotChanges` test and review the diff. New generator special cases go into `Tests/Mockolate.Tests/GeneratorCoverage`, which both test projects share.
- Public API changes must update `Tests/Mockolate.Api.Tests/*.awexpect-snapshot.txt` (one per target framework). Never commit `*.awexpect-snapshot.pending.txt`.
- Analyzer and code fix changes need tests in `Tests/Mockolate.Analyzers.Tests`.
- Changes in `Pipeline/BenchmarkReport.cs` need `Tests/Build.Tests`.
- Coverage must stay above 90% and SonarCloud must report no issues.

## Repository Conventions

- Files are stored with LF and checked out with CRLF on Windows (`.gitattributes` `text=auto`, `.editorconfig` `end_of_line = crlf`). Raw string literals take the file's line endings, so compare multi-line strings with `.IgnoringNewlineStyle()`.
- Some files start with a UTF-8 BOM and most do not. Keep each file's encoding: prefer the edit tools over PowerShell rewrites (`Set-Content`, `Out-File`), which add or drop the BOM depending on version and `-Encoding`, and check `git diff` for a changed U+FEFF.
- User docs live in `Docs/pages/` and are published to https://docs.testably.org/Mockolate/. Update them for user-visible changes. The README is the NuGet landing page and links there; update it only when the features or the getting started steps change.

## Documentation

Pages under `Docs/pages` are published on docs.testably.org and follow the [docs style guide](https://github.com/Testably/Testably.Site/blob/main/Docs/style-guide.md). The key rules:

- US English, sentence case for headings and labels, no em dashes, one line per paragraph.
- A `description` in the front matter of every page; the H1 is the title; order with unique `NN-` prefixes.
- Relative file links within the repository, site-absolute `/Library/...` links across libraries.
- Install instructions only through `<PackageInstall package="..." />`, never hand-written commands or NuGet badges.
- Examples check results with `await That(...)`; complete examples use TUnit `[Test]`.

The Mockolate-specific documentation rules are in [Conventions](Docs/agents/conventions.md#documentation).

## Git Workflow

- Branch from `main` and open a pull request. PRs are squash-merged, so the PR title becomes the commit subject and must follow conventional commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `ci:`, `chore:`).
- Commit messages: the subject is the PR title, the body explains why (not what or how), each paragraph is a single line, and there are no co-author or attribution trailers.
- Never commit `.claude/`, `Artifacts/`, `TestResults/` or pending snapshots.
- Dependabot PRs use `chore:` and are auto-merged by Mergify once unit and API tests pass. Pushing a `vX.Y.Z` tag publishes to nuget.org; do not create tags unless asked.
- Check review findings against the code before acting on them.

## References

- [Design goals](Docs/agents/design-goals.md): the goals for every change, in priority order, and the checks before a commit.
- [Conventions](Docs/agents/conventions.md): API shape, hot path, exceptions and messages, generator, analyzer, test and docs rules.
- [Workflows](Docs/agents/workflows.md): build, test, snapshot, benchmark, mutation, AOT and documentation commands, the Fallout targets in `Pipeline/` and the CI workflows.
- [Project structure](Docs/agents/project-structure.md): projects, architecture and where code lives.
