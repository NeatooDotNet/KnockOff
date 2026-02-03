# User Method Overload Fix

**Date:** 2026-02-02
**Related Todo:** [User Method Overload Generator Bug](../todos/user-method-overload-bug.md)
**Status:** Complete
**Last Updated:** 2026-02-02

---

## Overview

Fix the generator to correctly handle user method overloads by generating per-signature `RecordCall` methods, consistent with how regular method overloads work.

---

## Approach

**Recommended: Option 1 - Generate RecordCall overloads**

Generate multiple `RecordCall` methods on the user method interceptor, one per interface overload. This is consistent with how regular method overloads are handled and maintains the full tracking API.

---

## Root Cause Analysis

### The Bug

When an interface has overloaded methods and the user provides user methods for multiple overloads, the generator:

1. **Filters user methods OUT of method groups** (FlatModelBuilder.cs line 43):
   ```csharp
   .Where(m => !m.IsGenericMethod && m.UserMethodCall == null)
   ```

2. **Renders user method interceptors individually** (FlatRenderer.cs line 118-122):
   ```csharp
   foreach (var method in unit.Methods.Where(m => !m.IsGenericMethod && m.UserMethodCall != null))
   {
       if (renderedInterceptorClasses.Add(method.InterceptorClassName))
           RenderUserMethodInterceptorClass(w, method);
   }
   ```

3. **Deduplicates by `InterceptorClassName`** - When `Format(string)`, `Format(string, bool)`, and `Format(string, bool, int)` all have user methods, they all share `InterceptorClassName = "Format2Interceptor"`. Only the FIRST method's interceptor gets rendered.

4. **All interface implementations call the same interceptor** - But they pass different argument shapes:
   ```csharp
   // Generated (buggy):
   string IFormatter.Format(string input) {
       Format2.RecordCall(input);  // OK - matches single-param RecordCall
   }
   string IFormatter.Format(string input, bool uppercase) {
       Format2.RecordCall((input, uppercase));  // ERROR: No matching overload!
   }
   ```

### How Regular Overloads Work (Correctly)

Regular (non-user-method) overloads:
1. Are grouped via `FlatMethodGroup`
2. Get rendered by `RenderOverloadGroupContent` in `MethodInterceptorRenderer.cs`
3. Generate per-signature:
   - Delegates (`ProcessDelegate_String_void`, `ProcessDelegate_String_Int32_void`)
   - Storage (`_onCall_String_void`, `_onCall_String_Int32_void`)
   - RecordCall methods (different signatures)
   - Builder classes (`MethodCallBuilderImpl_String_void`)
4. Each signature gets proper tracking

---

## Design

### Solution: Group User Method Overloads

Instead of rendering user method interceptors individually, group them like regular overloads and generate per-signature `RecordCall` methods.

**Key Files Affected:**

| File | Change |
|------|--------|
| `src/Generator/Builder/FlatModelBuilder.cs` | Build user method overload groups |
| `src/Generator/Renderer/FlatRenderer.cs` | Render user method groups with per-signature RecordCall |
| `src/Generator/Model/Flat/FlatMethodGroup.cs` | May need extension for user method context |

### Generated Code Pattern

For interface:
```csharp
public interface IFormatter
{
    string Format(string input);
    string Format(string input, bool uppercase);
    string Format(string input, bool uppercase, int maxLength);
}
```

With user methods for all overloads:
```csharp
[KnockOff]
public partial class FormatterStub : IFormatter { }

public partial class FormatterStub
{
    protected string Format(string input) => input.ToUpper();
    protected string Format(string input, bool uppercase) => uppercase ? input.ToUpper() : input;
    protected string Format(string input, bool uppercase, int maxLength) => input[..maxLength];
}
```

**Current (Buggy) Generated Code:**
```csharp
// Only one RecordCall signature - BROKEN
public sealed class Format2Interceptor
{
    private int _callCount;
    private string _lastArg = default!;

    internal void RecordCall(string input) { _callCount++; _lastArg = input; }
    // ...
}

string IFormatter.Format(string input)
{
    Format2.RecordCall(input);  // OK
    return Format(input);
}

string IFormatter.Format(string input, bool uppercase)
{
    Format2.RecordCall((input, uppercase));  // ERROR!
    return Format(input, uppercase);
}
```

**Proposed Generated Code:**
```csharp
// Per-signature RecordCall methods
public sealed class Format2Interceptor
{
    // Aggregate tracking (combined across all overloads)
    private int _callCount;

    // Per-signature tracking
    private string _lastArg_String = default!;
    private (string input, bool uppercase) _lastArgs_String_Boolean;
    private (string input, bool uppercase, int maxLength) _lastArgs_String_Boolean_Int32;

    // RecordCall overloads
    internal void RecordCall(string input)
    {
        _callCount++;
        _lastArg_String = input;
    }
    internal void RecordCall(string input, bool uppercase)
    {
        _callCount++;
        _lastArgs_String_Boolean = (input, uppercase);
    }
    internal void RecordCall(string input, bool uppercase, int maxLength)
    {
        _callCount++;
        _lastArgs_String_Boolean_Int32 = (input, uppercase, maxLength);
    }

    // OnCall per signature (supersedes user method when configured)
    public IMethodTracking OnCall(Func<string, string> callback) { ... }
    public IMethodTracking OnCall(Func<string, bool, string> callback) { ... }
    public IMethodTracking OnCall(Func<string, bool, int, string> callback) { ... }

    // Aggregate verification
    public void Verify() => Verify(Times.AtLeastOnce);
    public void Verify(Times times) { ... }

    // ...
}
```

### API Surface Changes

**Tracking Properties:**

For overloaded user methods, the generator must handle LastArg/LastArgs differently since each signature has different types:

