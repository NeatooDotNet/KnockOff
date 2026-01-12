# Pseudo-Code Block Audit (Revised)

Analysis of 67 blocks currently marked with `<!-- pseudo: -->` markers.

## Corrected Understanding

**Pseudo-code** means code that is inherently non-compilable (fragments, hypotheticals). It does NOT mean:
- Code from other libraries (Moq IS compilable)
- Code that isn't in samples yet (add it)

---

## Summary

| Category | Count | Action |
|----------|-------|--------|
| **COMPILE** | 60 | Create samples, sync with `<!-- snippet: -->` |
| **INVALID** | 2 | Change to `<!-- invalid: -->` |
| **PSEUDO-OK** | 5 | Keep as pseudo (fragments/hypotheticals) |

---

## Truly Pseudo-Code (5 blocks) - Keep as-is

| Block | File | Line | Marker | Reason |
|-------|------|------|--------|--------|
| 17 | indexers.md | 33 | `indexer-single` | Incomplete fragment - just property names with comments |
| 18 | indexers.md | 41 | `indexer-multiple` | Incomplete fragment - just property names with comments |
| 53 | attributes.md | 125 | `knockoff-future-naming` | "NOT YET IMPLEMENTED" |
| 54 | attributes.md | 136 | `knockoff-future-strict` | "NOT YET IMPLEMENTED" |
| 55 | attributes.md | 147 | `knockoff-future-exclude` | "NOT YET IMPLEMENTED" |

---

## Need `<!-- invalid: -->` Marker (2 blocks)

| Block | File | Line | Marker | Reason |
|-------|------|------|--------|--------|
| 58 | generated-code.md | 180 | `user-method-detection` | Shows wrong patterns (public, wrong signature) |
| 60 | generated-code.md | 241 | `interface-constraint-valid` | Contains KO0010 error case |

---

## Need Compiled Samples (60 blocks)

### KnockOff API Usage (38 blocks)

| Block | File | Line | Marker | Code Shows |
|-------|------|------|--------|------------|
| 1 | customization-patterns.md | 157 | `callback-ko-access` | `ko.IsInitialized.WasCalled`, `ko.NameBacking` |
| 2 | customization-patterns.md | 220 | `priority-example-usage` | `Calculate.OnCall`, `Reset()` |
| 3 | customization-patterns.md | 249 | `reset-behavior` | `GetUser.OnCall`, `CallCount`, `Reset()` |
| 4 | customization-patterns.md | 289 | `combining-patterns-usage` | `GetById.OnCall`, `Reset()` |
| 5 | best-practices.md | 24 | `complex-interface-ok` | `[KnockOff<IEditBase>]` usage |
| 6 | best-practices.md | 240 | `reset-clear-backing` | `Reset()`, `.Value = default` |
| 7 | delegates.md | 260 | `named-delegate-workaround` | Named delegate with `[KnockOff<T>]` |
| 8 | events.md | 176 | `viewmodel-event-tests` | `DataChanged.HasSubscribers`, `.Raise()` |
| 9 | events.md | 205 | `viewmodel-unsubscribe-test` | `DataChanged.AddCount`, `RemoveCount` |
| 10 | generics.md | 194 | `generic-method-interface` | Interface + `[KnockOff]` stub |
| 11 | generics.md | 209 | `generic-method-config` | `Deserialize.Of<User>().OnCall` |
| 12 | generics.md | 224 | `generic-method-tracking` | `Of<T>().CallCount`, `TotalCallCount` |
| 13 | generics.md | 248 | `generic-method-multi-param` | Multi-type-param interface |
| 14 | generics.md | 260 | `generic-method-multi-usage` | `Convert.Of<string, int>().OnCall` |
| 15 | generics.md | 271 | `generic-method-constrained` | Constrained generic interface |
| 16 | generics.md | 280 | `generic-method-constrained-usage` | `Create.Of<Employee>().OnCall` |
| 19 | inline-stubs.md | 68 | `collision naming pattern` | Generic collision example |
| 20 | inline-stubs.md | 82 | `no collision naming` | Single generic example |
| 21 | multiple-interfaces.md | 32 | `separate-standalone-stubs` | Separate stubs with `OnCall` |
| 22 | multiple-interfaces.md | 54 | `inline-stubs-multiple` | Inline stubs test pattern |
| 23 | multiple-interfaces.md | 101 | `migration-from-multiple` | Migration example (deprecated + new) |
| 24 | properties.md | 68 | `set-only-property-interface` | Set-only property interface |
| 48 | attributes.md | 10 | `knockoff-attribute-declaration` | Attribute source (sync from library) |
| 49 | attributes.md | 23 | `knockoff-attribute-usage` | Basic attribute usage |
| 50 | attributes.md | 43 | `knockoff-valid-examples` | Multiple valid patterns |
| 51 | attributes.md | 104 | `knockoff-namespace-using` | Using statement |
| 52 | attributes.md | 112 | `knockoff-namespace-qualified` | Fully qualified usage |
| 56 | generated-code.md | 43 | `generated-code-input-example` | Interface + stub with user method |
| 57 | generated-code.md | 61 | `generated-code-full-structure` | Full generated structure (sync from Generated/) |
| 59 | generated-code.md | 217 | `multiple-parameters-tracking` | Tracking structure |
| 61 | generated-code.md | 259 | `interface-constraint-separate` | Separate stubs usage |
| 62 | interceptor-api.md | 57 | `method-interceptor-examples` | Method interceptor usage |
| 63 | interceptor-api.md | 114 | `property-interceptor-examples` | Property interceptor usage |
| 64 | interceptor-api.md | 192 | `indexer-interceptor-examples` | Indexer interceptor usage |
| 65 | interceptor-api.md | 262 | `event-interceptor-examples` | Event interceptor usage |
| 66 | interceptor-api.md | 313 | `async-method-examples` | Async method callbacks |
| 67 | interceptor-api.md | 375 | `generic-method-interceptor-examples` | Generic method `Of<T>()` API |

