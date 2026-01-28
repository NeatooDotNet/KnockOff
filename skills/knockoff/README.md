> **KnockOff Plugin** > README

# KnockOff Plugin for Claude Code

This plugin provides comprehensive assistance for using the KnockOff stub library in your .NET test projects.

## Features

### Skill: KnockOff Usage Knowledge

Automatically activates when you ask about:
- Creating stubs with `[KnockOff]` or `[KnockOff<T>]` attributes
- The three stub patterns (Stand-Alone, Inline Interface, Inline Class)
- Configuring behavior with `OnCall` (callback or value), `OnGet`, `OnSet`
- Sequential callbacks with `OnCallSequence`, `ThenCall`
- Verification with `Verify()`, `Verifiable()`, and `Times` constraints
- Async method handling
- Generic method stubbing with `.Of<T>()`
- Event subscription tracking and raising
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

## Usage Examples

### Create a New Stub

```
/knockoff:create-stub
```

Claude guides you through:
1. Choosing a stub pattern (Stand-Alone, Inline Interface, Inline Class)
2. Selecting the target interface or class
3. Generating the stub class and basic test scaffolding

### Migrate from Moq

```
/knockoff:migrate-from-moq path/to/tests/MyTests.cs
```

Analyzes your Moq tests and transforms them to KnockOff:
- Converts `Mock<T>` to stub classes
- Transforms `.Setup()` calls to `.OnCall()` configuration
- Migrates `.Verify()` calls to KnockOff verification

### Troubleshoot Issues

```
/knockoff:troubleshoot
```

Diagnose and fix common problems:
- Stub not generating
- Compilation errors in generated code
- Interceptor configuration issues
- Verification failures

## Key API Quick Reference

This is a quick reference of KnockOff's core APIs. For complete documentation with examples, see the [API Reference](skills/knockoff-usage/references/api-reference.md).

### OnCall - Two Convenient Overloads

**Callback overload** - Full control with argument-based logic:

<!-- snippet: plugin-readme-oncall-callback -->
```cs
stub.GetUser.OnCall((id) => new User { Id = id, Name = "Dynamic" });
```
<!-- endSnippet -->

**Value overload** - Simpler syntax for fixed return values:

<!-- snippet: plugin-readme-oncall-value -->
```cs
stub.GetUser.Returns(new User { Id = 1, Name = "Alice" });
```
<!-- endSnippet -->

Both overloads return an `IMethodTracking<T>` for verification and argument capture.

### Sequential Callbacks

Configure different return values for successive calls:

<!-- snippet: plugin-readme-sequential -->
```cs
stub.GetValue
    .OnCallSequence(() => 10)
    .ThenCall(() => 20)
    .ThenCall(() => 30)
    .Verifiable();
```
<!-- endSnippet -->

### Verification

**Immediate verification** - Verify a specific member:

<!-- snippet: plugin-readme-verify-immediate -->
```cs
var tracking = stub.Save.OnCall((user) => { });

// ... exercise stub ...
```
<!-- endSnippet -->

**Batch verification** - Mark with `Verifiable()`, then verify all at once:

<!-- snippet: plugin-readme-verify-batch -->
```cs
stub.GetUser.OnCall((id) => new User { Id = id }).Verifiable();
stub.Save.OnCall((user) => { }).Verifiable(Times.Exactly(2));

// ... exercise stub ...
```
<!-- endSnippet -->

### Generic Methods

Use `.Of<T>()` to configure and verify type-specific behavior:

<!-- snippet: plugin-readme-generic-methods -->
```cs
stub.GetById.Of<User>().OnCall((id) => new User { Id = id });
```
<!-- endSnippet -->

## Plugin Documentation

### Skill Reference

The **knockoff-usage** skill provides comprehensive documentation on KnockOff APIs and patterns:

- [Stub Patterns](skills/knockoff-usage/references/patterns.md) - All three patterns with complete examples
- [Methods Guide](skills/knockoff-usage/references/methods.md) - Method interceptor configuration and verification
- [Properties Guide](skills/knockoff-usage/references/properties.md) - Property interceptors (OnGet, OnSet)
- [API Reference](skills/knockoff-usage/references/api-reference.md) - Complete interceptor API
- [Moq Migration](skills/knockoff-usage/references/moq-migration.md) - Step-by-step migration guide

### Project Documentation

For library documentation and guides, see:

- [Project README](../../README.md) - Library overview and quick start
- [Getting Started Guide](../../docs/getting-started.md) - Installation and first usage
- [Interceptor API Reference](../../docs/reference/interceptor-api.md) - Complete API documentation
- [Migration Guides](../../docs/migration/) - Migrating from Moq and NSubstitute

---

**UPDATED:** 2026-01-25
