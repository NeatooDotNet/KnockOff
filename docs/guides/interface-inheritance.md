# Interface Inheritance

KnockOff supports interface inheritance, automatically implementing members from base interfaces.

## Basic Usage

```csharp
public interface IBaseEntity
{
    int Id { get; }
    DateTime CreatedAt { get; }
}

public interface IAuditableEntity : IBaseEntity
{
    DateTime? ModifiedAt { get; set; }
    string ModifiedBy { get; set; }
}

[KnockOff]
public partial class AuditableEntityKnockOff : IAuditableEntity { }
```

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

```csharp
public interface IEntity
{
    int Id { get; }
}

public interface ITimestampedEntity : IEntity
{
    DateTime CreatedAt { get; }
}

public interface IAuditableEntity : ITimestampedEntity
{
    string CreatedBy { get; }
    string? ModifiedBy { get; set; }
}

[KnockOff]
public partial class FullEntityKnockOff : IAuditableEntity { }
```

All members from all levels are generated:
- `Id` from `IEntity`
- `CreatedAt` from `ITimestampedEntity`
- `CreatedBy` and `ModifiedBy` from `IAuditableEntity`

## Common Patterns

### Entity Base Pattern

```csharp
public interface IEntity
{
    int Id { get; }
}

public interface IEmployee : IEntity
{
    string Name { get; set; }
    string Department { get; set; }
}

[KnockOff]
public partial class EmployeeKnockOff : IEmployee { }

// Pre-populate properties using interface-prefixed backing fields
knockOff.IEntity_IdBacking = 1;
knockOff.IEmployee_NameBacking = "Test Employee";
```

### Validation Pattern

```csharp
public interface IValidatable
{
    bool IsValid { get; }
    IEnumerable<string> GetErrors();
}

public interface IOrder : IValidatable
{
    decimal Total { get; }
    void Submit();
}

[KnockOff]
public partial class OrderKnockOff : IOrder { }

// Configure validation (using interface spy classes)
knockOff.IValidatable.IsValid.OnGet = (ko) => ko.IOrder.Total.GetCount > 0;
knockOff.IValidatable.GetErrors.OnCall = (ko) =>
    ko.IValidatable.IsValid.OnGet!(ko) ? [] : ["No total calculated"];
```

### Repository Hierarchy

```csharp
public interface IReadRepository<T>
{
    T? GetById(int id);
    IEnumerable<T> GetAll();
}

public interface IWriteRepository<T> : IReadRepository<T>
{
    void Add(T entity);
    void Delete(int id);
}

[KnockOff]
public partial class UserWriteRepositoryKnockOff : IWriteRepository<User> { }

// Base interface members via IReadRepository_User
knockOff.IReadRepository_User.GetById.OnCall = (ko, id) => users.FirstOrDefault(u => u.Id == id);
knockOff.IReadRepository_User.GetAll.OnCall = (ko) => users;

// Derived interface members via IWriteRepository_User
knockOff.IWriteRepository_User.Add.OnCall = (ko, user) => users.Add(user);
knockOff.IWriteRepository_User.Delete.OnCall = (ko, id) => users.RemoveAll(u => u.Id == id);
```
