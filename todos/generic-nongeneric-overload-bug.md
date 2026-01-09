# Bug: Generic and Non-Generic Method Overloads Share Interceptor

## Problem

When an interface has both a non-generic method and a generic method with the same name, the generator incorrectly merges them into a single generic interceptor structure. This causes compilation errors.

## Example: `IRuleManager`

```csharp
public interface IRuleManager
{
    // Non-generic overload
    Task RunRule(IRule r, CancellationToken? token);

    // Generic overload
    Task RunRule<T>(CancellationToken? token) where T : IRule;
}
```

## Generated Code (Broken)

The generator creates a single `IRuleManager_RunRuleInterceptor` with the generic `Of<T>()` pattern, then tries to use it for the non-generic method:

```csharp
// Line 275-280 of generated code
Task IRuleManager.RunRule(IRule r, CancellationToken? token)
{
    RunRule.RecordCall(r, token);  // ERROR: RecordCall doesn't exist on generic interceptor
    if (RunRule.OnCall is { } onCall) return onCall(this, r, token);  // ERROR: OnCall doesn't exist
    return Task.CompletedTask;
}
```

## Errors

```
error CS1061: 'IRuleManager_RunRuleInterceptor' does not contain a definition for 'RecordCall'
error CS1061: 'IRuleManager_RunRuleInterceptor' does not contain a definition for 'OnCall'
```

---

## Root Cause Analysis

**Location**: `KnockOffGenerator.cs:6044-6053` (and similar patterns at lines 1822, 4273, etc.)

The bug is in the branching logic that decides how to generate interceptors:

```csharp
var hasGenericOverload = group.Overloads.Any(o => o.IsGenericMethod);
if (hasGenericOverload)
{
    GenerateInlineStubGenericMethodHandlerClass(sb, group, interfaceSimpleName);  // Only this!
}
else
{
    GenerateInlineStubNonGenericMethodHandlerClass(sb, group, interfaceSimpleName);
}
```

When ANY overload is generic, the ENTIRE group is treated as generic-only. The non-generic overloads are then generated with code that references methods (`RecordCall`, `OnCall`) that don't exist on the generic interceptor.

**Why This Is Hard**: The current architecture assumes **one interceptor class per method name**. This works for:
- Multiple non-generic overloads: Combined parameters with nullable fields
- Multiple generic overloads with same type param count: `Of<T>()` pattern

But it **fails** when a method group contains **both** because these need fundamentally different interceptor structures.

---

## Design Options

### Option 1: Split Groups by "Genericity" (Recommended)

Treat generic and non-generic methods as separate groups with different names:
- `RunRule` -> non-generic interceptor with `RecordCall`, `CallCount`, `OnCall`, `LastCallArgs`
- `RunRuleGeneric` -> generic interceptor with `Of<T>()` pattern

**Pros**: Clean separation, each pattern handles what it's good at, predictable naming
**Cons**: Users might not immediately expect `RunRuleGeneric`

### Option 2: Hybrid Interceptor Class

Generate a single interceptor that supports both patterns:
```csharp
public class RunRuleInterceptor
{
    // Non-generic tracking
    public int CallCount { get; }
    public bool WasCalled => CallCount > 0;
    public void RecordCall(IRule r, CancellationToken? token) { ... }
    public Func<...>? OnCall { get; set; }

    // Generic tracking
    public RunRuleTypedHandler<T> Of<T>() where T : IRule { ... }
}
```

**Pros**: Single interceptor property, intuitive API
**Cons**: Complex implementation, potential naming conflicts, confusing API surface

### Option 3: Numbered Overloads

Extend existing overload numbering (used for multiple non-generic overloads):
- `RunRule` -> non-generic version
- `RunRule1` -> generic version (or vice versa)

**Pros**: Consistent with existing overload numbering pattern
**Cons**: Which gets which number? Arbitrary and hard to remember.

---

## Recommended Approach: Option 1 (Split Groups)

### Naming Convention

1. **Non-generic overloads get the base name**: `RunRule`
2. **Generic overloads get `Generic` suffix**: `RunRuleGeneric`
3. **If only generic overloads exist**: Keep base name without suffix (`Create` not `CreateGeneric`)
4. **If only non-generic overloads exist**: Keep base name (existing behavior)

### Detection Logic

```csharp
var nonGenericOverloads = group.Overloads.Where(o => !o.IsGenericMethod).ToList();
var genericOverloads = group.Overloads.Where(o => o.IsGenericMethod).ToList();
var isMixed = nonGenericOverloads.Count > 0 && genericOverloads.Count > 0;

if (isMixed)
{
    // Generate TWO interceptors with different names
    var nonGenericGroup = CreateSubGroup(group, nonGenericOverloads);
    var genericGroup = CreateSubGroup(group, genericOverloads);

    GenerateNonGenericInterceptor(sb, nonGenericGroup, baseName: group.Name);
    GenerateGenericInterceptor(sb, genericGroup, baseName: group.Name + "Generic");
}
else if (genericOverloads.Count > 0)
{
    GenerateGenericInterceptor(sb, group, baseName: group.Name);
}
else
{
    GenerateNonGenericInterceptor(sb, group, baseName: group.Name);
}
```

