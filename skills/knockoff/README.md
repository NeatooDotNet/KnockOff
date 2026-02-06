> **KnockOff Plugin** > README

# KnockOff Plugin for Claude Code

This plugin provides comprehensive assistance for using the KnockOff stub library in your .NET test projects.

## Features

### Skill: KnockOff Usage Knowledge

Automatically activates when you ask about:
- Creating stubs with `[KnockOff]` or `[KnockOff<T>]` attributes
- The nine stub patterns (Standalone, Generic Standalone, Standalone Class, Generic Standalone Class, Inline Interface, Inline Class, Inline Delegate, Open Generic Interface, Open Generic Class)
- Configuring behavior with `Returns`, `OnCall`, `OnGet`, `OnSet`
- Argument matching with `When()` API
- Sequential callbacks with `ThenCall`, `ThenGet`, `ThenSet`
- Verification with `Verify()`, `Verifiable()`, `VerifyAll()`, and `Times` constraints
- Async method handling (auto-wrapping)
- Generic method stubbing with `.Of<T>()`
- Event subscription tracking (`Raise()` method, `HasSubscribers` property)
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

### Nine Patterns

| Pattern | Attribute | Access |
|---------|-----------|--------|
| Standalone | `[KnockOff]` on partial class | `new MyStub()` |
| Generic Standalone | `[KnockOff]` on generic partial class | `new MyStub<T>()` |
| Standalone Class | `[KnockOffBase<MyClass>]` on partial class | `new MyStub().Object` |
| Generic Standalone Class | `[KnockOffBase(typeof(MyClass<>))]` on generic partial class | `new MyStub<T>().Object` |
| Inline Interface | `[KnockOff<IInterface>]` | `new Stubs.IInterface()` |
| Inline Class | `[KnockOff<MyClass>]` | `new Stubs.MyClass().Object` |
| Inline Delegate | `[KnockOff<MyDelegate>]` | `new Stubs.MyDelegate()` |
| Open Generic Interface | `[KnockOff(typeof(IFoo<>))]` | `new Stubs.IFoo<T>()` |
| Open Generic Class | `[KnockOff(typeof(Foo<>))]` | `new Stubs.Foo<T>().Object` |

### User Methods (Stand-Alone Only)

<!-- snippet: skill-readme-user-methods -->
```cs
[KnockOff]
public partial class RepoStub : IRepo { }

public partial class RepoStub
{
    // Override virtual method with underscore suffix - compiler enforces signature!
    protected override User? GetById_(int id) => new User { Id = id, Name = "Default" };
}
```
<!-- endSnippet -->

<!-- snippet: skill-readme-user-methods-usage -->
```cs
var stub = new RepoStub();

// User override is fallback; OnCall supersedes it
stub.GetById.OnCall(id => new User { Id = id, Name = "Override" });

// Returns for constant values (auto-wraps for async)
stub.GetById.Returns(new User { Id = 99 });
```
<!-- endSnippet -->

### Method Configuration

<!-- snippet: skill-readme-method-config -->
```cs
// Fixed value
stub.GetUser.Returns(new User { Id = 1, Name = "Alice" });

// Dynamic callback
stub.GetUser.OnCall((id) => new User { Id = id, Name = $"User{id}" });

// Argument matching
stub.GetUser.When(42).Returns(adminUser);
stub.GetUser.When(id => id < 0).Returns(null);

// Value sequences (NSubstitute-style) - repeats last after exhaustion
stub.GetNext.Returns(1, 2, 3);
// Returns: 1, 2, 3, 3, 3... (repeats last value)

// Mix callbacks with value sequences
stub.Add.OnCall((a, b) => a + b).ThenReturns(100, 200);
// First call: computed. Then: 100, 200, 200, 200...

// Use ThenDefault() to return default(T) instead of repeating
stub.GetNext.Returns(1, 2).ThenDefault();
// Returns: 1, 2, 0, 0... (default after exhaustion)
```
<!-- endSnippet -->

### Verification

<!-- snippet: skill-readme-verification -->
```cs
// Mark for batch verification
stub.Save.OnCall((user) => { }).Verifiable();
svc.Save(new User { Id = 1 }); // Call the method
stub.Verify();  // Checks all Verifiable() members

// Or verify individually
stub.Save.Reset(); // Reset for second pattern demo
var tracking = stub.Save.OnCall((user) => { });
svc.Save(new User { Id = 2 }); // Call the method
tracking.Verify(Times.Once);
```
<!-- endSnippet -->

### Critical Gotchas

1. **Sequences repeat last value** - `Returns(1, 2, 3)` repeats the last value after exhaustion (matching NSubstitute). Use `ThenDefault()` to return `default(T)` instead, or Strict mode to throw
2. **Events use Raise()** - `stub.EventName.Raise(...)` fires all subscribed handlers
3. **Class stubs use .Object** - `stub.Object` to get the class instance
4. **Times.Between() doesn't exist** - Use `AtLeast` + `AtMost` instead

## Plugin Documentation

### Skill Reference

The **knockoff-usage** skill provides comprehensive documentation:

- [Main Skill Guide](skills/knockoff-usage/SKILL.md) - Complete guide with gotchas
- [Stub Patterns](skills/knockoff-usage/references/patterns.md) - All nine patterns with examples
- [Methods Guide](skills/knockoff-usage/references/methods.md) - Method configuration and verification
- [Properties Guide](skills/knockoff-usage/references/properties.md) - Property interceptors
- [API Reference](skills/knockoff-usage/references/api-reference.md) - Complete interceptor API
- [Strict Mode](skills/knockoff-usage/references/strict-mode.md) - Strict mode configuration
- [Moq Migration](skills/knockoff-usage/references/moq-migration.md) - Migration guide

---

**UPDATED:** 2026-02-04
