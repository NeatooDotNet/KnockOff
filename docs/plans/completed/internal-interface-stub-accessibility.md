# Internal Interface Stub Accessibility

**Date:** 2026-03-07
**Related Todo:** [Internal Interface Stub Accessibility](../todos/completed/internal-interface-stub-accessibility.md)
**Status:** Complete
**Last Updated:** 2026-03-07

---

## Overview

KnockOff hardcodes `public class` on all generated stub classes and Base helper classes. When the target type (interface or class) is `internal`, this causes a compilation error because a `public` class cannot implement/inherit an `internal` type. The fix adds a target type accessibility field to each pipeline's model and uses it in each renderer's class declaration.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/internal-interface-stub-accessibility.md#requirements-review)

### Relevant Existing Requirements

#### Governing Constraints

- **Interceptor-as-Property Principle (CLAUDE.md)**: Not affected. This change modifies the accessibility modifier on generated stub class declarations, not the interceptor API. `stub.Method` remains a property returning an interceptor object.

- **API Consistency Principle (CLAUDE.md)**: Directly relevant. The fix must be applied consistently across all four pipelines. The current bug (hardcoded `public class`) affects all patterns, and the fix must address all pipelines equally.

- **Pipeline Verification Rule (CLAUDE.md)**: Critically relevant. Each pipeline has an independent hardcoded `public class` location. Fixing one pipeline does NOT fix the others. Each must be independently modified and verified.

#### Existing Tests

- No existing tests verify generated stub class accessibility. All existing Design.Stubs types use `public` accessibility. No behavioral contracts are at risk of being broken.

### Gaps

1. No Design.Stubs coverage for internal types (zero examples in `src/Design/Design.Stubs/`)
2. No test coverage for accessibility in `src/Tests/KnockOffTests/`
3. No coverage for `protected internal` or `private protected` accessibility combinations on target types
4. No coverage for nested internal types (public interface nested inside internal class)
5. Inline Delegate (Pattern 7) not explicitly addressed in todo

### Contradictions

None found. The proposed fix does not contradict any governing constraint or behavioral contract.

### Recommendations for Architect

All four pipelines need independent fixes. Models need a new target type accessibility field. Standalone patterns need the generated Base class to match the user's stub class accessibility. Inline patterns need the stub class to match the target type's accessibility. The Transform phase already resolves `DeclaredAccessibility` at `KnockOffGenerator.Transform.cs:1201-1208` for containing types, and the same approach applies here.

---

## Business Rules (Testable Assertions)

### Standalone Patterns (1-4): Base Class Accessibility

For standalone patterns, the user controls the stub class accessibility via their partial class declaration. The generator produces a `{ClassName}Base` helper class that the user's stub extends. This Base class must match the user's stub class accessibility.

1. WHEN the user's standalone stub class is declared `public`, THEN the generated `{ClassName}Base` class is emitted with `public` accessibility. Expected: `public class MyStubBase` -- Source: Existing behavior (preserving backward compatibility)

2. WHEN the user's standalone stub class is declared `internal`, THEN the generated `{ClassName}Base` class is emitted with `internal` accessibility. Expected: `internal class MyStubBase` -- Source: NEW (bug fix)

3. WHEN the user's standalone stub class has no explicit accessibility modifier (defaults to `internal` in C#), THEN the generated `{ClassName}Base` class is emitted with `internal` accessibility. Expected: `internal class MyStubBase` -- Source: NEW (follows C# default semantics)

### Inline Patterns (5-9): Stub Class Accessibility

For inline patterns, the generator produces the entire stub class nested inside the `Stubs` container. The generated stub class's accessibility must be compatible with the target type it implements/extends.

4. WHEN an inline interface stub's target interface is `public`, THEN the generated stub class is emitted with `public` accessibility. Expected: `public class IServiceStub : global::IService` -- Source: Existing behavior (preserving backward compatibility)

5. WHEN an inline interface stub's target interface is `internal`, THEN the generated stub class is emitted with `internal` accessibility. Expected: `internal class IInternalServiceStub : global::IInternalService` -- Source: NEW (bug fix)

6. WHEN an inline class stub's target class is `public`, THEN the generated wrapper stub class is emitted with `public` accessibility. Expected: `public class ServiceBase : IKnockOffStub` -- Source: Existing behavior

7. WHEN an inline class stub's target class is `internal`, THEN the generated wrapper stub class is emitted with `internal` accessibility. Expected: `internal class InternalServiceBase : IKnockOffStub` -- Source: NEW (bug fix)

8. WHEN an inline delegate stub's target delegate is `public`, THEN the generated stub class is emitted with `public sealed class`. Expected: `public sealed class MyDelegate : IKnockOffStub` -- Source: Existing behavior

9. WHEN an inline delegate stub's target delegate is `internal`, THEN the generated stub class is emitted with `internal sealed class`. Expected: `internal sealed class InternalDelegate : IKnockOffStub` -- Source: NEW (bug fix)

### Regression: Public Types Unchanged

10. WHEN all existing Design.Stubs stubs reference public types, THEN ALL existing generated code remains unchanged (no accessibility modifier changes on any existing stub). Expected: Zero diff in existing generated code -- Source: Existing behavior (regression guard)

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Public standalone interface stub | `[KnockOff] public partial class Stub : IPublicService` | Rule 1 | Generated Base class: `public class StubBase` |
| 2 | Internal standalone interface stub | `[KnockOff] internal partial class Stub : IInternalService` | Rule 2 | Generated Base class: `internal class StubBase` |
| 3 | Public standalone class stub | `[KnockOffBase<PublicServiceBase>] public partial class Stub` | Rule 1 | Generated Base class: `public class StubBase` |
| 4 | Internal standalone class stub | `[KnockOffBase<InternalClassBase>] internal partial class Stub` | Rule 2 | Generated Base class: `internal class StubBase` |
| 5 | Public inline interface stub | `[KnockOff<IPublicService>]` on test class | Rule 4 | Generated stub: `public class IPublicService : ...` |
| 6 | Internal inline interface stub | `[KnockOff<IInternalService>]` on test class | Rule 5 | Generated stub: `internal class IInternalService : ...` |
| 7 | Public inline class stub | `[KnockOff<PublicServiceBase>]` on test class | Rule 6 | Generated stub: `public class PublicServiceBase : ...` |
| 8 | Internal inline class stub | `[KnockOff<InternalClassBase>]` on test class | Rule 7 | Generated stub: `internal class InternalClassBase : ...` |
| 9 | Public inline delegate stub | `[KnockOff<PublicDelegate>]` on test class | Rule 8 | Generated stub: `public sealed class PublicDelegate : ...` |
| 10 | Internal inline delegate stub | `[KnockOff<InternalDelegate>]` on test class | Rule 9 | Generated stub: `internal sealed class InternalDelegate : ...` |
| 11 | Regression: all existing stubs unchanged | Build all existing Design.Stubs | Rule 10 | Zero compilation errors, no generated code changes |

