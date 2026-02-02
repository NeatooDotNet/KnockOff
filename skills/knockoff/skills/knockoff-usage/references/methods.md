# Method Interceptors Reference

Method interceptors track calls, capture arguments, and configure return values for interface methods in your stub. Each method on the stubbed interface gets a corresponding interceptor property that provides verification and configuration capabilities.

---

## Configuring Method Behavior

### Void Methods

Configure void methods using `OnCall` with an `Action`:

<!-- snippet: methods-oncall-void -->
```cs
// OnCall for void methods uses Action<...params>
var logged = new List<string>();
var tracking = stub.LogMessage.OnCall((message) =>
{
    logged.Add(message);
});

ILogSvcMethods logger = stub;
logger.LogMessage("Hello, World!");

Assert.Single(logged);
Assert.Equal("Hello, World!", logged[0]);
tracking.Verify();
```
<!-- endSnippet -->

### Methods with Return Values

#### Using a Callback

Configure methods that return values using `OnCall` with a `Func`:

<!-- snippet: methods-oncall-return -->
```cs
// OnCall with return value: Func<...params, TReturn>
var tracking = stub.GetUserName.OnCall((userId) => "TestUser");

ILogSvcMethods logger = stub;
var name = logger.GetUserName(42);

Assert.Equal("TestUser", name);
tracking.Verify();
```
<!-- endSnippet -->

#### Using a Fixed Value

For simple scenarios where the return value does not depend on arguments, use the value overload:

<!-- snippet: methods-oncall-value -->
```cs
// Returns - simpler syntax when you don't need callback logic
// Just pass the return value directly
var tracking = stub.GetUserName.Returns("StaticUser");

ILogSvcMethods logger = stub;
var name = logger.GetUserName(42);

Assert.Equal("StaticUser", name);
tracking.Verify();
```
<!-- endSnippet -->

#### When to Use Value, Params Sequence, or Callback

<!-- snippet: methods-oncall-value-vs-callback -->
```cs
// Use VALUE when returning a fixed result:
stub.GetUserName.Returns("Alice");

// Use CALLBACK when you need:
// - Dynamic values based on arguments
// - Side effects
// - Conditional logic
stub.GetUserName.OnCall((userId) => userId > 100 ? "Admin" : "User");

// Both return tracking objects for verification
```
<!-- endSnippet -->

**Params sequences** (NSubstitute-style):

```cs
// Use PARAMS SEQUENCE for multiple constant values:
// Returns 1, then 2, then 3, then repeats 3
stub.Calculate.Returns(1, 2, 3);

// Mix callbacks with params for complex sequences:
// First call uses callback, then returns 100, 200, 300
stub.Calculate.OnCall((x) => x * 2).ThenReturns(100, 200, 300);
```

| Scenario | Recommended Syntax |
|----------|-------------------|
| Fixed value (always same) | `Returns(value)` |
| Constant sequence | `Returns(first, second, ...)` |
| Dynamic based on args | `OnCall((args) => computed)` |
| Callback then constants | `OnCall(cb).ThenReturns(x, y, z)` |

### Methods with Multiple Parameters

Methods with multiple parameters include all parameters in the callback:

<!-- snippet: methods-oncall-multi-param -->
```cs
// All method parameters are passed to the callback in order
var tracking = stub.ValidateCredentials.OnCall((username, password) =>
    username == "admin" && password == "secret");

IAuthSvcMethods auth = stub;

Assert.True(auth.ValidateCredentials("admin", "secret"));
Assert.False(auth.ValidateCredentials("user", "wrong"));

// Verify exactly 2 calls were made
tracking.Verify(Times.Exactly(2));
```
<!-- endSnippet -->

---

## Verifying Method Calls

### Using Verify()

Call `.Verify()` on the tracking object returned by `OnCall`:

<!-- snippet: methods-verify-wascalled -->
```cs
stub.Save.OnCall((entity) => { }).Verifiable();

ISaveRepoMethods repository = stub;
repository.Save(new User { Id = 1 });

// Verify() checks all members marked with .Verifiable()
stub.Verify();
```
<!-- endSnippet -->

### Verifying Call Frequency with Times

Use `Times` to specify exact call count requirements:

<!-- snippet: methods-verify-callcount -->
```cs
var tracking = stub.Notify.OnCall((message) => { });

INotifierMethods notifier = stub;

// Simulate processing a 2-item collection
var items = new[] { "item1", "item2" };
foreach (var item in items)
{
    notifier.Notify($"Processing {item}");
}

// Verify exactly 2 calls (throws if different)
tracking.Verify(Times.Exactly(2));
```
<!-- endSnippet -->

