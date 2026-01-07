# KnockOff

A Roslyn Source Generator for creating unit test stubs. Unlike Moq's fluent runtime configuration, KnockOff uses partial classes for compile-time setup—trading flexibility for readability and performance.

## Why KnockOff? Your Tests Run Faster

KnockOff has the benefits of source generation—no runtime reflection, no Castle.Core dependency:

| Scenario | Moq | KnockOff | Speedup |
|----------|-----|----------|---------|
| Method invocation | 216 ns | 0.4 ns | **500x faster** |
| Create 1000 stubs | 745 μs | 5.6 μs | **133x faster** |
| Typical unit test | 72 μs | 31 ns | **2,300x faster** |

**Zero allocations on invocations.** Moq allocates 288-408 bytes per call for its interception machinery. KnockOff generates direct method calls—no allocations, no GC pressure.

**What this means for your test suite:**
- A project with 5,000 tests using mocks could see test runs drop from minutes to seconds
- CI/CD pipelines complete faster, giving you quicker feedback
- Local test runs feel instant, encouraging you to run tests more often

## Documentation

- [Getting Started](docs/getting-started.md) - Installation and first steps
- [Customization Patterns](docs/concepts/customization-patterns.md) - The two ways to customize stub behavior
- [KnockOff vs Moq Comparison](docs/knockoff-vs-moq.md) - Side-by-side comparison for supported scenarios
- [Migration from Moq](docs/migration-from-moq.md) - Step-by-step migration guide
- [Release Notes](docs/release-notes/index.md) - Version history

## Concept

Mark a partial class with `[KnockOff]` that implements an interface. The source generator:
1. Generates explicit interface implementations for all interface members
2. Tracks invocations via interface-named properties for test verification
3. Detects user-defined methods in the partial class and calls them from the generated intercepts
4. Provides `OnCall`/`OnGet`/`OnSet` callbacks for runtime customization

## Quick Example

```csharp
public interface IDataService
{
    string Name { get; set; }
    string? GetDescription(int id);
    int GetCount();
}

// Define your stub with behavior
[KnockOff]
public partial class DataServiceKnockOff : IDataService
{
    private readonly int _count;

    public DataServiceKnockOff(int count = 42)
    {
        _count = count;
    }

    // Non-nullable method - define to return meaningful value
    protected int GetCount() => _count;

    // GetDescription not defined - generated code returns null by default
}

// Use in tests
[Fact]
public void Test_DataService()
{
    var knockOff = new DataServiceKnockOff(count: 100);
    IDataService service = knockOff;

    // Property - uses generated backing field
    service.Name = "Test";
    Assert.Equal("Test", service.Name);
    Assert.Equal(1, knockOff.IDataService.Name.SetCount);

    // Nullable method - returns null, call is still verified
    var description = service.GetDescription(5);
    Assert.Null(description);
    Assert.True(knockOff.IDataService.GetDescription.WasCalled);
    Assert.Equal(5, knockOff.IDataService.GetDescription.LastCallArg);

    // Non-nullable method - returns constructor value
    var count = service.GetCount();
    Assert.Equal(100, count);
    Assert.Equal(1, knockOff.IDataService.GetCount.CallCount);
}
```

The stub behavior is defined once in the partial class. Every test uses the same predictable behavior. Verification happens through the interface-named property.

## Defining Stub Behavior

**Properties** use generated backing fields automatically—no code needed.

**Methods** need a protected method in your stub if you want custom behavior:

```csharp
public interface ICalculator
{
    int Add(int a, int b);
    Task<int> AddAsync(int a, int b);
    void Reset();
}

[KnockOff]
public partial class CalculatorKnockOff : ICalculator
{
    protected int Add(int a, int b) => a + b;
    protected Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
    protected void Reset() { /* side effect logic */ }
}
```

**Properties** use auto-generated backing fields. For custom behavior, use `OnGet`/`OnSet` callbacks:

```csharp
var knockOff = new UserServiceKnockOff();

// Custom getter behavior
knockOff.IUserService.Name.OnGet = (ko) => "Dynamic Value";

// Custom setter with side effects
knockOff.IUserService.Name.OnSet = (ko, value) => Console.WriteLine($"Set to: {value}");
```

## Runtime Callbacks (Optional)

If you need per-test behavior without creating a new stub class, use callbacks:

```csharp
var knockOff = new DataServiceKnockOff();

// Override for this specific test
knockOff.IDataService.GetCount.OnCall = (ko) => 999;
knockOff.IDataService.Name.OnGet = (ko) => "FromCallback";
knockOff.IDataService.Name.OnSet = (ko, value) => { /* custom logic */ };
```

Callbacks take precedence over user-defined methods. Use `Reset()` to clear callbacks and return to the stub's default behavior.

## Verification

```csharp
var knockOff = new DataServiceKnockOff();
IDataService service = knockOff;

service.GetDescription(1);
service.GetDescription(2);
service.GetDescription(42);

// Check invocation
Assert.True(knockOff.IDataService.GetDescription.WasCalled);
Assert.Equal(3, knockOff.IDataService.GetDescription.CallCount);

// Check arguments
Assert.Equal(42, knockOff.IDataService.GetDescription.LastCallArg);

// Check properties
service.Name = "First";
service.Name = "Second";
Assert.Equal(2, knockOff.IDataService.Name.SetCount);
Assert.Equal("Second", knockOff.IDataService.Name.LastSetValue);
```

## Features

| Feature | Status |
|---------|--------|
| Properties (get/set, get-only, set-only) | Supported |
| Void methods | Supported |
| Methods with return values | Supported |
| Methods with parameters (single and multiple) | Supported |
| Method overloads | Supported |
| Async methods (Task, Task&lt;T&gt;, ValueTask, ValueTask&lt;T&gt;) | Supported |
| Generic interfaces | Supported |
| Multiple interface implementation | Supported |
| Interface inheritance | Supported |
| Indexers (get-only, get/set) | Supported |
| User-defined method detection | Supported |
| OnCall/OnGet/OnSet callbacks | Supported |
| Named tuple argument tracking | Supported |
| Events | Supported |
| Generic methods | Supported |
| ref/out parameters | Supported |

## Limitation: Interfaces with Internal Members

KnockOff **cannot stub interfaces with `internal` members from external assemblies**. This is a C# language constraint, not a tooling limitation.

```csharp
// In ExternalLibrary.dll
public interface IEntity
{
    bool IsModified { get; }       // public - stubbable
    internal void MarkModified();  // internal - impossible to implement externally
}
```

Internal members are invisible to external assemblies. No C# syntax—implicit or explicit interface implementation—can reference an invisible member. The compiler errors are CS0122 ("inaccessible due to protection level") and CS9044 ("cannot implicitly implement inaccessible member").

**KnockOff's behavior:** Internal members from external assemblies are filtered out. Public members are stubbed normally. If your tests require mocking internal members, use a runtime proxy library that the target assembly has declared as a friend via `[InternalsVisibleTo]`.

## Generated Code

For each interface member, KnockOff generates:
- **Handler class** with tracking properties and callbacks
- **Explicit interface implementation** that records invocations
- **Backing storage** (field for properties, dictionary for indexers)
- **`AsXYZ()` helper** for typed interface access

Example generated structure:
```csharp
public partial class UserServiceKnockOff
{
    public IUserServiceKO IUserService { get; } = new();

    public sealed class IUserServiceKO
    {
        public IUserService_GetUserHandler GetUser { get; } = new();
        public IUserService_NameHandler Name { get; } = new();
        // ... handlers for each member
    }

    User IUserService.GetUser(int id)
    {
        IUserService.GetUser.RecordCall(id);
        if (IUserService.GetUser.OnCall is { } callback)
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
