# Class Stubbing via Inheritance

## Overview

Extend KnockOff to stub concrete classes using **inheritance**. The stub derives from the target class and overrides virtual/abstract members, enabling interception while maintaining **substitutability**.

## Core Requirement

**Stubs must be substitutable for the stubbed type.**

A `PersonStub` must be passable anywhere a `Person` is expected. This is the fundamental requirement that drives every design decision.

```csharp
void ProcessPerson(Person person) { ... }

var stub = new PersonStub(25);
ProcessPerson(stub);  // ✅ Must work - PersonStub IS-A Person
```

## Core Concept

```csharp
public class Person
{
    public Person(int age) { Age = age; }
    public int Age { get; }                    // Non-virtual - cannot intercept
    public virtual string Name { get; set; }   // Virtual - interceptable!
    public virtual void Save() { }             // Virtual - interceptable!
}

// User writes:
[KnockOff<Person>]
public partial class PersonStub { }

// Generated:
public partial class PersonStub : Person
{
    public PersonInterceptors Interceptor { get; } = new();

    public PersonStub(int age) : base(age) { }

    public override string Name
    {
        get
        {
            Interceptor.Name.RecordGet();
            if (Interceptor.Name.OnGet is { } onGet) return onGet(this);
            return base.Name;
        }
        set
        {
            Interceptor.Name.RecordSet(value);
            if (Interceptor.Name.OnSet is { } onSet) onSet(this, value);
            else base.Name = value;
        }
    }

    public override void Save()
    {
        Interceptor.Save.RecordCall();
        if (Interceptor.Save.OnCall is { } onCall) onCall(this);
        else base.Save();
    }

    // Interceptor classes and container...
}
```

Usage:
```csharp
var stub = new PersonStub(25);
stub.Interceptor.Name.OnGet = _ => "Intercepted";
stub.Interceptor.Save.OnCall = _ => { /* custom */ };

ProcessPerson(stub);  // ✅ Works - PersonStub IS-A Person

Assert.Equal("Intercepted", stub.Name);
Assert.Equal(1, stub.Interceptor.Save.CallCount);
```

## Why Inheritance

| Composition (rejected) | Inheritance (chosen) |
|------------------------|---------------------|
| ❌ Stub NOT substitutable | ✅ Stub IS-A target class |
| Can intercept all public members | Only virtual/abstract interceptable |
| Works with sealed classes | ❌ Cannot stub sealed classes |
| Generates interface from class | Uses class directly |

**Substitutability is non-negotiable.** The trade-off of losing sealed classes and non-virtual members is acceptable because the stub actually works in real-world scenarios.

## Design Decisions

### 1. Constructor Chaining

Stub constructors **call base constructors** with matching parameters:

```csharp
public class Person
{
    public Person() { }
    public Person(int age) { }
    public Person(string name, int age) { }
}

// Generated:
public partial class PersonStub : Person
{
    public PersonInterceptors Interceptor { get; } = new();

    public PersonStub() : base() { }

    public PersonStub(int age) : base(age) { }

    public PersonStub(string name, int age) : base(name, age) { }
}
```

### 2. Naming Conventions

Interceptor container uses `Interceptor` property (to avoid name collision with the stub class itself):

```csharp
// Interface stubs:
stub.IUserService.GetUser.CallCount

// Class stubs:
stub.Interceptor.Name.GetCount
stub.Interceptor.Save.CallCount
```

Full naming scheme:
- Interceptor container: `{ClassName}Interceptors`
- Interceptor container property: `Interceptor` (e.g., `stub.Interceptor`)
- Individual interceptors: `{ClassName}_{MemberName}Interceptor`

```csharp
stub.Interceptor.Name.GetCount       // Property tracking
stub.Interceptor.Save.CallCount      // Method tracking
stub.Interceptor.Save.OnCall         // Callback configuration
```

### 3. Member Inclusion

Only **virtual** and **abstract** members can be intercepted:

Included:
- Virtual properties (get/set/both)
- Virtual methods
- Virtual indexers
- Abstract properties, methods, indexers

Excluded (with KO2003 info diagnostic):
- Non-virtual members (cannot override)
- Static members
- Sealed overrides

