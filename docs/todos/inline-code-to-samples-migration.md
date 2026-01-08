# Inline Code to Samples Migration Plan

## Summary

**Problem:** 187 out of 266 C# code blocks in documentation are inline (70%) rather than sourced from compiled samples.

**Goal:** Move inline code to `Documentation.Samples/` so it compiles, tests, and stays in sync with documentation.

**Coverage Formula:**
```
Coverage = (snippet-sourced blocks / total ```csharp blocks) × 100
```

---

## Current State

| Category | Blocks | Percentage |
|----------|--------|------------|
| Snippet-sourced (good) | 79 | 30% |
| Inline (needs migration) | 187 | 70% |
| **Total C# blocks** | **266** | 100% |

### Files by Coverage (Low to High)

| File | Coverage | Total | Snippet | Inline | Priority |
|------|----------|-------|---------|--------|----------|
| multiple-interfaces.md | 0% | 4 | 0 | 4 | Medium |
| attributes.md | 0% | 9 | 0 | 9 | Reference |
| generated-code.md | 0% | 6 | 0 | 6 | Reference |
| interceptor-api.md | 0% | 6 | 0 | 6 | Reference |
| events.md | 20% | 15 | 3 | 12 | Medium |
| getting-started.md | 22% | 18 | 4 | 14 | **Critical** |
| indexers.md | 22% | 18 | 4 | 14 | Medium |
| migration-from-moq.md | 23% | 39 | 9 | 30 | **Critical** |
| knockoff-vs-moq.md | 28% | 28 | 8 | 20 | High |
| async-methods.md | 29% | 17 | 5 | 12 | Medium |
| generics.md | 30% | 20 | 6 | 14 | Medium |
| properties.md | 30% | 20 | 6 | 14 | Low |
| interface-inheritance.md | 41% | 12 | 5 | 7 | Low |
| customization-patterns.md | 44% | 9 | 4 | 5 | Low |
| methods.md | 45% | 20 | 9 | 11 | High |
| delegates.md | 58% | 12 | 7 | 5 | Low |
| inline-stubs.md | 69% | 13 | 9 | 4 | Low |

---

## Known Issues

### Orphan Snippets

Snippets in samples with no matching marker in docs:

- [ ] `getting-started.md: multiple-interfaces` - Either add marker to docs or remove region from samples

Run `.\scripts\extract-snippets.ps1 -Verify` to detect orphans.

---

## Can This Code Be Moved to Samples?

### YES - Move to Samples (~160 blocks)

Most inline code CAN and SHOULD move to samples:

1. **Test usage examples** - Show how to set up stubs, verify calls, use callbacks
2. **Complete interface/stub pairs** - Already partially in samples, just missing usage
3. **Comparison examples** - Moq vs KnockOff patterns can be in test methods
4. **Pattern demonstrations** - Reset, isolation, sequential returns

### KEEP INLINE - Moq "Before" Code (~15 blocks)

Migration docs show "before" Moq code. These should stay inline:
- Moq examples in `migration-from-moq.md` showing what to migrate FROM
- KnockOff "after" examples should be snippet-sourced

### EVALUATE - Reference Docs (~21 blocks)

Reference documentation may not need full sample coverage, but consider:

| File | Decision | Rationale |
|------|----------|-----------|
| attributes.md | Consider sourcing | Attribute definitions exist in code |
| generated-code.md | Keep inline | Shows output structure, not runnable |
| interceptor-api.md | Keep inline | API signatures, conceptual patterns |

---

## Migration Strategy

### Phase 1: Critical Path ✅
First impressions and adoption-critical docs.

- [x] **getting-started.md** → 100% coverage
- [x] **migration-from-moq.md** → 64% (max possible with Moq examples inline)

### Phase 2: High Priority ✅

- [x] **knockoff-vs-moq.md** → 68% (max possible with Moq examples inline)
- [x] **methods.md** → 100% coverage

### Phase 3: Medium Priority ✅

- [x] **events.md** → 87% (13/15)
- [x] **async-methods.md** → 100% (16/16)
- [x] **indexers.md** → 86% (12/14)
- [x] **generics.md** → 60% (12/20, generic interfaces done, generic methods inline)
- [x] **multiple-interfaces.md** → 0% (conceptual guide, samples file deleted)

### Phase 4: Low Priority (Already >40% Coverage)

- [x] **properties.md** → 95% (18/19)
- [x] **interface-inheritance.md** → 100% (10/10)
- [x] **customization-patterns.md** → 78% (7/9) - remaining blocks show behavior not implemented
- [x] **delegates.md** → 75% (9/12) - remaining blocks are limitation examples
- [ ] **inline-stubs.md** → in progress (class stubs section remaining)

### Phase 5: Reference Docs (Evaluate)

- [ ] Review attributes.md - can signatures come from source?
- [ ] Review generated-code.md - keep inline (output examples)
- [ ] Review interceptor-api.md - keep inline (API reference)

---

## Implementation Steps (Per File)

For each documentation file:

### 1. Audit inline blocks

```powershell
# Count blocks in a file
$file = "docs/getting-started.md"
$total = (Select-String -Path $file -Pattern '```csharp' | Measure-Object).Count
$snippet = (Select-String -Path $file -Pattern '<!-- snippet:' | Measure-Object).Count
Write-Host "Total: $total, Snippet: $snippet, Inline: $($total - $snippet)"
```

Categorize each inline block:
- Interface/stub definition
- Test usage example
- Callback example
- Conceptual/pseudo-code
- Moq comparison (keep inline)

### 2. Create sample code

Add to appropriate file in `Documentation.Samples/`:

```csharp
#region docs:getting-started:basic-verification
[Fact]
public void NotificationService_SendsEmail_WhenUserRegisters()
{
    // Arrange
    var emailKnockOff = new EmailServiceKnockOff();
    // ... test code
}
#endregion
```

### 3. Add/update tests

Ensure sample code is tested in `Documentation.Samples.Tests/`:

```csharp
[Fact]
public void GettingStarted_BasicVerification_Works()
{
    // Call the sample method to ensure it compiles and runs
    var samples = new GettingStartedSamples();
    samples.NotificationService_SendsEmail_WhenUserRegisters();
}
```

### 4. Update markdown

Replace inline block:

```markdown
<!-- snippet: docs:getting-started:basic-verification -->
```csharp
// Content will be inserted by extract-snippets.ps1
```
<!-- /snippet -->
```

### 5. Verify

```powershell
# Full verification workflow
dotnet build src/KnockOff.sln
dotnet test src/KnockOff.sln
.\scripts\extract-snippets.ps1 -Update   # Sync snippets to docs
.\scripts\extract-snippets.ps1 -Verify   # Confirm in sync
```

---

## Estimated Effort

| Phase | Files | Inline Blocks | Target Coverage |
|-------|-------|---------------|-----------------|
| Phase 1 | 2 | 44 | 90%+ |
| Phase 2 | 2 | 31 | 80%+ |
| Phase 3 | 5 | 56 | 70%+ |
| Phase 4 | 5 | 35 | 80%+ |
| Phase 5 | 3 | 21 | Evaluate |

**Total actionable blocks:** ~166 (excluding Moq "before" examples and reference docs)

---

## Success Criteria

- [ ] `dotnet build src/KnockOff.sln` passes
- [ ] `dotnet test src/KnockOff.sln` passes
- [ ] `.\scripts\extract-snippets.ps1 -Verify` passes with 0 out-of-sync
- [ ] `.\scripts\extract-snippets.ps1 -Verify` reports 0 orphan snippets
- [ ] getting-started.md has 100% snippet coverage
- [ ] All guides (non-reference) have ≥70% snippet coverage
- [ ] migration-from-moq.md KnockOff examples are 100% sourced (Moq examples stay inline)

---

## Verification Commands

```powershell
# Quick check: Are docs in sync?
.\scripts\extract-snippets.ps1 -Verify

