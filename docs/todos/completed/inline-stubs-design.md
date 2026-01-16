# Inline Stubs via Partial Properties

## Overview

Add support for declaring stubs directly in test classes using `[KnockOff<T>]` attributes and partial properties, providing full IntelliSense while reducing boilerplate.

## Prerequisites

- **C# 13** required for partial property syntax (`.NET 9+` or `<LangVersion>preview</LangVersion>`)
- Partial properties are optional - users can instantiate stubs directly: `var stub = new Stubs.IService();`
- Works on .NET 8+ when not using partial property syntax

## Diagnostics

| ID | Severity | Scenario | Message |
|----|----------|----------|---------|
| KO1001 | Error | `[KnockOff<T>]` where T is not interface/delegate | `Type '{0}' must be an interface or named delegate type` |
| KO1002 | Error | Same simple name from different namespaces | `Multiple interfaces named '{0}' found. Use explicit [KnockOff] pattern for disambiguation` |
| KO1003 | Error | `Stubs` type already exists in scope | `Type 'Stubs' conflicts with generated nested class. Rename existing type or use explicit pattern` |

## Limitations

- **No user method detection**: Inline stubs don't support user-defined methods. Use `OnCall` callbacks for custom behavior, or use the explicit pattern.
- **Same-named interfaces**: Interfaces with identical simple names from different namespaces cause KO1002 error. Use explicit pattern for disambiguation.
- **Generic delegates**: Both closed (`[KnockOff<Factory<string>>]`) and open (`[KnockOff(typeof(Factory<>))]`) generic delegates supported. *(Open generic support added in v10.20)*
- **Delegates with ref/out parameters**: Not supported. The `OnCall` callback uses `Func<>`/`Action<>` which cannot represent ref/out parameters. Use the explicit pattern with user-defined methods.

## Design

### User Experience

```csharp
[KnockOff<IUserService>]
[KnockOff<IRepository>]
public partial class MyTests
{
    protected partial Stubs.IUserService userService { get; }
    protected partial Stubs.IRepository repository { get; }

    [Fact]
    public void Test()
    {
        userService.GetUser.OnCall = id => new User(id, "Test");
        repository.Save.OnCall = entity => { };

        var sut = new Service(userService, repository);
        sut.DoWork(123);

        Assert.Equal(1, userService.GetUser.CallCount);
        Assert.True(repository.Save.WasCalled);
    }
}
```

### Generated Code

For each `[KnockOff<T>]` attribute, the generator produces:

```csharp
public partial class MyTests
{
    // Partial property implementations (only if user declared partial property)
    private Stubs.IUserService _userService = new();
    protected partial Stubs.IUserService userService => _userService;

    // Nested Stubs class containing all stub types for this test class
    public static class Stubs
    {
        // Handler class for GetUser method
        public sealed class IUserService_GetUserHandler
        {
            public delegate User GetUserDelegate(Stubs.IUserService ko, int id);
            public int CallCount { get; private set; }
            public bool WasCalled => CallCount > 0;
            public int? LastCallArg { get; private set; }
            public GetUserDelegate? OnCall { get; set; }
            public void RecordCall(int id) { CallCount++; LastCallArg = id; }
            public void Reset() { CallCount = 0; LastCallArg = default; OnCall = null; }
        }

        public class IUserService : global::IUserService
        {
            // Handler property
            public IUserService_GetUserHandler GetUser { get; } = new();

            // Explicit interface implementation
            User global::IUserService.GetUser(int id)
            {
                GetUser.RecordCall(id);
                if (GetUser.OnCall is { } onCall) return onCall(this, id);
                return default!;
            }
        }
    }
}
```

## Relationship to Existing Pattern

Two patterns will coexist:

| Pattern | Use Case |
|---------|----------|
| **Inline** (`[KnockOff<T>]` on test class) | Quick stubs scoped to one test class |
| **Explicit** (`[KnockOff]` on stub class) | Reusable stubs, custom user methods, shared across tests |

The explicit pattern remains the primary documented approach. Inline is a convenience layer.

## Implementation Plan

### Phase 1: Attribute Support ✅

- [x] Add `KnockOffAttribute<T>` generic attribute to core library
  - Requires `[AttributeUsage(AllowMultiple = true)]`
  - Target: Class
- [x] Update generator predicate to find classes with `[KnockOff<T>]` attributes
- [x] Extract interface types from generic attribute arguments

### Phase 2: Stubs Nested Class Generation ✅

