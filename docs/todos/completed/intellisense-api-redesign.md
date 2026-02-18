# IntelliSense API Redesign

**Status:** Complete
**Priority:** High
**Created:** 2026-02-17
**Last Updated:** 2026-02-18

---

## Problem

Versions .48-.52 focused on reducing build times by introducing precompiled interceptor base classes with generic type parameters (e.g., `AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>`). This made IntelliSense unintelligible -- users see walls of generic type noise instead of parameter names when they type `stub.Method.Call(`. NSubstitute shows clean signatures like `Task ITest.DoSomething(int streetNumber, string street)`. KnockOff can't match that exactly (different architecture) but must get IntelliSense as clear as possible.

The current architecture has three interleaved systems for method interceptors:

1. **Pre-compiled sealed types** (`MethodInterceptor0<TReturn>` through `MethodInterceptor1<TDelegate, TArg, TReturn>`, `AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>`, etc.) -- 12 sealed types with generic parameters that pollute IntelliSense tooltips.
2. **Base class hierarchy** (`VoidMethodInterceptorBase<TDelegate, TArgs>`, `MethodInterceptorBase<TDelegate, TArgs, TReturn>`) -- used for methods that can't use pre-compiled types (async, ref/out). Generated classes inherit these but still expose the generic base in tooltips.
3. **Fully generated interceptor classes** -- the old system, used as fallback for edge cases.

Additionally, the current overload handling uses numbered "slots" (`stub.Process`, `stub.Process2`, `stub.Process3`) which is verbose and non-discoverable. And the API verb names are still in flux (Returns/Return, Execute/Call, OnCall still present in typed handlers).

**Priority order (user-stated):** IntelliSense clarity > Runtime performance > Build time

## Solution

Comprehensive API redesign with 12 design decisions:

1. **Interceptor-as-Property**: `stub.Method` remains a property returning an interceptor (non-negotiable)
2. **Unified Callback**: `Call(callback)` / `ThenCall(callback)` for both void and non-void
3. **Return -- Value Only**: `Return(value)` / `ThenReturn(value)` strictly for values, never lambdas
4. **Named Tuples for 2+ Params**: `Call(args => ...)` where args is `(int id, string name)`
5. **Raw Types for 0-1 Params**: No-args `Call(() => ...)`, single-arg `Call(id => ...)`
6. **Delegate Fallback for ref/out**: Generated delegate types with XML comments
7. **Pristine XML Comments**: All generated methods get XML docs with migrated user param descriptions
8. **Fully Generated Interceptor Classes**: Non-generic base for runtime logic, generated typed wrappers
9. **Overload Disambiguation via Call/Return Overloads**: Single `stub.Process` property, no more slots
10. **When Chains**: `When(args => ...).Return(value)` / `When(args => ...).Call(callback)`
11. **Properties/Indexers**: Keep Get/Set, tuples for multi-key indexers
12. **No Arg-Style API**: Matching through When chains, not Arg matchers

---

## Plans

- [IntelliSense API Redesign Plan](../plans/intellisense-api-redesign.md)

---

## Tasks

- [x] Architect explores codebase and creates design plan
- [x] Developer reviews plan, raises concerns or approves (5 concerns raised)
- [x] Architect addresses developer concerns (all 5 resolved)
- [x] Developer re-reviews plan, approves or raises further concerns (approved rev 6)
- [x] Implementation Phase 1: Library foundation (non-generic base class with object? fields)
- [x] Implementation Phase 2: Generator - fully generated interceptor classes + Of<T>() rename
- [x] Implementation Phase 3: Test updates (moved earlier for validation)
- [x] Implementation Phase 4: Overload redesign (remove slots/compositor infrastructure)
- [x] Implementation Phase 5: XML comment generation pipeline
- [x] Implementation Phase 6: Named tuple integration for callbacks and When chains
- [x] Implementation Phase 7: Design project updates
- [x] Implementation Phase 8: Documentation and skill updates
- [x] Implementation Phase 9: Cleanup (remove pre-compiled types, slots, interfaces, bump version)
- [x] Architect verification