---

## Approach

The fix is a straightforward data flow addition across the four independent pipelines:

1. **Add accessibility to intermediate models** -- The Transform phase already knows how to resolve `DeclaredAccessibility` (see `KnockOffGenerator.Transform.cs:1201-1208`). Add the target type's accessibility to `InterfaceInfo`, `ClassStubInfo`, and `DelegateInfo`. For standalone patterns, add the user's stub class accessibility to `KnockOffTypeInfo` and `StandaloneClassStubInfo`.

2. **Flow accessibility through builders** -- Each builder (`FlatModelBuilder`, `StandaloneClassModelBuilder`, `InlineModelBuilder`, `ClassModelBuilder`) passes the accessibility string to the generation unit / stub model.

3. **Use accessibility in renderers** -- Each renderer replaces its hardcoded `public class` with the accessibility from the model.

### Accessibility String Format

Use the same format already established for `ContainingTypeInfo.AccessibilityModifier` and `ClassMemberInfo.AccessModifier`:

| Roslyn `DeclaredAccessibility` | String |
|---|---|
| `Public` | `"public"` |
| `Internal` | `"internal"` |
| `Protected` | `"protected"` |
| `ProtectedOrInternal` | `"protected internal"` |
| `ProtectedAndInternal` | `"private protected"` |
| `Private` | `"private"` |

### Standalone vs Inline: Different Accessibility Sources

| Pattern Group | Accessibility Source | Rationale |
|---|---|---|
| Standalone (1-4) | User's stub class `DeclaredAccessibility` | The user declares the stub class -- they control its accessibility. The generated Base class must match. |
| Inline (5-9) | Target type's `DeclaredAccessibility` | The generator creates the stub class -- it must be compatible with the target type. |

This distinction is important: a standalone stub for an `internal` interface can be `public` if the user has `InternalsVisibleTo` (cross-assembly scenario). The existing TroubleshootingSamples documentation at `src/Tests/KnockOff.Documentation.Samples/TroubleshootingSamples.cs:554-570` describes this valid use case. The fix must not break it.

---

## Design

### Phase 1: Model Changes

#### Intermediate Transform Models

**`InterfaceInfo`** (in `Models/InterfaceModels.cs`):
Add field: `string Accessibility = "public"` (default preserves backward compatibility)

**`ClassStubInfo`** (in `Models/ClassModels.cs`):
Add field: `string Accessibility = "public"`

**`DelegateInfo`** (in `Models/InlineStubModels.cs`):
Add field: `string Accessibility = "public"`

**`KnockOffTypeInfo`** (in `Models/CommonModels.cs`):
Add field: `string StubClassAccessibility = "public"` -- captures the user's stub class accessibility for standalone patterns.

**`StandaloneClassStubInfo`** (in `KnockOffGenerator.StandaloneClass.cs`):
Add field: `string StubClassAccessibility = "public"`

#### Generation Unit Models

**`FlatGenerationUnit`** (in `Model/Flat/FlatGenerationUnit.cs`):
Add field: `string BaseClassAccessibility = "public"` -- for the `{ClassName}Base` class.

**`StandaloneClassGenerationUnit`** (in `Model/StandaloneClass/StandaloneClassGenerationUnit.cs`):
Add field: `string BaseClassAccessibility = "public"` -- for the `{ClassName}Base` class.

**`InlineInterfaceStubModel`** (in `Model/Inline/InlineInterfaceStubModel.cs`):
Add field: `string Accessibility = "public"` -- for the stub class declaration.

**`InlineClassStubModel`** (in `Model/Inline/InlineClassStubModel.cs`):
Add field: `string Accessibility = "public"` -- for the wrapper stub class declaration.

**`InlineDelegateStubModel`** (in `Model/Inline/InlineDelegateStubModel.cs`):
Add field: `string Accessibility = "public"` -- for the delegate stub class declaration.

### Phase 2: Transform Changes

**`TransformClass`** (`KnockOffGenerator.Transform.cs:725`):
After resolving `classSymbol`, add:
```csharp
var stubClassAccessibility = classSymbol.DeclaredAccessibility switch
{
    Accessibility.Public => "public",
    Accessibility.Internal => "internal",
    Accessibility.Private => "private",
    Accessibility.Protected => "protected",
    Accessibility.ProtectedOrInternal => "protected internal",
    Accessibility.ProtectedAndInternal => "private protected",
    _ => "public"
};
```
Pass `StubClassAccessibility: stubClassAccessibility` to `KnockOffTypeInfo` constructor.

**`TransformStandaloneClassStub`** (`KnockOffGenerator.StandaloneClass.cs:21`):
Same approach -- resolve `classSymbol.DeclaredAccessibility` and pass to `StandaloneClassStubInfo`.

**`TransformInlineStubClass`** (`KnockOffGenerator.Transform.cs:20`):
For each target type (interface, class, delegate), resolve the target type's `DeclaredAccessibility`:
- In `ExtractInterfaceInfo`: resolve `iface.DeclaredAccessibility` and pass to `InterfaceInfo`.
- In `ClassStubInfo` construction (within `ExtractClassInfo`): resolve `classSource.DeclaredAccessibility` and pass to `ClassStubInfo`.
- In `DelegateInfo.Extract`: accept accessibility parameter from the caller (resolve `namedDelegate.DeclaredAccessibility` in `TransformInlineStubClass`).

### Phase 3: Builder Changes

**`FlatModelBuilder.Build`**: Pass `typeInfo.StubClassAccessibility` as `BaseClassAccessibility` on `FlatGenerationUnit`.

**`StandaloneClassModelBuilder.Build`**: Pass `info.StubClassAccessibility` as `BaseClassAccessibility` on `StandaloneClassGenerationUnit`.

**`InlineModelBuilder.BuildInterfaceStub`**: Pass `iface.Accessibility` to `InlineInterfaceStubModel`.

**`InlineModelBuilder.BuildClassStub`** (via `ClassModelBuilder.Build`): Pass `cls.Accessibility` to `InlineClassStubModel`.

**`InlineModelBuilder.BuildDelegateStub`**: Pass `del.Accessibility` to `InlineDelegateStubModel`.

### Phase 4: Renderer Changes

Four hardcoded locations, one per pipeline:

**`FlatRenderer.RenderBaseClass`** (line 268):
```csharp
// Before:
using (w.Block($"public class {unit.ClassName}Base{typeParams}{constraints}"))
// After:
using (w.Block($"{unit.BaseClassAccessibility} class {unit.ClassName}Base{typeParams}{constraints}"))
```

**`StandaloneClassRenderer.RenderBaseClass`** (line 273):
```csharp
// Before:
using (w.Block($"public class {unit.ClassName}Base{typeParams}{constraints}"))
// After:
using (w.Block($"{unit.BaseClassAccessibility} class {unit.ClassName}Base{typeParams}{constraints}"))
```