Generate stub classes for **all** `[KnockOff<T>]` attributes, regardless of whether partial properties exist. Users can instantiate stubs directly without declaring properties.

- [x] Generate `public static class Stubs` inside the target class
- [x] For each `[KnockOff<T>]` attribute:
  - Generate a stub class named after the interface (e.g., `Stubs.IUserService`)
  - Stub class implements the interface
  - Generate handler classes for each interface member (same pattern as explicit stubs)
  - Generate explicit interface implementations that delegate to handlers
- [x] Validate T is interface or named delegate (emit KO1001 if not)
- [x] Check for simple name collisions (emit KO1002 if collision)
- [x] Check for existing `Stubs` type in scope (emit KO1003 if conflict)

### Phase 3: Partial Property Detection & Implementation ✅

Partial properties are **optional convenience**. Users can skip them and use `new Stubs.IService()` directly.

- [x] Find partial property declarations with `Stubs.*` types in the target class (syntactic matching on type name prefix)
- [x] Match property type simple name to generated stub classes
- [x] Generate backing fields for each matched partial property
- [x] Generate partial property implementations returning the backing field
- [x] Initialize backing fields with `new()`

Note: Partial properties require C# 13 (.NET 9+).

### Phase 4: Handler Types ✅

Generate unique handler classes per member, following the same pattern as explicit stubs. Each handler class includes:

- `CallCount` / `GetCount` / `SetCount` - invocation tracking
- `WasCalled` - convenience predicate
- `LastCallArg` / `LastCallArgs` / `LastSetValue` / `LastGetKey` / `LastSetEntry` - argument capture
- `OnCall` / `OnGet` / `OnSet` - callback delegates
- `Reset()` - clears all tracking state

Handler class naming: `{InterfaceName}_{MemberName}Handler` (e.g., `IUserService_GetUserHandler`)

### Phase 5: Testing ✅

- [x] Unit tests for attribute detection
- [x] Unit tests for Stubs class generation
- [x] Unit tests for partial property generation
- [x] Integration tests with actual test scenarios
- [ ] Verify IntelliSense works in IDE (manual)

## Design Decisions

### 1. Naming: Keep interface name as-is

```csharp
Stubs.IUserService userService { get; }  // ✓ Keep the 'I' prefix
```

No ambiguity, matches the interface exactly.

### 2. Property visibility: User-controlled

Generator mirrors whatever visibility the user declares (protected, private, etc.).

### 3. Setters: Get-only

```csharp
protected partial Stubs.IUserService userService { get; }  // ✓ Get-only
```

Replacing a stub mid-test is unusual. If needed, use the explicit pattern.

### 4. Partial property optional

Stub classes are always generated for all `[KnockOff<T>]` attributes. Partial properties are optional convenience - users can instantiate stubs directly with `new Stubs.IService()`. If a partial property is declared, the generator creates a backing field and property implementation.

### 5. Both patterns allowed on same class

A class can have both `[KnockOff<T>]` attributes AND implement interfaces with `[KnockOff]`. This enables the **nested stubs pattern**.

### 6. Stubs class visibility: public

The `Stubs` nested class is always `public`. Since it's a nested class, the containing class controls effective visibility. A public nested class inside an internal test class is effectively internal.

### 7. No user method detection

Inline stubs don't detect user-defined methods. Use `OnCall` callbacks for custom behavior:
```csharp
userService.GetUser.OnCall = (ko, id) => new User(id, "Custom");
```
For user method detection, use the explicit pattern.

### 8. Handler types are generated per-member

Each interface member gets its own handler class (e.g., `IUserService_GetUserHandler`). This matches the explicit pattern and provides full IntelliSense for each member's specific signature.

### 9. Test isolation

Stub instances persist for the lifetime of the test class instance. Framework behavior:

| Framework | Test Class Lifetime | Isolation |
|-----------|---------------------|-----------|
| **xUnit** | New instance per test | ✓ Automatic isolation |
| **NUnit** | Shared by default | Call `Reset()` in `[SetUp]` |
| **MSTest** | Shared by default | Call `Reset()` in `[TestInitialize]` |

Each handler has a `Reset()` method that clears `CallCount`, `LastCallArg`/`LastCallArgs`, and `OnCall` callback. For NUnit/MSTest with shared fixtures, call `Reset()` on each handler in setup to prevent test pollution.

**Document this in:** `docs/guides/inline-stubs.md` and the KnockOff skill.

## Nested Stubs Pattern

When an interface returns another interface, use `[KnockOff<T>]` to create stubs for nested types:

