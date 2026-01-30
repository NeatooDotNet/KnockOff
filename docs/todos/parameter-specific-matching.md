# Parameter-Specific Matching (When API)

**Status:** Complete
**Priority:** High
**Created:** 2026-01-27
**Last Updated:** 2026-01-30 (Phase 15 complete - FEATURE COMPLETE)

---

## Problem

KnockOff's current `OnCall()` API applies to ALL invocations regardless of parameter values. Users cannot configure different return values for different argument combinations like NSubstitute's `calc.Add(1, 2).Returns(3)`.

Current workaround requires conditional logic in callbacks:
```csharp
stub.Add.OnCall((a, b) => (a, b) switch {
    (1, 2) => 3,
    (5, 7) => 12,
    _ => 0
});
```

## Solution

Add a fluent `When()` API for parameter-specific matching with sequential consumption:

```csharp
// Single When (repeats when matched)
stub.Add.When(1, 2).Returns(3);

// Chain with ThenWhen (last repeats when matched)
stub.Add
    .When(1, 2).Returns(3)
    .ThenWhen(5, 7).Returns(12)
    .ThenWhen((a, b) => a > 10).Returns(100);

// Chain with ThenCall (unconditional callback, repeats)
stub.Add
    .When(1, 2).Returns(3)
    .ThenCall((a, b) => a + b);

// Chain with ThenNone (explicit close, exhausts)
stub.Add
    .When(1, 2).Returns(3)
    .ThenWhen(5, 7).Returns(12)
    .ThenNone();

// Fallback coexists
stub.Add.OnCall((a, b) => 0);  // Used when When doesn't match or exhausted

// Void methods - When() returns chain directly (no builder, nothing to return)
stub.VoidMethod.When(1, 2);  // Tracking starts
stub.VoidMethod.When(1, 2).Verify(Times.Exactly(2));  // Parameter-specific verification
stub.VoidMethod.When(1, 2).Call((a, b) => Log(a, b));  // Optional callback
```

### Architecture Decision: Sequential HEAD Matching

**Chosen approach:** When chain with HEAD pointer. Only HEAD is checked per call. When matched, HEAD advances (except last ThenWhen which repeats).

**Last matcher behavior:**
- Last `ThenWhen`: repeats when matched, falls through when not
- `ThenCall`: unconditional callback, repeats forever (terminal)
- `ThenNone`: explicit exhaustion, always falls through (terminal)

**Invoke priority chain:**
1. **When chain (NEW - HEAD match check)**
2. Sequence (existing)
3. Returns value (existing)
4. OnCall callback (existing)
5. Source delegation (existing)
6. Strict mode check (existing)
7. Default (existing)

**When chains coexist with Sequence/Returns/OnCall** - When is checked first, rest serves as fallback.

---

## Prerequisites

- [Returns API Rename](./returns-api-rename.md) - establishes `Returns()` naming before `When().Returns()` is added

---

## Plans

- [Parameter-Specific Matching Design](../plans/parameter-specific-matching-design.md) - Complete architecture with When/ThenWhen/ThenCall/ThenNone API

---

## Requirements

### Core Features
- [x] Fluent API: `stub.Method.When(...).Returns(...)`
- [x] Exact value matching: `When(1, 2)`
- [x] Predicate matching: `When((a, b) => a > 0 && b < 10)` (single Func, no Arg class)
- [x] Sequential HEAD matching (only current matcher checked)
- [x] Last ThenWhen repeats when matched, falls through when not
- [x] `ThenCall(callback)` - unconditional terminal that repeats
- [x] `ThenNone()` - explicit exhaustion terminal
- [x] Fallback chain: When → Sequence → Returns/OnCall → Strict → Default
- [x] Methods only (properties/indexers later)
- [x] No `Arg` class - dropped in favor of predicate overload

