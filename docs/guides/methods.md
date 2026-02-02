[Guides](../docs/guides) > Method Interceptors

# Method Interceptors

Method interceptors track calls, capture arguments, and configure return values for interface methods in your stub. Each method on the stubbed interface gets a corresponding interceptor property that provides verification and configuration capabilities.

**Key concept**: The `OnCall` callback receives only the method's parameters—you configure behavior based on the inputs to the method being called.

---

## Configuring Method Behavior

### Void Methods

Configure void methods using `OnCall` with an `Action` that matches the method parameters:

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

Configure methods that return values using `OnCall`. You have two options:

**1. Callback syntax** - Use a `Func` for dynamic values or conditional logic:

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

**2. Value syntax** - Pass the return value directly for fixed results:

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

Choose the value syntax when returning a constant, or the callback syntax when you need to inspect parameters or apply logic.

### Methods with Multiple Parameters

The callback signature includes all method parameters in the same order:

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

KnockOff provides two verification patterns:
1. **Individual tracking**: Store the object returned by `OnCall` and call `.Verify()` on it
2. **Batch verification**: Mark interceptors with `.Verifiable()` then call `stub.Verify()` once

The tracking object returned by `OnCall` provides access to `Verify()`, `LastArg`/`LastArgs`, and call count information.

### Using Verify()

Call `.Verify()` on the tracking object returned by `OnCall` to verify that specific method was called:

<!-- snippet: methods-verify-wascalled -->
```cs
stub.Save.OnCall((entity) => { }).Verifiable();

ISaveRepoMethods repository = stub;
repository.Save(new User { Id = 1 });

// Verify() checks all members marked with .Verifiable()
stub.Verify();
```
<!-- endSnippet -->

### Verifying Call Frequency

Use `Times` to specify exact call count requirements. Available options include `Once`, `Never`, `AtLeastOnce`, and `Exactly(n)`:

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

### Using Verifiable()

For batch verification of multiple methods, mark each with `.Verifiable()` then call `stub.Verify()` once to check all:

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

## Overloaded Methods

When an interface has overloaded methods, KnockOff generates numbered suffixes for each overload:

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

Overloads are numbered in the order they appear in the interface definition.

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

This is useful when reusing a stub instance across multiple test phases or assertions.

---

## Sequences

Use sequences when a method should behave differently across multiple calls. KnockOff provides two approaches:

1. **Params syntax** (recommended for constant values) - `Returns(first, params rest)` creates a sequence in a single call
2. **Callback chaining** (for dynamic values) - Chain `ThenCall()` or `ThenReturns()` after `OnCall()`

### Concise Value Sequences (Params Syntax)

For constant value sequences, use the concise params syntax:

<!-- snippet: methods-sequence-params -->
```cs
// NSubstitute-style concise syntax: Returns(first, params rest)
stub.GetValue.Returns(1, 2, 3);

IValueSvc service = stub;

Assert.Equal(1, service.GetValue());
Assert.Equal(2, service.GetValue());
Assert.Equal(3, service.GetValue());

// After exhaustion: repeats last value (NSubstitute behavior)
Assert.Equal(3, service.GetValue());
```
<!-- endSnippet -->

This matches NSubstitute's `Returns(x, y, z)` syntax for easy migration. The sequence repeats the last value after exhaustion.

### Async Methods with Params

Async methods auto-wrap values - no `Task.FromResult` needed:

<!-- snippet: methods-sequence-params-async -->
```cs
// Async methods auto-wrap values - no Task.FromResult needed
stub.GetDataAsync.Returns("first", "second", "third");

IDataSvc service = stub;

Assert.Equal("first", await service.GetDataAsync(1));
Assert.Equal("second", await service.GetDataAsync(2));
Assert.Equal("third", await service.GetDataAsync(3));

// After exhaustion: repeats last value
Assert.Equal("third", await service.GetDataAsync(4));
```
<!-- endSnippet -->

### Mixing Callbacks with Value Params

Use `OnCall()` for the first callback, then `ThenReturns()` with params for subsequent values:

