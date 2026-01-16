# Common Patterns

Patterns for common stubbing scenarios.

## Return Values

### Static Return

```csharp
stub.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Test" };
```

### Conditional Returns

```csharp
stub.GetUser.OnCall = (ko, id) => id switch
{
    1 => new User { Id = 1, Name = "Admin" },
    2 => new User { Id = 2, Name = "Guest" },
    _ => null
};
```

### Sequential Returns

```csharp
var values = new Queue<int>([1, 2, 3]);
stub.GetNext.OnCall = (ko) => values.Dequeue();

Assert.Equal(1, service.GetNext());
Assert.Equal(2, service.GetNext());
Assert.Equal(3, service.GetNext());
```

### Throwing Exceptions

```csharp
stub.Connect.OnCall = (ko) =>
    throw new InvalidOperationException("Connection failed");
```

## Async Methods

### Task<T>

```csharp
stub.GetByIdAsync.OnCall = (ko, id) =>
    Task.FromResult<User?>(new User { Id = id });
```

### Task (void equivalent)

```csharp
stub.InitializeAsync.OnCall = (ko) => Task.CompletedTask;
```

### Simulating Delay

```csharp
stub.FetchAsync.OnCall = async (ko, url) =>
{
    await Task.Delay(100);  // Simulate network latency
    return "response";
};
```

### Simulating Failure

```csharp
// Faulted task
stub.SaveAsync.OnCall = (ko, entity) =>
    Task.FromException<int>(new DbException("Connection lost"));

// Or throw directly
stub.SaveAsync.OnCall = (ko, entity) =>
{
    throw new DbException("Connection lost");
};
```

### ValueTask<T>

```csharp
stub.CountAsync.OnCall = (ko) => new ValueTask<int>(100);
```

## Properties

### Static Value (Preferred)

```csharp
stub.IsConnected.Value = true;
stub.Name.Value = "TestService";
```

### Dynamic Getter

```csharp
stub.CurrentTime.OnGet = (ko) => DateTime.UtcNow;
```

### State-Dependent

```csharp
stub.IsConnected.OnGet = (ko) => ko.Connect.WasCalled;
```

### Intercept Setter

```csharp
var captured = new List<string>();
stub.Name.OnSet = (ko, value) => captured.Add(value);
```

## Events

### Raise Event

```csharp
// EventHandler<T>
stub.DataReceived.Raise(null, "test data");

// EventHandler
stub.Completed.Raise(null, EventArgs.Empty);

// Action<T>
stub.ProgressChanged.Raise(75);
```

### Testing Event Subscription

```csharp
var viewModel = new ViewModel(service);

Assert.True(stub.DataChanged.HasSubscribers);
Assert.Equal(1, stub.DataChanged.AddCount);
```

### Testing Event Unsubscription

```csharp
viewModel.Dispose();

Assert.Equal(1, stub.DataChanged.RemoveCount);
Assert.False(stub.DataChanged.HasSubscribers);
```

## Verification

### Was Called

```csharp
Assert.True(stub.Save.WasCalled);
Assert.False(stub.Delete.WasCalled);
```

### Call Count

```csharp
Assert.Equal(1, stub.Save.CallCount);      // Exactly once
Assert.Equal(0, stub.Delete.CallCount);    // Never
Assert.True(stub.GetAll.CallCount >= 1);   // At least once
```

### Argument Capture

```csharp
// Single param
service.GetById(42);
Assert.Equal(42, stub.GetById.LastCallArg);

// Multiple params (named tuple)
service.Log("error", "Failed");
Assert.Equal("error", stub.Log.LastCallArgs?.level);
Assert.Equal("Failed", stub.Log.LastCallArgs?.message);
```

### Call History (Full)

```csharp
var allCalls = new List<(string to, string subject)>();
stub.SendEmail.OnCall = (ko, to, subject, body) =>
{
    allCalls.Add((to, subject));
};

service.SendEmail("a@test.com", "S1", "B1");
service.SendEmail("b@test.com", "S2", "B2");

Assert.Equal(2, allCalls.Count);
Assert.Equal("a@test.com", allCalls[0].to);
```

### Call Order