```csharp
public interface IFoo
{
    IPropertyInfo this[int index] { get; }
}

public interface IPropertyInfo
{
    string Name { get; }
}

[KnockOff]                    // Generates stub for IFoo on class
[KnockOff<IPropertyInfo>]     // Makes Stubs.IPropertyInfo available
public partial class FooStub : IFoo
{
    protected partial Stubs.IPropertyInfo propStub { get; }
}
```

Usage:
```csharp
var stub = new FooStub();

// Configure the nested stub
stub.propStub.Name.OnGet = () => "TestProp";

// Wire it to the indexer
stub.IFoo.Indexer.OnGet = index => stub.propStub;

// Now stub[0].Name returns "TestProp"
```

Key points:
- No magic deep-stubbing - user explicitly wires nested stubs
- `Stubs.IPropertyInfo` implements `IPropertyInfo`, so it passes type checks
- Readable and debuggable

## Delegate Stubs

Stub named delegate types directly via `[KnockOff<TDelegate>]`, enabling call tracking and behavior configuration for delegates passed to code under test.

### User Experience

```csharp
// Named delegate type
public delegate bool IsUniqueRule(string value);

// Test class
[KnockOff<IsUniqueRule>]
public partial class MyTests
{
    protected partial Stubs.IsUniqueRule uniqueRule { get; }

    [Fact]
    public void Test()
    {
        // Configure behavior
        uniqueRule.OnCall = value => value != "duplicate";

        // Pass to code under test (implicit conversion)
        var sut = new Validator(uniqueRule);
        sut.Validate("test");

        // Verify
        Assert.True(uniqueRule.WasCalled);
        Assert.Equal("test", uniqueRule.LastCallArg);
    }
}
```

### Generated Code

```csharp
public partial class MyTests
{
    private Stubs.IsUniqueRule _uniqueRule = new();
    protected partial Stubs.IsUniqueRule uniqueRule => _uniqueRule;

    public static class Stubs
    {
        public class IsUniqueRule
        {
            public int CallCount { get; private set; }
            public bool WasCalled => CallCount > 0;
            public string? LastCallArg { get; private set; }
            public Func<string, bool>? OnCall { get; set; }

            private bool Invoke(string value)
            {
                CallCount++;
                LastCallArg = value;
                if (OnCall != null) return OnCall(value);
                return default!;
            }

            public void Reset()
            {
                CallCount = 0;
                LastCallArg = default;
                OnCall = null;
            }

            // Implicit conversion to delegate type
            public static implicit operator global::IsUniqueRule(IsUniqueRule stub)
                => stub.Invoke;
        }
    }
}
```

### Delegate Stub Design Decisions

1. **Named delegates only** - No support for `Func<>`/`Action<>` built-in types
2. **Naming matches delegate type** - `Stubs.IsUniqueRule` for delegate `IsUniqueRule`
3. **Implicit conversion** - Stub converts to delegate type for seamless passing
4. **No `ko` parameter** - Unlike interface handlers, `OnCall` receives only delegate parameters
5. **Deduplication** - Same delegate type in multiple attributes generates once

### Handler Properties

| Property | Type | Description |
|----------|------|-------------|
| `CallCount` | `int` | Number of invocations |
| `WasCalled` | `bool` | Convenience predicate |
| `LastCallArg` | `T?` | Last argument (single param delegates) |
| `LastCallArgs` | `(T1, T2, ...)?` | Last arguments as tuple (multi-param) |
| `OnCall` | `Func<...>?` or `Action<...>?` | Callback for custom behavior |
| `Reset()` | method | Clears tracking and callback |

### Implementation Phases (Delegate-Specific)

#### Phase 6: Detect Delegate Types ✅

- [x] In predicate/transform, detect when `[KnockOff<T>]` where `T` is a delegate type
- [x] Extract delegate signature: return type, parameter types/names
- [x] Create `DelegateInfo` (equatable) to store delegate info

#### Phase 7: Generate Delegate Stub Class ✅

- [x] `GenerateDelegateStubClass(DelegateModel delegate)` method
- [x] Generate handler properties (CallCount, WasCalled, LastCallArg/Args)
- [x] Generate `OnCall` property with appropriate Func/Action type
- [x] Generate private `Invoke` method matching delegate signature
- [x] Generate `Reset()` method
- [x] Generate implicit conversion operator

#### Phase 8: Delegate Edge Cases ✅

