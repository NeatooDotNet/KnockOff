# Delegate-Based Call API

**Date:** 2026-02-19
**Related Todo:** [IntelliSense API Redesign](../todos/intellisense-api-redesign.md) (successor work)
**Status:** Awaiting Verification
**Last Updated:** 2026-02-19

---

## Overview

The v0.52.0 IntelliSense API redesign introduced `Func<(T1, T2), TReturn>` / `Action<(T1, T2)>` for 2+ parameter Call callbacks. This causes CS0121 (ambiguous overload) when an interface has overloaded methods whose parameter tuples differ only in element count with the same element types. The fix: replace Func/Action+tuple with custom named delegates for ALL Call callbacks, add rich XML comments showing full method signatures with return types, and shorten generated type names to be method-name-based.

### The Bug

```csharp
public interface IAuthSvc
{
    bool ValidateCredentials(string username, string password);
    bool ValidateCredentials(string username, string password, string token);
}
```

Current generated overloads:
```csharp
Call(Func<(string username, string password), bool> callback)
Call(Func<(string username, string password, string token), bool> callback)
```

When a user writes `stub.ValidateCredentials.Call(args => args.username == "admin")`, C# cannot determine which `Func<tuple, bool>` the lambda targets. This is CS0121: ambiguous call between `Func<(string, string), bool>` and `Func<(string, string, string), bool>`.

### Why Tuples Fail for Overload Resolution

C# lambda type inference does not consider tuple element count or names. Both `Func<(string, string), bool>` and `Func<(string, string, string), bool>` are valid targets for an untyped lambda `args => ...`. The compiler cannot infer which one without explicit tuple type annotation, which defeats the purpose of the IntelliSense improvement.

This is NOT limited to same-type params. Any two overloads where the lambda body does not use a member that disambiguates (which is the common case for simple callbacks) will be ambiguous.

### Root Cause

`UnifiedInterceptorBuilder.BuildCallDelegateType()` and `BuildOverloadSignature()` use `Func<tuple, TReturn>` / `Action<tuple>` for 2+ param methods without ref/out. The overload resolution in the generated interceptor class depends on the C# compiler distinguishing these Func/Action types by their tuple argument, which it cannot do from a bare lambda.

---

## Design

### Design Decision 1: Custom Delegates for ALL Call Callbacks

**Replace Func/Action+tuple with per-method custom delegates for Call callbacks.** This applies to ALL methods, not just overloaded ones. Consistency is more important than optimization.

**Current (v0.52.0 -- entry point is already `Call`, but tuple-based delegates cause CS0121):**
```csharp
// Generated: single-signature, 2+ params
public MethodCallBuilderImpl Call(Func<(int a, int b), int> callback)

// Generated: overloaded, 2+ params -- CS0121 ambiguous when same-type overloads exist
public MethodCallBuilderImpl_String_String Call(Func<(string username, string password), bool> callback)
public MethodCallBuilderImpl_String_String_String Call(Func<(string username, string password, string token), bool> callback)
```

**New:**
```csharp
// Generated: single-signature
/// <summary>Configures callback for Add(int a, int b) -> int.</summary>
public AddImpl Call(AddDelegate callback)

// Generated: overloaded
/// <summary>Configures callback for ValidateCredentials(string username, string password) -> bool.</summary>
public ValidateCredentialsImpl Call(ValidateCredentialsDelegate callback)
```

**Rationale:**
- Custom delegates carry parameter names in their signature, giving full IntelliSense
- Custom delegates are always unambiguous (each method gets its own delegate type)
- Parameter names appear in IntelliSense tooltip when writing the lambda
- Consistent across all methods, not just overloaded ones
- Supports ref/out natively (already does for ref/out today)

### Design Decision 2: Delegate Naming Convention

**Method-name-based, with numbered suffixes for overloads.** This extends the existing convention already used for ref/out parameters (e.g., `TryGetValueDelegate`, `IncrementDelegate`, `SwapDelegate`) to ALL methods.

| Scenario | Delegate Name |
|----------|---------------|
| Single method | `{MethodName}Delegate` |
| Overload (first) | `{MethodName}Delegate` |
| Overload (second) | `{MethodName}Delegate2` |
| Overload (third) | `{MethodName}Delegate3` |
| Void single method | `{MethodName}Delegate` |
| Async method | `{MethodName}Delegate` (full Task signature), `{MethodName}SyncDelegate` (unwrapped) |

**Ordering rule:** Overloads are numbered by parameter count (ascending), then by parameter type lexicographic order if counts match. The overload with the fewest parameters gets no suffix (just `Delegate`).

**Examples:**
```csharp
// For IAuthSvc.ValidateCredentials overloads:
//   ValidateCredentials(string, string) -> bool
//   ValidateCredentials(string, string, out string) -> bool
//   ValidateCredentials(string, string, out string, int) -> bool

public delegate bool ValidateCredentialsDelegate(string username, string password);
public delegate bool ValidateCredentialsDelegate2(string username, string password, out string token);
public delegate bool ValidateCredentialsDelegate3(string username, string password, out string token, int timeout);
```

```csharp
// For IFormatter.Format overloads:
//   Format(string) -> string
//   Format(string, FormatOptions) -> string
//   Format(string, FormatOptions, int) -> string

public delegate string FormatDelegate(string input);
public delegate string FormatDelegate2(string input, FormatOptions options);
public delegate string FormatDelegate3(string input, FormatOptions options, int maxLength);
```

```csharp
// For ICalculator.Add (single method, not overloaded):
//   Add(int, int) -> int

public delegate int AddDelegate(int a, int b);
```

**Complete delegate types generated per method (including predicates from DD7):**

| Scenario | Delegate types generated |
|----------|------------------------|
| Non-overloaded sync, 2+ params | `{MethodName}Delegate` + `{MethodName}Predicate` |
| Non-overloaded sync, 0-1 params | `{MethodName}Delegate` (no predicate needed -- 0 params has no When predicate, 1 param uses raw `Func<T, bool>`) |
| Non-overloaded async, 2+ params | `{MethodName}Delegate` + `{MethodName}SyncDelegate` + `{MethodName}Predicate` |
| Non-overloaded async, 0-1 params | `{MethodName}Delegate` + `{MethodName}SyncDelegate` |
| Overloaded sync (Nth overload), 2+ params | `{MethodName}DelegateN` + `{MethodName}PredicateN` |
| Overloaded async (Nth overload), 2+ params | `{MethodName}DelegateN` + `{MethodName}SyncDelegateN` + `{MethodName}PredicateN` |

**Example -- IAuthSvc with overloaded async + sync methods:**
```csharp
// bool ValidateCredentials(string username, string password)
// bool ValidateCredentials(string username, string password, string token)
// Task<string> TransformAsync(string input)
// Task<string> TransformAsync(string input, CancellationToken ct)

// ValidateCredentials overload 1 (2 delegates):
public delegate bool ValidateCredentialsDelegate(string username, string password);
public delegate bool ValidateCredentialsPredicate(string username, string password);

// ValidateCredentials overload 2 (2 delegates):
public delegate bool ValidateCredentialsDelegate2(string username, string password, string token);
public delegate bool ValidateCredentialsPredicate2(string username, string password, string token);

// TransformAsync overload 1 (2 delegates -- 1 param, no predicate needed):
public delegate Task<string> TransformAsyncDelegate(string input);
public delegate string TransformAsyncSyncDelegate(string input);

// TransformAsync overload 2 (3 delegates -- 2 params, predicate needed):
public delegate Task<string> TransformAsyncDelegate2(string input, CancellationToken ct);
public delegate string TransformAsyncSyncDelegate2(string input, CancellationToken ct);
public delegate bool TransformAsyncPredicate2(string input, CancellationToken ct);
```

