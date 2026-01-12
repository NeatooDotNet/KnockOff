# KnockOff Migration Guide

Breaking changes and recommended practice updates across KnockOff versions.

## v10.14.0

### BREAKING: Unified Indexer API

Indexer naming simplified. Single indexer uses `Indexer`; multiple indexers use type suffix.

| Before | After (single indexer) | After (multiple indexers) |
|--------|------------------------|---------------------------|
| `knockOff.StringIndexer` | `knockOff.Indexer` | `knockOff.IndexerString` |
| `knockOff.Int32Indexer` | `knockOff.Indexer` | `knockOff.IndexerInt32` |
| `knockOff.StringIndexerBacking` | `knockOff.Indexer.Backing` | `knockOff.IndexerString.Backing` |

**Before:**
```csharp
knockOff.StringIndexer.OnGet = (ko, key) => values[key];
knockOff.StringIndexerBacking["config"] = defaultValue;
```

**After (single indexer):**
```csharp
knockOff.Indexer.OnGet = (ko, key) => values[key];
knockOff.Indexer.Backing["config"] = defaultValue;
```

**After (multiple indexers):**
```csharp
knockOff.IndexerString.OnGet = (ko, key) => stringValues[key];
knockOff.IndexerInt32.OnGet = (ko, idx) => intValues[idx];
knockOff.IndexerString.Backing["config"] = defaultValue;
```

### BREAKING: Inline Generic Stub Collision Detection

When multiple `[KnockOff<T>]` attributes use the same interface with different type arguments, stub classes now get type-suffixed names.

**Single generic (no change):**
```csharp
[KnockOff<IList<string>>]
public partial class Tests { }
// Generates: Stubs.IList
```

**Collision (type suffix added):**
```csharp
[KnockOff<IList<string>>]
[KnockOff<IList<int>>]
public partial class Tests { }
// Before: Stubs.IList (ambiguous - which one?)
// After:  Stubs.IListString, Stubs.IListInt32
```

**Type suffix rules:**

| Generic Type | Stub Name |
|--------------|-----------|
| `IList<string>` | `IListString` |
| `IList<int>` | `IListInt32` |
| `IDictionary<string, int>` | `IDictionaryStringInt32` |
| `IList<string[]>` | `IListStringArray` |
| `IList<int?>` | `IListNullableInt32` |

### New Feature: Generic Standalone Stubs

v10.14.0 adds support for **generic standalone stubs** - stub classes with their own type parameters that implement generic interfaces.

**Before (concrete stubs only):**
```csharp
// Had to create separate concrete stubs for each type
[KnockOff]
public partial class UserRepositoryStub : IRepository<User> { }

[KnockOff]
public partial class OrderRepositoryStub : IRepository<Order> { }
```

**After (reusable generic stub):**
```csharp
// Single generic stub works with any type
[KnockOff]
public partial class RepositoryStub<T> : IRepository<T> where T : class { }

// Usage
var userRepo = new RepositoryStub<User>();
var orderRepo = new RepositoryStub<Order>();
```

### Type Parameter Constraints

Constraints must match between stub class and interface:

```csharp
public interface IRepository<T> where T : class { }

// Correct: matching constraint
[KnockOff]
public partial class RepositoryStub<T> : IRepository<T> where T : class { }

// Compiler error: constraint mismatch
[KnockOff]
public partial class BadStub<T> : IRepository<T> { }  // Missing 'where T : class'
```

### New Diagnostic: KO0008

**KO0008: Type parameter count mismatch**

Triggered when the stub class has a different number of type parameters than the interface:

```csharp
public interface IRepository<T> { }

// Error KO0008: Generic standalone stub 'BadStub<T, TExtra>' has 2 type parameter(s)
// but interface 'IRepository<T>' has 1. Type parameter count must match exactly.
[KnockOff]
public partial class BadStub<T, TExtra> : IRepository<T> { }
```

**Fix:** Ensure stub class has same arity as interface:
```csharp
[KnockOff]
public partial class GoodStub<T> : IRepository<T> { }
```

