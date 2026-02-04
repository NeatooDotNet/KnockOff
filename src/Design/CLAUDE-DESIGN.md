# CLAUDE-DESIGN.md

This file provides guidance for Claude Code agents working with the KnockOff Design projects.

## Core Principle

**These files are the source of truth.** When answering questions about KnockOff's API or behavior, always read the relevant Design.Stubs file first. Do not rely on memory or assumptions.

## Quick Reference

### Stub Patterns

| Pattern | Attribute | Example |
|---------|-----------|---------|
| Standalone | `[KnockOff]` on partial class | `CalculatorStub.cs` |
| Inline Interface | `[KnockOff<IInterface>]` | `new Stubs.ICalculator()` |
| Inline Class | `[KnockOff<AbstractClass>]` | `new Stubs.ServiceBase()` |
| Inline Delegate | `[KnockOff<DelegateType>]` | `new Stubs.ArithmeticOperation()` |

### Member Types

| Member | Configuration | Verification |
|--------|---------------|--------------|
| Method | `Returns()`, `OnCall()`, `When()` | `Verify(Times)` |
| Property | `OnGet()`, `OnSet()` | `VerifyGet()`, `VerifySet()` |
| Indexer | `OnGet(key)`, `OnSet(key, value)` | `VerifyGet()`, `VerifySet()` |
| Event | `Handler` property | `VerifyAdd()`, `VerifyRemove()` |

## Common Tasks

### "How does [feature] work?"

1. Navigate to the relevant Design.Stubs file
2. Read the file thoroughly - it contains the answer
3. Look for `DESIGN DECISION` comments for the "why"

### "Can I do [X] with KnockOff?"

1. Search Design.Stubs for related patterns
2. Check `DID NOT DO THIS` sections for explicitly rejected features
3. If not documented, it may not exist

### "Why doesn't [X] work?"

1. Read the `COMMON MISTAKE` comments in related files
2. Check if behavior matches documentation
3. If docs differ from actual behavior, docs may need updating

## File Locations

```
src/Design/Design.Stubs/
  StubPatterns/          # Start here for patterns
  Methods/               # Method stubbing (most common)
  Properties/            # Property stubbing
  Indexers/              # Indexer stubbing
  Events/                # Event stubbing
  UserMethods/           # User-defined methods (base class pattern)
  Advanced/              # Strict mode, Source(), verification
```

## Key Behavioral Gotchas

### 1. Sequences Repeat Last Value

Sequences REPEAT the last callback after exhaustion (NSubstitute-like):

```csharp
// After 2 calls, repeats 999 forever
stub.Add.OnCall((a, b) => 1).ThenCall((a, b) => 999);
// Call 1: 1, Call 2: 999, Call 3+: 999

// Use ThenDefault() to return default(T) instead of repeating
stub.Add.OnCall((a, b) => 1).ThenCall((a, b) => 999).ThenDefault();
// Call 1: 1, Call 2: 999, Call 3+: 0 (default)

// Strict mode always throws on exhaustion
stub.Strict = true;
// Call 3 throws StubException.SequenceExhausted
```

### 2. Events Use Handler Property

Events don't have `Raise()` method. Use `Handler` directly:

```csharp
// Wrong: stub.StartedInterceptor.Raise(...)
// Right:
stub.StartedInterceptor.Handler?.Invoke(sender, args);
```

### 3. Class Stubs Don't Inherit

Inline class stubs implement `IKnockOffStub`, not the base class:

```csharp
// Wrong: ServiceBase service = stub;
// Right:
stub.Object.Initialize();  // Access via Object property
```

### 4. Generic Stub Naming

Closed generic stubs use simple names:

```csharp
// ICollection<string, int> generates:
new Stubs.ICollection();  // NOT Stubs.ICollection<string, int>
```

### 5. Event Interceptor Suffix

Event interceptors have `Interceptor` suffix:

```csharp
stub.StartedInterceptor  // NOT stub.Started
```

### 6. Times.Between Does Not Exist

Use `AtLeast` and `AtMost` instead:

```csharp
// Wrong: Times.Between(1, 5)
// Right:
stub.Add.Verify(Times.AtLeast(1));
stub.Add.Verify(Times.AtMost(5));
```

### 7. User Methods Use Base Class Pattern

User methods (protected overrides in standalone stubs) require `override` keyword and underscore suffix:

```csharp
// Generated base class creates virtual methods with underscore suffix:
// protected virtual string Process_(string input) => default!;

// Your override must use 'override' keyword and '_' suffix:
protected override string Process_(string input) => $"[Processed: {input}]";

// Interceptor uses clean name (no underscore):
stub.Process.Verify(Times.Once);
stub.Process.OnCall(input => "override");  // Supersedes user method
```

## When Updating Documentation

1. **Build first** - Ensure code compiles
2. **Run tests** - `dotnet test` must pass
3. **Update comments** - Keep DESIGN DECISION comments accurate
4. **Preserve intent** - Don't remove documentation, update it

## Design.Tests Reference

115 tests verify documented behavior:

- `PatternTests/` - Pattern instantiation and usage
- `MethodTests/` - Returns, OnCall, sequences, When
- `PropertyTests/` - OnGet, OnSet, sequences
- `IndexerTests/` - Indexer configurations
- `EventTests/` - Event subscription and verification
- `AdvancedTests/` - Source, Strict, Verification, Delegates

If a test fails, the documentation describes the expected behavior. Investigate carefully before changing tests.
