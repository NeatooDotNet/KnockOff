# User Method OnCall Support

**Status:** Complete
**Priority:** High
**Created:** 2026-02-02
**Last Updated:** 2026-02-02 (Implementation Complete)

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

- [User Method OnCall Implementation Plan](../plans/user-method-oncall-implementation.md)

---

## Tasks

- [x] Update user method interceptor generation to include OnCall delegate
- [x] Add Returns() convenience method for methods with return values
- [x] Update interface implementation to check callback before calling user method
- [x] Ensure void methods get OnCall (no Returns)
- [x] Add tests for OnCall superseding user method
- [x] Add tests for Returns superseding user method
- [x] Add tests for fallback to user method when no callback
- [x] Update Design.Stubs exploration to demonstrate new API
- [x] Update documentation

---

## Progress Log

**2026-02-02:** Created todo. Inconsistency discovered during user method exploration in `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs`. User confirmed design intent: shareable stubs with configurable overrides per test.

**2026-02-02:** Architectural design completed. Plan created at `docs/plans/user-method-oncall-implementation.md`. Key findings: FlatMethodModel already contains delegate type info, changes isolated to FlatRenderer.cs (interceptor class rendering and interface implementation rendering).

**2026-02-02:** Implementation completed. All phases done:
- Phase 1: Added OnCall, Returns, Callback to user method interceptor rendering
- Phase 2: Added callback check to interface implementation
- Phase 3: Added 16 new tests for OnCall/Returns functionality
- Phase 4: Updated Design.Stubs documentation

---

## Results / Conclusions

Implementation successful. Non-generic user method interceptors now support OnCall() and Returns():

1. **OnCall supersedes user method** - When configured, the callback is invoked instead of the user method
2. **User method is fallback** - When no callback configured, user method is called (backward compatible)
3. **Returns auto-wraps async** - For Task<T> methods, `Returns(value)` generates `OnCall(_ => Task.FromResult(value))`
4. **Reset preserves config** - Matches regular interceptor semantics

Key files changed:
- `src/Generator/Renderer/FlatRenderer.cs`: Modified `RenderUserMethodInterceptorClass` and `RenderUserMethodImplementation`
- `src/Tests/KnockOffTests/UserMethodOnCallTests.cs`: New test file with 16 tests
- `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs`: Updated documentation

