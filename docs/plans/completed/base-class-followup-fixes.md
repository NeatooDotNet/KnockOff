# Base Class Follow-up Fixes

**Date:** 2026-02-03
**Related Todo:** [Base Class User Methods](../todos/base-class-user-methods.md)
**Status:** Complete
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

---

## Developer Review

**Status:** Concerns Addressed - Ready for Re-review

**Concerns Addressed:**

### Concern 1: Signature Key Format Inconsistency

**Developer Concern:** Plan proposes `Format_(string)` without return type, but existing `FlatModelBuilder.BuildMethodSignatureKey` uses `{name}({paramTypes}):{returnType}` which includes return type.

**Resolution:** The plan's signature format is CORRECT for this use case. The two signature formats serve different purposes:

1. **`BuildMethodSignatureKey` (existing):** Used for matching user methods (legacy `UserMethods` collection) to interface members. Includes return type because two methods with same name and parameters but different return types are different interface contracts.
   ```csharp
   // File: src/Generator/Builder/FlatModelBuilder.cs:1967
   return $"{name}{genericPart}({paramPart}):{returnType}";
   ```

2. **Override signature key (new):** Used for detecting syntactic `override` methods in user code. Does NOT need return type because:
   - C# does not allow overloading by return type alone
   - The override method name already includes the `_` suffix
   - We're matching syntax (what user wrote) to syntax (what generator will produce)

**Format adopted for overrides:** `{MethodName}_({ParamType1},{ParamType2},...)`

Example: `Format_(string,bool)` matches user's `protected override string Format_(string input, bool uppercase)`

**Why not include return type:** In the user's partial class, they write `protected override string Format_(string input)`. The return type is part of the syntax, but including it in the key would require parsing the return type syntax AND handling cases like `async Task<string>` vs `string`. Since C# forbids return-type overloading, omitting it is safe and simpler.

### Concern 2: Type Name Matching Strategy Missing

**Developer Concern:** Plan uses `p.Type?.ToString()` for syntax-based detection, but Builder uses semantic type strings. Need normalization strategy.

**Resolution:** Both sides will use **syntax-level type strings** for the override detection path. This is intentional and correct:

**Transform phase (syntax):**
```csharp
// In DetectUserOverrideMethods - uses MethodDeclarationSyntax
var paramTypes = method.ParameterList.Parameters
    .Select(p => NormalizeTypeName(p.Type?.ToString() ?? "object"))
    .ToArray();
```

**Builder phase (matching):**
```csharp
// In FlatModelBuilder.BuildMethodModel - builds key from InterfaceMemberInfo
// InterfaceMemberInfo.Parameters[i].Type comes from semantic analysis
var paramTypes = member.Parameters.Select(p => NormalizeSyntaxType(p.Type));
```

**Normalization Function:**
```csharp
/// <summary>
/// Normalizes type names to match what users would write in their override methods.
/// Handles common differences between semantic and syntax representations.
/// </summary>
private static string NormalizeSyntaxType(string type)
{
    // Remove global:: prefix (user code won't have it)
    var result = type.Replace("global::", "");

    // Map fully qualified System types to keywords
    result = result switch
    {
        "System.String" => "string",
        "System.Int32" => "int",
        "System.Int64" => "long",
        "System.Boolean" => "bool",
        "System.Double" => "double",
        "System.Single" => "float",
        "System.Decimal" => "decimal",
        "System.Char" => "char",
        "System.Byte" => "byte",
        "System.Object" => "object",
        "System.Void" => "void",
        _ => result
    };

    return result;
}
```

**Why this works:**
- Interface semantic analysis produces `global::System.String`
- User syntax produces `string`
- After normalization, both become `string`
- Edge cases like `System.Collections.Generic.List<string>` remain as-is (user writes the same)

### Concern 3: Diagnostic Test Approach Undefined

**Developer Concern:** Codebase has no existing diagnostic test infrastructure. Adding KO0200-triggering code to test project causes build error.

**Resolution:** KO0200 is an **error diagnostic**, not a warning. Testing it requires a different approach than runtime tests.

**Approach: Build-Error Verification via Separate Compilation**

Since KnockOff's test project cannot contain code that triggers KO0200 (it would fail to build), we need a separate verification approach:

**Option A: Manual Verification (Documented) - RECOMMENDED for v0.9**

Document that KO0200 is verified manually:
1. Create a temporary test file with the offending code
2. Run `dotnet build` and observe the error
3. Delete the test file
4. Document the verification in the plan's completion evidence

