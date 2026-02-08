# Rename "User Methods" to "Stub Overrides" - Implementation Plan

**Date:** 2026-02-08
**Related Todo:** [Rename "User Methods" to "Stub Overrides"](../todos/rename-user-methods-to-stub-overrides.md)
**Status:** Complete
**Last Updated:** 2026-02-08

---

## Overview

Mechanical rename of "user methods" to "stub overrides" across the entire KnockOff codebase. No behavioral changes. The rename covers code identifiers, file names, folder names, documentation, skills, and tests.

### Naming Convention Mapping

| Old | New | Context |
|-----|-----|---------|
| `UserMethod` | `StubOverride` | PascalCase identifiers |
| `userMethod` | `stubOverride` | camelCase variables/parameters |
| `user method` | `stub override` | Prose/comments |
| `User Method` | `Stub Override` | Title case headings |
| `user-methods` | `stub-overrides` | kebab-case (files, snippets, URLs) |
| `UserMethods` | `StubOverrides` | Plural PascalCase |
| `user methods` | `stub overrides` | Plural prose |
| `__UserMethod_` | `__StubOverride_` | Generated internal forwarder prefix |
| `HasUserOverride` | `HasStubOverride` | Model property (see note) |
| `UserMethodFallback` | `StubOverrideFallback` | Model/renderer property |
| `UserMethodName` | `StubOverrideName` | Model property |
| `UserProperty` | `StubOverrideProperty` | PascalCase identifiers (properties domain) |
| `UserProperties` | `StubOverrideProperties` | Plural PascalCase (folders, namespaces) |
| `user property` | `stub override property` | Prose/comments |
| `user properties` | `stub override properties` | Plural prose |
| `User Properties` | `Stub Override Properties` | Title case headings |
| `user-properties` | `stub-override-properties` | kebab-case (snippet names, URLs) |
| `user-property` | `stub-override-property` | kebab-case (snippet names) |
| `UserOverrideMethods` | `StubOverrideMethods` | Record property (CommonModels, Transform, StandaloneClass) |
| `UserOverrideProperties` | `StubOverrideProperties` | Record property (CommonModels, Transform, StandaloneClass) |
| `userOverrideMethods` | `stubOverrideMethods` | camelCase local variables |
| `userOverrideProperties` | `stubOverrideProperties` | camelCase local variables |
| `DetectUserOverrideMethods` | `DetectStubOverrideMethods` | Helper method |
| `DetectUserOverrideProperties` | `DetectStubOverrideProperties` | Helper method |
| `HasMatchingUserOverride` | `HasMatchingStubOverride` | Builder method |
| `withUserOverride` | `withStubOverride` | Local variable in FlatModelBuilder |
| `withoutUserOverride` | `withoutStubOverride` | Local variable in FlatModelBuilder |
| `userOverrideName` | `stubOverrideName` | Local variable in FlatModelBuilder |
| `methodsWithUserOverride` | `methodsWithStubOverride` | Local variable in StandaloneClassRenderer |
| `RenderPropertyUserOverrideImplementation` | `RenderPropertyStubOverrideImplementation` | Renderer method |
| `hasUserOverride` | `hasStubOverride` | Local variable / parameter in builders |

**Note on `HasUserOverride` / `user override`:** The term "user override" appears extensively in comments and code alongside "user method." Both refer to the same feature. "User override" should become "stub override" for consistency. The property `HasUserOverride` becomes `HasStubOverride`.

---

## Open Question: User Properties -- RESOLVED

**Decision:** Yes, rename both. "User properties" follows the same pattern as "user methods" and should also become "stub overrides" for consistency. The plan below covers both methods and properties under the unified "stub override" terminology.

---

## Approach

This is a bottom-up rename:
1. **Generator code** first (models, builders, renderers) -- these are the foundation
2. **Library code** -- if any references exist
3. **Design projects** -- interfaces, stubs, tests
4. **Test projects** -- all test identifiers
5. **Documentation** -- guides, references, plans, todos
6. **Skills** -- skill files and references
7. **Agent files** -- architect and developer agent guidance
8. **File and folder renames** -- last, after content is updated

Each phase must compile and pass tests before proceeding to the next.

---

## Phase 1: Generator Code -- Models

### 1.1 `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs`

| Line | Old | New |
|------|-----|-----|
| 57 | `/// User method name for fallback (e.g., "Process_"). Null if no user override exists.` | `/// Stub override name for fallback (e.g., "Process_"). Null if no stub override exists.` |
| 60 | `string? UserMethodName,` | `string? StubOverrideName,` |
| 100 | `bool UserMethodFallback = false,` | `bool StubOverrideFallback = false,` |
| 103 | `/// When set along with UserMethodFallback, Invoke() takes a stub parameter to call user methods.` | `/// When set along with StubOverrideFallback, Invoke() takes a stub parameter to call stub overrides.` |

### 1.2 `src/Generator/Model/Shared/MethodOverloadSignature.cs`

| Line | Old | New |
|------|-----|-----|
| 38-39 | `/// User method name for this signature's fallback ... In mixed overload groups, some signatures may have user overrides while others do not.` | `/// Stub override name for this signature's fallback ... In mixed overload groups, some signatures may have stub overrides while others do not.` |
| 41 | `string? UserMethodName = null,` | `string? StubOverrideName = null,` |

### 1.3 `src/Generator/Model/Flat/FlatGenerationUnit.cs`

| Line | Old | New |
|------|-----|-----|
| 25 | `EquatableArray<FlatMethodGroup> UserMethodGroups,` | `EquatableArray<FlatMethodGroup> StubOverrideGroups,` |
| 28 | `EquatableArray<FlatGenericMethodHandlerGroup> GenericUserMethodHandlerGroups,` | `EquatableArray<FlatGenericMethodHandlerGroup> GenericStubOverrideHandlerGroups,` |

### 1.4 `src/Generator/Model/Flat/FlatMethodModel.cs`

| Line | Old | New |
|------|-----|-----|
| 31 | `bool HasUserOverride,` | `bool HasStubOverride,` |

### 1.5 `src/Generator/Model/Flat/FlatPropertyModel.cs`

| Line | Old | New |
|------|-----|-----|
| 29 | `/// in their partial class (base class user property pattern).` | `/// in their partial class (base class stub override property pattern).` |
| 31 | `bool HasUserOverride = false,` | `bool HasStubOverride = false,` |

### 1.6 `src/Generator/Model/Inline/InlineClassStubModel.cs`

| Line | Old | New |
|------|-----|-----|
| 225 | `/// in their partial class (base class user property pattern).` | `/// in their partial class (base class stub override property pattern).` |
| 227 | `bool HasUserOverride = false,` | `bool HasStubOverride = false,` |
| 314 | `bool HasUserOverride = false,` | `bool HasStubOverride = false,` |

### 1.7 `src/Generator/Models/CommonModels.cs`

| Line | Old | New |
|------|-----|-----|
| 32 | `EquatableArray<string> UserOverrideMethods,` | `EquatableArray<string> StubOverrideMethods,` |
| 35 | `/// Used for base class user property pattern.` | `/// Used for base class stub override property pattern.` |
| 38 | `EquatableArray<string> UserOverrideProperties,` | `EquatableArray<string> StubOverrideProperties,` |

---

## Phase 2: Generator Code -- Builders

### 2.1 `src/Generator/Builder/FlatModelBuilder.cs`

All `HasUserOverride` references (lines 48, 50, 60, 62, 372, 739, 747, 863, 982, 1817) become `HasStubOverride`.

All `UserMethodGroups` / `GenericUserMethodHandlerGroups` references (lines 61, 91, 104, 106) become `StubOverrideGroups` / `GenericStubOverrideHandlerGroups`.

All `UserOverrideMethods` / `UserOverrideProperties` references become `StubOverrideMethods` / `StubOverrideProperties`:
- Line 26: `typeInfo.UserOverrideMethods` -> `typeInfo.StubOverrideMethods`
- Line 29: `typeInfo.UserOverrideProperties` -> `typeInfo.StubOverrideProperties`
- Line 661: `typeInfo.UserOverrideMethods` -> `typeInfo.StubOverrideMethods`

Variable renames:
- Line 26: `userOverrideMethods` -> `stubOverrideMethods`
- Line 29: `userOverrideProperties` -> `stubOverrideProperties`
- Line 32, 42, 124, 171, 205, 266, 271, 272, 317, 353, 661, 730, 764, 863, 1739, 1742: all `userOverrideMethods` -> `stubOverrideMethods` and `userOverrideProperties` -> `stubOverrideProperties`
- Line 61: `flatUserMethodGroups` -> `flatStubOverrideGroups`
- Line 91: `genericUserMethodHandlerGroups` -> `genericStubOverrideHandlerGroups`
- Lines 271, 274, 280: `withUserOverride` -> `withStubOverride`
- Lines 272, 274, 289: `withoutUserOverride` -> `withoutStubOverride`
- Lines 278, 279, 283: `userOverrideName` -> `stubOverrideName`
- Line 353: `hasUserOverride` -> `hasStubOverride`

