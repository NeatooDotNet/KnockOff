# Method Interceptors

Method interceptors track calls, capture arguments, and configure return values for interface methods in your stub. Each method on the stubbed interface gets a corresponding interceptor property that provides verification and configuration capabilities.

**Critical concept**: The `OnCall` callback signature always includes the stub instance as the first parameter, followed by the method's parameters. This gives you access to stub state during callback execution.

---

## Configuring Method Behavior

### Void Methods

Configure void methods using `OnCall` with an `Action<TStub>`:

<!-- snippet: methods-oncall-void -->
```cs
[Fact]
public void VoidMethod_ConfiguredWithOnCall()
{
    var stub = new LogSvcMethodsStub();

    // OnCall for void methods uses Action<TStub, ...params>
    var logged = new List<string>();
    var tracking = stub.LogMessage.OnCall((message) =>
    {
        logged.Add(message);
    });

    ILogSvcMethods logger = stub;
    logger.LogMessage("Hello, World!");

    Assert.Single(logged);
    Assert.Equal("Hello, World!", logged[0]);
    Assert.True(tracking.WasCalled);
}
```
<!-- endSnippet -->

### Methods with Return Values

Configure methods that return values using `OnCall` with a `Func<TStub, T, R>`:

<!-- snippet: methods-oncall-return -->
```cs
[Fact]
public void MethodWithReturn_ConfiguredWithOnCall()
{
    var stub = new LogSvcMethodsStub();

    // OnCall callback receives the method parameters
    var tracking = stub.GetUserName.OnCall((userId) => "TestUser");

    ILogSvcMethods logger = stub;
    var name = logger.GetUserName(42);

    Assert.Equal("TestUser", name);
    Assert.True(tracking.WasCalled);
}
```
<!-- endSnippet -->

Notice the stub instance is the first parameter, followed by the method's `userId` parameter.

### Methods with Multiple Parameters

Methods with multiple parameters include all parameters after the stub instance:

<!-- snippet: methods-oncall-multi-param -->
```cs
[Fact]
public void MethodWithMultipleParams_AllAvailableInOnCall()
{
    var stub = new AuthSvcMethodsStub();

    // All method parameters are passed to the callback
    var tracking = stub.ValidateCredentials.OnCall((username, password) =>
        username == "admin" && password == "secret");

    IAuthSvcMethods auth = stub;

    Assert.True(auth.ValidateCredentials("admin", "secret"));
    Assert.False(auth.ValidateCredentials("user", "wrong"));

    // Verify exactly 2 calls were made
    tracking.Verify(Times.Exactly(2));
}
```
<!-- endSnippet -->

---

## Verifying Method Calls

### Using Verify()

The recommended approach is to call `.Verify()` on the tracking object returned by `OnCall`:

<!-- snippet: methods-verify-wascalled -->
```cs
[Fact]
public void Verify_VerifiesMethodInvocation()
{
    var stub = new SaveRepoMethodsStub();
    stub.Save.OnCall((entity) => { }).Verifiable();

    ISaveRepoMethods repository = stub;
    repository.Save(new User { Id = 1 });

    // Verify() checks all members marked with .Verifiable()
    stub.Verify();
}
```
<!-- endSnippet -->

### Verifying Call Frequency

Use `Times` to specify exact call count requirements:

<!-- snippet: methods-verify-callcount -->
```cs
[Fact]
public void Verify_ExactCallCount()
{
    var stub = new NotifierMethodsStub();
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
}
```
<!-- endSnippet -->

### Using Verifiable()

For batch verification of multiple methods, use `.Verifiable()` then call `stub.Verify()`:

<!-- snippet: methods-verify-verifiable -->
```cs
[Fact]
public void Verifiable_BatchVerification()
{
    var stub = new SaveRepoMethodsStub();

    // Mark expected calls
    stub.Save.OnCall((entity) => { }).Verifiable(Times.Once);
    stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();

    ISaveRepoMethods repository = stub;
    repository.Save(new User { Id = 1 });
    repository.GetById(1);

    // Verify all marked methods (throws if any not called correctly)
    stub.Verify();
}
```
<!-- endSnippet -->

---

## Capturing Arguments

### Single Parameter Methods

Access the last call's argument using `LastCallArg`:

<!-- snippet: methods-capture-single -->
```cs
[Fact]
public void LastArg_CapturesSingleParameter()
{
    var stub = new UserRepoMethodsStub();
    var tracking = stub.GetUser.OnCall((userId) => new User { Id = userId });

    IUserRepoMethods repository = stub;
    repository.GetUser(42);

    // LastArg captures the most recent call's argument (from tracking)
    int capturedId = tracking.LastArg;
    Assert.Equal(42, capturedId);
}
```
<!-- endSnippet -->

### Multiple Parameter Methods

Access arguments using the `LastCallArgs` named tuple:

<!-- snippet: methods-capture-multiple -->
```cs
[Fact]
public void LastArgs_CapturesAllParameters()
{
    var stub = new AuthSvcMethodsStub();
    var tracking = stub.ValidateCredentials.OnCall((username, password) => true);

    IAuthSvcMethods auth = stub;
    auth.ValidateCredentials("admin", "secret123");

    // LastArgs is a named tuple with all parameters (from tracking)
    var (username, password) = tracking.LastArgs;
    Assert.Equal("admin", username);
    Assert.Equal("secret123", password);
}
```
<!-- endSnippet -->

---

## Overloaded Methods

When an interface has overloaded methods, KnockOff generates numbered suffixes for each overload:

<!-- snippet: methods-overloads -->
```cs
[Fact]
public void Overloads_DistinguishedByCallbackSignature()
{
    var stub = new SearchRepoStub();

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
}
```
<!-- endSnippet -->

Overloads are numbered in the order they appear in the interface definition.

---

## Resetting Interceptors

Clear tracking state and remove callbacks using `Reset()`:

<!-- snippet: methods-reset -->
```cs
[Fact]
public void Reset_ClearsTrackingState()
{
    var stub = new ProcessorMethodsStub();
    var tracking = stub.ProcessData.OnCall((data) => { });

    IProcessorMethods processor = stub;
    processor.ProcessData("initial");

    // Verify one call was made
    tracking.Verify(Times.Once);

    // Reset clears WasCalled, LastCallArg, and callbacks on the interceptor
    stub.ProcessData.Reset();

    // After reset, Verify(Times.Never) passes via tracking
    tracking.Verify(Times.Never);
}
```
<!-- endSnippet -->

This is useful when reusing a stub instance across multiple test phases or assertions.

---

## Complete Example

This example demonstrates a realistic test using method configuration, execution, and verification:

<!-- snippet: methods-complete-example -->
```cs
[Fact]
public void UserService_UpdateUserEmail_CallsRepositoryCorrectly()
{
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
}
```
<!-- endSnippet -->

---

## Key Takeaways

- **OnCall signature**: Callback receives only the method parameters (no stub instance parameter)
- **Verification**: Use `tracking.Verify(Times)` or `.Verifiable()` + `stub.Verify()`
- **Arguments**: `LastCallArg` for single parameters, `LastCallArgs` tuple for multiple
- **Overloads**: Numbered suffixes (Method1, Method2, ...) in declaration order
- **Reset**: Clears `WasCalled`, arguments, and callbacks

Next: [Property Interceptors](properties.md) for get/set tracking and configuration.
