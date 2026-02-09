# Record Stub Support

**Date:** 2026-02-08
**Related Todo:** [Record Stub Support](../todos/record-stub-support.md)
**Status:** Verified
**Last Updated:** 2026-02-08

---

## Overview

C# record types cannot currently be stubbed by KnockOff. When a user applies `[KnockOff<MyRecord>]`, the generator produces code that fails to compile with 500+ errors. Records have compiler-synthesized members (`<Clone>$`, `EqualityContract`, `Equals`, `GetHashCode`, `ToString`, `PrintMembers`) and special inheritance rules (`only records may inherit from records`) that the generator does not account for.

This plan addresses all five root causes identified in the todo by:
1. Adding an `IsRecord` flag that flows from transform through model to renderer
2. Filtering out record-synthesized members at the transform layer
3. Emitting `sealed record Impl` instead of `sealed class Impl` when the target is a record

---

## Approach

**Minimal-surface-area change.** The fix touches three layers (transform, model, renderer) but makes small, targeted changes to each. The core principle: records are classes with special synthesized members. If we (a) filter out the synthesized members and (b) use the `record` keyword for the Impl type, existing class stub logic handles everything else.

**Where each fix lives:**

| Issue | Root Cause | Fix Location | Layer |
|-------|-----------|-------------|-------|
| CS8865: Only records may inherit from records | Impl emitted as `sealed class` | ClassRenderer + StandaloneClassRenderer | Renderer |
| `<Clone>$` method interceptor | Member scanner picks up compiler-synthesized method | `ExtractClassInfo` / `GetAllVirtualMembers` | Transform |
| Duplicate `ToString`/`GetHashCode`/`Equals`/`PrintMembers` | Virtual record members scanned as interceptable | `ExtractClassInfo` / `GetAllVirtualMembers` | Transform |
| `EqualityContract` property interceptor | Virtual record property scanned as interceptable | `ExtractClassInfo` / `GetAllVirtualMembers` | Transform |
| Positional `Deconstruct` method | Compiler-synthesized, not user-overridable | `ExtractClassInfo` / `GetAllVirtualMembers` | Transform |

---

## Design

### 1. Record Detection

**Where:** `ExtractClassInfo` in `KnockOffGenerator.Transform.cs` (line 408).

Records are detected via `INamedTypeSymbol.IsRecord`, which is already used in the codebase for containing type keyword detection (Transform.cs line 1096). We add an `IsRecord` flag to `ClassStubInfo`.

```csharp
// In ClassStubInfo record (Models/ClassModels.cs)
internal sealed record ClassStubInfo(
    string FullName,
    string Name,
    EquatableArray<ClassMemberInfo> Members,
    EquatableArray<ClassConstructorInfo> Constructors,
    EquatableArray<EventMemberInfo> Events,
    bool IsOpenGeneric = false,
    EquatableArray<TypeParameterInfo> TypeParameters = default,
    bool IsRecord = false)  // <-- NEW
    : IEquatable<ClassStubInfo>;
```

**In `ExtractClassInfo`:**
```csharp
var isRecord = classType.IsRecord;
```

Pass `isRecord` to the return value:
```csharp
return new ClassStubInfo(
    classFullName, className,
    new EquatableArray<ClassMemberInfo>(members.ToArray()),
    new EquatableArray<ClassConstructorInfo>(constructors.ToArray()),
    new EquatableArray<EventMemberInfo>(events.ToArray()),
    IsOpenGeneric: isOpenGeneric,
    TypeParameters: typeParameters,
    IsRecord: isRecord);  // <-- NEW
```

### 2. Member Filtering (Transform Layer)

**Where:** Inside `ExtractClassInfo` in `KnockOffGenerator.Transform.cs`, within the `GetAllVirtualMembers` iteration (starting around line 511).

The existing code iterates `GetAllVirtualMembers(classSource)` and filters to virtual/abstract/override members that are not sealed. We add record-specific filtering **after** the existing virtual check but **before** adding to the members list.

**Members to skip when `isRecord == true`:**

| Member | Detection | Why Skip |
|--------|-----------|----------|
| `<Clone>$` method | `method.Name == "<Clone>$"` | Compiler-synthesized, invalid C# identifier, cannot be overridden in user code |
| `EqualityContract` property | `property.Name == "EqualityContract"` | Record infrastructure, overriding produces incorrect behavior |
| `Equals(T)` method | `method.Name == "Equals" && method.Parameters.Length == 1 && paramType == classType` | Record value equality synthesized member |
| `Equals(object)` method | `method.Name == "Equals" && method.Parameters.Length == 1 && paramType is object` | Record value equality synthesized member |
| `GetHashCode()` method | `method.Name == "GetHashCode" && method.Parameters.Length == 0` | Record value equality synthesized member |
| `ToString()` method | `method.Name == "ToString" && method.Parameters.Length == 0` | Record formatting synthesized member |
| `PrintMembers(StringBuilder)` method | `method.Name == "PrintMembers" && method.Parameters.Length == 1` | Record formatting synthesized member |
| `Deconstruct(...)` method | `method.Name == "Deconstruct" && method.MethodKind == MethodKind.Ordinary` | Positional record synthesized deconstructor |