| Scenario | Current API | Proposed API |
|----------|-------------|--------------|
| Single signature, 1 param | `stub.Method2.LastArg` | No change |
| Single signature, N params | `stub.Method2.LastArgs` | No change |
| Multiple signatures | (broken) | Per-signature: `stub.Method2.LastArgs_String_Boolean` |

**Alternative Consideration:** For simplicity, aggregate tracking could be call-count-only when overloaded, with no LastArg/LastArgs. This is simpler but loses argument capture.

**Recommendation:** Generate per-signature `LastArg_*`/`LastArgs_*` properties with signature suffixes. This matches the regular overload pattern's `MethodCallBuilderImpl_*` classes.

### OnCall/Returns Behavior

OnCall/Returns on user method interceptors allows per-test overrides of user methods:
- Each signature gets its own OnCall method
- When OnCall is configured for a signature, it supersedes the user method
- This matches regular overload behavior

---

## Implementation Steps

### Phase 1: Build User Method Groups Using FlatMethodGroup

**Decision:** Reuse `FlatMethodGroup` for user method overload grouping.

1. **Modify FlatModelBuilder.cs**:
   - Add a second grouping operation for user method overloads (separate from regular method groups)
   - Create `FlatMethodGroup` instances for user method groups (same model type, different source data)
   - Group by `InterceptorName` (e.g., `Format2`) to collect all overloaded user methods together
   - Include signature suffix calculation for each user method overload

2. **Update FlatGenerationUnit**:
   - Add `UserMethodGroups` collection of type `EquatableArray<FlatMethodGroup>`
   - Keep separate from `MethodGroups` (see architectural decision below)

**Why Reuse FlatMethodGroup:**
- `FlatMethodGroup` already captures the essential structure: `InterceptorName`, `InterceptorClassName`, `NeedsNewKeyword`, and `Methods` collection
- User method groups need the same data: a group of methods sharing an interceptor name
- Enables potential reuse of signature suffix logic via `GetSignatureSuffix()`
- No new model type needed, reducing complexity

### Phase 2: Render User Method Overload Interceptors

1. **Modify FlatRenderer.cs**:
   - Replace individual user method interceptor rendering with group rendering
   - Generate per-signature `RecordCall` methods
   - Generate per-signature `_lastArg_*` / `_lastArgs_*` storage
   - Generate per-signature OnCall methods
   - Keep aggregate `_callCount` and `Verify()` methods

2. **Update interface implementation rendering**:
   - Use signature suffix when calling `RecordCall`

### Phase 3: Handle Partial User Method Coverage

When only SOME overloads have user methods (e.g., user method for `Format(string)` but not `Format(string, bool)`):

- Non-user-method overloads use regular interceptor (no "2" suffix)
- User-method overloads use *2 interceptor
- Each has separate tracking

This is currently already the intended behavior - no changes needed if implementation is correct.

### Phase 4: Update Verify Method Rendering (RenderVerifyMethods)

**Architectural Decision: Keep User Method Groups Separate**

User method groups remain in `unit.UserMethodGroups`, NOT merged into `unit.MethodGroups`. The existing separate iteration in `RenderVerifyMethods` is correct and should continue.

**Rationale:**

1. **Different semantics**: Regular groups use `MethodInterceptorRenderer.RenderInterceptorClass` with full OnCall API. User method groups use `RenderUserMethodInterceptorClass` with user-method-first semantics (callback supersedes user method).

2. **Different VerifyAll behavior**: The comment at line 2316 of FlatRenderer.cs states: "User-defined methods are NOT included in VerifyAll because they are always 'configured'". Merging would require conditional logic in VerifyAll.

3. **No duplicate variable issue**: User methods use the "2" suffix (`Format2`), regular methods do not (`Format`). When `RenderVerifyMethods` iterates both:
   - `methodInterceptorNames` contains `Format` (from MethodGroups)
   - `userMethodInterceptorNames` contains `Format2` (from UserMethodGroups)
   - Generated variables: `formatFailure` and `format2Failure` - no collision

4. **Current code structure already handles this**: Lines 2280-2307 of FlatRenderer.cs iterate `methodInterceptorNames` and `userMethodInterceptorNames` separately and correctly.

**Implementation:**

The CheckVerification method on user method interceptors remains unchanged - it uses aggregate `_callCount`:
```csharp
internal VerificationFailure? CheckVerification()
{
    if (!_isVerifiable) return null;
    var times = _verifiableTimes ?? Times.AtLeastOnce;
    // Aggregate call count across all signatures
    if (!times.Validate(_callCount))
        return new VerificationFailure("Format", times, _callCount);
    return null;
}
```

**Changes to userMethodInterceptorNames extraction:**

Currently (line 2248-2252):
```csharp
var userMethodInterceptorNames = unit.Methods
    .Where(m => !m.IsGenericMethod && m.UserMethodCall != null)
    .Select(m => m.InterceptorName)
    .Distinct()
    .ToList();
```

Updated to use UserMethodGroups:
```csharp
var userMethodInterceptorNames = unit.UserMethodGroups
    .Select(g => g.InterceptorName)
    .Distinct()
    .ToList();
```

This is cleaner and consistent with how `methodInterceptorNames` is derived from `unit.MethodGroups`.

### Phase 5: Generic User Method Overloads

**Scope:** Generic user method overloads are IN SCOPE for this fix.

**Background:**

Generic methods in KnockOff use the `Of<T>()` pattern. For example:
```csharp
public interface IService { T Create<T>() where T : new(); }

// Generated interceptor:
stub.Create.Of<List<int>>().Returns(new List<int>());
```

