# Migrate Method API to Singular Naming (Return/Call)

**Status:** Superseded by [IntelliSense API Redesign](intellisense-api-redesign.md)
**Priority:** High
**Created:** 2026-02-07
**Last Updated:** 2026-02-07 (Phases 1-4 implemented, awaiting architect verification)

---

## Problem

The v0.38.0 API uses plural names (`Returns`/`ThenReturns`) and `Execute`/`ThenExecute`. Two problems:

1. **"Execute" doesn't feel right** for void methods — should be `Call`/`ThenCall`
2. **Plural is inconsistent** — `Call`/`ThenCall` are singular, so `Returns`/`ThenReturns` should be `Return`/`ThenReturn`

The desired final API:

- Non-void: `.Return(callback)` / `.ThenReturn(callback)` (currently `.Returns()` / `.ThenReturns()`)
- Void: `.Call(callback)` / `.ThenCall(callback)` (currently `.Execute()` / `.ThenExecute()`)

Additionally, the `.Of<T>().OnCall()` typed handler API (for generic methods) was a known gap from v0.38.0 — it still uses `OnCall` for both void and non-void. This todo also renames those:

- Non-void generic: `.Of<T>().OnCall(callback)` → `.Of<T>().Return(callback)`
- Void generic: `.Of<T>().OnCall(callback)` → `.Of<T>().Call(callback)`

After this change, `Execute`, `OnCall`, `Returns`, and `ThenReturns` should not appear in the user-facing API.

## Solution

Three renames across the entire codebase:

1. **Execute → Call** (void methods): `.Execute()` → `.Call()`, `.ThenExecute()` → `.ThenCall()`
2. **Returns → Return** (non-void methods): `.Returns()` → `.Return()`, `.ThenReturns()` → `.ThenReturn()`
3. **OnCall → Return/Call** (typed handlers): `.Of<T>().OnCall()` → `.Of<T>().Return()` or `.Of<T>().Call()`

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
| `IMethodReturnsBuilder<T>` | `IMethodReturnBuilder<T>` |
| `IMethodReturnsBuilder<T, TArg>` | `IMethodReturnBuilder<T, TArg>` |
| `IMethodReturnsBuilderArgs<T, TArgs>` | `IMethodReturnBuilderArgs<T, TArgs>` |
| `IMethodReturnsSequence` | `IMethodReturnSequence` |
| `IMethodReturnsSequence<T>` | `IMethodReturnSequence<T>` |
| `IMethodReturnsBuilder.cs` (file) | `IMethodReturnBuilder.cs` |
| `IMethodReturnsSequence.cs` (file) | `IMethodReturnSequence.cs` |
| `.Execute(callback)` on `IVoidWhenChain` | `.Call(callback)` |
| `.ThenExecute(callback)` on `IVoidWhenChain` | `.ThenCall(callback)` |
| `.ThenExecute(callback)` on builders/sequences | `.ThenCall(callback)` |
| `.ThenReturns(callback)` on builders/sequences | `.ThenReturn(callback)` |

### Typed Handler OnCall → Return/Call (src/Generator/)

| File | What to Change |
|------|---------------|
| `FlatRenderer.cs` | `.OnCall(callback)` → `.Return(callback)` (non-void) / `.Call(callback)` (void) on typed handlers |
| `FlatRenderer.cs` | Overload variants `OnCall{Suffix}` → `Return{Suffix}` / `Call{Suffix}` |
| `FlatRenderer.cs` | Error messages referencing `OnCall` |
| `InlineRenderer.cs` | Same typed handler `OnCall` → `Return`/`Call` split |
| `InlineRenderer.cs` | Error messages and comments referencing `OnCall` |

### Generator — Execute → Call, Returns → Return (src/Generator/)

| File | What to Change |
|------|---------------|
| `MethodInterceptorRenderer.cs` | `"Execute"` → `"Call"` for void entry point name |
| `MethodInterceptorRenderer.cs` | `"Returns"` → `"Return"` for non-void entry point name |
| `MethodInterceptorRenderer.cs` | `"ThenExecute"` → `"ThenCall"` for void sequence chaining |
| `MethodInterceptorRenderer.cs` | `"ThenReturns"` → `"ThenReturn"` for non-void sequence chaining |
| `MethodInterceptorRenderer.cs` | `IMethodExecuteBuilder` → `IMethodCallBuilder` references |
| `MethodInterceptorRenderer.cs` | `IMethodExecuteSequence` → `IMethodCallSequence` references |
| `MethodInterceptorRenderer.cs` | `IMethodReturnsBuilder` → `IMethodReturnBuilder` references |
| `MethodInterceptorRenderer.cs` | `IMethodReturnsSequence` → `IMethodReturnSequence` references |
| `UnifiedInterceptorBuilder.cs` | `IMethodExecuteBuilder` → `IMethodCallBuilder`, `IMethodReturnsBuilder` → `IMethodReturnBuilder` |
| `ModelAdapters.cs` | Same interface reference updates |

