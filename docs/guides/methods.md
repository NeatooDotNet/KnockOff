# Methods

KnockOff supports void methods, methods with return values, and methods with any number of parameters.

## Method Types

### Void Methods (No Parameters)

```csharp
public interface IService
{
    void Initialize();
}

[KnockOff]
public partial class ServiceKnockOff : IService { }
```

Tracking:
- `IService.Initialize.WasCalled`
- `IService.Initialize.CallCount`
- `IService.Initialize.OnCall` — `Action<ServiceKnockOff>`

### Void Methods (With Parameters)

```csharp
public interface ILogger
{
    void Log(string message);
    void LogError(string message, Exception ex);
}
```

Single parameter:
- `ILogger.Log.LastCallArg` — the argument value (not a tuple)
- `ILogger.Log.AllCalls` — `List<string>`

Multiple parameters:
- `ILogger.LogError.LastCallArgs` — named tuple `(string message, Exception ex)`
- `ILogger.LogError.AllCalls` — `List<(string message, Exception ex)>`

### Methods with Return Values

```csharp
public interface IRepository
{
    User? GetById(int id);
    int Count();
}
```

Same tracking as void methods, plus:
- `OnCall` returns the value type

## Parameter Handling

### Single Parameter

For methods with one parameter, tracking uses the raw type (not a tuple):

```csharp
public interface IService
{
    User GetUser(int id);
}

// Usage
service.GetUser(42);

// Tracking
int? lastId = knockOff.IService.GetUser.LastCallArg;  // 42, not (42,)
List<int> allIds = knockOff.IService.GetUser.AllCalls; // [42]
```

### Multiple Parameters

For methods with 2+ parameters, tracking uses named tuples:

```csharp
public interface IService
{
    void Process(string name, int value, bool flag);
}

// Usage
service.Process("test", 42, true);

// Tracking - named tuple with original parameter names
var args = knockOff.IService.Process.LastCallArgs;
Assert.Equal("test", args?.name);
Assert.Equal(42, args?.value);
Assert.True(args?.flag);

// Tuple destructuring
if (knockOff.IService.Process.LastCallArgs is var (name, value, flag))
{
    Assert.Equal("test", name);
}

// All calls
var allCalls = knockOff.IService.Process.AllCalls;
Assert.Equal("test", allCalls[0].name);
```

## User-Defined Methods

Define protected methods in your stub class for default behavior:

```csharp
[KnockOff]
public partial class RepositoryKnockOff : IRepository
{
    protected User? GetById(int id) => new User { Id = id, Name = "Default" };

    protected int Count() => 100;
}
```

Rules:
- Must be `protected`
- Must match interface method signature exactly
- Called when no `OnCall` callback is set

## Callbacks

### Void Method Callbacks

```csharp
// No parameters
knockOff.IService.Initialize.OnCall = (ko) =>
{
    // Custom initialization logic
};

// Single parameter
knockOff.ILogger.Log.OnCall = (ko, message) =>
{
    Console.WriteLine($"Logged: {message}");
};

// Multiple parameters (individual params)
knockOff.ILogger.LogError.OnCall = (ko, message, ex) =>
{
    Console.WriteLine($"Error: {message} - {ex.Message}");
};
```

### Return Method Callbacks

```csharp
// No parameters
knockOff.IRepository.Count.OnCall = (ko) => 42;

// Single parameter
knockOff.IRepository.GetById.OnCall = (ko, id) => new User { Id = id };

// Multiple parameters
knockOff.IRepository.Find.OnCall = (ko, name, includeInactive) =>
{
    return users.Where(u => u.Name == name).ToList();
};
```

### Callback Signatures

Each method gets an `OnCall` property with a delegate or Func/Action type:

| Method Signature | OnCall Type |
|------------------|-------------|
| `void M()` | `Action<TKnockOff>?` |
| `void M(T1 a)` | `Action<TKnockOff, T1>?` |
| `void M(T1 a, T2 b)` | `Action<TKnockOff, T1, T2>?` |
| `R M()` | `Func<TKnockOff, R>?` |
| `R M(T1 a)` | `Func<TKnockOff, T1, R>?` |
| `R M(T1 a, T2 b)` | `Func<TKnockOff, T1, T2, R>?` |

## Priority Order

1. **Callback** (if set) — takes precedence
2. **User method** (if defined) — fallback
3. **Default** — returns `default(T)` for return methods, nothing for void

```csharp
[KnockOff]
public partial class ServiceKnockOff : IService
{
    protected int Calculate(int x) => x * 2;  // User method
}

var knockOff = new ServiceKnockOff();
IService service = knockOff;

// No callback → uses user method
Assert.Equal(10, service.Calculate(5));  // 5 * 2

// Callback → overrides user method
knockOff.IService.Calculate.OnCall = (ko, x) => x * 100;
Assert.Equal(500, service.Calculate(5));  // callback

// Reset → back to user method
knockOff.IService.Calculate.Reset();
Assert.Equal(10, service.Calculate(5));  // user method again
```

## Common Patterns

### Simulating Failures

```csharp
knockOff.IRepository.Save.OnCall = (ko, entity) =>
{
    throw new DbException("Connection failed");
};
```

### Conditional Returns

```csharp
knockOff.IRepository.GetById.OnCall = (ko, id) => id switch
{
    1 => new User { Id = 1, Name = "Admin" },
    2 => new User { Id = 2, Name = "Guest" },
    _ => null
};
```

### Capturing Arguments

```csharp
var capturedIds = new List<int>();
knockOff.IRepository.GetById.OnCall = (ko, id) =>
{
    capturedIds.Add(id);
    return new User { Id = id };
};
```

### Verifying Call Order

```csharp
var callOrder = new List<string>();

knockOff.IService.Initialize.OnCall = (ko) => callOrder.Add("Initialize");
knockOff.IService.Process.OnCall = (ko) => callOrder.Add("Process");
knockOff.IService.Cleanup.OnCall = (ko) => callOrder.Add("Cleanup");

// ... run code under test ...

Assert.Equal(["Initialize", "Process", "Cleanup"], callOrder);
```

### Sequential Returns

```csharp
var results = new Queue<int>([1, 2, 3]);
knockOff.ISequence.GetNext.OnCall = (ko) => results.Dequeue();

Assert.Equal(1, service.GetNext());
Assert.Equal(2, service.GetNext());
Assert.Equal(3, service.GetNext());
```

### Accessing Other Spy State

```csharp
knockOff.IService.Process.OnCall = (ko) =>
{
    if (!ko.IService.Initialize.WasCalled)
        throw new InvalidOperationException("Not initialized");
};
```