### Tracking & Verification
- [x] Shared `IWhenTracking` for entire chain
- [x] `ITracking` base interface shared with `IMethodTracking`
- [x] `Verify()` - chain reached terminal state (no Times overload)
- [x] `Reset()` - reset HEAD to first matcher
- [x] `Verifiable()` for batch verification

### Void Methods (Phase 11)
- [x] `IVoidWhenChain<TDelegate>` - When() returns chain directly (no builder needed)
- [x] `Call(callback)` - optional callback for parameter-matched execution
- [x] `Verify(Times)` - parameter-specific verification

### Deferred
- [ ] Property/Indexer support (correctly deferred - no parameters / complexity not justified)
- [ ] Event support (correctly deferred - no return value, When() not applicable)

### Completion Gate
**Feature is NOT complete until all four patterns are implemented:**
- [x] Standalone pattern
- [x] Inline Interface pattern
- [x] Inline Class pattern (IMPLEMENTED in Phase 13)
- [x] Inline Delegate pattern

**Additional Requirements (Gaps Found 2026-01-30):**
- [x] ThenWhen() must be accessible from Returns() result (FIXED in Phase 12)
- [x] Async auto-wrapping for When().Returns() (FIXED in Phase 14)
- [x] Tests that actually use ThenWhen() fluent API (ADDED in Phase 12)

---

## Tasks

### Phase 1: Public Interfaces
- [ ] Create `src/KnockOff/ITracking.cs` with base interface
- [ ] Modify `IMethodTracking` to extend `ITracking`
- [ ] Create `src/KnockOff/IWhenTracking.cs` with `IWhenTracking`, `IWhenChain<T>`, `IWhenBuilder<T>`

### Phase 2-3: Matcher & Builder Classes
- [ ] Add `WhenMatcher` abstract base class generation
- [ ] Add `WhenMatcherValue`, `WhenMatcherCall`, `WhenMatcherNone` implementations
- [ ] Add `WhenBuilderImpl` nested class generation
- [ ] Add `WhenChainImpl` nested class generation

### Phase 4-5: Storage & Entry Point
- [ ] Add `_whenChain` and `_whenChainHead` fields to interceptors
- [ ] Add `BuildWhenPredicateType()` helper to `UnifiedInterceptorBuilder`
- [ ] Generate `When()` value and predicate overloads
- [ ] Update `Reset()` to clear When chain state

### Phase 6-7: Priority & Verification
- [ ] Add When chain check at TOP of `Invoke()` priority chain
- [ ] Implement HEAD matching with last-matcher repeat logic
- [ ] Update `IsConfigured` and verification methods

### Phase 8: Overloads
- [ ] Replicate for overload group interceptors

### Phase 9: Inline Delegate Pattern
- [ ] Add When chain support to InlineDelegateStubModel
- [ ] Test delegate stub When chains

### Phase 10: Testing (Non-Void)
- [ ] Add comprehensive tests for all four patterns

### Phase 11: Void Method Support
- [x] Add `IVoidWhenChain<TDelegate>` interface
- [x] Generate `VoidWhenChainImpl` (no builder, chain returned directly from When)
- [x] Implement `Call(callback)` for optional callbacks
- [x] Implement `Verify(Times)` for parameter-specific verification
- [x] Test void method When chains for all four patterns

### Phase 12: Fix ThenWhen Interface Exposure (CRITICAL GAP) - COMPLETE
Architecture clarified 2026-01-30:
- Rename `WhenChainImpl` to `WhenChain` (drop "Impl" suffix for public class)
- Rename `WhenBuilderImpl` to `WhenBuilder` (drop "Impl" suffix for public class)
- Make `WhenChain` and `WhenBuilder` public
- Return concrete types (not interfaces) for fluent chaining
- Add explicit interface implementations for `Returns()` and `Verifiable()` to satisfy interfaces
- [x] Rename and make public in `MethodInterceptorRenderer.cs`
- [x] Update return types to concrete `WhenChain` / `WhenBuilder`
- [x] Apply same changes for overload groups
- [x] Apply same changes in `InlineRenderer.cs` for delegate stubs
- [x] Apply same changes in `WhenChainRenderer.cs` (shared renderer)
- [x] Apply same changes for void method `VoidWhenChain`
- [x] Add tests for actual `ThenWhen()` fluent chaining (3 new tests)

