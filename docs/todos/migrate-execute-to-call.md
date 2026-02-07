# Migrate Execute to Call in Void Method API

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-07
**Last Updated:** 2026-02-07

---

## Problem

The v0.38.0 unify-returns-execute API renamed void method callbacks from `OnCall` to `Execute` and `ThenCall` to `ThenExecute`. The goal was to differentiate void from non-void methods, but "Execute" doesn't feel right. The desired API is:

- Non-void: `.Returns(callback)` / `.ThenReturns(callback)` (keep as-is)
- Void: `.Call(callback)` / `.ThenCall(callback)` (currently `.Execute()` / `.ThenExecute()`)

After this change, "Execute" should not appear in the user-facing API.

## Solution

Rename all user-facing `Execute`/`ThenExecute` to `Call`/`ThenCall` across:

### Public Interfaces (src/KnockOff/)

| Current | Target |
|---------|--------|
| `IMethodExecuteBuilder<T>` | `IMethodCallBuilder<T>` |
| `IMethodExecuteBuilder<T, TArg>` | `IMethodCallBuilder<T, TArg>` |
| `IMethodExecuteBuilderArgs<T, TArgs>` | `IMethodCallBuilderArgs<T, TArgs>` |
| `IMethodExecuteSequence` | `IMethodCallSequence` |
| `IMethodExecuteSequence<T>` | `IMethodCallSequence<T>` |
| `IMethodExecuteBuilder.cs` (file) | `IMethodCallBuilder.cs` |
| `IMethodExecuteSequence.cs` (file) | `IMethodCallSequence.cs` |
| `.Execute(callback)` on `IVoidWhenChain` | `.Call(callback)` |
| `.ThenExecute(callback)` on `IVoidWhenChain` | `.ThenCall(callback)` |
| `.ThenExecute(callback)` on builders/sequences | `.ThenCall(callback)` |

### Generator (src/Generator/)

| File | What to Change |
|------|---------------|
| `MethodInterceptorRenderer.cs` | `"Execute"` → `"Call"` for void entry point name |
| `MethodInterceptorRenderer.cs` | `"ThenExecute"` → `"ThenCall"` for void sequence chaining |
| `MethodInterceptorRenderer.cs` | `IMethodExecuteBuilder` → `IMethodCallBuilder` references |
| `MethodInterceptorRenderer.cs` | `IMethodExecuteSequence` → `IMethodCallSequence` references |
| `UnifiedInterceptorBuilder.cs` | `IMethodExecuteBuilder` → `IMethodCallBuilder` references |
| `ModelAdapters.cs` | `IMethodExecuteBuilder` → `IMethodCallBuilder` references |

### Internal Generated Code

Also rename — user wants consistent Call nomenclature throughout, not just public API:

- `matcher.Execute(...)` → `matcher.Call(...)` — internal dispatch method on generated When matcher classes
- `StandaloneClassRenderer.cs` `Execute_()` — protected forwarder method (review if related to Call/ThenCall)
- Error messages mentioning "Execute" (e.g., "Configure via Returns or Execute" → "Configure via Returns or Call")
- Comments referencing "Execute" in the context of void method callbacks

### Documentation, Design, Skills, Tests

All files updated in v0.38.0 for the Execute rename need to be updated again for Call.

---

## Plans

---

## Tasks

- [ ] Review all uses of "Execute" in src/KnockOff/, src/Generator/, src/Design/, tests, docs, skills
- [ ] Rename public interfaces (IMethodExecuteBuilder → IMethodCallBuilder, IMethodExecuteSequence → IMethodCallSequence)
- [ ] Rename IVoidWhenChain.Execute → .Call, .ThenExecute → .ThenCall
- [ ] Update generator to emit Call/ThenCall instead of Execute/ThenExecute
- [ ] Update builder references in UnifiedInterceptorBuilder.cs and ModelAdapters.cs
- [ ] Rename internal matcher Execute() → Call() in generated code
- [ ] Update error messages mentioning Execute
- [ ] Update Design.Stubs and Design.Tests
- [ ] Update Documentation.Samples and run dotnet mdsnippets
- [ ] Update skills
- [ ] Verify all tests pass
- [ ] Version bump

---

## Progress Log

### 2026-02-07
- Created todo from user feedback on v0.38.0 API naming

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully
- [ ] Design project tests pass

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]

---

## Results / Conclusions