```csharp
var order = new List<string>();
stub.Initialize.OnCall = (ko) => order.Add("Initialize");
stub.Process.OnCall = (ko, v) => order.Add("Process");
stub.Cleanup.OnCall = (ko) => order.Add("Cleanup");

service.Initialize();
service.Process("test");
service.Cleanup();

Assert.Equal(["Initialize", "Process", "Cleanup"], order);
```

## Cross-Interceptor State

### Check Other Interceptor State

```csharp
stub.Process.OnCall = (ko, value) =>
{
    if (!ko.Initialize.WasCalled)
        throw new InvalidOperationException("Not initialized");
};
```

### Property Depends on Method Call

```csharp
stub.IsConnected.OnGet = (ko) => ko.Connect.WasCalled;
```

## Generic Methods

### Type-Specific Configuration

```csharp
stub.Deserialize.Of<User>().OnCall = (ko, json) => new User { Name = "Test" };
stub.Deserialize.Of<Order>().OnCall = (ko, json) => new Order { Id = 1 };
```

### Type-Specific Verification

```csharp
Assert.Equal(2, stub.Deserialize.Of<User>().CallCount);
Assert.Equal(1, stub.Deserialize.Of<Order>().CallCount);
Assert.Equal(3, stub.Deserialize.TotalCallCount);
```

### Multiple Type Parameters

```csharp
stub.Convert.Of<string, int>().OnCall = (ko, s) => s.Length;
```

## Method Overloads

Overloads get numeric suffixes:

```csharp
public interface IProcessor
{
    void Process(string data);        // stub.Process1
    void Process(string data, int n); // stub.Process2
}

stub.Process1.OnCall = (ko, data) => { };
stub.Process2.OnCall = (ko, data, n) => { };
```

## Indexers

### Pre-populate Backing

```csharp
stub.Indexer.Backing["Key1"] = "Value1";
stub.Indexer.Backing["Key2"] = "Value2";

Assert.Equal("Value1", store["Key1"]);
```

### Dynamic Getter

```csharp
stub.Indexer.OnGet = (ko, key) =>
{
    if (key == "special") return "computed";
    return ko.Indexer.Backing.GetValueOrDefault(key);
};
```

### Track Access

```csharp
_ = store["Key1"];
_ = store["Key2"];

Assert.Equal(2, stub.Indexer.GetCount);
Assert.Equal("Key2", stub.Indexer.LastGetKey);
```

## Out/Ref Parameters

### Out Parameters

```csharp
// Use explicit delegate type for out params
stub.TryGetValue.OnCall = (TryGetValueInterceptor.Delegate)((ko, key, out value) =>
{
    value = "found";
    return true;
});
```

### Ref Parameters

```csharp
// Ref params track input value (before callback modification)
stub.Increment.OnCall = (IncrementInterceptor.Delegate)((ko, ref value) =>
{
    value++;
});

int x = 5;
service.Increment(ref x);
Assert.Equal(6, x);
Assert.Equal(5, stub.Increment.LastCallArg);  // Input value
```

## Reset

### Reset Method

```csharp
stub.Process.Reset();
Assert.Equal(0, stub.Process.CallCount);
Assert.Null(stub.Process.OnCall);
```

### Reset Property (Value is preserved)

```csharp
stub.Name.Value = "Test";
stub.Name.Reset();
Assert.Equal(0, stub.Name.GetCount);
Assert.Equal("Test", stub.Name.Value);  // Still there!
```

### Reset Event (Handlers are removed)

```csharp
stub.DataReceived.Reset();
Assert.False(stub.DataReceived.HasSubscribers);
```

## User Methods (Compile-Time Defaults)

### Define Defaults in Stub Class

```csharp
[KnockOff]
public partial class UserRepoStub : IUserRepository
{
    protected User? GetById(int id) => new User { Id = id, Name = "Default" };
    protected IEnumerable<User> GetAll() => [];
}
```

### Override in Test

```csharp
var stub = new UserRepoStub();

// Uses user method by default
var user = service.GetById(1);  // Returns User with Name = "Default"

// Override for this test
stub.GetById.OnCall = (ko, id) => new User { Id = id, Name = "Special" };
var special = service.GetById(1);  // Returns User with Name = "Special"

// Reset to user method
stub.GetById.Reset();
var defaultAgain = service.GetById(1);  // Back to "Default"
```

## Priority Order

1. **Callback** (if set) — takes precedence
2. **User method** (if defined) — fallback for methods
3. **Smart default** — value types→default, new()→new T(), etc.