### 4. Default Behavior

When no callback configured:
- **Properties**: delegate to `base.Property`
- **Methods**: delegate to `base.Method(...)`
- **Abstract members**: return `default(T)` (cannot delegate to base)

```csharp
public override string Name
{
    get
    {
        Interceptor.Name.RecordGet();
        if (Interceptor.Name.OnGet is { } onGet) return onGet(this);
        return base.Name;  // Delegate to base
    }
    set
    {
        Interceptor.Name.RecordSet(value);
        if (Interceptor.Name.OnSet is { } onSet) onSet(this, value);
        else base.Name = value;  // Delegate to base
    }
}

public override void Save()
{
    Interceptor.Save.RecordCall();
    if (Interceptor.Save.OnCall is { } onCall)
        onCall(this);
    else
        base.Save();  // Delegate to base
}
```

### 5. Relationship to Interface Stubbing

Both patterns use similar interception, but differ in substitutability:

| Interface Stub | Class Stub |
|----------------|------------|
| `[KnockOff]` on class implementing interface | `[KnockOff<SomeClass>]` on partial class |
| Implements user-provided interface | Inherits from target class |
| Substitutable for interface | Substitutable for class |
| All interface members interceptable | Only virtual/abstract interceptable |
| Default: returns `default` | Default: delegates to base |

## Generated Code Pattern

### Full Example

Input:
```csharp
public class UserService
{
    public UserService(IDatabase db) { _db = db; }
    private readonly IDatabase _db;

    public virtual string CurrentUser { get; set; }
    public virtual User GetUser(int id) => _db.Find<User>(id);
    public virtual void SaveUser(User user) => _db.Save(user);
    public string ConnectionString => _db.ConnectionString;  // Non-virtual
}

[KnockOff<UserService>]
public partial class UserServiceStub { }
```

Output:
```csharp
public partial class UserServiceStub : UserService
{
    // Interceptor properties (via container, using Interceptor to avoid name collision)
    public UserServiceInterceptors Interceptor { get; } = new();

    // Constructor chains to base
    public UserServiceStub(IDatabase db) : base(db) { }

    // Property interceptor class
    public sealed class UserService_CurrentUserInterceptor
    {
        public delegate string GetDelegate(UserServiceStub ko);
        public delegate void SetDelegate(UserServiceStub ko, string value);

        public int GetCount { get; private set; }
        public int SetCount { get; private set; }
        public string? LastSetValue { get; private set; }
        public GetDelegate? OnGet { get; set; }
        public SetDelegate? OnSet { get; set; }

        public void RecordGet() => GetCount++;
        public void RecordSet(string value) { SetCount++; LastSetValue = value; }
        public void Reset() { GetCount = 0; SetCount = 0; LastSetValue = default; OnGet = null; OnSet = null; }
    }

    // Method interceptor classes
    public sealed class UserService_GetUserInterceptor
    {
        public delegate User GetUserDelegate(UserServiceStub ko, int id);

        public int CallCount { get; private set; }
        public bool WasCalled => CallCount > 0;
        public int? LastCallArg { get; private set; }
        public GetUserDelegate? OnCall { get; set; }

        public void RecordCall(int id) { CallCount++; LastCallArg = id; }
        public void Reset() { CallCount = 0; LastCallArg = default; OnCall = null; }
    }

    public sealed class UserService_SaveUserInterceptor
    {
        public delegate void SaveUserDelegate(UserServiceStub ko, User user);

        public int CallCount { get; private set; }
        public bool WasCalled => CallCount > 0;
        public User? LastCallArg { get; private set; }
        public SaveUserDelegate? OnCall { get; set; }

        public void RecordCall(User user) { CallCount++; LastCallArg = user; }
        public void Reset() { CallCount = 0; LastCallArg = default; OnCall = null; }
    }

    // Interceptor container
    public sealed class UserServiceInterceptors
    {
        public UserService_CurrentUserInterceptor CurrentUser { get; } = new();
        public UserService_GetUserInterceptor GetUser { get; } = new();
        public UserService_SaveUserInterceptor SaveUser { get; } = new();

        public void Reset()
        {
            CurrentUser.Reset();
            GetUser.Reset();
            SaveUser.Reset();
        }
    }

    // Overrides (only virtual members)
    public override string CurrentUser
    {
        get
        {
            Interceptor.CurrentUser.RecordGet();
            if (Interceptor.CurrentUser.OnGet is { } onGet) return onGet(this);
            return base.CurrentUser;
        }
        set
        {
            Interceptor.CurrentUser.RecordSet(value);
            if (Interceptor.CurrentUser.OnSet is { } onSet) onSet(this, value);
            else base.CurrentUser = value;
        }
    }

    public override User GetUser(int id)
    {
        Interceptor.GetUser.RecordCall(id);
        if (Interceptor.GetUser.OnCall is { } onCall)
            return onCall(this, id);
        return base.GetUser(id);
    }

    public override void SaveUser(User user)
    {
        Interceptor.SaveUser.RecordCall(user);
        if (Interceptor.SaveUser.OnCall is { } onCall)
            onCall(this, user);
        else
            base.SaveUser(user);
    }

    // Note: ConnectionString is non-virtual, cannot be overridden
    // KO2003 info diagnostic emitted

    // Reset all interceptors
    public void ResetInterceptors()
    {
        Interceptor.Reset();
    }
}
```

