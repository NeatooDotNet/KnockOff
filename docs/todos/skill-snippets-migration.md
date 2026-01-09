# Skill Documentation Snippets Migration Plan

## Summary

**Problem:** 20 out of 85 C# code blocks in KnockOff skill documentation are inline (24%) rather than sourced from compiled samples.

**Goal:** Move inline code to `KnockOff.Documentation.Samples/Skills/` so it compiles, tests, and stays in sync with skill files.

**Infrastructure:** Already exists! `extract-snippets.ps1` supports `#region skill:{file}:{id}` pattern.

---

## Current State

| File | Total | Snippet | Inline | Coverage |
|------|-------|---------|--------|----------|
| SKILL.md | 39 | 27 | 12 | 69% |
| customization-patterns.md | 11 | 11 | 0 | **100%** |
| moq-migration.md | 17 | 16 | 1 | 94% |
| interceptor-api.md | 11 | 11 | 0 | **100%** |
| migrations.md | 7 | 0 | 7 | 0% |
| **Total** | **85** | **65** | **20** | **76%** |

### Skill Files Location

```
~/.claude/skills/knockoff/
├── SKILL.md                    # Main skill reference (69% coverage)
├── customization-patterns.md   # 100% - DONE
├── moq-migration.md            # 94% - nearly done
├── interceptor-api.md          # 100% - DONE
└── migrations.md               # 0% - needs evaluation
```

### Sample Files Location

```
src/Tests/KnockOff.Documentation.Samples/Skills/
├── SkillSamples.cs                 # SKILL.md snippets
├── HandlerApiSamples.cs            # interceptor-api.md snippets
├── CustomizationPatternsSamples.cs # customization-patterns.md snippets
├── MoqMigrationSamples.cs          # moq-migration.md snippets
└── MigrationSamples.cs             # migrations.md snippets (TO CREATE)
```

---

## Analysis of Inline Blocks

### SKILL.md (12 inline blocks)

| Line | Section | Content | Decision |
|------|---------|---------|----------|
| 78 | Quick Start | Test usage example | **Source** - real test pattern |
| 116 | Inline Stubs | `[KnockOff<IUserService>]` pattern | **Source** - core API |
| 136 | Partial Properties | C# 13+ auto-instantiated stubs | **Source** - real API |
| 148 | Delegate Stubs | `[KnockOff<TDelegate>]` example | **Source** - core API |
| 177 | Class Stubs | `[KnockOff<TClass>]` example | **Source** - core API |
| 216 | Constructor Params | `new Stubs.Repository("...")` | **Source** - real API |
| 225 | Abstract Classes | Abstract member defaults | **Source** - real API |
| 236 | Non-Virtual Members | `.Object` access pattern | **Source** - real API |
| 365 | Stub Minimalism | Minimal stub example | **Source** - best practice |
| 387 | Reset | `knockOff.GetUser.Reset()` | **Source** - real API |
| 751 | Overloads | Single method (no suffix) | **Source** - real API |
| 775 | Nested Classes | Error example with ❌/✅ | **Keep inline** - shows compilation error |

**Summary:** 11 should be sourced, 1 kept inline → Target coverage: **97%** (38/39)

### moq-migration.md (1 inline block)

| Line | Content | Decision |
|------|---------|----------|
| 353 | Mixed Moq + KnockOff coexistence | **Keep inline** - contains Moq code |

The block demonstrates using both libraries in the same project. Since it includes `new Mock<IOrderService>()`, it cannot be sourced to the KnockOff samples project.

**Summary:** Already at max coverage (94% → 94%)

### migrations.md (7 inline blocks)

**Special case:** Migration docs show "before" code that no longer compiles with current KnockOff version.

| Line | Section | Content | Decision |
|------|---------|---------|----------|
| 19-23 | v10.9.0 Before | Old `stub.Interceptor.` API | Keep inline - won't compile |
| 26-30 | v10.9.0 After | Current `stub.Member` API | **Source** |
| 59-63 | v10.8.0 Works Same | Current access pattern | **Source** |
| 69-79 | v10.8.0 Migration | Mixed old/new delegate casts | Keep inline - old API |
| 104-108 | v10.7.0 Works Same | Current access pattern | **Source** |
| 112-118 | v10.7.0 Migration | Old `Spy` → `KO` rename | Keep inline - old API |
| 130-146 | v10.7.0 AllCalls | Callback capture workaround | **Source** - current pattern |

**Summary:** 4 can be sourced, 3 kept inline → Target coverage: **57%** (4/7)

---

## Tasks

### Phase 1: Source SKILL.md blocks (11 blocks)

