# Seven Patterns Documentation Plan

**Date:** 2026-02-04
**Related Todo:** [Document All Seven KnockOff Patterns](../todos/document-seven-patterns.md)
**Status:** Complete
**Last Updated:** 2026-02-04

---

## Overview

Update all documentation to accurately describe 7 patterns instead of 6. The "Open Generic" pattern is actually two distinct patterns with different behaviors:
- **Open Generic Interface** - Stub IS the implementation (like other interface stubs)
- **Open Generic Class** - Uses `.Object` property (like other class stubs)

---

## The Seven Patterns

| # | Pattern | Syntax | Instantiation |
|---|---------|--------|---------------|
| 1 | Standalone | `[KnockOff] partial class Stub : IService` | `new Stub()` |
| 2 | Generic Standalone | `[KnockOff] partial class Stub<T> : IService<T>` | `new Stub<T>()` |
| 3 | Inline Interface | `[KnockOff<IService>]` | `new Stubs.IService()` |
| 4 | Inline Class | `[KnockOff<ConcreteClass>]` | `new Stubs.ConcreteClass().Object` |
| 5 | Inline Delegate | `[KnockOff<DelegateType>]` | `new Stubs.DelegateType()` |
| 6 | Open Generic Interface | `[KnockOff(typeof(IService<>))]` | `new Stubs.IService<T>()` |
| 7 | Open Generic Class | `[KnockOff(typeof(ServiceBase<>))]` | `new Stubs.ServiceBase<T>().Object` |

---

## Key Behavioral Differences

### Open Generic Interface (Pattern 6)
- Uses `[KnockOff(typeof(IInterface<>))]` syntax
- Generated stub implements the interface directly
- Stub IS the implementation (no `.Object` property needed)
- Consistent with Inline Interface pattern behavior

### Open Generic Class (Pattern 7)
- Uses `[KnockOff(typeof(ConcreteClass<>))]` syntax
- Generated stub wraps a class that extends the base class
- Uses `.Object` property to access the actual instance
- Consistent with Inline Class pattern behavior
- Has numbered interceptor issue for overloads (see `class-stub-overload-consistency.md`)

---

## Approach

Update documentation in a specific order to maintain consistency:

1. **Source of Truth First** - Update Design projects
2. **Claude Documentation** - Update CLAUDE.md and CLAUDE-DESIGN.md
3. **User Documentation** - Update docs/guides/
4. **Skills** - Update skills/knockoff/
5. **Agents** - Update agent verification checklists

---

## Files to Update

### Phase 1: Design Source of Truth

| File | Change |
|------|--------|
| `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` | Split Pattern 5/6 into 6 (interface) and 7 (class), add Open Generic Class example |

### Phase 2: Claude Documentation

| File | Change |
|------|--------|
| `CLAUDE.md` | Update "Six Patterns" section to "Seven Patterns" with new table |
| `src/Design/CLAUDE-DESIGN.md` | Update Quick Reference table to show 7 patterns |

### Phase 3: User Documentation

| File | Change |
|------|--------|
| `docs/guides/stub-patterns.md` | Major update - split Open Generic section into Interface and Class variants |

### Phase 4: Skills

| File | Change |
|------|--------|
| `skills/knockoff/skills/knockoff-usage/references/patterns.md` | Update pattern list and comparison tables |
| `skills/knockoff/README.md` | Update "six patterns" to "seven patterns" (lines 13, 46, 128), update Quick API Reference table |
| `skills/knockoff/commands/troubleshoot.md` | Update "all six patterns" reference (line 127) |
| `skills/knockoff/skills/knockoff-usage/references/api-reference.md` | Update "all six KnockOff patterns" reference (line 45) |

### Phase 5: User Documentation (Additional)

| File | Change |
|------|--------|
| `docs/getting-started.md` | Update "six stub patterns" link text (line 254) |

### Phase 6: Test Samples

| File | Change |
|------|--------|
| `src/Tests/KnockOff.Documentation.Samples/PatternsSamples.cs` | Update "All Six Patterns" references (lines 256, 306), rename method `AllSixPatterns_WorkTogether()` to `AllSevenPatterns_WorkTogether()`, add Open Generic Class example to complete example |

### Phase 7: Agent Files