<!-- snippet: methods-sequence-callback-then-params -->
```cs
// OnCall for computed first value, then params for constants
stub.Calculate
    .OnCall((x, y) => x + y)      // First call: compute x + y
    .ThenReturns(100, 200, 300);  // Then: constant values

ICalculatorSvc calc = stub;

Assert.Equal(8, calc.Calculate(5, 3));   // 5 + 3 = 8 (computed)
Assert.Equal(100, calc.Calculate(0, 0)); // constant
Assert.Equal(200, calc.Calculate(0, 0)); // constant
Assert.Equal(300, calc.Calculate(0, 0)); // constant
Assert.Equal(300, calc.Calculate(0, 0)); // repeats last
```
<!-- endSnippet -->

### Callback Sequences

For callback sequences or mixed sequences with dynamic values, chain `ThenCall()` after `OnCall()`:

<!-- snippet: methods-sequence-basic -->
```cs
// Configure different returns for successive calls
stub.GetStatus
    .OnCall(() => "Pending")
    .ThenCall(() => "Processing")
    .ThenCall(() => "Complete");

IStatusSvc service = stub;

// Each call returns the next value in sequence
Assert.Equal("Pending", service.GetStatus());
Assert.Equal("Processing", service.GetStatus());
Assert.Equal("Complete", service.GetStatus());
```
<!-- endSnippet -->

Each callback in the sequence is invoked exactly once in order.

### Void Method Sequences

Sequences work with void methods using `Action` callbacks:

<!-- snippet: methods-sequence-void -->
```cs
// Void method sequences use Action callbacks
var calls = new List<string>();
stub.Notify
    .OnCall((msg) => calls.Add("first"))
    .ThenCall((msg) => calls.Add("second"))
    .ThenCall((msg) => calls.Add("third"));

INotifierSvc notifier = stub;

notifier.Notify("a");
notifier.Notify("b");
notifier.Notify("c");

Assert.Equal(new[] { "first", "second", "third" }, calls);
```
<!-- endSnippet -->

### Return Method Sequences

Sequences with return values use `Func` callbacks:

<!-- snippet: methods-sequence-return -->
```cs
// Return method sequences use Func callbacks
stub.Calculate
    .OnCall((x, y) => x + y)
    .ThenCall((x, y) => x * y)
    .ThenCall((x, y) => x - y);

ICalculatorSvc calc = stub;

// 5 + 3 = 8
Assert.Equal(8, calc.Calculate(5, 3));

// 5 * 3 = 15
Assert.Equal(15, calc.Calculate(5, 3));

// 5 - 3 = 2
Assert.Equal(2, calc.Calculate(5, 3));
```
<!-- endSnippet -->

The callback signature matches the method signature, just like `OnCall()`.

### Sequence Exhaustion

After the sequence is exhausted (all callbacks consumed), subsequent calls **repeat the last value** by default. This matches NSubstitute's behavior for easier migration and more forgiving tests.

<!-- snippet: methods-sequence-exhaustion -->
```cs
// Sequence callbacks run once each in order
stub.GetValue
    .OnCall(() => 1)
    .ThenCall(() => 2)
    .ThenCall(() => 3);

IValueSvc service = stub;

Assert.Equal(1, service.GetValue());
Assert.Equal(2, service.GetValue());
Assert.Equal(3, service.GetValue());

// After exhaustion: repeats last value in non-strict mode (NSubstitute behavior)
Assert.Equal(3, service.GetValue());
Assert.Equal(3, service.GetValue());
```
<!-- endSnippet -->

### Returning Default After Exhaustion

Use `ThenDefault()` when you want the sequence to return `default(T)` after exhaustion instead of repeating the last value:

<!-- snippet: methods-sequence-then-default -->
```cs
// ThenDefault() returns default(T) after exhaustion instead of repeating
stub.GetValue
    .OnCall(() => 1)
    .ThenCall(() => 2)
    .ThenDefault();

IValueSvc service = stub;

Assert.Equal(1, service.GetValue());
Assert.Equal(2, service.GetValue());

// After exhaustion with ThenDefault: returns default(int) = 0
Assert.Equal(0, service.GetValue());
Assert.Equal(0, service.GetValue()); // continues returning default
```
<!-- endSnippet -->