Method renames:
- Line 1129: `BuildGenericUserMethodHandlerGroups` -> `BuildGenericStubOverrideHandlerGroups`
- Line 1739: `HasMatchingUserOverride` -> `HasMatchingStubOverride`
- Line 1991: call to `RenderPropertyUserOverrideImplementation` -> `RenderPropertyStubOverrideImplementation` (in FlatRenderer.cs, but referenced from builder context)

Comment updates (lines 25, 28, 48, 60, 129, 170, 204, 259, 260, 261, 270, 276, 277, 286, 297, 352, 660, 664, 1126, 1133, 1536, 1736, 1737, 1815, 1816): All "user override" / "user method" / "UserOverrideMethods" comments -> "stub override".

### 2.2 `src/Generator/Builder/StandaloneClassModelBuilder.cs`

All `UserOverrideMethods` / `UserOverrideProperties` references become `StubOverrideMethods` / `StubOverrideProperties`:
- Line 57: `info.UserOverrideProperties` -> `info.StubOverrideProperties`
- Line 58: `info.UserOverrideMethods` -> `info.StubOverrideMethods`

Variable renames:
- Line 57: `userOverrideProperties` -> `stubOverrideProperties`
- Line 58: `userOverrideMethods` -> `stubOverrideMethods`
- Lines 158, 160, 244: `hasUserOverride` (local variable) -> `hasStubOverride`
- Lines 216, 217: `hasUserOverride` (local variable) -> `hasStubOverride`

Parameter renames:
- Line 501: `bool hasUserOverride` -> `bool hasStubOverride`
- Line 548: `bool hasUserOverride` -> `bool hasStubOverride`

| Line | Old | New |
|------|-----|-----|
| 56 | `// Build user override lookups for base class pattern` | `// Build stub override lookups for base class pattern` |
| 153 | `// Set per-signature UserMethodName for overloads with user overrides` | `// Set per-signature StubOverrideName for overloads with stub overrides` |
| 160 | `UserMethodName = $"__UserMethod_{m.Name}"` | `StubOverrideName = $"__StubOverride_{m.Name}"` |
| 164 | `// Pass userMethodName to the interceptor builder if ANY overload has a user override` | `// Pass stubOverrideName to the interceptor builder if ANY overload has a stub override` |
| 165 | `var anyHasUserOverride = signatures.Any(s => s.UserMethodName != null);` | `var anyHasStubOverride = signatures.Any(s => s.StubOverrideName != null);` |
| 166 | `var userMethodName = anyHasUserOverride ? $"__UserMethod_{group.MethodName}" : null;` | `var stubOverrideName = anyHasStubOverride ? $"__StubOverride_{group.MethodName}" : null;` |
| 175 | `userMethodName: userMethodName);` | `stubOverrideName: stubOverrideName);` |
| 243 | `// Check if this specific method overload has a user override` | `// Check if this specific method overload has a stub override` |
| 283 | `// Build base class properties (virtual protected properties for user override pattern)` | `// Build base class properties (virtual protected properties for stub override pattern)` |
| 296 | `// Build base class methods (virtual protected methods for user override pattern)` | `// Build base class methods (virtual protected methods for stub override pattern)` |
| 512 | `HasUserOverride: hasUserOverride,` | `HasStubOverride: hasStubOverride,` |
| 575 | `HasUserOverride: hasUserOverride,` | `HasStubOverride: hasStubOverride,` |

### 2.3 `src/Generator/Builder/UnifiedInterceptorBuilder.cs`

| Line | Old | New |
|------|-----|-----|
| 29 | `/// <param name="userMethodName">Optional user method name for fallback (e.g., "Process_"). Null if no user override.</param>` | `/// <param name="stubOverrideName">Optional stub override name for fallback (e.g., "Process_"). Null if no stub override.</param>` |
| 37 | `string? userMethodName = null)` | `string? stubOverrideName = null)` |
| 71 | `UserMethodName: userMethodName,` | `StubOverrideName: stubOverrideName,` |
| 101 | `// For multi-overload, user method is tracked per-signature (see MethodOverloadSignature.UserMethodName)` | `// For multi-overload, stub override is tracked per-signature (see MethodOverloadSignature.StubOverrideName)` |
| 102 | `UserMethodName: userMethodName,` | `StubOverrideName: stubOverrideName,` |
| 104 | `...BuildOverloadSignature(methodName, sig, ownerClassName, ownerTypeParameters, userMethodName)...` | `...BuildOverloadSignature(methodName, sig, ownerClassName, ownerTypeParameters, stubOverrideName)...` |
| 150 | `string? userMethodName = null)` | `string? stubOverrideName = null)` |
| 174 | `UserMethodName: sig.UserMethodName,` | `StubOverrideName: sig.StubOverrideName,` |
| 540 | `/// <summary>Per-signature user method name for partial overload coverage. Null if no user override for this signature.</summary>` | `/// <summary>Per-signature stub override name for partial overload coverage. Null if no stub override for this signature.</summary>` |
| 541 | `string? UserMethodName = null,` | `string? StubOverrideName = null,` |

---

## Phase 3: Generator Code -- Renderers

### 3.1 `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`

All `UserMethodFallback` references -> `StubOverrideFallback` (lines 672, 842, 898, 1038, 1155, 1258, 1296, 1369).

All `UserMethodName` references -> `StubOverrideName` (lines 672, 842, 850, 855, 898, 1038, 1046, 1051, 1155, 1258, 1263, 1296, 1369, 1373).

All `userMethodCallArgs` variables -> `stubOverrideCallArgs` (lines 845, 850, 855, 1041, 1046, 1051, 1261, 1263, 1371, 1373).

All comment updates "user method" -> "stub override" (lines 671, 841, 844, 897, 1037, 1040, 1154, 1257, 1260, 1295, 1368).

### 3.2 `src/Generator/Renderer/FlatRenderer.cs`

All `UserMethodGroups` / `GenericUserMethodHandlerGroups` references -> `StubOverrideGroups` / `GenericStubOverrideHandlerGroups` (lines 119, 128, 141, 169, 170, 176, 177, 193, 1736, 1782, 1805, 2154).

Method rename:
- Line 1991: call to `RenderPropertyUserOverrideImplementation` -> `RenderPropertyStubOverrideImplementation`
- Line 2040: method definition `RenderPropertyUserOverrideImplementation` -> `RenderPropertyStubOverrideImplementation`

Variable renames:
- Line 169: `multiOverloadUserMethodInterceptors` -> `multiOverloadStubOverrideInterceptors`
- Line 176: `multiOverloadGenericUserMethodInterceptors` -> `multiOverloadGenericStubOverrideInterceptors`
- Line 1805: `userMethodInterceptorNames` -> `stubOverrideInterceptorNames`

Method renames:
- Line 1270: `RenderGenericUserMethodHandlerGroup` -> `RenderGenericStubOverrideHandlerGroup`
- Line 1363: `RenderGenericUserMethodTypedHandlerClass` -> `RenderGenericStubOverrideTypedHandlerClass`
- Line 2210: `RenderUserOverrideImplementation` -> `RenderStubOverrideImplementation`
- Line 2289: `GetGenericUserMethodSignatureSuffix` -> `GetGenericStubOverrideSignatureSuffix`

Parameter renames:
- Line 2152: `multiOverloadUserMethodInterceptors` -> `multiOverloadStubOverrideInterceptors`
- Line 2153: `multiOverloadGenericUserMethodInterceptors` -> `multiOverloadGenericStubOverrideInterceptors`
- Line 2154: `genericUserMethodHandlerGroups` -> `genericStubOverrideHandlerGroups`

All comment updates (lines 56, 140, 168, 174, 193, 1268, 1272, 1735, 1741, 1778, 2036-2038, 2050, 2052, 2054, 2066, 2068, 2070, 2206-2208, 2212, 2266, 2287, 2387): All "user method" / "user override" comments -> "stub override".

### 3.3 `src/Generator/Renderer/StandaloneClassRenderer.cs`

All `HasUserOverride` references -> `HasStubOverride` (lines 555, 733, 773, 943).

All `UserMethodName` / `UserMethodFallback` references -> `StubOverrideName` / `StubOverrideFallback` (lines 93, 94, 101).

Variable renames:
- Line 93: `hasUserMethod` -> `hasStubOverride` (in this context)
- Line 555: `methodsWithUserOverride` -> `methodsWithStubOverride`
- Line 556: `methodsWithUserOverride` -> `methodsWithStubOverride`
- Line 559: `methodsWithUserOverride` -> `methodsWithStubOverride`

