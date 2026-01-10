# Skill Feedback: zTreatment Migration Evaluation

## Directive

**All code examples in skill files MUST be sourced from compiled samples** in `src/Tests/KnockOff.Documentation.Samples/Skills/`.

Exceptions:
- Pseudocode illustrating conceptual patterns
- "Don't do this" counter-examples

Use `#region skill:{file}:{id}` markers and run `.\scripts\extract-snippets.ps1 -Update` to sync.

---

## Overview

Feedback from evaluating the KnockOff skill while planning a Moq-to-KnockOff migration for the zTreatment project (2026-01-10).

**Context:** Migrating `VisitDateRuleTests.cs` which stubs `IVisitDateEdit` (extends `IEntityBase` from Neatoo framework).

## Task List (Refined)

Priority order:

1. - [ ] Fix "Generic methods | Not supported" error in moq-migration.md (factually wrong - they ARE supported)
2. - [ ] Fix Moq migration table (`mock.Object` → implicit conversion for interfaces, `.Object` for classes)
3. - [ ] Document interface inheritance (flattening behavior, accessing inherited members)
4. - [ ] Add `SetupProperty` → backing field migration example
5. - [ ] Rename "Interface Properties" section to "Multiple Interface Support"
6. - [ ] Document `.As{InterfaceName}()` method - when and why to use it
7. - [ ] Add backing field naming convention statement (low priority)

Deferred:
- Complete test class migration example → `future/complete-test-class-migration-example.md`

Skipped:
- ~~Nullable property backing fields~~ (works identically to non-nullable)

---

## Gap 1: Interface Inheritance from External Packages

**Severity:** Medium

**Issue:** Interfaces being stubbed extend `IEntityBase` from Neatoo framework:
```csharp
public interface IVisitDateEdit : IEntityBase
{
    long VisitId { get; }
    DateTime NewDate { get; set; }
    // ...
}
```

**Question not answered by skill:**
- Does KnockOff automatically stub inherited interface members from external packages?
- How are `IEntityBase` members accessed/configured on the stub?

**Recommendation:** Add section explaining:
1. KnockOff generates implementations for ALL interface members, including inherited
2. Example showing inherited member access pattern

---

## Gap 2: Backing Field Naming Convention

**Severity:** Low

**Issue:** Skill shows `stub.NameBacking` pattern but doesn't explicitly state this is the universal convention.

**Questions:**
- Is the pattern always `{PropertyName}Backing`?
- What about properties with unconventional names?

**Recommendation:** Add explicit statement:
> "For every property `Foo` on the interface, KnockOff generates a backing field `FooBacking`."

With examples:
```csharp
stub.NewDateBacking = DateTime.Today;        // DateTime
stub.VisitIdBacking = 42L;                   // long
stub.VisitLabelBacking = "Test";             // string
```

---

## Gap 3: Nullable Property Backing Fields

**Severity:** Low

**Issue:** `IVisitDateEdit` has nullable DateTime properties:
```csharp
DateTime? PreviousVisitDate { get; }
DateTime? NextVisitDate { get; }
```

Skill doesn't show examples of nullable backing field assignment.

**Recommendation:** Add example:
```csharp
stub.PreviousVisitDateBacking = null;                    // Explicit null
stub.PreviousVisitDateBacking = DateTime.Today.AddDays(-7);  // Has value
```

---

## Confusion 1: "Interface Properties" Section Title

**Severity:** Low

**Issue:** Section titled "Interface Properties" discusses tracking when a stub implements MULTIPLE interfaces, not how to stub interface properties.

**Current text:**
> "Each interface gets its own property for tracking and configuration"

This is confusing on first read - sounds like it's about stubbing properties.

**Recommendation:** Rename section to one of:
- "Multiple Interface Support"
- "Accessing Interface-Specific Interceptors"
- "Working with Multi-Interface Stubs"

---

## Confusion 2: Moq Migration Table - `.AsService()`

**Severity:** Medium

**Issue:** Migration table shows:
| Moq | KnockOff |
|-----|----------|
| `mock.Object` | Cast or `knockOff.AsService()` |

The `.AsService()` method is not explained elsewhere in the skill. For interface stubs, the actual pattern is implicit conversion:

```csharp
var stub = new Stubs.IVisitDateEdit();
IVisitDateEdit service = stub;  // Implicit conversion
```

**Recommendation:** Update table:
| Moq | KnockOff |
|-----|----------|
| `mock.Object` | `stub` (implicit conversion for interfaces) |

If `.AsService()` exists for a specific scenario, document when to use it.

---

## Confusion 3: Missing `SetupProperty` Migration

**Severity:** Medium

**Issue:** Moq's `SetupProperty` pattern is common but not explicitly covered:

```csharp
// Moq - property with get/set tracking
mock.SetupProperty(x => x.Active, true);
service.Active = false;
Assert.False(service.Active);  // Tracks the set
```

**Question:** What's the KnockOff equivalent?

**Recommendation:** Add explicit migration example:
```csharp
// KnockOff equivalent
stub.ActiveBacking = true;        // Initial value
service.Active = false;           // Sets backing field
Assert.False(service.Active);     // Returns backing field
Assert.Equal(1, stub.Active.SetCount);  // Tracking works
```

---

## Missing Example 1: Complete Test Class Migration

**Severity:** Medium

**Issue:** Skill shows individual patterns but no complete before/after of migrating an entire test fixture.

**Recommendation:** Add example showing:
1. Full Moq test class (constructor, helper method, multiple tests)
2. Equivalent KnockOff test class
3. Highlighting the key differences

---

## Missing Example 2: Interface Inheritance Stubbing

**Severity:** Low

**Issue:** No example of stubbing when interface inherits from another interface:

```csharp
public interface IBase { int Id { get; } }
public interface IChild : IBase { string Name { get; } }
```

**Question:** How are base interface members accessed?
```csharp
stub.IdBacking = 1;    // Is this correct?
stub.NameBacking = "Test";
```

**Recommendation:** Add explicit example showing inherited member access.

---

## Summary Table

| Item | Severity | Type |
|------|----------|------|
| Interface inheritance (external packages) | Medium | Gap |
| Backing field naming convention | Low | Gap |
| Nullable property backing fields | Low | Gap |
| "Interface Properties" section title | Low | Confusion |
| Moq migration `.AsService()` | Medium | Confusion |
| `SetupProperty` migration | Medium | Missing Example |
| Complete test class migration | Medium | Missing Example |
| Interface inheritance stubbing | Low | Missing Example |
