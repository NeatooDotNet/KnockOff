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
- `Spy.Initialize.WasCalled`
- `Spy.Initialize.CallCount`
- `Spy.Initialize.OnCall` — `Action<ServiceKnockOff>`

### Void Methods (With Parameters)

```csharp
public interface ILogger
{
    void Log(string message);
    void LogError(string message, Exception ex);
}
```

Single parameter:
- `Spy.Log.LastCallArg` — the argument value (not a tuple)
- `Spy.Log.AllCalls` — `List<string>`

Multiple parameters:
- `Spy.LogError.LastCallArgs` — named tuple `(string message, Exception ex)`
- `Spy.LogError.AllCalls` — `List<(string message, Exception ex)>`

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
int? lastId = knockOff.Spy.GetUser.LastCallArg;  // 42, not (42,)
List<int> allIds = knockOff.Spy.GetUser.AllCalls; // [42]
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
var args = knockOff.Spy.Process.LastCallArgs;
Assert.Equal("test", args?.name);
Assert.Equal(42, args?.value);
Assert.True(args?.flag);

// Tuple destructuring
if (knockOff.Spy.Process.LastCallArgs is var (name, value, flag))
{
    Assert.Equal("test", name);
}

// All calls
var allCalls = knockOff.Spy.Process.AllCalls;
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
knockOff.Spy.Initialize.OnCall((ko) =>
{
    // Custom initialization logic
});

// Single parameter
knockOff.Spy.Log.OnCall((ko, message) =>
{
    Console.WriteLine($"Logged: {message}");
});

// Multiple parameters (individual params)
knockOff.Spy.LogError.OnCall((ko, message, ex) =>
{
    Console.WriteLine($"Error: {message} - {ex.Message}");
});
```

### Return Method Callbacks

```csharp
// No parameters
knockOff.Spy.Count.OnCall((ko) => 42);

// Single parameter
knockOff.Spy.GetById.OnCall((ko, id) => new User { Id = id });

// Multiple parameters
knockOff.Spy.Find.OnCall((ko, name, includeInactive) =>
{
    return users.Where(u => u.Name == name).ToList();
});
```

### Callback Signatures

Each method gets a generated delegate type with individual parameters:

| Method Signature | OnCall Delegate |
|------------------|-----------------|
| `void M()` | `MDelegate(TKnockOff ko)` |
| `void M(T1 a)` | `MDelegate(TKnockOff ko, T1 a)` |
| `void M(T1 a, T2 b)` | `MDelegate(TKnockOff ko, T1 a, T2 b)` |
| `R M()` | `MDelegate(TKnockOff ko) → R` |
| `R M(T1 a)` | `MDelegate(TKnockOff ko, T1 a) → R` |
| `R M(T1 a, T2 b)` | `MDelegate(TKnockOff ko, T1 a, T2 b) → R` |

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
knockOff.Spy.Calculate.OnCall((ko, x) => x * 100);
Assert.Equal(500, service.Calculate(5));  // callback

// Reset → back to user method
knockOff.Spy.Calculate.Reset();
Assert.Equal(10, service.Calculate(5));  // user method again
```

## Common Patterns

### Simulating Failures

```csharp
knockOff.Spy.Save.OnCall((ko, entity) =>
{
    throw new DbException("Connection failed");
});
```

### Conditional Returns

```csharp
knockOff.Spy.GetById.OnCall((ko, id) => id switch
{
    1 => new User { Id = 1, Name = "Admin" },
    2 => new User { Id = 2, Name = "Guest" },
    _ => null
});
```

### Capturing Arguments

```csharp
var capturedIds = new List<int>();
knockOff.Spy.GetById.OnCall((ko, id) =>
{
    capturedIds.Add(id);
    return new User { Id = id };
});
```

### Verifying Call Order

```csharp
var callOrder = new List<string>();

knockOff.Spy.Initialize.OnCall((ko) => callOrder.Add("Initialize"));
knockOff.Spy.Process.OnCall((ko) => callOrder.Add("Process"));
knockOff.Spy.Cleanup.OnCall((ko) => callOrder.Add("Cleanup"));

// ... run code under test ...

Assert.Equal(["Initialize", "Process", "Cleanup"], callOrder);
```

### Sequential Returns

```csharp
var results = new Queue<int>([1, 2, 3]);
knockOff.Spy.GetNext.OnCall((ko) => results.Dequeue());

Assert.Equal(1, service.GetNext());
Assert.Equal(2, service.GetNext());
Assert.Equal(3, service.GetNext());
```

### Accessing Other Spy State

```csharp
knockOff.Spy.Process.OnCall((ko) =>
{
    if (!ko.Spy.Initialize.WasCalled)
        throw new InvalidOperationException("Not initialized");
});
```