## Diagnostics

| ID | Severity | Scenario | Message |
|----|----------|----------|---------|
| KO2001 | Error | `[KnockOff<T>]` where T is sealed | `Cannot stub sealed class '{0}'` |
| KO2002 | Error | `[KnockOff<T>]` where T has no accessible constructors | `Type '{0}' has no accessible constructors` |
| KO2003 | Info | Non-virtual member skipped | `Member '{0}.{1}' is not virtual and cannot be intercepted` |
| KO2004 | Warning | No virtual/abstract members | `Class '{0}' has no virtual or abstract members to intercept` |
| KO2005 | Error | `[KnockOff<T>]` where T is static class | `Cannot stub static class '{0}'` |
| KO2006 | Error | `[KnockOff<T>]` where T is a primitive/built-in type | `Cannot stub built-in type '{0}'` |

## Implementation Plan

### Phase 1: Attribute & Detection

- [x] Update generator predicate to detect `[KnockOff<T>]` where T is a class
- [x] Create `ClassModel` (equatable) to capture:
  - Class name and namespace
  - All virtual/abstract instance members
  - All accessible constructors with parameters
  - Non-virtual members (for KO2003 diagnostics)
- [x] Emit KO2001-KO2006 diagnostics

### Phase 2: Constructor Generation

- [x] Detect all accessible constructors of target class
- [x] Generate matching constructors in stub class
- [x] Chain to base constructor with `: base(...)`

### Phase 3: Interceptor Generation

Reuse existing interceptor generation logic from interface stubs:

- [x] Generate property interceptor classes (`GetCount`, `SetCount`, `OnGet`, `OnSet`, etc.)
- [x] Generate method interceptor classes (`CallCount`, `LastCallArg`, `OnCall`, etc.)
- [x] Generate indexer interceptor classes
- [x] Generate interceptor container class (`{ClassName}Interceptors`)
- [x] Generate container property on stub class

### Phase 4: Override Generation

- [x] Generate `override` for each virtual/abstract property
- [x] Generate `override` for each virtual/abstract method
- [x] Generate `override` for each virtual/abstract indexer
- [x] Track access, invoke callback or delegate to base

### Phase 5: Testing

- [x] Test: Simple class with virtual properties and methods
- [x] Test: Class with multiple constructors
- [x] Test: Class with constructor parameters
- [ ] Test: Class with virtual indexers
- [x] Test: Abstract class with abstract members
- [x] Test: Class with mix of virtual and non-virtual members
- [x] Test: Callback interception
- [x] Test: Default delegation to base
- [x] Test: Substitutability - passing stub to method expecting base class
- [ ] Test: Sealed class emits KO2001 error
- [ ] Test: Class with no virtual members emits KO2004 warning

### Phase 6: Documentation

- [ ] Add samples to `KnockOff.Documentation.Samples/ClassStubs/`
- [ ] Create `docs/guides/class-stubs.md`
- [ ] Update getting-started to mention class stubbing
- [ ] Add skill detail file for class stubs