| File | Change |
|------|--------|
| `.claude/agents/knockoff-architect.md` | Update pattern verification checklist |
| `.claude/agents/knockoff-developer.md` | Update pattern verification checklist |

---

## Implementation Steps

### Step 1: Update AllPatterns.cs
- [ ] Add Open Generic Class example using `ServiceBase<T>`
- [ ] Update header comment from "Six Patterns" to "Seven Patterns"
- [ ] Renumber Pattern 5 (Open Generic) to Pattern 6 (Open Generic Interface)
- [ ] Add Pattern 7 (Open Generic Class) section
- [ ] Update DESIGN DECISION SUMMARY section
- [ ] Add corresponding test if needed

### Step 2: Update CLAUDE.md
- [ ] Change "### Six Patterns" to "### Seven Patterns"
- [ ] Split Pattern 6 into two rows in the table
- [ ] Update scope checklist to say "all 7" instead of "all 6"

### Step 3: Update CLAUDE-DESIGN.md
- [ ] Update Quick Reference table to show both Open Generic patterns

### Step 4: Update docs/guides/stub-patterns.md
- [ ] Update intro paragraph ("six distinct patterns" → "seven distinct patterns")
- [ ] Update Pattern Relationships diagram (see Decision Tree Specification below)
- [ ] Add row to Quick Decision Guide table: "Test-local stub for generic class" → "Open Generic Class"
- [ ] Split "Open Generic Pattern" section into two sections (Open Generic Interface, Open Generic Class)
- [ ] Update Pattern Comparison table (see Pattern Comparison Table Specification below)
- [ ] Update decision tree to include both Open Generic variants (see Decision Tree Specification below)
- [ ] Update Complete Example to show pattern 7 (see Complete Example Specification below)
- [ ] Update "all six patterns" → "all seven patterns"

### Step 4a: Update docs/getting-started.md
- [ ] Update line 254: "six stub patterns" → "seven stub patterns"

### Step 4b: Update src/Tests/KnockOff.Documentation.Samples/PatternsSamples.cs
- [ ] Update line 256: "All Six Patterns Together" → "All Seven Patterns Together"
- [ ] Rename method `AllSixPatterns_WorkTogether()` to `AllSevenPatterns_WorkTogether()` (line 306)
- [ ] Add pattern 7 example to the complete example (see Complete Example Specification below)

### Step 5: Update skills/knockoff/.../patterns.md
- [ ] Update intro text ("six distinct patterns" → "seven distinct patterns")
- [ ] Update Pattern Relationships diagram (see Decision Tree Specification below)
- [ ] Update Quick Decision Guide table: Add "Test-local stub for generic class" → "Open Generic Class"
- [ ] Split Open Generic section into two sections
- [ ] Update Pattern Comparison table (see Pattern Comparison Table Specification below)
- [ ] Update decision tree (see Decision Tree Specification below)
- [ ] Update Complete Example (see Complete Example Specification below)

### Step 5a: Update skills/knockoff/README.md
- [ ] Line 13: "six stub patterns" → "seven stub patterns"
- [ ] Line 46: Update "Six Patterns" table header
- [ ] Line 55: Add row for Open Generic Class pattern
- [ ] Line 128: "All six patterns" → "All seven patterns"

### Step 5b: Update skills/knockoff/commands/troubleshoot.md
- [ ] Line 127: "all six patterns" → "all seven patterns"

### Step 5c: Update skills/knockoff/skills/knockoff-usage/references/api-reference.md
- [ ] Line 45: "all six KnockOff patterns" → "all seven KnockOff patterns"

### Step 6: Update agent files
- [ ] Update knockoff-architect.md pattern checklist
- [ ] Update knockoff-developer.md pattern checklist

### Step 7: Consistency verification
- [ ] Search for "six patterns" and "6 patterns" across codebase
- [ ] Verify all counts are consistent

---

## Acceptance Criteria

- [ ] AllPatterns.cs documents 7 patterns with examples for both Open Generic variants
- [ ] CLAUDE.md shows 7 patterns in the table
- [ ] CLAUDE-DESIGN.md quick reference shows 7 patterns
- [ ] docs/guides/stub-patterns.md has separate sections for Open Generic Interface and Class
- [ ] skills/knockoff patterns reference shows 7 patterns
- [ ] Agent verification checklists reference 7 patterns
- [ ] No remaining references to "six patterns" or "6 patterns" (except in historical context)

