# Bug: Generic Methods Edge Cases

**Created:** 2026-01-09
**Status:** Fixed
**Completed:** 2026-01-09
**Priority:** Medium

## Summary

Two bugs were discovered and fixed:

1. ~~User method detection for generic methods doesn't work~~ **FIXED**
2. ~~Mixed generic/non-generic overloads crash the generator~~ **FIXED**

---

## Bug 1: User Method Detection for Generic Methods

### Description

When a user defines a protected generic method that matches an interface's generic method, the generator should detect it and call the user method from the explicit interface implementation. However, the signature matching doesn't account for type parameters, so generic user methods are never matched.

### Example

```csharp
public interface IFactory
{
    T Create<T>() where T : new();
}

[KnockOff]
public partial class FactoryKnockOff : IFactory
{
    // This should be detected as a user method and called by the generated implementation
    protected T Create<T>() where T : new()
    {
        var instance = new T();
        // Custom logic...
        return instance;
    }
}
```

### Expected Behavior

The generator should:
1. Detect that `protected T Create<T>()` matches interface method `T Create<T>()`
2. Generate an explicit interface implementation that calls the user method

### Actual Behavior

The generator doesn't detect the user method because `GetMethodSignature` doesn't include type parameters in the signature comparison.

### Root Cause

In `KnockOffGenerator.cs` at line ~1687:

```csharp
private static string GetMethodSignature(string name, string returnType, EquatableArray<ParameterInfo> parameters)
{
    var paramTypes = string.Join(",", parameters.Select(p => p.Type));
    return $"{returnType} {name}({paramTypes})";
}
```

The signature `T Create()` for the user method doesn't match `T Create()` for the interface method because:
- Type parameters aren't included in the signature
- The return type `T` may be displayed differently (e.g., fully qualified vs. simple name)

### Fix Required

Update `GetMethodSignature` to include type parameters and constraints, or implement a separate matching logic for generic methods.

---

## Bug 2: Mixed Generic/Non-Generic Overloads Crash Generator

### Description

When an interface has both generic and non-generic overloads of the same method name, the generator crashes with a `KeyNotFoundException`.

### Example

```csharp
public interface IMixedService
{
    void Process(string value);   // Non-generic
    void Process<T>(T value);     // Generic
}

[KnockOff]
public partial class MixedServiceKnockOff : IMixedService { }
```

### Expected Behavior

The generator should:
1. Create separate handlers for each overload (e.g., `Process1`, `Process2`, or similar naming scheme)
2. Generate explicit interface implementations for both overloads

### Actual Behavior

Generator crashes with:
```
error CS8785: Generator 'KnockOffGenerator' failed to generate source.
Exception was of type 'KeyNotFoundException' with message
'The given key 'method:ProcessGeneric()_generic' was not present in the dictionary.'
```

### Root Cause

The flat name map (`BuildFlatNameMap`) doesn't properly handle the combination of generic and non-generic overloads. The key generation for generic methods includes a `_generic` suffix, but the lookup doesn't consistently use this suffix.

### Fix Required

Update the flat name map building and lookup to consistently handle mixed overload scenarios.

---

## Workarounds

### For Bug 1
Don't define user methods for generic interface methods. Use `OnCall` callbacks instead:

```csharp
knockOff.Create.Of<Customer>().OnCall = (ko) => new Customer { /* custom logic */ };
```

### For Bug 2
Avoid interfaces that mix generic and non-generic overloads of the same method name. Split into separate methods:

```csharp
public interface IService
{
    void ProcessString(string value);
    void Process<T>(T value);
}
```

---

## Implementation Notes

### Priority

These are edge cases that don't affect the core generic method functionality:
- Basic generic methods work: `T Get<T>()`, `void Process<T>(T)`, etc.
- Multiple type parameters work: `TOut Convert<TIn, TOut>(TIn)`
- Constraints work: `T Create<T>() where T : class, new()`
- Tracking and callbacks work as designed

### Testing Plan

Once fixed:
1. Add `IGenericMethodWithUserMethod` interface and `GenericMethodWithUserMethodKnockOff` class
2. Add tests verifying user method is called when no OnCall is set
3. Add `IMixedOverloadService` interface with both generic and non-generic overloads
4. Add tests verifying both overloads are tracked separately

---

## Resolution

### Bug 1 Fix: User Method Detection for Generic Methods

**Changes in `KnockOffGenerator.cs`:**

1. **Updated `UserMethodInfo` record** to include `IsGenericMethod` and `TypeParameters`:
   ```csharp
   internal sealed record UserMethodInfo(
       string Name,
       string ReturnType,
       EquatableArray<ParameterInfo> Parameters,
       bool IsGenericMethod,
       EquatableArray<TypeParameterInfo> TypeParameters);
   ```

2. **Updated `GetMethodSignature`** to include type parameters in signature:
   ```csharp
   private static string GetMethodSignature(string name, string returnType,
       EquatableArray<ParameterInfo> parameters, bool isGenericMethod,
       EquatableArray<TypeParameterInfo> typeParameters)
   {
       var paramTypes = string.Join(",", parameters.Select(p => p.Type));
       var typeParamSuffix = isGenericMethod
           ? $"<{string.Join(",", typeParameters.Select(tp => tp.Name))}>"
           : "";
       return $"{returnType} {name}{typeParamSuffix}({paramTypes})";
   }
   ```

3. **Updated user method call generation** to include type arguments:
   ```csharp
   var userMethodTypeArgs = member.IsGenericMethod
       ? $"<{string.Join(", ", member.TypeParameters.Select(tp => tp.Name))}>"
       : "";
   var userMethodCall = $"{member.Name}{userMethodTypeArgs}({paramNames})";
   ```

### Bug 2 Fix: Mixed Generic/Non-Generic Overloads

**Changes in `KnockOffGenerator.cs`:**

1. **Updated `GenerateFlatGenericMethodHandler`** to strip the "Generic" suffix when looking up keys in flatNameMap:
   ```csharp
   // Note: group.Name may have "Generic" suffix from SplitMixedGroup,
   // but the key in flatNameMap uses the original name
   var originalName = group.Name.EndsWith(GenericSuffix, StringComparison.Ordinal)
       ? group.Name.Substring(0, group.Name.Length - GenericSuffix.Length)
       : group.Name;
   var genericKey = $"method:{originalName}()_generic";
   var interceptorName = flatNameMap[genericKey];
   ```

### Test Coverage

Tests added in `GenericMethodBugTests.cs`:
- `GenericMethod_UserMethod_ShouldBeCalledInsteadOfDefault`
- `GenericMethod_UserMethod_WithParameter_ShouldTransformValue`
- `GenericMethod_UserMethod_OnCallTakesPriority`
- `MixedOverloads_NonGeneric_TrackedSeparately`
- `MixedOverloads_Generic_TrackedWithOf`
- `MixedOverloads_AllOverloads_IndependentTracking`
- `MixedOverloads_WithReturnType_BothWork`

All 7 tests pass across net8.0, net9.0, and net10.0.
