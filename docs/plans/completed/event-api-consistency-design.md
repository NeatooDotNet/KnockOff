# Event API Consistency Design

**Date:** 2026-02-06
**Related Todo:** [Fix Event API Inconsistency Between Patterns](../todos/completed/fix-event-api-inconsistency.md)
**Status:** Complete
**Last Updated:** 2026-02-06

---

## Overview

The event interceptor API is inconsistent across the four renderer pipelines. The standalone (Flat) pipeline generates `Raise()`, `HasSubscribers`, bare property names, and a private `_handler` field. The three other pipelines (InlineRenderer, ClassRenderer, StandaloneClassRenderer) generate a public `Handler` property, no `Raise()`, no `HasSubscribers`, and the inline pipeline additionally uses an `Interceptor` suffix on property names.

This plan brings all four pipelines into alignment with the FlatRenderer pattern:

1. **Add `Raise()` method** to InlineRenderer, ClassRenderer, and StandaloneClassRenderer event interceptor classes
2. **Add `HasSubscribers` property** to all three renderers
3. **Make handler field private** (`private _handler` instead of `public Handler { get; private set; }`)
4. **Drop `Interceptor` suffix** from inline event interceptor property names
5. **Add Raise-related fields** to InlineEventModel and InlineClassEventModel
6. **Add Raise computation** to InlineModelBuilder, ClassModelBuilder, and StandaloneClassModelBuilder
7. **Update Design.Stubs** to use `Raise()` API and remove old `DID NOT DO THIS` comments
8. **Update Design.Tests** to use `Raise()` API
9. **Update documentation and skill files** to reflect the consistent API

### Breaking Changes

All breaking changes are approved by the user:

- `Handler` property removed (was public, becomes private `_handler` field)
- `Interceptor` suffix removed from inline event properties (e.g., `StartedInterceptor` becomes `Started`)

---

## Approach

### Strategy: Extract GetRaiseMethodInfo into EventBuilderHelpers

The `FlatModelBuilder` already contains `GetRaiseMethodInfo()` which computes Raise parameters from `EventMemberInfo`. This logic will be extracted into a new shared static class `EventBuilderHelpers` so all four builders can use it.

