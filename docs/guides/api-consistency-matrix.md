# API Consistency Matrix

This document maps KnockOff's API across the 8 interface/class stub patterns (2×2×2 matrix) to demonstrate consistency and document intentional variations.

## The 2×2×2 Matrix

|  | **Interface** | **Class** |
|---|---|---|
| **Standalone** | Pattern 1: `[KnockOff]` | Pattern 3: `[KnockOffBase<T>]` |
| **Standalone Generic** | Pattern 2: `[KnockOff]` on `<T>` | Pattern 4: `[KnockOffBase(typeof(T<>))]` |
| **Inline** | Pattern 5: `[KnockOff<IFoo>]` | Pattern 6: `[KnockOff<Foo>]` |
| **Inline Generic** | Pattern 8: `[KnockOff(typeof(IFoo<>))]` | Pattern 9: `[KnockOff(typeof(Foo<>))]` |

*Pattern 7 (Inline Delegate) is a separate category.*

---

## Feature 1: Instantiation & Target Access

| | **Interface** | **Class** |
|---|---|---|
| **Standalone** | `var stub = new FooStub();`<br>`IFoo foo = stub;` | `var stub = new FooStub();`<br>`Foo foo = stub.Object;` |
| **Standalone Generic** | `var stub = new FooStub<T>();`<br>`IFoo<T> foo = stub;` | `var stub = new FooStub<T>();`<br>`Foo<T> foo = stub.Object;` |
| **Inline** | `var stub = new Stubs.IFoo();`<br>`IFoo foo = stub;` | `var stub = new Stubs.Foo();`<br>`Foo foo = stub.Object;` |
| **Inline Generic** | `var stub = new Stubs.IFoo<T>();`<br>`IFoo<T> foo = stub;` | `var stub = new Stubs.Foo<T>();`<br>`Foo<T> foo = stub.Object;` |

**Rule:** Interface stubs allow direct assignment. Class stubs require `.Object`.

**Why:** Class stubs use composition (wrapper + nested Impl) to avoid name collisions between interceptor properties and overridden members.

---

## Feature 2: Method Interception

All 8 patterns use identical API:

```csharp
// Configure behavior
stub.MethodName.Returns(value);
stub.MethodName.OnCall((arg1, arg2) => result);

// Verify calls
stub.MethodName.Verify();
stub.MethodName.Verify(Times.Once);
stub.MethodName.Verify(Times.Exactly(3));
stub.MethodName.Verify(Times.AtLeast(1));
stub.MethodName.Verify(Times.AtMost(5));
stub.MethodName.Verify(Times.Never);

// Access call history
var lastArg = stub.MethodName.LastCallArg;      // Single parameter
var args = stub.MethodName.LastArgs;            // Tuple for multiple
var count = stub.MethodName.CallCount;
```

| Feature | All 8 Patterns |
|---------|:--------------:|
| `Returns(value)` | ✓ |
| `OnCall((args) => result)` | ✓ |
| `Verify(Times.X)` | ✓ |
| `LastCallArg` / `LastArgs` | ✓ |
| `CallCount` | ✓ |

---

## Feature 3: Property Interception

All 8 patterns use identical API:

```csharp
// Configure getter
stub.PropertyName.OnGet(() => value);
stub.PropertyName.OnGet(value);  // Shorthand

// Configure setter
stub.PropertyName.OnSet((value) => { /* capture or validate */ });

// Verify
stub.PropertyName.VerifyGet();
stub.PropertyName.VerifyGet(Times.Exactly(2));
stub.PropertyName.VerifySet();
stub.PropertyName.VerifySet(Times.Once);

// Access history
var lastSet = stub.PropertyName.LastSetValue;
```

| Feature | All 8 Patterns |
|---------|:--------------:|
| `OnGet(() => value)` | ✓ |
| `OnSet((v) => { })` | ✓ |
| `VerifyGet(Times.X)` | ✓ |
| `VerifySet(Times.X)` | ✓ |
| `LastSetValue` | ✓ |

---

## Feature 4: Indexer Interception

All 8 patterns use identical API:

```csharp
// Configure getter
stub.Indexer.OnGet((key) => value);

// Configure setter
stub.Indexer.OnSet((key, value) => { });

// Use backing dictionary
stub.Indexer.Backing[key] = value;

// Verify
stub.Indexer.VerifyGet();
stub.Indexer.VerifySet();

// Access history
var lastKey = stub.Indexer.LastGetKey;
var lastSetKey = stub.Indexer.LastSetKey;
```

