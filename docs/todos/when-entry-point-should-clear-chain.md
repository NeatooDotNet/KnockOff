# When() Entry Point Should Clear Existing When Chain

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-02-07
**Last Updated:** 2026-02-07

---

## Problem

Calling `.When()` as a new entry point on an interceptor accumulates matchers into the existing When chain, identical to `.ThenWhen()`. This violates the "last one wins" principle that applies to all other configuration methods (Return, Call, Get, Set).

Current behavior:
```csharp
stub.Method.When(1, 2).Return(100);
stub.Method.When(1, 2).Return(200);  // Adds second matcher, does NOT replace
```

This creates two matchers in the list. First call returns 100, second call returns 200 (sequence behavior). The user expected only 200 to be returned because the second `.When()` is a fresh entry point, not a chained `.ThenWhen()`.

Expected behavior:
- `.When()` (entry point) — clears the existing When chain and starts fresh (last one wins)
- `.ThenWhen()` (chaining) — accumulates matchers in the existing chain

## Root Cause

The generated `.When()` method uses `??=` to initialize the list, which preserves the existing list if one already exists:

```csharp
// MethodInterceptorRenderer.cs line 2198
_whenChain ??= new List<WhenMatcher>();
// Should be: _whenChain = new List<WhenMatcher>();
```

The matcher is then added to the list when `.Return()` is called on the WhenBuilder — same code path as `.ThenWhen()`.

## Solution

Change the `.When()` entry point to clear the existing When chain before creating a new one. This makes `.When()` consistent with the "last one wins" principle while `.ThenWhen()` remains accumulative.

The fix should be in the renderer code that generates the When() entry point methods. The `??=` should become `=` (new list assignment), clearing any previous chain.

**Affected renderer locations:**
- `MethodInterceptorRenderer.cs` — `RenderWhenEntryPoints` for non-void methods
- `MethodInterceptorRenderer.cs` — `RenderVoidWhenEntryPoints` for void methods
- Also need to reset `_whenChainHead` to 0 when clearing

**Scope:** All patterns that support When chains (methods, delegates). Properties and indexers don't have When chains.

---

## Plans

[Plan(s) created to design and implement the solution]

---

## Tasks

- [ ] Create implementation plan
- [ ] Fix When() entry point to clear chain in renderer
- [ ] Fix void When() entry point to clear chain in renderer
- [ ] Reset _whenChainHead to 0 when chain is cleared
- [ ] Add tests: separate When() calls should replace (last one wins)
- [ ] Add tests: ThenWhen() still accumulates correctly
- [ ] Verify all existing When chain tests still pass
- [ ] Update Design.Stubs with examples showing the distinction
- [ ] Update skill and docs to clarify When() vs ThenWhen() behavior

---

## Progress Log

### 2026-02-07
- Discovered bug while documenting "last one wins" principle for all configuration methods
- Confirmed root cause: `??=` in generated When() entry point preserves existing chain
- When() and ThenWhen() are currently indistinguishable in behavior

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully
- [ ] Design project tests pass

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]

---

## Results / Conclusions

[What was learned? What decisions were made?]
