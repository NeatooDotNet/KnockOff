# Interface Inheritance

KnockOff supports interface inheritance, automatically implementing members from base interfaces.

## Basic Usage

<!-- snippet: interface-inheritance-basic-usage -->
```cs
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
<!-- endSnippet -->

The generator implements:
- `Id` and `CreatedAt` from `IBaseEntity`
- `ModifiedAt` and `ModifiedBy` from `IAuditableEntity`

## Tracking

All members from all inherited interfaces are tracked. With the flat API, all members are accessed directly on the stub regardless of which interface declared them:

<!-- snippet: interface-inheritance-tracking -->
```cs
// Access base interface properties
var id = entity.Id;
var created = entity.CreatedAt;

// Access derived interface properties
entity.ModifiedAt = DateTime.Now;
entity.ModifiedBy = "TestUser";

// All members tracked directly on the stub (flat API)
var idCount = knockOff.Id.GetCount;           // 1
var createdCount = knockOff.CreatedAt.GetCount; // 1
var modAtCount = knockOff.ModifiedAt.SetCount;  // 1
var modByCount = knockOff.ModifiedBy.SetCount;  // 1
```
<!-- endSnippet -->

## AsXYZ() Methods

Helper methods are generated for both base and derived interfaces:

<!-- snippet: interface-inheritance-interface-access -->
```cs
// Access as derived interface via implicit conversion
IIhAuditableEntity auditable = knockOff;

// Access as base interface
IIhBaseEntity baseEntity = knockOff;

// Same underlying instance
var areSame = ReferenceEquals(knockOff, auditable);  // true
```
<!-- endSnippet -->

## Callbacks

Set callbacks for any member directly on the stub (flat API):

<!-- snippet: interface-inheritance-callbacks -->
```cs
// Base interface member
knockOff.Id.OnGet = (ko) => 42;

// Derived interface member
knockOff.ModifiedBy.OnGet = (ko) => "System";
knockOff.ModifiedAt.OnSet = (ko, value) =>
{
    Console.WriteLine($"Modified at {value}");
};
```
<!-- endSnippet -->

## Deep Inheritance

KnockOff handles multiple levels of inheritance:

<!-- snippet: interface-inheritance-deep-inheritance -->
```cs
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
<!-- endSnippet -->

All members from all levels are generated:
- `Id` from `IEntity`
- `CreatedAt` from `ITimestampedEntity`
- `CreatedBy` and `ModifiedBy` from `IAuditableEntity`

## Common Patterns

### Entity Base Pattern

<!-- snippet: interface-inheritance-entity-base -->
```cs
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
<!-- endSnippet -->

### Validation Pattern

<!-- snippet: interface-inheritance-validation-pattern -->
```cs
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
<!-- endSnippet -->

<!-- snippet: interface-inheritance-validation-usage -->
```cs
// Configure validation (flat API)
knockOff.IsValid.OnGet = (ko) => ko.Total.GetCount > 0;
knockOff.GetErrors.OnCall = (ko) =>
    ko.IsValid.OnGet!(ko) ? [] : ["No total calculated"];
```
<!-- endSnippet -->

### Repository Hierarchy

<!-- snippet: interface-inheritance-repository-hierarchy -->
```cs
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
<!-- endSnippet -->

<!-- snippet: interface-inheritance-repository-usage -->
```cs
// All members accessed via flat API regardless of declaring interface
knockOff.GetById.OnCall = (ko, id) => users.FirstOrDefault(u => u.Id == id);
knockOff.GetAll.OnCall = (ko) => users;
knockOff.Add.OnCall = (ko, user) => users.Add(user);
knockOff.Delete.OnCall = (ko, id) => users.RemoveAll(u => u.Id == id);
```
<!-- endSnippet -->
