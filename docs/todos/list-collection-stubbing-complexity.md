# List/Collection Stubbing Complexity

**Status:** Pending
**Priority:** Medium
**Created:** 2026-01-15

---

## Problem

Stubbing interfaces that extend `IEnumerable<T>` or list-like interfaces (like Neatoo's `IEntityListBase<T>`) requires significant boilerplate code. Each stub needs manual wiring for:

- `AddItem()` or similar methods
- `GetEnumerator()` returning a real enumerator
- `Count` property
- Indexer `this[int]`
- `Clear()` method

Example from zTreatment test:

```csharp
private Stubs.ITreatmentStepList CreateStepListStub()
{
    var listStub = new Stubs.ITreatmentStepList();
    var steps = new List<ITreatmentStep>();

    // Wire up the list to use the backing list
    listStub.AddStep.OnCall = (ko, step) => steps.Add(step);
    listStub.GetEnumerator.OnCall = (ko) => steps.GetEnumerator();
    listStub.Count.OnGet = (ko) => steps.Count;
    listStub.Indexer.OnGet = (ko, idx) => steps[idx];
    listStub.Clear.OnCall = (ko) => steps.Clear();

    return listStub;
}
```

This is arguably more verbose than Moq for collection interfaces.

---

## Potential Solutions

1. **Auto-detect list interfaces**: When KnockOff detects a stub for `IEnumerable<T>` or similar, generate a backing list with common operations pre-wired.

2. **Helper method**: Provide `CreateCollectionStub<T>()` that returns a pre-configured stub with standard list behavior.

3. **Convention-based wiring**: If the stub has methods like `Add(T)`, `Clear()`, `GetEnumerator()`, auto-wire them to a backing collection.

4. **Fluent builder**: `new Stubs.IList().WithBackingList()` that handles the wiring.

---

## Tasks

- [ ] Analyze frequency of list/collection interface stubbing in test projects
- [ ] Design approach (generator vs runtime helper)
- [ ] Prototype solution
- [ ] Compare verbosity with Moq for same scenarios

---

## Related

- zTreatment: EntityListBase conversions required significant stub code
- Neatoo: IEntityListBase interface pattern
