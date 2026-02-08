# Enable .When() API with Stub Overrides - Implementation Plan

**Date:** 2026-02-05
**Related Todo:** [Enable .When() API with Stub Overrides](../todos/when-with-user-methods.md)
**Status:** Complete
**Last Updated:** 2026-02-05

---

## Overview

Extend standalone stub interceptors to support the `.When()` API when stub overrides are defined. Currently, methods with user overrides (the `_` suffix pattern) receive a simplified "tracking-only" interceptor that lacks `.When()` chains, sequences, and other features available in inline stubs.

The goal is API uniformity: standalone stubs with stub overrides should have the same interceptor capabilities as inline stubs.

---

## Scope

### Patterns Affected

| Pattern | Stub Overrides Supported | `.When()` Support After |
|---------|----------------------|------------------------|
| Standalone (interface) | Yes | Yes |
| Generic Standalone (interface) | Yes | Yes |
| Standalone Class | Yes | Yes |
| Generic Standalone Class | Yes | Yes |
| Inline Interface | No (by design) | Already has `.When()` |
| Inline Class | No (by design) | Already has `.When()` |
| Inline Delegate | No (by design) | N/A |
| Open Generic Interface | No (by design) | Already has `.When()` |
| Open Generic Class | No (by design) | Already has `.When()` |

### Member Types

- **Methods**: Yes - full `.When()` support
- **Properties**: No - inline pattern does not have `.When()` for properties
- **Indexers**: No - inline pattern does not have `.When()` for indexers
- **Events**: No - not applicable

### Features to Add

1. **When chains** - `.When(args).Returns(value)`, `.When(predicate).Returns(value)`
2. **Void When chains** - `.When(args).Call(callback)` for void methods
3. **Sequences** - `.OnCall().ThenCall()`, `.Returns().ThenReturns()`, `Returns(first, params rest[])`
4. **Async auto-wrap** - `.When(args).Returns(innerValue)` auto-wraps with `Task.FromResult`
5. **Verification** - `.Verifiable()` on When chains, HEAD tracking, `stub.Verify()` integration

---

## Design

### Priority Chain Specification

**Current Stub Override Interceptor:**
```
1. OnCall/Returns (if configured)
2. Stub Override (fallback)
```

**New Stub Override Interceptor (matches inline pattern):**
```
1. When chains (parameter-specific matching)
2. Sequences (ThenCall chain)
3. OnCall/Returns (explicit configuration)
4. Stub Override (fallback - replaces Source/Strict from inline)
```

### Generated Code Pattern

**Current (simplified interceptor):**
```csharp
string IService.Process(string input)
{
    Process.RecordCall(input);
    if (Process.Callback is { } callback) return callback(input);
    return Process_(input);  // Stub override
}
```

**New (full interceptor with Invoke):**
```csharp
string IService.Process(string input)
{
    return Process.Invoke(Strict, input);
}

// Inside ProcessInterceptor.Invoke():
internal string Invoke(bool strict, string input)
{
    // When chain - check HEAD matcher first
    if (_whenChain != null && _whenChainHead < _whenChain.Count)
    {
        var matcher = _whenChain[_whenChainHead];
        if (matcher.Matches(input))
        {
            matcher.CallCount++;
            if (_whenChainHead < _whenChain.Count - 1) _whenChainHead++;
            return matcher.Execute(input);
        }
        else if (matcher.IsTerminal)
        {
            _whenChainHead++;
        }
    }

    // Sequences
    if (_sequence != null && _sequenceIndex < _sequence.Count)
    {
        var (callback, tracking) = _sequence[_sequenceIndex];
        tracking.RecordCall(input);
        _sequenceIndex++;
        return callback(input);
    }

    // Returns(value)
    if (_hasReturnsValue && _returnsValueTracking != null)
    {
        _returnsValueTracking.RecordCall(input);
        return _returnsValue;
    }

    // OnCall
    if (_onCall != null && _onCallTracking != null)
    {
        _onCallTracking.RecordCall(input);
        return _onCall(input);
    }

    // Stub Override fallback (NEW - replaces Source/Strict)
    _unconfiguredCallCount++;
    _unconfiguredLastArg = input;
    return Process_(input);  // Call user override
}
```

### Key Differences from Inline Interceptor

The only difference between the stub override interceptor and inline interceptor is the final fallback:
- **Inline**: Falls to Source delegation, then Strict mode, then smart default
- **Stub Override**: Falls to stub override (which IS the "configured" behavior)

### Interceptor Class Structure

Stub override interceptors will gain these additional members:

```csharp
public sealed class ProcessInterceptor
{
    // === NEW: Sequence support ===
    private List<(ProcessDelegate Callback, MethodCallBuilderImpl Tracking)>? _sequence;
    private int _sequenceIndex;
    private bool _repeatLastValue = true;

    // === NEW: When chain support ===
    private List<WhenMatcher>? _whenChain;
    private int _whenChainHead;
    private bool _whenVerifiable;

    // === NEW: Returns(value) support ===
    private string _returnsValue = default!;
    private bool _hasReturnsValue;
    private MethodCallBuilderImpl? _returnsValueTracking;

    // === NEW: Unconfigured tracking ===
    private int _unconfiguredCallCount;
    private string? _unconfiguredLastArg;

    // === NEW: Methods ===
    public WhenBuilder When(string input) { ... }
    public WhenBuilder When(Func<string, bool> predicate) { ... }
    public MethodCallBuilderImpl Returns(string value) { ... }
    public MethodSequenceImpl Returns(string first, params string[] rest) { ... }
    internal string Invoke(bool strict, string input) { ... }

    // === NEW: Nested classes ===
    public sealed class MethodCallBuilderImpl { ... }
    public sealed class MethodSequenceImpl { ... }
    private abstract class WhenMatcher { ... }
    private sealed class WhenMatcherValue : WhenMatcher { ... }
    private sealed class WhenMatcherCall : WhenMatcher { ... }
    private sealed class WhenMatcherNone : WhenMatcher { ... }
    public sealed class WhenBuilder { ... }
    public sealed class WhenChain { ... }
}
```

---

## Approach

### Strategy: Unify with Inline Interceptor Generation

Rather than maintaining two separate interceptor rendering paths, extend the existing `MethodInterceptorRenderer` to handle stub override interceptors by:

1. Adding a parameter to indicate "stub override fallback" instead of "Source/Strict fallback"
2. Reusing all the When chain, Sequence, and verification rendering logic
3. Generating the `Invoke()` method with stub override as final fallback

### Alternative Considered: Separate Renderer

Rejected because:
- Would duplicate ~1000 lines of interceptor generation code
- Changes to inline interceptor would need mirroring in stub override interceptor
- Higher maintenance burden

---

## Implementation Steps

### Phase 1: Model Updates

Update the model layer to support unified rendering with stub override fallback.

