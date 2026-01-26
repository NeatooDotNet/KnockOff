# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Core Principle

**Trust the codebase, not documentation.** Always explore the actual code to understand patterns, conventions, and architecture. Do not rely on descriptions in this file or memory from previous sessions.

## Project Basics

**KnockOff** is a Roslyn Source Generator for creating unit test stubs.

**Solution file:** `src/KnockOff.sln`

**Versioning:** Pre-1.0. All changes bump minor version only.

## Folder Structure

- `src/` - Source code
- `docs/` - Documentation (markdown)
- `docs/todos/` - Project todos (use `/project-todos` skill)
- `docs/plans/` - Design documents (use `/project-todos` skill)
- `docs/release-notes/` - Release notes
- `.claude/agents/` - Agent-specific guidance files

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

## Documentation

Documentation uses MarkdownSnippets for code synchronization. Samples are compiled and tested. Explore the codebase to understand the current structure and patterns.

**Excluded from documentation pipeline:** `docs/todos/`, `docs/plans/`, `docs/release-notes/`, `.claude/`
