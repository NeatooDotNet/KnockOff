# Rename Call(Action) to Return(Action) on Task/ValueTask Methods

**Date:** 2026-02-07
**Related Todo:** [Remove Duplicate Call/Return on Task/ValueTask Methods](../todos/completed/action-vs-function-nomenclature.md)
**Status:** Complete
**Last Updated:** 2026-02-08

---

## Overview

Methods returning `Task` or `ValueTask` (without `<T>`) currently generate both `Return(delegate)` and `Call(Action<...>)` overloads. Having two different names for configuring the same method is confusing. The fix is to rename `Call(Action<...>)` to `Return(Action<...>)` so that all configuration for non-void methods uses `Return` consistently. This is a one-line rename in the renderer (applied in two places: primary method and overload group), plus updating tests and Design projects that use the old API.

---

## Approach

The renderer decides between `Call` and `Return` based on `IsVoid`. For `Task`/`ValueTask` methods, `IsVoid` is `false`, so the main entry point already generates `Return`. However, the *simplified void callback* overload (which accepts `Action<...>` instead of `Func<..., Task>`) is hardcoded to use the name `Call`. This is the only place that needs to change.

No model changes. No builder changes. No library changes. The generated code's method name changes from `Call` to `Return`; the delegate type, body, and behavior remain identical.

---

## Specific Code Changes

### 1. Renderer: Primary method simplified void callback

**File:** `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`
**Line 305** (approximate):

```csharp
// BEFORE:
w.Line($"public MethodCallBuilderImpl Call({voidDelegateType} callback)");

// AFTER:
w.Line($"public MethodCallBuilderImpl Return({voidDelegateType} callback)");
```

Also update the XML doc comment on line 304 from "Configures callback action" to something consistent with the Return naming.

### 2. Renderer: Overload group simplified void callback

**File:** `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`
**Line 539** (approximate):

```csharp
// BEFORE:
w.Line($"public MethodCallBuilderImpl_{overload.SignatureSuffix} Call({voidDelegateType} callback)");

// AFTER:
w.Line($"public MethodCallBuilderImpl_{overload.SignatureSuffix} Return({voidDelegateType} callback)");
```

Also update the XML doc comment on line 538.

### 3. Comments in renderer

Update the comment at line 300 (`// Call(Action<...>) - simplified void callback`) and line 534 to say `Return(Action<...>)` instead.

Update the `EmitSequenceElevation` XML doc comment at line 2645 which mentions `Call(simplifiedVoidCallback)` to say `Return(simplifiedVoidCallback)`.

Update the class-level XML doc at line 12 which mentions `Return()/Call()` to reflect the new naming.

---

## Affected Tests

### KnockOffTests (`.Call(` on Task/ValueTask methods -- rename to `.Return(`):

| File | Lines | Usage |
|------|-------|-------|
| `AsyncCallbackSimplificationTests.cs` | 180, 214, 227, 281, 343, 357, 384, 489, 505, 602 | `stub.SaveAsync.Call(...)`, `stub.RunAsync.Call(...)`, `stub.PingAsync.Call(...)` |
| `OverloadGroupAsyncCallbackTests.cs` | 180, 198, 220, 221, 461 | `stub.SaveAsync.Call((User user) => ...)` |
| `SequenceValueOverloadTests.cs` | 972, 992 | `knockOff.DoWorkAsync.Call(...)`, `knockOff.DoWorkValueTaskAsync.Call(...)` |

### Documentation Samples (`.Call(` on Task/ValueTask methods -- rename to `.Return(`):

| File | Lines | Usage |
|------|-------|-------|
| `AsyncSamples.cs` | 99-100, 117-118, 299 | `stub.UpdateUserAsync.Call(...)`, `stub.SaveAsync.Call(...)` |
| `SkillContentSamples.cs` | 355 | `stub.SaveAsync.Call(...)` |
| `TroubleshootingSamples.cs` | 430 | `stub.SaveAsync.Call(...)` |

### Design Projects (`.Call(` on Task/ValueTask methods -- rename to `.Return(`):