### Phase 13: Add When() Support to Inline Class Pattern - COMPLETE
Architecture clarified 2026-01-30:
- Add `Invoke()` method to inline class interceptors with full priority chain
- Impl class becomes thin pass-through (calls Invoke when IsConfigured)
- Consistent architecture across all four patterns
- [x] Add When chain storage fields to `ClassRenderer.RenderMethodInterceptorClass()`
- [x] Add matcher classes (WhenMatcher, WhenMatcherValue, etc.)
- [x] Add `WhenBuilder` and `WhenChain` public classes
- [x] Add `When()` entry points (value and predicate overloads)
- [x] Add `Returns()` method for simple return value configuration
- [x] Add `Invoke()` method with full priority chain (using `out bool handled` pattern)
- [x] Update `RenderImplMethodOverride()` to call Invoke when IsConfigured
- [x] Handle virtual vs abstract methods (base fallback vs default)
- [x] Add void method support with `VoidWhenChain` (implements `IVoidWhenChain`)
- [x] Add tests for inline class When() chains (void and non-void) - 15 new tests

### Phase 14: Fix Async Auto-Wrapping for When Chain - COMPLETE
Architecture clarified 2026-01-30:
- Generate only `Returns(TUnwrapped)` for async methods (no `Returns(Task<T>)`)
- Auto-wrap with `Task.FromResult()` internally
- Avoids overload ambiguity
- [x] Add `GetAsyncTypeInfo()` helper (extracts inner type from Task<T>/ValueTask<T>)
- [x] Update `WhenBuilderImpl` generation for async methods
- [x] Apply to `MethodInterceptorRenderer.cs`
- [x] Apply to `InlineRenderer.cs` for delegate stubs
- [x] Apply to `ClassRenderer.cs` for inline class
- [x] Apply to `WhenChainRenderer.cs` (shared renderer)
- [x] Add tests confirming auto-wrapping works (5 tests updated + 1 new ThenWhen chain test)

### Phase 15: Complete Testing and Documentation - COMPLETE
- [x] Add tests using actual `ThenWhen()` fluent chaining (confirmed in Phase 12)
- [x] Remove workaround comments from test file (no workarounds found - file clean)
- [x] Fill in Completion Evidence section with actual evidence
- [x] Update all completion gate checkboxes

---

## Progress Log

**2026-01-27:** Initial feature exploration and architecture design. Identified two approaches (Matcher List vs Composite Callback). Selected Matcher List approach for first-match-wins support. Compared with NSubstitute's Arg features - identified core features to include and features to defer (Arg.Do, Arg.Invoke, post-hoc verification). Added prerequisite: Returns API Rename todo - establishes `Returns()` naming before `When().Returns()` is added.

**2026-01-29:** Architect review resolved. All 11 issues addressed:
- Generic methods: Apply same design with `Of<T>()`
- Inline class stubs: Yes, support `When()` for all four patterns (including Inline Delegate)
- ITracking ambiguity: Keep `Verify()` on both - not confusing
- Ref/out parameters: Deferred
- Async wrapping: Yes, same auto-wrapping
- Overload groups: "Replicate the pattern" is sufficient
- Test plan gaps: Current list is sufficient
- Mutual exclusivity: When clears nothing - all coexist with priority chain
- ITracking base: Keep shared
- Simplification: Keep all three terminals
- ThenNone chainability: Intentional - terminals end the chain
Plan status updated to "Ready for Implementation".
Added completion gate: feature requires all four patterns (Standalone, Inline Interface, Inline Class, Inline Delegate).
Properties/Indexers correctly deferred (no parameters / complexity not justified).

