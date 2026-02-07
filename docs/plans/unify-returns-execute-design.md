# Unify Method API: Returns + Execute Design

**Date:** 2026-02-06
**Related Todo:** [Unify Configuration API: Returns + Execute, Drop OnCall](../todos/unify-returns-execute-api.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-02-06

---

## Overview

Replace the current three-method API (`Returns`, `OnCall`, `When`) for methods with a clean two-method API where the method's return type determines which configuration method is available:

- **Non-void methods** use `.Returns()` only (value, callback, and simplified async callback overloads)
- **Void methods** use `.Execute()` only (callback)
- **Drop `.OnCall()` entirely** from method interceptors
- **Rename sequence chaining**: `ThenCall` becomes `ThenReturns` (non-void) / `ThenExecute` (void)
- **Add simplified async callbacks to `ThenReturns`** (subsumes `sequence-callback-simplification` todo)
- **Rename void When chain methods**: `Call()` becomes `Execute()`, `ThenCall()` becomes `ThenExecute()`

Properties, indexers, and events are **explicitly out of scope** -- `Get`, `Set`, sequence chaining via `Get().ThenGet()`/`Set().ThenSet()` stay as-is.

---

## Approach

1. **Phase 1: Interface Redesign** -- Create new void-specific builder/sequence interfaces in `src/KnockOff/`. Rename existing interfaces.
2. **Phase 2: Generator Changes** -- Modify `MethodInterceptorRenderer.cs` to rename `OnCall` to `Returns`/`Execute`, rename `ThenCall` to `ThenReturns`/`ThenExecute`, add simplified async `ThenReturns` overloads. The void When chain renames are also in `MethodInterceptorRenderer.cs` (private methods `RenderVoidWhenChainImpl()` and `RenderVoidWhenEntryPoints()`).
3. **Phase 3: Test Updates** -- Update all test files (fresh agent).
4. **Phase 4: Design Project Updates** -- Update Design.Stubs and Design.Tests (fresh agent).
5. **Phase 5: Documentation Updates** -- Update skill files, docs, MarkdownSnippet samples (fresh docs agent).
6. **Phase 6: Cleanup** -- Version bump, move subsumed todo to completed.

---

## Design

### API Before/After Reference

#### Non-Void Methods (e.g., `int Add(int a, int b)`)

| Scenario | Before | After |
|----------|--------|-------|
| Constant value | `stub.Add.Returns(42)` | `stub.Add.Returns(42)` (unchanged) |
| Value sequence | `stub.Add.Returns(1, 2, 3)` | `stub.Add.Returns(1, 2, 3)` (unchanged) |
| Dynamic callback | `stub.Add.OnCall((a, b) => a + b)` | `stub.Add.Returns((a, b) => a + b)` |
| Sequence (callbacks) | `stub.Add.OnCall(cb1).ThenCall(cb2)` | `stub.Add.Returns(cb1).ThenReturns(cb2)` |
| Sequence (values) | `stub.Add.OnCall(cb1).ThenReturns(42)` | `stub.Add.Returns(cb1).ThenReturns(42)` |
| Sequence (params) | `stub.Add.OnCall(cb).ThenReturns(1, 2)` | `stub.Add.Returns(cb).ThenReturns(1, 2)` |
| ThenDefault | `...ThenCall(cb).ThenDefault()` | `...ThenReturns(cb).ThenDefault()` |
| When chain | `stub.Add.When(1,2).Returns(100)` | `stub.Add.When(1,2).Returns(100)` (unchanged) |
| When terminal | `...Returns(100).ThenCall(cb)` | `...Returns(100).ThenCall(cb)` (unchanged) |

#### Async Non-Void Methods (e.g., `Task<string> GetDataAsync(int id)`)

| Scenario | Before | After |
|----------|--------|-------|
| Value (auto-wrap) | `stub.GetDataAsync.Returns("val")` | `stub.GetDataAsync.Returns("val")` (unchanged) |
| Simplified callback | `stub.GetDataAsync.OnCall((id) => "val")` | `stub.GetDataAsync.Returns((id) => "val")` |
| Full delegate | `stub.GetDataAsync.OnCall((id) => Task.FromResult("val"))` | `stub.GetDataAsync.Returns((id) => Task.FromResult("val"))` |
| Sequence (simplified ThenReturns) | Not supported | `stub.GetDataAsync.Returns((id) => "v1").ThenReturns((id) => "v2")` **NEW** |

#### Void Methods (e.g., `void Reset()`)

| Scenario | Before | After |
|----------|--------|-------|
| Simple callback | `stub.Reset.OnCall(() => count++)` | `stub.Reset.Execute(() => count++)` |
| Sequence | `stub.Reset.OnCall(cb1).ThenCall(cb2)` | `stub.Reset.Execute(cb1).ThenExecute(cb2)` |
| When match action | `stub.Process.When(1,2).Call(cb)` | `stub.Process.When(1,2).Execute(cb)` |
| When chain action | `...Call(cb).ThenWhen(3,4).Call(cb2)` | `...Execute(cb).ThenWhen(3,4).Execute(cb2)` |
| When terminal | `...Call(cb).ThenCall(cb2)` | `...Execute(cb).ThenExecute(cb2)` |

#### Void Async Methods (e.g., `Task SaveDataAsync(string data)`)

| Scenario | Before | After |
|----------|--------|-------|
| Simplified callback | `stub.SaveDataAsync.OnCall((d) => saved = d)` | `stub.SaveDataAsync.Execute((d) => saved = d)` |

### Interface Redesign

#### Current Interface Hierarchy (methods only)

```
ITracking
  IMethodTracking : ITracking
    IMethodTracking<TArg> : IMethodTracking
    IMethodTrackingArgs<TArgs> : IMethodTracking
    IMethodCallBuilder<TCallback> : IMethodTracking        (has ThenCall)
    IMethodCallBuilder<TCallback, TArg> : IMethodTracking<TArg>
    IMethodCallBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>

IMethodSequence
  IMethodSequence<TCallback> : IMethodSequence             (has ThenCall)
```

#### New Interface Hierarchy

**Non-void builder interfaces** -- rename `ThenCall` to `ThenReturns`:

```csharp
// File: src/KnockOff/IMethodReturnsBuilder.cs (NEW FILE)
public interface IMethodReturnsBuilder<TCallback> : IMethodTracking
{
    IMethodReturnsSequence<TCallback> ThenReturns(TCallback callback);
    new IMethodReturnsBuilder<TCallback> Verifiable();
    new IMethodReturnsBuilder<TCallback> Verifiable(Times times);
}

public interface IMethodReturnsBuilder<TCallback, TArg> : IMethodTracking<TArg>
{
    IMethodReturnsSequence<TCallback> ThenReturns(TCallback callback);
    new IMethodReturnsBuilder<TCallback, TArg> Verifiable();
    new IMethodReturnsBuilder<TCallback, TArg> Verifiable(Times times);
}

public interface IMethodReturnsBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>
{
    IMethodReturnsSequence<TCallback> ThenReturns(TCallback callback);
    new IMethodReturnsBuilderArgs<TCallback, TArgs> Verifiable();
    new IMethodReturnsBuilderArgs<TCallback, TArgs> Verifiable(Times times);
}
```

**Void builder interfaces** -- new, with `ThenExecute`:

```csharp
// File: src/KnockOff/IMethodExecuteBuilder.cs (NEW FILE)
public interface IMethodExecuteBuilder<TCallback> : IMethodTracking
{
    IMethodExecuteSequence<TCallback> ThenExecute(TCallback callback);
    new IMethodExecuteBuilder<TCallback> Verifiable();
    new IMethodExecuteBuilder<TCallback> Verifiable(Times times);
}

public interface IMethodExecuteBuilder<TCallback, TArg> : IMethodTracking<TArg>
{
    IMethodExecuteSequence<TCallback> ThenExecute(TCallback callback);
    new IMethodExecuteBuilder<TCallback, TArg> Verifiable();
    new IMethodExecuteBuilder<TCallback, TArg> Verifiable(Times times);
}

public interface IMethodExecuteBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>
{
    IMethodExecuteSequence<TCallback> ThenExecute(TCallback callback);
    new IMethodExecuteBuilderArgs<TCallback, TArgs> Verifiable();
    new IMethodExecuteBuilderArgs<TCallback, TArgs> Verifiable(Times times);
}
```

**Non-void sequence interface** -- rename `ThenCall` to `ThenReturns`:

```csharp
// File: src/KnockOff/IMethodReturnsSequence.cs (NEW FILE)
public interface IMethodReturnsSequence : IMethodSequence
{
    // Inherits Verify(), Reset(), ThenDefault() from IMethodSequence
}

public interface IMethodReturnsSequence<TCallback> : IMethodReturnsSequence
{
    IMethodReturnsSequence<TCallback> ThenReturns(TCallback callback);
    new IMethodReturnsSequence<TCallback> Verifiable();
}
```

**Void sequence interface**:

```csharp
// File: src/KnockOff/IMethodExecuteSequence.cs (NEW FILE)
public interface IMethodExecuteSequence : IMethodSequence
{
    // Inherits Verify(), Reset(), ThenDefault() from IMethodSequence
}

public interface IMethodExecuteSequence<TCallback> : IMethodExecuteSequence
{
    IMethodExecuteSequence<TCallback> ThenExecute(TCallback callback);
    new IMethodExecuteSequence<TCallback> Verifiable();
}
```

**Void When chain interface** -- rename `Call` to `Execute`, `ThenCall` to `ThenExecute`:

```csharp
// Update existing file: src/KnockOff/IWhenTracking.cs
public interface IVoidWhenChain<TDelegate> : IWhenTracking
{
    IVoidWhenChain<TDelegate> Execute(TDelegate callback);   // was Call()
    IWhenTracking ThenExecute(TDelegate callback);            // was ThenCall()
    IWhenTracking ThenNone();
    void Verify(Times times);
    new IVoidWhenChain<TDelegate> Verifiable();
}
```

**Non-void When chain** -- `ThenCall` keeps its name per user decision:

```csharp
// IWhenChain<TDelegate, TReturn> stays as-is (ThenCall unchanged)
```

#### Old Interfaces to Remove

After the new interfaces are in place and the generator references them:

- `IMethodCallBuilder.cs` -- delete entire file (3 interfaces with `ThenCall`)
- Remove `ThenCall` from `IMethodSequence<TCallback>` (or delete and replace with `IMethodReturnsSequence`/`IMethodExecuteSequence`)

### Generator Changes

#### MethodInterceptorRenderer.cs

The primary changes in `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`:

1. **Rename `OnCall()` entry point**:
   - Non-void methods: `OnCall(delegateType callback)` becomes `Returns(delegateType callback)`
   - Void methods: `OnCall(delegateType callback)` becomes `Execute(delegateType callback)`
   - Simplified async `OnCall(Func<..., T>)` becomes `Returns(Func<..., T>)`
   - Simplified void async `OnCall(Action<...>)` becomes `Execute(Action<...>)`

2. **Rename `MethodCallBuilderImpl` nested class**:
   - Non-void: `ThenCall()` becomes `ThenReturns()` on the concrete class
   - Void: `ThenCall()` becomes `ThenExecute()` on the concrete class
   - Interface implementation references update accordingly

3. **Rename `MethodSequenceImpl` nested class**:
   - Non-void: `ThenCall()` becomes `ThenReturns()` on the concrete class
   - Void: `ThenCall()` becomes `ThenExecute()` on the concrete class

4. **Add simplified async `ThenReturns` overloads** (subsumes sequence-callback-simplification):
   - On `MethodCallBuilderImpl` for non-void async methods: add `ThenReturns(Func<..., T> callback)` that wraps in Task.FromResult/new ValueTask
   - On `MethodSequenceImpl` for non-void async methods: same pattern

5. **Update `Returns(value)` entry point** -- already returns `MethodCallBuilderImpl`, no method name change needed. But the builder's `ThenCall` becomes `ThenReturns`.

6. **Update `Returns(first, params rest)` entry point** -- internal calls to `ThenReturns` stay the same name (already called `ThenReturns`).

7. **Update builder interface references** -- `model.BuilderInterface` in `UnifiedInterceptorBuilder.GetBuilderInterface()` must return the correct interface name based on `isVoid`.

#### Void When Chain Methods (inside MethodInterceptorRenderer.cs)

**Note:** `WhenChainRenderer.cs` exists in the codebase but is dead code -- it has zero references from any renderer. All When chain rendering is done by private methods inside `MethodInterceptorRenderer.cs`. The following changes target those private methods:

1. **`RenderVoidWhenChainImpl()`** (line ~2406): Rename generated `Call()` to `Execute()`, `ThenCall()` to `ThenExecute()` on the `VoidWhenChainImpl` nested class
2. **`RenderVoidWhenEntryPoints()`** (line ~2233): Update the entry point method names if they reference `Call`
3. **`RenderWhenChainImpl()`** (line ~1978): Non-void When chain `ThenCall()` stays as-is (user decision)
4. **`RenderWhenEntryPoints()`** (line ~2109): Non-void When entry points stay as-is

#### UnifiedInterceptorBuilder.cs / ModelAdapters.cs

Update `GetBuilderInterface()` to return:
- Non-void: `IMethodReturnsBuilder<...>` variants
- Void: `IMethodExecuteBuilder<...>` variants

### Pipeline Analysis

All nine patterns use the shared `MethodInterceptorRenderer` for method interceptor generation:

| Pattern | Transform | Builder | Renderer | Affected? |
|---------|-----------|---------|----------|-----------|
| 1. Standalone | `TransformClass` | `FlatModelBuilder` | `FlatRenderer` -> `MethodInterceptorRenderer` | Yes |
| 2. Generic Standalone | `TransformClass` | `FlatModelBuilder` | `FlatRenderer` -> `MethodInterceptorRenderer` | Yes |
| 3. Standalone Class | `TransformStandaloneClass` | `StandaloneClassModelBuilder` | `StandaloneClassRenderer` -> `MethodInterceptorRenderer` | Yes |
| 4. Generic Standalone Class | `TransformStandaloneClass` | `StandaloneClassModelBuilder` | `StandaloneClassRenderer` -> `MethodInterceptorRenderer` | Yes |
| 5. Inline Interface | `TransformInlineStubClass` | `InlineModelBuilder` | `InlineRenderer` -> `MethodInterceptorRenderer` | Yes |
| 6. Inline Class | `TransformInlineStubClass` | `InlineModelBuilder` | `InlineRenderer` -> `MethodInterceptorRenderer` | Yes |
| 7. Inline Delegate | `TransformInlineStubClass` | `InlineModelBuilder` | `InlineRenderer` -> `MethodInterceptorRenderer` | Yes |
| 8. Open Generic Interface | Various | Various | `InlineRenderer` -> `MethodInterceptorRenderer` | Yes |
| 9. Open Generic Class | Various | Various | `InlineRenderer` -> `MethodInterceptorRenderer` | Yes |

All patterns share `MethodInterceptorRenderer`. A change there propagates to all nine patterns automatically.

When chains are also rendered inside `MethodInterceptorRenderer` (private methods), not in `WhenChainRenderer.cs` (which is dead code).

---

## Implementation Steps

### Phase 1: Interface Redesign (src/KnockOff/)

1. Create `IMethodReturnsBuilder.cs` with 3 interfaces
2. Create `IMethodExecuteBuilder.cs` with 3 interfaces
3. Create `IMethodReturnsSequence.cs` with 2 interfaces
4. Create `IMethodExecuteSequence.cs` with 2 interfaces
5. Update `IWhenTracking.cs`: rename `Call` to `Execute` and `ThenCall` to `ThenExecute` on `IVoidWhenChain<T>`. Remove `#pragma warning disable CA1716` / `#pragma warning restore CA1716` around the old `Call()` method (line 83-85) -- `Execute` is not a C# keyword and does not trigger CA1716.
6. Delete old `IMethodCallBuilder.cs`
7. Update `IMethodSequence.cs`: remove `ThenCall` from `IMethodSequence<TCallback>` (or delete file if replaced)

**Checkpoint 1:** `dotnet build src/KnockOff/KnockOff.csproj` passes (interfaces are additive until generator references them)

### Phase 2: Generator Changes

**Phase 2a: Update builder interface selection**

1. `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- update `GetBuilderInterface()` to accept `isVoid` and return `IMethodReturnsBuilder` or `IMethodExecuteBuilder`
2. `src/Generator/Renderer/Shared/ModelAdapters.cs` -- same update for `GetBuilderInterface()`
3. `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` and `MethodOverloadSignature.cs` -- ensure `IsVoid` is available where `BuilderInterface` is computed

**Phase 2b: MethodInterceptorRenderer.cs -- rename entry points**

1. Rename `OnCall(delegateType callback)` to:
   - `Returns(delegateType callback)` when `!model.IsVoid`
   - `Execute(delegateType callback)` when `model.IsVoid`
2. Rename simplified async `OnCall(Func<..., T>)` to `Returns(Func<..., T>)`
3. Rename simplified void async `OnCall(Action<...>)` to `Execute(Action<...>)`
4. Update all internal field name comments (cosmetic)
5. **Internal self-call pattern**: The generated `Returns(T first, params T[] rest)` method (line ~234) internally calls `OnCall(discardPrefix => first)` to set up the first sequence element (line ~241-249). After rename, this becomes `Returns(discardPrefix => first)` -- one `Returns` overload calling another. This is valid C# overload resolution: `Returns(T first, params T[] rest)` calls `Returns(TCallback callback)` where `TCallback` is a delegate type, which is unambiguous. No special handling needed beyond the mechanical rename of `OnCall` to `Returns`.

**Phase 2c: MethodInterceptorRenderer.cs -- rename builder/sequence classes**

1. In `RenderMethodCallBuilderImpl()`:
   - Non-void: rename `ThenCall()` to `ThenReturns()` on the concrete class
   - Void: rename `ThenCall()` to `ThenExecute()` on the concrete class
   - Update explicit interface implementations to reference new interface names
   - Add simplified async `ThenReturns(Func<..., T>)` overloads for non-void async methods
2. In `RenderMethodSequenceImpl()`:
   - Non-void: rename `ThenCall()` to `ThenReturns()` on the concrete class
   - Void: rename `ThenCall()` to `ThenExecute()` on the concrete class
   - Update explicit interface implementations
   - Add simplified async `ThenReturns(Func<..., T>)` overloads for non-void async methods
3. **Internal self-call pattern**: The existing `ThenReturns(T value)` methods on both `MethodCallBuilderImpl` (line ~1577-1587) and `MethodSequenceImpl` (line ~1732-1742) delegate to `ThenCall(callback)` internally. For example: `ThenReturns(string value) => ThenCall((_) => value)`. After rename, `ThenCall` becomes `ThenReturns`, making this: `ThenReturns(string value) => ThenReturns((_) => value)`. This is one `ThenReturns` overload calling another -- `ThenReturns(T value)` calling `ThenReturns(TCallback callback)`. These have distinct parameter types (`T` vs `Func<..., T>`) so C# overload resolution handles this correctly. No special handling needed beyond the mechanical rename.

**Phase 2d: MethodInterceptorRenderer.cs -- void When chain renames**

All void When chain rendering is done by private methods inside `MethodInterceptorRenderer.cs` (NOT `WhenChainRenderer.cs`, which is dead code with zero references):

1. In `RenderVoidWhenChainImpl()` (line ~2406): rename generated `Call()` to `Execute()`, `ThenCall()` to `ThenExecute()` on the `VoidWhenChainImpl` nested class
2. In `RenderVoidWhenEntryPoints()` (line ~2233): update entry point generation if it references `Call`
3. Update explicit interface implementations for `IVoidWhenChain<T>`
4. **CA1716 suppression**: The existing `#pragma warning disable CA1716` on `IVoidWhenChain<TDelegate>.Call()` in `IWhenTracking.cs` (line 83) exists because `Call` is close to a language keyword. After renaming to `Execute()`, this suppression can be removed -- `Execute` is not a C# keyword and does not match any keyword in the CA1716 rule set.

**Phase 2e: Update backward-compatible tracking properties and aggregate fields**

1. Update any comments referencing `OnCall` in the renderer
2. Verify `_onCall` field names stay as-is (internal, not user-facing)

**Checkpoint 2:** `dotnet build src/KnockOff.sln` passes. Generated code compiles with new API names.

### Phase 3: Test Updates (Fresh Agent)

Update all test files that use `OnCall`/`ThenCall` on method interceptors:

**KnockOffTests (46 files, ~511 OnCall + ~72 ThenCall occurrences):**

| File | OnCall count | ThenCall count |
|------|-------------|----------------|
| `VerificationTests.cs` | 46 | 3 |
| `AsyncCallbackSimplificationTests.cs` | 37 | 2 |
| `BclStandaloneTests.cs` | 37 | 0 |
| `ParamTypeSuffixTests.cs` | 36 | 0 |
| `OverloadGroupAsyncCallbackTests.cs` | 33 | 0 |
| `OverloadedMethodTests.cs` | 23 | 0 |
| `SequencingTests.cs` | 18 | 11 |
| `NeatooTests.cs` | 18 | 0 |
| `InlineStubTests.cs` | 16 | 0 |
| `GenericMethodBugTests.cs` | 15 | 0 |
| `GenericStandaloneEdgeCaseTests.cs` | 13 | 0 |
| `GenericMethodTests.cs` | 12 | 0 |
| `BuilderElevationTests.cs` | 11 | 13 |
| `SequenceValueOverloadTests.cs` | 21 | 11 |
| `WhenChainTests.cs` | 9 | 24 |
| `StandaloneClassStubTests.cs` | 11 | 2 |
| `StandaloneClassUserMethodTests.cs` | 9 | 2 |
| All others | ~146 combined | ~4 |

**Transformation rules for tests:**
- Non-void method `.OnCall(cb)` -> `.Returns(cb)`
- Void method `.OnCall(cb)` -> `.Execute(cb)`
- Non-void `.ThenCall(cb)` (sequence context) -> `.ThenReturns(cb)`
- Void `.ThenCall(cb)` (sequence context) -> `.ThenExecute(cb)`
- Void When `.Call(cb)` -> `.Execute(cb)`
- Void When `.ThenCall(cb)` -> `.ThenExecute(cb)`
- Non-void When `.ThenCall(cb)` -> stays as `.ThenCall(cb)`

**Checkpoint 3:** `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj` -- all tests pass

### Phase 4: Design Project Updates (Fresh Agent)

**Design.Stubs (10 files, ~90 OnCall occurrences):**

| File | Changes needed |
|------|---------------|
| `Methods/BasicMethods.cs` | 9 OnCall -> Returns/Execute, update design comments |
| `Methods/MethodSequences.cs` | 16 OnCall + 16 ThenCall -> Returns/ThenReturns + Execute/ThenExecute |
| `Methods/WhenMatching.cs` | 3 OnCall -> Returns, void When Call -> Execute |
| `Methods/MethodOverloads.cs` | 22 OnCall -> Returns/Execute |
| `Methods/AsyncConsistency.cs` | 10 OnCall -> Returns |
| `StubPatterns/AllPatterns.cs` | 13 OnCall + 8 Call -> Returns/Execute |
| `Advanced/Verification.cs` | 1 OnCall + 3 ThenCall -> Returns/Execute + ThenReturns/ThenExecute |
| `Advanced/SourceDelegation.cs` | 2 OnCall -> Returns/Execute |
| `Advanced/DelegateStubs.cs` | 4 OnCall + 1 ThenCall -> Returns/Execute + ThenReturns |
| `UserMethods/UserMethodBasics.cs` | 10 OnCall -> Returns/Execute |

**Design.Tests (16 files, ~145 OnCall + ~44 ThenCall occurrences):**

All test files in Design.Tests that reference OnCall/ThenCall for methods need the same transformation.

**Checkpoint 4:** `dotnet build src/Design/Design.Stubs` and `dotnet test src/Design/Design.Tests` pass

### Phase 5: Documentation Updates (Fresh Docs Agent)

#### Skill Files (PRIMARY DELIVERABLE)

| File | Impact | Description |
|------|--------|-------------|
| `skills/knockoff/SKILL.md` | Heavy | ~50+ lines referencing OnCall. Core examples, quick reference tables, pattern examples. Complete rewrite of method sections. |
| `skills/knockoff/references/methods.md` | Heavy | ~50+ lines. Primary method configuration reference. Entire file focused on OnCall/Returns for methods. |
| `skills/knockoff/references/api-reference.md` | Heavy | ~30+ lines. API tables, interceptor types, delegate signatures all reference OnCall. |
| `skills/knockoff/references/patterns.md` | Moderate | ~10+ lines. Pattern examples use OnCall for method configuration. |
| `skills/knockoff/references/moq-migration.md` | Light | ~5 lines. Migration table maps Moq `.Callback()` to OnCall. |
| `skills/knockoff/references/properties.md` | None | Properties are out of scope. |
| `skills/knockoff/references/strict-mode.md` | Light | May reference OnCall in examples. |

#### MarkdownSnippet Sample Files (30 files, ~383 OnCall occurrences)

Every sample file in `src/Tests/KnockOff.Documentation.Samples/` that uses `.OnCall()` or `.ThenCall()` on method interceptors must be updated. After updating, run `dotnet mdsnippets` to sync markdown.

Key files with highest impact:
- `MethodsSamples.cs` (28 occurrences)
- `NSubstituteMigrationSamples.cs` (31 occurrences)
- `DelegatesSamples.cs` (29 occurrences)
- `PatternsSamples.cs` (25 occurrences)
- `MoqMigrationSamples.cs` (25 occurrences)
- `InterceptorApiSamples.cs` (24 occurrences)
- `SkillContentSamples.cs` (23 occurrences)
- `SkillPatternsSamples.cs` (23 occurrences)
- `VerificationSamples.cs` (21 occurrences)
- `TroubleshootingSamples.cs` (22 occurrences)

#### Documentation Guides

| File | Impact |
|------|--------|
| `docs/guides/methods.md` | Heavy -- uses MarkdownSnippets, method-specific guide |
| `docs/guides/advanced-callbacks.md` | Heavy -- callback pattern guide |
| `docs/guides/async-patterns.md` | Moderate -- async OnCall patterns |
| `docs/guides/verification.md` | Moderate -- verification examples |
| `docs/guides/delegates.md` | Moderate -- delegate stub examples |
| `docs/guides/generic-methods.md` | Moderate |
| `docs/guides/parameter-matching.md` | Moderate -- When chain examples |
| `docs/guides/user-methods.md` | Moderate -- user method overrides |
| `docs/guides/api-consistency-matrix.md` | Moderate |
| `docs/guides/source-delegation.md` | Light |
| `docs/guides/stub-patterns.md` | Heavy -- all 9 patterns show OnCall |
| `docs/guides/strict-mode.md` | Light |
| `docs/reference/interceptor-api.md` | Moderate |
| `docs/reference/smart-defaults.md` | Light |
| `docs/getting-started.md` | Moderate -- first examples |
| `docs/troubleshooting.md` | Moderate |
| `docs/type-safety.md` | Light |
| `docs/comparison.md` | Light |
| `docs/migration/from-moq.md` | Moderate |
| `docs/migration/from-nsubstitute.md` | Light |

#### Migration Guide (NEW)

Create `docs/migration/oncall-to-returns-execute.md` with:
- Before/after tables
- Search-and-replace patterns
- Edge cases (async callbacks, When chains)

**Checkpoint 5:** `dotnet mdsnippets` succeeds, all docs synced.

### Phase 6: Cleanup

1. Bump version in `Directory.Build.props`
2. Create release notes in `docs/release-notes/`
3. Move `docs/todos/sequence-callback-simplification.md` to `docs/todos/completed/` (subsumed)
4. Update `sequence-callback-simplification.md` status to "Complete" with note: "Subsumed by unify-returns-execute-api"
5. Move `docs/plans/simplify-oncall-sequence-api-design.md` to `docs/plans/completed/` and update its status to "Superseded by unify-returns-execute-design"
6. Optionally delete `src/Generator/Renderer/Shared/WhenChainRenderer.cs` (dead code with zero references -- see Concern 1 in Developer Review)

**Checkpoint 6:** Full solution builds and tests pass: `dotnet test src/KnockOff.sln`

---

## Acceptance Criteria

- [ ] `.OnCall()` does not exist on any generated method interceptor
- [ ] Non-void methods expose `.Returns(value)`, `.Returns(callback)`, `.Returns(simplifiedCallback)` only
- [ ] Void methods expose `.Execute(callback)`, `.Execute(simplifiedCallback)` only
- [ ] Non-void sequence chaining uses `.ThenReturns(callback)` and `.ThenReturns(value)`
- [ ] Void sequence chaining uses `.ThenExecute(callback)`
- [ ] Simplified async `ThenReturns(Func<..., T>)` works for Task<T>/ValueTask<T> methods
- [ ] Void When chains use `.Execute()` instead of `.Call()` and `.ThenExecute()` instead of `.ThenCall()`
- [ ] Non-void When chain `.ThenCall()` is unchanged
- [ ] All nine patterns generate correct API
- [ ] All existing tests pass (with updated API calls)
- [ ] Design.Stubs compiles
- [ ] Design.Tests pass
- [ ] Skill files updated
- [ ] Documentation guides updated
- [ ] MarkdownSnippets synced
- [ ] Version bumped
- [ ] `sequence-callback-simplification` todo marked complete

---

## Dependencies

- None -- this is an API breaking change on pre-1.0 software

### Superseded Plans

This work supersedes `docs/plans/simplify-oncall-sequence-api-design.md` (status: "Ready for Implementation", dated 2026-01-29). That plan defined `IMethodCallBuilder` interfaces with `ThenCall` chaining from `OnCall`. This plan replaces those interfaces with `IMethodReturnsBuilder`/`IMethodExecuteBuilder` using `ThenReturns`/`ThenExecute` chaining from `Returns`/`Execute`. The `IMethodCallBuilder` interfaces created by that plan will be deleted in Phase 1.

The related todo `docs/todos/completed/simplify-oncall-sequence-api.md` is already in the `completed/` directory. Its plan should be moved to `docs/plans/completed/` during Phase 6 cleanup with a status note: "Superseded by unify-returns-execute-design".

---

## Risks / Considerations

### Breaking Change Scope

This renames a method used 511 times in KnockOffTests, 145 times in Design.Tests, 383 times in doc samples, and 90 times in Design.Stubs. Every external consumer of KnockOff will need to update.

**Mitigation:** Pre-1.0 software, breaking changes expected. Create migration guide. The rename is mechanical: `OnCall` -> `Returns`/`Execute`.

### Overload Resolution for Returns(value) vs Returns(callback)

For non-void methods, `Returns` must now accept both values and callbacks. For `int` return methods:
- `Returns(42)` -- value (int)
- `Returns((a, b) => a + b)` -- callback (Func<int, int, int>)

These are distinct types so C# overload resolution handles this naturally. No ambiguity.

For lambda expressions without explicit types, C# can infer the target type from the method signature. This works because the generated `Returns` methods have distinct parameter types (T value vs TDelegate callback).

### ThenReturns Naming Collision

`ThenReturns(value)` already exists alongside `ThenCall(callback)`. After rename, both become `ThenReturns`:
- `ThenReturns(T value)` -- existing value overload
- `ThenReturns(TCallback callback)` -- renamed from ThenCall

These have different types (`T` vs `TCallback` which is `Func<..., T>`), so overload resolution works. No collision.

### Sequence-Callback-Simplification Subsumption

Adding simplified async `ThenReturns(Func<..., T>)` is a natural extension during the rename work. The sequence-callback-simplification todo is fully subsumed.

---

## Architectural Verification

### Scope Table

| Pattern | Methods Affected? | Properties Affected? | Indexers Affected? | Events Affected? |
|---------|-------------------|---------------------|--------------------|------------------|
| 1. Standalone | Yes | No | No | No |
| 2. Generic Standalone | Yes | No | No | No |
| 3. Standalone Class | Yes | No | No | No |
| 4. Generic Standalone Class | Yes | No | No | No |
| 5. Inline Interface | Yes | No | No | No |
| 6. Inline Class | Yes | No | No | No |
| 7. Inline Delegate | Yes | No | No | No |
| 8. Open Generic Interface | Yes | No | No | No |
| 9. Open Generic Class | Yes | No | No | No |

### Pipeline Verification

All nine patterns route method interceptor generation through `MethodInterceptorRenderer.RenderInterceptorClass()`. Verified by tracing:

- `FlatRenderer.cs` calls `MethodInterceptorRenderer.RenderInterceptorClass()`
- `StandaloneClassRenderer.cs` calls `MethodInterceptorRenderer.RenderInterceptorClass()`
- `InlineRenderer.cs` calls `MethodInterceptorRenderer.RenderInterceptorClass()`
- `ClassRenderer.cs` calls `MethodInterceptorRenderer.RenderInterceptorClass()`

When chains are rendered by private methods inside `MethodInterceptorRenderer` itself (`RenderWhenChainImpl`, `RenderVoidWhenChainImpl`, `RenderWhenEntryPoints`, `RenderVoidWhenEntryPoints`). Note: `WhenChainRenderer.cs` exists in the codebase but is dead code with zero references from any renderer.

A single change to `MethodInterceptorRenderer` propagates to all nine patterns.

### Breaking Changes

**Yes -- this is a breaking change.**

- `.OnCall()` removed from all method interceptors
- `.ThenCall()` renamed on builder and sequence classes
- `IVoidWhenChain.Call()` renamed to `Execute()`
- `IVoidWhenChain.ThenCall()` renamed to `ThenExecute()`
- Old `IMethodCallBuilder` interfaces deleted
- Old `IMethodSequence<T>.ThenCall()` removed

**Migration path:** Mechanical rename. See migration guide section.

### Design Project Verification

Deferred to Phase 4 implementation. The Design projects will be updated after the generator changes, and compilation will be verified at Checkpoint 4.

### Codebase Deep-Dive (Files Examined)

**Generator files:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` (~2500 lines) -- primary target for `OnCall`/`ThenCall` generation AND void/non-void When chain generation (private methods: `RenderVoidWhenChainImpl()` at ~line 2406, `RenderVoidWhenEntryPoints()` at ~line 2233, `RenderWhenChainImpl()` at ~line 1978, `RenderWhenEntryPoints()` at ~line 2109)
- `src/Generator/Renderer/Shared/WhenChainRenderer.cs` (~833 lines) -- **DEAD CODE**: has zero references from any renderer. Contains its own When chain rendering methods but they are never called. Consider deleting in Phase 6 cleanup.
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- `GetBuilderInterface()` for builder interface selection
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- `GetBuilderInterface()` for inline/delegate patterns

**Interface files:**
- `src/KnockOff/IMethodCallBuilder.cs` -- 3 builder interfaces (to be replaced)
- `src/KnockOff/IMethodTracking.cs` -- tracking hierarchy (unchanged)
- `src/KnockOff/IMethodSequence.cs` -- sequence interface with `ThenCall` (to be replaced)
- `src/KnockOff/IWhenTracking.cs` -- When chain interfaces (void renames needed)
- `src/KnockOff/ITracking.cs` -- base interface (unchanged)
- `src/KnockOff/IPropertySequence.cs` -- property sequences (unchanged, out of scope)
- `src/KnockOff/IIndexerSequence.cs` -- indexer sequences (unchanged, out of scope)
- `src/KnockOff/IPropertyCallBuilder.cs` -- property builders (unchanged, out of scope)
- `src/KnockOff/IIndexerCallBuilder.cs` -- indexer builders (unchanged, out of scope)

**Design files examined:**
- `src/Design/Design.Stubs/Methods/BasicMethods.cs` -- basic method API usage
- `src/Design/Design.Stubs/Methods/MethodSequences.cs` -- sequence API usage
- `src/Design/Design.Stubs/Methods/WhenMatching.cs` -- When chain API usage
- `src/Design/Design.Stubs/Methods/AsyncConsistency.cs` -- async API across all patterns
- `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` -- all 9 patterns in one file

**Test files examined:**
- `src/Tests/KnockOffTests/WhenChainTests.cs` -- void When chain `.Call()` usage
- `src/Tests/KnockOffTests/BuilderElevationTests.cs` -- builder/sequence elevation
- `src/Tests/KnockOffTests/SequencingTests.cs` -- ThenCall sequencing

**Prior plan examined:**
- `docs/plans/simplify-oncall-sequence-api-design.md` -- already-approved plan for builder interfaces (now subsumed)

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-06

### Why This Plan Is Approved After Revision

All five original concerns have been addressed by the architect:
1. **WhenChainRenderer.cs dead code** -- All references corrected to `MethodInterceptorRenderer.cs` private methods. Dead code noted throughout and optional deletion added to Phase 6.
2. **Internal self-call in `Returns(first, params rest)`** -- Acknowledged in Phase 2b item 5 with overload resolution analysis.
3. **`ThenReturns(value)` delegation to renamed method** -- Acknowledged in Phase 2c item 3 with line references and overload resolution confirmation.
4. **Superseded plan coordination** -- `simplify-oncall-sequence-api-design.md` explicitly called out in Dependencies and Phase 6 cleanup.
5. **CA1716 suppression** -- Removal added to both Phase 1 step 5 and Phase 2d item 4.

The plan is now implementable without any ambiguity. Every generator code path is correctly identified, overload resolution edge cases are documented, and the phasing allows independent verification at each checkpoint.

### My Understanding of This Plan

**Core Change:** Drop `OnCall()` from method interceptors entirely. Non-void methods get `Returns(callback)` (merging with existing `Returns(value)`). Void methods get `Execute(callback)`. Sequence chaining renames `ThenCall` to `ThenReturns`/`ThenExecute`. Void When chains rename `Call` to `Execute` and `ThenCall` to `ThenExecute`. Simplified async `ThenReturns(Func<..., T>)` added to builder and sequence (subsuming sequence-callback-simplification todo).

**User-Facing API:** `stub.Method.Returns(callback)` or `stub.Method.Execute(callback)` replaces `stub.Method.OnCall(callback)`. Builder chains use `.ThenReturns()` / `.ThenExecute()`. Void When chain uses `.Execute()` / `.ThenExecute()`.

**Internal Changes:** New interface files replacing `IMethodCallBuilder`/`IMethodSequence` with void/non-void variants. `MethodInterceptorRenderer.cs` changes to generate renamed methods. `GetBuilderInterface()` updated for void/non-void split. `IWhenTracking.cs` updated for void When chain renames.

**Patterns Affected:** All 9 (shared renderer -- independently verified).

### Codebase Investigation

**Files Examined:**
- `src/KnockOff/IMethodCallBuilder.cs` -- 3 builder interfaces with `ThenCall`, to be replaced
- `src/KnockOff/IMethodSequence.cs` -- `IMethodSequence<T>.ThenCall`, to be replaced
- `src/KnockOff/IWhenTracking.cs` -- `IVoidWhenChain.Call()` and `.ThenCall()` confirmed, `IWhenChain.ThenCall()` stays
- `src/KnockOff/IMethodTracking.cs` -- Tracking hierarchy, unchanged (confirmed)
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- Primary target. Confirmed `OnCall()` generation (lines 171, 274, 304, 485, 514, 537), `Returns(value)` (line 207), `Returns(first, params rest)` (line 234), `MethodCallBuilderImpl.ThenCall()` (line 1550), `MethodSequenceImpl.ThenCall()` (line 1717), void When chain `Call()`/`ThenCall()` (lines 2441/2484), non-void When chain `ThenCall()` (line 2029)
- `src/Generator/Renderer/Shared/WhenChainRenderer.cs` -- **DEAD CODE. Zero references from any renderer.** Contains its own `RenderVoidWhenChainImpl()` etc., but none are called.
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- `GetBuilderInterface()` at line 269, returns `IMethodCallBuilder` variants
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- `GetBuilderInterface()` at line 200, same pattern
- `src/Tests/KnockOffTests/SequencingTests.cs` -- Confirmed OnCall/ThenCall patterns
- `src/Tests/KnockOffTests/WhenChainTests.cs` -- Confirmed void When `.Call()`/`.ThenCall()` patterns
- `src/Tests/KnockOffTests/BuilderElevationTests.cs` -- Confirmed builder elevation patterns

**Searches Performed:**
- `MethodInterceptorRenderer.RenderInterceptorClass` -- Found in `FlatRenderer.cs`, `StandaloneClassRenderer.cs`, `InlineRenderer.cs` (lines 166, 1320), `ClassRenderer.cs`. All 9 patterns confirmed.
- `WhenChainRenderer` usage -- ZERO references outside its own file. Dead code.
- `GetBuilderInterface` -- Found in `UnifiedInterceptorBuilder.cs` (3 sites) and `ModelAdapters.cs` (4 sites)
- `Returns(first, params rest)` internal call to `OnCall()` -- Found at line 241

**Design.Stubs Verification:**
The architect states "Design Project Verification: Deferred to Phase 4." Normally I would reject for this. However, this plan is a mechanical rename in a shared renderer layer, not a new feature added per-pipeline. The scope claim (all 9 patterns affected) is verified by tracing renderer call sites. The Design.Stubs deferral is acceptable here because the rename will be validated by Checkpoint 4 compilation.

### Concerns

**1. Incorrect File Reference: WhenChainRenderer.cs is Dead Code**

- **Details:** The plan (Phase 2d, Pipeline Analysis line 553, Codebase Deep-Dive) repeatedly references `WhenChainRenderer.cs` as the file to modify for void When chain renames. This file is NOT used. It has ZERO references from any renderer. The actual void When chain rendering is done by **private methods inside `MethodInterceptorRenderer.cs`**: `RenderVoidWhenChainImpl()` (line 2406), `RenderVoidWhenEntryPoints()` (line 2233), `RenderWhenChainImpl()` (line 1978), `RenderWhenEntryPoints()` (line 2109). The plan will misdirect implementers.
- **Question:** Should Phase 2d be corrected to reference `MethodInterceptorRenderer.cs` private methods instead of `WhenChainRenderer.cs`? Should `WhenChainRenderer.cs` be deleted as dead code?
- **Suggestion:** Rewrite Phase 2d to target `MethodInterceptorRenderer.RenderVoidWhenChainImpl()` (line 2406) and `MethodInterceptorRenderer.RenderVoidWhenEntryPoints()` (line 2233). Optionally add a cleanup step to delete `WhenChainRenderer.cs`.

**2. Internal Self-Call in `Returns(first, params rest)` Not Addressed**

- **Details:** The generated `Returns(first, params rest)` method (rendered at line 234) internally calls `OnCall(...)` to set up the first sequence element (line 241: `var builder = OnCall({discardPrefix} => first)`). After renaming `OnCall` to `Returns`, this generated code becomes `var builder = Returns(...)`. The plan item #6 says "internal calls to `ThenReturns` stay the same name (already called `ThenReturns`)" but does not mention the `OnCall` self-call in `Returns(first, params rest)`.
- **Question:** Is this intentional omission because it "just works" after rename (Returns calling Returns with different overloads), or was it missed?
- **Suggestion:** Explicitly call this out in Phase 2b. After rename, the generated code will read `var builder = Returns({discardPrefix} => first)` which calls the `Returns(callback)` overload from within the `Returns(value, params values)` overload. This is valid C# overload resolution but should be documented as an acknowledged internal pattern.

**3. `ThenReturns(value)` Internal Delegation to Renamed Method Not Acknowledged**

- **Details:** Currently, `ThenReturns(value)` on both `MethodCallBuilderImpl` and `MethodSequenceImpl` delegates to `ThenCall(callback)` internally (lines 1579, 1583, 1587 for builder; lines 1734, 1738, 1742 for sequence). After rename, `ThenReturns(value)` will delegate to `ThenReturns(callback)`. This creates a method calling another overload of itself -- valid for overload resolution but a noteworthy pattern.
- **Question:** Is the architect aware that after rename, `ThenReturns(string value) => ThenReturns((_) => value)` will be the generated pattern? This is correct but should be acknowledged.
- **Suggestion:** Add an explicit note in Phase 2c that the existing `ThenReturns(value)` delegation will naturally resolve after `ThenCall` is renamed to `ThenReturns`.

### What Looks Good

- **Pipeline analysis is correct.** All 9 patterns route through `MethodInterceptorRenderer.RenderInterceptorClass()`. Independently verified.
- **Interface design is clean.** The void/non-void split with `IMethodReturnsBuilder` / `IMethodExecuteBuilder` and `IMethodReturnsSequence` / `IMethodExecuteSequence` follows the existing hierarchy pattern naturally.
- **Overload resolution analysis is sound.** `Returns(value)` vs `Returns(callback)` and `ThenReturns(value)` vs `ThenReturns(callback)` are distinct types.
- **Scope boundaries are clear.** Properties, indexers, events explicitly out of scope. Non-void When chain `ThenCall` stays.
- **Phasing is logical.** Interfaces first, generator second, then tests/design/docs in fresh agents.
- **Acceptance criteria are comprehensive.** All key behaviors explicitly listed.
- **Sequence-callback-simplification subsumption is natural.** Adding simplified async `ThenReturns` during the rename is the right time.
- **Overload group rendering is covered.** The plan's Phase 2 changes apply to both single-signature and overload group paths in the renderer, and `isVoid` is already available on `MethodOverloadSignature`.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. The `#pragma warning disable CA1716` suppression for `Call()` on `IVoidWhenChain` -- after rename to `Execute()`, is this suppression still needed? "Execute" is not a C# keyword but may still trigger CA1716 (identifiers should not match keywords). Needs checking.
2. The overload group path for `Returns(value)` -- currently only the single-signature path generates `Returns(value)` and `Returns(first, params rest)`. The plan does not mention whether overload groups get `Returns(value)` entry points. This appears to be a pre-existing state (overload groups never had `Returns(value)`), but the plan should confirm this is intentional.

**Ways this could break existing functionality:**
1. The `simplify-oncall-sequence-api-design.md` plan is at status "Ready for Implementation" and defines `IMethodCallBuilder` interfaces that already exist. Deleting `IMethodCallBuilder.cs` and the related plan's status need coordination -- the plan says to move the sequence-callback-simplification todo to completed but does not mention the simplify-oncall-sequence-api todo/plan.

**Ways users could misunderstand the API:**
1. For non-void methods, having both `Returns(42)` (value) and `Returns((a,b) => a+b)` (callback) under the same method name is clean but could confuse users who don't realize the lambda is a callback that gets invoked on each call vs. the value which is returned directly. This is acknowledged in the risk section and is acceptable.

### Recommendation

The concerns are correctability -- none require architectural rethinking. Concern #1 (dead code file reference) is the most important as it would misdirect the implementer. Concerns #2 and #3 are documentation/acknowledgment issues that don't change the implementation.

**Send back to architect to correct:**
1. Fix Phase 2d to reference `MethodInterceptorRenderer.cs` private methods, not `WhenChainRenderer.cs`
2. Fix Pipeline Analysis statement "When chains also route through `WhenChainRenderer`" -- this is false
3. Acknowledge internal self-call patterns in Phase 2b/2c
4. Address the `simplify-oncall-sequence-api` plan status (separate from sequence-callback-simplification)
5. Check if `#pragma warning disable CA1716` is still needed after `Call()` -> `Execute()` rename

### Architect Response (2026-02-06)

All five concerns addressed. Changes made to the plan:

**Concern 1 -- WhenChainRenderer.cs is Dead Code (Critical):**
- Corrected Approach section (Phase 2 summary) to reference `MethodInterceptorRenderer.cs` private methods
- Rewrote "Generator Changes > WhenChainRenderer.cs" subsection entirely -- now titled "Void When Chain Methods (inside MethodInterceptorRenderer.cs)" with correct method names and line numbers: `RenderVoidWhenChainImpl()` (~2406), `RenderVoidWhenEntryPoints()` (~2233), `RenderWhenChainImpl()` (~1978), `RenderWhenEntryPoints()` (~2109)
- Rewrote Phase 2d header and content to target correct file and methods
- Fixed Pipeline Analysis to remove false "WhenChainRenderer" routing claim
- Updated Codebase Deep-Dive to mark `WhenChainRenderer.cs` as dead code with zero references
- Added optional cleanup step in Phase 6 to delete the dead code file

**Concern 2 -- Internal Self-Call in `Returns(first, params rest)`:**
- Added explicit note in Phase 2b (item 5) documenting that after rename, `Returns(T first, params T[] rest)` internally calls `Returns(TCallback callback)`. Confirmed overload resolution is unambiguous because the parameter types are distinct (`T` value vs `TCallback` delegate).

**Concern 3 -- `ThenReturns(value)` Delegation to Renamed Method:**
- Added explicit note in Phase 2c (item 3) documenting that `ThenReturns(T value) => ThenReturns((_) => value)` is the post-rename pattern on both `MethodCallBuilderImpl` and `MethodSequenceImpl`. Confirmed overload resolution is correct -- `T` vs `Func<..., T>` are distinct types.

**Concern 4 -- Stale `simplify-oncall-sequence-api-design.md` Plan:**
- Added "Superseded Plans" subsection under Dependencies documenting that `simplify-oncall-sequence-api-design.md` (status: "Ready for Implementation") is superseded by this plan
- Added Phase 6 step 5 to move the superseded plan to `docs/plans/completed/` with status update

**Concern 5 -- CA1716 Suppression:**
- Added explicit note in Phase 2d that `#pragma warning disable CA1716` can be removed after `Call()` -> `Execute()` rename -- `Execute` is not a C# keyword
- Added note in Phase 1 step 5 to remove the suppression pragmas from `IWhenTracking.cs`

---

## Implementation Contract

**Created:** 2026-02-06
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

Design.Stubs verification is deferred to Phase 4. The acceptance criterion is:
- [ ] `dotnet build src/Design/Design.Stubs` succeeds after all OnCall/ThenCall/Call references are updated to Returns/Execute/ThenReturns/ThenExecute

### In Scope

#### Phase 1: Interface Redesign (src/KnockOff/)

- [ ] Create `src/KnockOff/IMethodReturnsBuilder.cs` with 3 interfaces: `IMethodReturnsBuilder<TCallback>`, `IMethodReturnsBuilder<TCallback, TArg>`, `IMethodReturnsBuilderArgs<TCallback, TArgs>`
- [ ] Create `src/KnockOff/IMethodExecuteBuilder.cs` with 3 interfaces: `IMethodExecuteBuilder<TCallback>`, `IMethodExecuteBuilder<TCallback, TArg>`, `IMethodExecuteBuilderArgs<TCallback, TArgs>`
- [ ] Create `src/KnockOff/IMethodReturnsSequence.cs` with 2 interfaces: `IMethodReturnsSequence`, `IMethodReturnsSequence<TCallback>`
- [ ] Create `src/KnockOff/IMethodExecuteSequence.cs` with 2 interfaces: `IMethodExecuteSequence`, `IMethodExecuteSequence<TCallback>`
- [ ] Update `src/KnockOff/IWhenTracking.cs`: rename `IVoidWhenChain.Call()` to `Execute()`, `ThenCall()` to `ThenExecute()`, remove `#pragma warning disable/restore CA1716`
- [ ] Delete `src/KnockOff/IMethodCallBuilder.cs`
- [ ] Update `src/KnockOff/IMethodSequence.cs`: remove `ThenCall` from `IMethodSequence<TCallback>`
- [ ] **Checkpoint 1:** `dotnet build src/KnockOff/KnockOff.csproj` passes

#### Phase 2: Generator Changes

**Phase 2a: Builder interface selection**
- [ ] Update `src/Generator/Builder/UnifiedInterceptorBuilder.cs` `GetBuilderInterface()` -- accept `isVoid` parameter, return `IMethodReturnsBuilder` (non-void) or `IMethodExecuteBuilder` (void)
- [ ] Update `src/Generator/Renderer/Shared/ModelAdapters.cs` `GetBuilderInterface()` -- same void/non-void split
- [ ] Verify `IsVoid` is available at all `GetBuilderInterface()` call sites (it is on `UnifiedMethodInterceptorModel` and `MethodOverloadSignature`)

**Phase 2b: MethodInterceptorRenderer.cs -- rename entry points**
- [ ] Single-signature `OnCall(delegateType callback)` (line ~171): rename to `Returns(callback)` when `!isVoid`, `Execute(callback)` when `isVoid`
- [ ] Overload group `OnCall(overload.DelegateName callback)` (line ~485): same void/non-void rename
- [ ] Simplified async `OnCall(Func<..., T>)` (line ~274): rename to `Returns(Func<..., T>)`
- [ ] Simplified void async `OnCall(Action<...>)` (line ~304): rename to `Execute(Action<...>)`
- [ ] Overload group simplified async `OnCall(simplifiedDelegateType)` (line ~514): rename to `Returns(...)`
- [ ] Overload group simplified void async `OnCall(voidDelegateType)` (line ~537): rename to `Execute(...)`
- [ ] Internal `Returns(first, params rest)` self-call: the call to `OnCall(...)` at line ~241-249 becomes `Returns(...)` -- verify overload resolution is unambiguous
- [ ] Update comments referencing `OnCall` (cosmetic)

**Phase 2c: MethodInterceptorRenderer.cs -- rename builder/sequence ThenCall**
- [ ] `RenderMethodCallBuilderImpl()` (line ~1440): rename `ThenCall()` to `ThenReturns()` (non-void) or `ThenExecute()` (void) -- method name, explicit interface implementation
- [ ] `RenderMethodSequenceImpl()` (line ~1671): rename `ThenCall()` to `ThenReturns()` (non-void) or `ThenExecute()` (void) -- method name, explicit interface implementation
- [ ] Verify `ThenReturns(value)` delegation from `ThenCall(callback)` to `ThenReturns(callback)` works after rename (builder lines ~1577-1587, sequence lines ~1732-1742)
- [ ] Update explicit interface implementations on `MethodCallBuilderImpl`: change `IMethodCallBuilder.ThenCall` to `IMethodReturnsBuilder.ThenReturns` / `IMethodExecuteBuilder.ThenExecute` (line ~1662)
- [ ] Update explicit interface implementations on `MethodSequenceImpl`: change `IMethodSequence<T>.ThenCall` to `IMethodReturnsSequence<T>.ThenReturns` / `IMethodExecuteSequence<T>.ThenExecute` (line ~1800)
- [ ] Add simplified async `ThenReturns(Func<..., T> callback)` overloads on `MethodCallBuilderImpl` for non-void async methods (NEW -- subsumes sequence-callback-simplification)
- [ ] Add simplified async `ThenReturns(Func<..., T> callback)` overloads on `MethodSequenceImpl` for non-void async methods (NEW)

**Phase 2d: MethodInterceptorRenderer.cs -- void When chain renames**
- [ ] `RenderVoidWhenChainImpl()` (line ~2406): rename generated `Call()` to `Execute()`, `ThenCall()` to `ThenExecute()`
- [ ] Update explicit interface implementation for `IVoidWhenChain.Call` to `IVoidWhenChain.Execute` (line ~2449)
- [ ] `RenderVoidWhenEntryPoints()` (line ~2233): update if any references to `Call` exist
- [ ] Verify non-void `RenderWhenChainImpl()` (line ~1978) `ThenCall()` stays unchanged

**Phase 2e: Comments and cosmetic**
- [ ] Update code comments referencing `OnCall` in `MethodInterceptorRenderer.cs`
- [ ] Verify `_onCall` / `_onCallTracking` internal field names stay unchanged (not user-facing)

- [ ] **Checkpoint 2:** `dotnet build src/KnockOff.sln` passes

#### Phase 3: Test Updates (Fresh Agent) -- DONE

- [x] Update all `OnCall` usages to `Returns` (non-void methods) or `Execute` (void methods) across all test files
- [x] Update all `ThenCall` usages to `ThenReturns` (non-void sequence) or `ThenExecute` (void sequence) -- but NOT non-void When chain `ThenCall` which stays
- [x] Update all void When chain `.Call()` to `.Execute()` and `.ThenCall()` to `.ThenExecute()`
- [x] Verify no non-void When chain `.ThenCall()` was accidentally renamed
- [x] **Checkpoint 3:** All tests pass on all target frameworks (KnockOffTests: 1184-1185, NeatooInterfaceTests: 473, AssemblyStrict: 14)
- **Note:** Generic method typed handlers (`.Of<T>().OnCall(...)`) still use `OnCall` due to Phase 2 generator gap in typed handler rendering. 35 test lines remain using `.OnCall(` to match generated code.

#### Phase 4: Design Project Updates (Fresh Agent) -- DONE

- [x] Update all `OnCall`/`ThenCall`/`Call` references in Design.Stubs (10 files, ~90 OnCall occurrences)
- [x] Update all `OnCall`/`ThenCall`/`Call` references in Design.Tests (16 files, ~145 OnCall + ~44 ThenCall occurrences)
- [x] **Checkpoint 4:** `dotnet build src/Design/Design.Stubs` AND `dotnet test src/Design/Design.Tests` pass

#### Phase 5: Documentation Updates (Fresh Docs Agent) -- DONE

- [x] Update skill files (7 files, see plan table)
- [x] Update MarkdownSnippet sample files (30 files, ~383 OnCall occurrences)
- [x] Update documentation guides (~20 files, see plan table) + README.md
- [ ] Create migration guide at `docs/migration/oncall-to-returns-execute.md` (deferred)
- [x] **Checkpoint 5:** `dotnet mdsnippets` succeeds, all docs synced. Full test suite passes.

#### Phase 6: Cleanup -- DONE

- [x] Bump version in `Directory.Build.props` (0.37.0 → 0.38.0)
- [x] Create release notes in `docs/release-notes/v0.38.0.md`
- [x] Move `docs/todos/sequence-callback-simplification.md` to `docs/todos/completed/` with status "Subsumed by unify-returns-execute-api"
- [x] Move `docs/plans/simplify-oncall-sequence-api-design.md` to `docs/plans/completed/` with status "Superseded by unify-returns-execute-design"
- [x] Deleted `src/Generator/Renderer/Shared/WhenChainRenderer.cs` (dead code, zero references)
- [x] Fixed stale `src/Design/README.md` OnCall/ThenCall references
- [x] **Checkpoint 6:** `dotnet test src/KnockOff.sln` -- full solution builds (0 warnings, 0 errors) and all tests pass (KnockOffTests: 1184-1185, NeatooInterfaceTests: 473, Documentation.Samples: 571, Design.Tests: 259)

### Explicitly Out of Scope

- Property interceptors (`Get`, `Set`, sequence chaining via `Get().ThenGet()`/`Set().ThenSet()`) -- stay as-is
- Indexer interceptors (`Get`, `Set`, sequence chaining via `Get().ThenGet()`/`Set().ThenSet()`) -- stay as-is
- Event interceptors -- stay as-is
- Non-void When chain `ThenCall()` on `IWhenChain<TDelegate, TReturn>` -- stays as-is per user decision
- Overload group `Returns(value)` entry point -- not currently generated for overload groups, pre-existing state, not in scope

### Verification Gates

1. **After Phase 1:** `dotnet build src/KnockOff/KnockOff.csproj` passes. New interfaces compile. Old `IMethodCallBuilder.cs` deleted.
2. **After Phase 2:** `dotnet build src/KnockOff.sln` passes. Generated code compiles with new API names. Manual spot-check: inspect a generated `.g.cs` file for a non-void method to confirm `Returns(callback)` and `ThenReturns(callback)` appear; inspect a void method to confirm `Execute(callback)` and `ThenExecute(callback)` appear.
3. **After Phase 3:** `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj` -- all tests pass on all target frameworks (net8.0, net9.0, net10.0).
4. **After Phase 4:** `dotnet build src/Design/Design.Stubs` AND `dotnet test src/Design/Design.Tests` -- both pass.
5. **After Phase 5:** `dotnet mdsnippets` succeeds. Spot-check 2-3 markdown files for correct snippet content.
6. **Final:** `dotnet test src/KnockOff.sln` -- full solution, all tests pass. Grep the codebase to verify no remaining `.OnCall(` on method interceptors in generated code, tests, design, or docs.

### Stop Conditions

If any of these occur, STOP and report:
- An out-of-scope test (property, indexer, or event tests) starts failing
- An overload resolution ambiguity is discovered (Returns(value) vs Returns(callback) or ThenReturns(value) vs ThenReturns(callback))
- A non-void When chain `.ThenCall()` was accidentally renamed
- An architectural contradiction is discovered (e.g., `IsVoid` is not available where needed)
- Generated code does not compile after Phase 2 changes

---

## Implementation Progress

### Phase 1: Interface Redesign -- DONE
- Created `IMethodReturnsBuilder.cs`, `IMethodExecuteBuilder.cs`, `IMethodReturnsSequence.cs`, `IMethodExecuteSequence.cs`
- Updated `IWhenTracking.cs`: renamed `Call()`/`ThenCall()` to `Execute()`/`ThenExecute()` on `IVoidWhenChain`
- Deleted `IMethodCallBuilder.cs`
- Updated `IMethodSequence.cs`: removed `ThenCall` from generic `IMethodSequence<TCallback>`
- Checkpoint 1: `dotnet build src/KnockOff/KnockOff.csproj` passes (0 warnings, 0 errors)

### Phase 2: Generator Changes -- DONE
- **Phase 2a:** Updated `GetBuilderInterface()` in both `UnifiedInterceptorBuilder.cs` and `ModelAdapters.cs` to accept `isVoid` and return `IMethodReturnsBuilder`/`IMethodExecuteBuilder` accordingly
- **Phase 2b:** Renamed `OnCall` entry points to `Returns` (non-void) / `Execute` (void) in all 6 generation paths (single-signature, overload group, simplified async non-void, simplified async void, overload simplified async non-void, overload simplified async void)
- **Phase 2c:** Renamed `ThenCall` on `MethodCallBuilderImpl` and `MethodSequenceImpl` to `ThenReturns`/`ThenExecute`. Added `parameters` parameter to `RenderMethodCallBuilderImpl` and `RenderMethodSequenceImpl` signatures to support simplified async `ThenReturns(Func<..., T>)` overloads. Updated explicit interface implementations to reference `IMethodReturnsBuilder`/`IMethodExecuteBuilder` and `IMethodReturnsSequence`/`IMethodExecuteSequence`.
- **Phase 2d:** Renamed `Call()` to `Execute()` and `ThenCall()` to `ThenExecute()` on `VoidWhenChainImpl`. Updated explicit interface implementation. Verified non-void When chain `ThenCall` stays unchanged.
- **Phase 2e:** Updated comments throughout `MethodInterceptorRenderer.cs`. Fixed CS0539 build error from incorrect explicit interface implementation of `Verifiable()` on empty marker interfaces `IMethodReturnsSequence`/`IMethodExecuteSequence`.
- Checkpoint 2: Generator and library build with 0 warnings, 0 errors. Test/Design projects have expected `OnCall`/`ThenCall`/`Call` reference errors (2901 errors across 6 test projects) -- these will be resolved in Phase 3 (tests) and Phase 4 (design).

### Phase 3: Test Updates -- DONE
- Bulk renamed `.OnCall(` to `.Returns(` across all test files (KnockOffTests, NeatooInterfaceTests, AssemblyStrict, KnockOffSandbox, Benchmarks)
- Fixed void methods: changed `.Returns(` to `.Execute(` on 177 lines in KnockOffTests + 5 in NeatooInterfaceTests + 14 in Benchmarks + 2 in KnockOffSandbox
- Fixed void When chains: `.Call(` to `.Execute(`, `.ThenCall(` to `.ThenExecute(` (6 occurrences)
- Reverted non-void When chain `.ThenCall(` (19 occurrences stay as `.ThenCall(`)
- Reverted generic method typed handler `.OnCall(` (35 lines -- generator gap, typed handlers still generate `OnCall`)
- Simplified void async callback in NeatooInterfaceTests (removed `return Task.CompletedTask` from `.Execute(` lambda)
- Checkpoint 3: All tests pass on all TFMs (net8.0, net9.0, net10.0)

**Status:** Phase 3 complete. Ready for Phase 4 (Design project updates by fresh agent).

### Phase 4: Design Project Updates -- DONE
- Bulk renamed `.OnCall(` to `.Returns(` and `.ThenCall(` to `.ThenReturns(` across all Design.Stubs and Design.Tests `.cs` files
- Fixed void methods: changed `.Returns(` to `.Execute(` on void interceptors (Reset, SaveDataAsync, Execute, Log, Save, Delete, Initialize, SaveOrder, SaveAsync, Process) across Design.Stubs (11 fixes) and Design.Tests (20 fixes)
- Fixed void sequences: changed `.ThenReturns(` to `.ThenExecute(` on void method sequences (Design.Stubs: 2 occurrences, Design.Tests: 2 occurrences)
- Reverted non-void When chain `.ThenReturns(` back to `.ThenCall(` (Design.Stubs: 2 occurrences, Design.Tests: 5 occurrences) -- these stay per rule 7
- Reverted generic method typed handler `.Returns(` back to `.OnCall(` (Design.Stubs: 2 occurrences in UserMethodBasics.cs) -- known gap, typed handlers still generate `OnCall`
- Fixed void When chain `.Call(` to `.Execute(` (Design.Stubs: 2 occurrences in DelegateStubs.cs, Design.Tests: 1 occurrence in WhenChainVerificationBugTests.cs)
- Updated comments/documentation in Design.Stubs files referencing old API names:
  - `BasicMethods.cs` -- Updated ~15 comments (`OnCall` -> `Returns`/`Execute`, method names)
  - `MethodOverloads.cs` -- Updated ~15 comments (`OnCall` -> `Returns`/`Execute`, method names)
  - `MethodSequences.cs` -- Updated ~15 comments (`OnCall().ThenReturns()` -> `Returns().ThenReturns()`, `ThenCall` -> `ThenReturns`)
  - `WhenMatching.cs` -- Updated ~6 comments (`OnCall` -> `Returns`)
  - `AsyncConsistency.cs` -- Updated ~10 method names (`OnCallSimplified` -> `ReturnsSimplified`, `OnCallFull` -> `ReturnsFull`)
  - `AllPatterns.cs` -- Updated ~3 comments (`OnCall` -> `Returns(callback)`)
  - `DelegateStubs.cs` -- Updated ~5 comments/method names (`OnCall` -> `Returns`/`Execute`)
  - `SourceDelegation.cs` -- Updated ~3 comments (`OnCall/Returns` -> `Returns/Execute`)
  - `Verification.cs` -- Updated ~1 comment (`OnCall, Returns` -> `Returns, Execute`)
  - `UserMethodBasics.cs` -- Updated ~25 comments/method names (`OnCall` -> `Returns`/`Execute`)
  - `VoidUserMethodFallback.cs` -- Updated ~3 comments (`OnCall` -> `Execute`/`Returns`)
  - `StandaloneClassUserMethods.cs` -- Updated ~2 comments (`OnCall/Returns` -> `Returns/Execute`)
  - `GenericFormatterStub.cs` -- Updated ~1 comment (`OnCall` -> `Returns/Execute`)
- Checkpoint 4: `dotnet build src/Design/Design.Stubs` passes (0 warnings, 0 errors). `dotnet test src/Design/Design.Tests` passes: 259 tests, 0 failures on all 3 TFMs (net8.0, net9.0, net10.0).

**Status:** Phase 4 complete.

### Phase 5: Documentation Updates -- DONE

- [x] Updated skill files (SKILL.md, api-reference.md, methods.md, moq-migration.md, patterns.md -- done by prior agent)
- [x] Updated MarkdownSnippet sample files (30 files, ~383 OnCall occurrences -- done by prior agent)
- [x] Updated documentation guides (all non-snippet `OnCall`/`ThenCall`/`Call` references updated):
  - `docs/guides/methods.md` -- 16 non-snippet edits (headings, descriptions, key takeaways)
  - `docs/guides/async-patterns.md` -- 11 edits (tier table, headings, code descriptions)
  - `docs/guides/delegates.md` -- 11 edits (headings, tables, prose)
  - `docs/guides/verification.md` -- 5 edits (direct verification, batch, argument, history)
  - `docs/guides/user-methods.md` -- 7 edits (intro, how it works, override heading, tracking, reset, key takeaways)
  - `docs/guides/parameter-matching.md` -- 8 edits (problem statement, priority order, combining, state transitions, fallback heading)
  - `docs/guides/api-consistency-matrix.md` -- 6 edits (feature tables, priority chain, reset, user methods, async)
  - `docs/guides/source-delegation.md` -- 3 edits (intro, priority order, methods guide link)
  - `docs/guides/stub-patterns.md` -- 2 edits (behavior configuration, next steps link)
  - `docs/guides/advanced-callbacks.md` -- 1 edit (intro description)
  - `docs/reference/interceptor-api.md` -- 15 edits (config tables, callback signatures, reset summary, when chain, tracking objects)
  - `docs/reference/smart-defaults.md` -- 5 edits (priority order, guidance, option label, see also)
  - `docs/getting-started.md` -- 4 edits (method config overview, decision guide)
  - `docs/comparison.md` -- 2 edits (methods table, delegates table)
  - `docs/troubleshooting.md` -- 5 edits (error descriptions, sequences, key differences)
  - `docs/migration/from-moq.md` -- 7 edits (quick reference, steps, gotchas)
  - `docs/migration/from-nsubstitute.md` -- 12 edits (approach, trade-offs, quick reference, steps)
  - `docs/type-safety.md` -- 3 edits (descriptions, summary table)
  - `README.md` -- 9 edits (intro, feature list, source delegation, comparison table, documentation links)
- [x] Ran `dotnet mdsnippets` -- 574 snippets synced, no errors
- [x] **Checkpoint 5:** `dotnet mdsnippets` succeeds. Full solution tests pass (all 12 test runs, 0 failures). Design.Stubs builds (0 warnings, 0 errors).
- **Note:** Migration guide (`docs/migration/oncall-to-returns-execute.md`) not yet created -- listed in plan but deferred.
- **Note:** `.Of<T>().OnCall(...)` references in generic-methods.md and user-methods.md remain unchanged (known generator gap -- typed handlers still generate `OnCall`).

**Status:** Phase 5 complete (migration guide deferred). Ready for Phase 6 (Cleanup).

---

## Completion Evidence

### Phase 1-2 Evidence (Generator Changes)

**Build results:**
- `dotnet build src/KnockOff/KnockOff.csproj` -- 0 warnings, 0 errors (all 3 target frameworks: net8.0, net9.0, net10.0)
- Generator project builds cleanly as a dependency

### Files Modified in Phase 2

1. `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- `GetBuilderInterface()` accepts `isVoid`, returns void-specific builder interfaces
2. `src/Generator/Renderer/Shared/ModelAdapters.cs` -- `GetBuilderInterface()` same void/non-void split
3. `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- All Phase 2b-2e changes (entry point renames, ThenCall renames, simplified async ThenReturns overloads, void When chain renames, comment updates)

### Phase 3 Evidence (Test Updates)

**Test results (all pass, 0 failures):**
- KnockOffTests: 1184 (net8.0), 1185 (net9.0), 1185 (net10.0)
- NeatooInterfaceTests: 473 (net8.0), 473 (net9.0), 473 (net10.0)
- AssemblyStrict: 14 (net8.0), 14 (net9.0), 14 (net10.0)
- KnockOffSandbox: builds successfully (not a test project)
- Benchmarks: builds successfully (not a test project)

**Transformations applied:**
- `.OnCall(` on non-void methods -> `.Returns(` (511 occurrences across 46 files in KnockOffTests)
- `.OnCall(` on void methods -> `.Execute(` (177 occurrences across 29 files)
- `.ThenCall(` in sequence contexts -> `.ThenReturns(` (bulk rename, then corrections)
- `.ThenCall(` on void sequences -> `.ThenExecute(` (7 occurrences)
- `.Call(` on void When chains -> `.Execute(` (29 occurrences across 3 files)
- `.ThenCall(` on void When chains -> `.ThenExecute(` (6 occurrences)
- `.ThenCall(` on non-void When chains -> stays `.ThenCall(` (19 occurrences reverted)
- Simplified void async callback: removed `return Task.CompletedTask` where `.Execute(` simplifies to `Action` callback (1 occurrence in NeatooInterfaceTests)

**Known generator gap discovered:**
Generic method typed handlers (`.Of<T>().OnCall(...)`) still generate `OnCall` instead of `Returns`/`Execute`. The Phase 2 generator changes did not update the typed handler rendering code path. These test lines (24 in KnockOffTests, 4 in NeatooInterfaceTests, 7 in Benchmarks) remain using `.OnCall(` to match the current generated code. This should be tracked as a separate follow-up task.

**Remaining error sources (NOT in Phase 3 scope):**
- Documentation.Samples: ~728 errors (Phase 5)
- Design.Stubs: ~186 errors (Phase 4)
- Design.Tests: errors (Phase 4)
