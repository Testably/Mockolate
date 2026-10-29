# Design Goals

These goals guide agents and contributors when a change to Mockolate has more than one reasonable solution. [Conventions](conventions.md) turns them into concrete rules for the code and the tests.

Correctness is a hard requirement. Every other goal is a strong default: follow it, and when a change has to break one, say so in the pull request and explain why.

## Goals

When goals conflict, the earlier one wins.

1. **Correct (must).** A mock behaves exactly as its setups and behavior say, and records every interaction, also under concurrency. A verification failure states what was expected and what was found, and tests pin the complete message.
2. **Generated code compiles in every consumer.** The generator runs inside the user's compilation: nullable enabled or not, warnings as errors, any member names, C# keywords as identifiers, every supported target framework and every compiler from `MinimumRoslynVersion` on. A type or member that cannot be mocked is reported by an analyzer or left without setup surface; it never produces a compiler error in generated code.
3. **Stable and compatible.** Semantic versioning: breaking changes only in a major version, on purpose and documented. Both the runtime library and the generated surface (`sut.Mock.Setup.X`, `sut.Mock.Verify.X`, ...) are public API that user code calls. Library changes show up in the API snapshots, generator changes in the generator snapshots, and both are reviewed. The package has no dependency except `System.Memory` on `netstandard2.0`; keep it that way.
4. **AOT and trimming by construction.** Source generation instead of runtime proxies: no reflection-based dispatch, `Reflection.Emit`, expression trees or runtime generic instantiation, in the library and in the generated code.
5. **Errors at compile time.** Prefer a typed API, generated members and analyzers over runtime checks, so misuse such as an unmockable type or an unused verification fails the build.
6. **One consistent API.** Mocks come from `CreateMock()` and every capability lives under the `sut.Mock` verbs (`Setup`, `Verify`, `Raise`, `Monitor`, `As<T>()`, ...). Methods, properties, indexers, events and delegates share the same vocabulary across every arity, and IntelliSense shows only what the user can call.
7. **Fast and allocation-free on the hot path.** Recording an interaction, finding the matching setup and counting for a verification are the common case and do not allocate. Mockolate is benchmarked against Moq, NSubstitute, FakeItEasy, TUnit.Mocks and Imposter on every pull request, so back every performance claim with a benchmark before and after.
8. **Extensible.** Add-ons build on the same public extension points the library uses itself: `extension` blocks on `It` and the setup interfaces, `IParameter`, `IDefaultValueGenerator` and `MockBehavior`. The `HttpClient` support in `Web/` is the reference.
9. **Documented.** A user-visible feature is not done until it is described on the docs site with an example.

## Before You Commit

Run the checks that match the change (commands in [workflows](workflows.md)):

- **Any code change:** the tests of the affected projects on `net10.0` and on the oldest framework they run on (`net48` on Windows, `net8.0` elsewhere).
- **Generator output:** the generator snapshots, accepted and reviewed, and a type in `GeneratorCoverage` for a new special case.
- **Public API of `Mockolate`:** the API snapshot tests; review and accept intentional changes, never commit pending snapshots.
- **Generated code or reflection-sensitive library code:** the AOT smoke tests, and a new check there for a new feature.
- **Hot path or a performance claim:** a benchmark before and after.
- **Mockability rules:** both the generator and `MockabilityAnalyzer`, with tests on both sides.
- **User-visible feature:** the docs page and its example.
- **Static analysis:** no new Sonar or compiler findings, coverage above 90%. Avoid the common Sonar findings: high cognitive complexity, duplicated string literals, unused members and parameters, methods that could be static. Do not suppress a finding or exclude code from coverage without asking the maintainer first.

## Reviews

Verify every review finding against the code before changing anything. If you disagree, push back once with concrete reasoning; if the reviewer or the maintainer confirms the finding, implement it.
