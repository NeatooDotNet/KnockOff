# Property Semantics Redesign

## Status: Planning

The current implementation sacrifices semantic enforcement for "convenience". This redesign enforces correct property semantics at compile-time.

## Core Insight

**Init means "I get this from the outside."** The test IS the outside. There's nothing to mock - you provide the value, the code under test reads it.

## API Change

**Before (current):**
```csharp
stub.Id           // interceptor
stub.Id.Value     // the value (always { get; set; } - no enforcement!)
stub.Id.OnGet     // callback
```

**After (redesign):**
```csharp
stub.Id                  // the actual property (with correct semantics)
stub.IdInterceptor       // tracking/callbacks (only what makes sense)
```

## Scenarios to Evaluate

### 1. Init-only Property: `{ get; init; }`

**Semantics:** Input from outside, set once at construction, read many times.

**What tests need:**
- [x] Set value via object initializer (enforced by compiler)
- [x] Track reads (GetCount)
- [ ] ~~OnGet~~ - Not needed, you provide the value
- [ ] ~~OnSet~~ - Not needed, init happens once during construction you control

**Interceptor:**
```csharp
public sealed class IdInterceptor
{
    public int GetCount { get; private set; }
    public void RecordGet() => GetCount++;
    public void Reset() => GetCount = 0;
}
```

**Stub property (implicit implementation):**
```csharp
public string Id
{
    get { IdInterceptor.RecordGet(); return _id; }
    init { _id = value; }
}
private string _id = "";
```

**Usage:**
```csharp
var stub = new EntityKnockOff() { Id = "test-123" };
// stub.Id = "x";  // Won't compile - enforced!
```

**Status:** Design complete, needs implementation

---

### 2. Get-Set Property: `{ get; set; }`

**Semantics:** Read/write anytime.

**What tests need:**
- [x] Set value anytime (`Value`)
- [x] Track reads (`GetCount`)
- [x] Track writes (`SetCount`, `LastSetValue`)
- [x] OnGet to override returned value
- [x] OnSet callback to intercept writes

**Decision:** This is the baseline scenario. **No changes.** Full interceptor functionality remains:
- `Value { get; set; }`
- `OnGet`, `OnSet`
- `GetCount`, `SetCount`, `LastSetValue`
- `Reset()`

**Status:** Complete (existing implementation)

---

### 3. Get-only Property: `{ get; }`

**Semantics:** Value controlled by the object, outsiders can only read.

**What tests need:**
- [x] Provide a value for the stub to return (`Value` on interceptor)
- [x] Track reads (`GetCount`)
- [x] OnGet to control return value (for computed/dynamic values)

**Decision:** Interceptor keeps full functionality for setup:
- `Value { get; set; }` - to set up return value
- `OnGet` - for dynamic/computed returns
- `GetCount` - track reads
- `Reset()`

The **stub property itself** is get-only (matching interface). Setup done via interceptor.

```csharp
stub.VersionInterceptor.Value = 42;  // Setup
var v = stub.Version;                // Get-only access
```

**Status:** Design complete (behavior unchanged, only naming changes with redesign)

---

### 4. Set-only Property: `{ set; }`

**Semantics:** Write-only, can't read back.

**What tests need:**
- [x] Track writes (`SetCount`, `LastSetValue`)
- [x] OnSet callback to intercept writes

**Decision:** Interceptor has write-tracking only:
- `SetCount` - how many times set
- `LastSetValue` - what was written
- `OnSet` - callback to intercept
- `Reset()`
- No `Value` getter, no `OnGet`, no `GetCount`

The **stub property itself** is set-only (matching interface).

```csharp
stub.Output = "log message";                          // Set-only access
Assert.Equal("log message", stub.OutputInterceptor.LastSetValue);
```

**Status:** Design complete (behavior unchanged, only naming changes with redesign)

---

### 5. Required Properties: `required`

**Semantics:** Must be set during construction (object initializer).

**Decision:** Enforce `required` properly - no `[SetsRequiredMembers]` bypass.

**How it works (class stubs):**

1. Wrapper mirrors all `required` properties from base class
2. Wrapper mirrors all constructors from base class
3. `Object` is lazy-initialized (created on first access, not in constructor)
4. When creating Impl, pass wrapper's values through object initializer

```csharp
public abstract class Entity
{
    public required string Id { get; init; }
    protected Entity() { }
    protected Entity(string category) { /* ... */ }
}

// Generated stub:
public class EntityStub
{
    public required string Id { get; init; }  // Mirrored

    public EntityStub() { _ctorArgs = () => new Impl(this) { Id = this.Id }; }
    public EntityStub(string category) { _ctorArgs = () => new Impl(this, category) { Id = this.Id }; }

    private readonly Func<Impl> _ctorArgs;
    private Impl? _object;
    public Entity Object => _object ??= _ctorArgs();

    private class Impl : Entity
    {
        public Impl(EntityStub stub) : base() { _stub = stub; }
        public Impl(EntityStub stub, string category) : base(category) { _stub = stub; }

        public override required string Id
        {
            get { _stub.IdInterceptor.RecordGet(); return _stub.Id; }
            init { /* no-op, value comes from wrapper */ }
        }
    }
}
```

**Usage:**
```csharp
var stub = new EntityStub("category") { Id = "123" };  // Required enforced!
// stub.Id = "x";  // Won't compile - init only
```

**Combinations:**
- `required { get; set; }` - must provide at construction, can change later
- `required { get; init; }` - must provide at construction, immutable after

The `required` modifier adds the construction constraint. The `get/set` vs `init` determines mutability after construction.

**Status:** Design complete

---

## Design Summary

| Pattern | Stub Property | Interceptor Has |
|---------|---------------|-----------------|
| `{ get; set; }` | Read/write | `Value`, `OnGet`, `OnSet`, counts |
| `{ get; init; }` | Init-only | `GetCount` only (no Value, no callbacks) |
| `{ get; }` | Get-only | `Value`, `OnGet`, `GetCount` |
| `{ set; }` | Set-only | `OnSet`, `SetCount`, `LastSetValue` |
| `required` | Mirrored on wrapper | Lazy `Object` creation |

## Implementation Order

1. [x] Design `{ get; init; }` - init enforced, minimal interceptor
2. [x] Design `{ get; set; }` - no changes, full functionality
3. [x] Design `{ get; }` - no changes, setup via interceptor
4. [x] Design `{ set; }` - no changes, tracking only
5. [x] Design `required` - wrapper mirrors, lazy Object, no bypass
6. [ ] Implement API change: `stub.Property` + `stub.PropertyInterceptor`
7. [ ] Implement for interface stubs (standalone)
8. [ ] Implement for interface stubs (inline)
9. [ ] Implement for class stubs (with lazy Object)
10. [ ] Update all tests
11. [ ] Update documentation

## Breaking Changes

This is a breaking API change:

**Before:**
```csharp
stub.Id              // interceptor
stub.Id.Value        // the value
stub.Id.OnGet        // callback
```

**After:**
```csharp
stub.Id              // the actual property (with correct semantics)
stub.IdInterceptor   // tracking/callbacks
```

**Init-only properties:**
- No `Value` on interceptor (use object initializer)
- No `OnGet`/`OnSet` (not needed - you provide the value)
- Only `GetCount` and `Reset()`

**Class stubs:**
- `Object` is lazy (created on first access)
- Required properties mirrored on wrapper
- All constructors mirrored on wrapper
