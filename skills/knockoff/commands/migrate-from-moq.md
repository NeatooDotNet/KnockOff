---
description: Convert Moq tests to KnockOff
argument-hint: [file-path]
allowed-tools: Read, Write, Edit, Glob, Grep, AskUserQuestion
---

# Migrate from Moq to KnockOff

**Navigation:** [KnockOff Usage](../skills/knockoff-usage/) > Commands > Migrate from Moq

Convert Moq-based unit tests to use KnockOff stubs. This command analyzes your test files, identifies Moq patterns, and transforms them into equivalent KnockOff code.

## What This Command Does

This automated migration tool:
- Scans for Moq usage in test files
- Analyzes mock declarations, setups, callbacks, and verifications
- Creates KnockOff stub classes (Stand-Alone or Inline pattern)
- Transforms Moq fluent API calls to KnockOff callback syntax
- Updates using statements
- Preserves test behavior while improving readability

## Migration Workflow

### Step 1: Find Moq Usage

If $ARGUMENTS is provided, read that file.
Otherwise, search for Moq usage in the codebase:

```
Grep for: "using Moq" or "new Mock<" or "mock.Setup"
```

Present found files and ask which to migrate.

### Step 2: Analyze Moq Patterns

Read the target file and identify:

1. **Mock declarations**: `var mock = new Mock<IInterface>();`
2. **Setup calls**: `.Setup(x => x.Method()).Returns(value)`
3. **Property setups**: `.Setup(x => x.Property).Returns(value)`
4. **Async setups**: `.ReturnsAsync(value)`
5. **Callbacks**: `.Callback<T>(x => ...)`
6. **Verifications**: `.Verify(x => x.Method(), Times.Once())`
7. **Verifiable chains**: `.Verifiable()` + `mock.Verify()`

### Step 3: Create KnockOff Stubs

For each `Mock<IInterface>` found:

1. Determine if stub already exists
2. If not, create using Stand-Alone or Inline pattern
3. For Stand-Alone: Create `{InterfaceName}Stub.cs` file
4. For Inline: Add `[KnockOff<IInterface>]` to test class

### Step 4: Transform Patterns

Apply these transformations:

#### Mock Creation

<!-- snippet: moq-to-knockoff-mock-creation -->
```cs
// MOQ:
var mock = new Mock<IMoqUserRepo>();
IMoqUserRepo moqRepo = mock.Object;

// KNOCKOFF:
var stub = new MoqUserRepoStub();
IMoqUserRepo knockoffRepo = stub;
```
<!-- endSnippet -->

#### Method Setup with Returns

<!-- snippet: moq-to-knockoff-method-returns -->
```cs
// MOQ:
var mock = new Mock<IMoqUserRepo>();
mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(testUser);

// KNOCKOFF:
var stub = new MoqUserRepoStub();
stub.GetUser.OnCall((id) => testUser);
```
<!-- endSnippet -->

#### Property Setup

<!-- snippet: moq-to-knockoff-property-setup -->
```cs
// MOQ:
var mock = new Mock<IMoqUserRepo>();
mock.Setup(x => x.ConnectionString).Returns("server=localhost");

// KNOCKOFF:
var stub = new MoqUserRepoStub();
stub.ConnectionString.OnGet("server=localhost");
```
<!-- endSnippet -->

#### Async Methods

<!-- snippet: moq-to-knockoff-async-methods -->
```cs
// MOQ:
var mock = new Mock<IMoqUserRepo>();
mock.Setup(x => x.GetUserAsync(It.IsAny<int>())).ReturnsAsync(testUser);

// KNOCKOFF:
var stub = new MoqUserRepoStub();
stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(testUser));
```
<!-- endSnippet -->

#### Callbacks

<!-- snippet: moq-to-knockoff-callbacks -->
```cs
// MOQ:
var mock = new Mock<IMoqUserRepo>();
mock.Setup(x => x.SaveUser(It.IsAny<User>()))
    .Callback<User>(u => moqSavedUsers.Add(u));

// KNOCKOFF:
var stub = new MoqUserRepoStub();
stub.SaveUser.OnCall((user) => knockoffSavedUsers.Add(user));
```
<!-- endSnippet -->

