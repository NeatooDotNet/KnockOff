# KnockOff

A Roslyn Source Generator for creating unit test stubs. Unlike Moq's fluent runtime configuration, KnockOff uses partial classes for compile-time setup—trading flexibility for readability and performance.

## Concept

Mark a partial class with `[KnockOff]` that implements an interface. The source generator:
1. Generates explicit interface implementations for all interface members
2. Tracks invocations via `ExecutionInfo` for test verification
3. Detects user-defined methods in the partial class and calls them from the generated intercepts

## Example

```csharp
public interface IUserService
{
    Role Role { get; set; }
    void SomeMethod();
    User GetUser(int id);
}

// User writes this:
[KnockOff]
public partial class UserServiceKnockOff : IUserService
{
    // Optional: Override behavior for specific members
    protected User GetUser(int id)
    {
        return new User { Id = id, Name = "Test User" };
    }
}

// Source generator produces:
public partial class UserServiceKnockOff
{
    public ExecutionInfo ExecutionInfo { get; } = new ExecutionInfo();

    public class ExecutionInfo
    {
        public ExecutionDetails<Role> Role { get; } = new();
        public ExecutionDetails SomeMethod { get; } = new();
        public ExecutionDetails<User, int> GetUser { get; } = new();
    }

    // Backing member for property
    protected Role Role { get; set; }

    Role IUserService.Role
    {
        get
        {
            ExecutionInfo.Role.RecordGet();
            return this.Role;
        }
        set
        {
            ExecutionInfo.Role.RecordSet(value);
            this.Role = value;
        }
    }

    // Void method - user didn't define, so just tracks
    void IUserService.SomeMethod()
    {
        ExecutionInfo.SomeMethod.RecordCall();
    }

    // Method with return - user defined override
    User IUserService.GetUser(int id)
    {
        ExecutionInfo.GetUser.RecordCall(id);
        return this.GetUser(id);  // Calls user-defined method
    }
}
```

## Test Usage

```csharp
[Fact]
public void OrderService_CallsUserService()
{
    var userKnockOff = new UserServiceKnockOff();
    IUserService userService = userKnockOff;

    var orderService = new OrderService(userService);
    orderService.ProcessOrder(42);

    // Verify the service was called correctly
    Assert.True(userKnockOff.ExecutionInfo.GetUser.WasCalled);
    Assert.Equal(42, userKnockOff.ExecutionInfo.GetUser.LastCallArgs);
}
```

## ExecutionDetails

```csharp
// For void methods
public class ExecutionDetails
{
    public int CallCount { get; }
    public bool WasCalled => CallCount > 0;
    public void RecordCall();
}

// For methods with parameters and/or return values
public class ExecutionDetails<TReturn, TArg1, ...>
{
    public int CallCount { get; }
    public bool WasCalled => CallCount > 0;
    public (TArg1, ...) LastCallArgs { get; }
    public List<(TArg1, ...)> AllCalls { get; }
    public void RecordCall(TArg1 arg1, ...);
}

// For properties
public class ExecutionDetails<TValue>
{
    public int GetCount { get; }
    public int SetCount { get; }
    public TValue? LastSetValue { get; }
    public void RecordGet();
    public void RecordSet(TValue value);
}
```

## Scope - Phase 1 (MVP)

- Void methods (no parameters)
- Void methods (with parameters)
- Methods with return values
- Properties (get/set, get-only, set-only)

## Scope - Phase 2

- Async methods (`Task`, `Task<T>`, `ValueTask<T>`)
- Generic interfaces
- Multiple interface implementation
- Interface inheritance

## Scope - Phase 3 (If Needed)

- Generic methods
- Events
- Indexers
- ref/out parameters

## Open Questions

1. **User method detection**: Should user-defined overrides be `protected`? Must signature match exactly?
2. **Naming conflicts**: What prefix for generated backing members? (`_` could conflict)
3. **Callbacks**: Should ExecutionDetails support callback registration for dynamic behavior?
4. **Default returns**: For non-overridden methods returning values, return `default(T)`?

## Implementation Steps

### Phase 0: Project Setup
- [ ] Create solution structure (mirror RemoteFactory)
- [ ] Create `Directory.Build.props` with target frameworks (net8.0, net9.0, net10.0)
- [ ] Create `Directory.Packages.props` for central package management
- [ ] Create Generator project (netstandard2.0)
- [ ] Create KnockOff core library project
- [ ] Create test project with generator reference
- [ ] Create sandbox project for manual testing

### Phase 1: Core Generator Infrastructure
- [ ] Define `[KnockOff]` attribute in core library
- [ ] Create `IIncrementalGenerator` skeleton
- [ ] Implement predicate: classes with `[KnockOff]` implementing interfaces
- [ ] Define transform model types (must be equatable/serializable):
  - [ ] `KnockOffTypeInfo` - target class info
  - [ ] `InterfaceMemberInfo` - property/method info
  - [ ] `ParameterInfo` - method parameters
- [ ] Implement transform: extract interface members

### Phase 2: ExecutionDetails Infrastructure
- [ ] Define `ExecutionDetails` base class (void, no args)
- [ ] Define `ExecutionDetails<TReturn>` for return values
- [ ] Define `ExecutionDetails<TReturn, TArgs...>` variants (up to reasonable arity)
- [ ] Define property-specific tracking

### Phase 3: Code Generation - Properties
- [ ] Generate backing properties
- [ ] Generate explicit interface property implementations
- [ ] Generate ExecutionInfo property entries
- [ ] Handle get-only properties
- [ ] Handle set-only properties
- [ ] Test: property get/set tracking works

### Phase 4: Code Generation - Methods
- [ ] Generate explicit interface method implementations
- [ ] Generate ExecutionInfo method entries
- [ ] Detect user-defined method overrides in partial class
- [ ] Call user method when defined
- [ ] Return `default(T)` when not defined
- [ ] Test: void methods tracked
- [ ] Test: return value methods work
- [ ] Test: user overrides called

### Phase 5: NuGet Packaging
- [ ] Configure Generator.csproj for NuGet embedding
- [ ] Configure KnockOff.csproj as main package
- [ ] Create package metadata
- [ ] Test package installation in separate project

### Phase 6: CI/CD
- [ ] Create GitHub Actions workflow (build, test)
- [ ] Add NuGet publish on version tags

### Phase 7: Phase 2 Scope (Async, Generics, etc.)
- [ ] Async method support
- [ ] Generic interface support
- [ ] Multiple interfaces
- [ ] Interface inheritance
