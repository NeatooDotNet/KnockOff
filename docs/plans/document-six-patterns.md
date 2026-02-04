# Document All Six KnockOff Patterns

**Date:** 2026-02-03
**Related Todo:** [Document All Six KnockOff Patterns](../todos/document-six-patterns.md)
**Status:** Under Review (Developer)
**Last Updated:** 2026-02-03

---

## Overview

Update all documentation to accurately reflect the six distinct stub patterns KnockOff supports. This is a documentation-only change with no code modifications required.

---

## Canonical Pattern Definitions

**MUST be used identically across all documentation tiers.**

1. **Standalone** - `[KnockOff] partial class Stub : IService` - Dedicated stub class implementing interface
2. **Generic Standalone** - `[KnockOff] partial class Stub<T> : IService<T>` - Generic stub class with type parameters
3. **Inline Interface** - `[KnockOff<IService>]` - Nested stub for closed generic interface
4. **Inline Class** - `[KnockOff<ConcreteClass>]` - Nested stub for class with virtual members
5. **Inline Delegate** - `[KnockOff<DelegateType>]` - Nested stub for delegate types
6. **Open Generic** - `[KnockOff(typeof(T<>))]` - Nested generic stub from open generic type

### Pattern Numbering Clarification

**Current state in AllPatterns.cs:** Open Generic is labeled "PATTERN 5"

**Target state after this work:**
- Pattern 1: Standalone
- Pattern 2: Generic Standalone (NEW - inserted as "PATTERN 1B")
- Pattern 3: Inline Interface
- Pattern 4: Inline Class
- Pattern 5: Inline Delegate
- Pattern 6: Open Generic (renumbered from current PATTERN 5)

**Note:** AllPatterns.cs will label Generic Standalone as "PATTERN 1B" to minimize disruption to existing pattern numbers in comments throughout the codebase. Documentation tiers will use sequential numbering 1-6.

---

## The Six Patterns

| # | Pattern Name | Attribute Syntax | Description |
|---|--------------|------------------|-------------|
| 1 | **Standalone** | `[KnockOff] partial class Stub : IService` | Dedicated stub class implementing interface |
| 2 | **Generic Standalone** | `[KnockOff] partial class Stub<T> : IService<T>` | Generic stub class with type parameters |
| 3 | **Inline Interface** | `[KnockOff<IService>]` | Nested stub for closed generic interface |
| 4 | **Inline Class** | `[KnockOff<ConcreteClass>]` | Nested stub for class with virtual members |
| 5 | **Inline Delegate** | `[KnockOff<DelegateType>]` | Nested stub for delegate types |
| 6 | **Open Generic** | `[KnockOff(typeof(T<>))]` | Nested generic stub from open generic type |

### Pattern Relationships

```
Standalone Patterns (file-based, reusable)
├── 1. Standalone         - [KnockOff] class Stub : IFoo
└── 2. Generic Standalone - [KnockOff] class Stub<T> : IFoo<T>

Inline Patterns (nested within test class)
├── 3. Inline Interface   - [KnockOff<IFoo>]
├── 4. Inline Class       - [KnockOff<SomeClass>]
├── 5. Inline Delegate    - [KnockOff<SomeDelegate>]
└── 6. Open Generic       - [KnockOff(typeof(IFoo<>))]
```

---

## Documentation Architecture

### Five-Tier Hierarchy

| Tier | Purpose | Audience | Content Level |
|------|---------|----------|---------------|
| **Tier 1: Source of Truth** | Exhaustive implementation details | Deep divers, maintainers | Generated code, design rationale, edge cases, rejected alternatives |
| **Tier 2: Agent Quick Ref** | Fast lookup for Claude agents | Claude agents (all roles) | Tables only - name, syntax, one example. NO prose. |
| **Tier 3: User Guide** | Help developers choose and use patterns | KnockOff users | Tutorial - when to use, how to use, benefits, trade-offs |
| **Tier 4: Skill Reference** | Standalone reference for external projects | Claude in external repos | Self-contained mirror of Tier 3 (no KnockOff repo references) |
| **Tier 5: Agent Checklists** | Verification that features work across patterns | Architect/Developer agents | Pattern enumeration for checklists only |

### Key Principle

**Detail flows downward from Tier 1.** Each lower tier simplifies and refocuses for its audience. No tier duplicates exhaustive details from Tier 1.

