# Diagnostics Reference

KnockOff emits compiler diagnostics when it encounters issues. All diagnostic IDs use the `KO` prefix.

## Quick Reference

| ID | Severity | Description |
|----|----------|-------------|
| KO0008 | Error | Type parameter count mismatch |
| KO0010 | Error | Multiple interfaces on standalone stub |
| KO1001 | Error | Type must be interface, class, or delegate |
| KO1002 | Error | Name collision between types |
| KO1003 | Error | Stubs type conflict |
| KO2001 | Error | Cannot stub sealed class |
| KO2002 | Error | No accessible constructors |
| KO2003 | Info | Non-virtual member skipped |
| KO2004 | Warning | No virtual members |
| KO2005 | Error | Cannot stub static class |
| KO2006 | Error | Cannot stub built-in type |

## Standalone Stub Diagnostics (KO0xxx)

### KO0008: Type Parameter Count Mismatch

**Severity:** Error

**Message:** Generic standalone stub '{0}' has {1} type parameter(s) but interface '{2}' has {3}. Type parameter count must match exactly.

**Cause:** A generic standalone stub has a different number of type parameters than the interface it implements.

**Fix:**

<!-- invalid:diag-ko0008 -->
```csharp
// ERROR: Stub has 1 type param, interface has 2
[KnockOff]
public partial class RepoStub<T> : IRepository<T, int> { }

// CORRECT: Match the type parameters
[KnockOff]
public partial class RepoStub<T, TKey> : IRepository<T, TKey> { }
```
<!-- /snippet -->

### KO0010: Multiple Interfaces

**Severity:** Error

**Message:** KnockOff stubs should implement a single interface. Create separate stubs for {0}.

**Cause:** A standalone stub implements multiple unrelated interfaces.

**Fix:**

<!-- invalid:diag-ko0010 -->
```csharp
// ERROR: Multiple interfaces
[KnockOff]
public partial class ServiceStub : IService, IDisposable { }

// CORRECT: Separate stubs
[KnockOff]
public partial class ServiceStub : IService { }

[KnockOff]
public partial class DisposableStub : IDisposable { }

// OR: Use inline pattern for multiple interfaces
[KnockOff<IService>]
[KnockOff<IDisposable>]
public partial class MyTests { }
```
<!-- /snippet -->

## Inline Stub Diagnostics (KO1xxx)

### KO1001: Type Must Be Interface, Class, or Delegate

**Severity:** Error

**Message:** Type '{0}' must be an interface, class, or named delegate type.

**Cause:** The type argument to `[KnockOff<T>]` is not a valid type for stubbing.

**Fix:**

<!-- invalid:diag-ko1001 -->
```csharp
// ERROR: Cannot stub struct
[KnockOff<DateTime>]
public partial class MyTests { }

// ERROR: Cannot stub enum
[KnockOff<DayOfWeek>]
public partial class MyTests { }

// CORRECT: Use interface, class, or delegate
[KnockOff<IService>]
[KnockOff<EmailService>]
[KnockOff<Func<int, string>>]
public partial class MyTests { }
```
<!-- /snippet -->

### KO1002: Name Collision

**Severity:** Error

**Message:** Multiple types named '{0}' found; use explicit [KnockOff] pattern for disambiguation.

**Cause:** Two or more interfaces with the same simple name (from different namespaces) are being stubbed inline.

**Fix:**

<!-- invalid:diag-ko1002 -->
```csharp
// ERROR: Both namespaces have IService
[KnockOff<Namespace1.IService>]
[KnockOff<Namespace2.IService>]
public partial class MyTests { }

// CORRECT: Use standalone stubs with explicit names
[KnockOff]
public partial class Service1Stub : Namespace1.IService { }

[KnockOff]
public partial class Service2Stub : Namespace2.IService { }
```
<!-- /snippet -->

### KO1003: Stubs Type Conflict

**Severity:** Error

**Message:** Type 'Stubs' conflicts with generated nested class; rename existing type or use explicit pattern.

**Cause:** The containing class already has a nested type named `Stubs`.

**Fix:**

