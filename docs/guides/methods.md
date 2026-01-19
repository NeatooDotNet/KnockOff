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
    var tracking = stub.LogMessage.OnCall((ko, message) =>
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

    // OnCall with return value: first param is stub (ko), then method params
    var tracking = stub.GetUserName.OnCall((ko, userId) => "TestUser");

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

    // All method parameters follow the stub instance (ko)
    var tracking = stub.ValidateCredentials.OnCall((ko, username, password) =>
        username == "admin" && password == "secret");

    IAuthSvcMethods auth = stub;

    Assert.True(auth.ValidateCredentials("admin", "secret"));
    Assert.False(auth.ValidateCredentials("user", "wrong"));
    Assert.Equal(2, tracking.CallCount);
}
```
<!-- endSnippet -->

---

## Verifying Method Calls

### Checking If Called

Use `WasCalled` to verify a method was invoked:

<!-- snippet: methods-verify-wascalled -->
```cs
[Fact]
public void WasCalled_VerifiesMethodInvocation()
{
    var stub = new SaveRepoMethodsStub();
    var tracking = stub.Save.OnCall((ko, entity) => { });

    ISaveRepoMethods repository = stub;
    repository.Save(new User { Id = 1 });

    // WasCalled is true if method was invoked at least once
    Assert.True(tracking.WasCalled);
}
```
<!-- endSnippet -->

### Verifying Call Count

Use `CallCount` to verify the exact number of invocations:

<!-- snippet: methods-verify-callcount -->
```cs
[Fact]
public void CallCount_VerifiesExactInvocations()
{
    var stub = new NotifierMethodsStub();
    var tracking = stub.Notify.OnCall((ko, message) => { });

    INotifierMethods notifier = stub;

    // Simulate processing a 2-item collection
    var items = new[] { "item1", "item2" };
    foreach (var item in items)
    {
        notifier.Notify($"Processing {item}");
    }

    // Verify exact call count via tracking object
    Assert.Equal(2, tracking.CallCount);
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
    var tracking = stub.GetUser.OnCall((ko, userId) => new User { Id = userId });

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
    var tracking = stub.ValidateCredentials.OnCall((ko, username, password) => true);

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
    var findAllTracking = stub.Find.OnCall((SearchRepoStub ko) =>
        new List<User>());
    var findByIdTracking = stub.Find.OnCall((SearchRepoStub ko, int id) =>
        new User { Id = id, Name = "ById" });
    var findByNameTracking = stub.Find.OnCall((SearchRepoStub ko, string name) =>
        new User { Id = 1, Name = name });

    ISearchRepo repo = stub;

    // Call each overload
    repo.Find();
    repo.Find(42);
    repo.Find("Alice");

    // Each tracking object is specific to its overload
    Assert.Equal(1, findAllTracking.CallCount);
    Assert.Equal(1, findByIdTracking.CallCount);
    Assert.Equal(42, findByIdTracking.LastArg);
    Assert.Equal(1, findByNameTracking.CallCount);
    Assert.Equal("Alice", findByNameTracking.LastArg);
}
```
<!-- endSnippet -->

Overloads are numbered in the order they appear in the interface definition.

---

## Resetting Interceptors

Clear call counts and remove callbacks using `Reset()`:

<!-- snippet: methods-reset -->
```cs
[Fact]
public void Reset_ClearsTrackingState()
{
    var stub = new ProcessorMethodsStub();
    var tracking = stub.ProcessData.OnCall((ko, data) => { });

    IProcessorMethods processor = stub;
    processor.ProcessData("initial");

    Assert.Equal(1, tracking.CallCount);

    // Reset clears CallCount, WasCalled on the interceptor
    stub.ProcessData.Reset();

    // Tracking is also reset
    Assert.Equal(0, tracking.CallCount);
    Assert.False(tracking.WasCalled);
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
    var getTracking = stub.GetUser.OnCall((ko, id) => id == 1 ? testUser : null);
    var saveTracking = stub.SaveUser.OnCall((ko, user) => { });

    var service = new UserService(stub);

    // Act
    var result = service.UpdateUserEmail(1, "new@test.com");

    // Assert
    Assert.True(result);

    // Verify GetUser was called with correct ID
    Assert.True(getTracking.WasCalled);
    Assert.Equal(1, getTracking.LastArg);

    // Verify SaveUser was called
    Assert.True(saveTracking.WasCalled);

    // Verify saved user has new email via the tracking args
    var savedUser = saveTracking.LastArg;
    Assert.Equal("new@test.com", savedUser.Email);
}
```
<!-- endSnippet -->

---

## Key Takeaways

- **OnCall signature**: First parameter is always the stub instance
- **Verification**: Use `WasCalled` for existence, `CallCount` for exact count
- **Arguments**: `LastCallArg` for single parameters, `LastCallArgs` tuple for multiple
- **Overloads**: Numbered suffixes (Method1, Method2, ...) in declaration order
- **Reset**: Clears call tracking and callbacks

Next: [Property Interceptors](properties.md) for get/set tracking and configuration.