---

## Progress Log

### 2026-02-17
- Created todo to track the comprehensive IntelliSense API redesign
- Architect performed deep codebase exploration:
  - Examined all 12 pre-compiled interceptor sealed types in `src/KnockOff/Interceptors/`
  - Examined base class hierarchy (`VoidMethodInterceptorBase`, `MethodInterceptorBase`)
  - Examined `MethodInterceptorRenderer.cs` (~4582 lines), `PreCompiledInterceptorRenderer.cs`
  - Examined slot system (`IMethodOverloadSlot1-8`, slot extension methods)
  - Examined current Design.Stubs API usage (`BasicMethods.cs`, etc.)
  - Examined overload group rendering in the generator
  - Reviewed recent completed plans (`unify-returns-execute-design.md`, `ttuple-interceptors.md`)
  - Identified superseded todos: `rename-returns-to-return`, `migrate-execute-to-call`, `unify-returns-execute-api`, `reduce-generated-code-size` (partially), `arity-based-precompiled-interceptors`
- Created architectural design plan at `docs/plans/intellisense-api-redesign.md`

### 2026-02-17 (rev 2)
- Added Pattern-by-Pattern Analysis section covering all 9 patterns with actual generated code evidence
- Read generated `.g.cs` files for all patterns: `CalculatorStub.g.cs` (P1), `GenericServiceStub\`1.g.cs` (P2), `StandaloneClassStubOverrideStub.g.cs` (P3), `InlineClassExample.Stubs.g.cs` (P6), `InlineDelegateExample.Stubs.g.cs` (P7), `OpenGenericInterfaceExample.Stubs.g.cs` (P8), `OpenGenericClassExample.Stubs.g.cs` (P9), `MethodOverloadsDemo.Stubs.g.cs` (overloads)
- Investigated git history: precompiled types introduced in v0.48-v0.52 (commits `6768d022` through `00c3dbf2`)
- Confirmed: before v0.48, `src/KnockOff/Interceptors/` directory did not exist
- Added Starting Point Recommendation: evolve forward from HEAD, do NOT revert
- Added What Stays Precompiled section: explicit inventory of every library type (STAYS/GENERATED/DELETED)
- Key finding: property/indexer interceptors STAY precompiled (clean IntelliSense, not the problem)
- Softened breaking-change language (pre-1.0, single consumer)

### 2026-02-17 (rev 3 - developer review)
- Developer reviewed plan: **Concerns Raised**
- Investigated 15+ source files across Generator, KnockOff library, Design.Stubs, and Tests
- Identified 5 concerns:
  1. Generic method `Of<T>()` subsystem not in scope despite having separate rendering pipelines
  2. Non-generic base class logic boundary unclear (counts-only vs object? fields)
  3. Overloaded method user-facing API described as "new" but already exists (compositor is internal)
  4. Precompiled When chain interfaces not listed for deletion
  5. Long test breakage window between Phase 2 and Phase 6
- Plan architecture is sound; concerns are clarification-level

### 2026-02-17 (rev 4 - architect responses)
- Architect addressed all 5 developer concerns:
  1. **Of<T>() subsystem**: Agreed. Added to scope -- entry point rename `Return` -> `Call` for non-void typed handlers in 4 renderer locations. Phase 2 updated.
  2. **Base class logic boundary**: Agreed. Redesigned to use `object?`/`Delegate?` fields with abstract methods for type-specific operations. Full priority chain stays in base. Generated code ~150-250 lines/method (confirmed from current `StandaloneClassStubOverrideStub_ExecuteInterceptor`).
  3. **Overload API**: Agreed. Decision 9 rewritten to accurately describe current state (single property already exists). Phase 4 is now explicitly about removing internal slot/compositor infrastructure.
  4. **When chain interfaces**: Agreed. Two new DELETED sections added for `IWhenChain`, `IWhenBuilder`, `IVoidWhenChain` and generic builder/sequence interfaces. Phase 9 cleanup updated.
  5. **Test breakage window**: Agreed. Phases reordered: test updates moved to Phase 3 (immediately after generator rework). Validation now happens before overload redesign.