**2026-01-28:** Comprehensive design session. Key decisions:
- Dropped `Arg` class entirely - use `When(Func<T1, T2, bool>)` predicate overload instead
- Changed from first-match-wins to sequential HEAD matching (only current matcher checked)
- Added `ThenWhen()` for chaining conditions
- Added `ThenCall(callback)` for unconditional terminal that repeats
- Added `ThenNone()` for explicit exhaustion terminal
- Last `ThenWhen` in chain repeats when matched (not consumed)
- Created `ITracking` base interface shared by `IMethodTracking` and `IWhenTracking`
- `IWhenTracking.Verify()` checks terminal state (no Times overload)
- Shared tracking for entire When chain
- When chains coexist with Sequence/Returns/OnCall as fallback
- Created detailed plan: `docs/plans/parameter-specific-matching-design.md`

**2026-01-29:** Developer review completed. All 6 concerns addressed, Implementation Contract created.
Added void method support (Phase 11):
- `IVoidWhenChain<TDelegate>` - When() returns chain directly (no builder needed, nothing to return)
- `Call(callback)` - optional callback for parameter-matched execution
- `Verify(Times)` - enables parameter-specific verification for void methods
- Simplified API: no `Matches()` method needed since void has no return value to configure

**2026-01-29:** Architect review of void method additions completed. Clarifications added:
- `Verify()` vs `Verify(Times)` semantics documented (terminal state vs call count)
- Null parameter matching guidance (use predicate: `When(x => x == null)`)
- Reset() behavior clarified (clears HEAD AND individual matcher CallCounts for full reset)
- Parameterless methods documented (cannot use When, use Returns/OnCall instead)
- Void delegate (`Action<...>`) testing explicitly added to Phase 11

**2026-01-30:** Implementation of Phases 1-11 complete. All 901 tests pass (net9.0/net10.0), 900 pass (net8.0).

**2026-01-30:** GAP ANALYSIS PERFORMED - Critical gaps identified:
1. **CRITICAL:** `ThenWhen()` not exposed on `IWhenChain` interface - `Returns()` returns interface type, making `ThenWhen()` (on private impl class) inaccessible. The documented fluent API `stub.Add.When(1,2).Returns(3).ThenWhen(5,7).Returns(12)` does not compile.
2. **HIGH:** Inline Class pattern does NOT generate When() support at all - interceptors only have `OnCall()`.
3. **MEDIUM:** Async auto-wrapping not implemented for When chain - requires manual `Task.FromResult()` unlike regular `Returns()`.
4. **HIGH:** No tests actually use `ThenWhen()` fluent API - all use multiple `When()` calls as workaround.
5. **MEDIUM:** Completion Evidence section is empty.

New phases 12-15 added to address gaps. Status changed back to "In Progress".

**2026-01-30:** ARCHITECT CLARIFICATION for developer concerns on Phases 12-14:
- **Phase 12:** Rename `WhenChainImpl` to `WhenChain` (drop "Impl" for public classes). Return concrete types for fluent chaining.
- **Phase 13:** Add `Invoke()` method to inline class interceptors with full priority chain. Impl class becomes thin pass-through. Consistent architecture across all patterns.
- **Phase 14:** Generate only `Returns(TUnwrapped)` for async methods. Auto-wrap internally. No `Returns(Task<T>)` overload to avoid ambiguity.

Implementation order: Phase 12 before Phase 13 (inline class needs WhenChain pattern established). Phase 14 can parallel Phase 13 for non-inline-class, then apply to inline class.

