# Flatten Inherited Interface Members

## Problem

Currently, standalone stubs expose inherited interface members through their declaring interface:
```csharp
// Current (verbose, exposes inheritance chain):
stub.IValidateMetaProperties.IsBusy.OnGet = () => true;
stub.IEntityMetaProperties.IsNew.OnGet = () => false;
```

Meanwhile, inline stubs have flat access:
```csharp
// Inline (flat, user-friendly):
stub.IsBusy.OnGet = () => true;
```

**Goal:** Unify the API. Both standalone and inline stubs should have identical flat access.

## Design Decisions

### 1. Single Interface Constraint (Standalone)

Standalone stubs implement **one interface** (plus its inheritance chain, flattened).

```csharp
// Valid
[KnockOff]
class EntityStub : IEntityBase { }

// Invalid - emit diagnostic
[KnockOff]
class MultiStub : IUserService, ILogger { }
```

**Rationale:** A stub satisfies a dependency. Dependencies are typed as a single interface. Multiple unrelated interfaces on one stub is not a realistic use case—create separate stubs instead.

**Diagnostic:** "KO0010: KnockOff stubs should implement a single interface. Create separate stubs for IUserService and ILogger."

### 2. No Interface Prefix

With single interface constraint, no disambiguation needed. Members go directly on stub class:

```csharp
// Before (standalone)
stub.IEntityBase.Delete.WasCalled
stub.IValidateMetaProperties.IsBusy.OnGet = ...

// After (both standalone and inline)
stub.Delete.WasCalled
stub.IsBusy.OnGet = ...
```

### 3. Simple Interceptor Names

Interceptor classes use simple names without interface prefix:

```csharp
// Before
public class IValidateMetaProperties_IsBusyInterceptor { }

// After
public class IsBusyInterceptor { }
```

**Collision handling:** If two members have same name (rare with single interface), use numbered suffix: `ValueInterceptor`, `Value2Interceptor`.

### 4. Shared Tracking for Inherited Members

When the same member is inherited through multiple paths (diamond inheritance), there is a single interceptor instance. Tracking is shared.

**Rationale:** KnockOff stubs behavior, not architecture. Users care "was IsBusy called?" not "through which inheritance path?"

### 5. Honor `new` Keyword Hiding

When a derived interface hides a base member with `new`:
```csharp
interface IBase { int Value { get; } }
interface IDerived : IBase { new int Value { get; } }
```

We include only the most-derived member (`IDerived.Value`). The hidden `IBase.Value` is not surfaced.

**Rationale:** Same principle—we don't expose inheritance layers. If someone needs `IBase.Value`, they stub `IBase` directly.

### 6. Preserve Declaring Interface for Explicit Implementations

The member info tracks two concepts:

| Concept | Purpose | Example |
|---------|---------|---------|
| Implemented interface | The single interface the stub implements | `IEntityBase` |
| Declaring interface | For explicit implementation syntax | `IValidateMetaProperties` |

Generated code:
```csharp
public partial class EntityStub : IEntityBase
{
    // Interceptor uses simple name, lives on stub class
    public IsBusyInterceptor IsBusy { get; } = new();

    // Explicit implementation uses declaring interface (C# requirement)
    bool IValidateMetaProperties.IsBusy => IsBusy.Invoke();
}
```

## Tasks

### Phase 1: Add Single Interface Constraint ✅ COMPLETE

- [x] Add diagnostic KO0010 for multiple unrelated interfaces on standalone stub
- [x] Update `TransformClass` to detect and report this case
- [x] Determine "unrelated" = not in same inheritance chain (using FindRootInterfaces helper)
- [x] Updated test files to use single-interface stubs (removed MultiInterfaceKnockOff, etc.)
- [x] Fixed Documentation.Samples project to use separate single-interface stubs

### Phase 2: Refactor Member Collection ✅ COMPLETE

- [x] Added `FlatMembers` and `FlatEvents` properties to `KnockOffTypeInfo`
- [x] Flatten all inherited members into single collection
- [x] When encountering `new` keyword hiding, keep only the most-derived member
- [x] Deduplicate diamond inheritance (same member via multiple paths = one entry)
- [x] Track `DeclaringInterfaceFullName` for explicit implementation syntax

**Note:** The flat member collection is computed and stored, but not yet used for code generation.

### Phase 3: Flat API Generation ✅ COMPLETE

- [x] Remove interface container class (`IEntityBaseInterceptors`)
- [x] Generate interceptor properties directly on stub class
- [x] Simplify interceptor class names (remove interface prefix)
- [x] Add collision detection and numbered suffix handling
- [x] Update explicit interface implementations to use declaring interface
- [x] Handle out/ref parameters correctly
- [x] Handle generic methods with type parameters
- [x] Avoid conflicts with user-defined methods (use `2` suffix)

### Phase 4: Update Tests ✅ COMPLETE

- [x] Update `NeatooTests.cs` standalone tests to use flat access
- [x] Update all test files to remove interface prefix from interceptor access
- [x] Fix tuple field casing (`.key`/`.value` → `.Key`/`.Value`)
- [x] Fix inline stub interceptor names (no `2` suffix for inline stubs)
- [x] Fix class stub interceptor names (no `2` suffix)
- [x] Update event tests for correct API (`AddCount`, `Raise(sender, e)`)
- [x] Build succeeds with 0 errors

**Note:** Some tests fail due to pre-existing generator behavior issues (not related to flat API):
- OnCall doesn't override user methods (by design)
- Async methods return null instead of Task.CompletedTask
- Smart defaults "throw for non-constructible types" not implemented

### Phase 5: Update Documentation - PARTIALLY COMPLETE

- [x] **`docs/getting-started.md`** - updated to flat API
- [x] **`docs/reference/interceptor-api.md`** - updated to flat API and correct event API
- [x] **`docs/release-notes/v10.9.0.md`** - created with migration guide
- [ ] **Update remaining `docs/` markdown files** - ~24 files still have old patterns
- [ ] **Update KnockOff skill** (`~/.claude/skills/knockoff/`) - update API examples and patterns

**Note:** The key user-facing docs (getting-started.md, interceptor-api.md) are updated. Remaining files can be updated incrementally.

## Breaking Changes

This is a **breaking change** for standalone stub users:

```csharp
// v10.x
stub.IEntityBase.Delete.WasCalled
stub.IValidateMetaProperties.IsBusy.OnGet = ...

// v10.9
stub.Delete.WasCalled
stub.IsBusy.OnGet = ...
```

**Migration:** Find/replace `stub.I[InterfaceName].` with `stub.` for affected code.

## Success Criteria

1. `stub.IsBusy.OnGet` works for all members (inherited or direct)
2. Standalone and inline stubs have identical API
3. No interface prefix in member access
4. Explicit interface implementations still compile correctly
5. Single interface constraint enforced with clear diagnostic
6. All existing tests updated and passing