| File | Lines | Usage |
|------|-------|-------|
| `Design.Tests/MethodTests/AsyncConsistencyTests.cs` | 67, 120 | `stub.SaveAsync.Call(...)` |
| `Design.Stubs/Methods/BasicMethods.cs` | 298 | `stub.SaveDataAsync.Call(...)` |

### NOT affected (truly void methods, remain `Call`):

All other `.Call(` usages in the codebase are on truly void methods (`void` return type, `IsVoid == true`). These are correct and must not change. Examples: `stub.DoWork.Call(...)`, `stub.Dispose.Call(...)`, `stub.Delete.Call(...)`, etc.

---

## What Does NOT Change

- **`Call()` on truly void methods** -- stays `Call`. The `IsVoid` check on line 170 (`var entryPointName = model.IsVoid ? "Call" : "Return"`) is correct and untouched.
- **`ThenCall()` on truly void methods** -- stays `ThenCall`. The `isVoid ? "ThenCall" : "ThenReturn"` logic at lines 1565 and 1728 is correct for truly void methods. For Task/ValueTask methods, `IsVoid` is already `false`, so they already use `ThenReturn`.
- **No library changes** -- `IMethodCallSequence`, `IMethodReturnSequence`, etc. are unchanged.
- **No model changes** -- `IsVoid`, `IsVoidAsync` flags unchanged.
- **No builder changes** -- the builder pipeline is unaffected.
- **When chain `ThenCall`** -- this is part of the When chain API for all void methods and is unrelated.

---

## Acceptance Criteria

1. `dotnet build src/KnockOff.sln` succeeds
2. `dotnet test src/KnockOff.sln` -- all tests pass
3. `dotnet build src/Design/Design.Stubs` succeeds
4. `dotnet test src/Design/Design.Tests` -- all tests pass
5. Task/ValueTask methods no longer generate a `Call(Action<...>)` overload; they generate `Return(Action<...>)` instead
6. Truly void methods continue to use `Call()` unchanged

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking change for users | High | Low | Pre-1.0 software; documented in release notes |
| Miss a test file | Low | Low | Compiler errors will catch any missed `.Call(` on Task methods |
| Accidentally rename void method `Call` | Low | High | Only touch lines guarded by `isVoidAsync` condition, not `isVoid` |

---

## Architectural Verification

This is a renderer-only change. No pipeline stages are affected except the final code emission.

**Scope:** All nine patterns are affected equally because all patterns use the same `MethodInterceptorRenderer.cs` for generating method interceptor code. The `Call` -> `Return` rename applies uniformly.

**Breaking Changes:** Yes, but pre-1.0. Users who wrote `stub.SaveAsync.Call((data) => ...)` will need to change to `stub.SaveAsync.Return((data) => ...)`. Compiler errors guide them.

**Design.Stubs Verification:** Two files use the old API and will need updating:
- `Design.Stubs/Methods/BasicMethods.cs:298` -- `Call` -> `Return`
- `Design.Tests/MethodTests/AsyncConsistencyTests.cs:67,120` -- `Call` -> `Return`

After the renderer change, these files will fail to compile until updated, which proves the rename is working.

---

## Implementation Progress

**Started:** 2026-02-08

### Phase 1: Renderer Changes

- [x] Renamed `Call` to `Return` on line 305 (single-signature simplified void callback)
- [x] Renamed `Call` to `Return` on line 539 (overload group simplified void callback)
- [x] Updated comment at line 300: `Call(Action<...>)` -> `Return(Action<...>)`
- [x] Updated comment at line 534: `Call(Action<...>)` -> `Return(Action<...>)`
- [x] Updated class-level XML doc at line 12 to clarify Return/Call naming
- [x] Updated `EmitSequenceElevation` XML doc at line 2645: `Call(simplifiedVoidCallback)` -> `Return(simplifiedVoidCallback)`
- [x] Updated Branch 1 comment at line 2676: removed `or Call(callback)` since it is now `Return`
- [x] Updated Branch 4 comment at line 2734: `Call(simplifiedVoidCallback)` -> `Return(simplifiedVoidCallback)`
- **Verification**: `dotnet build src/KnockOff.sln` -- generator compiles, 84 expected CS1061 errors in consumer code (confirming rename works)

