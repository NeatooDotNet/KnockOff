# Phase 1 New Features - Enhanced Type Safety

## Summary of Decisions

| Feature | Decision |
|---------|----------|
| Generic ExecutionDetails | **Named tuples** - generates per-member classes with tuple args |
| ExecutionDetailsBase hierarchy | **Minimal** - base provides CallCount/WasCalled, generated classes add specifics |
| AsXYZ() accessors | **Simple cast** - `IPerson AsPerson() => this;` |
| Named parameter access | **Tuples only** - `LastCallArgs?.name`, no individual properties |
| Return value tracking | **Deferred** - not in Phase 1 |
| Callbacks | **Strongly-typed** - future callbacks will use tuple args |
| Breaking changes | **Acceptable** - pre-release, no stability guarantees |

---

## Design

### Generated ExecutionDetails Classes

For each interface member, generate a specific ExecutionDetails class with proper types:

```csharp
// For property: string Name { get; set; }
public sealed class NameExecutionDetails
{
    public int GetCount { get; private set; }
    public int SetCount { get; private set; }
    public string? LastSetValue { get; private set; }

    public void RecordGet() => GetCount++;
    public void RecordSet(string? value) { SetCount++; LastSetValue = value; }
    public void Reset() { GetCount = 0; SetCount = 0; LastSetValue = default; }
}

// For method: void DoWork()
public sealed class DoWorkExecutionDetails
{
    public int CallCount { get; private set; }
    public bool WasCalled => CallCount > 0;

    public void RecordCall() => CallCount++;
    public void Reset() => CallCount = 0;
}

// For method: int GetValue(string name, int count)
public sealed class GetValueExecutionDetails
{
    private readonly List<(string name, int count)> _calls = new();

    public int CallCount => _calls.Count;
    public bool WasCalled => _calls.Count > 0;

    public (string name, int count)? LastCallArgs =>
        _calls.Count > 0 ? _calls[_calls.Count - 1] : null;

    public IReadOnlyList<(string name, int count)> AllCalls => _calls;

    public void RecordCall(string name, int count) => _calls.Add((name, count));
    public void Reset() => _calls.Clear();
}
```

**Usage:**
```csharp
var knockOff = new ServiceKnockOff();
IService service = knockOff;

service.GetValue("test", 42);

// Strongly typed access with named tuple elements!
var args = knockOff.ExecutionInfo.GetValue.LastCallArgs;
Console.WriteLine(args?.name);   // "test"
Console.WriteLine(args?.count);  // 42

// Or destructure
if (knockOff.ExecutionInfo.GetValue.LastCallArgs is var (name, count))
{
    Console.WriteLine($"Called with {name}, {count}");
}
```

### Base Classes (Minimal)

After reflection, we may not need base classes at all. Each generated class is self-contained with exactly what it needs. However, if we want shared behavior (like a common `Reset()` pattern or interface for testing utilities), we could have:

```csharp
// Optional - only if we need polymorphism
public interface IExecutionDetails
{
    int CallCount { get; }
    bool WasCalled { get; }
    void Reset();
}
```

**Decision:** Start without base classes. Add them later only if a concrete need emerges (e.g., "reset all tracking" utility method).

### AsXYZ() Interface Accessors

Simple cast for discoverability:

```csharp
// Generated in partial class
public IPerson AsPerson() => this;
public IEmployee AsEmployee() => this;
```

**Usage:**
```csharp
var knockOff = new PersonKnockOff();

// These are equivalent:
IPerson p1 = knockOff;
IPerson p2 = knockOff.AsPerson();

// But AsXYZ() is more discoverable via IntelliSense
knockOff.AsPerson().Name = "Test";
```

**Future Option B (documented, not implemented):**
Rich wrapper that combines interface access with execution info - defer until there's a concrete use case.

---

## Implementation Plan

### Step 1: Update Generator Transform Model

Add types to track for code generation:
- Property type for properties
- Parameter names and types for methods
- Return type for methods (for future use)

```csharp
record InterfaceMemberInfo(
    string Name,
    string ReturnType,
    bool IsProperty,
    bool HasGetter,
    bool HasSetter,
    bool IsNullable,
    EquatableArray<ParameterInfo> Parameters  // Already have this
);
```

### Step 2: Generate Per-Member ExecutionDetails Classes

For each interface member, generate a nested class inside the partial class:

```csharp
partial class PersonKnockOff
{
    // Generated ExecutionDetails classes
    public sealed class NameExecutionDetails { ... }
    public sealed class GetValueExecutionDetails { ... }

    // ExecutionInfo uses them
    public sealed class PersonKnockOffExecutionInfo
    {
        public NameExecutionDetails Name { get; } = new();
        public GetValueExecutionDetails GetValue { get; } = new();
    }

    public PersonKnockOffExecutionInfo ExecutionInfo { get; } = new();
}
```

### Step 3: Update Recording Calls

Change from `RecordCall(params object?[] args)` to strongly-typed:

```csharp
// Old
ExecutionInfo.GetValue.RecordCall(name, count);  // object?[]

// New
ExecutionInfo.GetValue.RecordCall(name, count);  // (string, int)
```

### Step 4: Generate AsXYZ() Methods

For each interface the class implements:

```csharp
public IInterface1 AsInterface1() => this;
public IInterface2 AsInterface2() => this;
```

### Step 5: Remove Old ExecutionDetails.cs

The generic `ExecutionDetails` class is replaced by generated per-member classes.

---

## Test Updates

```csharp
[Fact]
public void Method_TracksArgs_WithNamedTuple()
{
    var knockOff = new SampleKnockOff();
    ISampleService service = knockOff;

    service.GetValue("test", 42);

    var args = knockOff.ExecutionInfo.GetValue.LastCallArgs;
    Assert.NotNull(args);
    Assert.Equal("test", args.Value.name);
    Assert.Equal(42, args.Value.count);
}

[Fact]
public void Property_TracksSetValue_Typed()
{
    var knockOff = new SampleKnockOff();
    ISampleService service = knockOff;

    service.Name = "Hello";

    // Strongly typed - no cast needed
    string? lastValue = knockOff.ExecutionInfo.Name.LastSetValue;
    Assert.Equal("Hello", lastValue);
}

[Fact]
public void AsInterface_ReturnsTypedInterface()
{
    var knockOff = new SampleKnockOff();

    ISampleService service = knockOff.AsSampleService();

    service.Name = "Test";
    Assert.Equal(1, knockOff.ExecutionInfo.Name.SetCount);
}
```

---

## Future Considerations

### Callbacks with Strongly-Typed Args

When implementing callbacks (Phase 9), they should receive the tuple:

```csharp
// Future API
knockOff.ExecutionInfo.GetValue.OnCall = (args) =>
{
    Console.WriteLine($"Called with {args.name}, {args.count}");
    return args.count * 2;  // Return value
};
```

### Return Value Tracking (Deferred)

Not in Phase 1, but design should not preclude adding later:

```csharp
// Future possibility
public sealed class GetValueExecutionDetails
{
    public (string name, int count)? LastCallArgs { get; }
    public int? LastReturnValue { get; }  // Future
}
```
