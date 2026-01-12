# Attributes Reference

## [KnockOff]

The primary attribute that marks a class for code generation.

### Declaration

<!-- pseudo:knockoff-attribute-declaration -->
```csharp
namespace KnockOff;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class KnockOffAttribute : Attribute
{
}
```
<!-- /snippet -->

### Usage

<!-- snippet: attributes-knockoff-usage -->
```cs
[KnockOff]
public partial class AttrMyServiceKnockOff : IAttrService
{
}
```
<!-- endSnippet -->

### Requirements

1. **Class must be `partial`** — The generator adds to the class
2. **Must implement at least one interface** — The generator needs interface members
3. **Cannot be `abstract`** — Must be instantiable
4. **Cannot be `static`** — Must be an instance class

### Valid Examples

<!-- snippet: attributes-valid-examples -->
```cs
// Basic usage
[KnockOff]
public partial class AttrServiceKnockOff : IAttrService { }

// Generic interface (with concrete type)
[KnockOff]
public partial class AttrUserRepoKnockOff : IAttrRepository<AttrUser> { }

// Interface inheritance
[KnockOff]
public partial class AttrAuditableKnockOff : IAttrAuditableEntity { }

// Internal class
[KnockOff]
internal partial class AttrInternalServiceKnockOff : IAttrService { }

// Nested class
public partial class AttrTestFixture
{
    [KnockOff]
    public partial class NestedKnockOff : IAttrService { }
}
```
<!-- endSnippet -->

### Invalid Examples

<!-- invalid:knockoff-invalid-examples -->
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
<!-- /snippet -->

### Namespace

The attribute is in the `KnockOff` namespace:

<!-- pseudo:knockoff-namespace-using -->
```csharp
using KnockOff;
```
<!-- /snippet -->

Or use fully qualified:

<!-- snippet: attributes-namespace-qualified -->
```cs
[KnockOff.KnockOff]
public partial class AttrQualifiedServiceKnockOff : IAttrService { }
```
<!-- endSnippet -->

## Future Attribute Options

The following options are under consideration for future versions:

### Naming Customization

<!-- pseudo:knockoff-future-naming -->
```csharp
// Hypothetical - NOT YET IMPLEMENTED
[KnockOff(KOPropertyName = "Mock")]
public partial class ServiceKnockOff : IService { }
// Would generate: public ServiceKnockOffKO Mock { get; }
```
<!-- /snippet -->

### Strict Mode

<!-- pseudo:knockoff-future-strict -->
```csharp
// Hypothetical - NOT YET IMPLEMENTED
[KnockOff(Strict = true)]
public partial class ServiceKnockOff : IService { }
// Would throw if any member is called without setup
```
<!-- /snippet -->

### Include/Exclude Members

<!-- pseudo:knockoff-future-exclude -->
```csharp
// Hypothetical - NOT YET IMPLEMENTED
[KnockOff(Exclude = ["Dispose"])]
public partial class ServiceKnockOff : IService, IDisposable { }
// Would not generate handler for Dispose
```
<!-- /snippet -->

These are design considerations only. The current implementation has no attribute options.
