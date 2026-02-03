# Base Class Follow-up Fixes

**Date:** 2026-02-03
**Related Todo:** [Base Class User Methods](../todos/base-class-user-methods.md)
**Status:** Draft
**Last Updated:** 2026-02-03

---

## Overview

Two issues identified during code review of the base-class-user-methods implementation that need follow-up work:

1. **Missing KO0200 Diagnostic Test** - The diagnostic was implemented and manually verified, but no automated regression test exists
2. **Overload Detection Granularity** - `HasUserOverride` is set by method NAME only, not parameter signature

---

## Issue 1: Missing KO0200 Diagnostic Test

### Problem

The KO0200 diagnostic ("Standalone stub cannot have user-defined base class") was implemented in Phase 6 and manually verified by temporarily modifying sandbox code. However, there is no automated regression test for this diagnostic.

From the implementation progress log:

> **Verification:**
> - Tested by temporarily adding `[KnockOff] public partial class UserServiceKnockOff : MyBaseClass, IUserService` to sandbox
> - Confirmed error message: `error KO0200: Standalone stub 'UserServiceKnockOff' cannot have base class 'KnockOff.Sandbox.MyBaseClass'. KnockOff generates a base class for user method support. Remove the base class or use inline stub pattern instead.`

Without an automated test, this behavior could regress silently in future changes.

### Solution

Add a diagnostic test that:
1. Defines a standalone stub with a user-defined base class
2. Verifies the generator emits KO0200
3. Verifies no stub code is generated (generation should be blocked)

### Test Location

`src/Tests/KnockOffTests/BaseClassUserMethodTests.cs` in a new `DiagnosticTests` region/section.

### Example Test

```csharp
public class DiagnosticStubBase { }

[KnockOff]
public partial class StubWithUserBaseClass : DiagnosticStubBase, IDiagnosticService { }

public interface IDiagnosticService
{
    void DoSomething();
}

[Fact]
public void KO0200_EmittedWhen_StandaloneStubHasUserBaseClass()
{
    // The stub should fail to compile due to KO0200
    // This test verifies via compilation errors or source generator testing utilities
}
```

