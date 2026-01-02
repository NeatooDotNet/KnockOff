# KnockOff

A Roslyn Source Generator for creating unit test stubs. Unlike Moq's fluent runtime configuration, KnockOff uses partial classes for compile-time setup—trading flexibility for readability and performance.

## Documentation

- [Getting Started](docs/getting-started.md) - Installation and first steps
- [Customization Patterns](docs/concepts/customization-patterns.md) - The two ways to customize stub behavior
- [KnockOff vs Moq Comparison](docs/knockoff-vs-moq.md) - Side-by-side comparison for supported scenarios
- [Migration from Moq](docs/migration-from-moq.md) - Step-by-step migration guide

## Concept

Mark a partial class with `[KnockOff]` that implements an interface. The source generator:
1. Generates explicit interface implementations for all interface members
2. Tracks invocations via `Spy` for test verification
3. Detects user-defined methods in the partial class and calls them from the generated intercepts
4. Provides `OnCall`/`OnGet`/`OnSet` callbacks for runtime customization

## Quick Example

```csharp
public interface IUserService
{
    string Name { get; set; }
    User GetUser(int id);
}

// Define your stub with behavior built-in
[KnockOff]
public partial class UserServiceKnockOff : IUserService
{
    protected User GetUser(int id) => new User { Id = id, Name = "Test User" };
}

// Use in tests
[Fact]
public void Test_GetUser_ReturnsUserWithCorrectId()
{
    var knockOff = new UserServiceKnockOff();
    IUserService service = knockOff;

    var user = service.GetUser(42);

    Assert.Equal(42, user.Id);
    Assert.Equal("Test User", user.Name);
    Assert.Equal(1, knockOff.Spy.GetUser.CallCount);
    Assert.Equal(42, knockOff.Spy.GetUser.LastCallArg);
}
```

The stub behavior is defined once in the partial class. Every test uses the same predictable behavior. Verification happens through `Spy`.

## Defining Stub Behavior

Define protected methods in your stub class that match interface members:

```csharp
[KnockOff]
public partial class RepositoryKnockOff : IRepository
{
    private readonly List<Entity> _entities = [];

    // Return value methods
    protected Entity? GetById(int id) => _entities.FirstOrDefault(e => e.Id == id);

    // Void methods
    protected void Save(Entity entity) => _entities.Add(entity);

    // Async methods
    protected Task<List<Entity>> GetAllAsync() => Task.FromResult(_entities.ToList());
}
```

Properties use backing fields automatically. For custom property behavior, define get/set methods:

```csharp
[KnockOff]
public partial class ConfigKnockOff : IConfig
{
    private int _callCount;

    protected string GetConnectionString() => $"Called {++_callCount} times";
}
```

## Runtime Callbacks (Optional)

If you need per-test behavior without creating a new stub class, use callbacks:

```csharp
var knockOff = new UserServiceKnockOff();

// Override the stub's built-in behavior for this specific test
knockOff.Spy.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Override" };
knockOff.Spy.Name.OnGet = (ko) => "FromCallback";
knockOff.Spy.Name.OnSet = (ko, value) => { /* custom logic */ };
```

Callbacks take precedence over user-defined methods. Use `Reset()` to clear callbacks and return to the stub's default behavior.

## Verification

```csharp
// Check invocation
Assert.True(knockOff.Spy.GetUser.WasCalled);
Assert.Equal(3, knockOff.Spy.GetUser.CallCount);

// Check arguments
Assert.Equal(42, knockOff.Spy.GetUser.LastCallArg);
var allCalls = knockOff.Spy.GetUser.AllCalls; // [1, 2, 42]

// Check properties
Assert.Equal(2, knockOff.Spy.Name.SetCount);
Assert.Equal("LastValue", knockOff.Spy.Name.LastSetValue);
```

## Features

| Feature | Status |
|---------|--------|
| Properties (get/set, get-only, set-only) | Supported |
| Void methods | Supported |
| Methods with return values | Supported |
| Methods with parameters (single and multiple) | Supported |
| Async methods (Task, Task&lt;T&gt;, ValueTask, ValueTask&lt;T&gt;) | Supported |
| Generic interfaces | Supported |
| Multiple interface implementation | Supported |
| Interface inheritance | Supported |
| Indexers (get-only, get/set) | Supported |
| User-defined method detection | Supported |
| OnCall/OnGet/OnSet callbacks | Supported |
| Named tuple argument tracking | Supported |
| Events | [Planned](docs/design/events.md) |
| Generic methods | Not yet |
| ref/out parameters | Not yet |

## Generated Code

For each interface member, KnockOff generates:
- **Handler class** in `Spy` with tracking properties and callbacks
- **Explicit interface implementation** that records invocations
- **Backing storage** (field for properties, dictionary for indexers)
- **`AsXYZ()` helper** for typed interface access

Example generated structure:
```csharp
public partial class UserServiceKnockOff
{
    public UserServiceKnockOffSpy Spy { get; } = new();

    public sealed class UserServiceKnockOffSpy
    {
        public GetUserHandler GetUser { get; } = new();
        public NameHandler Name { get; } = new();
        // ... handlers for each member
    }

    User IUserService.GetUser(int id)
    {
        Spy.GetUser.RecordCall(id);
        if (Spy.GetUser.OnCall is { } callback)
            return callback(this, id);
        return GetUser(id); // Calls user method
    }
}
```

## Installation

```bash
dotnet add package KnockOff
```

Or add to your `.csproj`:
```xml
<PackageReference Include="KnockOff" Version="1.0.0" />
```

## Viewing Generated Code

Enable in your test project:
```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
```

Generated files appear in `Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/`.