### Content Contract by Tier

| Content Type | Tier 1 | Tier 2 | Tier 3 | Tier 4 | Tier 5 |
|--------------|--------|--------|--------|--------|--------|
| Pattern Name | ✅ | ✅ | ✅ | ✅ | ✅ Checklist |
| Attribute Syntax | ✅ Full | ✅ Minimal | ✅ Full | ✅ Full | ❌ |
| When to Use | ✅ Detailed | ❌ | ✅ Practical | ✅ Practical | ❌ |
| Code Example | ✅ Exhaustive | ✅ One-liner | ✅ Tutorial | ✅ Tutorial | ❌ |
| Benefits/Trade-offs | ✅ Complete | ❌ | ✅ User-focused | ✅ User-focused | ❌ |
| Generated Code Details | ✅ Full | ❌ | ❌ | ❌ | ❌ |
| Design Rationale | ✅ Extensive | ❌ | ❌ | ❌ | ❌ |

### Code Example Approach by Tier

| Tier | Purpose | Example Style |
|------|---------|---------------|
| **Tier 1** | Exhaustive implementation details | Complete generated code, edge cases, all overloads, diagnostic examples |
| **Tier 3** | User guide tutorial | Realistic domain examples (Repository, Service), simplified to show concepts |
| **Tier 4** | Self-contained skill reference | Same as Tier 3, but all examples inline (no external snippet references) |