### Phase 2: Test and Sample File Updates

Files updated (`.Call(` on async methods renamed to `.Return(`):

1. `src/Tests/KnockOffTests/AsyncCallbackSimplificationTests.cs` -- 13 occurrences (lines 180, 195, 214, 227, 247, 262, 281, 343, 357, 384, 489, 505, 602)
2. `src/Tests/KnockOffTests/OverloadGroupAsyncCallbackTests.cs` -- 7 occurrences (lines 180, 198, 220, 221, 243, 259, 461)
3. `src/Tests/KnockOffTests/SequenceValueOverloadTests.cs` -- 2 occurrences (lines 972, 992) plus comment updates at lines 969, 989
4. `src/Tests/KnockOff.Documentation.Samples/AsyncSamples.cs` -- 3 occurrences (lines 100, 118, 299) plus comment at line 117
5. `src/Tests/KnockOff.Documentation.Samples/SkillContentSamples.cs` -- 1 occurrence (line 355)
6. `src/Tests/KnockOff.Documentation.Samples/TroubleshootingSamples.cs` -- 1 occurrence (line 430) plus comment at line 429
7. `src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/IRuleManagerTests.cs` -- 1 occurrence (line 129) **[not in original plan]**
8. `src/Design/Design.Tests/MethodTests/AsyncConsistencyTests.cs` -- 2 occurrences (lines 67, 120)
9. `src/Design/Design.Stubs/Methods/BasicMethods.cs` -- 1 occurrence (line 298)

**Note**: The plan missed `IRuleManagerTests.cs` and several line numbers in `AsyncCallbackSimplificationTests.cs` (195, 247, 262) and `OverloadGroupAsyncCallbackTests.cs` (243, 259). All were discovered by compiler errors.

- **Verification**: `dotnet build src/KnockOff.sln` -- 0 errors, 0 warnings

### Phase 3: Test Execution

- **Verification**: `dotnet test src/KnockOff.sln` -- all tests pass, zero failures
  - KnockOffTests: 1253 passed (net9.0), 1252 passed (net8.0), 1253 passed (net10.0)
  - KnockOffTests.AssemblyStrict: 14 passed x 3 frameworks
  - KnockOff.Documentation.Samples: 571 passed x 3 frameworks
  - KnockOff.NeatooInterfaceTests: 473 passed x 3 frameworks
- Design.Stubs: `dotnet build` -- 0 errors
- Design.Tests: `dotnet test` -- 301 passed x 3 frameworks

### Phase 4: Documentation Updates

Hand-written documentation:
- [x] `skills/knockoff/references/methods.md:486` -- Updated quick reference table from `Call` to `Return` for void async
- [x] `docs/guides/api-consistency-matrix.md:372` -- Updated feature table from `Call(Action<...>)` to `Return(Action<...>)`

Auto-generated documentation (via `dotnet mdsnippets`):
- [x] `docs/getting-started.md:210` -- Updated via snippet `async-task-simplified-void`
- [x] `skills/knockoff/SKILL.md:284` -- Updated via snippet `skill-method-oncall`
- [x] `skills/knockoff/references/methods.md:213` -- Updated via snippet `async-task-simplified-void`

Verified: All remaining `.Call(` references in docs and skills are on truly void methods (`stub.Save.Call`, `stub.Send.Call`, etc.) -- correct and unchanged.

---

## Completion Evidence

- **Tests Passing:** All tests pass across all 3 target frameworks (net8.0, net9.0, net10.0):
  - KnockOffTests: 1253/1253 (net9.0, net10.0), 1252/1252 (net8.0)
  - KnockOffTests.AssemblyStrict: 14/14 x 3 frameworks
  - KnockOff.Documentation.Samples: 571/571 x 3 frameworks
  - KnockOff.NeatooInterfaceTests: 473/473 x 3 frameworks
  - Design.Tests: 301/301 x 3 frameworks
