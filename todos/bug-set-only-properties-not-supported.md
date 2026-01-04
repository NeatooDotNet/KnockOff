# Bug: Set-Only Properties Not Supported

**Status:** Open
**Severity:** Low
**Discovered:** 2026-01-03

## Summary

Properties with only a setter (no getter) generate invalid C# because the backing field is created with only a setter.

## Reproduction

```csharp
public interface ILogger
{
    string Output { set; }  // Set-only property
}

[KnockOff]
public partial class LoggerKnockOff : ILogger { }
```

## Generated Code (Invalid)

```csharp
// CS8051: Auto-implemented property must have a get accessor
protected string OutputBacking { set; } = "";

string ILogger.Output
{
    set
    {
        Spy.Output.RecordSet(value);
        if (Spy.Output.OnSet is { } onSetCallback)
            onSetCallback(this, value);
        else
            OutputBacking = value;
    }
}
```

## Root Cause

The generator creates a backing property that mirrors the interface property's accessors. When the interface has only a setter, the backing property also has only a setter, which is invalid C#.

## Possible Fixes

1. **Always include getter on backing property** - Generate `protected string OutputBacking { get; set; }` regardless of interface
2. **Use backing field instead** - Generate `private string _outputBacking;` for set-only properties
3. **Emit diagnostic error** - Detect set-only properties and report as unsupported

## Workaround

Use get/set properties instead of set-only:

```csharp
public interface ILogger
{
    string Output { get; set; }  // Include getter
}
```

Or provide a custom implementation in the partial class.