Method rename:
- Line 553: `RenderUserMethodForwarders` -> `RenderStubOverrideForwarders`
- Line 171: call to `RenderUserMethodForwarders` -> `RenderStubOverrideForwarders`

String literal:
- Line 561: `$"__UserMethod_{method.MethodName}"` -> `$"__StubOverride_{method.MethodName}"`

Comment update (line 121): `// Extends the generated base class for user property overrides` -> `// Extends the generated base class for stub override property overrides`

All other comment updates (lines 92, 166, 169, 544, 547, 550, 735, 737, 738, 739, 748, 775, 777, 778, 779, 794, 945, 946, 947, 948, 966): All "user method" / "user override" comments -> "stub override".

### 3.4 `src/Generator/Renderer/Shared/ModelAdapters.cs`

All `UserMethodName` references -> `StubOverrideName` (lines 91, 140, 146, 170, 373).

All `UserMethodFallback` references -> `StubOverrideFallback` (line 382).

All `HasUserOverride` references -> `HasStubOverride` (lines 90, 91, 140, 147, 170).

All comment updates (lines 90, 139, 145, 146, 169, 346): All "user method" / "user override" comments -> "stub override".

### 3.5 `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`

All "user override" comments -> "stub override" (lines 265, 317, 320, 331, 335, 339, 367, 371, 375).

Comment update (line 265): reference to `RenderPropertyUserOverrideImplementation` -> `RenderPropertyStubOverrideImplementation`.

---

## Phase 4: Generator Code -- Transform, Helpers, and Entry Point

### 4.1 `src/Generator/KnockOffGenerator.Transform.cs`

All `UserOverrideMethods` / `UserOverrideProperties` record property references become `StubOverrideMethods` / `StubOverrideProperties`:
- Lines 691, 692, 724, 725, 762, 763, 792, 793, 913, 914, 935, 936: record initialization with `UserOverrideMethods:` / `UserOverrideProperties:` -> `StubOverrideMethods:` / `StubOverrideProperties:`

Variable renames:
- Line 919: `userOverrideMethods` -> `stubOverrideMethods`
- Line 920: `userOverrideMethodsArray` -> `stubOverrideMethodsArray`
- Line 923: `userOverrideProperties` -> `stubOverrideProperties`
- Line 924: `userOverridePropertiesArray` -> `stubOverridePropertiesArray`

Method call renames:
- Line 919: `DetectUserOverrideMethods` -> `DetectStubOverrideMethods`
- Line 923: `DetectUserOverrideProperties` -> `DetectStubOverrideProperties`

Comment updates (lines 918, 922): "user override" -> "stub override".

### 4.2 `src/Generator/KnockOffGenerator.Helpers.cs`

Method renames:
- Line 22: `DetectUserOverrideMethods` -> `DetectStubOverrideMethods`
- Line 122: `DetectUserOverrideProperties` -> `DetectStubOverrideProperties`

Comment and docstring updates (lines 21, 62, 107, 116, 121): "user override" / "user property" -> "stub override" / "stub override property".

### 4.3 `src/Generator/KnockOffGenerator.cs`

Comment update (line 504): "user overrides" -> "stub overrides".

**Diagnostic message `KO0200` (USER-FACING TEXT):**

| Line | Old | New |
|------|-----|-----|
| 88 | `/// The generator controls the base class for user method override support.` | `/// The generator controls the base class for stub override support.` |
| 93 | `messageFormat: "Standalone stub '{0}' cannot have base class '{1}'. KnockOff generates a base class for user method support. Remove the base class or use inline stub pattern instead."` | `messageFormat: "Standalone stub '{0}' cannot have base class '{1}'. KnockOff generates a base class for stub override support. Remove the base class or use inline stub pattern instead."` |

### 4.4 `src/Generator/KnockOffGenerator.StandaloneClass.cs`

All `UserOverrideMethods` / `UserOverrideProperties` record property references become `StubOverrideMethods` / `StubOverrideProperties`:
- Lines 119, 120, 154, 155, 188, 189, 244, 246: record initialization and record definition

Variable renames:
- Line 173: `userOverrideProperties` -> `stubOverrideProperties`
- Line 174: `userOverridePropertiesArray` -> `stubOverridePropertiesArray`
- Line 177: `userOverrideMethods` -> `stubOverrideMethods`
- Line 178: `userOverrideMethodsArray` -> `stubOverrideMethodsArray`

Method call renames:
- Line 173: `DetectUserOverrideProperties` -> `DetectStubOverrideProperties`
- Line 177: `DetectUserOverrideMethods` -> `DetectStubOverrideMethods`

Comment updates (lines 215, 243, 245): "user overrides" -> "stub overrides".

### 4.5 `src/Generator/Models/SymbolHelpers.cs`

Comment/docstring updates:
- Line 346: `DetectUserOverrideMethods` -> `DetectStubOverrideMethods`
- Line 370: "user override methods" -> "stub override methods".

---

## Phase 5: Design Domain Interfaces

### 5.1 `src/Design/Design.Domain/Services/IUserMethodService.cs`

**Rename file to:** `IStubOverrideService.cs`

Rename all identifiers within:
- `IUserMethodService` -> `IStubOverrideService`
- `IOverloadedUserMethodService` -> `IOverloadedStubOverrideService` (already defined in UserMethodBasics.cs)
- `IMixedUserMethodService` -> `IMixedStubOverrideService`
- `IAsyncUserMethodService` -> `IAsyncStubOverrideService`
- `IGenericUserMethodService` -> `IGenericStubOverrideService`

All "user method" comments -> "stub override" comments.

### 5.2 `src/Design/Design.Domain/Services/IVoidUserMethodService.cs`

**Rename file to:** `IVoidStubOverrideService.cs`

Rename identifiers:
- `IVoidUserMethodService` -> `IVoidStubOverrideService`

All "user method" comments -> "stub override" comments.

### 5.3 `src/Design/Design.Domain/Services/IUserPropertyService.cs`

**Rename file to:** `IStubOverridePropertyService.cs`

Rename all identifiers:
- `IUserPropertyService` -> `IStubOverridePropertyService`
- `IMixedUserPropertyService` -> `IMixedStubOverridePropertyService`
- `IGenericUserPropertyService` -> `IGenericStubOverridePropertyService`

Rename interface member names:
- `WithUserProperty` -> `WithStubOverrideProperty`
- `WithoutUserProperty` -> `WithoutStubOverrideProperty`
- `ComputedWithUserProperty` -> `ComputedWithStubOverrideProperty`
- `ComputedWithoutUserProperty` -> `ComputedWithoutStubOverrideProperty`

All "user property" / "user properties" comments -> "stub override property" / "stub override properties".

File header comments updated: "demonstrating user-defined property patterns" -> "demonstrating stub override property patterns", "user property behavior" -> "stub override property behavior".

### 5.4 `src/Design/Design.Domain/Abstractions/ConfigBase.cs`

Comment updates only (no identifier changes -- these are abstract domain classes, not "user property" types):

| Line | Old | New |
|------|-----|-----|
| 2 | `// Design.Domain - Abstract base class for demonstrating user property patterns` | `// Design.Domain - Abstract base class for demonstrating stub override property patterns` |
| 9 | `/// Abstract base class for testing user property overrides with standalone class stubs.` | `/// Abstract base class for testing stub override property overrides with standalone class stubs.` |
| 30 | `/// Generic abstract base class for testing user property overrides with` | `/// Generic abstract base class for testing stub override property overrides with` |

### 5.5 `src/Design/Design.Domain/Services/IRefOutService.cs`

Comment update:
| Line | Old | New |
|------|-----|-----|
| 8 | `// with ref/out parameters. Only constant Return(value), standalone user methods,` | `// with ref/out parameters. Only constant Return(value), standalone stub overrides,` |

---

## Phase 6: Design Stubs

### 6.1 **Folder rename:** `src/Design/Design.Stubs/UserMethods/` -> `src/Design/Design.Stubs/StubOverrides/`

### 6.2 `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs`

**Rename file to:** `StubOverrideBasics.cs` (inside renamed folder)