## Edge Cases

### Inherited Virtual Members

Virtual members from base classes are also intercepted:

```csharp
public class BaseEntity
{
    public virtual int Id { get; set; }
}

public class User : BaseEntity
{
    public virtual string Name { get; set; }
}

[KnockOff<User>]
public partial class UserStub { }

// Generated - both inherited and direct members intercepted:
public partial class UserStub : User
{
    public UserInterceptors Interceptor { get; } = new();

    public override int Id { ... }     // Inherited from BaseEntity
    public override string Name { ... } // From User
}
```

The stub can override anything `User` can override. Inheritance hierarchy is an implementation detail.

### Generic Classes

```csharp
public class Repository<T> where T : class
{
    public virtual T? Find(int id) { ... }
    public virtual void Save(T entity) { ... }
}

[KnockOff<Repository<User>>]
public partial class UserRepositoryStub { }

// Generates:
public partial class UserRepositoryStub : Repository<User>
{
    public Repository_UserInterceptors Interceptor { get; } = new();
    // Overrides for Find and Save...
}
```

Decision: Generate for closed generic types. Open generics can be future enhancement.

### Overloaded Methods

```csharp
public class Service
{
    public virtual void Process(int id) { }
    public virtual void Process(string name) { }
    public virtual void Process(string name, int priority) { }
}
```

Each overload gets its own intercept with numeric suffix (1-based):
- `stub.Interceptor.Process1.CallCount` - `Process(int id)`
- `stub.Interceptor.Process2.CallCount` - `Process(string name)`
- `stub.Interceptor.Process3.CallCount` - `Process(string name, int priority)`

Numeric suffixes are simple and predictable. XML doc comments on each handler describe the actual signature.

### Abstract Classes

Abstract classes work naturally with inheritance:

```csharp
public abstract class BaseService
{
    public abstract void DoWork();
    public virtual void Initialize() { }
}

[KnockOff<BaseService>]
public partial class BaseServiceStub { }

// Generates:
public partial class BaseServiceStub : BaseService
{
    public BaseServiceInterceptors Interceptor { get; } = new();

    public override void DoWork()
    {
        Interceptor.DoWork.RecordCall();
        if (Interceptor.DoWork.OnCall is { } onCall) onCall(this);
        // No base.DoWork() - it's abstract
    }

    public override void Initialize()
    {
        Interceptor.Initialize.RecordCall();
        if (Interceptor.Initialize.OnCall is { } onCall) onCall(this);
        else base.Initialize();
    }
}
```

### Protected Virtual Members

Protected virtual members can also be overridden:

```csharp
public class Service
{
    protected virtual void InternalProcess() { }
}

// Generated override:
protected override void InternalProcess()
{
    Interceptor.InternalProcess.RecordCall();
    if (Interceptor.InternalProcess.OnCall is { } onCall) onCall(this);
    else base.InternalProcess();
}
```

The intercept is still public for test access. However, protected members cannot be directly invoked from external test code. To invoke protected members, either test through public methods that call them, or add a public helper method in the user's partial class:

```csharp
[KnockOff<Service>]
public partial class ServiceStub
{
    public void CallInternalProcess() => InternalProcess();
}
```

### Internal Classes

Internal classes in the same assembly can be stubbed. Cross-assembly requires `InternalsVisibleTo`.

### ref/out Parameters

Methods with `ref` or `out` parameters have limited support:

```csharp
public class Parser
{
    public virtual bool TryParse(string input, out int result) { ... }
}
```

The intercept tracks the call but `OnCall` uses `Func<>` which cannot represent ref/out:

```csharp
public sealed class Parser_TryParseInterceptor
{
    public int CallCount { get; private set; }
    public string? LastCallArg_input { get; private set; }
    // Note: Cannot capture 'out result' in LastCallArg

    // OnCall cannot have ref/out - delegates to base if set
    public Func<string, bool>? OnCall { get; set; }

    public void RecordCall(string input) { CallCount++; LastCallArg_input = input; }
}

public override bool TryParse(string input, out int result)
{
    Parser.TryParse.RecordCall(input);
    if (Parser.TryParse.OnCall is { } onCall)
    {
        result = default;  // Cannot be set by OnCall
        return onCall(input);
    }
    return base.TryParse(input, out result);
}
```