**Available Times constraints:**

| Constraint | Description |
|------------|-------------|
| `Times.Never` | Method must not be called |
| `Times.Once` | Method must be called exactly once |
| `Times.AtLeastOnce` | Method must be called one or more times |
| `Times.Exactly(n)` | Method must be called exactly n times |

### Using Verifiable() for Batch Verification

For batch verification of multiple methods, use `.Verifiable()` then call `stub.Verify()`:

<!-- snippet: methods-verify-verifiable -->
```cs
// Mark expected calls
stub.Save.OnCall((entity) => { }).Verifiable(Times.Once);
stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();

ISaveRepoMethods repository = stub;
repository.Save(new User { Id = 1 });
repository.GetById(1);

// Verify all marked methods (throws if any not called correctly)
stub.Verify();
```
<!-- endSnippet -->

**Key difference:**
- `tracking.Verify()` - Verifies a single method
- `stub.Verify()` - Verifies all methods marked with `.Verifiable()`

---

## Capturing Arguments

### Single Parameter Methods

Access the last call's argument using `LastArg`:

<!-- snippet: methods-capture-single -->
```cs
var tracking = stub.GetUser.OnCall((userId) => new User { Id = userId });

IUserRepoMethods repository = stub;
repository.GetUser(42);

// LastArg captures the most recent call's argument (from tracking)
int capturedId = tracking.LastArg;
Assert.Equal(42, capturedId);
```
<!-- endSnippet -->

### Multiple Parameter Methods

Access arguments using the `LastArgs` named tuple:

<!-- snippet: methods-capture-multiple -->
```cs
var tracking = stub.ValidateCredentials.OnCall((username, password) => true);

IAuthSvcMethods auth = stub;
auth.ValidateCredentials("admin", "secret123");

// LastArgs is a named tuple with all parameters (from tracking)
var (username, password) = tracking.LastArgs;
Assert.Equal("admin", username);
Assert.Equal("secret123", password);
```
<!-- endSnippet -->

---

## Async Methods

KnockOff provides simplified syntax for async methods, eliminating verbose `Task.FromResult()` and `Task.CompletedTask` wrappers.

### Task<T> and ValueTask<T> Methods

For methods returning `Task<T>` or `ValueTask<T>`, you have three options:

<!-- snippet: async-task-value-overload -->
```cs
[Fact]
public async Task TaskResult_ValueOverload_AutoWraps()
{
    var stub = new AsyncUserSvcStub();

    // RETURNS: KnockOff auto-wraps the value in Task.FromResult
    // This is the simplest syntax for returning async values
    stub.GetUserAsync.Returns(new User { Id = 42, Name = "Alice" });

    IAsyncUserSvc service = stub;
    var user = await service.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

<!-- snippet: async-task-simplified-callback -->
```cs
[Fact]
public async Task TaskResult_SimplifiedCallback_AutoWraps()
{
    var stub = new AsyncUserSvcStub();

    // SIMPLIFIED CALLBACK: Return the unwrapped type, auto-wrapped in Task.FromResult
    // This combines the simplicity of Returns() with callback flexibility
    stub.GetUserAsync.OnCall((id) => new User { Id = id, Name = "Alice" }).Verifiable();

    IAsyncUserSvc service = stub;
    var user = await service.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
    stub.Verify();
}
```
<!-- endSnippet -->

<!-- snippet: async-task-result -->
```cs
[Fact]
public async Task TaskResult_ReturnedWithFromResult()
{
    var stub = new AsyncUserSvcStub();

    // FULL CALLBACK: Use Task.FromResult when you need async operations in the callback
    stub.GetUserAsync.OnCall((id) =>
        Task.FromResult<User?>(new User { Id = id, Name = "Alice" })).Verifiable();

    IAsyncUserSvc service = stub;
    var user = await service.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
    stub.Verify();
}
```
<!-- endSnippet -->

### Void Async Methods (Task/ValueTask)

For methods returning `Task` or `ValueTask` (no result), use `Action` callbacks - KnockOff auto-returns the completed task:

<!-- snippet: async-task-simplified-void -->
```cs
[Fact]
public async Task TaskVoid_SimplifiedCallback_AutoReturnsCompletedTask()
{
    var stub = new AsyncUserSvcStub();

    var updatedUsers = new List<User>();

    // SIMPLIFIED VOID CALLBACK: Just use Action, Task.CompletedTask is auto-returned
    stub.UpdateUserAsync.OnCall((user) => updatedUsers.Add(user)).Verifiable();

    IAsyncUserSvc service = stub;
    await service.UpdateUserAsync(new User { Id = 1, Name = "Bob" });

    Assert.Single(updatedUsers);
    stub.Verify();
}
```
<!-- endSnippet -->

### Async Callback Syntax Decision Guide

| Return Type | Simplest Syntax | When to Use Full Syntax |
|-------------|-----------------|------------------------|
| `Task<T>` | `OnCall((args) => value)` | When callback needs actual async operations |
| `ValueTask<T>` | `OnCall((args) => value)` | When callback needs actual async operations |
| `Task` | `OnCall((args) => { action(); })` | When callback needs to return a specific Task |
| `ValueTask` | `OnCall((args) => { action(); })` | When callback needs to return a specific ValueTask |

---

## Method Sequences

When a method should return different values on successive calls, use sequences. KnockOff supports NSubstitute-style concise syntax for creating value sequences.

### Concise Value Sequences (NSubstitute-style)

Use `Returns(first, ...rest)` to create a sequence from multiple values in a single call:

```cs
// Returns 1 on first call, 2 on second, 3 on third and all subsequent calls
stub.Calculate.Returns(1, 2, 3);

