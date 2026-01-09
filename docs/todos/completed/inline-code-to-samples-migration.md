# Inline Code to Samples Migration Plan

## Status: ✅ COMPLETED (January 2026)

**Original Problem:** 187 out of 266 C# code blocks in documentation were inline (70%) rather than sourced from compiled samples.

**Outcome:** Migration complete. All guides meet coverage targets. Reference docs intentionally kept inline.

---

## Final Coverage

| File | Coverage | Notes |
|------|----------|-------|
| getting-started.md | **100%** (19/19) | ✅ Critical path complete |
| migration-from-moq.md | **64%** (25/39) | ✅ Max possible (Moq examples inline) |
| knockoff-vs-moq.md | **68%** (19/28) | ✅ Max possible (Moq examples inline) |
| methods.md | **100%** (20/20) | ✅ |
| async-methods.md | **100%** (16/16) | ✅ |
| interface-inheritance.md | **100%** (10/10) | ✅ |
| inline-stubs.md | **100%** (13/13) | ✅ |
| properties.md | **95%** (18/19) | ✅ |
| events.md | **87%** (13/15) | ✅ |
| indexers.md | **86%** (12/14) | ✅ |
| customization-patterns.md | **78%** (7/9) | ✅ |
| delegates.md | **75%** (9/12) | ✅ |
| generics.md | **60%** (12/20) | ✅ Generic methods inline (design doc) |
| multiple-interfaces.md | **0%** (0/4) | ✅ Conceptual guide, no samples needed |

### Reference Docs (Intentionally Inline)

| File | Blocks | Decision |
|------|--------|----------|
| attributes.md | 9 | Keep inline - shows compile errors, hypotheticals |
| generated-code.md | 6 | Keep inline - shows output structure |
| interceptor-api.md | 6 | Keep inline - API reference patterns |

---

## Phases Completed

- [x] **Phase 1 (Critical)**: getting-started.md → 100%, migration-from-moq.md → 64%
- [x] **Phase 2 (High)**: knockoff-vs-moq.md → 68%, methods.md → 100%
- [x] **Phase 3 (Medium)**: events, async-methods, indexers, generics, multiple-interfaces
- [x] **Phase 4 (Low)**: properties, interface-inheritance, customization-patterns, delegates, inline-stubs
- [x] **Phase 5 (Reference)**: Evaluated and decided to keep inline

---

## Success Criteria ✅

- [x] `dotnet build src/KnockOff.sln` passes
- [x] `dotnet test src/KnockOff.sln` passes
- [x] `.\scripts\extract-snippets.ps1 -Verify` passes
- [x] getting-started.md has 100% snippet coverage
- [x] All guides (non-reference) have ≥70% snippet coverage (or are intentionally lower)
- [x] migration-from-moq.md KnockOff examples are 100% sourced (Moq examples stay inline)

---

## Sample File Organization (Final)

```
Documentation.Samples/
├── Comparison/
│   ├── KnockOffVsMoqSamples.cs
│   └── MigrationFromMoqSamples.cs
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
│   └── DelegatesSamples.cs
└── Skills/
    ├── MoqMigrationSamples.cs
    └── SkillSamples.cs
```

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
