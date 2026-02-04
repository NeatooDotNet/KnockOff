# Document All Seven KnockOff Patterns

**Status:** Open
**Priority:** Medium
**Created:** 2026-02-03
**Last Updated:** 2026-02-03

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

---

## Tasks

- [ ] Update AllPatterns.cs to document 7 patterns (split Open Generic)
- [ ] Update CLAUDE.md pattern count and descriptions
- [ ] Update CLAUDE-DESIGN.md
- [ ] Update docs/guides/stub-patterns.md
- [ ] Update skills/knockoff pattern references
- [ ] Update agent verification checklists
- [ ] Verify all pattern counts are consistent (search for "six patterns", "6 patterns")

---

## Progress Log

### 2026-02-03: Issue discovered

While investigating open generic overload API, discovered that Open Generic Interface and Open Generic Class behave differently and should be documented as separate patterns. The class variant uses `.Object` and has the numbered interceptor issue for overloads.

---

## Results / Conclusions
