# User Method Overload Generator Bug

**Status:** Open
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

---

## Tasks

- [ ] Investigate generator code path for user method overloads
- [ ] Choose solution approach
- [ ] Implement fix in generator
- [ ] Add tests for user method overloads
- [ ] Update Design.Stubs to enable commented-out tests
- [ ] Update documentation

---

## Progress Log

**2026-02-02:** Bug discovered while creating user method design documentation in `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs`. See that file for detailed analysis and disabled test code.

---

## Results / Conclusions
