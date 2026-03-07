# Delegate-Based Call API

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-19
**Last Updated:** 2026-02-19

---

## Problem

v0.52.0 introduced `Func<(T1, T2), TReturn>` / `Action<(T1, T2)>` for 2+ parameter Call callbacks. This causes CS0121 (ambiguous overload) when an interface has overloaded methods whose parameter tuples differ only in element count with the same element types. Example:

```csharp
public interface IAuthSvc
{
    bool ValidateCredentials(string username, string password);
    bool ValidateCredentials(string username, string password, string token);
}
```

C# cannot determine which `Func<tuple, bool>` a bare lambda targets.

## Solution

Replace Func/Action+tuple with custom named delegates for ALL Call callbacks. Add rich XML comments with return types, and rename generated type names to method-name-based (`AddDelegate`, `AddImpl`, `AddSequence`).

---

## Plans

- [Delegate-Based Call API Plan](../plans/delegate-based-call-api.md)

---

## Tasks

- [x] Architect creates design plan
- [x] Developer reviews plan, raises concerns (7 concerns raised)
- [x] Architect addresses developer concerns (revision 2)
- [x] User answers open questions (When predicates, async naming, ThenReturn/ThenCall delegates, overload numbering)
- [x] Developer re-reviews plan, approves (revision 3)
- [x] Implementation Phase 0: Dead code cleanup + regression test setup
- [x] Implementation Phase 1: Builder/Model changes
- [x] Implementation Phase 2: Renderer changes
- [x] Implementation Phase 3: Consumer code updates
- [x] Implementation Phase 4: Verification (5,390+ tests pass, CS0121 regression test passes)
- [x] Architect verification (VERIFIED 2026-02-19)
- [x] Version bumped to 0.53.0
- [x] Release notes created

---

## Progress Log

### 2026-02-19
- Created plan at `docs/plans/delegate-based-call-api.md`
- Architect designed 9 design decisions covering custom delegates, naming conventions, XML docs, async simplified delegates, and interaction with existing interceptor base class
- Developer reviewed: raised 7 concerns (2 major, 3 medium, 2 low)
- Architect addressed all concerns in revision 2 (DD5 rewritten as already-done, current examples fixed, Phase 0 added for dead code + regression test, async overload naming expanded)
- 4 open questions awaiting user input
- User answered all 4 open questions:
  1. When predicates: custom delegates (full consistency)
  2. Simplified async naming: shorter `{MethodName}SyncDelegate`
  3. ThenReturn/ThenCall: reuse same `{MethodName}Delegate`
  4. Overload numbering: shifts acceptable (users write lambdas, not delegate types)
- Plan updated with resolved decisions

---

## Critical Rules

### Developer: STOP If Any Pattern Is Missing

**At every implementation checkpoint**, the developer MUST verify the change works for ALL 9 patterns. If any pattern is not addressed, **STOP immediately** and report which pattern is missing.

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] CS0121 regression test compiles and passes (primary proof)
- [ ] All 9 patterns generate custom named delegates
- [ ] XML comments include `-> ReturnType`
- [ ] Builder/Sequence types use method-name-based names
- [ ] Design project builds and tests pass
- [ ] All test suites pass
- [ ] Version bumped to 0.53.0
