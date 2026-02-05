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

### Pipeline Verification Rule

**Each pattern group uses a separate code pipeline. A feature added to one pipeline does NOT exist in another.**

| Patterns | Transform | Builder | Renderer |
|---|---|---|---|
| `[KnockOff]` interface (1,2) | `TransformClass` | `FlatModelBuilder` | `FlatRenderer` |
| `[KnockOffBase<T>]` class (3,4) | `TransformStandaloneClass` | `StandaloneClassModelBuilder` | `StandaloneClassRenderer` |
| Inline interface/class (5,6) | `TransformInlineStubClass` | `InlineModelBuilder` | `InlineRenderer` |
| Open generic (7,8,9) | Various | Various | `InlineRenderer` |

**When a plan claims a feature works across patterns:**
1. Identify which pipelines are affected
2. For each pipeline, grep the actual builder AND renderer code for the feature
3. If the feature doesn't exist in a pipeline, say so - do NOT assume "same code path"
4. "Same code path" is never an acceptable justification without tracing the actual call chain

**Trust the compiler, not the plan.** Write a test. If it fails to compile, the feature doesn't exist.

### Post-Implementation Review Order

When reviewing completed work, follow this order. **Start with production code and keep it in context throughout.**

1. **Production code** - Read the actual generator changes (builder, renderer, model). This is the source of truth.
2. **Design** - Review against the plan. Does the implementation match what was designed? Are all claimed patterns actually implemented?
3. **Tests** - Do tests exercise what the production code actually generates? Try to write a test for each claimed pattern.
4. **Documentation** - Does it accurately describe what the production code does?
5. **Skills** - Do skill files reflect the current state of the production code?

**The key:** Everything gets reviewed *against* the production code, not the other way around. If the docs say a feature exists but the production code doesn't have it, the docs are wrong.

## Folder Structure

- `src/` - Source code
- `src/Design/` - **API design source of truth** (see below)
- `docs/` - Documentation (markdown)
- `docs/todos/` - Project todos (use `/project-todos` skill)
- `docs/plans/` - Design documents (use `/project-todos` skill)
- `docs/release-notes/` - Release notes
- `.claude/agents/` - Agent-specific guidance files

## Design Projects (Source of Truth)

`src/Design/` is the authoritative reference for KnockOff's API. **Read Design.Stubs files—not the generator or library code—to understand how KnockOff behaves.** Use Design projects when answering questions about KnockOff behavior, brainstorming enhancements, or evaluating feature ideas. See `.claude/rules/` for guidance when working in these directories.

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

Documentation uses MarkdownSnippets. See `.claude/rules/` for guidance.
