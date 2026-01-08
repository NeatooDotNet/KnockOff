# Class Stubs: Composition Pattern

## Motivation

The current class stub API is inconsistent with interface stubs:

```csharp
// Interface stub - direct access
userStub.GetUser.OnCall = (ko, id) => new User();

// Class stub - requires .Interceptor
emailStub.Interceptor.Send.OnCall = (ko, to, subj, body) => { };
```

The `.Interceptor` indirection exists because class stubs inherit from the target class, risking naming conflicts between interceptor properties and class members.

**Solution:** Change class stubs from inheritance to composition. The stub wraps a nested class that inherits from the target.

## Design

### Current (Inheritance)

```
EmailServiceStub : EmailService
├── Interceptor.Send.OnCall    ← configuration (indirect)
└── (stub IS the EmailService) ← passed directly
```

```csharp
// Generated (current)
public class EmailServiceStub : EmailService
{
    public EmailServiceInterceptors Interceptor { get; } = new();

    public EmailServiceStub(string connectionString) : base(connectionString) { }

    public override void Send(string to, string subject, string body)
    {
        Interceptor.Send.RecordCall(to, subject, body);
        if (Interceptor.Send.OnCall is { } onCall) onCall(this, to, subject, body);
        else base.Send(to, subject, body);
    }
}
```

### Proposed (Composition)

```
EmailServiceStub (wrapper)
├── Send.OnCall                ← configuration (direct!)
└── Object                     ← the actual EmailService
```

```csharp
// Generated (new)
public class EmailServiceStub
{
    // Interceptor properties directly on wrapper (no .Interceptor)
    public EmailService_SendInterceptor Send { get; } = new();

    // The actual EmailService instance
    public EmailService Object { get; }

    // Constructors create the inner instance
    public EmailServiceStub(string connectionString)
    {
        Object = new Impl(this, connectionString);
    }

    // Nested class that inherits from target
    private sealed class Impl : EmailService
    {
        private readonly EmailServiceStub _stub;

        public Impl(EmailServiceStub stub, string connectionString)
            : base(connectionString)
        {
            _stub = stub;
        }

        public override void Send(string to, string subject, string body)
        {
            _stub.Send.RecordCall(to, subject, body);
            if (_stub.Send.OnCall is { } onCall) onCall(_stub, to, subject, body);
            else base.Send(to, subject, body);
        }
    }
}
```

### Unified API

After this change, both interface and class stubs have direct interceptor access:

```csharp
// Interface stub
var userStub = new Stubs.IUserService();
userStub.GetUser.OnCall = (ko, id) => new User();
IUserService service = userStub;  // Direct assignment

// Class stub - SAME pattern for interceptors!
var emailStub = new Stubs.EmailService();
emailStub.Send.OnCall = (ko, to, subj, body) => { };
EmailService service = emailStub.Object;  // .Object to get the class instance
```

## What Changes

| Aspect | Interface Stubs | Class Stubs (Before) | Class Stubs (After) |
|--------|-----------------|----------------------|---------------------|
| Pattern | Implements interface | Inherits from class | Wraps nested class |
| Interceptor access | `stub.Member` | `stub.Interceptor.Member` | `stub.Member` |
| Get typed instance | `stub` (direct) | `stub` (is-a) | `stub.Object` |

## Implementation Plan

### Phase 1: Generator Changes

- [x] **1.1** Modify `GenerateClassStubClass` method
  - Change from generating `public class Stub : TargetClass`
  - Generate wrapper class (no inheritance)
  - Generate nested `Impl` class that inherits from target

- [x] **1.2** Move interceptor properties to wrapper
  - Currently on `.Interceptor` container
  - Move directly onto wrapper class
  - Remove `Interceptors` container class for class stubs

- [x] **1.3** Generate `.Object` property
  - Type: target class
  - Returns the `Impl` instance

- [x] **1.4** Update constructor generation
  - Wrapper constructors create `Impl` instance
  - Pass `this` (wrapper) to `Impl` constructor
  - `Impl` constructor chains to base

- [x] **1.5** Update override generation
  - `Impl` class overrides virtual/abstract members
  - Calls `_stub.Member.RecordCall(...)` instead of `Interceptor.Member.RecordCall(...)`
  - Callbacks receive `_stub` (the wrapper) as `ko` parameter
  - Added null checks for `_stub` during base constructor calls

- [x] **1.6** Handle `ResetInterceptors()`
  - Keep on wrapper class
  - Iterate all interceptor properties directly

### Phase 2: Test Updates

- [x] **2.1** Update all class stub tests
  - Change `stub.Interceptor.Member` → `stub.Member`
  - Add tests for `.Object` property

- [x] **2.2** Add new test cases
  - `.Object` returns correct type
  - `.Object` is substitutable for target class
  - Wrapper receives callbacks with `ko` parameter
  - Multiple constructors work correctly

- [x] **2.3** Verify existing behavior preserved
  - Virtual member interception
  - Abstract member interception
  - Non-virtual members delegate to base
  - Constructor parameter chaining

### Phase 3: Documentation Updates

- [x] **3.1** Update README.md
  - Updated class stub example to use new API
  - Updated description from "inherit" to "composition"