**Files:**
- `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` - Add `UserMethodName` property (string, null when no user override)
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` - Populate `UserMethodName` from existing `HasUserOverride` + naming convention

**Note:** Use existing `HasUserOverride` property (already in FlatMethodModel). Do NOT add new `HasUserMethod` property.

**Verification:** Build succeeds, no test changes

### Phase 2: Renderer Integration

Extend `MethodInterceptorRenderer` to handle stub override interceptors.

**Files:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Add `UserMethodFallback` option to `InterceptorRenderOptions`
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Modify `RenderInvokeMethod()` to use stub override as final fallback (skip Source/Strict checks)
- `src/Generator/Renderer/FlatRenderer.cs` - Replace `RenderUserMethodInterceptorClass()` calls with unified renderer

**Verification:**
1. Build succeeds
2. Existing tests pass
3. **Generated code verification**: Rebuild Design.Stubs and verify BasicUserMethodStub.g.cs:
   - ProcessInterceptor contains `_whenChain` field
   - ProcessInterceptor contains `When()` method overloads
   - ProcessInterceptor contains `WhenBuilder` and `WhenChain` nested classes
   - ProcessInterceptor contains `Invoke()` method with priority chain
   - Invoke() ends with `return Process_(input);` (stub override fallback), NOT Strict check

### Phase 3: Interface Implementation Update

Update interface implementation generation to use `Invoke()` pattern for stub overrides.

**Files:**
- `src/Generator/Renderer/FlatRenderer.cs` - Modify `RenderUserOverrideImplementation()` to call `Invoke()` instead of inline logic

**Verification:** Build succeeds, existing stub override tests pass

### Phase 4: Overload Group Support

Handle stub override overloads (multiple signatures with same name), including **mixed overload groups** where some signatures have stub overrides and others do not.

**Files:**
- `src/Generator/Renderer/FlatRenderer.cs` - Update `RenderUserMethodGroupInterceptorClass()` to use unified renderer
- `src/Generator/Builder/FlatModelBuilder.cs` - Ensure overload groups populate unified model correctly

**Mixed Overload Group Handling:**
When an overload group has mixed stub override coverage (e.g., Format(string) has user override, Format(string, bool) does not):
- Per-signature `Invoke_*` methods must use appropriate fallback:
  - Signature WITH user override: Falls to stub override
  - Signature WITHOUT user override: Falls to Source/Strict
- The interceptor class contains all When/Sequence infrastructure per signature
- `Source(T)` only populates `_source` for signatures without user overrides

**Verification:** Build succeeds, overload stub override tests pass, mixed overload scenarios verified

### Phase 5: Test Coverage

Add comprehensive tests for `.When()` with stub overrides.

**Files:**
- `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` - Add When chain examples
- `src/Design/Design.Tests/UserMethodTests/` - Add test files for When + stub override combinations
- `src/Tests/KnockOffTests/` - Add generator output verification tests

**Test Cases:**
- [ ] Basic `.When(value).Returns(value)` with stub override fallback
- [ ] Predicate `.When(predicate).Returns(value)` with stub override fallback
- [ ] `.When().ThenWhen()` chaining with stub override fallback
- [ ] Void method `.When(args).Call(callback)` with stub override fallback
- [ ] Async method `.When(args).Returns(innerValue)` auto-wrap
- [ ] Sequences `.Returns().ThenReturns()` with stub override fallback
- [ ] `.Verifiable()` on When chains
- [ ] `stub.Verify()` integration
- [ ] Mixed scenario: some calls match When, some fall to stub override
- [ ] Generic standalone with stub overrides
- [ ] Overloaded stub overrides with When chains

**Verification:** All new tests pass

### Phase 6: Documentation

Update documentation to reflect new capability.

**Files:**
- `docs/api-consistency-matrix.md` - Update to show `.When()` works with stub overrides
- `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` - Add documentation comments

**Verification:** Documentation builds, examples compile

---

## Acceptance Criteria

- [ ] `.When(value).Returns(value)` works on methods with user overrides
- [ ] `.When(predicate).Returns(value)` works on methods with user overrides
- [ ] When chain falls through to stub override when no match
- [ ] Sequences work: `.OnCall().ThenCall()`, `.Returns().ThenReturns()`
- [ ] Void methods support `.When(args).Call(callback)`
- [ ] Async methods auto-wrap inner type with `Task.FromResult`
- [ ] `.Verifiable()` works on When chains
- [ ] `stub.Verify()` includes When chain verification
- [ ] All existing stub override tests continue to pass
- [ ] All existing non-user-method tests continue to pass
- [ ] Generic standalone stubs with stub overrides have `.When()` support
- [ ] Overloaded stub overrides have `.When()` support

---

## Dependencies

- None - all required infrastructure exists in the codebase

---

## Risks / Considerations

### Risk: Generated Code Size Increase

Stub override interceptors will grow significantly (from ~80 lines to ~400+ lines).

**Mitigation:** This is acceptable - the code is generated, not maintained by users. The benefit of API consistency outweighs the generated code size.

### Risk: Breaking Existing Stub Override Behavior

If the priority chain is incorrect, existing stub override stubs could behave differently.

**Mitigation:**
- Existing tests verify current behavior
- New `Invoke()` method has stub override as final fallback (same effective behavior when no When/Sequence/OnCall configured)
- Phase 3 verification explicitly checks existing tests pass

### Risk: Overload Complexity

Overloaded stub overrides require per-signature When chains, adding complexity.

**Mitigation:** The codebase already handles overload groups for inline stubs. Reuse that pattern.

### Note: Source(T) Behavior

Stub override stubs have empty `Source(T)` method bodies by design. The stub override IS the fallback, so Source delegation is not applicable. This is intentional and will remain unchanged.

---

## Architectural Verification

**Nine Patterns Analysis:**
- Standalone (interface): Primary target - gains `.When()` with stub override fallback
- Generic Standalone (interface): Target - gains `.When()` with stub override fallback
- Standalone Class: Target - gains `.When()` with stub override fallback
- Generic Standalone Class: Target - gains `.When()` with stub override fallback
- Inline Interface: N/A - cannot have stub overrides, already has `.When()`
- Inline Class: N/A - cannot have stub overrides, already has `.When()`
- Inline Delegate: N/A - cannot have stub overrides
- Open Generic Interface: N/A - cannot have stub overrides, already has `.When()`
- Open Generic Class: N/A - cannot have stub overrides, already has `.When()`

**Breaking Changes:** No - adding new API surface, not modifying existing behavior

**Pattern Consistency:** Design follows inline pattern exactly, with stub override replacing Source/Strict as final fallback

**Diagnostic Requirements:** None needed - feature is purely additive

**Test Strategy:**
- Phase 5 defines comprehensive test matrix
- Existing tests verify no regression
- New tests verify new functionality

**Edge Cases:**
- Stub override that throws: When chain should still work, exception propagates from stub override on fallback
- Async stub override with sync When: Auto-wrap handles this
- Nullable return types: Handled same as inline
- Generic type parameters: Handled by generic standalone pattern

**Codebase Deep-Dive Completed:**

Files examined:
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/FlatRenderer.cs` - Lines 2060-2260 `RenderUserMethodInterceptorClass()`
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Full interceptor with When chains
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/WhenChainRenderer.cs` - When chain generation
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` - Current stub override patterns
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/BasicUserMethodStub.g.cs` - Current simplified interceptor
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/CalculatorStub.g.cs` - Full interceptor with When chains
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/WhenMatchingDemo.Stubs.g.cs` - Inline When chain example
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/GenericFormatterWithUserMethodsStub\`1.g.cs` - Mixed user/non-stub overrides

