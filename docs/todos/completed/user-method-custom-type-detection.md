# User Method Detection Fails for Custom Type Parameters

**Status:** Complete
**Priority:** High
**Created:** 2026-02-05
**Last Updated:** 2026-02-05

---

## Problem

User method override detection fails when interface method parameters use custom (non-primitive) types. The detection uses syntax-based type names (e.g., `"User"`) while the matching uses semantic-model fully-qualified type names (e.g., `"KnockOff.Documentation.Samples.User"`). These don't match, so the user method is not recognized.

**Root cause:** `BuildOverrideSignatureKey` in `KnockOffGenerator.Helpers.cs` (detection side) uses `p.Type?.ToString()` which returns the type as written in source. `BuildOverrideSignatureKey` in `SymbolHelpers.cs` (matching side) uses `ParameterInfo.Type` which stores `p.Type.ToDisplayString(FullyQualifiedWithNullability)` — the fully qualified form.

**Example:**

Interface: `void Update(User user)` — semantic model stores parameter as `global::KnockOff.Documentation.Samples.User`
User method: `protected override void Update_(User user)` — syntax has parameter as `User`

- Detection key: `"Update_(User)"`
- Matching key: `"Update_(KnockOff.Documentation.Samples.User)"`
- Result: **No match** — user method not detected

For primitive types this works because both sides normalize to keywords:
- `int` → `int`, `string` → `string`, etc.

**Consequence:** When detection fails, the generated interceptor:
1. Does NOT receive the stub instance in `Invoke()`
2. Does NOT call the user method as fallback
3. Falls back to `_source` (Source pattern) instead
4. Is included in `VerifyAll()` (user method members are excluded)

**Discovered in:** `MyRepoStub.g.cs` — `GetUser(int id)` correctly detects `GetUser_` (primitive param), but `Update(User user)` fails to detect `Update_` (custom type param).

**Affects:** All standalone patterns (1-4) with user methods that have custom type parameters.

## Solution

Fix the type normalization in `BuildOverrideSignatureKey` (Helpers.cs) to match the normalization in `BuildOverrideSignatureKey` (SymbolHelpers.cs). Either:
1. Use the semantic model in the detection path (preferred), or
2. Normalize the matching path to strip namespace qualifiers

---

## Plans

- [Fix User Method Detection for Custom Type Parameters](../plans/user-method-type-detection-fix.md)

---

## Tasks

- [x] Architect confirms bug and creates failing design tests
- [x] Create implementation plan
- [x] Fix generator type normalization
- [x] Verify all existing tests still pass
- [x] Add tests for user methods with custom type parameters

---

## Progress Log

**2026-02-05**: Created todo. Initially suspected void-specific issue, but root cause analysis revealed it's a type normalization mismatch in user method detection. Primitive types (int, string) work because both paths normalize to keywords. Custom types fail because syntax gives short name ("User") while semantic model gives fully qualified name ("Namespace.User").

**2026-02-05**: Architect confirmed bug via full code trace and created 5 failing tests in `src/Tests/KnockOffTests/UserMethodCustomTypeDetectionTests.cs`. Three tests FAIL (custom type params), two PASS as controls (primitive params and OnCall override). Verified by examining generated code: `CustomTypeUserMethodStub.g.cs` shows `GetById` interceptor receives `stub` parameter in `Invoke()` (user method detected), while `FindUser`, `SaveUser`, and `UpdateUser` interceptors do NOT receive `stub` (user method not detected). Also confirmed `MyRepoStub.g.cs` exhibits the same pattern: `GetUser(int)` has `stub.GetUser_(id)` fallback, `Update(User)` does not. All existing user method tests continue to pass.

---

## Results / Conclusions

**Fixed.** Changed `BuildOverrideSignatureKey` in `KnockOffGenerator.Helpers.cs` to resolve parameter types via the semantic model (`SemanticModel.GetTypeInfo(p.Type).Type.ToDisplayString(FullyQualifiedWithNullability)`) instead of syntax text (`p.Type?.ToString()`). This produces fully-qualified type names that match the keys built by the matching side in `SymbolHelpers.cs`.

`DetectUserOverrideMethods` now accepts `Compilation` and calls `compilation.GetSemanticModel(syntaxRef.SyntaxTree)` per partial declaration to handle methods in different syntax trees. Falls back to syntax text if semantic resolution fails.

**3 files changed:** `KnockOffGenerator.Helpers.cs` (core fix), `KnockOffGenerator.Transform.cs` (pass compilation), `KnockOffGenerator.StandaloneClass.cs` (pass compilation). No changes to builders, renderers, or model types — the equatable model graph stores only strings.

**6 previously-failing tests now pass** (3 in `UserMethodCustomTypeDetectionTests`, 3 in `VoidUserMethodFallbackTests`). Zero regressions.