Rename all identifiers (many occurrences):
- `BasicUserMethodStub` -> `BasicStubOverrideStub`
- `OverloadedUserMethodStub` -> `OverloadedStubOverrideStub`
- `MixedUserMethodStub` -> `MixedStubOverrideStub`
- `PartialOverloadUserMethodStub` -> `PartialOverloadStubOverrideStub`
- `StrictUserMethodStub` -> `StrictStubOverrideStub`
- `AsyncUserMethodStub` -> `AsyncStubOverrideStub`
- `GenericUserMethodStub` -> `GenericStubOverrideStub`
- `UserMethodBasicsDemo` -> `StubOverrideBasicsDemo`
- `MixedUserMethodDemo` -> `MixedStubOverrideDemo`
- `StrictModeUserMethodDemo` -> `StrictModeStubOverrideDemo`
- `AsyncUserMethodDemo` -> `AsyncStubOverrideDemo`
- `GenericUserMethodDemo` -> `GenericStubOverrideDemo`
- `IUserMethodService` -> `IStubOverrideService`
- `IOverloadedUserMethodService` -> `IOverloadedStubOverrideService`
- `IMixedUserMethodService` -> `IMixedStubOverrideService`
- `IAsyncUserMethodService` -> `IAsyncStubOverrideService`
- `IGenericUserMethodService` -> `IGenericStubOverrideService`
- `WithUserMethod` -> `WithStubOverride`
- `WithoutUserMethod` -> `WithoutStubOverride`
- `ComputeWithUserMethod` -> `ComputeWithStubOverride`
- `ComputeWithoutUserMethod` -> `ComputeWithoutStubOverride`
- `WithUserMethod_` -> `WithStubOverride_`
- `ComputeWithUserMethod_` -> `ComputeWithStubOverride_`

All "user method" / "user override" comments -> "stub override".
Namespace `Design.Stubs.UserMethods` -> `Design.Stubs.StubOverrides`.

### 6.3 `src/Design/Design.Stubs/UserMethods/VoidUserMethodFallback.cs`

**Rename file to:** `VoidStubOverrideFallback.cs` (inside renamed folder)

Rename identifiers:
- `VoidUserMethodFallbackStub` -> `VoidStubOverrideFallbackStub`
- `IVoidUserMethodService` -> `IVoidStubOverrideService`

All comments updated.
Namespace updated.

### 6.4 `src/Design/Design.Stubs/UserMethods/StandaloneClassUserMethods.cs`

**Rename file to:** `StandaloneClassStubOverrides.cs` (inside renamed folder)

Rename identifiers:
- `StandaloneClassUserMethodStub` -> `StandaloneClassStubOverrideStub`
- `RepositoryUserMethodStub` -> `RepositoryStubOverrideStub`

All comments updated.
Namespace updated.

### 6.5 `src/Design/Design.Stubs/StubPatterns/GenericFormatterStub.cs`

Rename identifiers:
- `GenericFormatterWithUserMethodsStub` -> `GenericFormatterWithStubOverridesStub`

### 6.6 `src/Design/README.md`

Update references to `UserMethods/` folder name and descriptions.
Also add `StubOverrideProperties/` folder to the directory listing (currently `UserProperties/` is not listed, but `UserMethods/` is).

### 6.7 **Folder rename:** `src/Design/Design.Stubs/UserProperties/` -> `src/Design/Design.Stubs/StubOverrideProperties/`

### 6.8 `src/Design/Design.Stubs/UserProperties/UserPropertyBasics.cs`

**Rename file to:** `StubOverridePropertyBasics.cs` (inside renamed folder)

Rename all identifiers (many occurrences):
- `BasicUserPropertyStub` -> `BasicStubOverridePropertyStub`
- `MixedUserPropertyStub` -> `MixedStubOverridePropertyStub`
- `StrictUserPropertyStub` -> `StrictStubOverridePropertyStub`
- `GenericUserPropertyStub` -> `GenericStubOverridePropertyStub`
- `ConfigUserPropertyStub` -> `ConfigStubOverridePropertyStub`
- `CacheUserPropertyStub` -> `CacheStubOverridePropertyStub`
- `UserPropertyBasicsDemo` -> `StubOverridePropertyBasicsDemo`
- `MixedUserPropertyDemo` -> `MixedStubOverridePropertyDemo`
- `StrictModeUserPropertyDemo` -> `StrictModeStubOverridePropertyDemo`
- `GenericUserPropertyDemo` -> `GenericStubOverridePropertyDemo`
- `StandaloneClassUserPropertyDemo` -> `StandaloneClassStubOverridePropertyDemo`
- `GenericStandaloneClassUserPropertyDemo` -> `GenericStandaloneClassStubOverridePropertyDemo`
- `IUserPropertyService` -> `IStubOverridePropertyService`
- `IMixedUserPropertyService` -> `IMixedStubOverridePropertyService`
- `IGenericUserPropertyService` -> `IGenericStubOverridePropertyService`
- `WithUserProperty` -> `WithStubOverrideProperty` (property name on stub and interface member references)
- `WithoutUserProperty` -> `WithoutStubOverrideProperty`
- `ComputedWithUserProperty` -> `ComputedWithStubOverrideProperty`
- `ComputedWithoutUserProperty` -> `ComputedWithoutStubOverrideProperty`
- `WithUserProperty_` -> `WithStubOverrideProperty_`
- `ComputedWithUserProperty_` -> `ComputedWithStubOverrideProperty_`
- `SetUserPropertyValue` -> `SetStubOverridePropertyValue`
- `_userPropertyValue` -> `_stubOverridePropertyValue`

All "user property" / "user properties" / "User Properties" / "USER PROPERTIES" comments and section headings -> "stub override property" / "stub override properties" / "Stub Override Properties" / "STUB OVERRIDE PROPERTIES".

Namespace: `Design.Stubs.UserProperties` -> `Design.Stubs.StubOverrideProperties`.

### 6.9 `src/Design/Design.Stubs/Methods/GenericMethodClassStubs.cs`

Comment update:
| Line | Old | New |
|------|-----|-----|
| 77 | `// Note: User method overrides (MethodName_ pattern) are NOT supported for` | `// Note: Stub override overrides (MethodName_ pattern) are NOT supported for` |

---

## Phase 7: Design Tests

### 7.1 **Folder rename:** `src/Design/Design.Tests/UserMethodTests/` -> `src/Design/Design.Tests/StubOverrideTests/`

### 7.2 `src/Design/Design.Tests/UserMethodTests/VoidUserMethodFallbackTests.cs`

**Rename file to:** `VoidStubOverrideFallbackTests.cs` (inside renamed folder)

Rename identifiers:
- `VoidUserMethodFallbackTests` -> `VoidStubOverrideFallbackTests`
- `VoidUserMethodFallbackStub` -> `VoidStubOverrideFallbackStub`
- `IVoidUserMethodService` -> `IVoidStubOverrideService`
- Namespace `Design.Tests.UserMethodTests` -> `Design.Tests.StubOverrideTests`
- Using `Design.Stubs.UserMethods` -> `Design.Stubs.StubOverrides`

All "user method" / "HasUserOverride" comments -> "stub override" / "HasStubOverride".

### 7.3 `src/Design/Design.Tests/GenericOverloadTests/GenericStandaloneOverloadTests.cs`

Rename identifiers:
- `GenericFormatterWithUserMethodsStub` -> `GenericFormatterWithStubOverridesStub`
- Test method names containing `UserMethod` -> `StubOverride`
- `"UserMethod"` string literal -> `"StubOverride"` (line 555)
- `"UserMethodById"` string literal -> `"StubOverrideById"` (line 576)

### 7.4 **Folder rename:** `src/Design/Design.Tests/UserPropertyTests/` -> `src/Design/Design.Tests/StubOverridePropertyTests/`

### 7.5 `src/Design/Design.Tests/UserPropertyTests/UserPropertyBasicsTests.cs`

**Rename file to:** `StubOverridePropertyBasicsTests.cs` (inside renamed folder)

Rename identifiers:
- `UserPropertyBasicsTests` -> `StubOverridePropertyBasicsTests`
- `BasicUserPropertyStub` -> `BasicStubOverridePropertyStub`
- `StrictUserPropertyStub` -> `StrictStubOverridePropertyStub`
- `MixedUserPropertyStub` -> `MixedStubOverridePropertyStub`
- `GenericUserPropertyStub` -> `GenericStubOverridePropertyStub`
- `ConfigUserPropertyStub` -> `ConfigStubOverridePropertyStub`
- `CacheUserPropertyStub` -> `CacheStubOverridePropertyStub`
- `IUserPropertyService` -> `IStubOverridePropertyService`
- `IMixedUserPropertyService` -> `IMixedStubOverridePropertyService`
- `IGenericUserPropertyService` -> `IGenericStubOverridePropertyService`
- `WithUserProperty` -> `WithStubOverrideProperty` (property accesses on stubs and service references)
- `WithoutUserProperty` -> `WithoutStubOverrideProperty`
- `ComputedWithUserProperty` -> `ComputedWithStubOverrideProperty`
- `ComputedWithoutUserProperty` -> `ComputedWithoutStubOverrideProperty`

