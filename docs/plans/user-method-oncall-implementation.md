# User Method OnCall Implementation Plan

**Date:** 2026-02-02
**Related Todo:** [User Method OnCall Support](../todos/user-method-oncall-support.md)
**Status:** Complete
**Last Updated:** 2026-02-02 (Implementation Complete)

---

## Overview

Add OnCall() delegate and Returns() convenience method to non-generic user method *2 interceptors. When configured, OnCall/Returns supersede the user-defined method. The user method becomes the fallback when no callback is configured.

---

## Codebase Analysis

**Files Examined:**

| File | Purpose | Key Findings |
|------|---------|--------------|
| `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` | Design exploration | Documents current tracking-only behavior and target API |
| `src/Design/Design.Stubs/Generated/.../GenericUserMethodStub.g.cs` | Reference pattern | Shows OnCall/Callback pattern on generic typed handlers |
| `src/Design/Design.Stubs/Generated/.../BasicUserMethodStub.g.cs` | Current output | Shows tracking-only interceptors (no OnCall/Callback) |
| `src/Generator/Renderer/FlatRenderer.cs` (lines 1548-1702) | `RenderUserMethodInterceptorClass` | Renders tracking-only interceptor class |
| `src/Generator/Renderer/FlatRenderer.cs` (lines 2496-2514) | `RenderUserMethodImplementation` | Renders interface method delegation to user method |
| `src/Generator/Renderer/FlatRenderer.cs` (lines 1708-1872) | Generic handler rendering | Pattern for OnCall/Callback in generic handlers |
| `src/Generator/Model/Flat/FlatMethodModel.cs` | Method model | Contains delegate info fields already populated |
| `src/Generator/Builder/FlatModelBuilder.cs` | Model builder | `FindUserMethod` detects user methods, already computes delegate types |
| `src/KnockOff/IMethodTracking.cs` | Runtime interfaces | No changes needed - existing interfaces support callbacks |

**Key Pattern Discovery:**

Generic user method typed handlers already implement the target pattern:

```csharp
// In GenericUserMethodStub.g.cs - CreateTypedHandler<T>:
private CreateDelegate? _onCall;

public global::KnockOff.IMethodTracking OnCall(CreateDelegate callback) { _onCall = callback; return this; }

internal CreateDelegate? Callback => _onCall;

// In interface implementation:
T IGenericUserMethodService.Create<T>()
{
    Create2.Of<T>().RecordCall();
    if (Create2.Of<T>().Callback is { } callback)  // OnCall supersedes
        return callback();
    if (Strict) throw ...;
    return Create<T>();  // User method fallback
}
```

**Current Non-Generic User Method Pattern:**

```csharp
// In BasicUserMethodStub.g.cs - Process2Interceptor:
// NO _onCall field
// NO OnCall() method
// NO Callback property
// Just tracking: _callCount, _lastArg, RecordCall, Reset, Verify, Verifiable

// In interface implementation:
string IUserMethodService.Process(string input)
{
    Process2.RecordCall(input);
    return Process(input);  // Always calls user method
}
```

**Critical Discovery: FlatMethodModel Already Has Delegate Info**

The `FlatMethodModel` record (lines 24-28 of `FlatMethodModel.cs`) already contains:
- `OnCallDelegateType` - the delegate type string (e.g., `Func<string, string>` or `ProcessDelegate?`)
- `NeedsCustomDelegate` - true if ref/out params or non-void return
- `CustomDelegateName` - e.g., `ProcessDelegate`
- `CustomDelegateSignature` - full delegate declaration string

These fields are populated by `FlatModelBuilder.BuildMethodModel()` even for user methods, making the implementation simpler - no model changes required.

---

## Design

### Target Generated Code

**Interceptor Class (enhanced):**

```csharp
public sealed class Process2Interceptor : global::KnockOff.IMethodTracking<string>
{
    // NEW: Delegate (reuse existing model field CustomDelegateSignature)
    public delegate string ProcessDelegate(string input);

    // NEW: OnCall storage
    private ProcessDelegate? _onCall;

    // EXISTING: Tracking fields
    private string _lastArg = default!;
    internal int _callCount;
    private bool _isVerifiable;
    private global::KnockOff.Times? _verifiableTimes;

    // EXISTING: LastArg property
    public string LastArg => _lastArg;

    // EXISTING: RecordCall (unchanged)
    internal void RecordCall(string input) { _callCount++; _lastArg = input; }

    // NEW: OnCall method
    public global::KnockOff.IMethodTracking<string> OnCall(ProcessDelegate callback)
    {
        _onCall = callback;
        return this;
    }

    // NEW: Returns method (only for non-void, single param uses _ discard)
    public global::KnockOff.IMethodTracking<string> Returns(string value) => OnCall(_ => value);

    // NEW: Callback property for invocation logic
    internal ProcessDelegate? Callback => _onCall;

    // Reset preserves OnCall configuration (matches regular interceptors)
    public void Reset()
    {
        _callCount = 0;
        _lastArg = default!;
        // _onCall is NOT cleared - configuration is preserved
    }

    // EXISTING: Verify methods (unchanged)
    // EXISTING: Verifiable methods (unchanged)
}
```