Generic user methods follow the same pattern:
```csharp
[KnockOff]
public partial class ServiceStub : IService { }

public partial class ServiceStub
{
    protected T Create<T>() where T : new() => new T();
}

// Generated: stub.Create2.Of<T>() - tracking-only with Of<T>() pattern
```

**Design for Generic User Method Overloads:**

When an interface has overloaded generic user methods:
```csharp
public interface IProcessor
{
    T Process<T>(T input);
    T Process<T>(T input, string options);
    TOut Process<TIn, TOut>(TIn input);
}
```

With user methods:
```csharp
protected T Process<T>(T input) => input;
protected T Process<T>(T input, string options) => input;
protected TOut Process<TIn, TOut>(TIn input) => default!;
```

**Generated Pattern:**

Generic user method overloads use a COMBINATION of:
1. `Of<T>()` pattern for type parameter resolution
2. Per-signature `RecordCall` overloads within each typed handler

```csharp
public sealed class Process2Interceptor  // Generic user method container
{
    private readonly Dictionary<TypeKey, object> _typedHandlers = new();

    // Of<T>() returns a typed handler that knows about ALL overloads for that type instantiation
    public Process2TypedHandler<T> Of<T>()
    {
        var key = typeof(T);
        if (!_typedHandlers.TryGetValue(key, out var handler))
        {
            handler = new Process2TypedHandler<T>();
            _typedHandlers[key] = handler;
        }
        return (Process2TypedHandler<T>)handler;
    }

    // For multi-type-parameter overloads
    public Process2TypedHandler<TIn, TOut> Of<TIn, TOut>()
    {
        var key = (typeof(TIn), typeof(TOut));
        if (!_typedHandlers.TryGetValue(key, out var handler))
        {
            handler = new Process2TypedHandler<TIn, TOut>();
            _typedHandlers[key] = handler;
        }
        return (Process2TypedHandler<TIn, TOut>)handler;
    }
}

// Typed handler with per-signature RecordCall
public sealed class Process2TypedHandler<T>
{
    private int _callCount;
    private T _lastArg_T = default!;
    private (T input, string options) _lastArgs_T_String;

    internal void RecordCall(T input) { _callCount++; _lastArg_T = input; }
    internal void RecordCall(T input, string options) { _callCount++; _lastArgs_T_String = (input, options); }

    public IMethodTracking OnCall(Func<T, T> callback) { ... }
    public IMethodTracking OnCall(Func<T, string, T> callback) { ... }

    public void Verify(Times times) { ... }
}
```

**Key Points:**

1. **Signature suffix for generics**: Uses type parameter names as suffix components: `_T`, `_T_String`, `_TIn_TOut`

2. **Multiple Of<>() methods**: Different type parameter arities get different `Of<>()` methods (same as current generic method handling)

3. **Tracking per type instantiation, per signature**: `stub.Process2.Of<string>().Verify()` tracks all calls to any signature with `T=string`

4. **LastArg within typed handler**: Per-signature LastArg/LastArgs properties within typed handlers, not on container

**Implementation Notes:**

- Extend `FlatGenericMethodHandlerModel` or create `FlatGenericUserMethodHandlerModel`
- Group generic user methods by (type parameter count, type parameter names) first, then by signature
- `RenderGenericMethodHandler` needs user method variant with per-signature RecordCall

---

## Acceptance Criteria

**Non-Generic User Method Overloads:**
- [ ] `FormatterStub` compiles with user methods for all `Format` overloads
- [ ] Each overload's arguments are tracked correctly via per-signature `RecordCall`
- [ ] `stub.Format2.Verify(Times.Exactly(3))` counts calls across all overloads
- [ ] Per-signature OnCall/Returns supersedes user method for that signature only
- [ ] Partial coverage (some overloads with user methods, some without) works correctly
- [ ] No duplicate variable names in Verify method generation

**Generic User Method Overloads:**
- [ ] Generic user method overloads compile (e.g., `Process<T>(T)` and `Process<T>(T, string)`)
- [ ] `stub.Process2.Of<string>()` returns typed handler with per-signature RecordCall
- [ ] Multi-type-parameter overloads work: `stub.Process2.Of<string, int>()` for `Process<TIn, TOut>`
- [ ] Per-signature OnCall within typed handlers supersedes user method
- [ ] Aggregate Verify across all type instantiations works

---

## Architectural Verification

**Verification Checklist:**
- [x] All three patterns analyzed (Standalone, Inline Interface, Inline Class)
- [x] Breaking changes assessment completed
- [x] Pattern consistency verified
- [x] Diagnostic requirements identified (none needed - this is bug fix)
- [x] Test strategy defined (enable existing disabled tests in Design.Stubs)
- [x] Edge cases documented (partial coverage, generic overloads)
- [x] Codebase deep-dive completed

**Three Patterns Analysis:**
- **Standalone:** Fully affected - this is where user methods exist
- **Inline Interface:** N/A - Inline stubs generate the entire class; user methods cannot be added. User methods are fundamentally a standalone-only feature.
- **Inline Class:** N/A - Same reason as inline interface
- **Inline Delegate:** N/A - Delegates have single invocation signature; no overloads possible.

**Breaking Changes:** No - This is a bug fix. Currently broken code will start compiling. No working code changes behavior.

**Pattern Consistency:**
- Reuses `FlatMethodGroup` model for user method groups (per user clarification)
- Follows regular overload pattern from `MethodInterceptorRenderer.RenderOverloadGroupContent`
- Uses same signature suffix strategy (`_String_Boolean_Int32`)
- Maintains OnCall supersedes user method semantics
- Keeps user method groups separate from regular method groups in `RenderVerifyMethods`

**Key Architectural Decisions:**

1. **Reuse FlatMethodGroup**: User method groups use the existing `FlatMethodGroup` record type. No new model needed.