Test method renames:
- `StrictMode_UserPropertyOverrideBypassed` -> `StrictMode_StubOverridePropertyOverrideBypassed`
- `StrictMode_SetOnlyUserPropertyOverrideBypassed` -> `StrictMode_SetOnlyStubOverridePropertyOverrideBypassed`
- `Pattern1_Standalone_UserPropertiesWork` -> `Pattern1_Standalone_StubOverridePropertiesWork`
- `Pattern2_GenericStandalone_UserPropertiesWork` -> `Pattern2_GenericStandalone_StubOverridePropertiesWork`
- `Pattern3_StandaloneClass_UserPropertiesWork` -> `Pattern3_StandaloneClass_StubOverridePropertiesWork`
- `Pattern4_GenericStandaloneClass_UserPropertiesWork` -> `Pattern4_GenericStandaloneClass_StubOverridePropertiesWork`

Namespace: `Design.Tests.UserPropertyTests` -> `Design.Tests.StubOverridePropertyTests`

Using: `Design.Stubs.UserProperties` -> `Design.Stubs.StubOverrideProperties`

All "user property" / "User Property" / "user override" comments -> "stub override property" / "Stub Override Property" / "stub override".

File header comment: `Design.Tests - User Property Basics Tests` -> `Design.Tests - Stub Override Property Basics Tests`.

---

## Phase 8: KnockOffTests

### 8.1 `src/Tests/KnockOffTests/BaseClassUserMethodTests.cs`

**Rename file to:** `BaseClassStubOverrideTests.cs`

Rename identifiers:
- `BaseClassUserMethodTests` -> `BaseClassStubOverrideTests`
- `StrictModeUserMethodStub` -> `StrictModeStubOverrideStub`
- `StrictModeUserMethodStubBase` -> `StrictModeStubOverrideStubBase`
- `IStrictModeUserMethodTest` -> `IStrictModeStubOverrideTest`
- `MultiParamUserMethodStub` -> `MultiParamStubOverrideStub`
- `IMultiParamUserMethodService` -> `IMultiParamStubOverrideService`
- `AsyncUserMethodTestStub` -> `AsyncStubOverrideTestStub`
- `IAsyncUserMethodTestService` -> `IAsyncStubOverrideTestService`
- `OverloadedUserMethodStub` -> `OverloadedStubOverrideStub`
- `IOverloadedUserMethodService` -> `IOverloadedStubOverrideService`
- `INoOverrideService` / `NoOverrideStub` -> keep as-is (no "UserMethod" in name)

All "user override" / "user method" comments -> "stub override".
Test method names: update `UserOverride` and `UserMethod` in method names.

### 8.2 `src/Tests/KnockOffTests/StandaloneClassUserMethodTests.cs`

**Rename file to:** `StandaloneClassStubOverrideTests.cs`

Rename identifiers:
- `StandaloneClassUserMethodTests` -> `StandaloneClassStubOverrideTests`
- `SCUserMethodStub` -> `SCStubOverrideStub`
- `SCGenericUserMethodStub` -> `SCGenericStubOverrideStub`

All "user override" / "user method" comments and test names -> "stub override".

### 8.3 `src/Tests/KnockOffTests/UserMethodCustomTypeDetectionTests.cs`

**Rename file to:** `StubOverrideCustomTypeDetectionTests.cs`

Rename identifiers:
- `UserMethodCustomTypeDetectionTests` -> `StubOverrideCustomTypeDetectionTests`
- `CustomTypeUserMethodStub` -> `CustomTypeStubOverrideStub`
- `ICustomTypeUserMethodService` -> `ICustomTypeStubOverrideService`

All test method names and comments updated.

### 8.4 `src/Tests/KnockOffTests/UserMethodOnCallTests.cs`

**Rename file to:** `StubOverrideOnCallTests.cs`

Rename identifiers:
- `UserMethodOnCallTests` -> `StubOverrideOnCallTests`
- References to `StrictModeUserMethodStub`, `MultiParamUserMethodStub`, `AsyncUserMethodTestStub` -> renamed versions from 8.1

All test method names and comments updated.

### 8.5 `src/Tests/KnockOffTests/UserMethodVerificationTests.cs`

**Rename file to:** `StubOverrideVerificationTests.cs`

Rename identifiers:
- `UserMethodVerificationTests` -> `StubOverrideVerificationTests`
- References to `StrictModeUserMethodStub` -> renamed version from 8.1

All test method names and comments updated.

### 8.6 `src/Tests/KnockOffTests/UserMethodWhenTests.cs`

**Rename file to:** `StubOverrideWhenTests.cs`

Rename identifiers:
- `UserMethodWhenTests` -> `StubOverrideWhenTests`
- `WhenUserMethodStub` -> `WhenStubOverrideStub`
- `IWhenUserMethodTest` -> `IWhenStubOverrideTest`
- `GenericWhenUserMethodStub` -> `GenericWhenStubOverrideStub`
- `IGenericWhenUserMethodService` -> `IGenericWhenStubOverrideService`
- `OverloadedUserMethodStub` / `IOverloadedUserMethodService` -> renamed versions from 8.1

All test method names and comments updated.

### 8.7 `src/Tests/KnockOffTests/StrictModeTests.cs`

Rename identifiers:
- `IStrictModeUserMethodTest` -> `IStrictModeStubOverrideTest`
- `StrictModeUserMethodStub` -> `StrictModeStubOverrideStub`

Test method names: `StandaloneStub_Strict_UserMethod_*` -> `StandaloneStub_Strict_StubOverride_*`.

### 8.8 `src/Tests/KnockOffTests/GenericMethodBugTests.cs`

Rename identifiers:
- `IGenericMethodWithUserMethod` -> `IGenericMethodWithStubOverride`
- `GenericMethodWithUserMethodKnockOff` -> `GenericMethodWithStubOverrideKnockOff`

Comments: "user overrides" -> "stub overrides".

### 8.9 `src/Tests/KnockOffTests/AsyncMethodTests.cs`

Test method names: `WithUserMethod` -> `WithStubOverride`.
Comments: "User method" -> "Stub override".

### 8.10 `src/Tests/KnockOffTests/BasicTests.cs`

Test method name: `Method_WithNullableReturn_NoUserMethod_ReturnsDefault` -> `Method_WithNullableReturn_NoStubOverride_ReturnsDefault`.

### 8.11 `src/Tests/KnockOffTests/CallbackTests.cs`

Comments only: "User method" -> "Stub override" (lines 36, 78, 121, 151).

### 8.12 `src/Tests/KnockOffTests/StandaloneClassStubTests.cs`

Region rename:
| Line | Old | New |
|------|-----|-----|
| 312 | `#region Custom User Methods Tests` | `#region Custom Stub Override Tests` |

### 8.13 `src/Tests/KnockOffTests/MethodValueOverloadTests.cs`

Comment update:
| Line | Old | New |
|------|-----|-----|
| 128 | `// GetRequiredAsync returns Task<string> - no user method, so we can test value overload` | `// GetRequiredAsync returns Task<string> - no stub override, so we can test value overload` |

---

## Phase 9: Documentation Samples

### 9.1 `src/Tests/KnockOff.Documentation.Samples/UserMethodsSamples.cs`

**Rename file to:** `StubOverrideSamples.cs`

Rename ALL identifiers containing `UserMethod`:
- `IUserMethodsRepo` -> `IStubOverrideRepo`
- `IAsyncUserMethodRepo` -> `IAsyncStubOverrideRepo`
- `UserMethodsRepoStub` -> `StubOverrideRepoStub`
- `AsyncUserMethodRepoStub` -> `AsyncStubOverrideRepoStub`
- `UserMethodFallbackTests` -> `StubOverrideFallbackTests`
- `UserMethodReturnTests` -> `StubOverrideReturnTests`
- `UserMethodAsyncReturnTests` -> `StubOverrideAsyncReturnTests`
- `UserMethodVerificationTests` -> `StubOverrideVerificationTests`
- `UserMethodResetTests` -> `StubOverrideResetTests`
- `UserMethodInterceptorApiExampleTests` -> `StubOverrideInterceptorApiExampleTests`
- `UserMethodStandalonePatternTests` -> `StubOverrideStandalonePatternTests`
- `UserMethodsFormatterStub` -> `StubOverrideFormatterStub`
- `OverloadUserMethodTests` -> `OverloadStubOverrideTests`
- `CompleteUserMethodExampleTests` -> `CompleteStubOverrideExampleTests`

