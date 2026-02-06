# Remove Count Properties from Interceptors

**Status:** Complete
**Priority:** Medium
**Created:** 2026-01-22
**Last Updated:** 2026-01-22

---

## Problem

KnockOff interceptors expose count properties (`GetCount`, `SetCount`, `AddCount`, `RemoveCount`, `CallCount`, `TotalCallCount`) that encourage direct assertions like `Assert.Equal(1, stub.Property.SetCount)`. This is inconsistent with the recent removal of `CallCount` and `WasCalled` from method interceptors in favor of the `Verify()` API.

The count properties:
- Duplicate functionality already available via `Verify(Times.Exactly(n))`
- Encourage verbose, less expressive test assertions
- Create API inconsistency between method interceptors (which use Verify) and other interceptors

## Solution

Remove all public count properties from interceptors and make them internal. Users should use the existing `Verify*()` methods with `Times` constraints instead:

| Current | Replacement |
|---------|-------------|
| `Assert.Equal(1, stub.Property.SetCount)` | `stub.Property.VerifySet(Times.Once)` |
| `Assert.Equal(3, stub.Property.GetCount)` | `stub.Property.VerifyGet(Times.Exactly(3))` |
| `Assert.Equal(1, stub.Event.AddCount)` | `stub.Event.VerifyAdd(Times.Once)` |
| `Assert.Equal(2, stub.Event.RemoveCount)` | `stub.Event.VerifyRemove(Times.Exactly(2))` |
| `Assert.Equal(1, stub.Indexer.GetCount)` | `stub.Indexer.VerifyGet(Times.Once)` |
| `Assert.Equal(1, stub.Indexer.SetCount)` | `stub.Indexer.VerifySet(Times.Once)` |
| `Assert.Equal(2, stub.Method.Of<T>().CallCount)` | `stub.Method.Of<T>().Verify(Times.Exactly(2))` |
| `Assert.Equal(3, stub.Method.TotalCallCount)` | `stub.Method.Verify(Times.Exactly(3))` |

---

## Plans

- [Remove Count Properties Design](../plans/completed/remove-count-properties-design.md)

---

## Tasks

- [x] Update property interceptor renderer to make GetCount/SetCount internal
- [x] Update indexer interceptor renderer to make GetCount/SetCount internal
- [x] Update event interceptor renderer to make AddCount/RemoveCount internal
- [x] Update generic method interceptor to make CallCount/TotalCallCount internal (already internal)
- [x] Update all tests to use Verify API instead of count assertions
- [x] Add release notes for breaking change
- [x] Bump minor version to 0.26.0

---

## Progress Log

**2026-01-22**: Implementation complete
- Updated FlatRenderer.cs, InlineRenderer.cs, ClassRenderer.cs for property, indexer, and event interceptors
- Generic method interceptors were already internal (no changes needed)
- Updated 12 test assertions across EventsSamples.cs, IRequiredRuleTests.cs, IEntityMetaPropertiesTests.cs, and PackageTest/Program.cs
- Created release notes v0.26.0.md
- Bumped version in Directory.Build.props
- All tests pass on net8.0, net9.0, and net10.0

---

## Results / Conclusions

**Completed Successfully**

All count properties (`GetCount`, `SetCount`, `AddCount`, `RemoveCount`) in property, indexer, and event interceptors are now internal. Users must use the Verify API:

- Properties: `VerifyGet(Times)`, `VerifySet(Times)`
- Indexers: `VerifyGet(Times)`, `VerifySet(Times)`
- Events: `VerifyAdd(Times)`, `VerifyRemove(Times)`

This completes the API simplification started in v0.24.0 (method CallCount) and v0.25.0 (method WasCalled).

**Test Results:**
- net8.0: 1214 tests passed
- net9.0: 1215 tests passed
- net10.0: 473 tests passed (subset run)

**Version:** 0.26.0
