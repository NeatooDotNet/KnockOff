# Warnings as Errors Cleanup

**Date:** 2026-03-23
**Related Todo:** [Warnings as Errors Cleanup](../todos/warnings-as-errors-cleanup.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-03-23 (developer review approved)

---

## Overview

Systematically evaluate and remove warning suppressions across the KnockOff solution. The goal is not zero suppressions but zero *unjustified* suppressions. Every remaining suppression must have a documented reason. Generated code must compile cleanly under `TreatWarningsAsErrors=true` in consumer projects.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/warnings-as-errors-cleanup.md#requirements-review)

### Relevant Existing Requirements

#### Behavioral Contracts

- **Generated code must compile under `TreatWarningsAsErrors=true` in consumer projects.** The generator already emits pragmas (SYSLIB0050, CS8765, CS8618, CS8763, CS8601, CS8603, CS8769) to ensure this. Removing any pragma without fixing the underlying pattern would break compilation for consumers.
- **Library interceptor base classes are the public API.** Files in `src/KnockOff/Interceptors/` define runtime types that generated interceptor classes inherit from. Their public field/type structure (suppressed by CA1034, CA1051, CA1002) is load-bearing. Generated code directly accesses these fields and nested types.

#### Governing Constraints Checked

- **Interceptor-as-Property Principle** -- NOT AT RISK. No API changes proposed. However, library interceptor file suppressions (CA1034, CA1051, CA1002) protect the architecture that enables interceptor-as-property.
- **API Consistency Principle** -- NOT AT RISK. No API changes proposed.
- **Nine Patterns** -- All four renderer pipelines emit `#pragma warning disable` directives. Changes must be verified across all pipelines per the Pipeline Verification Rule.
- **Design Projects as Source of Truth** -- Design.Stubs and Design.Tests have their own justified suppressions. Changes must not break Design project compilability.

### Gaps

None. This todo does not introduce new features.

### Contradictions

None.

### Recommendations for Architect

1. Categorize by risk tier before planning work.
2. Library interceptor pragmas will almost certainly need documented justification.
3. Generator-emitted pragmas need per-pipeline verification.
4. Design project suppressions must preserve compilability.
5. No API changes should be needed.

---

## Business Rules (Testable Assertions)

1. WHEN a consumer project enables `TreatWarningsAsErrors=true` and references KnockOff, THEN generated stub code compiles with zero warnings. -- Source: Existing behavioral contract
2. WHEN a `NoWarn` entry is removed from `Directory.Build.props` or any `.csproj`, THEN the entire solution still builds with zero warnings and zero errors. -- Source: NEW
3. WHEN a `#pragma warning disable` is removed from a renderer file, THEN the generated code for ALL nine patterns compiles with zero warnings under `TreatWarningsAsErrors=true`. -- Source: Pipeline Verification Rule
4. WHEN a suppression remains in the codebase after cleanup, THEN it has a justification comment explaining why removal is not feasible. -- Source: User clarification Q4
5. WHEN generated code pragmas are modified, THEN all four renderer pipelines (Flat, StandaloneClass, Inline/Class, Shared) are verified independently. -- Source: Pipeline Verification Rule
6. WHEN library runtime pragmas (interceptor files) are evaluated, THEN each is assessed for removal feasibility and documented with justification if kept. -- Source: User clarification Q3

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Remove a safe NoWarn entry (e.g., CA1822 from Directory.Build.props) | Fix the underlying code, remove the entry | Rule 2 | Solution builds with zero warnings/errors |
| 2 | Remove SYSLIB0050 file-level pragma from FlatRenderer | Check if generated code actually uses obsolete serialization API | Rule 3 | If no usage: generated code compiles cleanly; If usage: pragma must stay with justification |
| 3 | Consumer project with TreatWarningsAsErrors=true references KnockOff after cleanup | All remaining pragmas in generated code justified | Rule 1 | Zero compilation warnings in consumer |
| 4 | CA1034 justification documented on MethodInterceptorRuntime.cs | Evaluate if nested types can be un-nested | Rule 6 | Justification comment explains that generated code inherits these nested types |
| 5 | Remove IDE0044 from test projects | Fix underlying readonly field warnings in test code | Rule 2 | Test projects build cleanly |

---

## Approach

### Strategy

Work is divided into four phases by risk tier:

1. **Phase 1 -- Safe NoWarn Removals**: Remove `NoWarn` entries from `.props`/`.csproj` files where the fix is straightforward code changes (IDE-level, style rules, or rules that are simply wrong to suppress).
2. **Phase 2 -- Generated Code Pragma Cleanup**: Investigate each pragma emitted by renderers. Determine if the underlying generated code pattern can be improved to eliminate the warning, or if the pragma is inherent to the source generation approach.
3. **Phase 3 -- Library Runtime Pragma Evaluation**: Evaluate every `#pragma warning disable` in `src/KnockOff/` files. Document justification for each that must remain.
4. **Phase 4 -- Source File Pragmas in Tests, Design, Benchmarks**: Evaluate pragmas in non-core source files. Lower priority.

### Out of Scope

- `src/Prototype/` -- Per user clarification, leave as-is (experimental)
- `src/Tests/PackageTest/` -- Per user clarification, leave as-is (experimental)

---

## Design

### Complete Suppression Catalog

This is the comprehensive catalog of every warning suppression in the solution (excluding Prototype and PackageTest).

---

#### SECTION A: Solution-Wide NoWarn (Directory.Build.props)

**File:** `src/Directory.Build.props` line 9
**Suppressed:** `CA1861;CA1865;CA1510;IDE0021;IDE0022;IDE0023;IDE1006;CA1050;CA1822;MSB3277`

| Code | Description | Risk Tier | Assessment |
|------|-------------|-----------|------------|
| CA1861 | Prefer static readonly fields for constant arrays | Safe to fix | Fix call sites to use `static readonly` arrays. Mostly in tests. |
| CA1865 | Use `string.StartsWith(char)` overload | Safe to fix | Use char overload where applicable. Simple search-and-replace. |
| CA1510 | Use `ArgumentNullException.ThrowIfNull` | Safe to fix | Replace manual null checks. Note: Generator targets netstandard2.0 (no ThrowIfNull), but the NoWarn is solution-wide. May need per-project handling. |
| IDE0021 | Use expression body for constructors | Safe to fix | Style preference. Fix code or configure `.editorconfig` to allow block bodies. |
| IDE0022 | Use expression body for methods | Safe to fix | Style preference. Same approach as IDE0021. |
| IDE0023 | Use expression body for conversion operators | Safe to fix | Style preference. Same approach. |
| IDE1006 | Naming rule violation | Requires investigation | Some intentional naming (e.g., `_` prefix fields, interceptor method names like `Method_`). May need `.editorconfig` configuration or targeted suppression instead of blanket NoWarn. |
| CA1050 | Declare types in namespaces | Requires investigation | Some types may intentionally be in global namespace. Need to verify. |
| CA1822 | Mark members as static | Safe to fix | Add `static` to methods that don't access instance state. |
| MSB3277 | Found conflicts between different versions of assembly | Justified (keep) | Multi-targeting (`net8.0;net9.0;net10.0`) inherently produces this. Cannot be fixed without dropping multi-targeting. |

---

#### SECTION B: Generator Project NoWarn

**File:** `src/Generator/Generator.csproj` line 11
**Suppressed:** `RS2008`

| Code | Description | Risk Tier | Assessment |
|------|-------------|-----------|------------|
| RS2008 | Enable analyzer release tracking | Justified (keep) | Standard practice for source generators. The generator is not a published analyzer -- it is embedded in the KnockOff NuGet package. Release tracking is unnecessary overhead. |

---

#### SECTION C: Generator Source File Pragmas

**File:** `src/Generator/HashCode.cs` line 28
**Suppressed:** `RS1035` (Do not use APIs banned for analyzers)

| Code | Description | Risk Tier | Assessment |
|------|-------------|-----------|------------|
| RS1035 | Do not use banned API (`new Random()`) | Justified (keep) | `HashCode` is a netstandard2.0 polyfill. `new Random()` is used for one-time seed generation at startup. The analyzer bans it because analyzers should be deterministic, but this is a hash seed, not analysis logic. Safe and intentional. |

**File:** `src/Generator/HashCode.cs` line 186
**Suppressed:** `0809` (Obsolete member overrides non-obsolete member)

| Code | Description | Risk Tier | Assessment |
|------|-------------|-----------|------------|
| CS0809 | Obsolete member overrides non-obsolete member | Justified (keep) | The `[Obsolete(error: true)]` on `GetHashCode()` and `Equals()` is intentional -- it prevents accidental use of a mutable struct as a dictionary key. This is a standard pattern from `System.HashCode`. |

---

#### SECTION D: Renderer-Emitted Pragmas (Generated Code)

These are pragmas that renderers write into the generated `.g.cs` files. They directly affect consumer compilation.

##### D1: SYSLIB0050 -- Obsolete serialization API (File-level, all renderers)

| Renderer | Lines | Patterns Affected |
|----------|-------|-------------------|
| `FlatRenderer.cs` | 25, 244 | Standalone (1,2) |
| `StandaloneClassRenderer.cs` | 28, 249 | Standalone Class (3,4) |
| `InlineRenderer.cs` | 23 | Inline (5-9) |

**Risk Tier:** Requires investigation
**Assessment:** Emitted at file level in every generated file, but no generated code actually calls `FormatterServices.GetUninitializedObject()` or any other SYSLIB0050 API. This pragma also appears alongside CS8601 in `MethodInterceptorRenderer` source delegation blocks, but the file-level pragma already covers it. Investigation needed: is there ANY generated code that actually triggers SYSLIB0050? If not, all file-level emissions can be removed. The `CS8601, SYSLIB0050` pair in `MethodInterceptorRenderer` may be cargo-culted -- the SYSLIB0050 in the pair may be unnecessary.

##### D2: CS8601 -- Possible null reference assignment (Source delegation)

| Renderer | Lines | Context |
|----------|-------|---------|
| `MethodInterceptorRenderer.cs` | 1055, 1136, 2481, 2696, 2908, 3017 | Source delegation fallback: `if (_source is { } src) return src.Method(args);` |

**Risk Tier:** Requires investigation
**Assessment:** Emitted around source delegation code where the source method's return type may not match the interceptor's expected nullability. The generated code pattern: `if (_source is { } src) return src.Method(args);` may produce CS8601 when the source method returns `T?` but the interceptor expects `T`. Could potentially be fixed by adding null-forgiving operator (`!`) to the source delegation return, but need to verify this doesn't mask real nullability issues.

##### D3: CS8618 -- Non-nullable field must contain non-null value

| Renderer | Lines | Context |
|----------|-------|---------|
| `ClassRenderer.cs` | 694 | Inner class constructor for types with `required` members |
| `StandaloneClassRenderer.cs` | 667 | Inner class constructor for types with `required` members |
| `PropertyInterceptorRenderer.cs` | 533 | Ref return backing field `_refReturnBacking` |
| `IndexerInterceptorRenderer.cs` | 263 | Ref return backing field `_refReturnBacking` |
| `MethodInterceptorRenderer.cs` | 2220 | Ref return backing field `_refReturnBacking_{suffix}` |

**Risk Tier:** Justified (keep with documentation)
**Assessment:** Two distinct uses:
- **Required members:** When stubbing a class with `required` properties, the generated inner class constructor cannot initialize them (they are meant to be set by the caller). CS8618 is inherent.
- **Ref return backing fields:** These fields are initialized by `InvokeRefGet`/`InvokeRef` before first use, but the compiler cannot prove this at construction time. CS8618 is inherent -- the field must be a non-nullable type to match the ref return type.

Both uses are fundamentally required by the source generation patterns. These pragmas should remain but each emission site should have a comment explaining why.

##### D4: CS8765 -- Nullability of parameter doesn't match overridden member

| Renderer | Lines | Context |
|----------|-------|---------|
| `ClassRenderer.cs` | 805, 895, 1215 | Property/indexer setter with `[AllowNull]`, method with unconstrained type params |
| `StandaloneClassRenderer.cs` | 772, 901 | Property/indexer setter with `[AllowNull]` |

**Risk Tier:** Justified (keep with documentation)
**Assessment:** When the base class property setter has `[AllowNull]` attribute, the generated override setter has a different nullability annotation than the base. C# cannot express `[AllowNull]` on override setter parameters without CS8765. This is inherent to the override pattern. Similarly, methods with unconstrained generic type parameters (`T` where `T` may or may not be nullable) cannot have matching nullability annotations in overrides.

##### D5: CS8769 -- Nullability of reference type doesn't match implemented member (setter)

| Builder | Lines | Context |
|---------|-------|---------|
| `FlatModelBuilder.cs` | 1792 | Interface property/indexer setter with `[DisallowNull]` or `[AllowNull]` |
| `InlineModelBuilder.cs` | 1630 | Same |

**Risk Tier:** Justified (keep with documentation)
**Assessment:** When an interface property setter uses `[DisallowNull]` or `[AllowNull]`, the implementing class cannot express the exact nullability contract. CS8769 is inherent to interface implementation with nullability attributes on setters.

##### D6: CS8763 -- A method marked [DoesNotReturn] should not return

| Renderer | Lines | Context |
|----------|-------|---------|
| `ClassRenderer.cs` | 997, 1220 | Override of `[DoesNotReturn]` methods |
| `StandaloneClassRenderer.cs` | 996 | Override of `[DoesNotReturn]` methods |

**Risk Tier:** Justified (keep with documentation)
**Assessment:** Generated stubs override `[DoesNotReturn]` methods but the stub implementation may return (e.g., when no callback is configured). The stub needs to preserve the `[DoesNotReturn]` attribute for API fidelity but cannot guarantee the method never returns. This is inherent to stubbing `[DoesNotReturn]` methods.

##### D7: CS8603 -- Possible null reference return

| Renderer | Lines | Context |
|----------|-------|---------|
| `ClassRenderer.cs` | 1216 | Method override with unconstrained type params |

**Risk Tier:** Justified (keep with documentation)
**Assessment:** When overriding a method with unconstrained generic type parameters whose nullable annotations were stripped, the return type cannot match exactly. The generated code may return `default!` which the compiler sees as a possible null reference. Paired with CS8765 for the same methods.

---

#### SECTION E: Library Runtime Pragmas (src/KnockOff/)

##### E1: Interceptor Base Class Structural Suppressions

These suppressions protect the interceptor-as-property architecture. Generated code inherits from these base classes and directly accesses their public fields and nested types.

| File | Codes | Assessment |
|------|-------|------------|
| `MethodInterceptorRuntime.cs` | CA1034, CA1051, CA1002, CA1062, CA1716 | **Justified (keep).** Nested types (`Args` record, sequence classes) are the interceptor API. Public fields (`_onCall`, `_returnValue`, etc.) are accessed by generated code. Generic lists are the sequence storage. CA1062/CA1716 are API naming constraints. Un-nesting types or making fields private would break all generated interceptor classes. |
| `PropertyGetInterceptor.cs` | CA1034, CA1051 | **Justified (keep).** Same rationale. |
| `PropertySetInterceptor.cs` | CA1034, CA1051 | **Justified (keep).** |
| `PropertyGetSetInterceptor.cs` | CA1034, CA1051 | **Justified (keep).** |
| `PropertyGetInterceptorBase.cs` | CA1034, CA1051, CA1002 | **Justified (keep).** |
| `PropertySetInterceptorBase.cs` | CA1034, CA1051, CA1002 | **Justified (keep).** |
| `PropertyGetSetInterceptorBase.cs` | CA1034, CA1051, CA1002 | **Justified (keep).** |
| `IndexerGetSetInterceptor.cs` | CA1034, CA1051 | **Justified (keep).** |
| `IndexerGetSetInterceptorBase.cs` | CA1034, CA1051, CA1002, CA1716 | **Justified (keep).** |

##### E2: Interceptor Inline Suppressions

| File | Line | Code | Assessment |
|------|------|------|------------|
| `InterceptorExtensions.cs` | 1 | CA1062 | **Requires investigation.** Extension methods -- could add null checks. Low risk, straightforward fix. |
| `PropertyGetInterceptorBase.cs` | 218, 271 | CA1062 | **Requires investigation.** `params TValue[] values` -- could add null/empty check. |

##### E3: Library API Suppressions

| File | Line | Code | Assessment |
|------|------|------|------------|
| `IWhenTracking.cs` | 62 | CA1716 | **Justified (keep).** `Return` is a reserved keyword in VB.NET but is the natural method name for the KnockOff API. Renaming would harm usability. |
| `IWhenTracking.cs` | 85 | CA1716 | **Justified (keep).** `Call` -- same rationale. |

---

#### SECTION F: Design Project Suppressions

##### F1: Design.Domain

**File:** `src/Design/Design.Domain/Design.Domain.csproj` line 12
**Suppressed:** `CA1003;CA1070;CA1711;CA1716`

| Code | Description | Assessment |
|------|-------------|------------|
| CA1003 | Use generic EventHandler | **Justified (keep).** Demo code intentionally uses Action-based events. |
| CA1070 | Do not declare event fields as virtual | **Justified (keep).** Demo code tests access modifier preservation. |
| CA1711 | Identifiers should not have incorrect suffix | **Justified (keep).** `Collection` suffix used for clarity. |
| CA1716 | Identifiers should not match keywords | **Justified (keep).** `Stop` method used for clarity. |

**File:** `src/Design/Design.Domain/Abstractions/ConfigBase.cs` line 24
**Suppressed:** `CA1044` (Properties should not be write only)

| Code | Assessment |
|------|------------|
| CA1044 | **Justified (keep).** Intentional write-only property for demo purposes. |

**File:** `src/Design/Design.Domain/Services/IStubOverridePropertyService.cs` line 36
**Suppressed:** `CA1044`

| Code | Assessment |
|------|------------|
| CA1044 | **Justified (keep).** Same rationale. |

##### F2: Design.Stubs

**File:** `src/Design/Design.Stubs/Design.Stubs.csproj` line 14
**Suppressed:** `CA1707;CA2007;CA1030;CS0219;CA1052`

| Code | Description | Assessment |
|------|-------------|------------|
| CA1707 | Identifiers should not contain underscores | **Justified (keep).** Underscore naming used in demo code for clarity. |
| CA2007 | Consider calling ConfigureAwait | **Justified (keep).** Library demo code, not production. |
| CA1030 | Use events instead of Raise* methods | **Justified (keep).** Event demo code uses Raise prefix. |
| CS0219 | Variable assigned but never used | **Justified (keep).** Demo variables show result values without asserting. |
| CA1052 | Static holder types should be Static or NotInheritable | **Justified (keep).** KnockOff partial stub classes appear as static holders when the user adds no members. |

**File-level pragmas in Design.Stubs source files:**

| File | Code | Assessment |
|------|------|------------|
| `Advanced/InternalAccessibility.cs:18` | CA1812 | **Justified (keep).** Internal class design verification. |
| `Advanced/InternalAccessibility.cs:19` | CA1852 | **Justified (keep).** Standalone stubs must be partial. |
| `Methods/GenericMethodClassStubs.cs:63,120` | CA1052 | **Justified (keep).** Same as project-level CA1052. |
| `StubOverrides/StubOverrideBasics.cs:294,385` | CA1062 | **Justified (keep).** Demo code omitting null checks for clarity. |
| `StubOverrides/VoidStubOverrideFallback.cs:47` | CA1062 | **Justified (keep).** Same. |
| `StubOverrideProperties/StubOverridePropertyBasics.cs:111,543` | CA1024 | **Justified (keep).** Intentional method instead of property. |
| `StubPatterns/GenericTypeGapsVerification.cs:58,73` | CA1052 | **Justified (keep).** Same as project-level. |
| `StubPatterns/GenericFormatterStub.cs:36` | CA1062 | **Justified (keep).** Demo. |

##### F3: Design.Tests

**File:** `src/Design/Design.Tests/Design.Tests.csproj` line 11
**Suppressed:** `CA1707`

| Code | Assessment |
|------|------------|
| CA1707 | **Justified (keep).** Standard xUnit test naming convention uses underscores. |

**File-level pragmas:**

| File | Code | Assessment |
|------|------|------------|
| `MethodTests/RefReturnTests.cs:6` | CA1859 | **Justified (keep).** Tests intentionally use interface types. |
| `MethodTests/MethodOverloadTests.cs:158` | xUnit1051 | **Justified (keep).** Testing CancellationToken overload specifically. |

---

#### SECTION G: Test Project Suppressions

##### G1: Common Test Project NoWarn Pattern

Four test projects share the same `NoWarn` list:
- `KnockOffTests.csproj`
- `KnockOffTests.AssemblyStrict.csproj`
- `KnockOff.Analysis.Tests.csproj`
- `KnockOff.NeatooInterfaceTests.csproj`

**Suppressed:** `CA1861;CA1865;CA1510;IDE0021;IDE0022;IDE0023;IDE1006;IDE0044;CS4014;xUnit1051`

| Code | Description | Assessment |
|------|-------------|------------|
| CA1861 | Prefer static readonly for constant arrays | **Safe to fix** in test code if removal from solution-wide is done. Otherwise redundant with Directory.Build.props. |
| CA1865 | Use `string.StartsWith(char)` | Same as above. |
| CA1510 | Use `ArgumentNullException.ThrowIfNull` | Same as above. |
| IDE0021-IDE0023 | Expression body preferences | Same as above. |
| IDE1006 | Naming rule violation | **Requires investigation.** Test interfaces/classes may have intentional naming. |
| IDE0044 | Add readonly modifier | **Safe to fix** in test code. Add `readonly` to appropriate fields. |
| CS4014 | Unawaited async call | **Requires investigation.** May be intentional in some tests (fire-and-forget pattern). |
| xUnit1051 | CancellationToken parameter | **Justified (keep).** Tests intentionally verify CancellationToken overloads. |

Note: Many of these overlap with Directory.Build.props. If the solution-wide suppressions are removed, these per-project ones would need to be evaluated independently.

##### G2: Documentation Samples Project

**File:** `src/Tests/KnockOff.Documentation.Samples/KnockOff.Documentation.Samples.csproj` line 8
**Suppressed:** `CA1861;CA1865;CA1510;IDE0021;IDE0022;IDE0023;IDE1006;IDE0044;CS4014;xUnit1051;xUnit1031`

| Code | Description | Assessment |
|------|-------------|------------|
| xUnit1031 | Do not use blocking task operations in test methods | **Requires investigation.** May be intentional for Moq/NSubstitute comparison samples. |
| Others | Same as G1 | Same assessments. |

##### G3: Individual Test File Pragmas

| File | Code | Assessment |
|------|------|------------|
| `Analysis.Tests/ClassConstructorTests.cs:44` | CA1819 | **Justified.** Test deliberately uses array property. |
| `Analysis.Tests/ClassMethodVoidWithEventsTests.cs:36-37` | CA1070, CS0067 | **Justified.** Test deliberately uses virtual events and unused events. |
| `Analysis.Tests/ClassMethodReturnWithEventsTests.cs:36-37` | CA1070, CS0067 | **Justified.** Same. |
| `Analysis.Tests/ClassGenericEventsTests.cs:40-41` | CA1070, CS0067 | **Justified.** Same. |
| `Analysis.Tests/ClassPropertyTests.cs:48,53-54` | CA1044, CA1070, CS0067 | **Justified.** Test deliberately uses write-only property, virtual events. |
| `Analysis.Tests/ClassIndexerTests.cs:46-47,67,72-73,81,86-87` | CA1070, CS0067, CA1044 | **Justified.** Same patterns. |
| `Analysis.Tests/OptionalArgumentsTests.cs:66` | IDE0060 | **Justified.** Test deliberately uses unused parameter. |
| `Analysis.Tests/RequiredInitPropertyTests.cs:58` | CS8618 | **Justified.** Test class with required properties. |
| `Analysis.Tests/InterfacePropertyTests.cs:47` | CA1044 | **Justified.** Write-only property test. |
| `Analysis.Tests/InterfaceIndexerTests.cs:63,73` | CA1044 | **Justified.** Same. |
| `Analysis.Tests/AbstractClassPropertyTests.cs:51` | CA1044 | **Justified.** Same. |
| `Analysis.Tests/AbstractClassIndexerTests.cs:54` | CA1044 | **Justified.** Same. |
| `Analysis.Tests/DoesNotReturnTests.cs:151,172` | CS0162 | **Justified.** Unreachable code after `[DoesNotReturn]` method calls is the point of the test. |
| `Analysis.Tests/EventTests.cs:35` | CA1711 | **Justified.** Test uses `EventHandler` suffix intentionally. |
| `KnockOffTests/OverloadedMethodTests.cs:141` | xUnit1051 | **Justified.** Testing CancellationToken overload specifically. |
| `KnockOffTests/IndexerGapReproductionTests.cs:47` | CA1044 | **Justified.** Write-only property test. |
| `KnockOffTests/GenericTypeValidationTests.cs:209` | CA1052 | **Justified.** Partial stub class pattern. |
| `NeatooInterfaceTests/OtherBuiltInRuleTests.cs:463` | CS0067 | **Justified.** Unused event in test. |

---

#### SECTION H: Benchmark and Sandbox Suppressions

##### H1: Benchmarks

**File:** `src/Benchmarks/KnockOff.Benchmarks/KnockOff.Benchmarks.csproj` line 10
**Suppressed:** `CA1515;CA2227;CA1002;CA1724;CA1707`

| Code | Assessment |
|------|------------|
| CA1515 | **Justified (keep).** Public types needed for BenchmarkDotNet. |
| CA2227 | **Justified (keep).** Collection properties needed for BenchmarkDotNet. |
| CA1002 | **Justified (keep).** Generic list exposure in benchmark setup. |
| CA1724 | **Justified (keep).** Type names matching namespaces. |
| CA1707 | **Justified (keep).** Underscores in benchmark method names. |

**File-level pragmas:**

| File | Code | Assessment |
|------|------|------------|
| `FrameworkComparisonBenchmarks.cs:8-10` | CA1716, CA2007, CA1307 | **Justified (keep).** Benchmark code. |
| `VerificationBenchmarks.cs:24` | CA1859 | **Justified (keep).** Intentional interface type use. |
| `OverloadedMethodBenchmarks.cs:112` | CA1859 | **Justified (keep).** Same. |

##### H2: Sandbox

**File:** `src/Tests/KnockOffSandbox/KnockOffSandbox.csproj` line 8
**Suppressed:** `CA1303;CA1515;CA1859`

| Code | Assessment |
|------|------------|
| CA1303 | **Justified (keep).** Sandbox uses string literals. |
| CA1515 | **Justified (keep).** Public types in console app. |
| CA1859 | **Justified (keep).** Intentional interface usage. |

---

## Implementation Steps

### Phase 1: Safe NoWarn Removals from Directory.Build.props

**Goal:** Remove as many solution-wide NoWarn entries as possible by fixing underlying code.

**Target entries:** CA1861, CA1865, CA1510, IDE0021, IDE0022, IDE0023, CA1822

1. For each target code:
   a. Remove from `Directory.Build.props`
   b. Build the solution and capture all warnings
   c. Fix each warning in the source code
   d. If any warning cannot be fixed without significant restructuring, add a per-project suppression instead of a blanket solution-wide one
2. For IDE0021/IDE0022/IDE0023 (expression body preferences): Consider adding `.editorconfig` rules instead of code changes, as these are style preferences
3. For CA1510 (`ArgumentNullException.ThrowIfNull`): The Generator project targets netstandard2.0 which lacks this API. Move CA1510 to a per-project suppression on Generator.csproj if needed.
4. Handle IDE1006 and CA1050: Investigate what triggers them; may need `.editorconfig` configuration or targeted per-project suppressions.
5. **MSB3277 stays** with documented justification (multi-targeting requirement).
6. Verification gate: `dotnet build src/KnockOff.sln` and `dotnet test src/KnockOff.sln` pass with zero warnings and zero failures.

### Phase 2: Generated Code Pragma Cleanup

**Goal:** Eliminate unnecessary pragmas from generated code. Justify all that remain.

1. **SYSLIB0050 investigation:**
   a. Search generated code for any actual usage of `FormatterServices` or obsolete serialization APIs
   b. If no usage found: remove file-level `SYSLIB0050` pragma from `FlatRenderer.cs`, `StandaloneClassRenderer.cs`, and `InlineRenderer.cs`
   c. Check `MethodInterceptorRenderer.cs` -- the `CS8601, SYSLIB0050` pairs may only need `CS8601`
   d. Verify all 9 patterns still compile after removal

2. **CS8601 investigation (source delegation):**
   a. Check if adding null-forgiving operator to source delegation return (`return src.Method(args)!;`) eliminates the warning
   b. If so, update `MethodInterceptorRenderer.cs` to emit `!` on return values and remove the CS8601 pragma
   c. Verify across all patterns

3. **All remaining generated pragmas** (CS8618, CS8765, CS8769, CS8763, CS8603):
   a. Add justification comments to each emission site in the renderer code
   b. These are all inherent to the source generation patterns (detailed in Section D above)

4. Verification gate: Build `src/Design/Design.Stubs` and `src/Tests/KnockOffTests` with zero warnings. Run all tests.

### Phase 3: Library Runtime Pragma Evaluation

**Goal:** Document justification for every pragma in `src/KnockOff/`.

1. **Interceptor structural pragmas** (CA1034, CA1051, CA1002, CA1716):
   a. These protect the interceptor-as-property architecture
   b. Add a file-header comment block to each interceptor file explaining why these suppressions are required
   c. Format: `// Justification: Generated interceptor classes inherit from this type and directly access [fields/nested types/etc.].`
   d. No code changes needed -- these remain justified

2. **CA1062 (argument validation) in interceptor files:**
   a. `InterceptorExtensions.cs` line 1: Evaluate adding null checks on extension method parameters
   b. `PropertyGetInterceptorBase.cs` lines 218, 271: Evaluate adding null/empty check on `params TValue[] values`
   c. `MethodInterceptorRuntime.cs` line 7: Evaluate if individual methods can add null guards

3. **CA1716 (keyword identifiers) in IWhenTracking.cs:**
   a. `Return` and `Call` are the natural KnockOff API names
   b. Add justification comment: VB.NET keyword conflict accepted for API usability

4. Verification gate: `dotnet build src/KnockOff.sln` with zero warnings.

### Phase 4: Test Project Suppression Cleanup

**Goal:** Remove duplicated suppressions from test projects where possible; ensure remaining ones are justified.

1. After Phase 1 removes solution-wide NoWarn entries, update test project `.csproj` files:
   a. Remove entries that were fixed solution-wide
   b. Keep entries that are test-specific (IDE0044, CS4014, xUnit1051)
   c. For IDE0044: Fix readonly field warnings in test code
   d. For CS4014: Investigate each unawaited async call; justify or fix

2. All per-file `#pragma` suppressions in test files are **already justified** (they suppress warnings on intentional test constructs like write-only properties, virtual events, unused events, unreachable code after `[DoesNotReturn]`). These should remain with their existing comments.

3. Verification gate: `dotnet test src/KnockOff.sln` with zero failures.

---

## Acceptance Criteria

- [ ] Solution-wide `NoWarn` in `Directory.Build.props` reduced from 10 entries to only justified ones (expected: MSB3277 remains)
- [ ] Every remaining `#pragma warning disable` in source code has an accompanying justification comment
- [ ] Generated code compiles with zero warnings when consumer enables `TreatWarningsAsErrors=true`
- [ ] All tests pass across all target frameworks
- [ ] No public API changes to the KnockOff library
- [ ] No changes to Prototype or PackageTest projects
- [ ] Design.Stubs and Design.Tests still compile and pass

---

## Dependencies

- `.editorconfig` may need to be created or updated for IDE style rule configuration
- Generator project netstandard2.0 constraint limits which fix patterns are available

---

## Risks / Considerations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Removing a generated-code pragma breaks consumer compilation | Medium | High | Verify across all 9 patterns; test with Design.Stubs and Design.Tests |
| IDE/style rule fixes change code behavior | Low | Medium | IDE rules are cosmetic; review each fix |
| CA1510 fix breaks Generator project | Medium | Low | Move to per-project suppression for Generator if needed |
| IDE1006 naming rule triggers on intentional KnockOff naming patterns | High | Low | Use `.editorconfig` to configure naming rules rather than global suppression |
| Multi-targeting MSB3277 cannot be resolved | Certain | None | Already marked as justified; keep suppression |

---

## Architectural Verification

**Scope Table:**

This is a code-quality cleanup task, not a feature implementation. The scope table format is adapted to show which project areas are affected.

| Area | Affected? | Risk | Notes |
|------|-----------|------|-------|
| Generated Code (all 9 patterns) | Yes | Medium | Pragma changes in renderers affect all consumers |
| Library Runtime (src/KnockOff/) | Yes | High for interceptors, Low for others | Interceptor pragmas are architectural |
| Generator Source (src/Generator/) | Minimal | Low | Only HashCode.cs and NoWarn:RS2008 |
| Design Projects | Minimal | Low | Existing suppressions are justified |
| Test Projects | Yes | Low | Mostly duplicated solution-wide entries |
| Benchmark/Sandbox | No changes planned | N/A | Already justified |

**Breaking Changes:** No -- No public API changes. Generated code patterns remain the same; only unnecessary pragmas are removed.

**Codebase Analysis:**

Files examined:
- `src/Directory.Build.props` -- 10 solution-wide NoWarn entries
- `src/Generator/Generator.csproj` -- RS2008 suppression
- `src/Generator/HashCode.cs` -- RS1035 and CS0809 suppressions
- `src/Generator/Renderer/FlatRenderer.cs` -- SYSLIB0050 file-level pragma
- `src/Generator/Renderer/InlineRenderer.cs` -- SYSLIB0050 file-level pragma
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- SYSLIB0050, CS8618, CS8765, CS8763 pragmas
- `src/Generator/Renderer/ClassRenderer.cs` -- CS8618, CS8765, CS8763, CS8603 pragmas
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- CS8601+SYSLIB0050 pairs, CS8618
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` -- CS8618
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- CS8618
- `src/Generator/Builder/FlatModelBuilder.cs` -- CS8769 emission
- `src/Generator/Builder/InlineModelBuilder.cs` -- CS8769 emission
- `src/KnockOff/Interceptors/MethodInterceptorRuntime.cs` -- CA1034, CA1051, CA1002, CA1062, CA1716
- `src/KnockOff/Interceptors/PropertyGetInterceptor.cs` -- CA1034, CA1051
- `src/KnockOff/Interceptors/PropertySetInterceptor.cs` -- CA1034, CA1051
- `src/KnockOff/Interceptors/PropertyGetSetInterceptor.cs` -- CA1034, CA1051
- `src/KnockOff/Interceptors/PropertyGetInterceptorBase.cs` -- CA1034, CA1051, CA1002, CA1062
- `src/KnockOff/Interceptors/PropertySetInterceptorBase.cs` -- CA1034, CA1051, CA1002
- `src/KnockOff/Interceptors/PropertyGetSetInterceptorBase.cs` -- CA1034, CA1051, CA1002
- `src/KnockOff/Interceptors/IndexerGetSetInterceptor.cs` -- CA1034, CA1051
- `src/KnockOff/Interceptors/IndexerGetSetInterceptorBase.cs` -- CA1034, CA1051, CA1002, CA1716
- `src/KnockOff/Interceptors/InterceptorExtensions.cs` -- CA1062
- `src/KnockOff/IWhenTracking.cs` -- CA1716
- All test `.csproj` files
- All Design `.csproj` files
- Benchmark and Sandbox `.csproj` files
- Generated `.g.cs` files (verified SYSLIB0050 usage)

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Safe NoWarn Removals | developer | Yes | Touches many files across solution; needs fresh context for broad code changes | None |
| Phase 2: Generated Code Pragma Cleanup | developer | Yes | Renderer-focused; different files and concerns than Phase 1 | Phase 1 (solution should build cleanly first) |
| Phase 3: Library Runtime Pragma Evaluation | developer | Yes | Focused on src/KnockOff/ interceptor files | Phase 2 (generated code clean first) |
| Phase 4: Test Project Cleanup | developer | Yes | Test projects only; depends on what Phase 1 resolved | Phase 1 |

**Parallelizable phases:** Phase 3 and Phase 4 can run in parallel after Phase 2.

**Notes:** Phase 2 is the most important phase for consumers. Phase 1 should be done first to establish a clean baseline.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-23

### My Understanding of This Plan

**Core Change:** Systematically audit every warning suppression in the KnockOff solution, remove those that can be fixed, and document justifications for those that must remain.
**User-Facing API:** No user-facing API changes. Generated code should continue to compile cleanly under `TreatWarningsAsErrors=true`. The only user-visible effect is fewer unnecessary `#pragma` directives in generated `.g.cs` files (specifically SYSLIB0050 removal).
**Internal Changes:** Remove NoWarn entries from `.props`/`.csproj` by fixing code; remove cargo-culted SYSLIB0050 from renderers; add justification comments to remaining suppressions; investigate CA1062 fixes in interceptor extension methods.
**Patterns Affected:** All 9 patterns (via renderer pragma changes in Phase 2); no behavioral changes.

### Codebase Investigation

**Files Examined:**
- `src/Directory.Build.props` -- Confirmed 10 NoWarn entries exactly as listed in plan Section A
- `src/Generator/Renderer/FlatRenderer.cs` -- SYSLIB0050 at lines 25, 244 confirmed
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- SYSLIB0050 at lines 28, 249; CS8618 at 667; CS8765 at 772, 901; CS8763 at 996 confirmed
- `src/Generator/Renderer/InlineRenderer.cs` -- SYSLIB0050 at line 23 confirmed
- `src/Generator/Renderer/ClassRenderer.cs` -- CS8618 at 694; CS8765 at 805, 895, 1215; CS8763 at 997, 1220; CS8603 at 1216 confirmed
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- CS8601+SYSLIB0050 pairs at lines 1055, 1136, 2481, 2696, 2908, 3017 confirmed; CS8618 at 2220 confirmed
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` -- CS8618 at 533 confirmed
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- CS8618 at 263 confirmed
- `src/Generator/Builder/FlatModelBuilder.cs` -- CS8769 at 1792 confirmed
- `src/Generator/Builder/InlineModelBuilder.cs` -- CS8769 at 1630 confirmed
- `src/Generator/HashCode.cs` -- RS1035 at 28, CS0809 at 186 confirmed
- `src/KnockOff/Interceptors/MethodInterceptorRuntime.cs` -- CA1034, CA1051, CA1002, CA1062, CA1716 at lines 4-8 confirmed
- `src/KnockOff/Interceptors/InterceptorExtensions.cs` -- CA1062 at line 1 confirmed
- `src/KnockOff/Interceptors/PropertyGetInterceptorBase.cs` -- CA1034, CA1051, CA1002 at lines 1-3; CA1062 at lines 218, 271 confirmed
- `src/KnockOff/IWhenTracking.cs` -- CA1716 at lines 62, 85 confirmed
- `src/Tests/KnockOffTests/KnockOffTests.csproj` -- NoWarn without $(NoWarn) prefix confirmed
- `src/Tests/KnockOff.Documentation.Samples/KnockOff.Documentation.Samples.csproj` -- NoWarn without $(NoWarn) prefix; AnalysisMode=default confirmed
- `src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/CalculatorStub.g.cs` -- SYSLIB0050 at line 3 with zero SYSLIB0050-triggering API calls in the file confirmed

**Searches Performed:**
- `FormatterServices|GetUninitializedObject|ISerializable|IFormatter|BinaryFormatter|SerializationInfo|StreamingContext` across `src/Generator/` and `src/KnockOff/` -- ZERO matches. SYSLIB0050 is confirmed cargo-culted.
- `#pragma warning disable` count across all `.cs` in `src/` -- 130 occurrences in 56 files. Matches plan claim.
- `NoWarn` in all `.csproj` files -- 11 files found. All accounted for in plan.
- `NoWarn` in all `.props` files -- 2 files (main `Directory.Build.props` and `Prototype/Directory.Build.props`). Both accounted for.
- `GlobalSuppressions.cs`, `.ruleset`, `.globalconfig` -- None exist. No hidden suppression mechanisms.

**Discrepancies Found:**
- See Concern 1 below regarding NoWarn inheritance in test projects.

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | Phase 2: Remove file-level `SYSLIB0050` from `FlatRenderer.Render()` line 25, `FlatRenderer.RenderBaseClass()` line 244, `InlineRenderer.Render()` line 23, `StandaloneClassRenderer.Render()` line 28, `StandaloneClassRenderer.RenderBaseClass()` line 249. Remove `SYSLIB0050` from `CS8601, SYSLIB0050` pairs in `MethodInterceptorRenderer` (6 sites). All remaining renderer pragmas (CS8618, CS8765, CS8769, CS8763, CS8603) are justified by inherent C# limitations of source generation patterns (required members, ref returns, nullability on overrides, DoesNotReturn stubs, unconstrained type params). | Generated code compiles with zero warnings for consumers with TreatWarningsAsErrors=true. | Yes | SYSLIB0050 removal verified safe (zero API usage found). Remaining pragmas are all inherent to the patterns. |
| 2 | Phase 1: For each of CA1861, CA1865, CA1510, IDE0021-023, CA1822: remove from `Directory.Build.props` line 9, build solution, fix all resulting warnings in source code. For CA1510 specifically: `Generator.csproj` targets netstandard2.0 which lacks `ArgumentNullException.ThrowIfNull` -- move CA1510 suppression to `Generator.csproj` if needed. MSB3277 stays (multi-targeting inherent). IDE1006 and CA1050 require investigation before removal. | Solution builds with zero warnings and zero errors after each removal. | Yes | Approach is sound. See Concern 1 about test project NoWarn independence. |
| 3 | Phase 2 step 1: Remove SYSLIB0050 from file-level emissions in `FlatRenderer.Render()`, `FlatRenderer.RenderBaseClass()`, `StandaloneClassRenderer.Render()`, `StandaloneClassRenderer.RenderBaseClass()`, `InlineRenderer.Render()`. Remove SYSLIB0050 from inline pairs in `MethodInterceptorRenderer` at 6 sites. Verification: build Design.Stubs (covers patterns 1-9 via inline and standalone stubs) and run all tests. | Generated code for all 9 patterns compiles with zero warnings. | Yes | Pipeline coverage: FlatRenderer (patterns 1,2), StandaloneClassRenderer (3,4), InlineRenderer (5-9) -- all three renderer entry points are covered. Shared renderers (MethodInterceptorRenderer) cover all pipelines. |
| 4 | Phase 3 step 1: Add justification comment block to each interceptor file header explaining why CA1034/CA1051/CA1002/CA1716 suppressions are required (generated code inherits and accesses public fields/nested types). Phase 3 step 3: Add justification comment to `IWhenTracking.cs` CA1716 lines (VB.NET keyword conflict accepted). Phase 2 step 3: Add justification comments to remaining renderer pragma emission sites (CS8618, CS8765, CS8769, CS8763, CS8603). | Every remaining suppression has an accompanying justification comment. | Yes | |
| 5 | Phase 2 verification gate: After modifying any renderer, build `src/Design/Design.Stubs` (exercises all pattern pipelines), build `src/Tests/KnockOffTests` (exercises standalone and inline patterns), run full test suite. Each renderer pipeline (Flat, StandaloneClass, Inline/Class, Shared) is independently verified through the generated code it produces. | All four renderer pipelines verified independently. | Yes | Design.Stubs compilation is the strongest verification since it exercises all patterns. |
| 6 | Phase 3 steps 1-3: Each interceptor file suppression evaluated individually. CA1034/CA1051/CA1002/CA1716 assessed as justified (generated code architecture requires public nested types and fields). CA1062 in `InterceptorExtensions.cs` and `PropertyGetInterceptorBase.cs` assessed as fixable (add null guards). CA1062 in `MethodInterceptorRuntime.cs` assessed for individual method null guards. CA1716 in `IWhenTracking.cs` assessed as justified (API naming). Each kept suppression documented with justification. | Each library pragma assessed for removal feasibility and documented. | Yes | |

### Concerns

**Concern 1: Test Project NoWarn Override Pattern (Non-Blocking)**

The plan's Phase 4 description (lines 497-499) says "After Phase 1 removes solution-wide NoWarn entries, update test project .csproj files: Remove entries that were fixed solution-wide." This implies the test projects inherit from `Directory.Build.props` via `$(NoWarn)`. They do not.

All four test projects (`KnockOffTests.csproj`, `KnockOffTests.AssemblyStrict.csproj`, `KnockOff.Analysis.Tests.csproj`, `KnockOff.NeatooInterfaceTests.csproj`) and the `Documentation.Samples` project set `<NoWarn>` directly without the `$(NoWarn);` prefix, which **replaces** the inherited value rather than appending to it. Similarly, `KnockOffSandbox.csproj` uses the same override pattern.

**Impact:** Phase 1 changes to `Directory.Build.props` will have NO effect on these projects. The test projects have their own independent copies of the same suppression codes.

**Recommendation:** This is non-blocking because Phase 4 already plans to update test `.csproj` files. But the developer should be aware that:
1. Removing codes from `Directory.Build.props` does not automatically surface warnings in test projects.
2. After fixing codes in Phase 1, the test project `.csproj` files need the same codes removed independently.
3. Consider switching test projects to `$(NoWarn);additional-codes` pattern so they properly inherit from `Directory.Build.props` going forward. This would prevent the duplication from recurring.

**Concern 2: AnalysisMode Inconsistency (Non-Blocking, Informational)**

Several test projects set `<AnalysisMode>default</AnalysisMode>` (e.g., `KnockOffTests.csproj` line 6, `Documentation.Samples` line 6, `KnockOffSandbox.csproj` line 7), while `Directory.Build.props` sets `<AnalysisMode>all</AnalysisMode>`. This means test projects have fewer active analysis rules. The plan does not address this inconsistency.

This is purely informational -- not blocking for the current scope. But it means that even after removing NoWarn entries from test projects, some rules (like CA1861, CA1865) might not fire in test projects if they are only active under `AnalysisMode=all`. The implementer should verify which rules are active under `default` mode when removing test project NoWarn entries.

### What Looks Good

- The suppression catalog is comprehensive and verified. All 56 files with pragmas are accounted for. All 11 `.csproj` NoWarn entries are cataloged. No hidden suppression mechanisms (GlobalSuppressions.cs, .ruleset, .globalconfig) exist.
- The SYSLIB0050 finding is correct and well-documented. Zero SYSLIB0050 API usage exists in the generator or library code. This is a clear win.
- The risk tiering (Safe to fix / Requires investigation / Justified) is accurate based on my codebase examination.
- The phasing is practical. Phase 1 establishes a clean build baseline before touching generated code. Phase 2 is the highest-value phase (affects consumers). Phases 3 and 4 are correctly identified as parallelizable after Phase 2.
- The interceptor justifications (CA1034, CA1051, CA1002) are correct -- these protect the interceptor-as-property architecture and cannot be removed without breaking all generated code.
- The stop conditions are appropriate for this type of work.

### Why This Plan Is Approved

This plan is a code-quality cleanup, not a feature implementation. There are no business rules in the traditional sense -- the "rules" are all build verification gates (does it compile? do tests pass?). The suppression catalog is exhaustive and I verified it against the actual codebase. The risk assessments are accurate. The SYSLIB0050 finding is a genuine discovery backed by evidence (zero API references found). The phasing is practical and the verification gates are appropriate. The two concerns I identified are non-blocking and informational -- they describe implementation details the developer should be aware of, not design flaws.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. What if removing SYSLIB0050 from the file-level pragma in renderers causes warnings from code that the MethodInterceptorRenderer's inline `CS8601, SYSLIB0050` pairs previously covered? Answer: the file-level pragma is a superset -- removing it means the inline pairs need to be checked independently. The plan addresses this (Phase 2 step 1c).
2. What if IDE1006 naming violations are in generated code member names (e.g., interceptor property names like `Method_`)? The plan flags this for investigation but the developer should check whether removing IDE1006 from Directory.Build.props causes warnings in generated code, not just source code.
3. What if CA1050 ("types in namespaces") is triggered by generated code that uses global namespace? Again, removal from Directory.Build.props could surface warnings in generated output.

**Ways this could break existing functionality:**
1. If the null-forgiving operator (`!`) approach for CS8601 (Phase 2 step 2) changes the runtime behavior for null returns from source delegation, it could mask a real null reference. However, this would only affect the source delegation path which already returns unchecked values from the source object, so the risk is bounded.

**Ways users could misunderstand the API:**
1. Not applicable -- no API changes.

---

## Implementation Contract

**Created:** 2026-03-23
**Approved by:** knockoff-developer

### Verification Acceptance Criteria

- [ ] `dotnet build src/KnockOff.sln` -- zero warnings, zero errors
- [ ] `dotnet test src/KnockOff.sln` -- all tests pass
- [ ] `dotnet build src/Design/Design.Stubs` -- zero warnings
- [ ] `dotnet test src/Design/Design.Tests` -- all tests pass

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1 | Build verification | Solution-wide build with removed NoWarn entries |
| 2 | Build verification | Generated code compilation after SYSLIB0050 removal |
| 3 | Build verification | Design.Stubs compilation (represents consumer project) |
| 4 | Code review | Justification comments on interceptor files |
| 5 | Build verification | Test project compilation after IDE0044 fixes |

### In Scope

- [ ] `src/Directory.Build.props` -- remove safe NoWarn entries (CA1861, CA1865, CA1510, IDE0021-023, CA1822) by fixing underlying code; investigate IDE1006, CA1050
- [ ] `.editorconfig` -- create at `src/` level with style rules for IDE0021/IDE0022/IDE0023 if expression body style is preferred over code changes
- [ ] `src/Generator/Renderer/FlatRenderer.cs` -- remove file-level SYSLIB0050 pragma (lines 25, 244)
- [ ] `src/Generator/Renderer/InlineRenderer.cs` -- remove file-level SYSLIB0050 pragma (line 23)
- [ ] `src/Generator/Renderer/StandaloneClassRenderer.cs` -- remove file-level SYSLIB0050 pragma (lines 28, 249)
- [ ] `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- remove SYSLIB0050 from CS8601+SYSLIB0050 pairs (6 sites); investigate CS8601 fix via null-forgiving operator
- [ ] `src/Generator/Renderer/ClassRenderer.cs` -- add justification comments to CS8618, CS8765, CS8763, CS8603 emission sites
- [ ] `src/Generator/Renderer/StandaloneClassRenderer.cs` -- add justification comments to CS8618, CS8765, CS8763 emission sites
- [ ] `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` -- add justification comment to CS8618 emission site
- [ ] `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- add justification comment to CS8618 emission site
- [ ] `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- add justification comment to CS8618 emission site
- [ ] `src/Generator/Builder/FlatModelBuilder.cs` -- add justification comment to CS8769 emission site
- [ ] `src/Generator/Builder/InlineModelBuilder.cs` -- add justification comment to CS8769 emission site
- [ ] `src/KnockOff/Interceptors/*.cs` -- add file-header justification comment blocks to all 9 interceptor files explaining structural suppression rationale
- [ ] `src/KnockOff/Interceptors/InterceptorExtensions.cs` -- evaluate and fix CA1062 (add null checks)
- [ ] `src/KnockOff/Interceptors/PropertyGetInterceptorBase.cs` -- evaluate and fix CA1062 at lines 218, 271
- [ ] `src/KnockOff/Interceptors/MethodInterceptorRuntime.cs` -- evaluate CA1062 fixability for individual methods
- [ ] `src/KnockOff/IWhenTracking.cs` -- add justification comments to CA1716 pragmas
- [ ] Test project `.csproj` files -- remove codes fixed in Phase 1 independently (note: these override Directory.Build.props, not inherit); fix IDE0044; investigate CS4014
- [ ] Consider switching test project NoWarn to `$(NoWarn);additional-codes` pattern for proper inheritance

### Explicitly Out of Scope

- `src/Prototype/` -- experimental, excluded per user direction
- `src/Tests/PackageTest/` -- experimental, excluded per user direction
- Public API signature changes to KnockOff library
- Behavioral changes to generated code
- AnalysisMode inconsistencies between test projects and Directory.Build.props (informational finding, not in scope for this cleanup)

### Developer Notes

1. **Test project NoWarn independence:** The four test projects and Documentation.Samples all set `<NoWarn>` without `$(NoWarn);` prefix. They do NOT inherit from `Directory.Build.props`. Phase 1 changes to Directory.Build.props will NOT affect test projects. Handle test project NoWarn independently in Phase 4.
2. **AnalysisMode default in test projects:** Test projects use `AnalysisMode=default` vs solution-wide `all`. Some CA rules may not fire under `default` mode. Verify which rules are active before attempting to remove test project NoWarn entries.
3. **IDE1006 in generated code:** Check whether removing IDE1006 from Directory.Build.props causes warnings in generated code (interceptor property names). If so, add to `.editorconfig` or per-project suppression.
4. **CA1050 in generated code:** Check whether removing CA1050 causes warnings in generated code that uses global namespace. If so, add per-project suppression.

### Verification Gates

1. After Phase 1: `dotnet build src/KnockOff.sln` passes with zero warnings
2. After Phase 2: `dotnet build src/Design/Design.Stubs` and `dotnet test src/KnockOff.sln` both pass with zero warnings/failures
3. After Phase 3: `dotnet build src/KnockOff/KnockOff.csproj` passes cleanly; justification comments verified in place
4. Final: `dotnet build src/KnockOff.sln` zero warnings + `dotnet test src/KnockOff.sln` all tests pass + `dotnet build src/Design/Design.Stubs` zero warnings

### Stop Conditions

If any occur, STOP and report:
- Removing a pragma causes generated code to fail compilation in any of the 9 patterns
- A code fix changes runtime behavior (e.g., null-forgiving operator masks real null reference)
- A suppression removal would require public API changes
- IDE1006 or CA1050 removal surfaces warnings in generated code that cannot be addressed without generator changes

---

## Implementation Progress

**Started:** [date]
**Developer:** [agent name]

**Phase 1:** Safe NoWarn Removals
- [ ] Remove and fix CA1861, CA1865, CA1510, CA1822
- [ ] Configure IDE0021/IDE0022/IDE0023 via .editorconfig
- [ ] Investigate IDE1006, CA1050
- [ ] **Verification**: Solution builds with zero warnings

**Phase 2:** Generated Code Pragma Cleanup
- [ ] Investigate SYSLIB0050 necessity
- [ ] Investigate CS8601 fix via null-forgiving operator
- [ ] Add justification comments to remaining renderer pragmas
- [ ] **Verification**: Design.Stubs and all test projects build cleanly

**Phase 3:** Library Runtime Pragma Evaluation
- [ ] Add justification comments to interceptor file pragmas
- [ ] Evaluate CA1062 fixes in InterceptorExtensions.cs and PropertyGetInterceptorBase.cs
- [ ] **Verification**: Library builds cleanly

**Phase 4:** Test Project Cleanup
- [ ] Update test .csproj NoWarn lists after Phase 1
- [ ] Fix IDE0044 in test code
- [ ] Investigate CS4014 in test code
- [ ] **Verification**: All tests pass

---

## Completion Evidence

**Reported:** [date]

- **Tests Passing:** [Output or summary]
- **Verification Resources Pass:** [Yes/No/N/A]
- **All Contract Items:** [Confirmed 100% complete]

---

## Documentation

**Agent:** [documentation agent name]
**Completed:** [date]

### Expected Deliverables

- [ ] Justification comments on all remaining suppressions (in-code documentation)
- [ ] Skill updates: No
- [ ] Sample updates: No

### Files Updated

---

## Architect Verification

**Verified:** [date]
**Verdict:** [VERIFIED | SENT BACK]

**Independent test results:**
- [Project/module]: [Build result]
- All tests: [X passed, Y failed]

**Design match:** [Does the implementation match the original plan?]

**Issues found:** [List any issues, or "None"]

---

## Requirements Verification

**Reviewer:** [agent name]
**Verified:** [date]
**Verdict:** [REQUIREMENTS SATISFIED | REQUIREMENTS VIOLATION]

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Generated code compiles under TreatWarningsAsErrors | | |
| Library interceptor API unchanged | | |

### Unintended Side Effects

### Issues Found
