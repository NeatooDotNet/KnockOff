# Generated Code Reference

This document explains what KnockOff generates and the conventions used.

## What Gets Generated

For each `[KnockOff]` class, the generator creates:

1. **Spy class** — Nested class containing handlers for each member
2. **Spy property** — `public {ClassName}Spy Spy { get; } = new();`
3. **Handler classes** — One per interface member
4. **Backing fields** — For properties
5. **Backing dictionaries** — For indexers
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
    // Spy property
    public UserServiceKnockOffSpy Spy { get; } = new();

    // Backing field for property
    protected string NameBacking { get; set; } = "";

    // Spy class
    public sealed class UserServiceKnockOffSpy
    {
        public NameHandler Name { get; } = new();
        public GetUserHandler GetUser { get; } = new();

        // Handler for Name property
        public sealed class NameHandler
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
        public sealed class GetUserHandler
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
    }

    // Explicit interface implementation - property
    string IUserService.Name
    {
        get
        {
            Spy.Name.RecordGet();
            if (Spy.Name.OnGet is { } onGetCallback)
                return onGetCallback(this);
            return NameBacking;
        }
        set
        {
            Spy.Name.RecordSet(value);
            if (Spy.Name.OnSet is { } onSetCallback)
            {
                onSetCallback(this, value);
                return;
            }
            NameBacking = value;
        }
    }

    // Explicit interface implementation - method
    User? IUserService.GetUser(int id)
    {
        Spy.GetUser.RecordCall(id);
        if (Spy.GetUser.OnCall is { } onCallCallback)
            return onCallCallback(this, id);
        return GetUser(id);  // Calls user-defined method
    }

    // Interface accessor helper
    public IUserService AsUserService() => this;
}
```

## Naming Conventions

### Spy Class
- Name: `{ClassName}Spy`
- Example: `UserServiceKnockOff` → `UserServiceKnockOffSpy`

### Handlers
- Properties: `{PropertyName}Handler`
- Methods: `{MethodName}Handler`
- Indexers: `{KeyTypeName}IndexerHandler`

### Backing Storage
- Properties: `{PropertyName}Backing`
- Indexers: `{KeyTypeName}IndexerBacking`

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

Indexer handlers use the key type name:

| Indexer | Handler Name | Backing Name |
|---------|--------------|--------------|
| `this[string key]` | `StringIndexer` | `StringIndexerBacking` |
| `this[int index]` | `IntIndexer` | `IntIndexerBacking` |
| `this[Guid id]` | `GuidIndexer` | `GuidIndexerBacking` |

## Multiple Parameters (Tuples)

For methods with 2+ parameters, tracking uses named tuples:

```csharp
// Interface: void Log(string level, string message, int code)

// Generated tracking
public (string level, string message, int code)? LastCallArgs { get; }
public IReadOnlyList<(string level, string message, int code)> AllCalls { get; }

// Generated callback signature
public Action<ServiceKnockOff, (string level, string message, int code)>? OnCall { get; set; }
```

## Shared Members (Multiple Interfaces)

When interfaces share members with identical signatures:

```csharp
interface ILogger { void Log(string msg); }
interface IAuditor { void Log(string msg); }

[KnockOff]
public partial class LoggerKnockOff : ILogger, IAuditor { }
```

One handler is generated, used by both interface implementations:

```csharp
// Single handler
public sealed class LogHandler { ... }

// Both implementations use it
void ILogger.Log(string msg) { Spy.Log.RecordCall(msg); ... }
void IAuditor.Log(string msg) { Spy.Log.RecordCall(msg); ... }
```

## Null Handling

- Nullable reference types are respected
- Default return values use `default(T)` which is `null` for reference types
- Backing fields for reference types default to `""` for strings, `null` for others
