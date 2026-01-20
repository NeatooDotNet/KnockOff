# Class Stub Source() for DbSet Use Case

**Status:** Not Started
**Priority:** Medium
**Created:** 2026-01-19
**Last Updated:** 2026-01-19

---

## Problem

DbSet mocking with Moq is notoriously painful due to the IQueryable ceremony required:

```csharp
var data = new List<Blog> { ... }.AsQueryable();
var mockSet = new Mock<DbSet<Blog>>();
mockSet.As<IQueryable<Blog>>().Setup(m => m.Provider).Returns(data.Provider);
mockSet.As<IQueryable<Blog>>().Setup(m => m.Expression).Returns(data.Expression);
mockSet.As<IQueryable<Blog>>().Setup(m => m.ElementType).Returns(data.ElementType);
mockSet.As<IQueryable<Blog>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
```

KnockOff supports class stubbing via `[KnockOff<SomeClass>]`, but `Source()` was never extended to class stubs. The `Source()` pattern (proven for interface stubs) allows providing a backing implementation that handles default behavior while allowing selective overrides.

For DbSet, users want:
- Provide a list-backed IQueryable as the source
- Override specific methods (e.g., `FindAsync`) for test scenarios
- Get real query behavior + controlled exceptions

## Solution

Extend the existing `Source()` infrastructure to class stubs:

1. Add `_source` field to class stub interceptors (property, method, indexer)
2. Add `Source(TClass?)` method to generated class stubs
3. Update priority chain: OnCall → UserMethod → **Source** → Default
4. Class stubs use composition pattern (wrapper + `Impl`), so source delegation happens in wrapper

### Design Questions to Resolve

1. **Source type**: Should `Source()` accept:
   - `DbSet<User>?` (exact base class type)
   - `IQueryable<User>?` (common test data pattern)
   - Both via overloads?

2. **Virtual-only limitation**: Class stubs can only override virtual members. Non-virtual source behavior passes through naturally. Is this acceptable, or do we need diagnostics?

3. **Constructor complexity**: Class stubs use composition. Source can't be injected via constructor. Confirm `Source()` method post-construction is the right pattern.

---

## Plans

---

## Tasks

- [ ] Design: Resolve source type question (exact class vs IQueryable)
- [ ] Design: Document virtual-only limitation handling
- [ ] Extend `ClassModelBuilder` with `BuildSourceProviders()`
- [ ] Add `SourceProviderInfo` to `InlineClassStubModel`
- [ ] Add `_source` field to class stub interceptor generation
- [ ] Update property/method/indexer implementations with source delegation
- [ ] Generate `Source(TClass?)` method in `ClassRenderer`
- [ ] Add tests for class stub Source() functionality
- [ ] Add DbSet-specific sample/test demonstrating the use case

---

## Progress Log

**2026-01-19:** Initial research completed. Key findings:
- Source() is fully implemented for interfaces (flat and inline)
- Class stubbing works but uses composition pattern (wrapper + nested Impl class)
- Source() was never extended to class stubs - this is the gap
- Architecture is proven, main work is wiring through class stub path
- Estimated ~300-400 lines new code, ~100 lines tests

---

## Results / Conclusions