#### Verification

<!-- snippet: moq-to-knockoff-verification -->
```cs
// MOQ:
var mock = new Mock<IMoqUserRepo>();
mock.Object.SaveUser(new User { Name = "Bob" });
mock.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Once());

// KNOCKOFF (batch verification):
var stub = new MoqUserRepoStub();
stub.SaveUser.OnCall((user) => { }).Verifiable();
((IMoqUserRepo)stub).SaveUser(new User { Name = "Bob" });
stub.Verify();

// KNOCKOFF (individual verification):
var stub2 = new MoqUserRepoStub();
var tracking = stub2.SaveUser.OnCall((user) => { });
((IMoqUserRepo)stub2).SaveUser(new User { Name = "Bob" });
tracking.Verify(Times.Once);
```
<!-- endSnippet -->

#### Argument Matching

<!-- snippet: moq-to-knockoff-argument-matching -->
```cs
// MOQ:
var mock = new Mock<IMoqUserRepo>();
mock.Setup(x => x.GetUser(It.Is<int>(id => id > 0)))
    .Returns<int>(id => new User { Id = id, Name = "Valid" });

// KNOCKOFF:
var stub = new MoqUserRepoStub();
stub.GetUser.OnCall((id) =>
    id > 0 ? new User { Id = id, Name = "Valid" } : null);
```
<!-- endSnippet -->

#### Sequence/SetupSequence

<!-- snippet: moq-to-knockoff-sequence-pattern -->
```cs
// MOQ:
var mock = new Mock<IMoqUserRepo>();
mock.SetupSequence(x => x.GetUser(It.IsAny<int>()))
    .Returns(firstUser)
    .Returns(secondUser);

// KNOCKOFF:
var stub = new MoqUserRepoStub();
int callCount = 0;
stub.GetUser.OnCall((id) =>
{
    callCount++;
    return callCount == 1 ? firstUser : secondUser;
});
```
<!-- endSnippet -->

### Step 5: Update Using Statements

<!-- snippet: moq-to-knockoff-using-statements -->
```cs
// BEFORE (Moq):
// using Moq;

// AFTER (KnockOff):
// using KnockOff;
```
<!-- endSnippet -->

### Step 6: Apply Changes

Present the before/after for each transformation.
Ask for confirmation before applying edits.

Use Edit tool to:
1. Update using statements
2. Replace mock declarations with stub declarations
3. Transform Setup calls to OnCall
4. Transform Verify calls
5. Remove `.Object` where using interface stubs directly

### Step 7: Verify Build

After migration, remind the user to:
1. Build the project to trigger source generation
2. Check for any remaining Moq references
3. Run tests to verify behavior is preserved

## Common Issues to Watch For

### Multiple Setups for Same Method

KnockOff uses the last OnCall, not chained setups. Combine logic into a single callback with conditional logic.

### It.IsAny<T>() Patterns

Remove these - KnockOff callbacks receive all arguments naturally through typed parameters.

#### Sequence/SetupSequence

KnockOff has built-in sequence support via `Returns(first, ...rest)`, `ThenReturns()`, `ThenCall()`, and `ThenDefault()`. See the sequence pattern example above.

### Mock<T> Passed to Constructors

Change `mock.Object` to just `stub` (for interfaces) or `stub.Object` (for classes).

## Complete Migration Example

This consolidated example shows a full test migration from Moq to KnockOff:

<!-- snippet: moq-to-knockoff-complete-migration -->
```cs
// ========== MOQ VERSION ==========
var mockRepo = new Mock<IMoqUserRepo>();
var moqService = new UserServiceMigration(mockRepo.Object);

var user = new User { Id = 1, Name = "Alice" };
mockRepo.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);

var moqResult = await moqService.GetUserAsync(1);
mockRepo.Verify(x => x.GetUserAsync(1), Moq.Times.Once());

// ========== KNOCKOFF VERSION ==========
var stub = new MoqUserRepoStub();
var knockoffService = new UserServiceMigration(stub);

stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(user)).Verifiable();

var knockoffResult = await knockoffService.GetUserAsync(1);
stub.Verify();
```
<!-- endSnippet -->

---

**UPDATED:** 2026-01-25