**Implementation approach:** Add a helper method `IsRecordSynthesizedMember` that returns true for any of the above. Call it in the property and method filter blocks within `ExtractClassInfo`:

```csharp
// In the property filtering block (around line 520):
if (isRecord && IsRecordSynthesizedProperty(property))
    continue;

// In the method filtering block (around line 534):
if (isRecord && IsRecordSynthesizedMethod(method))
    continue;
```

**Helper methods:**

```csharp
/// <summary>
/// Returns true for record-synthesized properties that should not be intercepted.
/// </summary>
private static bool IsRecordSynthesizedProperty(IPropertySymbol property)
{
    return property.Name == "EqualityContract";
}

/// <summary>
/// Returns true for record-synthesized methods that should not be intercepted.
/// The <Clone>$ method, equality members, formatting members, and Deconstruct
/// are all compiler-generated for records and should be inherited as-is.
/// </summary>
private static bool IsRecordSynthesizedMethod(IMethodSymbol method)
{
    // <Clone>$ is the copy constructor used by `with` expressions
    if (method.Name == "<Clone>$")
        return true;

    // Value equality: Equals(T), Equals(object), GetHashCode()
    if (method.Name == "Equals")
        return true;

    if (method.Name == "GetHashCode" && method.Parameters.Length == 0)
        return true;

    // Formatting: ToString(), PrintMembers(StringBuilder)
    if (method.Name == "ToString" && method.Parameters.Length == 0)
        return true;

    if (method.Name == "PrintMembers" && method.Parameters.Length == 1)
        return true;

    // Positional record deconstructor
    if (method.Name == "Deconstruct")
        return true;

    return false;
}
```

**Design decision: Skip ALL `Equals` overloads, not just the typed one.** Records synthesize both `Equals(MyRecord)` and `Equals(object)`. Both should be skipped. The simplest and safest approach is to filter by name only, skipping all Equals methods. This is correct because:
- User-defined `Equals` on records would shadow the synthesized version, and the record still controls the equality semantics
- Even if a user defines a virtual `Equals` method on a record (extremely rare), the record infrastructure depends on it, so intercepting it would be wrong

**Positional properties ARE included.** Positional record properties (e.g., `Name` and `Age` in `record PositionalRecord(string Name, int Age)`) are virtual and ARE the primary value of stubbing records. They pass through the existing property filter unchanged.

### 3. Record Keyword for Impl Class (Renderer Layer)

**Where:** `ClassRenderer.RenderImplClass` (line 601) and `StandaloneClassRenderer.RenderImplClass` (line 598).

Currently both render:
```csharp
w.Line($"{indent}private sealed class Impl : {cls.BaseType}");
```

This must become:
```csharp
var implKeyword = cls.IsRecord ? "record" : "class";
w.Line($"{indent}private sealed {implKeyword} Impl : {cls.BaseType}");
```

**Model propagation:** The `IsRecord` flag must flow from `ClassStubInfo` through the builder to both `InlineClassStubModel` and `StandaloneClassGenerationUnit`.

### 4. Model Changes Summary

| Model | Change | File |
|-------|--------|------|
| `ClassStubInfo` | Add `bool IsRecord = false` | `Models/ClassModels.cs` |
| `InlineClassStubModel` | Add `bool IsRecord = false` | `Model/Inline/InlineClassStubModel.cs` |
| `StandaloneClassGenerationUnit` | Add `bool IsRecord = false` | `Model/StandaloneClass/StandaloneClassGenerationUnit.cs` |

### 5. Builder Changes

**ClassModelBuilder.Build** (line 23): Pass `cls.IsRecord` to `InlineClassStubModel`.

```csharp
// At end of Build method (around line 251):
return new InlineClassStubModel(
    // ... existing params ...
    HasRequiredMembers: hasRequiredMembers,
    RequiredMemberNames: requiredMemberNames,
    IsRecord: cls.IsRecord);  // <-- NEW
```

**StandaloneClassModelBuilder.Build** (line 24): Pass `cls.IsRecord` to `StandaloneClassGenerationUnit`.

```csharp
// At end of Build method (around line 311):
return new StandaloneClassGenerationUnit(
    // ... existing params ...
    BaseClassMethods: baseClassMethods,
    IsRecord: cls.IsRecord);  // <-- NEW
```

### 6. Positional Record Constructor Handling

Positional records have a **primary constructor** that sets all positional properties. For `record PositionalRecord(string Name, int Age)`, the constructors are:
- `PositionalRecord(string Name, int Age)` — primary constructor
- `PositionalRecord(PositionalRecord original)` — copy constructor (used by `with`)

The **copy constructor** is `protected` and is exposed through the `<Clone>$` method. It should be forwarded like any other constructor. The primary constructor should also be forwarded.

