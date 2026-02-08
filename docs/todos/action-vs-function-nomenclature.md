# Action vs Function Nomenclature and Generator Alignment

**Status:** Open
**Priority:** Medium
**Created:** 2026-02-07
**Last Updated:** 2026-02-07

---

## Problem

The generator currently treats `Task` and `ValueTask` returning methods as non-void (because the C# return type is technically non-void). This means methods like `Task DoWorkAsync()` get `Return`/`ThenReturn` instead of `Call`/`ThenCall`.

The correct distinction is not void vs non-void, but **Action vs Function**:

- **Action**: No return value the caller uses (`void`, `Task`, `ValueTask`) → `Call`/`ThenCall`
- **Function**: Produces a value the caller uses (`T`, `Task<T>`, `ValueTask<T>`) → `Return`/`ThenReturn`

This parallels C#'s `Action<>` vs `Func<>` distinction.

### Current incorrect behavior

```csharp
// Task DoWorkAsync() currently generates Return/ThenReturn
stub.DoWorkAsync.Return(() => Task.CompletedTask);  // wrong: Action, should be Call

// Task<string> GetDataAsync() correctly generates Return/ThenReturn
stub.GetDataAsync.Return(() => Task.FromResult("data"));  // correct: Function
```

### Expected behavior after fix

```csharp
// Task DoWorkAsync() should generate Call/ThenCall
stub.DoWorkAsync.Call(() => { });  // correct: Action

// Task<string> GetDataAsync() should generate Return/ThenReturn
stub.GetDataAsync.Return(() => Task.FromResult("data"));  // correct: Function
```

### Known instances of mixing in the codebase

- `src/Tests/KnockOffTests/WhenChainTests.cs` lines 423, 460: `.Return(100).ThenCall(...)` on non-void `Add` method (pre-existing)

---

## Solution

**Phase 1: Solidify the design and nomenclature.** Define exactly what "Action" and "Function" mean in the KnockOff context, update Design projects to reflect the correct API, and document the distinction.

**Phase 2: Bring the generator into alignment.** Update the generator to classify methods correctly and emit the right interceptor API.

---

## Plans

---

## Tasks

- [ ] Phase 1: Solidify the Action vs Function design and nomenclature in Design projects
- [ ] Phase 2: Update generator to classify `Task`/`ValueTask` methods as Action (not Function)
- [ ] Phase 2: Update generated interceptors to emit `Call`/`ThenCall` for Action methods
- [ ] Fix pre-existing WhenChainTests.cs mixing (lines 423, 460)
- [ ] Verify all existing tests still pass (or update to match new API)

---

## Progress Log

### 2026-02-07
- Discovered issue: generator treats `Task`/`ValueTask` as non-void, giving them `Return`/`ThenReturn` instead of `Call`/`ThenCall`
- Adopted nomenclature: **Action** (void, Task, ValueTask) vs **Function** (T, Task<T>, ValueTask<T>), paralleling C#'s `Action<>` vs `Func<>`
- Filed this todo

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully
- [ ] Design project tests pass
- [ ] All KnockOffTests pass

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]
- KnockOffTests: [Pending]

---

## Results / Conclusions