All `#region` snippet names:
- `user-methods-basic` -> `stub-overrides-basic`
- `user-methods-fallback` -> `stub-overrides-fallback`
- `user-methods-oncall` -> `stub-overrides-oncall`
- `user-methods-returns` -> `stub-overrides-returns`
- `user-methods-async-returns` -> `stub-overrides-async-returns`
- `user-methods-tracking` -> `stub-overrides-tracking`
- `user-methods-reset` -> `stub-overrides-reset`
- `user-methods-shareable-base` -> `stub-overrides-shareable-base`
- `user-methods-shareable-default` -> `stub-overrides-shareable-default`
- `user-methods-shareable-override` -> `stub-overrides-shareable-override`
- `user-methods-standalone-example` -> `stub-overrides-standalone-example`
- `user-methods-standalone-usage` -> `stub-overrides-standalone-usage`
- `user-methods-tracking-with-oncall` -> `stub-overrides-tracking-with-oncall`
- `user-methods-reset-preserves-oncall` -> `stub-overrides-reset-preserves-oncall`
- `user-methods-generated-base` -> `stub-overrides-generated-base`
- `user-methods-overloads` -> `stub-overrides-overloads`
- `user-methods-complete-example` -> `stub-overrides-complete-example`

Namespace: `KnockOff.Documentation.Samples.UserMethods` -> `KnockOff.Documentation.Samples.StubOverrides`

All comments updated.

### 9.2 `src/Tests/KnockOff.Documentation.Samples/TroubleshootingSamples.cs`

Rename identifiers:
- `TroubleshootUserMethodStub` -> `TroubleshootStubOverrideStub`
- `UserMethodExampleTests` -> `StubOverrideExampleTests`
- Test method: `UserMethod_ProvidesDefaultBehavior` -> `StubOverride_ProvidesDefaultBehavior`

### 9.3 `src/Tests/KnockOff.Documentation.Samples/SmartDefaultsSamples.cs`

Rename identifiers:
- `UserMethodOverrideStub` -> `StubOverrideOverrideStub` (or consider `StubOverrideExampleStub`)
- Test: `Override_WithUserMethod` -> `Override_WithStubOverride`

### 9.4 `src/Tests/KnockOff.Documentation.Samples/PatternsSamples.cs`

Rename identifiers:
- `UserRepoWithUserMethodsStub` -> `UserRepoWithStubOverridesStub`
- `#region patterns-user-methods` -> `#region patterns-stub-overrides`

### 9.5 `src/Tests/KnockOff.Documentation.Samples/SkillPatternsSamples.cs`

Rename identifiers:
- `UserMethodsTests` -> `StubOverridesTests`
- `UserMethods_ProvideDefaultImplementation` -> `StubOverrides_ProvideDefaultImplementation`
- `#region skill-patterns-user-methods` -> `#region skill-patterns-stub-overrides`

### 9.6 `src/Tests/KnockOff.Documentation.Samples/SkillContentSamples.cs`

Rename identifiers:
- `SkUserMethodRepoStub` -> `SkStubOverrideRepoStub`
- `UserMethodTests` -> `StubOverrideTests`
- Test method names: `UserMethodReturnOverride`, `UserMethodReturns`, etc. -> `StubOverrideReturnOverride`, `StubOverrideReturns`, etc.

### 9.7 `src/Tests/KnockOff.Documentation.Samples/SkillReadmeSamples.cs`

Rename identifiers:
- `SkillReadmeUserMethodsTests` -> `SkillReadmeStubOverridesTests`
- `UserMethods_ReturnSupersedes` -> `StubOverrides_ReturnSupersedes`
- `UserMethods_FallbackToUserOverride` -> `StubOverrides_FallbackToStubOverride`
- `#region skill-readme-user-methods` -> `#region skill-readme-stub-overrides`
- `#region skill-readme-user-methods-usage` -> `#region skill-readme-stub-overrides-usage`

Comments: "User override" -> "Stub override".

### 9.8 `src/Tests/KnockOff.Documentation.Samples/ApiConsistencyMatrixSamples.cs`

Rename identifiers:
- `MatrixUserMethodStub` -> `MatrixStubOverrideStub`
- `UserMethodsTests` -> `StubOverridesTests`
- `UserMethods_UsagePattern` -> `StubOverrides_UsagePattern`
- `#region matrix-user-methods-interface` -> `#region matrix-stub-overrides-interface`
- `#region matrix-user-methods-interface-usage` -> `#region matrix-stub-overrides-interface-usage`

### 9.9 `src/Tests/KnockOff.Documentation.Samples/CreateStubCommandSamples.cs`

Rename identifiers:
- `UserMethodsCommandTests` -> `StubOverridesCommandTests`
- `UserMethod_ProvidesDefaultBehavior` -> `StubOverride_ProvidesDefaultBehavior`
- `#region command-create-stub-user-methods` -> `#region command-create-stub-stub-overrides` (or simplify to `command-create-stub-overrides`)

### 9.10 `src/Tests/KnockOff.Documentation.Samples/PropertiesSamples.cs`

Rename identifiers:
- `UserPropertyBasicTests` -> `StubOverridePropertyBasicTests`
- `UserPropertyOnGetOnSetTests` -> `StubOverridePropertyOnGetOnSetTests`
- `UserPropertyTrackingTests` -> `StubOverridePropertyTrackingTests`
- `UserPropertyResetTests` -> `StubOverridePropertyResetTests`
- `UserPropertyStrictModeTests` -> `StubOverridePropertyStrictModeTests`

Test method renames:
- `UserProperty_ProvidesDefaultBehavior` -> `StubOverrideProperty_ProvidesDefaultBehavior`
- `OnGetOnSet_SupersedesUserProperty` -> `OnGetOnSet_SupersedesStubOverrideProperty`
- `Tracking_WorksThroughUserProperties` -> `Tracking_WorksThroughStubOverrideProperties`
- `StrictMode_BypassedForUserProperties` -> `StrictMode_BypassedForStubOverrideProperties`

All `#region` snippet names:
- `user-properties-interface-and-stub` -> `stub-override-properties-interface-and-stub`
- `user-properties-basic-usage` -> `stub-override-properties-basic-usage`
- `user-properties-onget-onset-override` -> `stub-override-properties-onget-onset-override`
- `user-properties-tracking` -> `stub-override-properties-tracking`
- `user-properties-reset` -> `stub-override-properties-reset`
- `user-properties-strict-mode` -> `stub-override-properties-strict-mode`

All "User Properties" / "user property" / "user properties" comments -> "Stub Override Properties" / "stub override property" / "stub override properties".

Comment updates: "user override" -> "stub override" (lines 586, 658 and additional lines throughout the User Properties section).

### 9.11 `src/Tests/KnockOff.Documentation.Samples/SkillContentSamples.cs`

Rename identifiers:
- `SkUserPropServiceStub` -> `SkStubOverridePropServiceStub`
- `UserPropertyTests` -> `StubOverridePropertyTests`

Test method renames:
- `UserPropertyOnGetOverride` -> `StubOverridePropertyOnGetOverride`
- `UserPropertyTracking` -> `StubOverridePropertyTracking`
- `UserPropertyReset` -> `StubOverridePropertyReset`

All `#region` snippet names:
- `skill-user-property-define` -> `skill-stub-override-property-define`
- `skill-user-property-onget` -> `skill-stub-override-property-onget`
- `skill-user-property-tracking` -> `skill-stub-override-property-tracking`
- `skill-user-property-reset` -> `skill-stub-override-property-reset`

All "User property" / "user property" / "User Properties" comments -> "Stub override property" / "stub override property" / "Stub Override Properties".

### 9.12 `src/Tests/KnockOff.Documentation.Samples/ReusableStubsSamples.cs`

Comment update:
| Line | Old | New |
|------|-----|-----|
| 175 | `// User method still works` | `// Stub override still works` |

---

## Phase 10: PackageTest

### 10.1 `src/Tests/PackageTest/Program.cs`

Comment update (line 48): "User method for Add" -> "Stub override for Add".

---

## Phase 11: Documentation Guides

### 11.1 `docs/guides/user-methods.md`

**Rename file to:** `docs/guides/stub-overrides.md`

Full content rewrite: every "user method" -> "stub override", every "User Method" -> "Stub Override".

All snippet references updated (e.g., `user-methods-basic` -> `stub-overrides-basic`).

Title: "User Methods" -> "Stub Overrides"
Breadcrumb: update accordingly.

### 11.2 `docs/guides/stub-patterns.md`

All "user methods" / "User methods" / "user methods/properties" references updated.
Link to `user-methods.md` -> `stub-overrides.md`.
Line 129: Update "user properties" -> "stub override properties" and link to `properties.md#user-properties-standalone-patterns` -> `properties.md#stub-override-properties-standalone-patterns`.

### 11.3 `docs/guides/methods.md`

Line 147: "user methods" -> "stub overrides". Link updated.

### 11.4 `docs/guides/properties.md`

Section heading rename: `## User Properties (Standalone Patterns)` -> `## Stub Override Properties (Standalone Patterns)`

All "user property" / "user properties" / "User Properties" prose updated throughout the User Properties section (lines 314-450+).