2. **Separate UserMethodGroups collection**: Add `UserMethodGroups: EquatableArray<FlatMethodGroup>` to `FlatGenerationUnit`. Do NOT merge into `MethodGroups` because:
   - Different rendering logic (user method interceptors vs regular interceptors)
   - Different VerifyAll semantics (user methods excluded from VerifyAll)
   - No collision risk due to "2" suffix on user method interceptor names

3. **Generic user method overloads**: Use combination of `Of<T>()` pattern with per-signature RecordCall within typed handlers. Follows existing generic method architecture.

**Codebase Analysis:**

Files Examined (Updated):
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Builder/FlatModelBuilder.cs` - Method grouping logic (lines 41-50, 139-177)
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/FlatRenderer.cs` - User method interceptor rendering (lines 117-122, 1543-1767) and RenderVerifyMethods (lines 2239-2352)
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Regular overload handling
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Model/Flat/FlatMethodGroup.cs` - Method group model (simple record with InterceptorName, InterceptorClassName, NeedsNewKeyword, Methods)
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Model/Flat/FlatGenerationUnit.cs` - Generation unit with MethodGroups collection
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs` - Generic method handler model for Of<T>() pattern
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Model/Shared/MethodOverloadSignature.cs` - Per-signature model
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` - Unified model with Overloads collection
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/KnockOffGenerator.Helpers.cs` - User method signature matching (GetMethodSignature)
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` - Bug reproduction, design patterns, and disabled test code
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Domain/Services/IUserMethodService.cs` - Test interfaces

Patterns Found:
- Regular overloads use `MethodOverloadSignature` with `SignatureSuffix` for per-signature disambiguation
- `UnifiedMethodInterceptorModel.Overloads` collection enables overload-aware rendering
- Signature suffix follows pattern: `GetTypeSuffix(type)` -> "String", "Int32", "Boolean", etc.
- `RecordCall` argument building uses `UnifiedInterceptorBuilder.BuildTrackingArgs()`
- Generic methods use `Of<T>()` pattern with dictionary-based typed handler storage
- RenderVerifyMethods iterates `methodInterceptorNames` and `userMethodInterceptorNames` separately - variable names use lowercased interceptor name, so "Format" -> "formatFailure" and "Format2" -> "format2Failure" (no collision)

---

## Dependencies

- None external
- Requires understanding of existing overload handling in `MethodInterceptorRenderer.cs`

---

## Risks / Considerations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| API complexity with per-signature LastArgs | Medium | Low | Clear documentation; aggregate Verify() still works |
| Signature suffix collision | Low | Medium | Use same suffix strategy as regular overloads |
| Generic user method overload complexity | Medium | Medium | Leverage existing `Of<T>()` infrastructure; typed handlers encapsulate per-signature logic |
| Multiple `Of<>()` methods for different arities | Low | Low | Follow existing generic method pattern in `RenderGenericMethodHandler` |
| FlatMethodGroup reuse limitations | Low | Low | `FlatMethodGroup` has all required fields; can always extend if needed later |

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-02

### Review Summary

**Files Examined:**
- `src/Generator/Model/Flat/FlatMethodGroup.cs` - Confirmed simple record with required fields
- `src/Generator/Model/Flat/FlatGenerationUnit.cs` - Current structure, will add UserMethodGroups
- `src/Generator/Builder/FlatModelBuilder.cs` - Bug source identified at lines 41-50
- `src/Generator/Renderer/FlatRenderer.cs` - User method rendering at lines 117-122, RenderVerifyMethods at lines 2239-2352
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Regular overload pattern at lines 372-600+
- `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` - Disabled tests and bug documentation

**Questions Checked:** 20 of 20 (all completeness, correctness, clarity, and risk questions)

**Devil's Advocate Items:** 3 edge cases identified (ref/out, optional params, explicit impl naming), all already handled by existing infrastructure

### Why This Plan Is Approved

1. **Root cause analysis is thorough** - Exact lines of buggy code identified with before/after examples
2. **Pattern consistency** - Reuses existing `FlatMethodGroup` and follows established overload pattern from `MethodInterceptorRenderer.RenderOverloadGroupContent`
3. **All three previous concerns addressed** with detailed architectural decisions
4. **Generated code examples** show exactly what will be emitted
5. **Phased implementation** allows incremental verification

**Original Concerns (All Resolved):**

1. **Generic User Method Overloads** - IN SCOPE with complete design (Phase 5)
2. **FlatMethodGroup Reuse** - Confirmed: reuse existing model type
3. **RenderVerifyMethods Integration** - Keep separate with documented rationale

---

## Implementation Contract

**Created:** 2026-02-02
**Approved by:** knockoff-developer

### In Scope

**Phase 1: Build User Method Groups Using FlatMethodGroup** (COMPLETE)
- [x] Add `UserMethodGroups: EquatableArray<FlatMethodGroup>` to `FlatGenerationUnit.cs`
- [x] Modify `FlatModelBuilder.cs` to create user method groups by grouping methods where `UserMethodCall != null` by `InterceptorName`
- [x] **Checkpoint:** Build succeeds

**Phase 2: Render User Method Overload Interceptors** (COMPLETE)
- [x] Create `RenderUserMethodGroupInterceptorClass` in `FlatRenderer.cs` (or modify `RenderUserMethodInterceptorClass`)
- [x] Generate per-signature `RecordCall` methods
- [x] Generate per-signature `_lastArg_{Suffix}` / `_lastArgs_{Suffix}` storage
- [x] Generate per-signature `LastArg_{Suffix}` / `LastArgs_{Suffix}` properties
- [x] Generate per-signature `OnCall` methods (supersedes user method for that signature)
- [x] Keep aggregate `_callCount` and `Verify()` methods
- [x] Update interface implementation rendering to use correct `RecordCall` signature
- [x] Replace individual user method interceptor loop with group rendering
- [x] **Checkpoint:** Build succeeds, basic compilation test passes