**EscapeIdentifier dependency:** `GetRaiseMethodInfo` calls `EscapeIdentifier` (a private method that prefixes C# keywords with `@`). This method is duplicated identically in all four builders (FlatModelBuilder:1518, InlineModelBuilder:1404, ClassModelBuilder:453, StandaloneClassModelBuilder:510). The extraction must include `EscapeIdentifier` as well, since `EventBuilderHelpers.GetRaiseMethodInfo` needs it to escape parameter names in Raise signatures.

**Recommended approach:** Create `src/Generator/Builder/EventBuilderHelpers.cs` containing both `GetRaiseMethodInfo` (extracted from FlatModelBuilder) and a private `EscapeIdentifier` method (copied from any builder -- they are all identical). Do NOT refactor the existing `EscapeIdentifier` calls in the four builders; leave those untouched. The `EventBuilderHelpers.EscapeIdentifier` is a localized copy specifically for event Raise parameter generation.

**Why not refactor all EscapeIdentifier into one place?** While tempting, a global refactoring of EscapeIdentifier is out of scope for this change. Each builder uses EscapeIdentifier for different purposes (method parameters, property names, indexer keys, etc.). Moving all 14+ call sites across 4 builders into a shared location would be a separate refactoring task with its own risk profile. The event-specific copy in `EventBuilderHelpers` is a minimal, focused solution.

### Shared Event Rendering

After adding Raise-related fields to the models, the rendering logic for `Raise()` and `HasSubscribers` can be identical across all four renderers. The `FlatRenderer.RenderEventRaiseMethod` pattern is the template.

---

## Current State Analysis

### Files Examined

| File | Role | Current Event Behavior |
|------|------|----------------------|
| `src/Generator/Model/Flat/FlatEventModel.cs` | Flat model | Has `RaiseParameters`, `RaiseArguments`, `RaiseReturnType`, `RaiseReturnsValue`, `UsesDynamicInvoke` |
| `src/Generator/Model/Inline/InlineEventModel.cs` | Inline model | Only `InterceptorClassName`, `EventName`, `DelegateType`, `TypeParameterList`, `ConstraintClauses` |
| `src/Generator/Model/Inline/InlineClassStubModel.cs` | Class model (`InlineClassEventModel`) | Only `InterceptorClassName`, `EventName`, `DelegateType`, `TypeParameterList`, `ConstraintClauses` |
| `src/Generator/Builder/FlatModelBuilder.cs` | Flat builder | Has `GetRaiseMethodInfo()` (lines 1216-1261), calls `EscapeIdentifier` (line 1518) |
| `src/Generator/Builder/InlineModelBuilder.cs` | Inline builder | `BuildEventModel()` (lines 529-545) -- no Raise info. `EscapeIdentifier` at line 1404 |
| `src/Generator/Builder/ClassModelBuilder.cs` | Class builder | `BuildEventModel()` (line 330) -- no Raise info. `EscapeIdentifier` at line 453 |
| `src/Generator/Builder/StandaloneClassModelBuilder.cs` | Standalone class builder | `BuildEventModel()` (line 384) -- no Raise info. `EscapeIdentifier` at line 510 |
| `src/Generator/Renderer/FlatRenderer.cs` | Flat renderer | `private _handler`, `HasSubscribers`, `Raise()` method, bare names |
| `src/Generator/Renderer/InlineRenderer.cs` | Inline renderer | `public Handler { get; private set; }`, no `HasSubscribers`, no `Raise()`, `Interceptor` suffix |
| `src/Generator/Renderer/ClassRenderer.cs` | Class renderer | `public Handler { get; private set; }`, no `HasSubscribers`, no `Raise()`, bare names |
| `src/Generator/Renderer/StandaloneClassRenderer.cs` | Standalone class renderer | `public Handler { get; private set; }`, no `HasSubscribers`, no `Raise()`, bare names |
| `src/Generator/Models/EventModels.cs` | Transform model | `EventMemberInfo` already carries `DelegateKind`, `DelegateParameters`, `ReturnTypeName` |

### Scope Table

| Pattern | Pipeline | Naming Fix | Raise/HasSubscribers | Handler Private |
|---------|----------|-----------|---------------------|-----------------|
| 1. Standalone | FlatRenderer | N/A (already bare) | N/A (already has) | N/A (already private) |
| 2. Generic Standalone | FlatRenderer | N/A | N/A | N/A |
| 3. Standalone Class | StandaloneClassRenderer | N/A (already bare) | Needs Implementation | Needs Implementation |
| 4. Generic Standalone Class | StandaloneClassRenderer | N/A | Needs Implementation | Needs Implementation |
| 5. Inline Interface | InlineRenderer | Needs Implementation (drop suffix) | Needs Implementation | Needs Implementation |
| 6. Inline Class | ClassRenderer | N/A (already bare) | Needs Implementation | Needs Implementation |
| 7. Inline Delegate | N/A | N/A (delegates have no events) | N/A | N/A |
| 8. Open Generic Interface | InlineRenderer | Needs Implementation (drop suffix) | Needs Implementation | Needs Implementation |
| 9. Open Generic Class | ClassRenderer | N/A (already bare) | Needs Implementation | Needs Implementation |

---

## Design

### Model Changes

#### InlineEventModel (Inline Interface + Open Generic Interface)

**File:** `src/Generator/Model/Inline/InlineEventModel.cs`

Add five new fields to match `FlatEventModel`:

```csharp
internal sealed record InlineEventModel(
    string InterceptorClassName,
    string EventName,
    string DelegateType,
    string TypeParameterList,
    string ConstraintClauses,
    // NEW FIELDS:
    string RaiseParameters,      // e.g., "object? sender, EventArgs e"
    string RaiseArguments,       // e.g., "sender, e"
    string RaiseReturnType,      // e.g., "void"
    bool RaiseReturnsValue,      // true for Func<> delegates
    bool UsesDynamicInvoke);     // true for custom delegates
```

#### InlineClassEventModel (Inline Class + Open Generic Class + Standalone Class + Generic Standalone Class)

**File:** `src/Generator/Model/Inline/InlineClassStubModel.cs`

Add the same five fields:

```csharp
internal sealed record InlineClassEventModel(
    string InterceptorClassName,
    string EventName,
    string DelegateType,
    string TypeParameterList = "",
    string ConstraintClauses = "",
    // NEW FIELDS:
    string RaiseParameters = "",
    string RaiseArguments = "",
    string RaiseReturnType = "void",
    bool RaiseReturnsValue = false,
    bool UsesDynamicInvoke = false);
```

### Builder Changes

#### New File: EventBuilderHelpers.cs

**File:** `src/Generator/Builder/EventBuilderHelpers.cs`

Create a new static class containing:

1. `GetRaiseMethodInfo(EventMemberInfo evt)` -- extracted from `FlatModelBuilder` (lines 1216-1261)
2. A private `EscapeIdentifier(string name)` method -- copied from any builder (all identical)

```csharp
internal static class EventBuilderHelpers
{
    internal static (string RaiseParams, string RaiseArgs, string RaiseReturnType,
                     bool RaiseReturnsValue, bool UsesDynamicInvoke) GetRaiseMethodInfo(EventMemberInfo evt)
    {
        // Same logic as FlatModelBuilder.GetRaiseMethodInfo, calling local EscapeIdentifier
    }

    private static string EscapeIdentifier(string name)
    {
        // Same logic as the private method in any builder
    }
}
```

The `FlatModelBuilder.GetRaiseMethodInfo` can then be updated to delegate to `EventBuilderHelpers.GetRaiseMethodInfo`, or left as-is (since it already works). The developer should choose whichever approach minimizes risk.

#### InlineModelBuilder.BuildEventModel (lines 529-545)

Update to compute Raise info from `EventMemberInfo`:

```csharp
private static InlineEventModel BuildEventModel(
    EventMemberInfo evt, string stubClassName,
    string typeParamList, string constraintClause)
{
    var interceptClassName = $"{stubClassName}_{evt.Name}Interceptor";
    var delegateType = evt.FullDelegateTypeName.TrimEnd('?');
    var (raiseParams, raiseArgs, raiseReturnType, raiseReturnsValue, usesDynamicInvoke) =
        EventBuilderHelpers.GetRaiseMethodInfo(evt);

    return new InlineEventModel(
        InterceptorClassName: interceptClassName,
        EventName: evt.Name,
        DelegateType: delegateType,
        TypeParameterList: typeParamList,
        ConstraintClauses: constraintClause,
        RaiseParameters: raiseParams,
        RaiseArguments: raiseArgs,
        RaiseReturnType: raiseReturnType,
        RaiseReturnsValue: raiseReturnsValue,
        UsesDynamicInvoke: usesDynamicInvoke);
}
```

#### ClassModelBuilder.BuildEventModel (~line 330)

Same change pattern -- add Raise info computation via `EventBuilderHelpers.GetRaiseMethodInfo(evt)`.

#### StandaloneClassModelBuilder.BuildEventModel (~line 384)

Same change pattern.

### Renderer Changes

#### InlineRenderer.RenderEventInterceptorClass (lines 885-999)

Change from:
```csharp
// Current: public Handler property
w.Line($"\t\t\tpublic {evt.DelegateType}? Handler {{ get; private set; }}");
```

To:
```csharp
// New: private _handler field + HasSubscribers + Raise()
w.Line($"\t\t\tprivate {evt.DelegateType}? _handler;");
w.Line();
w.Line("\t\t\t/// <summary>Whether any handlers are subscribed.</summary>");
w.Line("\t\t\tpublic bool HasSubscribers => _handler != null;");
w.Line();
// Raise method (same logic as FlatRenderer.RenderEventRaiseMethod)
```

Also update `RecordAdd`/`RecordRemove` to use `_handler` instead of `Handler`, update `Reset()` to use `_handler`, and update `IsConfigured` to use `_handler`.

#### ClassRenderer.RenderEventInterceptorClass (lines 129-242)

Same changes as InlineRenderer -- replace `Handler` property with `_handler` field, add `HasSubscribers`, add `Raise()`.

#### StandaloneClassRenderer.RenderEventInterceptorClass (lines 295-407)

Same changes as above.

### Naming Change (Inline Only)

#### InlineModelBuilder.BuildInterceptorProperties (line 627-635)

Change event interceptor property name from `$"{evt.Name}Interceptor"` to `evt.Name`:

```csharp
// Current:
properties.Add(new InlineInterceptorPropertyModel(
    PropertyName: $"{evt.Name}Interceptor",    // <-- "StartedInterceptor"
    ...));

// New:
properties.Add(new InlineInterceptorPropertyModel(
    PropertyName: evt.Name,                     // <-- "Started"
    ...));
```

#### InlineModelBuilder.BuildEventImplementation (line 1069)

Change event implementation `InterceptorName` from `$"{evt.Name}Interceptor"` to `evt.Name`:

```csharp
// Current:
InterceptorName: $"{evt.Name}Interceptor",     // <-- "StartedInterceptor"

// New:
InterceptorName: evt.Name,                      // <-- "Started"
```

---

## Implementation Steps

### Phase 1: Shared Helper and Model Changes

1. Create `src/Generator/Builder/EventBuilderHelpers.cs` with `GetRaiseMethodInfo` and private `EscapeIdentifier` extracted from `FlatModelBuilder`
2. Update `FlatModelBuilder.GetRaiseMethodInfo` to delegate to the shared helper (or leave as-is -- developer's choice)
3. Add five Raise fields to `InlineEventModel`
4. Add five Raise fields to `InlineClassEventModel`
5. Update `InlineModelBuilder.BuildEventModel` to compute and pass Raise info
6. Update `ClassModelBuilder.BuildEventModel` to compute and pass Raise info
7. Update `StandaloneClassModelBuilder.BuildEventModel` to compute and pass Raise info
8. **Verification Gate:** `dotnet build src/KnockOff.sln` -- must compile

### Phase 2: Renderer Changes

1. Update `InlineRenderer.RenderEventInterceptorClass`:
   - Replace `public Handler { get; private set; }` with `private _handler` field
   - Add `HasSubscribers` property
   - Add `Raise()` method (replicate `FlatRenderer.RenderEventRaiseMethod` logic)
   - Update `RecordAdd`/`RecordRemove` to use `_handler`
   - Update `Reset()` to use `_handler`
   - Update `IsConfigured` to use `_handler`

2. Update `ClassRenderer.RenderEventInterceptorClass`:
   - Same changes as InlineRenderer

3. Update `StandaloneClassRenderer.RenderEventInterceptorClass`:
   - Same changes as InlineRenderer

4. **Verification Gate:** `dotnet build src/KnockOff.sln` -- must compile

### Phase 3: Naming Change (Inline Only)

1. Update `InlineModelBuilder.BuildInterceptorProperties` (line ~632): Change `$"{evt.Name}Interceptor"` to `evt.Name`
2. Update `InlineModelBuilder.BuildEventImplementation` (line ~1069): Change `InterceptorName: $"{evt.Name}Interceptor"` to `InterceptorName: evt.Name`
3. **Verification Gate:** `dotnet build src/KnockOff.sln` -- must compile

### Phase 4: Fix Tests

Existing tests will break due to the API changes. **Exhaustive list of test files requiring updates:**

#### A. Event-specific changes (Handler -> Raise, suffix removal, HasSubscribers)

**KnockOffTests project:**

| File | Lines | Change Needed |
|------|-------|---------------|
| `src/Tests/KnockOffTests/BclInterfaceTests.cs` | 849 | `stub.PropertyChangedInterceptor.Handler?.Invoke(...)` -> `stub.PropertyChanged.Raise(...)` |
| `src/Tests/KnockOffTests/BclInterfaceTests.cs` | 863 | `stub.PropertyChangedInterceptor.VerifyAdd(...)` -> `stub.PropertyChanged.VerifyAdd(...)` |
| `src/Tests/KnockOffTests/BclInterfaceTests.cs` | 875 | `stub.PropertyChangingInterceptor.Handler?.Invoke(...)` -> `stub.PropertyChanging.Raise(...)` |
| `src/Tests/KnockOffTests/NeatooTests.cs` | 601 | `stub.PropertyChangedInterceptor.VerifyAdd(...)` -> `stub.PropertyChanged.VerifyAdd(...)` |
| `src/Tests/KnockOffTests/NeatooTests.cs` | 614 | `stub.PropertyChangedInterceptor.VerifyAdd(...)` -> `stub.PropertyChanged.VerifyAdd(...)` |
| `src/Tests/KnockOffTests/NeatooTests.cs` | 615 | `stub.PropertyChangedInterceptor.VerifyRemove(...)` -> `stub.PropertyChanged.VerifyRemove(...)` |
| `src/Tests/KnockOffTests/NeatooTests.cs` | 627 | `stub.NeatooPropertyChangedInterceptor.VerifyAdd(...)` -> `stub.NeatooPropertyChanged.VerifyAdd(...)` |
| `src/Tests/KnockOffTests/StandaloneClassStubTests.cs` | 470 | `stub.StatusChanged.Handler?.Invoke(...)` -> `stub.StatusChanged.Raise(...)` |

**KnockOff.NeatooInterfaceTests project:**

| File | Lines | Change Needed |
|------|-------|---------------|
| `src/Tests/KnockOff.NeatooInterfaceTests/Notifications/INotifyNeatooPropertyChangedTests.cs` | 35, 48, 49, 62 | `stub.NeatooPropertyChangedInterceptor.VerifyAdd/VerifyRemove(...)` -> `stub.NeatooPropertyChanged.VerifyAdd/VerifyRemove(...)` |
| `src/Tests/KnockOff.NeatooInterfaceTests/Properties/IValidatePropertyTests.cs` | 276, 289, 290, 301 | `stub.PropertyChangedInterceptor.VerifyAdd/VerifyRemove(...)` -> `stub.PropertyChanged.VerifyAdd/VerifyRemove(...)` and `stub.NeatooPropertyChangedInterceptor.VerifyAdd(...)` -> `stub.NeatooPropertyChanged.VerifyAdd(...)` |
| `src/Tests/KnockOff.NeatooInterfaceTests/PropertyManagers/IEntityPropertyManagerTests.cs` | 263, 274 | `stub.PropertyChangedInterceptor.VerifyAdd(...)` -> `stub.PropertyChanged.VerifyAdd(...)` and `stub.NeatooPropertyChangedInterceptor.VerifyAdd(...)` -> `stub.NeatooPropertyChanged.VerifyAdd(...)` |
| `src/Tests/KnockOff.NeatooInterfaceTests/PropertyManagers/IValidatePropertyManagerTests.cs` | 266, 277 | Same pattern as above |
| `src/Tests/KnockOff.NeatooInterfaceTests/Collections/IEntityListBaseTests.cs` | 192, 203 | Same pattern as above |
| `src/Tests/KnockOff.NeatooInterfaceTests/Collections/IValidateListBaseTests.cs` | 168, 179 | Same pattern as above |

#### B. Files that should NOT be changed (delegate stubs, not event stubs)

The following files use `stub.Interceptor.Verify()` etc. for **delegate stubs**, not events. These must NOT be modified:

- `src/Tests/KnockOffTests/InlineStubTests.cs` -- delegate interceptor access (e.g., `stub.Interceptor.Verify()`)
- `src/Tests/KnockOffTests/DelegateValueOverloadTests.cs` -- delegate interceptor access
- `src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs` -- delegate interceptor access
- `src/Tests/KnockOffTests/NeatooTests.cs` lines 765-820 -- delegate interceptor access
- `src/Tests/KnockOff.NeatooInterfaceTests/Notifications/INotifyNeatooPropertyChangedTests.cs` lines 136-187 -- delegate interceptor access
- All `src/Tests/KnockOff.Documentation.Samples/` delegate-related samples

#### C. Files that should still pass without changes

- `src/Tests/KnockOffTests/EventTests.cs` -- Already uses standalone (Flat) API with `Raise()`, `HasSubscribers`, bare names. Should pass without changes.

5. **Verification Gate:** `dotnet test src/KnockOff.sln` -- all tests must pass

### Phase 5: Design Project Updates

1. **`src/Design/Design.Stubs/Events/EventPatterns.cs`**:
   - Remove all `DID NOT DO THIS` / `REJECTED PATTERN` / `WHY NOT` comments about Raise and HasSubscribers
   - Change all `stub.StartedInterceptor.Handler?.Invoke(...)` to `stub.Started.Raise(...)`
   - Change all `stub.XxxInterceptor.Handler != null` to `stub.Xxx.HasSubscribers`
   - Change all `stub.XxxInterceptor.VerifyAdd(...)` to `stub.Xxx.VerifyAdd(...)`
   - Change all `stub.XxxInterceptor.Reset()` to `stub.Xxx.Reset()`
   - Update all comments to document the Raise/HasSubscribers/bare-name pattern as the canonical design

2. **`src/Design/Design.Tests/EventTests/EventBasicsTests.cs`**:
   - Change all `stub.StartedInterceptor.Handler?.Invoke(...)` to `stub.Started.Raise(...)`
   - Change all `stub.XxxInterceptor.Handler` assertions to use `stub.Xxx.HasSubscribers`
   - Change all `stub.XxxInterceptor.VerifyAdd(...)` to `stub.Xxx.VerifyAdd(...)`
   - Change all `stub.XxxInterceptor.Reset()` to `stub.Xxx.Reset()`
   - Update `Assert.Null(stub.StartedInterceptor.Handler)` to `Assert.False(stub.Started.HasSubscribers)`
   - Update `Assert.NotNull(stub.StartedInterceptor.Handler)` to `Assert.True(stub.Started.HasSubscribers)`

3. **Verification Gate:** `dotnet build src/Design/Design.Stubs && dotnet test src/Design/Design.Tests` -- must pass

### Phase 6: Documentation Updates

1. **`docs/guides/events.md`** -- Already documents `Raise()` and `HasSubscribers` from the standalone perspective. Verify snippets still compile. No changes expected unless snippet markers reference old code.

2. **`docs/guides/api-consistency-matrix.md`** -- Already claims 100% consistency for events. After this fix, the claim becomes true. May need snippet updates if snippets are auto-generated from code.

3. **`docs/todos/fix-event-api-inconsistency.md`** -- Will be updated as part of workflow completion.

### Phase 7: Skill File Updates

1. **`skills/knockoff/skills/knockoff-usage/references/api-reference.md`** -- Already documents `Raise()`, `HasSubscribers`. Verify it correctly reflects the unified API. Remove any caveats about inline patterns being different.

2. **`skills/knockoff/skills/knockoff-usage/references/patterns.md`** -- Check for any references to the old `Handler?.Invoke()` pattern.

3. **`skills/knockoff/skills/knockoff-usage/SKILL.md`** -- Check for any references to the old pattern.

4. **`skills/knockoff/README.md`** -- Check for any event-related content.

---

## Architectural Verification

### Design Project Verification

**Acceptance criteria code written:** `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs`

**New domain types created for acceptance criteria:**
- `src/Design/Design.Domain/Abstractions/EventServiceBase.cs` -- Abstract class with events (Pattern 3)
- `src/Design/Design.Domain/Services/IGenericEventSource.cs` -- Generic interface with events (Pattern 8)

This file exercises the new API on three pattern types:

| Pattern | Stub Declaration | Features Tested |
|---------|-----------------|-----------------|
| Pattern 5: Inline Interface | `[KnockOff<IEventSource>]` on `EventPatternsDemo` | Bare names, Raise (EventHandler, EventHandler<T>, Action, Action<T,T>), HasSubscribers, Verify/Verifiable/Reset |
| Pattern 3: Standalone Class | `[KnockOffBase<EventServiceBase>]` on `EventServiceBaseStub` | Raise (EventHandler, Action), HasSubscribers, Verify/Verifiable/Reset |
| Pattern 8: Open Generic Interface | `[KnockOff(typeof(IGenericEventSource<>))]` on `OpenGenericEventDemo` | Bare names, Raise (EventHandler, EventHandler<T>), HasSubscribers, Verify/Verifiable/Reset |

**Build result:** 78 CS1061 errors across 3 target frameworks (net8.0, net9.0, net10.0). All errors are expected -- the new API members do not exist yet.

**Failing features by pattern:**

| Feature | Pattern | Compiler Error | Evidence |
|---------|---------|---------------|----------|
| Bare event name (`stub.Started`) | Pattern 5 (Inline Interface) | CS1061 | Lines 42-45 |
| `Raise()` on EventHandler | Pattern 5 | CS1061 (cascading) | Line 60 |
| `Raise()` on EventHandler<T> | Pattern 5 | CS1061 (cascading) | Line 70 |
| `Raise()` on Action | Pattern 5 | CS1061 (cascading) | Line 80 |
| `Raise()` on Action<T,T> | Pattern 5 | CS1061 (cascading) | Line 90 |
| `HasSubscribers` | Pattern 5 | CS1061 (cascading) | Lines 103, 106 |
| `Verify`/`Verifiable`/`Reset` with bare names | Pattern 5 | CS1061 (cascading) | Lines 120-123 |
| `Raise()` on EventHandler | Pattern 3 (Standalone Class) | CS1061 on `_StatusChangedInterceptor` | Line 150 |
| `Raise()` on Action | Pattern 3 | CS1061 on `_CompletedInterceptor` | Line 160 |
| `HasSubscribers` | Pattern 3 | CS1061 on `_StatusChangedInterceptor` | Lines 168, 171 |
| Bare event name (`stub.StatusChanged`) | Pattern 8 (Open Generic Interface) | CS1061 | Lines 211, 231, 234, 245-248 |
| `Raise()` on EventHandler<T> | Pattern 8 | CS1061 (cascading) | Line 222 |

### Breaking Changes

**Yes -- approved by user.** Two breaking changes:

1. **`Handler` property removed** from inline, class, and standalone class event interceptors. Users currently calling `stub.StartedInterceptor.Handler?.Invoke(...)` must switch to `stub.Started.Raise(...)`.

2. **`Interceptor` suffix removed** from inline event interceptor property names. Users currently accessing `stub.StartedInterceptor` must switch to `stub.Started`.

### Pattern Consistency Verification

After implementation, all nine patterns must produce event interceptors with:
- `private {DelegateType}? _handler` field (not public)
- `bool HasSubscribers => _handler != null` property
- `void Raise(...)` method (signature varies by delegate kind)
- Bare property names (e.g., `stub.Started`, not `stub.StartedInterceptor`)
- `VerifyAdd()`, `VerifyRemove()`, `Verify()`, `Verifiable()`, `Reset()` methods
- `_addCount`, `_removeCount` tracking

### Codebase Analysis

Files that will be modified:

**New files:**
- `src/Generator/Builder/EventBuilderHelpers.cs` -- Shared `GetRaiseMethodInfo` and private `EscapeIdentifier`
- `src/Design/Design.Domain/Abstractions/EventServiceBase.cs` -- Abstract class with events (already created for acceptance criteria)
- `src/Design/Design.Domain/Services/IGenericEventSource.cs` -- Generic interface with events (already created for acceptance criteria)

**Modified Generator files (7):**
- `src/Generator/Model/Inline/InlineEventModel.cs` -- Add 5 fields
- `src/Generator/Model/Inline/InlineClassStubModel.cs` -- Add 5 fields to `InlineClassEventModel`
- `src/Generator/Builder/InlineModelBuilder.cs` -- Update `BuildEventModel`, `BuildInterceptorProperties`, `BuildEventImplementation`
- `src/Generator/Builder/ClassModelBuilder.cs` -- Update `BuildEventModel`
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- Update `BuildEventModel`
- `src/Generator/Renderer/InlineRenderer.cs` -- Update `RenderEventInterceptorClass`
- `src/Generator/Renderer/ClassRenderer.cs` -- Update `RenderEventInterceptorClass`
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- Update `RenderEventInterceptorClass`

**Modified Design files (2):**
- `src/Design/Design.Stubs/Events/EventPatterns.cs` -- Full rewrite of event usage patterns
- `src/Design/Design.Tests/EventTests/EventBasicsTests.cs` -- Update to new API

**Modified Test files (8):**
- `src/Tests/KnockOffTests/BclInterfaceTests.cs` -- Lines 849, 863, 875
- `src/Tests/KnockOffTests/NeatooTests.cs` -- Lines 601, 614, 615, 627
- `src/Tests/KnockOffTests/StandaloneClassStubTests.cs` -- Line 470
- `src/Tests/KnockOff.NeatooInterfaceTests/Notifications/INotifyNeatooPropertyChangedTests.cs` -- Lines 35, 48, 49, 62
- `src/Tests/KnockOff.NeatooInterfaceTests/Properties/IValidatePropertyTests.cs` -- Lines 276, 289, 290, 301
- `src/Tests/KnockOff.NeatooInterfaceTests/PropertyManagers/IEntityPropertyManagerTests.cs` -- Lines 263, 274
- `src/Tests/KnockOff.NeatooInterfaceTests/PropertyManagers/IValidatePropertyManagerTests.cs` -- Lines 266, 277
- `src/Tests/KnockOff.NeatooInterfaceTests/Collections/IEntityListBaseTests.cs` -- Lines 192, 203
- `src/Tests/KnockOff.NeatooInterfaceTests/Collections/IValidateListBaseTests.cs` -- Lines 168, 179

**Unmodified test file (verify still passes):**
- `src/Tests/KnockOffTests/EventTests.cs` -- Already uses Flat API

**Potentially modified Documentation files (0-3):**
- `docs/guides/events.md` -- Verify snippets; may need no changes
- `docs/guides/api-consistency-matrix.md` -- Verify snippets
- Skill files -- Verify and update as needed

---

## Acceptance Criteria

1. All nine patterns that support events produce identical event interceptor classes (with appropriate type parameter variations)
2. Every event interceptor has: `Raise()`, `HasSubscribers`, private `_handler` field
3. No event interceptor has a public `Handler` property
4. Inline event interceptor properties use bare names (no `Interceptor` suffix)
5. `dotnet build src/KnockOff.sln` succeeds
6. `dotnet test src/KnockOff.sln` -- all tests pass (zero failures)
7. `dotnet build src/Design/Design.Stubs` succeeds (acceptance criteria file compiles)
8. `dotnet test src/Design/Design.Tests` -- all tests pass
9. Design.Stubs event file has no `DID NOT DO THIS` comments about Raise/HasSubscribers
10. Skill files accurately describe the unified event API

---

## Dependencies

- None -- this is a self-contained refactoring of event interceptor generation

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| External users relying on `Handler` property | Medium | Medium | Pre-1.0 -- breaking changes acceptable |
| External users relying on `Interceptor` suffix | Medium | Medium | Pre-1.0 -- breaking changes acceptable |
| Render logic divergence across 4 renderers | Low | Medium | Extract shared rendering method if feasible |
| Open generic event type parameters in Raise signature | Low | High | Acceptance criteria cover Pattern 8 with generic EventHandler<T> |
| EscapeIdentifier duplication in EventBuilderHelpers | Low | Low | Intentionally scoped -- full refactoring is a separate task |

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-06

### Re-Review (2026-02-06)

All three original concerns have been fully addressed:

**Concern 1 (Test file coverage):** RESOLVED. Phase 4 now contains exhaustive tables of all 9 test files and ~30 individual lines that will break, organized by project. Includes explicit lists of files that must NOT be changed (delegate stubs) and files expected to pass unchanged. Verified against actual codebase -- all affected files are accounted for.

**Concern 2 (EscapeIdentifier dependency):** RESOLVED. The Approach section documents the dependency chain (`GetRaiseMethodInfo` -> `EscapeIdentifier`), recommends a private copy in `EventBuilderHelpers`, and provides rationale for not refactoring globally. All 4 builders confirmed to have identical `EscapeIdentifier` at: FlatModelBuilder:1518, InlineModelBuilder:1404, ClassModelBuilder:453, StandaloneClassModelBuilder:510.

**Concern 3 (Acceptance criteria scope):** RESOLVED. Acceptance criteria file now covers 3 patterns (Inline Interface, Standalone Class, Open Generic Interface) with compilable stubs exercising all 4 delegate types, Raise(), HasSubscribers, and verification. New domain types `EventServiceBase` and `IGenericEventSource<T>` are well-formed. 78 CS1061 errors confirmed as expected.

### Files Examined During Review

- `src/Generator/Builder/FlatModelBuilder.cs` (lines 1216-1261, 1515-1534) -- GetRaiseMethodInfo and EscapeIdentifier
- `src/Generator/Builder/InlineModelBuilder.cs` (lines 529-545, 625-636, 1064-1078) -- BuildEventModel, BuildInterceptorProperties, BuildEventImplementation
- `src/Generator/Builder/ClassModelBuilder.cs` (lines 330-343) -- BuildEventModel
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` (lines 384-397) -- BuildEventModel
- `src/Generator/Renderer/InlineRenderer.cs` (lines 885-999) -- RenderEventInterceptorClass
- `src/Generator/Renderer/ClassRenderer.cs` (lines 129-242) -- RenderEventInterceptorClass
- `src/Generator/Renderer/StandaloneClassRenderer.cs` (lines 295-407) -- RenderEventInterceptorClass
- `src/Generator/Renderer/FlatRenderer.cs` (lines 1510-1687) -- RenderEventInterceptorClass (target pattern)
- `src/Generator/Model/Flat/FlatEventModel.cs` -- 5 Raise fields confirmed
- `src/Generator/Model/Inline/InlineEventModel.cs` -- Confirmed missing Raise fields
- `src/Generator/Model/Inline/InlineClassStubModel.cs` -- Confirmed InlineClassEventModel missing Raise fields
- `src/Generator/Models/EventModels.cs` -- EventMemberInfo carries DelegateKind, DelegateParameters, ReturnTypeName
- `src/Tests/KnockOffTests/BclInterfaceTests.cs` (lines 839-878) -- Confirmed will break
- `src/Tests/KnockOffTests/NeatooTests.cs` (lines 594-628) -- Confirmed will break
- `src/Tests/KnockOffTests/StandaloneClassStubTests.cs` (line 470) -- Confirmed will break
- `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs` -- Verified all line numbers match

---

## Implementation Contract

**Created:** 2026-02-06
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

These are the failing Design.Stubs files left by the architect. Implementation is done when they all compile.

- [x] `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs:42-45` -- Pattern 5 (Inline Interface): Bare event names (`stub.Started` etc.) must compile (currently CS1061)
- [x] `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs:60,70,80,90` -- Pattern 5: `Raise()` on all 4 delegate types must compile (currently CS1061)
- [x] `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs:103,106` -- Pattern 5: `HasSubscribers` must compile (currently CS1061)
- [x] `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs:120-123` -- Pattern 5: `VerifyAdd`, `Verify`, `Verifiable`, `Reset` with bare names must compile (currently CS1061)
- [x] `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs:150,160` -- Pattern 3 (Standalone Class): `Raise()` on EventHandler and Action must compile (currently CS1061)
- [x] `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs:168,171` -- Pattern 3: `HasSubscribers` must compile (currently CS1061)
- [x] `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs:181-184` -- Pattern 3: Verification methods must compile (currently CS1061)
- [x] `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs:211,222` -- Pattern 8 (Open Generic Interface): `Raise()` on EventHandler and EventHandler<T> must compile (currently CS1061)
- [x] `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs:231,234` -- Pattern 8: `HasSubscribers` must compile (currently CS1061)
- [x] `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs:245-248` -- Pattern 8: Verification methods with bare names must compile (currently CS1061)

### In Scope

**Phase 1: Shared Helper and Model Changes**
- [x] Create `src/Generator/Builder/EventBuilderHelpers.cs` with `GetRaiseMethodInfo` (from FlatModelBuilder:1216-1261) and private `EscapeIdentifier` (from any builder)
- [x] Add 5 Raise fields to `src/Generator/Model/Inline/InlineEventModel.cs`
- [x] Add 5 Raise fields to `InlineClassEventModel` in `src/Generator/Model/Inline/InlineClassStubModel.cs`
- [x] Update `InlineModelBuilder.BuildEventModel` (line 529) to call `EventBuilderHelpers.GetRaiseMethodInfo` and pass Raise fields
- [x] Update `ClassModelBuilder.BuildEventModel` (line 330) to call `EventBuilderHelpers.GetRaiseMethodInfo` and pass Raise fields
- [x] Update `StandaloneClassModelBuilder.BuildEventModel` (line 384) to call `EventBuilderHelpers.GetRaiseMethodInfo` and pass Raise fields
- [x] Checkpoint: `dotnet build src/KnockOff.sln` must compile

**Phase 2: Renderer Changes**
- [x] Update `InlineRenderer.RenderEventInterceptorClass` (line 885): Replace `Handler` property with `_handler` field, add `HasSubscribers`, add `Raise()`, update `RecordAdd`/`RecordRemove`/`Reset`/`IsConfigured` to use `_handler`
- [x] Update `ClassRenderer.RenderEventInterceptorClass` (line 129): Same changes as InlineRenderer
- [x] Update `StandaloneClassRenderer.RenderEventInterceptorClass` (line 295): Same changes as InlineRenderer
- [x] Checkpoint: `dotnet build src/KnockOff.sln` must compile

**Phase 3: Naming Change (Inline Only)**
- [x] Update `InlineModelBuilder.BuildInterceptorProperties` (line 632): Change `$"{evt.Name}Interceptor"` to `evt.Name`
- [x] Update `InlineModelBuilder.BuildEventImplementation` (line 1069): Change `InterceptorName: $"{evt.Name}Interceptor"` to `InterceptorName: evt.Name`
- [x] Checkpoint: `dotnet build src/KnockOff.sln` must compile

**Phase 4: Fix Tests**
- [x] `src/Tests/KnockOffTests/BclInterfaceTests.cs` lines 849, 863, 875: Update `PropertyChangedInterceptor`/`PropertyChangingInterceptor` to bare names, `Handler?.Invoke` to `Raise`
- [x] `src/Tests/KnockOffTests/NeatooTests.cs` lines 601, 614, 615, 627: Update `PropertyChangedInterceptor`/`NeatooPropertyChangedInterceptor` to bare names
- [x] `src/Tests/KnockOffTests/StandaloneClassStubTests.cs` line 470: Change `stub.StatusChanged.Handler?.Invoke(...)` to `stub.StatusChanged.Raise(...)`
- [x] `src/Tests/KnockOff.NeatooInterfaceTests/Notifications/INotifyNeatooPropertyChangedTests.cs` lines 35, 48, 49, 62: Update to bare names
- [x] `src/Tests/KnockOff.NeatooInterfaceTests/Properties/IValidatePropertyTests.cs` lines 276, 289, 290, 301: Update to bare names
- [x] `src/Tests/KnockOff.NeatooInterfaceTests/PropertyManagers/IEntityPropertyManagerTests.cs` lines 263, 274: Update to bare names
- [x] `src/Tests/KnockOff.NeatooInterfaceTests/PropertyManagers/IValidatePropertyManagerTests.cs` lines 266, 277: Update to bare names
- [x] `src/Tests/KnockOff.NeatooInterfaceTests/Collections/IEntityListBaseTests.cs` lines 192, 203: Update to bare names
- [x] `src/Tests/KnockOff.NeatooInterfaceTests/Collections/IValidateListBaseTests.cs` lines 168, 179: Update to bare names
- [x] Checkpoint: `dotnet test src/KnockOff.sln` -- all tests must pass (zero failures)

**Phase 5: Design Project Updates**
- [x] Update `src/Design/Design.Stubs/Events/EventPatterns.cs`: Remove `DID NOT DO THIS` comments, change `Interceptor` suffix to bare names, change `Handler?.Invoke` to `Raise`, change `Handler != null` to `HasSubscribers`
- [x] Update `src/Design/Design.Tests/EventTests/EventBasicsTests.cs`: Same changes as EventPatterns.cs, plus update assertions (`Assert.Null/NotNull(Handler)` to `Assert.False/True(HasSubscribers)`)
- [x] Checkpoint: `dotnet build src/Design/Design.Stubs` -- must compile (acceptance criteria satisfied)
- [x] Checkpoint: `dotnet test src/Design/Design.Tests` -- all tests must pass

**Phase 6: Documentation Updates**
- [x] Verify `docs/guides/events.md` snippets still compile (no changes expected) -- confirmed, already uses new API
- [x] Verify `docs/guides/api-consistency-matrix.md` snippets still compile -- removed outdated Interceptor suffix note on line 148

**Phase 7: Skill File Updates**
- [x] Check `skills/knockoff/skills/knockoff-usage/references/api-reference.md` for old `Handler?.Invoke()` references -- none found, already correct
- [x] Check `skills/knockoff/skills/knockoff-usage/references/patterns.md` for old `Handler?.Invoke()` references -- none found, no event-specific API references
- [x] Check `skills/knockoff/skills/knockoff-usage/SKILL.md` for old references -- none found, already uses `Raise()` and bare names
- [x] Check `skills/knockoff/README.md` for old references -- none found, already mentions `Raise()` and `HasSubscribers`

### Explicitly Out of Scope

- Refactoring `EscapeIdentifier` across all 4 builders into a single shared method -- separate refactoring task with its own risk profile
- Modifying `FlatModelBuilder.GetRaiseMethodInfo` to delegate to shared helper -- developer's choice, but the existing method already works
- Modifying delegate stub test files -- these use `stub.Interceptor.Verify()` for delegate interceptors, NOT event interceptors
- Adding new event test cases -- this task is about API consistency, not new coverage
- Extracting shared rendering method across all 4 renderers -- the code is mechanical and duplication is acceptable

### Verification Gates

1. After Phase 1: `dotnet build src/KnockOff.sln` compiles
2. After Phase 2: `dotnet build src/KnockOff.sln` compiles
3. After Phase 3: `dotnet build src/KnockOff.sln` compiles
4. After Phase 4: `dotnet test src/KnockOff.sln` -- zero failures
5. After Phase 5: `dotnet build src/Design/Design.Stubs` compiles AND `dotnet test src/Design/Design.Tests` -- zero failures
6. Final: All 10 acceptance criteria stubs compile, all tests pass across the entire solution

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (especially delegate stub tests listed in Phase 4 Section B)
- A test file not listed in Phase 4 Section A starts failing
- Architectural contradiction discovered (e.g., event model fields not propagating correctly through pipeline)
- Generated code does not compile after any phase
- Any `EventTests.cs` test (standalone/Flat pattern) starts failing -- this file should be unaffected

---

## Implementation Progress

**Started:** 2026-02-06

**Phase 1: Shared Helper and Model Changes** -- COMPLETE
- Created `src/Generator/Builder/EventBuilderHelpers.cs` with `GetRaiseMethodInfo` and `EscapeIdentifier`
- Added 5 Raise fields to `InlineEventModel` and `InlineClassEventModel`
- Updated `InlineModelBuilder`, `ClassModelBuilder`, `StandaloneClassModelBuilder` to call `EventBuilderHelpers.GetRaiseMethodInfo`
- Verification: `dotnet build src/KnockOff.sln` -- 0 errors, 0 warnings

**Phase 2: Renderer Changes** -- COMPLETE
- Rewrote `RenderEventInterceptorClass` in all 3 renderers: private `_handler` field, `HasSubscribers`, `Raise()` method, updated `RecordAdd`/`RecordRemove`/`Reset`/`IsConfigured`
- Added `RenderEventRaiseMethod` to InlineRenderer, ClassRenderer, StandaloneClassRenderer
- Verification: Generator project 0 errors. Full solution: 9 expected test errors (Handler property removed)

**Phase 3: Naming Change** -- COMPLETE
- Changed `$"{evt.Name}Interceptor"` to `evt.Name` in `BuildInterceptorProperties` and `BuildEventImplementation`
- Verification: Generator project 0 errors. Full solution: 72 expected test errors (suffix removed + Handler removed)

**Phase 4: Fix Tests** -- COMPLETE
- Updated 9 test files: replaced `Interceptor` suffix with bare names, `Handler?.Invoke` with `Raise()`
- No delegate stub tests modified (out of scope)
- EventTests.cs (standalone/Flat pattern) passes without changes
- Verification: `dotnet test src/KnockOff.sln` -- 6,650 tests, 0 failures

**Phase 5: Design Project Updates** -- COMPLETE
- Rewrote `EventPatterns.cs`: removed all `DID NOT DO THIS`/`REJECTED PATTERN`/`WHY NOT` comments, updated to use `Raise()`, `HasSubscribers`, bare names
- Rewrote `EventBasicsTests.cs`: updated to use `Raise()`, `HasSubscribers`, bare names, renamed test methods to avoid CA1030
- Verification: `dotnet build Design.Stubs` -- 0 errors (acceptance criteria file compiles). `dotnet test Design.Tests` -- 259 tests x 3 frameworks, 0 failures

**Phase 6: Documentation Updates** -- COMPLETE
- `docs/guides/events.md` -- already uses new API, no changes needed
- `docs/guides/api-consistency-matrix.md` -- removed outdated note about inline stubs using `Interceptor` suffix (line 148)

**Phase 7: Skill File Updates** -- COMPLETE
- All 4 skill files already use correct API (`Raise()`, `HasSubscribers`, bare names). No changes needed.

---

## Completion Evidence

- **Tests Passing:** Full solution `dotnet test src/KnockOff.sln` -- 6,650 tests, 0 failures across 4 test projects and 3 target frameworks (net8.0, net9.0, net10.0):
  - KnockOffTests.AssemblyStrict: 14 x 3 = 42
  - KnockOff.Documentation.Samples: 571 x 3 = 1,713
  - KnockOff.NeatooInterfaceTests: 473 x 3 = 1,419
  - KnockOffTests: 1159/1158/1159 = 3,476
- **Design Projects Compile:** Yes -- `dotnet build Design.Stubs` succeeds with 0 errors across all 3 target frameworks. All 10 acceptance criteria in `EventApiConsistencyVerification.cs` compile.
- **Design Tests Pass:** Yes -- `dotnet test Design.Tests` -- 259 tests x 3 frameworks = 777 tests, 0 failures
- **All Contract Items:** Confirmed complete -- all 44 checklist items checked
- **No Stop Conditions Triggered:** No out-of-scope test failures, no architectural contradictions, EventTests.cs passes without changes
- **Files Modified:**
  - CREATED: `src/Generator/Builder/EventBuilderHelpers.cs`
  - MODIFIED (Generator): `InlineEventModel.cs`, `InlineClassStubModel.cs`, `InlineModelBuilder.cs`, `ClassModelBuilder.cs`, `StandaloneClassModelBuilder.cs`, `InlineRenderer.cs`, `ClassRenderer.cs`, `StandaloneClassRenderer.cs`
  - MODIFIED (Tests): `BclInterfaceTests.cs`, `NeatooTests.cs`, `StandaloneClassStubTests.cs`, `INotifyNeatooPropertyChangedTests.cs`, `IValidatePropertyTests.cs`, `IEntityPropertyManagerTests.cs`, `IValidatePropertyManagerTests.cs`, `IEntityListBaseTests.cs`, `IValidateListBaseTests.cs`
  - MODIFIED (Design): `EventPatterns.cs`, `EventBasicsTests.cs`
  - MODIFIED (Docs): `api-consistency-matrix.md`

---

## Architect Verification

**Verified:** 2026-02-06
**Verdict:** VERIFIED

### Independent Test Results

All builds and tests executed independently by the architect (not trusting developer-reported results):

| Project | Build | Tests | Result |
|---------|-------|-------|--------|
| `dotnet build src/KnockOff.sln` | 0 errors, 0 warnings | N/A | PASS |
| KnockOffTests.AssemblyStrict | N/A | 14 x 3 = 42 passed, 0 failed | PASS |
| KnockOff.Documentation.Samples | N/A | 571 x 3 = 1,713 passed, 0 failed | PASS |
| KnockOff.NeatooInterfaceTests | N/A | 473 x 3 = 1,419 passed, 0 failed | PASS |
| KnockOffTests | N/A | 1158+1159+1159 = 3,476 passed, 0 failed | PASS |
| `dotnet build Design.Stubs` | 0 errors, 0 warnings | N/A | PASS |
| `dotnet test Design.Tests` | N/A | 259 x 3 = 777 passed, 0 failed | PASS |
| **Total** | **0 errors** | **6,650 + 777 = 7,427 passed, 0 failed** | **PASS** |

### Design Match

Generated code spot-checked for 3 patterns against the plan's design:

**Pattern 5 (Inline Interface) -- `EventPatternsDemo.Stubs.g.cs`:**
- Private `_handler` field: Confirmed (line 546) -- not public Handler property
- `HasSubscribers` property: Confirmed (line 552)
- `Raise()` method: Confirmed with correct signatures for all 4 delegate kinds:
  - EventHandler: `Raise(object? sender, EventArgs e)` (line 561)
  - EventHandler<T>: `Raise(object? sender, DataEventArgs e)` (line 660)
  - Action: `Raise()` (line 759)
  - Action<T,T>: `Raise(string arg1, int arg2)` (line 858)
- Bare property names: `Started` (line 949), `DataReceived` (line 952), `Completed` (line 955), `Progress` (line 958) -- no `Interceptor` suffix

**Pattern 3 (Standalone Class) -- `EventServiceBaseStub.g.cs`:**
- Private `_handler` field: Confirmed (line 491)
- `HasSubscribers` property: Confirmed (line 498)
- `Raise()` methods: EventHandler (line 507), Action (line 603)
- Bare property names: `StatusChanged` (line 689), `Completed` (line 691)

**Pattern 8 (Open Generic Interface) -- `OpenGenericEventDemo.Stubs.g.cs`:**
- Private `_handler` field: Confirmed (line 579)
- `HasSubscribers` property: Confirmed (line 585)
- `Raise()` methods: EventHandler (line 594), EventHandler<GenericDataEventArgs<T>> (line 693) -- correctly parameterized with generic type
- Bare property names: `StatusChanged` (line 781), `DataReceived` (line 784) -- no `Interceptor` suffix

### Additional Verification

- No `DID NOT DO THIS` comments about Raise/HasSubscribers in Design.Stubs event files: Confirmed (grep returned no matches)
- No `Handler?.Invoke` references remain in Design.Stubs event files: Confirmed
- No `Interceptor suffix` references remain in `api-consistency-matrix.md`: Confirmed
- All 15 acceptance criteria methods in `EventApiConsistencyVerification.cs` compile: Confirmed
- Acceptance criteria cover Patterns 3, 5, and 8 as designed: Confirmed