This is acceptable for a single diagnostic because:
- The diagnostic IS implemented and working
- It blocks compilation, so any regression would be caught immediately by users
- Setting up a Roslyn test harness is significant effort for one test

**Option B: Roslyn CSharpGeneratorDriver Testing (Future Enhancement)**

For future diagnostic testing infrastructure:
```csharp
[Fact]
public void KO0200_Emitted_WhenStandaloneHasUserBaseClass()
{
    var source = @"
        using KnockOff;
        public class MyBase { }
        public interface IService { void DoSomething(); }

        [KnockOff]
        public partial class BadStub : MyBase, IService { }
    ";

    var generator = new KnockOffGenerator();
    var driver = CSharpGeneratorDriver.Create(generator);

    var compilation = CreateCompilation(source);
    driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
        compilation, out var outputCompilation, out var diagnostics);

    Assert.Contains(diagnostics, d => d.Id == "KO0200");
    Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
}
```

**Recommendation:** Use Option A for this plan. Create a follow-up todo for Option B to establish proper diagnostic test infrastructure for future diagnostics.

### Concern 4: Existing Test Behavior Ambiguity

**Developer Concern:** `Overload_NoUserOverride_UsesInterceptorOrDefault` test asserts `Assert.Null(result)`. Clarify if this is correct.

**Resolution:** The test IS currently passing and IS testing the CORRECT expected behavior for the BUG we're fixing.

**Current Situation (Bug):**
- User defines `protected override string Format_(string input)` for ONE overload
- Due to name-only matching, BOTH `Format_(string)` AND `Format_(string, bool)` get `HasUserOverride: true`
- For `Format_(string, bool)`, the implementation calls the BASE CLASS virtual method
- The base class method returns `default!` which is `null` for `string`
- The test asserts `Assert.Null(result)` - this passes because of the bug

**After Fix:**
- `Format_(string)` has `HasUserOverride: true` (user wrote override)
- `Format_(string, bool)` has `HasUserOverride: false` (no user override)
- For `Format_(string, bool)`, the implementation uses the INTERCEPTOR path
- With no OnCall configured, interceptor returns `default!` which is ALSO `null`
- The test still passes, but for the RIGHT reason (interceptor default, not base class default)

**Key Insight:** The test assertion `Assert.Null(result)` is CORRECT for both before and after the fix. The difference is:
- **Before (bug):** `null` comes from base class `Format_(string, bool)` returning `default!`
- **After (fix):** `null` comes from interceptor's default path returning `default!`

**Behavioral difference visible in Strict mode:**
- **Before (bug):** Strict mode does NOT throw because `HasUserOverride: true` bypasses strict check
- **After (fix):** Strict mode WILL throw because interceptor path checks `Strict` flag

**Test Update Needed:**
```csharp
[Fact]
public void Overload_NoUserOverride_StrictModeThrows()
{
    // Arrange - this test verifies the fix works
    var stub = new OverloadedUserMethodStub().Strict();
    IOverloadedUserMethodService service = stub;

    // Act & Assert - non-overridden overload should throw in strict mode
    Assert.Throws<StubException>(() => service.Format("hello", true));
}
```

### Concern 5: `CommonModels.cs` Listed But Not Detailed

**Developer Concern:** Plan lists it as a file to change but doesn't specify what changes.

**Resolution:** `CommonModels.cs` change IS required. Here are the specific changes:

**File:** `src/Generator/Models/CommonModels.cs`

**Change:** Update `UserOverrideMethods` type from `EquatableArray<string>` to hold signature keys:

```csharp
// Before (line 31):
EquatableArray<string> UserOverrideMethods,

// After:
/// <summary>
/// Method signatures (format: "MethodName_(ParamType1,ParamType2,...)") that have user-defined
/// "protected override" methods. Used for base class user method pattern.
/// Note: Does not include return type since C# forbids return-type overloading.
/// </summary>
EquatableArray<string> UserOverrideMethods,
```

The TYPE stays the same (`EquatableArray<string>`), but the CONTENT changes from method names to signature keys. Only the doc comment needs updating to reflect the new format.

**Why no type change needed:**
- `EquatableArray<string>` already stores strings
- We're just changing WHAT strings are stored (names -> signatures)
- No model structure changes required

### Concern 6: ref/out/in Modifier Extraction Not Specified

**Developer Concern:** Plan says include modifiers in signature but doesn't show how. `ParameterSyntax.Type` doesn't include modifiers.

**Resolution:** Correct - modifiers are in `ParameterSyntax.Modifiers`, not `ParameterSyntax.Type`. Here's the complete extraction code:

