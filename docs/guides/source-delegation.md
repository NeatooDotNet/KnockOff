[Home](../../README.md) > [Guides](.) > Source Delegation

# Source Delegation

`stub.Source(realImplementation)` delegates unconfigured calls to a real implementation. Configured methods (Return, Call, When) still take priority — the source is only consulted when nothing else is configured for that member.

KnockOff generates a separate `Source()` overload for each interface in the hierarchy. **You don't need a complete implementation** — pass an object that implements any interface in the hierarchy, and only the matching methods get delegated.

**Availability**: Source delegation is available for **interface stubs** only (Standalone and Inline patterns). Class stubs inherit from the base class directly and do not need `Source()`.

---

## Interface Hierarchy

When your stub implements an interface that extends other interfaces, KnockOff generates one `Source()` overload per level:

<!-- snippet: source-hierarchy-interface -->
```cs
public interface IStepList : IList<string>
{
    void AddRange(IEnumerable<string> items);
}
```
<!-- endSnippet -->

For this stub, KnockOff generates:
- `Source(IStepList)` — delegates **all** members (IStepList + IList + ICollection + IEnumerable)
- `Source(IList<string>)` — delegates IList, ICollection, and IEnumerable members only
- `Source(ICollection<string>)` — delegates ICollection and IEnumerable members only
- `Source(IEnumerable<string>)` — delegates IEnumerable members only

Each overload sets `_source` on matching interceptors and **clears** `_source` on non-matching ones. This means C# overload resolution does the right thing automatically.

### Example: Partial Source with `List<T>`

<!-- snippet: source-hierarchy-partial -->
```cs
var realList = new List<string> { "step1", "step2", "step3" };

// List<string> doesn't implement IStepList, but it does implement IList<string>
// KnockOff delegates IList/ICollection/IEnumerable members to the real list
stub.Source(realList);

IStepList list = stub;

// These work — delegated to List<string>
Assert.Equal(3, list.Count);          // ICollection<T>.Count
Assert.Equal("step1", list[0]);       // IList<T> indexer
var items = new List<string>();
foreach (var item in list)            // IEnumerable<T>
{
    items.Add(item);
}
Assert.Equal(new[] { "step1", "step2", "step3" }, items);

// AddRange is NOT delegated — it's on IStepList, which List<string> doesn't implement
// Configure it explicitly, or it returns the smart default
stub.AddRange.Call((newItems) =>
{
    foreach (var newItem in newItems)
    {
        list.Add(newItem);
    }
});
```
<!-- endSnippet -->

Without hierarchy-aware Source, you'd need a class that implements the **entire** interface just to get delegation on the parts you care about. With KnockOff, pass whatever you have — even a simple `List<T>` — and the matching members just work.

---

## Clearing Source

Remove source delegation by passing null:

<!-- snippet: source-clear -->
```cs
// Clear source to revert to smart defaults
stub.Source(null);
```
<!-- endSnippet -->

After clearing, unconfigured methods return defaults (or throw in strict mode). This is useful when you need source delegation for test setup but want to verify stub behavior independently later.

Note: `Reset()` on an individual interceptor also clears its source reference. If you reset a member and still want delegation, call `stub.Source(realImplementation)` again.

---

## Priority Order

KnockOff evaluates member calls in this order:

1. **When chains** — `stub.Method.When(...).Return(...)`
2. **Return / Call** — `stub.Method.Return(...)` or `stub.Method.Call(...)`
3. **Stub overrides** — `protected override` with `_` suffix (Standalone only)
4. **Source delegation** — `stub.Source(realImplementation)`
5. **Smart default** — KnockOff's built-in default value

The first match wins. This makes Source ideal as a baseline: set it once, then selectively override specific members at higher priority levels.

---

## When to Use Source

**Use `Source()` when:**
- Your stub extends a large interface hierarchy and you only have a partial implementation (e.g., `List<T>` for an `ICustomList<T> : IList<T>` stub)
- Testing decorator or wrapper patterns where you want real behavior by default
- Integration tests that need mostly-real dependencies with a few test overrides
- Large interfaces where manually configuring every member is impractical

**Don't use `Source()` when:**
- You want full isolation with no real dependencies (use pure stubbing)
- The source has side effects you want to avoid (database, network, file I/O)
- You need complete control over all return values

---

**Next Steps:**
- [Methods Guide](methods.md) - Complete guide to Return, Call, and When chains
- [Stub Overrides Guide](stub-overrides.md) - Default behavior through override methods
- [Verification Guide](verification.md) - Assert on stub interactions