For full control over ref/out parameters, users should create a manual stub class or use the explicit `[KnockOff]` pattern with user-defined methods.

### Combining Interface and Class Stubbing

A single stub class can use both patterns:

```csharp
[KnockOff<BaseService>]  // Class stub - inherit from BaseService
[KnockOff<ILogger>]       // Inline interface stub
public partial class ServiceStub { }
```

This creates a stub that:
1. Inherits from `BaseService` (class stubbing)
2. Has a `Stubs.ILogger` available (inline pattern)

The stub would have:
- `Interceptor` property (class intercepts for virtual members)
- `Stubs` nested class with `ILogger` (interface stub)
- `ResetInterceptors()` resets both

### `new virtual` Hiding

When a class uses `new virtual` to hide a base member:

```csharp
public class A { public virtual void M() { } }
public class B : A { public new virtual void M() { } }  // Hides A.M

[KnockOff<B>]
public partial class BStub { }
```

The generated `override` intercepts `B.M()` (the hiding member), not `A.M()`. This is standard C# override semantics. The hidden base member `A.M()` remains accessible via `((A)stub).M()` but is not intercepted.

### Thread Safety

Interceptors are **not thread-safe**. The `CallCount`, `GetCount`, `SetCount` properties use simple increment operations without synchronization. This is intentional:

- Unit tests typically run single-threaded
- Thread-safe counters add overhead
- Users needing thread safety can implement custom intercept logic via `OnCall`

## Comparison to Moq

| Feature | Moq | KnockOff Class Stubs |
|---------|-----|---------------------|
| Virtual-only interception | Yes | Yes |
| Sealed class support | No | No |
| Compile-time safety | Partial | Full |
| Setup syntax | Fluent runtime | Properties + callbacks |
| Verification | Verify() methods | Interceptor properties |
| Performance | Reflection/Castle proxy | Source-generated |
| Substitutability | Yes | Yes |

## Future Enhancements

- [ ] Event stubbing
- [ ] Open generic class support
- [ ] `new` keyword for hiding non-virtual members (with warning)

## Status

**Status**: Implemented (v10.7.0)

## Results/Conclusions

### Implementation Complete

Class stubbing via inheritance is fully implemented in the KnockOff source generator. The feature allows `[KnockOff<T>]` to work with classes (not just interfaces and delegates) by generating stub classes that inherit from the target class and override virtual/abstract members.

### Key Design Change: Interceptors Property Name

The original design proposed naming the interceptors property after the class name (e.g., `stub.SimpleService.Name.GetCount`). However, this creates a CS0542 compile error because the generated stub class `SimpleService` cannot have a property also named `SimpleService`.

**Solution**: The interceptors property is named `Interceptor` instead:

```csharp
// Generated:
public class SimpleService : KnockOff.Tests.SimpleService
{
    public SimpleServiceInterceptors Interceptor { get; } = new();  // Not "SimpleService"

    public override string Name
    {
        get
        {
            Interceptor.Name.RecordGet();  // Access via Interceptor, not SimpleService
            if (Interceptor.Name.OnGet is { } onGet) return onGet(this);
            return base.Name;
        }
        // ...
    }
}

// Usage:
var stub = new Stubs.SimpleService();
stub.Interceptor.Name.OnGet = _ => "Intercepted";
Assert.Equal(1, stub.Interceptor.Name.GetCount);
```

This provides a clear, readable access pattern for the interceptor container.

### Test Coverage

23 unit tests verify class stubbing functionality:
- Simple virtual properties and methods
- Multiple constructors with parameter chaining
- Abstract class with abstract members
- Mixed virtual/non-virtual members (only virtual intercepted)
- Callback interception
- Default delegation to base class
- Substitutability (stub IS-A base class)

### Remaining Work

A few test cases remain uncovered:
- Virtual indexers on classes
- Diagnostic emission tests (KO2001 sealed, KO2004 no virtual members)

These are edge cases that can be addressed in future iterations.