### Design Decision 3: Builder/Sequence Type Naming

**Builder and sequence types get method-name-based names.**

| Current | New |
|---------|-----|
| `MethodCallBuilderImpl` | `{MethodName}Impl` |
| `MethodCallBuilderImpl_{Suffix}` | `{MethodName}Impl` / `{MethodName}Impl2` |
| `MethodSequenceImpl` | `{MethodName}Sequence` |
| `MethodSequenceImpl_{Suffix}` | `{MethodName}Sequence` / `{MethodName}Sequence2` |

**Same numbering as delegates:** `Impl2` corresponds to `Delegate2` (same overload).

### Design Decision 4: Rich XML Comments with Return Type

**XML doc `<summary>` on Call methods shows the full method signature INCLUDING return type.**

**Current:**
```csharp
/// <summary>Configures callback for ValidateCredentials(string username, string password). Returns builder for sequence chaining.</summary>
public MethodCallBuilderImpl_String_String_Boolean Call(Func<(string username, string password), bool> callback)
```

**New:**
```csharp
/// <summary>Configures callback for ValidateCredentials(string username, string password) -> bool.</summary>
public ValidateCredentialsImpl Call(ValidateCredentialsDelegate callback)
```

The XML doc tooltip is the primary IntelliSense mechanism. When a user types `stub.ValidateCredentials.Call(` and hovers, they see the full signature with parameter names AND return type. The delegate's own parameter list provides secondary IntelliSense when writing the lambda body.

**Format:**
- Non-void: `{MethodName}({params}) -> {ReturnType}`
- Void: `{MethodName}({params})`
- Async with simplified: `{MethodName}({params}) -> {InnerType}. Result auto-wrapped in Task.FromResult.`

### Design Decision 5: Entry Point Naming (Already Done -- No Change Needed)

**The `Call()` entry point unification was already completed in v0.52.0.** All callback entry points already use `Call()` in the live renderer path (`RenderBaseClassContent` at line 827 of `MethodInterceptorRenderer.cs`). This includes both void and non-void single-signature methods. The verb distinction is already:
- `Call(callback)` -- configure behavior via callback/delegate
- `Return(value)` -- configure a constant return value

**Evidence:** `src/Design/Design.Stubs/Methods/BasicMethods.cs` line 78 uses `stub.Add.Call(args => args.a + args.b)`.

**Note for implementer:** The dead code method `RenderSingleSignatureContent` (lines 84-429) still contains the old `Return(callback)` pattern at line 206. This is dead code being removed as part of this plan (see Phase 0). Do NOT mistake it for the live code path.

### Design Decision 6: LastArgs Stays Tuple-Based

**LastArgs remains a named tuple.** Tuple-based `LastArgs` has no overload resolution issues because it is a read-only property, not a method call with lambda inference. Users destructure it: `var (username, password) = tracking.LastArgs;`.

No change to LastArg (single-parameter) or LastArgs (multi-parameter).

### Design Decision 7: When Predicates Use Custom Delegates

**When predicates use custom delegates for full consistency.** Same pattern as Call callbacks:

```csharp
// For IAuthSvc.ValidateCredentials overloads:
public delegate bool ValidateCredentialsPredicate(string username, string password);
public delegate bool ValidateCredentialsPredicate2(string username, string password, string token);

/// <summary>Configures parameter matching for ValidateCredentials(string username, string password) -> bool.</summary>
public WhenBuilder When(ValidateCredentialsPredicate predicate) { ... }

/// <summary>Configures parameter matching for ValidateCredentials(string username, string password, string token) -> bool.</summary>
public WhenBuilder2 When(ValidateCredentialsPredicate2 predicate) { ... }
```

When exact-match stays as individual parameters (already unambiguous by parameter count). Only the predicate form (`When(predicate)`) switches to custom delegates.

### Design Decision 8: Async Simplified Callbacks

For async methods that return `Task<T>` or `ValueTask<T>`, KnockOff generates a "simplified" callback that accepts the unwrapped return type. With custom delegates:

**Non-overloaded async method:**
```csharp
// Full async delegate
public delegate Task<string> TransformAsyncDelegate(string input);

// Simplified delegate (auto-wrapped)
public delegate string TransformAsyncSyncDelegate(string input);
```

**XML doc for simplified:**
```csharp
/// <summary>Configures callback for TransformAsync(string input) -> string. Result auto-wrapped in Task.FromResult.</summary>
public TransformAsyncImpl Call(TransformAsyncSyncDelegate callback)
```

**Overloaded async methods (e.g., IFormatter.TransformAsync):**
```csharp
// TransformAsync(string input) -> Task<string>
public delegate Task<string> TransformAsyncDelegate(string input);
public delegate string TransformAsyncSyncDelegate(string input);

// TransformAsync(string input, CancellationToken cancellationToken) -> Task<string>
public delegate Task<string> TransformAsyncDelegate2(string input, CancellationToken cancellationToken);
public delegate string TransformAsyncSyncDelegate2(string input, CancellationToken cancellationToken);

// Entry points -- 4 Call overloads total, each with a unique delegate type
/// <summary>Configures callback for TransformAsync(string input) -> Task&lt;string&gt;.</summary>
public TransformAsyncImpl Call(TransformAsyncDelegate callback) { ... }
/// <summary>Configures callback for TransformAsync(string input) -> string. Result auto-wrapped in Task.</summary>
public TransformAsyncImpl Call(TransformAsyncSyncDelegate callback) { ... }
/// <summary>Configures callback for TransformAsync(string input, CancellationToken cancellationToken) -> Task&lt;string&gt;.</summary>
public TransformAsyncImpl2 Call(TransformAsyncDelegate2 callback) { ... }
/// <summary>Configures callback for TransformAsync(string input, CancellationToken cancellationToken) -> string. Result auto-wrapped in Task.</summary>
public TransformAsyncImpl2 Call(TransformAsyncSyncDelegate2 callback) { ... }
```

**Numbering rule:** The overload number suffix applies consistently to ALL generated types for that overload: `Delegate2`, `SyncDelegate2`, `Impl2`, `Sequence2`. The number always corresponds to the same overload.

### Design Decision 9: Interaction with Interceptor Base Class Plan

The interceptor base class plan (docs/plans/interceptor-base-class-generator.md) uses `TArgs` as a ValueTuple internally for storage. This is unaffected by the delegate change. The delegate is only for the user-facing Call API surface. Internally, arguments are still packed into tuples for `RecordCall`, sequence tracking, and When chain evaluation.

**Pipeline:**
1. User writes: `stub.Add.Call((int a, int b) => a + b)` -- uses `AddDelegate` delegate
2. Generated Invoke method unpacks: `_call(a, b)` and records `RecordCall((a, b))` -- tuple storage
3. LastArgs returns: `(int a, int b)` -- tuple read-only

The delegate is just the entry surface. Everything downstream stays tuple-based.

---

## API Surface Summary

### Non-Overloaded Method (non-void)

```csharp
// Interface: int Add(int a, int b)
public sealed class AddInterceptor
{
    /// <summary>Callback delegate for Add(int a, int b) -> int.</summary>
    public delegate int AddDelegate(int a, int b);

    /// <summary>Configures callback for Add(int a, int b) -> int.</summary>
    public AddImpl Call(AddDelegate callback) { ... }

    /// <summary>Sets return value for Add(int a, int b) -> int.</summary>
    public AddImpl Return(int value) { ... }

    /// <summary>Sets return values as a sequence for Add(int a, int b) -> int.</summary>
    public AddSequence Return(int first, params int[] rest) { ... }

    // When chains (predicate uses custom delegate)
    /// <summary>Predicate delegate for Add(int a, int b) -> int.</summary>
    public delegate bool AddPredicate(int a, int b);

    /// <summary>Configures parameter matching for Add(int a, int b) -> int.</summary>
    public WhenBuilder When(int a, int b) { ... }
    /// <summary>Configures parameter matching for Add(int a, int b) -> int.</summary>
    public WhenBuilder When(AddPredicate predicate) { ... }

    // Builder/Sequence types
    public sealed class AddImpl : IMethodReturnBuilderArgs<AddDelegate, (int? a, int? b)> { ... }
    public sealed class AddSequence : IMethodReturnSequence<AddDelegate> { ... }
}
```