**Self-Contained Definition (Tier 4):**
- No references to Design/ projects
- No references to KnockOff repo-specific paths
- All code examples inline (no external snippet references that won't exist in external projects)
- Breadcrumb links are OK (they're relative within skill folder)

### Decision Tree Logic

```
Is it a generic interface/class?
├─ NO → Follow original 4-pattern decision tree
└─ YES → Do you need a reusable stub file?
    ├─ YES → Generic Standalone (Pattern 2)
    │         [KnockOff] partial class Stub<T> : IService<T>
    │         Reusable: new Stub<User>(), new Stub<Product>()
    │
    └─ NO → Open Generic (Pattern 6)
              [KnockOff(typeof(IService<>))]
              One-time use: new Stubs.IService<User>()
```

---

## Files by Tier

### Tier 1: Source of Truth

| File | Update Type |
|------|-------------|
| `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` | **Major** - Add Generic Standalone with exhaustive documentation |

### Tier 2: Agent Quick Reference

| File | Update Type |
|------|-------------|
| `CLAUDE.md` | **Minor** - Update "Four Patterns" to "Six Patterns" enumeration (no prose) |
| `src/Design/CLAUDE-DESIGN.md` | **Minor** - Add 2 rows to quick reference table |

### Tier 3: User-Facing Guide

| File | Update Type |
|------|-------------|
| `docs/guides/stub-patterns.md` | **Major** - Restructure for 6 patterns with full tutorial sections |
| `docs/guides/delegates.md` | **Trivial** - Add cross-reference to Open Generic pattern |
| `docs/getting-started.md` | **Trivial** - Update "three stub patterns" to "six stub patterns" |

### Tier 4: Skill Documentation

| File | Update Type |
|------|-------------|
| `skills/knockoff/README.md` | **Minor** - Update pattern table to 6 rows |
| `skills/knockoff/skills/knockoff-usage/SKILL.md` | **Minor** - Update pattern selection table |
| `skills/knockoff/skills/knockoff-usage/references/patterns.md` | **Major** - Restructure (mirror Tier 3, self-contained) |
| `skills/knockoff/skills/knockoff-usage/references/api-reference.md` | **Trivial** - One sentence + link |
| `skills/knockoff/commands/create-stub.md` | **Trivial** - Link to patterns.md |
| `skills/knockoff/commands/troubleshoot.md` | **Trivial** - Update pattern count |

**Excluded:** `skills/knockoff/commands/migrate-from-moq.md` - Not relevant to pattern documentation.

### Tier 5: Agent Behavior Files

| File | Update Type |
|------|-------------|
| `.claude/agents/knockoff-architect.md` | **Minor** - Update verification checklist references |
| `.claude/agents/knockoff-developer.md` | **Minor** - Update verification checklist references |

---

## Implementation Phases

### Phase 1: Establish Source of Truth

Update `AllPatterns.cs` with exhaustive Generic Standalone documentation. This becomes the reference for all other updates.

**File:** `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs`

**Changes:**
1. Update header comment from "Four" to "Six" patterns
2. Add enumeration of all 6 patterns in header
3. Add PATTERN 1B: GENERIC STANDALONE section with:
   - When to use (detailed scenarios)
   - Design decisions
   - Generator behavior (complete generated code example)
   - Instantiation differences vs Open Generic
4. Update "DESIGN DECISION SUMMARY" to list all 6 patterns

**Generic Standalone Section Template:**

The new PATTERN 1B section should include these subsections:

```csharp
// PATTERN 1B: GENERIC STANDALONE STUB
// ====================================
//
// [Overview paragraph: What this pattern is]
//
// WHEN TO USE:
// - [Scenario 1: Reusable generic stub across multiple tests]
// - [Scenario 2: Shared setup for generic interfaces]
// - [Scenario 3: Type-parameterized test fixtures]
//
// SYNTAX:
// [Example showing attribute and class declaration with constraints]
//
// GENERATED CODE:
// [Full generated interceptor code showing type parameter handling]
//
// USAGE IN TESTS:
// [Multiple instantiation examples with different type arguments]
//
// VS OPEN GENERIC PATTERN:
// [Table comparing instantiation, reusability, and use cases]
//
// DESIGN RATIONALE:
// [Why we support this pattern, generator implementation notes]
```

### Phase 2: Update Agent Quick References

**File:** `CLAUDE.md`

Replace "### Four Patterns" section with:
```markdown
### Six Patterns

**Standalone Patterns** (file-based, reusable across tests):
1. **Standalone** - `[KnockOff]` on partial class implementing interface
2. **Generic Standalone** - `[KnockOff]` on generic partial class: `class Stub<T> : IService<T>`

**Inline Patterns** (nested within test class):
3. **Inline Interface** - `[KnockOff<IInterface>]` generates nested stub class
4. **Inline Class** - `[KnockOff<ConcreteClass>]` generates nested stub for virtual/abstract members
5. **Inline Delegate** - `[KnockOff<DelegateType>]` generates stub for delegate invocation
6. **Open Generic** - `[KnockOff(typeof(T<>))]` generates generic nested stub from open generic type
```

Update scope checklist to reference "all 6" patterns.

**File:** `src/Design/CLAUDE-DESIGN.md`

Add 2 rows to quick reference table:
```markdown
| Generic Standalone | `[KnockOff]` on generic class | `new RepositoryStub<T>()` |
| Open Generic | `[KnockOff(typeof(IFoo<>))]` | `new Stubs.IRepository<User>()` |
```

### Phase 3: Update User-Facing Guide

**File:** `docs/guides/stub-patterns.md`

Major restructure:
1. Update intro from "three fundamental patterns" to "six patterns"
2. Add pattern relationship diagram
3. For each pattern, include:
   - When to Use (3-5 bullets)
   - Basic Setup (code snippet)
   - Usage in Tests (code snippet)
   - Benefits (3-5 bullets)
   - Trade-offs (3-5 bullets)
4. Add Generic Standalone section after Standalone
5. Add Open Generic section after Inline Delegate
6. Update Quick Decision Guide table (6 rows)
7. Update Pattern Comparison table (6 rows)
8. Update Decision Tree/flowchart

**Tutorial Section Template (for each pattern):**

Use this structure for consistency across all 6 pattern sections:

```markdown
## [Pattern Name]

[One-sentence description]

### When to Use

- [Use case 1]
- [Use case 2]
- [Use case 3]

### Basic Setup

<!-- snippet: pattern-name-setup -->
<!-- endSnippet -->

[Brief explanation of setup]

### Usage in Tests

<!-- snippet: pattern-name-usage -->
<!-- endSnippet -->

[Brief explanation of usage]

### Benefits

- [Benefit 1]
- [Benefit 2]
- [Benefit 3]

### Trade-offs

- [Trade-off 1]
- [Trade-off 2]
- [Trade-off 3]
```

**File:** `docs/guides/delegates.md`

Add NOTE box referencing Open Generic pattern in stub-patterns.md.

**File:** `docs/getting-started.md`

Update any references to "three stub patterns" to "six stub patterns" with link to full pattern guide.

### Phase 4: Update Skill Documentation

**File:** `skills/knockoff/README.md`

Update pattern table to 6 rows with same format as current 4.

**File:** `skills/knockoff/skills/knockoff-usage/SKILL.md`

Update "Pattern Selection" table to 6 rows.

**File:** `skills/knockoff/skills/knockoff-usage/references/patterns.md`

Major restructure mirroring Tier 3 (stub-patterns.md) but **self-contained**:
- No references to Design projects or KnockOff repo files
- Complete examples that work standalone
- Same structure: When to Use, Setup, Usage, Benefits, Trade-offs

**File:** `skills/knockoff/skills/knockoff-usage/references/api-reference.md`

Add one sentence: "The interceptor API works identically across all six KnockOff patterns (see [patterns.md](patterns.md) for details)."

**File:** `skills/knockoff/commands/create-stub.md`

Replace pattern enumeration with: "Supports all KnockOff patterns" + link to `references/patterns.md`.

**File:** `skills/knockoff/commands/troubleshoot.md`

Update pattern count references from "three" to "six".

### Phase 5: Update Agent Behavior Files

**File:** `.claude/agents/knockoff-architect.md`

- Update "Three-Pattern Verification" to "Pattern Verification"
- Change "all three stub patterns" to "all stub patterns"
- Update checklist item pattern count

**File:** `.claude/agents/knockoff-developer.md`

- Update "All three patterns addressed" references
- Change pattern count references throughout

---

## Acceptance Criteria

- [ ] Tier 1: AllPatterns.cs has exhaustive Generic Standalone documentation following template structure
- [ ] Tier 2: CLAUDE.md and CLAUDE-DESIGN.md have 6-pattern tables using canonical definitions
- [ ] Tier 3: stub-patterns.md has full tutorial for all 6 patterns following template structure
- [ ] Tier 3: docs/getting-started.md updated from "three" to "six" patterns
- [ ] Tier 4: skill patterns.md mirrors Tier 3 (self-contained - no Design/ references, all examples inline)
- [ ] Tier 5: Agent files reference "six patterns" in checklists
- [ ] No duplicate exhaustive content across tiers (detail flows from Tier 1)
- [ ] All cross-references work correctly
- [ ] Decision tree includes Generic Standalone vs Open Generic logic
- [ ] Pattern numbering consistent (1-6) across all documentation except AllPatterns.cs (which uses 1B)
- [ ] Existing tests still pass (GenericStandaloneStubTests.cs, OpenGenericInlineStubTests.cs)

---

## Evidence from Codebase

### Test Files Demonstrating Patterns

**Generic Standalone:**
- `src/Tests/KnockOffTests/GenericStandaloneStubTests.cs`
- `src/Tests/KnockOffTests/GenericStandaloneEdgeCaseTests.cs`

```csharp
[KnockOff]
public partial class GenericRepositoryStub<T> : IGenericRepository<T> where T : class { }

// Usage:
var userRepo = new GenericRepositoryStub<User>();
var entityRepo = new GenericRepositoryStub<TestEntity>();
```

**Open Generic:**
- `src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs`

```csharp
[KnockOff(typeof(IOGRepository<>))]
public partial class OpenGenericInterfaceTest { }

// Usage:
var stringStub = new OpenGenericInterfaceTest.Stubs.IOGRepository<string>();
var intStub = new OpenGenericInterfaceTest.Stubs.IOGRepository<int>();
```

---

## Architectural Verification

- [x] All six patterns identified and defined with canonical definitions
- [x] Documentation hierarchy established (5 tiers with clear content contracts)
- [x] Content ownership clear (what belongs where, no duplication)
- [x] Pattern numbering scheme clarified (1-6 in docs, 1B in AllPatterns.cs)
- [x] Self-contained definition specified for Tier 4
- [x] Code example approach defined (exhaustive vs tutorial vs inline)
- [x] Decision tree logic documented (Generic Standalone vs Open Generic)
- [x] Content templates provided (AllPatterns.cs section, tutorial section)
- [x] No code changes required
- [x] Existing test coverage confirms patterns work

---

## Developer Review

**Status:** Not Started

**Concerns:** [To be filled by developer]

---

## Implementation Contract

[To be filled after developer review]

---

## Implementation Progress

[To be filled during implementation]

---

## Completion Evidence

[To be filled upon completion]
