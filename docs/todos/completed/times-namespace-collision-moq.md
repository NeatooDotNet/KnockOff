# Times Namespace Collision with Moq

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-07
**Last Updated:** 2026-02-07

---

## Problem

When a test file imports both `KnockOff` and `Moq` (common during migration), every unqualified `Times` reference produces CS0104:

```
error CS0104: 'Times' is an ambiguous reference between 'KnockOff.Times' and 'Moq.Times'
```

This makes incremental migration painful — you can't have some tests using Moq and others using KnockOff in the same file without fully qualifying every `Times` usage.

### Reproduction

```csharp
using KnockOff;
using Moq;

// KnockOff test — uses KnockOff.Times
stub.GetAttributes.Verify(Times.Once);       // CS0104

// Moq test — uses Moq.Times
mock.Verify(x => x.Foo(), Times.Once());     // CS0104
```

### Impact

Found during dotnet/runtime migration of `TypeDescriptionProviderTests.cs`. The file has ~14 tests that must stay on Moq (due to class stub generator bugs with `TypeDescriptionProvider`) while interface stubs have been migrated to KnockOff. All 42 `Times` references in the Moq tests fail to compile.

This forces users to either:
1. Fully qualify all `Times` references as `Moq.Times` or `KnockOff.Times` (tedious)
2. Keep entire files on one framework (defeats incremental migration)

## Solution Options

### Option 1: Rename `KnockOff.Times` to avoid collision

Rename to something like `Repeat`, `CallCount`, or `Occurrences`:
```csharp
stub.GetAttributes.Verify(Repeat.Once);
stub.GetAttributes.Verify(Repeat.Exactly(2));
```

Pros:
- Eliminates collision permanently
- Clean break from Moq naming

Cons:
- Breaking API change
- Less familiar to Moq users migrating

### Option 2: Put `Times` in a nested namespace

Move to `KnockOff.Verification.Times` so it doesn't conflict when importing `KnockOff`:
```csharp
using KnockOff;
// Times not imported — no collision
// Users access via KnockOff.Times.Once or add: using KnockOff.Verification;
```

Pros:
- No collision with `using KnockOff;`
- Users can opt-in to the shorter name

Cons:
- Unusual pattern — might confuse users

### Option 3: Accept the collision, document the workaround

Document that users should use `using KnockOffTimes = KnockOff.Times;` or `using MoqTimes = Moq.Times;` during migration.

Pros:
- No code changes
- Standard C# solution

Cons:
- Friction during migration (the primary use case)

---

## Decision

**Option 1 variant: Rename `KnockOff.Times` to `KnockOff.Called`.**

Rationale:
- Eliminates CS0104 collision with `Moq.Times` permanently
- `Called` reads naturally in verification context: `stub.Method.Verify(Called.Once)`, `stub.Method.Verify(Called.Never)`
- Clean break from Moq naming avoids future confusion
- Breaking change is acceptable pre-1.0

## Plans

- [Rename Times to Called](../plans/rename-times-to-called.md)

---

## Tasks

- [x] Decide on approach (rename to `Called`)
- [ ] Implement rename across library, generator, tests, design, docs, skills
- [ ] Update documentation with migration guidance
- [ ] Bump version (breaking change)

---

## Progress Log

- 2026-02-07: Found during dotnet/runtime TypeDescriptionProviderTests.cs migration. 42 CS0104 errors when file imports both `KnockOff` and `Moq`.
- 2026-02-07: Decision: rename `KnockOff.Times` to `KnockOff.Called`. Plan created at `docs/plans/rename-times-to-called.md`.

---

## Results / Conclusions