<!-- invalid:diag-ko1003 -->
```csharp
// ERROR: Existing Stubs class conflicts
[KnockOff<IService>]
public partial class MyTests
{
    public class Stubs { }  // Conflict!
}

// CORRECT: Rename existing type
[KnockOff<IService>]
public partial class MyTests
{
    public class TestHelpers { }  // Renamed
}
```
<!-- /snippet -->

## Class Stub Diagnostics (KO2xxx)

### KO2001: Cannot Stub Sealed Class

**Severity:** Error

**Message:** Cannot stub sealed class '{0}'.

**Cause:** Attempting to stub a sealed class.

**Fix:**

<!-- invalid:diag-ko2001 -->
```csharp
// ERROR: HttpClient is sealed
[KnockOff<HttpClient>]
public partial class MyTests { }

// CORRECT: Stub an interface instead
[KnockOff<IHttpClientFactory>]
public partial class MyTests { }
```
<!-- /snippet -->

### KO2002: No Accessible Constructors

**Severity:** Error

**Message:** Type '{0}' has no accessible constructors.

**Cause:** The class has no public or protected constructors.

**Fix:**

<!-- invalid:diag-ko2002 -->
```csharp
// ERROR: Class has private constructor
[KnockOff<SingletonService>]  // SingletonService has private ctor
public partial class MyTests { }

// CORRECT: Create a wrapper or use interface
public interface IServiceWrapper { }

[KnockOff<IServiceWrapper>]
public partial class MyTests { }
```
<!-- /snippet -->

### KO2003: Non-Virtual Member Skipped

**Severity:** Info

**Message:** Member '{0}.{1}' is not virtual and cannot be intercepted.

**Cause:** A class member is not virtual or abstract and cannot be overridden.

**Note:** This is informational only. The member will use its real implementation.

### KO2004: No Virtual Members

**Severity:** Warning

**Message:** Class '{0}' has no virtual or abstract members to intercept.

**Cause:** A class stub was created but the class has no virtual or abstract members.

**Fix:**

<!-- invalid:diag-ko2004 -->
```csharp
// WARNING: No virtual members
public class Service
{
    public void DoWork() { }  // Not virtual
}

[KnockOff<Service>]
public partial class MyTests { }

// BETTER: Use interface instead
public interface IService
{
    void DoWork();
}

[KnockOff<IService>]
public partial class MyTests { }
```
<!-- /snippet -->

### KO2005: Cannot Stub Static Class

**Severity:** Error

**Message:** Cannot stub static class '{0}'.

**Cause:** Attempting to stub a static class.

**Fix:**

<!-- invalid:diag-ko2005 -->
```csharp
// ERROR: Cannot stub static class
[KnockOff<File>]  // System.IO.File is static
public partial class MyTests { }

// CORRECT: Create interface wrapper
public interface IFileSystem
{
    bool Exists(string path);
    string ReadAllText(string path);
}

[KnockOff<IFileSystem>]
public partial class MyTests { }
```
<!-- /snippet -->

### KO2006: Cannot Stub Built-In Type

**Severity:** Error

**Message:** Cannot stub built-in type '{0}'.

**Cause:** Attempting to stub a built-in framework type (string, object, ValueType, Enum, Delegate, Array).

**Fix:**

<!-- invalid:diag-ko2006 -->
```csharp
// ERROR: Cannot stub built-in types
[KnockOff<string>]
[KnockOff<object>]
public partial class MyTests { }

// CORRECT: Use your own interfaces/classes
[KnockOff<IStringProcessor>]
public partial class MyTests { }
```
<!-- /snippet -->

## Suppressing Diagnostics

To suppress a diagnostic in your code:

<!-- pseudo:diag-suppress-pragma -->
```csharp
#pragma warning disable KO2003  // Non-virtual member skipped
[KnockOff<MyClass>]
public partial class MyTests { }
#pragma warning restore KO2003
```
<!-- /snippet -->

Or in your `.editorconfig`:

<!-- pseudo:diag-suppress-editorconfig -->
```ini
[*.cs]
dotnet_diagnostic.KO2003.severity = none
```
<!-- /snippet -->