**Phase 3: Partial User Method Coverage (Validation)** (COMPLETE)
- [x] Verify partial coverage scenario works (some overloads with user methods, some without)
- [x] **Checkpoint:** Design.Stubs compiles with partial coverage stub

**Phase 4: Update Verify Method Rendering** (COMPLETE)
- [x] Update `RenderVerifyMethods` to derive `userMethodInterceptorNames` from `unit.UserMethodGroups.Select(g => g.InterceptorName).Distinct()`
- [x] **Checkpoint:** All existing tests pass

**Phase 5: Generic User Method Overloads** (COMPLETE)
- [x] Extend generic user method handling to support per-signature `RecordCall` within typed handlers
- [x] Generate `Of<T>()` typed handlers with per-signature `RecordCall`, `LastArg_*`/`LastArgs_*`, and `OnCall`
- [x] **Checkpoint:** Generic user method overload tests pass

**Phase 6: Enable Tests and Cleanup** (COMPLETE)
- [x] Enable `ENABLE_USER_METHOD_OVERLOAD_TESTS` tests in `UserMethodBasics.cs`
- [x] Enable `PartialOverloadUserMethodStub` (partial user method coverage)
- [x] Enable `ENABLE_GENERIC_USER_METHOD_OVERLOAD_TESTS` in `UserMethodBasics.cs`
- [x] Remove all preprocessor directives related to user method overload tests
- [x] **Checkpoint:** All tests pass, `dotnet test` succeeds
- [x] Move todo and plan to `completed/` directories

### Explicitly Out of Scope

- **Inline patterns** - User methods fundamentally require standalone pattern
- **Inline delegates** - Single invocation signature, no overloads possible
- **Per-type-instantiation LastArgs** - Generic tracking remains at type instantiation level

### Verification Gates

1. After Phase 2: `OverloadedUserMethodStub` compiles without errors
2. After Phase 4: All existing tests pass (no regression)
3. After Phase 5: Generic user method overload tests pass
4. Final: `dotnet test` passes, generated code samples in completion evidence

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (test not related to user method overloads)
- Architectural contradiction discovered (e.g., FlatMethodGroup insufficient)
- Generated code does not compile after Phase 2
- Generic method handler pattern doesn't accommodate per-signature RecordCall

---

## Implementation Progress

### Phase 1: Build User Method Groups Using FlatMethodGroup (Complete)

**Completed:** 2026-02-02

**Changes Made:**

1. **FlatGenerationUnit.cs** - Added `UserMethodGroups: EquatableArray<FlatMethodGroup>` property
   - Located between `MethodGroups` and `GenericMethodHandlers` in the record definition
   - Includes XML documentation: "User method groups for tracking-only interceptors (groups user method overloads by name)."

2. **FlatModelBuilder.cs** - Added user method grouping logic
   - Added grouping code after regular method groups (lines 51-61)
   - Groups methods where `UserMethodCall != null` by `InterceptorName`
   - Creates `FlatMethodGroup` instances with same structure as regular method groups
   - Updated return statement to include `UserMethodGroups` property

**Verification:**
- [x] Build succeeds: `dotnet build src/KnockOff.sln` - 0 errors, 0 warnings
- [x] All existing tests pass: 5,647 tests across all test projects
- [x] `FlatGenerationUnit` has new `UserMethodGroups` property
- [x] User methods are grouped correctly by `InterceptorName`

**Test Results (Phase 1 Checkpoint):**
```
Passed! - Failed: 0, Passed: 14 - KnockOffTests.AssemblyStrict.dll
Passed! - Failed: 0, Passed: 404 - KnockOff.Documentation.Samples.dll (per framework)
Passed! - Failed: 0, Passed: 473 - KnockOff.NeatooInterfaceTests.dll (per framework)
Passed! - Failed: 0, Passed: 1000/1001 - KnockOffTests.dll (per framework)
```

### Phase 2: Render User Method Overload Interceptors (Complete)

**Completed:** 2026-02-02

**Changes Made:**

1. **FlatRenderer.cs** - Updated rendering loop (line ~117)
   - Changed from iterating `unit.Methods.Where(m => !m.IsGenericMethod && m.UserMethodCall != null)` to `unit.UserMethodGroups`
   - Added `multiOverloadUserMethodInterceptors` set to track user method groups with multiple unique signatures

2. **FlatRenderer.cs** - Created `RenderUserMethodGroupInterceptorClass` method
   - Handles both single-method groups (delegates to existing `RenderUserMethodInterceptorClass`) and multi-overload groups
   - For multi-overload groups generates:
     - Per-signature delegates with suffixed names (e.g., `FormatDelegate_String_String`, `FormatDelegate_String_Boolean_String`)
     - Per-signature `RecordCall_{suffix}` methods
     - Per-signature `_lastArg_{suffix}` / `_lastArgs_{suffix}` storage and properties
     - Per-signature `OnCall_{suffix}` and `Returns_{suffix}` methods
     - Per-signature `Callback_{suffix}` properties (internal)
     - Aggregate `_callCount`, `Verify()`, and `Verifiable()` methods

3. **FlatRenderer.cs** - Updated `RenderMethodImplementation` signature
   - Added `multiOverloadUserMethodInterceptors` parameter
   - Passes to `RenderUserMethodImplementation`

4. **FlatRenderer.cs** - Updated `RenderUserMethodImplementation`
   - Now accepts `multiOverloadUserMethodInterceptors` parameter
   - Generates suffixed `RecordCall_{suffix}` and `Callback_{suffix}` calls when method is part of multi-overload group