- **Design Projects Compile:** Yes -- `dotnet build src/Design/Design.Stubs` succeeds with 0 errors
- **All Contract Items:** Confirmed complete
  1. [x] Renderer: `Call` renamed to `Return` on lines 305 and 539
  2. [x] Renderer: All associated comments updated (6 comments)
  3. [x] Tests: All 9 source files updated (7 test files + 2 Design files)
  4. [x] Samples: 3 documentation sample files updated
  5. [x] Hand-written docs: 2 files updated
  6. [x] Auto-generated docs: `dotnet mdsnippets` run, 3 markdown files auto-updated
- **Acceptance Criteria Met:**
  1. [x] `dotnet build src/KnockOff.sln` succeeds
  2. [x] `dotnet test src/KnockOff.sln` -- all tests pass
  3. [x] `dotnet build src/Design/Design.Stubs` succeeds
  4. [x] `dotnet test src/Design/Design.Tests` -- all tests pass
  5. [x] Task/ValueTask methods generate `Return(Action<...>)` instead of `Call(Action<...>)`
  6. [x] Truly void methods continue to use `Call()` unchanged

---

## Architect Verification

**Verified:** 2026-02-08
**Verdict:** VERIFIED

### Independent Build Results

All builds and tests executed independently by the architect:

- `dotnet build src/KnockOff.sln`: 0 errors, 0 warnings
- `dotnet test src/KnockOff.sln` (--no-build):
  - KnockOffTests: 1253 passed (net9.0, net10.0), 1252 passed (net8.0), 0 failed
  - KnockOffTests.AssemblyStrict: 14 passed x 3 frameworks, 0 failed
  - KnockOff.Documentation.Samples: 571 passed x 3 frameworks, 0 failed
  - KnockOff.NeatooInterfaceTests: 473 passed x 3 frameworks, 0 failed
- `dotnet build src/Design/Design.Stubs`: 0 errors, 0 warnings
- `dotnet test src/Design/Design.Tests`: 301 passed x 3 frameworks, 0 failed

### Production Code Review

- **MethodInterceptorRenderer.cs** is the only production file changed
- Line 170: `var entryPointName = model.IsVoid ? "Call" : "Return"` -- unchanged, correct for main entry point
- Line 300: Comment updated from `Call(Action<...>)` to `Return(Action<...>)`
- Line 305: `Call` renamed to `Return` for single-signature simplified void callback
- Line 534: Comment updated from `Call(Action<...>)` to `Return(Action<...>)`
- Line 539: `Call` renamed to `Return` for overload group simplified void callback
- Line 2645: EmitSequenceElevation XML doc updated
- Line 2676: Branch 1 comment updated
- Line 2734: Branch 4 comment updated
- No model, builder, or library files were changed (confirmed via git diff)

### Design Match

- Renderer changes match the plan exactly (2 functional line changes + 6 comment updates)
- Task/ValueTask methods now generate `Return(Action<...>)` instead of `Call(Action<...>)`
- Truly void methods continue to generate `Call` -- verified by grepping: no `Async.Call(` patterns remain, all remaining `.Call(` are on void methods (DoSomething, Dispose, Save, etc.)

### Test and Documentation Spot-Checks

- `AsyncCallbackSimplificationTests.cs`: Zero `.Call(` usages remain; all async methods now use `.Return(`
- `Design.Tests/AsyncConsistencyTests.cs` lines 67 and 120: Both use `.Return(` on SaveAsync
- `Design.Stubs/BasicMethods.cs` line 298: Uses `.Return(` on SaveDataAsync
- Hand-written docs updated: `methods.md:486` and `api-consistency-matrix.md:372`
- Auto-generated docs updated: `getting-started.md:210` shows `Return`

### Additional Finding (Acceptable)

Developer discovered one file not in the original plan: `IRuleManagerTests.cs` line 129. This was an async method using `.Call(` that the plan missed. Correctly updated. No concerns.