---

## Detailed Specifications

### Decision Tree Specification

The current decision tree leads to a single "Open Generic pattern" node. It must be updated to distinguish between interface and class:

**Current (incorrect):**
```
|-- NO --> Is it a GENERIC interface/class?
    |
    |-- YES --> Do you need the stub in MULTIPLE test files?
    |   |
    |   |-- YES --> Generic Standalone pattern
    |   |           [KnockOff] class Stub<T> : IRepo<T>
    |   |
    |   |-- NO --> Open Generic pattern
    |              [KnockOff(typeof(IRepo<>))]
```

**Updated (correct):**
```
|-- NO --> Is it a GENERIC interface/class?
    |
    |-- YES --> Do you need the stub in MULTIPLE test files?
    |   |
    |   |-- YES --> Generic Standalone pattern
    |   |           [KnockOff] class Stub<T> : IRepo<T>
    |   |
    |   |-- NO --> Is it a CLASS (not interface)?
    |       |
    |       |-- YES --> Open Generic Class pattern
    |       |           [KnockOff(typeof(ServiceBase<>))]
    |       |           Use: new Stubs.ServiceBase<T>().Object
    |       |
    |       |-- NO --> Open Generic Interface pattern
    |                  [KnockOff(typeof(IRepo<>))]
    |                  Use: new Stubs.IRepo<T>()
```

**Key Logic:**
1. First check: Is it a delegate? (YES → Inline Delegate)
2. Second check: Is it generic? (NO → proceed to non-generic checks)
3. If generic: Do you need it in multiple test files? (YES → Generic Standalone)
4. If generic and test-local: **NEW BRANCH** - Is it a class?
   - YES → Open Generic Class (uses `.Object`)
   - NO → Open Generic Interface (stub IS implementation)

### Pattern Comparison Table Specification

Add a new column for Open Generic Class (pattern 7). The table currently has 6 columns.

**New row values for Open Generic Class:**

| Feature | Open Generic Class |
|---------|-------------------|
| **Reusable across test files** | No |
| **Custom user methods** | No |
| **Extra file required** | No |
| **Supports interfaces** | No |
| **Supports classes** | Yes |
| **Supports delegates** | No |
| **Supports generics** | Yes |
| **Instantiation syntax** | `new Stubs.Foo<T>().Object` |
| **Best for** | Local generic class stubs |

**Updated complete table:**

| Feature | Standalone | Generic Standalone | Inline Interface | Inline Class | Inline Delegate | Open Generic Interface | Open Generic Class |
|---------|------------|-------------------|------------------|--------------|-----------------|----------------------|-------------------|
| **Reusable across test files** | Yes | Yes | No | No | No | No | No |
| **Custom user methods** | Yes | Yes | No | No | No | No | No |
| **Extra file required** | Yes | Yes | No | No | No | No | No |
| **Supports interfaces** | Yes | Yes | Yes | No | No | Yes | No |
| **Supports classes** | No | No | No | Yes | No | No | Yes |
| **Supports delegates** | No | No | No | No | Yes | Yes* | No |
| **Supports generics** | No | Yes | Closed only | Closed only | Closed only | Yes | Yes |
| **Instantiation syntax** | `new MyStub()` | `new MyStub<T>()` | `new Stubs.IFoo()` | `new Stubs.Foo().Object` | `new Stubs.Del()` | `new Stubs.IFoo<T>()` | `new Stubs.Foo<T>().Object` |
| **Best for** | Shared stubs | Shared generic stubs | Local stubs | Class stubs | Delegate stubs | Local generic interface stubs | Local generic class stubs |

*Note: Open Generic Delegate (`[KnockOff(typeof(Factory<>))]`) behaves like Open Generic Interface (no `.Object`), as delegates are reference types that can be directly assigned.

### Complete Example Specification

The Complete Example must add a 7th code example demonstrating Open Generic Class.

**Current example has:**
1. Standalone
2. Generic Standalone
3. Inline Interface
4. Inline Class
5. Inline Delegate
6. Open Generic (Interface)

**Add pattern 7:**