5. **FlatRenderer.cs** - Updated `RenderVerifyMethods` (Phase 4)
   - Changed `userMethodInterceptorNames` to derive from `unit.UserMethodGroups.Select(g => g.InterceptorName)`
   - Updated `hasUserMethods` check to use `unit.UserMethodGroups.Count > 0`

6. **Design.Stubs/UserMethods/UserMethodBasics.cs**
   - Enabled `OverloadedUserMethodStub` (removed `#if ENABLE_USER_METHOD_OVERLOAD_TESTS`)
   - Added `#pragma warning disable CA1062` for test stub methods

**Verification:**
- [x] Build succeeds: `dotnet build src/KnockOff.sln` - 0 errors, 0 warnings
- [x] Design.Stubs compiles with `OverloadedUserMethodStub` enabled
- [x] All existing tests pass: 5,647 tests across all test projects
- [x] Design.Tests pass: 141 tests
- [x] Generated code compiles for overloaded user methods

**Test Results (Phase 2 Checkpoint):**
```
Passed! - Failed: 0, Passed: 14 - KnockOffTests.AssemblyStrict.dll (per framework)
Passed! - Failed: 0, Passed: 404 - KnockOff.Documentation.Samples.dll (per framework)
Passed! - Failed: 0, Passed: 473 - KnockOff.NeatooInterfaceTests.dll (per framework)
Passed! - Failed: 0, Passed: 1000/1001 - KnockOffTests.dll (per framework)
Passed! - Failed: 0, Passed: 141 - Design.Tests.dll (per framework)
```

---

## Completion Evidence

### Phase 2 Generated Code Sample

**For `OverloadedUserMethodStub` implementing `IOverloadedUserMethodService`:**

**Interceptor Class (per-signature support):**
```csharp
public sealed class Format2Interceptor : global::KnockOff.IMethodTracking
{
    private int _callCount;  // Aggregate across all overloads

    // Per-signature delegates
    public delegate string FormatDelegate_String_String(string input);
    public delegate string FormatDelegate_String_Boolean_String(string input, bool uppercase);
    public delegate string FormatDelegate_String_Boolean_Int32_String(string input, bool uppercase, int maxLength);

    // Per-signature storage
    private FormatDelegate_String_String? _onCall_String_String;
    private string _lastArg_String_String = default!;
    // ... (similar for other signatures)

    // Per-signature RecordCall
    internal void RecordCall_String_String(string input) { _callCount++; _lastArg_String_String = input; }
    internal void RecordCall_String_Boolean_String((string? input, bool? uppercase) args) { ... }
    internal void RecordCall_String_Boolean_Int32_String((string? input, bool? uppercase, int? maxLength) args) { ... }

    // Per-signature OnCall/Returns
    public IMethodTracking OnCall_String_String(FormatDelegate_String_String callback) { ... }
    public IMethodTracking Returns_String_String(string value) => OnCall_String_String(_ => value);
    // ... (similar for other signatures)

    // Aggregate Verify()
    public void Verify() => Verify(Times.AtLeastOnce);
    public void Verify(Times times) { if (!times.Validate(_callCount)) throw ...; }
}
```

**Interface Implementation:**
```csharp
string IOverloadedUserMethodService.Format(string input)
{
    Format2.RecordCall_String_String(input);
    if (Format2.Callback_String_String is { } callback) return callback(input);
    return Format(input);  // User method fallback
}

string IOverloadedUserMethodService.Format(string input, bool uppercase)
{
    Format2.RecordCall_String_Boolean_String((input, uppercase));
    if (Format2.Callback_String_Boolean_String is { } callback) return callback(input, uppercase);
    return Format(input, uppercase);  // User method fallback
}
```

### Phase 3: Partial User Method Coverage (Complete)

**Completed:** 2026-02-02

**Problem Discovered:**

When partial user method coverage exists (e.g., `Format(string)` has user method but `Format(string, bool)` does not), the original implementation:
1. Reserved the method name `Format` for user methods
2. Assigned ALL `Format` overloads to `Format2` interceptor
3. Generated conflicting interceptor styles (user method style vs regular OnCall style) in the same class
4. Caused duplicate variable names in `Verify()` method and missing `RecordCall`/`Callback` methods

**Fix Applied:**

Modified `FlatModelBuilder.BuildNameMap()` to handle partial user method coverage:

1. **Added signature-based user method lookup** (`BuildUserMethodLookup`, `BuildMethodSignatureKey`, `HasMatchingUserMethod`)
   - Builds lookup table keyed by method signature (name + params + return type)
   - Allows checking if specific overload has matching user method

2. **Added `AssignNamesForOverloadGroup` helper**
   - Splits overloads by whether they have matching user methods
   - Overloads WITH user methods share one interceptor name
   - Overloads WITHOUT user methods share a different interceptor name

**Generated Code Pattern (Partial Coverage):**

For `PartialOverloadUserMethodStub` implementing `IOverloadedUserMethodService` where only `Format(string)` has a user method:

```csharp
// User method interceptor (tracking-only) for Format(string)
public sealed class Format2Interceptor : IMethodTracking<string>
{
    internal void RecordCall(string input) { ... }
    internal ProcessDelegate? Callback => _onCall;
    // ...
}

// Regular interceptor (OnCall API) for Format(string, bool) and Format(string, bool, int)
public sealed class Format3Interceptor
{
    public MethodCallBuilderImpl_String_Boolean_String OnCall(FormatDelegate_String_Boolean_String callback) { ... }
    internal string Invoke_String_Boolean_String(bool strict, ...) { ... }
    // ...
}

// Properties
public Format2Interceptor Format2 { get; } = new();  // User method overload
public Format3Interceptor Format3 { get; } = new();  // Regular overloads

// Interface implementations
string IOverloadedUserMethodService.Format(string input)
{
    Format2.RecordCall(input);  // Uses Format2 (user method pattern)
    if (Format2.Callback is { } callback) return callback(input);
    return Format(input);
}

string IOverloadedUserMethodService.Format(string input, bool uppercase)
{
    return Format3.Invoke_String_Boolean_String(Strict, input, uppercase);  // Uses Format3 (regular pattern)
}
```

