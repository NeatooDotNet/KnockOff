# Delegate Stub OnCall Method Syntax

**Status:** Complete
**Priority:** High
**Created:** 2026-01-24
**Last Updated:** 2026-01-24
**Plan Status:** Complete

---

## Problem

Delegate stubs (`[KnockOff<MyDelegate>]`) generate `OnCall` as a **property** with assignment syntax:

```csharp
stub.Interceptor.OnCall = (msg) => captured = msg;
```

This is inconsistent with the method-based API used for all other interceptors (methods, properties, indexers), which use **method** syntax:

```csharp
stub.GetUser.OnCall((id) => new User { Id = id });
```

The recent work in commit 434f7a4 ("feat!: unify OnGet/OnSet to method syntax with tracking returns") unified OnGet/OnSet to method syntax, but delegate stubs were not included in that change.

## Solution

Convert delegate stub `OnCall` from a property to a method to match the unified API:

**Before:**
```csharp
stub.Interceptor.OnCall = (msg) => captured = msg;
```

**After:**
```csharp
stub.Interceptor.OnCall((msg) => captured = msg);
```

This requires:
1. Updating `InlineRenderer.RenderDelegateStub()` to generate `OnCall` as a method
2. Updating all tests that use delegate stub `OnCall` assignment
3. Updating documentation examples

---

## Plans

- [Delegate Stub OnCall Method Syntax Implementation](../plans/delegate-stub-oncall-method-syntax.md)

---

## Tasks

- [x] Update InlineRenderer.RenderDelegateStub() to generate OnCall as method
- [x] Update InlineStubTests.cs delegate stub tests (8 occurrences + 2 assertion removals)
- [x] Update NeatooTests.cs delegate tests (4 occurrences)
- [x] Update OpenGenericInlineStubTests.cs delegate tests (2 occurrences)
- [x] Update INotifyNeatooPropertyChangedTests.cs delegate tests (4 occurrences)
- [x] Verify generated code compiles and tests pass
- [ ] Update documentation with new syntax (out of scope per plan - tracked separately)

---

## Progress Log

**2026-01-24:** Created todo. Identified 18 test file occurrences using property assignment syntax across 4 test files.

**2026-01-24:** Implementation complete.
- Updated `InlineRenderer.RenderDelegateStub()` to generate `OnCall` as method with internal `_onCall` field
- Updated 18 test usages across 4 test files from property assignment to method call syntax
- Removed 2 `Assert.NotNull(stub.Interceptor.OnCall)` assertions (OnCall is now a method)
- All 2250 tests pass across net8.0, net9.0, net10.0

---

## Results / Conclusions

Successfully converted delegate stub `OnCall` from property to method syntax, achieving API consistency with method/property/indexer interceptors.

**Key implementation details:**
1. Changed `_onCall` field from `private` to `internal` to allow the stub class to access it
2. Replaced property getter/setter with void method that sets the backing field
3. Updated `Invoke` method to reference `Interceptor._onCall` instead of `Interceptor.OnCall`

**Breaking change:** Users must update their code from:
```csharp
stub.Interceptor.OnCall = callback;
```
to:
```csharp
stub.Interceptor.OnCall(callback);
```

**Files changed:**
- `src/Generator/Renderer/InlineRenderer.cs`
- `src/Tests/KnockOffTests/InlineStubTests.cs`
- `src/Tests/KnockOffTests/NeatooTests.cs`
- `src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs`
- `src/Tests/KnockOff.NeatooInterfaceTests/Notifications/INotifyNeatooPropertyChangedTests.cs`