### Non-Overloaded Method (void)

```csharp
// Interface: void Process(string data)
public sealed class ProcessInterceptor
{
    /// <summary>Callback delegate for Process(string data).</summary>
    public delegate void ProcessDelegate(string data);

    /// <summary>Configures callback for Process(string data).</summary>
    public ProcessImpl Call(ProcessDelegate callback) { ... }

    public sealed class ProcessImpl : IMethodCallBuilder<ProcessDelegate, string> { ... }
    public sealed class ProcessSequence : IMethodCallSequence<ProcessDelegate> { ... }
}
```

### Overloaded Methods

```csharp
// Interface:
//   bool ValidateCredentials(string username, string password)
//   bool ValidateCredentials(string username, string password, string token)
public sealed class ValidateCredentialsInterceptor
{
    /// <summary>Callback delegate for ValidateCredentials(string username, string password) -> bool.</summary>
    public delegate bool ValidateCredentialsDelegate(string username, string password);

    /// <summary>Callback delegate for ValidateCredentials(string username, string password, string token) -> bool.</summary>
    public delegate bool ValidateCredentialsDelegate2(string username, string password, string token);

    /// <summary>Predicate delegate for ValidateCredentials(string username, string password) -> bool.</summary>
    public delegate bool ValidateCredentialsPredicate(string username, string password);

    /// <summary>Predicate delegate for ValidateCredentials(string username, string password, string token) -> bool.</summary>
    public delegate bool ValidateCredentialsPredicate2(string username, string password, string token);

    /// <summary>Configures callback for ValidateCredentials(string username, string password) -> bool.</summary>
    public ValidateCredentialsImpl Call(ValidateCredentialsDelegate callback) { ... }

    /// <summary>Configures callback for ValidateCredentials(string username, string password, string token) -> bool.</summary>
    public ValidateCredentialsImpl2 Call(ValidateCredentialsDelegate2 callback) { ... }

    // Return(value) -- only when return type is unique across overloads (bool is same, so skipped here)

    // When exact match -- individual params, unambiguous
    public WhenBuilder When(string username, string password) { ... }
    public WhenBuilder2 When(string username, string password, string token) { ... }

    // When predicate -- custom delegates, unambiguous
    /// <summary>Configures parameter matching for ValidateCredentials(string username, string password) -> bool.</summary>
    public WhenBuilder When(ValidateCredentialsPredicate predicate) { ... }
    /// <summary>Configures parameter matching for ValidateCredentials(string username, string password, string token) -> bool.</summary>
    public WhenBuilder2 When(ValidateCredentialsPredicate2 predicate) { ... }

    public sealed class ValidateCredentialsImpl : IMethodReturnBuilderArgs<ValidateCredentialsDelegate, (string? username, string? password)> { ... }
    public sealed class ValidateCredentialsImpl2 : IMethodReturnBuilderArgs<ValidateCredentialsDelegate2, (string? username, string? password, string? token)> { ... }
}
```

### Zero-Parameter Method

```csharp
// Interface: string GetStatus()
public sealed class GetStatusInterceptor
{
    /// <summary>Callback delegate for GetStatus() -> string.</summary>
    public delegate string GetStatusDelegate();

    /// <summary>Configures callback for GetStatus() -> string.</summary>
    public GetStatusImpl Call(GetStatusDelegate callback) { ... }

    /// <summary>Sets return value for GetStatus() -> string.</summary>
    public GetStatusImpl Return(string value) { ... }
}
```

### Single-Parameter Method

```csharp
// Interface: User? GetUser(int userId)
public sealed class GetUserInterceptor
{
    /// <summary>Callback delegate for GetUser(int userId) -> User?.</summary>
    public delegate User? GetUserDelegate(int userId);

    /// <summary>Configures callback for GetUser(int userId) -> User?.</summary>
    public GetUserImpl Call(GetUserDelegate callback) { ... }

    /// <summary>Sets return value for GetUser(int userId) -> User?.</summary>
    public GetUserImpl Return(User? value) { ... }
}
```

### Async Method with Simplified Callback

```csharp
// Interface: Task<string> GetDataAsync(int id)
public sealed class GetDataAsyncInterceptor
{
    /// <summary>Callback delegate for GetDataAsync(int id) -> Task&lt;string&gt;.</summary>
    public delegate Task<string> GetDataAsyncDelegate(int id);

    /// <summary>Simplified callback delegate for GetDataAsync(int id) -> string. Result auto-wrapped in Task.FromResult.</summary>
    public delegate string GetDataAsyncSyncDelegate(int id);

    /// <summary>Configures callback for GetDataAsync(int id) -> Task&lt;string&gt;.</summary>
    public GetDataAsyncImpl Call(GetDataAsyncDelegate callback) { ... }

    /// <summary>Configures callback for GetDataAsync(int id) -> string. Result auto-wrapped in Task.FromResult.</summary>
    public GetDataAsyncImpl Call(GetDataAsyncSyncDelegate callback) { ... }

    /// <summary>Sets return value for GetDataAsync(int id) -> string. Result auto-wrapped in Task.FromResult.</summary>
    public GetDataAsyncImpl Return(string value) { ... }
}
```

### ref/out Method

```csharp
// Interface: bool TryParse(string input, out int result)
public sealed class TryParseInterceptor
{
    /// <summary>Callback delegate for TryParse(string input, out int result) -> bool.</summary>
    public delegate bool TryParseDelegate(string input, out int result);

    /// <summary>Configures callback for TryParse(string input, out int result) -> bool.</summary>
    public TryParseImpl Call(TryParseDelegate callback) { ... }
}
```

---

## Impact on Existing API

### Breaking Changes (pre-1.0, acceptable)

| Change | Before (v0.52.0) | After |
|--------|-------------------|-------|
| 2+ param callbacks | `args => args.a + args.b` (tuple accessor) | `(int a, int b) => a + b` (direct params) |
| Builder type names | `MethodCallBuilderImpl` | `AddImpl` |
| Sequence type names | `MethodSequenceImpl` | `AddSequence` |
| Delegate type (all methods) | `Func<(int a, int b), int>` | `AddDelegate` (custom delegate) |
| Delegate type (overloads) | `Func<(string, string), bool>` | `ValidateCredentialsDelegate` |
| When predicate type (2+ params) | `Func<(int a, int b), bool>` | `AddPredicate` (custom predicate delegate) |

**Note:** The `Call(callback)` entry point name does NOT change -- it is already `Call()` as of v0.52.0.

### What Does NOT Change

| Feature | Status |
|---------|--------|
| `stub.Method` as property (interceptor-as-property) | No change |
| `Return(value)` for constant values | No change |
| `Return(first, params rest)` for value sequences | No change |
| `LastArg` / `LastArgs` (tuple-based) | No change |
| `Verify()` / `Verifiable()` | No change |
| `When(exactValues)` | No change |
| `When(predicate)` | Delegate type changes: `Func<tuple, bool>` -> custom predicate delegate (e.g., `AddPredicate`) |
| `ThenReturn(value)` / `ThenReturn(callback)` | No change in semantics, delegate type changes |
| `ThenCall(callback)` | No change in semantics, delegate type changes |
| `Reset()` | No change |
| `Source(T)` | No change |
| `Strict` mode | No change |
| Property interceptors | No change (already clean) |
| Indexer interceptors | No change (already clean) |
| Event interceptors | No change |

