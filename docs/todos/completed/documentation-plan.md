# Documentation Plan: KnockOff Comprehensive Documentation

## Current State Analysis

### What Exists
| Document | Status | Issues |
|----------|--------|--------|
| `README.md` | Outdated | Still shows `ExecutionInfo` (now `Spy`); no callbacks; stale open questions |
| `docs/knockoff-vs-moq.md` | Partial | Shows both patterns but doesn't articulate the duality |
| `docs/todos/*.md` | Internal | Implementation plans, not user-facing |

### What's Missing
1. **The Duality** — No explicit documentation of the two customization patterns
2. **Priority Order** — Nowhere explains callback → user method → default
3. **API Reference** — No comprehensive reference for Handler classes
4. **Indexed examples** — No clear examples for each member type
5. **Getting Started** — No quick-start guide for new users
6. **Best Practices** — No guidance on when to use which pattern

---

## Documentation Structure (Proposed)

```
README.md                          # Overview + quick start
docs/
├── getting-started.md             # Installation, first KnockOff, basic usage
├── concepts/
│   ├── customization-patterns.md  # THE DUALITY - central concept doc
│   ├── spy-and-tracking.md        # How Spy tracks invocations
│   └── backing-storage.md         # Backing fields and dictionaries
├── guides/
│   ├── properties.md              # Get/set, get-only, set-only
│   ├── methods.md                 # Void, return values, parameters
│   ├── async-methods.md           # Task, Task<T>, ValueTask<T>
│   ├── generics.md                # Generic interfaces
│   ├── multiple-interfaces.md     # Multi-interface + shared signatures
│   ├── interface-inheritance.md   # Base/derived interface patterns
│   └── indexers.md                # Indexer properties
├── reference/
│   ├── handler-api.md             # Complete Handler class reference
│   ├── generated-code.md          # What gets generated and why
│   └── attributes.md              # [KnockOff] attribute options
├── knockoff-vs-moq.md             # Existing comparison (update)
└── migration-from-moq.md          # Step-by-step migration guide
```

---

## Phase 1: Core Documentation

### 1.1 Update README.md

- [ ] **Fix outdated terminology**
  - Replace `ExecutionInfo` with `Spy` throughout
  - Update generated code examples to match current output

- [ ] **Remove stale open questions**
  - Delete questions 1-4 (all resolved)
  - Or move to "Design Decisions" section with answers

- [ ] **Add quick example of the duality**
  ```csharp
  // Pattern 1: Compile-time default in stub class
  protected int GetValue(int input) => input * 2;

  // Pattern 2: Runtime override per-test
  knockOff.Spy.GetValue.OnCall = (ko, input) => input * 100;
  ```

- [ ] **Link to detailed documentation**
  - Add links to getting-started.md and customization-patterns.md

### 1.2 Create `docs/concepts/customization-patterns.md`

This is the **central document** explaining the duality.

- [ ] **Introduction**
  - Two ways to customize stub behavior
  - Why both exist (compile-time vs runtime)

- [ ] **Pattern 1: User-Defined Methods**
  - Protected methods matching interface signature
  - When detected at compile time
  - Examples for methods, async methods
  - Note: Not applicable to properties/indexers

- [ ] **Pattern 2: Callbacks**
  - `OnCall` for methods
  - `OnGet` / `OnSet` for properties
  - `OnGet` / `OnSet` for indexers (with key parameter)
  - Signature variations by member type

- [ ] **Priority Order**
  ```
  1. Callback (if set) → takes precedence
  2. User method (if defined) → fallback
  3. Default behavior → last resort
     - Properties: return backing field value
     - Methods: return default(T)
     - Indexers: check backing dictionary, then default(T)
  ```

- [ ] **Reset Behavior**
  - `Reset()` clears callbacks and tracking
  - Falls back to user method after reset
  - Backing storage is NOT cleared