ICalculator calc = stub;
var r1 = calc.Calculate(0);  // 1
var r2 = calc.Calculate(0);  // 2
var r3 = calc.Calculate(0);  // 3
var r4 = calc.Calculate(0);  // 3 (repeats last value)
```

This matches NSubstitute's syntax for easier migration:
- NSubstitute: `substitute.Method().Returns(1, 2, 3);`
- KnockOff: `stub.Method.Returns(1, 2, 3);`

### Single Value vs Params Sequence

C# overload resolution distinguishes between single values and sequences:

```cs
// Single value - repeats indefinitely (no sequence)
stub.Calculate.Returns(42);

// Params sequence - progresses through values, repeats last
stub.Calculate.Returns(1, 2, 3);
```

### Mixing Callbacks with Params

Use `OnCall()` for the first value when you need callback logic, then `ThenReturns(params)` for subsequent constant values:

```cs
// First call: compute dynamically
// Then: return 100, 200, 300 in sequence
stub.Add.OnCall((a, b) => a + b).ThenReturns(100, 200, 300);

ICalculator calc = stub;
var r1 = calc.Add(1, 2);  // 3 (computed: 1 + 2)
var r2 = calc.Add(0, 0);  // 100
var r3 = calc.Add(0, 0);  // 200
var r4 = calc.Add(0, 0);  // 300
var r5 = calc.Add(0, 0);  // 300 (repeats last)
```

### Async Methods with Params Sequences

For `Task<T>` and `ValueTask<T>` methods, params values are auto-wrapped - no `Task.FromResult()` needed:

```cs
// Async values auto-wrapped - no Task.FromResult needed
stub.GetDataAsync.Returns("first", "second", "third");

IDataService service = stub;
var r1 = await service.GetDataAsync(1);  // "first"
var r2 = await service.GetDataAsync(2);  // "second"
var r3 = await service.GetDataAsync(3);  // "third"
var r4 = await service.GetDataAsync(4);  // "third" (repeats)
```

### Sequence Exhaustion Behavior

By default, sequences repeat the last value after exhaustion (matching NSubstitute):

```cs
stub.GetValue.Returns(1, 2);

var r1 = calc.GetValue();  // 1
var r2 = calc.GetValue();  // 2
var r3 = calc.GetValue();  // 2 (repeats last)
var r4 = calc.GetValue();  // 2 (still repeats)
```

For different exhaustion behaviors:

| Behavior | How to Configure |
|----------|-----------------|
| Repeat last value (default) | `Returns(1, 2, 3)` or `OnCall(...).ThenReturns(...)` |
| Return default(T) | `OnCall(...).ThenReturns(...).ThenDefault()` |
| Throw exception | Set `stub.Strict = true` |

### Sequence Verification

Params sequences support verification to ensure all values were consumed:

```cs
var sequence = stub.Calculate.Returns(1, 2, 3);

ICalculator calc = stub;
calc.Calculate(0);  // 1
calc.Calculate(0);  // 2
calc.Calculate(0);  // 3

sequence.Verify();  // Passes - all 3 values used
```

---

## Handling Overloaded Methods

When an interface has overloaded methods, KnockOff distinguishes them by the callback signature. The fully-typed lambda tells KnockOff which overload to configure:

<!-- snippet: methods-overloads -->
```cs
// Overloads are distinguished by the callback parameter types
// The fully-typed lambda tells KnockOff which overload to configure
var findAllTracking = stub.Find.OnCall(() =>
    new List<User>()).Verifiable();