### v10.14.0 Checklist

**Indexer API:**
1. [ ] Search for `StringIndexer` → replace with `Indexer` (single) or `IndexerString` (multiple)
2. [ ] Search for `Int32Indexer` → replace with `Indexer` (single) or `IndexerInt32` (multiple)
3. [ ] Search for `StringIndexerBacking` → replace with `Indexer.Backing` or `IndexerString.Backing`
4. [ ] Search for `Int32IndexerBacking` → replace with `Indexer.Backing` or `IndexerInt32.Backing`

**Inline Generic Stubs (only if using same interface with different type args):**
5. [ ] Search for `Stubs.IList` when `IList<string>` and `IList<int>` both exist → use `Stubs.IListString`, `Stubs.IListInt32`
6. [ ] Apply same pattern to other colliding generic interfaces

**Generic Standalone Stubs (new feature):**
7. [ ] Consider converting repeated concrete stubs to generic stubs
8. [ ] Ensure constraint compatibility when creating generic stubs

---

## v10.13.0

### Property Configuration: Prefer `Value` over `OnGet`

v10.13.0 establishes `Value` as the recommended pattern for static property values.

| Pattern | Use Case | Recommendation |
|---------|----------|----------------|
| `stub.Prop.Value = x` | Static test data | **Recommended** |
| `stub.Prop.OnGet = (ko) => x` | Dynamic/computed values | Use when needed |

**Before (verbose for static values):**
```csharp
knockOff.Name.OnGet = (ko) => "John Doe";
knockOff.IsEnabled.OnGet = (ko) => true;
knockOff.RetryCount.OnGet = (ko) => 3;
```

**After (simpler with Value):**
```csharp
knockOff.Name.Value = "John Doe";
knockOff.IsEnabled.Value = true;
knockOff.RetryCount.Value = 3;
```

**When to use `OnGet`:**
```csharp
// Different value each call
var counter = 0;
knockOff.NextId.OnGet = (ko) => ++counter;

// Depends on stub state
knockOff.IsConnected.OnGet = (ko) => ko.Connect.WasCalled;

// Computed from test context
knockOff.CurrentUser.OnGet = (ko) => _testFixture.User;

// Throw on access
knockOff.Secret.OnGet = (ko) => throw new UnauthorizedAccessException();
```

**Decision Guide:**

| Scenario | Use |
|----------|-----|
| Static test data | `Value` |
| Pre-populate before test | `Value` |
| Different value each call | `OnGet` |
| Depends on other stub state | `OnGet` |
| Computed from test context | `OnGet` |
| Throw on access | `OnGet` |

**Rule of thumb:** Start with `Value`. Only use `OnGet` when you need dynamic behavior.

### v10.13.0 Checklist

1. [ ] Search for `.OnGet = (ko) => <literal>` where `<literal>` is a constant → replace with `.Value = <literal>`
2. [ ] Keep `OnGet` for computed, conditional, or state-dependent values
3. [ ] No breaking changes - existing `OnGet` callbacks continue to work

---

## v10.9.0

### Class Stub Composition Pattern

Class stubs now use composition instead of inheritance, providing a unified API with interface stubs.

| Before (v10.8.0) | After (v10.9.0) |
|------------------|-----------------|
| `stub.Interceptor.Member` | `stub.Member` |
| `stub.Interceptor.Member.OnCall` | `stub.Member.OnCall` |
| `TargetClass x = stub;` | `TargetClass x = stub.Object;` |
| `PassToMethod(stub)` | `PassToMethod(stub.Object)` |

**Before (v10.8.0):**
```csharp
var stub = new Stubs.UserService();
stub.Interceptor.GetUser.OnCall = (ko, id) => new User { Id = id };
UserService service = stub;  // Direct assignment
```

**After (v10.9.0):**
```csharp
var stub = new Stubs.UserService();
stub.GetUser.OnCall = (ko, id) => new User { Id = id };  // Direct access!
UserService service = stub.Object;  // Use .Object for type
```

