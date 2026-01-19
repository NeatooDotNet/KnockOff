# Reset Semantics Improvement

**Status:** Complete
**Priority:** Medium
**Created:** (prior)
**Last Updated:** 2026-01-19

---

## Problem

`Reset()` behavior was inconsistent and cleared both tracking state AND configuration (callbacks, values). This didn't match what Moq developers expect - Moq's `Invocations.Clear()` only clears call history, not setup.

**Previous Behavior:**
```csharp
knockOff.GetUser.OnCall = (id) => new User { Id = id };
service.GetUser(1);
service.GetUser(2);

knockOff.GetUser.Reset();
// CallCount = 0 - correct
// LastCallArg = default - correct
// OnCall = null - WRONG! Callback cleared unexpectedly
```

## Solution

Updated all Reset() methods across all patterns (flat, inline, class) to:

**Clear (tracking state):**
- CallCount, GetCount, SetCount, AddCount, RemoveCount
- LastCallArg, LastCallArgs, LastSetValue, LastGetKey, LastSetEntry
- Sequence position (_sequenceIndex)
- Source delegation (_source)

**Preserve (configuration):**
- OnCall, OnGet, OnSet callbacks
- Value (property Value state)
- Sequence list (_sequence)
- Verifiable state (_isVerifiable, _verifiableTimes)
- Event handlers (Handler is tracking, but verifiable state preserved)
- Backing dictionary (for indexers)

---

## Plans

- (no formal plan needed - straightforward implementation)

---

## Tasks

- [x] Research what Moq's `Reset()` does
- [x] Update FlatRenderer property Reset() to preserve OnGet/OnSet/Value
- [x] Update FlatRenderer init-only property Reset() to preserve Value
- [x] Update FlatRenderer indexer Reset() to preserve OnGet/OnSet
- [x] Update FlatRenderer event Reset() to preserve verifiable marking
- [x] Update InlineRenderer property Reset() to preserve OnGet/OnSet/Value
- [x] Update InlineRenderer indexer Reset() to preserve OnGet/OnSet
- [x] Update InlineRenderer generic method handler Reset() to preserve OnCall
- [x] Update InlineRenderer event Reset() to preserve verifiable marking
- [x] Update InlineRenderer delegate Reset() to preserve OnCall
- [x] Update ClassRenderer property Reset() to preserve OnGet/OnSet
- [x] Update ClassRenderer indexer Reset() to preserve OnGet/OnSet
- [x] Update ClassRenderer method Reset() to preserve OnCall
- [x] Update ClassRenderer event Reset() to preserve verifiable marking
- [x] Update in-scope tests for new Reset semantics
- [x] All tests pass

---

## Progress Log

**2026-01-19:** Implementation complete. Updated Reset() semantics across all renderers:
- FlatRenderer.cs: Property (regular + init-only), Indexer, Event interceptors
- InlineRenderer.cs: Property, Indexer, GenericMethodHandler, TypedHandler, Event, Delegate interceptors
- ClassRenderer.cs: Property, Indexer, Method, Event interceptors
- MethodInterceptorRenderer.cs: Already had correct behavior (preserved OnCall)

Updated 5 test cases that were explicitly testing the old "clear callbacks" behavior to verify the new "preserve configuration" behavior.

---

## Results / Conclusions

**Breaking Change:** Code that depended on Reset() clearing callbacks will need to explicitly set callbacks to null if that behavior is desired.

**Files Modified:**
- `src/Generator/Renderer/FlatRenderer.cs`
- `src/Generator/Renderer/InlineRenderer.cs`
- `src/Generator/Renderer/ClassRenderer.cs`
- `src/Tests/KnockOffTests/InitPropertyTests.cs`
- `src/Tests/KnockOffTests/IndexerTests.cs`
- `src/Tests/KnockOffTests/InlineStubTests.cs`

**New Behavior:**
```csharp
knockOff.GetUser.OnCall = (id) => new User { Id = id };
service.GetUser(1);
service.GetUser(2);

knockOff.GetUser.Reset();
// CallCount = 0 - tracking cleared
// LastCallArg = default - tracking cleared
// OnCall = still set! - configuration preserved

// Can call immediately after Reset() with same configuration
service.GetUser(3);
Assert.Equal(1, knockOff.GetUser.CallCount);
```

This matches Moq developer expectations and provides consistent behavior across all interceptor types.