# Update docs from samples
.\scripts\extract-snippets.ps1 -Update

# Full pre-commit check
dotnet build src/KnockOff.sln && `
dotnet test src/KnockOff.sln && `
.\scripts\extract-snippets.ps1 -Verify
```

---

## Sample File Organization

```
Documentation.Samples/
├── Comparison/
│   ├── KnockOffVsMoqSamples.cs      # knockoff-vs-moq.md
│   └── MigrationFromMoqSamples.cs   # migration-from-moq.md
├── Concepts/
│   └── CustomizationPatternsSamples.cs
├── GettingStarted/
│   └── GettingStartedSamples.cs
├── Guides/
│   ├── MethodsSamples.cs
│   ├── PropertiesSamples.cs
│   ├── AsyncMethodsSamples.cs
│   ├── EventsSamples.cs
│   ├── GenericsSamples.cs
│   ├── IndexersSamples.cs
│   ├── InlineStubsSamples.cs
│   ├── InterfaceInheritanceSamples.cs
│   ├── MultipleInterfacesSamples.cs
│   └── DelegatesSamples.cs
└── Reference/
    └── (if needed)
```

---

## Naming Convention for Snippet IDs

```
docs:{doc-file-without-extension}:{descriptive-id}

Examples:
docs:methods:void-no-params
docs:getting-started:basic-verification
docs:knockoff-vs-moq:callback-comparison
docs:migration-from-moq:sequential-returns
```

Rules:
- Use kebab-case for IDs
- Keep IDs short but descriptive
- Group related snippets with common prefixes where sensible
