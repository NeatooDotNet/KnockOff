# Complete Test Class Migration Example

**Source:** skill-feedback-ztreatment-migration.md

## Description

Add a comprehensive before/after example of migrating an entire Moq test fixture to KnockOff.

## Task List

- [ ] Create full Moq test class example (constructor, helpers, multiple tests)
- [ ] Create equivalent KnockOff test class
- [ ] Add commentary highlighting key differences
- [ ] Add to moq-migration.md skill file

## Requirements

**Moq test class should include:**
- Constructor/setup with multiple mocks
- Helper method for creating configured mocks
- Multiple test methods using various Moq patterns (Setup, Returns, Verify, Callback)

**KnockOff equivalent should show:**
- Stub class definitions
- Test class structure
- How patterns map between frameworks

## Why Valuable

Real-world migrations involve full test classes, not isolated patterns. A complete example helps developers see the overall structure change.

## Samples Location

`src/Tests/KnockOff.Documentation.Samples/Skills/MoqMigration/`
