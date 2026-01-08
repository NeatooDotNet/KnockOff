# Skill Documentation Gaps

## Overview

This document tracks gaps in the Claude Code skill file (`~/.claude/skills/knockoff/SKILL.md`) that were identified during a Neatoo migration evaluation on 2026-01-07.

## Task List

- [ ] Add Class Stubs documentation to skill
- [ ] Add comprehensive delegate stubs section with examples
- [ ] Update skill sync status after documentation complete

## Gap 1: Missing Class Stubs Documentation

**Severity:** High - Class stubs are a major v10.8.0 feature

The skill documents:
- Explicit stubs (`[KnockOff]` on interface implementations)
- Inline stubs (`[KnockOff<TInterface>]`)
- Delegate stubs (`[KnockOff<TDelegate>]`)

**Missing:** Class stubs via `[KnockOff<TClass>]`

### What Should Be Added

The skill should document:

1. **Basic usage pattern:**
```csharp
public class UserService
{
    public virtual string Name { get; set; }
    public virtual User GetUser(int id) => LoadFromDb(id);
}

[KnockOff<UserService>]
public partial class ServiceTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.UserService();
        stub.Interceptor.GetUser.OnCall = (ko, id) => new User { Id = id };

        UserService service = stub;  // Full substitutability
        Assert.Equal(42, service.GetUser(42).Id);
    }
}
```

2. **Key differences from interface stubs:**
   - Access via `stub.Interceptor.{Member}` (not `stub.IInterface.{Member}`)
   - Only virtual/abstract members intercepted
   - Default behavior delegates to base class
   - Full substitutability (stub IS-A target class)

3. **Constructor chaining:**
```csharp
var stub = new Stubs.UserService(dependency);  // Chains to base constructor
```

4. **Limitations:**
   - Cannot stub sealed classes (KO2001)
   - Non-virtual members use base behavior (KO2003 info)
   - Requires accessible constructors (KO2002)

### Source Reference

Design document: `docs/todos/completed/class-stubs-design.md`
Release notes: `docs/release-notes/v10.8.0.md`

## Gap 2: Delegate Stubs Could Be More Comprehensive

**Severity:** Medium

The skill mentions delegate stubs briefly in the "Delegate Stubs" section, but could benefit from:

1. **More examples for multi-parameter delegates:**
```csharp
public delegate Task<bool> IsUniqueName(Guid? id, string firstName, string lastName);

[KnockOff<IsUniqueName>]
public partial class Tests
{
    [Fact]
    public async Task Test()
    {
        var stub = new Stubs.IsUniqueName();
        stub.Interceptor.OnCall = (ko, id, firstName, lastName) =>
            Task.FromResult(firstName != "duplicate");

        IsUniqueName check = stub;
        Assert.True(await check(null, "unique", "name"));

        // Verify with named tuple args
        var args = stub.Interceptor.LastCallArgs;
        Assert.Equal("unique", args?.firstName);
        Assert.Equal("name", args?.lastName);
    }
}
```

2. **Async delegate patterns:**
```csharp
// Task<T> return
stub.Interceptor.OnCall = (ko, args...) => Task.FromResult(result);

// Task return (void async)
stub.Interceptor.OnCall = (ko, args...) => Task.CompletedTask;
```

3. **Common Neatoo patterns:**
   - Validation rule delegates
   - Factory delegates

## Gap 3: Skill Sync Status Outdated

The skill mentions it's sourced from `src/Tests/KnockOff.Documentation.Samples/Skills/`, but the class stubs feature doesn't have corresponding skill samples yet.

### Action Required

1. Create skill samples for class stubs in `Skills/ClassStubsSamples.cs`
2. Run `.\scripts\extract-snippets.ps1 -Update`
3. Add class stubs section to SKILL.md

## Priority

1. **High:** Class stubs documentation (major feature missing)
2. **Medium:** Delegate stubs enhancements
3. **Low:** Sync status update (documentation task)
