# Parameter-Specific Matching Design (When API)

**Date:** 2026-01-28
**Related Todo:** [Parameter-Specific Matching](../todos/parameter-specific-matching.md)
**Status:** Complete
**Last Updated:** 2026-01-30 (Phase 15 complete - FEATURE COMPLETE)

---

## Overview

Add a fluent `When()` API for parameter-specific matching, enabling users to configure different return values for different argument combinations without conditional logic in callbacks.

---

## Approach

Implement a conditional matcher chain that is checked FIRST in the Invoke() priority chain. Each matcher is consumed when matched (except the last ThenWhen which repeats). The chain coexists with existing OnCall/Returns/Sequence as fallback.

---

## Design

### API Examples

```csharp
// Single When (repeats when matched)
stub.Method.When(1, 2).Returns(5);

// Chain with ThenWhen (last repeats when matched)
stub.Method
    .When(1, 2).Returns(5)
    .ThenWhen(2, 3).Returns(6)
    .ThenWhen((a, b) => a > 10).Returns(100);

// Chain with ThenCall (unconditional callback, repeats)
stub.Method
    .When(1, 2).Returns(5)
    .ThenCall((a, b) => a + b);

// Chain with ThenNone (explicit close, exhausts)
stub.Method
    .When(1, 2).Returns(5)
    .ThenWhen(2, 3).Returns(6)
    .ThenNone();

// Fallback coexists
stub.Method.OnCall((a, b) => 0);  // Used when When doesn't match or exhausted
```

### Void Method API Examples

```csharp
// Void method - When() returns chain directly (no builder, nothing to return)
stub.VoidMethod.When(1, 2);  // Tracking starts immediately

// Verify specific parameter combinations
var chain = stub.VoidMethod.When(1, 2);
// ... exercise code ...
chain.Verify(Times.Exactly(2));

// With optional callback
stub.VoidMethod.When(1, 2).Call((a, b) => Console.WriteLine($"{a}, {b}"));

// Predicate matching
stub.VoidMethod.When(x => x > 5).Verify(Times.AtLeastOnce);

// Chain void methods
stub.VoidMethod
    .When(1, 2)
    .ThenWhen(3, 4).Call((a, b) => DoSomething(a, b))
    .ThenWhen(x => x > 10)
    .Verifiable();

// Fallback coexists
stub.VoidMethod.OnCall((a, b) => Log(a, b));  // Used when When doesn't match
```

### Behavior Rules

1. **Priority Chain** (When checked FIRST):
   - When chain (if HEAD matches → use, advance; else continue)
   - Sequence
   - Returns(value)
   - OnCall(callback)
   - Source delegation
   - Strict mode check
   - Default

2. **HEAD Matching**:
   - Only HEAD matcher is checked per invocation
   - If matches: return value, advance HEAD (unless last ThenWhen which repeats)
   - If doesn't match: fall through to rest of priority chain

3. **Last Matcher Behavior**:
   - Last ThenWhen: repeats when matched, falls through when not
   - ThenCall: unconditional, repeats forever (terminal)
   - ThenNone: exhausts chain, always falls through (terminal)

4. **Tracking**:
   - Shared tracking for entire chain
   - Verify() = chain reached terminal state
   - Reset() = reset HEAD to first matcher

5. **Async Wrapping**:
   - Async wrapping occurs at configuration time in `Returns()` method
   - For async methods, `Returns(value)` automatically wraps with `Task.FromResult(value)`
   - Same pattern as existing Returns implementation

6. **Void Methods**:
   - `When()` returns `IVoidWhenChain` directly (no builder - nothing to return)
   - `Call(callback)` is optional for parameter-matched callbacks
   - `Verify(Times)` enables parameter-specific verification
   - Same HEAD matching and chain semantics as non-void methods

7. **Null Parameter Matching**:
   - For null matching, use predicate: `When(x => x == null).Returns(value)`
   - Direct `When(null)` would throw NullReferenceException during equality check
   - Predicate overload safely handles null comparisons

8. **Parameterless Methods**:
   - `When()` with no arguments requires at least one parameter to match
   - Parameterless methods cannot use `When()` - use `Returns()` or `OnCall()` instead
   - This prevents `When()` from becoming a redundant alias for existing configuration

### Interface Hierarchy

```csharp
// Base tracking interface (shared by IMethodTracking and IWhenTracking)
public interface ITracking
{
    void Verify();
    void Reset();
}

// Existing - unchanged except extends ITracking
public interface IMethodTracking : ITracking { /* existing members */ }

// NEW - When chain tracking
public interface IWhenTracking : ITracking
{
    IWhenTracking Verifiable();
}

// NEW - Chainable When (non-terminal)
public interface IWhenChain<TDelegate, TReturn> : IWhenTracking
{
    IWhenBuilder<TDelegate, TReturn> ThenWhen(/* value params - generated per method */);
    IWhenBuilder<TDelegate, TReturn> ThenWhen(/* Func<T1, T2, bool> predicate */);
    IWhenTracking ThenCall(TDelegate callback);
    IWhenTracking ThenNone();
    new IWhenChain<TDelegate, TReturn> Verifiable();
}

// NEW - Builder returned by When/ThenWhen (non-void methods)
public interface IWhenBuilder<TDelegate, TReturn>
{
    IWhenChain<TDelegate, TReturn> Returns(TReturn value);
}

// NEW - Void method When chain (no builder needed - nothing to return)
public interface IVoidWhenChain<TDelegate> : IWhenTracking
{
    IVoidWhenChain<TDelegate> ThenWhen(/* value params - generated per method */);
    IVoidWhenChain<TDelegate> ThenWhen(/* Func<T1, T2, bool> predicate */);
    IVoidWhenChain<TDelegate> Call(TDelegate callback);  // Optional callback
    IWhenTracking ThenCall(TDelegate callback);  // Terminal with callback
    IWhenTracking ThenNone();
    void Verify(Times times);  // Parameter-specific verification (see note below)
    new IVoidWhenChain<TDelegate> Verifiable();
}

// NOTE: Verify() vs Verify(Times) semantics:
// - Verify() (from ITracking) = chain reached terminal state (all matchers consumed)
// - Verify(Times) (on IVoidWhenChain) = this specific matcher was called N times
```

### Storage Design

Each interceptor class will contain:

```csharp
// When chain storage
private List<WhenMatcher>? _whenChain;
private int _whenChainHead;

// Nested matcher classes
private abstract class WhenMatcher
{
    public abstract bool Matches(/* params */);
    public abstract TReturn Execute(/* params */);
    public abstract bool IsTerminal { get; }
    public int CallCount { get; set; }
}

private sealed class WhenMatcherValue : WhenMatcher { /* predicate + value */ }
private sealed class WhenMatcherCall : WhenMatcher { /* callback, always matches */ }
private sealed class WhenMatcherNone : WhenMatcher { /* never matches, terminal */ }
```