---

## Pattern-by-Pattern Analysis

### Patterns 1-2: Standalone / Generic Standalone

**Pipeline:** `FlatModelBuilder` -> `FlatRenderer` -> `MethodInterceptorRenderer`

Changes in `UnifiedInterceptorBuilder`:
- `BuildCallDelegateType()`: Always return custom delegate name (no more Func/Action)
- `NeedsCustomDelegate()`: Always return true (or remove the concept -- every method gets a delegate)
- `BuildCustomDelegateSignature()`: Generate for every method, not just ref/out
- `BuildOverloadSignature()`: Always use custom delegate

Changes in `MethodInterceptorRenderer`:
- **Entry point already uses `Call()` -- no rename needed** (DD5 already done in v0.52.0)
- Delegate type changes from `Func<tuple, T>` / `Action<tuple>` to custom named delegates
- Builder class name: `MethodCallBuilderImpl` -> `{MethodName}Impl`
- Sequence class name: `MethodSequenceImpl` -> `{MethodName}Sequence`
- XML docs: Add `-> {ReturnType}` to summary

Changes in `ModelAdapters.ToUnifiedModel()`:
- Same logic changes as UnifiedInterceptorBuilder for flat model path

### Pattern 3-4: Standalone Class / Generic Standalone Class

**Pipeline:** `StandaloneClassModelBuilder` -> `StandaloneClassRenderer` -> `MethodInterceptorRenderer`

Same changes as Patterns 1-2. The `MethodInterceptorRenderer` is shared.

### Patterns 5-6: Inline Interface / Inline Class

**Pipeline:** `InlineModelBuilder` -> `InlineRenderer` -> `MethodInterceptorRenderer`

Same changes. The `MethodInterceptorRenderer` is shared.

### Pattern 7: Inline Delegate

**Pipeline:** `InlineModelBuilder` (delegate path) -> `InlineRenderer` -> `MethodInterceptorRenderer`

The delegate stub pattern generates a single interceptor for the delegate's Invoke signature. Same changes apply.

### Patterns 8-9: Open Generic Interface / Open Generic Class

**Pipeline:** `InlineModelBuilder` -> `InlineRenderer` -> `MethodInterceptorRenderer`

Same changes. Generic type parameters flow through to the delegate signature naturally:
```csharp
// IGenericService<T>.Process(T item) -> T
public delegate T ProcessDelegate(T item);
```

### Generic Methods (Of<T>() handlers)

Generic methods use a SEPARATE rendering path (`RenderGenericMethodHandler` / `RenderInlineTypedHandlerClass`). These already use custom delegates with the `{MethodName}Delegate` naming convention. **No naming change needed for generic method handler delegates** -- the existing convention matches this plan's convention.

Entry point already uses `Call` (no change needed).

Builder/sequence type names in generic method handlers should follow the same `{MethodName}Impl` / `{MethodName}Sequence` pattern for consistency.

---

## Implementation Phases

### Phase 0: Dead Code Cleanup and Regression Test Setup

**Dead Code Removal:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`
  - Delete `RenderSingleSignatureContent` method (lines 84-429, ~346 lines). This is dead code: the live path is `RenderBaseClassContent` (called at line 62). The dead method contains the old `Return(callback)` pattern and inline field management that was replaced by the base-class approach in v0.52.0.
  - Verify build succeeds after deletion (confirms nothing calls this method)

**Regression Test Interface:**
- Create a new test interface that **actually triggers CS0121** with the current tuple-based delegates. Neither `IAuthSvcMethods` (has `out` params, already gets custom delegates) nor `IFormatter` (different types per overload, tuples are distinguishable) triggers this bug.
- Add to `src/Tests/KnockOff.Documentation.Samples/MethodsSamples.cs` (or a new file):
  ```csharp
  public interface IOverloadSameTypes
  {
      bool Check(string a, string b);
      bool Check(string a, string b, string c);
  }
  ```
- Add a standalone stub and test that exercises `stub.Check.Call(...)` to confirm CS0121 occurs before the fix and succeeds after. This test will NOT compile initially (that is the point -- it proves the bug). Mark it with a comment `// CS0121 regression test -- should compile after delegate-based call API fix`.
- **Alternative:** If adding a non-compiling test is impractical in the test project, add the interface and stub definition, then add the Call usage in Phase 3 after the fix is in place. Document the CS0121 error message from a manual build attempt as evidence.

### Phase 1: Builder/Model Changes

**Files:**
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs`
  - `NeedsCustomDelegate()`: Return true always (or remove)
  - `BuildCallDelegateType()`: Always build custom delegate name
  - `BuildCustomDelegateSignature()`: Always generate
  - `BuildOverloadSignature()`: Always use custom delegate
  - Add overload numbering logic (sort by param count, then lex order, assign suffix numbers)
  - New: delegate naming method (`{MethodName}Delegate`, `{MethodName}Delegate2`, etc.)
  - New: predicate naming method (`{MethodName}Predicate`, `{MethodName}Predicate2`, etc.)
  - New: builder naming method (`{MethodName}Impl`, `{MethodName}Impl2`, etc.)
  - New: sequence naming method (`{MethodName}Sequence`, `{MethodName}Sequence2`, etc.)
  - `BuildWhenPredicateType()`: Change from `Func<tuple, bool>` to custom predicate delegate name

- `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs`
  - Add `DelegateFriendlyName` field (the `{MethodName}Delegate` name)
  - Add `PredicateFriendlyName` field (the `{MethodName}Predicate` name)
  - Add `PredicateDelegateSignature` field (the `delegate bool {MethodName}Predicate(...)` signature)
  - Add `BuilderFriendlyName` field (the `{MethodName}Impl` name)
  - Add `SequenceFriendlyName` field (the `{MethodName}Sequence` name)
  - `UsesTupleCallDelegate` field: Remove (no longer needed)

- `src/Generator/Model/Shared/MethodOverloadSignature.cs` (via `MethodOverloadSignature` record)
  - Add `DelegateFriendlyName`, `PredicateFriendlyName`, `PredicateDelegateSignature`, `BuilderFriendlyName`, `SequenceFriendlyName`
  - `UsesTupleCallDelegate`: Remove

- `src/Generator/Renderer/Shared/ModelAdapters.cs`
  - Update `ToUnifiedModel()` and `BuildMultiOverloadModel()` to use custom delegates always
  - Add overload numbering logic
  - Populate `PredicateFriendlyName` and `PredicateDelegateSignature` for each method/overload

### Phase 2: Renderer Changes

**Files:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`
  - **Note:** Entry point is already `Call()` -- no rename needed (DD5 already done in v0.52.0)
  - Replace all `MethodCallBuilderImpl` with `{model.BuilderFriendlyName}` (or overload-specific)
  - Replace all `MethodSequenceImpl` with `{model.SequenceFriendlyName}` (or overload-specific)
  - XML docs: Add `-> {ReturnType}` pattern
  - Remove tuple-unpacking logic for `BuildDelegateCallArgs` / `BuildBaseClassDelegateCallArgs` (delegates receive individual params, not tuples)
  - Remove `CreateValueDelegate` tuple discard logic for 2+ params
  - Custom delegate naming in single-sig mode: `{MethodName}Delegate` delegate declaration
  - Simplified async delegate naming: `{MethodName}SyncDelegate` / `{MethodName}SyncDelegate2`
  - Overload group: `{MethodName}Delegate`, `{MethodName}Delegate2`, etc. plus corresponding `SyncDelegate` variants for async overloads
  - **When predicate delegate rendering:**
    - `BuildPredicateType()` (private, line ~4503): Change from `Func<tuple, bool>` to custom predicate delegate name from model (e.g., `AddPredicate`)
    - `BuildWhenPredicateType()` in `UnifiedInterceptorBuilder.cs` (line ~579): Same change -- return custom predicate delegate name instead of `Func<tuple, bool>`
    - `RenderBaseClassWhenEntryPoints()` (line ~982): Update to emit `When({PredicateFriendlyName} predicate)` and render predicate delegate declaration
    - `RenderBaseClassVoidWhenEntryPoints()` (line ~1032): Same update for void methods
    - `RenderWhenEntryPoints()` (line ~4441): Update overload-group When to emit `When({PredicateFriendlyName} predicate)` with per-overload predicate delegates
    - `RenderVoidWhenEntryPoints()` (line ~4581): Same update for void overload-group When
  - **Async wrapping lambda changes (tuple-based to individual params):**
    - `BuildAsyncWrapExpression()` (line ~922): Currently uses `BuildLambdaParamDecls` which returns `(TArgs args)` tuple for 2+ params. Must change to use `BuildDelegateMatchingParamDecls` which returns individual params `(int a, int b)`, and change `callback({callArgs})` from `callback(args)` to `callback(a, b)` using `BuildDelegateMatchingCallArgs`
    - `BuildVoidAsyncWrapExpression()` (line ~938): Same change -- tuple param `(TArgs args)` -> individual params `(int a, int b)`, and `callback(args)` -> `callback(a, b)`
    - `BuildLambdaParamDecls()` (line ~964) and `BuildLambdaCallArgs()` (line ~973): These methods encode the tuple-based convention. Either refactor them to use individual params or replace their call sites with `BuildDelegateMatchingParamDecls`/`BuildDelegateMatchingCallArgs`

