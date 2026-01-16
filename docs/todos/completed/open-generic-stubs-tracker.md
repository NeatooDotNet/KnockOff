# Open Generic Stubs - Tracker

**Status:** Complete
**Completed:** v10.20

This todo tracks the overall effort to support open generic stubs across all type kinds (interface, class, delegate) and stub patterns (standalone, inline).

## Support Matrix

|  | **Stand-Alone Stub** | **Inline Stub** |
|--|---------------------|-----------------|
| **Open Generic Interface** | ✅ Supported (v10.14)<br>`[KnockOff] class Stub<T> : IRepo<T>` | ✅ Supported (v10.18)<br>`[KnockOff(typeof(IRepo<>))]` |
| **Open Generic Class** | ❌ Not Supported<br>Standalone requires interface implementation | ✅ Supported (v10.20)<br>`[KnockOff(typeof(SomeClass<>))]` |
| **Open Generic Delegate** | ❌ Impossible<br>Can't inherit from delegates | ✅ Supported (v10.20)<br>`[KnockOff(typeof(Factory<>))]` |

## Legend

- ✅ Supported - fully implemented and tested
- ❓ Untested - code path exists but no tests verify it works
- ❌ Not supported - needs implementation (see linked todo)
- ❌ Impossible - language constraint prevents this pattern

## Implementation Todos

| Todo | Type | Pattern | Priority | Notes |
|------|------|---------|----------|-------|
| ~~[generic-delegate-support.md](completed/generic-delegate-support.md)~~ | Delegate | Inline | ✅ Done | Completed in v10.20 |
| ~~[generic-inline-interface-support.md](generic-inline-interface-support.md)~~ | Interface | Inline | ✅ Done | Completed in v10.18 |
| ~~[generic-inline-class-support.md](completed/generic-inline-class-support.md)~~ | Class | Inline | ✅ Done | Completed in v10.20 |

## Task List

- [x] Verify standalone generic class stubs work → **Result: Not supported** (standalone requires interface implementation; use inline pattern for class stubs)
- [x] Implement `[KnockOff(typeof(...))]` attribute parsing (shared by all three)
- [x] Implement open generic delegate inline stubs → **Completed v10.20**
- [x] Implement open generic interface inline stubs → **Completed v10.18** (includes multi-param generics like `IKeyValueStore<TKey, TValue>` and type constraints like `where T : class`)
- [x] Implement open generic class inline stubs → **Completed v10.20**
- [x] Update documentation with complete support matrix

## Shared Implementation

All three inline implementations share common work:

1. **Attribute parsing** - Add `[KnockOff(Type type)]` constructor and parse `typeof(T<>)` syntax
2. **Unbound generic detection** - Check `IsUnboundGenericType` in transform
3. **Type parameter extraction** - Get type parameters from unbound generic
4. **Generic stub generation** - Emit `class Stub<T>` instead of `class Stub`
5. **Generic interceptor generation** - Interceptors also need type parameters

Once attribute parsing is implemented, the three type kinds (interface, class, delegate) each need their own generation logic since they differ in how stubs are structured.

## Syntax

All three use the same attribute syntax:

```csharp
// Interface
[KnockOff(typeof(IRepository<>))]

// Class
[KnockOff(typeof(Repository<>))]

// Delegate
[KnockOff(typeof(Factory<>))]

public partial class MyTests { }
```

Generated stubs:

```csharp
public partial class MyTests
{
    public static partial class Stubs
    {
        // Interface - implements
        public class Repository<T> : IRepository<T> { ... }

        // Class - extends
        public class Repository<T> : global::Repository<T> { ... }

        // Delegate - wraps (can't extend)
        public class Factory<T>
        {
            public T Invoke() { ... }
            public static implicit operator global::Factory<T>(Factory<T> stub) => stub.Invoke;
        }
    }
}
```

## Why Standalone Open Generic Delegate Is Impossible

Delegates are sealed types in C#. You cannot inherit from them:

```csharp
public delegate T Factory<T>();

// CS0509: cannot derive from sealed type 'Factory<T>'
public class FactoryStub<T> : Factory<T> { }
```

The only way to create a delegate stub is to wrap it with an implicit conversion operator, which requires the inline pattern.

## Priority Rationale

| Priority | Type | Reason |
|----------|------|--------|
| Medium | Delegate | No workaround - can't create standalone generic delegate stubs |
| Medium | Interface | Standalone workaround exists but inline is more convenient |
| Medium | Class | No workaround - standalone pattern requires interface implementation |

## Completion Criteria

This tracker is complete when:
- [x] All three implementation todos are complete
- [x] Support matrix shows ✅ for all implementable cells
- [x] Standalone generic class cell is verified (✅ or documented limitation)