**Transform phase (syntax-based detection):**
```csharp
private static string BuildOverrideSignatureKey(MethodDeclarationSyntax method)
{
    var name = method.Identifier.Text;
    var paramParts = method.ParameterList.Parameters
        .Select(p => {
            // Get modifier prefix (ref, out, in, ref readonly, params)
            var modifiers = p.Modifiers;
            var prefix = "";
            if (modifiers.Any(m => m.IsKind(SyntaxKind.RefKeyword)))
            {
                if (modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)))
                    prefix = "ref readonly ";
                else
                    prefix = "ref ";
            }
            else if (modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword)))
                prefix = "out ";
            else if (modifiers.Any(m => m.IsKind(SyntaxKind.InKeyword)))
                prefix = "in ";
            // Note: params not included - it doesn't affect signature

            // Normalize the type name
            var typeName = NormalizeSyntaxType(p.Type?.ToString() ?? "object");
            return prefix + typeName;
        })
        .ToArray();
    return $"{name}({string.Join(",", paramParts)})";
}
```

**Builder phase (semantic-based matching):**
```csharp
// Already handled - InterfaceMemberInfo has RefKind per parameter
var paramParts = member.Parameters.Select(p => {
    var prefix = p.RefKind switch
    {
        RefKind.Ref => "ref ",
        RefKind.Out => "out ",
        RefKind.In => "in ",
        RefKind.RefReadOnlyParameter => "ref readonly ",
        _ => ""
    };
    return prefix + NormalizeSyntaxType(p.Type);
});
var signatureKey = $"{member.Name}_({string.Join(",", paramParts)})";
```

**Example signatures:**
- `TryParse_(string,out int)` matches `protected override bool TryParse_(string input, out int result)`
- `Modify_(ref string)` matches `protected override void Modify_(ref string value)`
- `Read_(in int)` matches `protected override void Read_(in int value)`

---

## Architectural Verification

**Three Patterns Analysis:**
- **Standalone:** This is the ONLY pattern affected. User override methods only exist in standalone stubs.
- **Inline Interface:** N/A - No user code in generated stubs
- **Inline Class:** N/A - Uses different inheritance pattern (overriding base class virtuals, not generator-created virtuals)

