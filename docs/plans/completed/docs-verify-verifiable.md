# Plan: Update Documentation Samples to Use Verify()/Verifiable() API

## Related Todo

- [docs-verify-verifiable.md](../../todos/completed/docs-verify-verifiable.md)

## Objective

Update all documentation samples to prefer `Verify()` and `Verifiable()` over direct `CallCount` assertions. The only exception is samples specifically demonstrating call counting features.

## Verification API Quick Reference

### Old Pattern (Replace)
```csharp
Assert.Equal(1, tracking.CallCount);
Assert.True(tracking.CallCount >= 2);
```

### New Patterns (Use These)
```csharp
// Direct verification (throws on failure)
tracking.Verify();                    // At least once
tracking.Verify(Times.Once);          // Exactly once
tracking.Verify(Times.Exactly(3));    // Exactly N times
tracking.Verify(Times.AtLeast(2));    // At least N
tracking.Verify(Times.Never);         // Never called

// Marked for batch verification
stub.Method.OnCall(cb).Verifiable();              // Default: AtLeastOnce
stub.Method.OnCall(cb).Verifiable(Times.Once);    // With constraint
stub.Verify();                                     // Check all marked

// Verify all configured
stub.VerifyAll();  // Everything with OnCall/Value
```

## Implementation Steps

### Phase 1: Core Samples

Update the most visible documentation samples first.

#### 1.1 GettingStartedSamples.cs
- Replace `Assert.Equal(1, tracking.CallCount)` with `.Verifiable()` + `stub.Verify()`
- Show the fluent pattern as the default approach
- Keep samples simple and beginner-friendly

#### 1.2 ReadmeSamples.cs
- Update README quick start examples
- Use `Verifiable()` to show the recommended pattern
- Minimal, clean examples

#### 1.3 VerificationSamples.cs
- This is the primary verification guide
- Show all verification patterns: `Verify()`, `Verifiable()`, `VerifyAll()`
- Include `Times` constraints
- Only use `CallCount` when specifically demonstrating that feature

### Phase 2: Guide Samples

#### 2.1 MethodsSamples.cs
- Replace verification assertions with `Verify()`
- Show `Verifiable()` for method call tracking

#### 2.2 PropertiesSamples.cs
- Update property get/set verification
- Use `Verify()` for property access tracking

#### 2.3 AsyncSamples.cs
- Update async method verification
- Show `Verifiable()` with async patterns

#### 2.4 EventsSamples.cs
- Update event subscription verification

#### 2.5 GenericMethodsSamples.cs
- Update generic method verification

#### 2.6 AdvancedCallbacksSamples.cs
- Update complex callback verification

#### 2.7 PatternsSamples.cs
- Update all three pattern examples (Stand-Alone, Inline Interface, Inline Class)
- Each pattern should demonstrate `Verifiable()`

### Phase 3: Reference Samples

#### 3.1 InterceptorApiSamples.cs
- Complete API reference examples
- Show all `Times` constraints

#### 3.2 AttributeOptionsSamples.cs
- Update attribute configuration samples

#### 3.3 UserMethodsSamples.cs
- Update user-defined method samples

### Phase 4: Migration Samples

#### 4.1 MoqMigrationSamples.cs
- Critical: Show Moq's `.Verify()` → KnockOff's `.Verify()`
- Emphasize the API similarity
- Update all comparison examples

## Transformation Rules

### Rule 1: Simple Call Count = 1
**Before:**
```csharp
var tracking = stub.Method.OnCall((ko, x) => result);
sut.DoSomething();
Assert.Equal(1, tracking.CallCount);
```

**After:**
```csharp
stub.Method.OnCall((ko, x) => result).Verifiable();
sut.DoSomething();
stub.Verify();
```

### Rule 2: Specific Count
**Before:**
```csharp
Assert.Equal(3, tracking.CallCount);
```

**After:**
```csharp
tracking.Verify(Times.Exactly(3));
```

### Rule 3: At Least N
**Before:**
```csharp
Assert.True(tracking.CallCount >= 2);
```

**After:**
```csharp
tracking.Verify(Times.AtLeast(2));
```

### Rule 4: Never Called
**Before:**
```csharp
Assert.Equal(0, tracking.CallCount);
```

**After:**
```csharp
tracking.Verify(Times.Never);
```

### Rule 5: Multiple Verifications
**Before:**
```csharp
Assert.Equal(1, stub.Method1.CallCount);
Assert.Equal(1, stub.Method2.CallCount);
Assert.Equal(1, stub.Method3.CallCount);
```

**After:**
```csharp
stub.Method1.OnCall(cb1).Verifiable();
stub.Method2.OnCall(cb2).Verifiable();
stub.Method3.OnCall(cb3).Verifiable();
// ... act ...
stub.Verify();
```

## When to Keep CallCount

Only use `CallCount` when:
1. Demonstrating the `CallCount` property itself
2. Showing raw call counting for logging/debugging purposes
3. Complex assertions not covered by `Times` constraints

## All Three Patterns

Every sample file should demonstrate all three patterns where applicable:

### Stand-Alone Pattern
```csharp
[KnockOff]
public partial class UserServiceStub : IUserService { }

var stub = new UserServiceStub();
stub.GetUser.OnCall((ko, id) => user).Verifiable();
// ... act ...
stub.Verify();
```

### Inline Interface Pattern
```csharp
[KnockOff<IUserService>]
public partial class UserServiceStub { }

var stub = new UserServiceStub();
stub.GetUser.OnCall((ko, id) => user).Verifiable();
// ... act ...
stub.Verify();
```

### Inline Class Pattern
```csharp
[KnockOff<UserService>]
public partial class UserServiceStub { }

var stub = new UserServiceStub();
stub.GetUser.OnCall((ko, id) => user).Verifiable();
// ... act ...
stub.Verify();
```

## Testing

After each file is updated:
1. Build the solution to verify samples compile
2. Run tests in `KnockOff.Documentation.Samples` project
3. Run MarkdownSnippets to sync documentation

## Success Criteria

- [x] All 14 sample files updated (11 files had CallCount usage)
- [x] Verify()/Verifiable() used instead of CallCount = 1
- [x] All three patterns demonstrated
- [x] All tests pass (131 tests passed)
- [ ] MarkdownSnippets synced (needs manual run)
- [x] Documentation reads naturally
