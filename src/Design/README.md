# KnockOff Design Projects

This directory contains the authoritative source of truth for KnockOff's API design. These projects serve as living documentation that is always up-to-date because it compiles against the actual generator.

## Purpose

These projects exist to:

1. **Document API Design** - Every public API is demonstrated with working code
2. **Verify Correctness** - Code that doesn't compile reveals API changes immediately
3. **Guide Development** - Claude and developers should reference these files to understand how KnockOff works
4. **Test Coverage** - Design.Tests provides behavioral verification alongside documentation

## Project Structure

```
Design/
  Design.Domain/       # Interfaces and classes to stub (test subjects)
  Design.Stubs/        # Heavily commented stub configurations (documentation)
  Design.Tests/        # Behavioral tests verifying documented behavior
  Design.sln           # Separate solution for design projects
```

## Design.Domain

Contains the interfaces, abstract classes, and delegate types used throughout the design documentation:

- `Services/` - ICalculator, IDataService, IEventSource, IRepository
- `Entities/` - IEntity, ICollection (indexers)
- `Abstractions/` - ServiceBase (abstract class)
- `Delegates/` - ArithmeticOperation, LogAction, SimpleAction, Factory<T>

## Design.Stubs

The heart of the documentation. Each file demonstrates a specific aspect of KnockOff's API with heavy commenting. Comment types include:

- **DESIGN DECISION** - Explains why the API works this way
- **DID NOT DO THIS** - Documents rejected alternatives and why
- **GENERATOR BEHAVIOR** - Shows what code is generated
- **COMMON MISTAKE** - Warns about pitfalls

### File Organization

```
StubPatterns/          # Four stub patterns
  AllPatterns.cs       # All patterns in one file (standalone, inline interface, inline class, inline delegate)

Methods/               # Method stubbing
  BasicMethods.cs      # Returns, OnCall, verification
  MethodSequences.cs   # OnCall().ThenCall()
  WhenMatching.cs      # When() API

Properties/            # Property stubbing
  PropertyBasics.cs    # OnGet, OnSet, LastSetValue
  PropertySequences.cs # ThenGet, ThenSet

Indexers/              # Indexer stubbing
  IndexerBasics.cs     # OnGet(key), OnSet(key, value)
  IndexerSequences.cs  # ThenGet, ThenSet

Events/                # Event stubbing
  EventPatterns.cs     # Handler, VerifyAdd, VerifyRemove

UserMethods/           # User-defined methods
  UserMethodBasics.cs  # Base class pattern with protected override

Advanced/              # Advanced features
  SourceDelegation.cs  # Source() for partial mocking
  StrictMode.cs        # Strict vs loose mode
  Verification.cs      # Verify, Verifiable, VerifyAll
  DelegateStubs.cs     # Delegate stub patterns
```

## Design.Tests

Contains approximately 115 tests verifying the documented behavior actually works:

- **PatternTests/** - Tests for each stub pattern
- **MethodTests/** - Method stubbing behavior
- **PropertyTests/** - Property stubbing behavior
- **IndexerTests/** - Indexer stubbing behavior
- **EventTests/** - Event stubbing behavior
- **AdvancedTests/** - Source delegation, strict mode, verification, delegates

## How to Use

### For Understanding KnockOff

1. Start with `StubPatterns/AllPatterns.cs` to understand the four patterns
2. Read the specific feature file for the capability you need
3. Look for `DESIGN DECISION` comments to understand the "why"
4. Check `DID NOT DO THIS` to understand rejected alternatives

### For Developing KnockOff

1. Before making API changes, check how the feature is documented here
2. After API changes, update the documentation files to match
3. If tests fail, the documentation was accurate - update with care
4. Run `dotnet build` in this directory to verify documentation compiles

### For Claude Code Agents

These files serve as the authoritative reference for how KnockOff works. When asked about KnockOff behavior:

1. Read the relevant Design.Stubs file
2. Trust the code and comments as ground truth
3. If behavior differs from documentation, the documentation should be updated
4. Never guess about API behavior - always verify against these files

## Key Behavioral Notes

### Sequence Behavior

Sequences (Returns().ThenReturns(), OnGet().ThenGet(), etc.) **repeat the last value** after exhaustion. This matches NSubstitute's behavior for easier migration and more forgiving tests.

```csharp
// Default behavior: repeat last value after exhaustion
stub.Add.Returns((a, b) => 1).ThenReturns((a, b) => 2);
calc.Add(0, 0); // Returns 1
calc.Add(0, 0); // Returns 2
calc.Add(0, 0); // Returns 2 (repeats last value)

// Use ThenDefault() to return default(T) instead of repeating
stub.Add.Returns((a, b) => 1).ThenReturns((a, b) => 2).ThenDefault();
calc.Add(0, 0); // Returns 1
calc.Add(0, 0); // Returns 2
calc.Add(0, 0); // Returns 0 (default - exhausted)

// Strict mode throws StubException.SequenceExhausted (unchanged)
stub.Strict = true;
stub.Add.Returns((a, b) => 1).ThenReturns((a, b) => 2);
calc.Add(0, 0); // Returns 1
calc.Add(0, 0); // Returns 2
calc.Add(0, 0); // Throws StubException.SequenceExhausted
```

### Event Invocation

Events are raised via the `Handler` property, not a `Raise()` method:

```csharp
stub.StartedInterceptor.Handler?.Invoke(sender, EventArgs.Empty);
```

### Class Stubs

Class stubs (inline class pattern) do not inherit from the base class. They implement `IKnockOffStub` and expose an `Object` property to access the stubbed interface.

## Running

```bash
# Build
dotnet build

# Test
dotnet test

# Check for compilation errors (documentation still valid)
dotnet build --configuration Release
```