- Plan status: Under Review (Developer) -- awaiting developer re-review

### 2026-02-17 (rev 5 - generic-related gaps addressed)
- Architect addressed 10 generic-related gaps found during investigation:
  - **HIGH**: Added Phase 4 note documenting mixed generic/non-generic overload split (`Process` vs `ProcessGeneric.Of<T>()`). The overload redesign must preserve this builder-level split.
  - **MEDIUM (4 items)**:
    - Added "Generic Stubs Subsection" with full generated code examples for Pattern 2 (GenericServiceStub). Includes type parameter scoping rule: interceptors inherit `T` from enclosing class, MUST NOT redeclare (CS0693). Covers `Call(Func<int, T?> callback)` and `Return(T? value)` examples.
    - Added Pattern 4 dual-level type parameter analysis (`GenericMethodRepositoryBase<TEntity>.ConvertEntity<TResult>`). Documents builder split between class-level params (standard interceptors) and method-level params (Of<T>() handlers).
    - Added open generic delegate analysis (`OGFactory<T>`, `OGConverter<TIn, TOut, TResult>`). Documents that interceptor classes inherit from non-generic `MethodInterceptorRuntime` with their own type parameter declarations.
    - Added smart default factories section documenting two strategies: compile-time `new List<T>()` for class-level generics, runtime `SmartDefault<T>` helper for method-level generics.
  - **LOW (5 items)**:
    - Added `Of<T>()` arity grouping preservation note (`InlineGenericTypeArityGroup`, `FlatGenericMethodArityGroup`)
    - Added constraint scoping rule: nested interceptors inherit constraints from enclosing class
    - Added tuple + generic type example (`(TKey key, TValue value)`)
    - Added async + generic example (`Task<T?>` with `Func<int, T?>` simplified callback)
    - Added `unmanaged` constraint bug note (CS0449 pre-existing, test against if new constraint emission code is added)
- Plan rev 5, still Under Review (Developer)

### 2026-02-17 (rev 6 - developer approved)
- Developer re-reviewed plan after rev 4 (5 concerns resolved) and rev 5 (10 generic gaps addressed)
- Verified all 5 original concerns resolved with codebase investigation evidence
- Verified all 10 generic additions correct
- **Verdict: APPROVED**
- Implementation contract added to plan with 9 phases, verification gates, stop conditions, and explicit out-of-scope items

### 2026-02-17 (Phases 1-3 implemented)
- **Phase 1:** Created `MethodInterceptorRuntime.cs` non-generic base class (~560 lines). Library builds clean.
- **Phase 2:** Reworked `MethodInterceptorRenderer.cs` to always generate full interceptor classes inheriting `MethodInterceptorRuntime`. All 4 renderer pipelines updated. Of<T>() entry point renamed to `Call`. Generator builds clean, all 9 patterns generate correctly.
- **Phase 3:** Updated all consumer code (~100+ files): `Return(callback)` → `Call(callback)`, `ThenReturn(callback)` → `ThenCall(callback)`, `Of<T>().Return(callback)` → `Of<T>().Call(callback)`. Fixed 3 Phase 2 runtime bugs:
  - Bug 1: Ref/out parameter boxing — inlined priority chain for ref/out methods
  - Bug 2: ThenDefault NullRef for value types — added null-safe cast pattern
  - Bug 3: ThenNone matcher returning true instead of false
- Test results: 1725 passed, 3 failed (pre-existing ThrowsOnDefault bug for string returns), 4 skipped

