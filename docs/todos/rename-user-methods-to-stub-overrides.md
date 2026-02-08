# Rename "User Methods" to "Stub Overrides"

**Status:** In Progress
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

- [Rename "User Methods" to "Stub Overrides" - Implementation Plan](../plans/rename-user-methods-to-stub-overrides.md)

---

## Tasks

- [x] Create implementation plan mapping all rename locations
- [ ] Rename code identifiers in generator (models, builders, renderers)
- [ ] Rename code identifiers in library
- [ ] Rename Design project files and identifiers
- [ ] Rename test files and identifiers
- [ ] Rename documentation files and content
- [ ] Rename skill files and content
- [ ] Verify all builds pass
- [ ] Verify all tests pass

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

- [ ] Design project builds successfully
- [ ] Design project tests pass

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]

---

## Results / Conclusions

[What was learned? What decisions were made?]
