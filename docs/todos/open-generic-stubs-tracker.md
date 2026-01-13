# Open Generic Stubs - Tracker

This todo tracks the overall effort to support open generic stubs across all type kinds (interface, class, delegate) and stub patterns (standalone, inline).

## Support Matrix

|  | **Stand-Alone Stub** | **Inline Stub** |
|--|---------------------|-----------------|
| **Open Generic Interface** | ✅ Supported (v10.14)<br>`[KnockOff] class Stub<T> : IRepo<T>` | ❌ [generic-inline-interface-support.md](generic-inline-interface-support.md) |
| **Open Generic Class** | ❓ Untested<br>`[KnockOff] class Stub<T> : BaseClass<T>` | ❌ [generic-inline-class-support.md](generic-inline-class-support.md) |
| **Open Generic Delegate** | ❌ Impossible<br>Can't inherit from delegates | ❌ [generic-delegate-support.md](generic-delegate-support.md) |

## Legend

- ✅ Supported - fully implemented and tested
- ❓ Untested - code path exists but no tests verify it works
- ❌ Not supported - needs implementation (see linked todo)
- ❌ Impossible - language constraint prevents this pattern

## Implementation Todos

| Todo | Type | Pattern | Priority | Notes |
|------|------|---------|----------|-------|
| [generic-delegate-support.md](generic-delegate-support.md) | Delegate | Inline | Medium | No workaround exists |
| [generic-inline-interface-support.md](generic-inline-interface-support.md) | Interface | Inline | Medium | Standalone workaround exists |
| [generic-inline-class-support.md](generic-inline-class-support.md) | Class | Inline | Low | Standalone workaround exists |

## Task List

- [ ] Verify standalone generic class stubs work (may complete that cell)
- [ ] Implement `[KnockOff(typeof(...))]` attribute parsing (shared by all three)
- [ ] Implement open generic delegate inline stubs
- [ ] Implement open generic interface inline stubs
- [ ] Implement open generic class inline stubs
- [ ] Update documentation with complete support matrix

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
| Low | Class | Standalone workaround exists, class stubs are less common |

## Completion Criteria

This tracker is complete when:
- [ ] All three implementation todos are complete
- [ ] Support matrix shows ✅ for all implementable cells
- [ ] Standalone generic class cell is verified (✅ or documented limitation)