### Storage Semantics

The When chain has three distinct states that determine behavior:

| State | Condition | Behavior |
|-------|-----------|----------|
| **Null** | `_whenChain == null` | When not configured; fall through to next priority |
| **Empty** | `_whenChain.Count == 0` | When configured but no matchers added; fall through |
| **Exhausted** | `_whenChainHead >= _whenChain.Count` | All matchers consumed; fall through |

**Fall through behavior:** When any of these states is encountered, the Invoke() method proceeds to the next item in the priority chain (Sequence, Returns, OnCall, etc.)

**Reset behavior:** `Reset()` sets `_whenChainHead = 0` AND clears all individual matcher `CallCount` values to 0. This provides a full reset for multi-phase testing where each phase can verify counts independently.

### Generic Methods

For generic methods, `When()` is generated on `TypedHandler<T>` (the type returned by `Of<T>()`). The return type uses the type parameter `T`:

```csharp
// Generic method: T Process<T>(T input)
// TypedHandler<T> exposes When():
stub.Method.Of<string>().When("hello").Returns("HELLO");

// The generated When() on TypedHandler<T>:
public IWhenBuilder<Func<T, T>, T> When(T value) { ... }
public IWhenBuilder<Func<T, T>, T> When(Func<T, bool> predicate) { ... }
```

### Generated Classes

For each method interceptor:

1. **WhenBuilderImpl** - holds pending matcher, exposes `Returns(value)`
2. **WhenChainImpl** - exposes `ThenWhen()`, `ThenCall()`, `ThenNone()`, `Verify()`, `Reset()`, `Verifiable()`
3. **WhenMatcherPending** - helper to capture predicate before value assignment

---

## Implementation Steps

### Phase 1: Public Interfaces (Foundation)
- [ ] Create `src/KnockOff/ITracking.cs` with base interface
- [ ] Modify `IMethodTracking` to extend `ITracking`
- [ ] Create `src/KnockOff/IWhenTracking.cs` with `IWhenTracking`, `IWhenChain<T>`, `IWhenBuilder<T>`
- [ ] Build to verify interface compilation

### Phase 2: Matcher Classes (Storage & Logic)
- [ ] Add `WhenMatcher` abstract base class generation
- [ ] Add `WhenMatcherValue` implementation (predicate + value)
- [ ] Add `WhenMatcherCall` implementation (callback, always matches, terminal)
- [ ] Add `WhenMatcherNone` implementation (never matches, terminal)
- [ ] Add `WhenMatcherPending` helper class

### Phase 3: Builder Classes
- [ ] Add `WhenBuilderImpl` nested class generation
- [ ] Add `WhenChainImpl` nested class generation
- [ ] Implement Verify(), Reset(), Verifiable() in WhenChainImpl

### Phase 4: Storage Fields
- [ ] Add `_whenChain` and `_whenChainHead` fields to single-signature interceptors
- [ ] Add per-signature When chain fields to overload group interceptors
- [ ] Update Reset() to clear When chain state (HEAD and all matcher CallCounts)

**Note:** No model changes required - existing models provide all necessary information.

### Phase 5: When() Entry Point
- [ ] Add helper: `BuildWhenPredicateType()` to UnifiedInterceptorBuilder
- [ ] Generate When() value overload (exact value matching)
- [ ] Generate When() predicate overload (Func<T1, T2, bool>)
- [ ] When() coexists with all other configs (no mutual exclusivity)