**Void Method Interceptor (OnCall only, no Returns):**

```csharp
public sealed class Execute2Interceptor : global::KnockOff.IMethodTracking<string>
{
    public delegate void ExecuteDelegate(string command);

    private ExecuteDelegate? _onCall;

    // NO Returns() method for void

    public global::KnockOff.IMethodTracking<string> OnCall(ExecuteDelegate callback)
    {
        _onCall = callback;
        return this;
    }

    internal ExecuteDelegate? Callback => _onCall;

    // ... tracking unchanged
}
```

**Interface Implementation (enhanced):**

```csharp
string IUserMethodService.Process(string input)
{
    Process2.RecordCall(input);

    // NEW: OnCall supersedes user method
    if (Process2.Callback is { } callback)
        return callback(input);

    // EXISTING: User method is fallback
    return Process(input);
}

void IUserMethodService.Execute(string command)
{
    Execute2.RecordCall(command);

    // NEW: OnCall supersedes user method
    if (Execute2.Callback is { } callback)
    {
        callback(command);
        return;
    }

    // EXISTING: User method is fallback
    Execute(command);
}
```

### Strict Mode Behavior

User methods bypass strict mode because the user method itself IS the configuration. With OnCall/Returns, this remains consistent:

- **User method defined** = configured (bypasses strict check)
- **OnCall configured** = supersedes user method (no strict check needed)
- **Returns configured** = supersedes user method (no strict check needed)

The interface implementation does not need strict mode checks because there is always a fallback (the user method).

---

## Implementation Steps

### Phase 1: Enhance User Method Interceptor Rendering

**File:** `src/Generator/Renderer/FlatRenderer.cs`

**Method:** `RenderUserMethodInterceptorClass` (lines 1548-1702)

**Changes:**

1. Add delegate declaration (if `NeedsCustomDelegate`, use `CustomDelegateSignature`; otherwise use Action/Func)
2. Add `_onCall` field
3. Add `OnCall()` method returning appropriate `IMethodTracking` interface
4. Add `Returns()` method for non-void methods (parameter-count-aware lambda, async auto-wrap)
5. Add `Callback` property (internal)
6. `Reset()` does NOT clear `_onCall` - configuration is preserved (matches regular interceptors)

### Phase 2: Enhance User Method Implementation Rendering

**File:** `src/Generator/Renderer/FlatRenderer.cs`

**Method:** `RenderUserMethodImplementation` (lines 2496-2514)

**Changes:**

1. After `RecordCall`, add callback check: `if (InterceptorName.Callback is { } callback)`
2. For non-void: `return callback(args);`
3. For void: `{ callback(args); return; }`
4. Keep user method call as final fallback

### Phase 3: Update Tests

**Location:** `src/Tests/KnockOffTests/`

**Test Cases:**

1. OnCall supersedes user method (non-void)
2. OnCall supersedes user method (void)
3. Returns supersedes user method (non-void only)
4. User method is fallback when no OnCall/Returns
5. Reset preserves OnCall configuration
6. Verifiable works with OnCall configured
7. Multiple parameters (LastArgs) with OnCall

### Phase 4: Update Design Exploration

**File:** `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs`

**Changes:**

1. Update comments to reflect OnCall support is now implemented
2. Add demonstration code showing OnCall/Returns usage

---

## Architectural Verification

**Three Patterns Analysis:**

| Pattern | Applicability | Notes |
|---------|--------------|-------|
| **Stand-alone** | YES | Primary target. User methods require partial class, which only stand-alone supports. |
| **Inline Interface** | N/A | Cannot have user methods - generator creates the entire class. |
| **Inline Class** | N/A | Cannot have user methods - generator creates the entire class. |
| **Inline Delegate** | N/A | Cannot have user methods. |

**Breaking Changes:** No

- New methods (OnCall, Returns) are additive
- Existing tracking API unchanged
- Existing behavior (user method called) is preserved as fallback
- No changes to model or builder required