In `PatternsSamples.cs`, add to the setup:
```cs
// Open Generic abstract class for pattern 7
public abstract class ServiceBase<T>
{
    public abstract T? GetItem(int id);
    public abstract void Process(T item);
}

// Separate host for open generic class stub
[KnockOff(typeof(ServiceBase<>))]
public partial class CompleteExampleOpenGenericClassHost { }
```

In the test method, add after pattern 6:
```cs
// 7. Open Generic Class: inline stub with type args, uses .Object
var serviceStub = new CompleteExampleOpenGenericClassHost.Stubs.ServiceBase<Order>();
serviceStub.GetItem.OnCall((id) => new Order { Id = id }).Verifiable();
ServiceBase<Order> service = serviceStub.Object;  // Note: .Object required for class stub
```

**In documentation (stub-patterns.md and patterns.md), update the complete example section:**
```cs
// 1. Standalone: direct instantiation
// ... (existing)

// 2-6. (existing patterns)

// 7. Open Generic Class: inline stub with type args, uses .Object
var serviceStub = new Stubs.ServiceBase<Order>();
serviceStub.GetItem.OnCall((id) => new Order { Id = id }).Verifiable();
ServiceBase<Order> service = serviceStub.Object;  // .Object required for class patterns
```

---

## Dependencies

- Need to verify Open Generic Class behavior works as described (may need to check generator code)
- Related todo: `class-stub-overload-consistency.md` (documents numbered interceptor issue)

---

## Risks / Considerations

- **Breaking documentation links**: Update carefully to avoid broken internal links
- **Skill distribution**: Skills are distributed to other projects - ensure patterns.md is standalone
- **Test coverage**: May need to add tests for Open Generic Class pattern if not already covered

---

## Architectural Verification

**Completed By:** knockoff-architect
**Date:** 2026-02-04

### Codebase Investigation

**Files Examined:**

| File | Findings |
|------|----------|
| `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` | Currently documents 6 patterns with Open Generic as a single pattern (Pattern 5). Does not show Open Generic Class example. |
| `src/Design/Design.Tests/GenericOverloadTests/OpenGenericOverloadTests.cs` | Confirms both patterns exist: `[KnockOff(typeof(IGenericFormatter<>))]` (interface) and `[KnockOff(typeof(RepositoryBase<>))]` (class). Tests clearly show `.Object` usage for class pattern. |
| `src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs` | Has tests for both Open Generic Interface (`IOGRepository<>`) and Open Generic Class (`OGRepository<>`, `OGCache<,>`). Class tests use `stub.Object` to get instance. |
| `src/Generator/Builder/InlineModelBuilder.cs` | Builds interface stubs - the generated stub implements the interface directly. |
| `src/Generator/Builder/ClassModelBuilder.cs` | Builds class stubs - generates a wrapper class with `.Object` property returning the actual class instance. |
| `CLAUDE.md` | Documents "Six Patterns" with Open Generic as pattern 6. |
| `src/Design/CLAUDE-DESIGN.md` | Shows 6 patterns in Quick Reference table. |
| `docs/guides/stub-patterns.md` | Full user documentation showing 6 patterns. Open Generic section does not distinguish interface vs. class. |
| `skills/knockoff/skills/knockoff-usage/references/patterns.md` | Skill reference with 6 patterns. |
| `.claude/agents/knockoff-architect.md` | Verification checklist mentions "all six patterns" |
| `.claude/agents/knockoff-developer.md` | Review checklist mentions "all six patterns" |

### Seven Patterns Verification

| # | Pattern | Verified | Behavior | Instantiation |
|---|---------|----------|----------|---------------|
| 1 | Standalone | Yes | Stub IS interface implementation | `new Stub()` |
| 2 | Generic Standalone | Yes | Stub IS interface implementation | `new Stub<T>()` |
| 3 | Inline Interface | Yes | Stub IS interface implementation | `new Stubs.IFoo()` |
| 4 | Inline Class | Yes | Uses `.Object` for class instance | `new Stubs.Foo().Object` |
| 5 | Inline Delegate | Yes | Implicit conversion to delegate | `new Stubs.Del()` |
| 6 | Open Generic Interface | Yes | Stub IS interface implementation | `new Stubs.IFoo<T>()` |
| 7 | Open Generic Class | Yes | Uses `.Object` for class instance | `new Stubs.Foo<T>().Object` |

