# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**KnockOff** is a Roslyn Source Generator for creating unit test stubs. Unlike Moq's fluent runtime configuration, KnockOff uses partial classes for compile-time setup—trading flexibility for readability and performance.

Key concept: A class marked with `[KnockOff]` that implements an interface will have:
1. Explicit interface implementations generated for all members
2. Interface-named properties for test verification (call counts, args, callbacks)
3. User-defined methods detected and called from generated interceptors

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

## Generator Principles

1. **Generated code must compile.** If valid code cannot be generated for a scenario, emit a diagnostic error/warning instead of generating broken code.
2. **Fail fast with clear diagnostics.** Users should understand why generation failed and how to fix it.
3. **No silent failures.** Every unsupported scenario should have a corresponding diagnostic.

## Naming Conventions

### Interceptor Terminology

Use **Interceptor** (singular) and **Interceptors** (plural) for generated tracking/callback classes:

| Concept | Name | Example |
|---------|------|---------|
| Per-member class | `{Interface}_{Member}Interceptor` | `IUserService_GetUserInterceptor` |
| Container class | `{Interface}Interceptors` | `IUserServiceInterceptors` |
| Delegate stub property | `Interceptor` | `stub.Interceptor` |

**Do NOT use:**
- `*Intercept` (verb form, not a noun)
- `*Intercepts` (not a valid plural)
- `*Handler` (legacy naming, being phased out)

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

1. **Interface-named property** (e.g., `IUserService`) - Contains Interceptor classes for that interface's members
2. **Interceptor types**:
   - Method interceptors (CallCount, WasCalled, LastCallArg/LastCallArgs, OnCall, Reset)
   - Property interceptors (GetCount, SetCount, LastSetValue, OnGet, OnSet)
   - Indexer interceptors (GetCount, SetCount, LastGetKey, LastSetEntry, OnGet, OnSet)
3. **Protected backing members** - Properties get backing field, allows override
4. **Explicit interface implementations** - Record invocation, delegate to callback/user method/default
5. **User method detection** - If user defines matching protected method, generated code calls it

## Open Design Questions

See README.md "Open Questions" section:
- User method detection rules (protected? exact signature?)
- Naming conflicts for generated members
- Callback support in ExecutionDetails
- Default return values for non-overridden methods

## Documentation Snippets

Code examples in documentation must come from the `KnockOff.Documentation.Samples` project:

```
src/Tests/KnockOff.Documentation.Samples/     # Compiled samples with #region markers
src/Tests/KnockOff.Documentation.Samples.Tests/ # Tests for samples
docs/                                          # Markdown with <!-- snippet: --> markers
scripts/extract-snippets.ps1                   # Sync script
```

### Commands

```powershell
# Verify docs are in sync (for CI)
.\scripts\extract-snippets.ps1 -Verify

# Update docs from samples
.\scripts\extract-snippets.ps1 -Update

# List all snippets
.\scripts\extract-snippets.ps1
```

### Adding Code to Docs

1. Add `#region docs:{doc-file}:{snippet-id}` in `Documentation.Samples`
2. Add `<!-- snippet: docs:{doc-file}:{snippet-id} -->` marker in markdown
3. Run `-Update` to sync

See `/docs-snippets` skill for full documentation.

### Before Committing

**ALWAYS run `/docs-snippets` skill before creating any commit.** This loads the commit checklist:

1. `dotnet build` - Code compiles
2. `dotnet test` - Tests pass
3. `.\scripts\extract-snippets.ps1 -Verify` - Docs in sync
4. If release: version updated, release notes created

## Implementation Phases

See README.md for detailed checklist. Key phases:
- **Phase 0**: Project setup (solution, Directory.Build.props, projects)
- **Phase 1**: Generator infrastructure (attribute, predicate, transform, equatable models)
- **Phase 2**: ExecutionDetails classes
- **Phase 3**: Property code generation
- **Phase 4**: Method code generation
- **Phase 5**: NuGet packaging
- **Phase 6**: CI/CD
