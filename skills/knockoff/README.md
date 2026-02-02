> **KnockOff Plugin** > README

# KnockOff Plugin for Claude Code

This plugin provides comprehensive assistance for using the KnockOff stub library in your .NET test projects.

## Features

### Skill: KnockOff Usage Knowledge

Automatically activates when you ask about:
- Creating stubs with `[KnockOff]` or `[KnockOff<T>]` attributes
- The four stub patterns (Stand-Alone, Inline Interface, Inline Class, Inline Delegate)
- Configuring behavior with `Returns`, `OnCall`, `OnGet`, `OnSet`
- Argument matching with `When()` API
- Sequential callbacks with `ThenCall`, `ThenGet`, `ThenSet`
- Verification with `Verify()`, `Verifiable()`, `VerifyAll()`, and `Times` constraints
- Async method handling (auto-wrapping)
- Generic method stubbing with `.Of<T>()`
- Event subscription tracking (Handler property)
- Source delegation with `Source()`
- Strict mode configuration
- Common issues and best practices

### Commands

- **`/knockoff:create-stub`** - Create a new KnockOff stub class with the pattern of your choice
- **`/knockoff:migrate-from-moq`** - Convert existing Moq tests to KnockOff
- **`/knockoff:troubleshoot`** - Diagnose and fix common KnockOff issues

For detailed command documentation, see:
- [Create Stub Command](commands/create-stub.md)
- [Migrate from Moq Command](commands/migrate-from-moq.md)
- [Troubleshoot Command](commands/troubleshoot.md)

## Installation

This plugin is included in the KnockOff repository and loads automatically when working in this codebase. No additional setup required.

To use this plugin in other projects:
1. Copy the `skills/knockoff/` directory to your project
2. Claude Code will auto-discover it in your `.claude/` or `skills/` directories

## Quick API Reference

### Four Patterns

| Pattern | Attribute | Access |
|---------|-----------|--------|
| Stand-Alone | `[KnockOff]` on partial class | `new MyStub()` |
| Inline Interface | `[KnockOff<IInterface>]` | `new Stubs.IInterface()` |
| Inline Class | `[KnockOff<MyClass>]` | `new Stubs.MyClass()` then `.Object` |
| Inline Delegate | `[KnockOff<MyDelegate>]` | `new Stubs.MyDelegate()` |

### Method Configuration

```cs
// Fixed value
stub.GetUser.Returns(new User { Id = 1, Name = "Alice" });

// Dynamic callback
stub.GetUser.OnCall((id) => new User { Id = id, Name = $"User{id}" });

// Argument matching
stub.GetUser.When(42).Returns(adminUser);
stub.GetUser.When(id => id < 0).Returns(null);

// Sequences repeat last value after exhaustion (matching NSubstitute)
stub.GetNext.OnCall(() => 1).ThenCall(() => 2).ThenCall(() => 3);
// After 3 calls, continues returning 3

// Use ThenDefault() to return default(T) instead of repeating
stub.GetNext.OnCall(() => 1).ThenCall(() => 2).ThenDefault();
// After 2 calls, returns 0 (default)
```

### Verification

```cs
// Mark for batch verification
stub.Save.OnCall((user) => { }).Verifiable();
stub.Verify();  // Checks all Verifiable() members

// Or verify individually
var tracking = stub.Save.OnCall((user) => { });
tracking.Verify(Times.Once);
```

### Critical Gotchas

1. **Sequences repeat last value** - After all callbacks consumed, sequences repeat the last value (matching NSubstitute). Use `ThenDefault()` to return `default(T)` instead, or Strict mode to throw
2. **Events use Handler property** - `stub.EventInterceptor.Handler?.Invoke(...)` (no Raise method)
3. **Class stubs use .Object** - `stub.Object` to get the class instance
4. **Times.Between() doesn't exist** - Use `AtLeast` + `AtMost` instead

## Plugin Documentation

### Skill Reference

The **knockoff-usage** skill provides comprehensive documentation:

- [Main Skill Guide](skills/knockoff-usage/SKILL.md) - Complete guide with gotchas
- [Stub Patterns](skills/knockoff-usage/references/patterns.md) - All four patterns with examples
- [Methods Guide](skills/knockoff-usage/references/methods.md) - Method configuration and verification
- [Properties Guide](skills/knockoff-usage/references/properties.md) - Property interceptors
- [API Reference](skills/knockoff-usage/references/api-reference.md) - Complete interceptor API
- [Strict Mode](skills/knockoff-usage/references/strict-mode.md) - Strict mode configuration
- [Moq Migration](skills/knockoff-usage/references/moq-migration.md) - Migration guide

---

**UPDATED:** 2026-02-01