- Generic method handlers (`InlineRenderer.cs`, `FlatRenderer.cs`, `ClassRenderer.cs`, `StandaloneClassRenderer.cs`):
  - Generic method handler delegates already use `{MethodName}Delegate` naming -- no rename needed
  - Builder/sequence type names in generic method handlers should use `{MethodName}Impl` / `{MethodName}Sequence` for consistency

### Phase 3: Consumer Code Updates

**~100+ test files and Design projects:**
- **Note:** `Call(callback)` entry point name is already correct -- no rename needed (DD5 already done in v0.52.0)
- `args => args.a + args.b` -> `(int a, int b) => a + b` for 2+ param callbacks (remove tuple accessor syntax, use direct delegate params)
- When predicate lambdas: `args => args.a > 5` -> `(int a, int b) => a > 5` for 2+ param predicates (same change as Call callbacks)
- Update type references in tests that mention `MethodCallBuilderImpl`, `MethodSequenceImpl`, etc. to new method-name-based names
- Add regression test for `IOverloadSameTypes` exercising `stub.Check.Call(...)` to confirm CS0121 is fixed

### Phase 4: Verification

- Build all projects
- Run all tests
- **Verify `IOverloadSameTypes` regression test compiles and passes** -- this is the primary proof the CS0121 bug is fixed (IAuthSvcMethods does NOT trigger CS0121 because its overloads use `out` params)
- Verify Design.Stubs compiles
- Verify Design.Tests passes
- Verify Documentation.Samples passes
- Verify `RenderSingleSignatureContent` dead code was removed in Phase 0 (no residual references)

---

## Architectural Verification

### Scope Table

| Pattern | Affected | Notes |
|---------|----------|-------|
| P1 Standalone | Yes | FlatModelBuilder + ModelAdapters + MethodInterceptorRenderer |
| P2 Generic Standalone | Yes | Same pipeline as P1, generic type params flow through delegate |
| P3 Standalone Class | Yes | StandaloneClassModelBuilder + StandaloneClassRenderer + MethodInterceptorRenderer |
| P4 Generic Standalone Class | Yes | Same pipeline as P3 |
| P5 Inline Interface | Yes | InlineModelBuilder + InlineRenderer + MethodInterceptorRenderer |
| P6 Inline Class | Yes | Same pipeline as P5 |
| P7 Inline Delegate | Yes | Same pipeline as P5 (delegate stub path) |
| P8 Open Generic Interface | Yes | Same pipeline as P5, generic params flow through |
| P9 Open Generic Class | Yes | Same pipeline as P5 |

### Breaking Changes

Yes -- pre-1.0, single consumer (Neatoo). All breaking changes are in the generated API surface:
1. Delegate types change from `Func<tuple, T>` / `Action<tuple>` to custom named delegates (e.g., `AddDelegate`)
2. Builder/Sequence type names change from generic (`MethodCallBuilderImpl`) to method-name-based (`AddImpl` / `AddSequence`)
3. 2+ param callback syntax changes from tuple accessor (`args => args.a + args.b`) to direct params (`(int a, int b) => a + b`)

**Note:** Entry point name `Call(callback)` does NOT change -- already `Call()` as of v0.52.0.

### Codebase Analysis

Files examined during design:
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- All delegate type construction logic
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- All rendering for method interceptors (single + overload)
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- Flat model to unified model conversion
- `src/Generator/Builder/InlineModelBuilder.cs` -- Inline method group building
- `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` -- Model fields
- `src/Design/Design.Stubs/Methods/MethodOverloads.cs` -- Current overload API usage
- `src/Design/Design.Stubs/Generated/.../MethodOverloadsDemo.Stubs.g.cs` -- Current generated overload code
- `src/Tests/KnockOff.Documentation.Samples/MethodsSamples.cs` -- Current API samples with IAuthSvcMethods
- `src/Tests/.../AuthSvcMethodsStub.g.cs` -- Generated standalone overload code
- `src/Design/Design.Domain/Services/IFormatter.cs` -- Overload domain interface

---

## Open Questions — Resolved (2026-02-19)

1. **When predicates:** **Custom delegates.** Full consistency — When predicates get custom delegates too (e.g., `ValidateCredentialsPredicate`, `ValidateCredentialsPredicate2`).

2. **Simplified async delegates:** **Shorter naming.** Use `{MethodName}SyncDelegate` instead of `{MethodName}SimplifiedDelegate`.

3. **ThenReturn/ThenCall delegate types:** **Reuse same delegate.** `ThenReturn(callback)` and `ThenCall(callback)` use the same `{MethodName}Delegate` — same signature, no proliferation.

4. **Overload numbering stability:** **Shifts are acceptable.** Users write lambdas/expressions (`(int a, int b) => a + b`), not delegate types directly. Numbering shifts on interface changes are fine pre-1.0.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Large consumer code churn (~100+ files) | High | Medium | Mechanical find-replace, no logic changes |
| Overload numbering instability | Low | Low | Pre-1.0, can change. Document in migration guide. |
| Generic method handler builder/sequence rename misses | Medium | Low | Search for `MethodCallBuilderImpl` and `MethodSequenceImpl` in all renderer files |
| When predicate ambiguity (same bug for When) | Low | Low | Fixed: custom predicate delegates (user decision 2026-02-19) |
| Builder interface type parameters change | Medium | Medium | IMethodReturnBuilderArgs<TDelegate, TArgs> -- TDelegate changes from Func to custom delegate |

---

## Developer Review

**Status:** Concerns Addressed (Architect Revision 3)
**Reviewed:** 2026-02-19

### My Understanding of This Plan

**Core Change:** Replace `Func<tuple, TReturn>` / `Action<tuple>` with custom named delegates for ALL Call callbacks, rename generated type names to method-name-based (`AddDelegate`, `AddImpl`, `AddSequence`), add `-> ReturnType` to XML docs.

