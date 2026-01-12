# Create Compiled Samples for Pseudo-Code Blocks

30 blocks currently marked as pseudo-code need to be converted to compiled samples.

## Progress Tracking

| Phase | Blocks | Done | Remaining |
|-------|--------|------|-----------|
| 1: Core API | 10 | 10 | 0 |
| 2: Features | 18 | 18 | 0 |
| 3: Reference | 9 | 2 | 7 |
| 4: Moq | 23 | 0 | 23 |
| **Total** | **60** | **30** | **30** |

---

## Phase 1: KnockOff Core API ✅ Complete

All 10 blocks converted.

---

## Phase 2: Feature Documentation ✅ Complete

All 18 blocks converted:
- [x] docs/guides/generics.md (7 blocks)
- [x] docs/guides/events.md (2 blocks)
- [x] docs/guides/multiple-interfaces.md (3 blocks)
- [x] docs/guides/delegates.md (1 block)
- [x] docs/guides/properties.md (1 block)
- [x] docs/guides/best-practices.md (2 blocks) - `reset-clear-backing`, `complex-interface-ok`
- [x] docs/guides/inline-stubs.md (2 blocks) - `collision-naming-pattern`, `no-collision-naming`

---

## Phase 3: Reference Documentation ✅ Analyzed

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

## Phase 4: Moq Comparison (23 remaining)

**Blocked:** Requires adding Moq package reference to `KnockOff.Documentation.Samples`.

```xml
<PackageReference Include="Moq" Version="4.*" />
```

#### docs/knockoff-vs-moq.md (9 blocks)

- [ ] `moq-basic-setup` - Mock, Setup, Verify
- [ ] `moq-property-mocking` - Property setup/verify
- [ ] `moq-async-methods` - ReturnsAsync
- [ ] `moq-argument-capture` - Callback capture
- [ ] `moq-multiple-interfaces` - mock.As<T>()
- [ ] `moq-indexer-mocking` - Indexer setup
- [ ] `moq-event-mocking` - Raise, SetupAdd
- [ ] `moq-verification-patterns` - Times.*
- [ ] `moq-sequential-returns` - SetupSequence

#### docs/migration-from-moq.md (14 blocks)

- [ ] `moq-create-mock`
- [ ] `moq-mock-object`
- [ ] `moq-setup-returns`
- [ ] `moq-async-returns`
- [ ] `moq-verification`
- [ ] `moq-callback`
- [ ] `moq-property-setup`
- [ ] `moq-static-returns`
- [ ] `moq-conditional-returns`
- [ ] `moq-throwing-exceptions`
- [ ] `moq-setup-sequence`
- [ ] `moq-multiple-interfaces-as`
- [ ] `moq-argument-matching`
- [ ] `gradual-migration` - Moq + KnockOff coexistence

---

## Notes

### Some pseudo blocks are intentional

The `knockoff-future-*` blocks in attributes.md show hypothetical future API - these may stay as pseudo since they don't represent current functionality.

### MSBuild/csproj blocks

`emit-generated-files-csproj` is XML configuration, not C# code. May need different handling or remain as pseudo.
