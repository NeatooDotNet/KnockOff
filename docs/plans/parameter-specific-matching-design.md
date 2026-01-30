# Parameter-Specific Matching Design (When API)

**Date:** 2026-01-28
**Related Todo:** [Parameter-Specific Matching](../todos/parameter-specific-matching.md)
**Status:** Draft (Architect)
**Last Updated:** 2026-01-28

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
public interface IWhenChain<TDelegate> : IWhenTracking
{
    IWhenBuilder<TDelegate> ThenWhen(/* value params - generated per method */);
    IWhenBuilder<TDelegate> ThenWhen(/* Func<T1, T2, bool> predicate */);
    IWhenTracking ThenCall(TDelegate callback);
    IWhenTracking ThenNone();
    new IWhenChain<TDelegate> Verifiable();
}

// NEW - Builder returned by When/ThenWhen
public interface IWhenBuilder<TDelegate>
{
    IWhenChain<TDelegate> Returns<TReturn>(TReturn value);
}
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
- [ ] Update Reset() to clear When chain state

### Phase 5: When() Entry Point
- [ ] Add helper: `BuildWhenPredicateType()` to UnifiedInterceptorBuilder
- [ ] Generate When() value overload (exact value matching)
- [ ] Generate When() predicate overload (Func<T1, T2, bool>)
- [ ] Ensure When() clears competing configs (mutual exclusivity with Sequence)

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

### Phase 9: Integration Testing
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

---

## Acceptance Criteria

- [ ] `stub.Method.When(value1, value2).Returns(result)` works for exact matching
- [ ] `stub.Method.When((a, b) => predicate).Returns(result)` works for predicate matching
- [ ] `.ThenWhen()` chains multiple conditions
- [ ] Last ThenWhen repeats when matched, falls through when not
- [ ] `.ThenCall(callback)` provides unconditional terminal that repeats
- [ ] `.ThenNone()` provides explicit exhaustion terminal
- [ ] When chain coexists with OnCall/Returns as fallback
- [ ] `Verify()` checks if chain reached terminal state
- [ ] `Reset()` resets HEAD to first matcher
- [ ] All three patterns work (Standalone, Inline Interface, Inline Class)
- [ ] No `Arg` class - only value and predicate overloads

---

## Dependencies

- Returns API Rename (completed) - establishes `Returns()` naming

---

## Risks / Considerations

1. **Complexity** - When chains add significant complexity to interceptors. Mitigated by following existing Sequence patterns.

2. **Mutual Exclusivity** - When() clears Sequence (both are "sequence-like"). OnCall/Returns remain as fallback.

3. **Void Methods** - Skipped initially. When is primarily for return values. Can add later with ThenCall(Action).

4. **Properties/Indexers** - Skipped initially. Add after method implementation is stable.

5. **Overload Groups** - More complex due to per-signature suffixing. Handle in dedicated phase.

6. **Breaking Change to IMethodTracking** - Adding `ITracking` base interface is additive, not breaking.

---

## Architectural Verification

**Three Patterns Analysis:**
- Standalone: When chain generated in interceptor class, same as Sequence
- Inline Interface: Same generation pattern, different nesting level
- Inline Class: Same generation pattern, Object property access

**Breaking Changes:** No - additive only

**Pattern Consistency:** Follows existing Sequence pattern (List storage, index pointer, nested impl classes)

**Codebase Analysis:**
- `MethodInterceptorRenderer.cs` - primary change location for generation
- `UnifiedInterceptorBuilder.cs` - add `BuildWhenPredicateType()` helper
- `IMethodTracking.cs` - modify to extend ITracking
- New files: `ITracking.cs`, `IWhenTracking.cs`

---

## Architect Review

**Status:** Complete
**Date:** 2026-01-28

### High Priority Issues

| Concern | Description |
|---------|-------------|
| Generic Methods | Plan doesn't specify how When() works with Of<T>() pattern |
| Inline Class Stubs | Unclear if When() is supported (inline class has "simpler interceptor") |
| ITracking Semantic Ambiguity | Verify() means different things: "was called" vs "reached terminal" |

### Medium Priority Issues

| Concern | Description |
|---------|-------------|
| Ref/Out Parameters | Not addressed - Func<ref int, bool> isn't valid C# |
| Async Wrapping | No mention of automatic Task.FromResult() wrapping |
| Overload Groups | Phase 8 lacks implementation details |
| Test Plan Gaps | Missing: all 3 patterns, overloads, async, generic methods, edge cases |

### Design Questions Raised

1. **Mutual Exclusivity:** Why does When clear Sequence but coexist with OnCall/Returns? Seems arbitrary.
2. **ITracking Base Interface:** Is it worth the semantic confusion? Could keep IWhenTracking separate.
3. **Simplification Option:** Could remove ThenCall/ThenNone entirely - last matcher just repeats. Less powerful but simpler.
4. **ThenNone Chainability:** ThenNone() returns IWhenTracking which doesn't have ThenWhen(), but IWhenChain does - is this correct?

---

## Developer Review

**Status:** Not Started

**Concerns:**

---

## Implementation Contract

**In Scope:**
*(To be filled by developer after review)*

**Out of Scope:**
- Void method support (use OnCall with conditional logic)
- Property/Indexer support
- `Arg` class or similar matchers
- `Verify(Times)` on When chains

---

## Implementation Progress

*(To be filled during implementation)*

---

## Completion Evidence

*(Required before marking complete)*

- **Tests Passing:**
- **Generated Code Sample:**
- **All Checklist Items:**