| Feature | All 8 Patterns |
|---------|:--------------:|
| `OnGet((key) => value)` | ✓ |
| `OnSet((key, value) => { })` | ✓ |
| `Backing` dictionary | ✓ |
| `VerifyGet()` / `VerifySet()` | ✓ |

---

## Feature 5: Event Interception

All 8 patterns use identical API:

```csharp
// Access handler to raise event
stub.EventNameInterceptor.Handler?.Invoke(sender, args);

// Or use Raise helper
stub.EventNameInterceptor.Raise(sender, args);

// Check subscription
bool hasSubscribers = stub.EventNameInterceptor.HasSubscribers;

// Verify add/remove
stub.EventNameInterceptor.VerifyAdd();
stub.EventNameInterceptor.VerifyAdd(Times.Once);
stub.EventNameInterceptor.VerifyRemove();
```

| Feature | All 8 Patterns |
|---------|:--------------:|
| `Handler` property | ✓ |
| `Raise(sender, args)` | ✓ |
| `HasSubscribers` | ✓ |
| `VerifyAdd(Times.X)` | ✓ |
| `VerifyRemove(Times.X)` | ✓ |

**Note:** Event interceptors have `Interceptor` suffix (e.g., `CompletedInterceptor` not `Completed`).

---

## Feature 6: Sequences

All 8 patterns use identical API:

```csharp
// Return different values on successive calls
stub.Method
    .OnCall((x) => 1)
    .ThenCall((x) => 2)
    .ThenCall((x) => 3);
// Call 1: 1, Call 2: 2, Call 3+: 3 (repeats last)

// Return default after sequence
stub.Method
    .OnCall((x) => 1)
    .ThenCall((x) => 2)
    .ThenDefault();
// Call 1: 1, Call 2: 2, Call 3+: default(T)

// Properties support sequences too
stub.Property
    .OnGet(() => "first")
    .ThenGet(() => "second");
```

| Feature | All 8 Patterns |
|---------|:--------------:|
| `OnCall().ThenCall()` | ✓ |
| `ThenDefault()` | ✓ |
| Repeats last value | ✓ |
| Property sequences | ✓ |

---

## Feature 7: Conditional Matching (When)

All 8 patterns use identical API:

```csharp
// Match specific values
stub.Add.When(1, 2).Returns(100);
stub.Add.When(5, 5).Returns(999);

// Match with predicate
stub.Add.When((a, b) => a > 100).Returns(-1);

// Chain multiple conditions
stub.Add
    .When(1, 2).Returns(100)
    .ThenWhen(3, 4).Returns(200)
    .ThenWhen((a, b) => a < 0).Returns(0);

// Fallback for non-matching calls
stub.Add.Returns(42);  // Default if no When matches
```

| Feature | All 8 Patterns |
|---------|:--------------:|
| `When(values).Returns()` | ✓ |
| `When(predicate).Returns()` | ✓ |
| `ThenWhen()` chaining | ✓ |
| Fallback behavior | ✓ |

**Priority:** When > Sequence > Returns > OnCall

---

## Feature 8: Verification

All 8 patterns use identical API:

```csharp
// Mark for verification
stub.Method.OnCall((x) => x).Verifiable();
stub.Property.OnGet(() => "v").Verifiable();

// Verify only marked items
stub.Verify();  // Throws if any Verifiable() not called

// Verify all configured items
stub.VerifyAll();  // Throws if any configured member not called

// Individual member verification
stub.Method.Verify(Times.Once);
stub.Property.VerifyGet(Times.Exactly(2));
```

| Feature | All 8 Patterns |
|---------|:--------------:|
| `.Verifiable()` | ✓ |
| `stub.Verify()` | ✓ |
| `stub.VerifyAll()` | ✓ |
| `Times.Once/Never/Exactly/AtLeast/AtMost` | ✓ |

---

## Feature 9: Strict Mode

All 8 patterns use identical API:

```csharp
// Enable strict mode
stub.Strict = true;
// Or fluently
var stub = new FooStub().Strict();

// Assembly-level strict mode
[assembly: KnockOffStrict]
```

| Behavior | Interface Stubs | Class Stubs |
|----------|-----------------|-------------|
| Unconfigured method | Throws `StubException` | Throws `StubException` |
| Non-strict unconfigured | Returns smart default | Calls base class |

| Feature | All 8 Patterns |
|---------|:--------------:|
| `stub.Strict = true` | ✓ |
| `.Strict()` extension | ✓ |
| `[assembly: KnockOffStrict]` | ✓ |
| Throws `StubException` | ✓ |

---

## Feature 10: Reset

All 8 patterns use identical API:

