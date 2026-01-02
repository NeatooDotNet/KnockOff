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

// Define your KnockOff stub
[KnockOff]
public partial class UserServiceKnockOff : IUserService
{
    // Optional: Define default behavior (compile-time)
    protected User GetUser(int id) => new User { Id = id, Name = "Default" };
}

// Use in tests
[Fact]
public void Test_UserService()
{
    var knockOff = new UserServiceKnockOff();
    IUserService service = knockOff;

    // Override behavior for this test (runtime)
    knockOff.Spy.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Mocked" };

    var user = service.GetUser(42);

    Assert.Equal("Mocked", user.Name);
    Assert.Equal(1, knockOff.Spy.GetUser.CallCount);
    Assert.Equal(42, knockOff.Spy.GetUser.LastCallArg);
}
```

## Two Ways to Customize Behavior

KnockOff provides two complementary patterns for customizing stub behavior:

### Pattern 1: User-Defined Methods (Compile-Time)

Define protected methods in your stub class for consistent behavior across all tests:

```csharp
[KnockOff]
public partial class UserServiceKnockOff : IUserService
{
    protected User GetUser(int id) => new User { Id = id, Name = "Test User" };
}
```

### Pattern 2: Callbacks (Runtime)

Set callbacks for test-specific behavior:

```csharp
knockOff.Spy.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Custom" };
knockOff.Spy.Name.OnGet = (ko) => "FromCallback";
knockOff.Spy.Name.OnSet = (ko, value) => { /* custom logic */ };
```

### Priority Order

When an interface member is invoked:
1. **Callback** (if set) — takes precedence
2. **User method** (if defined) — fallback for methods
3. **Default** — backing field for properties, `default(T)` for methods

Use `Reset()` to clear callbacks and return to user method behavior.

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