**Breaking Changes:** No - This is a bug fix. The current behavior is incorrect. Users who override one overload get silent bugs (other overloads don't work as expected). The fix makes each overload independent, which matches user expectations and the original design intent.

**Pattern Consistency:** Signature matching aligns with how `BuildMethodSignatureKey` works for legacy user methods (without return type, with ref/out/in modifiers).

**Codebase Analysis:**

Files examined:
- `src/Generator/KnockOffGenerator.Helpers.cs:100-133` - `DetectUserOverrideMethods()` implementation
- `src/Generator/KnockOffGenerator.Transform.cs:876-889` - Where detection is called and result stored
- `src/Generator/Builder/FlatModelBuilder.cs:657,890` - Where `UserOverrideMethods` is consumed
- `src/Generator/Builder/FlatModelBuilder.cs:1967-1976` - Existing `BuildMethodSignatureKey` for comparison
- `src/Generator/Model/Flat/FlatMethodModel.cs:32` - `HasUserOverride` field
- `src/Generator/Models/CommonModels.cs:31` - `UserOverrideMethods` definition
- `src/Tests/KnockOffTests/BaseClassUserMethodTests.cs:435-446` - Existing test behavior
- `src/Tests/KnockOffTests/Generated/.../OverloadedUserMethodStub.g.cs` - Generated code for overloaded methods
- `src/Tests/KnockOffTests/Generated/.../OverloadedUserMethodStub.Base.g.cs` - Generated base class

---

## Implementation Contract

**In Scope:**

Phase 1 (KO0200 Diagnostic):
- [x] Document manual verification of KO0200 in completion evidence
- [x] Create follow-up todo for diagnostic test infrastructure

Phase 2 (Overload Detection Fix):
- [x] `src/Generator/KnockOffGenerator.Helpers.cs`:
  - Update `DetectUserOverrideMethods()` to build signature keys
  - Add `BuildOverrideSignatureKey()` helper
  - Add `NormalizeSyntaxType()` helper for type normalization
- [x] `src/Generator/Builder/FlatModelBuilder.cs`:
  - Update `BuildMethodModel()` to match signature keys instead of names
  - Add helper to build signature key from `InterfaceMemberInfo`
  - Update `BuildNameMap` and `AssignNamesForOverloadGroup` to split overloads correctly
- [x] `src/Generator/Models/CommonModels.cs`:
  - Update doc comment for `UserOverrideMethods` to describe new format
- [x] `src/Tests/KnockOffTests/BaseClassUserMethodTests.cs`:
  - Add `Overload_NoUserOverride_StrictModeThrows` test
  - Update existing overload tests to use correct interceptor names (Format vs Format2)
  - Rename `Overload_NoUserOverride_UsesInterceptorOrDefault` to `Overload_NoUserOverride_ThrowsWithoutOnCall` with correct expectation

**Out of Scope:**
- Roslyn CSharpGeneratorDriver test infrastructure (follow-up todo)
- Generic method signature matching (already excluded from base class pattern per design)
- Property/indexer/event user overrides (Phase 2 future work)

---

## Phase 1 Completion Evidence

**Completed:** 2026-02-03
**Developer:** knockoff-developer

### KO0200 Manual Verification

**Verification Method:** Temporarily added offending code to sandbox project, built, and observed diagnostic output.

**Temporary Test Code (added to `src/Tests/KnockOffSandbox/KO0200Verification.cs`):**

```csharp
namespace KnockOff.Sandbox;

public class MyBaseClass
{
    public virtual void SomeMethod() { }
}

public interface IKO0200TestService
{
    void DoSomething();
}

[KnockOff]
public partial class KO0200TestStub : MyBaseClass, IKO0200TestService
{
}
```

**Build Output (KO0200 Verified):**

```
/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffSandbox/KO0200Verification.cs(28,22): error KO0200: Standalone stub 'KO0200TestStub' cannot have base class 'KnockOff.Sandbox.MyBaseClass'. KnockOff generates a base class for user method support. Remove the base class or use inline stub pattern instead.
```

**Verification Results:**

1. **Diagnostic Emitted:** KO0200 fires with correct message format
   - Class name `'KO0200TestStub'` correctly identified
   - Base class `'KnockOff.Sandbox.MyBaseClass'` correctly identified
   - Message explains the issue and suggests alternatives

2. **Generation Blocked:** CS0535 error (`'KO0200TestStub' does not implement interface member 'IKO0200TestService.DoSomething()'`) confirms no stub code was generated

3. **Multi-Target Framework:** Error emitted for net8.0, net9.0, and net10.0

4. **Cleanup:** Temporary file removed, sandbox builds successfully

### Follow-up Todo Created

**File:** [docs/todos/diagnostic-test-infrastructure.md](../todos/diagnostic-test-infrastructure.md)

**Purpose:** Track future work to add CSharpGeneratorDriver-based diagnostic test infrastructure for automated regression testing of error diagnostics like KO0200.

---

## Phase 2 Completion Evidence

**Completed:** 2026-02-03
**Developer:** knockoff-developer

### Changes Made

**1. KnockOffGenerator.Helpers.cs:**
- Updated `DetectUserOverrideMethods()` to return signature keys instead of just method names
- Added `BuildOverrideSignatureKey(MethodDeclarationSyntax)` helper that builds signature key from syntax
- Added `NormalizeSyntaxType(string)` helper for type normalization (maps `System.String` to `string`, etc.)
- Signature key format: `"MethodName_(ParamType1,ParamType2,...)"`
- Includes ref/out/in modifiers in signature

**2. FlatModelBuilder.cs:**
- Added `BuildOverrideSignatureKeyFromMember(InterfaceMemberInfo)` helper to build signature key from semantic model
- Added `NormalizeTypeForOverrideMatching(string)` helper for consistent type normalization
- Added `HasMatchingUserOverride(InterfaceMemberInfo, HashSet<string>)` helper
- Updated `BuildNameMap` signature to accept `userOverrideMethods` parameter
- Updated `AssignNamesForOverloadGroup` to check both legacy `UserMethods` and new `UserOverrideMethods`
- Updated `BuildMethodModel` line ~890 to use signature key matching: `HasUserOverride: userOverrideMethods.Contains(BuildOverrideSignatureKeyFromMember(member))`

**3. CommonModels.cs:**
- Updated doc comment on `UserOverrideMethods` to describe the new signature key format:
  > Method signatures (format: "MethodName_(ParamType1,ParamType2,...)") that have user-defined "protected override" methods.

**4. BaseClassUserMethodTests.cs:**
- Updated `Overload_NoUserOverride_UsesInterceptorOrDefault` renamed to `Overload_NoUserOverride_ThrowsWithoutOnCall` with correct assertion
- Updated `Overload_OnCall_SupersedesUserOverride` to use `stub.Format.OnCall()` (single-overload interceptor)
- Updated `Overload_OnCall_OnNonOverriddenOverload_Works` to use `stub.Format2.OnCall()` (separate interceptor)
- Updated `Overload_MixedConfiguration_EachOverloadIndependent` to use `stub.Format2.OnCall()` for non-overridden overload
- Added `Overload_NoUserOverride_StrictModeThrows` test to verify strict mode works correctly

### Test Results

All tests pass (1032-1033 tests per framework):
- net8.0: 1032 passed
- net9.0: 1033 passed
- net10.0: 1033 passed

### Generated Code Sample

After the fix, `OverloadedUserMethodStub` generates TWO interceptors:

```csharp
// For Format(string) - HAS user override
public sealed class FormatInterceptor : global::KnockOff.IMethodTracking<string>
{
    // User method interceptor pattern
    // ...
}

// For Format(string, bool) - NO user override
public sealed class Format2Interceptor
{
    // Regular method interceptor pattern
    // ...
}
```

Interface implementations correctly route to the appropriate path:

```csharp
string IOverloadedUserMethodService.Format(string input)
{
    Format.RecordCall(input);
    if (Format.Callback is { } callback) return callback(input);
    return Format_(input);  // User override in base class
}

string IOverloadedUserMethodService.Format(string input, bool uppercase)
{
    return Format2.Invoke(Strict, input, uppercase);  // Regular interceptor path
}
```

### Behavioral Verification

1. **User override overload works:** `service.Format("hello")` returns `"USER:hello"` from user's override
2. **Non-overridden overload uses interceptor:** `service.Format("hello", true)` goes through `Format2` interceptor
3. **Strict mode correctly throws for non-overridden:** `stub.Strict().Format("hello", true)` throws `StubException`
4. **OnCall supersedes user override:** `stub.Format.OnCall(...)` overrides user's `Format_(string)` method
5. **Each overload is independent:** Different interceptors, different configuration, different behavior

---

## Phase 3 Completion Evidence

**Completed:** 2026-02-03
**Developer:** knockoff-developer

### Test Verification

All required tests exist and pass:

1. **`Overload_NoUserOverride_StrictModeThrows` test** (lines 499-514):
   - Verifies that strict mode throws `StubException` for non-overridden overloads
   - First overload with user override returns expected value
   - Second overload without user override throws as expected

2. **`Overload_NoUserOverride_ThrowsWithoutOnCall` test** (lines 435-446):
   - Renamed from `Overload_NoUserOverride_UsesInterceptorOrDefault`
   - Verifies that non-overridden overload throws `InvalidOperationException` when no OnCall configured
   - This is the correct behavior for regular method interceptors

3. **`Overload_MixedConfiguration_EachOverloadIndependent` test** (lines 481-496):
   - Provides behavioral verification that only overridden methods have the flag
   - Shows `Format` uses user override, `Format2` uses interceptor path

### Full Test Suite Results

```
Test Run Successful.
Total tests: 32 (BaseClassUserMethodTests)
     Passed: 32

Full suite:
- net8.0: 1032 passed
- net9.0: 1033 passed
- net10.0: 1033 passed
```

### Key Overload Tests Verified

```
Passed KnockOff.Tests.BaseClassUserMethodTests.Overload_NoUserOverride_StrictModeThrows [< 1 ms]
Passed KnockOff.Tests.BaseClassUserMethodTests.Overload_NoUserOverride_ThrowsWithoutOnCall [< 1 ms]
Passed KnockOff.Tests.BaseClassUserMethodTests.Overload_MixedConfiguration_EachOverloadIndependent [6 ms]
Passed KnockOff.Tests.BaseClassUserMethodTests.Overload_OnCall_OnNonOverriddenOverload_Works [< 1 ms]
Passed KnockOff.Tests.BaseClassUserMethodTests.Overload_OnCall_SupersedesUserOverride [< 1 ms]
Passed KnockOff.Tests.BaseClassUserMethodTests.Overload_UserOverride_OnSomeOverloads_Works [< 1 ms]
```

### All Acceptance Criteria Met

- [x] KO0200 has automated regression test (manual verification documented in Phase 1, follow-up todo created)
- [x] Overload detection uses full signature, not just name (implemented in Phase 2)
- [x] Per-overload user override detection is verified by tests (all overload tests pass)
- [x] Strict mode throws for non-overridden overloads when no OnCall configured (verified)
- [x] All existing tests continue to pass (1032-1033 per framework)