```csharp
// Reset individual member
stub.Method.Reset();

// Reset all interceptors
stub.ResetInterceptors();
```

Reset clears:
- OnCall/Returns configuration
- When matchers
- Call history (CallCount, LastArg)
- Sequence position
- Verifiable marking

| Feature | All 8 Patterns |
|---------|:--------------:|
| `member.Reset()` | ✓ |
| `stub.ResetInterceptors()` | ✓ |

---

## Feature 11: User Methods

This is the one feature with intentional variation:

| | **Interface** | **Class** |
|---|---|---|
| **Standalone** | ✓ Add custom methods | ✓ Add custom methods<br>✓ Override with `_` suffix |
| **Standalone Generic** | ✓ Add custom methods | ✓ Add custom methods<br>✓ Override with `_` suffix |
| **Inline** | ✗ Fully generated | ✗ Fully generated |
| **Inline Generic** | ✗ Fully generated | ✗ Fully generated |

**Standalone patterns** allow user-defined methods in the partial class:

```csharp
[KnockOff]
public partial class CalculatorStub : ICalculator
{
    // Custom helper method
    public void SetupForDivisionTests()
    {
        Divide.OnCall((a, b) => b == 0 ? throw new DivideByZeroException() : a / b);
    }
}

[KnockOffBase<ServiceBase>]
public partial class ServiceStub
{
    // Override base class virtual method
    protected override string Execute_(string cmd)
    {
        return $"Overridden: {cmd}";
    }
}
```

**Inline patterns** are fully generated and cannot be extended.

---

## Summary: Consistency Status

| Feature Category | Status |
|------------------|--------|
| Method Interception | ✓ **100% consistent** |
| Property Interception | ✓ **100% consistent** |
| Indexer Interception | ✓ **100% consistent** |
| Event Interception | ✓ **100% consistent** |
| Sequences | ✓ **100% consistent** |
| Conditional Matching | ✓ **100% consistent** |
| Verification | ✓ **100% consistent** |
| Strict Mode | ✓ **100% consistent** |
| Reset | ✓ **100% consistent** |
| Target Access | ✓ **Logical split** (Interface=direct, Class=`.Object`) |
| User Methods | ✓ **Logical split** (Standalone=yes, Inline=no) |

---

## Quick Reference: All Patterns Side-by-Side

```csharp
// Pattern 1: Standalone Interface
[KnockOff]
public partial class CalcStub : ICalculator { }
var stub = new CalcStub();
ICalculator calc = stub;

// Pattern 2: Generic Standalone Interface
[KnockOff]
public partial class RepoStub<T> : IRepository<T> { }
var stub = new RepoStub<User>();
IRepository<User> repo = stub;

// Pattern 3: Standalone Class
[KnockOffBase<ServiceBase>]
public partial class ServiceStub { }
var stub = new ServiceStub();
ServiceBase svc = stub.Object;

// Pattern 4: Generic Standalone Class
[KnockOffBase(typeof(ServiceBase<>))]
public partial class ServiceStub<T> { }
var stub = new ServiceStub<User>();
ServiceBase<User> svc = stub.Object;

// Pattern 5: Inline Interface
[KnockOff<ICalculator>]
public partial class MyTests { }
var stub = new Stubs.ICalculator();
ICalculator calc = stub;

// Pattern 6: Inline Class
[KnockOff<ServiceBase>]
public partial class MyTests { }
var stub = new Stubs.ServiceBase();
ServiceBase svc = stub.Object;

// Pattern 8: Open Generic Interface
[KnockOff(typeof(IRepository<>))]
public partial class MyTests { }
var stub = new Stubs.IRepository<User>();
IRepository<User> repo = stub;

// Pattern 9: Open Generic Class
[KnockOff(typeof(ServiceBase<>))]
public partial class MyTests { }
var stub = new Stubs.ServiceBase<User>();
ServiceBase<User> svc = stub.Object;
```

---

## Intentional Variations Explained

### Why Class Stubs Require `.Object`

Class stubs use a composition pattern (wrapper + nested Impl) to avoid C# compilation errors. If the stub class inherited from the target, there would be name collisions between:
- Interceptor properties (`Name`, `Execute`)
- Overridden members (`override string Name`, `override void Execute()`)

The `.Object` property returns the nested Impl instance that actually inherits from the target class.

### Why Only Standalone Patterns Support User Methods

Inline stubs are fully generated inside the test class's `Stubs` namespace. There's no partial class for users to extend. Standalone stubs are partial classes that users define, allowing them to add custom methods, constructors, and (for class stubs) override base class methods.
