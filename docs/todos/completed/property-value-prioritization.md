# Property Value Prioritization

## Summary

KnockOff properties support two configuration patterns:
- `stub.Property.Value = x` - Simple, direct value assignment
- `stub.Property.OnGet = (ko) => x` - Callback for dynamic/conditional behavior

Currently, documentation and skills may over-emphasize `OnGet` callbacks. This todo tracks updating all materials to **prioritize `Value` for simple cases** while explaining `OnGet` as the power-user option for dynamic behavior.

## Rationale

Most test setups need static values. Forcing callbacks for simple cases is verbose:

```csharp
// Good - simple and clear
stub.IsEnabled.Value = true;

// Unnecessary verbosity for static value
stub.IsEnabled.OnGet = (ko) => true;
```

Reserve `OnGet` for when you actually need dynamic behavior:
- Conditional returns based on stub state
- Sequences of different values
- Computed values

## Task List

### 1. Audit Current State
- [x] Review existing KnockOff tests for property usage patterns
- [x] Review current documentation for property examples
- [x] Review KnockOff skill for property guidance

### 2. Update Tests
- [x] Identify tests using `OnGet` where `Value` would be simpler
- [x] Update tests to use `Value` for static values
- [x] Keep `OnGet` examples where dynamic behavior is demonstrated
- [x] Ensure tests pass after changes

### 3. Update Documentation
- [x] Add/update section explaining both property patterns
- [x] Lead with `Value` as the primary/recommended approach
- [x] Show `OnGet` as the option for dynamic scenarios
- [x] Provide clear guidance on when to use each

### 4. Update Documentation Samples
- [x] Review `KnockOff.Documentation.Samples` for property examples
- [x] Update samples to use `Value` for simple cases
- [x] Ensure samples compile and tests pass
- [x] Keep at least one `OnGet` example to demonstrate dynamic use

### 5. Update KnockOff Skill
- [x] Review skill content for property guidance
- [x] Update to prioritize `Value` pattern
- [x] Explain `OnGet` as the dynamic/advanced option
- [x] Ensure examples match the prioritization

### 6. Sync and Verify
- [x] Run `.\scripts\extract-snippets.ps1 -Update`
- [x] Run `.\scripts\extract-snippets.ps1 -Verify`
- [x] Review updated documentation files
- [x] Review updated skill content
- [x] Build and test full solution

## Guidelines for Updates

### Property Pattern Guidance (to include in docs/skill)

**Use `Value` for static test data (recommended):**
```csharp
stub.UserName.Value = "john@example.com";
stub.IsActive.Value = true;
stub.RetryCount.Value = 3;
```

**Use `OnGet` for dynamic behavior:**
```csharp
// Return different values based on call count
var count = 0;
stub.NextId.OnGet = (ko) => ++count;

// Conditional based on other stub state
stub.IsConnected.OnGet = (ko) => ko.Connect.WasCalled;

// Access test context
stub.CurrentUser.OnGet = (ko) => _testFixture.User;
```

### Decision Tree

1. Is the value static for the entire test? → Use `Value`
2. Does the value depend on stub state or call order? → Use `OnGet`
3. Do you need to compute the value at access time? → Use `OnGet`

## Files Likely to Change

- `docs/` - Documentation markdown files
- `src/Tests/KnockOff.Documentation.Samples/` - Sample code
- `src/Tests/KnockOff.Documentation.Samples.Tests/` - Sample tests
- `.claude/skills/knockoff.md` or similar skill file