### 2026-02-18 (Phase 4 implemented)
- **Phase 4:** Removed dead compositor code from all renderers:
  - `PreCompiledInterceptorRenderer.cs`: ~700 lines removed (compositor rendering methods, slot interface builders)
  - `InlineRenderer.cs`: Removed compositor source delegation code and `GetCompositorSourceFallbackExpression` method
  - `ClassRenderer.cs` and `StandaloneClassRenderer.cs`: Fixed syntax errors in precompiled check
  - Updated overload-specific consumer code: `Return(callback)` → `Call(callback)` for overloaded method interceptors
  - Updated `Design.Stubs/Methods/MethodOverloads.cs` for new API
  - All compositor references eliminated from renderer directory
- Test results: KnockOffTests 1725/3/4, Design.Tests 370/0/0, NeatooInterfaceTests 473/0/0

### 2026-02-18 (Phase 5 implemented)
- **Phase 5:** XML Comment Generation Pipeline
  - **Model layer:** Added `string? XmlDoc` to `ParameterInfo` (interface/class models) and `ParameterModel` (shared model). Added `string? XmlDocSummary` to `InterfaceMemberInfo`, `ClassMemberInfo`, `FlatMethodModel`, `MethodSignatureInfo`, `UnifiedMethodInterceptorModel`, and `MethodOverloadSignature`.
  - **Extraction:** Added `SymbolHelpers.GetXmlDocSummary()` and `GetXmlDocForParameter()` using `IMethodSymbol.GetDocumentationCommentXml()` with XmlDocument parsing. Added `XmlEscape()`, `FormatMethodSignatureForXmlDoc()`, `ShortenTypeName()` helpers.
  - **Builder propagation:** Updated all 5 builder classes (`FlatModelBuilder`, `InlineModelBuilder`, `ClassModelBuilder`, `StandaloneClassModelBuilder`, `UnifiedInterceptorBuilder`) and `ModelAdapters` to thread XML doc fields through the pipeline.
  - **Renderer:** Added `EmitCallXmlDoc`, `EmitReturnXmlDoc`, `EmitWhenXmlDoc`, `EmitCallbackParamDoc` helpers to `MethodInterceptorRenderer`. Updated all Call/Return/When entry points across single-signature, base-class, and overload-group modes. Enhanced class-level summary to include method signature.
  - **Generated output examples:**
    - `/// <summary>Tracks and configures behavior for Add(int a, int b).</summary>` (class-level)
    - `/// <summary>Configures callback for Add(int a, int b).</summary>` (Call)
    - `/// <summary>Sets return value for Add(int a, int b).</summary>` (Return)
    - `/// <summary>Configures parameter matching for Add(int a, int b). Matches exact values using Object.Equals. Returns builder for Return().</summary>` (When)
    - `/// <summary>Configures callback for GetAsync(string input). Result auto-wrapped in Task.</summary>` (simplified async)
  - Full solution builds clean. Test results unchanged: 1725/3/4 (net9/net10), 1724/3/4 (net8)

