# Conventions

The rules the code already follows. Read the parts that match the change and copy the pattern of the nearest existing code. The [design goals](design-goals.md) explain why.

## General

- Follow `.editorconfig`: tabs, file-scoped namespaces, `System` usings first, no `this.`, explicit types instead of `var`, expression bodies for methods and properties. Private instance fields are `_camelCase` by convention (`.editorconfig` does not enforce it). Collection expressions keep a trailing comma (`[a, b,]`, `is { Count: > 0, }`).
- Every public member has XML docs. Implementations use `/// <inheritdoc cref="IInterface.Member(T)" />` with an explicit `cref`; members inside an `extension` block point to the containing class. Write keywords as `<see langword="null" />`.
- Suppress a Sonar rule as tightly as possible with `#pragma warning disable Sxxxx // <rule title>` and the matching `restore`, never with `[SuppressMessage]`. Explain an intentionally surviving mutant with `// Stryker disable once <Mutator> : <reason>`.
- Guard framework-specific code with `#if`: `NET8_0_OR_GREATER` for `Span<T>` matchers and `ValueTask`, `NET9_0_OR_GREATER` for `allows ref struct`, `NET10_0_OR_GREATER` for `System.Threading.Lock`. Polyfills live in `Source/Mockolate/Internals/Polyfills` or come from the `IsExternalInit` and `Nullable` packages.

## Runtime Library (`Source/Mockolate`)

### API shape

- **Arity:** the library contains the setup, verification and buffer types for up to four parameters (`ReturnMethodSetup<TReturn, T1..T4>`, `IndexerSetup<TValue, T1..T4>`, `FastMethod0..4Buffer`, ...), one full copy per arity. The generator emits the same types for more parameters (`MaxExplicitParameters` in `Sources.MockClass.cs`; the higher arities come from `Sources.MethodSetups.cs`, `Sources.IndexerSetups.cs` and `Sources.RefStructMethodSetups.cs`). A change to one arity applies to all of them, in the library and in the generator.
- **Builders:** setups are chains of small interfaces (`IReturnMethodSetup` → `Do`/`Returns`/`Throws` → `InParallel`/`When` → `For`/`Only`). Concrete setups implement them explicitly and return `this`. Terminators and convenience members (`Forever`, `OnlyOnce`, `ReturnsAsync`, the count methods on `VerificationResult<T>`) are C# 14 `extension` blocks; add new extensions as `extension` blocks, not as classic `this` extension methods.
- **Naming:** reuse the existing verbs. Setups use `Returns`, `Throws`, `Do`, `TransitionTo`, `InParallel`, `When`, `For`, `Only`. Behavior toggles are `-ing` methods with a `bool` defaulting to `true` that return a copy of the immutable `MockBehavior` record (`SkippingBaseClass`, `ThrowingWhenNotSetup`). Counts are `Never`, `Once`, `Twice`, `Exactly`, `AtLeast...`, `AtMost...`, `Between`, `Times`.
- **Matchers:** `It` is a non-static `partial class`, so add-ons extend it with `extension(It _)`. Each matcher lives in its own `It.<Name>.cs`, returns an interface (`IParameterWithCallback<T>`, `It.IIsParameter<T>`) and overrides `ToString()` to reproduce its call syntax, which is what failure messages show.
- **Members for generated code:** hide new members that are public only because generated code calls them with `[EditorBrowsable(EditorBrowsableState.Never)]` and say so in their docs (see `It.IsValue<T>`). Older infrastructure such as `MockRegistry.SetupMethod` is not hidden yet.
- **Debugging:** types carry `#if !DEBUG [DebuggerNonUserCode] #endif`, so the debugger steps from the test straight into the user's callback.

### Hot path

Recording, setup matching and counting must not allocate. Measure changes with the benchmarks.