**`InlineRenderer`** (line 247):
```csharp
// Before:
w.Line($"\t\tpublic class {iface.StubClassName}{stubTypeParamList} : {iface.BaseType}, global::KnockOff.IKnockOffStub{stubConstraints}");
// After:
w.Line($"\t\t{iface.Accessibility} class {iface.StubClassName}{stubTypeParamList} : {iface.BaseType}, global::KnockOff.IKnockOffStub{stubConstraints}");
```

**`InlineRenderer.RenderDelegateStub`** (line 1326):
```csharp
// Before:
w.Line($"\t\tpublic sealed class {del.StubClassName}{del.TypeParameterList} : global::KnockOff.IKnockOffStub{del.ConstraintClauses}");
// After:
w.Line($"\t\t{del.Accessibility} sealed class {del.StubClassName}{del.TypeParameterList} : global::KnockOff.IKnockOffStub{del.ConstraintClauses}");
```

**`ClassRenderer`** (line 118):
```csharp
// Before:
w.Line($"{indent}public class {cls.StubClassName}{cls.TypeParameterList} : global::KnockOff.IKnockOffStub{cls.ConstraintClauses}");
// After:
w.Line($"{indent}{cls.Accessibility} class {cls.StubClassName}{cls.TypeParameterList} : global::KnockOff.IKnockOffStub{cls.ConstraintClauses}");
```

### Phase 5: Design.Stubs Verification

Create internal types and stubs in Design.Stubs to verify compilation. Since Design.Stubs and Design.Domain are separate assemblies, define internal types directly in Design.Stubs (the stubs and the internal types must be in the same assembly for the `internal` modifier to work without `InternalsVisibleTo`).

Create a new file `src/Design/Design.Stubs/Advanced/InternalAccessibility.cs` containing:

1. Internal interface with a method
2. Internal abstract class with a virtual method
3. Internal delegate type
4. Standalone stub (Pattern 1) for the internal interface, declared `internal`
5. Inline stubs (Patterns 5, 6, 7) for the internal types on a test class

This file serves as both the Design source of truth and the compiler-verified acceptance criteria. If it compiles after implementation, the feature works.

Generic standalone (Pattern 2), standalone class (Patterns 3, 4), and open generic (Patterns 8, 9) can also be verified in the same file.

---

## Implementation Steps

1. **Add accessibility fields to intermediate transform models** (`InterfaceInfo`, `ClassStubInfo`, `DelegateInfo`, `KnockOffTypeInfo`, `StandaloneClassStubInfo`) with default value `"public"` for backward compatibility.

2. **Update Transform methods** to resolve and pass accessibility:
   - `TransformClass`: resolve `classSymbol.DeclaredAccessibility`, pass to `KnockOffTypeInfo`
   - `TransformStandaloneClassStub`: resolve `classSymbol.DeclaredAccessibility`, pass to `StandaloneClassStubInfo`
   - `ExtractInterfaceInfo`: resolve `iface.DeclaredAccessibility`, pass to `InterfaceInfo`
   - `ExtractClassInfo` / `ClassStubInfo` construction: resolve target class `DeclaredAccessibility`
   - `DelegateInfo.Extract`: accept and store accessibility parameter

3. **Add accessibility fields to generation unit models** (`FlatGenerationUnit`, `StandaloneClassGenerationUnit`, `InlineInterfaceStubModel`, `InlineClassStubModel`, `InlineDelegateStubModel`) with default value `"public"`.

4. **Update builders** to flow accessibility from intermediate models to generation units:
   - `FlatModelBuilder`: pass `StubClassAccessibility` to `BaseClassAccessibility`
   - `StandaloneClassModelBuilder`: pass `StubClassAccessibility` to `BaseClassAccessibility`
   - `InlineModelBuilder`: pass target type `Accessibility` to stub models
   - `ClassModelBuilder`: pass target type `Accessibility` to `InlineClassStubModel`

5. **Update renderers** -- replace hardcoded `public class` at each of the 5 locations (FlatRenderer:268, StandaloneClassRenderer:273, InlineRenderer:247, InlineRenderer:1326, ClassRenderer:118) with the accessibility from the model.

6. **Create Design.Stubs verification code** in `src/Design/Design.Stubs/Advanced/InternalAccessibility.cs`:
   - Define internal types (interface, abstract class, delegate) in the same assembly
   - Create stubs for each applicable pattern
   - File must compile after implementation; if it fails to compile before implementation, the failing code IS the acceptance criteria

7. **Run all builds and tests** -- verify zero regressions.

---

## Acceptance Criteria

- [ ] Internal interface can be stubbed with inline pattern (Pattern 5) -- compiles and works
- [ ] Internal class can be stubbed with inline pattern (Pattern 6) -- compiles and works
- [ ] Internal delegate can be stubbed with inline pattern (Pattern 7) -- compiles and works
- [ ] Internal standalone interface stub (Pattern 1) with `internal partial class` -- Base class is `internal`
- [ ] Internal standalone class stub (Pattern 3) with `internal partial class` -- Base class is `internal`
- [ ] All existing stubs continue to compile (zero regression)
- [ ] All existing tests pass
- [ ] Design.Stubs internal accessibility examples compile
- [ ] Open generic patterns (8, 9) work with internal types

---

## Dependencies

- No external dependencies
- No library (`KnockOff.csproj`) changes required -- purely renderer/model/builder/transform changes in the Generator project
- No NuGet package changes

---

## Risks / Considerations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Default value regression -- adding new field with wrong default breaks existing equatability caching | Low | High | Use `= "public"` default on all new fields, matching existing behavior |
| Nested type accessibility interaction -- C# allows nested types to have any accessibility | Low | Low | Inline stubs are always nested in `Stubs` container inside user's class. The key constraint is implementing/inheriting the target type, not the nesting. |
| `InternalsVisibleTo` scenario -- standalone public stub for internal interface breaks | Low | Medium | The standalone fix uses the USER's class accessibility, not the target interface's. A `public` stub for an `internal` interface (with `InternalsVisibleTo`) continues to generate `public class StubBase`. |
| Generic patterns need same fix | Low | Medium | Same code paths -- `InterfaceInfo.Accessibility` flows through to `InlineInterfaceStubModel` regardless of `IsOpenGeneric` flag |

---

## Architectural Verification

**Scope Table:**

