---
skill: knockoff
topic: troubleshooting
audience: all
---

# Troubleshooting KnockOff Stubs

Common issues, diagnostics, and debugging guide.

## Quick Debugging Checklist

When something isn't working:

1. **Is the class `partial`?** - Required for code generation
2. **Is the project building?** - Check for compilation errors
3. **Is OnCall set correctly?** - First param must be stub instance (`ko`)
4. **Class stubs using `.Object`?** - Required to get the class instance
5. **Check interceptor name** - Overloads get numeric suffixes

## Common Issues

### 1. Stub Not Generated

**Symptom:** `StubClass` doesn't have interceptors, IntelliSense shows nothing

**Causes:**

#### Missing `partial` keyword

```csharp
// WRONG - not partial
[KnockOff]
public class UserStub : IUser { }

// CORRECT
[KnockOff]
public partial class UserStub : IUser { }
```

**Fix:** Add `partial` keyword.

#### Build errors

Check build output for errors:
- CS0534: Missing interface implementation
- CS0535: Not implementing interface member
- CS8019: Unnecessary using directive (warnings, not errors)

**Fix:** Resolve build errors first, then rebuild.

#### Wrong target framework

KnockOff requires:
- .NET 8.0 or later
- C# 12 or later (for partial properties support in C# 13+)

**Fix:** Update `<TargetFramework>` in `.csproj`.

### 2. Wrong Interceptor Name

**Symptom:** `knockOff.Method` doesn't exist, but `knockOff.Method2` does

**Cause:** User method collision or overload suffix.

#### User Method Collision

When a user method matches the interface method, suffix is added:

```csharp
public interface IService
{
    int GetValue(int id);
}

[KnockOff]
public partial class ServiceStub : IService
{
    protected int GetValue(int id) => id * 2;  // User method exists
}

// Interceptor is GetValue2 (not GetValue)
knockOff.GetValue2.OnCall((ko, id) => id * 100);
```

**Fix:** Use the suffixed name (`GetValue2`) or remove the user method.

#### Overload Suffix

Overloaded methods get numeric suffixes:

```csharp
public interface IProcessor
{
    void Process(string data);        // Process1
    void Process(string data, int n); // Process2
}

knockOff.Process1.OnCall((ko, data) => { });
knockOff.Process2.OnCall((ko, data, n) => { });
```

**No overloads = no suffix:**
```csharp
public interface ISingle
{
    void Method();  // No suffix: knockOff.Method
}
```

### 3. Callback Not Called

**Symptom:** OnCall is set, but callback never executes

**Causes:**

#### Wrong signature (missing `ko`)

```csharp
// WRONG - missing ko parameter
knockOff.GetUser.OnCall((id) => new User());

// CORRECT
knockOff.GetUser.OnCall((ko, id) => new User());
```

**Fix:** First parameter must always be the stub instance.

#### Priority order

User method takes priority if no callback is set:

```csharp
[KnockOff]
public partial class ServiceStub : IService
{
    protected int GetValue(int id) => id * 2;
}

// This returns 10 (user method), not 100 (no callback set)
service.GetValue(5);

// Set callback to override
knockOff.GetValue2.OnCall((ko, id) => id * 100);
service.GetValue(5);  // Now returns 500
```

**Priority:** OnCall > User method > Smart default

#### Wrong interceptor name

```csharp
// Overloaded - need suffix
knockOff.Process.OnCall(...);  // ❌ doesn't exist
knockOff.Process1.OnCall(...); // ✅ correct
```

### 4. Type Mismatches

**Symptom:** `Cannot convert` or `Type mismatch` errors

#### Generic Methods - Missing `.Of<T>()`

```csharp
// WRONG
knockOff.Deserialize.OnCall((ko, json) => new User());

// CORRECT - specify type argument
knockOff.Deserialize.Of<User>().OnCall((ko, json) => new User());
```

#### Class Stubs - Forgetting `.Object`

```csharp
[KnockOff<EmailService>]
public partial class Tests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.EmailService();

        // WRONG - stub is the wrapper
        var service = new MyService(stub);

        // CORRECT - use .Object
        var service = new MyService(stub.Object);
    }
}
```

#### Interface Casting

For standalone stubs, cast to interface:

```csharp
var knockOff = new UserServiceStub();

// Interface instance
IUserService service = knockOff;  // Implicit cast
service.Method();

// Or explicit helper
IUserService service2 = knockOff.AsIUserService();
```

### 5. Using Moq Syntax

**Symptom:** Trying to use Moq methods on KnockOff stub