**Key Behavioral Differences Confirmed:**

1. **Interface patterns (1, 2, 3, 6)**: The generated stub class directly implements the interface. The stub IS the implementation.

2. **Class patterns (4, 7)**: The generated stub is a wrapper. An internal `_Generated` class extends the base class and delegates to interceptors. Access via `.Object` property.

3. **Delegate pattern (5)**: Uses implicit conversion operator. Unique behavior.

### Breaking Changes Assessment

**None.** This is documentation-only. No code changes, no API changes, no generated code changes.

### Pattern Consistency Check

The proposed seven-pattern documentation is consistent with existing patterns:
- Interface patterns (1, 2, 3, 6) all behave the same way - stub IS implementation
- Class patterns (4, 7) all behave the same way - use `.Object`
- The split of Open Generic into Interface and Class variants follows the same distinction as Inline Interface vs. Inline Class

### Edge Cases Documented

1. **Open Generic Class with overloads**: Uses numbered interceptors for overloads (documented in `class-stub-overload-consistency.md` todo)
2. **Generic constraints**: Both Open Generic Interface and Class preserve type constraints from the original type

### Test Strategy

This is documentation-only. No new tests needed. Existing tests in:
- `OpenGenericInlineStubTests.cs` - covers both interface and class variants
- `OpenGenericOverloadTests.cs` - covers overload handling for both

### Files Requiring Update

| File | Update Type | Priority |
|------|-------------|----------|
| `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` | Add Open Generic Class example, update comments | High |
| `CLAUDE.md` | Update "Six Patterns" to "Seven Patterns" | High |
| `src/Design/CLAUDE-DESIGN.md` | Update Quick Reference table | High |
| `docs/guides/stub-patterns.md` | Split Open Generic section, update all counts | Medium |
| `skills/knockoff/.../patterns.md` | Split Open Generic section, update all counts | Medium |
| `.claude/agents/knockoff-architect.md` | Update verification checklist | Low |
| `.claude/agents/knockoff-developer.md` | Update review checklist | Low |

### Additional Files Discovered

**Search for "six patterns" and "6 patterns" (Developer Review findings):**

Additional files found during developer review that require updates:
- `docs/getting-started.md` (line 254) - "six stub patterns" link text
- `skills/knockoff/README.md` (lines 13, 46, 128) - "six patterns" references and Quick API table
- `skills/knockoff/commands/troubleshoot.md` (line 127) - "all six patterns" reference
- `skills/knockoff/skills/knockoff-usage/references/api-reference.md` (line 45) - "all six KnockOff patterns"
- `src/Tests/KnockOff.Documentation.Samples/PatternsSamples.cs` (lines 256, 306) - "All Six Patterns" and method name

These are now included in the Files to Update table (Phases 4-6).

### Concerns / Questions

1. **Delegate patterns**: The plan correctly shows Inline Delegate as pattern 5, but does not mention that Open Generic also works with delegates (`[KnockOff(typeof(Factory<>))]`). Should this be documented as a sub-case of Open Generic, or is the current treatment sufficient?

   **Resolution:** Open Generic Delegate is a valid combination (see `DelegatesSamples.cs` lines 116-117), but it behaves like Open Generic Interface (no `.Object`). The distinction between interface/class applies to non-delegate types. The plan is correct as-is.

2. **Pattern numbering in AllPatterns.cs**: The file uses "1B" for Generic Standalone internally but user docs use sequential 1-6. Plan proposes 1-7 sequential in user docs. This is consistent.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-04

### Understanding of This Plan

**Core Change:** Update all documentation to describe 7 patterns instead of 6, splitting "Open Generic" into "Open Generic Interface" (pattern 6) and "Open Generic Class" (pattern 7).

**User-Facing API:** No API changes - documentation only. Users will see clearer documentation distinguishing Open Generic Interface (no `.Object` needed) from Open Generic Class (requires `.Object`).

**Internal Changes:** Documentation file updates only - no code changes.

### Codebase Investigation

**Files Examined:**

