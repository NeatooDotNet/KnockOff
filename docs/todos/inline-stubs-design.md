# Inline Stubs via Partial Properties

## Overview

Add support for declaring stubs directly in test classes using `[KnockOff<T>]` attributes and partial properties, providing full IntelliSense while reducing boilerplate.

## Design

### User Experience

```csharp
[KnockOff<IUserService>]
[KnockOff<IRepository>]
public partial class MyTests
{
    protected partial Stubs.IUserService userService { get; }
    protected partial Stubs.IRepository repository { get; }

    [Fact]
    public void Test()
    {
        userService.GetUser.OnCall = id => new User(id, "Test");
        repository.Save.OnCall = entity => { };

        var sut = new Service(userService, repository);
        sut.DoWork(123);

        Assert.Equal(1, userService.GetUser.CallCount);
        Assert.True(repository.Save.WasCalled);
    }
}
```

### Generated Code

For each `[KnockOff<T>]` attribute, the generator produces:

```csharp
public partial class MyTests
{
    // Partial property implementations
    private Stubs.IUserService _userService = new();
    protected partial Stubs.IUserService userService => _userService;

    // Nested Stubs class containing all stub types for this test class
    public static class Stubs
    {
        public class IUserService : global::IUserService
        {
            // Handler properties (direct on type - full IntelliSense)
            public MethodHandler<int, User> GetUser { get; } = new();

            // Explicit interface implementation
            User global::IUserService.GetUser(int id)
            {
                GetUser.RecordCall(id);
                if (GetUser.OnCall != null) return GetUser.OnCall(id);
                return default;
            }
        }
    }
}
```

## Relationship to Existing Pattern

Two patterns will coexist:

| Pattern | Use Case |
|---------|----------|
| **Inline** (`[KnockOff<T>]` on test class) | Quick stubs scoped to one test class |
| **Explicit** (`[KnockOff]` on stub class) | Reusable stubs, custom user methods, shared across tests |

The explicit pattern remains the primary documented approach. Inline is a convenience layer.

## Implementation Plan

### Phase 1: Attribute Support

- [ ] Add `KnockOffAttribute<T>` generic attribute to core library
  - Requires `[AttributeUsage(AllowMultiple = true)]`
  - Target: Class
- [ ] Update generator predicate to find classes with `[KnockOff<T>]` attributes
- [ ] Extract interface types from generic attribute arguments

### Phase 2: Stubs Nested Class Generation

- [ ] Generate `public static class Stubs` inside the target class
- [ ] For each `[KnockOff<T>]` attribute:
  - Generate a stub class named after the interface (e.g., `Stubs.IUserService`)
  - Stub class implements the interface
  - Generate handler properties for each interface member
  - Generate explicit interface implementations that delegate to handlers

### Phase 3: Partial Property Detection & Implementation

- [ ] Find partial property declarations with `Stubs.*` types in the target class
- [ ] Generate backing fields for each partial property
- [ ] Generate partial property implementations returning the backing field
- [ ] Initialize backing fields with `new()`

### Phase 4: Handler Types

Reuse existing handler types from the explicit pattern:

- `MethodHandler<TResult>` - void methods
- `MethodHandler<T1, TResult>` - methods with params
- `PropertyHandler<T>` - properties
- `IndexerHandler<TKey, TValue>` - indexers

### Phase 5: Testing

- [ ] Unit tests for attribute detection
- [ ] Unit tests for Stubs class generation
- [ ] Unit tests for partial property generation
- [ ] Integration tests with actual test scenarios
- [ ] Verify IntelliSense works in IDE (manual)

## Design Decisions

### 1. Naming: Keep interface name as-is

```csharp
Stubs.IUserService userService { get; }  // ✓ Keep the 'I' prefix
```

No ambiguity, matches the interface exactly.

### 2. Property visibility: User-controlled

Generator mirrors whatever visibility the user declares (protected, private, etc.).

### 3. Setters: Get-only

```csharp
protected partial Stubs.IUserService userService { get; }  // ✓ Get-only
```

Replacing a stub mid-test is unusual. If needed, use the explicit pattern.

### 4. Partial property required

If `[KnockOff<IUserService>]` is on the class but no matching partial property exists, nothing is generated for that interface. User must declare property to use it - this gives control over naming.

### 5. Both patterns allowed on same class

A class can have both `[KnockOff<T>]` attributes AND implement interfaces with `[KnockOff]`. This enables the **nested stubs pattern**.

## Nested Stubs Pattern

When an interface returns another interface, use `[KnockOff<T>]` to create stubs for nested types:

```csharp
public interface IFoo
{
    IPropertyInfo this[int index] { get; }
}

public interface IPropertyInfo
{
    string Name { get; }
}

[KnockOff]                    // Generates stub for IFoo on class
[KnockOff<IPropertyInfo>]     // Makes Stubs.IPropertyInfo available
public partial class FooStub : IFoo
{
    protected partial Stubs.IPropertyInfo propStub { get; }
}
```

Usage:
```csharp
var stub = new FooStub();

// Configure the nested stub
stub.propStub.Name.OnGet = () => "TestProp";

// Wire it to the indexer
stub.IFoo.Indexer.OnGet = index => stub.propStub;

// Now stub[0].Name returns "TestProp"
```

Key points:
- No magic deep-stubbing - user explicitly wires nested stubs
- `Stubs.IPropertyInfo` implements `IPropertyInfo`, so it passes type checks
- Readable and debuggable

## File Changes

```
src/
├── KnockOff/
│   └── KnockOffAttribute.cs      # Add generic KnockOffAttribute<T>
├── Generator/
│   ├── KnockOffGenerator.cs      # Update predicate for both patterns
│   ├── InlineStubGenerator.cs    # New: handles [KnockOff<T>] pattern
│   └── Models/
│       └── InlineStubModel.cs    # New: equatable model for inline stubs
└── Tests/
    └── KnockOffGeneratorTests/
        └── InlineStubTests.cs    # New: tests for inline pattern
```

## Example: Full Generated Output

Input:
```csharp
[KnockOff<ICalculator>]
public partial class CalculatorTests
{
    protected partial Stubs.ICalculator calc { get; }
}

public interface ICalculator
{
    int Add(int a, int b);
    int LastResult { get; }
}
```

Output:
```csharp
public partial class CalculatorTests
{
    private Stubs.ICalculator _calc = new();
    protected partial Stubs.ICalculator calc => _calc;

    public static class Stubs
    {
        public class ICalculator : global::ICalculator
        {
            // Method handler
            public MethodHandler<int, int, int> Add { get; } = new();

            // Property handler
            public PropertyHandler<int> LastResult { get; } = new();

            // Explicit interface implementations
            int global::ICalculator.Add(int a, int b)
            {
                Add.RecordCall(a, b);
                if (Add.OnCall != null) return Add.OnCall(a, b);
                return default;
            }

            int global::ICalculator.LastResult
            {
                get
                {
                    LastResult.RecordGet();
                    if (LastResult.OnGet != null) return LastResult.OnGet();
                    return LastResult.Value;
                }
            }
        }
    }
}
```
