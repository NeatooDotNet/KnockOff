# Unify Configuration API: Returns + Execute, Drop OnCall

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-06
**Last Updated:** 2026-02-06

---

## Problem

KnockOff currently has two overlapping configuration methods:
- `.Returns(value)` — set return value for non-void methods
- `.OnCall(lambda)` — set callback for any method (void or non-void), also used for overload disambiguation

Having both causes confusion. When the skill was used outside the KnockOff repository, Claude couldn't determine when to use `.Returns()` vs `.OnCall()`. The overlap is the root cause — they do conceptually the same thing ("when this member is called, do this") but are split across two names.

Additionally, `.When(value)` currently chains to `.Returns()` for parameter matching. This chain needs to work with the new unified API.

The method sequence entry point `.OnCallSequence()` must also be removed. Sequences should use `.Returns(...).ThenReturns(...)` / `.Execute(...).ThenExecute(...)` chaining instead of a separate entry point. Prior work exists: `IMethodCallBuilder` interfaces are already in `src/KnockOff/` and an approved plan exists at `docs/plans/simplify-oncall-sequence-api-design.md` (from 2026-01-29) — though method names will change to match the new Returns/Execute API.

**Explicitly out of scope:** Property and indexer APIs (`OnGet`, `OnSet`, `OnGetSequence`, `OnSetSequence`) stay as-is. The tension being resolved is void vs non-void *methods* only.

## Solution

Replace the current three-method API (`.Returns()`, `.OnCall()`, `.When()`) with a clean two-method API where the method's return type determines which configuration method is available:

**Non-void methods → `.Returns()` only**
```csharp
stub.GetName.Returns("John");                    // Direct value
stub.GetName.Returns((id) => LookupName(id));    // Lambda
stub.GetName.When("id1").Returns("Alice");        // Parameter matching
stub.GetEvents.Returns((attrs) => result);        // Overload disambiguation via lambda
```

**Void methods → `.Execute()` only**
```csharp
stub.DoWork.Execute(() => { log.Add("called"); });       // Simple callback
stub.DoWork.Execute((param) => { Process(param); });     // Overload disambiguation
stub.DoWork.When(specificParam).Execute(() => { });       // Parameter matching
```

**Key design decisions:**
1. Drop `.OnCall()` entirely
2. `.Returns()` stays for non-void methods (direct value + lambda overloads)
3. `.Execute()` replaces `.OnCall()` for void methods only
4. `.When()` stays as the parameter matcher, chains to either `.Returns()` or `.Execute()`
5. Generated interceptors only expose the correct method — void interceptors get `.Execute()`, non-void get `.Returns()`. The wrong method doesn't exist.
6. Breaking change — acceptable per user decision

**Why `.Execute()`:**
- `.Returns()` and `.Execute()` are complementary verbs — one is about output, one is about action
- `.Execute()` describes WHAT (execute this action), not WHEN
- Not copied from any existing mocking framework

---

## Plans

- [Unify Method API: Returns + Execute Design](../plans/unify-returns-execute-design.md)

---

## Tasks

- [ ] Architect designs the implementation plan (all 9 patterns, all 4 member types)
- [ ] Developer reviews and creates implementation contract
- [ ] Implement: Drop `.OnCall()` from non-void interceptors, ensure `.Returns()` covers all cases
- [ ] Implement: Replace `.OnCall()` with `.Execute()` on void interceptors
- [ ] Implement: Update `.When()` chains to work with both `.Returns()` and `.Execute()`
- [ ] Implement: Remove `.OnCallSequence()` entry point for methods
- [ ] Implement: Add `.ThenReturns()` / `.ThenExecute()` chaining from builder interfaces (methods only)
- [ ] Implement: Add simplified async callback overloads to `ThenReturns` (subsumes sequence-callback-simplification todo)
- [ ] Update Design.Stubs to use new API
- [ ] Update Design.Tests to use new API
- [ ] Update KnockOff skill documentation
- [ ] Update broader docs (references, guides)
- [ ] Update MarkdownSnippet samples
- [ ] Bump version (breaking change)

---

## Progress Log

### 2026-02-06
- Brainstormed API design: considered `.OnCall()`, `.Responds()`, `.Then()`, `.Answers()` as unified names
- Decided against single-method approach due to void/non-void semantic mismatch
- Settled on `.Returns()` for non-void + `.Execute()` for void — clean semantic split
- `.When()` stays as parameter matcher chaining to either terminal method
- Created todo to track the work
- Architect investigation complete. Key findings:
  - `OnCallSequence` already removed from generator (prior work). Builder chaining (`ThenCall`/`ThenReturns`) already exists.
  - All patterns share `MethodInterceptorRenderer.cs` — generator change propagates to all 9 patterns
  - Scale: ~511 OnCall in tests, ~235 in Design projects, ~383 in doc samples, ~6 skill files
  - Docs/skills work is comparable to or larger than the generator work
- Decisions made (all questions resolved):
  - Q1: Sequence chaining → `ThenReturns`/`ThenExecute` (Option A)
  - Q2: `Returns(callback)` replaces `OnCall` for non-void — yes
  - Q3: All three (value, simplified async, full delegate) under `Returns()` — yes
  - Q4: Void When chains: `Call()` → `Execute()`, `ThenCall()` → `ThenExecute()`. Non-void When chain `ThenCall` stays as-is (different semantics from sequence)
  - Q5: Incorporate sequence-callback-simplification (simplified async on ThenReturns) — subsumes that todo
  - Q6-7: Separate void vs non-void builder/sequence interfaces — yes
  - Q8: Minor version bump (pre-1.0 convention) — yes
  - Q9: Phase A — generator first, fresh agents for tests, fresh docs agent for docs/skills
- Invoking architect to create implementation plan
- Plan created at `docs/plans/unify-returns-execute-design.md` -- covers all 6 phases, interface redesign, generator changes, test/design/docs scope, and acceptance criteria
- Developer review raised 5 concerns (WhenChainRenderer dead code, internal self-call patterns, stale superseded plan, CA1716 suppression, overload group path). All addressed by architect -- plan updated and returned for developer re-review.

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully
- [ ] Design project tests pass
- [ ] All 9 patterns generate correct API (`.Returns()` for non-void, `.Execute()` for void)
- [ ] `.OnCall()` is fully removed from generated code
- [ ] `.OnCallSequence()` is fully removed from method interceptors
- [ ] `.When()` chains work with both `.Returns()` and `.Execute()`
- [ ] Skill documentation updated
- [ ] Version bumped

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]

---

## Results / Conclusions