```csharp
// WRONG - Moq syntax
knockOff.Setup(x => x.Method()).Returns(value);
knockOff.Verify(x => x.Method(), Times.Once);

// CORRECT - KnockOff syntax
knockOff.Method.OnCall((ko) => value);
Assert.Equal(1, knockOff.Method.CallCount);
```

See [moq-migration.md](moq-migration.md) for migration guide.

### 6. Multiple Interfaces (KO0010)

**Symptom:** Diagnostic KO0010 when trying to implement multiple interfaces

```csharp
// WRONG - standalone stubs support only one interface
[KnockOff]
public partial class MultiStub : IRepository, IUnitOfWork { }
// Error KO0010
```

**Fix:** Use separate stubs or inline stubs:

```csharp
// Option 1: Separate standalone stubs
[KnockOff]
public partial class RepositoryStub : IRepository { }

[KnockOff]
public partial class UnitOfWorkStub : IUnitOfWork { }

// Option 2: Inline stubs (supports multiple)
[KnockOff<IRepository>]
[KnockOff<IUnitOfWork>]
public partial class Tests { }
```

## Diagnostics Reference

### KO0008: Type Parameter Mismatch

**Cause:** Generic type parameter mismatch

```csharp
public interface IService<T> { }

[KnockOff]
public partial class ServiceStub<T> : IService<int> { }  // Mismatch
```

**Fix:** Match type parameters exactly.

### KO0010: Multiple Interfaces with Same Signature

**Cause:** Standalone stub implements multiple interfaces with conflicting signatures

```csharp
public interface IA { void Method(); }
public interface IB { void Method(); }

[KnockOff]
public partial class Stub : IA, IB { }  // KO0010
```

**Fix:** Use separate stubs or interface inheritance pattern.

### KO1001: Invalid Type

**Cause:** Attempting to stub invalid types (structs, static classes, sealed classes)

```csharp
[KnockOff]
public partial class StructStub : IMyStruct { }  // KO1001
```

**Fix:** Only stub interfaces, unsealed classes, or delegates.

### KO2001: Sealed Class

**Cause:** Attempting to stub a sealed class

```csharp
public sealed class SealedService { }

[KnockOff<SealedService>]  // KO2001
public partial class Tests { }
```

**Fix:** Sealed classes cannot be stubbed.

### KO2003: Non-Virtual Member

**Warning:** Non-virtual members in class stubs are not stubbed

```csharp
public class Service
{
    public void NonVirtual() { }  // Warning KO2003
    public virtual void Virtual() { }  // OK
}

[KnockOff<Service>]
public partial class Tests { }
```

**Fix:** Make member `virtual` or `abstract`, or accept that base implementation is called.

## Debugging Tips

### View Generated Code

Enable source generator output in `.csproj`:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

After rebuild, check:
```
obj/Debug/net9.0/generated/KnockOff.Generator/KnockOff.KnockOffGenerator/
```

### Inspect Interceptor Classes

Generated interceptors are nested classes:

```csharp
public partial class UserServiceStub
{
    public sealed class GetUser2Interceptor
    {
        public int CallCount { get; private set; }
        public Func<UserServiceStub, int, User?>? OnCall { get; set; }
        // ...
    }

    public GetUser2Interceptor GetUser2 { get; } = new();
}
```

### Testing Pattern

Use "create objects then test them" pattern:

```csharp
[Fact]
public void Test()
{
    // 1. Create stub
    var knockOff = new UserServiceStub();

    // 2. Configure
    knockOff.GetUser2.OnCall((ko, id) => new User { Id = id });

    // 3. Get interface instance
    IUserService service = knockOff;

    // 4. Use in test
    var user = service.GetUser(42);

    // 5. Verify
    Assert.Equal(42, user.Id);
    Assert.Equal(1, knockOff.GetUser2.CallCount);
}
```

### Check Package Version

Ensure you're on the latest version:

```bash
dotnet list package | findstr KnockOff
```

Update:
```bash
dotnet add package KnockOff
```

## When to STOP and Ask

From CLAUDE.md guidelines:

**STOP and ask** when you hit an obstacle:
- Code isn't testable as-is
- API doesn't exist for your scenario
- Pattern doesn't fit

Don't push through with workarounds. Ask the user for guidance.

## Next Steps

- [creating-stubs.md](creating-stubs.md) - Stub patterns
- [interceptor-api.md](interceptor-api.md) - API reference
- [moq-migration.md](moq-migration.md) - Moq → KnockOff migration
- [advanced.md](advanced.md) - Advanced scenarios