**User-Facing API:** `stub.Method.Call(callback)` using custom delegate (e.g., `AddDelegate`), `stub.Method.Return(value)` unchanged. Overloads disambiguated by distinct delegate types (`ValidateCredentialsDelegate` vs `ValidateCredentialsDelegate2`).

**Internal Changes:** `UnifiedInterceptorBuilder` always generates custom delegates. `MethodInterceptorRenderer` uses method-name-based type names. `ModelAdapters` updated for flat pipeline. All consumer code updated.

**Patterns Affected:** All 9 patterns (all share `MethodInterceptorRenderer`).

### Codebase Investigation

**Files Examined:**
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- `NeedsCustomDelegate()` returns `true` only for ref/out. `BuildCallDelegateType()` builds `Func<tuple, T>` / `Action<tuple>` for 2+ params.
- `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` -- Model has `CallDelegateType`, `NeedsCustomDelegate`, `CustomDelegateSignature`, `UsesTupleCallDelegate`.
- `src/Generator/Model/Shared/MethodOverloadSignature.cs` -- Per-overload model with `DelegateName`, `DelegateSignature`, `SignatureSuffix`, `UsesTupleCallDelegate`.
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- (~5200+ lines). Two live rendering modes: `RenderBaseClassContent` (single-sig, inherits `MethodInterceptorRuntime`) and `RenderOverloadGroupContent` (multi-sig, self-contained). `RenderSingleSignatureContent` is DEAD CODE (defined at line 84, never called).
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- Flat model to unified model conversion with own `BuildFuncActionDelegateType()`.
- `src/Generator/Renderer/InlineRenderer.cs` lines 672+ -- Generic method handler rendering uses `{MethodName}Delegate` naming.
- `src/Generator/Renderer/FlatRenderer.cs` lines 852+ -- Flat generic method handler rendering also uses `{MethodName}Delegate`.
- `src/Design/Design.Stubs/Methods/BasicMethods.cs` -- Current API: `stub.Add.Call(args => args.a + args.b)` (already uses Call, not Return).
- `src/Design/Design.Stubs/Methods/MethodOverloads.cs` -- Current overload API with explicit tuple syntax.
- `src/Tests/KnockOff.Documentation.Samples/MethodsSamples.cs` -- IAuthSvcMethods has `out` params (already uses custom delegates). No test interface triggers CS0121.
- `src/Design/Design.Domain/Services/IFormatter.cs` -- IFormatter overloads have different types per overload (not CS0121 vulnerable).

**Design.Stubs Verification:**
- The architect did NOT provide Design.Stubs compilation evidence. No "Design Project Verification" section with "Verified | Needs Implementation" entries. However, this is a bug fix plan where the current code compiles and the bug scenario is not yet in the test suite. Noted but not a hard rejection.

**Discrepancies Found:**
- See Concern 1 below (Design Decision 5 is already implemented).
- See Concern 2 below (plan's "Current" examples don't match live code path).

### Concerns

#### 1. MAJOR: Design Decision 5 Is Already Implemented

Design Decision 5 says: "Unify all callback entry points to `Call()`" and presents `stub.Add.Return(callback)` -> `stub.Add.Call(callback)` as a change to make.

**This was already done in v0.52.0.** Evidence:

- The live renderer at `MethodInterceptorRenderer.cs` line 827 emits: `w.Line($"public MethodCallBuilderImpl Call({delegateType} callback)");` for ALL single-signature methods (both void and non-void).
- The dead code method `RenderSingleSignatureContent` (line 84) contains the old `Return(callback)` path (line 206: `var entryPointName = model.IsVoid ? "Call" : "Return";`), but this method is NEVER called. The live path is `RenderBaseClassContent` (called at line 62).
- Design.Stubs confirms: `stub.Add.Call(args => args.a + args.b)` (BasicMethods.cs line 78).

**Impact:** The plan's breaking changes table, Phase 3 consumer updates, and migration guide all include this non-change. This will confuse the implementer who tries to rename something that's already renamed.

**Question:** Should Design Decision 5 be removed entirely from the plan, or rewritten to note it was already done in v0.52.0 and is merely being preserved?

#### 2. MAJOR: Plan's "Current (broken)" Examples Don't Match Live Code

The plan shows (line 53-57):
```
// Generated: single-signature, 2+ params
public MethodCallBuilderImpl Return(Func<(int a, int b), int> callback)
```

But the actual generated code uses `Call(...)`, not `Return(...)`. The live renderer path (`RenderBaseClassContent` -> `RenderBaseClassEntryPoints`) emits `Call` for all callbacks.

Similarly, the "Impact on Existing API" table shows `stub.Add.Return(callback)` as "Before". This is not the current state.

**Question:** Can you update all "Current" examples to reflect the actual v0.52.0 output? The entry point is already `Call()`, so the only real changes are: (a) delegate types from Func/Action to custom named delegates, (b) builder/sequence type names, and (c) XML doc return type.

#### 3. MEDIUM: No Regression Test Interface for the CS0121 Bug

The plan describes a CS0121 bug with overloaded methods whose tuples differ only in element count with same types. But examining the current test suite:

- `IAuthSvcMethods` has `out string token` on overloads -- already gets custom delegates, no CS0121.
- `IFormatter` has different types per overload (`string`, `FormatOptions`, `int`) -- tuple types are already distinguishable.

**There is no test interface that would actually trigger CS0121 today.** To verify the fix works, the plan should include creating a new test interface with same-type overloads, e.g.:
```csharp
public interface IOverloadSameTypes
{
    bool Check(string a, string b);
    bool Check(string a, string b, string c);
}
```
and a test that uses `stub.Check.Call(args => args.a == "x")` -- which would fail with CS0121 before the fix and succeed after.

**Question:** Can you add a regression test interface and test case to the plan's Phase 4 verification? Without this, we cannot prove the bug is fixed.

#### 4. MEDIUM: `RenderSingleSignatureContent` Dead Code Not Addressed

The plan lists changes to `MethodInterceptorRenderer` but does not mention that `RenderSingleSignatureContent` (~320 lines, lines 84-413) is dead code. This method:
- Is never called (only `RenderBaseClassContent` is called for single-sig methods)
- Contains the old `Return(callback)` pattern
- Will cause confusion during implementation because its code looks relevant but isn't

**Question:** Should the plan explicitly include deleting `RenderSingleSignatureContent` as a cleanup item? Or if it's intentionally kept as fallback, that should be documented.

#### 5. MEDIUM: Async Simplified Delegate Naming for Overloads

The plan defines:
- `{MethodName}SyncDelegate` for simplified async delegates
- `{MethodName}SyncDelegate2` for second overload

But for the overload group path, the current renderer generates simplified delegate storage per overload using `BuildSyncDelegateType()` which returns `Func<..., TInner>` (line 5208-5218). With custom delegates, each async overload needs TWO delegate types: full and simplified.

For an interface like:
```csharp
Task<string> TransformAsync(string input);
Task<string> TransformAsync(string input, CancellationToken ct);
```

The plan would need:
- `TransformAsyncDelegate` (full, 1-param)
- `TransformAsyncSyncDelegate` (simplified, 1-param)
- `TransformAsyncDelegate2` (full, 2-param)
- `TransformAsyncSyncDelegate2` (simplified, 2-param)

That's 4 delegate types per async overloaded method. The plan shows this pattern for non-overloaded async methods (Design Decision 8) but doesn't explicitly show the overloaded async case with numbering applied to both full and simplified.

**Question:** Can you add an explicit example showing the naming for overloaded async methods? Also confirm: should the `{MethodName}Impl` and `{MethodName}Sequence` types also get numbered to match (i.e., `TransformAsyncImpl` and `TransformAsyncImpl2`)?