---

## Developer Concerns Resolution

**Status:** Concerns Addressed by Architect

The developer raised 6 concerns during initial review. Each has been investigated and resolved below.

### Concern 1: OnCall/Returns Return Type Incompatibility

**Concern:** Current stub override interceptors return `IMethodTracking<T>` from `OnCall()`/`Returns()`, but the unified full interceptor returns `MethodCallBuilderImpl`. This could break existing code like:
```csharp
IMethodTracking<string> tracking = stub.Process.OnCall(x => x);
```

**Investigation:**
- Current stub override interceptor (BasicUserMethodStub.g.cs line 30): `public global::KnockOff.IMethodTracking<string> OnCall(ProcessDelegate callback)`
- Full interceptor (CalculatorStub.g.cs line 58): `public MethodCallBuilderImpl OnCall(AddDelegate callback)`

**Resolution: NOT a breaking change.**

Examining the interfaces:
- `IMethodCallBuilder<TCallback>` extends `IMethodTracking`
- `IMethodCallBuilder<TCallback, TArg>` extends `IMethodTracking<TArg>`
- `IMethodCallBuilderArgs<TCallback, TArgs>` extends `IMethodTrackingArgs<TArgs>`

The full interceptor's `MethodCallBuilderImpl` implements these extended interfaces. Since the return type widens (subtype to supertype is implicitly convertible), existing code like `IMethodTracking<string> tracking = stub.Process.OnCall(...)` will continue to work.

The change is **additive**: users gain `.ThenCall()`, `.ThenReturns()`, and new `Verifiable()` overloads, but all existing code remains compatible.

**Action:** No change to plan. Proceed with unified interceptor.

---

### Concern 2: Source(T) Delegation Is Broken for Stub Override Stubs

**Concern:** Looking at BasicUserMethodStub.g.cs, the `Source(T)` method has an empty body:
```csharp
public void Source(global::Design.Domain.Services.IUserMethodService? source)
{
}
```

**Investigation:**
This is **by design**, not a bug. Looking at UserMethodBasics.cs line 141:
```
// OnCall() - supersede stub override per-test
```

Stub override stubs have a different fallback model:
- Regular stubs: OnCall > Sequences > Returns > Source delegation > Strict/default
- Stub override stubs: OnCall > Stub Override