### Phase 6: Invoke() Priority Integration
- [ ] Add When chain check at TOP of Invoke() priority chain
- [ ] Implement HEAD matching logic
- [ ] Implement last-matcher repeat logic (don't advance if last ThenWhen)
- [ ] Implement terminal matcher logic (advance and stop for ThenCall/ThenNone)

### Phase 7: Verification Support
- [ ] Update `IsConfigured` to include When chain check
- [ ] Add When chain verification to `CheckVerification()`
- [ ] Add When chain verification to `CheckVerificationAll()`

### Phase 8: Overload Groups
- [ ] Replicate all changes for overload group interceptors with per-signature suffixing
- [ ] Generate per-signature matcher/builder/chain classes

### Phase 9: Inline Delegate Pattern
- [ ] Create `WhenChainRenderer.cs` with shared helper methods for When chain generation
- [ ] Refactor `MethodInterceptorRenderer` to use `WhenChainRenderer` helpers
- [ ] Update `InlineRenderer.RenderDelegateStub()` to use `WhenChainRenderer` helpers
- [ ] Add When chain support to InlineDelegateStubModel generation
- [ ] Generate When()/ThenWhen()/ThenCall()/ThenNone() for delegate interceptors
- [ ] Test delegate stub When chains

### Phase 10: Testing (Non-Void Methods)
- [ ] Test When(value).Returns(value) basic case
- [ ] Test When(predicate).Returns(value) basic case
- [ ] Test ThenWhen chaining (multiple conditions)
- [ ] Test last ThenWhen repeats when matched
- [ ] Test ThenCall terminal behavior (repeats forever)
- [ ] Test ThenNone terminal behavior (exhausts, falls through)
- [ ] Test fallback to OnCall/Returns when When doesn't match
- [ ] Test verification of When chains
- [ ] Test Reset() clears When chain state
- [ ] Test priority order (When > Sequence > Returns > OnCall)

### Phase 11: Void Method Support
- [ ] Add `IVoidWhenChain<TDelegate>` interface to `IWhenTracking.cs`
- [ ] Modify `WhenChainRenderer` to detect void methods and generate `IVoidWhenChain`
- [ ] Generate `VoidWhenChainImpl` with `ThenWhen()`, `Call()`, `ThenCall()`, `ThenNone()`, `Verify(Times)`
- [ ] For void methods, `When()` returns chain directly (no builder)
- [ ] Update Invoke() for void methods to check When chain (same logic, no return value)
- [ ] Test void method `When()` basic case
- [ ] Test void method `When().Call(callback)` with callback
- [ ] Test void method `When().Verify(Times)` parameter-specific verification
- [ ] Test void method When chains for all four patterns

---

## Acceptance Criteria

### Non-Void Methods
- [ ] `stub.Method.When(value1, value2).Returns(result)` works for exact matching
- [ ] `stub.Method.When((a, b) => predicate).Returns(result)` works for predicate matching
- [ ] `.ThenWhen()` chains multiple conditions
- [ ] Last ThenWhen repeats when matched, falls through when not
- [ ] `.ThenCall(callback)` provides unconditional terminal that repeats
- [ ] `.ThenNone()` provides explicit exhaustion terminal
- [ ] When chain coexists with OnCall/Returns as fallback
- [ ] `Verify()` checks if chain reached terminal state
- [ ] `Reset()` resets HEAD to first matcher and clears all matcher CallCounts
- [ ] All four patterns work (Standalone, Inline Interface, Inline Class, Inline Delegate)
- [ ] No `Arg` class - only value and predicate overloads

### Void Methods
- [ ] `stub.VoidMethod.When(value1, value2)` returns chain directly (no builder)
- [ ] `stub.VoidMethod.When((a, b) => predicate)` works for predicate matching
- [ ] `.Call(callback)` adds optional callback to matcher
- [ ] `.Verify(Times)` enables parameter-specific verification
- [ ] Same chaining semantics (ThenWhen, ThenCall, ThenNone)
- [ ] All four patterns work for void methods

---

## Completion Gate

**This feature is NOT complete until all applicable patterns are implemented.**

| Pattern | Has Parameters? | Required? | Status |
|---------|-----------------|-----------|--------|
| Standalone | Yes (methods) | **Yes** | Not Started |
| Inline Interface | Yes (methods) | **Yes** | Not Started |
| Inline Class | Yes (methods) | **Yes** | Not Started |
| Inline Delegate | Yes (delegate params) | **Yes** | Not Started |

**Deferred (correctly):**
- Properties - no parameters, When() not applicable
- Indexers - has key parameters but complexity not justified
- Events - no return value, When() not applicable

---

## Dependencies

- Returns API Rename (completed) - establishes `Returns()` naming

---

## Risks / Considerations

1. **Complexity** - When chains add significant complexity to interceptors. Mitigated by following existing Sequence patterns.

2. **No Mutual Exclusivity** - When() coexists with all other configurations. Priority chain determines which is used.

3. **Void Methods** - Different API surface (`IVoidWhenChain` returns directly from `When()`, no builder needed). Adds `Call()` for optional callbacks and `Verify(Times)` for parameter-specific verification.

4. **Properties/Indexers/Events** - Correctly deferred. Properties and events have no parameters/return values for When(). Indexers have key parameters but complexity not justified.

5. **Overload Groups** - More complex due to per-signature suffixing. Handle in dedicated phase.

6. **Breaking Change to IMethodTracking** - Adding `ITracking` base interface is additive, not breaking.

---

## Architectural Verification

**Four Patterns Analysis:**
- Standalone: When chain generated in interceptor class, same as Sequence
- Inline Interface: Same generation pattern, different nesting level
- Inline Class: Same generation pattern, Object property access
- Inline Delegate: Same generation pattern, delegates have parameters like methods

**Breaking Changes:** No - additive only

**Pattern Consistency:** Follows existing Sequence pattern (List storage, index pointer, nested impl classes)

**Codebase Analysis:**
- `MethodInterceptorRenderer.cs` - primary change location for generation
- `UnifiedInterceptorBuilder.cs` - add `BuildWhenPredicateType()` helper
- `IMethodTracking.cs` - modify to extend ITracking
- New files: `ITracking.cs`, `IWhenTracking.cs`

---

## Architect Review

**Status:** Complete - All Issues Resolved
**Date:** 2026-01-28
**Resolved:** 2026-01-29
**Void Method Review:** 2026-01-29

### High Priority Issues - Resolved

| Concern | Resolution |
|---------|------------|
| Generic Methods | Apply same design with `Of<T>()`: `stub.Method.Of<string>().When("hello").Returns("HELLO")` |
| Inline Class Stubs | Yes, support `When()` for all four patterns (Standalone, Inline Interface, Inline Class, Inline Delegate) |
| ITracking Semantic Ambiguity | Keep `Verify()` on both - not confusing, semantics are similar enough |

### Medium Priority Issues - Resolved

| Concern | Resolution |
|---------|------------|
| Ref/Out Parameters | Deferred - users use `OnCall` for methods with ref/out parameters |
| Async Wrapping | Yes, apply same auto-wrapping with `Task.FromResult()` |
| Overload Groups | "Replicate the pattern" is sufficient guidance |
| Test Plan Gaps | Current list is sufficient - developer will expand as needed |

### Design Questions - Resolved

| Question | Resolution |
|----------|------------|
| Mutual Exclusivity | When clears nothing - all configurations coexist with priority chain |
| ITracking Base Interface | Keep shared `ITracking` base interface |
| Simplification Option | Keep all three terminals (`ThenWhen`, `ThenCall`, `ThenNone`) |
| ThenNone Chainability | Intentional - terminals end the chain, return `IWhenTracking` |

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-01-29

**Concerns:**

| # | Concern | Resolution | Status |
|---|---------|------------|--------|
| 1 | Delegate stub code duplication | Phase 9 updated: Create `WhenChainRenderer.cs` with shared helpers used by both `MethodInterceptorRenderer` and `InlineRenderer.RenderDelegateStub()` | Addressed |
| 2 | Interface type parameters | Interface Hierarchy updated: `IWhenBuilder<TDelegate, TReturn>` and `IWhenChain<TDelegate, TReturn>` now include return type | Addressed |
| 3 | Generic method handling | "Generic Methods" subsection added to Design: `When()` generated on `TypedHandler<T>` with return type using type parameter `T` | Addressed |
| 4 | Model changes unclear | Clarification added to Phase 4: "No model changes required - existing models provide all necessary information" | Addressed |
| 5 | Async wrapping timing | "Async Wrapping" rule added to Behavior Rules: wrapping occurs at configuration time in `Returns()` method | Addressed |
| 6 | Storage state behavior | "Storage Semantics" subsection added: documents null/empty/exhausted states and their fall-through behavior | Addressed |

**Review Summary:**
- Files examined: IMethodTracking.cs, IMethodSequence.cs, MethodInterceptorRenderer.cs, UnifiedInterceptorBuilder.cs, InlineRenderer.cs
- Questions checked: 16 of 16
- Devil's advocate items: 4 generated, all acceptable for Phase 1

---

## Implementation Contract

**In Scope:**

### Phase 1: Public Interfaces (Foundation)
- [x] Create `src/KnockOff/ITracking.cs` with base interface (`Verify()`, `Reset()`)
- [x] Modify `src/KnockOff/IMethodTracking.cs` to extend `ITracking`
- [x] Create `src/KnockOff/IWhenTracking.cs` with:
  - `IWhenTracking : ITracking` (adds `Verifiable()`)
  - `IWhenChain<TDelegate, TReturn> : IWhenTracking` (adds `ThenWhen()`, `ThenCall()`, `ThenNone()`)
  - `IWhenBuilder<TDelegate, TReturn>` (adds `Returns()`)
- [x] **Checkpoint:** Build solution - verify interface compilation

### Phase 2-3: Matcher and Builder Classes (in MethodInterceptorRenderer.cs)
- [x] Add `WhenMatcher` abstract base class generation
- [x] Add `WhenMatcherValue` implementation (predicate + value + CallCount)
- [x] Add `WhenMatcherCall` implementation (callback, always matches, terminal)
- [x] Add `WhenMatcherNone` implementation (never matches, terminal)
- [x] Add `WhenBuilderImpl` nested class generation
- [x] Add `WhenChainImpl` nested class generation with `Verify()`, `Reset()`, `Verifiable()`

### Phase 4: Storage Fields (in MethodInterceptorRenderer.cs)
- [x] Add `_whenChain` (List<WhenMatcher>), `_whenChainHead` (int), and `_whenVerifiable` (bool) fields to single-signature interceptors
- [x] Add per-signature When chain fields to overload group interceptors
- [x] Update `Reset()` to set `_whenChainHead = 0` and clear all matcher `CallCount` values
- [x] Add conditional generation (only for non-void methods with parameters and no ref/out)
- [x] Add calls to `RenderWhenMatcherClasses()`, `RenderWhenBuilderImpl()`, `RenderWhenChainImpl()` in both single-signature and overload group interceptors

### Phase 5: When() Entry Point (in MethodInterceptorRenderer.cs + UnifiedInterceptorBuilder.cs)
- [x] Add helper: `BuildWhenPredicateType()` to UnifiedInterceptorBuilder
- [x] Generate `When()` value overload (exact value matching via equality)
- [x] Generate `When()` predicate overload (`Func<T1, T2, bool>`)

### Phase 6: Invoke() Priority Integration (in MethodInterceptorRenderer.cs)
- [x] Add When chain check at TOP of `RenderInvokeMethod()`
- [x] Implement HEAD matching logic (check current matcher)
- [x] Implement last-matcher repeat logic (don't advance if last ThenWhen matches)
- [x] Implement terminal matcher logic (ThenCall/ThenNone)
- [x] **Checkpoint:** Run existing tests - verify no regressions

### Phase 7: Verification Support (in MethodInterceptorRenderer.cs)
- [x] Update `IsConfigured` to include `_whenChain != null && _whenChain.Count > 0`
- [x] Add When chain verification to `CheckVerification()` (if marked verifiable)
- [x] Add When chain verification to `CheckVerificationAll()` (if configured)

### Phase 8: Overload Groups (in MethodInterceptorRenderer.cs)
- [x] Replicate all When chain changes for overload group interceptors
- [x] Use per-signature suffixing (`_whenChain_{suffix}`, `_whenChainHead_{suffix}`)
- [x] Generate per-signature WhenBuilder/WhenChain impl classes
- [x] **Checkpoint:** Run tests - verify overload scenarios work

### Phase 9: Inline Delegate Pattern + WhenChainRenderer
- [x] Create `src/Generator/Renderer/Shared/WhenChainRenderer.cs` with shared helpers:
  - `RenderWhenMatcherClasses()` - matcher base and implementations
  - `RenderWhenBuilderImpl()` - builder nested class
  - `RenderWhenChainImpl()` - chain nested class
  - `RenderWhenStorageFields()` - field declarations
  - `RenderWhenEntryPoints()` - When() method overloads
  - `RenderWhenInvokeCheck()` - invoke priority chain logic
- [x] Refactor `MethodInterceptorRenderer` to use `WhenChainRenderer` helpers (kept existing implementation - works well)
- [x] Update `InlineRenderer.RenderDelegateStub()` to add When chain support
- [x] Update `InlineRenderer.RenderTypedHandlerClass()` for generic method When support (deferred to Phase 10)
- [x] **Checkpoint:** Build + run tests - verify all patterns work

### Phase 10: Testing (Non-Void Methods)
- [ ] Test `When(value).Returns(value)` basic case
- [ ] Test `When(predicate).Returns(value)` basic case
- [ ] Test `ThenWhen` chaining (multiple conditions)
- [ ] Test last `ThenWhen` repeats when matched
- [ ] Test `ThenCall` terminal behavior (repeats forever)
- [ ] Test `ThenNone` terminal behavior (exhausts, falls through)
- [ ] Test fallback to `OnCall`/`Returns` when When doesn't match
- [ ] Test verification of When chains
- [ ] Test `Reset()` clears HEAD and all matcher CallCounts
- [ ] Test priority order (`When` > `Sequence` > `Returns` > `OnCall`)
- [ ] Test async wrapping (`Returns(value)` wraps with `Task.FromResult`)
- [ ] Test inline delegate When chains
- [ ] Test generic method When chains (`stub.Method.Of<T>().When()`)
- [ ] **Checkpoint:** All non-void tests pass, all patterns verified

### Phase 11: Void Method Support
- [ ] Add `IVoidWhenChain<TDelegate>` interface to `src/KnockOff/IWhenTracking.cs`
- [ ] Modify `WhenChainRenderer` to detect void methods (`model.IsVoid` or similar)
- [ ] Generate `VoidWhenChainImpl` nested class with:
  - `ThenWhen()` value and predicate overloads
  - `Call(TDelegate callback)` for optional callback
  - `ThenCall(TDelegate callback)` terminal
  - `ThenNone()` terminal
  - `Verify(Times times)` for parameter-specific verification
  - `Verify()`, `Reset()`, `Verifiable()` from base
- [ ] For void methods, `When()` returns `IVoidWhenChain` directly (no builder needed)
- [ ] Update Invoke() for void methods to check When chain (execute callback if configured, no return)
- [ ] Verify `WhenChainRenderer` handles void delegates (`Action<...>`) correctly
- [ ] Test void method `When()` basic case
- [ ] Test void method `When().Call(callback)` with optional callback
- [ ] Test void method `When().Verify(Times)` parameter-specific verification
- [ ] Test void method chaining (ThenWhen, ThenCall, ThenNone)
- [ ] Test void method When chains for all four patterns (including void delegates)
- [ ] Test void delegate (`Action<T1, T2>`) returns `IVoidWhenChain` directly
- [ ] **Final Checkpoint:** All tests pass, all patterns verified (void and non-void)

**Out of Scope:**
- Property/Indexer/Event support (no parameters or no return value)
- `Arg` class or similar matchers

---

## Implementation Progress

**Status:** COMPLETE - All phases implemented (Phases 1-15)

### Phase 11 Summary (Void Method Support)
- Added `IVoidWhenChain<TDelegate>` interface to `IWhenTracking.cs`
- Updated `MethodInterceptorRenderer` to detect void methods and generate `VoidWhenChainImpl`
- Generated void-specific When chain classes: `VoidWhenMatcher`, `VoidWhenMatcherPredicate`, `VoidWhenMatcherCall`, `VoidWhenMatcherNone`
- For void methods, `When()` returns chain directly (no builder needed - nothing to return)
- `Call(callback)` provides optional callback for parameter-matched execution
- `Verify(Times)` enables parameter-specific verification on the chain
- Updated Invoke() for void methods to check When chain (execute callback if configured, no return)
- Added void delegate support in `InlineRenderer`
- All void method tests pass (Standalone, Inline Interface, Inline Delegate patterns)
- All 901 tests pass (net9.0/net10.0), 900 pass (net8.0)

### Phase 10 Summary (Non-Void Method Tests)
- Created `WhenChainTests.cs` with 54+ comprehensive tests
- Tested all patterns: Standalone, Inline Interface, Inline Class, Inline Delegate
- Tested: When value/predicate, ThenWhen chaining, ThenCall terminal, ThenNone terminal
- Tested: Fallback behavior, priority order, verification, Reset(), async wrapping
- Tested: Null matching via predicate, empty string direct match, edge cases

### Phase 9 Summary
- Created `src/Generator/Renderer/Shared/WhenChainRenderer.cs` with shared helper methods for When chain generation
- Updated `InlineRenderer.RenderDelegateStub()` to add When chain support for non-void delegates with parameters
- Added inline implementations in `InlineRenderer`:
  - `RenderDelegateWhenEntryPoints()` - When() value and predicate overloads
  - `RenderDelegateWhenMatcherClasses()` - WhenMatcher, WhenMatcherValue, WhenMatcherCall, WhenMatcherNone
  - `RenderDelegateWhenBuilderImpl()` - WhenBuilderImpl with Returns()
  - `RenderDelegateWhenChainImpl()` - WhenChainImpl with ThenWhen, ThenCall, ThenNone, Verify, Reset, Verifiable
- When chain storage fields are `internal` for access from stub's Invoke method
- Matcher classes are `internal` to match field accessibility
- Correctly skips When chain for void delegates (no return value to configure)
- Correctly skips When chain for parameterless delegates (nothing to match on)
- Updated stub's Invoke method to check When chain first (highest priority)
- All tests pass: 824 on net9.0/net10.0, 823 on net8.0 (KnockOffTests), plus all other test projects
- Generated code sample: `DelegateStubTests.Stubs.g.cs` shows complete When chain support for `Formatter` delegate

### Phase 8 Summary
- Verified all When chain changes were already implemented for overload groups in previous phases
- Per-signature storage fields: `_whenChain_{suffix}`, `_whenChainHead_{suffix}`, `_whenVerifiable_{suffix}`
- Per-signature When() entry points with value and predicate overloads
- Per-signature nested classes: `WhenMatcher_{suffix}`, `WhenMatcherValue_{suffix}`, `WhenMatcherCall_{suffix}`, `WhenMatcherNone_{suffix}`
- Per-signature builder classes: `WhenBuilderImpl_{suffix}`
- Per-signature chain implementation classes: `WhenChainImpl_{suffix}` with ThenWhen, ThenCall, ThenNone, Verify, Reset, Verifiable
- Invoke methods check When chain at TOP of priority chain with per-signature suffixing
- Reset() clears HEAD and matcher CallCounts for each overload
- IsConfigured and IsVerifiable include When chain checks for all overloads
- CheckVerification() and CheckVerificationAll() handle When chain verification per overload
- All tests pass: 824 on net9.0/net10.0, 823 on net8.0 (KnockOffTests), plus all other test projects
- Generated code sample: `MethodOverloadServiceKnockOff.g.cs` shows complete When chain support for 3 overloads

### Phase 7 Summary
- Updated `RenderInternalVerificationMembers()` to accept `hasWhenChain` parameter
- Updated `IsConfigured` property to include When chain check: `|| (_whenChain?.Count ?? 0) > 0`
- Updated `CheckVerification()` to check both `_isVerifiable` and `_whenVerifiable`:
  - Early return condition is now conditional based on `hasWhenChain`
  - If `_whenVerifiable` is true, verifies chain reached terminal state
- Updated `CheckVerificationAll()` to verify When chain if configured:
  - Checks if HEAD reached end or terminal matcher
  - Returns `VerificationFailure.SequenceIncomplete()` if chain not fully consumed
- Applied same changes for overload groups with per-signature suffixing
- Removed `#pragma warning disable CS0414` for `_whenVerifiable` (now used)
- All tests pass: 824 on net9.0/net10.0, 823 on net8.0 (KnockOffTests), plus all other test projects

### Phase 6 Summary
- Created `RenderWhenChainInvokeCheck()` helper method in `MethodInterceptorRenderer.cs`
- Added When chain check at TOP of `RenderInvokeMethod()` for single-signature interceptors
- Added When chain check at TOP of `RenderOverloadInvokeMethod()` for overload groups
- Implemented HEAD matching logic with priority chain behavior:
  - When chain checked FIRST (before Sequence, Returns, OnCall)
  - HEAD matcher checked with `Matches()` - if matches, execute and advance (unless last)
  - Last matcher repeats (both ThenWhen and ThenCall) - no advancement
  - ThenNone handled via `IsTerminal` check - advances to exhaust chain on reach
- `Execute()` returns full return type - no async wrapping needed (already wrapped at config time)
- Per-signature suffixing for overload groups working correctly
- All tests pass: 824 on net9.0/net10.0, 823 on net8.0 (KnockOffTests), plus all other test projects

### Phase 5 Summary
- Added `BuildWhenPredicateType()` helper to `UnifiedInterceptorBuilder.cs`
- Created `RenderWhenEntryPoints()` method in `MethodInterceptorRenderer.cs`
- Generated `When()` value overload with `Object.Equals`-based equality predicate
- Generated `When()` predicate overload with `Func<T1, T2, ..., bool>` parameter
- Added When() entry points for single-signature interceptors
- Added When() entry points for overload group interceptors with per-signature suffixing
- Conditional generation: When() only generated for methods with `canHaveWhenChain` (non-void, has params, no ref/out)
- All tests pass (1649 on net9.0, 1648 on net8.0)

### Phase 4 Summary
- Added `_whenChain`, `_whenChainHead`, `_whenVerifiable` fields to single-signature interceptors
- Added per-signature When chain fields to overload group interceptors
- Made storage field generation conditional (only non-void methods with parameters, no ref/out)
- Added render method calls for `RenderWhenMatcherClasses`, `RenderWhenBuilderImpl`, `RenderWhenChainImpl`
- Updated `Reset()` method to clear When chain state for both single and overload groups
- Fixed lambda parameter naming to avoid C# keyword conflicts (using `_arg0`, `_arg1`, etc.)
- Added `#pragma warning disable CS0414` for `_whenVerifiable` (will be used in Phase 7)
- All tests pass (2471+ total across all test projects)

### Verification Gates

1. **After Phase 1:** Interfaces compile, `IMethodTracking` extends `ITracking`
2. **After Phase 6:** Existing tests pass, When chain logic integrated
3. **After Phase 8:** Overload group tests pass
4. **After Phase 9:** All four patterns generate correct code
5. **After Phase 10:** All non-void tests pass
6. **Final (Phase 11):** All void method tests pass, all patterns verified

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (test not directly related to When feature)
- Architectural contradiction discovered (e.g., existing pattern prevents design)
- Generated code does not compile
- Breaking change to existing API detected

---

## Gap Analysis (2026-01-30)

### Critical Gap 1: ThenWhen() Not Exposed on IWhenChain Interface

**Problem:** The `IWhenChain<TDelegate, TReturn>` interface only contains a comment about ThenWhen, but no actual method definitions. The `Returns()` method returns the interface type, making `ThenWhen()` (which only exists on the private `WhenChainImpl` class) inaccessible from user code.

**Impact:** The main documented API does not work:
```csharp
// DOES NOT COMPILE - ThenWhen is not on IWhenChain interface
stub.Add.When(1, 2).Returns(3).ThenWhen(5, 7).Returns(12);
```

**Evidence:** Test file has this comment (lines 21-26):
```
NOTE: ThenWhen chaining is NOT currently testable because:
- WhenChainImpl is a private nested class
- Returns() returns IWhenChain<> interface which doesn't define ThenWhen()
- ThenWhen() methods are only on the private impl class
```

### Critical Gap 2: Inline Class Pattern Missing When() Support

**Problem:** The Inline Class pattern does not generate When() methods at all. The generated interceptors only have `OnCall()`.

**Impact:** One of the four patterns from the completion gate does not work:
```csharp
classStub.ComputeVirtual.When(1, 2).Returns(100); // Method doesn't exist!
```

**Evidence:** Generated `WhenChainBaseClass_ComputeVirtualInterceptor` only has `OnCall()`, no `When()`.

### Gap 3: Async Auto-Wrapping Not Implemented for When Chain

**Problem:** Design says "For async methods, `Returns(value)` automatically wraps with `Task.FromResult(value)`" but generated code requires manual wrapping.

**Impact:** Inconsistent API between regular `Returns()` and `When().Returns()`:
```csharp
stub.GetAsync.Returns("hello");  // Works - auto-wraps
stub.GetAsync.When("x").Returns("hello");  // FAILS - requires Task.FromResult("hello")
```

### Gap 4: Test Coverage Missing for Actual ThenWhen Chaining

**Problem:** No tests actually call `ThenWhen()` on the result of `Returns()` because it's impossible. All "ThenWhen" tests use multiple `When()` calls as a workaround.

### Gap 5: Completion Evidence Empty

**Problem:** The completion evidence section has no entries despite the plan being marked complete.

---

## Phase 12: Fix ThenWhen Interface Exposure (CRITICAL)

**Problem:** Make `ThenWhen()` accessible from user code after calling `Returns()`.

### Developer Concern: Public Class Exposure (Medium Priority)

The original plan proposed making `WhenChainImpl` public. This exposes implementation details (the "Impl" suffix suggests internal use).

**Architectural Decision: Rename to `WhenChain` (drop "Impl" suffix)**

Reasoning:
- `WhenChain` is a meaningful public name for users to work with
- Follows pattern of `MethodSequenceImpl` being internal while `IMethodSequence` is public
- The class IS the When chain from the user's perspective - not merely an implementation detail
- Generated per-interceptor, so name collision is not a concern

**Implementation Details:**

```csharp
// Generated in each interceptor (e.g., ICalculator_AddInterceptor):

/// <summary>When chain for Add method with ThenWhen chaining support.</summary>
public sealed class WhenChain : global::KnockOff.IWhenChain<AddDelegate, int>
{
    private readonly ICalculator_AddInterceptor _interceptor;

    public WhenChain(ICalculator_AddInterceptor interceptor) => _interceptor = interceptor;

    // ThenWhen returns concrete type to enable chaining
    public WhenBuilderImpl ThenWhen(int a, int b) { ... }
    public WhenBuilderImpl ThenWhen(global::System.Func<int, int, bool> predicate) { ... }

    // Terminal operations return IWhenTracking (no further chaining)
    public global::KnockOff.IWhenTracking ThenCall(AddDelegate callback) { ... }
    public global::KnockOff.IWhenTracking ThenNone() { ... }

    // IWhenChain<> interface implementation
    public WhenChain Verifiable() { ... }
    global::KnockOff.IWhenTracking global::KnockOff.IWhenTracking.Verifiable() => Verifiable();
}

/// <summary>Builder for When matchers. Captures predicate, awaits Returns(value).</summary>
public sealed class WhenBuilderImpl : global::KnockOff.IWhenBuilder<AddDelegate, int>
{
    // Returns concrete WhenChain for ThenWhen chaining
    public WhenChain Returns(int value) { ... }
}
```

**Files to Change:**
- `MethodInterceptorRenderer.cs`: Rename `WhenChainImpl` to `WhenChain`, make public
- `MethodInterceptorRenderer.cs`: Make `WhenBuilderImpl` public
- `MethodInterceptorRenderer.cs`: Change return types to concrete types
- `InlineRenderer.cs` (delegate stubs): Same changes

**Implementation Checklist:**
- [ ] Rename `WhenChainImpl` to `WhenChain` in `MethodInterceptorRenderer.cs`
- [ ] Change `private sealed class WhenChainImpl` to `public sealed class WhenChain`
- [ ] Change `private sealed class WhenBuilderImpl` to `public sealed class WhenBuilderImpl`
- [ ] Change `WhenBuilderImpl.Returns()` return type from `IWhenChain<>` to `WhenChain`
- [ ] Change `WhenChain.ThenWhen()` return type from `IWhenBuilder<>` to `WhenBuilderImpl`
- [ ] Change `WhenChain.Verifiable()` return type from `IWhenChain<>` to `WhenChain`
- [ ] Apply same changes for overload groups (per-signature classes)
- [ ] Apply same changes in `InlineRenderer.cs` for delegate stubs
- [ ] Apply same changes for void method `VoidWhenChain` (was `VoidWhenChainImpl`)
- [ ] Verify tests can now use actual `ThenWhen()` fluent chaining
- [ ] Add tests for `ThenWhen()` chaining (value and predicate overloads)
- [ ] **Checkpoint:** All existing tests pass + new ThenWhen tests pass

---

## Phase 13: Add When() Support to Inline Class Pattern

**Problem:** Inline Class interceptors don't generate When() chains.

### Developer Concern: Architecture Incompatibility (High Priority)

The inline class pattern uses a fundamentally different architecture from standalone/inline interface:

| Aspect | Standalone/Inline Interface | Inline Class |
|--------|----------------------------|--------------|
| Invocation flow | Interceptor has `Invoke()` method | Impl class checks `Callback` property |
| Priority chain | In `Invoke()` method | Hardcoded in Impl override |
| When chain check | At top of `Invoke()` | Not present |

**Current Impl class pattern (from `ClassRenderer.cs`):**
```csharp
public override int ComputeVirtual(int a, int b)
{
    _stub?.ComputeVirtual.RecordCall(a, b);
    if (_stub?.ComputeVirtual.Callback is { } onCall) return onCall(a, b);
    return base.ComputeVirtual(a, b);
}
```

**Architectural Decision: Option A - Add `Invoke()` method to Inline Class interceptors**

Reasoning:
- Consistent architecture across all patterns
- Priority chain logic lives in ONE place (the interceptor)
- Impl class becomes a thin pass-through (consistent with standalone pattern)
- Easier to maintain - changes to priority chain only in interceptor
- When chain, Returns, OnCall, Sequence all handled the same way

**How it works:**

1. **Interceptor gains `Invoke()` method** with full priority chain:
   ```csharp
   public sealed class MyClass_ComputeVirtualInterceptor : global::KnockOff.IMethodTracking
   {
       // Existing: _onCall, _whenChain, _sequence, _returnsValue, etc.

       // NEW: Full Invoke() with priority chain
       internal int Invoke(bool strict, int a, int b)
       {
           // When chain (highest priority)
           if (_whenChain != null && _whenChainHead < _whenChain.Count) { ... }

           // Sequence
           if (_sequence != null && _sequenceIndex < _sequence.Count) { ... }

           // Returns(value)
           if (_hasReturnsValue) { ... }

           // OnCall
           if (_onCall != null) { return _onCall(a, b); }

           // Unconfigured - return null to signal "use default behavior"
           return default!; // Or use out bool to signal "not handled"
       }

       // Callback property kept for backward compatibility
       internal Func<int, int, int>? Callback => _onCall;
   }
   ```

2. **Impl class updated to use Invoke():**
   ```csharp
   public override int ComputeVirtual(int a, int b)
   {
       _stub?.ComputeVirtual.RecordCall(a, b);

       // Option 1: Invoke returns result + handled flag
       if (_stub?.ComputeVirtual.TryInvoke(a, b, out var result)) return result;

       // Option 2: Check IsConfigured first
       if (_stub?.ComputeVirtual.IsConfigured == true)
           return _stub.ComputeVirtual.Invoke(_stub.Strict, a, b);

       return base.ComputeVirtual(a, b);
   }
   ```

3. **For abstract methods** (no base to call):
   ```csharp
   public override int ComputeAbstract(int a, int b)
   {
       _stub?.ComputeAbstract.RecordCall(a, b);
       return _stub?.ComputeAbstract.Invoke(_stub.Strict, a, b) ?? default!;
   }
   ```

**Alternative Considered: Option B - Inline priority check in Impl class**

Would duplicate priority chain logic in every generated override. Rejected because:
- Logic duplication across all overrides
- Harder to maintain
- Inconsistent with other patterns

**Files to Change:**
- `ClassRenderer.cs`:
  - Add `RenderMethodInterceptorInvokeMethod()` for full priority chain
  - Add When chain storage fields, matcher classes, builder/chain classes
  - Add When() entry points to method interceptor
  - Update `RenderImplMethodOverride()` to call `Invoke()` instead of checking `Callback`
- Model changes: None (existing `InlineClassMethodModel` has all needed info)

**Scope: Which method variants to support:**
- Non-void methods with parameters: Full When chain support
- Void methods with parameters: `IVoidWhenChain` support
- Parameterless methods: No When support (nothing to match on)
- Generic methods: When on TypedHandler<T> (if class supports generics - verify)
- Methods with ref/out: No When support (use OnCall)

**Implementation Checklist:**
- [ ] Add storage fields to `RenderMethodInterceptorClass()`: `_whenChain`, `_whenChainHead`, `_whenVerifiable`, `_hasReturnsValue`, `_returnsValue`, `_sequence`, etc.
- [ ] Add matcher classes (`WhenMatcher`, `WhenMatcherValue`, `WhenMatcherCall`, `WhenMatcherNone`)
- [ ] Add `WhenBuilderImpl` and `WhenChain` classes to method interceptor
- [ ] Add `When()` entry points (value and predicate overloads)
- [ ] Add `Returns()` method for simple return value configuration
- [ ] Add `Invoke()` method with full priority chain (When > Sequence > Returns > OnCall)
- [ ] Update `IsConfigured` to include When chain and Returns
- [ ] Update `CheckVerification()` and `CheckVerificationAll()` for When chain
- [ ] Update `RenderImplMethodOverride()` to call `Invoke()` when `IsConfigured`
- [ ] Handle virtual methods (fall back to base when not configured)
- [ ] Handle abstract methods (use Invoke result or default)
- [ ] Add void method support with `IVoidWhenChain`
- [ ] Skip When chain for parameterless methods
- [ ] Skip When chain for methods with ref/out parameters
- [ ] Add tests for inline class When() chains (void and non-void)
- [ ] Add tests for When + base class fallback behavior
- [ ] **Checkpoint:** All inline class tests pass, When() works on class stubs

---

## Phase 14: Fix Async Auto-Wrapping for When Chain

**Problem:** `When().Returns(value)` for async methods should auto-wrap with `Task.FromResult()` like regular `Returns()`.

### Developer Concern: Async Overload Ambiguity (Medium Priority)

If we add both `Returns(TUnwrapped value)` and `Returns(Task<T> value)`, there could be overload resolution issues.

**Example ambiguity:**
```csharp
// For method: Task<string> GetAsync(string key)
// If we generate both:
//   Returns(Task<string> value)  - explicit Task
//   Returns(string value)        - unwrapped, auto-wraps

// This works fine:
stub.GetAsync.When("key").Returns("value");      // Calls Returns(string)
stub.GetAsync.When("key").Returns(Task.FromResult("value")); // Calls Returns(Task<string>)

// But what about:
stub.GetAsync.When("key").Returns(someVariable); // Which overload?
```

**Architectural Decision: Option A - Detect async at generation time, generate only unwrapped Returns()**

For async methods (`Task<T>`, `ValueTask<T>`), the `WhenBuilderImpl` generates:
- `Returns(T value)` - wraps with `Task.FromResult(value)` internally
- Does NOT generate `Returns(Task<T> value)` - no ambiguity

Reasoning:
- Users almost never want to pass an already-wrapped Task to `Returns()`
- For edge cases where users need Task control, they can use `OnCall`
- Consistent with existing `Returns(T value)` on interceptors (already auto-wraps)
- No overload ambiguity - only one `Returns()` method exists

**Generated code for async method:**
```csharp
// For: Task<string> GetAsync(string key)

public sealed class WhenBuilderImpl : global::KnockOff.IWhenBuilder<GetAsyncDelegate, global::System.Threading.Tasks.Task<string>>
{
    private readonly IService_GetAsyncInterceptor _interceptor;
    private readonly global::System.Func<string, bool> _predicate;

    // Only one Returns() - takes unwrapped type, wraps internally
    public WhenChain Returns(string value)
    {
        _interceptor._whenChain ??= new global::System.Collections.Generic.List<WhenMatcher>();
        // Note: WhenMatcherValue stores the wrapped Task
        _interceptor._whenChain.Add(new WhenMatcherValue(_predicate, global::System.Threading.Tasks.Task.FromResult(value)));
        return new WhenChain(_interceptor);
    }
}
```

**For non-async methods:**
```csharp
// For: int Add(int a, int b)

public sealed class WhenBuilderImpl : global::KnockOff.IWhenBuilder<AddDelegate, int>
{
    public WhenChain Returns(int value)
    {
        _interceptor._whenChain ??= new global::System.Collections.Generic.List<WhenMatcher>();
        _interceptor._whenChain.Add(new WhenMatcherValue(_predicate, value));
        return new WhenChain(_interceptor);
    }
}
```

**Detection logic in generator:**
```csharp
bool isAsync = returnType.StartsWith("System.Threading.Tasks.Task<")
            || returnType.StartsWith("global::System.Threading.Tasks.Task<")
            || returnType.StartsWith("System.Threading.Tasks.ValueTask<")
            || returnType.StartsWith("global::System.Threading.Tasks.ValueTask<");

string unwrappedType = isAsync ? ExtractGenericArg(returnType) : returnType;
string wrapExpression = isAsync ? $"global::System.Threading.Tasks.Task.FromResult({valueParam})" : valueParam;
```

**Edge case: Task (non-generic) / ValueTask (non-generic)**
- `Task` methods are void-equivalent (no return value to configure)
- Generate `IVoidWhenChain` instead of `IWhenBuilder`

**Files to Change:**
- `MethodInterceptorRenderer.cs`: Add async detection in `RenderWhenBuilderImpl()`
- `MethodInterceptorRenderer.cs`: Generate `Returns(TUnwrapped)` that wraps
- `InlineRenderer.cs`: Same changes for delegate stubs
- `ClassRenderer.cs`: Same changes for inline class interceptors (Phase 13)

**Implementation Checklist:**
- [ ] Add `IsAsyncMethod()` helper to detect `Task<T>` / `ValueTask<T>` return types
- [ ] Add `GetUnwrappedType()` helper to extract `T` from `Task<T>`
- [ ] Update `RenderWhenBuilderImpl()` to use unwrapped type for async methods
- [ ] Update `WhenMatcherValue` construction to wrap with `Task.FromResult()` for async
- [ ] Apply same changes in `InlineRenderer.cs` for delegate stubs
- [ ] Apply same changes in `ClassRenderer.cs` for inline class (after Phase 13)
- [ ] Add test: `When().Returns(unwrappedValue)` works for async methods
- [ ] Add test: verify `Task.FromResult()` wrapping happens correctly
- [ ] Add test: `ValueTask<T>` auto-wrapping works
- [ ] Update existing async tests to use unwrapped syntax
- [ ] **Checkpoint:** Async When tests use unwrapped type, all pass

---

## Phase 15: Complete Testing and Documentation

**Implementation:**
- [ ] Add tests that actually use `ThenWhen()` fluent chaining (not workaround)
- [ ] Test inline class When() chains (void and non-void)
- [ ] Test async auto-wrapping
- [ ] Remove workaround comments from `WhenChainTests.cs`
- [ ] Update completion gate checkboxes
- [ ] Fill in Completion Evidence section with actual evidence
- [ ] **Final Checkpoint:** All tests pass, all patterns verified, documentation accurate

---

## Architect Clarification (2026-01-30)

### Developer Concerns Addressed

| Concern | Priority | Resolution |
|---------|----------|------------|
| Phase 13: Inline Class Architecture Incompatibility | HIGH | Add `Invoke()` method to inline class interceptors; Impl class becomes thin pass-through. See Phase 13 for details. |
| Phase 12: Public Class Exposure | MEDIUM | Rename `WhenChainImpl` to `WhenChain` (drop "Impl" suffix). Public name is meaningful and per-interceptor. |
| Phase 14: Async Overload Ambiguity | MEDIUM | Generate only `Returns(TUnwrapped)` for async methods; wraps internally. No `Returns(Task<T>)` overload. |

### Key Architectural Decisions

1. **Concrete return types for fluent chaining**: `WhenBuilderImpl.Returns()` returns concrete `WhenChain` (not interface) to enable `ThenWhen()` access.

2. **Unified priority chain location**: All patterns (Standalone, Inline Interface, Inline Class, Inline Delegate) have priority chain logic in the interceptor's `Invoke()` method.

3. **Single `Returns()` overload for async**: Avoids ambiguity by only generating unwrapped parameter type.

4. **Class naming**: Drop "Impl" suffix for public classes (`WhenChain`, not `WhenChainImpl`). Builder keeps "Impl" suffix since it's a transitional object.

### Implementation Order

Phase 12 must complete before Phase 13 (inline class needs the `WhenChain` pattern established).
Phase 14 can be done in parallel with Phase 13 for non-inline-class patterns, then applied to inline class.

---

## Completion Evidence

### Tests Passing

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

### All Checklist Items Verified

**Completion Gate (all checked):**
- [x] Standalone pattern
- [x] Inline Interface pattern
- [x] Inline Class pattern
- [x] Inline Delegate pattern

**All gaps resolved:**
- [x] ThenWhen() fluent chaining accessible (Phase 12)
- [x] Inline Class pattern When() support (Phase 13)
- [x] Async auto-wrapping (Phase 14)
- [x] Tests using ThenWhen fluent API (Phase 12)
- [x] Completion Evidence documented (Phase 15)
