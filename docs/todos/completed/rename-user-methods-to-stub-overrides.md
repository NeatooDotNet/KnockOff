# Rename "User Methods" to "Stub Overrides"

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-08
**Last Updated:** 2026-02-08

---

## Problem

The term "user methods" is vague — every method a user writes is a "user method." It doesn't convey what the feature actually does: providing compiled-in default behavior via `protected override` methods with the `_` suffix on standalone stub classes.

## Solution

Rename "user methods" to "stub overrides" across the entire codebase. This is a mechanical rename with no behavioral changes. "Stub overrides" accurately communicates both concepts: they're on a **stub**, and they use C#'s `override` keyword.

The rename covers:
- Code identifiers (classes, properties, methods, variables)
- File names and folder names
- Documentation (guides, skills, release notes)
- Tests (class names, file names)
- Design projects (interfaces, stubs, test files)

---

## Plans

- [Rename "User Methods" to "Stub Overrides" - Implementation Plan](../plans/completed/rename-user-methods-to-stub-overrides.md)

---

## Tasks

- [x] Create implementation plan mapping all rename locations
- [x] Rename code identifiers in generator (models, builders, renderers)
- [x] Rename code identifiers in library
- [x] Rename Design project files and identifiers
- [x] Rename test files and identifiers
- [x] Rename documentation files and content
- [x] Rename skill files and content
- [x] Verify all builds pass
- [x] Verify all tests pass

---

## Progress Log

### 2026-02-08
- Brainstormed naming alternatives: "stubbed methods", "standalone mock methods", "default overrides", "stub overrides"
- Researched Moq/NSubstitute terminology for comparison
- Decided on "stub overrides" — clear, accurate, concise
- Created branch `stubOverride`
- Created this todo
- Created comprehensive implementation plan with 16 phases covering all rename locations
- Plan identifies 50+ files requiring changes across generator, design, tests, docs, and skills
- Open question raised: should "user properties" also be renamed in this effort?

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] Design project builds successfully
- [x] Design project tests pass

**Verification results:**
- Design build: 0 errors, 0 warnings
- Design tests: 1,068 passed, 0 failed

---

## Results / Conclusions

Renamed "user methods" to "stub overrides" and "user properties" to "stub override properties" across the entire codebase. 106 files changed (17 file/folder renames, 89 content modifications). All 6,336 tests passing. Zero grep matches for old terminology in active files. PR #65.
