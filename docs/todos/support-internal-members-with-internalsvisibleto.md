# Support Internal Members with InternalsVisibleTo

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-03-14
**Last Updated:** 2026-03-14

---

## Problem

Standalone stubs don't automatically include internal interface members even when `InternalsVisibleTo` is configured between the interface's assembly and the stub's assembly. Users expect that if their test project has `InternalsVisibleTo` access, the generator should stub internal members too.

## Solution

Change the `IsMemberAccessible` check in the generator from same-assembly identity (`SymbolEqualityComparer.Default.Equals`) to Roslyn's `IAssemblySymbol.GivesAccessTo()`, which respects `InternalsVisibleTo`. A secondary constructor accessibility check in `ExtractClassInfo` has the same pattern and needs the same fix.

---

## Clarifications

[Pending — Step 2]

---

## Requirements Review

**Reviewer:** [pending]
**Reviewed:** [pending]
**Verdict:** Pending

### Relevant Requirements Found

### Gaps

### Contradictions

### Recommendations for Architect

---

## Plans

---

## Tasks

- [ ] Architect comprehension check (Step 2)
- [ ] Business requirements review (Step 3)
- [ ] Architect plan creation & design (Step 4)
- [ ] Developer review (Step 5)
- [ ] Implementation (Step 7)
- [ ] Verification (Step 8)
- [ ] Documentation (Step 9)
- [ ] Completion (Step 10)

---

## Progress Log

### 2026-03-14
- User reported that standalone stubs miss internal interface members even with InternalsVisibleTo
- Architect investigated feasibility: confirmed it's a targeted fix in `IsMemberAccessible` (`KnockOffGenerator.Transform.cs:1261`) — replace `SymbolEqualityComparer.Default.Equals` with `GivesAccessTo`
- Affects all four patterns (shared gatekeeper method) plus one constructor check in `ExtractClassInfo`
- Todo created, not started

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass

**Verification results:**
- Build: [Pending]
- Tests: [Pending]

---

## Results / Conclusions