#### 6. LOW: When Predicate CS0121 Is the Same Bug

The plan acknowledges this as an "OPEN QUESTION" (Design Decision 7): When predicates use `Func<tuple, bool>` which has the same CS0121 problem for overloads with same-type params.

This is the SAME fundamental bug being fixed for Call callbacks. Leaving it unfixed means overloaded methods have a partially broken API: Call works but When-predicate doesn't.

The plan correctly identifies this and asks the user to decide. However, I note that fixing When predicates requires custom predicate delegates too (e.g., `ValidateCredentialsPredicate`, `ValidateCredentialsPredicate2`), which adds significant delegate proliferation.

**Not blocking, but the user should be aware this is a known gap in the fix.**

#### 7. LOW: Overload Numbering Stability (Already Noted)

The plan's Open Question 4 asks about numbering stability. The proposed `Delegate2` / `Delegate3` numbering shifts when overloads are added between existing ones. This is acceptable pre-1.0 but should be documented in migration notes.

### What Looks Good

- **Core design is sound.** Custom named delegates solve the CS0121 ambiguity definitively. Every method gets its own delegate type, eliminating all overload resolution issues.
- **Pattern-by-pattern analysis is thorough.** All 9 patterns covered with correct pipeline identification.
- **Separation of concerns is clear.** Delegates are entry surface only; tuples stay for internal storage and LastArgs. When predicates also use custom delegates (DD7).
- **Generic method handler delegates already use the correct naming convention** (`{MethodName}Delegate`), so no rename needed there.
- **Risk assessment is realistic** about the large consumer code churn.
- **Open questions are transparently presented** rather than hidden.
- **XML doc enhancement** (`-> ReturnType`) is a good IntelliSense improvement.
- **Method-name-based type naming** (`AddImpl` vs `MethodCallBuilderImpl`) is much cleaner.

### Recommendation

Send back to architect to address Concerns 1-5 before implementation. Concerns 1 and 2 are particularly important because they will lead the implementer to attempt changes that are already done, wasting time and risking regressions.

### Architect Responses (Revision 2 -- 2026-02-19)

**Concern 1 (DD5 Already Implemented):** Agreed. DD5 has been rewritten to state "Already Done -- No Change Needed" with evidence from the live renderer path and Design.Stubs. The breaking changes table, Phase 2, and Phase 3 have been updated to remove all references to a `Return(callback)` -> `Call(callback)` rename.

**Concern 2 (Current Examples Wrong):** Agreed. All "Current" code examples in DD1 updated to show `Call(...)` instead of `Return(...)`. The breaking changes table "Before" column now says "v0.52.0" and accurately reflects the current state.

**Concern 3 (No Regression Test for CS0121):** Agreed. Added Phase 0 with explicit instructions to create `IOverloadSameTypes` with same-type, different-arity overloads (no ref/out) as a regression test. Phase 4 verification updated to cite this interface as the primary proof the bug is fixed, with a note that `IAuthSvcMethods` does NOT trigger CS0121.

**Concern 4 (Dead Code Cleanup):** Agreed. Added Phase 0 with explicit instructions to delete `RenderSingleSignatureContent` (~346 lines, lines 84-429). This dead code predates the base-class rendering approach from v0.52.0 and will confuse implementers.

**Concern 5 (Async Overloaded Naming):** Agreed. DD8 expanded with a full `IFormatter.TransformAsync` overloaded example showing all 4 delegate types (`TransformAsyncDelegate`, `TransformAsyncSyncDelegate`, `TransformAsyncDelegate2`, `TransformAsyncSyncDelegate2`) and the corresponding 4 `Call()` entry points with `TransformAsyncImpl` / `TransformAsyncImpl2`. Added explicit numbering rule: the suffix applies consistently to ALL types for that overload.

**Concern 6 (When Predicate CS0121):** Acknowledged. This is the same fundamental bug and leaving it unfixed creates a partial fix. The open question is preserved for the user. If the user decides to fix When predicates too, the mechanical change is identical (custom predicate delegates per overload). **Update (Revision 3):** User decided yes -- When predicates now use custom delegates. See DD7 and Revision 3 responses below.

**Concern 7 (Overload Numbering Stability):** Acknowledged, preserved as open question. Acceptable pre-1.0.

### Architect Responses (Revision 3 -- 2026-02-19)

**Concern 1 (MEDIUM -- When predicates not integrated into plan):** Agreed. The user's DD7 decision (custom predicate delegates for When) was not fully integrated into the plan body. Changes made:
- "What Does NOT Change" table: `When(predicate)` row updated from "No change (still tuple-based Func)" to show delegate type changes to custom predicate delegate.
- API Surface Summary: Non-overloaded method example updated to show `AddPredicate` delegate declaration and `When(AddPredicate predicate)` instead of `Func<(int a, int b), bool>`.
- Breaking changes table: Added row for When predicate type change (`Func<tuple, bool>` -> custom predicate delegate).
- Phase 1 model changes: Added `PredicateFriendlyName` and `PredicateDelegateSignature` fields to `UnifiedMethodInterceptorModel` and `MethodOverloadSignature`. Added `BuildWhenPredicateType()` change and predicate naming method to `UnifiedInterceptorBuilder`. Added predicate population to `ModelAdapters`.
- Phase 2 renderer changes: Added explicit list of 6 methods that need predicate delegate updates: `BuildPredicateType()`, `BuildWhenPredicateType()`, `RenderBaseClassWhenEntryPoints()`, `RenderBaseClassVoidWhenEntryPoints()`, `RenderWhenEntryPoints()`, `RenderVoidWhenEntryPoints()`.

**Concern 2 (LOW -- copy-paste error):** Fixed. Open Question 2 now reads "instead of `{MethodName}SimplifiedDelegate`".

**Concern 3 (MEDIUM -- delegate types per method):** Addressed as consequence of Concern 1. Added a "Complete delegate types generated per method" table and example to DD2 showing all delegate types for each scenario: call delegate, sync delegate (async only), and predicate delegate (2+ params only), with numbered variants for overloads.

**Concern 4 (LOW -- async wrapping lambda):** Added to Phase 2. `BuildAsyncWrapExpression()` and `BuildVoidAsyncWrapExpression()` explicitly called out with the required change: tuple-based `BuildLambdaParamDecls`/`BuildLambdaCallArgs` (which produce `(TArgs args)` and `args` for 2+ params) must be replaced with individual-param equivalents `BuildDelegateMatchingParamDecls`/`BuildDelegateMatchingCallArgs` (which produce `(int a, int b)` and `a, b`).

---

## Notes

### Why Not Just Fix Overloaded Methods?

The user explicitly wants custom delegates for ALL methods, not just overloaded ones. Reasons:
1. **Consistency** -- same API pattern everywhere
2. **IntelliSense quality** -- custom delegates show parameter names in the lambda tooltip for ALL methods, not just overloaded ones
3. **Future-proofing** -- if an interface later adds an overload, existing stub code doesn't need to change syntax
4. **Cleaner type names** -- `AddDelegate` vs `Func<(int a, int b), int>` in error messages and IntelliSense

### Relationship to v0.52.0

This is a bug fix for v0.52.0's tuple-based overload resolution. The plan supersedes the tuple approach for Call callbacks and When predicates while preserving tuples for LastArgs (read-only, no overload resolution issue) and When exact-match (explicit parameter types, already unambiguous).

### Version

This should bump to v0.53.0 (new minor version for API change).

---

## Implementation Progress

**Started:** 2026-02-19

### Phase 1: Builder/Model Changes -- COMPLETE

**Files modified:**

