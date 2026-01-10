# Bug: Inherited Interface Methods with Different Parameter Types

**Status:** Fixed
**Discovered in:** KnockOff.NeatooInterfaceTests
**Related errors:** CS1503 (cannot convert argument type)
**Fixed in:** KnockOffGenerator.cs - Added delegation for base interface methods

## Problem

When a generic interface inherits from a non-generic base interface, and both have methods with the same name but different parameter types (the derived uses the type parameter, the base uses a fixed type), the generator incorrectly shares one interceptor instead of creating separate ones.

## Reproduction

Interface hierarchy:
```csharp
public interface IRule
{
    Task<IRuleMessages> RunRule(IValidateBase target, CancellationToken? token);
}

public interface IRule<T> : IRule where T : IValidateBase
{
    Task<IRuleMessages> RunRule(T target, CancellationToken? token);
}
```

When generating a stub for `IRule<ICustomValidateBase>`:
- The generator creates one `IRule_RunRuleInterceptor` with `RecordCall(ICustomValidateBase target, ...)`
- The generic implementation `IRule<ICustomValidateBase>.RunRule(ICustomValidateBase target, ...)` works correctly
- The base implementation `IRule.RunRule(IValidateBase target, ...)` fails because it tries to pass `IValidateBase` to the interceptor expecting `ICustomValidateBase`

## Generated Code (Current - Broken)

```csharp
public sealed class IRule_RunRuleInterceptor
{
    public void RecordCall(ICustomValidateBase target, CancellationToken? token) { ... }
    public Func<IRule, ICustomValidateBase, CancellationToken?, Task<IRuleMessages>>? OnCall { get; set; }
}

// Works - parameter type matches
Task<IRuleMessages> global::Neatoo.Rules.IRule<ICustomValidateBase>.RunRule(ICustomValidateBase target, CancellationToken? token)
{
    RunRule.RecordCall(target, token);  // OK: ICustomValidateBase -> ICustomValidateBase
    ...
}

// Broken - parameter type mismatch
Task<IRuleMessages> global::Neatoo.Rules.IRule.RunRule(IValidateBase target, CancellationToken? token)
{
    RunRule.RecordCall(target, token);  // ERROR: IValidateBase -> ICustomValidateBase
    ...
}
```

## Possible Solutions

### Option 1: Separate interceptors per parameter type

Create `IRule_RunRule_IValidateBase_Interceptor` and `IRule_RunRule_ICustomValidateBase_Interceptor`.

**Pros:** Clean separation, each interceptor has correct types
**Cons:** More complex API, potential naming conflicts

### Option 2: Cast in base method implementation

```csharp
Task<IRuleMessages> global::Neatoo.Rules.IRule.RunRule(IValidateBase target, CancellationToken? token)
{
    // Cast and delegate to the derived implementation
    return ((IRule<ICustomValidateBase>)this).RunRule((ICustomValidateBase)target, token);
}
```

**Pros:** Single interceptor, simpler API
**Cons:** Runtime cast could fail if called with non-matching type

### Option 3: Base interceptor with object parameters

Create the interceptor with the most general type (`IValidateBase`), use casts in the typed implementation.

**Pros:** Works for both cases
**Cons:** Loses type safety in interceptor delegates

## Affected Files

- `src/Tests/KnockOff.NeatooInterfaceTests/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/IRuleOfCustomTypeTests.Stubs.g.cs`
- Similar patterns in other `IRule<T>` implementations

## Test Coverage

**Build errors (current failing tests):**
- `IRuleOfCustomTypeTests.Stubs.g.cs` lines 179-180: CS1503 type conversion errors

**Unit tests (will run once build passes):**
- `IRuleOfCustomTypeTests.InlineStub_CanBeInstantiated` - basic instantiation
- `IRuleOfCustomTypeTests.InlineStub_ImplementsInterface` - implements `IRule<ICustomValidateBase>`
- `IRuleOfCustomTypeTests.RunRule_AcceptsCustomType` - derived interface method works
- `IRuleOfCustomTypeTests.RunRule_BaseInterfaceMethod_WorksWithDerivedType` - **regression test** for base interface method

## Solution Implemented

**Approach:** Option 2 - Cast and delegate to typed implementation

**Changes:**
- Added `FindDelegationTarget()` helper to detect base interface methods with typed counterparts
- Added `GenerateInlineStubDelegationImplementation()` to generate delegation code
- Modified `GenerateInlineStubClass()` to check for delegation before generating normal interceptor code

**Generated code (after fix):**
```csharp
// Base interface method now delegates to typed implementation
Task<IRuleMessages> IRule.RunRule(IValidateBase target, CancellationToken? token)
{
    return ((IRule<ICustomValidateBase>)this).RunRule((ICustomValidateBase)target, token);
}
```

## Task List

- [x] Add regression test for base interface method
- [x] Decide on solution approach (Option 2: cast and delegate)
- [x] Implement fix in `KnockOffGenerator.cs`
- [x] Verify fix with NeatooInterfaceTests build (CS1503 errors resolved)
- [x] Run tests to confirm behavior (224 Documentation.Samples.Tests pass)
