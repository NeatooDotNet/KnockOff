# KnockOff Plugin for Claude Code

This plugin provides comprehensive assistance for using the KnockOff stub library in your .NET test projects.

## Features

### Skill: KnockOff Usage Knowledge

Automatically activates when you ask about:
- Creating stubs with `[KnockOff]` or `[KnockOff<T>]` attributes
- The three stub patterns (Stand-Alone, Inline Interface, Inline Class)
- Configuring behavior with `OnCall`, `OnGet`, `OnSet`
- Verification with `Verify()` and `Verifiable()`
- Async method handling
- Common issues and best practices

### Commands

- **`/knockoff:create-stub`** - Create a new KnockOff stub class with the pattern of your choice
- **`/knockoff:migrate-from-moq`** - Convert existing Moq tests to KnockOff
- **`/knockoff:troubleshoot`** - Diagnose and fix common KnockOff issues

## Installation

This plugin is included in the KnockOff repository. To use it:

```bash
# From the KnockOff repo root
claude --plugin-dir skills/knockoff
```

Or add to your Claude Code settings to always load it when working in this repo.

## Usage Examples

### Create a new stub
```
/knockoff:create-stub
```
Follow the prompts to select your pattern and target interface.

### Migrate from Moq
```
/knockoff:migrate-from-moq path/to/tests/MyTests.cs
```
Analyzes your Moq tests and transforms them to KnockOff.

### Troubleshoot issues
```
/knockoff:troubleshoot
```
Describe your issue or point to the problematic code.

## Documentation

For comprehensive KnockOff documentation, see:
- [Getting Started](../../docs/getting-started.md)
- [Stub Patterns](../../docs/guides/stub-patterns.md)
- [Methods Guide](../../docs/guides/methods.md)
- [Properties Guide](../../docs/guides/properties.md)
- [Migration from Moq](../../docs/migration/from-moq.md)
