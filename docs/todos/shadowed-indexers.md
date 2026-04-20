# Shadowed indexers (`new this[...]` across interface/class hierarchy)

**Type:** Bug
**Status:** Not Started
**Priority:** Low

**Created:** 2026-04-20

---

## Problem

The `property-new-narrowing-bug` fix addressed shadowed **properties** but explicitly deferred shadowed **indexers**. Indexers dedupe by key-type signature (e.g., `this[int]` vs `this[string]`), so the property-name dedup fix does not apply directly.

No repro currently exists. File a repro when a user reports it or we add coverage proactively.

## Context

- Parent bug: [property-new-narrowing-bug](../plans/completed/property-new-narrowing-bug.md)
- Potential repro shape:
  ```csharp
  public interface IIndexed { int this[int i] { get; set; } }
  public interface INarrowedIndexed : IIndexed { new int this[int i] { get; } }
  ```

## Task List

- [ ] Write repro in Design.Stubs (inline + standalone)
- [ ] Confirm generator failure mode (interceptor type vs explicit impl mismatch)
- [ ] Design union-accessor fix for indexer dedup in `InlineModelBuilder` and `FlatRenderer`
- [ ] Extend Design.Tests with routing coverage