- [ ] **When to Use Which**
  | Scenario | Recommended Pattern |
  |----------|---------------------|
  | Same behavior in all tests | User method |
  | Different behavior per test | Callback |
  | Static return value | Either (user method simpler) |
  | Dynamic return based on args | Callback |
  | Side effects needed | Callback |
  | Access to Spy state | Callback (receives `ko` instance) |

- [ ] **Combining Both Patterns**
  - User method as default, callback for specific test
  - Example showing override and reset

### 1.3 Create `docs/getting-started.md`

- [ ] **Installation**
  - NuGet package reference
  - Required project settings

- [ ] **Your First KnockOff**
  - Define interface
  - Create `[KnockOff]` partial class
  - Use in test

- [ ] **Basic Verification**
  - Check `WasCalled`, `CallCount`
  - Check `LastCallArg`, `AllCalls`

- [ ] **Adding Custom Behavior**
  - User method example
  - Callback example

- [ ] **Next Steps**
  - Link to customization-patterns.md
  - Link to specific guides

---

## Phase 2: Member-Specific Guides

### 2.1 Create `docs/guides/properties.md`

- [ ] **Property Types**
  - Get/set properties
  - Get-only properties
  - Set-only properties

- [ ] **Tracking**
  - `GetCount`, `SetCount`
  - `LastSetValue`, `AllSetValues`

- [ ] **Customization**
  - `OnGet` callback
  - `OnSet` callback
  - Backing field behavior

- [ ] **Examples**
  - Simple property stub
  - Callback returning computed value
  - Callback capturing set values

### 2.2 Create `docs/guides/methods.md`

- [ ] **Method Types**
  - Void, no parameters
  - Void, with parameters
  - Return value, no parameters
  - Return value, with parameters

- [ ] **Tracking**
  - `WasCalled`, `CallCount`
  - `LastCallArg` (single param)
  - `LastCallArgs` (multiple params — named tuple)
  - `AllCalls` history

- [ ] **Customization**
  - User-defined protected method
  - `OnCall` callback
  - Priority order

- [ ] **Parameter Handling**
  - Single parameter: `LastCallArg` (no tuple)
  - Multiple parameters: Named tuple with param names
  - Tuple destructuring examples

### 2.3 Create `docs/guides/async-methods.md`

- [ ] **Supported Types**
  - `Task` (void async)
  - `Task<T>` (async with return)
  - `ValueTask` (void async)
  - `ValueTask<T>` (async with return)

- [ ] **User Methods**
  - Must return matching async type
  - Example: `protected Task<int> GetAsync(int id) => Task.FromResult(id);`

- [ ] **Callbacks**
  - `OnCall` returns the async type directly
  - Example: `OnCall = (ko, id) => Task.FromResult(id * 10);`

- [ ] **Default Behavior**
  - `Task`: Returns `Task.CompletedTask`
  - `Task<T>`: Returns `Task.FromResult(default(T))`
  - `ValueTask`: Returns `default(ValueTask)`
  - `ValueTask<T>`: Returns `new ValueTask<T>(default(T))`

### 2.4 Create `docs/guides/generics.md`

- [ ] **Generic Interface Support**
  - KnockOff must specify concrete type
  - Example: `class UserRepoKnockOff : IRepository<User>`

- [ ] **Type Parameters in Tracking**
  - Handler types use concrete types
  - `LastCallArg` is strongly typed

- [ ] **Examples**
  - Generic repository pattern
  - Callback returning mocked entity

### 2.5 Create `docs/guides/multiple-interfaces.md`

- [ ] **Implementing Multiple Interfaces**
  - Single KnockOff class, multiple interfaces
  - Generated `AsXYZ()` methods

- [ ] **Shared Members**
  - Same property name: shared backing field
  - Same method signature: shared Handler

- [ ] **Distinct Members**
  - Unique members get their own Handlers

- [ ] **Examples**
  - Logger + Notifier (different methods)
  - Logger + Auditor (shared `Log` method)

### 2.6 Create `docs/guides/interface-inheritance.md`

- [ ] **Inherited Interface Support**
  - Derived interface inherits base members
  - KnockOff implements all members