### 2026-02-18 (Phase 6 implemented)
- **Phase 6:** Named Tuple Integration
  - **Builder changes (`UnifiedInterceptorBuilder.cs`):**
    - `NeedsCustomDelegate`: Changed to `sig.HasRefOrOutParams` only (was `sig.HasRefOrOutParams || !sig.IsVoid`)
    - `BuildCallDelegateType`: Non-void methods now use `Func<..., TReturn>` instead of custom delegates. 2+ params wrapped in named tuples: `Func<(int a, int b), int>`
    - Void 2+ params: `Action<(int a, int b)>` instead of `Action<int, int>`
    - 0-1 params: unchanged (`Action`/`Action<T>`, `Func<TReturn>`/`Func<T, TReturn>`)
    - ref/out: unchanged (custom delegates preserved)
  - **Renderer changes (`MethodInterceptorRenderer.cs`):**
    - `BuildBaseClassDelegateCallArgs`: 2+ params passes `typedArgs` directly (tuple) instead of unpacking `typedArgs.a, typedArgs.b`
    - `BuildSimplifiedDelegateType`/`BuildSimplifiedVoidDelegateType`: 2+ params use tuple
    - `BuildDelegateMatchingParamDecls`/`BuildDelegateMatchingCallArgs`: 2+ params use tuple param
    - `BuildAsyncWrapExpression`/`BuildVoidAsyncWrapExpression`: Updated for tuple params
    - `CreateValueDelegate`: Single discard `_` instead of `__discard0, __discard1` for 2+ params
    - When predicate: `Func<(T1 n1, T2 n2), bool>` for 2+ params (no bridging lambda needed)
    - When exact match: Kept as individual params (more ergonomic for exact values)
  - **Overloaded methods:** Also use Func/Action with named tuples (different tuple arities are distinct C# types, so overload resolution works). Custom delegates only remain for ref/out params. `BuildOverloadSignature` updated accordingly. No more `public delegate` declarations in generated overload code.
  - **Bug fix:** Fixed `ThrowsOnDefault` for inline stubs — `InlineModelBuilder` now uses `DefaultValueStrategy` from interface metadata instead of hardcoded `IsUninstantiableType` list. This fixed 3 pre-existing test failures.
  - **Consumer code:** Updated ~100+ files for new tuple-based API (`(a, b) => a + b` → `args => args.a + args.b`)
  - Test results: KnockOffTests 1728/0/4 (3 pre-existing failures now fixed!), Design.Tests 370/0/0, NeatooInterfaceTests 473/0/0

### 2026-02-18 (Phase 7 implemented)
- **Phase 7:** Design Project Updates (comment-only changes)
  - Updated 22 files across Design.Stubs, Design.Tests, and Design.Domain
  - All comment/doc references to old API names updated:
    - `Returns(value)` → `Return(value)`, `Returns(callback)`/`OnCall(callback)`/`Execute(callback)` → `Call(callback)`
    - `ThenReturns()` → `ThenReturn()`, `ThenExecute()` → `ThenCall()`
    - `VoidMethodInterceptor<T>`/`MethodInterceptor<T1,T2,T3>` → `MethodInterceptorRuntime`
  - XML doc comments on domain interfaces (ICalculator, IDataService, IStubOverrideService, ProcessorBase) updated
  - Design README updated
  - No code or assertions modified — strictly comments and documentation
  - Test results: Design.Stubs builds clean, Design.Tests 370/0/0

### 2026-02-18 (Phase 8 implemented)
- **Phase 8:** Documentation and Skill Updates
  - **Skill files** (14 files updated): All API references updated (`Returns`/`Execute`/`OnCall` → `Return`/`Call`, tuple syntax for 2+ params, When exact match individual params)
  - **Documentation guides** (16 files updated): Prose and inline examples updated, snippet blocks left untouched for mdsnippets regeneration
  - **README.md** (4 edits): Inline code and prose updated to new API names
  - **Migration guide** created: `docs/guides/migration-v0.52.md` — covers all breaking changes with before/after examples
  - **MarkdownSnippets** regenerated: 710 snippets from Documentation.Samples (already updated in Phase 3/6)
  - Test results: Design.Tests 370/0/0, Documentation.Samples 691/0/0

### 2026-02-18 (Phase 9 implemented)
- **Phase 9:** Cleanup
  - **29 files deleted:**
    - 12 precompiled interceptor sealed types (`MethodInterceptor`, `VoidMethodInterceptor`, `AsyncMethodInterceptor`, `AsyncVoidMethodInterceptor` × 3 arities each)
    - 2 base class hierarchy files (`VoidMethodInterceptorBase`, `MethodInterceptorBase`)
    - 8 slot system files (entire `Interceptors/Slots/` directory)
    - 1 dead code utility (`DelegateInvokerFactory.cs`)
    - 6 test files for deleted types
  - **NOT deleted** (still live, used by generator): `IMethodCallBuilder`, `IMethodReturnBuilder`, `IMethodCallSequence`, `IMethodReturnSequence`, `IWhenTracking`, `ITracking`
  - **Version bumped:** 0.51.2 → 0.52.0
  - **Release notes created:** `docs/release-notes/v0.52.0.md`
  - Test results: KnockOffTests 1510/0/4 (218 tests removed with deleted types), Design.Tests 370/0/0, Documentation.Samples 691/0/0, NeatooInterfaceTests 473/0/0
  - **Note:** `PreCompiledInterceptorRenderer.cs` has dead method `GetMethodInterceptorType()` referencing deleted type names as strings — not a compilation issue, can be cleaned up separately

---

## Critical Rules

### Developer: STOP If Any Pattern Is Missing

**At every implementation checkpoint**, the developer MUST verify the change works for ALL 9 patterns. If any pattern is not addressed, **STOP immediately** and report which pattern is missing. Do NOT continue to the next phase. This is the most common failure mode — a feature works for some patterns but silently misses others.

### Architect: Verify the Tests and Design Projects, Not the Developer's Checklist

During post-implementation verification, the architect MUST **read the actual KnockOffTests and Design projects** (Design.Stubs, Design.Tests) for all 9 patterns — not the developer's "Completion Evidence" section. Tests and Design projects are the source of truth for whether a feature works. The developer may have checked a box without testing every pattern. The architect's job is to independently confirm by examining tests, Design.Stubs usage, and running builds/tests.

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] Design project builds successfully
- [x] Design project tests pass
- [x] All 9 patterns generate correct API — **verified by reading generated code, not developer claims**
- [x] IntelliSense shows clean signatures (no generic type noise)
- [x] XML comments flow through to generated methods
- [x] Overloaded methods use single property (no slots)
- [x] Named tuples work for 2+ param callbacks and When chains
- [x] Skill documentation updated
- [x] Version bumped