**Verification:**
- [x] Build succeeds: `dotnet build src/KnockOff.sln` - 0 errors, 0 warnings
- [x] Design.Stubs compiles with `PartialOverloadUserMethodStub` enabled
- [x] All existing tests pass: 5,647 tests across all test projects
- [x] Design.Tests pass: 141 tests

**Test Results (Phase 3 Checkpoint):**
```
Passed! - Failed: 0, Passed: 404 - KnockOff.Documentation.Samples.dll (per framework)
Passed! - Failed: 0, Passed: 473 - KnockOff.NeatooInterfaceTests.dll (per framework)
Passed! - Failed: 0, Passed: 1000/1001 - KnockOffTests.dll (per framework)
Passed! - Failed: 0, Passed: 141 - Design.Tests.dll (per framework)
```

### Remaining Phases

- **Phase 4:** Update RenderVerifyMethods - COMPLETE (done as part of Phase 2)
- **Phase 5:** Generic user method overloads - COMPLETE
- **Phase 6:** Enable tests and cleanup - Partially complete (enabled OverloadedUserMethodStub, enabled PartialOverloadUserMethodStub, enabled OverloadedGenericUserMethodStub)

### Phase 5: Generic User Method Overloads (Complete)

**Completed:** 2026-02-02

**Changes Made:**

1. **FlatGenericMethodHandlerGroup.cs** (NEW FILE)
   - Created `FlatGenericMethodHandlerGroup` record for top-level container
   - Created `FlatGenericTypeArityGroup` record for grouping by type parameter count
   - Created `FlatGenericSignatureGroup` record for per-signature details within each type arity

2. **FlatGenerationUnit.cs**
   - Added `GenericUserMethodHandlerGroups: EquatableArray<FlatGenericMethodHandlerGroup>` property

3. **FlatModelBuilder.cs**
   - Added `BuildGenericUserMethodHandlerGroups` method to create handler groups for overloaded generic user methods
   - Added `ComputeSignatureSuffixForGeneric` helper for computing signature suffixes
   - Added `HasMatchingUserMethodForOverload` helper for checking user methods against `MethodOverloadInfo`
   - Modified generic handler building to skip methods that are part of overloaded generic user method groups

4. **FlatRenderer.cs**
   - Added `RenderGenericUserMethodHandlerGroup` method for interceptor classes with multiple `Of<>()` methods
   - Added `RenderGenericUserMethodTypedHandlerClass` method for typed handlers with per-signature members
   - Added `GetGenericUserMethodSignatureSuffix` helper to find signature suffixes at runtime
   - Updated `RenderMethodImplementation` and `RenderGenericMethodImplementation` to use suffixes
   - Added `multiOverloadGenericUserMethodInterceptors` set for tracking

5. **IUserMethodService.cs**
   - Added `IOverloadedGenericUserMethodService` interface for testing

6. **UserMethodBasics.cs**
   - Added `OverloadedGenericUserMethodStub` test stub

**Generated Code Pattern (Generic User Method Overloads):**

For interface with overloaded generic user methods:
```csharp
public interface IOverloadedGenericUserMethodService
{
    T Process<T>(T input);
    T Process<T>(T input, string options);
    TOut Process<TIn, TOut>(TIn input);
}
```

Generated interceptor:
```csharp
public sealed class Process2Interceptor
{
    private readonly Dictionary<Type, object> _typedHandlers = new();
    private readonly Dictionary<(Type, Type), object> _typedHandlers_2 = new();

    // Of<T>() for single type parameter overloads
    public ProcessTypedHandler<T> Of<T>()
    {
        var key = typeof(T);
        if (!_typedHandlers.TryGetValue(key, out var handler))
        {
            handler = new ProcessTypedHandler<T>();
            _typedHandlers[key] = handler;
        }
        return (ProcessTypedHandler<T>)handler;
    }

    // Of<TIn, TOut>() for two type parameter overloads
    public ProcessTypedHandler2<TIn, TOut> Of<TIn, TOut>()
    {
        var key = (typeof(TIn), typeof(TOut));
        if (!_typedHandlers_2.TryGetValue(key, out var handler))
        {
            handler = new ProcessTypedHandler2<TIn, TOut>();
            _typedHandlers_2[key] = handler;
        }
        return (ProcessTypedHandler2<TIn, TOut>)handler;
    }

    // Aggregate verify across all type instantiations
    public void Verify(Times times) { ... }

    // Typed handler for T (handles Process<T>(T) and Process<T>(T, string))
    public sealed class ProcessTypedHandler<T> : IGenericMethodCallTracker, IResettable, IMethodTracking
    {
        public delegate T ProcessDelegate_T(T input);
        public delegate T ProcessDelegate_T_String(T input, string options);

        private ProcessDelegate_T? _onCall_T;
        private ProcessDelegate_T_String? _onCall_T_String;

        internal int _callCount;
        public T? LastCallArg_T { get; private set; }
        public (T? input, string? options)? LastCallArgs_T_String { get; private set; }

        public IMethodTracking OnCall_T(ProcessDelegate_T callback) { ... }
        public IMethodTracking OnCall_T_String(ProcessDelegate_T_String callback) { ... }

        internal ProcessDelegate_T? Callback_T => _onCall_T;
        internal ProcessDelegate_T_String? Callback_T_String => _onCall_T_String;

        internal void RecordCall_T(T? input) { _callCount++; LastCallArg_T = input; }
        internal void RecordCall_T_String(T? input, string? options) { _callCount++; LastCallArgs_T_String = (input, options); }

        public void Verify(Times times) { ... }
    }

    // Typed handler for TIn, TOut (handles Process<TIn, TOut>(TIn))
    public sealed class ProcessTypedHandler2<TIn, TOut> : IGenericMethodCallTracker, IResettable, IMethodTracking
    {
        public delegate TOut ProcessDelegate_TIn(TIn input);

        private ProcessDelegate_TIn? _onCall_TIn;

        internal int _callCount;
        public TIn? LastCallArg_TIn { get; private set; }

        public IMethodTracking OnCall_TIn(ProcessDelegate_TIn callback) { ... }
        internal ProcessDelegate_TIn? Callback_TIn => _onCall_TIn;
        internal void RecordCall_TIn(TIn? input) { _callCount++; LastCallArg_TIn = input; }

        public void Verify(Times times) { ... }
    }
}
```

