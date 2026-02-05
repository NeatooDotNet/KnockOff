# Document All Seven KnockOff Patterns

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-03
**Last Updated:** 2026-02-04

---

## Problem

The documentation was recently updated to describe "6 patterns" but Open Generic was incorrectly counted as one pattern when it's actually two distinct patterns:

- **Open Generic Interface** - `[KnockOff(typeof(IService<>))]`
- **Open Generic Class** - `[KnockOff(typeof(ServiceBase<>))]`

These behave differently:
- Open Generic Interface: stub IS the implementation (like other interface stubs)
- Open Generic Class: uses `.Object` property (like other class stubs)
- API inconsistency: class version uses numbered interceptors for overloads (see `class-stub-overload-consistency.md`)

**The 7 patterns:**

1. **Standalone** - `[KnockOff] partial class Stub : IService`
2. **Generic Standalone** - `[KnockOff] partial class Stub<T> : IService<T>`
3. **Inline Interface** - `[KnockOff<IService>]`
4. **Inline Class** - `[KnockOff<ConcreteClass>]`
5. **Inline Delegate** - `[KnockOff<DelegateType>]`
6. **Open Generic Interface** - `[KnockOff(typeof(IService<>))]`
7. **Open Generic Class** - `[KnockOff(typeof(ServiceBase<>))]`

## Solution

Update all documentation to accurately describe 7 patterns:

1. Update `AllPatterns.cs` - Split Pattern 6 into 6 (interface) and 7 (class)
2. Update `CLAUDE.md` "Six Patterns" section to "Seven Patterns"
3. Update `src/Design/CLAUDE-DESIGN.md`
4. Update `docs/guides/stub-patterns.md`
5. Update `skills/knockoff/` reference files
6. Update agent files (knockoff-architect.md, knockoff-developer.md)

---

## Plans

- [Seven Patterns Documentation Plan](../plans/seven-patterns-documentation.md)

---

## Tasks

- [x] Update AllPatterns.cs to document 7 patterns (split Open Generic)
- [x] Update CLAUDE.md pattern count and descriptions
- [x] Update CLAUDE-DESIGN.md
- [x] Update docs/guides/stub-patterns.md
- [x] Update skills/knockoff pattern references
- [x] Update agent verification checklists
- [x] Verify all pattern counts are consistent (search for "six patterns", "6 patterns")

---

## Progress Log

### 2026-02-04: Plan approved, implementation contract created

Developer review verified all 4 concerns were adequately addressed:
1. Missing files - all 5 files verified to exist with stated content
2. Decision tree - specification shows before/after with clear logic
3. Complete example - includes compilable code
4. Comparison table - all 9 rows with all 7 column values

Plan approved. Implementation contract created with 7 phases, verification gates, and stop conditions.

### 2026-02-04: Developer concerns addressed

Developer review identified 4 concerns with the plan:
1. Missing files (5 additional files with "six patterns" references)
2. Decision tree logic not specified
3. Complete example scope unclear
4. Pattern comparison table values missing

All concerns addressed by knockoff-architect. Plan now includes detailed specifications for decision tree, complete example, and pattern comparison table.

### 2026-02-03: Issue discovered

While investigating open generic overload API, discovered that Open Generic Interface and Open Generic Class behave differently and should be documented as separate patterns. The class variant uses `.Object` and has the numbered interceptor issue for overloads.

### 2026-02-04: Implementation complete

All 7 phases of the implementation contract completed successfully:

1. **Phase 1:** Updated `AllPatterns.cs` - header, Pattern 6 renamed, Pattern 7 added
2. **Phase 2:** Updated `CLAUDE.md` and `CLAUDE-DESIGN.md` to seven patterns
3. **Phase 3:** Updated `docs/guides/stub-patterns.md` and `docs/getting-started.md`
4. **Phase 4:** Updated `skills/knockoff/skills/knockoff-usage/references/patterns.md`
5. **Phase 5:** Updated README.md, troubleshoot.md, api-reference.md in skills/knockoff/
6. **Phase 6:** Updated `PatternsSamples.cs` - method renamed, pattern 7 example added
7. **Phase 7:** Updated agent files and verified no "six patterns" in active docs

All tests pass (406 tests). Build succeeded with 0 errors, 0 warnings.

---

## Results / Conclusions

**Implementation successful.** All documentation now accurately describes the 7 KnockOff patterns:

1. Standalone
2. Generic Standalone
3. Inline Interface
4. Inline Class
5. Inline Delegate
6. Open Generic Interface
7. Open Generic Class

Key distinction documented:
- Open Generic Interface: stub IS the implementation (no `.Object`)
- Open Generic Class: uses `.Object` property for class instance

Files updated: 12 documentation and code files across Design, CLAUDE docs, user docs, skills, and agent files.