var findByIdTracking = stub.Find.OnCall((int id) =>
    new User { Id = id, Name = "ById" }).Verifiable();
var findByNameTracking = stub.Find.OnCall((string name) =>
    new User { Id = 1, Name = name }).Verifiable();

ISearchRepo repo = stub;

// Call each overload
repo.Find();
repo.Find(42);
repo.Find("Alice");

// Verify all overloads were called
stub.Verify();

// Access last arguments via tracking objects
Assert.Equal(42, findByIdTracking.LastArg);
Assert.Equal("Alice", findByNameTracking.LastArg);
```
<!-- endSnippet -->

**Important:** The callback signature determines which overload is configured. Use explicit types in lambdas when parameter types are ambiguous.

---

## Resetting Interceptors

Clear tracking state and remove callbacks using `Reset()`:

<!-- snippet: methods-reset -->
```cs
var tracking = stub.ProcessData.OnCall((data) => { });

IProcessorMethods processor = stub;
processor.ProcessData("initial");

// Verify one call was made
tracking.Verify(Times.Once);

// Reset clears tracking state on the interceptor
stub.ProcessData.Reset();

// After reset, Verify(Times.Never) passes via tracking
tracking.Verify(Times.Never);
```
<!-- endSnippet -->

**Use cases for Reset():**
- Reusing a stub instance across multiple test phases
- Testing a sequence of interactions where counts should restart
- Isolating assertions between test setup and execution phases

---

## Complete Example

This example demonstrates a realistic test using method configuration, execution, and verification:

<!-- snippet: methods-complete-example -->
```cs
// Arrange
var stub = new CompleteUserRepoStub();

var testUser = new User { Id = 1, Name = "Alice", Email = "old@test.com" };
var getTracking = stub.GetUser.OnCall((id) => id == 1 ? testUser : null).Verifiable();
var saveTracking = stub.SaveUser.OnCall((user) => { }).Verifiable();

var service = new UserService(stub);

// Act
var result = service.UpdateUserEmail(1, "new@test.com");

// Assert
Assert.True(result);

// Verify both methods were called
stub.Verify();

// Verify GetUser was called with correct ID
Assert.Equal(1, getTracking.LastArg);

// Verify saved user has new email via the tracking args
var savedUser = saveTracking.LastArg;
Assert.Equal("new@test.com", savedUser.Email);
```
<!-- endSnippet -->

---

## Quick Reference

| Task | Code |
|------|------|
| Configure void method | `stub.Method.OnCall((args) => { })` |
| Configure method with callback | `stub.Method.OnCall((args) => returnValue)` |
| Configure method with value | `stub.Method.Returns(fixedValue)` |
| Create value sequence (NSubstitute-style) | `stub.Method.Returns(1, 2, 3)` |
| Mix callback with value sequence | `stub.Method.OnCall(cb).ThenReturns(x, y, z)` |
| Configure async Task<T> (simplified) | `stub.AsyncMethod.Returns(value)` |
| Configure async Task<T> sequence | `stub.AsyncMethod.Returns(v1, v2, v3)` |
| Configure async Task (void, simplified) | `stub.AsyncMethod.OnCall((args) => { action(); })` |
| Verify method was called | `tracking.Verify()` |
| Verify call count | `tracking.Verify(Times.Exactly(n))` |
| Mark for batch verify | `stub.Method.OnCall(...).Verifiable()` |
| Batch verify all | `stub.Verify()` |
| Get last single arg | `tracking.LastArg` |
| Get last multiple args | `tracking.LastArgs` (named tuple) |
| Reset interceptor | `stub.Method.Reset()` |

---

## Key Takeaways

- **OnCall signature**: Callback receives only the method parameters
- **Value vs Callback**: Use `Returns(value)` for fixed returns, `OnCall(callback)` for dynamic logic
- **Params sequences**: Use `Returns(1, 2, 3)` for concise value sequences (matches NSubstitute)
- **Sequence exhaustion**: Last value repeats after exhaustion (NSubstitute-like behavior)
- **Async auto-wrapping**: Params values auto-wrap for `Task<T>` and `ValueTask<T>` - no `Task.FromResult()` needed
- **Verification**: Use `tracking.Verify(Times)` for single methods or `.Verifiable()` + `stub.Verify()` for batch
- **Arguments**: `LastArg` for single parameters, `LastArgs` tuple for multiple
- **Overloads**: Distinguished by callback parameter types - use explicit types in lambdas
- **Reset**: Clears call counts and tracking state

---

**UPDATED:** 2026-02-02
