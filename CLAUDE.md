# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Core Principle

**Trust the codebase, not documentation.** Always explore the actual code to understand patterns, conventions, and architecture. Do not rely on descriptions in this file or memory from previous sessions.

## Project Basics

**KnockOff** is a Roslyn Source Generator for creating unit test stubs.

**Solution file:** `src/KnockOff.sln`

**Versioning:** Pre-1.0. All changes bump minor version only.

## KnockOff Patterns and Members

**ALWAYS consider all patterns and members when creating todos or plans.** Missing a pattern or member type leads to incomplete implementations.

### Nine Patterns

**Standalone Patterns** (file-based, reusable across tests):
1. **Standalone** - `[KnockOff]` on partial class implementing interface
2. **Generic Standalone** - `[KnockOff]` on generic partial class: `class Stub<T> : IService<T>`
3. **Standalone Class** - `[KnockOffBase<ConcreteClass>]` on partial class (uses `.Object`)
4. **Generic Standalone Class** - `[KnockOffBase(typeof(ClassBase<>))]` on generic partial class (uses `.Object`)

**Inline Patterns** (nested within test class):
5. **Inline Interface** - `[KnockOff<IInterface>]` generates nested stub class
6. **Inline Class** - `[KnockOff<ConcreteClass>]` generates nested stub for virtual/abstract members (uses `.Object`)
7. **Inline Delegate** - `[KnockOff<DelegateType>]` generates stub for delegate invocation
8. **Open Generic Interface** - `[KnockOff(typeof(IService<>))]` generates generic nested stub (stub IS implementation)
9. **Open Generic Class** - `[KnockOff(typeof(ServiceBase<>))]` generates generic nested stub (uses `.Object`)

### Four Member Types

1. **Methods** - Instance methods with various signatures
2. **Properties** - Get-only, set-only, get/set properties
3. **Indexers** - `this[...]` accessors (get/set with key parameters)
4. **Events** - Event add/remove handlers

### Scope Checklist for Todos/Plans

When defining scope, explicitly state which patterns and members are affected:
- [ ] Which patterns? (all 9, or specific subset)
- [ ] Which member types? (all 4, or specific subset)
- [ ] Are there pattern+member combinations that need special handling?

## Folder Structure

- `src/` - Source code
- `src/Design/` - **API design source of truth** (see below)
- `docs/` - Documentation (markdown)
- `docs/todos/` - Project todos (use `/project-todos` skill)
- `docs/plans/` - Design documents (use `/project-todos` skill)
- `docs/release-notes/` - Release notes
- `.claude/agents/` - Agent-specific guidance files

**Do NOT load as reference:** `docs/todos/`, `docs/plans/`, `docs/release-notes/` are work artifacts, not reference documentation. Only access these when:
- Actively working on a specific todo or plan
- Adding a new release note
Never load these folders to understand the codebase, research history, or problem-solve.

## Design Projects (Source of Truth)

**The `src/Design/` directory is the authoritative source of truth for KnockOff's API.**

When answering questions about how KnockOff works:
1. Read the relevant file in `src/Design/Design.Stubs/`
2. Trust the code and comments as ground truth
3. Never guess - verify against these files

Key files by topic:
- **Stub patterns**: `StubPatterns/AllPatterns.cs`
- **Methods**: `Methods/BasicMethods.cs`, `Methods/WhenMatching.cs`
- **Properties**: `Properties/PropertyBasics.cs`
- **Indexers**: `Indexers/IndexerBasics.cs`
- **Events**: `Events/EventPatterns.cs`
- **Advanced**: `Advanced/` (Source delegation, Strict mode, Verification)

See `src/Design/CLAUDE-DESIGN.md` for Claude-specific guidance.

## KnockOff Skill (`skills/knockoff/`)

**The skill must remain stand-alone.** It is distributed to other projects where Design projects don't exist. When updating the skill, incorporate insights from Design projects but never add dependencies on Design files.

## Plan Mode and Project Todos

**When plan mode completes:**
1. Plan mode creates the design document through brainstorming conversation
2. Plan mode then uses project-todos skill to:
   - Create a todo in docs/todos/ capturing the user request
   - Create a plan in docs/plans/ with the design content
   - Link them together
   - Set todo status: "In Progress"
   - Set plan status: "Draft (Architect)"

**After plan mode creates todo+plan:**
- Automatically invoke knockoff-architect agent to enhance the plan
- Architect reviews, adds KnockOff-specific architecture, completes verification checklist
- Architect hands off to knockoff-developer for review
- Developer reviews and either raises concerns or creates implementation contract
- After user approval, developer implements with milestone verification

**The automatic pipeline:**
```
Plan Mode → knockoff-architect → knockoff-developer → Implementation
```

## Code Review Checklist

- Check that plans in the PR are linked to a single todo in their plan markdown

## Agent Files

Agent files in `.claude/agents/` provide role-specific guidance. They receive CLAUDE.md automatically and should not duplicate its rules.

## Source-Generated Files

**Roslyn-generated code is excluded from git.** Generated files in `Generated/` folders are not tracked in version control. Tests verify that generator output is correct.

## Documentation

Documentation uses MarkdownSnippets for code synchronization. Samples are compiled and tested. Explore the codebase to understand the current structure and patterns.

**Excluded from documentation pipeline:** `docs/todos/`, `docs/plans/`, `docs/release-notes/`, `.claude/`
