# For AI Assistants

This page provides explicit guidance for AI code assistants generating KnockOff stubs.

## When to Use KnockOff

Use KnockOff when:
- The project has a `KnockOff` package reference
- User is writing unit tests that need test doubles
- User asks to stub, mock, or fake an interface or class

Do NOT use KnockOff when:
- Project uses Moq and user hasn't asked to migrate
- User explicitly requests Moq patterns
- Testing doesn't require test doubles

## Stub Patterns

### Pattern 1: Standalone Stub (Most Common)

Use for reusable stubs shared across test files:

<!-- snippet: ai-standalone-stub -->
```cs
[KnockOff]
public partial class AiUserRepositoryStub : IAiUserRepository { }
```
<!-- endSnippet -->

**Usage:**

<!-- snippet: ai-standalone-usage -->
```cs
public static void StandaloneUsage()
{
    var stub = new AiUserRepositoryStub();
    stub.GetById.OnCall = (ko, id) => new AiUser { Id = id };
    IAiUserRepository repo = stub;

    _ = repo;
}
```
<!-- endSnippet -->

### Pattern 2: Inline Stub

Use for stubs scoped to a single test class:

<!-- snippet: ai-inline-stub -->
```cs
[KnockOff<IAiUserRepository>]
[KnockOff<IAiEmailService>]
public partial class AiUserServiceTests
{
    public void Test()
    {
        var repoStub = new Stubs.IAiUserRepository();
        var emailStub = new Stubs.IAiEmailService();
        // ...

        _ = (repoStub, emailStub);
    }
}
```
<!-- endSnippet -->

### Pattern 3: Class Stub

Use for stubbing unsealed classes with virtual members:

<!-- snippet: ai-class-stub -->
```cs
[KnockOff<AiEmailServiceClass>]
public partial class AiNotificationTests
{
    public void Test()
    {
        var stub = new Stubs.AiEmailServiceClass();
        stub.Send.OnCall = (ko, to, body) => { };

        // Use .Object to get the class instance
        AiEmailServiceClass service = stub.Object;

        _ = service;
    }
}
```
<!-- endSnippet -->

### Pattern 4: Delegate Stub

Use for stubbing `Func<>`, `Action<>`, or named delegates:

<!-- snippet: ai-delegate-stub -->
```cs
[KnockOff<Func<int, bool>>]
public partial class AiValidationTests
{
    public void Test()
    {
        var stub = new Stubs.Func();
        stub.Interceptor.OnCall = (ko, value) => value > 0;

        Func<int, bool> func = stub;

        _ = func;
    }
}
```
<!-- endSnippet -->

## Interceptor API

### Method Interceptors

<!-- pseudo:ai-method-interceptors -->
```csharp
stub.MethodName.OnCall = (ko, arg1, arg2) => returnValue;  // Set behavior
stub.MethodName.CallCount      // int: number of calls
stub.MethodName.WasCalled      // bool: CallCount > 0
stub.MethodName.LastCallArg    // T: last argument (single param methods)
stub.MethodName.LastCallArgs   // named tuple: last arguments (multi param)
stub.MethodName.Reset()        // Clear tracking and callbacks
```
<!-- /snippet -->

### Property Interceptors

<!-- pseudo:ai-property-interceptors -->
```csharp
stub.PropertyName.Value = value;       // Set return value
stub.PropertyName.OnGet = (ko) => value;  // Dynamic getter
stub.PropertyName.OnSet = (ko, value) => { };  // Setter callback
stub.PropertyName.GetCount             // int: getter calls
stub.PropertyName.SetCount             // int: setter calls
stub.PropertyName.LastSetValue         // T: last set value
stub.PropertyName.Reset()              // Clear tracking and callbacks
```
<!-- /snippet -->

### Indexer Interceptors

<!-- pseudo:ai-indexer-interceptors -->
```csharp
stub.Indexer.Backing[key] = value;     // Pre-populate backing dictionary
stub.Indexer.OnGet = (ko, key) => value;  // Dynamic getter
stub.Indexer.OnSet = (ko, key, value) => { };  // Setter callback
stub.Indexer.GetCount                  // int: getter calls
stub.Indexer.SetCount                  // int: setter calls
stub.Indexer.LastGetKey                // TKey: last accessed key
stub.Indexer.LastSetEntry              // (TKey, TValue): last set entry
```
<!-- /snippet -->

### Event Interceptors

<!-- pseudo:ai-event-interceptors -->
```csharp
stub.EventName.Raise(sender, args);    // Raise the event
stub.EventName.AddCount                // int: subscription count
stub.EventName.RemoveCount             // int: unsubscription count
stub.EventName.HasSubscribers          // bool: any subscribers
```
<!-- /snippet -->

## Common Mistakes to Avoid

### 1. Forgetting `partial`

