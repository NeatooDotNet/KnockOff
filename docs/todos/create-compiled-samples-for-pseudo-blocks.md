# Create Compiled Samples for Pseudo-Code Blocks

60 blocks currently marked as pseudo-code need to be converted to compiled samples in `docs/samples/`.

## Task Breakdown

### Phase 1: KnockOff Core API (High Priority)

#### docs/reference/interceptor-api.md (6 blocks) ✅

These are the core API reference examples - highest priority.

- [x] Block 62 (line 57) `interceptor-api-method-interceptor-examples` - Method interceptor usage
  - `WasCalled`, `LastCallArg`, `LastCallArgs`, `OnCall`
- [x] Block 63 (line 114) `interceptor-api-property-interceptor-examples` - Property interceptor usage
  - `GetCount`, `SetCount`, `LastSetValue`, `OnGet`, `OnSet`, `Reset()`
- [x] Block 64 (line 192) `interceptor-api-indexer-interceptor-examples` - Indexer interceptor usage
  - `Indexer.Backing`, `GetCount`, `LastGetKey`, `OnGet`, `OnSet`
- [x] Block 65 (line 262) `interceptor-api-event-interceptor-examples` - Event interceptor usage
  - `AddCount`, `RemoveCount`, `HasSubscribers`, `Raise()`
- [x] Block 66 (line 313) `interceptor-api-async-method-examples` - Async method callbacks
  - `Task.FromResult`, `Task.FromException`
- [x] Block 67 (line 375) `interceptor-api-generic-method-interceptor-examples` - Generic method API
  - `Of<T>().OnCall`, `Of<T>().CallCount`, `TotalCallCount`, `CalledTypeArguments`

**Samples:** `src/Tests/KnockOff.Documentation.Samples/Reference/InterceptorApiSamples.cs`
**Tests:** `src/Tests/KnockOff.Documentation.Samples.Tests/Reference/InterceptorApiSamplesTests.cs`

#### docs/concepts/customization-patterns.md (4 blocks) ✅

Key conceptual examples showing the duality pattern.

- [x] Block 1 (line 157) `customization-patterns-callback-ko-access` - Accessing `ko` parameter
  - `ko.Initialize.WasCalled`, `ko.Name.Value`
- [x] Block 2 (line 220) `customization-patterns-priority-example-usage` - Priority demonstration
  - User method vs callback, `Reset()` returning to user method
  - Note: Uses `Calculate2` interceptor (suffix due to user method)
- [x] Block 3 (line 249) `customization-patterns-reset-behavior` - Reset semantics
  - `CallCount`, `OnCall`, `Reset()` clearing tracking/callbacks
- [x] Block 4 (line 289) `customization-patterns-combining-patterns-usage` - Combined patterns
  - User method default + callback override
  - Note: Uses `GetById2` interceptor (suffix due to user method)

**Samples:** Extended `src/Tests/KnockOff.Documentation.Samples/Concepts/CustomizationPatternsSamples.cs`
**Tests:** Extended `src/Tests/KnockOff.Documentation.Samples.Tests/Concepts/CustomizationPatternsSamplesTests.cs`

---

### Phase 2: Feature Documentation (Medium Priority)

#### docs/guides/generics.md (7 blocks)

- [ ] Block 10 (line 194) `generic-method-interface` - Interface + stub definition
- [ ] Block 11 (line 209) `generic-method-config` - `Of<T>().OnCall` configuration
- [ ] Block 12 (line 224) `generic-method-tracking` - Per-type and aggregate tracking
- [ ] Block 13 (line 248) `generic-method-multi-param` - Multi-type-parameter interface
- [ ] Block 14 (line 260) `generic-method-multi-usage` - Multi-param `Of<T1,T2>()` usage
- [ ] Block 15 (line 271) `generic-method-constrained` - Constrained generic interface
- [ ] Block 16 (line 280) `generic-method-constrained-usage` - Constrained generic usage

#### docs/guides/events.md (2 blocks)

- [ ] Block 8 (line 176) `viewmodel-event-tests` - ViewModel subscription tests
  - Requires `MyViewModel` class that subscribes to events
- [ ] Block 9 (line 205) `viewmodel-unsubscribe-test` - Dispose/unsubscribe test

#### docs/guides/multiple-interfaces.md (3 blocks)

- [ ] Block 21 (line 32) `separate-standalone-stubs` - Separate stubs with `OnCall`
- [ ] Block 22 (line 54) `inline-stubs-multiple` - Inline stubs test pattern
- [ ] Block 23 (line 101) `migration-from-multiple` - v10 → v10.9 migration

#### docs/guides/inline-stubs.md (2 blocks)

- [ ] Block 19 (line 68) `collision naming pattern` - Generic collision with suffixes
- [ ] Block 20 (line 82) `no collision naming` - Single generic, simple name

#### docs/guides/delegates.md (1 block)

- [ ] Block 7 (line 260) `named-delegate-workaround` - Named delegate pattern

#### docs/guides/properties.md (1 block)

- [ ] Block 24 (line 68) `set-only-property-interface` - Set-only property

#### docs/guides/best-practices.md (2 blocks)

- [ ] Block 5 (line 24) `complex-interface-ok` - Large interface with smart defaults
- [ ] Block 6 (line 240) `reset-clear-backing` - `Reset()` + clearing backing value

