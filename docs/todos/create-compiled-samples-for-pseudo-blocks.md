# Create Compiled Samples for Pseudo-Code Blocks

33 blocks currently marked as pseudo-code need to be converted to compiled samples.

## Progress Tracking

| Phase | Blocks | Done | Remaining |
|-------|--------|------|-----------|
| 1: Core API | 10 | 10 | 0 |
| 2: Features | 18 | 15 | 3 |
| 3: Reference | 9 | 2 | 7 |
| 4: Moq | 23 | 0 | 23 |
| **Total** | **60** | **27** | **33** |

---

## Phase 1: KnockOff Core API ✅ Complete

All 10 blocks converted.

---

## Phase 2: Feature Documentation (3 remaining)

#### docs/guides/best-practices.md (1 block)

- [ ] `complex-interface-ok` (line 23) - Large interface with smart defaults

#### docs/guides/inline-stubs.md (2 blocks)

- [ ] `collision-naming-pattern` (line 67) - Generic collision with suffixes
- [ ] `no-collision-naming` (line 82) - Single generic, simple name

#### Completed in Phase 2:
- [x] docs/guides/generics.md (7 blocks)
- [x] docs/guides/events.md (2 blocks)
- [x] docs/guides/multiple-interfaces.md (3 blocks)
- [x] docs/guides/delegates.md (1 block)
- [x] docs/guides/properties.md (1 block)
- [x] docs/guides/best-practices.md - `reset-clear-backing` (1 block)

---

## Phase 3: Reference Documentation (7 remaining)

#### docs/reference/attributes.md (5 blocks)

- [ ] `knockoff-attribute-declaration` (line 9) - Attribute source code
  - Consider syncing from actual `KnockOffAttribute.cs`
- [ ] `knockoff-namespace-using` (line 97) - Using statement example
- [ ] `knockoff-future-naming` (line 118) - Future naming options
- [ ] `knockoff-future-strict` (line 129) - Future strict mode option
- [ ] `knockoff-future-exclude` (line 140) - Future exclude option

#### docs/reference/generated-code.md (2 blocks)

- [ ] `emit-generated-files-csproj` (line 29) - MSBuild properties
- [ ] `generated-code-full-structure` (line 60) - Full generated structure
  - Consider syncing from actual Generated/ file

#### Completed in Phase 3:
- [x] `generated-code-input-example`
- [x] `interface-constraint-separate`

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