1. **`src/Generator/Builder/UnifiedInterceptorBuilder.cs`**
   - `NeedsCustomDelegate()`: Now always returns `true`
   - `BuildCallDelegateType()`: Now always returns `{methodName}Delegate?` (no more Func/Action)
   - `BuildCustomDelegateSignature()`: Now always generates (removed `if (!NeedsCustomDelegate(sig)) return null` guard)
   - `BuildOverloadSignature()`: Now always uses custom delegates with numbered suffixes. Added `overloadSuffix` parameter.
   - `BuildWhenPredicateType()`: Added optional `predicateFriendlyName` parameter. For 2+ params, uses custom predicate delegate name when available; falls back to tuple-based for backward compatibility during transition.
   - New: `SortOverloadsForNumbering()` -- sorts by param count ascending, then lex order by param types
   - New: `GetOverloadSuffix()` -- index 0 -> "", index 1 -> "2", index 2 -> "3"
   - New: `BuildPredicateDelegateSignature()` -- builds predicate delegate signature for 2+ params
   - Single-signature path: now populates `DelegateFriendlyName`, `PredicateFriendlyName`, `PredicateDelegateSignature`, `BuilderFriendlyName`, `SequenceFriendlyName`
   - Multi-overload path: now sorts signatures, assigns numbered suffixes, populates all friendly name fields per overload

2. **`src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs`**
   - Removed: `UsesTupleCallDelegate` field
   - Added: `DelegateFriendlyName`, `PredicateFriendlyName`, `PredicateDelegateSignature`, `BuilderFriendlyName`, `SequenceFriendlyName` (all nullable with defaults)

3. **`src/Generator/Model/Shared/MethodOverloadSignature.cs`**
   - Removed: `UsesTupleCallDelegate` field
   - Added: `DelegateFriendlyName`, `PredicateFriendlyName`, `PredicateDelegateSignature`, `BuilderFriendlyName`, `SequenceFriendlyName` (all nullable with defaults)

4. **`src/Generator/Renderer/Shared/ModelAdapters.cs`**
   - `BuildSingleSignatureModel()`: Now uses `NeedsCustomDelegate: true`, populates all friendly name fields, removed `usesTuple` computation
   - `BuildMultiOverloadModel()`: Complete rewrite -- sorts unique methods for stable numbering, builds custom delegates with `{MethodName}Delegate{N}` naming, populates all friendly name fields per overload
   - Removed: `BuildFuncActionDelegateType()` helper (no longer needed)

5. **`src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`**
   - Replaced all 7 `model.UsesTupleCallDelegate` references with `false`
   - Replaced all 2 `overload.UsesTupleCallDelegate` references with `false`
   - (Further renderer changes for Phase 2 will use the new friendly name fields)

**Verification:**
- Generator project (`src/Generator/Generator.csproj`): Builds successfully with 0 warnings, 0 errors
- KnockOff library project (`src/KnockOff/KnockOff.csproj`): Builds successfully with 0 warnings, 0 errors
- Full solution: 3920 errors in generated `.g.cs` files (expected -- the model now produces custom delegate types but the renderer hasn't been updated to emit compatible code yet; that's Phase 2 work)
- Error patterns are consistent with plan expectations:
  - CS1503: Custom delegate types (e.g., `AddDelegate`) cannot convert to `System.Action<string>` (renderer still emits Func/Action in some places)
  - CS7036: Custom delegates with individual params but renderer still passes args as tuples

### Phase 2: Renderer Changes -- COMPLETE

(Completed in prior session. See git history for full details.)

**Phase 2 regression fix:** `ModelAdapters.cs` `BuildMultiOverloadModel()` was incorrectly using `model.InterfaceType` for `_source` field type in overload-group interceptors. For inherited interfaces (e.g., `IStore : IReadableStore`), the `_source` must use the declaring interface type, not the stub's target interface. Fixed by using `method.ContainingInterfaceType` from the method model.

### Phase 3: Consumer Code Updates -- COMPLETE

**Documentation.Samples (23 files updated):**
All consumer code updated from tuple accessor syntax (`args => args.a + args.b`) to direct delegate params (`(int a, int b) => a + b`). Files include:
- `BasicMethodSamples.cs`, `CallApiSamples.cs`, `EventSamples.cs`, `GettingStartedSamples.cs`
- `IndexerSamples.cs`, `InlineClassSamples.cs`, `InlineDelegateSamples.cs`
- `InlineGenericSamples.cs`, `InlineInterfaceSamples.cs`, `MethodsSamples.cs`
- `OpenGenericClassSamples.cs`, `OpenGenericInterfaceSamples.cs`
- `PropertySamples.cs`, `SequenceSamples.cs`, `SourceDelegationSamples.cs`
- `StandaloneClassSamples.cs`, `StandaloneGenericSamples.cs`, `StandaloneSamples.cs`
- `StrictModeSamples.cs`, `StubOverrideSamples.cs`, `VerificationSamples.cs`
- `WhenApiSamples.cs`, `OverloadSameTypesSamples.cs`

**Design.Stubs (7 files updated):**
- `BasicMethods.cs`, `MethodSequences.cs`, `MethodOverloads.cs`, `WhenMatching.cs`
- `StubOverrideBasics.cs`, `Verification.cs`, `SourceDelegation.cs`
- Updated code and comments to reflect delegate-based syntax and named parameters

**Design.Tests (11 files updated):**
- `MethodBasicsTests.cs`, `MethodSequenceTests.cs`, `MethodOverloadTests.cs`, `WhenMatchingTests.cs`
- `StandalonePatternTests.cs`, `InlineInterfacePatternTests.cs`
- `SourceDelegationTests.cs`, `WhenChainVerificationBugTests.cs`
- `GenericStandaloneOverloadTests.cs`, `InlineClassOverloadTests.cs`, `OpenGenericOverloadTests.cs`

**CS0121 Regression Test:**
Added 6 new test methods to `OverloadSameTypesSamples.cs` proving the delegate-based API resolves overload ambiguity for same-type, different-arity overloads:
- `Standalone_Call_TwoParam_CS0121Regression`
- `Standalone_Call_ThreeParam_CS0121Regression`
- `Inline_Call_TwoParam_CS0121Regression`
- `Inline_Call_ThreeParam_CS0121Regression`
- `Standalone_WhenPredicate_TwoParam_CS0121Regression`
- `Standalone_WhenPredicate_ThreeParam_CS0121Regression`

### Phase 4: Verification -- COMPLETE

**Full solution build (src/KnockOff.sln):** 0 errors, 0 warnings

**Full solution tests (src/KnockOff.sln):**
| Project | net8.0 | net9.0 | net10.0 |
|---------|--------|--------|---------|
| KnockOffTests | 1509 pass, 4 skip | 1510 pass, 4 skip | 1510 pass, 4 skip |
| AssemblyStrict | 14 pass | 14 pass | 14 pass |
| NeatooInterfaceTests | 473 pass | 473 pass | 473 pass |
| Documentation.Samples | 701 pass | 701 pass | 701 pass |

Skipped tests are pre-existing (`BugRegressionTests.Property*_Verifiable_CalledConstraint_IsApplied` and `Indexer*_Verifiable_CalledConstraint_IsApplied`).

**Design project build (src/Design/Design.Stubs):** 0 errors, 0 warnings

**Design project tests (src/Design/Design.Tests):**
| TFM | Result |
|-----|--------|
| net8.0 | 370 pass, 0 fail, 0 skip |
| net9.0 | 370 pass, 0 fail, 0 skip |
| net10.0 | 370 pass, 0 fail, 0 skip |

**CS0121 regression test verified:** All 6 regression tests compile and pass, proving the delegate-based Call API resolves the overload ambiguity bug for same-type, different-arity method overloads.
