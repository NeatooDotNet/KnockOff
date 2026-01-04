# Bug: Same Parameter Name with Different Types in Overloads

**Status:** FIXED (by overload-handlers-per-signature implementation)
**Severity:** Medium
**Discovered:** 2026-01-03
**Fixed:** 2026-01-03

## Summary

When an interface has method overloads where the **same parameter name has different types**, the generator produces invalid code.

## Reproduction

```csharp
public interface ISameNameDifferentType
{
    void Process(string input);
    void Process(int input);  // Same name 'input', different type!
}

[KnockOff]
public partial class SameNameDifferentTypeKnockOff : ISameNameDifferentType { }
```

## Generated Code (Buggy)

```csharp
public sealed class ISameNameDifferentType_ProcessHandler
{
    // Uses first overload's type for storage
    private readonly List<string> _calls = new();

    // Works fine
    public void RecordCall(string input) => _calls.Add(input);

    // BUG: CS1503 - cannot convert from 'int' to 'string'
    public void RecordCall(int input) => _calls.Add(input);

    // Also uses first type
    public string? LastCallArg => ...;
}
```

## Root Cause

In `GenerateMethodGroupHandlerClass`, when building the unified tracking type:
1. It collects unique parameter names across all overloads
2. For single-param methods, it uses the first overload's type for the list
3. It doesn't detect when the same parameter name has different types across overloads

## Possible Fixes

### Option 1: Type-suffixed names in tuple
Use `(string? input_string, int? input_int)` - works but ugly naming.

### Option 2: Separate tracking per overload
```csharp
private readonly List<string> _calls0 = new();  // Process(string)
private readonly List<int> _calls1 = new();     // Process(int)

public IReadOnlyList<string> AllCalls0 => _calls0;
public IReadOnlyList<int> AllCalls1 => _calls1;
```

### Option 3: Use object storage
```csharp
private readonly List<object?> _calls = new();
```
Loses type safety but works.

### Option 4: Emit diagnostic
Detect this case and emit a warning/error suggesting the user rename parameters or use separate stubs.

## Recommended Fix

**Option 2 (separate tracking per overload)** seems cleanest. It maintains type safety and provides clear access patterns:
```csharp
// Access overload-specific tracking
var stringCalls = handler.AllCalls0;  // List<string>
var intCalls = handler.AllCalls1;     // List<int>

// Or unified count
Assert.Equal(5, handler.CallCount);  // Total across all overloads
```

## Workaround

Use different parameter names in overloads:
```csharp
public interface ISameNameDifferentType
{
    void Process(string textInput);
    void Process(int numericInput);  // Different name - works!
}
```

Or split into separate interfaces (if from external library, use separate stubs).