---

## Implementation Plan

### Phase 1: Core Infrastructure

- [ ] Add `IsMixed` computed property to `MethodGroupInfo` record
- [ ] Add helper method `SplitMixedGroup(MethodGroupInfo) -> (nonGeneric, generic)`
- [ ] Define naming convention constants (`GenericSuffix = "Generic"`)

### Phase 2: Inline Stub Generation

These methods handle `[KnockOff<IInterface>]` pattern:

- [ ] Update `GenerateInlineStubMethodGroupHandlerClass` (line 6038) to detect mixed groups
- [ ] For mixed groups, call both `GenerateInlineStubNonGenericMethodHandlerClass` and `GenerateInlineStubGenericMethodHandlerClass` with appropriate names
- [ ] Update `GenerateInlineStubInterceptorProperties` to generate two properties for mixed groups
- [ ] Update `GenerateInlineStubMethodImplementation` (line 7371) to route to correct interceptor based on whether the specific overload is generic

### Phase 3: Flat API Generation (Standalone Stubs)

These methods handle `[KnockOff] class Foo : IInterface` pattern:

- [ ] Update flat method group handling (lines 1819-1867) to detect mixed groups
- [ ] Update `GenerateFlatMethodGroupInterceptorClassWithNames` for mixed groups
- [ ] Update `GenerateFlatMethodGroupInterceptorProperties` to generate two properties
- [ ] Update explicit interface implementation generation to route correctly

### Phase 4: Interface-Scoped Generation

For the `stub.IInterface.Method` access pattern:

- [ ] Update `GenerateInterfaceMethodGroupHandlerClass` (line 3057) for mixed groups
- [ ] Update interface method implementation routing

### Phase 5: Testing

- [ ] Create `MixedOverloadTests.cs` with comprehensive test interface
- [ ] Test non-generic overload tracking works independently
- [ ] Test generic overload tracking with `Of<T>()` works independently
- [ ] Test both can coexist on same stub instance
- [ ] Test `Reset()` on each interceptor is independent
- [ ] Uncomment `InlineRuleManagerTests` and verify it works

---

## Files to Modify

| File | Changes |
|------|---------|
| `src/Generator/KnockOffGenerator.cs` | All generation logic changes |
| `src/Tests/KnockOffTests/MixedOverloadTests.cs` | New test file |
| `src/Tests/KnockOffTests/NeatooTests.cs` | Uncomment `InlineRuleManagerTests` |

### Specific Methods to Update in `KnockOffGenerator.cs`

| Line | Method | Change |
|------|--------|--------|
| 1648 | `GroupMethodsByName` | Consider returning split groups, or add `IsMixed` flag |
| 1819-1867 | Flat interceptor generation loop | Handle mixed case |
| 3057 | `GenerateInterfaceMethodGroupHandlerClass` | Handle mixed case |
| 6038 | `GenerateInlineStubMethodGroupHandlerClass` | Handle mixed case |
| 7371 | `GenerateInlineStubMethodImplementation` | Route to correct interceptor |

---

## Test Interface

```csharp
public interface IMixedOverloads
{
    // Mixed: non-generic + generic with same name
    void Process(string data);                        // Non-generic
    void Process<T>(T data);                          // Generic

    // Mixed with return values
    Task<int> Execute(int id);                        // Non-generic
    Task<T> Execute<T>(T item) where T : class;       // Generic

    // Generic-only (no suffix needed)
    T Create<T>() where T : new();

    // Non-generic only (no suffix needed)
    void Log(string message);

    // Multiple non-generic overloads (existing behavior)
    void Save(string path);
    void Save(string path, bool overwrite);
}
```

### Expected Generated API

```csharp
var stub = new Stubs.IMixedOverloads();

// Mixed - separate interceptors
stub.Process.WasCalled;                    // Non-generic tracking
stub.Process.OnCall = (s, data) => { };    // Non-generic callback
stub.ProcessGeneric.Of<int>().WasCalled;   // Generic tracking
stub.ProcessGeneric.Of<int>().OnCall = (s, data) => { };

stub.Execute.CallCount;                    // Non-generic
stub.ExecuteGeneric.Of<string>().CallCount; // Generic

// Generic-only - base name
stub.Create.Of<MyClass>().OnCall = (s) => new MyClass();

// Non-generic only - base name
stub.Log.WasCalled;

// Multiple non-generic - combined parameters (existing)
stub.Save.LastCallArgs; // (string path, bool? overwrite)
```

---

## Discovered

While adding inline stub tests for `IRuleManager` from Neatoo.

## Blocked

- `InlineRuleManagerTests` in `NeatooTests.cs` is commented out pending this fix
- Generated file `InlineRuleManagerTests.Stubs.g.cs` exists but produces compile errors if consumed
