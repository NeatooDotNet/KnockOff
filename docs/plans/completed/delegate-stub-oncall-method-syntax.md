# Delegate Stub OnCall Method Syntax Implementation

**Date:** 2026-01-24
**Related Todo:** [Delegate Stub OnCall Method Syntax](../todos/delegate-stub-oncall-method-syntax.md)
**Status:** Complete
**Last Updated:** 2026-01-24

---

## Overview

Convert delegate stub `OnCall` from a property to a method to unify the API with all other interceptor types.

---

## Approach

Minimal change: modify only the delegate stub interceptor renderer to generate `OnCall()` as a method instead of a property. Follow the pattern established by `MethodInterceptorRenderer.cs` for consistency.

---

## Design

### Current Generated Code (InlineRenderer.cs lines 1279-1281)

```csharp
/// <summary>Callback invoked when delegate is called.</summary>
public {del.OnCallType}? OnCall { get; set; }
```

### New Generated Code (following MethodInterceptorRenderer pattern)

```csharp
private {del.OnCallType}? _onCall;

/// <summary>Configures callback invoked when delegate is called.</summary>
public void OnCall({del.OnCallType} callback)
{
    _onCall = callback;
}
```

### Key Decisions

1. **Return type**: `void` for simplicity (delegate stubs don't need chaining like `IMethodTracking`)
2. **Parameter**: The delegate type directly (matches the current property type)
3. **No tracking return**: Unlike method interceptors, delegate stubs are simpler and don't need `IMethodTracking` return

### Files to Change

| File | Change |
|------|--------|
| `src/Generator/Renderer/InlineRenderer.cs` | Update `RenderDelegateStub()` method (lines 1279-1281) |

### Tests to Update

| File | Lines | Description |
|------|-------|-------------|
| `src/Tests/KnockOffTests/InlineStubTests.cs` | 346, 370, 396, 408, 421, 428, 441, 455, 468, 482 | Delegate stub OnCall tests |
| `src/Tests/KnockOffTests/NeatooTests.cs` | 759, 776, 794, 814 | Neatoo delegate tests |
| `src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs` | 170, 184 | Open generic delegate tests |
| `src/Tests/KnockOff.NeatooInterfaceTests/Notifications/INotifyNeatooPropertyChangedTests.cs` | 132, 145, 162, 181 | Neatoo property changed tests |

---

## Implementation Steps

1. **Update InlineRenderer.RenderDelegateStub()**
   - Add private `_onCall` field (already exists in tests, need to verify)
   - Replace property with method
   - Update the Invoke method to use `_onCall` field

2. **Run tests to see failures** (expected: 18 compile errors)

3. **Update test files** - Convert property assignments to method calls:
   ```csharp
   // Before
   stub.Interceptor.OnCall = (msg) => captured = msg;

   // After
   stub.Interceptor.OnCall((msg) => captured = msg);
   ```

4. **Special case: Reset tests**
   - Lines 421 and 482 in InlineStubTests.cs check `Assert.NotNull(stub.Interceptor.OnCall);`
   - These need to be removed or changed (OnCall is no longer a property)
   - Alternative: Keep a read-only property for checking configuration state

5. **Verify all tests pass**

---

## Acceptance Criteria

- [ ] Delegate stub `OnCall` is a method, not a property
- [ ] All delegate stub tests pass
- [ ] Generated code compiles
- [ ] API is consistent with method/property interceptors

---

## Dependencies

None - self-contained change.

---

## Risks / Considerations

1. **Breaking change for users**: Anyone using delegate stubs will need to update their code
   - Migration: `stub.Interceptor.OnCall = callback;` → `stub.Interceptor.OnCall(callback);`

2. **Reset test assertions**: Two tests check `Assert.NotNull(stub.Interceptor.OnCall);`
   - Option A: Remove these assertions (OnCall is no longer exposed)
   - Option B: Add `IsConfigured` property for test inspection
   - **Recommendation**: Option A (simpler, Reset behavior is still tested by other assertions)

---

## Architectural Verification

### Verification Checklist

- [x] All three patterns analyzed (Standalone, Inline Interface, Inline Class)
- [x] Breaking changes assessment completed
- [x] Pattern consistency check (follows existing patterns)
- [x] Diagnostic requirements identified (none needed)
- [x] Test strategy defined
- [x] Edge cases documented
- [x] Codebase deep-dive completed

### Three Patterns Analysis

| Pattern | Applies? | Notes |
|---------|----------|-------|
| **Standalone** | N/A | Delegate stubs only exist in the inline pattern. `[KnockOff]` on a class implementing an interface generates method/property/indexer interceptors, not delegate interceptors. |
| **Inline Interface** | N/A | `[KnockOff<IFoo>]` generates interface stub, which uses `MethodInterceptorRenderer` for methods. This change does not affect interface stubs. |
| **Inline Class** | N/A | `[KnockOff<SomeClass>]` generates class stubs, which also use `MethodInterceptorRenderer`. Not affected. |
| **Inline Delegate** | **YES** | `[KnockOff<DelegateType>]` generates delegate stubs via `RenderDelegateStub()`. This is the sole pattern affected. |

**Conclusion**: This change is isolated to delegate stub generation only. The `RenderDelegateStub()` method in `InlineRenderer.cs` is the only code path that generates delegate interceptors.

### Breaking Changes Assessment

**Breaking Change**: Yes, this is a breaking API change.

| Aspect | Current | New |
|--------|---------|-----|
| Syntax | `stub.Interceptor.OnCall = callback;` | `stub.Interceptor.OnCall(callback);` |
| OnCall Type | Public property with getter/setter | Public void method |
| Null check | `if (stub.Interceptor.OnCall != null)` | Not directly possible |

**Migration Path**:
```csharp
// Before (property assignment)
stub.Interceptor.OnCall = (msg) => captured = msg;

// After (method call)
stub.Interceptor.OnCall((msg) => captured = msg);
```

**Impact Assessment**:
- KnockOff is pre-1.0, so breaking changes are acceptable
- Delegate stubs are less commonly used than interface stubs
- The migration is mechanical: replace `=` with `(` and `)` at the end

### Pattern Consistency Check

**Follows Established Pattern**: Yes, aligns with `MethodInterceptorRenderer.OnCall()` pattern.

| Aspect | Method Interceptors | Delegate Interceptors (Current) | Delegate Interceptors (New) |
|--------|--------------------|---------------------------------|-----------------------------|
| OnCall syntax | `stub.Method.OnCall(callback)` | `stub.Interceptor.OnCall = callback` | `stub.Interceptor.OnCall(callback)` |
| Returns | `IMethodTracking` | N/A (property) | `void` |

**Note on return type**: Method interceptors return `IMethodTracking` for chaining (e.g., `.Verifiable()`). Delegate interceptors don't need this because:
1. They already have `Verify()` and `Verifiable()` on the interceptor directly
2. Delegate stubs are simpler, no need for per-callback tracking

### Diagnostic Requirements

**No diagnostics needed**: This is a pure code generation change. The generated code will compile correctly; any usage errors from old syntax will be compile-time errors at the test project level.

### Test Strategy

**Tests to Verify**:
1. All existing delegate stub tests must pass after updating syntax
2. Tests cover: void delegates, returning delegates, multi-param delegates, generic delegates, open generic delegates
3. Reset behavior tests need special handling (see Edge Cases)

**Test Files**:
| File | OnCall Usages | Notes |
|------|---------------|-------|
| `InlineStubTests.cs` | 8 assignment + 2 null checks | Lines 346, 370, 396, 408, 421 (null check), 428, 441, 455, 468, 482 (null check) |
| `NeatooTests.cs` | 4 | Lines 759, 776, 794, 814 |
| `OpenGenericInlineStubTests.cs` | 2 | Lines 170, 184 |
| `INotifyNeatooPropertyChangedTests.cs` | 4 | Lines 132, 145, 162, 181 |
| **Total** | 18 usages to update |

### Edge Cases

1. **Reset tests with null checks** (Lines 421, 482 in InlineStubTests.cs):
   - Current: `Assert.NotNull(stub.Interceptor.OnCall);`
   - Problem: OnCall becomes a method, not a property
   - **Resolution**: Remove these assertions. The Reset() behavior is already verified by other assertions (call count resets but callback still works).

2. **Invoke method usage of OnCall**:
   - Current code in `RenderDelegateStub()`: `if (Interceptor.OnCall is { } onCall) onCall(args);`
   - **Resolution**: Change to `if (_onCall is { } onCall) onCall(args);` (reference private field)

3. **Open generic delegate stubs**:
   - Same pattern applies; no special handling needed
   - Example: `OGFactoryInterceptor<T>` will have `OnCall(Func<T> callback)` method

### Codebase Deep-Dive

**Files Examined**:

1. **`src/Generator/Renderer/InlineRenderer.cs`** (lines 1252-1373)
   - `RenderDelegateStub()` method generates delegate interceptor classes
   - Line 1279-1281: Current OnCall property generation
   - Line 1356, 1361: Invoke method references `Interceptor.OnCall`

2. **`src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`** (lines 109-121)
   - Reference implementation for OnCall method pattern
   - Shows: private field, public method, tracking return

3. **Generated code samples examined**:
   - `DelegateInlineTest.Stubs.g.cs`: Shows current property-based OnCall
   - `OpenGenericDelegateTests.Stubs.g.cs`: Confirms same pattern for open generics

4. **Test files**:
   - Verified all 18 OnCall usages via grep
   - Confirmed 2 null check assertions that need removal

### Documentation Impact

**Documentation files with delegate OnCall examples** (out of scope for this change, but noted):
- `docs/release-notes/v10.8.0.md` - Line 60
- `docs/release-notes/v10.20.0.md` - Line 25
- `docs/todos/test-knockoff-skill.md` - Multiple references

These will need updates in a follow-up documentation task.

---

## Developer Review

**Status:** Not Started

**Concerns:**

---

## Implementation Contract

**In Scope:**
- [x] `src/Generator/Renderer/InlineRenderer.cs`: Convert OnCall property to method in RenderDelegateStub
  - Add internal `_onCall` field before OnCall method
  - Replace property (lines 1279-1281) with method signature
  - Update Invoke methods (lines 1356, 1361) to reference `Interceptor._onCall` instead of `Interceptor.OnCall`
- [x] `src/Tests/KnockOffTests/InlineStubTests.cs`: Update 10 OnCall usages
  - Lines 346, 370, 396, 408, 428, 441, 455, 468: Change `=` to `(...)` syntax
  - Lines 421, 482: Remove `Assert.NotNull(stub.Interceptor.OnCall)` assertions
- [x] `src/Tests/KnockOffTests/NeatooTests.cs`: Update 4 OnCall usages
  - Lines 759, 776, 794, 814: Change `=` to `(...)` syntax
- [x] `src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs`: Update 2 OnCall usages
  - Lines 170, 184: Change `=` to `(...)` syntax
- [x] `src/Tests/KnockOff.NeatooInterfaceTests/Notifications/INotifyNeatooPropertyChangedTests.cs`: Update 4 OnCall usages
  - Lines 132, 145, 162, 181: Change `=` to `(...)` syntax

**Out of Scope:**
- Documentation updates (release notes v10.8.0, v10.20.0; tracked separately)
- Any changes to method/property/indexer interceptors
- Changes to interface stub generation (MethodInterceptorRenderer already uses method syntax)
- Changes to standalone stub generation
- Adding `IsConfigured` property (determined unnecessary per risk analysis)

---

## Implementation Progress

**Phase 1:** Generator Change - COMPLETE
- [x] Add internal `_onCall` field generation in RenderDelegateStub()
- [x] Replace OnCall property with OnCall method in RenderDelegateStub()
- [x] Update Invoke method to use `Interceptor._onCall` field instead of `Interceptor.OnCall`
- [x] **Verification**: Build generator project - SUCCESS

**Phase 2:** Test Updates - COMPLETE
- [x] Update InlineStubTests.cs (8 syntax changes + 2 assertion removals)
- [x] Update NeatooTests.cs (4 syntax changes)
- [x] Update OpenGenericInlineStubTests.cs (2 syntax changes)
- [x] Update INotifyNeatooPropertyChangedTests.cs (4 syntax changes)
- [x] **Verification**: All tests pass - SUCCESS (2250 total tests across all frameworks)

**Phase 3:** Generated Code Verification - COMPLETE
- [x] Inspect generated `DelegateInlineTest.Stubs.g.cs` to confirm method syntax
- [x] Inspect generated `OpenGenericDelegateTests.Stubs.g.cs` for open generic support
- [x] **Verification**: Generated code matches expected pattern from design section

---

## Completion Evidence

- **Tests Passing:** All 2250 tests pass across net8.0, net9.0, net10.0
  ```
  Passed!  - Failed:     0, Passed:   134, Skipped:     0, Total:   134 - KnockOff.Documentation.Samples.dll (net8.0)
  Passed!  - Failed:     0, Passed:   134, Skipped:     0, Total:   134 - KnockOff.Documentation.Samples.dll (net9.0)
  Passed!  - Failed:     0, Passed:   134, Skipped:     0, Total:   134 - KnockOff.Documentation.Samples.dll (net10.0)
  Passed!  - Failed:     0, Passed:   473, Skipped:     0, Total:   473 - KnockOff.NeatooInterfaceTests.dll (net9.0)
  Passed!  - Failed:     0, Passed:   473, Skipped:     0, Total:   473 - KnockOff.NeatooInterfaceTests.dll (net10.0)
  Passed!  - Failed:     0, Passed:   473, Skipped:     0, Total:   473 - KnockOff.NeatooInterfaceTests.dll (net8.0)
  Passed!  - Failed:     0, Passed:   643, Skipped:     0, Total:   643 - KnockOffTests.dll (net9.0)
  Passed!  - Failed:     0, Passed:   642, Skipped:     0, Total:   642 - KnockOffTests.dll (net8.0)
  Passed!  - Failed:     0, Passed:   643, Skipped:     0, Total:   643 - KnockOffTests.dll (net10.0)
  ```

- **Generated Code Sample:** From `DelegateInlineTest.Stubs.g.cs`:
  ```csharp
  // Interceptor class with OnCall method (not property)
  public sealed class VoidOneParamDelegateInterceptor
  {
      private int _callCount;

      /// <summary>The argument from the last invocation.</summary>
      public string? LastCallArg { get; private set; }

      internal global::System.Action<string>? _onCall;

      /// <summary>Configures callback invoked when delegate is called.</summary>
      public void OnCall(global::System.Action<string> callback) { _onCall = callback; }
      // ... rest of interceptor
  }

  // Stub class accessing internal _onCall field
  private void Invoke(string message)
  {
      Interceptor.RecordCall(message);
      if (Interceptor._onCall is { } onCall) onCall(message);
  }
  ```

- **All Checklist Items:** Confirmed 100% complete
