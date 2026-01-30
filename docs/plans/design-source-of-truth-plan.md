# Design Source of Truth - Implementation Plan

**Date:** 2026-01-30
**Related Todo:** [Create Design Source of Truth Projects](../todos/design-source-of-truth.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-01-30
**Reviewed By:** knockoff-architect (2026-01-30)

---

## Overview

Create a set of C# projects in `src/Design/` that serve as the authoritative reference for KnockOff's API design. These projects are specifically designed for Claude Code to understand, reason about, and extend the API.

---

## Approach

Build interconnected projects that demonstrate KnockOff's full API surface:

1. **Design.Domain** - Class library with interfaces and classes to stub
2. **Design.Stubs** - Stub implementations demonstrating all patterns
3. **Design.Tests** - Tests showing usage patterns and API contracts

Each project will be heavily commented with four types of annotations:
- **API documentation** - What this code demonstrates
- **Design rationale** - Why this approach was chosen
- **Rejected alternatives** - What was NOT done and why (often with commented-out code)
- **Generator behavior** - What code the source generator produces

The projects must demonstrate all **four stub patterns** and all **four member types**.

---

## Design

### Directory Structure

```
src/Design/
├── Design.sln
├── README.md                    # Explains purpose for humans
├── CLAUDE-DESIGN.md            # Detailed guidance for Claude Code
├── Design.Domain/
│   ├── Design.Domain.csproj
│   ├── Services/
│   │   ├── ICalculator.cs      # Simple methods
│   │   ├── IRepository.cs      # CRUD methods with generics
│   │   ├── IDataService.cs     # Async methods
│   │   └── IEventSource.cs     # Events
│   ├── Entities/
│   │   ├── IEntity.cs          # Properties and indexers
│   │   └── ICollection.cs      # Indexers
│   └── Abstractions/
│       └── ServiceBase.cs      # Abstract class for class stubs
├── Design.Stubs/
│   ├── Design.Stubs.csproj
│   ├── StubPatterns/
│   │   └── AllPatterns.cs      # Side-by-side comparison of all 4 patterns
│   ├── Methods/
│   │   ├── BasicMethods.cs     # Returns, OnCall, callbacks
│   │   ├── MethodSequences.cs  # OnCall().ThenCall() chains
│   │   └── WhenMatching.cs     # When() API comprehensive
│   ├── Properties/
│   │   ├── PropertyBasics.cs   # OnGet, OnSet, Value
│   │   └── PropertySequences.cs # OnGet().ThenGet() chains
│   ├── Indexers/
│   │   ├── IndexerBasics.cs    # OnGet, OnSet, Backing
│   │   └── IndexerSequences.cs # Sequences for indexers
│   ├── Events/
│   │   └── EventPatterns.cs    # Raise, VerifyAdd, VerifyRemove
│   ├── Delegates/
│   │   └── DelegatePatterns.cs # Inline delegate stubs
│   ├── Verification/
│   │   └── VerificationPatterns.cs # Verifiable, Verify, Times
│   └── Advanced/
│       ├── SourceDelegation.cs # Source() pattern
│       └── StrictMode.cs       # Strict behavior
└── Design.Tests/
    ├── Design.Tests.csproj
    ├── PatternTests/
    │   ├── StandalonePatternTests.cs
    │   ├── InlineInterfacePatternTests.cs
    │   ├── InlineClassPatternTests.cs
    │   └── InlineDelegatePatternTests.cs
    ├── MemberTests/
    │   ├── MethodTests.cs
    │   ├── PropertyTests.cs
    │   ├── IndexerTests.cs
    │   └── EventTests.cs
    └── FeatureTests/
        ├── WhenApiTests.cs
        ├── SequenceTests.cs
        └── VerificationTests.cs
```

### Comment Standards

#### API Documentation Comments

```csharp
/// <summary>
/// Demonstrates: OnCall() for method callbacks with argument access.
///
/// Key points:
/// - Arguments are typed and named in the delegate signature
/// - Returns value from callback, not from .Returns()
/// - OnCall() inherently matches any arguments (no Arg.Any needed)
/// - Async methods: callback returns Task<T>, but Returns() auto-wraps with Task.FromResult
/// </summary>
```

#### Design Rationale Comments

```csharp
// DESIGN DECISION: We use separate Returns() and OnCall() APIs.
// - Returns(value) is for simple constant returns
// - OnCall(callback) is for dynamic returns based on arguments
//
// OnCall receives typed arguments directly: (a, b) => a + b
// This avoids NSubstitute's callInfo.Arg<T>() pattern.
//
// See: src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs for implementation
```

#### Rejected Alternative Comments

```csharp
// DID NOT DO THIS: Single .Setup() method like Moq
//
// Reasons:
// 1. Moq's Setup requires lambda expression analysis at runtime
// 2. Source generators work at compile time - can't analyze lambdas
// 3. Our approach: explicit member access (stub.Method.OnCall)
//
// REJECTED PATTERN:
// stub.Setup(x => x.Method(It.IsAny<int>())).Returns(42);
//
// ACTUAL PATTERN:
// stub.Method.OnCall((arg) => 42);
// // or for constant:
// stub.Method.Returns(42);
```

#### Generator Behavior Comments

```csharp
// GENERATOR BEHAVIOR: For this interface method:
//
// interface ICalculator { int Add(int a, int b); }
//
// The generator produces:
//
// 1. Interceptor class with When chain support:
//    public class AddInterceptor : MethodInterceptor<int, (int a, int b), int>
//    {
//        public AddWhenBuilder When(int a, int b) { ... }
//        public AddWhenBuilder When(Func<int, int, bool> predicate) { ... }
//    }
//
// 2. When builder/chain classes (per-method because ThenWhen needs parameter types):
//    public class AddWhenBuilder : IWhenBuilder<Func<int, int, int>, int> { ... }
//    public class AddWhenChain : IWhenChain<Func<int, int, int>, int> { ... }
//
// 3. Property on stub:
//    public AddInterceptor Add { get; }
//
// 4. Interface implementation:
//    int ICalculator.Add(int a, int b) => Add.Call((a, b));
```

#### Common Mistake Comments

```csharp
// COMMON MISTAKE: Using Returns() then ThenCall() for sequences
//
// WRONG:
// stub.Method.Returns(1).ThenCall((a) => 2);  // Returns() nulls callback reference
//
// RIGHT:
// stub.Method.OnCall((a) => 1).ThenCall((a) => 2);
//
// Why: Returns(value) sets _callback to null internally. ThenCall() relies on
// the callback reference from OnCall(). Use OnCall() for all sequence setups.
```

#### Pattern Comparison Comments

```csharp
// PATTERN COMPARISON: Four ways to create stubs
//
// 1. STANDALONE - Full control, explicit class
//    [KnockOff]
//    public partial class CalculatorStub : ICalculator { }
//    var stub = new CalculatorStub();
//
// 2. INLINE INTERFACE - Nested in test class, no separate file
//    [KnockOff<ICalculator>]
//    public partial class CalculatorTests { }
//    var stub = new Stubs.ICalculator();
//
// 3. INLINE CLASS - For virtual/abstract members of classes
//    [KnockOff<CalculatorBase>]
//    public partial class CalculatorTests { }
//    var stub = new Stubs.CalculatorBase();
//    CalculatorBase instance = stub.Object;  // .Object exposes the wrapped instance
//
// 4. INLINE DELEGATE - For delegate types
//    [KnockOff<Func<int, int, int>>]
//    public partial class CalculatorTests { }
//    var stub = new Stubs.FuncInt32Int32Int32();
//    Func<int, int, int> func = stub;  // implicit conversion
//    stub.Interceptor.OnCall((a, b) => a + b);  // .Interceptor for configuration
```

#### Priority Order Comments

```csharp
// PRIORITY ORDER: When a method is called, KnockOff checks in this order:
//
// 1. When chains - Parameter-specific matching (highest priority)
//    stub.Method.When(1, 2).Returns(100);
//
// 2. Sequences - If OnCall().ThenCall() was used and not exhausted
//    stub.Method.OnCall(() => 1).ThenCall(() => 2);
//
// 3. Returns - Simple constant return value
//    stub.Method.Returns(42);
//
// 4. OnCall - Callback invocation (mutually exclusive with Returns)
//    stub.Method.OnCall((a, b) => a + b);
//
// 5. Source - Delegation to real implementation
//    stub.Source(realImplementation);
//
// 6. Smart Default - default(T) for value types, null for references
//    (or StubException in strict mode)
```

### API Coverage Checklist

The design projects must demonstrate ALL of these (verified against codebase 2026-01-30):

**Four Stub Patterns:**
- [ ] Standalone - `[KnockOff]` on partial class implementing interface
- [ ] Inline Interface - `[KnockOff<IInterface>]` generates nested stub
- [ ] Inline Class - `[KnockOff<ConcreteClass>]` stubs virtual/abstract members (`.Object` property)
- [ ] Inline Delegate - `[KnockOff<DelegateType>]` stubs delegate invocation (`.Interceptor` property)
- [ ] Open Generic - `[KnockOff(typeof(IInterface<>))]` for open generics via typeof syntax

**Four Member Types:**
- [ ] Methods - void and return, sync and async
- [ ] Properties - get-only, set-only, get/set, init-only
- [ ] Indexers - single and multi-key (get-only, get/set)
- [ ] Events - EventHandler, EventHandler<T>, Action, Action<T1,T2,...>

**Method APIs (IMethodTracking, IMethodCallBuilder, IMethodSequence):**
- [ ] `Returns(value)` - simple return value (sets callback to null)
- [ ] `OnCall(callback)` - callback with argument access
- [ ] `OnCall().ThenCall()` - sequence chains (lazy elevation to sequence mode)
- [ ] `When(args).Returns(value)` - parameter matching with value equality
- [ ] `When(predicate).Returns(value)` - predicate matching
- [ ] `ThenWhen(args).Returns()` / `ThenWhen(predicate).Returns()` - chained matchers
- [ ] `ThenCall(callback)` - terminal callback (repeats forever)
- [ ] `ThenNone()` - exhaust and fall through to next priority
- [ ] `Verify()` / `Verify(Times)` - call count verification
- [ ] `Verifiable()` / `Verifiable(Times)` - batch verification marker
- [ ] `Reset()` - clear tracking state (LastArg/LastArgs = default, call count = 0)
- [ ] `LastArg` / `LastArgs` - argument capture (single vs. tuple)
- [ ] Async auto-wrapping - `Returns(value)` auto-wraps with Task.FromResult for async methods

**Void Method APIs (IVoidWhenChain):**
- [ ] `When(args)` - returns IVoidWhenChain (no builder needed)
- [ ] `When(predicate)` - predicate matching
- [ ] `.Call(callback)` - optional callback for matched void method
- [ ] `ThenWhen(args).Call()` / `ThenWhen(predicate).Call()` - chained matchers
- [ ] `ThenCall(callback)` - terminal callback
- [ ] `ThenNone()` - exhaust chain
- [ ] `Verify(Times)` - verify specific matcher was called

**Property APIs (IPropertyGetBuilder, IPropertySetBuilder, IPropertyGetSequence, IPropertySetSequence):**
- [ ] `OnGet(value)` - getter return value
- [ ] `OnGet(callback)` - dynamic getter
- [ ] `OnGet().ThenGet(callback)` / `ThenGet(value)` - getter sequences
- [ ] `OnSet(callback)` - setter callback
- [ ] `OnSet().ThenSet(callback)` - setter sequences
- [ ] `Value` - backing store for get/set properties
- [ ] `VerifyGet()` / `VerifyGet(Times)` - getter verification
- [ ] `VerifySet()` / `VerifySet(Times)` - setter verification
- [ ] `LastValue` - capture last set value (IPropertySetTracking)
- [ ] `Verifiable()` - batch verification marker
- [ ] `Reset()` - clear tracking

**Indexer APIs (IIndexerGetBuilder, IIndexerSetBuilder, IIndexerGetSequence, IIndexerSetSequence):**
- [ ] `OnGet(callback)` - getter with key access (Func<TKey, TValue>)
- [ ] `OnSet(callback)` - setter with key and value (Action<TKey, TValue>)
- [ ] `OnGet().ThenGet(callback)` - getter sequences
- [ ] `OnSet().ThenSet(callback)` - setter sequences
- [ ] `Backing` - Dictionary<TKey, TValue> for storage
- [ ] `VerifyGet()` / `VerifyGet(Times)` - getter verification
- [ ] `VerifySet()` / `VerifySet(Times)` - setter verification
- [ ] `LastGetKey` - capture last get key (generated property; interface uses `LastKey`)
- [ ] `LastSetEntry` - capture last (Key, Value) tuple (generated property; interface uses `LastEntry`)
- [ ] `Verifiable()` - batch verification marker
- [ ] `Reset()` - clear tracking but preserve configuration

> **Note on Indexer Tracking Names:** The API checklist shows *generated property names* (`LastGetKey`, `LastSetEntry`) which is what users interact with in code. The underlying interface types (`IIndexerGetTracking`, `IIndexerSetTracking`) use shorter names (`LastKey`, `LastEntry`). The generator adds the `Get`/`Set` prefix to disambiguate when both exist on the same interceptor.

**Event APIs:**
- [ ] `Raise(sender, args)` - fire EventHandler<T> events
- [ ] `Raise(args)` - fire EventArgs events
- [ ] `Raise(arg1, arg2, ...)` - fire Action<T1, T2, ...> events
- [ ] `VerifyAdd()` / `VerifyAdd(Times)` - subscription verification
- [ ] `VerifyRemove()` / `VerifyRemove(Times)` - unsubscription verification
- [ ] `HasSubscribers` - check for active handlers
- [ ] `Reset()` - clear handlers and tracking

**Advanced Features:**
- [ ] `Source(realImpl)` - delegate to real object (priority: OnCall > Source > SmartDefault)
- [ ] `Source(null)` - clear source delegation
- [ ] `Strict = true` - throw StubException on unconfigured calls (instance property)
- [ ] `[KnockOff(Strict = true)]` - attribute-level strict default
- [ ] `[assembly: KnockOffStrict]` - assembly-level strict default
- [ ] `.Strict()` extension method - fluent strict mode
- [ ] `stub.Verify()` - batch verify all Verifiable() marked items
- [ ] `stub.VerifyAll()` - verify all configured items were called at least once
- [ ] Constructor `strict` parameter - inline stubs support `new Stubs.IService(strict: true)`

**Priority Order (When > Sequence > Returns > OnCall > Source > SmartDefault):**
- [ ] Document priority order with examples
- [ ] Show how When chains interact with OnCall/Returns fallback

**Verification (Times struct):**
- [ ] `Times.Once` - exactly one call
- [ ] `Times.Twice` - exactly two calls
- [ ] `Times.Never` - no calls expected
- [ ] `Times.Exactly(n)` - exact count
- [ ] `Times.AtLeastOnce` - one or more calls
- [ ] `Times.AtLeast(n)` - minimum count
- [ ] `Times.AtMost(n)` - maximum count

**IKnockOffStub interface:**
- [ ] `Strict` property - get/set strict mode at runtime

**Partial Properties (Inline stubs):**
- [ ] `protected partial Stubs.IService service { get; }` - auto-instantiation pattern

### Evolution Strategy

When the API changes:

1. **Update Design.* projects first** - This is the source of truth
2. **Add "was/now" comments** for changed behavior:
   ```csharp
   // CHANGED in v10.x: Previously sequences repeated last value.
   // Now sequences exhaust and return default(T) in non-strict mode.
   //
   // OLD (v9.x):
   // stub.Method.OnCall(() => 1).ThenCall(() => 2);
   // // Third call returned 2 (repeated)
   //
   // NEW (v10.x+):
   // stub.Method.OnCall(() => 1).ThenCall(() => 2);
   // // Third call returns default(int) = 0
   ```
3. **Update main codebase** to implement the change
4. **Update skills/knockoff** to reflect new patterns
5. **Update user documentation** last

### Who Updates Design Projects

| Who | When |
|-----|------|
| **knockoff-architect** | When designing new features - updates Design.* first |
| **knockoff-developer** | When implementing - ensures Design.* matches implementation |
| **Before any PR that changes public API** | Design.* must be updated |

### Validation Requirements

- Tests in Design.Tests must pass
- All `DESIGN DECISION` comments must remain accurate
- No commented-out code should be stale (outdated rejected patterns)
- `GENERATOR BEHAVIOR` comments must match actual generator output

**GENERATOR BEHAVIOR Comment Verification Process:**

Since automated comment parsing is out of scope for v1, GENERATOR BEHAVIOR comments are verified through:

1. **During Implementation:** When writing a GENERATOR BEHAVIOR comment, the developer must:
   - Build the project to generate code
   - Open the corresponding `.g.cs` file in `Generated/` folder
   - Manually verify the comment matches the generated output
   - If discrepancy found, update the comment to match reality

2. **During PR Review:** Reviewers should spot-check GENERATOR BEHAVIOR comments against generated code

3. **Indirect Verification:** Tests that exercise the documented patterns provide confidence:
   - If the test compiles and passes, the documented pattern is at least valid
   - Broken documentation would likely cause test failures

4. **Future Consideration:** Automated verification (parsing comments, comparing to AST) could be added in a future version if manual review proves insufficient

---

## Implementation Steps

### Phase 1: Foundation (BLOCKING)

1. Create `src/Design/Design.sln` solution
2. Create `Design.Domain` project with interfaces to stub
3. Create `Design.Stubs` project with stub definitions
4. Create `Design.Tests` project
5. Add project reference to KnockOff source (for generator)
6. Verify the solution builds

**Dependencies:** None. Must complete before any other phase.

### Phase 2: Pattern Documentation (depends on Phase 1)

7. Create `StubPatterns/AllPatterns.cs` showing all four patterns side-by-side
8. Add extensive comments explaining when to use each pattern
9. Document what the generator produces for each pattern
10. Add "DID NOT DO THIS" comments for rejected pattern alternatives

**Dependencies:** Phase 1 (projects must exist)

### Phase 3: Member Type Coverage - Methods (depends on Phase 1)

11. Implement `Methods/BasicMethods.cs` - Returns, OnCall, callbacks
12. Implement `Methods/MethodSequences.cs` - OnCall().ThenCall() chains
13. Implement `Methods/WhenMatching.cs` - When() API comprehensive
14. Add "GENERATOR BEHAVIOR" comments showing generated interceptor code
15. Add "COMMON MISTAKE" comments for Returns()+ThenCall() error

**Dependencies:** Phase 1 (projects must exist)

### Phase 4: Member Type Coverage - Properties, Indexers, Events (depends on Phase 1)

16. Implement `Properties/PropertyBasics.cs` - OnGet, OnSet, Value
17. Implement `Properties/PropertySequences.cs` - sequences
18. Implement `Indexers/IndexerBasics.cs` - OnGet, OnSet, Backing
19. Implement `Indexers/IndexerSequences.cs` - sequences
20. Implement `Events/EventPatterns.cs` - Raise, VerifyAdd, VerifyRemove

**Dependencies:** Phase 1 (projects must exist)

### Phase 5: Advanced Features (depends on Phase 1)

21. Implement `Advanced/SourceDelegation.cs` - Source() pattern
22. Implement `Advanced/StrictMode.cs` - strict behavior
23. Implement `Verification/VerificationPatterns.cs` - all Times patterns
24. Implement `Delegates/DelegatePatterns.cs` - inline delegate stubs

**Dependencies:** Phase 1 (projects must exist)

### Phase 6: Testing (depends on Phases 2-5)

25. Create tests for each stub pattern
26. Create tests for each member type
27. Create tests for advanced features
28. Ensure all tests pass on all target frameworks

**Dependencies:** Phases 2-5 (cannot write tests for code that doesn't exist)

### Phase 7: Documentation (can overlap with Phase 6)

29. Create `README.md` explaining the purpose
30. Create `CLAUDE-DESIGN.md` with Claude-specific guidance
31. Update main `CLAUDE.md` to reference design projects as source of truth

**Dependencies:** Phase 2 minimum (need patterns documented to write CLAUDE-DESIGN.md)

### Phase Dependency Diagram

```
Phase 1 (Foundation) - BLOCKING
    |
    +---> Phase 2 (Patterns) ----+
    |                            |
    +---> Phase 3 (Methods) -----+---> Phase 6 (Testing)
    |                            |         |
    +---> Phase 4 (Props/etc) ---+         |
    |                            |         v
    +---> Phase 5 (Advanced) ----+    Phase 7 (Docs)
                                      (can start after Phase 2)
```

**Parallelization:** After Phase 1 completes, Phases 2-5 can be implemented in any order or in parallel. Phase 6 requires all of 2-5. Phase 7 can begin once Phase 2 is complete and overlap with Phase 6.

---

## Acceptance Criteria

- [ ] All projects compile without errors
- [ ] All tests pass
- [ ] All four stub patterns demonstrated
- [ ] All four member types demonstrated
- [ ] Every public API element from checklist is demonstrated with comments
- [ ] At least 10 "DID NOT DO THIS BECAUSE" comments
- [ ] At least 10 "DESIGN DECISION" comments
- [ ] At least 5 "GENERATOR BEHAVIOR" comments
- [ ] At least 5 "COMMON MISTAKE" comments
- [ ] CLAUDE.md updated to reference src/Design as source of truth

---

## Out of Scope

**Explicitly NOT included in Design projects:**

- Complex real-world scenarios (keep examples focused on API)
- Integration with test frameworks beyond xUnit
- Performance benchmarks
- Production-ready error handling

**Differentiation from skills/knockoff:**

| Aspect | skills/knockoff | Design.* |
|--------|-----------------|----------|
| Purpose | User learning | AI comprehension |
| Complexity | Progressive disclosure | Minimal viable demonstrations |
| Comments | Teaching-focused | Design rationale + rejected alternatives |
| Rejected alternatives | None shown | Multiple per file |
| Target audience | Developers learning | Claude Code understanding API |

---

## Dependencies

- KnockOff source projects (via project references)
- .NET 8.0/9.0/10.0 SDK
- xUnit for tests

---

## KnockOff-Specific Implementation Notes

### Generator Pipeline Understanding

Design projects must reference the generator pipeline when explaining generated code:

```
[KnockOff] Attribute Detection
          |
          v
    +-----------+
    | Predicate | - Syntax-level filtering
    +-----------+    (IsCandidateClass, HasTypeofArgument)
          |
          v
    +-----------+
    | Transform | - Roslyn symbols -> equatable models
    +-----------+    (KnockOffTypeInfo -> FlatGenerationUnit/InlineGenerationUnit)
          |
          v
    +---------+
    | Builder | - Models -> generation units
    +---------+    (FlatModelBuilder, InlineModelBuilder)
          |
          v
    +----------+
    | Renderer | - Generation units -> C# source
    +----------+    (FlatRenderer, InlineRenderer, ClassRenderer)
```

When documenting GENERATOR BEHAVIOR, reference the appropriate pipeline stage.

### Method Interceptor Architecture

Each method generates:
1. **Interceptor class** - Holds callback, sequence, When chain
2. **When builder** - Per-method because it needs parameter types for ThenWhen
3. **When chain** - Returned by When().Returns(), enables ThenWhen chaining

The When chain architecture requires method-specific types because:
- ThenWhen(arg1, arg2, ...) needs the exact parameter types
- ThenWhen(predicate) needs Func<T1, T2, ..., bool>
- Generic interfaces like IWhenChain<TDelegate, TReturn> don't expose ThenWhen

### Indexer Container Pattern

Indexers use `IndexerContainer<TKey, TValue>` which holds:
- Backing dictionary
- OnGet/OnSet callbacks
- Sequence state
- Tracking (LastGetKey, LastSetEntry)

Multi-key indexers use tuple keys: `IndexerContainer<(TKey1, TKey2), TValue>`

### Class Stub .Object Pattern

Inline class stubs differ from interface stubs:
- `new Stubs.MyClass()` returns the stub wrapper
- `stub.Object` returns the generated class instance
- The generated class extends the base class
- Unconfigured virtual methods call base implementation (not smart default)

This is different from Moq where `mock.Object` returns the proxy for both interfaces and classes.

---

## Risks / Considerations

1. **Maintenance burden** - These projects must be updated whenever the API changes. Mitigated by making it the first step in the design workflow.

2. **Scope creep** - Temptation to add too much. Keep focused on API demonstration, not comprehensive usage examples.

3. **Comment rot** - Old comments becoming inaccurate. Mitigated by:
   - Making Design projects the source of truth that flows to everything else
   - Tests that verify GENERATOR BEHAVIOR comments against actual output
   - Requiring update of Design projects before any API-changing PR

4. **API discoverability vs. completeness** - Risk of documenting internal APIs that shouldn't be used. Mitigated by:
   - Only documenting public interface types (IMethodTracking, etc.)
   - Marking internal implementation details as such
   - Focusing on user-facing APIs

5. **Pattern divergence** - Four patterns could accidentally demonstrate different subsets of API. Mitigated by:
   - API coverage checklist requiring all patterns
   - Tests that verify pattern parity where applicable

---

## Architectural Verification

**Reviewed By:** knockoff-architect (2026-01-30)

### Four Patterns Analysis

| Pattern | Covered | Special Considerations |
|---------|---------|------------------------|
| **Standalone** | Yes | Base case, most comprehensive API surface |
| **Inline Interface** | Yes | Nested `Stubs.IInterfaceName` class |
| **Inline Class** | Yes | Requires `.Object` property to access wrapped instance; base class fallback behavior |
| **Inline Delegate** | Yes | Uses `.Interceptor` property; implicit conversion to delegate type |

**All four patterns must be demonstrated side-by-side** in `StubPatterns/AllPatterns.cs` with clear comments explaining:
- When to use each pattern
- What gets generated for each
- How configuration differs (e.g., `.Object` vs direct interface cast)

### Four Member Types Analysis

| Member Type | All Patterns? | Special Handling |
|-------------|---------------|------------------|
| **Methods** | Yes | Void vs. return, sync vs. async, generic methods, overloads |
| **Properties** | Yes | get-only, set-only, get/set, init-only |
| **Indexers** | Yes | Single key vs. multi-key; inline class inherits from base |
| **Events** | Yes | EventHandler, EventHandler<T>, Action<T...> variants |

### Breaking Changes Assessment

**Breaking Changes:** No - this is purely additive infrastructure.

The Design projects are new and do not modify any existing public API. They:
- Create a new solution file (`src/Design/Design.sln`)
- Create new projects that reference existing KnockOff source
- Do not change generator output, models, builders, or renderers
- Do not affect existing tests or user code

### Pattern Consistency Check

| Aspect | Consistent? | Notes |
|--------|-------------|-------|
| Project organization | Yes | Follows existing `src/` structure |
| Project references | Yes | Uses `<ProjectReference>` to KnockOff source like Tests projects |
| Generator integration | Yes | Uses `<ProjectReference Include="...Generator.csproj" OutputItemType="Analyzer">` |
| Test framework | Yes | Uses xUnit like existing tests |
| Multi-targeting | Yes | Will target same frameworks as main library |

### Diagnostic Requirements

No new diagnostics required. Design projects use existing KnockOff diagnostics.

### Codebase Analysis

**Files Examined:**

Generator Structure:
- `src/Generator/KnockOffGenerator.cs` - Entry point, pipeline setup
- `src/Generator/KnockOffGenerator.Transform.cs` - Roslyn symbols to models
- `src/Generator/Builder/FlatModelBuilder.cs` - Standalone generation units
- `src/Generator/Builder/InlineModelBuilder.cs` - Inline generation units
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Method interceptor generation
- `src/Generator/Renderer/Shared/WhenChainRenderer.cs` - When chain generation

Public API (KnockOff library):
- `src/KnockOff/KnockOffAttribute.cs` - `[KnockOff]`, `[KnockOff<T>]`, Strict property
- `src/KnockOff/KnockOffStrictAttribute.cs` - `[assembly: KnockOffStrict]`
- `src/KnockOff/Times.cs` - Verification constraints (Once, Twice, Exactly, AtLeast, AtMost, Never)
- `src/KnockOff/IMethodCallBuilder.cs` - OnCall builder/tracking interfaces
- `src/KnockOff/IMethodSequence.cs` - Sequence interfaces (ThenCall)
- `src/KnockOff/IMethodTracking.cs` - Tracking interfaces (Verify, Verifiable, Reset, LastArg/LastArgs)
- `src/KnockOff/IPropertyCallBuilder.cs` - Property builder interfaces (OnGet, OnSet, ThenGet, ThenSet)
- `src/KnockOff/IPropertySequence.cs` - Property sequence interfaces
- `src/KnockOff/IPropertyTracking.cs` - Property tracking (LastValue)
- `src/KnockOff/IIndexerCallBuilder.cs` - Indexer builder interfaces
- `src/KnockOff/IIndexerSequence.cs` - Indexer sequence interfaces
- `src/KnockOff/IIndexerTracking.cs` - Indexer tracking (LastGetKey, LastSetEntry)
- `src/KnockOff/IWhenTracking.cs` - When chain interfaces (IWhenBuilder, IWhenChain, IVoidWhenChain)
- `src/KnockOff/IKnockOffStub.cs` - Marker interface with Strict property
- `src/KnockOff/StubExtensions.cs` - `.Strict()` extension method

Test Files (for API usage patterns):
- `src/Tests/KnockOffTests/WhenChainTests.cs` - Comprehensive When API tests across all 4 patterns
- `src/Tests/KnockOffTests/EventTests.cs` - Event API tests (Raise, VerifyAdd, HasSubscribers)
- `src/Tests/KnockOffTests/IndexerTests.cs` - Indexer API tests (Backing, LastGetKey, LastSetEntry)
- `src/Tests/KnockOffTests/SequencingTests.cs` - Sequence API tests (OnCall().ThenCall())
- `src/Tests/KnockOffTests/VerificationTests.cs` - Verification tests (Verify, Verifiable, Times)
- `src/Tests/KnockOff.Documentation.Samples/SourceDelegationSamples.cs` - Source() delegation pattern

**Key Patterns Discovered:**

1. **Builder Elevation Pattern**: OnCall() returns IMethodCallBuilder which can elevate to IMethodSequence via ThenCall(). This lazy elevation is critical to document.

2. **When Chain Architecture**: Each method gets its own WhenBuilder and WhenChain classes because ThenWhen() needs access to method-specific parameter types. This is why When chains aren't a simple generic.

3. **Priority System**: The call resolution order (When > Sequence > Returns > OnCall > Source > SmartDefault) is fundamental but undocumented. Design projects must make this explicit.

4. **Inline Class .Object Pattern**: Inline class stubs wrap the generated class and expose it via `.Object`. This differs from interface stubs where the stub IS the implementation.

5. **Async Auto-Wrapping**: Both Returns() and When().Returns() auto-wrap values with Task.FromResult for async methods, avoiding boilerplate.

### Test Strategy

Design.Tests will include:

**1. Compilation Tests (1 test)**
- Single test verifying all Design projects compile without errors (implicit: projects must build to run any tests)

**2. Pattern Parity Tests (~20 tests)**
- For each API element that applies to multiple patterns, verify identical behavior across all applicable patterns
- Example: `Returns(42)` tested for Standalone, Inline Interface, Inline Class (3 tests for this API element)
- Definition of "parity": Same test assertions, different stub instantiation
- Not all APIs apply to all patterns (e.g., `.Object` is Inline Class only)

**3. API Coverage Tests (~50 tests minimum)**
- At least one test per major API element in the checklist
- Group related APIs: e.g., `Times.Once`, `Times.Twice`, `Times.Never` can share a single test
- Focus on demonstrating correct usage, not exhaustive edge cases (that's what KnockOffTests is for)

**4. Comment Accuracy Verification (Manual)**
- GENERATOR BEHAVIOR comments verified through **manual review during PR**, not automated tests
- Rationale: Automated comment parsing would require significant infrastructure and is out of scope for v1
- Process: When adding/modifying GENERATOR BEHAVIOR comments, developer must verify against actual generated code in `Generated/` folder
- A test that exercises the documented pattern provides indirect verification (if the pattern compiles and works, the comment is likely accurate)

**Total estimated test count: ~70 tests**

### Edge Cases to Document

1. **Null matching in When()**: Cannot use `When(null)` directly; must use predicate `When(s => s == null)`
2. **Returns() vs OnCall() mutual exclusion**: Setting one clears the other
3. **Sequence exhaustion behavior**: Strict mode throws, non-strict returns default
4. **Init-only properties**: Generated differently than regular setters
5. **Generic method interceptors**: Use special handler pattern
6. **Method overloads**: Generate grouped interceptors with disambiguation

### Verification Checklist

- [x] All four patterns analyzed (Standalone, Inline Interface, Inline Class, Inline Delegate)
- [x] All four member types analyzed (Methods, Properties, Indexers, Events)
- [x] Breaking changes assessment completed (No breaking changes)
- [x] Pattern consistency check (Follows existing patterns)
- [x] Diagnostic requirements identified (None new)
- [x] Test strategy defined
- [x] Edge cases documented
- [x] Codebase deep-dive completed (28 files examined)

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-01-30
**Re-Reviewed:** 2026-01-30
**Reviewer:** knockoff-developer

### Initial Review (2026-01-30)

**Codebase Investigation:**
- `src/KnockOff/IMethodCallBuilder.cs` - Confirmed OnCall/ThenCall builder APIs
- `src/KnockOff/IMethodTracking.cs` - Confirmed Verify/Verifiable/Reset/LastArg/LastArgs
- `src/KnockOff/IWhenTracking.cs` - Confirmed IWhenBuilder/IWhenChain/IVoidWhenChain
- `src/KnockOff/IPropertyCallBuilder.cs` - Confirmed OnGet/OnSet/ThenGet/ThenSet
- `src/KnockOff/IPropertyTracking.cs` - Confirmed LastValue for setters
- `src/KnockOff/IIndexerCallBuilder.cs` - Confirmed indexer builder APIs
- `src/KnockOff/IIndexerTracking.cs` - Interface uses `LastKey`/`LastEntry`
- `src/KnockOff/Times.cs` - Confirmed all Times variants
- `src/KnockOff/KnockOffAttribute.cs` - Confirmed attribute APIs
- `src/KnockOff/IKnockOffStub.cs` - Confirmed Strict property
- `src/KnockOff/StubExtensions.cs` - Confirmed .Strict() extension
- `src/KnockOff/IMethodSequence.cs` - Confirmed sequence APIs
- `src/KnockOff/IPropertySequence.cs` - Confirmed property sequence APIs
- `src/KnockOff/IIndexerSequence.cs` - Confirmed indexer sequence APIs
- `src/Tests/KnockOffTests/WhenChainTests.cs` - Comprehensive test examples for all 4 patterns
- `src/Tests/KnockOffTests/EventTests.cs` - Event API test examples
- `src/Tests/KnockOffTests/IndexerTests.cs` - Uses `LastGetKey`/`LastSetEntry` (generated names)
- `src/Tests/KnockOffTests/InlineStubTests.cs` - Partial property pattern verified

**Initial Concerns Raised:** 4 concerns (API naming, test strategy, comment verification, phase dependencies)

### Re-Review After Architect Response (2026-01-30)

**Concern 1 (API Naming Inconsistency):** RESOLVED
- Architect added clarifying note at lines 306-308 explaining interface vs. generated property names
- Verified against codebase: `IIndexerTracking.cs` uses `LastKey`/`LastEntry`, tests use `LastGetKey`/`LastSetEntry`

**Concern 2 (Test Strategy Specificity):** RESOLVED
- Architect expanded Test Strategy section (lines 727-750) with concrete test counts (~70 tests)
- Defined "pattern parity" as same assertions with different stub instantiation
- Minimum coverage: at least one test per major API element

**Concern 3 (Comment Accuracy Verification):** RESOLVED
- Architect added GENERATOR BEHAVIOR Comment Verification Process (lines 385-400)
- Manual verification during implementation, PR review spot-checks, indirect verification via tests
- Automated parsing explicitly out of scope for v1

**Concern 4 (Phase Dependencies):** RESOLVED
- Architect added explicit dependency annotations to each phase
- Added visual dependency diagram (lines 475-488)
- Clear statement that Phases 2-5 can run in parallel

### Why This Plan Is Approved

1. All four original concerns have been addressed with specific, actionable guidance
2. No new issues emerged from the architect's updates
3. The plan demonstrates thorough understanding of all four stub patterns and member types
4. Test strategy now has concrete numbers and definitions
5. Phase dependencies are explicit with visual diagram
6. This is purely additive infrastructure with no breaking changes

### Review Summary

- Files examined: 18 source files
- Questions checked: All checklist items verified
- Devil's advocate: No remaining edge cases that would block implementation

---

### Architect Response to Concerns (2026-01-30)

**Concern 1: API Naming Inconsistency (Minor)**
- **Resolution:** Added clarifying note to the Indexer APIs section explaining that the checklist shows generated property names (what users interact with) while noting the underlying interface names. Also added explanation of why the generator adds `Get`/`Set` prefix (disambiguation).

**Concern 2: Test Strategy Lacks Specificity (Moderate)**
- **Resolution:** Expanded Test Strategy section with:
  - Concrete test counts (~70 tests total)
  - Definition of "pattern parity" (same assertions, different stub instantiation)
  - Minimum coverage requirement (at least one test per major API element)
  - Grouping strategy for related APIs (e.g., Times variants can share tests)

**Concern 3: Comment Accuracy Test Implementation Unclear (Moderate)**
- **Resolution:** Added detailed "GENERATOR BEHAVIOR Comment Verification Process" to Validation Requirements section:
  - Manual verification during implementation (build, check Generated/, update comment)
  - PR review spot-checks
  - Indirect verification via tests
  - Future consideration for automation noted but explicitly out of scope for v1

**Concern 4: Phase Dependencies Not Explicit (Minor)**
- **Resolution:** Added explicit dependency annotations to each phase plus a visual dependency diagram showing:
  - Phase 1 is blocking
  - Phases 2-5 can run in parallel after Phase 1
  - Phase 6 requires all of 2-5
  - Phase 7 can overlap with Phase 6 after Phase 2 completes

All concerns have been addressed. Plan is ready for developer re-review.

---

## Implementation Contract

**Created:** 2026-01-30
**Approved by:** knockoff-developer

### In Scope

**Phase 1: Foundation (BLOCKING)**
- [ ] Create `src/Design/Design.sln` solution file
- [ ] Create `src/Design/Design.Domain/Design.Domain.csproj` with:
  - TargetFrameworks: net8.0;net9.0;net10.0
  - Reference pattern from `src/KnockOff/KnockOff.csproj`
- [ ] Create `src/Design/Design.Stubs/Design.Stubs.csproj` with:
  - Reference to Design.Domain
  - Reference to KnockOff library
  - Analyzer reference to Generator
  - EmitCompilerGeneratedFiles pattern from KnockOffTests.csproj
- [ ] Create `src/Design/Design.Tests/Design.Tests.csproj` with:
  - xUnit v3 test project
  - Reference to Design.Stubs
  - Same test project patterns as KnockOffTests.csproj
- [ ] **Checkpoint:** Run `dotnet build src/Design/Design.sln` - must succeed with no errors

**Phase 2: Pattern Documentation**
- [ ] Create `src/Design/Design.Domain/Services/ICalculator.cs` - simple methods interface
- [ ] Create `src/Design/Design.Domain/Abstractions/ServiceBase.cs` - abstract class for class stubs
- [ ] Create `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` with:
  - Standalone pattern: `[KnockOff] partial class CalculatorStub : ICalculator`
  - Inline Interface pattern: `[KnockOff<ICalculator>]` on test class
  - Inline Class pattern: `[KnockOff<ServiceBase>]` with `.Object` documentation
  - Inline Delegate pattern: `[KnockOff<Func<int,int,int>>]` with `.Interceptor` documentation
  - Open Generic pattern: `[KnockOff(typeof(IRepository<>))]` documentation
  - At least 3 "DID NOT DO THIS" comments for rejected alternatives
  - At least 3 "DESIGN DECISION" comments explaining choices
- [ ] **Checkpoint:** Build succeeds, generated files appear in Design.Stubs/Generated/

**Phase 3: Member Type Coverage - Methods**
- [ ] Create `src/Design/Design.Domain/Services/IDataService.cs` - async methods interface
- [ ] Create `src/Design/Design.Stubs/Methods/BasicMethods.cs` demonstrating:
  - `Returns(value)` with GENERATOR BEHAVIOR comment
  - `OnCall(callback)` with typed argument access
  - Async auto-wrapping with Task.FromResult
  - COMMON MISTAKE comment for Returns() + ThenCall() error
- [ ] Create `src/Design/Design.Stubs/Methods/MethodSequences.cs` demonstrating:
  - `OnCall().ThenCall()` chains
  - Lazy elevation to sequence mode
  - `ThenNone()` for exhausting sequences
- [ ] Create `src/Design/Design.Stubs/Methods/WhenMatching.cs` demonstrating:
  - `When(args).Returns(value)` - value equality matching
  - `When(predicate).Returns(value)` - predicate matching
  - `ThenWhen().Returns()` chaining
  - Void method variants with IVoidWhenChain
  - GENERATOR BEHAVIOR comment showing When chain generation
- [ ] **Checkpoint:** Build succeeds, at least 2 GENERATOR BEHAVIOR comments verified against Generated/

**Phase 4: Member Type Coverage - Properties, Indexers, Events**
- [ ] Create `src/Design/Design.Domain/Entities/IEntity.cs` - properties interface
- [ ] Create `src/Design/Design.Domain/Entities/ICollection.cs` - indexers interface
- [ ] Create `src/Design/Design.Domain/Services/IEventSource.cs` - events interface
- [ ] Create `src/Design/Design.Stubs/Properties/PropertyBasics.cs` demonstrating:
  - `OnGet(value)`, `OnGet(callback)`, `Value` backing store
  - `OnSet(callback)`, `LastValue` capture
  - Get-only, set-only, get/set, init-only variations
- [ ] Create `src/Design/Design.Stubs/Properties/PropertySequences.cs` demonstrating:
  - `OnGet().ThenGet()` chains
  - `OnSet().ThenSet()` chains
- [ ] Create `src/Design/Design.Stubs/Indexers/IndexerBasics.cs` demonstrating:
  - `OnGet(callback)` with key access
  - `OnSet(callback)` with key and value
  - `Backing` dictionary
  - `LastGetKey`, `LastSetEntry` tracking (with note about interface vs generated names)
- [ ] Create `src/Design/Design.Stubs/Indexers/IndexerSequences.cs` for sequences
- [ ] Create `src/Design/Design.Stubs/Events/EventPatterns.cs` demonstrating:
  - `Raise(sender, args)`, `Raise(args)`, `Raise(arg1, arg2, ...)`
  - `VerifyAdd()`, `VerifyRemove()`, `HasSubscribers`
  - EventHandler, EventHandler<T>, Action<T...> variants
- [ ] **Checkpoint:** Build succeeds, all four member types demonstrated

**Phase 5: Advanced Features**
- [ ] Create `src/Design/Design.Stubs/Verification/VerificationPatterns.cs` demonstrating:
  - `Verify()`, `Verify(Times)`, `Verifiable()`, `Verifiable(Times)`
  - All Times variants: Once, Twice, Never, Exactly, AtLeastOnce, AtLeast, AtMost
  - `stub.Verify()` batch verification, `stub.VerifyAll()`
  - `Reset()` for clearing tracking
- [ ] Create `src/Design/Design.Stubs/Advanced/SourceDelegation.cs` demonstrating:
  - `Source(realImpl)` delegation pattern
  - `Source(null)` to clear
  - Priority documentation (OnCall > Source > SmartDefault)
- [ ] Create `src/Design/Design.Stubs/Advanced/StrictMode.cs` demonstrating:
  - `Strict = true` instance property
  - `[KnockOff(Strict = true)]` attribute-level
  - `[assembly: KnockOffStrict]` assembly-level
  - `.Strict()` extension method
  - StubException on unconfigured calls
- [ ] Create `src/Design/Design.Stubs/Delegates/DelegatePatterns.cs` demonstrating:
  - `[KnockOff<DelegateType>]` pattern
  - `.Interceptor` property for configuration
  - Implicit conversion to delegate type
- [ ] **Checkpoint:** Build succeeds, priority order documented

**Phase 6: Testing**
- [ ] Create `src/Design/Design.Tests/PatternTests/StandalonePatternTests.cs` (~5 tests)
- [ ] Create `src/Design/Design.Tests/PatternTests/InlineInterfacePatternTests.cs` (~5 tests)
- [ ] Create `src/Design/Design.Tests/PatternTests/InlineClassPatternTests.cs` (~5 tests)
- [ ] Create `src/Design/Design.Tests/PatternTests/InlineDelegatePatternTests.cs` (~5 tests)
- [ ] Create `src/Design/Design.Tests/MemberTests/MethodTests.cs` (~15 tests)
- [ ] Create `src/Design/Design.Tests/MemberTests/PropertyTests.cs` (~10 tests)
- [ ] Create `src/Design/Design.Tests/MemberTests/IndexerTests.cs` (~5 tests)
- [ ] Create `src/Design/Design.Tests/MemberTests/EventTests.cs` (~5 tests)
- [ ] Create `src/Design/Design.Tests/FeatureTests/WhenApiTests.cs` (~10 tests)
- [ ] Create `src/Design/Design.Tests/FeatureTests/SequenceTests.cs` (~5 tests)
- [ ] Create `src/Design/Design.Tests/FeatureTests/VerificationTests.cs` (~5 tests)
- [ ] **Checkpoint:** Run `dotnet test src/Design/Design.sln` - all tests pass (~70 tests)

**Phase 7: Documentation**
- [ ] Create `src/Design/README.md` explaining purpose for humans
- [ ] Create `src/Design/CLAUDE-DESIGN.md` with Claude-specific guidance:
  - How to use Design.* as source of truth
  - When to update Design.* (before any API-changing PR)
  - How to verify GENERATOR BEHAVIOR comments
- [ ] Update `CLAUDE.md` to reference `src/Design/` as source of truth for API design
- [ ] **Checkpoint:** Documentation complete, all references accurate

### Explicitly Out of Scope

- **Modifying existing generator code** - This plan creates documentation projects only
- **Modifying existing tests** - KnockOffTests unchanged
- **Complex real-world scenarios** - Keep examples focused on API demonstration
- **Integration with test frameworks beyond xUnit** - Only xUnit used
- **Performance benchmarks** - Not in scope
- **Automated comment parsing/verification** - Manual review only for v1

### Verification Gates

1. **After Phase 1:** `dotnet build src/Design/Design.sln` succeeds with zero errors
2. **After Phase 2:** Generated files appear in Design.Stubs/Generated/, all 4 patterns compile
3. **After Phase 3:** At least 2 GENERATOR BEHAVIOR comments manually verified against Generated/ files
4. **After Phase 4:** All 4 member types have demonstrations that compile
5. **After Phase 5:** Priority order demonstrated, strict mode demonstrations work
6. **After Phase 6:** `dotnet test src/Design/Design.sln` passes all ~70 tests
7. **Final:** Acceptance criteria checklist 100% complete:
   - [ ] All projects compile without errors
   - [ ] All tests pass
   - [ ] All four stub patterns demonstrated
   - [ ] All four member types demonstrated
   - [ ] At least 10 "DID NOT DO THIS BECAUSE" comments
   - [ ] At least 10 "DESIGN DECISION" comments
   - [ ] At least 5 "GENERATOR BEHAVIOR" comments
   - [ ] At least 5 "COMMON MISTAKE" comments
   - [ ] CLAUDE.md updated to reference src/Design as source of truth

### Stop Conditions

If any of these occur, STOP and report to user:

1. **Out-of-scope test fails** - Any existing test in KnockOffTests starts failing
2. **Generator behavior mismatch** - Generated code does not match documented behavior
3. **Missing API element** - An API in the checklist cannot be demonstrated because it does not exist
4. **Breaking change discovered** - Implementation reveals this requires changes to existing code

---

## Implementation Progress

**Phase 1: Foundation - COMPLETE (2026-01-30)**
- [x] Created `src/Design/Design.sln` solution file
- [x] Created `Design.Domain/Design.Domain.csproj` with TargetFrameworks: net8.0;net9.0;net10.0
- [x] Created `Design.Stubs/Design.Stubs.csproj` with KnockOff and Generator references
- [x] Created `Design.Tests/Design.Tests.csproj` with xUnit v3
- [x] Created initial domain interfaces: ICalculator, ServiceBase
- [x] Created delegate types: ArithmeticOperation, LogAction, SimpleAction, Factory<T>
- [x] Created AllPatterns.cs with all 4 stub patterns
- [x] Created initial test file: StandalonePatternTests.cs (5 tests)
- [x] **Verification:** `dotnet build src/Design/Design.sln` succeeds (0 errors)
- [x] **Verification:** `dotnet test src/Design/Design.sln` passes (5 tests on 3 frameworks)
- [x] **Verification:** Generated files appear in Design.Stubs/Generated/

**Phase 2: Pattern Documentation - IN PROGRESS**

---

## Completion Evidence

[Required before marking complete]

- **Tests Passing:** (To be provided)
- **Build Output:** (To be provided)
- **All Checklist Items:** (To be confirmed)
