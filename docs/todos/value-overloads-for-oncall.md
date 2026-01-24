# Value-Based Overloads for OnCall/OnGet/OnSet Methods

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-24
**Last Updated:** 2026-01-24

---

## Problem

Currently, OnCall/OnGet/OnSet methods only accept callback delegates. For many common test scenarios, users simply want to return a fixed value:

```csharp
// Current API - verbose for simple cases
stub.GetById.OnCall((id) => expectedUser);
stub.Name.OnGet(() => "Alice");

// Desired API - clean value-based overloads
stub.GetById.OnCall(expectedUser);       // Returns same value for all calls
stub.Name.OnGet("Alice");                // Returns "Alice" for all gets
```

The callback-only approach adds unnecessary ceremony when the return value is static and does not depend on the input parameters.

## Solution

Add value-based overloads to OnCall/OnGet methods that accept a return value directly. Design with clean architecture principles:

1. **Value Source Abstraction** - Unified model for "where does the return value come from?" (callback vs. direct value)
2. **Async Auto-Wrapping** - For `Task<T>` return types, auto-wrap non-Task values in `Task.FromResult()`
3. **Sequence Value Support** - Add `ThenReturn(value)` to complement `ThenCall(callback)`
4. **Pattern Consistency** - Method, property, and indexer interceptors should share the same patterns

---

## Plans

- [Value-Based Overloads Architecture](../plans/value-based-overloads-architecture.md)

---

## Tasks

- [ ] Design value source abstraction
- [ ] Design async auto-wrapping strategy
- [ ] Update UnifiedMethodInterceptorModel
- [ ] Update UnifiedPropertyInterceptorModel
- [ ] Update MethodInterceptorRenderer
- [ ] Update PropertyInterceptorRenderer
- [ ] Add IMethodTracking interface extensions
- [ ] Add IPropertyGetSequence.ThenGet(value) overload
- [ ] Add IMethodSequence.ThenReturn(value) overload
- [ ] Comprehensive test coverage

---

## Progress Log

- 2026-01-24: Created todo and initial architectural analysis

---

## Results / Conclusions