The existing constructor extraction logic in `ExtractClassInfo` already handles this correctly since it extracts all accessible constructors. No additional changes needed.

**Important caveat:** The copy constructor has a parameter of the record's own type. The Impl constructor will receive `(StubClassName stub, PositionalRecord original)` and call `: base(original)`. This should compile correctly because the nested Impl record inherits from the target record.

### 7. Deconstruct Decision

**Decision: Skip `Deconstruct`.** The `Deconstruct` method for positional records is synthesized by the compiler with `out` parameters. Intercepting it would:
- Create an interceptor with `out` parameters (awkward API)
- Not provide meaningful value (users don't typically need to control deconstruction behavior)
- The Impl record inherits the correct Deconstruct automatically

---

## Scope

### Patterns Affected

| Pattern | Affected | Pipeline | Notes |
|---------|----------|----------|-------|
| 1. Standalone | No | -- | Interface stubs, not applicable |
| 2. Generic Standalone | No | -- | Interface stubs, not applicable |
| 3. Standalone Class | **Yes** | `TransformStandaloneClass` / `StandaloneClassModelBuilder` / `StandaloneClassRenderer` | Uses `ExtractClassInfo` for member scanning |
| 4. Generic Standalone Class | **Yes** | Same as Pattern 3 | Shares `ExtractClassInfo` |
| 5. Inline Interface | No | -- | Interface stubs, not applicable |
| 6. Inline Class | **Yes** | `TransformInlineStubClass` / `ClassModelBuilder` / `ClassRenderer` | Uses `ExtractClassInfo` for member scanning |
| 7. Inline Delegate | No | -- | Delegate stubs, not applicable |
| 8. Open Generic Interface | No | -- | Interface stubs, not applicable |
| 9. Open Generic Class | **Yes** | Uses `ExtractClassInfo` → `ClassModelBuilder` → `ClassRenderer` via `InlineRenderer` | Shares inline class pipeline |

### Member Types

| Member Type | Impact | Notes |
|-------------|--------|-------|
| Methods | Filter synthesized methods (`<Clone>$`, `Equals`, `GetHashCode`, `ToString`, `PrintMembers`, `Deconstruct`) | Transform layer |
| Properties | Filter `EqualityContract`; positional properties pass through normally | Transform layer |
| Indexers | N/A | Records don't synthesize indexers |
| Events | N/A | Records don't synthesize events; user-defined events on records work as-is |

### Pipeline Verification

Each affected pipeline shares `ExtractClassInfo` for member scanning. The member filtering changes in `ExtractClassInfo` automatically apply to all four affected patterns. The `IsRecord` flag flows through independent model types in each pipeline.

| Pipeline Path | Member Filtering | IsRecord Propagation |
|---------------|-----------------|---------------------|
| Inline: `ExtractClassInfo` -> `ClassStubInfo` -> `ClassModelBuilder` -> `InlineClassStubModel` -> `ClassRenderer` | Via `ExtractClassInfo` | `ClassStubInfo.IsRecord` -> `InlineClassStubModel.IsRecord` -> `ClassRenderer` |
| Standalone: `ExtractClassInfo` -> `ClassStubInfo` -> `StandaloneClassModelBuilder` -> `StandaloneClassGenerationUnit` -> `StandaloneClassRenderer` | Via `ExtractClassInfo` | `ClassStubInfo.IsRecord` -> `StandaloneClassGenerationUnit.IsRecord` -> `StandaloneClassRenderer` |

---

## Architectural Verification

### Nine Patterns Analysis

| Pattern | Impact | Verified |
|---------|--------|----------|
| 1. Standalone | None | N/A - different pipeline, no class inheritance |
| 2. Generic Standalone | None | N/A - different pipeline, no class inheritance |
| 3. Standalone Class | **Affected** | `ExtractClassInfo` filters + `IsRecord` in `StandaloneClassRenderer` |
| 4. Generic Standalone Class | **Affected** | Same as Pattern 3 |
| 5. Inline Interface | None | N/A - different pipeline |
| 6. Inline Class | **Affected** | `ExtractClassInfo` filters + `IsRecord` in `ClassRenderer` |
| 7. Inline Delegate | None | N/A - different pipeline |
| 8. Open Generic Interface | None | N/A - different pipeline |
| 9. Open Generic Class | **Affected** | Same inline class pipeline as Pattern 6 |

### Breaking Changes

**None.** This change only affects records, which currently fail to compile. No existing working code is affected. The `IsRecord` flag defaults to `false`, so all existing non-record class stubs generate identical code.

### Design Project Verification

Verification will be performed during implementation using `RecordTests.cs` in the test project. The Design.Stubs project does not currently contain record examples. Design.Stubs verification is deferred to implementation phase since the feature is entirely new (records currently produce compilation errors, so there is no existing behavior to verify).

### Codebase Deep-Dive

**Files examined:**

- `src/Generator/KnockOffGenerator.Transform.cs` — Member scanning in `ExtractClassInfo` (lines 408-579) and `GetAllVirtualMembers` (lines 597-616). Both property and method filtering happens in the loop starting at line 511. Records' `EqualityContract`, `<Clone>$`, `Equals`, `GetHashCode`, `ToString`, `PrintMembers`, and `Deconstruct` all pass through the existing virtual/abstract check and are NOT filtered.
- `src/Generator/Models/ClassModels.cs` — `ClassStubInfo` (line 12) and `ClassMemberInfo` (line 33) record definitions. No `IsRecord` flag exists.
- `src/Generator/Builder/ClassModelBuilder.cs` — Builds `InlineClassStubModel` from `ClassStubInfo`. No record awareness.
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` — Builds `StandaloneClassGenerationUnit` from `StandaloneClassStubInfo`. No record awareness.
- `src/Generator/Renderer/ClassRenderer.cs` — Line 601: `private sealed class Impl : {cls.BaseType}` — always emits `class`, never `record`.
- `src/Generator/Renderer/StandaloneClassRenderer.cs` — Line 598: `private sealed class Impl : {unit.TargetClassForInheritance}` — always emits `class`, never `record`.
- `src/Generator/Model/Inline/InlineClassStubModel.cs` — No `IsRecord` field.
- `src/Generator/Model/StandaloneClass/StandaloneClassGenerationUnit.cs` — No `IsRecord` field.
- `src/Tests/KnockOffTests/RecordTests.cs` — Reproduction test with three record types: `MyRecord`, `PositionalRecord(string Name, int Age)`, `AbstractRecord`.
- `src/Tests/KnockOffTests/Generated/.../RecordTests.Stubs.g.cs` — 436KB of generated code (500+ errors). Confirms: `EqualityContract` interceptor generated, `ToString`/`PrintMembers`/`Equals`/`GetHashCode` interceptors generated, `<Clone>$` picked up, `Impl` uses `sealed class` instead of `sealed record`.

**Key insight from generated code (line 3269):**
```
private sealed class Impl : global::KnockOffTests.MyRecord
```
This is the primary CS8865 error. Must be `sealed record Impl`.

**Key insight from generated code (lines 13-14):**
```
/// <summary>Tracks and configures behavior for EqualityContract.</summary>
public sealed class MyRecord_EqualityContractInterceptor
```
`EqualityContract` should never be intercepted.

---

## Implementation Steps

### Phase 1: Transform Layer - Member Filtering

1. Add `IsRecordSynthesizedProperty` helper to `KnockOffGenerator.Transform.cs`
2. Add `IsRecordSynthesizedMethod` helper to `KnockOffGenerator.Transform.cs`
3. In `ExtractClassInfo`, detect `isRecord = classType.IsRecord`
4. In the property filtering loop, add: `if (isRecord && IsRecordSynthesizedProperty(property)) continue;`
5. In the method filtering loop, add: `if (isRecord && IsRecordSynthesizedMethod(method)) continue;`
6. Add `IsRecord` parameter to `ClassStubInfo` return value
7. **Checkpoint:** Build the generator project. Verify no compilation errors.

### Phase 2: Model Layer - IsRecord Propagation

8. Add `bool IsRecord = false` to `ClassStubInfo` in `Models/ClassModels.cs`
9. Add `bool IsRecord = false` to `InlineClassStubModel` in `Model/Inline/InlineClassStubModel.cs`
10. Add `bool IsRecord = false` to `StandaloneClassGenerationUnit` in `Model/StandaloneClass/StandaloneClassGenerationUnit.cs`
11. In `ClassModelBuilder.Build`, pass `cls.IsRecord` to `InlineClassStubModel`
12. In `StandaloneClassModelBuilder.Build`, pass `cls.IsRecord` to `StandaloneClassGenerationUnit`
13. **Checkpoint:** Build the generator project. Verify no compilation errors.

### Phase 3: Renderer Layer - Record Keyword

14. In `ClassRenderer.RenderImplClass` (line 601), change:
    - From: `private sealed class Impl : {cls.BaseType}`
    - To: `private sealed {implKeyword} Impl : {cls.BaseType}` where `implKeyword = cls.IsRecord ? "record" : "class"`
15. In `StandaloneClassRenderer.RenderImplClass` (line 598), make the same change using `unit.IsRecord`
16. **Checkpoint:** Build the full solution including tests. The `RecordTests.cs` should now compile.

### Phase 4: Test Verification

17. Run all tests: `dotnet test src/KnockOff.sln`
18. Verify `RecordTests` pass (virtual method, positional properties, abstract method)
19. Verify no regressions in existing class stub tests
20. **Checkpoint:** All tests pass with zero failures.

### Phase 5: Additional Test Coverage

21. Add test for record with `Deconstruct` (verify it's skipped, not intercepted)
22. Add test for record with user-defined virtual method plus synthesized members
23. Add test confirming positional record properties are interceptable
24. Consider adding standalone class record test (`[KnockOffBase<MyRecord>]`)
25. **Checkpoint:** All new and existing tests pass.

---

## Acceptance Criteria

- [ ] `[KnockOff<MyRecord>]` generates compilable code (zero compiler errors)
- [ ] `[KnockOff<PositionalRecord>]` generates compilable code with interceptable positional properties
- [ ] `[KnockOff<AbstractRecord>]` generates compilable code with interceptable abstract methods
- [ ] `[KnockOffBase<MyRecord>]` generates compilable code (standalone class pattern)
- [ ] Generated Impl type uses `sealed record Impl` when target is a record
- [ ] `EqualityContract` is NOT intercepted
- [ ] `<Clone>$` method is NOT intercepted
- [ ] `Equals`, `GetHashCode`, `ToString`, `PrintMembers` are NOT intercepted
- [ ] `Deconstruct` is NOT intercepted
- [ ] Positional record properties (`Name`, `Age`) ARE intercepted and configurable
- [ ] User-defined virtual methods on records ARE intercepted
- [ ] All existing class stub tests pass (no regressions)
- [ ] Record `with` expressions work correctly on `.Object` (inherits record copy semantics)

---

## Dependencies

- Existing `ExtractClassInfo` method in `KnockOffGenerator.Transform.cs`
- Existing `ClassRenderer` and `StandaloneClassRenderer` for Impl class generation
- Existing `ClassModelBuilder` and `StandaloneClassModelBuilder` for model construction
- `INamedTypeSymbol.IsRecord` API (available in Microsoft.CodeAnalysis, already used in codebase at line 1096)

---

## Risks / Considerations

1. **Record struct types.** `record struct` types are value types and cannot be stubbed via inheritance. The existing `IsSealed` check (KO2001) should catch these since structs are effectively sealed. Need to verify this during implementation. If not, add an explicit diagnostic.

2. **Sealed records.** All non-abstract records without virtual members are effectively sealed for our purposes. The existing KO2001 (sealed) and KO2004 (no virtual members) diagnostics handle this.

3. **Record with inheritance.** Records can inherit from other records: `record Derived : Base`. The Impl record inherits from the target record and gets the full hierarchy. No special handling needed beyond what exists.

4. **Primary constructor parameter names vs property names.** Positional records have constructor parameters that match property names exactly. The constructor forwarding in the Impl class should work correctly since constructors are already forwarded by the existing logic.

5. **`with` expression support.** `record.with { Name = "new" }` works via the `<Clone>$` method and property setters. Since Impl inherits from the record and we skip `<Clone>$`, the record's own copy semantics are preserved. The `with` expression on `stub.Object` will call the record's copy constructor and then set the specified properties. Since properties on the Impl class delegate to interceptors, `with` expressions on the Impl will invoke the interceptors. This is the correct behavior.

6. **Equality semantics preservation.** By skipping `Equals`/`GetHashCode` overrides in the Impl record, the Impl inherits the record's default value-equality semantics based on its properties. Since the Impl's property overrides delegate to interceptors, two Impl instances will be "equal" based on their interceptor-returned values. This is acceptable behavior for a test stub.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-08

### Review Summary

- **Files examined:** 10 source files (Transform.cs, ClassModels.cs, ClassRenderer.cs, StandaloneClassRenderer.cs, InlineClassStubModel.cs, StandaloneClassGenerationUnit.cs, ClassModelBuilder.cs, StandaloneClassModelBuilder.cs, KnockOffGenerator.StandaloneClass.cs, RecordTests.cs)
- **Questions checked:** 16 of 16 (completeness, correctness, clarity, risk)
- **Devil's advocate items:** 5 generated, all acceptable

### Why Approved

This plan is a minimal-surface-area, well-targeted change that:
1. Addresses a clear bug (records generating 500+ compilation errors)
2. Every file path, line number, and code snippet verified against the actual codebase
3. The `IsRecord` propagation follows the established `IsOpenGeneric` / `HasRequiredMembers` pattern
4. The `IsRecord = false` default guarantees zero impact on existing functionality
5. All identified edge cases (record `Equals` overloads, custom `Deconstruct`, record inheritance) are either extreme or already handled

### Minor Correction

- **Risk #1 (record struct):** Plan says "The existing `IsSealed` check (KO2001) should catch these." Actually, `record struct` has `TypeKind.Struct`, which is caught earlier by KO1001 in both the inline path (Transform.cs line 102) and standalone path (StandaloneClass.cs line 76). The `IsSealed` check is never reached. This is documentation-only; no functional impact.

### Concerns: None

---

## Implementation Contract

**Created:** 2026-02-08
**Approved by:** knockoff-developer

### In Scope

**Phase 1: Transform Layer - Member Filtering**
- [x] Add `IsRecordSynthesizedProperty` helper method to `src/Generator/KnockOffGenerator.Transform.cs`
- [x] Add `IsRecordSynthesizedMethod` helper method to `src/Generator/KnockOffGenerator.Transform.cs`
- [x] In `ExtractClassInfo`, detect `isRecord = classSource.IsRecord`
- [x] In property filtering loop, add: `if (isRecord && IsRecordSynthesizedProperty(property)) continue;`
- [x] In method filtering loop, add: `if (isRecord && IsRecordSynthesizedMethod(method)) continue;`
- [x] Add `IsRecord: isRecord` to `ClassStubInfo` constructor call
- [x] Add `bool IsRecord = false` parameter to `ClassStubInfo` record in `src/Generator/Models/ClassModels.cs`
- [x] **Checkpoint:** `dotnet build src/Generator/Generator.csproj` succeeds

**Phase 2: Model Layer - IsRecord Propagation**
- [x] Add `bool IsRecord = false` to `InlineClassStubModel` in `src/Generator/Model/Inline/InlineClassStubModel.cs`
- [x] Add `bool IsRecord = false` to `StandaloneClassGenerationUnit` in `src/Generator/Model/StandaloneClass/StandaloneClassGenerationUnit.cs`
- [x] In `ClassModelBuilder.Build`, pass `IsRecord: cls.IsRecord` to `InlineClassStubModel` constructor
- [x] In `StandaloneClassModelBuilder.Build`, pass `IsRecord: cls.IsRecord` to `StandaloneClassGenerationUnit` constructor
- [x] **Checkpoint:** `dotnet build src/Generator/Generator.csproj` succeeds

**Phase 3: Renderer Layer - Record Keyword**
- [x] In `ClassRenderer.RenderImplClass`, change `private sealed class Impl` to conditionally emit `record` when `cls.IsRecord`
- [x] In `StandaloneClassRenderer.RenderImplClass`, same change using `unit.IsRecord`
- [x] **Checkpoint:** `dotnet build src/KnockOff.sln` succeeds (including test projects). RecordTests.cs compiles.

**Phase 4: Test Verification**
- [x] Run `dotnet test src/KnockOff.sln` -- all existing tests pass
- [x] Verify RecordTests pass: `Record_VirtualMethod_CanBeStubbed`, `Record_PositionalWithVirtualMethod_CanBeStubbed`, `Record_AbstractMethod_CanBeStubbed`
- [x] **Checkpoint:** All tests pass with zero failures across net8.0/net9.0/net10.0

**Phase 5: Additional Test Coverage**
- [x] Add test for standalone class record pattern (`[KnockOffBase<MyRecord>]`)
- [x] Verified `EqualityContract`, `Equals`, `GetHashCode`, `ToString`, `PrintMembers`, `<Clone>$`, `Deconstruct` are NOT intercepted (grep confirms zero matches in generated code)
- [x] Add test verifying explicit virtual property IS interceptable on record
- [x] Add test verifying explicit virtual method IS interceptable on record
- [x] Add test verifying positional property values are preserved on Object
- [x] Discovery: Positional record properties are NOT interceptable via inheritance (Roslyn API limitation; see Implementation Progress for details)
- [x] **Checkpoint:** All 8 record tests pass, all existing tests pass

### Explicitly Out of Scope

- `record struct` support (rejected by KO1001 at TypeKind check; value types cannot be stubbed via inheritance)
- Intercepting record-synthesized members (`Equals`, `GetHashCode`, `ToString`, `PrintMembers`, `EqualityContract`, `<Clone>$`, `Deconstruct`)
- Design.Stubs record examples (can be added post-implementation as a follow-up)
- Updating the KnockOff skill file (follow-up task)

### Verification Gates

1. **After Phase 1:** Generator project compiles. `ClassStubInfo` has `IsRecord` parameter.
2. **After Phase 2:** Generator project compiles. Both `InlineClassStubModel` and `StandaloneClassGenerationUnit` have `IsRecord`.
3. **After Phase 3:** Full solution including test projects compiles. `RecordTests.cs` generates valid code.
4. **After Phase 4:** All existing tests pass. Three `RecordTests` pass.
5. **Final:** All new and existing tests pass. `dotnet test src/KnockOff.sln` reports zero failures.

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (any test other than RecordTests starts failing)
- Architectural contradiction discovered (e.g., `IsRecord` cannot propagate through existing model structure)
- Generated record stub code does not compile (beyond RecordTests)
- Constructor forwarding for positional records produces compilation errors

---

## Implementation Progress

**Started:** 2026-02-08

**Phase 1: Transform Layer - Member Filtering**
- [x] Added `IsRecordSynthesizedProperty` helper to `KnockOffGenerator.Transform.cs`
- [x] Added `IsRecordSynthesizedMethod` helper to `KnockOffGenerator.Transform.cs`
- [x] In `ExtractClassInfo`, detect `isRecord = classSource.IsRecord`
- [x] In property filtering, skip record-synthesized properties when `isRecord`
- [x] In method filtering, skip record-synthesized methods when `isRecord`
- [x] Added `IsRecord: isRecord` to `ClassStubInfo` return
- [x] Added `bool IsRecord = false` parameter to `ClassStubInfo` record in `Models/ClassModels.cs`
- [x] **Checkpoint:** Generator builds successfully

**Phase 2: Model Layer - IsRecord Propagation**
- [x] Added `bool IsRecord = false` to `InlineClassStubModel`
- [x] Added `bool IsRecord = false` to `StandaloneClassGenerationUnit`
- [x] In `ClassModelBuilder.Build`, pass `IsRecord: cls.IsRecord`
- [x] In `StandaloneClassModelBuilder.Build`, pass `IsRecord: cls.IsRecord`
- [x] **Checkpoint:** Generator builds successfully

**Phase 3: Renderer Layer - Record Keyword**
- [x] In `ClassRenderer.RenderImplClass`, conditionally emit `sealed record Impl` vs `sealed class Impl`
- [x] In `StandaloneClassRenderer.RenderImplClass`, same change with `unit.IsRecord`
- [x] **Checkpoint:** Full solution builds with zero errors

**Phase 4: Test Verification**
- [x] All existing tests pass (zero regressions)
- [x] Three initial record tests pass
- [x] **Checkpoint:** All tests pass across net8.0/net9.0/net10.0

**Phase 5: Additional Test Coverage**
- [x] Added standalone class record test (`[KnockOffBase<MyRecord>]`)
- [x] Added test for record with explicit virtual property (interceptable)
- [x] Added test for record with explicit virtual method (interceptable)
- [x] Added test verifying positional property values are preserved on Object
- [x] Added test verifying Object is assignable to record type
- [x] **Checkpoint:** All 8 record tests pass, all existing tests pass

### Discovery: Positional Record Properties Are NOT Interceptable

During implementation, testing with Roslyn 4.14.0 revealed that **positional record properties** (e.g., `Name` and `Age` on `record Foo(string Name, int Age)`) are NOT reported as `IsVirtual = true` by the Roslyn symbol API. The C# compiler also confirms this with CS0506: "cannot override inherited member because it is not marked virtual."

While the C# language spec says these properties are "virtual unless the record type is sealed," the actual Roslyn compilation treats them as non-overridable via the `override` keyword. They can only be re-declared in derived positional records via positional parameter matching, not via `override`.

**Consequence:** Positional record properties cannot be intercepted via KnockOff's inheritance-based stubbing. This is a fundamental limitation of the approach, not a bug. The test was updated to use a positional record with an explicitly declared virtual method instead.

**What works for records:**
- Explicitly declared `virtual` methods and properties
- `abstract` methods and properties
- `override` methods and properties

**What does NOT work (by design):**
- Positional record properties (synthesized, not truly virtual)
- Record-synthesized members (Equals, GetHashCode, ToString, etc.)

---

## Completion Evidence

### Test Results

**Full solution: `dotnet test src/KnockOff.sln` -- zero failures**

| Test Project | net8.0 | net9.0 | net10.0 |
|---|---|---|---|
| KnockOffTests | 1333 passed | 1334 passed | 1334 passed |
| KnockOff.Documentation.Samples | 599 passed | 599 passed | 599 passed |
| KnockOff.NeatooInterfaceTests | 473 passed | 473 passed | 473 passed |
| KnockOffTests.AssemblyStrict | 14 passed | 14 passed | -- |

**Record-specific tests (8 tests, all pass):**
- `Record_VirtualMethod_CanBeStubbed` -- inline stub, virtual method on record
- `Record_PositionalWithVirtualMethod_CanBeStubbed` -- positional record with virtual method
- `Record_PositionalProperties_PreservedOnObject` -- positional property values via constructor
- `Record_AbstractMethod_CanBeStubbed` -- abstract record with abstract method
- `Record_ExplicitVirtualProperty_CanBeStubbed` -- virtual property on record, intercepted
- `Record_ExplicitVirtualMethod_CanBeStubbed` -- virtual method on record, intercepted
- `Record_StandaloneClassStub_CanBeCreated` -- standalone class pattern for record
- `Record_ObjectIsRecord` -- Object is assignable to record type

### Generated Code Verification

**Inline stubs (ClassRenderer):**
```
private sealed record Impl : global::KnockOffTests.MyRecord
private sealed record Impl : global::KnockOffTests.PositionalRecordWithMethod
private sealed record Impl : global::KnockOffTests.AbstractRecord
private sealed record Impl : global::KnockOffTests.RecordWithVirtualProperty
```

**Standalone class stub (StandaloneClassRenderer):**
```
private sealed record Impl : global::KnockOffTests.MyRecord
```

**Synthesized members NOT intercepted:**
- No `EqualityContract` interceptor in any generated file
- No `<Clone>$`, `Equals`, `GetHashCode`, `ToString`, `PrintMembers`, or `Deconstruct` interceptors

### Files Modified

**Generator (production code):**
- `src/Generator/Models/ClassModels.cs` -- Added `bool IsRecord = false` to `ClassStubInfo`
- `src/Generator/KnockOffGenerator.Transform.cs` -- Added `isRecord` detection, `IsRecordSynthesizedProperty`, `IsRecordSynthesizedMethod` helpers, member filtering
- `src/Generator/Model/Inline/InlineClassStubModel.cs` -- Added `bool IsRecord = false`
- `src/Generator/Model/StandaloneClass/StandaloneClassGenerationUnit.cs` -- Added `bool IsRecord = false`
- `src/Generator/Builder/ClassModelBuilder.cs` -- Propagate `IsRecord`
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- Propagate `IsRecord`
- `src/Generator/Renderer/ClassRenderer.cs` -- Conditional `record` vs `class` keyword
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- Conditional `record` vs `class` keyword

**Tests:**
- `src/Tests/KnockOffTests/RecordTests.cs` -- 8 tests covering inline, standalone, virtual methods, virtual properties, abstract methods, positional records

### All Contract Items Confirmed Complete

---

## Architect Verification

**Verified:** 2026-02-08
**Verdict:** VERIFIED

### Independent Build Results

```
dotnet build src/KnockOff.sln -- Build succeeded. 0 Warning(s) 0 Error(s)
```

### Independent Test Results

| Test Project | net8.0 | net9.0 | net10.0 |
|---|---|---|---|
| KnockOffTests | 1333 passed, 0 failed | 1334 passed, 0 failed | 1334 passed, 0 failed |
| KnockOff.Documentation.Samples | 599 passed, 0 failed | 599 passed, 0 failed | 599 passed, 0 failed |
| KnockOff.NeatooInterfaceTests | 473 passed, 0 failed | 473 passed, 0 failed | 473 passed, 0 failed |
| KnockOffTests.AssemblyStrict | 14 passed, 0 failed | 14 passed, 0 failed | 14 passed, 0 failed |

**Zero failures across all projects and all target frameworks.**

### Design Match

All production code changes match the plan:

- **Transform layer**: `isRecord = classSource.IsRecord` detection, `IsRecordSynthesizedProperty` and `IsRecordSynthesizedMethod` helper methods, filtering in property and method loops -- matches plan Section 2 exactly.
- **Model layer**: `bool IsRecord = false` added to `ClassStubInfo`, `InlineClassStubModel`, and `StandaloneClassGenerationUnit` -- matches plan Sections 1 and 4.
- **Builder layer**: `IsRecord: cls.IsRecord` propagated in both `ClassModelBuilder.Build` and `StandaloneClassModelBuilder.Build` -- matches plan Section 5.
- **Renderer layer**: `var implKeyword = cls.IsRecord ? "record" : "class"` in `ClassRenderer.RenderImplClass` and `var implKeyword = unit.IsRecord ? "record" : "class"` in `StandaloneClassRenderer.RenderImplClass` -- matches plan Section 3.

### Generated Code Spot-Check

**Inline stubs (RecordTests.Stubs.g.cs):**
- Line 325: `private sealed record Impl : global::KnockOffTests.MyRecord` -- CORRECT (record, not class)
- Line 775: `private sealed record Impl : global::KnockOffTests.PositionalRecordWithMethod` -- CORRECT
- Line 1225: `private sealed record Impl : global::KnockOffTests.AbstractRecord` -- CORRECT
- Line 2407: `private sealed record Impl : global::KnockOffTests.RecordWithVirtualProperty` -- CORRECT

**Standalone stub (MyRecordStandaloneStub.g.cs):**
- Line 320: `private sealed record Impl : global::KnockOffTests.MyRecord` -- CORRECT

**Synthesized member filtering:**
- Zero occurrences of `EqualityContract` in RecordTests.Stubs.g.cs -- CORRECT
- Zero occurrences of `Clone` in RecordTests.Stubs.g.cs -- CORRECT
- Zero occurrences of `PrintMembers` or `Deconstruct` in RecordTests.Stubs.g.cs -- CORRECT
- Zero occurrences of `ToString` or `GetHashCode` interceptors in RecordTests.Stubs.g.cs -- CORRECT
- The only `Equals` references are in `When` matcher code (`System.Object.Equals`), not interceptors -- CORRECT

**Helper method verification (KnockOffGenerator.Transform.cs):**
- `IsRecordSynthesizedProperty`: Filters `EqualityContract` -- matches plan
- `IsRecordSynthesizedMethod`: Filters `<Clone>$`, `Equals`, `GetHashCode` (0 params), `ToString` (0 params), `PrintMembers` (1 param), `Deconstruct` -- matches plan

### Test Coverage Assessment

8 record-specific tests covering:
1. Inline stub with virtual method (MyRecord)
2. Positional record with virtual method (PositionalRecordWithMethod)
3. Positional property value preservation through constructor
4. Abstract record with abstract method
5. Explicit virtual property interception
6. Explicit virtual method interception
7. Standalone class pattern for records
8. Object assignability to record type

### Discovery Acknowledged

Positional record properties are NOT interceptable via inheritance (Roslyn API reports them as non-virtual). This is a fundamental limitation of the inheritance-based stubbing approach, not a bug in the implementation. The plan was updated to reflect this discovery.
