# Interface Inheritance

KnockOff supports interface inheritance, automatically implementing members from base interfaces.

## Basic Usage

<!-- snippet: docs:interface-inheritance:basic-usage -->
```csharp
public interface IIhBaseEntity
{
    int Id { get; }
    DateTime CreatedAt { get; }
}

public interface IIhAuditableEntity : IIhBaseEntity
{
    DateTime? ModifiedAt { get; set; }
    string ModifiedBy { get; set; }
}

[KnockOff]
public partial class IhAuditableEntityKnockOff : IIhAuditableEntity { }
```
<!-- /snippet -->

The generator implements:
- `Id` and `CreatedAt` from `IBaseEntity`
- `ModifiedAt` and `ModifiedBy` from `IAuditableEntity`

## Tracking

All members from all inherited interfaces are tracked. Each interface gets its own spy class with handlers for the members it defines:

```csharp
var knockOff = new AuditableEntityKnockOff();
IAuditableEntity entity = knockOff;

// Access base interface properties
var id = entity.Id;
var created = entity.CreatedAt;

// Access derived interface properties
entity.ModifiedAt = DateTime.Now;
entity.ModifiedBy = "TestUser";

// Base interface members tracked in IBaseEntity spy
Assert.Equal(1, knockOff.IBaseEntity.Id.GetCount);
Assert.Equal(1, knockOff.IBaseEntity.CreatedAt.GetCount);

// Derived interface members tracked in IAuditableEntity spy
Assert.Equal(1, knockOff.IAuditableEntity.ModifiedAt.SetCount);
Assert.Equal(1, knockOff.IAuditableEntity.ModifiedBy.SetCount);
```

## AsXYZ() Methods

Helper methods are generated for both base and derived interfaces:

```csharp
var knockOff = new AuditableEntityKnockOff();

// Access as derived interface
IAuditableEntity auditable = knockOff.AsAuditableEntity();

// Access as base interface
IBaseEntity baseEntity = knockOff.AsBaseEntity();

// Same underlying instance
Assert.Same(knockOff, auditable);
Assert.Same(knockOff, baseEntity);
```

## Accessing Base via Derived

Since the derived interface inherits from the base, you can access base members through the derived reference:

```csharp
IAuditableEntity entity = knockOff;

// Base interface members accessible through derived
var id = entity.Id;
var createdAt = entity.CreatedAt;

// Tracked in the base interface's spy class
Assert.Equal(1, knockOff.IBaseEntity.Id.GetCount);
Assert.Equal(1, knockOff.IBaseEntity.CreatedAt.GetCount);
```

## Callbacks

Set callbacks for any member using its defining interface's spy class:

```csharp
// Base interface member (via IBaseEntity spy)
knockOff.IBaseEntity.Id.OnGet = (ko) => 42;

// Derived interface member (via IAuditableEntity spy)
knockOff.IAuditableEntity.ModifiedBy.OnGet = (ko) => "System";
knockOff.IAuditableEntity.ModifiedAt.OnSet = (ko, value) =>
{
    // Track modification timestamps
    Console.WriteLine($"Modified at {value}");
};
```

## Deep Inheritance

KnockOff handles multiple levels of inheritance:

<!-- snippet: docs:interface-inheritance:deep-inheritance -->
```csharp
public interface IIhEntity
{
    int Id { get; }
}

public interface IIhTimestampedEntity : IIhEntity
{
    DateTime CreatedAt { get; }
}

public interface IIhFullAuditableEntity : IIhTimestampedEntity
{
    string CreatedBy { get; }
    string? ModifiedBy { get; set; }
}

[KnockOff]
public partial class IhFullEntityKnockOff : IIhFullAuditableEntity { }
```
<!-- /snippet -->

All members from all levels are generated:
- `Id` from `IEntity`
- `CreatedAt` from `ITimestampedEntity`
- `CreatedBy` and `ModifiedBy` from `IAuditableEntity`

## Common Patterns

### Entity Base Pattern

<!-- snippet: docs:interface-inheritance:entity-base -->
```csharp
public interface IIhEntityBase
{
    int Id { get; }
}

public interface IIhEmployee : IIhEntityBase
{
    string Name { get; set; }
    string Department { get; set; }
}

[KnockOff]
public partial class IhEmployeeKnockOff : IIhEmployee { }
```
<!-- /snippet -->

```csharp
// Pre-populate properties using interface-prefixed backing fields
knockOff.IIhEntityBase_IdBacking = 1;
knockOff.IIhEmployee_NameBacking = "Test Employee";
```

### Validation Pattern

<!-- snippet: docs:interface-inheritance:validation-pattern -->
```csharp
public interface IIhValidatable
{
    bool IsValid { get; }
    IEnumerable<string> GetErrors();
}

public interface IIhOrder : IIhValidatable
{
    decimal Total { get; }
    void Submit();
}

[KnockOff]
public partial class IhOrderKnockOff : IIhOrder { }
```
<!-- /snippet -->

```csharp
// Configure validation (using interface spy classes)
knockOff.IIhValidatable.IsValid.OnGet = (ko) => ko.IIhOrder.Total.GetCount > 0;
knockOff.IIhValidatable.GetErrors.OnCall = (ko) =>
    ko.IIhValidatable.IsValid.OnGet!(ko) ? [] : ["No total calculated"];
```

### Repository Hierarchy

<!-- snippet: docs:interface-inheritance:repository-hierarchy -->
```csharp
public interface IIhReadRepository<T>
{
    T? GetById(int id);
    IEnumerable<T> GetAll();
}

public interface IIhWriteRepository<T> : IIhReadRepository<T>
{
    void Add(T entity);
    void Delete(int id);
}

[KnockOff]
public partial class IhUserWriteRepositoryKnockOff : IIhWriteRepository<IhUser> { }
```
<!-- /snippet -->

```csharp
// Base interface members via IIhReadRepository_IhUser
knockOff.IIhReadRepository_IhUser.GetById.OnCall = (ko, id) => users.FirstOrDefault(u => u.Id == id);
knockOff.IIhReadRepository_IhUser.GetAll.OnCall = (ko) => users;

// Derived interface members via IIhWriteRepository_IhUser
knockOff.IIhWriteRepository_IhUser.Add.OnCall = (ko, user) => users.Add(user);
knockOff.IIhWriteRepository_IhUser.Delete.OnCall = (ko, id) => users.RemoveAll(u => u.Id == id);
```