**Verification results:**
- Design build: PASS (0 warnings, 0 errors)
- Design tests: PASS (370 passed, 0 failed, all 3 TFMs)

---

## Architect Verification

**Verified:** 2026-02-18
**Verdict:** VERIFIED

### Independent Test Results

| Project | net8.0 | net9.0 | net10.0 |
|---------|--------|--------|---------|
| KnockOffTests | 1509 passed, 0 failed, 4 skipped | 1510 passed, 0 failed, 4 skipped | 1510 passed, 0 failed, 4 skipped |
| Design.Tests | 370 passed, 0 failed | 370 passed, 0 failed | 370 passed, 0 failed |
| Documentation.Samples | 691 passed, 0 failed | 691 passed, 0 failed | 691 passed, 0 failed |
| NeatooInterfaceTests | 473 passed, 0 failed | 473 passed, 0 failed | 473 passed, 0 failed |
| AssemblyStrict | 14 passed, 0 failed | 14 passed, 0 failed | 14 passed, 0 failed |

4 skipped tests are pre-existing property/indexer `Verifiable(Called)` bug regression tests -- unrelated to this redesign.

### Design Match -- All 9 Patterns Verified

Generated `.g.cs` files independently read and verified:

| Pattern | Generated File | Base Class | XML Comments | Named Tuples | Slots |
|---------|---------------|------------|-------------|-------------|-------|
| P1 Standalone | `CalculatorStub.g.cs` | `MethodInterceptorRuntime` | Yes -- `<summary>Configures callback for Add(int a, int b).</summary>` | Yes -- `Func<(int a, int b), int>` for 2-param `Add` | None |
| P2 Generic Standalone | `GenericServiceStub\`1.g.cs` | `MethodInterceptorRuntime` | Yes | N/A (0-1 param methods, raw types: `Func<int, T?>`) | None |
| P3 Standalone Class | `StandaloneClassStubOverrideStub.g.cs` | `MethodInterceptorRuntime` | Yes | N/A (0 param) | None |
| P4 Generic Standalone Class | `RepositoryStubOverrideStub\`1.g.cs` | `MethodInterceptorRuntime` | Yes | N/A (1 param, raw type: `Func<int, T?>`) | None |
| P5 Inline Interface | `InlineInterfaceExample.Stubs.g.cs` | `MethodInterceptorRuntime` | Yes | Yes -- `Func<(int a, int b), int>` | None |
| P6 Inline Class | `InlineClassExample.Stubs.g.cs` | `MethodInterceptorRuntime` | Yes | N/A (0 param) | None |
| P7 Inline Delegate | `InlineDelegateExample.Stubs.g.cs` | `MethodInterceptorRuntime` | Yes | Yes -- `((int a, int b))args!` in Invoke | None |
| P8 Open Generic Interface | `OpenGenericInterfaceExample.Stubs.g.cs` | `MethodInterceptorRuntime` | Yes | N/A (1 param, raw type) | None |
| P9 Open Generic Class | `OpenGenericClassExample.Stubs.g.cs` | `MethodInterceptorRuntime` | Yes | N/A (0 param) | None |
| Overloads | `MethodOverloadsDemo.Stubs.g.cs` | Self-contained (no inheritance) | Yes | Yes -- `Func<(string input, FormatOptions options), string>` | Single `Format` property |

