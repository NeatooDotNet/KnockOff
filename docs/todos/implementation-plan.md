# KnockOff Implementation Plan

## Phase 0: Project Setup ✅ COMPLETE

- [x] Create solution file `src/KnockOff.sln`
- [x] Create `src/Directory.Build.props` with:
  - Target frameworks: net8.0, net9.0, net10.0
  - Central package management enabled
  - Common properties (nullable, implicit usings, lang version)
- [x] Create `src/Directory.Packages.props` with package versions
- [x] Create `src/Generator/Generator.csproj` (netstandard2.0)
- [x] Create `src/KnockOff/KnockOff.csproj` (multi-target)
- [x] Create `src/Tests/KnockOffTests/KnockOffTests.csproj`
- [x] Create `src/Tests/KnockOffSandbox/KnockOffSandbox.csproj`
- [x] Verify solution builds

## Phase 1: Core Generator Infrastructure ✅ COMPLETE

### 1.1 Attribute Definition
- [x] Create `src/KnockOff/KnockOffAttribute.cs`
- [x] Link attribute file in Generator.csproj

### 1.2 Generator Skeleton
- [x] Create `src/Generator/KnockOffGenerator.cs`
- [x] Implement `IIncrementalGenerator`
- [x] Register with `[Generator(LanguageNames.CSharp)]`

### 1.3 Predicate Implementation
- [x] Use `ForAttributeWithMetadataName("KnockOff.KnockOffAttribute", ...)`
- [x] Filter: `ClassDeclarationSyntax` with `partial` modifier
- [x] Filter: Must implement at least one interface
- [x] Filter: Not abstract, not generic (Phase 1)

### 1.4 Transform Model Types
- [x] Create `src/Generator/EquatableArray.cs` (copy from RemoteFactory)
- [x] Create `src/Generator/HashCode.cs` (copy from RemoteFactory)
- [x] Define `KnockOffTypeInfo` record
- [x] Define `InterfaceInfo` record
- [x] Define `InterfaceMemberInfo` record (properties + methods)
- [x] Define `ParameterInfo` record
- [x] Define `UserMethodInfo` record (user-defined overrides)

### 1.5 Transform Implementation
- [x] Extract class namespace and name
- [x] Get all implemented interfaces
- [x] For each interface, extract all members (properties, methods)
- [x] Detect user-defined methods in the partial class
- [x] Return populated `KnockOffTypeInfo`

### 1.6 Verification
- [x] Create test: class with `[KnockOff]` compiles without errors
- [x] Create test: generator produces output file
- [x] Verify generated file appears in `Generated/` folder

## Phase 2-4: Strongly-Typed ExecutionDetails ✅ COMPLETE

*Refactored: Generate per-member ExecutionDetails classes with named tuples instead of shared class with object[].*

### 2.1 Per-Member ExecutionDetails Classes (Generated)
- [x] Generate `{MemberName}ExecutionDetails` class for each interface member
- [x] Properties: `GetCount`, `SetCount`, `LastSetValue` (strongly typed)
- [x] Methods (0 params): `CallCount`, `WasCalled`
- [x] Methods (1 param): `LastCallArg` (typed), `AllCalls` (typed list)
- [x] Methods (2+ params): `LastCallArgs` (named tuple), `AllCalls` (tuple list)
- [x] All classes include `Reset()` method
- [x] Removed old `src/KnockOff/ExecutionDetails.cs` - everything is generated

### 3.1 ExecutionInfo Class Generation
- [x] Generate nested `{ClassName}ExecutionInfo` class
- [x] Use generated per-member ExecutionDetails types

### 3.2 Backing Property Generation
- [x] Generate `protected {Type} {Name}Backing { get; set; }` for each property
- [x] Handle get-only properties
- [x] Default value for non-nullable types (strings get `""`)

### 3.3 Explicit Interface Implementation
- [x] Generate explicit interface implementations for properties
- [x] Record get/set with strongly-typed `RecordSet(T value)`
- [x] Delegate to backing property

### 4.1-4.3 Method Generation
- [x] Void methods: record call, call user method if exists
- [x] Return methods with user method: record call, return user method result
- [x] Nullable returns without user method: return `default!`
- [x] Non-nullable returns without user method: throw `InvalidOperationException`
- [x] Strongly-typed `RecordCall(T1 arg1, T2 arg2, ...)` for all methods