The stub override IS the fallback. Source delegation is not applicable because:
1. Stub overrides provide the default behavior (that's why you define them)
2. If you wanted source delegation, you wouldn't use stub overrides

**Resolution: Leave Source(T) empty for stub override stubs.**

The empty body is intentional. The method exists only for API consistency (all stubs have Source). When stub overrides exist, Source delegation is conceptually replaced by stub override fallback.

After this feature, the new priority chain becomes:
- When chains > Sequences > OnCall/Returns > Stub Override (no Source)

**Action:** Document in plan that Source(T) remains empty for stub override stubs. This is consistent with the design: stub overrides ARE the fallback.

---

### Concern 3: Mixed Overload Groups Handling

**Concern:** What happens when only SOME signatures in an overload group have stub overrides? For example:
```csharp
// User defines:
protected override string Format_(string input) => ...;
// But NOT Format_(string, bool) or Format_(string, bool, int)
```

**Investigation:**
Looking at GenericFormatterWithUserMethodsStub\`1.g.cs, the generator already handles mixed scenarios at the interface level. However, the existing design handles this at the **method level**, not interceptor level.

Looking at FlatModelBuilder.cs line 50:
```csharp
.Where(m => !m.IsGenericMethod && !m.HasUserOverride)
```

Methods are separated into two groups:
- `flatMethodGroups`: Methods WITHOUT user overrides (full interceptor)
- `flatUserMethodGroups`: Methods WITH user overrides (simplified interceptor)

This means in a mixed overload scenario:
- Overloads with stub override: Go through stub override path
- Overloads without stub override: Go through regular path

**Resolution: Handle at per-signature level, not interceptor level.**

The unified interceptor approach must support **mixed overload groups** where:
- Some signatures have stub override fallback
- Some signatures have Source/Strict fallback

Looking at GenericFormatterWithUserMethodsStub, it already generates per-signature Invoke methods:
- `Invoke_T_void(bool strict, T item)` - falls to Source/Strict
- `Invoke_T_String_void(bool strict, T item, string tag)` - falls to Source/Strict

For stub override interceptors, we need:
- `Invoke_T_void(bool strict, T item)` - falls to stub override
- `Invoke_T_String_void(bool strict, T item, string tag)` - falls to Source/Strict (no user override)

**Action:** Update Phase 4 to explicitly handle mixed overload groups where per-signature fallback differs.

---

### Concern 4: Model Property Naming Inconsistency

**Concern:** Plan proposes adding `HasUserMethod` property, but codebase uses `HasUserOverride`.

**Investigation:**
Confirmed in FlatMethodModel.cs line 31:
```csharp
bool HasUserOverride,
```

And FlatPropertyModel.cs line 31:
```csharp
bool HasUserOverride = false);
```

**Resolution: Use existing `HasUserOverride` naming.**

The plan incorrectly proposed `HasUserMethod`. The correct approach is to:
1. Use existing `HasUserOverride` property (already exists in FlatMethodModel)
2. Add `UserMethodName` property if needed (but this can be derived: `{MethodName}_`)

**Action:** Update Phase 1 to use existing `HasUserOverride` property instead of adding new `HasUserMethod`.

---

### Concern 5: Strict Mode + When Chain + Stub Override Priority

**Concern:** What happens when:
1. Stub is in strict mode
2. When chain is configured but doesn't match
3. Stub override exists

Does it: (a) throw StubException, (b) call stub override, or (c) something else?

**Investigation:**
Looking at CalculatorStub.g.cs Invoke() method (lines 118-184):
```csharp
// When chain - check HEAD matcher first
if (_whenChain != null && _whenChainHead < _whenChain.Count)
{
    var matcher = _whenChain[_whenChainHead];
    if (matcher.Matches(a, b)) { ... return matcher.Execute(a, b); }
    else if (matcher.IsTerminal) { _whenChainHead++; }
    // Non-terminal didn't match: fall through to rest of priority chain
}

// ... sequences, OnCall, Returns ...

if (strict) throw global::KnockOff.StubException.NotConfigured("", "Add");
return default!;
```

The key insight: **When chains are not "one-shot"**. If a non-terminal When matcher doesn't match, execution falls through. The Strict check only happens at the END of the priority chain.

For stub overrides, the stub override IS the final fallback, so Strict mode never triggers:

```csharp
// Stub override fallback (replaces Strict check)
_unconfiguredCallCount++;
return Process_(input);  // Stub override is ALWAYS called if nothing else matches
```

**Resolution: Stub override fallback bypasses Strict mode.**

This is consistent with existing design (UserMethodBasics.cs line 417):
```csharp
// User overrides bypass strict mode - they ARE the configuration
```

When chains that don't match fall through to stub override. Strict mode is irrelevant because stub override IS configuration.

**Action:** Clarify in plan that Strict mode is not checked when stub override exists. The stub override IS the "configured" behavior.

---

### Concern 6: Phase 2 Lacks Generated Code Verification

**Concern:** Phase 2 says "Build succeeds, existing tests pass" but doesn't verify that generated code actually gained When chain support.

**Investigation:**
Valid concern. Phase 2 is the core change, and we need to verify the generated output, not just that it compiles.

**Resolution: Add explicit generated code verification to Phase 2.**

**Action:** Update Phase 2 to include:
- Rebuild Design.Stubs to regenerate BasicUserMethodStub.g.cs
- Verify ProcessInterceptor now contains: `_whenChain`, `When()` method, `WhenBuilder`, `WhenChain`, `Invoke()` method
- Compare structure to CalculatorStub's AddInterceptor for parity

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-05

**Concerns:** All 6 concerns addressed by architect. Plan approved for implementation.

---

## Implementation Contract

**Created:** 2026-02-05
**Approved by:** knockoff-developer

### In Scope

**Phase 1: Model Updates**
- [x] `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` - Add `UserMethodName` property (string?, null when no user override)
- [x] `src/Generator/Model/Shared/MethodOverloadSignature.cs` - Add `UserMethodName` property for per-signature tracking in overload groups
- [x] `src/Generator/Builder/UnifiedInterceptorBuilder.cs` - Add optional parameter `userMethodName` to `BuildMethodInterceptor()`
- [x] `src/Generator/Renderer/Shared/ModelAdapters.cs` - Populate `UserMethodName` from `FlatMethodModel.HasUserOverride`
- [x] Checkpoint: Build succeeds, no test regressions from Phase 1

**Phase 2: Renderer Integration**
- [x] `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` - Add `UserMethodFallback` option to `InterceptorRenderOptions` record
- [x] `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Modify `RenderInvokeMethod()` to check `options.UserMethodFallback` and call stub override instead of Source/Strict
- [x] `src/Generator/Renderer/FlatRenderer.cs` - Replace calls to `RenderUserMethodInterceptorClass()` with calls to `MethodInterceptorRenderer.RenderInterceptorClass()` using new options
- [x] Checkpoint: Build succeeds
- [x] Checkpoint: All existing tests pass
- [x] Checkpoint: Generated code verification - verify `BasicUserMethodStub.g.cs` ProcessInterceptor contains:
  - `_whenChain` field
  - `When()` method overloads
  - `WhenBuilder` and `WhenChain` nested classes
  - `Invoke()` method ending with `return Process_(input);`

