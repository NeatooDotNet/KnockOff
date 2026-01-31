# Create Design Source of Truth Projects

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-30
**Last Updated:** 2026-01-30 (Developer review complete - approved)

---

## Problem

Design decisions are being lost, forgotten, or contradicted because there's no authoritative source of truth:

- **Codebase**: API is too hard to deduce from generator implementation (complex Roslyn code)
- **User documentation**: Always behind and structured for users, not AI comprehension
- **Sample projects**: User-focused, fall behind, not structured for API deduction
- **CLAUDE.md**: Causes confusion during design changes (AI reverts to "what was")
- **skills/knockoff/**: Skill documentation is for teaching usage, not understanding design

This leads to:
- Repeated proposals of previously-rejected designs
- Enhancements that miss critical existing functionality
- Losing track of why certain design decisions were made
- Inconsistent API evolution across patterns and member types

## Solution

Create a new `src/Design/` directory with actual C# projects specifically designed for Claude Code to understand the KnockOff API. These projects will:

1. **Be the authoritative design reference** - Updated first, everything else flows from it
2. **Include extensive comments** - Not just "what" but "what we didn't do and why"
3. **Cover the full public API** - All four patterns, all four member types
4. **Be fully functional** - Compiles and tests pass, ensuring accuracy
5. **Capture design evolution** - Commented-out code showing rejected approaches

### Key Characteristics

- Heavy comments including `// DID NOT DO THIS BECAUSE XYZ`
- Commented-out code showing alternatives that were rejected
- Comments tying back to KnockOff generator internals where important
- `// GENERATOR BEHAVIOR:` comments showing what code is generated
- Separate solution (`src/Design/Design.sln`) to avoid noise in main solution

### Design Update Workflow

```
Design Code → Design Plan → Updated Codebase + Design Code → Skills/Samples → Documentation
```

---

## Plans

- [Design Source of Truth - Implementation Plan](../plans/design-source-of-truth-plan.md)

---

## Tasks

### Phase 1: Foundation
- [ ] Create `src/Design/` directory structure
- [ ] Create `Design.sln` solution with project reference to KnockOff
- [ ] Create `Design.Domain` project with basic stub definitions

### Phase 2: Pattern Documentation
- [ ] Create `StubPatterns/AllPatterns.cs` showing all four stub patterns side-by-side
- [ ] Add extensive comments explaining when to use each pattern
- [ ] Document what the generator produces for each pattern

### Phase 3: Member Type Coverage
- [ ] Methods documentation (OnCall, Returns, When, ThenCall, Verify)
- [ ] Properties documentation (OnGet, OnSet, Value, sequences)
- [ ] Indexers documentation (OnGet, OnSet, Backing, sequences)
- [ ] Events documentation (Raise, VerifyAdd, VerifyRemove)
- [ ] Delegates documentation (Interceptor pattern)

### Phase 4: Advanced Features
- [ ] When() API comprehensive documentation
- [ ] Sequence API (OnCall().ThenCall()) documentation
- [ ] Verification patterns (Verifiable, Verify, Times)
- [ ] Source delegation pattern
- [ ] Strict mode behavior

### Phase 5: Testing Patterns
- [ ] Create `Design.Tests` project
- [ ] Demonstrate stub usage patterns
- [ ] Show common testing scenarios

### Phase 6: Documentation & Finalization
- [ ] Create `README.md` and `CLAUDE-DESIGN.md`
- [ ] Update main `CLAUDE.md` to reference design projects
- [ ] Re-evaluate relationship with skills/knockoff/ after completion

### Comment Requirements
- [ ] At least 10 "DID NOT DO THIS BECAUSE" comments
- [ ] At least 10 "DESIGN DECISION" comments
- [ ] At least 5 "GENERATOR BEHAVIOR" comments
- [ ] At least 5 "COMMON MISTAKE" comments

---

## Progress Log

**2026-01-30**: Created todo and plan based on RemoteFactory's design-source-of-truth pattern.

**2026-01-30**: Architect completed review - enhanced plan with API coverage checklist, test strategy, and KnockOff-specific implementation notes.

**2026-01-30**: Developer initial review - raised 4 concerns (API naming, test strategy specificity, comment verification, phase dependencies).

**2026-01-30**: Architect addressed all 4 concerns with specific guidance.

**2026-01-30**: Developer re-review - all concerns resolved, plan approved. Implementation contract created with 7 phases, ~70 tests, and specific verification gates.

**2026-01-30**: Fixed high-priority documentation issues from review:
1. README.md - Updated file organization section to reflect actual structure (only AllPatterns.cs exists in StubPatterns/, not 5 separate files)
2. SourceDelegation.cs - Fixed contradictory documentation about Reset() behavior. Section header said "Does NOT Clear Source" but note said it actually does. Rewrote to clearly state Reset() DOES clear source reference.
3. IMatrix - Already properly documented as KNOWN LIMITATION in IndexerBasics.cs. Added clarifying comment to IMatrix interface definition in Design.Domain.
All 115 tests pass across net8.0, net9.0, and net10.0.

---

## Results / Conclusions