- [ ] Add samples to `Skills/SkillSamples.cs` for:
  - [ ] `skill:SKILL:quick-start-usage` (line 78 - test usage)
  - [ ] `skill:SKILL:inline-stub-pattern` (line 116 - inline interface stub)
  - [ ] `skill:SKILL:partial-properties` (line 136 - C# 13+ pattern)
  - [ ] `skill:SKILL:delegate-stubs` (line 148 - delegate stubbing)
  - [ ] `skill:SKILL:class-stubs` (line 177 - class stubbing)
  - [ ] `skill:SKILL:class-constructor` (line 216 - constructor params)
  - [ ] `skill:SKILL:abstract-classes` (line 225 - abstract defaults)
  - [ ] `skill:SKILL:non-virtual-members` (line 236 - .Object access)
  - [ ] `skill:SKILL:stub-minimalism` (line 365 - minimal stub)
  - [ ] `skill:SKILL:interceptor-reset` (line 387 - Reset())
  - [ ] `skill:SKILL:overload-no-suffix` (line 751 - single method)
- [ ] Add `<!-- snippet: -->` markers in SKILL.md
- [ ] Run `extract-snippets.ps1 -Update`
- [ ] Verify: 38/39 snippets (97% coverage)

### Phase 2: moq-migration.md - COMPLETE

No action needed. The 1 inline block (line 353) correctly shows mixed Moq/KnockOff usage and must stay inline.

### Phase 3: Source migrations.md blocks (4 blocks)

- [ ] Create `Skills/MigrationSamples.cs`
- [ ] Add samples for:
  - [ ] `skill:migrations:v10-9-after` (line 26 - current API)
  - [ ] `skill:migrations:v10-8-works-same` (line 59 - current pattern)
  - [ ] `skill:migrations:v10-7-works-same` (line 104 - current pattern)
  - [ ] `skill:migrations:v10-7-callback-capture` (line 130 - AllCalls workaround)
- [ ] Add `<!-- snippet: -->` markers in migrations.md
- [ ] Run `extract-snippets.ps1 -Update`
- [ ] Verify: 4/7 snippets (57% coverage)

### Phase 4: Verify and Document

- [ ] Run `.\scripts\extract-snippets.ps1 -Verify`
- [ ] Ensure 0 out-of-sync snippets
- [ ] Update this file with final coverage numbers

---

## Implementation Pattern

### 1. Add region in samples

```csharp
// In Skills/SkillSamples.cs or new file
#region skill:SKILL:example-id
[Fact]
public void ExampleUsage()
{
    // Working example code
}
#endregion
```

### 2. Add marker in skill file

```markdown
<!-- snippet: skill:SKILL:example-id -->
```csharp
// Content synced by extract-snippets.ps1
```
<!-- /snippet -->
```

### 3. Sync and verify

```powershell
.\scripts\extract-snippets.ps1 -Update
.\scripts\extract-snippets.ps1 -Verify
```

---

## Exceptions (Keep Inline)

Similar to docs migration, some code should stay inline:

| Exception Type | Reason |
|----------------|--------|
| "Before" migration code | Won't compile with current version |
| Mixed library examples | Contains non-KnockOff code (e.g., Moq) |
| Error pattern examples | Intentionally shows code that won't compile |
| Generated code output | Shows structure, not runnable |

### Specific Documented Exceptions

| File | Block | Reason |
|------|-------|--------|
| SKILL.md:775 | Nested class error pattern | Shows ❌ won't compile / ✅ correct |
| moq-migration.md:353 | Moq + KnockOff coexistence | Contains `new Mock<T>()` |
| migrations.md:19-23 | v10.9 "Before" | Old `stub.Interceptor.` API |
| migrations.md:69-79 | v10.8 migration | Old delegate cast syntax |
| migrations.md:112-118 | v10.7 migration | Old `Spy` → `KO` naming |

---

## Success Criteria

- [ ] `dotnet build src/KnockOff.sln` passes
- [ ] `dotnet test src/KnockOff.sln` passes
- [ ] `.\scripts\extract-snippets.ps1 -Verify` passes with 0 out-of-sync
- [ ] SKILL.md: 38/39 sourced (97%) - 1 inline exception documented
- [ ] moq-migration.md: 16/17 sourced (94%) - max possible, Moq code inline
- [ ] migrations.md: 4/7 sourced (57%) - "Before" examples intentionally inline

### Target Coverage Summary

| File | Before | After | Inline Exceptions |
|------|--------|-------|-------------------|
| SKILL.md | 69% | **97%** | Error pattern with ❌/✅ |
| customization-patterns.md | 100% | 100% | - |
| moq-migration.md | 94% | 94% | Moq coexistence example |
| interceptor-api.md | 100% | 100% | - |
| migrations.md | 0% | **57%** | Old API "Before" blocks |
| **Total** | **76%** | **93%** | 5 documented |

---

## Verification Commands

```powershell
# Check current state
.\scripts\extract-snippets.ps1 -Verify

# Update skill files from samples
.\scripts\extract-snippets.ps1 -Update

# Full pre-commit check
dotnet build src/KnockOff.sln && `
dotnet test src/KnockOff.sln && `
.\scripts\extract-snippets.ps1 -Verify
```

---

## Notes

- Skill files are in `~/.claude/skills/knockoff/` (user home, not repo)
- The `extract-snippets.ps1` script handles skills via `-SkillsPath` parameter
- Default paths:
  - Linux/WSL: `~/.claude/skills/knockoff`
  - Windows: `$env:USERPROFILE\.claude\skills\knockoff`
- Sample files live in repo: `src/Tests/KnockOff.Documentation.Samples/Skills/`