Snippet reference renames:
- `<!-- snippet: user-properties-interface-and-stub -->` -> `<!-- snippet: stub-override-properties-interface-and-stub -->`
- `<!-- snippet: user-properties-onget-onset-override -->` -> `<!-- snippet: stub-override-properties-onget-onset-override -->`
- `<!-- snippet: user-properties-tracking -->` -> `<!-- snippet: stub-override-properties-tracking -->`
- `<!-- snippet: user-properties-strict-mode -->` -> `<!-- snippet: stub-override-properties-strict-mode -->`

Anchor `#user-properties-standalone-patterns` -> `#stub-override-properties-standalone-patterns` (update links elsewhere that reference this anchor).

All "user override" comments -> "stub override" (lines 396, 402, 438).
Link updates if referencing `user-methods.md`.

### 11.5 `docs/guides/api-consistency-matrix.md`

Section "Feature 11: User Methods" -> "Feature 11: Stub Overrides".
Snippet references updated.
All "user method" / "User Method" prose updated.
Link to `user-methods.md` -> `stub-overrides.md`.

### 11.6 `docs/guides/source-delegation.md`

Line 95: "User methods" -> "Stub overrides".
Line 120: Link text and href updated.

### 11.7 `docs/guides/reusable-stubs.md`

All "user method" / "User methods" references updated.
Line 328: Link text and href to `user-methods.md` -> `stub-overrides.md`.

### 11.8 `docs/troubleshooting.md`

Lines 272-331: All "user method" prose updated to "stub override".
Diagnostic message text if it contains "user method" (check if this is a generated string literal in the generator -- if so, update there too).

### 11.9 `docs/reference/smart-defaults.md`

Line 207: Link text and href updated.

### 11.10 `docs/reference/interceptor-api.md`

Line 22: References updated.

### 11.11 `docs/release-notes/v0.13.1.md`

Lines 17, 32: "user methods" -> "stub overrides".

### 11.12 `docs/release-notes/v0.34.0.md`

Line 115: "User Override" -> "Stub Override".

### 11.13 `docs/release-notes/v0.33.0.md`

| Line | Old | New |
|------|-----|-----|
| 31 | `// Aggregate verification includes user methods` | `// Aggregate verification includes stub overrides` |
| 43 | `**Note:** User-defined methods are intentionally excluded from \`VerifyAll()\`. Since user methods always have an implementation...` | `**Note:** User-defined methods are intentionally excluded from \`VerifyAll()\`. Since stub overrides always have an implementation...` |
| 53 | `- Added \`_isVerifiable\` and \`_verifiableTimes\` fields to user method interceptors` | `- Added \`_isVerifiable\` and \`_verifiableTimes\` fields to stub override interceptors` |
| 55 | `- Expanded guard condition in FlatRenderer to include user methods and properties` | `- Expanded guard condition in FlatRenderer to include stub overrides and properties` |
| 56 | `- 18 new tests covering user method verification scenarios` | `- 18 new tests covering stub override verification scenarios` |

### 11.14 `docs/release-notes/v0.36.0.md`

| Line | Old | New |
|------|-----|-----|
| 37 | `### User Method Custom Type Detection` | `### Stub Override Custom Type Detection` |

---

## Phase 12: Documentation Plans and Todos

### Active plans and todos (update content but NOT filenames):

These files contain historical references. Update prose and identifiers but preserve filenames for traceability.

- `docs/plans/when-with-user-methods.md` -- Keep filename, update content: all "user method" -> "stub override"
- `docs/plans/user-method-verifiable-implementation.md` -- Keep filename, update content
- `docs/plans/documentation-structure.md` -- Update references to `user-methods.md` -> `stub-overrides.md`
- `docs/plans/migrate-execute-to-call.md` -- Update references
- `docs/plans/unify-returns-execute-design.md` -- Update references
- `docs/plans/rename-times-to-called.md` -- Update references to `UserProperties/UserPropertyBasics.cs` -> `StubOverrideProperties/StubOverridePropertyBasics.cs` and `UserPropertyTests/UserPropertyBasicsTests.cs` -> `StubOverridePropertyTests/StubOverridePropertyBasicsTests.cs`
- `docs/plans/documentation-fixes-implementation.md` -- Update references
- `docs/todos/when-with-user-methods.md` -- Keep filename, update content

### Completed plans and todos (update ONLY broken links):

These are historical records. Only update if they reference `user-methods.md` as a link target (since that file is being renamed to `stub-overrides.md`). Do NOT update all "user method" prose in completed files.

Files with links to `user-methods.md`:
- `docs/plans/completed/remove-legacy-user-method-pattern.md` -- Update link targets only
- `docs/plans/completed/standalone-class-user-methods.md` -- Update link targets only
- `docs/plans/completed/returns-api-rename.md` -- Update link targets only
- `docs/plans/completed/base-class-followup-fixes.md` -- Update link targets only
- `docs/plans/completed/base-class-user-methods-design.md` -- Update link targets only
- `docs/todos/completed/standalone-class-user-methods.md` -- Update link targets only
- `docs/todos/completed/remove-legacy-user-method-pattern.md` -- Update link targets only
- `docs/todos/completed/returns-api-rename.md` -- Update link targets only
- `docs/todos/completed/base-class-user-methods.md` -- Update link targets only
- `docs/todos/completed/docs-verify-verifiable.md` -- Update link targets only
- `docs/todos/completed/documentation-rewrite.md` -- Update link targets only
- `docs/todos/completed/focus-documentation-snippets.md` -- Update link targets only

Files with `UserProperty` / `UserProperties` path references (update broken paths only, not prose):
- `docs/plans/completed/user-properties-design.md` -- Historical record, do NOT update all prose
- `docs/plans/completed/standalone-class-user-methods.md` -- Has `UserPropertyBasics.cs` and `CacheUserPropertyStub` references
- `docs/plans/completed/rename-onget-onset-design.md` -- Has `UserProperties/`, `UserPropertyTests/`, `IUserPropertyService.cs` path references
- `docs/todos/completed/add-user-properties.md` -- Historical record, do NOT update all prose

---

## Phase 13: Skills

### 13.1 `skills/knockoff/SKILL.md`

Lines 135, 142, 213, 528-590, 669, 725: All "User Method" / "user method" / "User Methods" -> "Stub Override" / "stub override" / "Stub Overrides".

Section heading: "## User Methods (Stand-Alone Only)" -> "## Stub Overrides (Stand-Alone Only)"

Additional User Property renames:
- Line 137: `User Property` -> `Stub Override Property` in interceptor table
- Line 142: "user property interceptors" -> "stub override property interceptors" and "user property" -> "stub override property" references
- Line 213: "User Methods & Properties" -> "Stub Overrides" and "User Properties" -> "Stub Override Properties"
- Section heading: `## User Properties (Stand-Alone Only)` -> `## Stub Override Properties (Stand-Alone Only)` (line 621)
- Lines 623-669: All "User property" / "user property" / "User Properties" prose -> "Stub override property" / "stub override property" / "Stub Override Properties"
- Line 846: "`references/properties.md` - Property interceptors and user properties" -> "and stub override properties"

Snippet reference renames:
- `<!-- snippet: skill-user-property-define -->` -> `<!-- snippet: skill-stub-override-property-define -->`
- `<!-- snippet: skill-user-property-onget -->` -> `<!-- snippet: skill-stub-override-property-onget -->`
- `<!-- snippet: skill-user-property-tracking -->` -> `<!-- snippet: skill-stub-override-property-tracking -->`
- `<!-- snippet: skill-user-property-reset -->` -> `<!-- snippet: skill-stub-override-property-reset -->`

### 13.2 `skills/knockoff/references/methods.md`

Lines 392-526: Full section rename. "User Method Interceptors" -> "Stub Override Interceptors".
All "user method" prose updated.
Snippet references updated.

### 13.3 `skills/knockoff/references/patterns.md`

Lines 101, 112, 179, 194, 256, 324, 343, 395, 469, 587, 657, 679: All "user methods" / "User Methods" / "User methods" -> "stub overrides" / "Stub Overrides" / "Stub overrides".
Section heading: "### User Methods" -> "### Stub Overrides".
Snippet reference updated.

### 13.4 `skills/knockoff/references/api-reference.md`

Lines 112-155, 573, 683, 695: All "user method" / "User Method" -> "stub override" / "Stub Override".
Section heading updated.

### 13.5 `skills/knockoff/references/properties.md`

Lines 433, 439, 480, 496: All "user override" -> "stub override".

Additional User Property renames:

Section heading: `## User Properties (Stand-Alone Pattern)` -> `## Stub Override Properties (Stand-Alone Pattern)` (line 331)

