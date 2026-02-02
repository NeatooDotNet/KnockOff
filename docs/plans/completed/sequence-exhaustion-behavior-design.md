# Sequence Exhaustion Behavior Design

**Date:** 2026-02-01
**Related Todo:** [Sequence Exhaustion Behavior](../todos/sequence-exhaustion-behavior.md)
**Status:** Complete
**Last Updated:** 2026-02-01

---

## Developer Concerns Clarification

### Concern 1: Interface Updates

**Question:** Should `ThenDefault()` be added to `IMethodSequence<T>`, `IPropertyGetSequence<T>`, `IIndexerGetSequence<T>` interfaces, or only to generated concrete classes?

**Answer:** `ThenDefault()` should be added to **both** the KnockOff library interfaces AND the generated concrete classes.

**Rationale:**
- The library interfaces (`IMethodSequence<T>`, `IPropertyGetSequence<T>`, `IIndexerGetSequence<T>`) define the API contract
- Users may hold references typed as the interface (e.g., when storing sequences in variables)
- Adding to interfaces ensures discoverability via IntelliSense regardless of declared type

**Implementation:**

1. In `src/KnockOff/IMethodSequence.cs`, add to `IMethodSequence`:
   ```csharp
   /// <summary>
   /// Terminates sequence with default(T) after exhaustion instead of repeating last value.
   /// </summary>
   void ThenDefault();
   ```

2. In `src/KnockOff/IPropertySequence.cs`, add to `IPropertyGetSequence<TValue>`:
   ```csharp
   /// <summary>
   /// Terminates sequence with default(TValue) after exhaustion instead of repeating last value.
   /// </summary>
   void ThenDefault();
   ```

3. In `src/KnockOff/IIndexerSequence.cs`, add to `IIndexerGetSequence<TKey, TValue>`:
   ```csharp
   /// <summary>
   /// Terminates sequence with default(TValue) after exhaustion instead of repeating last value.
   /// </summary>
   void ThenDefault();
   ```

4. Generated concrete classes implement these interfaces and provide the actual behavior.

**Note:** Setter sequence interfaces (`IPropertySetSequence<T>`, `IIndexerSetSequence<TKey, TValue>`) also get `ThenDefault()` for API consistency - see Concern 3 below.

---

### Concern 2: MethodCallBuilderImpl and Single-Callback Case

**Question:** Plan mentions adding `ThenDefault()` to `MethodCallBuilderImpl` but single callbacks already repeat forever. Is this needed?

**Answer:** **Remove this from the plan.** `ThenDefault()` should NOT be added to `MethodCallBuilderImpl`.

**Rationale:**
- `OnCall(callback)` without `ThenCall()` is NOT a sequence - it's a single repeating callback
- There is no "exhaustion" concept for a single repeating callback
- `ThenDefault()` only makes sense when a finite sequence can be exhausted
- `MethodCallBuilderImpl.ThenCall()` elevates to `MethodSequenceImpl` which DOES support `ThenDefault()`

**Correct Mental Model:**
```csharp
// Single callback - repeats forever, no exhaustion possible
stub.Method.OnCall(() => 1);  // Returns: 1, 1, 1, 1... (forever)

// Sequence - can be exhausted, ThenDefault() applies here
stub.Method.OnCall(() => 1).ThenReturns(2);  // ThenCall elevates to MethodSequenceImpl
// Returns: 1, 2, 2, 2... (repeats last, or with ThenDefault(): 1, 2, default, default...)
```

**Implementation:**
- Only add `ThenDefault()` to `MethodSequenceImpl` (and corresponding property/indexer sequence classes)
- Do NOT add `ThenDefault()` to `MethodCallBuilderImpl`
- The table in "Member Types Affected" section was incorrect and will be corrected below

---

### Concern 3: Void Method Sequences

**Question:** Should `ThenDefault()` be included on void method sequences for consistency, or omitted since it has no observable effect on return values?

