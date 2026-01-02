# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**KnockOff** is a Roslyn Source Generator for creating unit test stubs. Unlike Moq's fluent runtime configuration, KnockOff uses partial classes for compile-time setup—trading flexibility for readability and performance.

Key concept: A class marked with `[KnockOff]` that implements an interface will have:
1. Explicit interface implementations generated for all members
2. `Spy` property for test verification (call counts, args, callbacks)
3. User-defined methods detected and called from generated intercepts

## Project Status

This is a new project. Use RemoteFactory (`c:\src\neatoodotnet\RemoteFactory`) as the reference implementation for structure and patterns.

## Phased Scope

**Phase 1 (MVP)**: Void methods, methods with return values, properties (get/set/get-only/set-only)
**Phase 2**: Async methods, generic interfaces, multiple interfaces, interface inheritance
**Phase 3**: Generic methods, events, indexers, ref/out parameters

## Solution Structure (To Be Created)

Follow RemoteFactory's structure:
```
KnockOff/
├── src/
│   ├── KnockOff/                    # Core library (KnockOff)
│   │   └── KnockOffAttribute.cs     # [KnockOff] marker attribute
│   ├── Generator/                   # Roslyn source generator (netstandard2.0)
│   │   ├── Generator.csproj
│   │   ├── KnockOffGenerator.cs     # Main generator
│   │   ├── EquatableArray.cs        # For incremental generation
│   │   └── HashCode.cs              # For incremental generation
│   └── Tests/
│       ├── KnockOffGeneratorTests/  # Unit tests using "create objects then test them" approach
│       └── KnockOffSandbox/         # Manual testing sandbox
├── .github/workflows/               # CI/CD (GitHub Actions)
└── KnockOff.sln
```

## Build Commands

```bash
# Build solution
dotnet build src/KnockOff.sln

# Run tests
dotnet test src/KnockOff.sln

# Build Release
dotnet build src/KnockOff.sln --configuration Release

# Create NuGet package
dotnet pack src/KnockOff/KnockOff.csproj --configuration Release --output ./artifacts

# Publish to NuGet (via GitHub Actions)
# Push a version tag to trigger automated NuGet publishing:
git tag v10.1.0
git push origin v10.1.0
```

## Source Generator Requirements

### Generator Project Configuration
- **Must target `netstandard2.0`** (Roslyn requirement)
- Enable `EnforceExtendedAnalyzerRules`
- Share attribute files from core library via `<Compile Include="..." Link="..." />`
- Package generator DLL in `analyzers/dotnet/cs/` for NuGet

### Incremental Generator Pattern
1. Use `ForAttributeWithMetadataName` for the predicate
2. Transform must return **serializable/equatable** types (use `EquatableArray<T>`, records, or implement `IEquatable<T>`)
3. If transform data changes → generator re-executes
4. Reference RemoteFactory's `FactoryGenerator.Transform.cs` for pattern

### Test Project Configuration
```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
```
Reference generator as analyzer:
```xml
<ProjectReference Include="..\..\Generator\Generator.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

## Testing Approach

Use "create objects then test them" pattern:
1. Define a test interface and `[KnockOff]` stub class
2. Source generator produces explicit interface implementations
3. Instantiate the stub and verify behavior through the interface

Key files in RemoteFactory to reference:
- `FactoryGeneratorTests/FactoryTestBase.cs` - Base test pattern
- `FactoryGeneratorTests/ClientServerContainers.cs` - DI container setup

## Supported Frameworks

Target the same frameworks as RemoteFactory:
- .NET 8.0 (LTS)
- .NET 9.0 (STS)
- .NET 10.0 (LTS)

Central configuration via `Directory.Build.props` and `Directory.Packages.props`.

## Core Generated Code Pattern

For a `[KnockOff]` class implementing an interface:

1. **Spy property** - Contains Handler classes for each interface member
2. **Handler types**:
   - Method handlers (CallCount, WasCalled, LastCallArg(s), AllCalls, OnCall)
   - Property handlers (GetCount, SetCount, LastSetValue, OnGet, OnSet)
   - Indexer handlers (GetCount, SetCount, LastGetKey, LastSetEntry, OnGet, OnSet)
3. **Protected backing members** - Properties get backing field, allows override
4. **Explicit interface implementations** - Record invocation, delegate to callback/user method/default
5. **User method detection** - If user defines matching protected method, generated code calls it

## Open Design Questions

See README.md "Open Questions" section:
- User method detection rules (protected? exact signature?)
- Naming conflicts for generated members
- Callback support in ExecutionDetails
- Default return values for non-overridden methods

## Implementation Phases

See README.md for detailed checklist. Key phases:
- **Phase 0**: Project setup (solution, Directory.Build.props, projects)
- **Phase 1**: Generator infrastructure (attribute, predicate, transform, equatable models)
- **Phase 2**: ExecutionDetails classes
- **Phase 3**: Property code generation
- **Phase 4**: Method code generation
- **Phase 5**: NuGet packaging
- **Phase 6**: CI/CD
