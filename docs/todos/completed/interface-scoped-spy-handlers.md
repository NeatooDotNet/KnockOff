# Plan: Interface-Scoped Spy Handlers (Drop Spy Property)

## Problem
When a KnockOff stub implements multiple interfaces with same-named members having different signatures, the current flat `Spy.Method` structure can't handle type conflicts.

## Solution
Drop `Spy` property entirely. Interface spy classes become direct properties on the KnockOff class:

```csharp
// Old (current)
knockOff.Spy.Log.CallCount

// New (single interface)
knockOff.ILogger.Log.CallCount

// New (multiple interfaces)
knockOff.ILogger.Log.CallCount
knockOff.INotifier.Notify.CallCount
```

Always use interface prefix, even for single-interface stubs (consistency).

## Name Collision Handling

**Problem:** What if an interface has a member named `ILogger` (same as the interface spy property)?

```csharp
public interface ILogger
{
    string ILogger { get; }  // Property named "ILogger" - collides with spy accessor!
}
```

**Solution:** Detect collision and suffix the spy accessor:

```csharp
// Generated when collision detected:
public ILoggerSpy ILogger_ { get; } = new();  // Suffixed with underscore

// Or emit a diagnostic warning suggesting rename
```

**Detection logic:**
```csharp
var interfaceSpyName = interfaceInfo.SimpleName;  // "ILogger"
var memberNames = interfaceInfo.Members.Select(m => m.Name).ToHashSet();

if (memberNames.Contains(interfaceSpyName))
{
    // Collision! Use "ILogger_" or "ILoggerSpy" as property name
    interfaceSpyName = interfaceSpyName + "_";
}
```

**Alternative:** Use full interface name with underscores: `KnockOff_Tests_ILogger` - but this is ugly. Prefer underscore suffix for the rare collision case.

## Current Structure (to understand what changes)

```
KnockOffGenerator.cs key locations:
- Lines 1785-1863: Data models (KnockOffTypeInfo, InterfaceInfo, etc.)
- Lines 560-588: Member deduplication (currently flattens across interfaces)
- Lines 1400-1434: GenerateSpyClass() - creates flat Spy
- Lines 688-875: GenerateMemberHandlerClass() - property handlers
- Lines 960-1168: GenerateMethodGroupHandlerClass() - method handlers
- Lines 1490-1525: GeneratePropertyImplementation() - uses Spy.{Name}
- Lines 1589-1756: GenerateMethod() - uses Spy.{Name}
```

## Implementation Steps

### 1. Remove Spy Class, Add Interface Spy Classes
Replace the single `{ClassName}Spy` class with per-interface spy classes:

**Old structure:**
```csharp
public sealed class FooKnockOffSpy { ... }
public FooKnockOffSpy Spy { get; } = new();
```

**New structure:**
```csharp
public sealed class ILoggerSpy
{
    public LogHandler Log { get; } = new();
    public NameHandler Name { get; } = new();
}
public ILoggerSpy ILogger { get; } = new();

public sealed class INotifierSpy
{
    public NotifyHandler Notify { get; } = new();
    public NameHandler Name { get; } = new();  // Own handler, own types!
}
public INotifierSpy INotifier { get; } = new();
```

### 2. Generate Handlers Per-Interface (No Deduplication)
Currently lines 560-588 dedupe members across interfaces. Remove this:
- Each interface gets its own handlers
- Handlers are nested inside interface spy class (no naming conflicts)

### 3. Handle Spy Property Name Collisions
Before generating `public ILoggerSpy ILogger { get; }`:
```csharp
string spyPropertyName = interfaceInfo.SimpleName;
var memberNames = interfaceInfo.Members.Select(m => m.Name).ToHashSet();
if (memberNames.Contains(spyPropertyName))
{
    spyPropertyName += "_";  // ILogger_ to avoid collision
}
```

### 4. Update Interface Implementations
All implementations now use interface-prefixed spy access:

```csharp
// Property implementation
string ILogger.Name
{
    get
    {
        ILogger.Name.RecordGet();  // Was: Spy.Name.RecordGet()
        if (ILogger.Name.OnGet is { } cb) return cb(this);
        return NameBacking;
    }
}

// Method implementation
void ILogger.Log(string message)
{
    ILogger.Log.RecordCall(message);  // Was: Spy.Log.RecordCall()
    if (ILogger.Log.GetCallback() is { } cb) { cb(this, message); return; }
}
```

### 5. Keep AsXxx() Methods
These still make sense for casting convenience:
```csharp
public ILogger AsLogger() => this;
public INotifier AsNotifier() => this;
```

### 6. Separate Backing Properties Per Interface
Each interface gets its own backing field, even if property names/types match:

```csharp
// ILogger.Name and INotifier.Name each get their own backing
protected string ILogger_NameBacking { get; set; } = "";
protected string INotifier_NameBacking { get; set; } = "";
```

This provides complete isolation between interfaces - changing `ILogger.Name` doesn't affect `INotifier.Name`.

**Naming convention:** `{InterfaceSimpleName}_{PropertyName}Backing`

## Files to Modify

| File | Changes |
|------|---------|
| `src/Generator/KnockOffGenerator.cs` | All changes - single file generator |

## Specific Code Changes

### A. Replace GenerateSpyClass() with GenerateInterfaceSpyClasses()
Generate one spy class per interface, each containing handlers for that interface's members.

### B. Add collision detection
Check if interface simple name collides with any member name, suffix with `_` if so.

### C. Update all implementation generators
Pass interface info to `GeneratePropertyImplementation()`, `GenerateMethod()`, `GenerateEventImplementation()`.
Use `{InterfaceSpyPropertyName}.{MemberName}` instead of `Spy.{MemberName}`.

### D. Remove cross-interface deduplication
Current code at lines 560-588 dedupes by signature. Change to iterate interfaces directly.

## Testing

1. **All existing tests break** - update from `Spy.X` to `IInterface.X`
2. Update `MultiInterfaceKnockOff` tests
3. Add test for conflicting signatures (the original bug - now works)
4. Add test for spy property name collision (`ILogger` property on `ILogger` interface)

## Breaking Changes

**This is a breaking change for all users:**
- `Spy.Method` → `IInterface.Method`
- `Spy.Property` → `IInterface.Property`
- `Spy` property removed entirely

Document in release notes. Major version bump recommended.

## Checklist

**Status: ✅ COMPLETE**

### Generator Changes
- [x] Create `GenerateInterfaceSpyClass()` method
- [x] Replace `GenerateSpyClass()` call with per-interface generation
- [x] Add spy property name collision detection (suffix with `_`)
- [x] Remove `Spy` property generation
- [x] Remove cross-interface member deduplication
- [x] Update backing property naming: `{Interface}_{Property}Backing`
- [x] Update `GeneratePropertyImplementation()` to take interface context
- [x] Update `GenerateMethod()` to take interface context
- [x] Update `GenerateEventImplementation()` to take interface context
- [x] Update `GenerateIndexerImplementation()` if applicable

### Tests
- [x] Update all existing tests (`Spy.X` → `IInterface.X`)
- [x] Add test: conflicting method signatures across interfaces (original bug)
- [x] Add test: spy property name collision

### Documentation
- [x] Update getting-started.md (migration guide)
- [x] Update/remove bug todo file