**Answer:** **Include `ThenDefault()` for consistency.**

**Rationale:**
1. **API Consistency:** Users should not need to remember "does this method return void?" when writing tests. The API should be uniform.

2. **Semantic Meaning:** While void methods have no return value, `ThenDefault()` still has semantic meaning:
   - "After exhaustion, do nothing" vs. "After exhaustion, repeat last callback"
   - For void methods with side effects, repeating the last callback could be significant

3. **Verification Semantics:** `ThenDefault()` affects exhaustion behavior, NOT verification:
   - `sequence.Verify()` verifies the sequence was fully consumed at least once
   - Whether the sequence repeats or returns default after exhaustion is orthogonal to verification
   - A void sequence with `ThenDefault()` still passes `Verify()` if consumed once

4. **Practical Effect for Void Methods:**
   - With `_repeatLastValue = true` (default): Last callback executes repeatedly after exhaustion
   - With `ThenDefault()` (`_repeatLastValue = false`): No callback executes after exhaustion (strict mode throws, non-strict silently continues)

**Implementation:**
- Add `ThenDefault()` to all sequence classes regardless of void/non-void
- The method signature is always `void ThenDefault()` (no return type to worry about)
- The implementation is identical: `_repeatLastValue = false;`

---

## Overview

Change sequence exhaustion behavior from "return default" to "repeat last value" (NSubstitute-like). Add `ThenDefault()` method as explicit opt-out for tests that need the original behavior.

---

## Approach

1. **Default behavior**: After sequence exhausted, repeat the last configured callback
2. **ThenDefault()**: Explicit terminator that restores "return default" behavior
3. **Strict mode**: Unchanged - throws exception on exhaustion

This is a **breaking change** that aligns KnockOff with NSubstitute's more forgiving default.

---

## Design

### API Surface

**Default behavior (repeat last):**
```csharp
stub.Method.OnCall(() => 1).ThenReturns(2).ThenReturns(3);
// Returns: 1, 2, 3, 3, 3, 3... (repeats forever)

stub.Name.OnGet("first").ThenGet("second");
// Returns: "first", "second", "second", "second"...
```

**Explicit default termination:**
```csharp
stub.Method.OnCall(() => 1).ThenReturns(2).ThenDefault();
// Returns: 1, 2, default, default, default...

stub.Name.OnGet("first").ThenGet("second").ThenDefault();
// Returns: "first", "second", null, null, null...
```

**Strict mode (unchanged):**
```csharp
stub.Strict = true;
stub.Method.OnCall(() => 1).ThenReturns(2);
// Returns: 1, 2, then throws SequenceExhausted
```

### Generated Code Pattern

**MethodSequenceImpl:**
```csharp
private sealed class MethodSequenceImpl : IMethodSequence<Func<int>>
{
    private bool _repeatLastValue = true;  // NEW: default to repeat

    public MethodSequenceImpl ThenCall(Func<int> callback) { ... }
    public MethodSequenceImpl ThenReturns(int value) { ... }

    // NEW: Terminates sequence with default(T) after exhaustion
    public void ThenDefault()
    {
        _repeatLastValue = false;
    }
}
```

**Invoke method changes (conceptual):**
```csharp
// When sequence exhausted
if (_sequence != null && _sequenceIndex >= _sequence.Count)
{
    if (_repeatLastValue)
    {
        // Repeat last callback (do NOT increment index)
        var (callback, tracking) = _sequence[_sequence.Count - 1];
        tracking.RecordCall(...);
        return callback(...);
    }
    // Existing exhaustion handling (strict throws, non-strict returns default)
    ...
}
```

### Member Types Affected