**Why This Change:**
- Unified API: Interface and class stubs now use identical patterns
- No naming conflicts: Interceptor properties can match class member names
- Clearer semantics: Stub is configuration, `.Object` is the instance

### v10.9.0 Checklist

1. [ ] Search for `.Interceptor.` on class stubs → remove `.Interceptor`
2. [ ] Search for assignments of class stubs to target type → add `.Object`
3. [ ] Search for class stub passed as method argument → add `.Object`
4. [ ] Rebuild and run tests

---

## v10.8.0

### Terminology Change: Handler → Interceptor

Generated tracking/callback classes now use "Interceptor" naming:

| Before (v10.7.0) | After (v10.8.0) |
|------------------|-----------------|
| `IUserService_GetUserHandler` | `IUserService_GetUserInterceptor` |
| `IUserServiceKO` | `IUserServiceInterceptors` |

**Most code unaffected.** The common access pattern is unchanged:

```csharp
// Works the same
knockOff.IUserService.GetUser.CallCount
knockOff.IUserService.GetUser.OnCall = (ko, id) => new User();
```

**Migration required** only if you:
1. Reference generated interceptor class types directly
2. Use out/ref parameter callbacks with explicit delegate types

```csharp
// Before
IUserServiceKO ko = knockOff.IUserService;
knockOff.IParser.TryParse.OnCall =
    (IParser_TryParseHandler.TryParseDelegate)(...);

// After
IUserServiceInterceptors interceptors = knockOff.IUserService;
knockOff.IParser.TryParse.OnCall =
    (IParser_TryParseInterceptor.TryParseDelegate)(...);
```

### v10.8.0 Checklist

1. [ ] Search for `KO` property type references → rename to `Interceptors`
2. [ ] Search for `Handler.` in delegate casts → rename to `Interceptor.`
3. [ ] Rebuild and run tests

---

## v10.7.0

> **Note:** The KO suffix introduced in v10.7.0 was further renamed to `Interceptors` in v10.8.0.

### Container Class Rename: Spy to KO

Generated container classes renamed from `{Interface}Spy` to `{Interface}KO`.

| Before | After |
|--------|-------|
| `ILoggerSpy` | `ILoggerKO` |
| `IUserServiceSpy` | `IUserServiceKO` |

**Most code unaffected.** The common access pattern is unchanged:

```csharp
// Works the same
knockOff.ILogger.Log.CallCount
knockOff.IUserService.Name.OnGet = (ko) => "Test";
```

**Migration required** only if you directly reference the container class type:

```csharp
// Before
ILoggerSpy spy = knockOff.ILogger;

// After
ILoggerKO ko = knockOff.ILogger;
```

### Zero-Allocation Tracking: Removed Properties

| Interceptor Type | Removed |
|--------------|---------|
| Method | `AllCalls` |
| Indexer | `AllGetKeys`, `AllSetEntries` |
| Generic `.Of<T>()` | `AllCalls` |

**Migration:**

```csharp
// Before
var allCalls = knockOff.IService.GetUser.AllCalls;
Assert.Equal(3, allCalls.Count);

// After - use count and last value
Assert.Equal(3, knockOff.IService.GetUser.CallCount);
Assert.Equal(42, knockOff.IService.GetUser.LastCallArg);

// Or capture in callback if you need full history
var allCalls = new List<int>();
knockOff.IService.GetUser.OnCall = (ko, id) =>
{
    allCalls.Add(id);
    return new User { Id = id };
};
```

### v10.7.0 Checklist

1. [ ] Search for `Spy` class references -> rename to `KO`
2. [ ] Search for `.AllCalls` -> use `CallCount` + `LastCallArg`
3. [ ] Search for `.AllGetKeys` -> use `GetCount` + `LastGetKey`
4. [ ] Search for `.AllSetEntries` -> use `SetCount` + `LastSetEntry`
5. [ ] Rebuild and run tests