<!-- invalid:ai-mistake-partial -->
```csharp
// WRONG
[KnockOff]
public class UserRepositoryStub : IUserRepository { }

// CORRECT
[KnockOff]
public partial class UserRepositoryStub : IUserRepository { }
```
<!-- /snippet -->

### 2. Using Moq Syntax

<!-- invalid:ai-mistake-moq-syntax -->
```csharp
// WRONG - This is Moq syntax
stub.Setup(x => x.GetUser(It.IsAny<int>())).Returns(user);

// CORRECT - KnockOff syntax
stub.GetUser.OnCall = (ko, id) => user;
```
<!-- /snippet -->

### 3. Forgetting `.Object` for Class Stubs

<!-- invalid:ai-mistake-object -->
```csharp
// WRONG - stub is the wrapper, not the class instance
var service = new MyService(stub);

// CORRECT - use .Object to get the class instance
var service = new MyService(stub.Object);
```
<!-- /snippet -->

### 4. Wrong Callback Signature

<!-- invalid:ai-mistake-callback -->
```csharp
// WRONG - callback signature must match interface method
stub.GetUser.OnCall = (id) => new User();  // Missing 'ko' parameter

// CORRECT - first parameter is always the stub instance
stub.GetUser.OnCall = (ko, id) => new User();
```
<!-- /snippet -->

### 5. Using Wrong Interceptor for Overloads

<!-- snippet: ai-overloads -->
```cs
public static void OverloadedMethods()
{
    var stub = new AiProcessorStub();

    // CORRECT - overloads get numeric suffixes
    stub.Process1.OnCall = (ko, data) => { };
    stub.Process2.OnCall = (ko, data, n) => { };
}
```
<!-- endSnippet -->

## User-Defined Methods

To add default behavior, define protected methods matching interface signatures:

<!-- snippet: ai-user-methods -->
```cs
[KnockOff]
public partial class AiUserRepositoryWithDefaultsStub : IAiUserRepository
{
    // Called by default when IAiUserRepository.GetById is invoked
    protected AiUser? GetById(int id) => new AiUser { Id = id, Name = "Default" };
}
```
<!-- endSnippet -->

**Rules:**
- Must be `protected`
- Must match exact signature (name, parameters, return type)
- Callbacks (`OnCall`) take precedence over user methods

## Naming Conventions

### Interceptor Names

| Interface Member | Interceptor Name |
|-----------------|------------------|
| `void Save(User u)` | `stub.Save` |
| `User GetById(int id)` | `stub.GetById` |
| `string Name { get; set; }` | `stub.Name` |
| `string this[int i]` | `stub.Indexer` |
| `string this[string key]` | `stub.IndexerString` (if multiple) |
| `event EventHandler Changed` | `stub.Changed` |

### Overloaded Method Names

Methods with overloads get numeric suffixes (1-based):

| Interface | Interceptor |
|-----------|-------------|
| `void Process(string s)` | `stub.Process1` |
| `void Process(string s, int n)` | `stub.Process2` |

Methods without overloads have no suffix.

### User Method Collision

If a user method exists with the same name as an interface method, the interceptor gets a numeric suffix:

<!-- snippet: ai-user-method-collision -->
```cs
[KnockOff]
public partial class AiCalcStub : IAiCalculator
{
    protected int Add(int a, int b) => a + b;  // User method
}

// Interceptor becomes Add2 to avoid collision
// stub.Add2.CallCount
```
<!-- endSnippet -->

## Complete Example

<!-- snippet: ai-order-service-interface -->
```cs
public interface IAiOrderService
{
    AiOrder? GetOrder(int id);
    void SaveOrder(AiOrder order);
    Task<bool> ValidateAsync(AiOrder order);
    decimal TotalAmount { get; }
}
```
<!-- endSnippet -->

<!-- snippet: ai-order-service-stub -->
```cs
// Standalone stub with user method defaults
[KnockOff]
public partial class AiOrderServiceStub : IAiOrderService
{
    protected AiOrder? GetOrder(int id) => new AiOrder { Id = id, Status = "Pending" };
    protected Task<bool> ValidateAsync(AiOrder order) => Task.FromResult(true);
}
```
<!-- endSnippet -->

<!-- snippet: ai-complete-example -->
```cs
public class OrderProcessingTests
{
    public async Task ProcessOrder_Succeeds()
    {
        // Arrange
        var stub = new AiOrderServiceStub();
        stub.TotalAmount.Value = 150.00m;

        // Override default for this test
        stub.ValidateAsync2.OnCall = (ko, order) => Task.FromResult(order.Amount > 0);

        var processor = new AiOrderProcessor(stub);

        // Act
        var result = await processor.ProcessAsync(orderId: 1);

        // Assert
        Assert.True(result);
        Assert.Equal(1, stub.GetOrder2.CallCount);
        Assert.Equal(1, stub.SaveOrder.CallCount);
        Assert.Equal("Completed", stub.SaveOrder.LastCallArg?.Status);
    }
}
```
<!-- endSnippet -->