| Member Type | Sequence Class | Changes |
|-------------|----------------|---------|
| Methods (non-void) | `MethodSequenceImpl` | Add `_repeatLastValue`, `ThenDefault()` |
| Methods (void) | `MethodSequenceImpl` | Add `_repeatLastValue`, `ThenDefault()` (for consistency, repeats last callback) |
| Property Gets | `PropertyGetSequenceImpl` | Add `_repeatLastValue`, `ThenDefault()` |
| Property Sets | `PropertySetSequenceImpl` | Add `_repeatLastValue`, `ThenDefault()` (for consistency, repeats last callback) |
| Indexer Gets | `IndexerGetSequenceImpl` | Add `_repeatLastValue`, `ThenDefault()` |
| Indexer Sets | `IndexerSetSequenceImpl` | Add `_repeatLastValue`, `ThenDefault()` (for consistency, repeats last callback) |

**Important:** `MethodCallBuilderImpl` does NOT get `ThenDefault()` - it handles single repeating callbacks which cannot be exhausted. See Concern 2 clarification above.

### Library Interface Updates

The following interfaces in `src/KnockOff/` need `ThenDefault()` added:

| Interface | File | Signature |
|-----------|------|-----------|
| `IMethodSequence` | `IMethodSequence.cs` | `void ThenDefault();` |
| `IPropertyGetSequence<TValue>` | `IPropertySequence.cs` | `void ThenDefault();` |
| `IPropertySetSequence<TValue>` | `IPropertySequence.cs` | `void ThenDefault();` |
| `IIndexerGetSequence<TKey, TValue>` | `IIndexerSequence.cs` | `void ThenDefault();` |
| `IIndexerSetSequence<TKey, TValue>` | `IIndexerSequence.cs` | `void ThenDefault();` |

### Return Type of ThenDefault()

**`void`** - Terminates the fluent chain since no further configuration makes sense after declaring exhaustion behavior.

---

## Implementation Steps

### Phase 0: Library Interface Updates

1. Add `void ThenDefault();` to `IMethodSequence` in `src/KnockOff/IMethodSequence.cs`
2. Add `void ThenDefault();` to `IPropertyGetSequence<TValue>` in `src/KnockOff/IPropertySequence.cs`
3. Add `void ThenDefault();` to `IPropertySetSequence<TValue>` in `src/KnockOff/IPropertySequence.cs`
4. Add `void ThenDefault();` to `IIndexerGetSequence<TKey, TValue>` in `src/KnockOff/IIndexerSequence.cs`
5. Add `void ThenDefault();` to `IIndexerSetSequence<TKey, TValue>` in `src/KnockOff/IIndexerSequence.cs`
6. **Checkpoint:** KnockOff library builds

### Phase 1: Method Sequences (Generator)

7. In `MethodInterceptorRenderer.cs`, modify `RenderMethodSequenceImpl`:
   - Add `private bool _repeatLastValue = true;` field
   - Add `public void ThenDefault() { _repeatLastValue = false; }` method
8. Modify `Invoke` in interceptor to check `_repeatLastValue` when sequence exhausted
9. **Checkpoint:** Build, verify generated code for method sequences

### Phase 2: Property Sequences (Generator)

10. Add `_repeatLastValue` field and `ThenDefault()` to property get sequence rendering
11. Add `_repeatLastValue` field and `ThenDefault()` to property set sequence rendering
12. Modify property sequence execution for repeat behavior
13. **Checkpoint:** Build, verify generated code for property sequences

### Phase 3: Indexer Sequences (Generator)

14. Add same pattern to indexer get sequence rendering
15. Add same pattern to indexer set sequence rendering
16. **Checkpoint:** Build, verify generated code for indexer sequences

### Phase 4: Tests

17. Update existing exhaustion tests to expect repeat-last behavior
18. Add new tests for `ThenDefault()` functionality
19. Add tests for void method/setter sequences with `ThenDefault()`
20. Add tests for interaction with strict mode
21. **Checkpoint:** All tests pass

### Phase 5: Documentation

22. Update Design.Stubs sequence documentation
23. Add comparison note showing KnockOff matches NSubstitute behavior

---

## Acceptance Criteria