- [x] Delegates with no parameters
- [x] Delegates with many parameters (tuple tracking)
- [x] Void delegates (Action-like) - `OnCall` is `Action<...>?`
- [x] Closed generic delegates (e.g., `[KnockOff<Factory<string>>]`) - generates `Stubs.Factory`

**Known limitations:**
- **ref/out parameters**: Not supported. `Func<>`/`Action<>` types used for `OnCall` cannot represent ref/out parameters. Workaround: use explicit `[KnockOff]` pattern with user-defined methods.
- **Open generic delegates**: Now supported as of v10.20 via `[KnockOff(typeof(Factory<>))]` syntax.

#### Phase 9: Delegate Testing ✅

- [x] Test: Simple delegate with single parameter
- [x] Test: Void delegate (Action-like)
- [x] Test: Multi-parameter delegate with tuple tracking
- [x] Test: Delegate with no parameters
- [x] Test: Closed generic delegate (e.g., `Factory<string>`)
- [x] Test: Implicit conversion works correctly
- [x] Test: OnCall callback invoked
- [x] Test: Reset() clears state

### Documentation (Inline Stubs + Delegates)

#### Phase 10: Documentation Samples ✅

Add samples covering both inline interface stubs and delegate stubs.

**File header convention:** Each sample file should include a header listing all snippets:
```csharp
/// <summary>
/// Code samples for docs/guides/inline-stubs.md
///
/// Snippets in this file:
/// - docs:inline-stubs:basic-example
/// - docs:inline-stubs:multiple-interfaces
/// - docs:inline-stubs:nested-pattern
///
/// Corresponding tests: InlineStubSamplesTests.cs
/// </summary>
```

**Inline Interface Stubs:**
- [x] Add inline stub samples to `src/Tests/KnockOff.Documentation.Samples/Guides/InlineStubsSamples.cs`
  - Basic inline stub example (`[KnockOff<IService>]` on test class)
  - Multiple interfaces example
  - Nested stubs pattern (interface returns interface)
- [x] Mark samples with `#region docs:inline-stubs:{snippet-id}` markers

**Delegate Stubs:**
- [x] Add delegate stub samples to `src/Tests/KnockOff.Documentation.Samples/Guides/DelegateStubsSamples.cs`
  - Simple delegate stub example
  - Multi-parameter delegate example
  - Void delegate example
  - Implicit conversion usage
- [x] Mark samples with `#region docs:delegates:{snippet-id}` markers

**Tests:**
- [x] Add tests for all samples in `src/Tests/KnockOff.Documentation.Samples.Tests/Guides/`

#### Phase 11: Documentation Guides ✅