| File | Verification |
|------|-------------|
| `src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs` | Confirmed behavioral difference: Interface tests use `IOGRepository<string> repo = stub;` (no `.Object`), Class tests use `OGRepository<User> repo = stub.Object;` |
| `src/Design/Design.Tests/GenericOverloadTests/OpenGenericOverloadTests.cs` | Confirmed: Interface `IGenericFormatter<string> formatter = stub;`, Class `RepositoryBase<TestEntity> repo = stub.Object;` |
| `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` | Currently documents "All Six" with Open Generic as pattern 5 |
| `docs/guides/stub-patterns.md` | Current state has 6 patterns, Open Generic section does not distinguish interface vs class |
| All 5 additional files | Verified existence and line numbers match plan |

### Concern Verification

**Concern 1: Missing Files** - ADEQUATELY ADDRESSED
- All 5 files verified to exist with "six patterns" at stated line numbers

**Concern 2: Decision Tree** - ADEQUATELY ADDRESSED
- Specification shows before/after states with clear logic and instantiation syntax

**Concern 3: Complete Example** - ADEQUATELY ADDRESSED
- Specification includes compilable code for `ServiceBase<T>` and test method

**Concern 4: Comparison Table** - ADEQUATELY ADDRESSED
- All 9 rows with all 7 column values provided, including Open Generic Delegate footnote

### Why This Plan Is Approved

1. All four concerns addressed with detailed specifications (not just acknowledged)
2. Behavioral difference verified in actual test code
3. Every file requiring changes listed with specific line numbers
4. Documentation-only changes eliminate implementation risk
5. Decision tree, comparison table, and complete example are fully specified

---

## Implementation Contract

**Created:** 2026-02-04
**Approved by:** knockoff-developer

### In Scope

**Phase 1: Design Source of Truth**
- [ ] `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` - Update header comment, add Open Generic Class example, update DESIGN DECISION SUMMARY section

**Phase 2: Claude Documentation**
- [ ] `CLAUDE.md` - Change "Six Patterns" to "Seven Patterns", update table, update scope checklist
- [ ] `src/Design/CLAUDE-DESIGN.md` - Update Quick Reference table

**Phase 3: User Documentation**
- [ ] `docs/guides/stub-patterns.md` - Major update: intro, diagram, Quick Decision Guide table, split Open Generic section, Pattern Comparison table, decision tree, Complete Example
- [ ] `docs/getting-started.md` - Line 254: "six" to "seven"

**Phase 4: Test Samples**
- [ ] `src/Tests/KnockOff.Documentation.Samples/PatternsSamples.cs` - Update line 256 comment, rename method line 306, add pattern 7 example
- [ ] **Checkpoint: Run tests to verify PatternsSamples compiles and passes**

**Phase 5: Skills**
- [ ] `skills/knockoff/skills/knockoff-usage/references/patterns.md` - Mirror changes from stub-patterns.md
- [ ] `skills/knockoff/README.md` - Lines 13, 46, 55 (add row), 128
- [ ] `skills/knockoff/commands/troubleshoot.md` - Line 127
- [ ] `skills/knockoff/skills/knockoff-usage/references/api-reference.md` - Line 45

**Phase 6: Agent Files**
- [ ] `.claude/agents/knockoff-architect.md` - Update pattern verification checklist
- [ ] `.claude/agents/knockoff-developer.md` - Update pattern verification checklist

**Phase 7: Final Verification**
- [ ] Search for "six patterns" and "6 patterns" across codebase
- [ ] Verify all counts are consistent
- [ ] **Checkpoint: Run full test suite**

### Explicitly Out of Scope

- Code changes - documentation only, no generator or runtime changes
- New tests - existing tests already cover both Open Generic patterns
- API changes - no behavioral changes
- Agent file content beyond pattern count - only updating pattern list/checklist

### Verification Gates

1. After Phase 4: PatternsSamples.cs compiles and `AllSevenPatterns_WorkTogether()` test passes
2. After Phase 7: Search for "six patterns" returns only historical references (if any)
3. Final: All tests pass, no grep results for "six patterns" in active documentation

### Stop Conditions

If any of these occur, STOP and report:
- Any test fails that was passing before changes
- Generated code from snippets does not compile
- Unexpected "six patterns" references found in files not listed in plan
- Any file listed in plan does not exist

---

## Implementation Progress