Interface implementation:
```csharp
T IOverloadedGenericUserMethodService.Process<T>(T input)
{
    Process2.Of<T>().RecordCall_T(input);
    if (Process2.Of<T>().Callback_T is { } callback) return callback(input);
    if (Strict) throw StubException.NotConfigured(...);
    return Process<T>(input);
}

T IOverloadedGenericUserMethodService.Process<T>(T input, string options)
{
    Process2.Of<T>().RecordCall_T_String(input, options);
    if (Process2.Of<T>().Callback_T_String is { } callback) return callback(input, options);
    if (Strict) throw StubException.NotConfigured(...);
    return Process<T>(input, options);
}

TOut IOverloadedGenericUserMethodService.Process<TIn, TOut>(TIn input)
{
    Process2.Of<TIn, TOut>().RecordCall_TIn(input);
    if (Process2.Of<TIn, TOut>().Callback_TIn is { } callback) return callback(input);
    if (Strict) throw StubException.NotConfigured(...);
    return Process<TIn, TOut>(input);
}
```

**Verification:**
- [x] Build succeeds: `dotnet build src/KnockOff.sln` - 0 errors, 0 warnings
- [x] Generic user method overloads generate per-signature RecordCall in typed handlers
- [x] Generated code compiles
- [x] All existing tests pass: 5,647 tests across all test projects

**Test Results (Phase 5 Checkpoint):**
```
Passed! - Failed: 0, Passed: 1001 - KnockOffTests.dll (per framework)
Passed! - Failed: 0, Passed: 404 - KnockOff.Documentation.Samples.dll (per framework)
Passed! - Failed: 0, Passed: 473 - KnockOff.NeatooInterfaceTests.dll (per framework)
```

### Phase 6: Enable Tests and Cleanup (Complete)

**Completed:** 2026-02-02

**Changes Made:**

1. **UserMethodBasics.cs** - Removed `#if ENABLE_GENERIC_USER_METHOD_OVERLOAD_TESTS` preprocessor directive
   - Lines 585-627 now enabled: `OverloadedGenericUserMethodStub` and `OverloadedGenericUserMethodDemo`
   - Added comment: "Generator fix: Per-signature RecordCall methods now generated for overloaded generic user methods"

**Verification:**
- [x] Build succeeds: `dotnet build src/KnockOff.sln` - 0 errors, 0 warnings
- [x] All preprocessor directives for user method overload tests removed
- [x] All tests pass

**Test Results (Phase 6 Final Checkpoint):**
```
Passed! - Failed: 0, Passed: 14 - KnockOffTests.AssemblyStrict.dll (per framework)
Passed! - Failed: 0, Passed: 404 - KnockOff.Documentation.Samples.dll (per framework)
Passed! - Failed: 0, Passed: 473 - KnockOff.NeatooInterfaceTests.dll (per framework)
Passed! - Failed: 0, Passed: 1001 - KnockOffTests.dll (net9.0, net10.0)
Passed! - Failed: 0, Passed: 1000 - KnockOffTests.dll (net8.0)
Passed! - Failed: 0, Passed: 141 - Design.Tests.dll (per framework)
```

---

## Final Completion Evidence

**Completed:** 2026-02-02

### All Tests Pass

All test projects pass across all target frameworks (net8.0, net9.0, net10.0):
- KnockOffTests.AssemblyStrict: 14 tests
- KnockOff.Documentation.Samples: 404 tests
- KnockOff.NeatooInterfaceTests: 473 tests
- KnockOffTests: 1000-1001 tests
- Design.Tests: 141 tests

### No Disabled Test Code Remains

All preprocessor-disabled test code for user method overloads has been enabled:
- `OverloadedUserMethodStub` - enabled (Phase 2)
- `PartialOverloadUserMethodStub` - enabled (Phase 3)
- `OverloadedGenericUserMethodStub` - enabled (Phase 6)

### Generated Code Compiles

All three user method overload stubs compile successfully:
1. `OverloadedUserMethodStub` - non-generic overloads with per-signature RecordCall
2. `PartialOverloadUserMethodStub` - partial coverage with separate interceptors
3. `OverloadedGenericUserMethodStub` - generic overloads with per-signature typed handlers

### Contract Items Verified

All implementation contract items are complete:
- [x] Phase 1: FlatMethodGroup-based user method groups
- [x] Phase 2: Per-signature RecordCall rendering
- [x] Phase 3: Partial user method coverage
- [x] Phase 4: RenderVerifyMethods update
- [x] Phase 5: Generic user method overloads
- [x] Phase 6: Enable tests and cleanup