- [ ] **AsXYZ() Methods**
  - Generated for both derived and base interfaces

- [ ] **Examples**
  - `IAuditableEntity : IBaseEntity`
  - Accessing base properties via derived interface

### 2.7 Create `docs/guides/indexers.md`

- [ ] **Indexer Types**
  - Get-only indexers
  - Get/set indexers

- [ ] **Tracking**
  - `GetCount`, `LastGetKey`, `AllGetKeys`
  - `SetCount`, `LastSetEntry`, `AllSetEntries`

- [ ] **Backing Dictionary**
  - Auto-generated `{IndexerName}Backing` dictionary
  - Pre-populate for test setup
  - Not cleared by `Reset()`

- [ ] **Callbacks**
  - `OnGet = (ko, key) => ...`
  - `OnSet = (ko, key, value) => ...`
  - Priority: Callback → Backing dictionary → default

- [ ] **Examples**
  - Property store pattern
  - Conditional callback based on key

---

## Phase 3: Reference Documentation

### 3.1 Create `docs/reference/handler-api.md`

- [ ] **Handler Types Overview**
  - One Handler per interface member
  - Nested in `Spy` class

- [ ] **Method Handler**
  | Member | Type | Description |
  |--------|------|-------------|
  | `CallCount` | `int` | Number of calls |
  | `WasCalled` | `bool` | `CallCount > 0` |
  | `LastCallArg` | `T` | Last arg (single param) |
  | `LastCallArgs` | `(T1, T2, ...)` | Named tuple (multi param) |
  | `AllCalls` | `List<T>` or `List<(T1, T2, ...)>` | Call history |
  | `OnCall` | `Func<TKnockOff, TArgs, TReturn>?` | Callback |
  | `Reset()` | `void` | Clear tracking and callback |

- [ ] **Property Handler**
  | Member | Type | Description |
  |--------|------|-------------|
  | `GetCount` | `int` | Number of gets |
  | `SetCount` | `int` | Number of sets |
  | `LastSetValue` | `T?` | Last value set |
  | `AllSetValues` | `List<T>` | Set history |
  | `OnGet` | `Func<TKnockOff, T>?` | Getter callback |
  | `OnSet` | `Action<TKnockOff, T>?` | Setter callback |
  | `Reset()` | `void` | Clear tracking and callbacks |

- [ ] **Indexer Handler**
  | Member | Type | Description |
  |--------|------|-------------|
  | `GetCount` | `int` | Number of gets |
  | `SetCount` | `int` | Number of sets |
  | `LastGetKey` | `TKey?` | Last key accessed |
  | `AllGetKeys` | `List<TKey>` | Get history |
  | `LastSetEntry` | `(TKey, TValue)?` | Last set entry |
  | `AllSetEntries` | `List<(TKey, TValue)>` | Set history |
  | `OnGet` | `Func<TKnockOff, TKey, TValue>?` | Getter callback |
  | `OnSet` | `Action<TKnockOff, TKey, TValue>?` | Setter callback |
  | `Reset()` | `void` | Clear tracking and callbacks |

### 3.2 Create `docs/reference/generated-code.md`

- [ ] **What Gets Generated**
  - Partial class extension
  - `Spy` property with nested class
  - Handler classes for each member
  - Backing fields/dictionaries
  - Explicit interface implementations
  - `AsXYZ()` helper methods

- [ ] **Generation Rules**
  - User method detection (protected, matching signature)
  - Member naming conventions
  - Indexer naming (`StringIndexer`, `IntIndexer`, etc.)

- [ ] **Viewing Generated Code**
  - Enable `EmitCompilerGeneratedFiles`
  - Location: `Generated/KnockOff.Generator/...`

### 3.3 Create `docs/reference/attributes.md`

- [ ] **[KnockOff]**
  - Applied to partial class implementing interface(s)
  - No parameters (currently)

- [ ] **Future Considerations**
  - Potential options for customizing generation

---

## Phase 4: Migration and Comparison

### 4.1 Update `docs/knockoff-vs-moq.md`

