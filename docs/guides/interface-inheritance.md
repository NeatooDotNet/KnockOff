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

All members from all inherited interfaces are tracked:

```csharp
var knockOff = new AuditableEntityKnockOff();
IAuditableEntity entity = knockOff;

// Access base interface properties
var id = entity.Id;
var created = entity.CreatedAt;

// Access derived interface properties
entity.ModifiedAt = DateTime.Now;
entity.ModifiedBy = "TestUser";

// All tracked in Spy
Assert.Equal(1, knockOff.Spy.Id.GetCount);
Assert.Equal(1, knockOff.Spy.CreatedAt.GetCount);
Assert.Equal(1, knockOff.Spy.ModifiedAt.SetCount);
Assert.Equal(1, knockOff.Spy.ModifiedBy.SetCount);
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

Assert.Equal(1, knockOff.Spy.Id.GetCount);
Assert.Equal(1, knockOff.Spy.CreatedAt.GetCount);
```

## Callbacks

Set callbacks for any member, regardless of which interface level defines it:

```csharp
// Base interface member
knockOff.Spy.Id.OnGet = (ko) => 42;

// Derived interface member
knockOff.Spy.ModifiedBy.OnGet = (ko) => "System";
knockOff.Spy.ModifiedAt.OnSet = (ko, value) =>
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

// Pre-populate base properties
knockOff.IdBacking = 1;
knockOff.NameBacking = "Test Employee";
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

// Configure validation
knockOff.Spy.IsValid.OnGet = (ko) => ko.Spy.Total.GetCount > 0;
knockOff.Spy.GetErrors.OnCall = (ko) =>
    ko.Spy.IsValid.OnGet!(ko) ? [] : ["No total calculated"];
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

// All methods available
knockOff.Spy.GetById.OnCall = (ko, id) => users.FirstOrDefault(u => u.Id == id);
knockOff.Spy.GetAll.OnCall = (ko) => users;
knockOff.Spy.Add.OnCall = (ko, user) => users.Add(user);
knockOff.Spy.Delete.OnCall = (ko, id) => users.RemoveAll(u => u.Id == id);
```