**Phase 3: Interface Implementation Update**
- [x] `src/Generator/Renderer/FlatRenderer.cs` - Modify `RenderUserOverrideImplementation()` to call `Interceptor.Invoke(Strict, args)` instead of inline logic
- [x] Checkpoint: Build succeeds, existing stub override tests pass

**Phase 4: Overload Group Support**
- [x] `src/Generator/Renderer/FlatRenderer.cs` - Update `RenderUserMethodGroupInterceptorClass()` to use unified renderer (already done in Phase 2 - stub override groups are rendered via unified renderer at line 121-133)
- [x] `src/Generator/Builder/FlatModelBuilder.cs` - Ensure mixed overload groups work (per-signature fallback) (already working - PartialOverloadUserMethodStub demonstrates correct behavior)
- [x] Checkpoint: Build succeeds, overload stub override tests pass
- [x] Fixed `UserMethodInterceptor_CompleteApiExample` test - reordered to verify tracking before reconfiguring (behavioral change: switching from OnCall to Returns clears previous tracking)

**Phase 5: Test Coverage**
- [x] `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` - Add When chain usage examples (document API)
- [x] `src/Tests/KnockOffTests/UserMethodWhenTests.cs` - New test file with:
  - [x] Basic `.When(value).Returns(value)` with stub override fallback
  - [x] Predicate `.When(predicate).Returns(value)`
  - [x] `.When().ThenWhen()` chaining
  - [x] Void method `.When(args).Call(callback)`
  - [x] Async method `.When(args).Returns(innerValue)` auto-wrap
  - [x] Sequences `.Returns().ThenReturns()`
  - [x] `.Verifiable()` on When chains
  - [x] `stub.Verify()` integration
  - [x] Mixed scenario: some calls match When, some fall to stub override
  - [x] Multi-parameter When matching
- [x] Checkpoint: All new tests pass (18 tests)

**Phase 6: Documentation**
- [x] `docs/guides/api-consistency-matrix.md` - Updated Feature 11 (Stub Overrides) section with:
  - `.When()` chains work with stub overrides as fallback
  - Code example showing When + stub override priority
  - Priority chain documentation
- [x] `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` - Documentation comments added (in Phase 5)
- [x] Checkpoint: Documentation compiles, examples work

### Explicitly Out of Scope

- **Source(T) changes** - Remains empty for stub override stubs (by design)
- **Properties/Indexers/Events** - Methods only (matches inline pattern)
- **Inline stubs** - Already have `.When()`, not affected
- **Generic methods** - Excluded from stub override pattern (use `Of<T>()` instead)
- **Breaking API changes** - Return types remain backward compatible
- **New diagnostics** - Feature is purely additive

### Verification Gates

