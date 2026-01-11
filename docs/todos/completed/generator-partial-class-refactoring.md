# Generator Partial Class Refactoring

## Status: Complete

## Summary

Refactored the monolithic `KnockOffGenerator.cs` (8,088 lines) into multiple partial class files and extracted model types into separate files with factory methods.

## Tasks

- [x] Analyze generator structure for splitting
- [x] Extract model records to `Models/` folder with factory methods
- [x] Create `KnockOffGenerator.Transform.cs` partial
- [x] Create `KnockOffGenerator.Helpers.cs` partial
- [x] Create `KnockOffGenerator.GenerateFlat.cs` partial
- [x] Create `KnockOffGenerator.GenerateClass.cs` partial
- [x] Create `KnockOffGenerator.GenerateInline.cs` partial
- [x] Remove extracted code from main generator
- [x] Build and verify (all 3,124 tests pass)

## Before

```
src/Generator/
└── KnockOffGenerator.cs    (~8,088 lines - everything in one file)
```

## After

```
src/Generator/
├── KnockOffGenerator.cs              (2,877 lines) - Main: diagnostics, initialize, standalone generation
├── KnockOffGenerator.Transform.cs    (852 lines)   - Transform/extraction methods
├── KnockOffGenerator.Helpers.cs      (355 lines)   - Shared utility methods
├── KnockOffGenerator.GenerateFlat.cs (1,520 lines) - Flat API generation
├── KnockOffGenerator.GenerateInline.cs (1,823 lines) - Inline stubs generation
├── KnockOffGenerator.GenerateClass.cs (721 lines)  - Class stub generation
└── Models/
    ├── ClassModels.cs       (204 lines) - ClassStubInfo, ClassMemberInfo, ClassConstructorInfo
    ├── CommonModels.cs      (52 lines)  - KnockOffTypeInfo, ContainingTypeInfo, DiagnosticInfo
    ├── EventModels.cs       (66 lines)  - EventMemberInfo, EventDelegateKind
    ├── InlineStubModels.cs  (71 lines)  - InlineStubClassInfo, DelegateInfo, PartialPropertyInfo
    ├── InterfaceModels.cs   (210 lines) - InterfaceInfo, InterfaceMemberInfo, ParameterInfo
    ├── MethodModels.cs      (41 lines)  - UserMethodInfo, MethodGroupInfo, MethodOverloadInfo
    └── SymbolHelpers.cs     (191 lines) - Shared symbol analysis utilities
```

**Total: 8,983 lines** (slight increase due to partial class headers)

## Key Changes

### 1. Model Extraction with Factory Methods

Moved ~20 record types from the bottom of `KnockOffGenerator.cs` to organized model files. Added static factory methods to records that encapsulate creation logic:

- `InterfaceMemberInfo.FromProperty(IPropertySymbol, string)`
- `InterfaceMemberInfo.FromMethod(IMethodSymbol, string)`
- `EventMemberInfo.FromEvent(IEventSymbol, string)`
- `ClassMemberInfo.FromProperty(IPropertySymbol)`
- `ClassMemberInfo.FromMethod(IMethodSymbol)`
- `DelegateInfo.Extract(INamedTypeSymbol)`

### 2. SymbolHelpers Extraction

Created `SymbolHelpers.cs` with shared utilities:
- `FullyQualifiedWithNullability` - Symbol display format
- `GetDefaultValueStrategy()` / `GetDefaultValueStrategyWithConcreteType()`
- `GetCollectionInterfaceMapping()`
- `GetSimpleTypeName()`
- `GetTypeParameterConstraints()`
- `ClassifyDelegateKind()`
- `IsAsyncDelegate()`

### 3. Partial Class Organization

Split by responsibility:
- **Transform.cs**: All `Transform*` methods that extract data from Roslyn symbols
- **Helpers.cs**: Formatting utilities, parameter handling, method grouping
- **GenerateFlat.cs**: Flat API generation (v10.9+ interceptor pattern)
- **GenerateInline.cs**: Inline stubs (`[KnockOff<T>]` pattern)
- **GenerateClass.cs**: Class stub generation via inheritance

## Design Decisions

### Why Partial Classes (Not Full Polymorphism)

Source generators require **equatable models** for incremental generation caching. Polymorphism with virtual methods doesn't work well because:
1. Models must implement `IEquatable<T>` for proper caching
2. Records with inheritance lose value equality semantics
3. The generator's transform phase must produce stable, comparable data

Partial classes maintain the flat, equatable model structure while organizing code logically.

### Why Factory Methods on Records

Moving creation logic to factory methods:
1. **Encapsulation**: Symbol-to-model conversion logic lives with the model
2. **Reduces duplication**: Same extraction logic was repeated in multiple places
3. **Testability**: Factory methods could be unit tested independently
4. **Consistency**: Single source of truth for how models are created

## Results

- Main generator file reduced from 8,088 to 2,877 lines (**64% reduction**)
- Code organized by responsibility
- All 3,124 tests pass
- No changes to generated output or public API