**2026-01-30:** PHASE 12 COMPLETE - ThenWhen fluent chaining now works.
Changes made:
- Renamed `WhenChainImpl` to `WhenChain` and `WhenBuilderImpl` to `WhenBuilder` (public classes)
- Changed from `private sealed` to `public sealed` for all When chain classes
- Updated `Returns()` to return concrete `WhenChain` type
- Updated `When()` entry points to return concrete `WhenBuilder` type
- Updated `ThenWhen()` methods to return concrete `WhenBuilder` type
- Updated `Verifiable()` to return concrete types
- Added explicit interface implementations for interface method contracts
- Applied changes to: `MethodInterceptorRenderer.cs`, `WhenChainRenderer.cs`, `InlineRenderer.cs`
- Applied to: standalone pattern, inline interface pattern, inline delegate pattern, overload groups
- Applied to both non-void (`WhenBuilder`/`WhenChain`) and void (`VoidWhenChain`) variants
- Added 3 new tests demonstrating ThenWhen fluent chaining works
- All tests pass (904/903 tests - 3 new tests added)

**2026-01-30:** PHASE 13 COMPLETE - Inline Class pattern now supports When() chains.
Changes made in `ClassRenderer.cs`:
- Added When chain storage fields (`_whenChain`, `_whenChainHead`, `_whenVerifiable`) to interceptors
- Added matcher classes: `WhenMatcher`, `WhenMatcherValue`, `WhenMatcherCall`, `WhenMatcherNone`
- Added public `WhenBuilder` and `WhenChain` classes for fluent API
- Added `When()` entry points (exact value and predicate overloads)
- Added `Returns()` method for simple return value configuration
- Added `Invoke()` method with full priority chain and `out bool handled` parameter
- Updated `RenderImplMethodOverride()` to use `handled` out parameter for base class fallback
- Added void method support with `VoidWhenMatcher`, `VoidWhenMatcherPredicate`, `VoidWhenMatcherCall`, `VoidWhenMatcherNone`, and `VoidWhenChain` classes
- Added 15 new tests for inline class When() chains covering: basic When/Returns, predicate matching, ThenWhen chaining, ThenCall terminal, ThenNone fallback, verification, void methods
- All 919 tests pass

Key architectural decisions:
- Used `out bool handled` parameter in `Invoke()` to signal whether the call was handled
- When `handled=false`, Impl class falls back to base class implementation (for virtual methods)
- Maintains consistent priority chain: When > Sequence > Returns > OnCall > Base/Default

**2026-01-30:** PHASE 14 COMPLETE - Async auto-wrapping for When().Returns() now works.
Changes made:
- Added `GetAsyncTypeInfo()` helper to extract inner type from Task<T>/ValueTask<T>
- Updated `RenderWhenBuilderImpl` in `MethodInterceptorRenderer.cs` to detect async methods and generate Returns(TInner) that auto-wraps
- Updated `RenderDelegateWhenBuilderImpl` in `InlineRenderer.cs` for delegate stubs
- Updated `RenderWhenBuilderClass` in `ClassRenderer.cs` for inline class pattern
- Updated `RenderWhenBuilderImpl` in `WhenChainRenderer.cs` (shared renderer)
- For async methods: Returns() accepts unwrapped type, internally wraps with Task.FromResult() or new ValueTask<T>()
- Updated 5 async tests to use new auto-wrapping syntax (removed Task.FromResult)
- Added 1 new test for ThenWhen chain with async auto-wrapping
- All 920 tests pass (net9.0/net10.0), 919 pass (net8.0)

**2026-01-30:** PHASE 15 COMPLETE - Final cleanup and documentation.
Actions taken:
- Reviewed `WhenChainTests.cs` for workaround comments - file is clean (96 tests)
- Confirmed ThenWhen fluent chaining tests exist for all four patterns (added in Phase 12)
- Updated all completion gate checkboxes - all four patterns checked
- Filled in Completion Evidence section with test counts and code samples
- Cleared gaps from "What Does NOT Work" section (all fixed)
- Updated status to "Complete"
- All 920 tests pass (net9.0/net10.0), 919 pass (net8.0)

---

## Results / Conclusions

