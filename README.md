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

## Compile-Time Advantages

KnockOff generates real C# code. This unlocks benefits that runtime mocking frameworks can't provide.

### Compile-Time Safety

When an interface changes, KnockOff fails at compile time. Moq fails at runtime.

```csharp
// IUserService adds a new method: Task<User> GetUserAsync(int id);

// Moq: Compiles fine, fails at runtime in CI
var mock = new Mock<IUserService>();
mock.Setup(x => x.GetUser(1)).Returns(new User());  // Oops, missed GetUserAsync

// KnockOff: Compiler error immediately
[KnockOff]
public partial class UserServiceKnockOff : IUserService { }
// CS0535: 'UserServiceKnockOff' does not implement interface member 'IUserService.GetUserAsync(int)'
```

### Full IDE Support

Source-generated code means full IntelliSense, Ctrl+Click navigation, and refactoring:

- **Rename a method?** All stubs update automatically
- **Find all references?** Includes stub usages
- **Hover for docs?** Shows parameter names and types

### Debuggable Stubs

Set breakpoints in your user-defined methods. Step through your stub logic like normal code.

```csharp
[KnockOff]
public partial class RepositoryKnockOff : IRepository
{
    protected User? GetById(int id)
    {
        // Set a breakpoint here — it works!
        return _testUsers.FirstOrDefault(u => u.Id == id);
    }
}
```

With Moq, you're stepping through Castle.Core proxy internals.

### No Lambda Ceremony

Define behavior with normal methods, not expression trees:

```csharp
// Moq
mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns((int id) => new User { Id = id });
mock.Setup(x => x.IsActive).Returns(true);
mock.Setup(x => x.SaveAsync(It.IsAny<User>())).ReturnsAsync(true);

// KnockOff — just write methods
protected User GetUser(int id) => new User { Id = id };
protected bool IsActive => true;
protected Task<bool> SaveAsync(User user) => Task.FromResult(true);
```

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

### Inline Stubs — Interfaces and Classes

Add `[KnockOff<T>]` to your test class. Works with interfaces and unsealed classes:

```csharp
[KnockOff<IUserService>]        // Interface
[KnockOff<EmailService>]        // Unsealed class (virtual members only)
public partial class UserTests
{
    [Fact]
    public void NotifiesUser_WhenOrderShipped()
    {
        // Create stubs
        var userStub = new Stubs.IUserService();
        var emailStub = new Stubs.EmailService();

        // Configure behavior (unified API for both interface and class stubs)
        userStub.GetUser.OnCall = (ko, id) => new User { Id = id, Email = "test@example.com" };
        emailStub.Send.OnCall = (ko, to, subject, body) => { };

        // Inject and test (.Object for class stubs)
        var service = new OrderService(userStub, emailStub.Object);
        service.ShipOrder(orderId: 42, userId: 1);

        // Verify
        Assert.True(userStub.GetUser.WasCalled);
        Assert.Equal(1, userStub.GetUser.LastCallArg);
        Assert.Equal("test@example.com", emailStub.Send.LastCallArgs?.to);
    }
}
```

**Class stubs use composition** — access the target class via `.Object`. Virtual/abstract members are intercepted; non-virtual members accessed through `.Object`.

### Reusable Stubs with Default Behavior

For stubs shared across test files, use the explicit pattern with user-defined methods:

```csharp
[KnockOff]
public partial class CalculatorKnockOff : ICalculator
{
    // User-defined method — called by generated code
    protected int Add(int a, int b) => a + b;

    // Multiply not defined — returns default(int) = 0
}

// Every test gets the same behavior
[Fact]
public void Test1()
{
    var calc = new CalculatorKnockOff();
    Assert.Equal(5, calc.AsCalculator().Add(2, 3));  // Uses your method
    Assert.Equal(1, calc.ICalculator.Add.CallCount);
}

[Fact]
public void Test2_OverrideForThisTest()
{
    var calc = new CalculatorKnockOff();
    calc.ICalculator.Add.OnCall = (ko, a, b) => 999;  // Override just here
    Assert.Equal(999, calc.AsCalculator().Add(2, 3));
}
```

### Delegate Stubs

Stub named delegates for validation rules, factories, and callbacks:

```csharp
public delegate bool IsUniqueRule(string value);

[KnockOff<IsUniqueRule>]
public partial class ValidationTests
{
    [Fact]
    public void RejectsNonUniqueName()
    {
        var uniqueCheck = new Stubs.IsUniqueRule();
        uniqueCheck.Interceptor.OnCall = (ko, value) => value != "duplicate";

        IsUniqueRule rule = uniqueCheck;  // Implicit conversion
        Assert.False(rule("duplicate"));
        Assert.True(uniqueCheck.Interceptor.WasCalled);
    }
}
```

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