- Interactions are recorded into typed per-member buffers (`FastMockInteractions`, `Fast*Buffer`, `ChunkedSlotStorage`) and boxed into `IInteraction` only on demand.
- No LINQ, closures, iterators or boxing on these paths. Use index-based loops, `static` lambdas and the `TState` overloads (`GetOrCreateBuffer`, `Callback<T>.Invoke<TState>`). LINQ is fine on cold paths such as failure messages.
- Concurrency: the per-member setup tables are copy-on-write arrays published with `Volatile.Write` and read without a lock; lazy fields are created with `Interlocked.CompareExchange`; new locks use the `MockolateLock` alias. Do not run user code (a callback or a matcher) while holding a lock; snapshot under the lock and invoke afterwards, as `MockSetups.Methods.cs` does.
- The latest matching setup wins: lookups walk the setups backwards, and scenario setups come before the default ones.
- Callbacks: register with `_callbacks = _callbacks.Register(callback)`; modifiers act on `_callbacks?.Active`. Sequences advance with `Interlocked.Increment`.

### Exceptions and messages

- Throw `MockException` for misuse, `MockNotSetupException` when a strict mock is called without setup and `MockVerificationException` (or `MockVerificationTimeoutException`) for failed verifications.
- Messages are full sentences. Verifications read `Expected that mock <expectation> <count>, but it <found>.`, for example `Expected that mock invoked method Dispense(It.IsAny<string>(), It.IsAny<int>()) at least 3 times, but it did twice.` Build the expectation lazily (`Func<string>`) so the success path does not format strings.

### Async

Async code is limited to waiting for verifications (`Within`, `WithCancellation`) and the `ReturnsAsync`/`ThrowsAsync` helpers. Every `await` uses `ConfigureAwait(false)`. A cancelled caller token surfaces as `OperationCanceledException`, while an expired `Within` or `WithCancellation` surfaces as `MockVerificationTimeoutException`. `ValueTask` members are `net8.0`+ only. Awaitable verifications belong to the separate `aweXpect.Mockolate` package.

## Source Generator (`Source/Mockolate.SourceGenerators`)

- **Incremental caching:** every pipeline model is value-equatable (records, `EquatableArray<T>`, hand-written `IEquatable` for aggregates); never put an `ISymbol`, `Compilation` or `SyntaxNode` into the pipeline. `EntityCache` lives for one compilation, and results built inside its scope are materialized there (`.ToArray()`). Each shared output (`MethodSetups.g.cs`, `IndexerSetups.g.cs`, ...) is keyed on the smallest input it needs. Cover new models in the equality tests.
- **Determinism:** sort the collected mocks by ordinal name; mock names are stable and get suffixes on clashes.
- **No diagnostics from the generator:** it silently skips what it cannot mock; `MockabilityAnalyzer` reports it. The analyzer duplicates the generator's rules (`Helpers.HasAccessibleSignature`, `Helpers.IsOverridableFrom`, `Class.cs`); change both together.
- **Emitted code:**
  - every file starts with the `<auto-generated>` header from `Sources.InitializeBuilder()`; per-mock files use `#nullable enable annotations` and `namespace Mockolate;`, shared files emit into the namespace of the library types they extend (`Mockolate.Setup`, `Mockolate.Interactions`, `Mockolate.Verify`);
  - write every type as `global::...` through `Helpers.TypeDisplayFormat` and escape keywords with `@`;
  - avoid clashes with user members through `GetUniqueName`, `CreateUniquePropertyName`, `GetUniqueLocalVariableName` and `CreateUniqueParameterName` instead of fixed names (the `Mock` property becomes `Mockolate_Mock` when the type already has a `Mock` member);
  - mock types get `[EditorBrowsable(Never)]` and `[ExcludeFromCodeCoverage]`, and `[DebuggerNonUserCode]` in a Release build of the generator;
  - write with `StringBuilder` and literal tab indentation, like the surrounding emitter code.
- **Consumer differences:** decide target-framework features with `#if` in the emitted code, and optional types by looking them up in the compilation (`OverloadResolutionPriorityAttribute` via `GetTypeByMetadataName`).

## Analyzers and Code Fixes