### 4.4 User Method Detection
- [x] Detect protected methods matching interface signatures
- [x] Match by: name, return type, parameter types

### 4.5 AsXYZ() Interface Accessors
- [x] Generate `As{InterfaceName}()` method for each interface
- [x] Strip leading 'I' from interface name (IUserService → AsUserService)
- [x] Returns `this` cast to interface type

### Verification Tests (12 tests)
- [x] Test: Property get is tracked (GetCount)
- [x] Test: Property set is tracked (SetCount, LastSetValue - typed)
- [x] Test: Void method tracked (WasCalled, CallCount)
- [x] Test: Single param method tracks typed arg (LastCallArg)
- [x] Test: Multi-param method tracks named tuple (LastCallArgs)
- [x] Test: AllCalls tracks full history
- [x] Test: Return method calls user method and returns value
- [x] Test: Nullable return returns default when no user method
- [x] Test: AsInterface() returns typed interface
- [x] Test: Reset() clears tracking state
- [x] Test: Tuple destructuring works

## Phase 5: NuGet Packaging ✅ COMPLETE

### 5.1 Generator Packaging
- [x] Configure Generator.csproj:
  - `IncludeBuildOutput=false`
  - `GeneratePackageOnBuild=false`
  - `IsPackable=false`
- [x] Generator embedded in main package

### 5.2 Main Package Configuration
- [x] Configure KnockOff.csproj for NuGet
- [x] Embed Generator.dll in `analyzers/dotnet/cs/`
- [x] Add MIT license (`PackageLicenseExpression`)
- [x] Include README.md and LICENSE files
- [x] Set `DevelopmentDependency=true` (compile-time only package)

### 5.3 Verification
- [x] Create local NuGet package (`KnockOff.10.0.0.nupkg`)
- [x] Create standalone test project (PackageTest)
- [x] Verify generator runs correctly from NuGet package
- [x] All 12 tests pass on net8.0, net9.0, net10.0

## Phase 6: CI/CD ✅ COMPLETE

### 6.1 GitHub Actions Workflow (build.yml)
- [x] Create `.github/workflows/build.yml`
- [x] Trigger on push to main and PRs
- [x] Setup .NET 8.0, 9.0, 10.0 SDKs
- [x] Build and run tests on all target frameworks
- [x] Cache NuGet packages via setup-dotnet
- [x] Upload test results and NuGet package artifacts
- [x] Concurrency control to cancel in-progress runs

### 6.2 NuGet Publishing (publish.yml)
- [x] Create `.github/workflows/publish.yml`
- [x] Trigger on version tags (v*)
- [x] Support manual workflow dispatch
- [x] Support version suffix for pre-releases (e.g., alpha, beta, rc1)
- [x] Extract version from tag for releases
- [x] Run tests before publishing
- [x] Push to NuGet.org with `NUGET_API_KEY` secret

## Phase 7: Multiple Interfaces

### 7.1 Multiple Interface Support
- [ ] Generate explicit implementations for all interfaces
- [ ] Single backing method for identical signatures across interfaces
- [ ] Document behavior: same signature = same backing = same tracking
- [ ] Test: Two interfaces, different methods
- [ ] Test: Two interfaces, same method signature (single backing)

## Phase 8: Extended Features

### 8.1 Async Methods
- [ ] Handle `Task` return (void async)
- [ ] Handle `Task<T>` return
- [ ] Handle `ValueTask<T>` return

### 8.2 Generic Interfaces
- [ ] Support `IRepository<T>` style interfaces
- [ ] Handle type parameter constraints

### 8.3 Interface Inheritance
- [ ] Walk full interface hierarchy
- [ ] Generate for inherited members

## Phase 9: Callbacks

### 9.1 ExecutionDetails Callback Support
- [ ] Add `OnCall` delegate property to ExecutionDetails
- [ ] Signature includes knockoff instance: `Func<TKnockOff, TArgs..., TReturn>`
- [ ] Generated code checks OnCall before user method / default behavior
- [ ] Test: Callback invoked with correct args
- [ ] Test: Callback receives knockoff instance
- [ ] Test: Callback can access ExecutionInfo of other members

## Definition of Done

Each phase is complete when:
1. All checklist items are done
2. All tests pass on net8.0, net9.0, net10.0
3. No compiler warnings
4. Generated code compiles without errors
