# Add User Properties

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-04
**Last Updated:** 2026-02-04

---

## Problem

Standalone stubs support user-defined methods via the base class pattern (`protected override string Process_(string input)`), but there's no equivalent for properties. Users cannot provide custom property implementations that use constructor-injected state.

**Current limitation:**
```csharp
[KnockOff]
public partial class MyRepoStub(List<User> Users) : IMyRepo
{
    // No way to do this:
    // protected override int Count_ => Users.Count;
}
```

Users must configure properties dynamically in tests via `OnGet()`, which doesn't allow leveraging constructor state for reusable defaults.

## Solution

Extend the existing User Methods base class pattern to include properties. Generate virtual properties with underscore suffix that users can override in their partial class.

**Target syntax:**
```csharp
[KnockOff]
public partial class MyRepoStub(List<User> Users) : IMyRepo
{
    protected override int Count_ => Users.Count;
}
```

---

## Plans

- [User Properties Design](../plans/user-properties-design.md)

---

## Tasks

- [x] Design property override syntax for get-only, set-only, and get/set properties
- [x] Update generator to emit virtual properties in base class
- [x] Implement override detection for properties
- [x] Integrate with existing interceptor tracking (VerifyGet, VerifySet, LastSetValue)
- [x] Support OnGet/OnSet superseding user overrides (like OnCall supersedes user methods)
- [x] Add Design.Stubs examples in UserMethods/ or new UserProperties/ folder
- [x] Add tests for all applicable patterns
- [x] Update skill documentation

---

## Progress Log

**2026-02-04:** Created todo and initial plan. Feature requested to allow property overrides in standalone stubs using the same base class pattern as user methods.

**2026-02-04:** Developer review completed. Four concerns raised regarding plan inconsistencies and gaps. Status changed to "Concerns Raised" pending architect response.

**2026-02-04:** Architect addressed all four concerns with substantial plan revisions:
- C1: Phase 3 rewritten to implement DC1 Option B (interface implementation approach)
- C2: New interceptor methods specified (RecordGet, HasOnGet, InvokeGetCallback, RecordSet, HasOnSet, InvokeSetCallback)
- C3: Property deduplication logic added to Phase 2.1
- C4: Test impact assessment added (no existing tests affected)

**2026-02-04:** Developer re-review completed. All concerns satisfactorily addressed. Plan approved. Implementation contract created with 6 phases and verification gates. Status: Ready for Implementation.

**2026-02-04:** Implementation completed using 3-agent approach:
- Generator Agent (Phases 1-3): Model changes, base class generation, interceptor methods
- Examples Agent (Phase 4): UserPropertyBasics.cs with all 4 patterns
- Tests & Docs Agent (Phases 5-6): 25 tests + skill documentation

**2026-02-04:** Discovered patterns 3-4 (standalone class) needed additional generator work. Extended generator to support all 4 standalone patterns.

**2026-02-04:** Found architectural conflict - original fix broke existing tests. Implemented compile-time detection: generator only uses user override code path when user actually provides an override.

**2026-02-04:** All 2000+ tests pass. Feature complete.

---

## Results / Conclusions

**Delivered:**
- User properties work for all 4 standalone patterns (Standalone, Generic Standalone, Standalone Class, Generic Standalone Class)
- Syntax: `protected override T PropertyName_ => ...` for get-only, `{ get; set; }` for get/set
- OnGet/OnSet supersedes user override per-test
- Full tracking (VerifyGet, VerifySet, LastSetValue) works through overrides
- Strict mode bypassed for overridden properties
- 25 new tests covering all scenarios
- Skill documentation updated

**Key design decisions:**
- Compile-time detection: Generator analyzes partial class for property overrides, generates different code paths accordingly
- Consistent with user methods pattern: same underscore suffix, same priority order
- No breaking changes to existing stubs