### Strict Mode Sequence Exhaustion

In strict mode, exhausted sequences throw `StubException.SequenceExhausted` regardless of `ThenDefault()`:

<!-- snippet: methods-sequence-strict -->
```cs
// Strict mode throws on sequence exhaustion
stub.Strict = true;

stub.GetValue
    .OnCall(() => 1)
    .ThenCall(() => 2);

IValueSvc service = stub;

Assert.Equal(1, service.GetValue());
Assert.Equal(2, service.GetValue());

// Third call throws StubException.SequenceExhausted in strict mode
Assert.Throws<StubException>(() => service.GetValue());
```
<!-- endSnippet -->

### Mixing Fixed Values and Dynamic Callbacks

You can mix fixed values and dynamic callbacks in the same sequence using `OnCall()`:

<!-- snippet: methods-sequence-mixed -->
```cs
// Mix fixed values with dynamic callbacks using OnCall
stub.GetStatus
    .OnCall(() => "Initial")
    .ThenCall(() => DateTime.Now.ToString("HH:mm:ss"))
    .ThenCall(() => "Final");

IStatusSvc service = stub;

// First call: fixed value
Assert.Equal("Initial", service.GetStatus());

// Second call: dynamic value (time)
var timeResult = service.GetStatus();
Assert.Matches(@"\d{2}:\d{2}:\d{2}", timeResult);

// Third call: fixed value
Assert.Equal("Final", service.GetStatus());
```
<!-- endSnippet -->

**Note:** Use `OnCall(() => value)` to include fixed values in a sequence chain.

### Sequence Verification

Sequences can be verified like any other callback configuration:

<!-- snippet: methods-sequence-verification -->
```cs
// Sequence can be verified like any callback
var sequence = stub.Process
    .OnCall(() => { })
    .ThenCall(() => { })
    .ThenCall(() => { });

IProcessSvc processor = stub;
processor.Process();
processor.Process();
processor.Process();

// Verify sequence was exhausted
sequence.Verify();
```
<!-- endSnippet -->

### Combining Sequences With Verification

<!-- snippet: methods-sequence-with-times -->
```cs
// Mark sequence for batch verification via stub.Verify()
stub.Process
    .OnCall(() => { })
    .ThenCall(() => { })
    .Verifiable();

IProcessSvc processor = stub;
processor.Process();
processor.Process();

// stub.Verify() checks all Verifiable() sequences completed
stub.Verify();
```
<!-- endSnippet -->

---

## Complete Example

This example demonstrates method configuration, argument capturing, and verification in a realistic scenario. The example assumes a `UserService` class that depends on `ICompleteUserRepo`:

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

## Key Takeaways

- **Configuration options**: Use `OnCall(callback)` for dynamic values or `Returns(value)` for fixed return values
- **OnCall signature**: Callback matches method signature—receives only the method parameters
- **Verification patterns**: Individual tracking with `tracking.Verify(Times)` or batch verification with `.Verifiable()` then `stub.Verify()`
- **Times options**: `Once`, `Never`, `AtLeastOnce`, `Exactly(n)`
- **Argument capture**: `LastArg` for single parameters, `LastArgs` tuple for multiple
- **Overloads**: Configure using fully-typed lambda to distinguish which overload
- **Sequences**: Use `Returns(1, 2, 3)` for constant value sequences (NSubstitute-style); use `ThenCall()` chaining for callback sequences
- **Async auto-wrapping**: Async methods auto-wrap params values - no `Task.FromResult` needed
- **ThenDefault()**: Opt-in to returning `default(T)` after sequence exhaustion instead of repeating
- **Reset**: Clears call count, captured arguments, and removes callbacks

Next: [Property Interceptors](properties.md) for get/set tracking and configuration.

**See also:**
- [Parameter Matching Guide](parameter-matching.md) - Use `When()` to match specific argument values

---

**UPDATED:** 2026-02-02
