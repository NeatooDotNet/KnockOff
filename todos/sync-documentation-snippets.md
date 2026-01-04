# Sync Documentation Snippets

## Status: Complete

**Final state**: 68 snippets synced, 2 intentional orphans

## Problem

The `KnockOff.Documentation.Samples` project contains 70 code snippets with `#region docs:*` markers, but only 2 snippets were actually connected to the markdown documentation. The remaining 68 snippets were "orphans" - they exist in the samples but the markdown files didn't reference them.

This means:
- Code in docs may drift from the actual API
- Changes to samples don't automatically update docs
- Code examples in docs aren't compiled or tested

## Solution

Add `<!-- snippet: docs:{file}:{id} -->` markers to the markdown files, then run `.\scripts\extract-snippets.ps1 -Update` to sync.

## Current State

| Doc File | Snippets in Samples | Snippets Synced |
|----------|---------------------|-----------------|
| getting-started.md | 5 | 2 |
| methods.md | 11 | 0 |
| properties.md | 6 | 0 |
| async-methods.md | 5 | 0 |
| generics.md | 6 | 0 |
| multiple-interfaces.md | 4 | 0 |
| interface-inheritance.md | 5 | 0 |
| indexers.md | 4 | 0 |
| events.md | 3 | 0 |
| customization-patterns.md | 4 | 0 |
| knockoff-vs-moq.md | 8 | 0 |
| migration-from-moq.md | 9 | 0 |
| **Total** | **70** | **2** |

## Process for Each File

1. Open the markdown file
2. Find code blocks that match snippets in samples
3. Replace raw code blocks with snippet markers:
   ```markdown
   <!-- snippet: docs:{file}:{id} -->
   ```csharp
   placeholder
   ```
   <!-- /snippet -->
   ```
4. Run `.\scripts\extract-snippets.ps1 -Update`
5. Verify content was replaced correctly

## Checklist

### Getting Started (5/5 synced)
- [x] interface-definition
- [x] stub-class
- [x] user-method
- [x] returning-values
- [x] multiple-interfaces

### Guides

#### methods.md (9/11 synced, 2 intentional orphans)
- [x] void-no-params
- [x] void-with-params
- [x] return-value
- [ ] single-param (intentional orphan - docs use inline example)
- [ ] multiple-params (intentional orphan - docs use inline example)
- [x] user-defined
- [x] priority-order
- [x] simulating-failures
- [x] verifying-call-order
- [x] sequential-returns
- [x] accessing-spy-state

#### properties.md (6/6 synced)
- [x] get-set-property
- [x] get-only-property
- [x] conditional-logic
- [x] computed-property
- [x] tracking-changes
- [x] throwing-on-access

#### async-methods.md (5/5 synced)
- [x] basic-interface
- [x] user-defined
- [x] cancellation
- [x] simulating-failures
- [x] call-order

#### generics.md (6/6 synced)
- [x] basic-interface
- [x] multiple-params
- [x] constrained
- [x] factory-pattern
- [x] collection-repo
- [x] async-generic

#### multiple-interfaces.md (4/4 synced)
- [x] basic-usage
- [x] shared-method
- [x] repo-uow
- [x] multiple-repos

#### interface-inheritance.md (5/5 synced)
- [x] basic-usage
- [x] deep-inheritance
- [x] entity-base
- [x] validation-pattern
- [x] repository-hierarchy

#### indexers.md (4/4 synced)
- [x] basic-interface
- [x] entity-property
- [x] dictionary-like
- [x] integer-indexer

#### events.md (3/3 synced)
- [x] basic-interface
- [x] testing-viewmodel
- [x] progress-reporting

### Concepts

#### customization-patterns.md (4/4 synced)
- [x] user-method-basic
- [x] user-method-async
- [x] priority-example
- [x] combining-patterns

### Comparison

#### knockoff-vs-moq.md (8/8 synced)
- [x] basic-stub
- [x] async-stub
- [x] multiple-interfaces
- [x] indexer-stub
- [x] event-stub
- [x] verification-patterns
- [x] sequential-returns
- [x] per-test-override

#### migration-from-moq.md (9/9 synced)
- [x] create-knockoff-class
- [x] static-returns-user-method
- [x] multiple-interfaces
- [x] shared-stubs
- [x] argument-matching
- [x] sequential-returns
- [x] throwing-exceptions
- [x] automatic-tracking
- [x] complex-callbacks

## Verification

Final verification output:
```
Verification complete. 68 snippets verified, 2 orphan snippets.
```

The 2 orphan snippets (`single-param` and `multiple-params` in methods.md) are intentional - the documentation uses more complete inline examples for those sections instead of the minimal stub classes from samples.

## Notes

- Renamed `docs/design/events.md` to `docs/design/events-design.md` to avoid filename collision with `docs/guides/events.md`
- The snippet content in markdown will be completely replaced by the sample code.
- Focus on matching existing code blocks to samples, not rewriting documentation.
