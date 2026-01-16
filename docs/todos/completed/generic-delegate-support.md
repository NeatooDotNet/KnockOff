# Generic Delegate Support

**Status:** Complete
**Completed:** v10.20

## Problem

KnockOff supports **closed** generic delegates but not **open** generic delegates.

**Works:**
```csharp
public delegate T Factory<T>();

[KnockOff<Factory<string>>]  // Closed - T is string
public partial class MyTests { }

// Generates: MyTests.Stubs.Factory (implements Factory<string>)
```

**Not Supported:**
```csharp
public delegate T Factory<T>();

// Standalone stub - IMPOSSIBLE (can't inherit from delegate)
[KnockOff]
public partial class FactoryStub<T> : Factory<T> { }  // Compile error!

// Open generic inline - NOT IMPLEMENTED
[KnockOff(typeof(Factory<>))]
public partial class MyTests { }  // Currently doesn't work
```

## Use Case

User wants to stub a generic delegate without pre-declaring every type argument:

```csharp
public delegate T Factory<T>();

// Desired:
var stringFactory = new MyTests.Stubs.Factory<string>();
var intFactory = new MyTests.Stubs.Factory<int>();
```

## Current Workaround

Create separate inline stubs for each closed type needed:

```csharp
[KnockOff<Factory<string>>]
[KnockOff<Factory<int>>]
[KnockOff<Factory<User>>]
public partial class MyTests { }

// Use: new MyTests.Stubs.FactoryString(), etc.
```

## Solution

Use `typeof()` syntax with an open generic delegate. The generator creates a generic stub class:

```csharp
// User writes (non-generic test class):
[KnockOff(typeof(Factory<>))]
public partial class MyTests { }

// Generator produces:
public partial class MyTests
{
    public static partial class Stubs
    {
        public class Factory<T>  // Generator makes the stub generic
        {
            private T _returnValue;

            public T Invoke()
            {
                InvokeCallCount++;
                return _returnValue;
            }

            public int InvokeCallCount { get; private set; }

            public static implicit operator global::Factory<T>(Factory<T> stub)
                => stub.Invoke;
        }
    }
}
```

### Usage in Tests

```csharp
public class MyTests
{
    [Fact]
    public void Factory_Returns_String()
    {
        var stub = new MyTests.Stubs.Factory<string>();
        stub._returnValue = "hello";

        Factory<string> factory = stub;
        Assert.Equal("hello", factory());
    }

    [Fact]
    public void Factory_Returns_Int()
    {
        var stub = new MyTests.Stubs.Factory<int>();
        stub._returnValue = 42;

        Factory<int> factory = stub;
        Assert.Equal(42, factory());
    }
}
```

### Why This Works

- Test class stays non-generic (xUnit compatible)
- Generic is on the stub itself, where it belongs
- Stays inline with test class (consistent with other inline stubs)
- No need to pre-declare types - use any type argument at instantiation

## Task List

- [ ] Add support for `[KnockOff(typeof(Delegate<>))]` syntax in attribute parsing
- [ ] Detect open generic delegates (unbound type arguments)
- [ ] Generate generic stub class with type parameters from delegate
- [ ] Handle multiple type parameters: `Factory<T, U>` → `Stubs.Factory<T, U>`
- [ ] Preserve type constraints from delegate definition
- [ ] Add tests for open generic delegate stubs
- [ ] Update documentation

## Technical Notes

### Why Standalone Doesn't Work

C# doesn't allow inheriting from delegates:
```csharp
class MyClass : SomeDelegate { }  // CS0509: cannot derive from sealed type 'SomeDelegate'
```

### Generator Detection

```csharp
// In transform phase:
if (attribute is { AttributeClass.Name: "KnockOffAttribute" }
    && attribute.ConstructorArguments[0].Value is INamedTypeSymbol typeSymbol
    && typeSymbol.IsUnboundGenericType)
{
    // Open generic delegate - generate generic stub
    var typeParams = typeSymbol.TypeParameters;  // [T] or [T, U], etc.
}
```

## Priority

Medium - workaround exists but is verbose for multiple type arguments.

## Related

- Generic standalone stubs for interfaces (implemented in v10.14)
- Closed generic delegate stubs (working)
