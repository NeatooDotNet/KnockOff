# User Method Overload Generator Bug

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-02
**Last Updated:** 2026-02-02

---

## Problem

When an interface has overloaded methods and a stub provides user methods for multiple overloads, the generator produces invalid code that fails to compile.

**Root Cause:** The generator creates a single `*2Interceptor` class with one `RecordCall` signature based on the first overload's parameters. The explicit interface implementations for other overloads try to call `RecordCall` with different argument shapes (tuples), causing compilation errors.

**Example:**

```csharp
// Interface
public interface IFormatter
{
    string Format(string input);
    string Format(string input, bool uppercase);
    string Format(string input, bool uppercase, int maxLength);
}

// Stub with user methods for all overloads
[KnockOff]
public partial class FormatterStub : IFormatter { }

public partial class FormatterStub
{
    protected string Format(string input) => input.ToUpper();
    protected string Format(string input, bool uppercase) => uppercase ? input.ToUpper() : input;
    protected string Format(string input, bool uppercase, int maxLength) => input[..maxLength].ToUpper();
}
```

**Generated code (buggy):**

```csharp
class Format2Interceptor {
    void RecordCall(string input) { ... }  // Only first overload's signature!
}

// Interface implementations:
string IFormatter.Format(string input) {
    Format2.RecordCall(input);  // OK
    return Format(input);
}

string IFormatter.Format(string input, bool uppercase) {
    Format2.RecordCall((input, uppercase));  // ERROR: No matching overload!
    return Format(input, uppercase);
}
```

**Compiler errors:**
- `CS1503: Argument 1: cannot convert from '(string, bool)' to 'string'`
- `CS0128: A local variable or function named 'format2Failure' is already defined` (in Verify method)

---

## Solution

Fix the generator to handle user method overloads. Options:

### Option 1: Generate RecordCall overloads (Recommended)
Generate multiple `RecordCall` methods on the interceptor, one per interface overload:

```csharp
class Format2Interceptor {
    void RecordCall(string input) { _callCount++; _lastArg_1 = input; }
    void RecordCall(string input, bool uppercase) { _callCount++; _lastArgs_2 = (input, uppercase); }
    void RecordCall(string input, bool uppercase, int maxLength) { _callCount++; _lastArgs_3 = (input, uppercase, maxLength); }
}
```

**Pros:** Type-safe, consistent with regular method overload pattern
**Cons:** Multiple LastArg properties needed, API complexity

### Option 2: Track only call count
For overloaded user methods, only track call count (no LastArg/LastArgs):

```csharp
class Format2Interceptor {
    void RecordCall() { _callCount++; }  // No args stored
    // No LastArg property
}
```

**Pros:** Simple, avoids shape ambiguity
**Cons:** Loses argument capture capability

### Option 3: Separate interceptors per overload
Generate `Format2_1`, `Format2_2`, `Format2_3` interceptors:

```csharp
stub.Format2_1.Verify();  // 1-param overload
stub.Format2_2.Verify();  // 2-param overload
stub.Format2_3.Verify();  // 3-param overload
```

**Pros:** Full tracking per overload
**Cons:** Verbose API, inconsistent with regular method overloads

---

## Plans

- [User Method Overload Fix](../plans/user-method-overload-fix.md)

---

## Tasks

- [x] Investigate generator code path for user method overloads
- [x] Choose solution approach (Option 1: Generate RecordCall overloads)
- [x] Implement fix in generator
- [x] Add tests for user method overloads
- [x] Update Design.Stubs to enable commented-out tests
- [x] Update documentation

---

## Progress Log

**2026-02-02:** Bug discovered while creating user method design documentation in `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs`. See that file for detailed analysis and disabled test code.

**2026-02-02:** Architectural analysis completed by knockoff-architect. Root cause identified: user method interceptors are rendered individually with deduplication by `InterceptorClassName`, causing only the first overload's `RecordCall` signature to be generated. Recommended solution: Option 1 (generate RecordCall overloads), consistent with how regular method overloads work. See plan for implementation details.

**2026-02-02:** Developer concerns addressed by knockoff-architect:
- Generic user method overloads: Now IN SCOPE with complete design (Phase 5)
- FlatMethodGroup reuse: Confirmed - no new model type needed
- RenderVerifyMethods integration: Keep separate with architectural rationale documented

---

## Results / Conclusions

**Completed:** 2026-02-02

### Summary

Fixed the generator bug where user method overloads produced invalid code. The fix generates per-signature `RecordCall` methods for user method interceptors, consistent with how regular method overloads are handled.

### Implementation Highlights

1. **FlatMethodGroup reuse**: User method overloads are grouped using the existing `FlatMethodGroup` model, stored in `FlatGenerationUnit.UserMethodGroups`

2. **Per-signature generation**: User method interceptors now generate:
   - Per-signature `RecordCall_{suffix}` methods
   - Per-signature `LastArg_{suffix}` / `LastArgs_{suffix}` properties
   - Per-signature `OnCall_{suffix}` / `Returns_{suffix}` methods
   - Per-signature `Callback_{suffix}` properties
   - Aggregate `_callCount` and `Verify()` methods

3. **Partial coverage support**: When only some overloads have user methods, the generator correctly splits them:
   - User method overloads use `*2` interceptor with tracking-only behavior
   - Non-user-method overloads use a separate interceptor (`*3`, etc.) with full OnCall API

4. **Generic user method overloads**: Extended to support per-signature tracking within `Of<T>()` typed handlers

### Tests Enabled

All previously disabled test code is now enabled:
- `OverloadedUserMethodStub` - non-generic user method overloads
- `PartialOverloadUserMethodStub` - partial user method coverage
- `OverloadedGenericUserMethodStub` - generic user method overloads

### All Tests Pass

All 5,647+ tests pass across net8.0, net9.0, and net10.0 target frameworks.
