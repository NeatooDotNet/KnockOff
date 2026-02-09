# Record Stub Support

**Status:** Complete
**Priority:** High
**Created:** 2026-02-08
**Last Updated:** 2026-02-08
**Plan:** [record-stub-support.md](../plans/record-stub-support.md)

---

## Problem

KnockOff cannot stub C# record types. When users apply `[KnockOff<MyRecord>]` to stub a record, the generated code fails to compile with 500+ errors.

Records are increasingly common in modern C# codebases. Users expect to stub records the same way they stub classes.

### Root Cause

The generator treats records as regular classes but records have special compiler-synthesized members and inheritance rules that the generator doesn't account for:

1. **`CS8865: Only records may inherit from records`** — The inner `Impl` class is generated as `sealed class Impl : MyRecord` but must be `sealed record Impl : MyRecord`
2. **`<Clone>$` method** — Records have a compiler-synthesized `<Clone>$` method. The generator picks it up as a virtual method named "Clone" and tries to create interceptors/overrides for it, which doesn't compile (the method name isn't valid C# syntax)
3. **Duplicate synthesized members** — `ToString()`, `PrintMembers()`, `GetHashCode()`, `Equals()` are virtual in records and get picked up by the member scanner, but the `Impl` record already inherits correct implementations. Overriding them creates duplicates or incorrect interceptors
4. **`EqualityContract` property** — Record-specific `protected virtual Type EqualityContract` property is picked up and generates broken interceptors
5. **Positional record constructor** — Positional records (`record Foo(string Name, int Age)`) have synthesized virtual properties and a `Deconstruct` method that need special handling

### Reproduction

Test file at `src/Tests/KnockOffTests/RecordTests.cs` on the `recordsSupport` branch reproduces the issue with three record types:
- `MyRecord` — record with a virtual method (mirrors Rocks test)
- `PositionalRecord(string Name, int Age)` — record with positional properties
- `AbstractRecord` — abstract record with abstract method

## Solution

Filter out record-synthesized members that shouldn't be intercepted, and ensure the `Impl` type uses `record` instead of `class` when the target type is a record.

### Specific Changes Needed

1. **Use `record` keyword for Impl** — When `INamedTypeSymbol.IsRecord` is true, generate `sealed record Impl` instead of `sealed class Impl`
2. **Skip `<Clone>$` method** — Filter out the compiler-synthesized clone method (has `MethodKind.Ordinary` but name `<Clone>$`)
3. **Skip record equality/formatting members** — Filter out `EqualityContract`, `Equals(T)`, `GetHashCode()`, `ToString()`, `PrintMembers(StringBuilder)` when they come from the record synthesizer (not user-defined overrides)
4. **Handle positional properties** — Positional record properties ARE virtual and SHOULD be intercepted (this is the main value of stubbing records)
5. **Handle `Deconstruct`** — Decide whether to intercept or skip

### Patterns Affected

- [ ] Inline Class (Pattern 6) — `[KnockOff<MyRecord>]`
- [ ] Open Generic Class (Pattern 9) — `[KnockOff(typeof(MyRecord<>))]`
- [ ] Standalone Class (Pattern 3) — `[KnockOffBase<MyRecord>]`
- [ ] Generic Standalone Class (Pattern 4) — `[KnockOffBase(typeof(MyRecord<>))]`

All four class stub patterns use the same member-scanning logic, so the fix should apply to all.

### Pipelines Affected

| Patterns | Transform | Builder | Renderer |
|---|---|---|---|
| Inline class (6) | `TransformInlineStubClass` | `InlineModelBuilder` / `ClassModelBuilder` | `InlineRenderer` / `ClassRenderer` |
| Open generic class (9) | Various | Various | `InlineRenderer` |
| Standalone class (3,4) | `TransformStandaloneClass` | `StandaloneClassModelBuilder` | `StandaloneClassRenderer` |

### Member Types

- [x] Methods — Need filtering of synthesized methods (`<Clone>$`, `Equals`, `GetHashCode`, `ToString`, `PrintMembers`)
- [x] Properties — Need filtering of `EqualityContract`; positional properties SHOULD be intercepted
- [ ] Indexers — N/A for records
- [ ] Events — N/A (records rarely have events, but should work if present)

---

## Plans

- [Record Stub Support Plan](../plans/record-stub-support.md)

---

## Tasks

- [x] Reproduce the issue with test cases (RecordTests.cs)
- [x] Architect: Design the member filtering approach ([plan](../plans/record-stub-support.md))
- [x] Implement record-aware member filtering
- [x] Implement `record` keyword for Impl type
- [x] Verify all four class stub patterns work
- [x] Add comprehensive tests

---

## Progress Log

- **2026-02-08:** Reproduced the issue. Created `RecordTests.cs` with three record types covering virtual methods, positional properties, and abstract records. Generator produces 500+ compilation errors. Root cause analysis complete — five distinct issues identified.
- **2026-02-08:** Architect created implementation plan. Deep-dived into transform layer (`ExtractClassInfo`), renderers (`ClassRenderer`, `StandaloneClassRenderer`), builders, and models. Plan covers member filtering at transform layer, `IsRecord` flag propagation through models, and `sealed record Impl` generation at renderer layer. Five implementation phases defined.

---

## Results / Conclusions

Record stub support implemented and verified. KnockOff now handles `record` types across all four class stub patterns (3, 4, 6, 9).

**Changes:** 82 lines added across 8 production files (Transform, Models, Builders, Renderers). The fix filters record-synthesized members (`<Clone>$`, `EqualityContract`, `Equals`, `GetHashCode`, `ToString`, `PrintMembers`, `Deconstruct`) and emits `sealed record Impl` instead of `sealed class Impl`.

**Tests:** 8 new tests in `RecordTests.cs` covering virtual methods, positional records, abstract records, explicit virtual properties, and standalone stubs. All 1334 tests pass across net8.0/net9.0/net10.0.

**Key discovery:** Positional record properties (`record Foo(string Name, int Age)`) are NOT interceptable via inheritance — Roslyn reports `IsVirtual=false` for them. Explicitly declared virtual properties on records ARE interceptable. This is a C# language limitation, not a KnockOff limitation.