### Internal Generated Code

Also rename — user wants consistent nomenclature throughout, not just public API:

- `matcher.Execute(...)` → `matcher.Call(...)` — internal dispatch method on generated When matcher classes
- `StandaloneClassRenderer.cs` `Execute_()` — NOT in scope (domain method name, not void callback API)
- Error messages mentioning "Execute" or "Returns" (e.g., "Configure via Returns or Execute" → "Configure via Return or Call")
- Comments referencing "Execute" or "Returns" in the context of method callbacks

### Documentation, Design, Skills, Tests

All files updated in v0.38.0 for the Execute rename need to be updated again for Call.

---

## Plans

- [Migrate Execute to Call Design](../plans/migrate-execute-to-call.md)

---

## Tasks

- [ ] Review all uses of "Execute", "Returns", "ThenReturns", and "OnCall" in src/KnockOff/, src/Generator/, src/Design/, tests, docs, skills
- [ ] Rename void interfaces (IMethodExecuteBuilder → IMethodCallBuilder, IMethodExecuteSequence → IMethodCallSequence)
- [ ] Rename non-void interfaces (IMethodReturnsBuilder → IMethodReturnBuilder, IMethodReturnsSequence → IMethodReturnSequence)
- [ ] Rename IVoidWhenChain.Execute → .Call, .ThenExecute → .ThenCall
- [ ] Update generator to emit Call/ThenCall and Return/ThenReturn instead of Execute/ThenExecute and Returns/ThenReturns
- [ ] Update builder references in UnifiedInterceptorBuilder.cs and ModelAdapters.cs
- [ ] Rename internal matcher Execute() → Call() in generated code
- [ ] Update error messages mentioning Execute or Returns
- [ ] Rename typed handler .Of<T>().OnCall() → .Return() (non-void) / .Call() (void) in FlatRenderer.cs and InlineRenderer.cs
- [ ] Update error messages and comments referencing OnCall in typed handler code
- [ ] Update Design.Stubs and Design.Tests
- [ ] Update Documentation.Samples and run dotnet mdsnippets
- [ ] Update skills
- [ ] Verify all tests pass
- [ ] Version bump

---

## Progress Log

### 2026-02-07
- Created todo from user feedback on v0.38.0 API naming
- Architect completed codebase audit and created implementation plan
- StandaloneClassRenderer.cs `Execute_()` confirmed out of scope (domain method forwarder, not void callback API)
- Plan linked at docs/plans/migrate-execute-to-call.md
- Scope expanded: folded typed handler `.Of<T>().OnCall()` -> `.Returns()`/`.Call()` rename into same plan
- Architect audited typed handler code in FlatRenderer.cs and InlineRenderer.cs, confirmed `IsVoid` available on all handler models
- Updated plan with Phase 2g (typed handler renderers), expanded Phases 3-5 for OnCall references, updated pipeline analysis, scope table, acceptance criteria, codebase deep-dive
- Plan status set to "Under Review (Developer)" -- prior developer approval invalidated by scope expansion
- Internal model properties (`OnCallDelegateType`, `OnCallArgs`, `OnCallArgumentList`) and internal fields (`_onCall`) confirmed out of scope (not user-facing)
- Scope expanded again: `Returns` → `Return`, `ThenReturns` → `ThenReturn` (singular naming consistency)
- This adds IMethodReturnsBuilder → IMethodReturnBuilder, IMethodReturnsSequence → IMethodReturnSequence to the rename scope
- Architect investigated IWhenBuilder.Returns(value) — confirmed it becomes .Return(value) (user wants all plural gone)
- Architect audited Returns/ThenReturns occurrences: ~600 in KnockOffTests, ~468 in Documentation.Samples, ~445 in Design, ~259 in skills, ~51 in NeatooInterfaceTests
- Internal generated fields (_returnsValue, _hasReturnsValue, _returnsValueTracking) confirmed out of scope — private, not user-facing
- Plan fully updated with three-rename scope: Returns->Return, Execute->Call, OnCall->Return/Call
- Added new "Generator Changes -- Returns -> Return" design section, updated scope table, acceptance criteria, pipeline analysis, risks, codebase deep-dive
- Updated typed handler references from "Returns" to "Return" (singular) throughout plan
- Plan status set to "Under Review (Developer)" — fresh developer review required for full three-rename scope

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