### Production Code Verified

- `MethodInterceptorRuntime.cs` exists as non-generic base class with `object?`/`Delegate?` fields (~560+ lines)
- All 4 renderers (`FlatRenderer`, `InlineRenderer`, `ClassRenderer`, `StandaloneClassRenderer`) emit `MethodInterceptorRuntime` base
- `MethodInterceptorRenderer.cs` generates fully typed interceptor classes for all entry points
- XML comment helpers (`EmitCallXmlDoc`, `EmitReturnXmlDoc`, `EmitWhenXmlDoc`) wired into 24+ call sites
- `XmlDocSummary` field propagated through all 5 builders and model adapters (42 occurrences across 12 files)
- 29 precompiled interceptor files deleted (12 sealed types, 2 base classes, 8 slot files, 1 dead utility, 6 test files)
- No slot interface references (`IMethodOverloadSlots`, `IVoidOverloadSlots`, etc.) in any generated code

### Minor Items (Non-Blocking)

1. **Dead code in PreCompiledInterceptorRenderer.cs**: `GetMethodInterceptorType()` method (~lines 284-350) references deleted type names as strings. Never called. Cleanup-only, does not affect functionality.
2. **Stale comment in MethodInterceptorRenderer.cs**: Lines 753-754 reference `MethodInterceptorBase`/`VoidMethodInterceptorBase` in a comment. Comment-only, no functional impact.

### Additional Verifications

- Version: `0.52.0` in `Directory.Build.props` (both `FileVersion` and `PackageVersion`)
- Release notes: `docs/release-notes/v0.52.0.md` exists with comprehensive changelog
- Migration guide: `docs/guides/migration-v0.52.md` covers all breaking changes
- Skill files: `SKILL.md` and reference files (`methods.md`, `sequences.md`, `when-chains.md`) updated with `Call`/`Return`/`When` API, named tuples, and `MethodInterceptorRuntime` terminology

---

## Results / Conclusions

Comprehensive IntelliSense API redesign completed and verified across all 9 patterns. Key achievements:

1. **Clean IntelliSense**: All method interceptors are fully generated classes inheriting from `MethodInterceptorRuntime` -- no generic type noise in tooltips
2. **Unified API**: `Call(callback)` for behavior, `Return(value)` for values, `When()` for matching -- consistent across all patterns
3. **Named Tuples**: 2+ parameter methods use `Func<(int a, int b), int>` providing named parameter IntelliSense; 0-1 param methods use raw types
4. **XML Comments**: Every generated `Call`, `Return`, `When` method includes `<summary>` with the original method signature
5. **No Slots**: Overloaded methods use a single interceptor property; Call/Return lambda signature disambiguates overloads
6. **29 files deleted**: Precompiled interceptor types, base class hierarchy, slot infrastructure, and associated tests removed
7. **Zero test failures**: 3044+ tests pass across all TFMs (3 previously-failing ThrowsOnDefault tests fixed during Phase 6)