- [ ] Default: `OnCall().ThenReturns()` repeats last value after exhaustion
- [ ] Default: `OnGet().ThenGet()` repeats last value after exhaustion
- [ ] `ThenDefault()` causes sequence to return `default(T)` after exhaustion
- [ ] Strict mode still throws on exhaustion (unless explicit repeat configured)
- [ ] `sequence.Verify()` passes when sequence consumed at least once
- [ ] `Reset()` works correctly with repeating sequences
- [ ] All four patterns supported

---

## Dependencies

- Existing `ThenReturns(value)` implementation (just completed)
- Existing sequence infrastructure in `MethodInterceptorRenderer.cs`

---

## Risks / Considerations

### Breaking Change

**Impact:** Tests that relied on exhausted sequences returning `default(T)` will now get the last value repeated.

**Mitigation:**
- Update KnockOff's own test suite
- Document in release notes with migration guide
- `ThenDefault()` provides escape hatch for affected tests

### Verification Semantics

**Question:** What does `sequence.Verify()` mean when repeating?

**Answer:** Verify passes when the sequence has been fully consumed at least once. Repeated calls beyond sequence length don't affect verification.

### Strict Mode Interaction

**Decision:** Strict mode STILL throws on exhaustion, even with `_repeatLastValue = true` by default. This is because strict mode is explicitly checking for unexpected calls. To allow repeats in strict mode, user must... actually, reconsider this.

**Revised Decision:** If `_repeatLastValue = true` (default), strict mode should NOT throw. The "repeat last" is the configured behavior. Only if `ThenDefault()` was called (making `_repeatLastValue = false`) should strict mode throw.

Wait, that doesn't match the user's request. Let me re-read:
> "#2 with the addition of ThenDefault and strict mode throws exception"

This means:
- Default: repeat last (NSubstitute behavior)
- ThenDefault(): return default after exhaustion
- Strict mode: throws exception (regardless of _repeatLastValue)

So strict mode ALWAYS throws on exhaustion, period. It's orthogonal to repeat behavior.

**Final Decision:** Strict mode throws on exhaustion regardless of `_repeatLastValue`. The flag only affects non-strict mode behavior.

---

## Architectural Verification

### Three Patterns Analysis

**Standalone Pattern:**
- Uses shared `MethodInterceptorRenderer` for method sequences
- Uses shared `PropertyInterceptorRenderer` for property sequences
- Changes apply automatically

**Inline Interface Pattern:**
- Same renderers, changes apply automatically

**Inline Class Pattern:**
- Same renderers, changes apply automatically

**Inline Delegate Pattern:**
- Uses same `MethodInterceptorRenderer`
- Changes apply automatically

### Breaking Changes Assessment

**Breaking:** Yes - default behavior changes from "return default" to "repeat last"

**Migration:** Add `.ThenDefault()` to any sequence that relied on exhaustion returning default values

### Pattern Consistency

Matches NSubstitute behavior. Provides explicit escape hatch unlike NSubstitute (which has no way to return default after exhaustion).

---

## Developer Review

**Status:** Concerns Addressed

**Original Concerns (2026-02-01):**
1. Interface update unclear - RESOLVED: See "Developer Concerns Clarification" section above
2. MethodCallBuilderImpl confusion - RESOLVED: ThenDefault() is NOT added to MethodCallBuilderImpl
3. Void sequences - RESOLVED: Include ThenDefault() for API consistency

**Architect Response:** All three concerns have been addressed with explicit guidance in the "Developer Concerns Clarification" section. The implementation steps have been updated to reflect the corrected design.

---

## Implementation Contract

[To be filled by developer after review]

---

## Implementation Progress

[Track progress through phases]

---

## Completion Evidence

[Required before marking complete]

- **Tests Passing:** [Output or screenshot]
- **Generated Code Sample:** [Snippet showing feature works]
- **All Checklist Items:** [Confirmed 100% complete]
