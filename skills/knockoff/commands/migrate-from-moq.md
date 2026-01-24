---
description: Convert Moq tests to KnockOff
argument-hint: [file-path]
allowed-tools: Read, Write, Edit, Glob, Grep, AskUserQuestion
---

Migrate Moq-based tests to KnockOff. Follow this workflow:

## Step 1: Find Moq Usage

If $ARGUMENTS is provided, read that file.
Otherwise, search for Moq usage in the codebase:

```
Grep for: "using Moq" or "new Mock<" or "mock.Setup"
```

Present found files and ask which to migrate.

## Step 2: Analyze Moq Patterns

Read the target file and identify:

1. **Mock declarations**: `var mock = new Mock<IInterface>();`
2. **Setup calls**: `.Setup(x => x.Method()).Returns(value)`
3. **Property setups**: `.Setup(x => x.Property).Returns(value)`
4. **Async setups**: `.ReturnsAsync(value)`
5. **Callbacks**: `.Callback<T>(x => ...)`
6. **Verifications**: `.Verify(x => x.Method(), Times.Once())`
7. **Verifiable chains**: `.Verifiable()` + `mock.Verify()`

## Step 3: Create KnockOff Stubs

For each `Mock<IInterface>` found:

1. Determine if stub already exists
2. If not, create using Stand-Alone or Inline pattern
3. For Stand-Alone: Create `{InterfaceName}Stub.cs` file
4. For Inline: Add `[KnockOff<IInterface>]` to test class

## Step 4: Transform Patterns

Apply these transformations:

### Mock Creation
```csharp
// Moq
var mock = new Mock<IUserRepo>();
IUserRepo repo = mock.Object;

// KnockOff (Stand-Alone)
var stub = new UserRepoStub();
IUserRepo repo = stub;

// KnockOff (Inline)
var stub = new Stubs.IUserRepo();
IUserRepo repo = stub;
```

### Method Setup with Returns
```csharp
// Moq
mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(user);

// KnockOff
stub.GetUser.OnCall((id) => user);
```

### Property Setup
```csharp
// Moq
mock.Setup(x => x.ConnectionString).Returns("server=localhost");

// KnockOff
stub.ConnectionString.Value = "server=localhost";
```

### Async Methods
```csharp
// Moq
mock.Setup(x => x.GetUserAsync(It.IsAny<int>())).ReturnsAsync(user);

// KnockOff
stub.GetUserAsync.OnCall((id) => Task.FromResult(user));
```

### Callbacks
```csharp
// Moq
mock.Setup(x => x.SaveUser(It.IsAny<User>()))
    .Callback<User>(u => savedUsers.Add(u));

// KnockOff
stub.SaveUser.OnCall((user) => {
    savedUsers.Add(user);
});
```

### Verification
```csharp
// Moq
mock.Verify(x => x.SaveUser(It.IsAny<User>()), Times.Once());

// KnockOff - Option 1: With tracking
var tracking = stub.SaveUser.OnCall((user) => { }).Verifiable();
// ... after act ...
tracking.Verify(Times.Once);

// KnockOff - Option 2: Batch verify
stub.SaveUser.OnCall((user) => { }).Verifiable();
// ... after act ...
stub.Verify();
```

### Argument Matching
```csharp
// Moq
mock.Setup(x => x.GetUser(It.Is<int>(id => id > 0)))
    .Returns<int>(id => new User { Id = id });

// KnockOff - conditional logic in callback
stub.GetUser.OnCall((id) =>
    id > 0 ? new User { Id = id } : null);
```

## Step 5: Update Using Statements

Replace:
```csharp
using Moq;
```

With:
```csharp
using KnockOff;
```

## Step 6: Apply Changes

Present the before/after for each transformation.
Ask for confirmation before applying edits.

Use Edit tool to:
1. Update using statements
2. Replace mock declarations with stub declarations
3. Transform Setup calls to OnCall
4. Transform Verify calls
5. Remove `.Object` where using interface stubs directly

## Step 7: Verify Build

After migration, remind the user to:
1. Build the project to trigger source generation
2. Check for any remaining Moq references
3. Run tests to verify behavior is preserved

## Common Issues to Watch For

**Multiple setups for same method:**
KnockOff uses last OnCall, not chained setups. Combine logic into single callback.

**It.IsAny<T>() patterns:**
Remove these - KnockOff callbacks receive all arguments naturally.

**Sequence/SetupSequence:**
KnockOff doesn't have built-in sequence support. Use stateful callbacks:
```csharp
var callCount = 0;
stub.GetUser.OnCall((id) => {
    callCount++;
    return callCount == 1 ? user1 : user2;
});
```

**Mock<T> passed to constructors:**
Change `mock.Object` to just `stub` (for interfaces) or `stub.Object` (for classes).