**Note:** The exact testing approach depends on how KnockOff tests diagnostics. This may require:
- A compile-time test (the stub won't compile if KO0200 is an error)
- A source generator test harness that captures diagnostics
- Review existing diagnostic tests in the codebase to match the pattern

---

## Issue 2: Overload Detection Granularity

### Problem

Currently, `HasUserOverride` is set based on method **NAME** only, not the full parameter signature. This means if a user overrides one overload, ALL overloads of that method get `HasUserOverride: true`.

**Example:**

```csharp
interface IFormatter {
    string Format(string input);
    string Format(string input, bool uppercase);
    string Format(string input, bool uppercase, int maxLength);
}

public partial class FormatterStub {
    // User only overrides the first overload
    protected override string Format_(string input) => input.ToUpper();

    // Format_(string, bool) and Format_(string, bool, int) are NOT overridden
}
```

**Current behavior:** ALL three overloads have `HasUserOverride: true` because they share the method name "Format".

**Impact:**

1. **Non-overridden overloads call the virtual method which returns `default!`**
   - `Format_(string, bool)` returns `null` (string default) instead of being handled by interceptor
   - This is likely unexpected by users

2. **Strict mode does NOT throw for non-overridden overloads**
   - With `Strict = true`, calling `Format("test", true)` returns `null` instead of throwing
   - User expects strict mode to catch unconfigured methods

3. **OnCall still works correctly**
   - Priority is OnCall > User Override, so `stub.Format.OnCall((s, b) => ...)` still works
   - This is the saving grace - users can work around the issue

### Root Cause

In `KnockOffGenerator.Transform.cs` (or related), the override detection builds a set of method names from user-defined overrides:

```csharp
// Simplified from detection logic
var overrideMethods = new HashSet<string>();
// ...
if (methodName.EndsWith("_"))
{
    overrideMethods.Add(methodName.TrimEnd('_'));  // Just the name, no signature
}
```

Later, when building `FlatMethodModel`, the code checks:

```csharp
// Simplified
bool hasOverride = overrideMethods.Contains(methodName);
```

This matches by name only, not by full signature.

### Solution Options

#### Option A: Full Signature Matching (Recommended)

Change the override detection to build a set of **signature keys** instead of just method names.

**Signature Key Format:** `{MethodName}({ParameterType1},{ParameterType2},...)`

Example keys:
- `Format_(string)` for `Format_(string input)`
- `Format_(string,bool)` for `Format_(string input, bool uppercase)`
- `Format_(string,bool,int)` for `Format_(string input, bool uppercase, int maxLength)`

**Changes Required:**

1. **Transform phase:** Build signature key from `MethodDeclarationSyntax`:
   ```csharp
   private static string BuildOverrideSignatureKey(MethodDeclarationSyntax method)
   {
       var name = method.Identifier.Text;
       var paramTypes = method.ParameterList.Parameters
           .Select(p => p.Type?.ToString() ?? "object")
           .ToArray();
       return $"{name}({string.Join(",", paramTypes)})";
   }
   ```

2. **Model builder:** Match signature key instead of just method name:
   ```csharp
   var signatureKey = $"{methodName}_({string.Join(",", parameterTypes)})";
   bool hasOverride = overrideSignatures.Contains(signatureKey);
   ```

**Pros:**
- Correct behavior for partial overload overrides
- Strict mode works as expected for non-overridden overloads
- Precise control over which overloads have user implementation

**Cons:**
- Slightly more complex signature key building
- Type name matching must be consistent between syntax and semantic representations

#### Option B: Keep Current Behavior, Document as Intentional

Document that if you override any overload, you must override all overloads (or use OnCall for the rest).

**Pros:**
- No code changes
- Simple mental model: "override any = override all"

**Cons:**
- Counterintuitive behavior
- Strict mode doesn't catch unconfigured overloads
- The design document explicitly states overloads work independently

**Recommendation:** Option A - Full Signature Matching

The original design explicitly states:
> "Each overload becomes a separate virtual method in the base class. Users override the ones they want."

The current implementation doesn't match this design intent.

### Files to Change

Based on codebase exploration:

1. **`src/Generator/KnockOffGenerator.Transform.cs`**
   - Update `DetectUserOverrideMethods()` to return signature keys, not just names
   - Build signature from `MethodDeclarationSyntax.ParameterList`

2. **`src/Generator/Builder/FlatModelBuilder.cs`**
   - Update override matching to use full signature key

3. **`src/Generator/Models/CommonModels.cs`** (or relevant model file)
   - If storing detected overrides, may need signature key representation

4. **Tests:**
   - Add tests verifying per-overload detection
   - Existing `OverloadedUserMethodTests` may need updates

### Edge Cases

1. **Generic parameters in signature**
   - `Format_<T>(T item)` - generic type parameter must be handled
   - Use `T` as the type string, not the substituted type

2. **Nullable types**
   - `Format_(string? input)` vs `Format_(string input)`
   - Syntax includes `?` in type string - should work correctly

3. **Array types**
   - `Format_(string[] items)` - syntax includes `[]`
   - Should work correctly

4. **ref/out/in parameters**
   - `Format_(ref int value)` - modifiers affect signature
   - Include modifier in signature key: `Format_(ref int)`

---

## Scope

### Patterns Affected

- **Standalone:** YES - this is the only pattern with user methods
- **Inline Interface:** NO - no user code in generated stubs
- **Inline Class:** NO - different pattern
- **Inline Delegate:** NO - different pattern

### Members Affected

- **Methods:** YES - this is a methods-only feature
- **Properties:** NO - not supported for user methods (Phase 2)
- **Indexers:** NO - not supported for user methods
- **Events:** NO - not supported for user methods

---

## Test Requirements

### Issue 1: KO0200 Diagnostic Test

| Test | Description |
|------|-------------|
| `KO0200_Emitted_WhenStandaloneHasUserBaseClass` | Verify diagnostic emitted |
| `KO0200_BlocksGeneration_WhenBaseClassPresent` | Verify no stub generated |

### Issue 2: Overload Detection

| Test | Description |
|------|-------------|
| `Overload_PartialOverride_OnlyOverriddenMethodsHaveFlag` | Override one, others should not have flag |
| `Overload_NonOverridden_UsesInterceptor` | Non-overridden overload uses interceptor path |
| `Overload_NonOverridden_StrictModeThrows` | Strict mode catches non-overridden overload |
| `Overload_PartialOverride_OnCallStillWorks` | OnCall supersedes correctly on all overloads |

---

## Implementation Plan

### Phase 1: Add KO0200 Diagnostic Test

1. Review existing diagnostic tests in codebase to understand pattern
2. Add test(s) to `BaseClassUserMethodTests.cs`
3. Verify test passes (diagnostic is already implemented)

### Phase 2: Fix Overload Detection Granularity

1. Update signature key building in Transform phase
2. Update signature matching in Builder
3. Add/update tests for per-overload detection
4. Verify existing tests still pass

---

## Acceptance Criteria

- [ ] KO0200 has automated regression test
- [ ] Overload detection uses full signature, not just name
- [ ] Per-overload user override detection is verified by tests
- [ ] Strict mode throws for non-overridden overloads when no OnCall configured
- [ ] All existing tests continue to pass

---

## Risks / Considerations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Type name mismatch between syntax/semantic | Medium | High | Build signature from same source (syntax) consistently |
| Existing tests depend on current behavior | Low | Medium | Review test assumptions before changing |
| Generic method edge cases | Low | Medium | Exclude generic methods from signature matching (already excluded from base class) |
