# Rename Returns/ThenReturns to Return/ThenReturn

**Status:** Not Started
**Priority:** High
**Created:** 2026-02-07
**Last Updated:** 2026-02-07

---

## Problem

KnockOff's non-void method API uses plural third-person verb forms (`Returns`/`ThenReturns`) while the rest of the API uses singular imperative forms (`Call`/`ThenCall`/`Get`/`Set`/`ThenGet`/`ThenSet`). The API should be consistently singular imperative.

Current API:
```csharp
stub.GetName.Returns("John");                          // plural
stub.GetName.Returns(() => "First").ThenReturns(() => "Second");  // plural
stub.GetName.When("id").Returns("Alice");              // plural
```

Target API:
```csharp
stub.GetName.Return("John");                           // singular
stub.GetName.Return(() => "First").ThenReturn(() => "Second");    // singular
stub.GetName.When("id").Return("Alice");               // singular
```

This decision was made during the OnGet/OnSet → Get/Set rename discussion. All KnockOff configuration verbs should use singular imperative form for consistency.

## Solution

Rename all `Returns`/`ThenReturns` to `Return`/`ThenReturn` across:

| Current | Target |
|---------|--------|
| `.Returns(value)` | `.Return(value)` |
| `.Returns(callback)` | `.Return(callback)` |
| `.ThenReturns(value)` | `.ThenReturn(value)` |
| `.ThenReturns(callback)` | `.ThenReturn(callback)` |
| `IMethodReturnsBuilder<T>` | `IMethodReturnBuilder<T>` |
| `IMethodReturnsSequence<T>` | `IMethodReturnSequence<T>` |

Also update When chain returns: `.When(value).Returns(...)` → `.When(value).Return(...)`

**Depends on:** Should be done after `rename-onget-onset-to-get-set.md` and `migrate-execute-to-call.md` to avoid merge conflicts.

---

## Plans

---

## Tasks

- [ ] Architect creates implementation plan
- [ ] Developer reviews and approves
- [ ] Implement rename in generator, library, design, tests, docs, skills
- [ ] Version bump

---

## Progress Log

### 2026-02-07
- Created todo from API verb form consistency decision
- Scope: non-void method API only (`Returns`/`ThenReturns` → `Return`/`ThenReturn`)

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