| Pattern | Fix Location | Accessibility Source | Status | Compiler Evidence |
|---|---|---|---|---|
| 1. Standalone | `FlatRenderer.cs:268` | User's stub class `DeclaredAccessibility` | Needs Implementation | `InternalServiceStub.Base.g.cs:7` emits `public class` (no compiler error but wrong accessibility) |
| 2. Generic Standalone | `FlatRenderer.cs:268` | User's stub class `DeclaredAccessibility` | Needs Implementation | Same code path as Pattern 1 |
| 3. Standalone Class | `StandaloneClassRenderer.cs:273` | User's stub class `DeclaredAccessibility` | Needs Implementation | `InternalClassStub.Base.g.cs:7` emits `public class` (no compiler error but wrong accessibility) |
| 4. Generic Standalone Class | `StandaloneClassRenderer.cs:273` | User's stub class `DeclaredAccessibility` | Needs Implementation | Same code path as Pattern 3 |
| 5. Inline Interface | `InlineRenderer.cs:247` | Target interface `DeclaredAccessibility` | Needs Implementation | CS0053 on `.Object`, CS0051 on `Source()` |
| 6. Inline Class | `ClassRenderer.cs:118` | Target class `DeclaredAccessibility` | Needs Implementation | CS0053 on `.Object` |
| 7. Inline Delegate | `InlineRenderer.cs:1326` | Target delegate `DeclaredAccessibility` | Needs Implementation | CS0056 on implicit operator |
| 8. Open Generic Interface | `InlineRenderer.cs:247` | Target interface `DeclaredAccessibility` | Needs Implementation | CS0053 on `.Object`, CS0051 on `Source()` |
| 9. Open Generic Class | `ClassRenderer.cs:118` | Target class `DeclaredAccessibility` | Needs Implementation | Same code path as Pattern 6 |

**Verification Evidence:**

Design.Stubs verification code created at `src/Design/Design.Stubs/Advanced/InternalAccessibility.cs`. Current build result: **18 errors (6 unique, x3 target frameworks)**, confirming the bug. The failing code IS the acceptance criteria -- implementation is complete when this file compiles.

Compilation errors observed (all are "inconsistent accessibility" errors -- `public` stub referencing `internal` target type):

- Pattern 5 (Inline Interface): CS0053 on `.Object` property, CS0051 on `Source()` method
- Pattern 6 (Inline Class): CS0053 on `.Object` property
- Pattern 7 (Inline Delegate): CS0056 on implicit operator return type
- Pattern 8 (Open Generic Interface): CS0053 on `.Object` property, CS0051 on `Source()` method

Standalone patterns (1, 3) compile without errors because C# allows a class to inherit from a more accessible base class. However, the generated `public class InternalServiceStubBase` is unnecessarily public and leaks to the assembly's public API. The fix correctly changes this to `internal` to match the user's intent.

Verification status per pattern:
- Pattern 1 (Standalone): Needs Implementation -- Base class emitted as `public` (file: `InternalServiceStub.Base.g.cs:7`)
- Pattern 2 (Generic Standalone): Not tested -- same code path as Pattern 1
- Pattern 3 (Standalone Class): Needs Implementation -- Base class emitted as `public` (file: `InternalClassStub.Base.g.cs:7`)
- Pattern 4 (Generic Standalone Class): Not tested -- same code path as Pattern 3
- Pattern 5 (Inline Interface): Needs Implementation -- CS0053/CS0051 errors
- Pattern 6 (Inline Class): Needs Implementation -- CS0053 error
- Pattern 7 (Inline Delegate): Needs Implementation -- CS0056 error
- Pattern 8 (Open Generic Interface): Needs Implementation -- CS0053/CS0051 errors
- Pattern 9 (Open Generic Class): Not tested -- same code path as Pattern 6

**Breaking Changes:** No. All new model fields have `= "public"` defaults, preserving existing behavior for all public types. No API surface changes. No library changes.

**Codebase Analysis:**

Files examined during design:
- `src/Generator/KnockOffGenerator.Transform.cs` -- Transform phase, `DeclaredAccessibility` resolution at lines 1201-1208
- `src/Generator/KnockOffGenerator.StandaloneClass.cs` -- StandaloneClass transform, `TransformStandaloneClassStub` method
- `src/Generator/Models/CommonModels.cs` -- `KnockOffTypeInfo`, `ContainingTypeInfo` records
- `src/Generator/Models/InterfaceModels.cs` -- `InterfaceInfo` record
- `src/Generator/Models/ClassModels.cs` -- `ClassStubInfo` record
- `src/Generator/Models/InlineStubModels.cs` -- `InlineStubClassInfo`, `DelegateInfo` records
- `src/Generator/Model/Flat/FlatGenerationUnit.cs` -- FlatGenerationUnit record
- `src/Generator/Model/StandaloneClass/StandaloneClassGenerationUnit.cs` -- StandaloneClassGenerationUnit record
- `src/Generator/Model/Inline/InlineInterfaceStubModel.cs` -- InlineInterfaceStubModel record
- `src/Generator/Model/Inline/InlineClassStubModel.cs` -- InlineClassStubModel record
- `src/Generator/Model/Inline/InlineDelegateStubModel.cs` -- InlineDelegateStubModel record
- `src/Generator/Model/Inline/InlineGenerationUnit.cs` -- InlineGenerationUnit record
- `src/Generator/Builder/FlatModelBuilder.cs` -- Build method, line 23
- `src/Generator/Builder/InlineModelBuilder.cs` -- Build method, BuildInterfaceStub, BuildDelegateStub
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- Build method
- `src/Generator/Builder/ClassModelBuilder.cs` -- Build method for InlineClassStubModel
- `src/Generator/Renderer/FlatRenderer.cs` -- `RenderBaseClass`, hardcoded `public class` at line 268
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- `RenderBaseClass`, hardcoded `public class` at line 273
- `src/Generator/Renderer/InlineRenderer.cs` -- Hardcoded `public class` at line 247, delegate at line 1326
- `src/Generator/Renderer/ClassRenderer.cs` -- Hardcoded `public class` at line 118
- `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` -- All 9 patterns reference
- `src/Design/Design.Stubs/Design.Stubs.csproj` -- Project references
- `src/Design/Design.Domain/Design.Domain.csproj` -- Separate assembly (no InternalsVisibleTo to Design.Stubs)

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Model + Transform + Builder + Renderer | developer | Yes | All generator changes are tightly coupled -- model fields, transform resolution, builder plumbing, renderer emission. Doing them together prevents partial states. | None |
| Phase 2: Design.Stubs verification | developer | No | Same agent continues to write Design.Stubs code and verify compilation. | Phase 1 |
| Phase 3: Final build and test | developer | No | Same agent runs final validation. | Phase 2 |

**Parallelizable phases:** None -- all phases are sequential.