### Moq Examples (22 blocks) - Need Moq package reference in samples

| Block | File | Line | Marker | Code Shows |
|-------|------|------|--------|------------|
| 25 | knockoff-vs-moq.md | 44 | `moq-basic-setup` | `Mock<T>`, `Setup`, `Verify` |
| 26 | knockoff-vs-moq.md | 85 | `moq-property-mocking` | `Setup`, `SetupSet`, `VerifySet` |
| 27 | knockoff-vs-moq.md | 126 | `moq-async-methods` | `ReturnsAsync` |
| 28 | knockoff-vs-moq.md | 161 | `moq-argument-capture` | `Callback<T>` |
| 29 | knockoff-vs-moq.md | 200 | `moq-multiple-interfaces` | `mock.As<T>()` |
| 30 | knockoff-vs-moq.md | 244 | `moq-indexer-mocking` | Indexer setup |
| 31 | knockoff-vs-moq.md | 281 | `moq-event-mocking` | `Raise`, `SetupAdd` |
| 32 | knockoff-vs-moq.md | 329 | `moq-verification-patterns` | `Times.Once`, `Times.Never`, etc. |
| 33 | knockoff-vs-moq.md | 365 | `moq-sequential-returns` | `SetupSequence` |
| 34 | migration-from-moq.md | 26 | `moq-create-mock` | `new Mock<T>()` |
| 35 | migration-from-moq.md | 51 | `moq-mock-object` | `mock.Object` |
| 36 | migration-from-moq.md | 71 | `moq-setup-returns` | `Setup().Returns()` |
| 37 | migration-from-moq.md | 90 | `moq-async-returns` | `ReturnsAsync` |
| 38 | migration-from-moq.md | 109 | `moq-verification` | `Verify` patterns |
| 39 | migration-from-moq.md | 132 | `moq-callback` | `Callback<T>` |
| 40 | migration-from-moq.md | 158 | `moq-property-setup` | Property setup |
| 41 | migration-from-moq.md | 182 | `moq-static-returns` | Static returns |
| 42 | migration-from-moq.md | 211 | `moq-conditional-returns` | Conditional setup |
| 43 | migration-from-moq.md | 235 | `moq-throwing-exceptions` | `Throws` |
| 44 | migration-from-moq.md | 261 | `moq-setup-sequence` | `SetupSequence` |
| 45 | migration-from-moq.md | 290 | `moq-multiple-interfaces-as` | `mock.As<T>()` |
| 46 | migration-from-moq.md | 330 | `moq-argument-matching` | `It.Is<T>()` |
| 47 | migration-from-moq.md | 460 | `gradual-migration` | Moq + KnockOff coexistence |

---

## Next Steps

1. **Immediate (2 blocks):** Change blocks 58, 60 to `<!-- invalid: -->` markers
2. **Keep (5 blocks):** Blocks 17, 18, 53, 54, 55 are legitimate pseudo-code
3. **Samples work (60 blocks):** Create compiled samples and sync - this is significant work

### Priority for samples work:
1. High: interceptor-api.md examples (62-67) - core API reference
2. High: customization-patterns.md (1-4) - key concept doc
3. Medium: generics.md (10-16) - feature documentation
4. Medium: events.md (8-9) - feature documentation
5. Lower: Moq examples (25-47) - requires adding Moq package reference
