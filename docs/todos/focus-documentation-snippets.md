# Focus Documentation Snippets

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-02-02
**Last Updated:** 2026-02-02

---

## Problem

Documentation snippets currently include full test methods with ceremony (attributes, method signatures, setup boilerplate, assertions). This makes side-by-side comparisons verbose and harder to scan.

## Solution

Apply the "focused snippets" pattern established in the README's method overload section: strip each snippet to its essential 2-5 lines showing only the API usage and explanatory comment. No test ceremony, no setup, no assertions.

**Focused snippet characteristics:**
- Strip: `[Fact]`, method signatures, `var stub = new...`, `Assert.*`, closing braces
- Keep: The actual API call + explanatory comment above it
- One concept per snippet

---

## Plans

---

## Documents to Focus (21 files, ~270 snippets)

Work document-by-document, updating both the markdown file and corresponding C# sample file.

### High Priority (Migration & Core Guides)
- [ ] `docs/migration/from-nsubstitute.md` (36 snippets)
- [ ] `docs/guides/parameter-matching.md` (28 snippets)
- [ ] `docs/guides/methods.md` (24 snippets)
- [ ] `docs/guides/delegates.md` (23 snippets)
- [ ] `docs/guides/properties.md` (18 snippets)
- [ ] `docs/migration/from-moq.md` (17 snippets)

### Medium Priority (Feature Guides)
- [ ] `docs/guides/verification.md` (16 snippets)
- [ ] `docs/guides/indexers.md` (15 snippets)
- [ ] `docs/migration/property-value-removal.md` (12 snippets)
- [ ] `docs/getting-started.md` (10 snippets)
- [ ] `docs/guides/stub-patterns.md` (9 snippets)
- [ ] `docs/guides/async-patterns.md` (9 snippets)
- [ ] `docs/guides/generic-methods.md` (9 snippets)

### Lower Priority (Troubleshooting & Reference)
- [ ] `docs/troubleshooting.md` (8 snippets)
- [ ] `docs/guides/advanced-callbacks.md` (8 snippets)
- [ ] `docs/guides/events.md` (8 snippets)
- [ ] `docs/reference/interceptor-api.md` (7 snippets)
- [ ] `docs/reference/smart-defaults.md` (6 snippets)
- [ ] `docs/guides/user-methods.md` (6 snippets)
- [ ] `docs/guides/source-delegation.md` (6 snippets)
- [ ] `docs/reference/attribute-options.md` (4 snippets)

---

## Workflow Per Document

1. **Read the markdown file** - Identify all snippets and their purpose
2. **Locate sample source file** - Find the corresponding C# file with `#region` markers
3. **Analyze each snippet** - Determine which lines are essential
4. **Update sample file** - Split `#region` markers to isolate essential lines
5. **Update markdown** - Adjust prose context if needed, may need to split one snippet into multiple focused snippets
6. **Run mdsnippets** - Sync changes
7. **Build and test** - Verify samples still compile
8. **Mark document complete** - Check off in this todo

---

## Progress Log

---

## Results / Conclusions
