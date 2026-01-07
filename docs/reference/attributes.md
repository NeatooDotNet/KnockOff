# Attributes Reference

## [KnockOff]

The primary attribute that marks a class for code generation.

### Declaration

```csharp
namespace KnockOff;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class KnockOffAttribute : Attribute
{
}
```

### Usage

```csharp
using KnockOff;

[KnockOff]
public partial class MyServiceKnockOff : IMyService
{
}
```

### Requirements

1. **Class must be `partial`** — The generator adds to the class
2. **Must implement at least one interface** — The generator needs interface members
3. **Cannot be `abstract`** — Must be instantiable
4. **Cannot be `static`** — Must be an instance class

### Valid Examples

```csharp
// Basic usage
[KnockOff]
public partial class ServiceKnockOff : IService { }

// Multiple interfaces
[KnockOff]
public partial class DataContextKnockOff : IRepository, IUnitOfWork { }

// Generic interface (with concrete type)
[KnockOff]
public partial class UserRepoKnockOff : IRepository<User> { }

// Interface inheritance
[KnockOff]
public partial class AuditableKnockOff : IAuditableEntity { }

// Internal class
[KnockOff]
internal partial class InternalServiceKnockOff : IService { }

// Nested class
public class TestFixture
{
    [KnockOff]
    public partial class NestedKnockOff : IService { }
}
```

### Invalid Examples

```csharp
// Missing partial keyword - COMPILE ERROR
[KnockOff]
public class ServiceKnockOff : IService { }

// No interface - GENERATOR WARNING
[KnockOff]
public partial class NoInterfaceKnockOff { }

// Abstract class - GENERATOR ERROR
[KnockOff]
public abstract partial class AbstractKnockOff : IService { }

// Static class - COMPILE ERROR
[KnockOff]
public static partial class StaticKnockOff : IService { }

// Generic KnockOff class - NOT SUPPORTED
[KnockOff]
public partial class GenericKnockOff<T> : IRepository<T> { }
```

### Namespace

The attribute is in the `KnockOff` namespace:

```csharp
using KnockOff;
```

Or use fully qualified:

```csharp
[KnockOff.KnockOff]
public partial class ServiceKnockOff : IService { }
```

## Future Attribute Options

The following options are under consideration for future versions:

### Naming Customization

```csharp
// Hypothetical - NOT YET IMPLEMENTED
[KnockOff(KOPropertyName = "Mock")]
public partial class ServiceKnockOff : IService { }
// Would generate: public ServiceKnockOffKO Mock { get; }
```

### Strict Mode

```csharp
// Hypothetical - NOT YET IMPLEMENTED
[KnockOff(Strict = true)]
public partial class ServiceKnockOff : IService { }
// Would throw if any member is called without setup
```

### Include/Exclude Members

```csharp
// Hypothetical - NOT YET IMPLEMENTED
[KnockOff(Exclude = ["Dispose"])]
public partial class ServiceKnockOff : IService, IDisposable { }
// Would not generate handler for Dispose
```

These are design considerations only. The current implementation has no attribute options.