- [x] **3.2** Update `docs/guides/inline-stubs.md`
  - Class stub examples use new API
  - Document `.Object` property usage

- [x] **3.3** Update `docs/release-notes/v10.8.0.md`
  - Class stub section reflects composition pattern
  - Note the API change from `.Interceptor.Member` to `.Member`

- [ ] **3.4** Update `docs/todos/completed/class-stubs-design.md`
  - Archive as historical
  - Create new design doc or update inline

- [x] **3.5** Create migration note
  - Created `docs/release-notes/v10.9.0.md`
  - Breaking change: `stub.Interceptor.Member` → `stub.Member`
  - Breaking change: direct assignment `TargetClass x = stub` now requires `.Object`

### Phase 4: Skill Updates

- [x] **4.1** Update `~/.claude/skills/knockoff/SKILL.md`
  - Add class stub section (currently missing)
  - Show unified API pattern

- [ ] **4.2** Update `~/.claude/skills/knockoff/interceptor-api.md`
  - Add class stub interceptor patterns (optional - interceptor API is same as interface)

- [x] **4.3** Update `~/.claude/skills/knockoff/migrations.md`
  - Add v10.9.0 migration for class stub API change

## Breaking Changes

1. **API Change**: `stub.Interceptor.Member` → `stub.Member`
   - All existing class stub code must be updated

2. **Type Change**: Stub no longer IS-A target class
   - `TargetClass x = stub;` → `TargetClass x = stub.Object;`
   - Methods expecting `TargetClass` require `.Object`

3. **`ko` Parameter Type**: Now wrapper, not the inner class
   - Callbacks receive wrapper instance
   - Can access all interceptors from callback

   ```csharp
   // Before (inheritance): ko IS the EmailService
   stub.Interceptor.Send.OnCall = (ko, to, subj, body) => {
       ((EmailService)ko).SomeOtherMethod();  // Works - ko is EmailService
   };

   // After (composition): ko is the wrapper
   stub.Send.OnCall = (ko, to, subj, body) => {
       ((EmailService)ko).SomeOtherMethod();  // Fails - ko is EmailServiceStub
       ko.Object.SomeOtherMethod();           // Need .Object
   };
   ```

   **Documentation Note:** This change must be clearly documented in `docs/guides/inline-stubs.md` with examples showing how to access the underlying object from callbacks.

## Edge Cases

### Sealed Classes

Sealed classes cannot be stubbed (neither inheritance nor composition can work). The generator emits diagnostic KO2001: "Cannot create stub for sealed class '{0}'".

### Protected Virtual Members

Protected members in the target class are overridden in `Impl`. The interceptor is on the wrapper (public), so tests can configure behavior. The actual override calls through to the wrapper's interceptor.

### Constructor with `this` Reference

The `Impl` class receives the wrapper in its constructor. This is safe because:
- `Impl` constructor only stores the reference in `_stub`
- Interceptor field initializers run before wrapper constructor body
- Even if base constructor calls a virtual method, `_stub.Member` is already initialized

### Abstract Classes

Work the same way. `Impl` inherits from abstract class and provides implementations for abstract members (delegating to wrapper's interceptors).

### User Method Detection

User method detection (where users define protected methods that are called from generated code) is **not implemented for class stubs**. This is intentional:

- Interface stubs need user methods because interfaces have no default implementation
- Class stubs have `base.Method()` as the natural default
- The `OnCall` callback pattern provides equivalent flexibility

The composition pattern does not affect this - there's nothing to preserve.

### Generic Classes

```csharp
[KnockOff<Repository<User>>]
public partial class Tests { }

// Generated:
public class RepositoryStub
{
    public Repository_User_FindInterceptor Find { get; } = new();
    public Repository<User> Object { get; }

    private sealed class Impl : Repository<User> { ... }
}
```

## Files to Modify

### Generator
- `src/Generator/KnockOffGenerator.cs`
  - `GenerateClassStubClass` (lines ~4508-4594)
  - `GenerateClassConstructor`
  - `GenerateClassPropertyOverride`
  - `GenerateClassMethodOverride`
  - `GenerateClassIndexerOverride`
  - `GenerateClassEventOverride`
  - Remove/modify `GenerateClassInterceptorsContainer`

### Tests
- `src/Tests/KnockOffTests/` - All class stub tests
- `src/Tests/KnockOff.Documentation.Samples/` - Sample code
- `src/Tests/KnockOff.Documentation.Samples.Tests/` - Sample tests

### Documentation
- `README.md`
- `docs/guides/inline-stubs.md`
- `docs/release-notes/v10.8.0.md` (or create v10.9.0)
- `docs/todos/completed/class-stubs-design.md`

### Skill Files
- `~/.claude/skills/knockoff/SKILL.md`
- `~/.claude/skills/knockoff/interceptor-api.md`
- `~/.claude/skills/knockoff/migrations.md`

## Version Considerations

This is a breaking change to the class stub API. Decision: **v10.9.0** since class stubs are new in v10.8.0 and adoption is minimal.

## Success Criteria

- [ ] Unified API: both interface and class stubs use `stub.Member.OnCall`
- [ ] `.Object` property returns target class instance
- [ ] All existing class stub functionality preserved
- [ ] All tests pass
- [ ] Documentation updated
- [ ] Skill files updated