**Pattern Consistency:**

This change aligns non-generic user method interceptors with:
- Generic user method typed handlers (already have OnCall/Callback)
- Regular method interceptors (have full OnCall/Returns/sequences)

The simplified API (no sequences, no ThenReturns) is appropriate because:
1. User methods already provide the "default" behavior
2. OnCall/Returns are for per-test overrides, not complex sequences
3. Keeps the *2 interceptor focused on its primary purpose

**Diagnostic Requirements:** None

- No new compile-time errors needed
- Runtime behavior follows existing patterns

**Edge Cases:**

1. **Async user methods** - Return type is `Task<T>` or `ValueTask<T>`. OnCall uses the full delegate type. **Returns auto-wraps:** `Returns("data")` generates `OnCall(_ => Task.FromResult("data"))` for `Task<T>` methods.

2. **Nullable return types** - OnCall callback returns nullable. Returns accepts nullable value.

3. **ref/out parameters** - Custom delegate with ref/out modifiers is already computed in `FlatMethodModel.CustomDelegateSignature`. No Returns method for ref/out (semantically doesn't make sense).

4. **User method overloads** - Currently broken (separate bug in todo). OnCall support should work for single-signature user methods.

5. **Multiple trackable parameters** - Use `LastArgs` tuple type. OnCall callback takes all parameters. **Returns lambda generation:**
   - 0 params: `Returns(value) => OnCall(() => value)`
   - 1 param: `Returns(value) => OnCall(_ => value)`
   - 2 params: `Returns(value) => OnCall((_, _) => value)`
   - 3+ params: `Returns(value) => OnCall((_, _, _) => value)` etc.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-02
**Updated:** 2026-02-02 (Concerns Resolved)

### Original Concerns (Resolved)

1. **[Code Generation]: Returns lambda for varying parameter counts**
   - **Resolution:** Implement explicit codegen for 0, 1, and 2+ parameters:
     - 0 params: `Returns(value) => OnCall(() => value)` (or `OnCall(() => Task.FromResult(value))` for async)
     - 1 param: `Returns(value) => OnCall(_ => value)` (or `OnCall(_ => Task.FromResult(value))` for async)
     - 2+ params: `Returns(value) => OnCall((_, _) => value)` etc.

2. **[Design Decision]: Reset behavior**
   - **Resolution:** Reset preserves OnCall configuration (matches regular interceptors)
   - **Note:** This differs from generic user method typed handlers which clear `_onCall` in Reset
   - **Rationale:** User method interceptors should match regular interceptor semantics for consistency with the main API

3. **[Edge Case]: Async method Returns type**
   - **Resolution:** Auto-wrap in `Task.FromResult()` for `Task<T>`, `new ValueTask<T>(value)` for `ValueTask<T>`
   - Example: `Returns("data")` generates `OnCall(_ => Task.FromResult("data"))` for `Task<string>` return type
   - Example: `Returns(42)` generates `OnCall(_ => new ValueTask<int>(42))` for `ValueTask<int>` return type

4. **[Edge Case]: Void Task methods (Task return, no generic)**
   - **Resolution:** Methods returning plain `Task` (not `Task<T>`) are treated like void methods: OnCall only, no Returns
   - **Rationale:** Plain `Task` has no value to return, semantically equivalent to `void`

5. **[Implementation]: Async type detection**
   - **Resolution:** Extract `GetAsyncTypeInfoForMethod` from ClassRenderer.cs to a shared location (or duplicate in FlatRenderer)
   - This helper detects `Task<T>` and `ValueTask<T>` patterns and extracts the inner type

### Important Finding: Generic User Methods

**Generic user method typed handlers (GenericUserMethodStub.g.cs) currently have:**
- OnCall - YES
- Returns - NO
- Reset clears `_onCall` - YES

**Design decision:** Non-generic user method interceptors will have a richer API than generic typed handlers:
- OnCall - YES (same as generic)
- Returns - YES (convenience method, not on generic)
- Reset preserves config - YES (matches regular interceptors, differs from generic)

**Potential Follow-up:** Consider adding Returns to generic user method typed handlers for consistency. This is out of scope for this task.

### Files Examined During Review

- `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` - Design exploration, confirms tracking-only current state
- `src/Generator/Model/Flat/FlatMethodModel.cs` - Verified delegate info fields exist
- `src/Generator/Renderer/FlatRenderer.cs` lines 834-1183 - Regular interceptor pattern with Returns
- `src/Generator/Renderer/FlatRenderer.cs` lines 1548-1702 - Current user method interceptor (tracking-only)
- `src/Generator/Renderer/FlatRenderer.cs` lines 1708-1872 - Generic typed handler pattern (has OnCall/Callback)
- `src/Generator/Renderer/FlatRenderer.cs` lines 2496-2514 - Current user method implementation
- `src/Design/Design.Stubs/Generated/.../BasicUserMethodStub.g.cs` - Current output
- `src/Design/Design.Stubs/Generated/.../GenericUserMethodStub.g.cs` - Reference pattern for OnCall/Callback (NO Returns)
- `src/Design/Design.Stubs/Generated/.../CalculatorStub.g.cs` - Regular interceptor pattern (Returns, Reset preserves config)
- `src/KnockOff/IMethodTracking.cs` - Interface definition

---

## Implementation Contract

**Created:** 2026-02-02
**Approved by:** knockoff-developer

**In Scope:**

- [ ] `src/Generator/Renderer/FlatRenderer.cs`: Modify `RenderUserMethodInterceptorClass` (lines ~1548-1702) to add:
  - [ ] Delegate type declaration (if `NeedsCustomDelegate` use `CustomDelegateSignature`, else use Action/Func)
  - [ ] `_onCall` field (`{DelegateType}? _onCall`)
  - [ ] `Callback` property (`internal {DelegateType}? Callback => _onCall;`)
  - [ ] `OnCall(delegate)` method returning appropriate `IMethodTracking` interface
  - [ ] `Returns(value)` method for non-void methods with:
    - Parameter-count-aware lambda generation (0, 1, 2+ params)
    - Async auto-wrap (`Task.FromResult()` for `Task<T>`, `ValueTask.FromResult()` for `ValueTask<T>`)
  - [ ] Reset does NOT clear `_onCall` (configuration preserved)
- [ ] `src/Generator/Renderer/FlatRenderer.cs`: Modify `RenderUserMethodImplementation` (lines ~2496-2514) to:
  - [ ] After `RecordCall`, check `if (InterceptorName.Callback is { } callback)`
  - [ ] For non-void: `return callback(args);`
  - [ ] For void: `{ callback(args); return; }`
  - [ ] Keep user method call as final fallback
- [ ] Test: OnCall supersedes user method (non-void)
- [ ] Test: OnCall supersedes user method (void)
- [ ] Test: Returns supersedes user method (non-void)
- [ ] Test: User method fallback when no callback
- [ ] Test: Reset preserves OnCall configuration
- [ ] Test: Multiple parameters work with OnCall
- [ ] Test: Async user method with OnCall (Task<T>)
- [ ] Update `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` with OnCall/Returns demonstration

**Explicitly Out of Scope:**

- Generic user methods (already have OnCall via Of<T>() typed handlers - potential future Returns addition is separate work)
- Inline stubs (cannot have user methods)
- Model changes (FlatMethodModel already has delegate info)
- Builder changes (FlatModelBuilder already computes delegate types)
- Method overload support (separate bug documented in UserMethodBasics.cs)
- Sequences (ThenReturns) for user method interceptors

**Verification Gates:**

1. After Phase 1 (Interceptor Class): Build succeeds, generated code has OnCall/Returns/Callback
2. After Phase 2 (Interface Implementation): Build succeeds, callback supersedes user method
3. After Phase 3 (Testing): All tests pass including new OnCall/Returns tests
4. Final: All existing tests pass, Design.Stubs compiles

**Stop Conditions:**

If any of these occur, STOP and report:
- Out-of-scope test fails
- Architectural contradiction discovered (e.g., FlatMethodModel missing expected fields)
- Generated code does not compile
- Reset behavior causes unexpected test failures

---

## Implementation Progress

[To be filled during implementation]

**Phase 1:** Interceptor Class Enhancement
- [x] Add delegate type declaration (custom or Action/Func based on `NeedsCustomDelegate`)
- [x] Add `_onCall` field
- [x] Add `Callback` property (internal)
- [x] Add `OnCall` method returning appropriate tracking interface
- [x] Add `Returns` method (non-void only) with:
  - [x] Parameter-count-aware lambda (0, 1, 2+ params)
  - [x] Async auto-wrap for Task<T>/ValueTask<T>
- [x] Verify Reset does NOT clear `_onCall`
- [x] **Verification**: Build succeeds, inspect generated BasicUserMethodStub.g.cs

**Phase 2:** Interface Implementation
- [x] Modify `RenderUserMethodImplementation` to check callback first
- [x] Handle non-void methods: `if (Callback is { } callback) return callback(args);`
- [x] Handle void methods: `if (Callback is { } callback) { callback(args); return; }`
- [x] Preserve user method call as fallback
- [x] **Verification**: Build succeeds, callback supersedes user method

**Phase 3:** Testing
- [x] Add test: OnCall supersedes user method (non-void)
- [x] Add test: OnCall supersedes user method (void)
- [x] Add test: Returns supersedes user method (non-void)
- [x] Add test: User method fallback when no callback
- [x] Add test: Reset preserves OnCall configuration
- [x] Add test: Multiple parameters with OnCall
- [x] Add test: Async user method with OnCall (Task<T> and ValueTask<T>)
- [x] **Verification**: All 16 new tests pass, full test suite passes (2874 tests)

**Phase 4:** Design.Stubs Documentation
- [x] Update UserMethodBasics.cs comments (remove TODO)
- [x] Add demonstration code for OnCall/Returns usage
- [x] **Verification**: Design.Stubs compiles

---

## Acceptance Criteria

- [x] `stub.Method2.OnCall(callback)` compiles and works for non-generic user methods
- [x] `stub.Method2.Returns(value)` compiles and works for non-void user methods
- [x] Configured callback supersedes user method implementation
- [x] User method is called when no callback configured
- [x] `Reset()` preserves OnCall configuration (only clears tracking state)
- [x] Void methods have OnCall but no Returns
- [x] All existing tests continue to pass
- [x] Design.Stubs UserMethodBasics.cs demonstrates the new API

---

## Dependencies

None - This is a self-contained enhancement to the generator renderer.

---

## Risks / Considerations

1. **Delegate Type Selection**: Must correctly choose between Action/Func and custom delegate. The existing `FlatMethodModel` fields handle this, but verify for edge cases.

2. **Returns Lambda Syntax**: For multi-parameter methods, Returns is implemented as `OnCall((_, _, ...) => value)`. Must generate correct number of discards for 0, 1, and 2+ parameter cases.

3. **Async Auto-Wrap**: For `Task<T>` methods, Returns must generate `Task.FromResult(value)`. For `ValueTask<T>`, must generate `new ValueTask<T>(value)` or `ValueTask.FromResult(value)`.

4. **Test Coverage**: Ensure tests cover the interaction between OnCall and user method - specifically verifying that OnCall supersedes and user method is fallback.

5. **Generic User Method Divergence**: Note that this implementation gives non-generic user method interceptors a richer API than generic typed handlers (which don't have Returns). This is intentional but may warrant a follow-up for consistency.

---

## Completion Evidence

**Completed:** 2026-02-02

### Tests Passing

Full test suite: 2874 tests pass across all frameworks (net8.0, net9.0, net10.0)
- New user method OnCall tests: 16 tests added in `UserMethodOnCallTests.cs`
- Tests cover: OnCall supersedes, Returns supersedes, user method fallback, Reset preserves config, multi-parameter, async methods

### Generated Code Sample

**BasicUserMethodStub.g.cs - Process2Interceptor:**
```csharp
public sealed class Process2Interceptor : global::KnockOff.IMethodTracking<string>
{
    public delegate string ProcessDelegate(string input);
    private ProcessDelegate? _onCall;
    // ... tracking fields ...

    public global::KnockOff.IMethodTracking<string> OnCall(ProcessDelegate callback)
    {
        _onCall = callback;
        return this;
    }

    public global::KnockOff.IMethodTracking<string> Returns(string value) => OnCall(_ => value);

    internal ProcessDelegate? Callback => _onCall;
    // ... rest unchanged ...
}
```

**AsyncUserMethodStub.g.cs - ProcessAsync2Interceptor (Task<T> auto-wrap):**
```csharp
public global::KnockOff.IMethodTracking<string> Returns(string value)
    => OnCall(_ => global::System.Threading.Tasks.Task.FromResult(value));
```

**Interface Implementation (callback supersedes user method):**
```csharp
string global::Design.Domain.Services.IUserMethodService.Process(string input)
{
    Process2.RecordCall(input);
    if (Process2.Callback is { } callback) return callback(input);
    return Process(input);  // User method fallback
}
```

### All Checklist Items

Confirmed 100% complete:
- [x] Phase 1: Interceptor class rendering (OnCall, Returns, Callback, async auto-wrap)
- [x] Phase 2: Interface implementation (callback check before user method)
- [x] Phase 3: Testing (16 new tests)
- [x] Phase 4: Design.Stubs documentation
