# Base Class Approach for User Methods

**Status:** Complete
**Priority:** High
**Created:** 2026-02-02
**Last Updated:** 2026-02-03

---

## Problem

The current user methods feature has two significant issues:

1. **The '2' postfix is ugly**: When a user defines a protected method like `GetUserById(int id)`, the tracker property becomes `GetUserById2` because the name `GetUserById` is reserved by the user's method. This naming is confusing and aesthetically poor.

2. **Signature changes are silent**: If the interface method signature changes (e.g., `GetById(int id)` → `GetById(string id)`), the user's protected method no longer matches. The generator silently stops recognizing it as a user method, creates a regular interceptor, and the orphaned protected method is never called. **There is no compile-time error.**

## Solution

Generate a base class with virtual protected methods that users must override. This provides:

1. **Clean tracker names**: Trackers use the method name directly (`stub.GetById`), no '2' suffix needed
2. **Compile-time signature enforcement**: If user's override doesn't match, compiler error: "no suitable method to override"
3. **IntelliSense discovery**: Users see available methods to override when typing in the stub class

### Key Insight

In C#, a **property** and a **method with parameters** can have the same name when the property is on a derived class hiding an inherited method. But actually... they can coexist naturally because they're different member kinds distinguished by call syntax:
- `GetById` (no parens) → property
- `GetById(id)` (with parens) → method

**CORRECTION**: This doesn't work. C# does not allow a property and method with the same name in the same class, even with different signatures. The solution is to suffix the base class virtual methods with `_`:

```csharp
// Generated base class
public class RepoStubBase {
    protected virtual Task<Order> GetById_(int id) { throw new NotImplementedException(); }
}

// User writes override
protected override Task<Order> GetById_(int id) => ...;

// Generated partial - clean tracker names
public GetByIdInterceptor GetById { get; }  // No suffix!
```

---

## Plans

- [Base Class User Methods Design](../plans/base-class-user-methods-design.md)
- [Base Class Follow-up Fixes](../plans/base-class-followup-fixes.md)

---

## Tasks

- [x] Analyze overload handling with base class approach
- [x] Analyze generic method handling with base class approach
- [x] Determine if properties should be supported (currently methods only)
- [x] Resolve source generator timing question (SYNTACTIC detection works!)
- [x] Investigate exception-free fallback (conditional generation solves this)
- [ ] Design the generated base class structure
- [ ] Handle edge case: user already has a base class (block with KO0200)
- [ ] Implementation planning

---

## Progress Log

### 2026-02-02 - Initial Exploration

Explored the base class approach through conversation. Key findings:

1. **Current implementation**: User methods are detected by matching protected method signatures to interface methods. Name collision causes '2' suffix on trackers.

2. **Base class approach viable**: Generator creates `{ClassName}Base` with virtual methods. Users write `protected override`. Signature mismatches cause compile errors.

3. **Naming conflict resolution**: Property and method can't share names in C#. Solution: suffix base class methods with `_` (e.g., `GetById_`). Tracker properties keep clean names (`GetById`).

4. **Properties not currently supported**: `GetUserDefinedMethods()` explicitly filters with `!member.IsProperty`. Only methods are user-definable today.

### 2026-02-02 - Overloads, Generics, and Properties Analysis

**Overloads:** Work naturally. Each overload becomes a separate virtual method in base class (`Format_(string)`, `Format_(string, FormatOptions)`, etc.). Users can override any subset. Non-overridden overloads use interceptor.

**Generic methods:** Recommend EXCLUDING from base class pattern. Current `.Of<T>()` pattern is already good for type-specific configuration. User override would be a single method for all type arguments, losing the per-type flexibility.

**Properties:** Currently NOT supported (code explicitly filters `!member.IsProperty`). The base class pattern could work for properties, but defer to Phase 2. Methods are higher priority and more common use case.

### 2026-02-02 - Syntactic Override Detection Breakthrough

**Performance Concern Raised:** Throwing `NotImplementedException` in base class virtual methods would drastically slow tests. Exception handling is expensive in .NET.

**Investigation Result:** Syntactic detection IS possible!

Key insight: The `override` keyword is a **syntax token**, not a semantic property. We can detect it via:
- `classSymbol.DeclaringSyntaxReferences` - gives all partial class declarations
- `MethodDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword))` - works without base class

**Solution:** Conditional code generation:
- If override detected syntactically: generate `return GetById_(id);`
- If no override detected: generate `return Task.FromResult<Order>(default!);` (no exception!)

This gives us:
1. No exceptions in hot paths (critical for test performance)
2. No runtime reflection needed
3. Clean names, signature enforcement, IntelliSense - all preserved
4. Compatible with incremental generation

### 2026-02-02 - Developer Concerns Addressed

Updated plan to address 6 developer concerns:

1. **Generic standalone stubs**: Documented how `RepoStub<T>` generates `RepoStubBase<T>` with type parameters and constraints propagated.

2. **Test strategy**: Defined 7 new test categories and enumerated 2 existing test files requiring migration (47 total references to `*2` pattern).

3. **Base class file structure**: Decided on two files per stub (`{ClassName}.Base.g.cs` and `{ClassName}.g.cs`).

4. **Model/Builder/Renderer responsibilities**: Mapped changes to each pipeline component. Transform detects overrides syntactically, Builder populates `HasUserOverride`, Renderer conditionally generates override vs interceptor path.

5. **Existing tests migration**: Detailed the changes needed (add `override`, add `_` suffix, remove `2` from tracker access).

6. **Source delegation + user override interaction**: Documented the priority chain: OnCall > Source > User Override > Strict > Default. Key insight: generator produces different code based on override detection, no runtime check needed.

---

## Results / Conclusions

### Completed Successfully

The base class pattern for user methods has been implemented and all follow-up issues resolved.

**Key Accomplishments:**

1. **Base Class Pattern Implemented** - Standalone stubs now generate a `{ClassName}Base` class with virtual methods suffixed with `_`. Users can `override` these methods to provide default behavior.

2. **Clean Interceptor Names** - Interceptors use clean names (`stub.GetValue`) instead of the `2` suffix (`stub.GetValue2`).

3. **Compile-Time Signature Enforcement** - If the interface method signature changes, user overrides no longer match and the compiler reports "no suitable method to override".

4. **Syntactic Override Detection** - Overrides are detected at generation time via `OverrideKeyword` syntax token, enabling conditional code generation with no runtime exceptions.

5. **KO0200 Diagnostic** - Standalone stubs with user-defined base classes are blocked with a clear error message.

6. **Per-Overload Detection** - Fixed issue where overriding one method overload incorrectly flagged all overloads. Now uses full signature matching (`MethodName_(ParamType1,ParamType2,...)`).

**Follow-up Created:**
- `docs/todos/diagnostic-test-infrastructure.md` - Track future work for CSharpGeneratorDriver-based diagnostic testing.

**Test Results:**
- All 1032-1033 tests pass per framework (net8.0, net9.0, net10.0)
- 32 dedicated base class tests cover all scenarios

