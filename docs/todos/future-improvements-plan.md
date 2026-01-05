# KnockOff: Future Improvements Plan

**Created:** 2026-01-04
**Status:** Planning

## Overview

This document captures remaining concerns, gaps, and potential improvements identified during deep analysis of KnockOff after multiple development iterations.

---

## High Priority: Feature Gaps

### 1. Generic Methods Support

**Status:** Not implemented
**Impact:** High - Some Moq usage will remain until this is solved

**Challenge:** The generator can't know `T` at compile time for methods like:
```csharp
public interface ISerializer
{
    T Deserialize<T>(string json);
    string Serialize<T>(T value);
}
```

**Possible Approaches:**
- [ ] Emit `object`-based handlers with runtime type checking
- [ ] Generate handlers for common type arguments if detectable from usage
- [ ] Emit diagnostic suggesting explicit interface with concrete types for testing

**Tasks:**
- [ ] Research how other source generators handle generic methods
- [ ] Design handler API for generic method tracking
- [ ] Implement basic support for unconstrained generic methods
- [ ] Add constraint-aware generation (`where T : class`, `where T : new()`)

---

### 2. Set-Only Properties

**Status:** Bug filed (`bug-set-only-properties-not-supported.md`)
**Impact:** Low - Edge case but should work

**Example:**
```csharp
public interface IWriteOnly
{
    string Password { set; }  // No getter
}
```

**Tasks:**
- [ ] Investigate current generator behavior for set-only properties
- [ ] Generate backing field and setter without getter
- [ ] Add handler tracking for set operations
- [ ] Add test coverage

---

## Medium Priority: API Refinements

### 3. Handler Naming Consistency

**Current State:**
- `LastCallArg` (singular) - single parameter
- `LastCallArgs` (tuple) - multiple parameters
- `AllCalls` (plural) - list of all calls

**Inconsistencies:**
- Singular vs plural isn't immediately obvious
- `Args` suffix used inconsistently

**Proposal:**
```csharp
// Current
handler.LastCallArg      // Single param
handler.LastCallArgs     // Multiple params (tuple)
handler.AllCalls         // All calls

// Consider standardizing to:
handler.LastCall         // Always returns the args (single value or tuple)
handler.Calls            // All calls (rename from AllCalls)
handler.CallCount        // Keep as-is
```

**Tasks:**
- [ ] Audit all handler properties for naming consistency
- [ ] Decide on naming convention
- [ ] Implement changes (breaking change - major version bump)
- [ ] Update documentation

---

### 4. Overload Handler UX

**Current State:** Overloads with same parameter name but different types generate `AllCalls0`, `AllCalls1` accessors.

**Issue:** Numeric suffixes are not self-documenting.

**Proposal:**
```csharp
// Current
handler.AllCalls0  // Process(string input)
handler.AllCalls1  // Process(int input)

// Better
handler.StringOverload.AllCalls
handler.IntOverload.AllCalls

// Or signature-based
handler["string"].AllCalls
handler["int"].AllCalls
```

**Tasks:**
- [ ] Design improved overload handler API
- [ ] Consider type-based nested handlers
- [ ] Implement and test
- [ ] Update migration guide

---

### 5. Reset Semantics

**Current State:** `Reset()` clears both tracking AND callbacks.

**Issue:** Sometimes you want to reset tracking but keep the callback behavior.

**Proposal:**
```csharp
handler.Reset()           // Clears everything (current behavior)
handler.ResetTracking()   // Clears call history only
handler.ResetCallback()   // Clears OnCall only, keeps tracking
```

**Tasks:**
- [ ] Add `ResetTracking()` method to handlers
- [ ] Add `ResetCallback()` method to handlers
- [ ] Keep `Reset()` as convenience for both
- [ ] Update documentation

---

## Low Priority: Nice to Have

### 6. Async Handler Clarity

**Question:** Does `GetValueAsync` handler track the `Task<T>` or the unwrapped `T` value?

**Current:** Tracks the arguments passed TO the method, not the return value.

**Task:**
- [ ] Clarify in documentation
- [ ] Consider adding `LastReturnValue` tracking (opt-in?)

---

### 7. Strict Mode Equivalent

**Moq Feature:** `MockBehavior.Strict` throws if unexpected method called.

**KnockOff Gap:** No equivalent - always returns defaults.

**Possible Implementation:**
```csharp
[KnockOff(Strict = true)]
public partial class StrictServiceKnockOff : IService { }

// Generated code throws if method called without OnCall set
```

**Tasks:**
- [ ] Evaluate demand for strict mode
- [ ] Design attribute parameter
- [ ] Implement strict checking in generated code

---

### 8. VerifyNoOtherCalls Equivalent

**Moq Feature:** Ensures no unexpected calls were made.

**KnockOff Gap:** Would require tracking all calls and comparing to verified set.

**Possible Implementation:**
```csharp
knockOff.VerifyNoOtherCalls(); // Throws if any unverified calls exist
```

**Tasks:**
- [ ] Design verification tracking mechanism
- [ ] Implement `VerifyNoOtherCalls()` method
- [ ] Add to spy classes

---

## Documentation Gaps

### 9. Documentation Snippet Sync

**Status:** Todo exists (`sync-documentation-snippets.md`)

**Task:**
- [ ] Ensure all code snippets in docs are compiled and tested
- [ ] Set up snippet extraction from test projects

---

## Ecosystem Considerations

### 10. Community Adoption

**Challenge:** Moq has 15 years of Stack Overflow answers and community knowledge.

**Tasks:**
- [ ] Create migration guide from Moq (exists: `migration-from-moq.md`)
- [ ] Add common patterns/recipes documentation
- [ ] Consider blog post or article introducing KnockOff
- [ ] Respond to community questions/issues promptly

---

## Summary

| Item | Priority | Complexity | Breaking Change |
|------|----------|------------|-----------------|
| Generic methods | High | High | No |
| Set-only properties | High | Low | No |
| Handler naming consistency | Medium | Medium | Yes |
| Overload handler UX | Medium | Medium | Yes |
| Reset semantics | Medium | Low | No (additive) |
| Async handler clarity | Low | Low | No |
| Strict mode | Low | Medium | No |
| VerifyNoOtherCalls | Low | Medium | No |
| Doc snippet sync | Low | Low | No |

**Recommended Next Steps:**
1. Fix set-only properties bug (low effort, completes feature set)
2. Implement generic methods (high value, unlocks full Moq parity)
3. Add `ResetTracking()` / `ResetCallback()` (non-breaking improvement)