---

### Phase 3: Reference Documentation (Medium Priority)

#### docs/reference/attributes.md (5 blocks)

- [ ] Block 48 (line 10) `knockoff-attribute-declaration` - Attribute source
  - Consider syncing from actual `KnockOffAttribute.cs` source file
- [ ] Block 49 (line 23) `knockoff-attribute-usage` - Basic usage
- [ ] Block 50 (line 43) `knockoff-valid-examples` - Multiple valid patterns
- [ ] Block 51 (line 104) `knockoff-namespace-using` - Using statement
- [ ] Block 52 (line 112) `knockoff-namespace-qualified` - Fully qualified usage

#### docs/reference/generated-code.md (4 blocks)

- [ ] Block 56 (line 43) `generated-code-input-example` - Input interface + stub
- [ ] Block 57 (line 61) `generated-code-full-structure` - Full generated structure
  - Consider syncing from actual Generated/ file
- [ ] Block 59 (line 217) `multiple-parameters-tracking` - Tracking structure
- [ ] Block 61 (line 259) `interface-constraint-separate` - Separate stubs

---

### Phase 4: Moq Comparison (Lower Priority)

Requires adding Moq package reference to `KnockOff.Documentation.Samples`.

#### docs/knockoff-vs-moq.md (9 blocks)

- [ ] Block 25 (line 44) `moq-basic-setup` - Mock, Setup, Verify
- [ ] Block 26 (line 85) `moq-property-mocking` - Property setup/verify
- [ ] Block 27 (line 126) `moq-async-methods` - ReturnsAsync
- [ ] Block 28 (line 161) `moq-argument-capture` - Callback capture
- [ ] Block 29 (line 200) `moq-multiple-interfaces` - mock.As<T>()
- [ ] Block 30 (line 244) `moq-indexer-mocking` - Indexer setup
- [ ] Block 31 (line 281) `moq-event-mocking` - Raise, SetupAdd
- [ ] Block 32 (line 329) `moq-verification-patterns` - Times.*
- [ ] Block 33 (line 365) `moq-sequential-returns` - SetupSequence

#### docs/migration-from-moq.md (14 blocks)

- [ ] Block 34 (line 26) `moq-create-mock`
- [ ] Block 35 (line 51) `moq-mock-object`
- [ ] Block 36 (line 71) `moq-setup-returns`
- [ ] Block 37 (line 90) `moq-async-returns`
- [ ] Block 38 (line 109) `moq-verification`
- [ ] Block 39 (line 132) `moq-callback`
- [ ] Block 40 (line 158) `moq-property-setup`
- [ ] Block 41 (line 182) `moq-static-returns`
- [ ] Block 42 (line 211) `moq-conditional-returns`
- [ ] Block 43 (line 235) `moq-throwing-exceptions`
- [ ] Block 44 (line 261) `moq-setup-sequence`
- [ ] Block 45 (line 290) `moq-multiple-interfaces-as`
- [ ] Block 46 (line 330) `moq-argument-matching`
- [ ] Block 47 (line 460) `gradual-migration` - Moq + KnockOff coexistence

---

## Implementation Notes

### Sample File Organization

```
src/Tests/KnockOff.Documentation.Samples/
├── Reference/
│   ├── InterceptorApiSamples.cs      # Blocks 62-67
│   ├── AttributesSamples.cs          # Blocks 48-52
│   └── GeneratedCodeSamples.cs       # Blocks 56-57, 59, 61
├── Concepts/
│   └── CustomizationPatternsSamples.cs  # Blocks 1-4 (extend existing)
├── Guides/
│   ├── GenericsSamples.cs            # Blocks 10-16 (extend existing)
│   ├── EventsSamples.cs              # Blocks 8-9 (extend existing)
│   ├── MultipleInterfacesSamples.cs  # Blocks 21-23
│   ├── InlineStubsSamples.cs         # Blocks 19-20 (extend existing)
│   ├── DelegatesSamples.cs           # Block 7 (extend existing)
│   ├── PropertiesSamples.cs          # Block 24 (extend existing)
│   └── BestPracticesSamples.cs       # Blocks 5-6 (extend existing)
└── Comparison/
    ├── KnockOffVsMoqSamples.cs       # Blocks 25-33 (extend existing)
    └── MigrationFromMoqSamples.cs    # Blocks 34-47 (extend existing)
```

### For Each Block

1. Add interface/stub definitions if needed (outside region)
2. Add usage code inside `#region {snippet-id}` markers
3. Add corresponding test in `*.Tests` project
4. Run `dotnet mdsnippets` to sync
5. Change marker from `<!-- pseudo: -->` to `<!-- snippet: -->`

### Moq Samples

Add to `KnockOff.Documentation.Samples.csproj`:
```xml
<PackageReference Include="Moq" Version="4.*" />
```

---

## Progress Tracking

| Phase | Blocks | Done | Remaining |
|-------|--------|------|-----------|
| 1: Core API | 10 | 10 | 0 |
| 2: Features | 18 | 0 | 18 |
| 3: Reference | 9 | 0 | 9 |
| 4: Moq | 23 | 0 | 23 |
| **Total** | **60** | **10** | **50** |
