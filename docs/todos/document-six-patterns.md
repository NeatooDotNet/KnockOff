# Document All Six KnockOff Patterns

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-02-03
**Last Updated:** 2026-02-03

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

- [ ] Audit current pattern documentation across all files
- [ ] Update CLAUDE.md pattern count and descriptions
- [ ] Update/create stub-patterns documentation
- [ ] Update skills/knockoff pattern references
- [ ] Verify Design source of truth is consistent
- [ ] Add examples for Generic Standalone and Open Generic patterns

---

## Progress Log

---

## Results / Conclusions
