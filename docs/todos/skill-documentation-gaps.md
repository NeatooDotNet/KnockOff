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

## Priority

1. ~~**High:** Class stubs documentation~~ - DONE
2. ~~**Medium:** Delegate stubs enhancements~~ - DONE
3. **Medium:** Move skills into repo (enables proper snippet sync)
