# Methods

KnockOff supports void methods, methods with return values, and methods with any number of parameters.

## Method Types

### Void Methods (No Parameters)

<!-- snippet: docs:methods:void-no-params -->
```csharp
public interface IMethodService
{
    void Initialize();
}

[KnockOff]
public partial class MethodServiceKnockOff : IMethodService { }
```
<!-- /snippet -->

Tracking:
- `knockOff.Initialize.WasCalled`
- `knockOff.Initialize.CallCount`
- `knockOff.Initialize.OnCall` — `Action<MethodServiceKnockOff>`

### Void Methods (With Parameters)

<!-- snippet: docs:methods:void-with-params -->
```csharp
public interface IMethodLogger
{
    void Log(string message);
    void LogError(string message, Exception ex);
}

[KnockOff]
public partial class MethodLoggerKnockOff : IMethodLogger { }
```
<!-- /snippet -->

Single parameter:
- `knockOff.Log.LastCallArg` — the argument value (not a tuple)

Multiple parameters:
- `knockOff.LogError.LastCallArgs` — named tuple `(string message, Exception ex)`

### Methods with Return Values

<!-- snippet: docs:methods:return-value -->
```csharp
public interface IMethodRepository
{
    MethodUser? GetById(int id);
    int Count();
}

[KnockOff]
public partial class MethodRepositoryKnockOff : IMethodRepository { }
```
<!-- /snippet -->

Same tracking as void methods, plus:
- `OnCall` returns the value type

## Parameter Handling

### Single Parameter

For methods with one parameter, tracking uses the raw type (not a tuple):

<!-- snippet: docs:methods:single-param -->
```csharp
service.GetUser(42);

        // Tracking - single parameter uses raw type (not a tuple)
        int? lastId = knockOff.GetUser.LastCallArg;  // 42, not (42,)
```
<!-- /snippet -->

### Multiple Parameters

For methods with 2+ parameters, tracking uses named tuples:

<!-- snippet: docs:methods:multiple-params -->
```csharp
service.Process("test", 42, true);

        // Tracking - named tuple with original parameter names
        var args = knockOff.Process.LastCallArgs;
        var name = args?.name;   // "test"
        var value = args?.value; // 42
        var flag = args?.flag;   // true
```
<!-- /snippet -->

## User-Defined Methods

Define protected methods in your stub class for default behavior:

<!-- snippet: docs:methods:user-defined -->
```csharp
[KnockOff]
public partial class MethodUserDefinedKnockOff : IMethodUserDefined
{
    protected MethodUser? GetById(int id) => new MethodUser { Id = id, Name = "Default" };

    protected int Count() => 100;
}
```
<!-- /snippet -->

Rules:
- Must be `protected`
- Must match interface method signature exactly
- Called when no `OnCall` callback is set

## Callbacks

### Void Method Callbacks

<!-- snippet: docs:methods:void-callbacks -->
```csharp
// No parameters
        serviceKnockOff.Initialize.OnCall = (ko) =>
        {
            // Custom initialization logic
        };

        // Single parameter
        loggerKnockOff.Log.OnCall = (ko, message) =>
        {
            Console.WriteLine($"Logged: {message}");
        };

        // Multiple parameters
        loggerKnockOff.LogError.OnCall = (ko, message, ex) =>
        {
            Console.WriteLine($"Error: {message} - {ex.Message}");
        };
```
<!-- /snippet -->

### Return Method Callbacks

<!-- snippet: docs:methods:return-callbacks -->
```csharp
// No parameters
        knockOff.Count.OnCall = (ko) => 42;

        // Single parameter
        knockOff.GetById.OnCall = (ko, id) => new MethodUser { Id = id };
```
<!-- /snippet -->

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

<!-- snippet: docs:methods:priority-order -->
```csharp
[KnockOff]
public partial class MethodPriorityKnockOff : IMethodPriority
{
    protected int Calculate(int x) => x * 2;  // User method
}
```
<!-- /snippet -->

<!-- snippet: docs:methods:priority-order-usage -->
```csharp
// No callback → uses user method
        var result1 = service.Calculate(5);  // 10 (5 * 2)

        // Callback → overrides user method
        knockOff.Calculate2.OnCall = (ko, x) => x * 100;
        var result2 = service.Calculate(5);  // 500 (callback)

        // Reset → back to user method
        knockOff.Calculate2.Reset();
        var result3 = service.Calculate(5);  // 10 (user method again)
```
<!-- /snippet -->

## Common Patterns

### Simulating Failures

<!-- snippet: docs:methods:simulating-failures -->
```csharp
[KnockOff]
public partial class MethodFailureKnockOff : IMethodFailure { }
```
<!-- /snippet -->

<!-- snippet: docs:methods:simulating-failures-usage -->
```csharp
knockOff.Save.OnCall = (ko, entity) =>
        {
            throw new InvalidOperationException("Connection failed");
        };
```
<!-- /snippet -->

### Conditional Returns

<!-- snippet: docs:methods:conditional-returns -->
```csharp
knockOff.GetById.OnCall = (ko, id) => id switch
        {
            1 => new MethodUser { Id = 1, Name = "Admin" },
            2 => new MethodUser { Id = 2, Name = "Guest" },
            _ => null
        };
```
<!-- /snippet -->

### Capturing Arguments

<!-- snippet: docs:methods:capturing-arguments -->
```csharp
var capturedIds = new List<int>();
        knockOff.GetById.OnCall = (ko, id) =>
        {
            capturedIds.Add(id);
            return new MethodUser { Id = id };
        };
```
<!-- /snippet -->

### Verifying Call Order

<!-- snippet: docs:methods:verifying-call-order -->
```csharp
[KnockOff]
public partial class MethodCallOrderKnockOff : IMethodCallOrder { }
```
<!-- /snippet -->

<!-- snippet: docs:methods:verifying-call-order-usage -->
```csharp
var callOrder = new List<string>();

        knockOff.Initialize.OnCall = (ko) => callOrder.Add("Initialize");
        knockOff.Process.OnCall = (ko) => callOrder.Add("Process");
        knockOff.Cleanup.OnCall = (ko) => callOrder.Add("Cleanup");

        service.Initialize();
        service.Process();
        service.Cleanup();

        // callOrder is ["Initialize", "Process", "Cleanup"]
```
<!-- /snippet -->

### Sequential Returns

<!-- snippet: docs:methods:sequential-returns -->
```csharp
[KnockOff]
public partial class MethodSequentialKnockOff : IMethodSequential { }
```
<!-- /snippet -->

<!-- snippet: docs:methods:sequential-returns-usage -->
```csharp
var results = new Queue<int>([1, 2, 3]);
        knockOff.GetNext.OnCall = (ko) => results.Dequeue();

        var first = service.GetNext();   // 1
        var second = service.GetNext();  // 2
        var third = service.GetNext();   // 3
```
<!-- /snippet -->

### Accessing Other Interceptor State

<!-- snippet: docs:methods:accessing-handler-state -->
```csharp
[KnockOff]
public partial class MethodHandlerStateKnockOff : IMethodHandlerState { }
```
<!-- /snippet -->

<!-- snippet: docs:methods:accessing-handler-state-usage -->
```csharp
knockOff.Process.OnCall = (ko) =>
        {
            if (!ko.Initialize.WasCalled)
                throw new InvalidOperationException("Not initialized");
        };
```
<!-- /snippet -->