- Rule IDs are `MockolateNNNN` in the category `Usage`. Add the descriptor through `Rules.CreateDescriptor` (only `Mockolate0003` lives in `MockabilityAnalyzer`, see the comment in `Rules.cs`), the `Title`, `MessageFormat` and `Description` texts in `Resources.resx`, the row in `AnalyzerReleases.Unshipped.md` and a section with an example in `Docs/pages/10-analyzers.md`.
- Analyzers call `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)` and `EnableConcurrentExecution()`, and tolerate unresolved symbols in code that does not compile yet.
- Code fixes are `[ExportCodeFixProvider]` and `[Shared]`, support `BatchFixer` and use the resource name as equivalence key.

## Tests

- **Structure:** `public sealed partial class <Feature>Tests` per feature folder (`MockMethods`, `MockProperties`, `Verify`, `Web`, ...), with nested `public sealed class <Aspect>Tests` in `<Feature>Tests.<Aspect>Tests.cs`.
- **Names:** prefer `<Subject>_Should<Outcome>` or `When<Condition>_Should<Outcome>`, the form of most tests; follow the neighbouring tests where a class uses another form.
- **Body:** `public async Task` with arrange, act and assert separated by blank lines, `[Arguments(...)]` for data, and the chocolate-themed types in `TestHelpers` (`IChocolateDispenser`, ...) or a small interface next to the test. The mock is called `sut`.
- **Assertions:** aweXpect: `await That(result).IsEqualTo(42);`, a `.Because(...)` reason instead of a comment, `.IgnoringNewlineStyle()` for multi-line strings. Assert verifications through aweXpect.Mockolate: `await That(sut.Mock.Verify.Dispense(...)).Once();`.
- **Exceptions:** a local `void Act()`, then `await That(Act).Throws<MockVerificationException>().WithMessage("...")` with the complete message; `.AsWildcard()` only for parts that vary.
- **Frameworks:** guard tests of framework-specific features with the same `#if` symbols as the library; `Mockolate.Tests` has no `IsExternalInit` polyfill.
- **Generator tests:** compile a raw string with `Generator.Run("""...""")` and assert on `result.Sources["Mock.<Name>.g.cs"]`. Put special cases that should compile for real into `Tests/Mockolate.Tests/GeneratorCoverage`; they also feed the snapshots.
- **Analyzer tests:** `await Verifier.VerifyAnalyzerAsync("""...""")` with `{|#0:...|}` markup and `Verifier.Diagnostic(...)`, and `VerifyCodeFixAsync` for fixes.

## Documentation

The pages follow the [docs style guide](https://github.com/Testably/Testably.Site/blob/main/Docs/style-guide.md) (see [AGENTS.md](../../AGENTS.md#documentation)). In addition:

- Files and folders in `Docs/pages/` carry unique two-digit prefixes (`04-create-mocks.md`, `05-setup/`). A folder has a `_category_.json` with its `label` only (no `position`, which the prefix replaces, and no `collapsed`) and a `00-index.md` overview page, which the docs site turns into the category's own page. Pages that use tabs are `.mdx`.
- The pages are ordered from introduction to reference: getting started, core concepts (with the complete example and the cheat sheet), creating mocks, setup, parameter matching, verification, advanced features, special types and analyzers, then migration and benchmarks. Parameter matching is documented once, in `06-parameter-matching.md`, and linked from elsewhere.
- The automated migration pages live in the Mockolate.Migration repository and are copied into the migration section by the docs site. Link to them by site path (`/Mockolate/migration/automated/`).
- Examples use the `IChocolateDispenser` from `02-core-concepts/00-index.md`. Unlike in the tests, the mock is named after its role (`IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();`) and `sut` is reserved for the class under test. Further members are declared next to the example or in an "Example types used on this page" `<details>` block.
- Edge cases and implementation background go into a collapsed `<details>` block, so the common path stays short. Code that must not compile is shown as a `// ❌` comment, not as code.
- Link to other pages by relative file path including the number prefix (`../06-parameter-matching.md#tolerance`), so the docs site resolves and checks the link. This includes the `00-index` overview pages: the docs site renames them to `index` and rewrites the links to them.
- Update the README only for the landing page content (features, getting started); details belong on the docs site.