**Notes:** This is a small, focused change (add one field per model, update 5 renderer lines). A single developer agent should handle all phases in one session.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-07

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `TransformClass`: `classSymbol.DeclaredAccessibility == Public` -> `"public"` -> `KnockOffTypeInfo.StubClassAccessibility` -> `FlatModelBuilder.Build` copies to `FlatGenerationUnit.BaseClassAccessibility` -> `FlatRenderer.RenderBaseClass` emits `{unit.BaseClassAccessibility} class {unit.ClassName}Base` | `public class MyStubBase` | Yes | Default `"public"` preserves existing behavior |
| 2 | `TransformClass`: `classSymbol.DeclaredAccessibility == Internal` -> `"internal"` -> same flow as Rule 1 | `internal class MyStubBase` | Yes | Switch case `Accessibility.Internal => "internal"` |
| 3 | `TransformClass`: `classSymbol.DeclaredAccessibility == Internal` (C# default for no modifier) -> `"internal"` -> same flow | `internal class MyStubBase` | Yes | Roslyn reports no-modifier classes as `Accessibility.Internal` |
| 4 | `ExtractInterfaceInfo`: `iface.DeclaredAccessibility == Public` -> `"public"` -> `InterfaceInfo.Accessibility` -> `InlineModelBuilder.BuildInterfaceStub` copies to `InlineInterfaceStubModel.Accessibility` -> `InlineRenderer` emits `{iface.Accessibility} class {iface.StubClassName}` | `public class IServiceStub` | Yes | Default `"public"` preserves existing |
| 5 | `ExtractInterfaceInfo`: `iface.DeclaredAccessibility == Internal` -> `"internal"` -> same flow as Rule 4 | `internal class IInternalServiceStub` | Yes | Switch resolves to `"internal"` |
| 6 | `ExtractClassInfo`: `classSource.DeclaredAccessibility == Public` -> `"public"` -> `ClassStubInfo.Accessibility` -> `ClassModelBuilder.Build` copies to `InlineClassStubModel.Accessibility` -> `ClassRenderer` emits `{cls.Accessibility} class {cls.StubClassName}` | `public class ServiceBase` | Yes | Default `"public"` preserves existing |
| 7 | Same as Rule 6 but `classSource.DeclaredAccessibility == Internal` -> `"internal"` | `internal class InternalServiceBase` | Yes | |
| 8 | `TransformInlineStubClass`: `namedDelegate.DeclaredAccessibility == Public` -> `"public"` passed to `DelegateInfo.Extract` -> `DelegateInfo.Accessibility` -> `InlineModelBuilder.BuildDelegateStub` copies to `InlineDelegateStubModel.Accessibility` -> `InlineRenderer.RenderDelegateStub` emits `{del.Accessibility} sealed class` | `public sealed class MyDelegate` | Yes | |
| 9 | Same as Rule 8 but `namedDelegate.DeclaredAccessibility == Internal` -> `"internal"` | `internal sealed class InternalDelegate` | Yes | |
| 10 | All new fields default to `"public"`. Existing types are all public. `DeclaredAccessibility` for public types returns `Public` -> `"public"`. Same string as hardcoded before. | Zero diff | Yes | Default value backward compatibility |

### Observations

1. **Patterns 2, 4, 9 not in Design.Stubs:** The plan notes these share code paths with Patterns 1, 3, 6. Verified: Pattern 2 (Generic Standalone) uses same `FlatRenderer.cs:268`, Pattern 4 (Generic Standalone Class) uses same `StandaloneClassRenderer.cs:273`, Pattern 9 (Open Generic Class) uses same `ClassRenderer.cs:118`. Informational only -- not blocking.

2. **All assertion traces verified:** Every rule's implementation path cites a specific method name, switch condition, model field, and renderer emission point. No gaps found.

### Why This Plan Is Exceptionally Clear

This is a straightforward bug fix with well-defined scope: add one string field per model type, resolve it in transform using the existing `DeclaredAccessibility` switch pattern (already at `Transform.cs:1201-1208`), flow through builder, use in renderer. Every file, line number, and code change is specified. The Design.Stubs acceptance criteria are compiler-verified -- 18 errors that must go to zero. Zero ambiguity about "done."

### Review Summary

- Files examined: 20+ source files across all 4 pipelines (renderers, models, builders, transforms)
- Questions checked: 16 of 16 (completeness, correctness, clarity, risk)
- Devil's advocate items: 3 edge cases (file-scoped types, `_` default, protected nested), 1 breakage scenario (none found), 1 API confusion (InternalsVisibleTo -- addressed in plan)

---

## Implementation Contract

**Created:** 2026-03-07
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

- [ ] `src/Design/Design.Stubs/Advanced/InternalAccessibility.cs` -- 18 compiler errors (6 unique x 3 target frameworks) must resolve to zero

### In Scope

- [ ] Add `Accessibility` field to `InterfaceInfo` (default `"public"`)
- [ ] Add `Accessibility` field to `ClassStubInfo` (default `"public"`)
- [ ] Add `Accessibility` field to `DelegateInfo` (default `"public"`)
- [ ] Add `StubClassAccessibility` field to `KnockOffTypeInfo` (default `"public"`)
- [ ] Add `StubClassAccessibility` field to `StandaloneClassStubInfo` (default `"public"`)
- [ ] Add `BaseClassAccessibility` field to `FlatGenerationUnit` (default `"public"`)
- [ ] Add `BaseClassAccessibility` field to `StandaloneClassGenerationUnit` (default `"public"`)
- [ ] Add `Accessibility` field to `InlineInterfaceStubModel` (default `"public"`)
- [ ] Add `Accessibility` field to `InlineClassStubModel` (default `"public"`)
- [ ] Add `Accessibility` field to `InlineDelegateStubModel` (default `"public"`)
- [ ] Update `TransformClass` to resolve `classSymbol.DeclaredAccessibility` and pass to `KnockOffTypeInfo.StubClassAccessibility`
- [ ] Update `TransformStandaloneClassStub` to resolve `classSymbol.DeclaredAccessibility` and pass to `StandaloneClassStubInfo.StubClassAccessibility`
- [ ] Update `ExtractInterfaceInfo` to resolve `iface.DeclaredAccessibility` and pass to `InterfaceInfo.Accessibility`
- [ ] Update `ExtractClassInfo` to resolve `classSource.DeclaredAccessibility` and pass to `ClassStubInfo.Accessibility`
- [ ] Update `DelegateInfo.Extract` to accept and store accessibility
- [ ] Update `TransformInlineStubClass` to pass `namedDelegate.DeclaredAccessibility` to `DelegateInfo.Extract`
- [ ] Update `FlatModelBuilder.Build` to flow `typeInfo.StubClassAccessibility` to `FlatGenerationUnit.BaseClassAccessibility`
- [ ] Update `StandaloneClassModelBuilder.Build` to flow `info.StubClassAccessibility` to `StandaloneClassGenerationUnit.BaseClassAccessibility`
- [ ] Update `InlineModelBuilder.BuildInterfaceStub` to flow `iface.Accessibility` to `InlineInterfaceStubModel.Accessibility`
- [ ] Update `ClassModelBuilder.Build` to flow `cls.Accessibility` to `InlineClassStubModel.Accessibility`
- [ ] Update `InlineModelBuilder.BuildDelegateStub` to flow `del.Accessibility` to `InlineDelegateStubModel.Accessibility`
- [ ] Update `FlatRenderer.RenderBaseClass` at line 268: replace `public` with `{unit.BaseClassAccessibility}`
- [ ] Update `StandaloneClassRenderer.RenderBaseClass` at line 273: replace `public` with `{unit.BaseClassAccessibility}`
- [ ] Update `InlineRenderer` at line 247: replace `public` with `{iface.Accessibility}`
- [ ] Update `InlineRenderer.RenderDelegateStub` at line 1326: replace `public` with `{del.Accessibility}`
- [ ] Update `ClassRenderer` at line 118: replace `public` with `{cls.Accessibility}`
- [ ] Checkpoint: All existing tests pass (`dotnet test src/KnockOff.sln`)
- [ ] Checkpoint: `dotnet build src/Design/Design.Stubs` succeeds (18 errors resolved to zero)

### Explicitly Out of Scope

- Library changes (`KnockOff.csproj`) -- not needed
- New diagnostic rules for accessibility mismatches
- `protected internal` / `private protected` target type scenarios (only `public` and `internal` are practical for top-level types)
- Cross-assembly `InternalsVisibleTo` scenarios (already works for standalone patterns per existing documentation)
- Adding Patterns 2, 4, 9 to Design.Stubs (same code paths as 1, 3, 6)

### Verification Gates

1. After all generator changes (model + transform + builder + renderer): All existing tests pass
2. After Design.Stubs: `dotnet build src/Design/Design.Stubs` succeeds with zero errors
3. Final: `dotnet test src/KnockOff.sln` -- all tests pass

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure
- Architectural contradiction discovered
- Equatability/caching issues from new model fields
- Generated code does not compile for existing public types

---

## Implementation Progress

**Started:** 2026-03-07
**Developer:** knockoff-developer

**Milestone 1:** Generator Changes (Model + Transform + Builder + Renderer)
- [x] Add accessibility fields to intermediate models (5 fields: `InterfaceInfo.Accessibility`, `ClassStubInfo.Accessibility`, `DelegateInfo.Accessibility`, `KnockOffTypeInfo.StubClassAccessibility`, `StandaloneClassStubInfo.StubClassAccessibility`)
- [x] Update transform methods to resolve DeclaredAccessibility (`TransformClass`, `TransformStandaloneClassStub`, `ExtractInterfaceInfo`, `ExtractClassInfo`, `TransformInlineStubClass` for delegates)
- [x] Add accessibility fields to generation unit models (5 fields: `FlatGenerationUnit.BaseClassAccessibility`, `StandaloneClassGenerationUnit.BaseClassAccessibility`, `InlineInterfaceStubModel.Accessibility`, `InlineClassStubModel.Accessibility`, `InlineDelegateStubModel.Accessibility`)
- [x] Update builders to flow accessibility (`FlatModelBuilder.Build`, `StandaloneClassModelBuilder.Build`, `InlineModelBuilder.BuildInterfaceStub`, `ClassModelBuilder.Build`, `InlineModelBuilder.BuildDelegateStub`)
- [x] Update 5 renderer locations (`FlatRenderer.cs:268`, `StandaloneClassRenderer.cs:273`, `InlineRenderer.cs:247`, `InlineRenderer.cs:1326`, `ClassRenderer.cs:118`)
- [x] **Verification**: `dotnet test src/KnockOff.sln` -- all existing tests pass (8,114 passed, 0 failed, 12 skipped pre-existing)

**Milestone 2:** Design.Stubs Verification
- [x] `InternalAccessibility.cs` already existed with internal types and stubs (created by architect during design). Added `#pragma warning disable CA1812, CA1852` for code analysis warnings on internal standalone stubs.
- [x] **Verification**: `dotnet build src/Design/Design.Stubs` succeeds (0 errors, 0 warnings, all 3 TFMs)

---

## Completion Evidence

**Reported:** 2026-03-07

- **Tests Passing:** All 8,114 tests pass across 12 test suites (4 projects x 3 target frameworks). Zero failures. 12 skipped (pre-existing BugRegressionTests).
  - KnockOffTests: 1,514/1,515 passed x 3 TFMs (net8.0/net9.0/net10.0)
  - Documentation.Samples: 703 passed x 3 TFMs
  - NeatooInterfaceTests: 473 passed x 3 TFMs
  - AssemblyStrict: 14 passed x 3 TFMs

- **Verification Resources Pass:** Yes. `dotnet build src/Design/Design.Stubs` succeeds with 0 errors, 0 warnings across all 3 target frameworks. The 18 original compiler errors (CS0053, CS0051, CS0056 -- inconsistent accessibility) are all resolved.

- **All Contract Items:** Confirmed 100% complete (28/28 checklist items).

### Files Modified

**Intermediate transform models (5 fields added):**
- `src/Generator/Models/InterfaceModels.cs` -- Added `Accessibility = "public"` field
- `src/Generator/Models/ClassModels.cs` -- Added `Accessibility = "public"` field
- `src/Generator/Models/InlineStubModels.cs` -- Added `Accessibility = "public"` field to `DelegateInfo`, updated `DelegateInfo.Extract` to accept `accessibility` parameter
- `src/Generator/Models/CommonModels.cs` -- Added `StubClassAccessibility = "public"` field to `KnockOffTypeInfo`
- `src/Generator/KnockOffGenerator.StandaloneClass.cs` -- Added `StubClassAccessibility = "public"` field to `StandaloneClassStubInfo`, added accessibility resolution in `TransformStandaloneClassStub`

**Generation unit models (5 fields added):**
- `src/Generator/Model/Flat/FlatGenerationUnit.cs` -- Added `BaseClassAccessibility = "public"`
- `src/Generator/Model/StandaloneClass/StandaloneClassGenerationUnit.cs` -- Added `BaseClassAccessibility = "public"`
- `src/Generator/Model/Inline/InlineInterfaceStubModel.cs` -- Added `Accessibility = "public"`
- `src/Generator/Model/Inline/InlineClassStubModel.cs` -- Added `Accessibility = "public"`
- `src/Generator/Model/Inline/InlineDelegateStubModel.cs` -- Added `Accessibility = "public"`

**Transform methods (3 files, 5 resolution points):**
- `src/Generator/KnockOffGenerator.Transform.cs` -- Added `stubClassAccessibility` resolution in `TransformClass`, `ifaceAccessibility` resolution in `ExtractInterfaceInfo`, `classAccessibility` resolution in `ExtractClassInfo`, `delegateAccessibility` resolution in `TransformInlineStubClass`
- `src/Generator/KnockOffGenerator.StandaloneClass.cs` -- Added `stubClassAccessibility` resolution in `TransformStandaloneClassStub`

**Builders (4 files):**
- `src/Generator/Builder/FlatModelBuilder.cs` -- Passes `StubClassAccessibility` to `BaseClassAccessibility`
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- Passes `StubClassAccessibility` to `BaseClassAccessibility`
- `src/Generator/Builder/InlineModelBuilder.cs` -- Passes `iface.Accessibility` and `del.Accessibility` to stub models
- `src/Generator/Builder/ClassModelBuilder.cs` -- Passes `cls.Accessibility` to `InlineClassStubModel`

**Renderers (4 files, 5 locations):**
- `src/Generator/Renderer/FlatRenderer.cs` -- `{unit.BaseClassAccessibility}` replaces hardcoded `public`
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- `{unit.BaseClassAccessibility}` replaces hardcoded `public`
- `src/Generator/Renderer/InlineRenderer.cs` -- `{iface.Accessibility}` and `{del.Accessibility}` replace hardcoded `public` (2 locations)
- `src/Generator/Renderer/ClassRenderer.cs` -- `{cls.Accessibility}` replaces hardcoded `public`

**Design.Stubs:**
- `src/Design/Design.Stubs/Advanced/InternalAccessibility.cs` -- Added `#pragma warning disable CA1812, CA1852` for code analysis on internal standalone stubs

### Generated Code Samples

After implementation, the generated code for the internal types in `InternalAccessibility.cs` correctly uses `internal` accessibility:

- Pattern 1 (Standalone): `InternalServiceStub.Base.g.cs` now emits `internal class InternalServiceStubBase` instead of `public class InternalServiceStubBase`
- Pattern 3 (Standalone Class): `InternalClassStub.Base.g.cs` now emits `internal class InternalClassStubBase` instead of `public class InternalClassStubBase`
- Pattern 5 (Inline Interface): Inline stub class emits `internal class IInternalService` instead of `public class IInternalService`
- Pattern 6 (Inline Class): Wrapper stub class emits `internal class InternalClassBase` instead of `public class InternalClassBase`
- Pattern 7 (Inline Delegate): Delegate stub class emits `internal sealed class InternalOperation` instead of `public sealed class InternalOperation`
- Pattern 8 (Open Generic Interface): Inline stub emits `internal class IGenericInternalService` instead of `public class IGenericInternalService`
- All existing public type stubs continue to emit `public class` (zero regression)

---

## Documentation

**Completed:** 2026-03-07
**Documenter:** knockoff-requirements-documenter

### Files Updated

- `docs/guides/api-consistency-matrix.md` -- Added Feature 13: Type Accessibility section documenting how generated stub class accessibility matches the target type across all 9 patterns, including the standalone vs inline accessibility source distinction. Added row to Summary table.
- `docs/guides/stub-patterns.md` -- Added callout note about internal type support with link to the API Consistency Matrix feature section.

### Developer Deliverables

No Developer Deliverables -- documentation is complete. The existing Design.Stubs file (`src/Design/Design.Stubs/Advanced/InternalAccessibility.cs`) serves as the behavioral contract. No new Documentation.Samples are needed because the feature is a generator-level accessibility fix with no new user-facing API surface to demonstrate in snippet form.

### Discrepancies Found

None. All 10 assertions in the plan's Business Rules section are backed by Design.Stubs code in `src/Design/Design.Stubs/Advanced/InternalAccessibility.cs` (Patterns 1, 3, 5, 6, 7, 8) or confirmed via shared code paths (Patterns 2, 4, 9). Rule 10 (regression guard) is confirmed by the 8,114 passing tests with zero generated code changes for existing public types.

---

## Architect Verification

**Verified:** 2026-03-07
**Verdict:** VERIFIED

### Independent Build Results

- `dotnet build src/KnockOff.sln` -- 0 errors, 0 warnings
- `dotnet build src/Design/Design.Stubs` -- 0 errors, 0 warnings (all 3 TFMs: net8.0, net9.0, net10.0)

### Independent Test Results

| Test Suite | net8.0 | net9.0 | net10.0 |
|---|---|---|---|
| KnockOffTests | 1,514 passed, 4 skipped | 1,515 passed, 4 skipped | 1,515 passed, 4 skipped |
| Documentation.Samples | 703 passed | 703 passed | 703 passed |
| NeatooInterfaceTests | 473 passed | 473 passed | 473 passed |
| AssemblyStrict | 14 passed | 14 passed | 14 passed |
| Design.Tests | 370 passed | 370 passed | 370 passed |

**Total: 8,114 passed, 0 failed, 12 skipped** (skips are pre-existing BugRegressionTests). Design.Tests add 1,110 more passing tests (370 x 3 TFMs).

### Design Match

The implementation matches the original plan exactly across all four pipelines:

**Renderer changes (5 locations):**
- `FlatRenderer.cs:268` -- Uses `{unit.BaseClassAccessibility}` instead of hardcoded `public`. Matches plan.
- `StandaloneClassRenderer.cs:273` -- Uses `{unit.BaseClassAccessibility}` instead of hardcoded `public`. Matches plan.
- `InlineRenderer.cs:247` -- Uses `{iface.Accessibility}` instead of hardcoded `public`. Matches plan.
- `InlineRenderer.cs:1326` -- Uses `{del.Accessibility}` instead of hardcoded `public`. Matches plan.
- `ClassRenderer.cs:118` -- Uses `{cls.Accessibility}` instead of hardcoded `public`. Matches plan.

**Intermediate transform models (5 fields):**
- `InterfaceInfo.Accessibility = "public"` -- Verified in InterfaceModels.cs:54
- `ClassStubInfo.Accessibility = "public"` -- Verified in ClassModels.cs:43
- `DelegateInfo.Accessibility = "public"` -- Verified in InlineStubModels.cs:54
- `KnockOffTypeInfo.StubClassAccessibility = "public"` -- Verified in CommonModels.cs:53
- `StandaloneClassStubInfo.StubClassAccessibility = "public"` -- Verified in KnockOffGenerator.StandaloneClass.cs:264

**Generation unit models (5 fields):**
- `FlatGenerationUnit.BaseClassAccessibility = "public"` -- Verified in FlatGenerationUnit.cs:34
- `StandaloneClassGenerationUnit.BaseClassAccessibility = "public"` -- Verified in StandaloneClassGenerationUnit.cs:66
- `InlineInterfaceStubModel.Accessibility = "public"` -- Verified in InlineInterfaceStubModel.cs:43
- `InlineClassStubModel.Accessibility = "public"` -- Verified in InlineClassStubModel.cs:61
- `InlineDelegateStubModel.Accessibility = "public"` -- Verified in InlineDelegateStubModel.cs:44

**Transform resolution (5 resolution points):**
- `TransformClass` -- `classSymbol.DeclaredAccessibility` switch at Transform.cs:773, passed to `KnockOffTypeInfo` at line 1088
- `TransformStandaloneClassStub` -- `classSymbol.DeclaredAccessibility` switch at StandaloneClass.cs:30, passed to `StandaloneClassStubInfo` at line 202
- `ExtractInterfaceInfo` -- `iface.DeclaredAccessibility` switch at Transform.cs:410, passed to `InterfaceInfo` at line 433
- `ExtractClassInfo` -- `classSource.DeclaredAccessibility` switch at Transform.cs:641, passed to `ClassStubInfo` at line 662
- `TransformInlineStubClass` (delegates) -- `namedDelegate.DeclaredAccessibility` switch at Transform.cs:143, passed to `DelegateInfo.Extract` at line 154

**Builder flow (4 builders):**
- `FlatModelBuilder.Build` -- `typeInfo.StubClassAccessibility` flows to `FlatGenerationUnit.BaseClassAccessibility` at line 90
- `StandaloneClassModelBuilder.Build` -- `info.StubClassAccessibility` flows to `StandaloneClassGenerationUnit.BaseClassAccessibility` at line 350
- `InlineModelBuilder.BuildInterfaceStub` -- `iface.Accessibility` flows to `InlineInterfaceStubModel.Accessibility` at line 232
- `InlineModelBuilder.BuildDelegateStub` -- `del.Accessibility` flows to `InlineDelegateStubModel.Accessibility` at line 1252
- `ClassModelBuilder.Build` -- `cls.Accessibility` flows to `InlineClassStubModel.Accessibility` at line 280

**Design.Stubs acceptance code:**
- `src/Design/Design.Stubs/Advanced/InternalAccessibility.cs` compiles cleanly. Covers Patterns 1, 3, 5, 6, 7, and 8 with internal target types. The 18 original compiler errors (6 unique x 3 TFMs) are all resolved.

### Issues Found

None.

---

## Requirements Verification

**Reviewer:** knockoff-requirements-reviewer
**Verified:** 2026-03-07
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Interceptor-as-Property (CLAUDE.md) | Satisfied | No interceptor API changes. All modified files are models, transforms, builders, and renderers. The library project (`src/KnockOff/`) has zero changes. `stub.Method` remains a property returning an interceptor object. |
| API Consistency Principle (CLAUDE.md) | Satisfied | All four pipelines independently fixed: `FlatRenderer.cs:268`, `StandaloneClassRenderer.cs:273`, `InlineRenderer.cs:247`, `InlineRenderer.cs:1326`, `ClassRenderer.cs:118`. Each uses its model's accessibility field. Design.Stubs verifies Patterns 1, 3, 5, 6, 7, 8 with internal types. |
| Pipeline Verification Rule (CLAUDE.md) | Satisfied | Each pipeline has independent model field, transform resolution, builder flow, and renderer emission. Traced all 5 resolution points: `Transform.cs:773` (standalone interface), `StandaloneClass.cs:30` (standalone class), `Transform.cs:410` (interface), `Transform.cs:641` (class), `Transform.cs:143` (delegate). No shared code modified. |
| Nine Patterns (CLAUDE.md) | Satisfied | All 9 patterns addressed. Patterns 1, 3 via standalone accessibility (user's class). Patterns 5, 8 via `InlineRenderer.cs:247`. Patterns 6, 9 via `ClassRenderer.cs:118`. Pattern 7 via `InlineRenderer.cs:1326`. Patterns 2, 4 share code paths with 1, 3. Design.Stubs covers Patterns 1, 3, 5, 6, 7, 8 with internal types. |
| Four Member Types (CLAUDE.md) | Satisfied | Not directly affected -- this change modifies class-level accessibility declarations, not member interception. Design.Stubs `InternalAccessibility.cs` exercises methods, properties, and delegates to confirm all member types continue working with internal accessibility. |
| Design Projects as Source of Truth (CLAUDE.md) | Satisfied | `src/Design/Design.Stubs/Advanced/InternalAccessibility.cs` establishes the behavioral contract for internal accessibility across 6 patterns. File compiles cleanly (0 errors, 0 warnings). |
| BR-1: Public standalone -> public Base | Satisfied | `TransformClass` switch: `Accessibility.Public => "public"` at `Transform.cs:775`. Default `"public"` on `FlatGenerationUnit.BaseClassAccessibility`. All existing public stubs unchanged. |
| BR-2: Internal standalone -> internal Base | Satisfied | `TransformClass` switch: `Accessibility.Internal => "internal"` at `Transform.cs:776`. `InternalServiceStub.Base.g.cs` now emits `internal class InternalServiceStubBase`. |
| BR-4: Public inline interface -> public stub | Satisfied | `ExtractInterfaceInfo` switch: `Accessibility.Public => "public"` at `Transform.cs:412`. Default `"public"` on `InlineInterfaceStubModel.Accessibility`. |
| BR-5: Internal inline interface -> internal stub | Satisfied | `ExtractInterfaceInfo` switch: `Accessibility.Internal => "internal"` at `Transform.cs:413`. Resolves CS0053/CS0051 errors for `IInternalService`. |
| BR-6: Public inline class -> public stub | Satisfied | `ExtractClassInfo` switch: `Accessibility.Public => "public"` at `Transform.cs:643`. Default `"public"` on `InlineClassStubModel.Accessibility`. |
| BR-7: Internal inline class -> internal stub | Satisfied | `ExtractClassInfo` switch: `Accessibility.Internal => "internal"` at `Transform.cs:644`. Resolves CS0053 error for `InternalClassBase`. |
| BR-8: Public inline delegate -> public sealed | Satisfied | `TransformInlineStubClass` switch: `Accessibility.Public => "public"` at `Transform.cs:144`. Default `"public"` on `InlineDelegateStubModel.Accessibility`. |
| BR-9: Internal inline delegate -> internal sealed | Satisfied | `TransformInlineStubClass` switch: `Accessibility.Internal => "internal"` at `Transform.cs:145`. Resolves CS0056 error for `InternalOperation`. |
| BR-10: Regression -- public types unchanged | Satisfied | All new fields default to `"public"`, matching prior hardcoded behavior. 8,114 tests pass (0 failures). Design.Tests 1,110 pass (370 x 3 TFMs). |

### Unintended Side Effects

None found.

- **Library base classes:** Zero changes to `src/KnockOff/` (library project). `MethodInterceptorRuntime`, `PropertyGetSetInterceptor`, and all other base classes are unaffected.
- **Shared builder infrastructure:** `UnifiedInterceptorBuilder` has zero accessibility-related changes. Verified by grep.
- **Generated code structure:** Only the class declaration accessibility modifier changed. No changes to interceptor class structure, member implementations, or wiring.
- **API surface:** No user-facing API changes. The `Accessibility` fields on models are internal to the generator. Users interact with stubs identically.
- **Equatability/caching:** All new fields have `= "public"` defaults on record types, which participate in equatability automatically. No risk of cache invalidation for existing public types since the resolved value matches the previous hardcoded value.
- **InternalsVisibleTo scenario:** Standalone patterns derive accessibility from the user's stub class declaration (not the target type). A `public partial class MyStub : IInternalService` with `InternalsVisibleTo` continues to generate `public class MyStubBase`. Not broken.

### Issues Found

None.