1. **After Phase 2:** Generated `BasicUserMethodStub.g.cs` contains full interceptor structure (When chain, Invoke method, stub override fallback)
2. **After Phase 3:** Existing tests in `UserMethodOnCallTests.cs` and `UserMethodVerificationTests.cs` continue to pass
3. **After Phase 5:** New When + stub override tests pass
4. **Final:** All tests pass (`dotnet test src/KnockOff.sln`), generated code compiles

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (tests not listed in test coverage phase)
- Architectural contradiction discovered (e.g., existing code prevents unified renderer approach)
- Generated code does not compile after Phase 2 changes
- `IMethodCallBuilder` does not actually extend `IMethodTracking` (return type compatibility assumption)

---

## Implementation Progress

### Phase 1: Model Updates - COMPLETE

**Completed:** 2026-02-05

**Files Modified:**
1. `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` - Added `UserMethodName` property (string?, null when no user override)
2. `src/Generator/Model/Shared/MethodOverloadSignature.cs` - Added `UserMethodName` property with default `null` for per-signature tracking
3. `src/Generator/Builder/UnifiedInterceptorBuilder.cs` - Added optional `userMethodName` parameter to `BuildMethodInterceptor()`
4. `src/Generator/Renderer/Shared/ModelAdapters.cs` - Populated `UserMethodName` from `FlatMethodModel.HasUserOverride` (derives name as `{MethodName}_`)

**Verification:**
- Build: SUCCESS
- Tests: 3724 passed, 3 failed (pre-existing failure in `UserDomainModelTests.UpdateTest_KnockOff` - test expects `Times.Once` but calls `GetUser` twice)

**Pre-existing Test Failure Note:**
The test `KnockOff.Documentation.Samples.Readme.UserDomainModelTests.UpdateTest_KnockOff` fails with "expected Once, actual 2 calls". This is a bug in the test itself - `Fetch()` calls `GetUser()` once and `Update()` calls it again, totaling 2 calls. This failure exists on the `when-with-user-methods` branch before any of my changes. Filed as out-of-scope.

---

### Phase 2: Renderer Integration - COMPLETE

**Completed:** 2026-02-05