**STATUS: COMPLETE** - All phases complete. Feature fully implemented.

---

## Completion Evidence

### Test Results

**All tests pass across all target frameworks:**
- net8.0: 919 tests passed
- net9.0: 920 tests passed
- net10.0: 920 tests passed

**WhenChain tests specifically:** 96 tests covering all four patterns and all features.

### Generated Code Sample (ThenWhen Fluent Chaining)

```csharp
// From WhenChainTests.cs - demonstrates full fluent API works
stub.Add
    .When(1, 2).Returns(100)
    .ThenWhen(3, 4).Returns(200)
    .ThenWhen((a, b) => a > 100).Returns(999);

Assert.Equal(100, service.Add(1, 2));   // First matcher
Assert.Equal(200, service.Add(3, 4));   // Second matcher
Assert.Equal(999, service.Add(150, 0)); // Third matcher (predicate)
Assert.Equal(999, service.Add(200, 0)); // Last matcher repeats
```

### All Four Patterns Verified

| Pattern | Basic When | ThenWhen Chaining | Void Methods | Async Auto-Wrap |
|---------|-----------|-------------------|--------------|-----------------|
| Standalone | Yes | Yes | Yes | Yes |
| Inline Interface | Yes | Yes | Yes | N/A |
| Inline Class | Yes | Yes | Yes | N/A |
| Inline Delegate | Yes | Yes | Yes | N/A |

### Tests Demonstrating Each Pattern with ThenWhen

1. **Standalone:** `Standalone_When_ThenWhen_FluentChaining_NonVoid` (line 52)
2. **Inline Interface:** `InlineInterface_When_ThenWhen_FluentChaining` (line 670)
3. **Inline Class:** `InlineClass_When_ThenWhen_FluentChaining` (line 812)
4. **Inline Delegate:** `InlineDelegate_When_ThenWhen_FluentChaining` (line 1039)

---

## Summary of What Works

### Core Features (All Implemented)
- `When(value).Returns(value)` for ALL FOUR PATTERNS
- `When(predicate).Returns(value)` for all four patterns
- **ThenWhen() fluent chaining** (Phase 12 fix)
- `ThenCall()` and `ThenNone()` terminal operations
- Fallback to OnCall/Returns/Sequence when When doesn't match
- **Inline Class pattern: When() falls back to base class when not matched** (Phase 13)
- Void method support with `When().Call()` and `Verify(Times)` for all four patterns
- **Async auto-wrapping: When().Returns("value") auto-wraps with Task.FromResult()** (Phase 14)

### All Gaps Resolved
- ThenWhen() chaining accessibility - **FIXED in Phase 12**
- Inline Class pattern When() support - **FIXED in Phase 13**
- Async auto-wrapping - **FIXED in Phase 14**

### Code Examples

**Async Auto-Wrapping:**
```csharp
// Clean API - no Task.FromResult needed
stub.GetAsync.When("hello").Returns("HELLO");
```

**ThenWhen Fluent Chaining:**
```csharp
stub.Add
    .When(1, 2).Returns(100)
    .ThenWhen(3, 4).Returns(200)
    .ThenWhen((a, b) => a > 100).Returns(999);
```

**Void Method Parameter-Specific Verification:**
```csharp
var chain = stub.Process.When(1, 2);
service.Process(1, 2);
service.Process(1, 2);
chain.Verify(Times.Exactly(2));  // Parameter-specific count!
```

### Key Design Decisions
- No `Arg` class - use predicates instead: `When(x => x == null)`
- Sequential HEAD matching (only current matcher checked per call)
- Last ThenWhen repeats when matched, falls through when not
- When chains coexist with OnCall/Returns/Sequence as fallback
- `Verify()` = terminal state reached; `Verify(Times)` = call count

### Deferred (Correctly Out of Scope)
- Property/Indexer support - no parameters, complexity not justified
- Event support - no return value, When() not applicable

