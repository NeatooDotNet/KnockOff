# Create Compiled Samples for Pseudo-Code Blocks

All phases complete - 53 blocks converted to compiled samples.

## Progress Tracking

| Phase | Blocks | Done | Remaining |
|-------|--------|------|-----------|
| 1: Core API | 10 | 10 | 0 |
| 2: Features | 18 | 18 | 0 |
| 3: Reference | 9 | 2 | 7 (legitimate pseudo) |
| 4: Moq | 23 | 23 | 0 |
| **Total** | **60** | **53** | **7** |

---

## Phase 1: KnockOff Core API - Complete

All 10 blocks converted.

---

## Phase 2: Feature Documentation - Complete

All 18 blocks converted:
- [x] docs/guides/generics.md (7 blocks)
- [x] docs/guides/events.md (2 blocks)
- [x] docs/guides/multiple-interfaces.md (3 blocks)
- [x] docs/guides/delegates.md (1 block)
- [x] docs/guides/properties.md (1 block)
- [x] docs/guides/best-practices.md (2 blocks) - `reset-clear-backing`, `complex-interface-ok`
- [x] docs/guides/inline-stubs.md (2 blocks) - `collision-naming-pattern`, `no-collision-naming`

---

## Phase 3: Reference Documentation - Complete

Most Phase 3 blocks are legitimately pseudo (API reference, hypothetical features, XML config).

#### Converted to `generated:` marker:
- [x] `generated-code-full-structure` - Now points to actual `GenUserServiceKnockOff.g.cs`

#### Legitimately pseudo (API reference / incomplete fragments):
- `knockoff-attribute-declaration` - Library internal source code
- `knockoff-namespace-using` - Just a using statement
- `emit-generated-files-csproj` - XML/MSBuild config, not C#

#### Legitimately pseudo (hypothetical features):
- `knockoff-future-naming` - Not yet implemented
- `knockoff-future-strict` - Not yet implemented (see strict-mode.md todo)
- `knockoff-future-exclude` - Not yet implemented

#### Already compiled:
- [x] `generated-code-input-example`
- [x] `generated-code-multiple-parameters`
- [x] `generated-code-interface-constraint-separate`

---

## Phase 4: Moq Comparison - Complete

Added Moq package reference to `KnockOff.Documentation.Samples.Tests` and created compiled samples.

#### docs/knockoff-vs-moq.md (9 blocks)

- [x] `moq-basic-setup` - Mock, Setup, Verify
- [x] `moq-property-mocking` - Property setup/verify
- [x] `moq-async-methods` - ReturnsAsync
- [x] `moq-argument-capture` - Callback capture
- [x] `moq-multiple-interfaces` - mock.As<T>()
- [x] `moq-indexer-mocking` - Indexer setup
- [x] `moq-event-mocking` - Raise, SetupAdd
- [x] `moq-verification-patterns` - Times.*
- [x] `moq-sequential-returns` - SetupSequence

#### docs/migration-from-moq.md (14 blocks)

- [x] `moq-create-mock`
- [x] `moq-mock-object`
- [x] `moq-setup-returns`
- [x] `moq-async-returns`
- [x] `moq-verification`
- [x] `moq-callback`
- [x] `moq-property-setup`
- [x] `moq-static-returns`
- [x] `moq-conditional-returns`
- [x] `moq-throwing-exceptions`
- [x] `moq-setup-sequence`
- [x] `moq-multiple-interfaces-as`
- [x] `moq-argument-matching`
- [x] `gradual-migration` - Moq + KnockOff coexistence

---

## Notes

### Remaining pseudo blocks are intentional

The 7 remaining pseudo blocks cannot be compiled:
- 3 hypothetical future features (`knockoff-future-*`)
- 3 reference fragments (attribute declaration, using statement, XML config)
- 1 converted to `generated:` marker

### Implementation Details

Moq samples created in:
- `src/Tests/KnockOff.Documentation.Samples.Tests/Comparison/MoqComparisonSamples.cs` - 9 snippets for knockoff-vs-moq.md
- `src/Tests/KnockOff.Documentation.Samples.Tests/Comparison/MoqMigrationSamples.cs` - 14 snippets for migration-from-moq.md

Types use `MoqMig` prefix (for migration samples) and `Moq` prefix (for comparison samples) to avoid conflicts with KnockOff stub types.
