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

## Phase 2-4: Strongly-Typed Handler ✅ COMPLETE

*Refactored: Generate per-member Handler classes with named tuples instead of shared class with object[].*

### 2.1 Per-Member Handler Classes (Generated)
- [x] Generate `{MemberName}Handler` class for each interface member
- [x] Properties: `GetCount`, `SetCount`, `LastSetValue` (strongly typed)
- [x] Methods (0 params): `CallCount`, `WasCalled`
- [x] Methods (1 param): `LastCallArg` (typed), `AllCalls` (typed list)
- [x] Methods (2+ params): `LastCallArgs` (named tuple), `AllCalls` (tuple list)
- [x] All classes include `Reset()` method
- [x] Removed old `src/KnockOff/Handler.cs` - everything is generated

### 3.1 Spy Class Generation
- [x] Generate nested `{ClassName}Spy` class
- [x] Use generated per-member Handler types

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

## Phase 7: Multiple Interfaces ✅ COMPLETE

### 7.1 Multiple Interface Support
- [x] Generate explicit implementations for all interfaces
- [x] Single backing property/method for identical signatures across interfaces
- [x] Behavior: same signature = same backing = same tracking (documented in tests)
- [x] Test: Two interfaces, different methods (MultiInterfaceKnockOff)
- [x] Test: Two interfaces, same method signature (SharedSignatureKnockOff)
- [x] Test: Shared properties across interfaces with different accessors
- [x] Test: AsXYZ() methods work for all interfaces

## Phase 8: Extended Features ✅ COMPLETE

### 8.1 Async Methods
- [x] Handle `Task` return (void async) - returns `Task.CompletedTask`
- [x] Handle `Task<T>` return - returns `Task.FromResult<T>(default!)` or user method
- [x] Handle `ValueTask` return - returns `default` (completed)
- [x] Handle `ValueTask<T>` return - returns `default` or user method
- [x] Proper nullability detection for generic type arguments
- [x] Tests: Task, Task<T>, Task<T?>, ValueTask, ValueTask<T>

### 8.2 Generic Interfaces
- [x] Support `IRepository<T>` style interfaces (works out of the box)
- [x] Concrete type arguments correctly substituted in generated code
- [x] Type constraints respected (where T : class)
- [x] Tests: IRepository<User> with methods and async methods

### 8.3 Interface Inheritance
- [x] Walk full interface hierarchy (uses `AllInterfaces` which includes inherited)
- [x] Generate for inherited members automatically
- [x] Generate AsXYZ() for both derived and base interfaces
- [x] Tests: IAuditableEntity inheriting IBaseEntity

## Phase 9: Callbacks ✅ COMPLETE

### 9.1 Handler Callback Support
- [x] Add `OnCall` delegate property to Handler
- [x] Add `OnGet`/`OnSet` delegate properties for properties
- [x] Signature includes knockoff instance: `Func<TKnockOff, TArgs..., TReturn>`
- [x] Methods with 2+ params use tuple: `Func<TKnockOff, (TArg1, TArg2), TReturn>`
- [x] Generated code checks OnCall/OnGet/OnSet before user method / default behavior
- [x] Reset() clears callback along with tracking state
- [x] Test: Callback invoked with correct args (10 new tests)
- [x] Test: Callback receives knockoff instance
- [x] Test: Callback can access Spy of other members
- [x] Test: OnGet returns callback value
- [x] Test: OnSet intercepts setter
- [x] Test: Async method callbacks work
- [x] Test: Generic interface callbacks work
- [x] Test: Inherited property callbacks work

## Phase 10: Indexer Support ✅ COMPLETE

### 10.1 Indexer Detection and Generation
- [x] Detect indexer properties in interface (`IsIndexer = true`)
- [x] Extract indexer key type and return type
- [x] Generate `StringIndexerHandler` class (named by parameter type)
- [x] Generate `OnGet` callback: `Func<TKnockOff, TKey, TReturn>?`
- [x] Generate `OnSet` callback: `Action<TKnockOff, TKey, TValue>?` for settable indexers
- [x] Generate backing dictionary for default values (`public Dictionary<TKey, TReturn>`)
- [x] Generate explicit indexer implementation with proper fallback chain

### 10.2 Handler for Indexers
- [x] Track `GetCount`, `LastGetKey`, `AllGetKeys` for getter
- [x] Track `SetCount`, `LastSetEntry`, `AllSetEntries` for setter
- [x] `RecordGet(key)` and `RecordSet(key, value)` methods
- [x] `Reset()` clears tracking state and callbacks

### 10.3 Verification (10 new tests)
- [x] Test: Indexer get is tracked (`Indexer_Get_TracksKeyAccessed`)
- [x] Test: Multiple keys tracked (`Indexer_Get_MultipleKeys_TracksAllKeys`)
- [x] Test: OnGet callback works (`Indexer_OnGet_CallbackReturnsValue`)
- [x] Test: OnGet receives knockoff instance (`Indexer_OnGet_CallbackCanAccessKnockOffInstance`)
- [x] Test: Indexer set is tracked (`Indexer_Set_TracksKeyAndValue`)
- [x] Test: Set stores in backing dictionary (`Indexer_Set_StoresInBackingDictionary`)
- [x] Test: OnSet callback intercepts setter (`Indexer_OnSet_CallbackInterceptsSetter`)
- [x] Test: Reset clears indexer state (`Indexer_Reset_ClearsAllState`)
- [x] Test: Nullable return when not found (`Indexer_NullableReturn_ReturnsDefaultWhenNotFound`)

## Phase 11: Verify Helpers (Planned - Convenience)

### 11.1 Fluent Verification Methods
- [ ] Add `VerifyCalledOnce()` to Handler
- [ ] Add `VerifyNeverCalled()` to Handler
- [ ] Add `VerifyCalledTimes(int expected)`
- [ ] Add `VerifyCalledAtLeast(int minimum)`
- [ ] Property-specific: `VerifyGetterCalledOnce()`, `VerifySetterCalledOnce()`

## Phase 12: ReturnsValue Shorthand (Planned - Convenience)

### 12.1 Static Return Value Property
- [ ] Add `ReturnsValue` property to Handler
- [ ] Check ReturnsValue before OnCall in generated code
- [ ] Reset clears ReturnsValue

## Definition of Done

Each phase is complete when:
1. All checklist items are done
2. All tests pass on net8.0, net9.0, net10.0
3. No compiler warnings
4. Generated code compiles without errors