- [ ] **Update to match current implementation**
  - Replace `ExecutionInfo` with `Spy`
  - Verify all examples compile

- [ ] **Add section on the duality**
  - Moq: Setup in test method
  - KnockOff: User method OR callback

- [ ] **Update feature matrix**
  - Mark indexers as supported
  - Update any other changes

### 4.2 Create `docs/migration-from-moq.md`

- [ ] **Pattern Mapping**
  | Moq Pattern | KnockOff Equivalent |
  |-------------|---------------------|
  | `.Setup().Returns()` | `OnCall` callback |
  | `.ReturnsAsync()` | `OnCall` returning `Task.FromResult()` |
  | `.Callback()` | `OnCall` with side effects |
  | `.Verify(Times.X)` | `Assert.Equal(X, CallCount)` |
  | `It.IsAny<T>()` | Implicit (callback receives all args) |
  | `It.Is<T>(predicate)` | Check in callback body |

- [ ] **Step-by-Step Migration**
  1. Create KnockOff class
  2. Replace `Mock<T>` with KnockOff instance
  3. Replace `mock.Object` with interface cast
  4. Replace `Setup` with `OnCall`
  5. Replace `Verify` with assertions on Handler

- [ ] **Common Patterns**
  - Static returns
  - Conditional returns
  - Argument capture
  - Call counting

---

## Phase 5: README Overhaul

### 5.1 Restructure README.md

- [ ] **Keep it concise**
  - Brief overview (what is KnockOff)
  - Single compelling example
  - The duality in 10 lines
  - Link to full docs

- [ ] **New Structure**
  ```markdown
  # KnockOff

  ## What is KnockOff?
  [2-3 sentences]

  ## Quick Example
  [Interface + KnockOff + Test usage]

  ## Two Ways to Customize
  [Brief explanation of user methods vs callbacks]

  ## Features
  [Bullet list with links]

  ## Getting Started
  [Link to getting-started.md]

  ## Documentation
  [Links to all docs]

  ## Comparison with Moq
  [Link to knockoff-vs-moq.md]
  ```

---

## Implementation Checklist

### Priority 1: Fix Immediate Issues
- [ ] Update README.md: Replace `ExecutionInfo` with `Spy`
- [ ] Update README.md: Remove stale open questions
- [ ] Update knockoff-vs-moq.md: Replace `ExecutionInfo` with `Spy`

### Priority 2: Core Concept Documentation
- [ ] Create `docs/concepts/customization-patterns.md` (THE DUALITY)
- [ ] Create `docs/getting-started.md`

### Priority 3: Member Guides
- [ ] Create `docs/guides/properties.md`
- [ ] Create `docs/guides/methods.md`
- [ ] Create `docs/guides/async-methods.md`
- [ ] Create `docs/guides/generics.md`
- [ ] Create `docs/guides/multiple-interfaces.md`
- [ ] Create `docs/guides/interface-inheritance.md`
- [ ] Create `docs/guides/indexers.md`

### Priority 4: Reference Documentation
- [ ] Create `docs/reference/handler-api.md`
- [ ] Create `docs/reference/generated-code.md`
- [ ] Create `docs/reference/attributes.md`

### Priority 5: Migration
- [ ] Create `docs/migration-from-moq.md`
- [ ] Update `docs/knockoff-vs-moq.md` with duality section

### Priority 6: Final Polish
- [ ] Restructure README.md to be concise entry point
- [ ] Cross-link all documents
- [ ] Review for consistency

---

## Notes

### Terminology Alignment
- `Spy` (not `ExecutionInfo`) — current name
- `Handler` — the tracking class for each member
- `Backing` — auto-generated storage (field or dictionary)
- User method — protected method matching interface signature
- Callback — `OnCall`/`OnGet`/`OnSet` delegate

### Key Message
**The duality is a feature, not an accident.** User methods provide compile-time defaults; callbacks provide runtime flexibility. Together they offer layered customization that neither Moq nor other mocking frameworks provide.
