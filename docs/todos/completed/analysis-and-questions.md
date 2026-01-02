# KnockOff Analysis & Open Questions

## Original README Issues Identified

| Issue | Status |
|-------|--------|
| `ExecutionDetails` was undefined | Resolved - added specification with variants |
| Only void method in example | Resolved - added return value example |
| No test usage example | Resolved - added `[Fact]` verification pattern |
| Implementation steps had duplicate | Resolved - expanded to 7 phases |
| No scope boundaries | Resolved - defined Phase 1/2/3 |
| Missing async/generics/events | Resolved - documented as later phases |

## Open Design Questions

### 1. User Method Detection Rules

**Question**: How does the generator identify that a user has defined an override method?

Options:
- **A) Protected + exact signature match**: User defines `protected User GetUser(int id)` to match `User IService.GetUser(int id)`
- **B) Any accessibility + name match**: Match by method name only, ignore return type/params
- **C) Attribute-based**: User marks overrides with `[KnockOffOverride]`

**Recommendation**: Option A - protected + exact signature. It's explicit and catches signature mismatches at compile time.

Answer: Yes, Option A. 

### 2. Naming Conflicts for Generated Members

**Question**: The original example used `_SomeMethod()` as an intermediate. What if the interface has a method named `_SomeMethod`?

Options:
- **A) Double underscore prefix**: `__SomeMethod` (could still conflict)
- **B) Suffix pattern**: `SomeMethod_KnockOff`
- **C) No intermediate**: Generate explicit implementation directly calling user method
- **D) Nested class**: Put backing members in a generated nested class

**Recommendation**: Option C - eliminate the intermediate `_Method` pattern. The explicit interface implementation can:
1. Record the call
2. Check if user method exists and call it, OR return default

Answer: Yes, Option C. Great call!

### 3. Callback Support in ExecutionDetails

**Question**: Should ExecutionDetails support callback registration for dynamic test behavior?

```csharp
// Potential API
knockOff.ExecutionInfo.GetUser.OnCall = (id) => new User { Id = id };
```


Options:
- **A) Yes, include callbacks**: More flexible, closer to Moq
- **B) No, user methods only**: Simpler, user writes real code in partial class
- **C) Phase 2**: Start without, add if needed

**Recommendation**: Option B for MVP. The partial class approach IS the callback mechanism. Adding delegates defeats the purpose of compile-time setup.

Answer: Option B for MVP, but add a later to phase to implement. I want some way to have unit test by unit test behavior to at least a small degree. Question: Should we always send the knockoff partial class instance into the callback?

### 4. Default Return Values

**Question**: For methods with return values where the user hasn't defined an override, what should be returned?

Options:
- **A) `default(T)`**: Return null/0/false
- **B) Throw exception**: Force user to define all return-value methods
- **C) Configurable via ExecutionDetails**: `knockOff.ExecutionInfo.GetUser.Returns = new User()`

**Recommendation**: Option A for MVP. Return `default(T)` with potential for Option C in Phase 2.

Answer: Let's add a general approach that if a property or method is nullable (we can't return null) then it throws an exception unless the user has defined a value.

### 5. Property Backing Fields

**Question**: Should generated backing properties be settable from test setup?

```csharp
var knockOff = new UserServiceKnockOff();
knockOff.Role = Role.Admin;  // Set initial state before test
IUserService service = knockOff;
var role = service.Role;     // Returns Admin
```

Options:
- **A) Protected set**: Only settable from derived classes
- **B) Public set**: Directly settable for test setup
- **C) Via ExecutionDetails**: `knockOff.ExecutionInfo.Role.Value = Role.Admin`

**Recommendation**: Option A (protected) with Option C available. Keeps the knockoff usable only through interface, but allows setup via ExecutionDetails.

Answer: Yes and No, the knockoff should only be used thru it's interfaces. So if there is an interface that allows Role to be set then it can be set in the test. But the
class (knockoff above) should not provide back doors. We may change this in the future. I need to have something to work with to see where I get stuck and real life usage.

### 6. Multiple Interfaces - Member Conflicts

**Question**: What if a class implements two interfaces with the same member signature?

```csharp
interface IReader { string Read(); }
interface ILoader { string Read(); }

[KnockOff]
partial class ReaderLoaderKnockOff : IReader, ILoader { }
```

Options:
- **A) Single backing method**: Both explicit implementations call same `Read()`
- **B) Separate backing**: Generate `_IReader_Read()` and `_ILoader_Read()`
- **C) Phase 2**: Defer until multiple interface support

**Recommendation**: Option C - defer to Phase 2. Focus on single interface first.

Answer: Option A, with good documentation on this senario.

### 7. Interface Inheritance

**Question**: How to handle inherited interface members?

```csharp
interface IEntity { int Id { get; } }
interface IUser : IEntity { string Name { get; } }

[KnockOff]
partial class UserKnockOff : IUser { }  // Must implement Id and Name
```

**Recommendation**: The generator should walk the full interface hierarchy. This is Phase 2 scope.

Answer: Yes, agreed.

## Decisions Made

| Question | Decision | Rationale |
|----------|----------|-----------|
| User method detection | Protected + exact signature | Explicit, compile-time safety |
| Intermediate methods | Eliminate `_Method` pattern | Simpler, no naming conflicts |
| Callbacks | MVP: No. Phase 3: Yes with `(self, args)` signature | Balance simplicity now, flexibility later |
| Default returns | Nullable: `default`. Non-nullable: throw unless user-defined | Fail-fast for non-nullable, permissive for nullable |
| Property access | Interface-only, no backdoors | KnockOff used only through interfaces |
| Multiple interfaces | Single backing method for same signature | Simpler, document the behavior |
| Interface inheritance | Phase 2 | Walk full hierarchy when implemented |

## Callback Design (Phase 3)

When callbacks are implemented, the signature will include the knockoff instance:

```csharp
// Generated in ExecutionDetails for GetUser(int id) -> User
public Func<UserServiceKnockOff, int, User>? OnCall { get; set; }

// Usage in test
knockOff.ExecutionInfo.GetUser.OnCall = (self, id) => {
    // Access to full knockoff instance
    return new User { Id = id };
};
```

This allows callbacks to:
- Access `ExecutionInfo` to coordinate with other tracked calls
- Read any user-defined fields/properties in the partial class
- Maintain interface-only usage (callback is setup, not a backdoor)
