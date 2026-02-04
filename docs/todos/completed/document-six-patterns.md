# Document All Six KnockOff Patterns

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-03
**Completed:** 2026-02-03

---

## Problem

The current documentation says KnockOff has "4 patterns" but the codebase actually supports 6 distinct patterns:

1. **Standalone** - `[KnockOff] partial class Stub : IService`
2. **Generic Standalone** - `[KnockOff] partial class Stub<T> : IService<T>`
3. **Inline Interface** - `[KnockOff<IService>]`
4. **Inline Class** - `[KnockOff<ConcreteClass>]`
5. **Inline Delegate** - `[KnockOff<DelegateType>]`
6. **Open Generic** - `[KnockOff(typeof(T<>))]` (interfaces, classes, or delegates)

The Design source of truth (`AllPatterns.cs`) explicitly documents Pattern 5 as "OPEN GENERIC STUB" but this isn't reflected in user-facing docs. Generic Standalone has its own dedicated test file (`GenericStandaloneStubTests.cs`) but isn't called out as a distinct pattern.

## Solution

Update documentation to accurately describe all 6 patterns:

1. Update `CLAUDE.md` "Four Patterns" section to "Six Patterns"
2. Update `docs/guides/stub-patterns.md` (if it exists) or create pattern documentation
3. Update `skills/knockoff/` reference files (patterns.md, SKILL.md)
4. Update Design file comments if needed for consistency

---

## Plans

- [Document All Six KnockOff Patterns](../plans/document-six-patterns.md)

---

## Tasks

- [x] Audit current pattern documentation across all files
- [x] Update CLAUDE.md pattern count and descriptions
- [x] Update/create stub-patterns documentation
- [x] Update skills/knockoff pattern references
- [x] Verify Design source of truth is consistent
- [x] Add examples for Generic Standalone and Open Generic patterns

---

## Progress Log

**2026-02-03:** Implementation complete across all 5 phases:
- Phase 1: AllPatterns.cs - Added PATTERN 1B Generic Standalone, renumbered Open Generic to Pattern 6
- Phase 2: CLAUDE.md, CLAUDE-DESIGN.md - Updated to six patterns
- Phase 3: stub-patterns.md major rewrite, delegates.md, getting-started.md updates
- Phase 4: All 6 skill documentation files updated (self-contained)
- Phase 5: Agent files (knockoff-architect.md, knockoff-developer.md) updated

---

## Results / Conclusions

**Successfully documented all six KnockOff stub patterns.**

19 files changed (+1432 / -271 lines):
- Tier 1: AllPatterns.cs (exhaustive source of truth)
- Tier 2: CLAUDE.md, CLAUDE-DESIGN.md (agent quick refs)
- Tier 3: stub-patterns.md, delegates.md, getting-started.md (user guides)
- Tier 4: 6 skill files (self-contained for external projects)
- Tier 5: 2 agent files (verification checklists)

New files created:
- IGenericService.cs (interface for Generic Standalone example)
- PatternsSamples.cs (code samples for documentation)

All tests pass. Documentation is now consistent across all tiers.