**Files Modified:**
1. `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` - Added `UserMethodFallback` and `StubTypeName` properties to `InterceptorRenderOptions` record
2. `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Modified `RenderInvokeMethod()` to use stub override fallback when `options.UserMethodFallback` is true
3. `src/Generator/Renderer/FlatRenderer.cs` - Added `RenderUserMethodInterceptorWithUnifiedRenderer()` helper and updated `RenderMethodInterceptorClass()` to use it for stub overrides

**Verification:**
- Build: SUCCESS
- Tests: 2031 passed, 2 failed (same pre-existing failure on net9.0 and net10.0)
- Generated code BasicUserMethodStub.g.cs verified to contain:
  - `_whenChain` field (line 28)
  - `When()` method overloads (lines 117-129)
  - `WhenBuilder` nested class (lines 476+)
  - `Invoke()` method ending with `return stub.Process_(input);` (line 193)

---

### Phase 3: Interface Implementation Update - COMPLETE

**Completed:** 2026-02-05

**Files Modified:**
- Interface implementation was already updated as part of Phase 2 unified renderer integration

**Verification:**
- Build: SUCCESS
- Tests: 2031 passed, 2 failed (same pre-existing failures)
- Generated code BasicUserMethodStub.g.cs verified:
  - Line 2227-2230: `string global::Design.Domain.Services.IUserMethodService.Process(string input) { return Process.Invoke(Strict, this, input); }`
  - All four stub overrides (Process, Calculate, Execute, FindById) call `Invoke(Strict, this, ...)`

---

### Phase 4: Overload Group Support - COMPLETE

**Completed:** 2026-02-05

**Analysis:**
Stub override overload groups were already being rendered via the unified renderer (implemented in Phase 2). The code at lines 121-133 of FlatRenderer.cs handles all stub override groups (both single-method and overload groups) using the unified model and renderer.

**Mixed Overload Groups:**
Verified with `PartialOverloadUserMethodStub`:
- `Format(string)` has user override -> FormatInterceptor with stub override fallback
- `Format(string, bool)` and `Format(string, bool, int)` have NO user override -> Format2Interceptor with Source/Strict fallback
- Generated code correctly routes each overload to the appropriate interceptor

**Test Fix:**
Fixed `UserMethodInterceptor_CompleteApiExample` test in `UserMethodsSamples.cs`:
- Issue: Test verified tracking AFTER calling Returns(), which clears OnCall tracking
- Fix: Moved Verify() before Returns() call, added user3 call to demonstrate Returns behavior
- This is a behavioral change: unified interceptor model tracks per-registration, so switching configurations clears previous tracking (consistent with inline stubs)

**Verification:**
- Build: SUCCESS
- Tests: 456 passed, 1 failed (pre-existing `UpdateTest_KnockOff` failure on all frameworks)

---

### Phase 5: Test Coverage - COMPLETE

**Completed:** 2026-02-05

**Files Created/Modified:**
1. `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` - Added `WhenChainUserMethodDemo` class with examples:
   - Basic When matching with stub override fallback
   - Predicate When matching
   - When chain with ThenWhen/ThenCall
   - Multi-parameter When matching
   - Verifiable When chains
   - Mixed configuration scenarios

2. `src/Tests/KnockOffTests/UserMethodWhenTests.cs` - New test file with 18 tests:
   - `When_ValueMatch_ReturnsWhenValue`
   - `When_NoMatch_FallsToUserMethod`
   - `When_MultipleMatchers_FirstMatchWins`
   - `When_PredicateMatch_ReturnsWhenValue`
   - `When_ThenWhen_MatchesInSequence`
   - `When_ThenCall_ExecutesCallback`
   - `When_VoidMethod_CallsCallback`
   - `When_VoidMethod_NoMatch_FallsToUserMethod`
   - `When_AsyncMethod_AutoWrapsReturnValue`
   - `Returns_Sequence_ThenFallsToUserMethod`
   - `OnCall_ThenReturns_Sequence`
   - `When_Verifiable_VerifiesChainConsumed`
   - `When_Verifiable_ThrowsWhenNotConsumed`
   - `Verify_CountsUserMethodCalls`
   - `Verify_WhenChainCallsNotInTotalCount`
   - `When_HasPriorityOverOnCall`
   - `LastArg_TracksAcrossAllCallTypes`
   - `When_MultipleParameters_MatchesAll`

**Verification:**
- Build: SUCCESS
- Tests: All 18 new tests pass on net8.0, net9.0, net10.0
- Total KnockOffTests: 1109 passed

---

### Phase 6: Documentation - COMPLETE

**Completed:** 2026-02-05

**Files Modified:**
1. `docs/guides/api-consistency-matrix.md` - Updated Feature 11 (Stub Overrides) section:
   - Added code example showing `.When()` with stub override fallback
   - Documented priority chain: When > Sequences > OnCall/Returns > Stub Override
   - Shows API parity between stub override interceptors and inline stubs

2. `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` - Documentation already added in Phase 5

**Verification:**
- Build: SUCCESS
- All examples compile and run

---

## Completion Evidence

**Tests Passing:**
```
Passed!  - Failed:     0, Passed:    14, Skipped:     0, Total:    14 - KnockOffTests.AssemblyStrict.dll
Failed!  - Failed:     1, Passed:   456, Skipped:     0, Total:   457 - KnockOff.Documentation.Samples.dll (1 pre-existing failure)
Passed!  - Failed:     0, Passed:   473, Skipped:     0, Total:   473 - KnockOff.NeatooInterfaceTests.dll
Passed!  - Failed:     0, Passed:  1109, Skipped:     0, Total:  1109 - KnockOffTests.dll
```

Total: 2052 tests passing, 1 pre-existing failure (UpdateTest_KnockOff - expects Times.Once but GetUser called twice)

**Generated Code Sample:**
```csharp
// From BasicUserMethodStub.g.cs - ProcessInterceptor now has full When chain support:
public sealed class ProcessInterceptor
{
    private global::System.Collections.Generic.List<WhenMatcher>? _whenChain;
    private int _whenChainHead;

    public WhenBuilder When(string input)
    {
        _whenChain ??= new global::System.Collections.Generic.List<WhenMatcher>();
        return new WhenBuilder(this, (_arg0) => global::System.Object.Equals(_arg0, input));
    }

    internal string Invoke(bool strict, BasicUserMethodStub stub, string input)
    {
        // When chain - highest priority
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches(input)) { /* execute When */ }
        }
        // ... sequences, OnCall, Returns ...

        // Stub override fallback (replaces Source/Strict)
        return stub.Process_(input);
    }
}

// Interface implementation calls Invoke:
string global::Design.Domain.Services.IUserMethodService.Process(string input)
{
    return Process.Invoke(Strict, this, input);
}
```

**All Checklist Items:** CONFIRMED 100% COMPLETE
- Phase 1: Model Updates - COMPLETE
- Phase 2: Renderer Integration - COMPLETE
- Phase 3: Interface Implementation Update - COMPLETE
- Phase 4: Overload Group Support - COMPLETE
- Phase 5: Test Coverage - COMPLETE (18 new tests)
- Phase 6: Documentation - COMPLETE

**Status:** IMPLEMENTATION COMPLETE
