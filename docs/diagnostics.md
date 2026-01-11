# KnockOff Diagnostics

KnockOff reports diagnostics when it encounters issues during code generation. This page documents all diagnostic codes and how to resolve them.

## Inline Stub Diagnostics (KO1xxx)

These diagnostics apply to inline stubs using `[KnockOff<T>]`.

### KO1001: Type must be interface, class, or delegate

**Severity:** Error

**Message:** Type '{0}' must be an interface, class, or named delegate type

**Cause:** The type argument to `[KnockOff<T>]` is not a supported type.

**Solution:** Use an interface, unsealed class, or named delegate type:

```csharp
// Error: struct not supported
[KnockOff<MyStruct>]  // KO1001

// Correct: interface
[KnockOff<IMyService>]

// Correct: class
[KnockOff<MyService>]

// Correct: delegate
[KnockOff<MyCallback>]
```

### KO1002: Name collision

**Severity:** Error

**Message:** Multiple types named '{0}' found; use explicit [KnockOff] pattern for disambiguation

**Cause:** Multiple `[KnockOff<T>]` attributes reference types with the same simple name from different namespaces.

**Solution:** Use the explicit standalone stub pattern instead:

```csharp
// Error: same name from different namespaces
[KnockOff<Namespace1.IService>]
[KnockOff<Namespace2.IService>]  // KO1002
public partial class MyTests { }

// Correct: use standalone stubs
[KnockOff]
public partial class Service1Stub : Namespace1.IService { }

[KnockOff]
public partial class Service2Stub : Namespace2.IService { }
```

### KO1003: Stubs type conflict

**Severity:** Error

**Message:** Type 'Stubs' conflicts with generated nested class; rename existing type or use explicit pattern

**Cause:** The class already contains a nested type named `Stubs`, which conflicts with the generated stub container.

**Solution:** Rename the existing `Stubs` type or use standalone stubs:

```csharp
// Error: existing Stubs type
[KnockOff<IService>]
public partial class MyTests
{
    public class Stubs { }  // KO1003: conflicts with generated Stubs
}

// Correct: rename existing type
[KnockOff<IService>]
public partial class MyTests
{
    public class TestStubs { }  // Renamed
}
```

## Standalone Stub Diagnostics (KO0xxx)

These diagnostics apply to standalone stubs using `[KnockOff]` on a class that implements an interface.

### KO0008: Type parameter count mismatch

**Severity:** Error

**Message:** Generic standalone stub '{0}' has {1} type parameter(s) but interface '{2}' has {3}. Type parameter count must match exactly.

**Cause:** A generic standalone stub has a different number of type parameters than the interface it implements.

**Solution:** Ensure the stub class has the same number of type parameters as the interface:

```csharp
public interface IRepository<T> { }

// Error: mismatched arity
[KnockOff]
public partial class BadStub<T, TExtra> : IRepository<T> { }  // KO0008: 2 vs 1

// Correct: matching arity
[KnockOff]
public partial class GoodStub<T> : IRepository<T> { }
```

### KO0010: Multiple interfaces on standalone stub

**Severity:** Error

**Message:** KnockOff stubs should implement a single interface. Create separate stubs for {0}.

**Cause:** A standalone stub implements multiple interfaces.

**Solution:** Create separate stub classes for each interface:

```csharp
// Error: multiple interfaces
[KnockOff]
public partial class BadStub : IService, IRepository { }  // KO0010

// Correct: separate stubs
[KnockOff]
public partial class ServiceStub : IService { }

[KnockOff]
public partial class RepositoryStub : IRepository { }
```

## Class Stub Diagnostics (KO2xxx)

These diagnostics apply to class stubs using `[KnockOff<T>]` where `T` is a class.

### KO2001: Cannot stub sealed class

**Severity:** Error

**Message:** Cannot stub sealed class '{0}'

**Cause:** The target class is sealed and cannot be inherited.

**Solution:** The class must be unsealed to create a stub. If you control the class, remove the `sealed` modifier. Otherwise, extract an interface.

```csharp
public sealed class SealedService { }  // Cannot stub

// Option 1: unseal if you control it
public class UnsealedService { }

// Option 2: extract interface
public interface IService { }
public sealed class SealedService : IService { }
// Then stub the interface instead
```

### KO2002: No accessible constructors

**Severity:** Error

**Message:** Type '{0}' has no accessible constructors

**Cause:** The target class has no public or protected constructors that the stub can call.

**Solution:** Add an accessible constructor or extract an interface:

```csharp
public class NoConstructor
{
    private NoConstructor() { }  // KO2002: not accessible
}

// Solution: add accessible constructor
public class WithConstructor
{
    protected WithConstructor() { }  // Accessible to stub
}
```

### KO2003: Non-virtual member skipped

**Severity:** Info

**Message:** Member '{0}.{1}' is not virtual and cannot be intercepted

**Cause:** A member on the target class is not virtual or abstract, so it cannot be overridden by the stub.

**Resolution:** This is informational only. Non-virtual members are accessible through `.Object` but cannot have callbacks configured. To intercept, make the member virtual:

```csharp
public class MyService
{
    public void NonVirtual() { }        // KO2003: skipped, not interceptable
    public virtual void Virtual() { }   // Intercepted
}
```

### KO2004: No virtual members

**Severity:** Warning

**Message:** Class '{0}' has no virtual or abstract members to intercept

**Cause:** The target class has no virtual or abstract members, so the stub provides no interception capability.

**Resolution:** Consider whether you need to stub this class. If you need to intercept behavior, make members virtual or extract an interface.

### KO2005: Cannot stub static class

**Severity:** Error

**Message:** Cannot stub static class '{0}'

**Cause:** Static classes cannot be instantiated or inherited.

**Solution:** Static classes cannot be stubbed. Refactor to use an instance-based design or wrap static calls in an interface.

### KO2006: Cannot stub built-in type

**Severity:** Error

**Message:** Cannot stub built-in type '{0}'

**Cause:** Built-in types like `string`, `int`, `object` cannot be stubbed.

**Solution:** Built-in types should not be stubbed. Use actual values in tests.
