# Init-Only and Required Property Support

## Status: Complete

Init-only property support for both interfaces and classes has been implemented. Required property support for classes is also complete.

## Design Decision: Practical Init Semantics

**Decision:** Init semantics are enforced at the interface level, but internal backing storage uses `{ get; set; }`.

**Why not strict semantics on Value property?**

The original plan was to make `Interceptor.Value` use `{ get; init; }` when the source property is init-only. However, this doesn't work because:

1. The explicit interface implementation has `init { ... Value = value; }`
2. This tries to assign to `Interceptor.Value` from within the stub's init accessor
3. C# only allows init assignment within the **same object's** init accessor or constructor
4. Since `Value` is on a different object (the interceptor), assignment fails

**Solution:** The explicit interface implementation uses `init` (enforcing the contract), but `Interceptor.Value` uses `set` (for internal flexibility).

**Result:**
- Interface consumers see proper init-only semantics (can only set via object initializer)
- Test code can conveniently set `stub.Id.Value = "test"` at any time
- Best of both worlds

## Design Decision: Required Property Initialization Order

For class stubs with required properties, the generated `Impl` constructor must:

1. Initialize required properties to `default!` (to satisfy `[SetsRequiredMembers]`)
2. Set `_stub = stub` to enable interceptor tracking

**Critical order:** Required properties must be initialized BEFORE `_stub` is assigned. Otherwise, the property setter records the initialization call (incrementing `SetCount`), which would cause confusing test behavior.

```csharp
// Generated constructor - correct order:
public Impl(EntityBaseWithRequiredProperty stub) : base()
{
    Id = default!;    // 1. Initialize while _stub is null (setter skips recording)
    _stub = stub;     // 2. Now enable interceptor tracking
}
```

This ensures `SetCount` only reflects user-initiated sets, not framework initialization.

## Completed Changes

### Phase 1: Model Changes

- [x] `src/Generator/Models/InterfaceModels.cs` - Added `IsInitOnly` property
- [x] `src/Generator/Models/ClassModels.cs` - Added `IsInitOnly` and `IsRequired` properties

### Phase 2: Code Generation Changes

- [x] `src/Generator/KnockOffGenerator.GenerateFlat.cs` - Updated explicit interface implementation to use `init` keyword
- [x] `src/Generator/KnockOffGenerator.GenerateInline.cs` - Updated explicit interface implementation to use `init` keyword
- [x] `src/Generator/KnockOffGenerator.GenerateClass.cs` - Updated class override to use `init` and `required` keywords

## Test Coverage

Tests created in `src/Tests/KnockOffTests/InitPropertyTests.cs`:

### Interface Init Properties (Standalone Stubs)

- [x] Basic init-only property: `string Id { get; init; }`
- [x] Nullable init-only: `string? Name { get; init; }`
- [x] Value type init-only: `int Count { get; init; }`
- [x] Nullable value type init-only: `int? Revision { get; init; }`
- [x] Multiple init properties
- [x] Mixed init and set properties
- [x] Mixed init and get-only properties

### Interface Init Properties (Inline Stubs)

- [x] Basic init property via `[KnockOff<IInterface>]`
- [x] Mixed properties via inline stubs
- [x] Nullable init property via inline stubs

### Behavioral Tests

- [x] Interceptor.Value can be set for test setup
- [x] OnGet callback works with init properties
- [x] Reset() clears tracking for init properties
- [x] GetCount tracks access through interface

### Class Init Properties (Inline Stubs)

- [x] Abstract class with virtual init property
- [x] Abstract class with abstract init property
- [x] Mixed init/set/get-only properties on abstract class
- [x] Callback works with class init properties
- [x] Reset() clears tracking for class init properties
- [x] GetCount/SetCount tracks access through Object property

### Class Required Properties (Inline Stubs)

- [x] Abstract class with required virtual property (get/set)
- [x] Required + init combination
- [x] Multiple required properties
- [x] Non-required properties alongside required ones
- [x] SetCount not incremented by constructor initialization
- [x] LastSetValue tracks user-set values correctly

## Remaining Work

### Edge Cases (Not Yet Tested)

- [ ] Generic interface with init property
- [ ] Interface inheritance with init properties
- [ ] Interface hiding where base has `set` and derived has `init`

## Usage Examples

### Interface with Init Property

```csharp
public interface IEntity
{
    string Id { get; init; }
}

[KnockOff]
public partial class EntityKnockOff : IEntity { }

// Test usage - Value property is { get; set; } for convenience:
var stub = new EntityKnockOff();
stub.Id.Value = "test-123";

// Access through interface:
IEntity entity = stub;
Assert.Equal("test-123", entity.Id);

// Tracking works:
Assert.Equal(1, stub.Id.GetCount);
```

### Mixed Init and Set Properties

```csharp
public interface IDocument
{
    string Id { get; init; }       // Immutable identifier
    string Title { get; set; }     // Mutable
}

[KnockOff]
public partial class DocumentKnockOff : IDocument { }

// Test setup:
var stub = new DocumentKnockOff();
stub.Id.Value = "doc-1";
stub.Title.Value = "Draft";

// Can change any Value property:
stub.Title.Value = "Final";
stub.Id.Value = "doc-2";  // Works! Value is { get; set; }

// But interface enforces init semantics:
IDocument doc = stub;
// doc.Id = "new";  // Won't compile - init only through interface
doc.Title = "Updated";  // Works - set property
```

## References

- [C# 9 Init-only properties](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init)
- [C# 11 Required members](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/required-members)
- [IMethodSymbol.IsInitOnly](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.imethodsymbol.isinitonly)
- [IPropertySymbol.IsRequired](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.ipropertysymbol.isrequired)
