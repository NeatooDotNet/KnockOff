# Generated Code Reference

This document explains what KnockOff generates and the conventions used.

## What Gets Generated

For each `[KnockOff]` class, the generator creates:

1. **Interface spy classes** — One per implemented interface, containing handlers for that interface's members
2. **Interface spy properties** — `public {InterfaceName}Spy {InterfaceName} { get; } = new();`
3. **Handler classes** — One per interface member
4. **Backing fields** — For properties (prefixed with interface name)
5. **Backing dictionaries** — For indexers (prefixed with interface name)
6. **Explicit interface implementations** — Recording + delegation
7. **AsXYZ() methods** — For typed interface access

## File Location

Generated files are placed in:
```
{ProjectRoot}/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/
```

Each KnockOff class gets its own file: `{ClassName}.g.cs`

## Enabling Output

Add to your project file:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

## Example: Full Generated Structure

Given this input:

```csharp
public interface IUserService
{
    string Name { get; set; }
    User? GetUser(int id);
}

[KnockOff]
public partial class UserServiceKnockOff : IUserService
{
    protected User? GetUser(int id) => new User { Id = id };
}
```

KnockOff generates:

```csharp
public partial class UserServiceKnockOff
{
    // Interface spy property
    public IUserServiceSpy IUserService { get; } = new();

    // Backing field for property (interface-prefixed)
    protected string IUserService_NameBacking { get; set; } = "";

    // Interface spy class
    public sealed class IUserServiceSpy
    {
        public IUserService_NameHandler Name { get; } = new();
        public IUserService_GetUserHandler GetUser { get; } = new();
    }

    // Handler for Name property
    public sealed class IUserService_NameHandler
    {
        public int GetCount { get; private set; }
        public int SetCount { get; private set; }
        public string? LastSetValue { get; private set; }

        public Func<UserServiceKnockOff, string>? OnGet { get; set; }
        public Action<UserServiceKnockOff, string>? OnSet { get; set; }

        public void RecordGet() => GetCount++;
        public void RecordSet(string value)
        {
            SetCount++;
            LastSetValue = value;
        }

        public void Reset()
        {
            GetCount = 0;
            SetCount = 0;
            LastSetValue = default;
            OnGet = null;
            OnSet = null;
        }
    }

    // Handler for GetUser method
    public sealed class IUserService_GetUserHandler
    {
        private readonly List<int> _calls = new();

        public int CallCount => _calls.Count;
        public bool WasCalled => _calls.Count > 0;
        public int? LastCallArg => _calls.Count > 0 ? _calls[^1] : null;
        public IReadOnlyList<int> AllCalls => _calls;

        public Func<UserServiceKnockOff, int, User?>? OnCall { get; set; }

        public void RecordCall(int id) => _calls.Add(id);

        public void Reset()
        {
            _calls.Clear();
            OnCall = null;
        }
    }

    // Explicit interface implementation - property
    string IUserService.Name
    {
        get
        {
            IUserService.Name.RecordGet();
            if (IUserService.Name.OnGet is { } onGetCallback)
                return onGetCallback(this);
            return IUserService_NameBacking;
        }
        set
        {
            IUserService.Name.RecordSet(value);
            if (IUserService.Name.OnSet is { } onSetCallback)
                onSetCallback(this, value);
            else
                IUserService_NameBacking = value;
        }
    }

    // Explicit interface implementation - method
    User? IUserService.GetUser(int id)
    {
        IUserService.GetUser.RecordCall(id);
        if (IUserService.GetUser.OnCall is { } onCallCallback)
            return onCallCallback(this, id);
        return GetUser(id);  // Calls user-defined method
    }

    // Interface accessor helper
    public IUserService AsUserService() => this;
}
```

## Naming Conventions

### Interface Spy Class
- Name: `{InterfaceName}Spy`
- Example: `IUserService` → `IUserServiceSpy`

### Interface Spy Property
- Name: `{InterfaceName}` (same as interface name)
- Example: `public IUserServiceSpy IUserService { get; }`

### Handlers
- Properties: `{InterfaceName}_{PropertyName}Handler`
- Methods: `{InterfaceName}_{MethodName}Handler`
- Indexers: `{InterfaceName}_{KeyTypeName}IndexerHandler`

### Backing Storage
- Properties: `{InterfaceName}_{PropertyName}Backing`
- Indexers: `{InterfaceName}_{KeyTypeName}IndexerBacking`

### Helper Methods
- Interface access: `As{InterfaceName}()`

## User Method Detection

The generator looks for protected methods matching interface signatures:

```csharp
[KnockOff]
public partial class ServiceKnockOff : IService
{
    // Detected - matches IService.GetValue(int)
    protected int GetValue(int id) => id * 2;

    // NOT detected - wrong access modifier
    public int GetValue2(int id) => id * 2;

    // NOT detected - wrong signature
    protected int GetValue(string id) => 0;
}
```

Rules:
- Must be `protected`
- Must match method name exactly
- Must match parameter types and count
- Must match return type

## Indexer Naming

Indexer handlers use the key type name with interface prefix:

| Interface | Indexer | Handler Access | Backing Name |
|-----------|---------|----------------|--------------|
| `IPropertyStore` | `this[string key]` | `IPropertyStore.StringIndexer` | `IPropertyStore_StringIndexerBacking` |
| `IList` | `this[int index]` | `IList.IntIndexer` | `IList_IntIndexerBacking` |
| `ICache` | `this[Guid id]` | `ICache.GuidIndexer` | `ICache_GuidIndexerBacking` |

## Multiple Parameters

For methods with 2+ parameters, tracking uses named tuples:

```csharp
// Interface: void Log(string level, string message, int code)

// Generated tracking
public (string level, string message, int code)? LastCallArgs { get; }
public IReadOnlyList<(string level, string message, int code)> AllCalls { get; }

// Generated callback signature - individual parameters
public Action<ServiceKnockOff, string, string, int>? OnCall { get; set; }

// Usage
knockOff.ILogger.Log.OnCall = (ko, level, message, code) =>
{
    Console.WriteLine($"[{level}] {message} ({code})");
};
```

## Multiple Interfaces

When a KnockOff class implements multiple interfaces, each interface gets its own spy class with separate handlers:

```csharp
interface ILogger { void Log(string msg); }
interface IAuditor { void Log(string msg); }

[KnockOff]
public partial class LoggerKnockOff : ILogger, IAuditor { }
```

Separate handlers are generated for each interface:

```csharp
// Interface spy classes
public sealed class ILoggerSpy
{
    public ILogger_LogHandler Log { get; } = new();
}

public sealed class IAuditorSpy
{
    public IAuditor_LogHandler Log { get; } = new();
}

// Interface spy properties
public ILoggerSpy ILogger { get; } = new();
public IAuditorSpy IAuditor { get; } = new();

// Each implementation uses its own handler
void ILogger.Log(string msg) { ILogger.Log.RecordCall(msg); ... }
void IAuditor.Log(string msg) { IAuditor.Log.RecordCall(msg); ... }
```

This allows tracking calls separately:

```csharp
var knockOff = new LoggerKnockOff();
ILogger logger = knockOff;
IAuditor auditor = knockOff;

logger.Log("hello");
auditor.Log("world");

Assert.Equal(1, knockOff.ILogger.Log.CallCount);   // Only ILogger call
Assert.Equal(1, knockOff.IAuditor.Log.CallCount);  // Only IAuditor call
```

## Null Handling

- Nullable reference types are respected
- Default return values use `default(T)` which is `null` for reference types
- Backing fields for reference types default to `""` for strings, `null` for others
