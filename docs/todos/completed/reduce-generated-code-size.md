# Reduce Generated Code Size

**Status:** Superseded by [IntelliSense API Redesign](intellisense-api-redesign.md) — direction reversed (fully generated classes prioritized over build time)
**Priority:** Medium
**Created:** 2026-02-13
**Last Updated:** 2026-02-13 (generic inner classes plan added)

---

## Problem

The KnockOff generator produces very large output files. The largest generated files are:

| Lines | File |
|------:|------|
| 21,464 | `Benchmarks/.../LargeServiceStub.g.cs` (intentional benchmark) |
| 17,234 | `KnockOffTests/.../DataReaderStubTests.Stubs.g.cs` |
| 14,532 | `KnockOffTests/.../DataRecordStubTests.Stubs.g.cs` |
| 14,185 | `NeatooInterfaceTests/.../IEntityListBaseTests.Stubs.g.cs` |
| 14,145 | `NeatooInterfaceTests/.../EntityListBaseStub.g.cs` |

Large generated files increase compile times, IDE lag, and make generated code harder to inspect when debugging.

## Solution

Move structurally identical interceptor logic into pre-compiled generic base classes in the KnockOff library. Generated interceptor classes inherit from these base classes and provide only the thin, method-specific overrides. This reduces per-interceptor generated code by ~85%, shifting that code from "generated and compiled every time" to "compiled once in the library."

Approach validated through exploratory research analyzing all interceptor types (method, property, indexer) across multiple generated stubs.

---

## Plans

- [Interceptor Base Class Prototype](../plans/interceptor-base-class-prototype.md) -- Validate that interceptor logic can be moved into pre-compiled generic base classes
- [Interceptor Base Class Generator Changes](../plans/interceptor-base-class-generator.md) -- Modify the KnockOff generator to emit code using validated base classes
- [Generic Inner Classes](../plans/generic-inner-classes.md) -- Make base class inner classes implement library interfaces directly, eliminating generated thin subclasses

---

## Tasks

- [x] Analyze generated code for repeated patterns and redundancy
- [x] Identify candidates for size reduction
- [x] Design approach to shorten output (base class approach)
- [x] Build prototype validating base class approach
- [ ] Modify generator to emit code using base classes
- [ ] Verify no behavioral regressions

---

## Progress Log

### 2026-02-13
- Surveyed generated file sizes across the solution
- Identified top 5 largest generated files
- Exploratory research: analyzed interceptor code structure, identified ~85% per-interceptor code is structurally identical across all interceptors
- Designed base class approach: generic interceptor base classes in KnockOff library hold fields + priority chain logic, generated interceptors become thin wrappers
- Key decisions: base class (not interface) for interceptors, Invoke split via RunPriorityChain + InvokeDelegate override, TArgs pattern (single type / tuple / Unit)
- Created prototype plan: `docs/plans/interceptor-base-class-prototype.md`
- Prototype implemented and verified (110 tests pass, ~91% reduction per interceptor confirmed)
- Post-prototype analysis: identified remaining duplicate patterns (LastArg/LastArgs, Return methods, inner classes), resolved via thin subclass approach
- Created generator implementation plan: `docs/plans/interceptor-base-class-generator.md`
  - Covers all 9 patterns, 4 member types
  - Emission mode concept: base class mode for ~70-80% of interceptors, inline mode fallback for edge cases (async, ref return, ref/out params, overload groups)
  - 5 implementation phases: library port, method renderer, property renderer, indexer renderer, final verification
  - 7 risk areas identified with mitigations
- Developer review: 5 concerns raised and resolved (void-async clarification, FindLastArgInTracking contradiction fixed, set-only properties addressed, PropertyGetSetInterceptorBase detailed, netstandard2.0 error corrected)
- Implementation contract created with 30 checklist items, 5 verification gates, 6 out-of-scope items, 5 stop conditions
- **Plan status: Ready for Implementation** — awaiting implementation on another machine

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

[What was learned? What decisions were made?]
