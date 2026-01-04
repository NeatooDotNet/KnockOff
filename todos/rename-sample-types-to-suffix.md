# Rename Sample Types to Suffix Convention

## Status: Not Started

## Problem

The `KnockOff.Documentation.Samples` project uses **prefix** naming for sample types to avoid conflicts:
- `IIdxEntityBase` (Idx = indexers)
- `IGenRepository` (Gen = generics)
- `IMigUserService` (Mig = migration)
- `IVsUserService` (Vs = vs-moq comparison)

This is harder to read. The user prefers **suffix** naming:
- `IEntityBaseIdx`
- `IRepositoryGen`
- `IUserServiceMig`
- `IUserServiceVs`

## Scope

Update all sample files in `src/Tests/KnockOff.Documentation.Samples/` to use suffix naming convention.

## Current Prefixes

| Prefix | Meaning | Files |
|--------|---------|-------|
| `Gen` | generics.md samples | GenericsSamples.cs |
| `Mi` | multiple-interfaces.md samples | MultipleInterfacesSamples.cs |
| `Ih` | interface-inheritance.md samples | InterfaceInheritanceSamples.cs |
| `Idx` | indexers.md samples | IndexersSamples.cs |
| `Guide` | guides (events, methods, etc.) | EventsSamples.cs, MethodsSamples.cs, etc. |
| `Mig` | migration-from-moq.md samples | MigrationFromMoqSamples.cs |
| `Vs` | knockoff-vs-moq.md samples | KnockOffVsMoqSamples.cs |
| `Pattern` | customization-patterns.md samples | CustomizationPatternsSamples.cs |
| `Prop` | properties.md samples | PropertiesSamples.cs |
| `Async` | async-methods.md samples | AsyncMethodsSamples.cs |
| `Method` | methods.md samples | MethodsSamples.cs |

## Process

1. For each sample file:
   - [ ] Rename types from prefix to suffix (e.g., `IIdxPropertyStore` → `IPropertyStoreIdx`)
   - [ ] Update KnockOff class names to match
   - [ ] Verify build succeeds

2. After all renames:
   - [ ] Run `.\scripts\extract-snippets.ps1 -Update` to sync docs
   - [ ] Run `.\scripts\extract-snippets.ps1 -Verify` to confirm sync
   - [ ] Build solution to verify no breaks

## Checklist

### Samples Files

- [ ] GenericsSamples.cs
  - `IGenRepository<T>` → `IRepository<T>Gen` (or simpler: just use unique names without prefix/suffix)
  - `IGenCache<TKey, TValue>` → `ICache<TKey, TValue>Gen`
  - etc.

- [ ] MultipleInterfacesSamples.cs
  - `IMiRepository` → `IRepositoryMi`
  - `IMiUnitOfWork` → `IUnitOfWorkMi`
  - etc.

- [ ] InterfaceInheritanceSamples.cs
  - `IIhEntity` → `IEntityIh`
  - `IIhAuditable` → `IAuditableIh`
  - etc.

- [ ] IndexersSamples.cs
  - `IIdxPropertyStore` → `IPropertyStoreIdx`
  - `IIdxEntityBase` → `IEntityBaseIdx`
  - etc.

- [ ] EventsSamples.cs
  - `IGuideEventSource` → `IEventSourceGuide`
  - `IGuideDataService` → `IDataServiceGuide`
  - etc.

- [ ] MethodsSamples.cs
  - `IMethodVoidNoParams` → `IVoidNoParamsMethod`
  - `IMethodUserDefined` → `IUserDefinedMethod`
  - etc.

- [ ] PropertiesSamples.cs
  - `IPropGetSet` → `IGetSetProp`
  - `IPropGetOnly` → `IGetOnlyProp`
  - etc.

- [ ] AsyncMethodsSamples.cs
  - `IAsyncRepository` → `IRepositoryAsync`
  - `IAsyncProcessor` → `IProcessorAsync`
  - etc.

- [ ] MigrationFromMoqSamples.cs
  - `IMigUserService` → `IUserServiceMig`
  - `IMigRepository` → `IRepositoryMig`
  - etc.

- [ ] KnockOffVsMoqSamples.cs
  - `IVsUserService` → `IUserServiceVs`
  - `IVsRepository` → `IRepositoryVs`
  - etc.

- [ ] CustomizationPatternsSamples.cs
  - `IPatternUserService` → `IUserServicePattern`
  - `IPatternRepository` → `IRepositoryPattern`
  - etc.

- [ ] GettingStartedSamples.cs
  - Review if any renames needed

## Alternative Approach

Instead of prefix/suffix, consider using **unique descriptive names** that don't need disambiguation:
- `IEmailService` (getting-started)
- `INotificationService` (events)
- `IOrderRepository` (generics)
- `IEmployeeRepository` (multiple-interfaces)

This would be more natural but requires more thought for each sample.

## Notes

- After renaming, run snippet sync to update all documentation
- The snippet markers in docs won't change (they reference snippet IDs, not type names)
- Only the code content inside the snippet blocks will be updated
