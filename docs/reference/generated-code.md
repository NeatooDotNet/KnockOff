# Generated Code Reference

This document explains what KnockOff generates and the conventions used.

## What Gets Generated

For each `[KnockOff]` class, the generator creates:

1. **Interceptor classes** — One per interface member (nested in the stub class)
2. **Interceptor properties** — Direct access to each interceptor on the stub
3. **Backing fields** — For properties
4. **Backing dictionaries** — For indexers
5. **Explicit interface implementations** — Recording + delegation
6. **AsIXYZ() methods** — For typed interface access

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
    // Interceptor properties (flat API - direct access)
    public NameInterceptor Name { get; } = new();
    public GetUserInterceptor GetUser { get; } = new();

    // Backing field for property
    protected string NameBacking { get; set; } = "";

    // Interceptor for Name property
    public sealed class NameInterceptor
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

    // Interceptor for GetUser method
    public sealed class GetUserInterceptor
    {
        public int CallCount { get; private set; }
        public bool WasCalled => CallCount > 0;
        public int? LastCallArg { get; private set; }

        public Func<UserServiceKnockOff, int, User?>? OnCall { get; set; }

        public void RecordCall(int id)
        {
            CallCount++;
            LastCallArg = id;
        }

        public void Reset()
        {
            CallCount = 0;
            LastCallArg = default;
            OnCall = null;
        }
    }

    // Explicit interface implementation - property
    string IUserService.Name
    {
        get
        {
            Name.RecordGet();
            if (Name.OnGet is { } onGetCallback)
                return onGetCallback(this);
            return NameBacking;
        }
        set
        {
            Name.RecordSet(value);
            if (Name.OnSet is { } onSetCallback)
                onSetCallback(this, value);
            else
                NameBacking = value;
        }
    }

    // Explicit interface implementation - method
    User? IUserService.GetUser(int id)
    {
        GetUser.RecordCall(id);
        if (GetUser.OnCall is { } onCallCallback)
            return onCallCallback(this, id);
        return GetUser(id);  // Calls user-defined method
    }

    // Interface accessor helper
    public IUserService AsIUserService() => this;
}
```

## Naming Conventions

### Interceptor Classes
- Properties: `{PropertyName}Interceptor`
- Methods: `{MethodName}Interceptor`
- Indexers: `{KeyTypeName}IndexerInterceptor`

### Interceptor Properties
- Direct on stub: `knockOff.{MemberName}`
- Example: `knockOff.GetUser`, `knockOff.Name`, `knockOff.StringIndexer`

### Backing Storage
- Properties: `{PropertyName}Backing`
- Indexers: `{KeyTypeName}IndexerBacking`

### Helper Methods
- Interface access: `AsI{InterfaceName}()` (includes the 'I' prefix)
- Example: `knockOff.AsIUserService()`

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

Indexer interceptors use the key type name:

| Indexer | Interceptor Access | Backing Name |
|---------|----------------|--------------|
| `this[string key]` | `knockOff.StringIndexer` | `knockOff.StringIndexerBacking` |
| `this[int index]` | `knockOff.IntIndexer` | `knockOff.IntIndexerBacking` |
| `this[Guid id]` | `knockOff.GuidIndexer` | `knockOff.GuidIndexerBacking` |

## Multiple Parameters

For methods with 2+ parameters, tracking uses named tuples:

```csharp
// Interface: void Log(string level, string message, int code)

// Generated tracking
public int CallCount { get; private set; }
public bool WasCalled => CallCount > 0;
public (string level, string message, int code)? LastCallArgs { get; private set; }

// Generated callback signature - individual parameters
public Action<LoggerKnockOff, string, string, int>? OnCall { get; set; }

// Usage
knockOff.Log.OnCall = (ko, level, message, code) =>
{
    Console.WriteLine($"[{level}] {message} ({code})");
};
```

## Interface Constraint

Standalone `[KnockOff]` stubs implement exactly one interface. Attempting to implement multiple unrelated interfaces emits diagnostic `KO0010`:

```csharp
// VALID - single interface
[KnockOff]
public partial class LoggerKnockOff : ILogger { }

// VALID - interface with inheritance (IChild : IParent)
[KnockOff]
public partial class ChildKnockOff : IChild { }  // Also implements IParent members

// INVALID - multiple unrelated interfaces (emits KO0010)
[KnockOff]
public partial class BadKnockOff : ILogger, IAuditor { }  // Error!
```

If you need multiple unrelated interfaces, use separate stubs:

```csharp
[KnockOff]
public partial class LoggerKnockOff : ILogger { }

[KnockOff]
public partial class AuditorKnockOff : IAuditor { }

// In test
var logger = new LoggerKnockOff();
var auditor = new AuditorKnockOff();
```

For **inline stubs** within a test class, multiple interfaces are supported - see the inline stubs documentation.

## Null Handling

- Nullable reference types are respected
- Default return values use `default(T)` which is `null` for reference types
- Backing fields for reference types default to `""` for strings, `null` for others
