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

---

## Tasks

- [ ] Architect designs the implementation plan (all 9 patterns, all 4 member types)
- [ ] Developer reviews and creates implementation contract
- [ ] Implement: Drop `.OnCall()` from non-void interceptors, ensure `.Returns()` covers all cases
- [ ] Implement: Replace `.OnCall()` with `.Execute()` on void interceptors
- [ ] Implement: Update `.When()` chains to work with both `.Returns()` and `.Execute()`
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

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully
- [ ] Design project tests pass
- [ ] All 9 patterns generate correct API (`.Returns()` for non-void, `.Execute()` for void)
- [ ] `.OnCall()` is fully removed from generated code
- [ ] `.When()` chains work with both `.Returns()` and `.Execute()`
- [ ] Skill documentation updated
- [ ] Version bumped

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]

---

## Results / Conclusions