**Started:** 2026-02-04
**Completed:** 2026-02-04
**Developer:** knockoff-developer

### Phase 1: Design Source of Truth - COMPLETE
- [x] `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` - Updated header comment from "Six" to "Seven", renamed Pattern 6 to "Open Generic Interface", added Pattern 7 "Open Generic Class" with full documentation and example, updated DESIGN DECISION SUMMARY

### Phase 2: Claude Documentation - COMPLETE
- [x] `CLAUDE.md` - Changed "Six Patterns" to "Seven Patterns", split Open Generic into Interface (6) and Class (7), updated scope checklist to "all 7"
- [x] `src/Design/CLAUDE-DESIGN.md` - Updated Quick Reference table with 7 patterns

### Phase 3: User Documentation - COMPLETE
- [x] `docs/guides/stub-patterns.md` - Updated intro, Pattern Relationships diagram, Quick Decision Guide, added Open Generic Class section, updated Pattern Comparison table, updated decision tree, updated Complete Example, updated UPDATED date
- [x] `docs/getting-started.md` - Updated "six stub patterns" to "seven stub patterns"

### Phase 4: Skills Core - COMPLETE
- [x] `skills/knockoff/skills/knockoff-usage/references/patterns.md` - Full update mirroring stub-patterns.md changes
- [x] **Verification Gate:** `dotnet build` - Build succeeded (0 errors)

### Phase 5: Skills Additional - COMPLETE
- [x] `skills/knockoff/README.md` - Updated "six patterns" to "seven patterns", updated Quick API Reference table
- [x] `skills/knockoff/commands/troubleshoot.md` - Updated "all six patterns" to "all seven patterns"
- [x] `skills/knockoff/skills/knockoff-usage/references/api-reference.md` - Updated "all six KnockOff patterns" to "all seven KnockOff patterns"

### Phase 6: Test Samples - COMPLETE
- [x] `src/Tests/KnockOff.Documentation.Samples/PatternsSamples.cs` - Updated "All Six Patterns" to "All Seven Patterns", renamed method to `AllSevenPatterns_WorkTogether()`, added Open Generic Class example with `ServiceBase<T>` and `CompleteExampleOpenGenericClassHost`

### Phase 7: Agent Files and Final Verification - COMPLETE
- [x] `.claude/agents/knockoff-architect.md` - Updated all pattern references from 6 to 7, expanded pattern lists
- [x] `.claude/agents/knockoff-developer.md` - Updated all pattern references from 6 to 7, expanded pattern lists
- [x] **Verification Gate:** Grep for "six patterns" - Only historical references remain in docs/plans/ and docs/todos/

---

## Completion Evidence

**Completed:** 2026-02-04

### Test Results

```
Passed!  - Failed:     0, Passed:   406, Skipped:     0, Total:   406, Duration: 211 ms - KnockOff.Documentation.Samples.dll (net9.0)
```

### Build Results

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Grep Verification

Remaining "six patterns" references are ONLY in:
- `docs/plans/` - Historical plan documents
- `docs/todos/` - Historical todo documents
- `docs/plans/completed/` and `docs/todos/completed/` - Archived completed work

All active documentation has been updated to seven patterns.

### Files Modified

| File | Change |
|------|--------|
| `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` | Header, Open Generic Interface section, added Open Generic Class section |
| `CLAUDE.md` | Seven Patterns section, scope checklist |
| `src/Design/CLAUDE-DESIGN.md` | Quick Reference table |
| `docs/guides/stub-patterns.md` | Full update (intro, diagrams, tables, sections, example) |
| `docs/getting-started.md` | Link text |
| `src/Tests/KnockOff.Documentation.Samples/PatternsSamples.cs` | Method name, example, test assertions |
| `skills/knockoff/skills/knockoff-usage/references/patterns.md` | Full update matching stub-patterns.md |
| `skills/knockoff/README.md` | Pattern count, table |
| `skills/knockoff/commands/troubleshoot.md` | Pattern count |
| `skills/knockoff/skills/knockoff-usage/references/api-reference.md` | Pattern count |
| `.claude/agents/knockoff-architect.md` | Pattern count, checklists |
| `.claude/agents/knockoff-developer.md` | Pattern count, checklist |

### Status Update

- Plan status: Complete
- Todo status: Complete
