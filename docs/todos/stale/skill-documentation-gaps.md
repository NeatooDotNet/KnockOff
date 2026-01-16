# Skill Documentation Gaps

## Overview

This document tracks gaps in the Claude Code skill file (`~/.claude/skills/knockoff/SKILL.md`) that were identified during a Neatoo migration evaluation on 2026-01-07.

**Updated 2026-01-10:** Class Stubs documentation complete. Remaining work is skill-to-repo migration.

## Task List

- [x] Add Class Stubs documentation to skill
- [x] Add comprehensive delegate stubs section with examples
- [ ] Move skills into repo at `.claude/skills/knockoff/`
- [ ] Run `dotnet mdsnippets` to expand snippet markers
- [ ] Add `<!-- pseudo: -->` markers for intentional inline code in migrations.md

## Gap 1: Class Stubs Documentation - COMPLETE

**Status:** Done (SKILL.md lines 136-199)

The skill now documents:
- Basic usage pattern with `[KnockOff<TClass>]`
- Unified API (same as interface stubs)
- Constructor parameters
- Abstract classes
- Non-virtual members
- Comparison table (interface vs class stubs)

## Gap 2: Delegate Stubs - COMPLETE

**Status:** Done (SKILL.md lines 124-134)

Basic delegate stub pattern documented with snippet markers.

## Gap 3: Skills Not in Repository

**Status:** In Progress

Skills are currently at `~/.claude/skills/knockoff/` (shared location) but should live in the repo at `.claude/skills/knockoff/` per the new MarkdownSnippets workflow.

### Action Required

1. Copy skills into repo: `.claude/skills/knockoff/`
2. Run `dotnet mdsnippets` to expand snippet markers
3. Add `<!-- pseudo: -->` markers for inline code in migrations.md (10 blocks)
4. Update copy-on-commit hook to sync repo → shared location

### Why This Matters

- Skills should be versioned with the code they describe
- MarkdownSnippets can process skills in repo
- Shared location (`~/.claude/skills/`) gets updated on commit

## Gap 4: API Changes (10.2.0 → 10.3.0+)

**Status:** Not Started

**Source:** `NeatooATM/docs/todos/knockoff-limitations-investigation.md`

These gaps were discovered during NeatooATM implementation (2026-01-11).

### Task List

- [ ] **Update tracking API documentation**: `Spy` property no longer exists, replaced with `ExecutionInfo`:
  | Feature | 10.2.0 (documented) | 10.3.0+ (actual) |
  |---------|---------------------|------------------|
  | Tracking API | `Spy.Method.CallCount` | `ExecutionInfo.Method.CallCount` |
  | Runtime callbacks | `OnCall = (ko, args) => ...` | **NOT AVAILABLE** |
  | Cast helper | Direct cast | `AsInterfaceName()` method |

- [ ] **Document ExecutionInfo API**: The new tracking structure:
  ```csharp
  // Per-method execution details
  stub.ExecutionInfo.Method.CallCount      // int
  stub.ExecutionInfo.Method.WasCalled      // bool
  stub.ExecutionInfo.Method.LastCallArgs   // Named tuple (nullable)
  stub.ExecutionInfo.Method.AllCalls       // IReadOnlyList<tuple>
  stub.ExecutionInfo.Method.Reset()        // Clears tracking
  ```

- [ ] **Document removal of runtime callbacks**: `OnCall` is no longer available in 10.3.0+. All behavior must be defined at compile-time via user methods.

- [ ] **Document mutable state pattern for per-test behavior**: Since `OnCall` doesn't exist, use mutable fields:
  ```csharp
  [KnockOff]
  public partial class ServiceKnockOff : IService
  {
      // Test-controllable state
      public bool ShouldSucceed { get; set; } = true;  // Happy path default

      protected Task<bool> DoWorkAsync(...)
      {
          return Task.FromResult(ShouldSucceed);
      }
  }

  // In test:
  var stub = new ServiceKnockOff();
  stub.ShouldSucceed = false;  // Override for this test
  ```

- [ ] **Document compile-time only behavior limitation**: Cannot configure behavior per-test using callbacks like Moq. Must use mutable state or separate stub classes.

- [ ] **Document method overload type confusion**: With method overloads, KnockOff may generate incorrect tuple field types (e.g., `EmployeeId` instead of `Guid`). Workaround: access via tuple index instead of named field.

- [ ] **Add migration guide from 10.2.0 to 10.3.0+**: Breaking changes require test code updates.

---

## Priority

1. ~~**High:** Class stubs documentation~~ - DONE
2. ~~**Medium:** Delegate stubs enhancements~~ - DONE
3. **Medium:** Move skills into repo (enables proper snippet sync)
4. **Medium:** API 10.2.0 → 10.3.0+ documentation updates