Sub-section headings:
- `### Why Use User Properties?` -> `### Why Use Stub Override Properties?` (line 335)
- `### Defining User Properties` -> `### Defining Stub Override Properties` (line 345)
- `### Using Stubs with User Properties` -> `### Using Stubs with Stub Override Properties` (line 389)
- `### Get/Set Supersede User Properties` -> `### Get/Set Supersede Stub Override Properties` (line 410)
- `### Tracking Works Through User Properties` -> `### Tracking Works Through Stub Override Properties` (line 437)
- `### Strict Mode Bypassed for User Properties` -> `### Strict Mode Bypassed for Stub Override Properties` (line 478)

All "user property" / "user properties" / "User Properties" / "User Property" prose throughout lines 331-520 -> "stub override property" / "stub override properties" / "Stub Override Properties" / "Stub Override Property".

Snippet reference renames:
- `<!-- snippet: user-properties-interface-and-stub -->` -> `<!-- snippet: stub-override-properties-interface-and-stub -->`
- `<!-- snippet: user-properties-basic-usage -->` -> `<!-- snippet: stub-override-properties-basic-usage -->`
- `<!-- snippet: user-properties-onget-onset-override -->` -> `<!-- snippet: stub-override-properties-onget-onset-override -->`
- `<!-- snippet: user-properties-tracking -->` -> `<!-- snippet: stub-override-properties-tracking -->`
- `<!-- snippet: user-properties-reset -->` -> `<!-- snippet: stub-override-properties-reset -->`
- `<!-- snippet: user-properties-strict-mode -->` -> `<!-- snippet: stub-override-properties-strict-mode -->`

Table row (line 504): `User Property Support` -> `Stub Override Property Support`.

---

## Phase 14: Agent Files

### 14.1 `.claude/agents/knockoff-architect.md`

Lines 617-634: Update "User Methods" -> "Stub Overrides" in the example.
Lines 619-620: "user method override" -> "stub override".

### 14.2 `.claude/agents/knockoff-developer.md`

Lines 413-414, 475: "user methods" -> "stub overrides".

---

## Phase 15: Generated Files (Gitignored)

Generated `.g.cs` files in `Generated/` folders are excluded from git. After the generator code is renamed, these files will be regenerated automatically with new names on the next build. No manual action needed, but the file names will change (e.g., `BasicUserMethodStub.g.cs` -> `BasicStubOverrideStub.g.cs`, `BasicUserPropertyStub.g.cs` -> `BasicStubOverridePropertyStub.g.cs`).

**Important:** The `__UserMethod_` prefix in generated forwarder method names changes to `__StubOverride_`. This is an internal implementation detail not exposed to users. The user-facing API (the `_` suffix convention) does NOT change.

---

## Phase 16: Verification

### 16.1 Build verification

```bash
dotnet build src/KnockOff.sln
dotnet build src/Design/Design.Stubs
```

### 16.2 Test verification

```bash
dotnet test src/KnockOff.sln
dotnet test src/Design/Design.Tests
```

### 16.3 Grep verification

After all renames, run these searches to confirm no occurrences remain:

```bash
# Should return ZERO results (excluding completed plans/todos and this plan):
grep -ri "UserMethod" src/
grep -ri "userMethod" src/
grep -ri "user method" src/ docs/guides/ docs/reference/ skills/
grep -ri "HasUserOverride" src/
grep -ri "UserMethodFallback" src/
grep -ri "UserMethodName" src/
grep -ri "__UserMethod_" src/
grep -ri "UserPropert" src/
grep -ri "user propert" src/ docs/guides/ docs/reference/ skills/
grep -ri "user-propert" src/ docs/guides/ skills/
grep -ri "IUserPropertyService" src/
grep -ri "UserOverrideMethod" src/
grep -ri "UserOverridePropert" src/
grep -ri "DetectUserOverride" src/
grep -ri "HasMatchingUserOverride" src/
grep -ri "withUserOverride" src/
grep -ri "withoutUserOverride" src/
grep -ri "userOverrideName" src/
grep -ri "methodsWithUserOverride" src/
grep -ri "RenderPropertyUserOverride" src/
grep -ri "hasUserOverride" src/
```

Expected remaining occurrences (acceptable):
- `docs/plans/completed/` -- historical records, not updated
- `docs/todos/completed/` -- historical records, not updated
- `docs/plans/when-with-user-methods.md` -- filename preserved, content updated
- `docs/plans/user-method-verifiable-implementation.md` -- filename preserved, content updated
- `docs/todos/when-with-user-methods.md` -- filename preserved, content updated

---

## Breaking Changes Assessment

**No breaking changes.** The rename affects:
- Internal generator identifiers (not part of any public API)
- Internal generated method names (`__UserMethod_` -> `__StubOverride_`) which are implementation details
- File/folder names in Design, Tests, and Docs (no external consumers)

The user-facing API is unchanged:
- The `_` suffix convention for override methods is unchanged
- Attribute names (`[KnockOff]`, `[KnockOffBase<T>]`) are unchanged
- Interceptor property names (`stub.Method`, `stub.Property`) are unchanged
- All library types are unchanged

---

## Implementation Order

The recommended execution order minimizes broken-build windows:

1. **Phases 1-4** (Generator code) -- rename all identifiers, build to verify
2. **Phase 5** (Design domain interfaces) -- rename interfaces, build Design.Stubs to verify
3. **Phase 6** (Design stubs) -- rename stubs + folder, build to verify
4. **Phase 7** (Design tests) -- rename tests + folder, run Design.Tests
5. **Phases 8-10** (KnockOffTests + PackageTest + Documentation samples) -- rename tests and samples, run full test suite
6. **Phases 11-14** (Docs, skills, agents) -- content updates, no build impact
7. **Phase 16** (Verification) -- final grep + build + test

---

## Architectural Verification

This is a mechanical rename with no architectural impact. No scope table or Design.Stubs compilation verification is needed beyond confirming the build still passes after each phase.

**Pattern impact:** None. All nine patterns continue to work identically.

**Breaking changes:** None.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Missed rename location | Medium | Build failure | Exhaustive grep verification in Phase 16 |
| MarkdownSnippets breakage | Medium | Stale docs | Update snippet names in both source and markdown |
| Merge conflicts with other branches | Low | Time cost | Complete on dedicated branch, merge promptly |
| `__UserMethod_` in diagnostic messages | Low | User-facing text change | Verify troubleshooting.md diagnostic strings match generator |

---

## Estimated Effort

This is a large mechanical rename touching ~50+ files. The changes are repetitive and low-risk individually, but the volume requires careful execution.

Estimated phases:
- Generator code (Phases 1-4): Medium -- most complex, requires careful model/builder/renderer alignment
- Design + Tests (Phases 5-10): Large -- many files with many identifier renames
- Documentation (Phases 11-14): Medium -- prose and link updates
- Verification (Phase 16): Small -- automated checks

---

## Developer Review

**Status:** Approved (after revision)
**Reviewed:** 2026-02-08

**Concerns raised and resolved:**

1. **~70 `UserOverride`/`userOverride` identifiers missed** -- FIXED. Added all missing identifiers to the naming convention mapping table and to the appropriate phase sections (Phases 1, 2, 3, 4).
2. **Diagnostic message `KO0200` missed** -- FIXED. Added to Phase 4.3 (KnockOffGenerator.cs) with explicit line-level entries for the user-facing comment and messageFormat string.
3. **5 source files not in any phase** -- FIXED. Added: `ReusableStubsSamples.cs` to Phase 9.12, `StandaloneClassStubTests.cs` to Phase 8.12, `MethodValueOverloadTests.cs` to Phase 8.13, `GenericMethodClassStubs.cs` to Phase 6.9, `IRefOutService.cs` to Phase 5.5.
4. **2 release notes files missed** -- FIXED. Added `v0.33.0.md` as Phase 11.13 and `v0.36.0.md` as Phase 11.14.
5. **5 completed files missing for link updates** -- FIXED. Added `base-class-followup-fixes.md`, `base-class-user-methods-design.md`, `standalone-class-user-methods.md` (todos), `remove-legacy-user-method-pattern.md` (todos), and `returns-api-rename.md` (todos) to the Phase 12 completed section.
6. **Phase 4 organization confusing** -- FIXED. Merged subsections 4.7-4.10 into their primary phase entries: FlatPropertyModel.cs comment merged into Phase 1.5, InlineClassStubModel.cs comment merged into Phase 1.6, KnockOffGenerator.Helpers.cs docstring merged into Phase 4.2, StandaloneClassRenderer.cs comment merged into Phase 3.3.
7. **Implementation order lists Phase 9 twice** -- FIXED. Consolidated step 5 to include Phases 8-10 (KnockOffTests + PackageTest + Documentation samples) and step 6 now covers Phases 11-14 (Docs, skills, agents). Removed duplicate Phase 9 entry.
