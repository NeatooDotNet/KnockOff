# User Method OnCall Support

**Status:** Open
**Priority:** High
**Created:** 2026-02-02
**Last Updated:** 2026-02-02

---

## Problem

Non-generic user method interceptors are tracking-only (Verify, LastArg, Reset, Verifiable). Generic user method interceptors already have OnCall through their typed handlers. This inconsistency prevents a key use case: shareable stubs with sensible defaults that individual tests can override.

**Current behavior:**
```csharp
// User method provides default
protected string Process(string input) => $"[Default: {input}]";

// Test wants different behavior - NO WAY TO DO THIS
stub.Process2.OnCall(...);  // ❌ OnCall doesn't exist
```

**Generic user methods already work this way:**
```csharp
// GenericUserMethodStub.g.cs shows the pattern:
if (Create2.Of<T>().Callback is { } callback)
    return callback();  // OnCall configured - use it
return Create<T>();     // No OnCall - use user method
```

---

## Solution

Add OnCall() and Returns() to non-generic user method interceptors. When configured, they supersede the user method. User method becomes the fallback when no callback is configured.

**Target API:**
```csharp
// User method as shareable default
protected string Process(string input) => $"[Default: {input}]";

// Test overrides when needed
stub.Process2.OnCall(input => $"[Test: {input}]");
// or
stub.Process2.Returns("fixed value");

// Generated implementation pattern:
string IService.Process(string input)
{
    Process2.RecordCall(input);
    if (Process2.Callback is { } callback)
        return callback(input);  // OnCall supersedes
    return Process(input);       // User method fallback
}
```

---

## Scope

### Patterns Affected
- [x] Stand-alone stubs with user methods
- [ ] Inline interface - N/A (cannot have user methods)
- [ ] Inline class - N/A (cannot have user methods)
- [ ] Inline delegate - N/A (cannot have user methods)

### Member Types Affected
- [x] Methods (non-generic) - add OnCall/Returns
- [ ] Methods (generic) - already has OnCall through Of<T>()
- [ ] Properties - user methods for properties? (investigate)
- [ ] Indexers - user methods for indexers? (investigate)
- [ ] Events - N/A

---

## Plans

---

## Tasks

- [ ] Update user method interceptor generation to include OnCall delegate
- [ ] Add Returns() convenience method for methods with return values
- [ ] Update interface implementation to check callback before calling user method
- [ ] Ensure void methods get OnCall (no Returns)
- [ ] Add tests for OnCall superseding user method
- [ ] Add tests for Returns superseding user method
- [ ] Add tests for fallback to user method when no callback
- [ ] Update Design.Stubs exploration to demonstrate new API
- [ ] Update documentation

---

## Progress Log

**2026-02-02:** Created todo. Inconsistency discovered during user method exploration in `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs`. User confirmed design intent: shareable stubs with configurable overrides per test.

---

## Results / Conclusions

