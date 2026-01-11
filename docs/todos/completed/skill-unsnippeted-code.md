# Skill Documentation: Unsnippeted Code

Code examples in skill documentation that should be sourced from compiled samples but currently are not in snippet regions.

## Status: COMPLETED

## Files Updated

- `~/.claude/skills/knockoff/SKILL.md`
- `~/.claude/skills/knockoff/moq-migration.md`

## Task List

### SKILL.md

- [x] **Accessing as Interface Type section** - Interface vs class stub access patterns
  - Snippet: `skill:SKILL:interface-class-access`
  - Source: `SkillSamplesTests.cs`

### moq-migration.md

- [x] **Step 2 - Class stubs example**
  - Snippet: `skill:moq-migration:class-stub-object-usage`
  - Source: `MoqMigrationSamples.cs`

- [x] **As{InterfaceName}() helper methods**
  - Snippet: `skill:moq-migration:as-interface-helpers-usage`
  - Source: `MoqMigrationSamples.cs`
  - Note: Verified naming convention is `As{InterfaceName}()` where the leading "I" is stripped (e.g., `IMmEmployee` -> `AsMmEmployee()`)

- [x] **SetupProperty (Tracked Properties) - KnockOff examples**
  - Kept as pseudocode in documentation
  - Reason: Backing properties (`*Backing`) are `protected`, requiring access from within the stub class or derived class
  - Unit tests verify tracking works through interface

- [x] **Interface Inheritance - KnockOff examples**
  - OnGet callbacks: Snippet `skill:moq-migration:interface-inheritance-callbacks`
  - Backing properties: Kept as pseudocode (protected access)
  - Source: `MoqMigrationSamples.cs`

## Notes

- Backing properties (`*Backing`) are `protected` - examples showing direct access remain as pseudocode per CLAUDE.md guidance (pseudocode is acceptable for illustrative patterns)
- Moq comparison code (showing "before" patterns) is acceptable as illustrative pseudocode
- Run `.\scripts\extract-snippets.ps1 -Update` after modifying samples to sync to skill files
- **PowerShell required** - sync script not run during this session (pwsh not available)

## Files Changed

### Samples
- `src/Tests/KnockOff.Documentation.Samples/Skills/SkillSamples.cs` - Added access pattern types
- `src/Tests/KnockOff.Documentation.Samples/Skills/MoqMigrationSamples.cs` - Added class stub, interface helpers, tracked props, inheritance samples

### Tests
- `src/Tests/KnockOff.Documentation.Samples.Tests/Skills/SkillSamplesTests.cs` - Added interface-class-access test
- `src/Tests/KnockOff.Documentation.Samples.Tests/Skills/MoqMigrationSamplesTests.cs` - Added tests for new samples

### Skills
- `~/.claude/skills/knockoff/SKILL.md` - Updated Accessing as Interface Type section with snippet
- `~/.claude/skills/knockoff/moq-migration.md` - Updated class stubs, As{Interface} helpers, interface inheritance sections

## Post-Completion

Run the following to verify snippets are in sync (requires PowerShell):
```powershell
.\scripts\extract-snippets.ps1 -Verify
```
