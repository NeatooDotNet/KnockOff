# KnockOff

KnockOff uses a Roslyn Source Generator for creating unit test stubs. The generated code fully implements all interface members automatically. Then you customize their behavior per stub or per test. KnockOff supports interfaces, classes, and delegates via [two approaches](#quick-example): inline stubs with `[KnockOff<T>]` or stand-alone stubs implementing the target type.

This entire repository—code and documentation—was written by Claude Code AI with my ideas and guidance. A working version took a couple of weeks. This was an idea I carried for years. It's an exciting new era!

## Why KnockOff? Your Tests Run Faster

KnockOff has the benefits of source generation—no runtime reflection, no Castle.Core dependency:

| Scenario | Moq | KnockOff | Speedup |
|----------|-----|----------|---------|
| Method invocation | 216 ns | 0.4 ns | **500x faster** |
| Create 1000 stubs | 745 μs | 5.6 μs | **133x faster** |
| Typical unit test | 226 μs | 50 ns | **4,500x faster** |

**What this means for your test suite:**
- A project with 5,000 tests using mocks could see test runs drop from minutes to seconds
- CI/CD pipelines complete faster, giving you quicker feedback
- Local test runs feel instant, encouraging you to run tests more often

## Performance Benchmarks

Benchmarks compare KnockOff against [Moq](https://github.com/moq/moq), the industry-standard mocking framework, and [Rocks](https://github.com/JasonBock/Rocks), another source-generated alternative. KnockOff has [limitations vs Moq](#limitations-vs-moq), but for many test scenarios the tradeoff is worth it.

```
BenchmarkDotNet v0.14.0, Ubuntu 24.04.3 LTS (WSL)
Intel Core i7-11800H 2.30GHz, .NET 9.0.11

| Method                              | Mean         | Allocated |
|------------------------------------ |-------------:|----------:|
| Moq_TypicalUnitTest                 | 225,992.6 ns |   38,159 B |
| KnockOff_TypicalUnitTest            |      49.7 ns |      232 B |
| Rocks_TypicalUnitTest               |     663.0 ns |    2,360 B |
| CachedRepository_CacheMiss_Moq      | 475,506.4 ns |   45,004 B |
| CachedRepository_CacheMiss_KnockOff |     408.7 ns |    1,576 B |
| CachedRepository_CacheMiss_Rocks    |     903.2 ns |    3,112 B |
```

### Analysis

| Scenario | KnockOff vs Moq | Rocks vs Moq |
|----------|-----------------|--------------|
| Typical Unit Test | **4,549x faster**, 165x less memory | 341x faster, 16x less memory |
| Cached Repository | **1,163x faster**, 29x less memory | 527x faster, 14x less memory |

### Generated Code Size

Source generators add code to your project. Less generated code means faster builds.

| Scenario | KnockOff | Rocks |
|----------|----------|-------|
| Typical Unit Test (1 interface) | 141 lines | 324 lines |
| Cached Repository (3 interfaces) | 451 lines | 880 lines |

KnockOff generates ~2x less code than Rocks for equivalent functionality.

**Why the difference?**

- **Moq** uses runtime reflection, expression tree compilation, and Castle.Core dynamic proxy generation
- **Rocks** generates code at compile-time but creates expectation tracking infrastructure per-test
- **KnockOff** generates minimal stub classes with direct delegate invocation and no verification overhead

For large test suites (1000+ tests), these differences compound significantly. A test suite taking *30 seconds* with Moq might complete in under *1 second* with KnockOff.

## At a Glance: Moq vs KnockOff vs Rocks

### Setup Style

| Framework | Pattern |
|-----------|---------|
| Moq | `mock.Setup(x => x.Method()).Returns(value)` |
| KnockOff | `stub.Method.OnCall = (ko, args) => value` |
| Rocks | `expectations.Methods.Method().ReturnValue(value)` |

### Verification Style

| Framework | Pattern |
|-----------|---------|
| Moq | `mock.Verify(x => x.Method(), Times.Once)` |
| KnockOff | `Assert.Equal(1, stub.Method.CallCount)` |
| Rocks | `expectations.Verify()` (checks all expectations) |

### Argument Access

| Framework | Pattern |
|-----------|---------|
| Moq | Callback capture: `Callback<T>(x => captured = x)` |
| KnockOff | Automatic: `stub.Method.LastCallArg` or `LastCallArgs` |
| Rocks | Callback capture in handler |

### Property Setup

| Framework | Pattern |
|-----------|---------|
| Moq | `mock.Setup(x => x.Prop).Returns(10)` |
| KnockOff | `stub.Prop.Value = 10` |
| Rocks | `expectations.Properties.Getters.Prop().ReturnValue(10)` |

## Side-by-Side Example

**Production Code**

<!-- snippet: readme-side-by-side-types -->
```cs
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Total { get; set; }
}

public interface IOrderService
{
    Order GetOrder(int id);
    bool ValidateOrder(Order order);
    decimal CalculateTotal(Order order);
    void SaveOrder(Order order);
}

public class OrderProcessor(IOrderService orderService)
{
    public void Process(int orderId)
    {
        var order = orderService.GetOrder(orderId);
        if (orderService.ValidateOrder(order))
        {
            order.Total = orderService.CalculateTotal(order);
            orderService.SaveOrder(order);
        }
    }
}
```
<!-- endSnippet -->

**Moq**

<!-- snippet: readme-side-by-side-moq -->
```cs
[Fact]
public void OrderProcessor_ProcessesValidOrder_Moq()
{
    // Arrange
    var mock = new Mock<IOrderService>();
    mock.Setup(x => x.GetOrder(It.IsAny<int>()))
        .Returns((int id) => new Order { Id = id, CustomerId = 1 });
    mock.Setup(x => x.ValidateOrder(It.IsAny<Order>())).Returns(true);
    mock.Setup(x => x.CalculateTotal(It.IsAny<Order>())).Returns(100m);

    var sut = new OrderProcessor(mock.Object);

    // Act
    sut.Process(1);

    // Assert
    mock.Verify(x => x.GetOrder(1), Times.Once);
    mock.Verify(x => x.ValidateOrder(It.IsAny<Order>()), Times.Once);
    mock.Verify(x => x.SaveOrder(It.IsAny<Order>()), Times.Once);
}
```
<!-- endSnippet -->

**KnockOff**

<!-- snippet: readme-side-by-side-knockoff -->
```cs
[Fact]
public void OrderProcessor_ProcessesValidOrder_KnockOff()
{
    // Arrange
    var stub = new OrderServiceStub();
    stub.GetOrder.OnCall = (ko, id) => new Order { Id = id, CustomerId = 1 };
    stub.ValidateOrder.OnCall = (ko, _) => true;
    stub.CalculateTotal.OnCall = (ko, _) => 100m;

    var sut = new OrderProcessor(stub);

    // Act
    sut.Process(1);

    // Assert
    Assert.Equal(1, stub.GetOrder.CallCount);
    Assert.Equal(1, stub.ValidateOrder.CallCount);
    Assert.Equal(1, stub.SaveOrder.CallCount);
}
```
<!-- endSnippet -->

## Quick Example

### Inline Stubs — Interfaces and Classes

Add `[KnockOff<T>]` to your test class. Works with interfaces and unsealed classes:

<!-- snippet: readme-inline-stubs -->
```cs
[KnockOff<IUserService>]
[KnockOff<EmailService>]
public partial class UserTests
{
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
        System.Diagnostics.Debug.Assert(userStub.GetUser.WasCalled);
        System.Diagnostics.Debug.Assert(userStub.GetUser.LastCallArg == 1);
        System.Diagnostics.Debug.Assert(emailStub.Send.LastCallArgs?.to == "test@example.com");
    }
}
```
<!-- endSnippet -->

**Class stubs use composition** — access the target class via `.Object`. Virtual/abstract members are intercepted; non-virtual members accessed through `.Object`. Constructor parameters are passed through to the base class.

### Reusable Stubs with Default Behavior

For stubs shared across test files, use the explicit pattern with user-defined methods:

<!-- snippet: readme-reusable-stubs -->
```cs
[KnockOff]
public partial class CalculatorKnockOff : ICalculator
{
    // User-defined method - called by generated code
    protected int Add(int a, int b) => a + b;

    // Multiply not defined - returns default(int) = 0
}
```
<!-- endSnippet -->

<!-- snippet: readme-reusable-stubs-usage -->
```cs
public class CalculatorUsageExample
{
    public void Test1()
    {
        var calc = new CalculatorKnockOff();
        ICalculator calculator = calc;

        var result = calculator.Add(2, 3);      // Returns 5 (uses your method)
        var callCount = calc.Add2.CallCount;    // 1 (Add2: renamed to avoid collision)
    }

    public void Test2_OverrideForThisTest()
    {
        var calc = new CalculatorKnockOff();
        calc.Add2.OnCall = (ko, a, b) => 999;   // Override just here
        var result = ((ICalculator)calc).Add(2, 3);  // Returns 999
    }
}
```
<!-- endSnippet -->

### Delegate Stubs

Stub named delegates for validation rules, factories, and callbacks:

<!-- snippet: readme-delegate-stubs -->
```cs
public delegate bool IsUniqueRule(string value);

[KnockOff<IsUniqueRule>]
public partial class ValidationTests
{
    public void RejectsNonUniqueName()
    {
        var uniqueCheck = new Stubs.IsUniqueRule();
        uniqueCheck.Interceptor.OnCall = (ko, value) => value != "duplicate";

        IsUniqueRule rule = uniqueCheck;  // Implicit conversion
        var result = rule("duplicate");   // Returns false
        var wasCalled = uniqueCheck.Interceptor.WasCalled;  // true
    }
}
```
<!-- endSnippet -->

### Properties

Use `.Value` to set a property's return value. Use `OnGet` and `OnSet` when you need dynamic behavior or want to track setter calls:

<!-- snippet: readme-properties -->
```cs
public class PropertiesUsageExample
{
    public void ConfigureGettersAndSetters()
    {
        var stub = new ConfigServiceStub();
        IConfigService config = stub;

        // Simple value - most common pattern
        stub.ConnectionString.Value = "Server=localhost;Database=test";

        // Dynamic getter
        stub.LogLevel.OnGet = (ko) =>
            Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Info";

        // Setter tracking
        config.LogLevel = "Debug";

        // Verify property access
        _ = config.ConnectionString;
        var getCount = stub.ConnectionString.GetCount;     // 1
        var setCount = stub.LogLevel.SetCount;             // 1
        var lastValue = stub.LogLevel.LastSetValue;        // "Debug"
    }
}
```
<!-- endSnippet -->

### Verification

<!-- snippet: readme-verification -->
```cs
public class VerificationUsageExample
{
    public void TracksCallsAndArguments()
    {
        var stub = new DataServiceStub();
        stub.GetDescription.OnCall = (ko, id) => $"Item {id}";
        IDataService service = stub;

        service.GetDescription(1);
        service.GetDescription(2);
        service.GetDescription(42);

        // Assert method calls
        Assert.True(stub.GetDescription.WasCalled);
        Assert.Equal(3, stub.GetDescription.CallCount);
        Assert.Equal(42, stub.GetDescription.LastCallArg);

        // Assert property access
        service.Name = "First";
        service.Name = "Second";
        Assert.Equal(2, stub.Name.SetCount);
        Assert.Equal("Second", stub.Name.LastSetValue);
    }
}
```
<!-- endSnippet -->

## API Design

KnockOff supports three stub patterns, all with the same consistent API:

| Access | What You Get |
|--------|--------------|
| `stub.Member` | Interceptor (tracking, callbacks) |
| `stub.Object.Member` | Actual value (interface/class instance) |

### Pattern 1: Inline Interface Stub

<!-- snippet: docs:design:inline-interface-definition -->
```cs
/// <summary>
/// Pattern 1: Inline Interface Stub
/// Apply [KnockOff&lt;T&gt;] to generate nested stub class.
/// </summary>
[KnockOff<IDsUserService>]
public partial class DsInlineInterfaceTests { }
```
<!-- endSnippet -->

<!-- snippet: docs:design:inline-interface-usage -->
```cs
public static void InlineInterfaceStubUsage()
{
    // Create stub
    var stub = new DsInlineInterfaceTests.Stubs.IDsUserService();
    IDsUserService service = stub.Object;

    // METHOD: Configure and verify
    stub.GetUser.OnCall = (ko, id) => new DsUser { Id = id };
    var user = service.GetUser(42);
    // Verify: stub.GetUser.CallCount, stub.GetUser.LastCallArg

    // PROPERTY: Configure and verify
    stub.Name.Value = "Test";
    var name = service.Name;
    // Verify: stub.Name.GetCount, stub.Name.SetCount

    // INDEXER: Configure and verify
    stub.Indexer.Backing["key"] = "value";
    var val = service["key"];
    // Verify: stub.Indexer.GetCount, stub.Indexer.LastGetKey
}
```
<!-- endSnippet -->

### Pattern 2: Inline Class Stub

<!-- snippet: docs:design:inline-class-definition -->
```cs
/// <summary>
/// Pattern 2: Inline Class Stub
/// Apply [KnockOff&lt;T&gt;] with a class to stub virtual members.
/// </summary>
[KnockOff<DsEmailService>]
public partial class DsInlineClassTests { }
```
<!-- endSnippet -->

<!-- snippet: docs:design:inline-class-usage -->
```cs
public static void InlineClassStubUsage()
{
    // Create stub
    var stub = new DsInlineClassTests.Stubs.DsEmailService();
    DsEmailService service = stub.Object;

    // METHOD: Configure and verify
    stub.Send.OnCall = (ko, to, body) => { /* custom behavior */ };
    service.Send("test@example.com", "Hello");
    // Verify: stub.Send.CallCount, stub.Send.LastCallArgs

    // PROPERTY: Configure and verify
    stub.ServerName.OnGet = (ko) => "smtp.test.com";
    var server = service.ServerName;
    // Verify: stub.ServerName.GetCount

    // INDEXER: Configure and verify
    stub.Indexer.Backing[0] = "value";
    var val = service[0];
    // Verify: stub.Indexer.GetCount
}
```
<!-- endSnippet -->

### Pattern 3: Stand-Alone Interface Stub

<!-- snippet: docs:design:standalone-interface-definition -->
```cs
/// <summary>
/// Pattern 3: Stand-Alone Interface Stub
/// Apply [KnockOff] to a partial class that implements the interface.
/// </summary>
[KnockOff]
public partial class DsUserServiceStub : IDsUserService { }
```
<!-- endSnippet -->

<!-- snippet: docs:design:standalone-interface-usage -->
```cs
public static void StandaloneInterfaceStubUsage()
{
    // Create stub
    var stub = new DsUserServiceStub();
    IDsUserService service = stub.Object;

    // METHOD: Configure and verify
    stub.GetUser.OnCall = (ko, id) => new DsUser { Id = id };
    var user = service.GetUser(42);
    // Verify: stub.GetUser.CallCount, stub.GetUser.LastCallArg

    // PROPERTY: Configure and verify
    stub.Name.Value = "Test";
    var name = service.Name;
    // Verify: stub.Name.GetCount, stub.Name.SetCount

    // INDEXER: Configure and verify
    stub.Indexer.Backing["key"] = "value";
    var val = service["key"];
    // Verify: stub.Indexer.GetCount, stub.Indexer.LastGetKey
}
```
<!-- endSnippet -->

## Advanced Features

### Indexers

Indexers use a backing dictionary for storage and support custom callbacks:

<!-- snippet: docs:design:indexer-basic-usage -->
```cs
public static void IndexerBasicUsage()
{
    var stub = new DsIndexerTests.Stubs.IDsCache();
    IDsCache cache = stub.Object;

    // Pre-populate backing dictionary
    stub.Indexer.Backing["user:1"] = "Alice";
    stub.Indexer.Backing["user:2"] = "Bob";

    // Access through interface
    var user1 = cache["user:1"];  // Returns "Alice"
    cache["user:3"] = "Charlie";  // Adds to backing

    // Verify access
    // stub.Indexer.GetCount == 1
    // stub.Indexer.SetCount == 1
    // stub.Indexer.LastGetKey == "user:1"
    // stub.Indexer.LastSetEntry == ("user:3", "Charlie")
}
```
<!-- endSnippet -->

For multiple indexers (different key types), each gets a separate interceptor:

<!-- snippet: docs:design:indexer-multiple-usage -->
```cs
public static void MultipleIndexersUsage()
{
    var stub = new DsIndexerTests.Stubs.IDsMultiIndexer();
    IDsMultiIndexer multi = stub.Object;

    // Each indexer has its own interceptor named by key type
    // String-keyed indexer: IndexerString
    // Int-keyed indexer: IndexerInt32
    stub.IndexerString.Backing["name"] = "Alice";
    stub.IndexerInt32.Backing[0] = "First";

    var byName = multi["name"];  // "Alice"
    var byIndex = multi[0];      // "First"

    // Verify separately
    // stub.IndexerString.GetCount == 1
    // stub.IndexerInt32.GetCount == 1
}
```
<!-- endSnippet -->

### Init Properties

Init-only properties (C# 9+) are configured via `.Value` on the interceptor:

<!-- snippet: docs:design:init-property-interface-usage -->
```cs
public static void InitPropertyInterfaceUsage()
{
    var stub = new DsInitPropertyTests.Stubs.IDsEntity();
    IDsEntity entity = stub.Object;

    // Configure init properties via interceptor's Value
    stub.Id.Value = "entity-123";
    stub.Name.Value = "Test Entity";

    // Read through interface
    var id = entity.Id;      // "entity-123"
    var name = entity.Name;  // "Test Entity"

    // Verify read access
    // stub.Id.GetCount == 1
    // stub.Name.GetCount == 1
}
```
<!-- endSnippet -->

Mixed init/set properties work together seamlessly:

<!-- snippet: docs:design:init-property-mixed-usage -->
```cs
public static void InitPropertyMixedUsage()
{
    var stub = new DsInitPropertyTests.Stubs.IDsDocument();
    IDsDocument doc = stub.Object;

    // Init property: configure via Value
    stub.Id.Value = "doc-456";

    // Regular property: configure via Value or OnGet/OnSet
    stub.Title.Value = "My Document";

    // Get-only property: configure via Value
    stub.Version.Value = 1;

    // Read values
    var id = doc.Id;         // "doc-456" (init - immutable)
    doc.Title = "New Title"; // Works (regular setter)
    var ver = doc.Version;   // 1 (get-only)

    // Init properties track reads, not writes (writes via stub.X.Value)
    // stub.Id.GetCount == 1
    // stub.Title.SetCount == 1
}
```
<!-- endSnippet -->

### Required Properties

Required properties (C# 11+) work like regular class properties. The `[SetsRequiredMembers]` attribute is auto-generated:

<!-- snippet: docs:design:required-property-usage -->
```cs
public static void RequiredPropertyUsage()
{
    // Required properties work like regular properties in stubs
    // The [SetsRequiredMembers] attribute is auto-generated on constructors
    var stub = new DsRequiredPropertyTests.Stubs.DsAuditableEntity();
    DsAuditableEntity entity = stub.Object;

    // Configure via OnGet (class stub pattern)
    stub.Id.OnGet = _ => "audit-001";
    stub.CreatedBy.OnGet = _ => "admin";
    stub.CreatedAt.OnGet = _ => new DateTime(2024, 1, 15);

    // Access through object
    var id = entity.Id;              // "audit-001"
    var createdBy = entity.CreatedBy; // "admin"
    var createdAt = entity.CreatedAt; // 2024-01-15

    // Or set through object (required { get; set; } allows mutation)
    entity.Id = "audit-002";
    // stub.Id.SetCount == 1
}
```
<!-- endSnippet -->

## Documentation

- [Getting Started](docs/getting-started.md) - Installation and first steps
- [Customization Patterns](docs/concepts/customization-patterns.md) - The two ways to customize stub behavior
- [Generic Interfaces](docs/guides/generics.md) - Generic interfaces and standalone stubs
- [KnockOff vs Moq Comparison](docs/knockoff-vs-moq.md) - Side-by-side comparison for supported scenarios
- [Migration from Moq](docs/migration-from-moq.md) - Step-by-step migration guide
- [Diagnostics](docs/diagnostics.md) - Compiler diagnostics and how to resolve them
- [Release Notes](docs/release-notes/index.md) - Version history

## Compile-Time Advantages

KnockOff generates real C# code. This unlocks benefits that runtime mocking frameworks can't provide.

### Compile-Time Safety

When an interface changes, KnockOff fails at compile time. Moq fails at runtime.

<!-- invalid:readme-compile-time-safety -->
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
<!-- /snippet -->

### Full IDE Support

Source-generated code means full IntelliSense, Ctrl+Click navigation, and refactoring:

- **Rename a method?** All stubs update automatically
- **Find all references?** Includes stub usages
- **Hover for docs?** Shows parameter names and types

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
| Generic standalone stubs (`Stub<T> : IRepo<T>`) | Supported |
| Multiple interface implementation | Supported |
| Interface inheritance | Supported |
| Indexers (get-only, get/set) | Supported |
| User-defined method detection | Supported |
| OnCall/OnGet/OnSet callbacks | Supported |
| Named tuple argument tracking | Supported |
| Events | Supported |
| Generic methods | Supported |
| ref/out parameters | Supported |

## Limitations vs Moq

KnockOff doesn't support: argument matchers (`It.IsAny<T>`, `It.Is<T>(predicate)`), `SetupSequence`, strict mode, `VerifyNoOtherCalls`, `InSequence` call ordering, `LINQ to Mocks`, `As<T>()` interface addition, recursive auto-mocking, protected member mocking, or `MockRepository`.

**Covered differently:** Moq's `Callback` → `OnCall`; `Returns` → `OnCall` return or `.Value`; `Times.Once` → `Assert.Equal(1, stub.Method.CallCount)`.

## Limitation: Interfaces with Internal Members

KnockOff **cannot stub interfaces with `internal` members from external assemblies**. This is a C# language constraint, not a tooling limitation.

<!-- pseudo:readme-internal-members -->
```csharp
// In ExternalLibrary.dll
public interface IEntity
{
    bool IsModified { get; }       // public - stubbable
    internal void MarkModified();  // internal - impossible to implement externally
}
```
<!-- /snippet -->

Internal members are invisible to external assemblies. No C# syntax—implicit or explicit interface implementation—can reference an invisible member. The compiler errors are CS0122 ("inaccessible due to protection level") and CS9044 ("cannot implicitly implement inaccessible member").

**KnockOff's behavior:** Internal members from external assemblies are filtered out. Public members are stubbed normally. If your tests require mocking internal members, use a runtime proxy library that the target assembly has declared as a friend via `[InternalsVisibleTo]`.

## Generated Code

For each interface member, KnockOff generates:
- **Handler class** with tracking properties and callbacks
- **Explicit interface implementation** that records invocations
- **Backing storage** (field for properties, dictionary for indexers)
- **`AsXYZ()` helper** for typed interface access

Example generated structure:

<!-- pseudo:readme-generated-code -->
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
<!-- /snippet -->

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