- [x] Create `docs/guides/inline-stubs.md` with snippet markers
  - Overview of inline pattern vs explicit pattern
  - Prerequisites (C# 13 for partial properties, or direct instantiation)
  - User experience walkthrough
  - Nested stubs pattern
  - Test isolation guidance (xUnit vs NUnit/MSTest - call `Reset()` in setup)
  - When to use inline vs explicit
- [x] Create `docs/guides/delegates.md` with snippet markers
  - Overview of delegate stubs
  - Handler properties reference
  - Implicit conversion usage
  - Edge cases and limitations (closed generics only)
- [ ] Update `docs/getting-started.md` to mention inline pattern (deferred to release)
- [x] Run `.\scripts\extract-snippets.ps1 -Update` to sync snippets

**Release Notes (for the version that ships this feature):**
- [ ] Create `docs/release-notes/vX.X.X.md` with:
  - Summary of inline stubs and delegate stubs features
  - "What's New" section with code examples
  - Link to new guides
- [ ] Update `docs/release-notes/index.md`:
  - Add to "Current Version" section
  - Add row to Highlights table

#### Phase 12: KnockOff Skill ✅

Updated the skill directory (`~/.claude/skills/knockoff/`):

- [x] Update `SKILL.md` with new "Stub Patterns" section covering:
  - Inline stubs (`[KnockOff<T>]` pattern)
  - Partial property auto-instantiation (C# 13+)
  - Delegate stubs with implicit conversion
- [x] Update "Supported Features" table with new features
- [x] Run `.\scripts\extract-snippets.ps1 -Update` to sync snippets

Note: Dedicated `inline-stubs.md` and `delegates.md` skill detail files deferred - content integrated into main SKILL.md for now.

#### Phase 13: Todo Completion ✅

Implementation complete:

- [x] All phases completed
- [x] 412 tests passing (182 KnockOffTests on net8, 183 on net9/10; 228-229 Documentation.Samples.Tests)
- [x] Documentation guides created: `inline-stubs.md`, `delegates.md`
- [x] KnockOff skill updated with new patterns

**Results:**
- Inline stubs: 9 snippet regions, comprehensive test coverage
- Delegate stubs: 8 snippet regions, including closed generic delegates
- Known limitation documented: ref/out parameters in delegates not supported (Func<>/Action<> limitation)
- Diagnostics implemented: KO1001 (type validation), KO1002 (name collision), KO1003 (Stubs conflict)

**To complete release:**
- [ ] Move this file to `docs/todos/completed/inline-stubs-design.md`
- [ ] Create release notes version when publishing

## File Changes

### Generator & Tests
```
src/
├── KnockOff/
│   └── KnockOffAttribute.cs      # Add generic KnockOffAttribute<T>
├── Generator/
│   ├── KnockOffGenerator.cs      # Update predicate for both patterns
│   ├── InlineStubGenerator.cs    # New: handles [KnockOff<T>] pattern
│   └── Models/
│       ├── InlineStubModel.cs    # New: equatable model for inline stubs
│       └── DelegateModel.cs      # New: equatable model for delegate stubs
└── Tests/
    ├── KnockOffGeneratorTests/
    │   ├── InlineStubTests.cs        # New: tests for inline pattern
    │   └── DelegateStubTests.cs      # New: tests for delegate stubs
    ├── KnockOff.Documentation.Samples/
    │   ├── InlineStubs/              # New: inline stub samples
    │   ├── DelegateStubs/            # New: delegate stub samples
    │   └── Skills/                   # Existing: add inline/delegate skill samples
    └── KnockOff.Documentation.Samples.Tests/
        ├── InlineStubSamplesTests.cs     # New: tests for inline samples
        └── DelegateStubSamplesTests.cs   # New: tests for delegate samples
```

### Documentation
```
docs/
├── guides/
│   ├── inline-stubs.md           # New: inline stubs guide
│   └── delegates.md              # New: delegate stubs guide
├── getting-started.md            # Update: mention inline pattern
└── release-notes/
    └── vX.X.X.md                 # New: release notes for this feature
```

### Skill Files
```
~/.claude/skills/knockoff/
├── SKILL.md                      # Update: add to Additional Resources
├── inline-stubs.md               # New: inline stubs skill detail
└── delegates.md                  # New: delegate stubs skill detail
```

## Example: Full Generated Output

Input:
```csharp
[KnockOff<ICalculator>]
public partial class CalculatorTests
{
    protected partial Stubs.ICalculator calc { get; }
}

public interface ICalculator
{
    int Add(int a, int b);
    int LastResult { get; }
}
```

Output:
```csharp
public partial class CalculatorTests
{
    private Stubs.ICalculator _calc = new();
    protected partial Stubs.ICalculator calc => _calc;

    public static class Stubs
    {
        // Handler for Add method
        public sealed class ICalculator_AddHandler
        {
            public delegate int AddDelegate(Stubs.ICalculator ko, int a, int b);
            public int CallCount { get; private set; }
            public bool WasCalled => CallCount > 0;
            public (int a, int b)? LastCallArgs { get; private set; }
            public AddDelegate? OnCall { get; set; }
            public void RecordCall(int a, int b) { CallCount++; LastCallArgs = (a, b); }
            public void Reset() { CallCount = 0; LastCallArgs = default; OnCall = null; }
        }

        // Handler for LastResult property
        public sealed class ICalculator_LastResultHandler
        {
            public delegate int GetDelegate(Stubs.ICalculator ko);
            public int GetCount { get; private set; }
            public int Value { get; set; }
            public GetDelegate? OnGet { get; set; }
            public void RecordGet() => GetCount++;
            public void Reset() { GetCount = 0; Value = default; OnGet = null; }
        }

        public class ICalculator : global::ICalculator
        {
            // Handler properties
            public ICalculator_AddHandler Add { get; } = new();
            public ICalculator_LastResultHandler LastResult { get; } = new();

            // Explicit interface implementations
            int global::ICalculator.Add(int a, int b)
            {
                Add.RecordCall(a, b);
                if (Add.OnCall is { } onCall) return onCall(this, a, b);
                return default!;
            }

            int global::ICalculator.LastResult
            {
                get
                {
                    LastResult.RecordGet();
                    if (LastResult.OnGet is { } onGet) return onGet(this);
                    return LastResult.Value;
                }
            }
        }
    }
}
```
